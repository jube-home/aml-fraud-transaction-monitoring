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
    internal sealed record AuditArtifactFile(string RelativePath, byte[] Content);

    internal static class AskJoobyChatAuditArtifacts
    {
        internal const string ToolCallsDirectory = "tool-calls";
        internal const string DataCacheDirectory = "data-cache";
        internal const string CompactionsDirectory = "compactions";
        internal const string WebSearchDirectory = "web-search";
        internal const string OpenCorporatesDirectory = "open-corporates";

        internal static bool HasToolCallPayload(AskJoobyToolCall call)
        {
            return !string.IsNullOrEmpty(call.Arguments) || !string.IsNullOrEmpty(call.Result);
        }

        internal static string ToolCallFilePath(AskJoobyToolCall call)
        {
            return $"{ToolCallsDirectory}/{call.Id:D5}-{SanitizeFileNameSegment(call.ToolName)}.json";
        }

        internal static string DataCacheFilePath(AskJoobyDataCacheListItemJsonDto entry)
        {
            var label = string.IsNullOrWhiteSpace(entry.Label) ? entry.PointerKey : entry.Label;

            return $"{DataCacheDirectory}/{entry.Id:D5}-{SanitizeFileNameSegment(label)}.csv";
        }

        internal static string CompactionBeforeFilePath(AskJoobyCompaction compaction)
        {
            return $"{CompactionsDirectory}/{compaction.Id:D5}-before.txt";
        }

        internal static string CompactionAfterFilePath(AskJoobyCompaction compaction)
        {
            return $"{CompactionsDirectory}/{compaction.Id:D5}-after.txt";
        }

        internal static string WebSearchFilePath(AskJoobyWebSearchCall call)
        {
            return $"{WebSearchDirectory}/{call.Id:D5}.json";
        }

        internal static string OpenCorporatesFilePath(AskJoobyOpenCorporatesCall call)
        {
            return $"{OpenCorporatesDirectory}/{call.Id:D5}.json";
        }

        internal static string SanitizeFileNameSegment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "untitled";
            }

            var sb = new StringBuilder();

            foreach (var c in value.Trim())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }

            var result = sb.ToString().Trim('-');

            if (result.Length > 40)
            {
                result = result[..40];
            }

            return result.Length == 0 ? "untitled" : result;
        }

        internal static string Preview(string? text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "_(none)_";
            }

            var singleLine = text.Replace("\r", "").Replace("\n", " ⏎ ").Trim();

            return singleLine.Length <= maxChars ? singleLine : singleLine[..maxChars] + "…";
        }

        internal static bool TryParseCachedTable(string columnsJson, string dataJson,
            out List<string> columns, out List<Dictionary<string, JsonElement>> rows)
        {
            try
            {
                columns = JsonSerializer.Deserialize<List<JsonElement>>(columnsJson)?
                    .Select(c => c.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : c.ToString())
                    .ToList() ?? [];

                rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(dataJson) ?? [];

                return true;
            }
            catch (JsonException)
            {
                columns = [];
                rows = [];

                return false;
            }
        }

        internal static object? ParseJsonOrRaw(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(raw);

                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return raw;
            }
        }

        internal static string CsvEscape(string value)
        {
            return value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        internal static string? ExtractStringArgument(string? argumentsJson, string propertyName)
        {
            if (string.IsNullOrEmpty(argumentsJson))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(argumentsJson);

                return document.RootElement.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}