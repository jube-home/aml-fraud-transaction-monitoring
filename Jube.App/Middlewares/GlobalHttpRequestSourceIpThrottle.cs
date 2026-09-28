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

namespace Jube.App.Middlewares
{
    using System;
    using System.Collections.Concurrent;

    public sealed class GlobalHttpRequestSourceIpThrottle
    {
        private readonly ConcurrentDictionary<string, Window> windowsByRemoteIp = new();

        public bool TryRecordAndCheckExceeded(string remoteIp, int maxRequestsPerWindow, TimeSpan window,
            TimeProvider clock)
        {
            if (string.IsNullOrEmpty(remoteIp) || maxRequestsPerWindow <= 0)
            {
                return false;
            }

            var now = clock.GetUtcNow().UtcDateTime;
            var updated = windowsByRemoteIp.AddOrUpdate(remoteIp,
                _ => new Window(1, now),
                (_, existing) => now - existing.StartUtc > window
                    ? new Window(1, now)
                    : existing with { Count = existing.Count + 1 });

            return updated.Count > maxRequestsPerWindow;
        }

        private sealed record Window(int Count, DateTime StartUtc);
    }
}