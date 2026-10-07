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
    using System.Collections.Generic;
    using System.Reflection;

    public record VersionFieldChange(string PropertyName, object FromValue, object ToValue);

    public static class VersionDiff
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        private static readonly IReadOnlySet<string> NoIgnoredFields = new HashSet<string>();

        public static IReadOnlyList<VersionFieldChange> Compare<TVersion>(TVersion from, TVersion to)
            where TVersion : class
        {
            return Compare(from, to, NoIgnoredFields);
        }

        public static IReadOnlyList<VersionFieldChange> Compare(object from, object to,
            IReadOnlySet<string> ignoredFields)
        {
            var changes = new List<VersionFieldChange>();

            if (from is null || to is null)
            {
                return changes;
            }

            foreach (var property in from.GetType().GetProperties(PublicInstance))
            {
                if (!property.CanRead || !IsSimple(property.PropertyType)
                                      || ignoredFields.Contains(property.Name))
                {
                    continue;
                }

                var target = to.GetType().GetProperty(property.Name, PublicInstance);

                if (target is null || !target.CanRead || !IsSimple(target.PropertyType))
                {
                    continue;
                }

                var fromValue = property.GetValue(from);
                var toValue = target.GetValue(to);

                if (!VersionText.Matches(fromValue, toValue))
                {
                    changes.Add(new VersionFieldChange(property.Name, fromValue, toValue));
                }
            }

            return changes;
        }

        private static bool IsSimple(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string)
                   || underlying == typeof(Guid) || underlying == typeof(DateTime)
                   || underlying == typeof(DateTimeOffset) || underlying == typeof(decimal)
                   || underlying == typeof(TimeSpan);
        }
    }
}