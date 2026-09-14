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
using System.Linq;
using System.Threading;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics.OpenTelemetry
{
    public static class OpenTelemetryMetricCapture
    {
        private const int MaxDistinctSeries = 10000;
        private static readonly Lock aggregatorsLock = new();
        private static Dictionary<string, Aggregator> aggregators = new();
        private static long droppedNewSeriesCount;
        private static long sampledOutCount;
        private static double samplePercentage = 100.0;
        private static MeterListener listener;

        public static long SampledOutCount => Interlocked.Read(ref sampledOutCount);

        public static int QueueDepth
        {
            get
            {
                lock (aggregatorsLock)
                {
                    return aggregators.Count;
                }
            }
        }

        public static long TakeDroppedNewSeriesCount()
        {
            return Interlocked.Exchange(ref droppedNewSeriesCount, 0);
        }

        public static void AddDroppedNewSeriesCount(long amount)
        {
            Interlocked.Add(ref droppedNewSeriesCount, amount);
        }

        public static void Start(double newSamplePercentage, params string[] meterNames)
        {
            samplePercentage = Math.Clamp(newSamplePercentage, 0.0, 100.0);
            listener?.Dispose();

            var names = new HashSet<string>(meterNames, StringComparer.Ordinal);
            var newListener = new MeterListener
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (names.Contains(instrument.Meter.Name))
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                }
            };

            newListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
                Record(instrument, measurement, tags));
            newListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
                Record(instrument, measurement, tags));
            newListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
                Record(instrument, measurement, tags));

            newListener.Start();
            listener = newListener;
        }

        public static List<Aggregation> DrainAll()
        {
            Dictionary<string, Aggregator> drained;
            lock (aggregatorsLock)
            {
                drained = aggregators;
                aggregators = new Dictionary<string, Aggregator>();
            }

            return drained.Values.Select(aggregator => new Aggregation(
                aggregator.InstrumentName, aggregator.InstrumentType, aggregator.Tags,
                aggregator.Count, aggregator.Sum, aggregator.Min, aggregator.Max)).ToList();
        }

        private static void Record(Instrument instrument, double value,
            ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            if (samplePercentage < 100.0 && Random.Shared.NextDouble() * 100.0 >= samplePercentage)
            {
                Interlocked.Increment(ref sampledOutCount);
                return;
            }

            var tagsKey = BuildTagsKey(tags);
            var key = $"{instrument.Name}|{tagsKey}";

            lock (aggregatorsLock)
            {
                if (!aggregators.TryGetValue(key, out var aggregator))
                {
                    if (aggregators.Count >= MaxDistinctSeries)
                    {
                        Interlocked.Increment(ref droppedNewSeriesCount);
                        return;
                    }

                    aggregator = new Aggregator(instrument.Name, DescribeInstrumentType(instrument), tagsKey);
                    aggregators[key] = aggregator;
                }

                aggregator.Add(value);
            }
        }

        private static string BuildTagsKey(ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            if (tags.Length == 0)
            {
                return string.Empty;
            }

            var pairs = new List<string>(tags.Length);
            foreach (var tag in tags)
            {
                pairs.Add($"{tag.Key}={tag.Value}");
            }

            pairs.Sort(StringComparer.Ordinal);
            return string.Join(',', pairs);
        }

        private static string DescribeInstrumentType(Instrument instrument)
        {
            var name = instrument.GetType().Name;
            var backtickIndex = name.IndexOf('`');
            return backtickIndex < 0 ? name : name[..backtickIndex];
        }

        public sealed record Aggregation(
            string InstrumentName,
            string InstrumentType,
            string Tags,
            long Count,
            double Sum,
            double Min,
            double Max);
    }
}