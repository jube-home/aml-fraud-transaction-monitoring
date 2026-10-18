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
using Jube.Service.RedisKeyActivity;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddRedisKeyActivity(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "RedisKeyActivityList", OperationKind.Read, true, false,
                    "Samples up to 100 keys from the live Redis keyspace (SCAN, never KEYS) and reports each " +
                    "one's type, idle time, and -- only under an LFU maxmemory-policy -- its approximate " +
                    "access frequency. The 100-key cap is fixed. Optional glob pattern narrows the sample. " +
                    "For spotting a hot/cold key design problem, never for enumerating the keyspace. " +
                    "Read-only.",
                    RedisKeyActivityService.RequiredPermissions, RedisKeyActivityService.RequiresLandlord));
        }
    }
}