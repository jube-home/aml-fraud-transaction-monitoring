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

namespace Jube.Case
{
    public static class WebhookTarget
    {
        public const int MaximumLength = 2048;

        public const string Empty = "empty";
        public const string NotAbsolute = "notAbsolute";
        public const string SchemeNotAllowed = "schemeNotAllowed";
        public const string AuthorityNotLiteral = "authorityNotLiteral";
        public const string HostChanged = "hostChanged";
        public const string TooLong = "tooLong";

        public static (string? Url, string? Refusal) Resolve(string? template, Func<string, string> render)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return (null, Empty);
            }

            var trimmed = template.Trim();
            var first = trimmed.IndexOf("[@", StringComparison.Ordinal);
            var literal = first < 0 ? trimmed : trimmed[..first];

            if (!literal.Contains("://", StringComparison.Ordinal))
            {
                return (null, first < 0 ? NotAbsolute : AuthorityNotLiteral);
            }

            if (first >= 0 && !AuthorityIsComplete(literal))
            {
                return (null, AuthorityNotLiteral);
            }

            if (!Uri.TryCreate(AuthorityOf(literal), UriKind.Absolute, out var expected))
            {
                return (null, NotAbsolute);
            }

            var rendered = render(trimmed);

            if (rendered.Length > MaximumLength)
            {
                return (null, TooLong);
            }

            if (!Uri.TryCreate(rendered, UriKind.Absolute, out var uri))
            {
                return (null, NotAbsolute);
            }

            if (uri.Scheme is not ("http" or "https"))
            {
                return (null, SchemeNotAllowed);
            }

            if (!string.Equals(uri.Host, expected.Host, StringComparison.OrdinalIgnoreCase) ||
                uri.Port != expected.Port || uri.Scheme != expected.Scheme || !string.IsNullOrEmpty(uri.UserInfo))
            {
                return (null, HostChanged);
            }

            return (uri.AbsoluteUri, null);
        }

        private static bool AuthorityIsComplete(string literal)
        {
            var scheme = literal.IndexOf("://", StringComparison.Ordinal);

            if (scheme < 0)
            {
                return false;
            }

            var rest = literal[(scheme + 3)..];

            return rest.Length > 0 && rest.IndexOfAny(['/', '?', '#']) > 0;
        }

        private static string AuthorityOf(string literal)
        {
            var scheme = literal.IndexOf("://", StringComparison.Ordinal);

            if (scheme < 0)
            {
                return literal;
            }

            var rest = literal[(scheme + 3)..];
            var end = rest.IndexOfAny(['/', '?', '#']);

            return literal[..(scheme + 3)] + (end < 0 ? rest : rest[..end]);
        }
    }
}
