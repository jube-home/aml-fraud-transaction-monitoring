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
using System.Text.RegularExpressions;

namespace Jube.AskJooby.Validation
{
    public static class ValidationDocs
    {
        private static readonly Regex header = new(
            @"^\[[^\]\r\n]+\]\s+\S.*\(.*\)\s*$",
            RegexOptions.Multiline | RegexOptions.Compiled);

        public static string Compact(IEnumerable<string> storedResults, int budgetCharacters = 20000)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var kept = new List<string>();
            var named = new List<string>();
            var used = 0;

            foreach (var bite in storedResults.SelectMany(Split))
            {
                var title = FirstLine(bite);

                if (!seen.Add(title))
                {
                    continue;
                }

                if (budgetCharacters > 0 && used + bite.Length > budgetCharacters && kept.Count > 0)
                {
                    named.Add(title);
                    continue;
                }

                kept.Add(bite);
                used += bite.Length;
            }

            if (kept.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append(string.Join("\n\n", kept));

            if (named.Count > 0)
            {
                builder.Append("\n\nAlso retrieved in this conversation, not quoted here to keep this "
                               + "readable -- search the Pasture for any you need: ")
                    .Append(string.Join("; ", named));
            }

            return builder.ToString();
        }

        private static IEnumerable<string> Split(string storedResult)
        {
            if (string.IsNullOrWhiteSpace(storedResult))
            {
                yield break;
            }

            var matches = header.Matches(storedResult);

            if (matches.Count == 0)
            {
                yield return storedResult.Trim();
                yield break;
            }

            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : storedResult.Length;

                var bite = storedResult[start..end]
                    .Replace("[CRITICAL CONTEXT END]", string.Empty, StringComparison.Ordinal)
                    .Trim();

                if (bite.Length > 0)
                {
                    yield return bite;
                }
            }
        }

        private static string FirstLine(string bite)
        {
            var stop = bite.IndexOf('\n');

            return (stop < 0 ? bite : bite[..stop]).Trim();
        }
    }
}