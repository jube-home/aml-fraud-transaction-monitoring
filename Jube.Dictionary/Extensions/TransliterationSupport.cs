/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using System.Text;

namespace Jube.Dictionary.Extensions
{
    internal static class TransliterationSupport
    {
        private static readonly Dictionary<char, string> transliterations = new()
        {
            ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "yo",
            ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m",
            ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
            ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "shch", ['ъ'] = "",
            ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya", ['і'] = "i", ['ї'] = "yi",
            ['є'] = "ye", ['ґ'] = "g",
            ['α'] = "a", ['β'] = "b", ['γ'] = "g", ['δ'] = "d", ['ε'] = "e", ['ζ'] = "z", ['η'] = "i",
            ['θ'] = "th", ['ι'] = "i", ['κ'] = "k", ['λ'] = "l", ['μ'] = "m", ['ν'] = "n", ['ξ'] = "x",
            ['ο'] = "o", ['π'] = "p", ['ρ'] = "r", ['σ'] = "s", ['ς'] = "s", ['τ'] = "t", ['υ'] = "y",
            ['φ'] = "f", ['χ'] = "ch", ['ψ'] = "ps", ['ω'] = "o", ['ά'] = "a", ['έ'] = "e", ['ή'] = "i",
            ['ί'] = "i", ['ϊ'] = "i", ['ΐ'] = "i", ['ό'] = "o", ['ύ'] = "y", ['ϋ'] = "y", ['ΰ'] = "y", ['ώ'] = "o"
        };

        private static readonly Dictionary<char, char> lookalikes = new()
        {
            ['а'] = 'a', ['е'] = 'e', ['о'] = 'o', ['р'] = 'p', ['с'] = 'c', ['у'] = 'y', ['х'] = 'x',
            ['і'] = 'i', ['ј'] = 'j', ['ѕ'] = 's', ['ο'] = 'o', ['α'] = 'a', ['ρ'] = 'p', ['ν'] = 'v',
            ['ι'] = 'i', ['κ'] = 'k'
        };

        private static readonly Dictionary<char, char> leetspeak = new()
        {
            ['0'] = 'o', ['1'] = 'i', ['3'] = 'e', ['4'] = 'a', ['5'] = 's', ['7'] = 't', ['@'] = 'a',
            ['$'] = 's', ['!'] = 'i'
        };

        internal static string Transliterate(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                if (transliterations.TryGetValue(char.ToLowerInvariant(character), out var latin))
                {
                    builder.Append(latin);
                }
                else
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        internal static string Fold(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                var lower = char.ToLowerInvariant(character);

                if (leetspeak.TryGetValue(lower, out var digit))
                {
                    builder.Append(digit);
                }
                else if (lookalikes.TryGetValue(lower, out var letter))
                {
                    builder.Append(letter);
                }
                else if (!IsSeparator(lower))
                {
                    builder.Append(lower);
                }
            }

            return builder.ToString();
        }

        private static bool IsSeparator(char character)
        {
            return char.IsWhiteSpace(character) ||
                   char.IsPunctuation(character) ||
                   CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Format;
        }
    }
}