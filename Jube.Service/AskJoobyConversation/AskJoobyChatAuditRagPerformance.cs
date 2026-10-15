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
using System.Text.RegularExpressions;
using Jube.Data.Poco;

namespace Jube.Service.AskJoobyConversation
{
    internal static class AskJoobyChatAuditRagPerformance
    {
        private const string SearchRagToolName = "SearchRag";
        private const int LargestSearchesShown = 5;

        private static readonly Regex hitCountPattern = new(@"^Found (\d+) Bite", RegexOptions.Multiline);
        private static readonly JsonSerializerOptions prettyJsonOptions = new() { WriteIndented = true };

        private sealed record SearchRow(
            int ToolCallId,
            int? RunId,
            string? Query,
            int? HitCount,
            bool Empty,
            int ResponseCharacters,
            long? DurationMs,
            DateTime? CreatedDate);

        private sealed record PerformanceResult(
            int SearchCount,
            int EmptyCount,
            double AverageHitCount,
            double AverageResponseCharacters,
            int MaxResponseCharacters,
            double AverageDurationMs,
            long MaxDurationMs,
            List<SearchRow> Largest);

        internal static string BuildMarkdownSection(IReadOnlyList<AskJoobyToolCall> toolCalls)
        {
            var result = Compute(toolCalls);
            var sb = new StringBuilder();

            sb.AppendLine("## RAG Performance");
            sb.AppendLine();

            sb.AppendLine("Every SearchRag call this conversation made, summarised -- response size and " +
                          "duration are what actually matter operationally: a search returning an oversized " +
                          "response is exactly the kind of thing that goes unnoticed until it blows a model " +
                          "call's context or token budget. The same numbers are in `rag-performance.json`.");

            sb.AppendLine();

            if (result.SearchCount == 0)
            {
                sb.AppendLine("_No SearchRag calls were made in this conversation._");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                return sb.ToString();
            }

            sb.AppendLine($"- **Searches**: {result.SearchCount} total, {result.EmptyCount} returned nothing.");
            sb.AppendLine($"- **Bites per search**: {result.AverageHitCount:0.0} average.");

            sb.AppendLine($"- **Response size**: {result.AverageResponseCharacters:0} characters average, " +
                          $"{result.MaxResponseCharacters} characters at the largest.");
            sb.AppendLine($"- **Duration**: {result.AverageDurationMs:0} ms average, " +
                          $"{result.MaxDurationMs} ms at the slowest.");

            sb.AppendLine();
            sb.AppendLine($"**Largest searches by response size** (top {LargestSearchesShown}):");
            sb.AppendLine();
            sb.AppendLine("| When | Run | Query | Bites | Response chars | Duration |");
            sb.AppendLine("|---|---|---|---|---|---|");

            foreach (var row in result.Largest)
            {
                sb.AppendLine($"| {FormatDate(row.CreatedDate)} | {row.RunId?.ToString() ?? "-"} | " +
                              $"{row.Query ?? "-"} | {row.HitCount?.ToString() ?? "-"} | " +
                              $"{row.ResponseCharacters} | {row.DurationMs?.ToString() ?? "-"} ms |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            return sb.ToString();
        }

        internal static byte[] BuildRagPerformanceJson(IReadOnlyList<AskJoobyToolCall> toolCalls)
        {
            var result = Compute(toolCalls);

            var payload = new
            {
                searchCount = result.SearchCount,
                emptyCount = result.EmptyCount,
                averageHitCount = result.AverageHitCount,
                averageResponseCharacters = result.AverageResponseCharacters,
                maxResponseCharacters = result.MaxResponseCharacters,
                averageDurationMs = result.AverageDurationMs,
                maxDurationMs = result.MaxDurationMs,
                largestSearches = result.Largest.Select(row => new
                {
                    toolCallId = row.ToolCallId, runId = row.RunId, query = row.Query, hitCount = row.HitCount,
                    empty = row.Empty, responseCharacters = row.ResponseCharacters, durationMs = row.DurationMs,
                    createdDate = row.CreatedDate
                })
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, prettyJsonOptions));
        }

        private static PerformanceResult Compute(IReadOnlyList<AskJoobyToolCall> toolCalls)
        {
            var rows = toolCalls
                .Where(c => string.Equals(c.ToolName, SearchRagToolName, StringComparison.Ordinal))
                .Select(c => new SearchRow(
                    c.Id, c.AskJoobyRunId, AskJoobyChatAuditArtifacts.ExtractStringArgument(c.Arguments, "query"),
                    ExtractHitCount(c.Result), c.Result?.StartsWith("No matching Bites found.",
                        StringComparison.Ordinal) ?? false, c.Result?.Length ?? 0, c.DurationMs, c.CreatedDate))
                .ToList();

            if (rows.Count == 0)
            {
                return new PerformanceResult(0, 0, 0, 0, 0, 0, 0, []);
            }

            var largest = rows.OrderByDescending(r => r.ResponseCharacters).Take(LargestSearchesShown).ToList();

            return new PerformanceResult(
                rows.Count, rows.Count(r => r.Empty), rows.Average(r => r.HitCount ?? 0),
                rows.Average(r => r.ResponseCharacters), rows.Max(r => r.ResponseCharacters),
                rows.Average(r => r.DurationMs ?? 0), rows.Max(r => r.DurationMs ?? 0), largest);
        }

        private static int? ExtractHitCount(string? result)
        {
            if (string.IsNullOrEmpty(result))
            {
                return null;
            }

            var match = hitCountPattern.Match(result);

            return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : null;
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                : "-";
        }
    }
}