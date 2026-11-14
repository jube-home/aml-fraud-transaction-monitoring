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

namespace Jube.Service.Query.Parser
{
    internal static class ParseRateLimiter
    {
        private const int MaximumKeys = 10_000;
        private static readonly object gate = new();
        private static readonly Dictionary<string, (DateTime Start, int Count)> windows = new();

        internal static bool TryAcquire(string key, int limit, TimeSpan window, DateTime now)
        {
            lock (gate)
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
                    Purge(window, now);
                }

                if (windows.Count >= MaximumKeys && !windows.ContainsKey(key))
                {
                    return true;
                }

                windows[key] = (now, 1);

                return true;
            }
        }

        private static void Purge(TimeSpan window, DateTime now)
        {
            foreach (var key in windows.Where(w => now - w.Value.Start >= window).Select(w => w.Key).ToList())
            {
                windows.Remove(key);
            }
        }
    }
}
