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

using Jube.Service.Agent.Models;
using Jube.Service.Repository.RoleRegistryPermission;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddRoleRegistryPermission(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionList", OperationKind.Read, true, false,
                    "Lists Role Registry Permission grants visible to the caller's tenant, keyset-paged and capped.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Role Registry Permissions may use.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionFilter", OperationKind.Read, true, false,
                    "Returns the Role Registry Permissions matching query builder JSON, keyset-paged and capped.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionCount", OperationKind.Read, true, false,
                    "Counts the Role Registry Permissions matching query builder JSON, optionally grouped by a field.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionGet", OperationKind.Read, true, false,
                    "Returns a single Role Registry Permission grant by Id.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Role Registry Permission grant by Id.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionCreate", OperationKind.Write, false, false,
                    "Grants a Permission Specification to a Role Registry; calling twice creates two grants.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionValidate", OperationKind.Read, true, false,
                    "Validates a Role Registry Permission without saving it and returns every failure.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionUpdate", OperationKind.Write, true, false,
                    "Updates a Role Registry Permission grant; writes a version-audit row.",
                    RoleRegistryPermissionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryPermissionDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Role Registry Permission grant; reversible only by direct database action.",
                    RoleRegistryPermissionService.RequiredPermissions)
            ]);
        }
    }
}