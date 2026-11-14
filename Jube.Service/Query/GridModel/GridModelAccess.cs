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

using System.Collections.Concurrent;
using System.Reflection;

namespace Jube.Service.Query.GridModel
{
    public static class GridModelAccess
    {
        private static readonly ConcurrentDictionary<string, (bool LandlordOnly, int[] Permissions)> cache =
            new(StringComparer.OrdinalIgnoreCase);

        public static (bool LandlordOnly, int[] Permissions) Resolve(string area)
        {
            return cache.GetOrAdd(area, Find);
        }

        public static bool IsKnown(string area)
        {
            return FindType(area) != null;
        }

        private static Type? FindType(string area)
        {
            return typeof(GridModelAccess).Assembly.GetTypes().FirstOrDefault(t =>
                string.Equals(t.Name, area + "Service", StringComparison.OrdinalIgnoreCase) &&
                t.GetProperty("RequiredPermissions", BindingFlags.Public | BindingFlags.Static) != null);
        }

        private static (bool LandlordOnly, int[] Permissions) Find(string area)
        {
            var type = FindType(area);

            if (type == null)
            {
                return (true, []);
            }

            var permissions = type.GetProperty("RequiredPermissions", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null) as IEnumerable<int> ?? [];

            var landlordOnly = type.GetProperty("RequiresLandlord", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as bool? ?? false;

            return (landlordOnly || !permissions.Any(), permissions.ToArray());
        }
    }
}
