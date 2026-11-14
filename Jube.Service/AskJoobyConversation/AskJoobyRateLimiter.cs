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

namespace Jube.Service.AskJoobyConversation
{
    public static class AskJoobyRateLimiter
    {
        private const int MaximumKeys = 10_000;
        private static readonly object gate = new();
        private static readonly Dictionary<string, (DateTime Start, int Count)> windows = new();
        private static readonly Dictionary<string, int> running = new();

        public static bool TryBegin(int tenantRegistryId, string userName, AskJoobyRateLimits limits, DateTime now,
            out IDisposable? slot)
        {
            var userKey = $"user:{tenantRegistryId}:{(userName ?? string.Empty).ToLowerInvariant()}";
            var tenantKey = $"tenant:{tenantRegistryId}";
            var window = TimeSpan.FromSeconds(limits.WindowSeconds);

            lock (gate)
            {
                slot = null;

                if (running.GetValueOrDefault(userKey) >= limits.MaxConcurrentTurnsPerUser
                    || running.GetValueOrDefault(tenantKey) >= limits.MaxConcurrentTurnsPerTenant)
                {
                    return false;
                }

                if (!WithinWindow(userKey, limits.MaxMessagesPerUserPerWindow, window, now)
                    || !WithinWindow(tenantKey, limits.MaxMessagesPerTenantPerWindow, window, now))
                {
                    return false;
                }

                running[userKey] = running.GetValueOrDefault(userKey) + 1;
                running[tenantKey] = running.GetValueOrDefault(tenantKey) + 1;
                slot = new Slot(userKey, tenantKey);

                return true;
            }
        }

        private static bool WithinWindow(string key, int limit, TimeSpan window, DateTime now)
        {
            if (windows.TryGetValue(key, out var current) && now - current.Start < window)
            {
                if (current.Count >= limit)
                {
                    return false;
                }

                windows[key] = (current.Start, current.Count + 1);

                return true;
            }

            if (windows.Count >= MaximumKeys && !windows.ContainsKey(key))
            {
                foreach (var stale in windows.Where(w => now - w.Value.Start >= window).Select(w => w.Key).ToList())
                {
                    windows.Remove(stale);
                }
            }

            if (windows.Count >= MaximumKeys && !windows.ContainsKey(key))
            {
                return true;
            }

            windows[key] = (now, 1);

            return true;
        }

        private static void Release(string userKey, string tenantKey)
        {
            lock (gate)
            {
                Decrement(userKey);
                Decrement(tenantKey);
            }
        }

        private static void Decrement(string key)
        {
            if (!running.TryGetValue(key, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                running.Remove(key);
            }
            else
            {
                running[key] = count - 1;
            }
        }

        public static void Reset()
        {
            lock (gate)
            {
                windows.Clear();
                running.Clear();
            }
        }

        private sealed class Slot(string userKey, string tenantKey) : IDisposable
        {
            private int released;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref released, 1) == 0)
                {
                    Release(userKey, tenantKey);
                }
            }
        }
    }
}
