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

using System.Text;
using System.Text.Json;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;

namespace Jube.Service.AskJoobyConversation
{
    internal static class AskJoobyChatAuditHealthSignals
    {
        private const int RetryLoopThreshold = 3;
        private const int FanOutThreshold = 5;
        private const int GuardRejectionClusterThreshold = 3;
        private const int EffortfulModelCallThreshold = 3;

        private static readonly JsonSerializerOptions prettyJsonOptions = new() { WriteIndented = true };

        private sealed record HealthResult(
            List<string> RetryLoops,
            List<string> HandoffBounces,
            List<string> FanOuts,
            int MaxRunDepth,
            int RoutingTotal,
            int RoutingFallbackCount,
            List<string> GuardClusters,
            List<string> EffortWithoutOutput,
            List<string> AnomalousFinishReasons);

        internal static string BuildMarkdownSection(
            IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<AskJoobyRouting> routings,
            IReadOnlyList<AskJoobyAgentGuardRejection> agentGuardRejections)
        {
            var result = Compute(runs, toolCalls, modelCalls, routings, agentGuardRejections);
            var sb = new StringBuilder();

            sb.AppendLine("## Health Signals");
            sb.AppendLine();

            sb.AppendLine("Rule-based checks over the data above -- no model call involved -- aimed at the " +
                          "kind of tool-calling and agent-hierarchy wiring problem that is easy to miss by eye " +
                          "but shows up as a pattern across many rows. Context size and compaction pressure " +
                          "are in the Context Efficiency section below instead. The same numbers are in " +
                          "`health.json` for tooling.");

            sb.AppendLine();

            var totalFlags = result.RetryLoops.Count + result.HandoffBounces.Count + result.FanOuts.Count +
                             result.GuardClusters.Count + result.EffortWithoutOutput.Count +
                             result.AnomalousFinishReasons.Count;

            sb.AppendLine(totalFlags == 0
                ? "**Overall**: no anomalies flagged by the checks below."
                : $"**Overall**: {totalFlags} signal(s) flagged across the checks below.");

            sb.AppendLine();

            AppendSignalGroup(sb, "Tool-call retry loops",
                "the same tool called repeatedly with identical arguments by the same run -- the clearest " +
                "sign of an agent chasing its own tail", result.RetryLoops);

            AppendSignalGroup(sb, "Handoff bounces",
                "a run whose agent type matches its grandparent run's agent type -- control bounced back up " +
                "the hierarchy to where it started, rather than making forward progress", result.HandoffBounces);

            AppendSignalGroup(sb, "Delegation fan-out",
                $"a single run spawning more than {FanOutThreshold} child runs -- check for runaway delegation",
                result.FanOuts);

            sb.AppendLine($"- **Routing fallback rate**: {result.RoutingFallbackCount} of {result.RoutingTotal} " +
                          "routing decision(s) fell through to the Model or No-candidates tier rather than " +
                          "Pattern/Embedding/Continuity.");

            AppendSignalGroup(sb, "AgentGuard rejection clusters",
                $"a single rule firing {GuardRejectionClusterThreshold} or more times -- worth checking " +
                "whether it is structurally blocking normal flow rather than catching rare abuse",
                result.GuardClusters);

            AppendSignalGroup(sb, "Effort without output",
                $"a run that made {EffortfulModelCallThreshold} or more model calls but produced no Result",
                result.EffortWithoutOutput);

            AppendSignalGroup(sb, "Anomalous model finish reasons",
                "a model call that ended with Length (cut off by the token limit) or ContentFilter " +
                "(cut off by a safety filter) rather than Stop or ToolCalls -- a truncated completion is a " +
                "common cause of malformed tool-call arguments and the retries that follow",
                result.AnomalousFinishReasons);

            sb.AppendLine($"- **Max run depth**: {result.MaxRunDepth}.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            return sb.ToString();
        }

        internal static byte[] BuildHealthJson(
            IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<AskJoobyRouting> routings,
            IReadOnlyList<AskJoobyAgentGuardRejection> agentGuardRejections)
        {
            var result = Compute(runs, toolCalls, modelCalls, routings, agentGuardRejections);

            var payload = new
            {
                overallFlagCount = result.RetryLoops.Count + result.HandoffBounces.Count + result.FanOuts.Count +
                                   result.GuardClusters.Count + result.EffortWithoutOutput.Count +
                                   result.AnomalousFinishReasons.Count,
                toolCallRetryLoops = result.RetryLoops,
                handoffBounces = result.HandoffBounces,
                delegationFanOuts = result.FanOuts,
                routing = new
                {
                    total = result.RoutingTotal, fallbackCount = result.RoutingFallbackCount,
                    fallbackRate = result.RoutingTotal > 0
                        ? (double)result.RoutingFallbackCount / result.RoutingTotal
                        : 0
                },
                agentGuardClusters = result.GuardClusters,
                effortWithoutOutput = result.EffortWithoutOutput,
                anomalousFinishReasons = result.AnomalousFinishReasons,
                maxRunDepth = result.MaxRunDepth
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, prettyJsonOptions));
        }

