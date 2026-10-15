/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Concurrent;
using System.Diagnostics;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Observability.Models;
using Jube.AskJooby.Routing.Interfaces;
using Jube.AskJooby.Routing.Models;
using Jube.AskJooby.Workflows;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Routing
{
    public static class AgentRouter
    {
        private const string DefaultAgentType = "general";
        private const int EmbeddingTimeoutSeconds = 5;
        private static readonly ConcurrentDictionary<string, float[]> exemplarEmbeddingsCache = new();

        public static async Task<string> RouteAsync(
            string task, IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>>? embedder,
            IReadOnlyDictionary<string, AgentDefinition> availableAgents, CancellationToken token = default,
            string? recentContext = null, ChatOptions? chatOptions = null,
            IAgentRoutingLedger? ledger = null, IRoutingContinuityClient? continuity = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var observed = new RoutingObservation();

            async Task<string> RecordAsync(AgentRoutingStage stage, string agentType)
            {
                stopwatch.Stop();

                if (ledger is not null)
                {
                    await ledger.RecordAsync(new AgentRoutingDecision(stage, agentType, observed.Score,
                        observed.EmbeddingRequests, observed.EmbeddingValues, stopwatch.ElapsedMilliseconds,
                        observed.Error), token).ConfigureAwait(false);
                }

                return agentType;
            }

            var candidates = RoutingRules.All.Where(rule => availableAgents.ContainsKey(rule.AgentType)).ToList();

            if (candidates.Count == 0)
            {
                return await RecordAsync(AgentRoutingStage.NoCandidates, DefaultAgentType).ConfigureAwait(false);
            }

            var regexMatch = candidates.FirstOrDefault(rule => rule.Patterns.Any(pattern => pattern.IsMatch(task)));

            if (regexMatch is not null)
            {
                return await RecordAsync(AgentRoutingStage.Pattern, regexMatch.AgentType).ConfigureAwait(false);
            }

            if (continuity is not null
                && await StickyAgentAsync(continuity, availableAgents, token).ConfigureAwait(false)
                    is { } sticky)
            {
                return await RecordAsync(AgentRoutingStage.Continuity, sticky).ConfigureAwait(false);
            }

            if (embedder is not null)
            {
                try
                {
                    var (embeddingMatch, bestScore, requests, values) =
                        await RouteByEmbeddingAsync(task, embedder, candidates, token).ConfigureAwait(false);

                    observed.EmbeddingRequests = requests;
                    observed.EmbeddingValues = values;
                    observed.Score = bestScore;

                    if (embeddingMatch is not null)
                    {
                        return await RecordAsync(AgentRoutingStage.Embedding, embeddingMatch)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    observed.Error = ex.Message;
                }
            }

            var llmMatch = await RouteByLlmAsync(task, chatClient, candidates, recentContext, chatOptions, token)
                .ConfigureAwait(false);

            if (llmMatch is not null && availableAgents.ContainsKey(llmMatch))
            {
                return await RecordAsync(AgentRoutingStage.Model, llmMatch).ConfigureAwait(false);
            }

            return await RecordAsync(AgentRoutingStage.Default, DefaultAgentType).ConfigureAwait(false);
        }

        public static async Task<int> WarmExemplarCacheAsync(
            IEmbeddingGenerator<string, Embedding<float>> embedder, CancellationToken token = default)
        {
            var uncached = RoutingRules.All
                .SelectMany(rule => rule.Exemplars)
                .Where(exemplar => !exemplarEmbeddingsCache.ContainsKey(exemplar))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (uncached.Count == 0)
            {
                return 0;
            }

            var generated = await embedder.GenerateAsync(uncached, cancellationToken: token)
                .ConfigureAwait(false);

            if (generated.Count != uncached.Count)
            {
                return 0;
            }

            for (var index = 0; index < uncached.Count; index++)
            {
                exemplarEmbeddingsCache.TryAdd(uncached[index], generated[index].Vector.ToArray());
            }

            return uncached.Count;
        }

        private static async Task<string?> StickyAgentAsync(IRoutingContinuityClient continuity,
            IReadOnlyDictionary<string, AgentDefinition> availableAgents, CancellationToken token)
        {
            try
            {
                var position = await continuity.ReadAsync(token).ConfigureAwait(false);

                if (position?.LastAgentType is not { } agentType
                    || !availableAgents.ContainsKey(agentType)
                    || WorkflowPosition.Find(position.WorkflowId) is not { } workflow)
                {
                    return null;
                }

                return WorkflowPosition.Outstanding(workflow, position.Progress).Count > 0
                    ? agentType
                    : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return null;
            }
        }

        private static async Task<(string? AgentType, double Score, int Requests, int Values)>
            RouteByEmbeddingAsync(
                string task, IEmbeddingGenerator<string, Embedding<float>> embedder,
                IReadOnlyList<RoutingRule> candidates, CancellationToken token)
        {
            var uncached = candidates
                .SelectMany(rule => rule.Exemplars)
                .Where(exemplar => !exemplarEmbeddingsCache.ContainsKey(exemplar))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var batch = new List<string>(uncached.Count + 1) { task };
            batch.AddRange(uncached);

            using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(EmbeddingTimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token);

            try
            {
                var generated = await embedder.GenerateAsync(batch, cancellationToken: linked.Token)
                    .ConfigureAwait(false);

                if (generated.Count != batch.Count)
                {
                    return (null, 0, 1, batch.Count);
                }

                var taskEmbedding = generated[0].Vector.ToArray();

                for (var index = 0; index < uncached.Count; index++)
                {
                    exemplarEmbeddingsCache.TryAdd(uncached[index], generated[index + 1].Vector.ToArray());
                }

                string? bestAgentType = null;
                var bestScore = 0.0;

                foreach (var rule in candidates)
                {
                    foreach (var exemplar in rule.Exemplars)
                    {
                        if (!exemplarEmbeddingsCache.TryGetValue(exemplar, out var exemplarEmbedding))
                        {
                            continue;
                        }

                        var score = CosineSimilarity(taskEmbedding, exemplarEmbedding);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestAgentType = rule.AgentType;
                        }
                    }
                }

                return (bestScore >= RoutingRules.EmbeddingConfidenceThreshold ? bestAgentType : null,
                    bestScore, 1, batch.Count);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return (null, 0, 1, batch.Count);
            }
        }

        private static async Task<string?> RouteByLlmAsync(
            string task, IChatClient chatClient, IReadOnlyList<RoutingRule> candidates, string? recentContext,
            ChatOptions? chatOptions, CancellationToken token)
        {
            var options = string.Join(
                "\n", candidates.Select(rule => $"- {rule.AgentType}: {rule.Description}"));

            var preceding = string.IsNullOrWhiteSpace(recentContext)
                ? string.Empty
                : $"\n\nEarlier in this conversation the caller asked:\n{recentContext}\n\nUse that only to " +
                  "understand what the task below refers to. Route the task itself, not the earlier questions.";

            var prompt =
                "You are routing a task to the right specialist. Reply with ONLY the specialist's name " +
                $"below, or the word \"general\" if none of them genuinely fit -- no other text.\n\n{options}" +
                $"{preceding}\n\nTask: {task}";

            var response = await chatClient
                .GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], chatOptions, token)
                .ConfigureAwait(false);

            var reply = response.Text.Trim().Trim('.', '"').ToLowerInvariant();

            if (string.IsNullOrEmpty(reply))
            {
                return null;
            }

            return candidates.FirstOrDefault(rule => rule.AgentType.Equals(reply, StringComparison.OrdinalIgnoreCase))
                ?.AgentType;
        }

        private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
        {
            double dot = 0;
            double leftMagnitude = 0;
            double rightMagnitude = 0;

            for (var i = 0; i < left.Count; i++)
            {
                dot += left[i] * right[i];
                leftMagnitude += left[i] * left[i];
                rightMagnitude += right[i] * right[i];
            }

            if (leftMagnitude == 0 || rightMagnitude == 0)
            {
                return 0;
            }

            return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
        }

        private sealed class RoutingObservation
        {
            public int EmbeddingRequests { get; set; }
            public int EmbeddingValues { get; set; }
            public double? Score { get; set; }
            public string? Error { get; set; }
        }
    }
}