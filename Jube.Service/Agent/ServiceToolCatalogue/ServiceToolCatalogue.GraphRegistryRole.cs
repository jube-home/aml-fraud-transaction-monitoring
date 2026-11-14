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
using Jube.Service.Repository.GraphRegistryRole;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddGraphRegistryRole(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "GraphRegistryRoleListByGraphRegistryGuid", OperationKind.Read, true, false,
                    "Lists the Role grants for a Graph Registry.",
                    GraphRegistryRoleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryRoleCreate", OperationKind.Write, false, false,
                    "Grants a Role access to a Graph Registry; calling twice creates two grants.",
                    GraphRegistryRoleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryRoleDelete", OperationKind.Delete, false, true,
                    "Removes a Role's grant on a Graph Registry; reversible only by direct database action. A repeat call on an already-removed grant throws rather than succeeding as a no-op.",
                    GraphRegistryRoleService.RequiredPermissions)
            ]);
        }
    }
}
