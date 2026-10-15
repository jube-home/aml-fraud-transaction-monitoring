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

using System.ComponentModel;
using System.Text;
using Jube.AskJooby.Tools.History.Interfaces;

namespace Jube.AskJooby.Tools.History
{
    public sealed class ConversationHistoryTool(IConversationHistoryClient history)
    {
        private const int MaxAllowedResults = 8;
        private const int MaxCharactersPerEntry = 4000;

        [Description(
            "Searches the Hoofprints -- past AskJooby conversation turns, not the current one, that's " +
            "already in front of you -- by keyword and meaning: the immediate, sequential trail back " +
            "through what's already been said. Use \"User\" scope to search only this caller's own past " +
            "conversations, or \"System\" to search every user's conversations tenant-wide. Useful when " +
            "the caller references something discussed before, or you suspect this exact question has " +
            "already been answered.")]
        public async Task<string> SearchHistory(
            [Description("A natural-language description of what you're looking for.")]
            string query,
            [Description("\"User\" (only this caller's own past conversations) or \"System\" (every " +
                         "user's, tenant-wide).")]
            string scope = "User",
            [Description(
                "How many past turns to return. Leave unset to use the default. Use fewer (1-2) for a " +
                "narrow lookup, more (up to 8) when you're casting a wider net or unsure what's there.")]
            int? maxResults = null,
            CancellationToken token = default)
        {
            var effectiveTopK = maxResults.HasValue ? Math.Clamp(maxResults.Value, 1, MaxAllowedResults) : 5;
            var hits = await history.SearchAsync(query, scope, effectiveTopK, token);

            if (hits.Count == 0)
            {
                return "No matching Hoofprints found.";
            }

            var builder = new StringBuilder();

            foreach (var hit in hits)
            {
                var label = string.IsNullOrWhiteSpace(hit.Title) ? $"Conversation #{hit.ConversationId}" : hit.Title;
                var when = hit.CreatedDate.HasValue ? $"{hit.CreatedDate.Value:yyyy-MM-dd HH:mm} UTC" : "date unknown";

                var content = hit.Content.Length > MaxCharactersPerEntry
                    ? hit.Content[..MaxCharactersPerEntry] + "\n[...truncated]"
                    : hit.Content;

                builder.AppendLine($"[{label}]{(hit.IsCompact ? " (summary)" : "")} ({when}) {content}");
            }

            return builder.ToString();
        }
    }
}