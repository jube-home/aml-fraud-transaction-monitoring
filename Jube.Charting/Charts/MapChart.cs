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

using System.Globalization;
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Core.Models;
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Charting.Maps;
using Jube.Charting.Ticks;

namespace Jube.Charting.Charts
{
    public sealed class MapChart(MapChartSpec spec) : IStyleModeChart
    {
        public MapChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new MapChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, id => MapRenderer.Render(Model(), id));
        }

        private MapModel Model()
        {
            var s = Spec;
            ChartLimits.Points(s.Points.Count + s.Countries.Count);

            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            var unmatched = 0;

            foreach (var entry in s.Countries)
            {
                var country = WorldMap.Find(entry.Code);

                if (country == null)
                {
                    unmatched++;
                    continue;
                }

                if (!double.IsFinite(entry.Value))
                {
                    continue;
                }

                if (!values.TryAdd(country.Iso3, entry.Value))
                {
                    throw new ChartSpecificationException(
                        "The country " + country.Name + " appears more than once.");
                }
            }

            foreach (var point in s.Points)
            {
                if (!double.IsFinite(point.Latitude) || !double.IsFinite(point.Longitude) ||
                    Math.Abs(point.Latitude) > 90 || Math.Abs(point.Longitude) > 180)
                {
                    throw new ChartSpecificationException(
                        "A point has a latitude outside -90 to 90 or a longitude outside -180 to 180.");
                }
            }

            var groups = s.Points.Select(p => p.Group ?? string.Empty).Distinct(StringComparer.Ordinal).ToList();
            ChartLimits.Series(groups.Count);

            var extent = new Extent();

            foreach (var v in values.Values)
            {
                extent.Add(v);
            }

            var lo = extent.Any ? extent.Min : 0;
            var hi = extent.Any ? extent.Max : 1;

            if (hi <= lo)
            {
                hi = lo + 1;
            }

            var detail = new List<string>();

            if (values.Count > 0)
            {
                detail.Add(values.Count + " countries are coloured by value, from " + NumberFormat.Value(lo) + " to " +
                           NumberFormat.Value(hi) + ".");
            }

            if (s.Points.Count > 0)
            {
                detail.Add(s.Points.Count.ToString(CultureInfo.InvariantCulture) + " points are plotted.");
            }

            if (unmatched > 0)
            {
                detail.Add(unmatched + " values did not match a country and are not shown.");
            }

            return new MapModel
            {
                Options = s.Options,
                ValuesByIso3 = values,
                Points = s.Points,
                Groups = groups,
                Domain = (lo, hi),
                Format = s.ValueFormat ?? NumberFormat.Value,
                Detail = string.Join(" ", detail),
                LegendTitle = s.LegendTitle
            };
        }
    }
}
