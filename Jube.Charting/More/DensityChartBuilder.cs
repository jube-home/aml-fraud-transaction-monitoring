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

using Jube.Charting.Core;
using Jube.Charting.Exceptions;
using Jube.Charting.More.Models;
using Jube.Charting.Ticks;

namespace Jube.Charting.More
{
    public sealed class DensityChartBuilder : ChartBuilder<DensityChartBuilder>
    {
        private readonly List<(string Name, double[] Values, string? Color)> groups = [];
        private double adjust = 1;
        private double? bandwidth;
        private bool filled = true;
        private double? lower;
        private int points = 128;
        private double? upper;
        private Func<double, string>? xFormat;

        public DensityChartBuilder Values(string name, IEnumerable<double> values, string? color = null)
        {
            groups.Add((name, [.. values], color));

            return this;
        }

        public DensityChartBuilder Bandwidth(double bandwidthValue)
        {
            bandwidth = bandwidthValue;

            return this;
        }

        public DensityChartBuilder BandwidthAdjust(double factor)
        {
            adjust = factor;

            return this;
        }

        public DensityChartBuilder Filled(bool enabled = true)
        {
            filled = enabled;

            return this;
        }

        public DensityChartBuilder GridPoints(int pointCount)
        {
            points = pointCount;

            return this;
        }

        public DensityChartBuilder Range(double lowerBound, double upperBound)
        {
            lower = lowerBound;
            upper = upperBound;

            return this;
        }

        public DensityChartBuilder XFormat(Func<double, string> format)
        {
            xFormat = format;

            return this;
        }

        public BandChart Build()
        {
            if (!(adjust > 0 && double.IsFinite(adjust)))
            {
                throw new ChartSpecificationException("The bandwidth adjustment must be a positive finite number.");
            }

            ChartLimits.Series(groups.Count);
            var series = new List<BandSeries>();

            foreach (var (name, values, color) in groups)
            {
                ChartLimits.Observations(values.Length);
                var silvermanBandwidth = this.bandwidth ?? DensityMath.SilvermanBandwidth(values) * adjust;
                var estimate = DensityMath.Estimate(values, silvermanBandwidth, points, 3, lower, upper);

                var label = name + " (n=" + estimate.Count + ", bandwidth " +
                            NumberFormat.Value(Math.Round(estimate.Bandwidth, 4)) + ")";

                var zeros = estimate.Grid.Select(_ => 0.0).ToArray();

                series.Add(new BandSeries(label, estimate.Grid, estimate.Density, color, BandSeriesStyle.Line, false,
                    filled && estimate.Count > 0
                        ? [new IntervalBand("Density", zeros, estimate.Density, true)]
                        : null));
            }

            return new BandChart(new BandChartSpec
            {
                Options = Options,
                Kind = "Density chart",
                Series = series,
                XFormat = xFormat,
                YFromZero = true
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}