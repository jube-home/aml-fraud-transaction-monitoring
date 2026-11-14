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

using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jube.Validations
{
    public static class FilterSqlText
    {
        private const int MaxLength = 65536;

        private static readonly Regex forbidden = new(
            @"\b(select|union|intersect|except|from|into|copy|insert|update|delete|drop|alter|create|truncate|grant|revoke|call|do|execute|pg_[a-z_]+|lo_[a-z_]+|set|with|values|returning|window|offset|limit|order|group|having|for)\b|::\s*regclass|\bcurrent_setting\b|\bversion\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        public static bool IsAcceptable(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return true;
            }

            if (sql.Length > MaxLength)
            {
                return false;
            }

            var outside = new StringBuilder(sql.Length);
            var depth = 0;
            var quote = '\0';

            foreach (var c in sql)
            {
                if (quote != '\0')
                {
                    if (c == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                switch (c)
                {
                    case '\'' or '"':
                        if (c == '\'' && StartsEscapeString(outside))
                        {
                            return false;
                        }

                        quote = c;
                        outside.Append(' ');
                        continue;
                    case '\0' or '\\' or '$':
                        return false;
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;

                        if (depth < 0)
                        {
                            return false;
                        }

                        break;
                    case ';':
                        return false;
                }

                if (char.IsControl(c) && c is not ('\t' or '\r' or '\n'))
                {
                    return false;
                }

                outside.Append(c);
            }

            if (quote != '\0' || depth != 0)
            {
                return false;
            }

            var text = outside.ToString();

            if (text.Contains("--") || text.Contains("/*") || text.Contains("*/"))
            {
                return false;
            }

            try
            {
                return !forbidden.IsMatch(text);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static bool StartsEscapeString(StringBuilder outside)
        {
            if (outside.Length == 0 || outside[^1] is not ('e' or 'E'))
            {
                return false;
            }

            if (outside.Length == 1)
            {
                return true;
            }

            var before = outside[^2];

            return !(char.IsLetterOrDigit(before) || before is '_' or '$');
        }

        public static bool IsAcceptableTokens(string? tokens)
        {
            if (string.IsNullOrWhiteSpace(tokens))
            {
                return true;
            }

            if (!JsonText.IsAcceptable(tokens))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(tokens);

                return document.RootElement.ValueKind == JsonValueKind.Array;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}