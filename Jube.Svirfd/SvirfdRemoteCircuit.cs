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

namespace Jube.Svirfd
{
    public sealed class SvirfdRemoteCircuit
    {
        private readonly object sync = new();
        private DateTime openUntil = DateTime.MinValue;
        private DateTime lastOpened = DateTime.MinValue;
        private int consecutive;

        public DateTime OpenUntil
        {
            get
            {
                lock (sync)
                {
                    return openUntil;
                }
            }
        }

        public bool IsOpen(DateTime now)
        {
            lock (sync)
            {
                return openUntil > now;
            }
        }

        public TimeSpan OpenForRateLimit(DateTime now)
        {
            lock (sync)
            {
                consecutive = now - lastOpened < TimeSpan.FromMinutes(1) ? Math.Min(consecutive + 1, 6) : 0;
                lastOpened = now;

                var delay = TimeSpan.FromSeconds(Math.Min(60, 1 << consecutive));
                openUntil = now.Add(delay);

                return delay;
            }
        }

        public void Open(DateTime now, TimeSpan delay)
        {
            lock (sync)
            {
                lastOpened = now;
                openUntil = now.Add(delay);
            }
        }

        public void OpenIndefinitely()
        {
            lock (sync)
            {
                openUntil = DateTime.MaxValue;
            }
        }
    }
}
