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

using System.Reflection;

namespace Jube.Preservation
{
    internal static class DateTimeKinds
    {
        public static void MarkUtc<T>(IEnumerable<T> rows)
        {
            foreach (var row in rows)
            {
                foreach (var property in Properties<T>.All)
                {
                    var value = property.GetValue(row);

                    switch (value)
                    {
                        case DateTime { Kind: not DateTimeKind.Utc } dateTime:
                            property.SetValue(row, DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
                            break;
                    }
                }
            }
        }

        private static class Properties<T>
        {
            public static readonly IReadOnlyList<PropertyInfo> All = typeof(T).GetProperties()
                .Where(w => (w.PropertyType == typeof(DateTime) || w.PropertyType == typeof(DateTime?))
                            && w is { CanRead: true, CanWrite: true })
                .ToList();
        }
    }
}