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
using Jube.Service.Repository.GraphRegistryEdge;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddGraphRegistryEdge(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeList", OperationKind.Read, true, false,
                    "Lists Graph Registry Edges visible to the caller's tenant, keyset-paged and capped.",
                    GraphRegistryEdgeService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Graph Registry Edges may use.",
                    GraphRegistryEdgeService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeFilter", OperationKind.Read, true, false,
                    "Returns the Graph Registry Edges matching query builder JSON, keyset-paged and capped.",
                    GraphRegistryEdgeService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeCount", OperationKind.Read, true, false,
                    "Counts the Graph Registry Edges matching query builder JSON, optionally grouped by a field.",
                    GraphRegistryEdgeService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeListByGraphRegistryId", OperationKind.Read, true, false,
                    "Lists the edge definitions of one Graph Registry.",
                    GraphRegistryEdgeService.ListPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeGet", OperationKind.Read, true, false,
                    "Returns a single Graph Registry Edge by Id.",
                    GraphRegistryEdgeService.ReadPermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeKeys", OperationKind.Read, true, false,
                    "Lists the report keys an edge definition may name, with the models each is flagged Archive Key on.",
                    GraphRegistryEdgeService.WritePermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeCreate", OperationKind.Write, false, false,
                    "Creates a Graph Registry Edge; calling twice creates two.",
                    GraphRegistryEdgeService.WritePermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeValidate", OperationKind.Read, true, false,
                    "Validates a Graph Registry Edge without saving it and returns every failure.",
                    GraphRegistryEdgeService.WritePermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeUpdate", OperationKind.Write, true, false,
                    "Updates a Graph Registry Edge; overwrites, writes a version-audit row.",
                    GraphRegistryEdgeService.WritePermissions),
                new ServiceToolDescriptor(
                    "GraphRegistryEdgeDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Graph Registry Edge; reversible only by direct database action.",
                    GraphRegistryEdgeService.WritePermissions)
            ]);
        }
    }
}
