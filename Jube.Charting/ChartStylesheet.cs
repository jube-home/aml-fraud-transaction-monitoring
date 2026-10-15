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

using System.Security.Cryptography;
using System.Text;
using Jube.Charting.Theme;

namespace Jube.Charting
{
    public static class ChartStylesheet
    {
        public const string FileName = "jube-charting.css";
        public static string Version => versionValue.Value;

        public static string Css(bool minified = false)
        {
            return minified ? compact.Value : readable.Value;
        }

        private static readonly Lazy<string> compact = new(BuildCompact);
        private static readonly Lazy<string> readable = new(() => Format(compact.Value));

        private static readonly Lazy<string> versionValue = new(() =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(compact.Value)), 0, 4).ToLowerInvariant());

        private static string BuildCompact()
        {
            const string k = ".jc";
            var css = new StringBuilder();
            css.Append(ChartCss.ExternalRoot());
            css.Append(ChartCss.Body(k));
            css.Append(ChartCss.ExternalSeries(k));
            css.Append(ChartCss.ReducedMotion(k));
            css.Append(ChartCss.SequentialClasses(k));
            css.Append(ChartCss.ExternalVariants(k));

            foreach (var family in StyleFamilies.All)
            {
                var scope = StyleFamilies.Scope(family);
                css.Append(StyleFamilies.Rules(family, scope));
                css.Append(StyleFamilies.Relations(family, scope));
            }

            return css.ToString();
        }

        internal static string Format(string cssText)
        {
            var b = new StringBuilder(cssText.Length + cssText.Length / 8);
            var depth = 0;
            var paren = 0;

            foreach (var c in cssText)
            {
                switch (c)
                {
                    case '(':
                        paren++;
                        b.Append(c);
                        break;
                    case ')':
                        paren--;
                        b.Append(c);
                        break;
                    case '{' when paren == 0:
                        b.Append(" {");
                        depth++;

                        if (depth == 1 && IsAtRule(b))
                        {
                            b.Append('\n');
                        }

                        break;
                    case '}' when paren == 0:
                        depth--;
                        b.Append("}\n");
                        break;
                    default:
                        b.Append(c);
                        break;
                }
            }

            return b.ToString();
        }

        private static bool IsAtRule(StringBuilder b)
        {
            var text = b.ToString();
            var start = text.LastIndexOf('\n') + 1;

            return start < text.Length && text[start] == '@';
        }
    }
}