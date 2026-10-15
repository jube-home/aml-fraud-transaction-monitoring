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

using System.Text;

namespace Jube.Charting.Svg
{
    internal static class XmlText
    {
        public static string Clean(string? value, int maxLength = ChartLimits.MaxTextCharacters)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(Math.Min(value.Length, maxLength));
            var i = 0;

            while (i < value.Length && builder.Length < maxLength)
            {
                var c = value[i];

                if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    builder.Append(c).Append(value[i + 1]);
                    i += 2;
                    continue;
                }

                i++;

                if (char.IsSurrogate(c))
                {
                    builder.Append('�');
                }
                else if (c is '\t' or '\n' or '\r')
                {
                    builder.Append(' ');
                }
                else if (!(c < 0x20 || c is >= '\u007F' and <= '\u009F' || c is '￾' or '￿' || IsBidiControl(c)))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public static string Escape(string? value, int maxLength = ChartLimits.MaxTextCharacters)
        {
            var clean = Clean(value, maxLength);

            if (clean.Length == 0)
            {
                return clean;
            }

            var builder = new StringBuilder(clean.Length + 8);

            foreach (var c in clean)
            {
                switch (c)
                {
                    case '&':
                        builder.Append("&amp;");
                        break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '"':
                        builder.Append("&quot;");
                        break;
                    case '\'':
                        builder.Append("&apos;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private static bool IsBidiControl(char c)
        {
            return c is >= '‪' and <= '‮' or >= '⁦' and <= '⁩' or '‎' or '‏' or '؜';
        }
    }
}