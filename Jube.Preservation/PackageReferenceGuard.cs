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

namespace Jube.Preservation
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Reflection;

    internal static class PackageReferenceGuard
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> propertiesByType = new();

        internal static HashSet<Guid> Collect(params object?[] roots)
        {
            var guids = new HashSet<Guid>();
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

            foreach (var root in roots)
            {
                Walk(root, guids, visited);
            }

            return guids;
        }

        internal static List<T>? Within<T>(IEnumerable<T>? rows, HashSet<Guid> known,
            params Func<T, Guid>[] references)
        {
            return rows?.Where(row => references.All(reference => known.Contains(reference(row)))).ToList();
        }

        private static void Walk(object? node, HashSet<Guid> guids, HashSet<object> visited)
        {
            if (node is null or string || node.GetType().IsValueType || node is byte[])
            {
                return;
            }

            if (node is IEnumerable sequence)
            {
                if (node is Array array && array.GetType().GetElementType() is { IsValueType: true })
                {
                    return;
                }

                foreach (var item in sequence)
                {
                    Walk(item, guids, visited);
                }

                return;
            }

            if (!visited.Add(node))
            {
                return;
            }

            foreach (var property in propertiesByType.GetOrAdd(node.GetType(), type => type
                         .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(w => w.CanRead && w.GetIndexParameters().Length == 0).ToArray()))
            {
                var value = property.GetValue(node);

                if (property.Name == "Guid" && value is Guid guid)
                {
                    guids.Add(guid);
                }
                else
                {
                    Walk(value, guids, visited);
                }
            }
        }
    }
}
