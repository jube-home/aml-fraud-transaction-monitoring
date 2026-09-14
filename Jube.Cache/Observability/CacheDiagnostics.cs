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

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Jube.Cache.Observability
{
    public static class CacheDiagnostics
    {
        public const string Name = "Jube.Cache";

        private static readonly AssemblyName Asm = typeof(CacheDiagnostics).Assembly.GetName();
        private static readonly string Version = Asm.Version?.ToString() ?? "0.0.0";
        private static readonly Meter Meter = new(Name, Version);

        private static readonly Counter<long> RedisCallCount =
            Meter.CreateCounter<long>("jube.cache.redis.call.count", "{call}");

        private static readonly Histogram<double> RedisCallDuration =
            Meter.CreateHistogram<double>("jube.cache.redis.call.duration", "ms");

        private static long inFlightCount;
        public static long InFlightCallCount => Interlocked.Read(ref inFlightCount);

        public static async Task<T> RecordAsync<T>(string call, Func<Task<T>> func)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref inFlightCount);
            try
            {
                return await func().ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref inFlightCount);
                Record(call, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public static async Task RecordAsync(string call, Func<Task> func)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref inFlightCount);
            try
            {
                await func().ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref inFlightCount);
                Record(call, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public static T Record<T>(string call, Func<T> func)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref inFlightCount);
            try
            {
                return func();
            }
            finally
            {
                Interlocked.Decrement(ref inFlightCount);
                Record(call, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public static void Record(string call, TimeSpan elapsed)
        {
            Record(call, elapsed.TotalMilliseconds);
        }

        private static void Record(string call, double elapsedMilliseconds)
        {
            var tags = new TagList { { "call", call } };
            RedisCallCount.Add(1, tags);
            RedisCallDuration.Record(elapsedMilliseconds, tags);

            CacheCallCounters.CacheCallCounters.Record(call, elapsedMilliseconds);
        }
    }
}