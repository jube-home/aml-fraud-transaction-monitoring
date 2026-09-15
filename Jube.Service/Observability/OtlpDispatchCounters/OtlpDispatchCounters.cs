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

namespace Jube.Service.Observability.OtlpDispatchCounters
{
    public static class OtlpDispatchCounters
    {
        private static readonly ConcurrentDictionary<string, OtlpDispatchAccumulator> accumulators = new();

        public static void RecordDispatch(string signal, bool success, long itemCount, double elapsedMilliseconds)
        {
            var accumulator = accumulators.GetOrAdd(signal, static _ => new OtlpDispatchAccumulator());
            var microseconds = (long)(elapsedMilliseconds * 1000.0);

            Interlocked.Increment(ref accumulator.Count);
            if (success)
            {
                Interlocked.Increment(ref accumulator.SuccessCount);
            }
            else
            {
                Interlocked.Increment(ref accumulator.FailureCount);
            }

            Interlocked.Add(ref accumulator.ItemCount, itemCount);
            Interlocked.Add(ref accumulator.TotalMicroseconds, microseconds);
            AccumulateMin(ref accumulator.MinMicroseconds, microseconds);
            AccumulateMax(ref accumulator.MaxMicroseconds, microseconds);
        }

        public static void RecordDropped(string signal, long droppedCount)
        {
            var accumulator = accumulators.GetOrAdd(signal, static _ => new OtlpDispatchAccumulator());
            Interlocked.Add(ref accumulator.DroppedCount, droppedCount);
        }

        public static List<Snapshot> TakeSnapshot()
        {
            var snapshot = new List<Snapshot>();

            foreach (var (signal, accumulator) in accumulators)
            {
                var count = Interlocked.Exchange(ref accumulator.Count, 0);
                var droppedCount = Interlocked.Exchange(ref accumulator.DroppedCount, 0);
                if (count == 0 && droppedCount == 0)
                {
                    continue;
                }

                var successCount = Interlocked.Exchange(ref accumulator.SuccessCount, 0);
                var failureCount = Interlocked.Exchange(ref accumulator.FailureCount, 0);
                var itemCount = Interlocked.Exchange(ref accumulator.ItemCount, 0);
                var totalMicroseconds = Interlocked.Exchange(ref accumulator.TotalMicroseconds, 0);
                var minMicroseconds = Interlocked.Exchange(ref accumulator.MinMicroseconds, long.MaxValue);
                var maxMicroseconds = Interlocked.Exchange(ref accumulator.MaxMicroseconds, long.MinValue);

                if (count == 0)
                {
                    minMicroseconds = 0;
                    maxMicroseconds = 0;
                }

                snapshot.Add(new Snapshot(signal, count, successCount, failureCount, itemCount, droppedCount,
                    totalMicroseconds, minMicroseconds, maxMicroseconds));
            }

            return snapshot;
        }

        private static void AccumulateMin(ref long location, long value)
        {
            var initial = Volatile.Read(ref location);
            while (value < initial)
            {
                var previous = Interlocked.CompareExchange(ref location, value, initial);
                if (previous == initial)
                {
                    return;
                }

                initial = previous;
            }
        }

        private static void AccumulateMax(ref long location, long value)
        {
            var initial = Volatile.Read(ref location);
            while (value > initial)
            {
                var previous = Interlocked.CompareExchange(ref location, value, initial);
                if (previous == initial)
                {
                    return;
                }

                initial = previous;
            }
        }

        public sealed record Snapshot(
            string Signal,
            long Count,
            long SuccessCount,
            long FailureCount,
            long ItemCount,
            long DroppedCount,
            long TotalMicroseconds,
            long MinMicroseconds,
            long MaxMicroseconds);
    }
}