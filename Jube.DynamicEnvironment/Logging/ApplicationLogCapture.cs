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
using System.Collections.Generic;
using System.Threading;

namespace Jube.DynamicEnvironment.Logging
{
    public static class ApplicationLogCapture
    {
        private const int MaxQueueLength = 20000;

        private static readonly ConcurrentQueue<ApplicationLogCaptureRecord> queue = new();
        private static long droppedCount;

        public static long DroppedCount => Interlocked.Read(ref droppedCount);

        public static void Enqueue(ApplicationLogCaptureRecord record)
        {
            if (queue.Count >= MaxQueueLength)
            {
                Interlocked.Increment(ref droppedCount);
                return;
            }

            queue.Enqueue(record);
        }

        public static List<ApplicationLogCaptureRecord> DrainAll()
        {
            var drained = new List<ApplicationLogCaptureRecord>();

            while (queue.TryDequeue(out var record))
            {
                drained.Add(record);
            }

            return drained;
        }
    }
}