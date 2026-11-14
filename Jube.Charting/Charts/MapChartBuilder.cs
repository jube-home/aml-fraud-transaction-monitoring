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

namespace Jube.Charting.Charts
{
    public sealed class MapChartBuilder : ChartBuilder<MapChartBuilder>
    {
        private readonly List<MapCountryValue> countries = [];
        private readonly List<MapPoint> points = [];
        private string? legendTitle;
        private Func<double, string>? format;

        public MapChartBuilder Point(double latitude, double longitude, double size = double.NaN, string? label = null,
            string? group = null)
        {
            points.Add(new MapPoint(latitude, longitude, size, label, group));

            return this;
        }

        public MapChartBuilder Points(IEnumerable<double> latitudes, IEnumerable<double> longitudes,
            IEnumerable<double>? sizes = null, IEnumerable<string?>? labels = null, IEnumerable<string?>? groups = null)
        {
            var lat = latitudes.ToArray();
            var lon = longitudes.ToArray();
            var size = sizes?.ToArray();
            var label = labels?.ToArray();
            var group = groups?.ToArray();

            if (lat.Length != lon.Length || size != null && size.Length != lat.Length ||
                label != null && label.Length != lat.Length || group != null && group.Length != lat.Length)
            {
                throw new ChartSpecificationException(
                    "Latitudes, longitudes, sizes, labels and groups must have the same length.");
            }

            for (var i = 0; i < lat.Length; i++)
            {
                points.Add(new MapPoint(lat[i], lon[i], size?[i] ?? double.NaN, label?[i], group?[i]));
            }

            return this;
        }

        public MapChartBuilder Country(string code, double value)
        {
            countries.Add(new MapCountryValue(code, value));

            return this;
        }

        public MapChartBuilder Countries(IEnumerable<string> codes, IEnumerable<double> values)
        {
            var c = codes.ToArray();
            var v = values.ToArray();

            if (c.Length != v.Length)
            {
                throw new ChartSpecificationException("Codes and values must have the same length.");
            }

            for (var i = 0; i < c.Length; i++)
            {
                countries.Add(new MapCountryValue(c[i], v[i]));
            }

            return this;
        }

        public MapChartBuilder LegendTitle(string? title)
        {
            legendTitle = title;

            return this;
        }

        public MapChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public MapChart Build()
        {
            return new MapChart(new MapChartSpec
            {
                Options = Options,
                Points = [.. points],
                Countries = [.. countries],
                LegendTitle = legendTitle,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}
