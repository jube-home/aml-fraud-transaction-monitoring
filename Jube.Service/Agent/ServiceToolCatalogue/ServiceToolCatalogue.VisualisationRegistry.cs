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
using Jube.Service.Repository.VisualisationRegistry;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddVisualisationRegistry(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "VisualisationRegistryList", OperationKind.Read, true, false,
                    "Lists Visualisation Registries visible to the caller's tenant, keyset-paged and capped.",
                    VisualisationRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Visualisation Registrys may use.",
                    VisualisationRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryFilter", OperationKind.Read, true, false,
                    "Returns the Visualisation Registrys matching query builder JSON, keyset-paged and capped.",
                    VisualisationRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryCount", OperationKind.Read, true, false,
                    "Counts the Visualisation Registrys matching query builder JSON, optionally grouped by a field.",
                    VisualisationRegistryService.ListPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryGet", OperationKind.Read, true, false,
                    "Returns a single Visualisation Registry by Id.",
                    VisualisationRegistryService.ReadPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Visualisation Registry by Id.",
                    VisualisationRegistryService.ReadPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryGetByGuidActiveOnly", OperationKind.Read, true, false,
                    "Returns a single active Visualisation Registry by Guid, for embedded recall, provided the " +
                    "caller holds a role grant on it.",
                    VisualisationRegistryService.ReadPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryListByShowInDirectory", OperationKind.Read, true,
                    false,
                    "Lists active, Show In Directory Visualisation Registries that the caller holds a role grant on.",
                    VisualisationRegistryService.ShowInDirectoryPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryCreate", OperationKind.Write, false, false,
                    "Creates a Visualisation Registry; calling twice creates two.",
                    VisualisationRegistryService.WritePermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryValidate", OperationKind.Read, true, false,
                    "Validates a Visualisation Registry without saving it and returns every failure.",
                    VisualisationRegistryService.WritePermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryUpdate", OperationKind.Write, true, false,
                    "Updates a Visualisation Registry; overwrites, writes a version-audit row.",
                    VisualisationRegistryService.WritePermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Visualisation Registry; reversible only by direct database action.",
                    VisualisationRegistryService.WritePermissions)
            ]);
        }
    }
}