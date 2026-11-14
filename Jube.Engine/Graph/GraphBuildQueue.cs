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

namespace Jube.Engine.Graph
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    public static class GraphBuildQueue
    {
        public const int Capacity = 200000;
        private static readonly ConcurrentQueue<Guid> queue = new();
        private static long dropped;

        public static int Count => queue.Count;

        public static long Dropped => Interlocked.Read(ref dropped);

        public static void Enqueue(IEnumerable<Guid> entryGuids)
        {
            foreach (var guid in entryGuids)
            {
                if (queue.Count >= Capacity)
                {
                    Interlocked.Increment(ref dropped);

                    continue;
                }

                queue.Enqueue(guid);
            }
        }

        public static List<Guid> Drain(int maximum)
        {
            var taken = new List<Guid>();

            while (taken.Count < maximum && queue.TryDequeue(out var guid))
            {
                taken.Add(guid);
            }

            return taken;
        }
    }
}
