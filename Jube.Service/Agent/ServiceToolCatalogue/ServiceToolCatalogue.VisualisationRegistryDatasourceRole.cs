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
        static partial void AddVisualisationRegistryDatasourceRole(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "VisualisationRegistryDatasourceRoleListByVisualisationRegistryDatasourceGuid", OperationKind.Read,
                Idempotent: true, Destructive: false,
                "Lists the Role grants for a Visualisation Registry Datasource."),
            new ServiceToolDescriptor(
                "VisualisationRegistryDatasourceRoleCreate", OperationKind.Write, Idempotent: false,
                Destructive: false,
                "Grants a Role access to a Visualisation Registry Datasource; calling twice creates two grants."),
            new ServiceToolDescriptor(
                "VisualisationRegistryDatasourceRoleDelete", OperationKind.Delete, Idempotent: false,
                Destructive: true,
                "Removes a Role's grant on a Visualisation Registry Datasource; reversible only by direct " +
                "database action. A repeat call on an already-removed grant throws rather than succeeding as a " +
                "no-op, so retries are not safe to assume succeeded.")
        ]);
    }
}