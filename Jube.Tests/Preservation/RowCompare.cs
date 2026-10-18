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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using LinqToDB.Mapping;

namespace Jube.Test.Preservation
{
    internal static class RowCompare
    {
        public static IReadOnlyList<PropertyInfo> ColumnProperties(Type type)
        {
            return
            [
                .. type.GetProperties()
                    .Where(w => w.GetCustomAttribute<ColumnAttribute>() != null)
            ];
        }

        public static void ShouldMatch<T>(this IReadOnlyList<T> source, IReadOnlyList<T> target,
            Func<PropertyInfo, bool> ignore, string what)
        {
            target.Count.Should().Be(source.Count, $"{what} row count");

            var properties = ColumnProperties(typeof(T)).Where(w => !ignore(w)).ToList();
            properties.Should().NotBeEmpty();

            for (var i = 0; i < source.Count; i++)
            {
                foreach (var property in properties)
                {
                    var expected = Normalise(property.GetValue(source[i]));
                    var actual = Normalise(property.GetValue(target[i]));
                    actual.Should().Be(expected, $"{what}[{i}].{property.Name}");
                }
            }
        }

        public static Func<PropertyInfo, bool> Ignoring(params string[] names)
        {
            return p => p.Name == "Id" || names.Contains(p.Name);
        }

        private static object? Normalise(object? value)
        {
            return value switch
            {
                DateTime dateTime => dateTime.Ticks,
                byte[] bytes => Convert.ToBase64String(bytes),
                _ => value
            };
        }
    }
}