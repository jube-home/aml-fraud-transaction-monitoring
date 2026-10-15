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
using Jube.Service.Patroni;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddPatroniAdministrator(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "PatroniClusterStatusNow", OperationKind.Read, true, false,
                    "Reads every discoverable Patroni member's status RIGHT NOW over its own REST API -- " +
                    "role, state, timeline, lag, pending restart -- not the background sampler's last-" +
                    "stored row (see PatroniMemberStatusList), which can be up to a minute stale.",
                    PatroniAdministratorService.RequiredPermissions,
                    PatroniAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "PatroniConfigGet", OperationKind.Read, true, false,
                    "Reads a Patroni member's own resolved dynamic configuration (DCS-stored config " +
                    "merged with whatever is local to that node) as plain JSON text. Read-only.",
                    PatroniAdministratorService.RequiredPermissions,
                    PatroniAdministratorService.RequiresLandlord));
        }
    }
}