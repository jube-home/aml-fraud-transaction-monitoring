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

namespace Jube.Data.Query
{
    using System;
    using System.Globalization;
    using System.Linq;

    public static class VersionText
    {
        private const int DisplayLimit = 200;

        private const string Ellipsis = "...";

        public static string Normalise(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var lines = text.TrimStart('﻿')
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0);

            return string.Join('\n', lines);
        }

        public static bool Matches(object from, object to)
        {
            if (from is string || to is string)
            {
                return string.Equals(Normalise(from as string), Normalise(to as string), StringComparison.Ordinal);
            }

            return Equals(from, to);
        }

        public static string ChangeKind(object before, object after)
        {
            if (Normalise(Text(before)).Length == 0)
            {
                return "Added";
            }

            return Normalise(Text(after)).Length == 0 ? "Removed" : "Changed";
        }

        private static string Text(object value)
        {
            return value switch
            {
                null => string.Empty,
                bool flag => flag ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        public static string Preview(object value)
        {
            return Truncate(Text(value));
        }

        public static object Shorten(object value)
        {
            return value is string text ? Truncate(text) : value;
        }

        private static string Truncate(string text)
        {
            return text.Length > DisplayLimit ? text[..DisplayLimit] + Ellipsis : text;
        }
    }
}