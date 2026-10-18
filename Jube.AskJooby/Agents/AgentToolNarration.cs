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

using System.Text.RegularExpressions;

namespace Jube.AskJooby.Agents
{
    public static class AgentToolNarration
    {
        public static string? DescribeStarting(string toolName)
        {
            return Describe(toolName, false);
        }

        public static string? DescribeFinished(string toolName)
        {
            return Describe(toolName, true);
        }

        private const string AnLetters = "AEFHILMNORSX";

        private static readonly Regex tokenPattern =
            new(@"[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z0-9]+", RegexOptions.Compiled);

        private static readonly Dictionary<string, (string Gerund, string Past, bool TakesArticle)> operations =
            new(StringComparer.Ordinal)
            {
                ["List"] = ("Listing", "Listed", false),
                ["Count"] = ("Counting", "Counted", false),
                ["Filter"] = ("Filtering", "Filtered", false),
                ["Search"] = ("Searching", "Searched", false),
                ["Login"] = ("Signing in", "Signed in", false),
                ["Logout"] = ("Signing out", "Signed out", false),
                ["Get"] = ("Fetching", "Fetched", true),
                ["Create"] = ("Creating", "Created", true),
                ["Update"] = ("Updating", "Updated", true),
                ["Delete"] = ("Deleting", "Deleted", true),
                ["Validate"] = ("Validating", "Validated", true),
                ["Execute"] = ("Running", "Ran", true),
                ["Run"] = ("Running", "Ran", true),
                ["Invoke"] = ("Invoking", "Invoked", true),
                ["Parse"] = ("Parsing", "Parsed", true),
                ["Compile"] = ("Compiling", "Compiled", true),
                ["Replay"] = ("Replaying", "Replayed", true),
                ["Insert"] = ("Inserting", "Inserted", true),
                ["Upload"] = ("Uploading", "Uploaded", true),
                ["Reset"] = ("Resetting", "Reset", true),
                ["Stop"] = ("Stopping", "Stopped", true),
                ["Remove"] = ("Removing", "Removed", true),
                ["Build"] = ("Building", "Built", true),
                ["Switch"] = ("Switching", "Switched", true),
                ["Send"] = ("Sending", "Sent", true),
                ["Allocate"] = ("Allocating", "Allocated", true),
                ["Deallocate"] = ("Deallocating", "Deallocated", true),
                ["Reallocate"] = ("Reallocating", "Reallocated", true),
                ["Rollback"] = ("Rolling back", "Rolled back", true),
                ["Submit"] = ("Submitting", "Submitted", true),
                ["Explain"] = ("Explaining", "Explained", true),
                ["Describe"] = ("Describing", "Described", true),
                ["Check"] = ("Checking", "Checked", true),
                ["Calculate"] = ("Calculating", "Calculated", true),
                ["Archive"] = ("Archiving", "Archived", true),
                ["Compare"] = ("Comparing", "Compared", true),
                ["Promote"] = ("Promoting", "Promoted", true),
                ["Capture"] = ("Capturing", "Captured", true),
                ["Exclude"] = ("Excluding", "Excluded", true),
                ["Show"] = ("Showing", "Showed", true),
                ["Set"] = ("Setting", "Set", true)
            };

        private static readonly Dictionary<string, string> acronyms = new(StringComparer.Ordinal)
        {
            ["Ttl"] = "TTL",
            ["TTL"] = "TTL",
            ["Http"] = "HTTP",
            ["Json"] = "JSON",
            ["Csv"] = "CSV",
            ["Sql"] = "SQL",
            ["Otlp"] = "OTLP",
            ["Api"] = "API",
            ["Kvp"] = "KVP",
            ["Id"] = "ID",
            ["Guid"] = "GUID",
            ["Roc"] = "ROC",
            ["Waf"] = "WAF",
            ["XPath"] = "XPath",
            ["HAProxy"] = "HAProxy",
            ["OpenTelemetry"] = "OpenTelemetry",
            ["DotNet"] = ".NET",
            ["AskJooby"] = "Ask Jooby",
            ["Jooby"] = "Jooby",
            ["Rag"] = "Rag",
            ["Postgres"] = "Postgres",
            ["Redis"] = "Redis",
            ["Patroni"] = "Patroni",
            ["Docker"] = "Docker",
            ["Sentinel"] = "Sentinel",
            ["Etcd"] = "etcd"
        };

