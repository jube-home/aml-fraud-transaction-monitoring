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

namespace Jube.Cache.Observability.CacheCallCounters
{
    public static class CacheCallCounters
    {
        private static readonly ConcurrentDictionary<string, CacheCallCounterAccumulator> accumulators = new();

        public static void Record(string call, double elapsedMilliseconds)
        {
            var accumulator = accumulators.GetOrAdd(call, static _ => new CacheCallCounterAccumulator());
            var microseconds = (long)(elapsedMilliseconds * 1000.0);

            Interlocked.Increment(ref accumulator.Count);
            Interlocked.Add(ref accumulator.TotalMicroseconds, microseconds);
            AccumulateMin(ref accumulator.MinMicroseconds, microseconds);
            AccumulateMax(ref accumulator.MaxMicroseconds, microseconds);
        }

        public static List<Snapshot> TakeSnapshot()
        {
            var snapshot = new List<Snapshot>();

            foreach (var (call, accumulator) in accumulators)
            {
                var count = Interlocked.Exchange(ref accumulator.Count, 0);
                if (count == 0)
                {
                    continue;
                }

                var totalMicroseconds = Interlocked.Exchange(ref accumulator.TotalMicroseconds, 0);
                var minMicroseconds = Interlocked.Exchange(ref accumulator.MinMicroseconds, long.MaxValue);
                var maxMicroseconds = Interlocked.Exchange(ref accumulator.MaxMicroseconds, long.MinValue);

                snapshot.Add(new Snapshot(call, count, totalMicroseconds, minMicroseconds, maxMicroseconds));
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
            string Call,
            long Count,
            long TotalMicroseconds,
            long MinMicroseconds,
            long MaxMicroseconds);
    }
}