        private static void AppendSignalGroup(StringBuilder sb, string title, string description,
            List<string> items)
        {
            sb.AppendLine(
                $"- **{title}**: {(items.Count == 0 ? "none" : $"{items.Count} flagged")} -- {description}.");

            foreach (var item in items)
            {
                sb.AppendLine($"  - {item}");
            }
        }

        private static HealthResult Compute(
            IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<AskJoobyRouting> routings,
            IReadOnlyList<AskJoobyAgentGuardRejection> agentGuardRejections)
        {
            var runsById = runs.Where(r => r.Id != 0).ToDictionary(r => r.Id);

            var retryLoops = toolCalls
                .Where(c => c.AskJoobyRunId is not null && !string.IsNullOrEmpty(c.Arguments))
                .GroupBy(c => (c.AskJoobyRunId, c.ToolName, c.Arguments))
                .Where(g => g.Count() >= RetryLoopThreshold)
                .Select(g => $"Run #{g.Key.AskJoobyRunId} called **{g.Key.ToolName}** {g.Count()} times with " +
                             "identical arguments.")
                .ToList();

            var handoffBounces = new List<string>();

            foreach (var run in runs)
            {
                if (run.ParentAskJoobyRunId is not { } parentId || !runsById.TryGetValue(parentId, out var parent))
                {
                    continue;
                }

                if (parent.ParentAskJoobyRunId is not { } grandparentId ||
                    !runsById.TryGetValue(grandparentId, out var grandparent))
                {
                    continue;
                }

                if (string.Equals(run.AgentType, grandparent.AgentType, StringComparison.Ordinal))
                {
                    handoffBounces.Add($"Run #{run.Id} ({run.AgentType}) bounced back to the same agent type as " +
                                       $"grandparent run #{grandparentId}, via run #{parentId} " +
                                       $"({parent.AgentType}).");
                }
            }

            var fanOuts = runs
                .GroupBy(r => r.ParentAskJoobyRunId ?? 0)
                .Where(g => g.Key != 0 && g.Count() > FanOutThreshold)
                .Select(g => $"Run #{g.Key} spawned {g.Count()} child runs.")
                .ToList();

            var routingFallbackCount = routings.Count(r => r.Stage is 3 or 5);

            var guardClusters = agentGuardRejections
                .Where(r => !string.IsNullOrEmpty(r.RuleName))
                .GroupBy(r => r.RuleName)
                .Where(g => g.Count() >= GuardRejectionClusterThreshold)
                .Select(g => $"**{g.Key}** fired {g.Count()} time(s).")
                .ToList();

            var modelCallCountByRunId = modelCalls
                .Where(m => m.AskJoobyRunId is not null)
                .GroupBy(m => m.AskJoobyRunId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var effortWithoutOutput = runs
                .Where(r => modelCallCountByRunId.TryGetValue(r.Id, out var count) &&
                            count >= EffortfulModelCallThreshold && string.IsNullOrEmpty(r.Result))
                .Select(r => $"Run #{r.Id} ({r.AgentType}) made {modelCallCountByRunId[r.Id]} model call(s) but " +
                             "produced no Result.")
                .ToList();

            var anomalousFinishReasons = modelCalls
                .Where(m => m.FinishReasonId is (byte)AskJoobyFinishReason.Length or
                    (byte)AskJoobyFinishReason.ContentFilter)
                .Select(m =>
                {
                    var agentType = m.AskJoobyRunId is { } runId && runsById.TryGetValue(runId, out var run)
                        ? run.AgentType
                        : "-";

                    var reason = (AskJoobyFinishReason)m.FinishReasonId!.Value == AskJoobyFinishReason.Length
                        ? "Length (token limit)"
                        : "ContentFilter (safety filter)";

                    return $"Run #{m.AskJoobyRunId?.ToString() ?? "-"} ({agentType})'s model call ended with " +
                           $"{reason}.";
                })
                .ToList();

            return new HealthResult(retryLoops, handoffBounces, fanOuts,
                runs.Count == 0 ? 0 : runs.Max(r => r.Depth ?? 0), routings.Count, routingFallbackCount,
                guardClusters, effortWithoutOutput, anomalousFinishReasons);
        }
    }
}