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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddRoleRegistryPermission(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "RoleRegistryPermissionList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists Role Registry Permission grants visible to the caller's tenant, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "RoleRegistryPermissionGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single Role Registry Permission grant by Id."),
            new ServiceToolDescriptor(
                "RoleRegistryPermissionCreate", OperationKind.Write, Idempotent: false, Destructive: false,
                "Grants a Permission Specification to a Role Registry; calling twice creates two grants."),
            new ServiceToolDescriptor(
                "RoleRegistryPermissionUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a Role Registry Permission grant; writes a version-audit row."),
            new ServiceToolDescriptor(
                "RoleRegistryPermissionDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Soft-deletes a Role Registry Permission grant; reversible only by direct database action.")
        ]);
    }
}