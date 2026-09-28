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

namespace Jube.Service.Authentication
{
    using System.Collections.Concurrent;

    public sealed class LoginSourceIpThrottle
    {
        private readonly ConcurrentDictionary<string, Window> windowsByRemoteIp = new();

        public bool IsExceeded(string? remoteIp, int maxAttemptsPerWindow, TimeSpan window, TimeProvider clock)
        {
            if (string.IsNullOrEmpty(remoteIp) || maxAttemptsPerWindow <= 0
                                               || !windowsByRemoteIp.TryGetValue(remoteIp, out var existing))
            {
                return false;
            }

            var now = clock.GetUtcNow().UtcDateTime;
            return now - existing.StartUtc <= window && existing.Count > maxAttemptsPerWindow;
        }

        public void RecordFailedAttempt(string? remoteIp, TimeSpan window, TimeProvider clock)
        {
            if (string.IsNullOrEmpty(remoteIp))
            {
                return;
            }

            var now = clock.GetUtcNow().UtcDateTime;
            windowsByRemoteIp.AddOrUpdate(remoteIp,
                _ => new Window(1, now),
                (_, existing) => now - existing.StartUtc > window
                    ? new Window(1, now)
                    : existing with { Count = existing.Count + 1 });
        }

        private sealed record Window(int Count, DateTime StartUtc);
    }
}