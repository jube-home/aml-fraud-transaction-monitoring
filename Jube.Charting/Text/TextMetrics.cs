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
using Jube.Charting.Svg;

namespace Jube.Charting.Text
{
    public static class TextMetrics
    {
        public const string Ellipsis = "…";

        public static double Width(string? text, double fontSize, bool bold = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var em = 0.0;
            var enumerator = StringInfo.GetTextElementEnumerator(XmlText.Clean(text));

            while (enumerator.MoveNext())
            {
                em += ElementWidth((string)enumerator.Current);
            }

            return em * fontSize * (bold ? 1.06 : 1);
        }

        public static string Truncate(string? text, double maxWidth, double fontSize, bool bold = false)
        {
            var clean = XmlText.Clean(text);

            if (Width(clean, fontSize, bold) <= maxWidth)
            {
                return clean;
            }

            var ellipsisWidth = Width(Ellipsis, fontSize, bold);

            if (maxWidth <= ellipsisWidth)
            {
                return maxWidth >= ellipsisWidth * 0.9 ? Ellipsis : string.Empty;
            }

            var builder = new StringBuilder();
            var used = ellipsisWidth;
            var enumerator = StringInfo.GetTextElementEnumerator(clean);

            while (enumerator.MoveNext())
            {
                var element = (string)enumerator.Current;
                var w = ElementWidth(element) * fontSize * (bold ? 1.06 : 1);

                if (used + w > maxWidth)
                {
                    break;
                }

                builder.Append(element);
                used += w;
            }

            return builder.ToString().TrimEnd() + Ellipsis;
        }

        public static double MaxWidth(IEnumerable<string> texts, double fontSize, bool bold = false)
        {
            var max = 0.0;

            foreach (var t in texts)
            {
                max = Math.Max(max, Width(t, fontSize, bold));
            }

            return max;
        }

        private static double ElementWidth(string element)
        {
            var rune = element.EnumerateRunes().First();
            var value = rune.Value;

            if (value > 0xFFFF)
            {
                return 1.1;
            }

            var c = (char)value;
            var category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)
            {
                return 0;
            }

            if (value is >= 0x1100 and <= 0x115F or >= 0x2E80 and <= 0xA4CF or >= 0xAC00 and <= 0xD7A3
                or >= 0xF900 and <= 0xFAFF or >= 0xFE30 and <= 0xFE6F or >= 0xFF00 and <= 0xFF60)
            {
                return 1.0;
            }

            if (value is >= 0x0590 and <= 0x08FF)
            {
                return 0.6;
            }

            return c switch
            {
                ' ' => 0.28,
                'i' or 'j' or 'l' or 'I' or '!' or '|' or '.' or ',' or ':' or ';' or '\'' or '`' => 0.26,
                't' or 'f' or 'r' or '(' or ')' or '[' or ']' or '-' or '/' or '\\' => 0.34,
                'm' or 'w' => 0.83,
                'M' or 'W' or '@' or '%' => 0.9,
                >= '0' and <= '9' => 0.56,
                >= 'A' and <= 'Z' => 0.68,
                >= 'a' and <= 'z' => 0.54,
                _ => 0.6
            };
        }
    }
}