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
        static partial void AddRedisCallCounter(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "RedisCallCounterList", OperationKind.Read, true, false,
                    "Lists per-minute Redis call counters -- one row per distinct \"call\" label (a " +
                    "Jube.Cache repository method, e.g. \"CachePayloadRepository.InsertAsync\") per minute, " +
                    "with Count/TotalMicroseconds/MinMicroseconds/MaxMicroseconds for that window, most " +
                    "recent first, capped at 100000. The dedicated, reset-each-minute counterpart to the live " +
                    "jube.cache.redis.call.count/.duration OpenTelemetry instruments, answering exactly where " +
                    "Redis load is coming from by area of the system. Supports optional date-range and " +
                    "substring search (Call) filters, not scoped to any tenant. Also accepts samplePercentage " +
                    "(0-100) to draw an unbiased random subset of matching rows instead of the most recent " +
                    "ones."));
        }
    }
}