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

namespace Jube.Dictionary.Fuzzy
{
    public static class FuzzyNormaliser
    {
        private static readonly HashSet<string> Titles = new(StringComparer.OrdinalIgnoreCase)
        {
            "mr", "mrs", "ms", "miss", "mx", "dr", "prof", "sir", "dame", "lord", "lady", "rev", "hon", "sr", "sra",
            "herr", "frau", "monsieur", "madame", "mme", "mlle"
        };

        private static readonly HashSet<string> CompanySuffixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "ltd", "limited", "llc", "inc", "incorporated", "plc", "corp", "corporation", "gmbh", "ag", "sa", "sarl",
            "bv", "nv", "oy", "ab", "srl", "spa", "co", "company", "llp", "lp", "pte", "pty", "sas", "kg", "ug"
        };

        public static string Normalise(string? value, FuzzyNormalisation options)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            var text = value;
            if (options.RemoveDiacritics)
            {
                text = FoldDiacritics(text);
            }

            if (options.IgnoreCase)
            {
                text = text.ToLowerInvariant();
            }

            if (options.StripPunctuation)
            {
                text = StripPunctuation(text);
            }

            var tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (options.RemoveTitles)
            {
                while (tokens.Count > 1 && Titles.Contains(tokens[0]))
                {
                    tokens.RemoveAt(0);
                }
            }

            if (options.RemoveCompanySuffixes)
            {
                while (tokens.Count > 1 && CompanySuffixes.Contains(tokens[^1]))
                {
                    tokens.RemoveAt(tokens.Count - 1);
                }
            }

            return string.Join(options.RemoveSpaces ? "" : " ", tokens);
        }

        public static string[] Tokens(string normalised)
        {
            return normalised.Length == 0
                ? []
                : normalised.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        public static string FoldDiacritics(string text)
        {
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                switch (c)
                {
                    case 'ß':
                        builder.Append("ss");
                        break;
                    case 'æ':
                        builder.Append("ae");
                        break;
                    case 'Æ':
                        builder.Append("AE");
                        break;
                    case 'œ':
                        builder.Append("oe");
                        break;
                    case 'Œ':
                        builder.Append("OE");
                        break;
                    case 'ø':
                        builder.Append('o');
                        break;
                    case 'Ø':
                        builder.Append('O');
                        break;
                    case 'đ':
                        builder.Append('d');
                        break;
                    case 'Đ':
                        builder.Append('D');
                        break;
                    case 'ł':
                        builder.Append('l');
                        break;
                    case 'Ł':
                        builder.Append('L');
                        break;
                    case 'ı':
                        builder.Append('i');
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string StripPunctuation(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (c is '\'' or '’' or '‘' or '`' or '´')
                {
                    continue;
                }

                builder.Append(char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) ? c : ' ');
            }

            return builder.ToString();
        }
    }
}
