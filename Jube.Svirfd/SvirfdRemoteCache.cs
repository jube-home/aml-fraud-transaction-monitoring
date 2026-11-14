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
using System;
using System.Collections.Concurrent;
using System.Linq;
using Svirfd;

namespace Jube.Svirfd
{
    public sealed class SvirfdRemoteCache(TimeSpan lifetime, int maximumEntries)
    {
        private readonly ConcurrentDictionary<string, (IpResponse Record, DateTime Expires)> entries = new();

        public int Count => entries.Count;

        public bool TryGet(string address, DateTime now, out IpResponse? record)
        {
            record = null;

            if (!entries.TryGetValue(address, out var entry))
            {
                return false;
            }

            if (entry.Expires > now)
            {
                record = entry.Record;

                return true;
            }

            entries.TryRemove(address, out _);

            return false;
        }

        public void Set(string address, IpResponse record, DateTime now)
        {
            if (entries.Count >= maximumEntries && !entries.ContainsKey(address))
            {
                Sweep(now);
            }

            entries[address] = (record, now.Add(lifetime));
        }

        private void Sweep(DateTime now)
        {
            foreach (var expired in entries.Where(e => e.Value.Expires <= now).Select(e => e.Key).ToArray())
            {
                entries.TryRemove(expired, out _);
            }

            if (entries.Count < maximumEntries)
            {
                return;
            }

            var oldest = entries.OrderBy(e => e.Value.Expires).Take(Math.Max(1, maximumEntries / 4))
                .Select(e => e.Key).ToArray();

            foreach (var key in oldest)
            {
                entries.TryRemove(key, out _);
            }
        }
    }
}
