/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Text;
using Jube.AskJooby.DataCache.Models;

namespace Jube.AskJooby.DataCache
{
    public static class CachedRowText
    {
        public static string Format(IReadOnlyList<CachedColumn> columns,
            IReadOnlyDictionary<string, object?> row, Func<object?, string>? describe = null)
        {
            var builder = new StringBuilder();

            foreach (var column in columns)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                var value = row.GetValueOrDefault(column.Name);

                builder.Append(column.Name).Append('=')
                    .Append(describe is null ? value?.ToString() ?? "null" : describe(value));
            }

            return builder.ToString();
        }

        public static string FormatFirst(IReadOnlyList<CachedColumn> columns,
            IReadOnlyDictionary<string, object?> row, int count, Func<object?, string>? describe = null)
        {
            var taken = count >= columns.Count ? columns : Slice(columns, count);

            return Format(taken, row, describe);
        }

        private static List<CachedColumn> Slice(IReadOnlyList<CachedColumn> columns, int count)
        {
            var slice = new List<CachedColumn>(Math.Max(count, 0));

            for (var at = 0; at < count && at < columns.Count; at++)
            {
                slice.Add(columns[at]);
            }

            return slice;
        }
    }
}