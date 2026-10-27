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

using Jube.Charting.Charts.Models;
using Jube.Charting.Core;

namespace Jube.Charting.Charts
{
    public sealed class HistogramChartBuilder : ChartBuilder<HistogramChartBuilder>
    {
        private readonly List<HistogramSeries> binned = [];
        private readonly List<HistogramReference> references = [];
        private readonly List<(string Name, double[] Values, string? Color)> raw = [];
        private int? bins;
        private bool density;
        private bool normalize;

        public HistogramChartBuilder Values(string name, IEnumerable<double> values, string? color = null)
        {
            var array = values.Where(double.IsFinite).ToArray();
            ChartLimits.Points(raw.Sum(r => (long)r.Values.Length) + array.Length);
            raw.Add((name, array, color));

            return this;
        }

        public HistogramChartBuilder Bins(string name, IEnumerable<double> edges, IEnumerable<double> counts,
            string? color = null)
        {
            binned.Add(new HistogramSeries(name, [.. edges], [.. counts], color));

            return this;
        }

        public HistogramChartBuilder Reference(double x, string label)
        {
            references.Add(new HistogramReference(x, label));

            return this;
        }

        public HistogramChartBuilder BinCount(int binCount)
        {
            bins = binCount;

            return this;
        }

        public HistogramChartBuilder Normalized(bool enabled = true)
        {
            normalize = enabled;

            return this;
        }

        public HistogramChartBuilder Density(bool enabled = true)
        {
            density = enabled;

            return this;
        }

        public HistogramChart Build()
        {
            var series = new List<HistogramSeries>();

            if (raw.Count > 0)
            {
                var all = raw.SelectMany(r => r.Values).ToArray();
                var edges = BinEdges(all, bins);

                foreach (var (name, values, color) in raw)
                {
                    series.Add(new HistogramSeries(name, edges, Count(values, edges), color));
                }
            }

            series.AddRange(binned);

            return new HistogramChart(new HistogramChartSpec
            {
                Options = Options,
                Series = [.. series],
                Normalize = normalize,
                Density = density,
                References = [.. references]
            });
        }

        public override string Render()
        {
            return Build().Render();
        }

        public static double[] BinEdges(double[] values, int? requested)
        {
            if (values.Length == 0)
            {
                return [0, 1];
            }

            var min = values.Min();
            var max = values.Max();

            if (Math.Abs(min - max) < 0.0001)
            {
                return [min - 0.5, min + 0.5];
            }

            int bins;

            if (requested is > 0)
            {
                bins = Math.Min(requested.Value, 200);

                return [.. Enumerable.Range(0, bins + 1).Select(i => min + (max - min) * i / bins)];
            }

            var sorted = values.OrderBy(v => v).ToArray();
            var q1 = sorted[(int)((sorted.Length - 1) * 0.25)];
            var q3 = sorted[(int)((sorted.Length - 1) * 0.75)];
            var iqr = q3 - q1;

            var width = iqr > 0
                ? 2 * iqr / Math.Cbrt(sorted.Length)
                : (max - min) / (Math.Ceiling(Math.Log2(sorted.Length)) + 1);

            width = Math.Max(width, (max - min) / 60);
            width = Math.Min(width, (max - min) / 4);
            width = NiceWidth(width);
            var start = Math.Floor(min / width) * width;
            var end = Math.Ceiling(max / width) * width;

            if (end <= max)
            {
                end += width;
            }

            var count = (int)Math.Round((end - start) / width);
            var result = new double[count + 1];

            for (var i = 0; i <= count; i++)
            {
                result[i] = Math.Round(start + i * width, 12);
            }

            return result;
        }

        public static double[] Count(double[] values, IReadOnlyList<double> edges)
        {
            var counts = new double[edges.Count - 1];

            foreach (var v in values)
            {
                var lo = 0;
                var hi = edges.Count - 1;

                if (v < edges[0] || v > edges[hi])
                {
                    continue;
                }

                while (hi - lo > 1)
                {
                    var mid = (lo + hi) / 2;

                    if (v >= edges[mid])
                    {
                        lo = mid;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                counts[lo]++;
            }

            return counts;
        }

        private static double NiceWidth(double width)
        {
            var exponent = Math.Floor(Math.Log10(width));
            var fraction = width / Math.Pow(10, exponent);
            var nice = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;

            return nice * Math.Pow(10, exponent);
        }
    }
}