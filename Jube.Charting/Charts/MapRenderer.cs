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

using System.Text;
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Core.Models;
using Jube.Charting.Maps;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Ticks;

namespace Jube.Charting.Charts
{
    internal static class MapRenderer
    {
        private const double MinimumRadius = 4;
        private const double MaximumRadius = 22;
        private const double PointRadius = 4.5;
        private const double LegendReserve = 76;
        private const int MaximumCountryLabels = 12;

        public static string Render(MapModel m, string id)
        {
            var summary = ("Map of the world. " + m.Detail).Trim();

            var legend = m.Groups.Count > 1
                ? m.Groups.Select((g, i) => new LegendEntry(g.Length == 0 ? "Other" : g, i)).ToList()
                : [];

            var canvas = new ChartCanvas(m.Options, id, summary, legend, m.Groups.Count, [], m.HasValues);
            canvas.LimitInteractive(m.ValuesByIso3.Count + m.Points.Count);
            canvas.Frame(0, 0, m.HasValues ? LegendReserve : 0);
            canvas.Begin();

            var plot = canvas.Plot;
            var (minX, minY, maxX, maxY) = WorldMap.Bounds();
            var scale = Math.Min(plot.Width / (maxX - minX), plot.Height / (maxY - minY));
            var offsetX = plot.Left + (plot.Width - (maxX - minX) * scale) / 2 - minX * scale;
            var offsetY = plot.Top + (plot.Height - (maxY - minY) * scale) / 2 - minY * scale;

            (double X, double Y) ToPlot(double lon, double lat)
            {
                var p = WorldMap.Project(lon, lat);

                return (p.X * scale + offsetX, p.Y * scale + offsetY);
            }

            var bins = Palette.Light.Sequential.Count;
            var (lo, hi) = m.Domain;

            int Bin(double v)
            {
                return Math.Clamp((int)Math.Floor((v - lo) / (hi - lo) * bins), 0, bins - 1);
            }

            foreach (var country in m.Countries)
            {
                var path = PathOf(country, ToPlot);

                if (!m.ValuesByIso3.TryGetValue(country.Iso3, out var value))
                {
                    canvas.W.Empty("path", ("class", "cell m na"), ("d", path));
                    continue;
                }

                var rows = new List<TipRow> { new(country.Name, true), new("Value: " + m.Format(value)) };
                canvas.OpenMark(-1, country.Name + ", " + m.Format(value));
                canvas.W.Empty("path", ("class", "cell m q" + Bin(value)), ("d", path));

                var anchor = ToPlot(Centre(country).Lon, Centre(country).Lat);
                canvas.Tooltip(anchor.X, anchor.Y, rows);
                canvas.CloseMark();
            }

            if (!canvas.Interactive)
            {
                DrawCountryLabels(canvas, m, ToPlot);
            }

            DrawPoints(canvas, m, ToPlot);

            if (m.HasValues)
            {
                DrawLegendBar(canvas, m, plot, bins, lo, hi);
            }

            return canvas.End();
        }

        private static void DrawCountryLabels(ChartCanvas canvas, MapModel m,
            Func<double, double, (double X, double Y)> toPlot)
        {
            var labelled = m.Countries.Where(c => m.ValuesByIso3.ContainsKey(c.Iso3))
                .OrderByDescending(c => m.ValuesByIso3[c.Iso3]).Take(MaximumCountryLabels);

            foreach (var country in labelled)
            {
                var centre = Centre(country);
                var at = toPlot(centre.Lon, centre.Lat);

                canvas.W.Element("text", TextMetrics.Truncate(country.Name, 90, 11), ("class", "lbl ctr mid"),
                    ("x", Num.F(at.X)), ("y", Num.F(at.Y)));
            }
        }

