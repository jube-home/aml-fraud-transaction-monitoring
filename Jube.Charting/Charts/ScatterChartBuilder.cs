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
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class ScatterChartBuilder : XyBuilderBase<ScatterChartBuilder>
    {
        private bool trends;

        public ScatterChartBuilder Bubbles(string name, IEnumerable<double> xs, IEnumerable<double> ys,
            IEnumerable<double> sizes, string? color = null)
        {
            var xa = xs.ToArray();
            var ya = ys.ToArray();
            var sa = sizes.ToArray();

            if (xa.Length != ya.Length || xa.Length != sa.Length)
            {
                throw new ChartSpecificationException("X, Y and size values must have the same length.");
            }

            var points = new DataPoint[xa.Length];

            for (var i = 0; i < xa.Length; i++)
            {
                points[i] = new DataPoint(xa[i], ya[i], null, sa[i]);
            }

            return Series(name, points, color);
        }

        public ScatterChartBuilder TrendLines(bool enabled = true)
        {
            trends = enabled;

            return this;
        }

        public ScatterChartBuilder Diagonal(string? label = "Ideal")
        {
            ReferenceList.Add(new ReferenceLine(double.NegativeInfinity, double.NegativeInfinity,
                double.PositiveInfinity,
                double.PositiveInfinity, label));

            return this;
        }

        public ScatterChart Build()
        {
            var series = SeriesList.ToArray();

            var refs = ReferenceList
                .Select(r => double.IsInfinity(r.X1) && double.IsInfinity(r.Y1) ? DiagonalOver(series, r.Label) : r)
                .ToArray();

            return new ScatterChart(new ScatterChartSpec
            {
                Options = Options,
                Series = series,
                XAxis = XKind == XAxisKind.Category ? XAxisKind.Linear : XKind,
                YScale = YKind,
                References = refs,
                TrendLines = trends,
                XDomain = XDomainFixed,
                YDomain = YDomainFixed
            });
        }

        public override string Render()
        {
            return Build().Render();
        }

        private static ReferenceLine DiagonalOver(PointSeries[] series, string? label)
        {
            var e = new Extent();

            foreach (var s in series)
            {
                foreach (var p in s.Points)
                {
                    e.Add(p.X);
                    e.Add(p.Y);
                }
            }

            return e.Any ? new ReferenceLine(e.Min, e.Min, e.Max, e.Max, label) : new ReferenceLine(0, 0, 1, 1, label);
        }
    }
}