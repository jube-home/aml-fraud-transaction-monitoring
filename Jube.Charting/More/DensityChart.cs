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
    public sealed class EcdfChartBuilder : ChartBuilder<EcdfChartBuilder>
    {
        private readonly List<(string Name, double[] Values, string? Color)> groups = [];
        private double alpha = 0.05;
        private bool band;
        private bool complementary;
        private Func<double, string>? xFormat;

        public EcdfChartBuilder Values(string name, IEnumerable<double> values, string? color = null)
        {
            groups.Add((name, [.. values], color));

            return this;
        }

        public EcdfChartBuilder ConfidenceBand(double alphaLevel = 0.05)
        {
            band = true;
            alpha = alphaLevel;

            return this;
        }

        public EcdfChartBuilder Complementary(bool enabled = true)
        {
            complementary = enabled;

            return this;
        }

        public EcdfChartBuilder XFormat(Func<double, string> format)
        {
            xFormat = format;

            return this;
        }

        public BandChart Build()
        {
            if (!(alpha is > 0 and < 1))
            {
                throw new ChartSpecificationException("The confidence band alpha must be strictly between 0 and 1.");
            }

            ChartLimits.Series(groups.Count);
            var series = new List<BandSeries>();

            foreach (var (name, values, color) in groups)
            {
                ChartLimits.Observations(values.Length);
                var steps = DensityMath.Ecdf(values).ToList();
                var count = DensityMath.Finite(values).Length;

                if (steps.Count > 1500)
                {
                    steps = MoreKit.Thin(steps, 1500);
                }

                var xs = new List<double>();
                var ys = new List<double>();

                if (steps.Count > 0)
                {
                    xs.Add(steps[0].X);
                    ys.Add(complementary ? 1 : 0);
                }

                foreach (var (x, y) in steps)
                {
                    xs.Add(x);
                    ys.Add(complementary ? 1 - y : y);
                }

                var epsilon = count > 0 ? Math.Sqrt(Math.Log(2 / alpha) / (2 * count)) : 0;

                var bands = band && count > 0
                    ? new[]
                    {
                        new IntervalBand(NumberFormat.Value(Math.Round((1 - alpha) * 100, 1)) + "% band (DKW)",
                            [.. ys.Select(v => Math.Max(v - epsilon, 0))],
                            [.. ys.Select(v => Math.Min(v + epsilon, 1))])
                    }
                    : null;

                series.Add(new BandSeries(name + " (n=" + count + ")", xs, ys, color, BandSeriesStyle.Step, false,
                    bands));
            }

            return new BandChart(new BandChartSpec
            {
                Options = Options,
                Kind = complementary ? "Complementary ECDF chart" : "ECDF chart",
                Series = series,
                XFormat = xFormat,
                YFormat = v => NumberFormat.Percent(v, 0),
                YDomain = (0, 1)
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}