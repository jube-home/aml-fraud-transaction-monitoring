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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Data.Repository.Models.AskJoobyDataCache;

namespace Jube.Service.AskJoobyConversation
{
    using ConversationPoco = Data.Poco.AskJoobyConversation;
    using MemoryPoco = Data.Poco.AskJoobyMemory;

    internal static class AskJoobyChatAuditMarkdownBuilder
    {
        private const string FileDownloadPathPrefix = "/api/AskJoobyConversation/Files/";

        public static string Build(
            ConversationPoco conversation,
            IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<MemoryPoco> memories,
            IReadOnlyList<AskJoobyDataCacheListItemJsonDto> dataCacheEntries,
            IReadOnlyList<DynamicSqlQueryLog> sqlLog,
            IReadOnlyList<AskJoobyFile> files,
            IReadOnlyList<AskJoobyCompaction> compactions,
            IReadOnlyList<AskJoobyRouting> routings,
            IReadOnlyList<AskJoobyWorkflowStep> workflowSteps,
            IReadOnlyList<AskJoobyAgentGuardRejection> agentGuardRejections,
            IReadOnlyList<AskJoobyWebSearchCall> webSearchCalls,
            IReadOnlyList<AskJoobyOpenCorporatesCall> openCorporatesCalls,
            DateTime generatedAtUtc,
            string generatedByUser)
        {
            var sb = new StringBuilder();

            AppendHeader(sb, conversation, generatedAtUtc, generatedByUser);
            AppendBundleLayout(sb);

            sb.Append(AskJoobyChatAuditHealthSignals.BuildMarkdownSection(runs, toolCalls, modelCalls, routings,
                agentGuardRejections));

            sb.Append(AskJoobyChatAuditRagGapSignals.BuildMarkdownSection(entries, toolCalls));
            sb.Append(AskJoobyChatAuditRagPerformance.BuildMarkdownSection(toolCalls));

            sb.Append(AskJoobyChatAuditContextEfficiency.BuildMarkdownSection(runs, toolCalls, modelCalls,
                compactions));

            AppendTranscript(sb, entries, files);
            AppendGuidanceNudges(sb, entries, runs);
            AppendExecutionDetail(sb, runs, toolCalls, modelCalls, webSearchCalls, openCorporatesCalls);
            AppendSqlLog(sb, sqlLog);
            AppendMemories(sb, memories);
            AppendDataCache(sb, dataCacheEntries);
            AppendCompactions(sb, compactions);
            AppendRouting(sb, routings);
            AppendAgentGuardRejections(sb, agentGuardRejections);
            AppendWorkflowSteps(sb, workflowSteps);
            AppendFiles(sb, files);

            return sb.ToString();
        }

