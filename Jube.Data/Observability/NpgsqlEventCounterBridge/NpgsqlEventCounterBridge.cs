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
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Jube.Data.Observability.NpgsqlEventCounterBridge
{
    public static class NpgsqlEventCounterBridge
    {
        private static readonly HashSet<string> incrementingCounterNames =
            new(StringComparer.Ordinal) { "bytes-written-per-second", "bytes-read-per-second", "commands-per-second" };

        private static readonly Dictionary<string, Counter<double>> counters = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Histogram<double>> histograms = new(StringComparer.Ordinal);
        private static readonly Lock instrumentLock = new();
        private static readonly Lock startLock = new();
        private static EventListener listener;

        public static void Start()
        {
            lock (startLock)
            {
                listener?.Dispose();
                listener = new Listener();
            }
        }

        internal static void Record(string counterName, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return;
            }

            var metricName = "jube.postgres.client." + counterName.Replace('-', '_');

            if (incrementingCounterNames.Contains(counterName))
            {
                GetCounter(metricName).Add(value);
            }
            else
            {
                GetHistogram(metricName).Record(value);
            }
        }

        private static Counter<double> GetCounter(string metricName)
        {
            lock (instrumentLock)
            {
                if (!counters.TryGetValue(metricName, out var counter))
                {
                    counter = DataDiagnostics.Meter.CreateCounter<double>(metricName);
                    counters[metricName] = counter;
                }

                return counter;
            }
        }

        private static Histogram<double> GetHistogram(string metricName)
        {
            lock (instrumentLock)
            {
                if (histograms.TryGetValue(metricName, out var histogram))
                {
                    return histogram;
                }

                histogram = DataDiagnostics.Meter.CreateHistogram<double>(metricName);
                histograms[metricName] = histogram;

                return histogram;
            }
        }
    }
}