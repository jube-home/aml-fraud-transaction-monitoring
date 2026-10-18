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

namespace Jube.Service.AskJoobyConversation
{
    internal static class AskJoobyChatAuditContextEfficiency
    {
        private const int LargestShown = 5;

        private static readonly JsonSerializerOptions prettyJsonOptions = new() { WriteIndented = true };

        private sealed record ModelCallRow(
            int ModelCallId,
            int? RunId,
            string? AgentType,
            int InputTokens,
            int OutputTokens,
            long? DurationMs,
            DateTime? CreatedDate);

        private sealed record ToolResultRow(
            int ToolCallId,
            int? RunId,
            string? ToolName,
            int ResultCharacters,
            long? DurationMs,
            DateTime? CreatedDate);

        private sealed record EfficiencyResult(
            int ModelCallCount,
            double AverageInputTokens,
            int MaxInputTokens,
            List<ModelCallRow> LargestModelCalls,
            List<ToolResultRow> LargestToolResults,
            int CompactionCount,
            double AverageCompactionSavingsPercent,
            int? CharactersBeforeFirstCompaction);

        internal static string BuildMarkdownSection(
            IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<AskJoobyCompaction> compactions)
        {
            var result = Compute(runs, toolCalls, modelCalls, compactions);
            var sb = new StringBuilder();

            sb.AppendLine("## Context Efficiency");
            sb.AppendLine();

            sb.AppendLine("Where this conversation's context actually got big -- not just RAG, every tool's " +
                          "result and every model call's input token count, the same shape of problem that " +
                          "caught the oversized-Bite issue, generalised to anything that can blow up a model " +
                          "call's context. The same numbers are in `context-efficiency.json`.");

            sb.AppendLine();

            if (modelCalls.Count == 0 && toolCalls.Count == 0)
            {
                sb.AppendLine("_No model calls or tool calls were recorded in this conversation._");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                return sb.ToString();
            }

            if (result.ModelCallCount > 0)
            {
                sb.AppendLine($"- **Model call input size**: {result.AverageInputTokens:0} tokens average, " +
                              $"{result.MaxInputTokens} tokens at the largest.");
            }

            sb.AppendLine($"- **Context compaction**: {result.CompactionCount} compaction(s), averaging " +
                          $"{result.AverageCompactionSavingsPercent:0.0}% characters removed" +
                          (result.CharactersBeforeFirstCompaction.HasValue
                              ? $", {result.CharactersBeforeFirstCompaction} characters already built up " +
                                "before the first one fired"
                              : string.Empty) + ".");

            if (result.LargestModelCalls.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"**Largest model calls by input tokens** (top {LargestShown}):");
                sb.AppendLine();
                sb.AppendLine("| When | Run | Agent | Input tokens | Output tokens | Duration |");
                sb.AppendLine("|---|---|---|---|---|---|");

                foreach (var row in result.LargestModelCalls)
                {
                    sb.AppendLine($"| {FormatDate(row.CreatedDate)} | {row.RunId?.ToString() ?? "-"} | " +
                                  $"{row.AgentType ?? "-"} | {row.InputTokens} | {row.OutputTokens} | " +
                                  $"{row.DurationMs?.ToString() ?? "-"} ms |");
                }
            }

            if (result.LargestToolResults.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"**Largest tool results by response size, any tool** (top {LargestShown}):");
                sb.AppendLine();
                sb.AppendLine("| When | Run | Tool | Response chars | Duration |");
                sb.AppendLine("|---|---|---|---|---|");

                foreach (var row in result.LargestToolResults)
                {
                    sb.AppendLine($"| {FormatDate(row.CreatedDate)} | {row.RunId?.ToString() ?? "-"} | " +
                                  $"{row.ToolName ?? "-"} | {row.ResultCharacters} | " +
                                  $"{row.DurationMs?.ToString() ?? "-"} ms |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            return sb.ToString();
        }

        internal static byte[] BuildContextEfficiencyJson(
            IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<AskJoobyCompaction> compactions)
        {
            var result = Compute(runs, toolCalls, modelCalls, compactions);

            var payload = new
            {
                modelCallCount = result.ModelCallCount,
                averageInputTokens = result.AverageInputTokens,
                maxInputTokens = result.MaxInputTokens,
                largestModelCalls = result.LargestModelCalls.Select(row => new
                {
                    modelCallId = row.ModelCallId, runId = row.RunId, agentType = row.AgentType,
                    inputTokens = row.InputTokens, outputTokens = row.OutputTokens, durationMs = row.DurationMs,
                    createdDate = row.CreatedDate
                }),
                largestToolResults = result.LargestToolResults.Select(row => new
                {
                    toolCallId = row.ToolCallId, runId = row.RunId, toolName = row.ToolName,
                    responseCharacters = row.ResultCharacters, durationMs = row.DurationMs,
                    createdDate = row.CreatedDate
                }),
                compactions = new
                {
                    count = result.CompactionCount, averageSavingsPercent = result.AverageCompactionSavingsPercent,
                    charactersBeforeFirstCompaction = result.CharactersBeforeFirstCompaction
                }
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, prettyJsonOptions));
        }

        private static EfficiencyResult Compute(
            IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<AskJoobyCompaction> compactions)
        {
            var runsById = runs.Where(r => r.Id != 0).ToDictionary(r => r.Id, r => r.AgentType);

            var largestModelCalls = modelCalls
                .OrderByDescending(m => m.InputTokens ?? 0)
                .Take(LargestShown)
                .Select(m => new ModelCallRow(
                    m.Id, m.AskJoobyRunId,
                    m.AskJoobyRunId is { } runId && runsById.TryGetValue(runId, out var agentType)
                        ? agentType
                        : null,
                    m.InputTokens ?? 0, m.OutputTokens ?? 0, m.DurationMs, m.CreatedDate))
                .ToList();

            var largestToolResults = toolCalls
                .OrderByDescending(c => c.Result?.Length ?? 0)
                .Take(LargestShown)
                .Select(c => new ToolResultRow(
                    c.Id, c.AskJoobyRunId, c.ToolName, c.Result?.Length ?? 0, c.DurationMs, c.CreatedDate))
                .ToList();

            var averageSavings = compactions.Count == 0
                ? 0
                : compactions.Average(c =>
                {
                    var before = c.BeforeCharacters ?? 0;
                    var after = c.AfterCharacters ?? 0;

                    return before > 0 ? 100.0 - after * 100.0 / before : 0.0;
                });

            var firstCompactionCharacters = compactions
                .OrderBy(c => c.CreatedDate)
                .Select(c => c.BeforeCharacters)
                .FirstOrDefault();

            return new EfficiencyResult(
                modelCalls.Count, modelCalls.Count == 0 ? 0 : modelCalls.Average(m => m.InputTokens ?? 0),
                modelCalls.Count == 0 ? 0 : modelCalls.Max(m => m.InputTokens ?? 0), largestModelCalls,
                largestToolResults, compactions.Count, averageSavings, firstCompactionCharacters);
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                : "-";
        }
    }
}