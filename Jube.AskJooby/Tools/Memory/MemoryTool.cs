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
using System.Text.RegularExpressions;
using Jube.AskJooby.Memory;
using Jube.AskJooby.Tools.Memory.Interfaces;

namespace Jube.AskJooby.Tools.Memory
{
    public sealed class MemoryTool(IMemoryClient memory)
    {
        private const int MaxAllowedMemories = 8;

        private static readonly Regex volatileValue = new(
            @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b" +
            @"|\b\d{4}-\d{2}-\d{2}\b" +
            @"|\bId\s*[:=#]?\s*\d+\b" +
            @"|\b\d+\s+(rows?|records?|rules?|counters?|transactions?|accounts?)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [Description(
            "Lays down a durable, generic fact at the Salt Lick for later recall. Only remember what stays " +
            "true: a generic plan, or a SQL pattern with its literal values replaced by named parameters. " +
            "Never remember live values -- counts, ids, guids, dates, rule or counter state -- because they " +
            "go stale; query those again when you need them. Content carrying such values is refused. " +
            "Use \"User\" scope (the default) for " +
            "anything specific to this caller that should follow them into future conversations too -- " +
            "their preferences, their context. Use \"System\" scope deliberately and sparingly, only for " +
            "something genuinely useful to any user of this platform -- System entries are shared " +
            "tenant-wide, so treat this as writing something durable for a colleague, not a private note. " +
            "Use \"Conversation\" scope for something that only matters to this specific conversation and " +
            "its own sub-tasks -- it will never surface in a different conversation, even for the same " +
            "caller, which keeps one-off task state from polluting unrelated future chats.")]
        public async Task<string> Remember(
            [Description("The fact to remember, in plain language.")]
            string content,
            [Description(
                "\"User\" (follows this caller into future conversations), \"System\" (shared tenant-wide), " +
                "or \"Conversation\" (visible only within this one conversation and its own sub-tasks).")]
            string scope = "User",
            [Description(
                "Minutes until this memory expires and stops being recalled. Leave unset (LIFETIME) for " +
                "important, durable facts -- most memories should never expire and this should be your " +
                "default. Set it deliberately for genuinely short-lived state: TRANSIENT (roughly 5-30 " +
                "minutes) for something only useful for the next few steps of what you're doing right " +
                "now; EPISODIC (roughly 60-1440 minutes, i.e. an hour to a day) for something tied to " +
                "the current topic that will go stale once it's resolved, e.g. queuing up a follow-up " +
                "for yourself later today. Not valid with \"System\" scope -- durable, shared knowledge " +
                "should never expire.")]
            int? expiresInMinutes = null,
            [Description(
                "\"Fact\" (the default) for a durable observation about the caller or this platform -- " +
                "something you'd stand behind later. \"Reflection\" for your own internal working-through: " +
                "a plan, a note-to-self about a task in progress, your reasoning about how to approach " +
                "something. Reflections are excluded from Recall by default, so use \"Fact\" for anything " +
                "you actually want to inform a future answer.")]
            string kind = "Fact",
            CancellationToken token = default)
        {
            if (volatileValue.IsMatch(content))
            {
                throw new ArgumentException(
                    "That looks like a live value (an id, guid, date or count), which goes stale. Remember the " +
                    "generic plan or the parameterised SQL instead, and query the live value when you need it.");
            }

            if (scope != "User" && scope != "System" && scope != "Conversation")
            {
                throw new ArgumentException("scope must be \"User\", \"System\", or \"Conversation\".");
            }

            if (expiresInMinutes.HasValue && scope == "System")
            {
                throw new ArgumentException("expiresInMinutes is not supported for \"System\" scope memories.");
            }

            if (expiresInMinutes is <= 0)
            {
                throw new ArgumentException("expiresInMinutes must be positive.");
            }

            if (kind != "Fact" && kind != "Reflection")
            {
                throw new ArgumentException("kind must be \"Fact\" or \"Reflection\".");
            }

            var expiresAt = expiresInMinutes.HasValue
                ? DateTime.UtcNow.AddMinutes(expiresInMinutes.Value)
                : (DateTime?)null;

            var saved = await memory.RememberAsync(content, scope, expiresAt: expiresAt, kind: kind, token: token);

            return $"Remembered at the Salt Lick ({saved.Scope} scope).";
        }

        [Description(
            "Visits the Salt Lick for facts relevant to a query -- always draws on this caller's own " +
            "entries, everything shared tenant-wide, and anything laid down earlier in this specific " +
            "conversation. Use before assuming you don't know something about this caller or this " +
            "platform's conventions.")]
        public async Task<string> Recall(
            [Description("A natural-language description of what you want to recall.")]
            string query,
            [Description(
                "How many memories to return. Leave unset to use the default. Use fewer (1-2) for a " +
                "narrow lookup, more (up to 8) when you're casting a wider net or unsure what's there.")]
            int? maxMemories = null,
            [Description(
                "Only return memories recorded within this many days. Leave unset (the default) to " +
                "search across all time -- most lookups want a durable fact regardless of its age. Set " +
                "this when you specifically want recent state rather than old background knowledge, " +
                "e.g. 1 for \"today\", 7 for \"this past week\".")]
            int? sinceDaysAgo = null,
            [Description(
                "Also include your own past Reflections (internal working-through, not confirmed facts) " +
                "in the results, not just Facts. Leave false (the default) for almost every lookup -- only " +
                "set true if you specifically want to see your own past reasoning about something, e.g. " +
                "resuming a multi-step task you left notes for yourself about.")]
            bool includeReflections = false,
            CancellationToken token = default)
        {
            if (sinceDaysAgo is <= 0)
            {
                throw new ArgumentException("sinceDaysAgo must be positive.");
            }

            var effectiveTopK = maxMemories.HasValue ? Math.Clamp(maxMemories.Value, 1, MaxAllowedMemories) : 5;
            var since = sinceDaysAgo.HasValue ? DateTime.UtcNow.AddDays(-sinceDaysAgo.Value) : (DateTime?)null;
            var hits = await memory.RecallAsync(query, effectiveTopK, since, includeReflections, token);

            if (hits.Count == 0)
            {
                return "Nothing at the Salt Lick matches that.";
            }

            var builder = new StringBuilder();

            foreach (var hit in hits)
            {
                var kindTag = hit.Kind == "Reflection" ? " [Reflection]" : "";
                builder.AppendLine($"[{hit.Scope}]{kindTag} {DescribeStatus(hit.Status)} ({DescribeAge(hit)})");
                builder.AppendLine(hit.Content);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string DescribeAge(MemoryEntry hit)
        {
            var recorded = hit.CreatedDate.HasValue
                ? $"recorded {hit.CreatedDate.Value:yyyy-MM-dd HH:mm} UTC"
                : "recorded date unknown";

            return hit.ExpiresAt.HasValue
                ? $"{recorded}, expires {hit.ExpiresAt.Value:yyyy-MM-dd HH:mm} UTC"
                : recorded;
        }

        private static string DescribeStatus(byte? status)
        {
            return status switch
            {
                1 => "This is an example of a correct response.",
                0 => "This is an example of an incorrect response -- do not repeat this.",
                _ => "This is an example of an agent generated memory."
            };
        }
    }
}