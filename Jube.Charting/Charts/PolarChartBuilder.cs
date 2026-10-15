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
    public sealed class PolarChartBuilder : ChartBuilder<PolarChartBuilder>
    {
        private readonly List<PointSeries> series = [];
        private Func<double, string>? format;
        private PolarMode mode;
        private int spokes = 12;

        public PolarChartBuilder Series(string name, IEnumerable<double> anglesDegrees, IEnumerable<double> radii,
            string? color = null)
        {
            var a = anglesDegrees.ToArray();
            var r = radii.ToArray();

            if (a.Length != r.Length)
            {
                throw new ChartSpecificationException("Angles and radii must have the same length.");
            }

            series.Add(new PointSeries(name, [.. a.Select((v, i) => new DataPoint(v, r[i]))], color));

            return this;
        }

        public PolarChartBuilder Series(string name, IEnumerable<DataPoint> points, string? color = null)
        {
            series.Add(new PointSeries(name, [.. points], color));

            return this;
        }

        public PolarChartBuilder Lines()
        {
            mode = PolarMode.Line;

            return this;
        }

        public PolarChartBuilder Areas()
        {
            mode = PolarMode.Area;

            return this;
        }

        public PolarChartBuilder Spokes(int spokeCount)
        {
            spokes = spokeCount;

            return this;
        }

        public PolarChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public PolarChart Build()
        {
            return new PolarChart(new PolarChartSpec
            {
                Options = Options,
                Series = [.. series],
                Mode = mode,
                Spokes = spokes,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}