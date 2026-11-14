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
using Jube.Service.Query.VisualisationRegistryDatasourceCommandExecution;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddVisualisationRegistryDatasourceCommandExecution(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceCommandExecutionCard", OperationKind.Read, true, false,
                    "Returns the card for a Visualisation Registry Datasource the caller may see: its chart drawn as an SVG, the rows only when its grid is on, and its state.",
                    VisualisationRegistryDatasourceCommandExecutionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "VisualisationRegistryDatasourceCommandExecutionExecute", OperationKind.Read, true,
                    false,
                    "Executes a Visualisation Registry Datasource command with caller-supplied parameter values and returns its rows.",
                    VisualisationRegistryDatasourceCommandExecutionService.RequiredPermissions)
            ]);
        }
    }
}