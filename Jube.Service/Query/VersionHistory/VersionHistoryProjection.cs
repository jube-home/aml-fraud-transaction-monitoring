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

namespace Jube.Service.Query.VersionHistory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Data.Query;

    public static class VersionHistoryProjection
    {
        private const string VersionSuffix = "Version";
        private const string IdSuffix = "Id";

        private static readonly string[] metadataFields =
        [
            "Id", "Version", "CreatedDate", "CreatedUser", "Deleted", "DeletedDate", "DeletedUser"
        ];

        public static IReadOnlySet<string> ExposedFields(Type dtoType, Type versionType)
        {
            ArgumentNullException.ThrowIfNull(dtoType);
            ArgumentNullException.ThrowIfNull(versionType);

            var parentField = versionType.Name.EndsWith(VersionSuffix, StringComparison.Ordinal)
                ? versionType.Name[..^VersionSuffix.Length] + IdSuffix
                : null;

            var candidates = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .Concat(metadataFields);

            if (parentField is not null)
            {
                candidates = candidates.Append(parentField);
            }

            return candidates
                .Where(name => versionType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is
                    { } property && IsSimple(property.PropertyType))
                .ToHashSet(StringComparer.Ordinal);
        }

        public static Dictionary<string, object?> Project<TVersion>(TVersion row, IReadOnlySet<string> fields)
            where TVersion : class
        {
            ArgumentNullException.ThrowIfNull(row);
            ArgumentNullException.ThrowIfNull(fields);

            var projected = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var property in typeof(TVersion).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && fields.Contains(property.Name) && IsSimple(property.PropertyType))
                {
                    projected[property.Name] = property.GetValue(row);
                }
            }

            return projected;
        }

        public static IReadOnlyList<VersionFieldChange> Changes(IReadOnlyList<VersionFieldChange> changes,
            IReadOnlySet<string> fields)
        {
            ArgumentNullException.ThrowIfNull(changes);
            ArgumentNullException.ThrowIfNull(fields);

            return changes.Where(c => fields.Contains(c.PropertyName))
                .Select(c => new VersionFieldChange(c.PropertyName, VersionText.Shorten(c.FromValue),
                    VersionText.Shorten(c.ToValue)))
                .ToList();
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