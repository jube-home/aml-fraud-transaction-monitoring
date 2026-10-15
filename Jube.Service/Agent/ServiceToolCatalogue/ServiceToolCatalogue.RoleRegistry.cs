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
using Jube.Service.Repository.RoleRegistry;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddRoleRegistry(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "RoleRegistryList", OperationKind.Read, true, false,
                    "Lists Roles visible to the caller's tenant, keyset-paged and capped.",
                    RoleRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Role Registrys may use.",
                    RoleRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryFilter", OperationKind.Read, true, false,
                    "Returns the Role Registrys matching query builder JSON, keyset-paged and capped.",
                    RoleRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryCount", OperationKind.Read, true, false,
                    "Counts the Role Registrys matching query builder JSON, optionally grouped by a field.",
                    RoleRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryGet", OperationKind.Read, true, false,
                    "Returns a single Role by Id.",
                    RoleRegistryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Role by Id.",
                    RoleRegistryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryCreate", OperationKind.Write, false, false,
                    "Creates a Role; calling twice creates two.",
                    RoleRegistryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryValidate", OperationKind.Read, true, false,
                    "Validates a Role Registry without saving it and returns every failure.",
                    RoleRegistryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryUpdate", OperationKind.Write, true, false,
                    "Updates a Role; overwrites, writes a version-audit row.",
                    RoleRegistryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "RoleRegistryDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Role; reversible only by direct database action.",
                    RoleRegistryService.RequiredPermissions)
            ]);
        }
    }
}