        private static void DrawPoints(ChartCanvas canvas, MapModel m, Func<double, double, (double X, double Y)> toPlot)
        {
            if (m.Points.Count == 0)
            {
                return;
            }

            var sized = m.Points.Any(p => double.IsFinite(p.Size) && p.Size > 0);
            var largest = sized ? m.Points.Where(p => double.IsFinite(p.Size)).Max(p => p.Size) : 1;

            for (var group = 0; group < m.Groups.Count; group++)
            {
                canvas.OpenSeries(group);

                foreach (var p in m.Points.Where(p => (p.Group ?? string.Empty) == m.Groups[group]))
                {
                    if (p.Latitude is < WorldMap.MinimumLatitude or > WorldMap.MaximumLatitude)
                    {
                        continue;
                    }

                    var at = toPlot(p.Longitude, p.Latitude);
                    var hasSize = double.IsFinite(p.Size) && p.Size > 0 && largest > 0;

                    var radius = sized && hasSize
                        ? MinimumRadius + (MaximumRadius - MinimumRadius) * Math.Sqrt(p.Size / largest)
                        : PointRadius;

                    var cls = sized ? "bub" : "pt";
                    var label = p.Label ?? NumberFormat.Value(p.Latitude) + ", " + NumberFormat.Value(p.Longitude);
                    var rows = new List<TipRow> { new(label, true) };

                    if (m.Groups.Count > 1)
                    {
                        rows.Add(new TipRow("Group: " + (p.Group is { Length: > 0 } ? p.Group : "Other")));
                    }

                    if (hasSize)
                    {
                        rows.Add(new TipRow("Size: " + m.Format(p.Size)));
                    }

                    if (canvas.Interactive)
                    {
                        canvas.OpenMark(group, string.Join(", ", rows.Select(r => r.Text)));
                        canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(at.X)), ("cy", Num.F(at.Y)),
                            ("r", Num.F(Math.Max(radius + 4, 9))));

                        canvas.W.Empty("circle", ("class", cls), ("cx", Num.F(at.X)), ("cy", Num.F(at.Y)),
                            ("r", Num.F(radius)));

                        canvas.Tooltip(at.X, at.Y - radius, rows);
                        canvas.CloseMark();
                    }
                    else
                    {
                        canvas.W.Empty("circle", ("class", cls), ("cx", Num.F(at.X)), ("cy", Num.F(at.Y)),
                            ("r", Num.F(radius)));
                    }
                }

                canvas.W.Close();
            }
        }

        private static string PathOf(MapCountry country, Func<double, double, (double X, double Y)> toPlot)
        {
            var builder = new StringBuilder();

            foreach (var ring in country.Rings)
            {
                for (var i = 0; i < ring.Length; i++)
                {
                    var p = toPlot(ring[i].Lon, ring[i].Lat);
                    builder.Append(i == 0 ? 'M' : 'L').Append(Tenth(p.X)).Append(' ').Append(Tenth(p.Y));
                }

                builder.Append('Z');
            }

            return builder.ToString();
        }

        private static string Tenth(double value)
        {
            var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);

            return rounded == 0 ? "0" : rounded.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static (double Lon, double Lat) Centre(MapCountry country)
        {
            var biggest = country.Rings.OrderByDescending(r => r.Length).First();

            return (biggest.Average(p => p.Lon), biggest.Average(p => p.Lat));
        }

        private static void DrawLegendBar(ChartCanvas canvas, MapModel m, Rect plot, int bins, double lo, double hi)
        {
            var x = plot.Right + 16;
            var height = Math.Min(plot.Height, 220);
            var top = plot.Top;
            var block = height / bins;

            for (var i = 0; i < bins; i++)
            {
                canvas.W.Empty("rect", ("class", "q" + (bins - 1 - i)), ("x", Num.F(x)), ("y", Num.F(top + i * block)),
                    ("width", "12"), ("height", Num.F(block + 0.5)));
            }

            if (m.LegendTitle is not null)
            {
                canvas.Text(x, top - 14, TextMetrics.Truncate(m.LegendTitle, 70, 11), "tk");
            }

            var ticks = ExtendedTicks.Generate(lo, hi, Math.Clamp((int)(height / 40), 2, 6))
                .Where(v => v >= lo - 1e-9 && v <= hi + 1e-9).ToArray();

            var lastY = double.NegativeInfinity;

            foreach (var v in ticks)
            {
                var y = top + (1 - (v - lo) / (hi - lo)) * height;

                if (Math.Abs(y - lastY) < 14)
                {
                    continue;
                }

                lastY = y;
                canvas.W.Empty("line", ("class", "ax"), ("x1", Num.F(x + 12)), ("y1", Num.F(y)), ("x2", Num.F(x + 16)),
                    ("y2", Num.F(y)));

                canvas.W.Element("text", TextMetrics.Truncate(m.Format(v), 56, 11), ("class", "tk mid"),
                    ("x", Num.F(x + 20)), ("y", Num.F(y)));
            }
        }
    }
}
