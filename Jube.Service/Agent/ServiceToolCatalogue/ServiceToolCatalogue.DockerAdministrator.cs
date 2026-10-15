/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Service.Agent.Models;
using Jube.Service.Docker;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddDockerAdministrator(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "DockerContainerListNow", OperationKind.Read, true, false,
                    "Lists every container Docker currently knows about RIGHT NOW (running or not) -- id, " +
                    "name, image, state, status -- not the background sampler's last-stored row (see " +
                    "DockerContainerMetricList), which can be up to a minute stale. Read-only.",
                    DockerAdministratorService.RequiredPermissions, DockerAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "DockerHostInfoNow", OperationKind.Read, true, false,
                    "Reads the Docker host's own live info/version -- container/image counts, CPU, " +
                    "memory, kernel, OS, Docker/API version -- RIGHT NOW, not the background sampler's " +
                    "last-stored row (see DockerHostMetricList). Read-only.",
                    DockerAdministratorService.RequiredPermissions, DockerAdministratorService.RequiresLandlord));
        }
    }
}