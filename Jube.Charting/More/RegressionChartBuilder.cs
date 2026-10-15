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
    public sealed class RegressionChartBuilder : ChartBuilder<RegressionChartBuilder>
    {
        private readonly List<(string Name, double[] Xs, double[] Ys, string? Color)> groups = [];
        private double confidence = 0.95;
        private bool confidenceBand = true;
        private int degree = 1;
        private double extend;
        private bool predictionBand = true;
        private Func<double, string>? xFormat;
        private Func<double, string>? yFormat;

        public RegressionChartBuilder Data(string name, IEnumerable<double> xs, IEnumerable<double> ys,
            string? color = null)
        {
            groups.Add((name, [.. xs], [.. ys], color));

            return this;
        }

        public RegressionChartBuilder Degree(int polynomialDegree)
        {
            degree = polynomialDegree;

            return this;
        }

        public RegressionChartBuilder ConfidenceLevel(double level)
        {
            confidence = level;

            return this;
        }

        public RegressionChartBuilder ConfidenceBand(bool show = true)
        {
            confidenceBand = show;

            return this;
        }

        public RegressionChartBuilder PredictionBand(bool show = true)
        {
            predictionBand = show;

            return this;
        }

        public RegressionChartBuilder Extend(double fraction)
        {
            extend = fraction;

            return this;
        }

        public RegressionChartBuilder XFormat(Func<double, string> format)
        {
            xFormat = format;

            return this;
        }

        public RegressionChartBuilder YFormat(Func<double, string> format)
        {
            yFormat = format;

            return this;
        }

        public BandChart Build()
        {
            if (extend < 0 || extend > 2)
            {
                throw new ChartSpecificationException("The regression extension must be between 0 and 2.");
            }

            var series = new List<BandSeries>();
            var level = NumberFormat.Value(Math.Round(confidence * 100, 1)) + "%";

            foreach (var (name, xs, ys, color) in groups)
            {
                var fit = RegressionMath.Fit(xs, ys, degree, confidence);

                var finite = Enumerable.Range(0, xs.Length).Where(i => double.IsFinite(xs[i]) && double.IsFinite(ys[i]))
                    .ToArray();

                var lo = finite.Min(i => xs[i]);
                var hi = finite.Max(i => xs[i]);
                var span = hi - lo;
                var from = lo - span * extend;
                var to = hi + span * extend;
                const int samples = 60;
                var gx = new double[samples];
                var gy = new double[samples];
                var cl = new double[samples];
                var cu = new double[samples];
                var pl = new double[samples];
                var pu = new double[samples];

                for (var i = 0; i < samples; i++)
                {
                    gx[i] = from + (to - from) * i / (samples - 1);
                    gy[i] = fit.Predict(gx[i]);
                    (cl[i], cu[i]) = fit.MeanInterval(gx[i]);
                    (pl[i], pu[i]) = fit.PredictionInterval(gx[i]);
                }

                series.Add(new BandSeries(name, xs, ys, color, BandSeriesStyle.Points));
                var bands = new List<IntervalBand>();

                if (predictionBand)
                {
                    bands.Add(new IntervalBand(level + " prediction", pl, pu));
                }

                if (confidenceBand)
                {
                    bands.Add(new IntervalBand(level + " confidence", cl, cu));
                }

                var raw = fit.RawCoefficients();

                var equation = degree == 1
                    ? "y = " + NumberFormat.Value(raw[0]) + (raw[1] < 0 ? " - " : " + ") +
                      NumberFormat.Value(Math.Abs(raw[1])) + "x"
                    : "degree " + degree;

                series.Add(new BandSeries(
                    name + " fit: " + equation + ", R² " + NumberFormat.Value(Math.Round(fit.RSquared, 3)),
                    gx, gy, null, BandSeriesStyle.Line, false, bands));
            }

            return new BandChart(new BandChartSpec
            {
                Options = Options,
                Kind = "Regression chart",
                Series = series,
                XFormat = xFormat,
                YFormat = yFormat
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}