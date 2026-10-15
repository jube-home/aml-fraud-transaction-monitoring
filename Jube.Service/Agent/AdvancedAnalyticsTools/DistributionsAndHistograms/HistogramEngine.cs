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

using Accord.Statistics;
using Accord.Statistics.Visualizations;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal static class HistogramEngine
    {
        public const int MaximumBins = 100000;

        public static QuantileMethod ToMethod(QuantileDefinition definition)
        {
            return (QuantileMethod)(int)definition;
        }

        public static double Quantile(double[] values, double probability,
            QuantileDefinition definition = QuantileDefinition.Type7)
        {
            return values.Quantile(probability, false, ToMethod(definition));
        }

        public static double[] Quantiles(double[] values, double[] probabilities,
            QuantileDefinition definition = QuantileDefinition.Type7)
        {
            return values.Quantiles(probabilities, false, ToMethod(definition));
        }

        public static double StandardDeviation(double[] v)
        {
            return v.StandardDeviation();
        }

        public static double Mean(double[] v)
        {
            return v.Mean();
        }

        public static double Covariance(double[] a, double[] b)
        {
            return a.Covariance(b);
        }

        public static double InterquartileRange(double[] values)
        {
            var q = Quantiles(values, new[] { 0.25, 0.75 });

            return q[1] - q[0];
        }

        public static int BinsForRule(double[] values, BinningRule rule, List<string> notes, out double width)
        {
            var n = values.Length;
            var min = values.Min();
            var max = values.Max();
            var range = max - min;
            width = 0;

            if (range <= 0 || n < 2)
            {
                width = 1;

                return 1;
            }

            double count;

            switch (rule)
            {
                case BinningRule.Sturges:
                    count = Math.Ceiling(Math.Log2(n) + 1);
                    break;
                case BinningRule.Rice:
                    count = Math.Ceiling(2.0 * Math.Cbrt(n));
                    break;
                case BinningRule.SquareRoot:
                    count = Math.Ceiling(Math.Sqrt(n));
                    break;
                case BinningRule.Doane:
                    var skew = n > 2 ? values.Skewness(false) : 0.0;
                    var sigma = Math.Sqrt(6.0 * (n - 2) / ((n + 1.0) * (n + 3.0)));

                    count = n > 2 && sigma > 0
                        ? Math.Ceiling(1 + Math.Log2(n) + Math.Log2(1 + Math.Abs(skew) / sigma))
                        : Math.Ceiling(Math.Log2(n) + 1);

                    break;
                case BinningRule.Scott:
                    var sd = values.StandardDeviation();
                    var scottWidth = 3.49 * sd / Math.Cbrt(n);

                    if (scottWidth <= 0)
                    {
                        notes.Add("Scott rule needs a nonzero standard deviation; fell back to Sturges.");

                        return BinsForRule(values, BinningRule.Sturges, notes, out width);
                    }

                    count = Math.Ceiling(range / scottWidth);
                    break;
                default:
                    var iqr = InterquartileRange(values);
                    var fdWidth = 2.0 * iqr / Math.Cbrt(n);

                    if (fdWidth <= 0)
                    {
                        notes.Add("Freedman-Diaconis needs a nonzero interquartile range; fell back to Sturges.");

                        return BinsForRule(values, BinningRule.Sturges, notes, out width);
                    }

                    count = Math.Ceiling(range / fdWidth);
                    break;
            }

            var bins = (int)Math.Min(Math.Max(count, 1), MaximumBins);

            if (count > MaximumBins)
            {
                notes.Add($"The rule asked for {count} bins; limited to {MaximumBins}.");
            }

            width = range / bins;

            return bins;
        }

        public static HistogramResult FixedBins(double[] values, int binCount, HistogramScale scale, bool cumulative,
            string method, List<string> notes)
        {
            var min = values.Min();
            var max = values.Max();

            if (max - min <= 0)
            {
                notes.Add("Every value is identical, so a single bin of width 1 centred on the value was used.");

                return Build(values, new[] { min - 0.5, min + 0.5 }, new[] { values.Length }, 0, scale, cumulative,
                    method,
                    notes);
            }

            var histogram = new Histogram();
            histogram.Compute(values, binCount);
            var edges = (double[])histogram.Edges.Clone();
            var counts = (int[])histogram.Values.Clone();

            return Build(values, edges, counts, 0, scale, cumulative, method, notes);
        }

        public static HistogramResult ByWidth(double[] values, double binWidth, HistogramScale scale, bool cumulative,
            string method, List<string> notes)
        {
            var min = values.Min();
            var max = values.Max();

            if (max - min <= 0)
            {
                notes.Add(
                    "Every value is identical, so a single bin of the requested width centred on the value was used.");

                return Build(values, new[] { min - binWidth / 2, min + binWidth / 2 }, new[] { values.Length }, 0,
                    scale,
                    cumulative, method, notes);
            }

            var bins = Math.Ceiling((max - min) / binWidth);

            if (bins > MaximumBins)
            {
                throw new ArgumentException(
                    $"binWidth {binWidth} would create {bins} bins over the range {min} to {max}; use a larger binWidth (at most {MaximumBins} bins).",
                    "binWidth");
            }

            var histogram = new Histogram();
            histogram.Compute(values, binWidth);
            var edges = (double[])histogram.Edges.Clone();
            var counts = (int[])histogram.Values.Clone();

            return Build(values, edges, counts, 0, scale, cumulative, method, notes);
        }

        public static int[] CountIntoEdges(double[] values, double[] edges, out int outside)
        {
            var counts = new int[edges.Length - 1];
            outside = 0;
            var last = edges[^1];

            foreach (var v in values)
            {
                if (v < edges[0] || v > last)
                {
                    outside++;
                    continue;
                }

                var index = Array.BinarySearch(edges, v);

                if (index < 0)
                {
                    index = ~index - 1;
                }

                if (index >= counts.Length)
                {
                    index = counts.Length - 1;
                }

                counts[index]++;
            }

            return counts;
        }

        public static HistogramResult Build(double[] values, double[] edges, int[] counts, int outside,
            HistogramScale scale,
            bool cumulative, string method, IEnumerable<string> notes)
        {
            var bins = counts.Length;
            var centers = new double[bins];
            var widths = new double[bins];
            var relative = new double[bins];
            var density = new double[bins];
            var inside = counts.Sum();

            for (var i = 0; i < bins; i++)
            {
                widths[i] = edges[i + 1] - edges[i];
                centers[i] = (edges[i] + edges[i + 1]) / 2.0;
                relative[i] = (double)counts[i] / values.Length;
                density[i] = inside > 0 && widths[i] > 0 ? counts[i] / (inside * widths[i]) : 0.0;
            }

            var display = new double[bins];

            for (var i = 0; i < bins; i++)
            {
                display[i] = scale switch
                {
                    HistogramScale.RelativeFrequency => relative[i],
                    HistogramScale.Percent => relative[i] * 100.0,
                    HistogramScale.Density => density[i],
                    _ => counts[i]
                };
            }

            if (cumulative)
            {
                var running = 0.0;

                for (var i = 0; i < bins; i++)
                {
                    var step = scale == HistogramScale.Density ? density[i] * widths[i] : display[i];
                    running += step;
                    display[i] = running;
                }
            }

            return new HistogramResult(
                values.Length,
                bins,
                method,
                scale.ToString(),
                cumulative,
                edges,
                centers,
                widths,
                counts,
                relative,
                density,
                display,
                outside,
                values.Min(),
                values.Max(),
                notes.ToArray());
        }
    }
}