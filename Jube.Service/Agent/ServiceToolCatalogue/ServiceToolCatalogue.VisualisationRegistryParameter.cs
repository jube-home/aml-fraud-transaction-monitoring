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
using Jube.Service.Repository.VisualisationRegistryParameter;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddVisualisationRegistryParameter(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterList", OperationKind.Read, true, false,
                    "Lists Visualisation Registry Parameters for the caller's tenant, keyset-paged and capped.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Visualisation Registry Parameters may use.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterFilter", OperationKind.Read, true, false,
                    "Returns the Visualisation Registry Parameters matching query builder JSON, keyset-paged and capped.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterCount", OperationKind.Read, true, false,
                    "Counts the Visualisation Registry Parameters matching query builder JSON, optionally grouped by a field.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterGet", OperationKind.Read, true, false,
                    "Returns one Visualisation Registry Parameter by its id, scoped to the caller's tenant.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterGetByGuid", OperationKind.Read, true, false,
                    "Returns one Visualisation Registry Parameter by its id, scoped to the caller's tenant.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterGetByVisualisationRegistryId", OperationKind.Read, true, false,
                    "Lists the Parameters belonging to a given Visualisation Registry, ordered by Id, scoped to " +
                    "the caller's tenant.",
                    VisualisationRegistryParameterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterGetByVisualisationRegistryIdActiveOnly", OperationKind.Read,
                    true, false,
                    "Lists the active Parameters belonging to a given Visualisation Registry that the caller " +
                    "holds a role grant on, scoped to the caller's tenant.",
                    VisualisationRegistryParameterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterCreate", OperationKind.Write, false, false,
                    "Registers a Parameter under a Visualisation Registry in the caller's tenant; calling twice " +
                    "creates two.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterValidate", OperationKind.Read, true, false,
                    "Validates a Visualisation Registry Parameter without saving it and returns every failure.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterUpdate", OperationKind.Write, true, false,
                    "Updates a Parameter in the caller's tenant by id.",
                    VisualisationRegistryParameterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryParameterDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Parameter in the caller's tenant by id.",
                    VisualisationRegistryParameterService.RequiredPermissions)
            ]);
        }
    }
}