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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Jube.Service.Security
{
    public static partial class HtmlSanitiser
    {
        private static readonly HashSet<string> allowedTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "div", "span", "b", "strong", "i", "em", "u", "s", "strike", "sub", "sup", "ul", "ol", "li",
            "a", "img", "table", "thead", "tbody", "tr", "td", "th", "blockquote", "pre", "code", "hr", "h1", "h2",
            "h3", "h4", "h5", "h6"
        };

        private static readonly HashSet<string> voidTags = new(StringComparer.OrdinalIgnoreCase) { "br", "hr", "img" };

        private static readonly HashSet<string> allowedStyles = new(StringComparer.OrdinalIgnoreCase)
        {
            "color", "background-color", "font-size", "font-weight", "font-style", "text-decoration", "text-align",
            "width", "height", "border", "padding", "margin", "font-family", "vertical-align", "border-collapse",
            "list-style-type"
        };

        private static readonly HashSet<string> numericAttributes = new(StringComparer.OrdinalIgnoreCase)
            { "width", "height", "colspan", "rowspan" };

        [GeneratedRegex(
            "^<(/?)([a-zA-Z][a-zA-Z0-9]*)((?:\\s+[a-zA-Z][a-zA-Z0-9-]*(?:\\s*=\\s*(?:\"[^\"]*\"|'[^']*'|[^\\s\"'=<>`]+))?)*)\\s*/?>",
            RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
        private static partial Regex TagPattern();

        private const int PlainTagWindow = 4096;

        [GeneratedRegex("([a-zA-Z][a-zA-Z0-9-]*)(?:\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s\"'=<>`]+)))?",
            RegexOptions.CultureInvariant)]
        private static partial Regex AttributePattern();

        [GeneratedRegex("^[A-Za-z0-9#\\s.,%()'-]+$", RegexOptions.CultureInvariant)]
        private static partial Regex StyleValuePattern();

        [GeneratedRegex(
            "^(data:image/(png|jpeg|jpg|gif|webp);base64,[A-Za-z0-9+/=]+|https://[^\\s\"'<>`]+|/(icons|images)/[A-Za-z0-9_-]+(?:/[A-Za-z0-9_-]+)*\\.(png|jpe?g|gif|svg|webp))$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex SafeSourcePattern();

        [GeneratedRegex("^(https?://[^\\s\"'<>`]+|mailto:[^\\s\"'<>`]+|/[^\\s\"'<>`/][^\\s\"'<>`]*)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex SafeLinkPattern();

        [return: NotNullIfNotNull(nameof(html))]
        public static string? Sanitise(string? html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            var builder = new StringBuilder(html.Length);
            var index = 0;
            var nextClose = -1;
            var longTagBudget = 4L * html.Length;
            while (index < html.Length)
            {
                var current = html[index];
                if (current == '<')
                {
                    if (nextClose < index)
                    {
                        nextClose = html.IndexOf('>', index);
                        if (nextClose < 0)
                        {
                            nextClose = int.MaxValue;
                        }
                    }

                    var end = nextClose == int.MaxValue ? -1 : nextClose;
                    var match = end < 0 ? Match.Empty : MatchTag(html, index, end - index + 1, ref longTagBudget);
                    if (match.Success && allowedTags.Contains(match.Groups[2].Value))
                    {
                        AppendTag(builder, match);
                        index = end + 1;
                        continue;
                    }

                    if (match.Success)
                    {
                        index = end + 1;
                        continue;
                    }

                    builder.Append("&lt;");
                    index++;
                    continue;
                }

                if (current == '>')
                {
                    builder.Append("&gt;");
                }
                else if (current == '\0' || (char.IsControl(current) && current is not ('\t' or '\r' or '\n')))
                {
                }
                else
                {
                    builder.Append(current);
                }

                index++;
            }

            return builder.ToString();
        }

        private static Match MatchTag(string html, int index, int length, ref long longTagBudget)
        {
            if (length > PlainTagWindow)
            {
                if (length > longTagBudget || !StartsImageTag(html, index))
                {
                    return Match.Empty;
                }

                longTagBudget -= length;
            }

            try
            {
                return TagPattern().Match(html, index, length);
            }
            catch (RegexMatchTimeoutException)
            {
                return Match.Empty;
            }
        }

        private static bool StartsImageTag(string html, int index)
        {
            return index + 4 < html.Length &&
                   string.Compare(html, index + 1, "img", 0, 3, StringComparison.OrdinalIgnoreCase) == 0 &&
                   (char.IsWhiteSpace(html[index + 4]) || html[index + 4] is '/' or '>');
        }

        private static void AppendTag(StringBuilder builder, Match match)
        {
            var closing = match.Groups[1].Value == "/";
            var name = match.Groups[2].Value.ToLowerInvariant();
            if (closing)
            {
                if (!voidTags.Contains(name))
                {
                    builder.Append("</").Append(name).Append('>');
                }

                return;
            }

            builder.Append('<').Append(name);
            foreach (Match attribute in AttributePattern().Matches(match.Groups[3].Value))
            {
                var attributeName = attribute.Groups[1].Value.ToLowerInvariant();
                var value = attribute.Groups[2].Success ? attribute.Groups[2].Value
                    : attribute.Groups[3].Success ? attribute.Groups[3].Value
                    : attribute.Groups[4].Value;
                value = System.Net.WebUtility.HtmlDecode(value).Trim();
                var safe = attributeName switch
                {
                    "style" => SanitiseStyle(value),
                    "href" when name == "a" && SafeLinkPattern().IsMatch(value) => value,
                    "src" when name == "img" && SafeSourcePattern().IsMatch(value) => value,
                    "alt" or "title" when name is "img" or "a" => value,
                    "colspan" or "rowspan" or "width" or "height" when numericAttributes.Contains(attributeName) &&
                                                                       value.Length <= 6 &&
                                                                       value.All(c => char.IsAsciiDigit(c) || c == '%')
                        => value,
                    "align" when value is "left" or "right" or "center" or "justify" => value,
                    _ => null
                };

                if (!string.IsNullOrEmpty(safe))
                {
                    builder.Append(' ').Append(attributeName).Append("=\"").Append(Encode(safe)).Append('"');
                }
            }

            if (name == "a")
            {
                builder.Append(" rel=\"noopener noreferrer\"");
            }

            builder.Append(voidTags.Contains(name) ? " />" : ">");
        }

        private static string? SanitiseStyle(string style)
        {
            var kept = new List<string>();
            foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var colon = declaration.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                var property = declaration[..colon].Trim();
                var value = declaration[(colon + 1)..].Trim();
                if (allowedStyles.Contains(property) && value.Length is > 0 and <= 120 &&
                    StyleValuePattern().IsMatch(value) &&
                    !value.Contains("url", StringComparison.OrdinalIgnoreCase) &&
                    !value.Contains("expression", StringComparison.OrdinalIgnoreCase))
                {
                    kept.Add($"{property.ToLowerInvariant()}:{value}");
                }
            }

            return kept.Count == 0 ? null : string.Join(";", kept);
        }

        private static string Encode(string value) =>
            value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}