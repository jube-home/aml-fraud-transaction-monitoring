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
using Jube.Data.Repository.Models.AskJoobyDataCache;

namespace Jube.Service.AskJoobyConversation
{
    using ConversationPoco = Data.Poco.AskJoobyConversation;

    internal static class AskJoobyChatAuditBundleBuilder
    {
        private static readonly JsonSerializerOptions prettyJsonOptions = new() { WriteIndented = true };

        internal static List<AuditArtifactFile> BuildArtifactFiles(
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyDataCacheListItemJsonDto> dataCacheEntries,
            IReadOnlyList<AskJoobyCompaction> compactions,
            IReadOnlyList<AskJoobyWebSearchCall> webSearchCalls,
            IReadOnlyList<AskJoobyOpenCorporatesCall> openCorporatesCalls)
        {
            var files = new List<AuditArtifactFile>();

            foreach (var call in toolCalls.Where(AskJoobyChatAuditArtifacts.HasToolCallPayload))
            {
                files.Add(new AuditArtifactFile(AskJoobyChatAuditArtifacts.ToolCallFilePath(call),
                    BuildToolCallFile(call)));
            }

            foreach (var entry in dataCacheEntries)
            {
                files.Add(new AuditArtifactFile(AskJoobyChatAuditArtifacts.DataCacheFilePath(entry),
                    BuildDataCacheCsv(entry)));
            }

            foreach (var compaction in compactions)
            {
                if (!string.IsNullOrEmpty(compaction.Before))
                {
                    files.Add(new AuditArtifactFile(AskJoobyChatAuditArtifacts.CompactionBeforeFilePath(compaction),
                        Encoding.UTF8.GetBytes(compaction.Before)));
                }

                if (!string.IsNullOrEmpty(compaction.After))
                {
                    files.Add(new AuditArtifactFile(AskJoobyChatAuditArtifacts.CompactionAfterFilePath(compaction),
                        Encoding.UTF8.GetBytes(compaction.After)));
                }
            }

            foreach (var call in webSearchCalls.Where(c => !string.IsNullOrEmpty(c.ResponseJson)))
            {
                files.Add(new AuditArtifactFile(AskJoobyChatAuditArtifacts.WebSearchFilePath(call),
                    BuildJsonFile(call.ResponseJson)));
            }

            foreach (var call in openCorporatesCalls.Where(c => !string.IsNullOrEmpty(c.ResponseJson)))
            {
                files.Add(new AuditArtifactFile(AskJoobyChatAuditArtifacts.OpenCorporatesFilePath(call),
                    BuildJsonFile(call.ResponseJson)));
            }

            return files;
        }

        internal static byte[] BuildManifest(
            ConversationPoco conversation,
            DateTime generatedAtUtc,
            string generatedByUser,
            IReadOnlyList<AskJoobyToolCall> toolCalls,
            IReadOnlyList<AskJoobyDataCacheListItemJsonDto> dataCacheEntries,
            IReadOnlyList<AskJoobyCompaction> compactions,
            IReadOnlyList<AskJoobyWebSearchCall> webSearchCalls,
            IReadOnlyList<AskJoobyOpenCorporatesCall> openCorporatesCalls)
        {
            var artifacts = new List<object>();

            foreach (var call in toolCalls.Where(AskJoobyChatAuditArtifacts.HasToolCallPayload))
            {
                artifacts.Add(new
                {
                    kind = "tool-call", id = call.Id, path = AskJoobyChatAuditArtifacts.ToolCallFilePath(call),
                    runId = call.AskJoobyRunId, toolName = call.ToolName
                });
            }

            foreach (var entry in dataCacheEntries)
            {
                artifacts.Add(new
                {
                    kind = "data-cache", id = entry.Id, path = AskJoobyChatAuditArtifacts.DataCacheFilePath(entry),
                    pointerKey = entry.PointerKey, label = entry.Label, rowCount = entry.RowCount
                });
            }

            foreach (var compaction in compactions)
            {
                if (!string.IsNullOrEmpty(compaction.Before))
                {
                    artifacts.Add(new
                    {
                        kind = "compaction-before", id = compaction.Id,
                        path = AskJoobyChatAuditArtifacts.CompactionBeforeFilePath(compaction),
                        messageId = compaction.AskJoobyMessageId
                    });
                }

                if (!string.IsNullOrEmpty(compaction.After))
                {
                    artifacts.Add(new
                    {
                        kind = "compaction-after", id = compaction.Id,
                        path = AskJoobyChatAuditArtifacts.CompactionAfterFilePath(compaction),
                        messageId = compaction.CompactedToAskJoobyMessageId
                    });
                }
            }

            foreach (var call in webSearchCalls.Where(c => !string.IsNullOrEmpty(c.ResponseJson)))
            {
                artifacts.Add(new
                {
                    kind = "web-search", id = call.Id, path = AskJoobyChatAuditArtifacts.WebSearchFilePath(call),
                    runId = call.AskJoobyRunId, query = call.Query
                });
            }

            foreach (var call in openCorporatesCalls.Where(c => !string.IsNullOrEmpty(c.ResponseJson)))
            {
                artifacts.Add(new
                {
                    kind = "open-corporates", id = call.Id,
                    path = AskJoobyChatAuditArtifacts.OpenCorporatesFilePath(call), runId = call.AskJoobyRunId,
                    operation = call.Operation, query = call.Query
                });
            }

            var manifest = new
            {
                conversationId = conversation.Id, conversationGuid = conversation.Guid, title = conversation.Title,
                generatedAtUtc, generatedByUser, technicalLog = "technical-log.md", artifacts
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest, prettyJsonOptions));
        }

        private static byte[] BuildToolCallFile(AskJoobyToolCall call)
        {
            var payload = new
            {
                toolName = call.ToolName, runId = call.AskJoobyRunId, createdDateUtc = call.CreatedDate,
                durationMs = call.DurationMs, arguments = AskJoobyChatAuditArtifacts.ParseJsonOrRaw(call.Arguments),
                result = AskJoobyChatAuditArtifacts.ParseJsonOrRaw(call.Result)
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, prettyJsonOptions));
        }

        private static byte[] BuildJsonFile(string raw)
        {
            var parsed = AskJoobyChatAuditArtifacts.ParseJsonOrRaw(raw);

            return parsed is JsonElement element
                ? Encoding.UTF8.GetBytes(JsonSerializer.Serialize(element, prettyJsonOptions))
                : Encoding.UTF8.GetBytes(raw);
        }

        private static byte[] BuildDataCacheCsv(AskJoobyDataCacheListItemJsonDto entry)
        {
            var sb = new StringBuilder();

            if (!AskJoobyChatAuditArtifacts.TryParseCachedTable(entry.ColumnsJson, entry.DataJson, out var columns,
                    out var rows))
            {
                sb.AppendLine("could not parse cached data");

                return Encoding.UTF8.GetBytes(sb.ToString());
            }

            sb.AppendLine(string.Join(",", columns.Select(AskJoobyChatAuditArtifacts.CsvEscape)));

            foreach (var row in rows)
            {
                var cells = columns.Select(c => row.TryGetValue(c, out var value) ? value.ToString() : "");
                sb.AppendLine(string.Join(",", cells.Select(AskJoobyChatAuditArtifacts.CsvEscape)));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}