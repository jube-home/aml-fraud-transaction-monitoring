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
using Jube.Service.RedisSentinel;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddRedisSentinelAdministrator(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "RedisSentinelClusterStatusNow", OperationKind.Read, true, false,
                    "Reads every master/slave/sentinel Sentinel currently reports RIGHT NOW -- not the " +
                    "background sampler's last-stored row (see RedisSentinelStatusList), which can be " +
                    "up to a minute stale.",
                    RedisSentinelAdministratorService.RequiredPermissions,
                    RedisSentinelAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "RedisSentinelCheckQuorum", OperationKind.Read, true, false,
                    "Reads whether enough Sentinels currently agree a failover of the given master " +
                    "could actually succeed right now -- the real pre-flight check before reaching for " +
                    "RedisSentinelFailover.",
                    RedisSentinelAdministratorService.RequiredPermissions,
                    RedisSentinelAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "RedisSentinelGetMasterAddress", OperationKind.Read, true, false,
                    "Reads Sentinel's own current, authoritative address for the given master, as " +
                    "\"host:port\".",
                    RedisSentinelAdministratorService.RequiredPermissions,
                    RedisSentinelAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "RedisSentinelFailover", OperationKind.Write, false, true,
                    "Forces a manual failover of the given master right now -- the action Sentinel's " +
                    "own design exists to let an operator trigger under a real incident. Mutating.",
                    RedisSentinelAdministratorService.RequiredPermissions,
                    RedisSentinelAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "RedisSentinelReset", OperationKind.Write, false, true,
                    "Forgets Sentinel's current state of every master matching the given glob pattern, " +
                    "forcing Sentinel to rediscover it from scratch. Self-healing, no data loss, but " +
                    "still a real mutating call. Mutating.",
                    RedisSentinelAdministratorService.RequiredPermissions,
                    RedisSentinelAdministratorService.RequiresLandlord));
        }
    }
}