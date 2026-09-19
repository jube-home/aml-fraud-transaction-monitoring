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
        static partial void AddVisualisationRegistryDatasource(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceList", OperationKind.Read, true, false,
                    "Lists Datasources for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceGet", OperationKind.Read, true, false,
                    "Returns one Datasource by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceGetByVisualisationRegistryId", OperationKind.Read, true,
                    false,
                    "Lists Datasources belonging to a given Visualisation Registry, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceGetByVisualisationRegistryIdActiveOnly", OperationKind.Read,
                    true, false,
                    "Lists active, role-visible Datasources belonging to a given Visualisation Registry for the caller."),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceCreate", OperationKind.Write, false, false,
                    "Registers a Datasource under a Visualisation Registry in the caller's tenant, validating its SQL Command; calling twice creates two."),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceUpdate", OperationKind.Write, true, false,
                    "Updates a Datasource in the caller's tenant by id, re-validating its SQL Command."),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Datasource in the caller's tenant by id.")
            ]);
        }
    }
}