        private static void AppendHeader(StringBuilder sb, ConversationPoco conversation, DateTime generatedAtUtc,
            string generatedByUser)
        {
            sb.AppendLine($"# AskJooby Chat Audit -- Conversation #{conversation.Id}");
            sb.AppendLine();

            sb.AppendLine("This is a complete, unfiltered reconstitution of every background table behind " +
                          "this conversation -- the full transcript, every tool call and model call, every " +
                          "SQL statement actually executed, every memory laid down, every cached query " +
                          "result, and every file. Generated for support and transparency: it is meant to " +
                          "be readable by both an engineer and the person who had the conversation.");

            sb.AppendLine();
            sb.AppendLine($"- **Title**: {NullToDash(conversation.Title)}");
            sb.AppendLine($"- **Conversation Guid**: {conversation.Guid?.ToString() ?? "-"}");

            sb.AppendLine(
                $"- **Created**: {FormatDate(conversation.CreatedDate)} by {NullToDash(conversation.CreatedUser)}");
            sb.AppendLine(
                $"- **Last updated**: {FormatDate(conversation.UpdatedDate)} by {NullToDash(conversation.UpdatedUser)}");

            sb.AppendLine($"- **Generated**: {FormatDate(generatedAtUtc)} by {NullToDash(generatedByUser)}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendBundleLayout(StringBuilder sb)
        {
            sb.AppendLine("## Bundle Layout");
            sb.AppendLine();

            sb.AppendLine("This log is one file inside a zip bundle. Every section below is a lean narrative " +
                          "plus a short preview of each heavy artifact -- open the referenced file only when " +
                          "the preview on its own does not settle the question.");

            sb.AppendLine();

            sb.AppendLine("- `manifest.json` -- every artifact below, indexed by kind/id/path, for tooling " +
                          "that wants structured access instead of parsing this file.");
            sb.AppendLine("- `health.json` -- the same rule-based checks as the Health Signals section below, " +
                          "as structured data.");
            sb.AppendLine("- `rag-gaps.json` -- the same documentation/corpus gaps as the RAG & Tooling Gap " +
                          "Analysis section below, as structured data.");
            sb.AppendLine("- `rag-performance.json` -- the same response-size and duration numbers as the " +
                          "RAG Performance section below, as structured data.");
            sb.AppendLine("- `context-efficiency.json` -- the same model-call and tool-result size numbers " +
                          "as the Context Efficiency section below, as structured data.");

            sb.AppendLine("- `ai-analysis.md` -- if present, one bounded LLM pass over the reports above, " +
                          "grounded in what the involved agents were actually told to do. Omitted when " +
                          "that call is disabled or fails -- its absence is not itself a signal of anything.");

            sb.AppendLine("- `tool-calls/` -- full arguments and result for every tool call that carried one.");

            sb.AppendLine("- `data-cache/` -- full CSV for every cached query result (the tables below show " +
                          "only the first rows).");

            sb.AppendLine("- `compactions/` -- full before/after text for every history compaction.");
            sb.AppendLine("- `web-search/` -- full response JSON for every web search call.");
            sb.AppendLine("- `open-corporates/` -- full response JSON for every OpenCorporates call.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendTranscript(StringBuilder sb, IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyFile> files)
        {
            sb.AppendLine("## Conversation Transcript");
            sb.AppendLine();

            if (entries.Count == 0)
            {
                sb.AppendLine("_No messages recorded._");
                sb.AppendLine();

                return;
            }

            foreach (var entry in entries)
            {
                var kindLabel = EntryKindLabel(entry);

                sb.AppendLine($"### [{kindLabel}] {FormatDate(entry.CreatedDate)}");
                sb.AppendLine();
                sb.AppendLine(string.IsNullOrEmpty(entry.Content) ? "_(empty)_" : entry.Content);
                sb.AppendLine();

                var attachments = files.Where(f => f.AskJoobyMessageId == entry.Id).ToList();

                if (attachments.Count > 0)
                {
                    sb.AppendLine("**Attachments:**");

                    foreach (var file in attachments)
                    {
                        sb.AppendLine($"- {FileLink(file)}");
                    }

                    sb.AppendLine();
                }
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        private const string WasToldMarker = " was told: ";

        private sealed record GuidancePickup(
            AskJoobyMessage Step,
            IReadOnlyList<int> NudgeIds,
            AskJoobyMessage? Reaction);

        private static void AppendGuidanceNudges(StringBuilder sb, IReadOnlyList<AskJoobyMessage> entries,
            IReadOnlyList<AskJoobyRun> runs)
        {
            sb.AppendLine("## Guidance Nudges");
            sb.AppendLine();

            sb.AppendLine("Every message sent while a turn was already running -- an ordinary message, " +
                          "flagged `IsNudge`, typed into a conversation Jooby had not finished replying to " +
                          "yet. Paired here with when the running turn actually noticed it (the \"was " +
                          "told\" Step the transcript above records the moment the agent's next model call " +
                          "drained it) and the very next thing the agent did afterwards. Several nudges " +
                          "sent before that next model call are drained and announced together, so they " +
                          "share one pickup; a nudge with no pickup at all means the turn finished before " +
                          "it was ever drained.");

            sb.AppendLine();

            var nudges = entries.Where(e => e.IsNudge == true).OrderBy(e => e.Id).ToList();

            if (nudges.Count == 0)
            {
                sb.AppendLine("_No nudges were sent in this conversation._");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                return;
            }

            var pickups = FindGuidancePickups(entries);
            var topLevelRuns = runs.Where(r => r.ParentAskJoobyRunId is null or 0).ToList();

            foreach (var nudge in nudges)
            {
                var pickup = pickups.FirstOrDefault(p => p.NudgeIds.Contains(nudge.Id));
                var runningRun = FindRunningRun(topLevelRuns, nudge.CreatedDate);

                sb.AppendLine($"### Nudge sent {FormatDate(nudge.CreatedDate)}");
                sb.AppendLine();
                sb.AppendLine(string.IsNullOrEmpty(nudge.Content) ? "_(empty)_" : nudge.Content);
                sb.AppendLine();

                sb.AppendLine("- **Run running at the time**: " + (runningRun is null
                    ? "-"
                    : $"Run #{runningRun.Id} -- {NullToDash(runningRun.AgentType)}"));

                if (pickup is null)
                {
                    sb.AppendLine("- **Picked up**: _never -- the turn finished before its next model call_");
                }
                else
                {
                    var batchNote = pickup.NudgeIds.Count > 1
                        ? $" (drained together with {pickup.NudgeIds.Count - 1} other nudge(s))"
                        : string.Empty;

                    sb.AppendLine($"- **Picked up**: {FormatDate(pickup.Step.CreatedDate)}{batchNote}");

                    sb.AppendLine("- **Agent's next move afterwards**: " + (pickup.Reaction is null
                        ? "_(nothing further recorded)_"
                        : $"[{EntryKindLabel(pickup.Reaction)}] {pickup.Reaction.Content}"));
                }

                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static List<GuidancePickup> FindGuidancePickups(IReadOnlyList<AskJoobyMessage> entries)
        {
            var nudgesInOrder = entries.Where(e => e.IsNudge == true).OrderBy(e => e.Id).ToList();

            var steps = entries
                .Where(e => e.EntryTypeId == AskJoobyMessageRepository.EntryTypeStep && e.Content is not null
                    && e.Content.Contains(WasToldMarker, StringComparison.Ordinal))
                .OrderBy(e => e.Id)
                .ToList();

            var pickups = new List<GuidancePickup>();
            var nextUnclaimed = 0;

            foreach (var step in steps)
            {
                if (nextUnclaimed >= nudgesInOrder.Count)
                {
                    break;
                }

                var markerIndex = step.Content!.IndexOf(WasToldMarker, StringComparison.Ordinal);
                var afterMarker = step.Content[(markerIndex + WasToldMarker.Length)..];

                var matched = 0;

                for (var take = 1; nextUnclaimed + take <= nudgesInOrder.Count; take++)
                {
                    var candidates = nudgesInOrder.Skip(nextUnclaimed).Take(take)
                        .Select(n => (n.Content ?? string.Empty).Trim())
                        .ToList();

                    var expected = candidates.Count == 1
                        ? candidates[0]
                        : string.Join("\n", candidates.Select(c => "- " + c));

                    if (!string.Equals(expected, afterMarker, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    matched = take;
                    break;
                }

                if (matched == 0)
                {
                    continue;
                }

                var claimedIds = nudgesInOrder.Skip(nextUnclaimed).Take(matched).Select(n => n.Id).ToList();

                pickups.Add(new GuidancePickup(step, claimedIds, FindReaction(entries, step)));
                nextUnclaimed += matched;
            }

            return pickups;
        }

        private static AskJoobyMessage? FindReaction(IReadOnlyList<AskJoobyMessage> entries, AskJoobyMessage step)
        {
            return entries.Where(e => e.Id > step.Id).OrderBy(e => e.Id).FirstOrDefault();
        }

        private static AskJoobyRun? FindRunningRun(IReadOnlyList<AskJoobyRun> topLevelRuns, DateTime? at)
        {
            if (at is null)
            {
                return null;
            }

            return topLevelRuns
                .Where(r => r.CreatedDate.HasValue && r.CreatedDate.Value <= at.Value)
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();
        }

        private static string EntryKindLabel(AskJoobyMessage entry)
        {
            return entry.EntryTypeId switch
            {
                AskJoobyMessageRepository.EntryTypeMessage => RoleName(entry.RoleId),
                AskJoobyMessageRepository.EntryTypeStep => "Step",
                AskJoobyMessageRepository.EntryTypeThought => "Thought",
                AskJoobyMessageRepository.EntryTypeThinking => "Thinking",
                AskJoobyMessageRepository.EntryTypeDelegation => "Delegation",
                AskJoobyMessageRepository.EntryTypeHandoff => "Handoff",
                AskJoobyMessageRepository.EntryTypeValidation => "Validation",
                AskJoobyMessageRepository.EntryTypeRevision => "Revision",
                _ => "Entry"
            };
        }

        private static void AppendExecutionDetail(StringBuilder sb, IReadOnlyList<AskJoobyRun> runs,
            IReadOnlyList<AskJoobyToolCall> toolCalls, IReadOnlyList<AskJoobyModelCall> modelCalls,
            IReadOnlyList<AskJoobyWebSearchCall> webSearchCalls,
            IReadOnlyList<AskJoobyOpenCorporatesCall> openCorporatesCalls)
        {
            sb.AppendLine("## Execution Detail");
            sb.AppendLine();

            sb.AppendLine("Every agent run behind the transcript above -- the top-level Worker run, and any " +
                          "validator/revise/Joobrik sub-runs it spawned -- with the model calls and tool " +
                          "calls each one made.");

            sb.AppendLine();

            if (runs.Count == 0)
            {
                sb.AppendLine("_No runs recorded._");
                sb.AppendLine();

                return;
            }

            var toolCallsByRunId =
                toolCalls.GroupBy(t => t.AskJoobyRunId ?? 0).ToDictionary(g => g.Key, g => g.ToList());
            var modelCallsByRunId =
                modelCalls.GroupBy(m => m.AskJoobyRunId ?? 0).ToDictionary(g => g.Key, g => g.ToList());
            var webSearchCallsByRunId =
                webSearchCalls.GroupBy(w => w.AskJoobyRunId ?? 0).ToDictionary(g => g.Key, g => g.ToList());
            var openCorporatesCallsByRunId =
                openCorporatesCalls.GroupBy(o => o.AskJoobyRunId ?? 0).ToDictionary(g => g.Key, g => g.ToList());
            var runsByParentId =
                runs.GroupBy(r => r.ParentAskJoobyRunId ?? 0).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var topLevelRun in runs.Where(r => r.ParentAskJoobyRunId is null or 0).OrderBy(r => r.Id))
            {
                AppendRun(sb, topLevelRun, toolCallsByRunId, modelCallsByRunId, webSearchCallsByRunId,
                    openCorporatesCallsByRunId, runsByParentId, 0);
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendRun(StringBuilder sb, AskJoobyRun run,
            IReadOnlyDictionary<int, List<AskJoobyToolCall>> toolCallsByRunId,
            IReadOnlyDictionary<int, List<AskJoobyModelCall>> modelCallsByRunId,
            IReadOnlyDictionary<int, List<AskJoobyWebSearchCall>> webSearchCallsByRunId,
            IReadOnlyDictionary<int, List<AskJoobyOpenCorporatesCall>> openCorporatesCallsByRunId,
            IReadOnlyDictionary<int, List<AskJoobyRun>> runsByParentId, int depth)
        {
            var heading = new string('#', Math.Min(3 + depth, 6));
            sb.AppendLine($"{heading} Run #{run.Id} -- {NullToDash(run.AgentType)} (depth {run.Depth ?? 0})");
            sb.AppendLine();
            sb.AppendLine($"- **Status**: {FormatRunStatus(run.StatusId)}");
            sb.AppendLine($"- **Duration**: {FormatDuration(run.DurationMs)}");
            sb.AppendLine($"- **Created**: {FormatDate(run.CreatedDate)} by {NullToDash(run.CreatedUser)}");
            sb.AppendLine();
            sb.AppendLine("**Task:**");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrEmpty(run.Task) ? "_(none)_" : $"```\n{run.Task}\n```");
            sb.AppendLine();
            sb.AppendLine("**Result:**");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrEmpty(run.Result) ? "_(none)_" : $"```\n{run.Result}\n```");
            sb.AppendLine();

            if (modelCallsByRunId.TryGetValue(run.Id, out var runModelCalls) && runModelCalls.Count > 0)
            {
                sb.AppendLine("**Model calls:**");
                sb.AppendLine();
                sb.AppendLine("| Model | Input tokens | Output tokens | Finish reason | Duration | First chunk |");
                sb.AppendLine("| --- | --- | --- | --- | --- | --- |");

                foreach (var call in runModelCalls)
                {
                    sb.AppendLine($"| {FormatModelType(call.ModelTypeId)} | {call.InputTokens?.ToString() ?? "-"} | " +
                                  $"{call.OutputTokens?.ToString() ?? "-"} | {FormatFinishReason(call.FinishReasonId)} | " +
                                  $"{FormatDuration(call.DurationMs)} | {FormatDuration(call.FirstChunkMs)} |");
                }

                sb.AppendLine();
            }

            if (toolCallsByRunId.TryGetValue(run.Id, out var runToolCalls) && runToolCalls.Count > 0)
            {
                sb.AppendLine("**Tool calls:**");
                sb.AppendLine();

                foreach (var call in runToolCalls)
                {
                    sb.AppendLine($"- **{NullToDash(call.ToolName)}** -- {FormatToolCallStatus(call.StatusId)}, " +
                                  $"{FormatDuration(call.DurationMs)}, {FormatDate(call.CreatedDate)}");

                    if (!string.IsNullOrEmpty(call.Result))
                    {
                        sb.AppendLine($"  - Result preview: {AskJoobyChatAuditArtifacts.Preview(call.Result, 160)}");
                    }

                    if (AskJoobyChatAuditArtifacts.HasToolCallPayload(call))
                    {
                        sb.AppendLine(
                            $"  - Full arguments and result: `{AskJoobyChatAuditArtifacts.ToolCallFilePath(call)}`");
                    }
                }

                sb.AppendLine();
            }

            if (webSearchCallsByRunId.TryGetValue(run.Id, out var runWebSearchCalls) && runWebSearchCalls.Count > 0)
            {
                sb.AppendLine("**Web search calls:**");
                sb.AppendLine();

                foreach (var call in runWebSearchCalls)
                {
                    sb.AppendLine($"- **{NullToDash(call.Query)}** -- {FormatDuration(call.DurationMs)}, " +
                                  $"{FormatDate(call.CreatedDate)}");

                    if (!string.IsNullOrEmpty(call.ResponseJson))
                    {
                        sb.AppendLine(
                            $"  - Response preview: {AskJoobyChatAuditArtifacts.Preview(call.ResponseJson, 160)}");

                        sb.AppendLine(
                            $"  - Full response: `{AskJoobyChatAuditArtifacts.WebSearchFilePath(call)}`");
                    }
                }

                sb.AppendLine();
            }

            if (openCorporatesCallsByRunId.TryGetValue(run.Id, out var runOpenCorporatesCalls)
                && runOpenCorporatesCalls.Count > 0)
            {
                sb.AppendLine("**OpenCorporates calls:**");
                sb.AppendLine();

                foreach (var call in runOpenCorporatesCalls)
                {
                    sb.AppendLine($"- **{NullToDash(call.Operation)}** ({NullToDash(call.Query)}) -- " +
                                  $"{FormatDuration(call.DurationMs)}, {FormatDate(call.CreatedDate)}");

                    if (!string.IsNullOrEmpty(call.ResponseJson))
                    {
                        sb.AppendLine(
                            $"  - Response preview: {AskJoobyChatAuditArtifacts.Preview(call.ResponseJson, 160)}");

                        sb.AppendLine(
                            $"  - Full response: `{AskJoobyChatAuditArtifacts.OpenCorporatesFilePath(call)}`");
                    }
                }

                sb.AppendLine();
            }

            if (runsByParentId.TryGetValue(run.Id, out var childRuns))
            {
                foreach (var child in childRuns.OrderBy(r => r.Id))
                {
                    AppendRun(sb, child, toolCallsByRunId, modelCallsByRunId, webSearchCallsByRunId,
                        openCorporatesCallsByRunId, runsByParentId, depth + 1);
                }
            }
        }

        private static void AppendSqlLog(StringBuilder sb, IReadOnlyList<DynamicSqlQueryLog> sqlLog)
        {
            sb.AppendLine("## SQL Queries Executed");
            sb.AppendLine();

            sb.AppendLine("The final, resolved SQL that actually ran (or was attempted) via the dynamic-SQL " +
                          "tool -- after tenant/role scoping was spliced in, never the model's raw " +
                          "pre-substitution input.");

            sb.AppendLine();

            if (sqlLog.Count == 0)
            {
                sb.AppendLine("_No SQL queries were executed._");
                sb.AppendLine();

                return;
            }

            foreach (var entry in sqlLog)
            {
                sb.AppendLine($"### {FormatDate(entry.CreatedDate)} -- {(entry.Succeeded ? "Succeeded" : "Failed")}");
                sb.AppendLine();
                sb.AppendLine($"- **Rows**: {entry.RowCount?.ToString() ?? "-"}");
                sb.AppendLine($"- **Duration**: {FormatDuration(entry.DurationMs)}");

                if (!string.IsNullOrEmpty(entry.ErrorMessage))
                {
                    sb.AppendLine($"- **Error**: {entry.ErrorMessage}");
                }

                sb.AppendLine();
                sb.AppendLine($"```sql\n{entry.Sql}\n```");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendMemories(StringBuilder sb, IReadOnlyList<MemoryPoco> memories)
        {
            sb.AppendLine("## Memories Recorded This Conversation");
            sb.AppendLine();

            sb.AppendLine("Only memories scoped to (and laid down by) this one conversation -- not the " +
                          "user's or tenant's whole Salt Lick, which this conversation may separately have " +
                          "recalled from (see the MemoryTool.Recall tool calls above for what was read).");

            sb.AppendLine();

            if (memories.Count == 0)
            {
                sb.AppendLine("_No conversation-scoped memories were recorded._");
                sb.AppendLine();

                return;
            }

            foreach (var memory in memories)
            {
                sb.AppendLine($"- **{FormatMemoryKind(memory.KindId)}** ({FormatDate(memory.CreatedDate)}, " +
                              $"status: {FormatMemoryStatus(memory.StatusId)}): {memory.Content}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private const int DataCachePreviewRowLimit = 10;

        private static void AppendDataCache(StringBuilder sb, IReadOnlyList<AskJoobyDataCacheListItemJsonDto> entries)
        {
            sb.AppendLine("## Cached Query Results");
            sb.AppendLine();

            sb.AppendLine("Every tabular result the SQL/export tools cached during this conversation -- the " +
                          "same data a data_pointer referenced. Only the first rows are shown here; the full " +
                          "CSV is in the linked file.");

            sb.AppendLine();

            if (entries.Count == 0)
            {
                sb.AppendLine("_No cached query results._");
                sb.AppendLine();

                return;
            }

            foreach (var entry in entries)
            {
                sb.AppendLine($"### {NullToDash(entry.Label)} ({entry.RowCount} row(s), pointer `{entry.PointerKey}`)");
                sb.AppendLine();
                AppendCachedTablePreview(sb, entry);
                sb.AppendLine($"- **Full data**: `{AskJoobyChatAuditArtifacts.DataCacheFilePath(entry)}`");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendCachedTablePreview(StringBuilder sb, AskJoobyDataCacheListItemJsonDto entry)
        {
            if (!AskJoobyChatAuditArtifacts.TryParseCachedTable(entry.ColumnsJson, entry.DataJson, out var columns,
                    out var rows))
            {
                sb.AppendLine("_(cached data could not be rendered as a table)_");

                return;
            }

            if (columns.Count == 0 || rows.Count == 0)
            {
                sb.AppendLine("_(no rows)_");

                return;
            }

            sb.AppendLine("| " + string.Join(" | ", columns) + " |");
            sb.AppendLine("| " + string.Join(" | ", columns.Select(_ => "---")) + " |");

            foreach (var row in rows.Take(DataCachePreviewRowLimit))
            {
                var cells = columns.Select(c => row.TryGetValue(c, out var value)
                    ? value.ToString().Replace("|", "\\|").Replace("\n", " ")
                    : "");
                sb.AppendLine("| " + string.Join(" | ", cells) + " |");
            }

            if (rows.Count > DataCachePreviewRowLimit)
            {
                sb.AppendLine();
                sb.AppendLine($"_(showing first {DataCachePreviewRowLimit} of {rows.Count} rows)_");
            }
        }

        private static void AppendCompactions(StringBuilder sb, IReadOnlyList<AskJoobyCompaction> compactions)
        {
            sb.AppendLine("## History Compaction");
            sb.AppendLine();

            sb.AppendLine("Every time this conversation's history was folded down to keep it inside the " +
                          "context window, with the full text on both sides of the fold. The transcript " +
                          "above shows what was said; this shows what later turns were actually still able " +
                          "to see, which is the only place the difference is recorded.");

            sb.AppendLine();

            if (compactions.Count == 0)
            {
                sb.AppendLine("_History was never compacted in this conversation._");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                return;
            }

            foreach (var compaction in compactions)
            {
                var before = compaction.BeforeCharacters ?? 0;
                var after = compaction.AfterCharacters ?? 0;
                var saved = before > 0 ? 100.0 - after * 100.0 / before : 0.0;

                sb.AppendLine(
                    $"### {FormatDate(compaction.CreatedDate)} -- {compaction.MessageCount ?? 0} message(s) folded");

                sb.AppendLine();
                sb.AppendLine($"- **Characters**: {before} before, {after} after ({saved:0.0}% removed)");

                sb.AppendLine($"- **Messages**: {compaction.AskJoobyMessageId?.ToString() ?? "-"} " +
                              $"compacted to {compaction.CompactedToAskJoobyMessageId?.ToString() ?? "-"}");

                sb.AppendLine($"- **Duration**: {compaction.DurationMs?.ToString() ?? "-"} ms");
                sb.AppendLine($"- **By**: {NullToDash(compaction.CreatedUser)}");
                sb.AppendLine($"- **Before preview**: {AskJoobyChatAuditArtifacts.Preview(compaction.Before, 200)}");
                sb.AppendLine($"- **After preview**: {AskJoobyChatAuditArtifacts.Preview(compaction.After, 200)}");

                sb.AppendLine($"- **Full before/after**: " +
                              $"`{AskJoobyChatAuditArtifacts.CompactionBeforeFilePath(compaction)}` / " +
                              $"`{AskJoobyChatAuditArtifacts.CompactionAfterFilePath(compaction)}`");

                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendRouting(StringBuilder sb, IReadOnlyList<AskJoobyRouting> routings)
        {
            sb.AppendLine("## Routing Decisions");
            sb.AppendLine();

            sb.AppendLine("Which agent each turn was given to, and which tier of the router decided it. The " +
                          "transcript shows who answered; this shows why they were the one asked, which is " +
                          "the question a turn answered by the wrong specialist always raises. The tiers " +
                          "differ by orders of magnitude in cost, so a deployment that believes it routes " +
                          "on wording rules while in fact falling through to a model every time can see " +
                          "that here and nowhere else.");

            sb.AppendLine();

            if (routings.Count == 0)
            {
                sb.AppendLine("_No routing decision was recorded for this conversation._");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                return;
            }

            sb.AppendLine(
                "| When | Agent | Decided by | Score | Embedding requests | Embedding values | Duration | Error |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");

            foreach (var routing in routings)
            {
                sb.AppendLine($"| {FormatDate(routing.CreatedDate)} | {NullToDash(routing.AgentType)} "
                              + $"| {FormatRoutingStage(routing.Stage)} "
                              + $"| {routing.Score?.ToString("0.000") ?? "-"} "
                              + $"| {routing.EmbeddingRequests?.ToString() ?? "-"} "
                              + $"| {routing.EmbeddingValues?.ToString() ?? "-"} "
                              + $"| {FormatDuration(routing.DurationMs)} | {NullToDash(routing.Error)} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static string FormatRoutingStage(int? stage)
        {
            return stage switch
            {
                1 => "Pattern",
                2 => "Embedding",
                3 => "Model",
                4 => "Default",
                5 => "No candidates",
                6 => "Continuity",
                null => "-",
                _ => stage.Value.ToString()
            };
        }

        private static void AppendAgentGuardRejections(
            StringBuilder sb, IReadOnlyList<AskJoobyAgentGuardRejection> agentGuardRejections)
        {
            sb.AppendLine("## AgentGuard Rejections");
            sb.AppendLine();

            sb.AppendLine("Every time one of AskJooby's AgentGuard rules fired against this conversation -- " +
                          "blocking a message, a tool call, or a tool result outright, or redacting part of " +
                          "one. No rejected content is stored here, only which rule fired and why, matching " +
                          "AgentGuard's own hash-only default for this kind of record.");

            sb.AppendLine();

            if (agentGuardRejections.Count == 0)
            {
                sb.AppendLine("_No AgentGuard rule fired in this conversation._");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                return;
            }

            sb.AppendLine("| When | Phase | Pipeline | Rule | Action | Reason |");
            sb.AppendLine("|---|---|---|---|---|---|");

            foreach (var rejection in agentGuardRejections)
            {
                sb.AppendLine($"| {FormatDate(rejection.CreatedDate)} | {NullToDash(rejection.PhaseId?.ToString())} "
                              + $"| {NullToDash(rejection.PipelineName)} | {NullToDash(rejection.RuleName)} "
                              + $"| {NullToDash(rejection.ActionId?.ToString())} | {NullToDash(rejection.Reason)} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendWorkflowSteps(StringBuilder sb, IReadOnlyList<AskJoobyWorkflowStep> steps)
        {
            sb.AppendLine("## Workflow Progress Reported");
            sb.AppendLine();

            sb.AppendLine("Every statement an agent made about where a graph-directed workflow had got to, in " +
                          "the order it made them. Append only, so a step reported done and later reported " +
                          "not done appears twice rather than being overwritten -- the corrections are the " +
                          "interesting rows, and a correction is only readable beside what it corrects.");

            sb.AppendLine();

            if (steps.Count == 0)
            {
                sb.AppendLine("_No workflow progress was reported in this conversation._");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                return;
            }

            sb.AppendLine("| When | Workflow | Step | Stated | Note | By |");
            sb.AppendLine("|---|---|---|---|---|---|");

            foreach (var step in steps)
            {
                var stated = step.Completed switch
                {
                    1 => "done",
                    0 => "not done",
                    _ => "-"
                };

                sb.AppendLine($"| {FormatDate(step.CreatedDate)} | {NullToDash(step.WorkflowId)} "
                              + $"| {NullToDash(step.NodeId)} | {stated} | {NullToDash(step.Note)} "
                              + $"| {NullToDash(step.CreatedUser)} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        private static void AppendFiles(StringBuilder sb, IReadOnlyList<AskJoobyFile> files)
        {
            sb.AppendLine("## Files");
            sb.AppendLine();

            sb.AppendLine("Files are linked, never embedded -- the same download endpoint the widget " +
                          "itself uses, still subject to its own access control.");

            sb.AppendLine();

            if (files.Count == 0)
            {
                sb.AppendLine("_No files attached._");
                sb.AppendLine();

                return;
            }

            foreach (var file in files)
            {
                sb.AppendLine($"- {FileLink(file)} -- {file.Size?.ToString() ?? "?"} bytes, " +
                              $"{NullToDash(file.ContentType)}, uploaded {FormatDate(file.CreatedDate)} " +
                              $"by {NullToDash(file.CreatedUser)}");
            }

            sb.AppendLine();
        }

        private static string FileLink(AskJoobyFile file)
        {
            var name = string.IsNullOrEmpty(file.Name) ? $"file-{file.Id}" : file.Name;

            return $"[{name}]({FileDownloadPathPrefix}{file.Id})";
        }

        private static string FormatMemoryStatus(byte? status)
        {
            return (AskJoobyMemoryFeedbackStatus?)status switch
            {
                AskJoobyMemoryFeedbackStatus.Correct => "confirmed correct",
                AskJoobyMemoryFeedbackStatus.Incorrect => "confirmed incorrect",
                _ => "unreviewed"
            };
        }

        private static string FormatMemoryKind(byte kindId)
        {
            return Enum.IsDefined(typeof(AskJoobyMemoryKind), kindId)
                ? ((AskJoobyMemoryKind)kindId).ToString()
                : "-";
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                : "-";
        }

        private static string FormatDuration(long? ms)
        {
            return ms.HasValue ? $"{ms.Value} ms" : "-";
        }

        private static string NullToDash(string? value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static string FormatModelType(byte? modelTypeId)
        {
            if (modelTypeId is not { } value)
            {
                return "-";
            }

            return Enum.IsDefined(typeof(AskJoobyModelType), value) ? ((AskJoobyModelType)value).ToString() : "-";
        }

        private static string FormatRunStatus(byte? statusId)
        {
            return Enum.IsDefined(typeof(AskJoobyRunStatus), statusId ?? 0) && statusId.HasValue
                ? ((AskJoobyRunStatus)statusId.Value).ToString()
                : "-";
        }

        private static string FormatToolCallStatus(byte? statusId)
        {
            return Enum.IsDefined(typeof(AskJoobyToolCallStatus), statusId ?? 0) && statusId.HasValue
                ? ((AskJoobyToolCallStatus)statusId.Value).ToString()
                : "-";
        }

        private static string FormatFinishReason(byte? finishReasonId)
        {
            return finishReasonId.HasValue && Enum.IsDefined(typeof(AskJoobyFinishReason), finishReasonId.Value)
                ? ((AskJoobyFinishReason)finishReasonId.Value).ToString()
                : "-";
        }

        private static string RoleName(byte? role)
        {
            return role switch
            {
                (byte)AskJoobyMessageRole.User => "User",
                (byte)AskJoobyMessageRole.Assistant => "Assistant",
                (byte)AskJoobyMessageRole.System => "System",
                _ => "Message"
            };
        }
    }
}