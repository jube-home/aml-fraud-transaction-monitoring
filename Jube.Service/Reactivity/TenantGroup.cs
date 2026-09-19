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

using System.Globalization;

namespace Jube.Service.Reactivity
{
    public static class TenantGroup
    {
        public const string Prefix = "Tenant_";

        private const string InvalidTenantMessage = "A tenant group needs a positive tenant registry id.";

        public static string Name(int tenantRegistryId)
        {
            if (tenantRegistryId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tenantRegistryId), tenantRegistryId,
                    InvalidTenantMessage);
            }

            return Prefix + tenantRegistryId.ToString(CultureInfo.InvariantCulture);
        }

        public static string? TryName(int? tenantRegistryId)
        {
            return tenantRegistryId is > 0 ? Name(tenantRegistryId.Value) : null;
        }

        public static string? TryName(string? tenantRegistryId)
        {
            return int.TryParse(tenantRegistryId, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? TryName(id)
                : null;
        }

        public static bool IsTenantGroup(string groupName)
        {
            return groupName.StartsWith(Prefix, StringComparison.Ordinal);
        }
    }
}