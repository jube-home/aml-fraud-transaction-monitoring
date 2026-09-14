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

using System.Collections.Concurrent;

namespace Jube.Cache
{
    public static class RedisSentinelEventCapture
    {
        private const int MaxQueueLength = 20000;

        private static readonly ConcurrentQueue<RedisSentinelEventCaptureRecord> queue = new();
        private static long droppedCount;

        public static int QueueDepth => queue.Count;

        public static long TakeDroppedCount()
        {
            return Interlocked.Exchange(ref droppedCount, 0);
        }

        public static void AddDroppedCount(long amount)
        {
            Interlocked.Add(ref droppedCount, amount);
        }

        public static void Enqueue(RedisSentinelEventCaptureRecord record)
        {
            if (queue.Count >= MaxQueueLength)
            {
                Interlocked.Increment(ref droppedCount);
                return;
            }

            queue.Enqueue(record);
        }

        public static List<RedisSentinelEventCaptureRecord> DrainAll()
        {
            var drained = new List<RedisSentinelEventCaptureRecord>();

            while (queue.TryDequeue(out var record))
            {
                drained.Add(record);
            }

            return drained;
        }
    }
}