        private static readonly HashSet<string> prepositions =
            new(StringComparer.Ordinal) { "From", "To", "In", "Of", "With", "For", "On", "At" };

        private static readonly string[] soundedConsonant = ["use", "uni", "uno", "eu", "one"];

        private static readonly Dictionary<string, string> joins = new(StringComparer.Ordinal)
        {
            ["X|Path"] = "XPath",
            ["HA|Proxy"] = "HAProxy",
            ["Open|Telemetry"] = "OpenTelemetry",
            ["Dot|Net"] = "DotNet",
            ["Ask|Jooby"] = "AskJooby"
        };

        private static string? Describe(string toolName, bool finished)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                return null;
            }

            var tokens = Tokenise(toolName);
            var verb = FindVerb(tokens);

            if (verb < 0)
            {
                return null;
            }

            var operation = operations[tokens[verb]];
            var subject = BuildSubject(tokens, verb);

            if (subject is null)
            {
                return null;
            }

            var lead = finished ? operation.Past : operation.Gerund;

            if (!operation.TakesArticle || Article(subject) is not { } article)
            {
                return $"{lead} {subject}";
            }

            return $"{lead} {article} {subject}";
        }

        private static List<string> Tokenise(string toolName)
        {
            var raw = new List<string>();

            foreach (Match match in tokenPattern.Matches(toolName))
            {
                raw.Add(match.Value);
            }

            var tokens = new List<string>(raw.Count);

            for (var i = 0; i < raw.Count; i++)
            {
                if (i + 1 < raw.Count && joins.TryGetValue($"{raw[i]}|{raw[i + 1]}", out var joined))
                {
                    tokens.Add(joined);
                    i++;
                    continue;
                }

                tokens.Add(raw[i]);
            }

            return tokens;
        }

        private static int FindVerb(List<string> tokens)
        {
            var qualifier = tokens.IndexOf("By");
            var limit = qualifier < 0 ? tokens.Count : qualifier;

            var found = LastVerbBefore(tokens, limit);

            return found < 0 ? LastVerbBefore(tokens, tokens.Count) : found;
        }

        private static int LastVerbBefore(List<string> tokens, int limit)
        {
            for (var i = limit - 1; i >= 0; i--)
            {
                if (!operations.ContainsKey(tokens[i]))
                {
                    continue;
                }

                if (i > 0 && prepositions.Contains(tokens[i - 1]))
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        private static string? BuildSubject(List<string> tokens, int verb)
        {
            var words = new List<string>(tokens.Count);

            for (var i = 0; i < tokens.Count; i++)
            {
                if (i == verb)
                {
                    continue;
                }

                var word = acronyms.TryGetValue(tokens[i], out var acronym) ? acronym : tokens[i].ToLowerInvariant();

                if (words.Count > 0 && string.Equals(words[^1], word, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                words.Add(word);
            }

            return words.Count == 0 ? null : string.Join(" ", words);
        }

        private static string? Article(string subject)
        {
            var words = subject.Split(' ');

            if (IsPlural(words[^1]))
            {
                return null;
            }

            var first = words[0];

            if (first.Length > 1 && first == first.ToUpperInvariant() && char.IsLetter(first[0]))
            {
                return AnLetters.Contains(first[0]) ? "an" : "a";
            }

            var lower = first.ToLowerInvariant();

            foreach (var exception in soundedConsonant)
            {
                if (lower.StartsWith(exception, StringComparison.Ordinal))
                {
                    return "a";
                }
            }

            return "aeiou".Contains(char.ToLowerInvariant(first[0])) ? "an" : "a";
        }

        private static bool IsPlural(string word)
        {
            var lower = word.ToLowerInvariant();

            if (!lower.EndsWith("s", StringComparison.Ordinal))
            {
                return false;
            }

            return !lower.EndsWith("ss", StringComparison.Ordinal)
                   && !lower.EndsWith("us", StringComparison.Ordinal)
                   && !lower.EndsWith("is", StringComparison.Ordinal);
        }
    }
}