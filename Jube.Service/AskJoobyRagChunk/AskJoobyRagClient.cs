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

using System.Globalization;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.Data.Query.GetAskJoobyRagChunkByHashQuery;
using Jube.Data.Query.GetAskJoobyRagChunkByParentHashQuery;
using Jube.Data.Query.SearchAskJoobyRagChunkQuery;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyRagChunk.Interfaces;
using log4net;
using Microsoft.Extensions.AI;

namespace Jube.Service.AskJoobyRagChunk
{
    public sealed class AskJoobyRagClient(
        SearchAskJoobyRagChunkQuery query,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator,
        IRagReranker? reranker = null,
        GetAskJoobyRagChunkByHashQuery? parentQuery = null,
        GetAskJoobyRagChunkByParentHashQuery? siblingQuery = null,
        int candidateCount = 0,
        double rerankMinScore = 0,
        int promotionBudgetCharacters = 0,
        int maxCandidateCharacters = 9000) : IRagClient
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AskJoobyRagClient));

        private const int EmbeddingTimeoutSeconds = 5;

        public Task<IReadOnlyList<RagChunk>> SearchAsync(string queryText, int topK = 5, double minScore = 0,
            int requested = 0, CancellationToken token = default)
        {
            return SearchAsync(queryText, topK, minScore, requested, null, null, token);
        }

        public async Task<IReadOnlyList<RagChunk>> SearchAsync(string queryText, int topK, double minScore,
            int requested, IRunRecorder? recorder, string? runId, CancellationToken token = default)
        {
            var embeddingText = string.Empty;

            if (embeddingGenerator is not null)
            {
                var startedAt = DateTimeOffset.UtcNow;

                using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(EmbeddingTimeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token);

                try
                {
                    var generated = await embeddingGenerator.GenerateAsync([queryText], cancellationToken: linked.Token)
                        .ConfigureAwait(false);

                    var embedding = generated[0];
                    embeddingText = ToPgVectorText(embedding.Vector.Span);

                    if (recorder is not null && runId is not null)
                    {
                        var durationMs = (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;

                        await recorder.RecordEmbeddingCallAsync(runId, embedding.ModelId ?? "unknown",
                            (int?)generated.Usage?.InputTokenCount, durationMs, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    log.Warn($"AskJooby RAG: query embedding timed out after {EmbeddingTimeoutSeconds}s, "
                             + "falling back to a text-only search.");
                }
                catch (Exception exception) when (!token.IsCancellationRequested)
                {
                    log.Warn("AskJooby RAG: query embedding call failed, falling back to a text-only search.",
                        exception);
                }
            }

            var width = requested > 0 ? requested : candidateCount;
            var fetch = width > topK ? width : topK;

            var rows = await query.ExecuteAsync(queryText, embeddingText, fetch, minScore, token)
                .ConfigureAwait(false);

            var (candidates, oversized) = PartitionByMaxCharacters(rows.Select(Map).ToList(), maxCandidateCharacters);

            if (oversized.Count > 0)
            {
                log.Warn($"AskJooby RAG: dropped {oversized.Count} oversized Bite(s) before reranking -- over "
                         + $"the {maxCandidateCharacters}-character limit ({string.Join(", ", oversized.Select(
                             c => $"{c.QualifiedName ?? c.SourceFile ?? c.Hash} ({c.Content.Length} chars)"))}).");
            }

            if (candidates.Count == 0 || topK <= 0)
            {
                return [];
            }

            var ordered = await RerankAsync(queryText, candidates, recorder, runId, token).ConfigureAwait(false);

            return await PromoteAsync(ordered, topK, token).ConfigureAwait(false);
        }

        private async Task<List<RagChunk>> RerankAsync(
            string queryText, List<RagChunk> candidates, IRunRecorder? recorder, string? runId,
            CancellationToken token)
        {
            if (reranker is null || candidates.Count <= 1)
            {
                return candidates;
            }

            var startedAt = DateTimeOffset.UtcNow;

            var outcome = await reranker
                .RankAsync(queryText, candidates.Select(candidate => candidate.Content).ToList(),
                    candidates.Count, token)
                .ConfigureAwait(false);

            if (recorder is not null && runId is not null)
            {
                var durationMs = (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;

                await recorder.RecordRerankCallAsync(runId, RerankerModelName(), candidates.Count,
                    outcome.TokensCompared, durationMs, token).ConfigureAwait(false);
            }

            if (outcome.Documents is null)
            {
                return candidates;
            }

            return outcome.Documents
                .Where(candidate => candidate.Score >= rerankMinScore)
                .Select(candidate => candidates[candidate.Index] with { Score = candidate.Score })
                .ToList();
        }

        private async Task<IReadOnlyList<RagChunk>> PromoteAsync(
            List<RagChunk> ordered, int topK, CancellationToken token)
        {
            if (parentQuery is null || promotionBudgetCharacters <= 0)
            {
                return ordered.Count <= topK ? ordered : ordered.Take(topK).ToList();
            }

            var groups = new List<List<RagChunk>>();
            var byParent = new Dictionary<string, List<RagChunk>>(StringComparer.Ordinal);

            foreach (var chunk in ordered)
            {
                if (string.IsNullOrEmpty(chunk.ParentHash))
                {
                    groups.Add([chunk]);
                    continue;
                }

                if (byParent.TryGetValue(chunk.ParentHash, out var existing))
                {
                    existing.Add(chunk);
                    continue;
                }

                var group = new List<RagChunk> { chunk };
                byParent[chunk.ParentHash] = group;
                groups.Add(group);
            }

            var chosen = groups.Take(topK).ToList();

            var siblingHashes = chosen
                .Where(group => IsTabular(group[0].Kind))
                .Select(group => group[0].ParentHash)
                .Where(hash => !string.IsNullOrEmpty(hash))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var siblings = siblingHashes.Count == 0 || siblingQuery is null
                ? []
                : (await siblingQuery.ExecuteAsync(siblingHashes, [(short)AskJoobyRagChunkKind.TableRow], token)
                    .ConfigureAwait(false))
                .Select(Map)
                .GroupBy(row => row.ParentHash!, StringComparer.Ordinal)
                .ToDictionary(row => row.Key, row => row.ToList(), StringComparer.Ordinal);

            var parentHashes = chosen
                .Where(group => !IsTabular(group[0].Kind) && !IsIndexedParent(group[0].Kind))
                .Select(group => group[0].ParentHash)
                .Where(hash => !string.IsNullOrEmpty(hash))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var parents = (await parentQuery.ExecuteAsync(parentHashes, token).ConfigureAwait(false))
                .GroupBy(parent => parent.Hash, StringComparer.Ordinal)
                .ToDictionary(parent => parent.Key, parent => Map(parent.First()), StringComparer.Ordinal);

            var results = new List<RagChunk>();

            foreach (var group in chosen)
            {
                var best = group[0];

                if (IsTabular(best.Kind) && best.ParentHash is not null
                                         && siblings.TryGetValue(best.ParentHash, out var row)
                                         && row.Sum(sibling => sibling.Content.Length) <= promotionBudgetCharacters)
                {
                    results.AddRange(row.Select(sibling => sibling with { Score = best.Score }));
                    continue;
                }

                if (IsTabular(best.Kind) || IsIndexedParent(best.Kind))
                {
                    results.AddRange(group);
                    continue;
                }

                if (best.ParentHash is not null
                    && parents.TryGetValue(best.ParentHash, out var parent)
                    && parent.Content.Length <= promotionBudgetCharacters
                    && group.Count >= 2)
                {
                    results.Add(parent with { Score = best.Score });
                    continue;
                }

                results.AddRange(group);
            }

            return results;
        }

        internal static (List<RagChunk> Kept, List<RagChunk> Oversized) PartitionByMaxCharacters(
            List<RagChunk> candidates, int maxCandidateCharacters)
        {
            if (maxCandidateCharacters <= 0)
            {
                return (candidates, []);
            }

            var oversized = candidates.Where(c => c.Content.Length > maxCandidateCharacters).ToList();

            if (oversized.Count == 0)
            {
                return (candidates, oversized);
            }

            return (candidates.Where(c => c.Content.Length <= maxCandidateCharacters).ToList(), oversized);
        }

        private string RerankerModelName()
        {
            return reranker is BgeReranker bge ? bge.Model : "unknown";
        }

        private static bool IsTabular(string? kind)
        {
            return string.Equals(kind, nameof(AskJoobyRagChunkKind.TableRow), StringComparison.Ordinal);
        }

        private static bool IsIndexedParent(string? kind)
        {
            return string.Equals(kind, nameof(AskJoobyRagChunkKind.ConfigBlock), StringComparison.Ordinal);
        }

        // CategoryId/KindId are the wire codes stored on AskJoobyRagChunk; everything downstream
        // of this client (the agent tool, RagSearchTool's logging) works with the enum member
        // name, same as before this column became an integer, so nothing else has to learn new
        // magic numbers.
        private static RagChunk Map(SearchAskJoobyRagChunkQueryDto row)
        {
            return new RagChunk(
                row.Hash, row.ParentHash, CategoryName(row.CategoryId), KindName(row.KindId), row.SourceFile,
                row.QualifiedName, row.Content ?? string.Empty, row.Score);
        }

        public static string? CategoryName(byte? categoryId) =>
            categoryId is { } id ? ((AskJoobyRagChunkCategory)id).ToString() : null;

        public static string? KindName(byte? kindId) =>
            kindId is { } id ? ((AskJoobyRagChunkKind)id).ToString() : null;

        public static string ToPgVectorText(ReadOnlySpan<float> values)
        {
            var parts = new string[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                parts[i] = values[i].ToString(CultureInfo.InvariantCulture);
            }

            return "[" + string.Join(",", parts) + "]";
        }
    }
}