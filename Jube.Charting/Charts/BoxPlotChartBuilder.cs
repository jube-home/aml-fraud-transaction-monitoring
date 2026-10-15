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
    public sealed class BoxPlotChartBuilder : ChartBuilder<BoxPlotChartBuilder>
    {
        private readonly List<BoxPlotItem> boxes = [];
        private Func<double, string>? format;

        public BoxPlotChartBuilder Box(string label, double min, double q1, double median, double q3, double max,
            IEnumerable<double>? outliers = null, string? color = null)
        {
            boxes.Add(new BoxPlotItem(label, min, q1, median, q3, max, outliers?.ToArray(), color));

            return this;
        }

        public BoxPlotChartBuilder FromValues(string label, IEnumerable<double> values, string? color = null)
        {
            var data = values.Where(double.IsFinite).OrderBy(v => v).ToArray();
            ChartLimits.Points(boxes.Sum(_ => 5L) + data.Length);

            if (data.Length == 0)
            {
                boxes.Add(new BoxPlotItem(label, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, null,
                    color,
                    0));

                return this;
            }

            var q1 = Quantile(data, 0.25);
            var med = Quantile(data, 0.5);
            var q3 = Quantile(data, 0.75);
            var iqr = q3 - q1;
            var lowFence = q1 - 1.5 * iqr;
            var highFence = q3 + 1.5 * iqr;
            var inside = data.Where(v => v >= lowFence && v <= highFence).ToArray();
            var outliers = data.Where(v => v < lowFence || v > highFence).ToArray();
            boxes.Add(new BoxPlotItem(label, inside.Min(), q1, med, q3, inside.Max(), outliers, color, data.Length));

            return this;
        }

        public BoxPlotChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public static double Quantile(IReadOnlyList<double> sorted, double p)
        {
            var position = (sorted.Count - 1) * p;
            var lo = (int)Math.Floor(position);
            var hi = (int)Math.Ceiling(position);

            return sorted[lo] + (sorted[hi] - sorted[lo]) * (position - lo);
        }

        public BoxPlotChart Build()
        {
            return new BoxPlotChart(new BoxPlotChartSpec
                { Options = Options, Boxes = [.. boxes], ValueFormat = format });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}