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
using Jube.Charting.Core;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Jube.Charting.Performance.Models;
using Jube.Charting.Svg;
using Jube.Charting.Text;

namespace Jube.Charting.Performance
{
    public sealed class MetricsPanelChart(MetricsPanelChartSpec spec) : IStyleModeChart
    {
        private const double TileHeight = 72;
        private const double TileGap = 8;
        private const double GroupHeader = 22;
        private const double GroupGap = 10;
        private const double HeaderReserve = 60;

        public MetricsPanelChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new MetricsPanelChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var columns = Math.Clamp(s.Columns, 1, 6);
            var tiles = s.Groups.Sum(g => g.Tiles.Count);
            ChartLimits.Categories(tiles);

            var contentHeight = s.Groups.Sum(g =>
                GroupHeader + Math.Ceiling(g.Tiles.Count / (double)columns) * (TileHeight + TileGap) + GroupGap);

            var options = s.Options with
            {
                Legend = LegendPosition.None,
                Height = Math.Clamp(HeaderReserve + contentHeight + ChartCanvas.Pad, 60, 4000)
            };

            return ChartRun.Execute(options, id => Draw(options, id, columns));
        }

        private string Draw(ChartOptions options, string id, int columns)
        {
            var s = Spec;
            var count = s.Groups.Sum(g => g.Tiles.Count);

            var summary = "Model performance measures panel with " + count + " measures: " + string.Join("; ",
                s.Groups.SelectMany(g => g.Tiles).Take(20).Select(t => TextMetrics.Truncate(t.Name, 60, 12) + " " +
                                                                       Display(t)));

            var canvas = new ChartCanvas(options, id, summary, [], 0, [], true);
            canvas.LimitInteractive(count);
            canvas.FramePlain(4);
            var plot = canvas.Plot;
            canvas.Begin();

            if (count == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");

                return canvas.End();
            }

            var tileWidth = (plot.Width - TileGap * (columns - 1)) / columns;
            var y = plot.Top;

            foreach (var group in s.Groups)
            {
                canvas.Text(plot.Left, y + 12, TextMetrics.Truncate(group.Title, plot.Width, 12), "at");
                y += GroupHeader;

                for (var i = 0; i < group.Tiles.Count; i++)
                {
                    var tile = group.Tiles[i];
                    var x = plot.Left + i % columns * (tileWidth + TileGap);
                    var top = y + (double)i / columns * (TileHeight + TileGap);
                    var rows = new List<TipRow> { new(tile.Name, true), new("Value: " + Display(tile)) };

                    if (!string.IsNullOrWhiteSpace(tile.Detail))
                    {
                        rows.Add(new TipRow(tile.Detail!));
                    }

                    if (!string.IsNullOrWhiteSpace(tile.Description))
                    {
                        rows.Add(new TipRow(tile.Description!));
                    }

                    canvas.OpenMark(-1, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("rect", ("class", "gtrack"), ("x", Num.F(x)), ("y", Num.F(top)),
                        ("width", Num.F(tileWidth)), ("height", Num.F(TileHeight)), ("rx", "6"));

                    canvas.Text(x + 10, top + 18, TextMetrics.Truncate(tile.Name, tileWidth - 20, 11), "tk");
                    canvas.Text(x + 10, top + 46, TextMetrics.Truncate(Display(tile), tileWidth - 20, 28), "big");

                    if (!string.IsNullOrWhiteSpace(tile.Detail))
                    {
                        canvas.Text(x + 10, top + 63, TextMetrics.Truncate(tile.Detail!, tileWidth - 20, 11), "tk");
                    }

                    canvas.Tooltip(x + tileWidth / 2, top, rows);
                    canvas.CloseMark();
                }

                y += Math.Ceiling(group.Tiles.Count / (double)columns) * (TileHeight + TileGap) + GroupGap;
            }

            return canvas.End();
        }

        private static string Display(MetricTile tile)
        {
            return double.IsFinite(tile.Value)
                ? tile.Value.ToString(tile.Format ?? "0.000", CultureInfo.InvariantCulture)
                : "n/a";
        }
    }
}