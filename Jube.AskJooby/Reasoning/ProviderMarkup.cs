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

namespace Jube.AskJooby.Reasoning
{
    public static class ProviderMarkup
    {
        private const string Anchor = "<\uFF5C";

        private static readonly Regex invokeName = new(
            "invoke\\s+name\\s*=\\s*\"(?<name>[A-Za-z_][A-Za-z0-9_]*)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex separatedName = new(
            "\uFF5Ctool\u2581sep\uFF5C>\\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> danglingTrailingWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "for", "to", "of", "in", "on", "at", "by", "as", "with", "and", "or",
            "but", "nor", "is", "are", "was", "were", "that", "this", "which", "who", "whom",
            "whose", "when", "where", "while", "if", "because", "so", "from", "than", "then",
            "into", "onto", "about", "its", "their", "his", "her", "your", "my", "our", "not", "no"
        };

        private static readonly Regex trailingWord = new(
            "(?<word>[A-Za-z]+)[ \t]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool LooksTruncated(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var match = trailingWord.Match(value.TrimEnd());

            return match.Success && danglingTrailingWords.Contains(match.Groups["word"].Value);
        }

        public static bool Contains(string? value)
        {
            return value is not null && value.Contains(Anchor, StringComparison.Ordinal);
        }

        public static string Strip(string? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            var at = value.IndexOf(Anchor, StringComparison.Ordinal);

            return at < 0 ? value : value[..at].TrimEnd();
        }

        public static string? IntendedToolName(string? value)
        {
            if (value is null)
            {
                return null;
            }

            var match = invokeName.Match(value);

            if (match.Success)
            {
                return match.Groups["name"].Value;
            }

            match = separatedName.Match(value);

            return match.Success ? match.Groups["name"].Value : null;
        }

        public static bool ContainsFabricatedCall(string? value, IEnumerable<string>? toolNames)
        {
            return FindFabricated(value, toolNames).Index >= 0;
        }

        public static string? FabricatedToolName(string? value, IEnumerable<string>? toolNames)
        {
            return FindFabricated(value, toolNames).Name;
        }

        public static int FabricationIndex(string? value, IEnumerable<string>? toolNames)
        {
            if (value is null)
            {
                return -1;
            }

            var anchor = value.IndexOf(Anchor, StringComparison.Ordinal);
            var element = FindFabricated(value, toolNames).Index;

            if (anchor < 0)
            {
                return element;
            }

            return element < 0 ? anchor : Math.Min(anchor, element);
        }

        public static int SafePrefixLength(string value, IEnumerable<string>? toolNames)
        {
            if (toolNames is null)
            {
                return value.Length;
            }

            var names = toolNames as IReadOnlyCollection<string> ?? toolNames.ToList();

            for (var at = value.IndexOf('<'); at >= 0; at = value.IndexOf('<', at + 1))
            {
                foreach (var name in names)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    var open = "<" + name + ">";
                    var comparable = Math.Min(open.Length, value.Length - at);

                    if (string.CompareOrdinal(value, at, open, 0, comparable) == 0)
                    {
                        return at;
                    }
                }
            }

            return value.Length;
        }

        private static (int Index, string? Name) FindFabricated(string? value, IEnumerable<string>? toolNames)
        {
            if (value is null || toolNames is null)
            {
                return (-1, null);
            }

            var index = -1;
            string? name = null;

            var candidates = toolNames as IReadOnlyCollection<string> ?? toolNames.ToList();

            foreach (var candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                var open = "<" + candidate + ">";
                var at = value.IndexOf(open, StringComparison.Ordinal);

                if (at < 0 || value.IndexOf("</" + candidate + ">", at + open.Length,
                        StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                if (index < 0 || at < index)
                {
                    index = at;
                    name = candidate;
                }
            }

            return (index, name);
        }
    }
}