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
using Jube.Service.Repository.GraphRegistry;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddGraphRegistry(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "GraphRegistryList", OperationKind.Read, true, false,
                    "Lists Graph Registries visible to the caller's tenant, keyset-paged and capped.",
                    GraphRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Graph Registries may use.",
                    GraphRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryFilter", OperationKind.Read, true, false,
                    "Returns the Graph Registries matching query builder JSON, keyset-paged and capped.",
                    GraphRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryCount", OperationKind.Read, true, false,
                    "Counts the Graph Registries matching query builder JSON, optionally grouped by a field.",
                    GraphRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryGet", OperationKind.Read, true, false,
                    "Returns a single Graph Registry by Id.",
                    GraphRegistryService.ReadPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Graph Registry by Guid.",
                    GraphRegistryService.ReadPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryGetByGuidActiveOnly", OperationKind.Read, true, false,
                    "Returns a single active Graph Registry by Guid provided the caller holds a role grant on it.",
                    GraphRegistryService.ReadPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryListByShowInDirectory", OperationKind.Read, true, false,
                    "Lists active Graph Registries shown in the Directory that the caller holds a role grant on.",
                    GraphRegistryService.DirectoryPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryCreate", OperationKind.Write, false, false,
                    "Creates a Graph Registry; calling twice creates two.",
                    GraphRegistryService.WritePermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryValidate", OperationKind.Read, true, false,
                    "Validates a Graph Registry without saving it and returns every failure.",
                    GraphRegistryService.WritePermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryUpdate", OperationKind.Write, true, false,
                    "Updates a Graph Registry; overwrites, writes a version-audit row.",
                    GraphRegistryService.WritePermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Graph Registry; reversible only by direct database action.",
                    GraphRegistryService.WritePermissions)
            ]);
        }
    }
}
