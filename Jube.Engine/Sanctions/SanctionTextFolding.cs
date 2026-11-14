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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Jube.Engine.Sanctions
{
    public static class SanctionTextFolding
    {
        private static readonly Dictionary<char, char> confusables = BuildConfusables();

        public static string Fold(string raw)
        {
            var builder = new StringBuilder(raw.Length);

            foreach (var c in raw.Normalize(NormalizationForm.FormKD))
            {
                if (char.IsWhiteSpace(c))
                {
                    builder.Append(' ');

                    continue;
                }

                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.Format
                    or UnicodeCategory.Control)
                {
                    continue;
                }

                builder.Append(c);
            }

            return FoldLookalikes(builder.ToString()).ToLowerInvariant();
        }

        public static string Spaced(string raw)
        {
            return string.Join(' ', Fold(raw)
                .Replace("-", " ")
                .Replace(",", " ")
                .Replace(".", " ")
                .Replace("'", "")
                .Split(' ', System.StringSplitOptions.RemoveEmptyEntries));
        }

        public static string[] Tokens(string raw)
        {
            return
            [
                .. Spaced(raw)
                    .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 1)
                    .Distinct()
            ];
        }

        public static bool IsMixedScript(string raw)
        {
            foreach (var run in LetterRuns(raw.Normalize(NormalizationForm.FormKD)))
            {
                var latin = false;
                var other = false;

                foreach (var c in run)
                {
                    if (IsLatin(c))
                    {
                        latin = true;
                    }
                    else if (IsCyrillicOrGreek(c))
                    {
                        other = true;
                    }
                }

                if (latin && other)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FoldLookalikes(string text)
        {
            var builder = new StringBuilder(text.Length);
            var run = new StringBuilder();

            void Flush()
            {
                if (run.Length == 0)
                {
                    return;
                }

                var word = run.ToString();
                builder.Append(FoldWord(word));
                run.Clear();
            }

            foreach (var c in text)
            {
                if (char.IsLetter(c))
                {
                    run.Append(c);
                }
                else
                {
                    Flush();
                    builder.Append(c);
                }
            }

            Flush();

            return builder.ToString();
        }

        private static string FoldWord(string word)
        {
            var replaceable = false;

            foreach (var c in word)
            {
                if (confusables.ContainsKey(c))
                {
                    replaceable = true;
                }
                else if (!IsLatin(c))
                {
                    return word;
                }
            }

            if (!replaceable)
            {
                return word;
            }

            var builder = new StringBuilder(word.Length);

            foreach (var c in word)
            {
                builder.Append(confusables.TryGetValue(c, out var latin) ? latin : c);
            }

            return builder.ToString();
        }

        private static IEnumerable<string> LetterRuns(string text)
        {
            var run = new StringBuilder();

            foreach (var c in text)
            {
                if (char.IsLetter(c))
                {
                    run.Append(c);

                    continue;
                }

                if (run.Length > 0)
                {
                    yield return run.ToString();
                    run.Clear();
                }
            }

            if (run.Length > 0)
            {
                yield return run.ToString();
            }
        }

        private static bool IsLatin(char c)
        {
            return c < 0x250 && char.IsLetter(c) || c is >= '\u1E00' and <= '\u1EFF';
        }

        private static bool IsCyrillicOrGreek(char c)
        {
            return c is >= '\u0370' and <= '\u03FF' or >= '\u0400' and <= '\u052F';
        }

        private static Dictionary<char, char> BuildConfusables()
        {
            const string pairs =
                "\u0430a\u0441c\u0435e\u043Eo\u0440p\u0445x\u0443y\u0456i\u0458j\u0455s\u04BBh\u0501d\u051Bq\u051Dw\u04CFl" +
                "\u0410A\u0412B\u0421C\u0415E\u041DH\u0406I\u0408J\u041AK\u041CM\u041EO\u0420P\u0405S\u0422T\u0425X\u0423Y" +
                "\u03B1a\u03BFo\u03BDv\u03C1p\u03B9i\u03BAk\u03C5u\u03C7x" +
                "\u0391A\u0392B\u0395E\u0396Z\u0397H\u0399I\u039AK\u039CM\u039DN\u039FO\u03A1P\u03A4T\u03A5Y\u03A7X" +
                "\u0131i\u0251a\u0261g";

            var map = new Dictionary<char, char>();

            for (var i = 0; i < pairs.Length; i += 2)
            {
                map[pairs[i]] = pairs[i + 1];
            }

            return map;
        }
    }
}
