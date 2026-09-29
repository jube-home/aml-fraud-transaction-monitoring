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

namespace Jube.ResilientNpgsqlConnection
{
    using System.Collections.Concurrent;

    public sealed class PoolClearGate(TimeProvider timeProvider, TimeSpan window)
    {
        private const int DefaultWindowMilliseconds = 5000;

        private static PoolClearGate shared = new(TimeProvider.System,
            TimeSpan.FromMilliseconds(DefaultWindowMilliseconds));

        private readonly ConcurrentDictionary<string, long> lastClear = new();

        public static PoolClearGate Shared => Volatile.Read(ref shared);

        public TimeSpan Window { get; } = window;

        public static void ConfigureShared(TimeSpan window)
        {
            Volatile.Write(ref shared, new PoolClearGate(TimeProvider.System, window));
        }

        public bool TryEnter(string key)
        {
            var now = timeProvider.GetTimestamp();

            while (true)
            {
                if (!lastClear.TryGetValue(key, out var last))
                {
                    if (lastClear.TryAdd(key, now))
                    {
                        return true;
                    }

                    continue;
                }

                if (timeProvider.GetElapsedTime(last, now) < Window)
                {
                    return false;
                }

                if (lastClear.TryUpdate(key, now, last))
                {
                    return true;
                }
            }
        }
    }
}