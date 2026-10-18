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
using Jube.AskJooby.Tools.Compaction.Interfaces;

namespace Jube.AskJooby.Tools.Compaction
{
    public sealed class CompactedHistoryTool(ICompactedHistoryClient client)
    {
        private const int MaxCharactersPerEntry = 4000;

        [Description(
            "Chews the cud: reads back the ORIGINAL wording of an earlier part of this conversation that " +
            "has since been folded into a summary. Older turns are summarised to keep the conversation " +
            "affordable, and a summary can lose a detail -- an exact value, a name, a precise thing the " +
            "caller said. Use this when the summary you can see refers to something without stating it, " +
            "when the caller refers back to a specific they gave you earlier and you only have the gist, " +
            "or when they say you have forgotten something. Do NOT use it to re-read what is already in " +
            "front of you, and do not use it for Jube documentation -- that is the Pasture.")]
        public async Task<string> SearchCompactedHistory(
            [Description("What you are trying to recover, in natural language -- the detail, not the topic.")]
            string query,
            [Description(
                "Leave true to look only in this conversation, which is nearly always what you want. Set " +
                "false to look across the caller's earlier conversations as well.")]
            bool thisConversationOnly = true,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Say what you are trying to recover.";
            }

            var entries = await client.SearchAsync(query, thisConversationOnly, 3, token).ConfigureAwait(false);

            if (entries.Count == 0)
            {
                return "Nothing compacted matches that. Either it was never said, or it is still in front "
                       + "of you -- re-read what you already have rather than asking again.";
            }

            var builder = new StringBuilder();

            builder.AppendLine($"Found {entries.Count} compacted stretch"
                               + $"{(entries.Count == 1 ? "" : "es")} of original conversation.");

            builder.AppendLine();

            foreach (var entry in entries)
            {
                var title = entry.Title ?? "this conversation";
                var content = entry.Before;

                var text = content.Length > MaxCharactersPerEntry
                    ? content[..MaxCharactersPerEntry] + "\n[...truncated]"
                    : content;

                builder.AppendLine($"[Original turns from \"{title}\"]");
                builder.AppendLine(text);
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}