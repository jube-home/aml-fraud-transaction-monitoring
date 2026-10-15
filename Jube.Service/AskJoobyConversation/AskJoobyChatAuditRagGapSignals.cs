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
using Jube.Data.Repository;

namespace Jube.Service.AskJoobyConversation
{
    internal static class AskJoobyChatAuditRagGapSignals
    {
        private const string SearchRagToolName = "SearchRag";
        private const string RunSqlQueryToolName = "RunSqlQuery";
        private const string EmptyRagResultPrefix = "No matching Bites found.";
        private const string ValidationOmissionMarker = "found a gap the docs don't cover";
        private const string ValidationNeverPickedUpMarker = "never picked up this time";

        private static readonly JsonSerializerOptions prettyJsonOptions = new() { WriteIndented = true };

        private sealed record RagGapResult(
            List<string> EmptySearches,
            List<string> ValidationOmissions,
            List<string> ValidationNeverCompleted,
            int SqlFallbackCount);

        internal static string BuildMarkdownSection(IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyToolCall> toolCalls)
        {
            var result = Compute(entries, toolCalls);
            var sb = new StringBuilder();

            sb.AppendLine("## RAG & Tooling Gap Analysis");
            sb.AppendLine();

            sb.AppendLine("Where the agent needed a real answer and the Pasture (RAG) either had nothing, " +
                          "was never searched, or the fact-checker could not confirm a claim against it -- " +
                          "these are documentation/corpus gaps for a developer to close, not agent " +
                          "misbehaviour on their own. The same numbers are in `rag-gaps.json`.");

            sb.AppendLine();

            var totalFlags = result.EmptySearches.Count + result.ValidationOmissions.Count +
                             result.ValidationNeverCompleted.Count;

            sb.AppendLine(totalFlags == 0
                ? "**Overall**: no coverage gaps flagged by the checks below."
                : $"**Overall**: {totalFlags} coverage gap(s) flagged across the checks below.");

            sb.AppendLine();

            AppendGapGroup(sb, "Empty Pasture searches",
                "a SearchRag call that found nothing at all", result.EmptySearches);

            AppendGapGroup(sb, "Pasture coverage gaps (validation omissions)",
                "the fact-checker searched the Pasture to confirm a claim and came back empty, so the " +
                "reply went out with an unverified note rather than a confirmed fact", result.ValidationOmissions);

            AppendGapGroup(sb, "Validation never completed",
                "the fact-check never ran to completion, so nothing confirmed or denied a claim either way",
                result.ValidationNeverCompleted);

            sb.AppendLine($"- **Live-SQL reliance**: {result.SqlFallbackCount} RunSqlQuery call(s) -- not a " +
                          "gap by itself, SQL is the right tool for live platform data, but a pattern of SQL " +
                          "immediately following an empty or skipped Pasture search above suggests the corpus " +
                          "is missing something SQL ended up standing in for.");

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            return sb.ToString();
        }

        internal static byte[] BuildRagGapJson(IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyToolCall> toolCalls)
        {
            var result = Compute(entries, toolCalls);

            var payload = new
            {
                overallGapCount = result.EmptySearches.Count + result.ValidationOmissions.Count +
                                  result.ValidationNeverCompleted.Count,
                emptyPastureSearches = result.EmptySearches,
                validationOmissions = result.ValidationOmissions,
                validationNeverCompleted = result.ValidationNeverCompleted,
                sqlFallbackCount = result.SqlFallbackCount
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, prettyJsonOptions));
        }

        private static void AppendGapGroup(StringBuilder sb, string title, string description, List<string> items)
        {
            sb.AppendLine(
                $"- **{title}**: {(items.Count == 0 ? "none" : $"{items.Count} flagged")} -- {description}.");

            foreach (var item in items)
            {
                sb.AppendLine($"  - {item}");
            }
        }

        private static RagGapResult Compute(IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyToolCall> toolCalls)
        {
            var emptySearches = toolCalls
                .Where(c => string.Equals(c.ToolName, SearchRagToolName, StringComparison.Ordinal) &&
                            c.Result is not null &&
                            c.Result.StartsWith(EmptyRagResultPrefix, StringComparison.Ordinal))
                .Select(c =>
                {
                    var query = AskJoobyChatAuditArtifacts.ExtractStringArgument(c.Arguments, "query");

                    return $"Run #{c.AskJoobyRunId?.ToString() ?? "-"} searched for " +
                           (query is null ? "an unrecorded query" : $"\"{query}\"") + " and found nothing.";
                })
                .ToList();

            var validationOmissions = entries
                .Where(e => e.EntryTypeId == AskJoobyMessageRepository.EntryTypeValidation && e.Content is not null
                    && e.Content.Contains(ValidationOmissionMarker, StringComparison.Ordinal))
                .Select(e => $"{FormatDate(e.CreatedDate)}: the fact-checker could not confirm a claim against " +
                             "the Pasture.")
                .ToList();

            var validationNeverCompleted = entries
                .Where(e => e.EntryTypeId == AskJoobyMessageRepository.EntryTypeValidation && e.Content is not null
                    && e.Content.Contains(ValidationNeverPickedUpMarker, StringComparison.Ordinal))
                .Select(e => $"{FormatDate(e.CreatedDate)}: the fact-check never ran to completion.")
                .ToList();

            var sqlFallbackCount = toolCalls.Count(c =>
                string.Equals(c.ToolName, RunSqlQueryToolName, StringComparison.Ordinal));

            return new RagGapResult(emptySearches, validationOmissions, validationNeverCompleted, sqlFallbackCount);
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                : "-";
        }
    }
}