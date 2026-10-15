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
using Jube.Charting.Interfaces;
using Jube.Charting.More.Models;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class ParallelChart(ParallelChartSpec spec) : IStyleModeChart
    {
        private const int HoverRowLimit = 1200;

        public ParallelChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ParallelChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var dims = s.Dimensions;

            if (dims.Count < 2)
            {
                throw new ChartSpecificationException("Parallel coordinates need at least two dimensions.");
            }

            ChartLimits.Categories(dims.Count);
            var rows = -1;

            foreach (var d in dims)
            {
                var count = d.Values?.Count ?? d.Categories?.Count ?? throw new ChartSpecificationException(
                    "Each dimension needs numeric values or categories.");

                if (d.Values is not null && d.Categories is not null)
                {
                    throw new ChartSpecificationException("A dimension is either numeric or categorical, not both.");
                }

                if (rows >= 0 && count != rows)
                {
                    throw new ChartSpecificationException("Every dimension must have the same number of rows.");
                }

                rows = count;
            }

            rows = Math.Max(rows, 0);
            ChartLimits.Points((long)rows * dims.Count);

            if (s.Groups is not null && s.Groups.Count != rows)
            {
                throw new ChartSpecificationException("Group labels must have one entry per row.");
            }

            if (s.RowLabels is not null && s.RowLabels.Count != rows)
            {
                throw new ChartSpecificationException("Row labels must have one entry per row.");
            }

            var groupNames = new List<string>();
            var groupOf = new int[rows];

            if (s.Groups is not null)
            {
                var index = new Dictionary<string, int>();

                for (var r = 0; r < rows; r++)
                {
                    var g = s.Groups[r];

                    if (!index.TryGetValue(g, out var gi))
                    {
                        gi = groupNames.Count;
                        index[g] = gi;
                        groupNames.Add(g);
                    }

                    groupOf[r] = gi;
                }
            }
            else if (rows > 0)
            {
                groupNames.Add("All rows");
            }

            ChartLimits.Series(groupNames.Count);

            var legend = groupNames.Count > 1
                ? groupNames.Select((g, i) => new LegendEntry(g, i, LegendShape.Line)).ToList()
                : [];

            var summary = "Parallel coordinates chart of " + rows + " rows across " + dims.Count + " dimensions: " +
                          MoreKit.Names([.. dims.Select(d => d.Name)], 8) + ".";

            var options = s.Options.LegendMaxRows > 0 ? s.Options : s.Options with { LegendMaxRows = 3 };
            var canvas = new ChartCanvas(options, id, summary, legend, groupNames.Count, []);
            var interactive = rows <= HoverRowLimit;
            canvas.LimitInteractive(interactive ? rows : long.MaxValue);
            canvas.Family = StyleFamily.Parallel;

            var headerHeight = 22.0;
            var models = dims.Select(d => Model(d)).ToArray();

            var rightReserve =
                Math.Min(Math.Max(models[^1].MaxLabelWidth + 10, TextMetrics.Width(models[^1].Name, 12, true) / 2),
                    canvas.Width * 0.2);

            var frame = canvas.Frame(0, 4, rightReserve);

            var plot = new Rect(frame.Left + Math.Max(TextMetrics.Width(models[0].Name, 12, true) / 2, 8),
                frame.Top + headerHeight, frame.Right, frame.Bottom);
            canvas.SetPlot(plot);
            canvas.Begin();
            var n = dims.Count;
            var xs = Enumerable.Range(0, n).Select(i => plot.Left + plot.Width * i / (n - 1)).ToArray();
            var spacing = plot.Width / (n - 1);

            double Y(int dim, double t)
            {
                var d = dims[dim];
                t = d.Inverted ? 1 - t : t;

                return plot.Bottom - t * plot.Height;
            }

            for (var i = 0; i < n; i++)
            {
                canvas.W.Empty("line", ("class", "pax"), ("x1", Num.F(xs[i])), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(xs[i])),
                    ("y2", Num.F(plot.Bottom)));

                var head = TextMetrics.Truncate(models[i].Name, Math.Max(spacing - 8, 40), 12, true);

                canvas.W.Element("text", head, ("class", "pah ctr"), ("x", Num.F(xs[i])),
                    ("y", Num.F(plot.Top - 8)));

                for (var t = 0; t < models[i].TickPositions.Length; t++)
                {
                    var y = Y(i, models[i].TickPositions[t]);

                    canvas.W.Empty("line", ("class", "pax"), ("x1", Num.F(xs[i] - 3)), ("y1", Num.F(y)),
                        ("x2", Num.F(xs[i])),
                        ("y2", Num.F(y)));

                    var label = TextMetrics.Truncate(models[i].TickLabels[t],
                        i == n - 1 ? rightReserve - 8 : Math.Max(spacing - 12, 20), 10.5);

                    canvas.W.Element("text", label, ("class", "pcl mid"), ("x", Num.F(xs[i] + 5)), ("y", Num.F(y)));
                }
            }

            var order = Enumerable.Range(0, rows).OrderBy(r => groupOf[r]).ThenBy(r => r).ToArray();
            var dots = interactive && (long)rows * n <= 3000;
            var fine = rows > 400;
            var current = -1;

            foreach (var r in order)
            {
                if (groupOf[r] != current)
                {
                    if (current >= 0)
                    {
                        canvas.W.Close();
                    }

                    current = groupOf[r];
                    canvas.OpenSeries(current);
                }

                var pts = new double[n];
                var path = new PathBuilder();

                for (var i = 0; i < n; i++)
                {
                    var t = models[i].Position(r);
                    pts[i] = double.IsFinite(t) ? Y(i, t) : double.NaN;
                    path.Line(xs[i], pts[i]);
                }

                if (interactive)
                {
                    var label = s.RowLabels is null ? "Row " + (r + 1) : s.RowLabels[r];
                    var tip = new List<TipRow> { new(label, true, groupNames.Count > 1 ? groupOf[r] : null) };

                    if (groupNames.Count > 1)
                    {
                        tip.Add(new TipRow("Group: " + groupNames[groupOf[r]]));
                    }

                    for (var i = 0; i < Math.Min(n, 12); i++)
                    {
                        tip.Add(new TipRow(models[i].Name + ": " + models[i].Text(r)));
                    }

                    if (n > 12)
                    {
                        tip.Add(new TipRow("+" + (n - 12) + " more"));
                    }

                    canvas.OpenMark(groupOf[r], string.Join(", ", tip.Select(x => x.Text)));
                    canvas.W.Empty("path", ("class", "hl"), ("d", path.ToString()));
                    canvas.W.Empty("path", ("class", "pl"), ("d", path.ToString()));

                    if (dots)
                    {
                        for (var i = 0; i < n; i++)
                        {
                            if (double.IsFinite(pts[i]))
                            {
                                canvas.W.Empty("circle", ("class", "cx"), ("cx", Num.F(xs[i])), ("cy", Num.F(pts[i])),
                                    ("r", "3.5"));
                            }
                        }
                    }

                    canvas.Tooltip(plot.CenterX, plot.Top, tip, false, 4);
                    canvas.CloseMark();
                }
                else
                {
                    canvas.W.Empty("path", ("class", fine ? "pl fine" : "pl"), ("d", path.ToString()));
                }
            }

            if (current >= 0)
            {
                canvas.W.Close();
            }

            if (rows == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            return canvas.End();
        }

        private static AxisModel Model(ParallelDimension d)
        {
            if (d.Categories is not null)
            {
                var cats = d.Categories.Where(c => !string.IsNullOrEmpty(c)).Select(c => c!).Distinct()
                    .OrderBy(c => c, StringComparer.Ordinal).ToList();
                ChartLimits.Categories(cats.Count);
                var index = cats.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);
                var k = Math.Max(cats.Count, 1);

                return new AxisModel
                {
                    Name = d.Name,
                    TickLabels = [.. cats],
                    TickPositions = [.. Enumerable.Range(0, cats.Count).Select(i => (i + 0.5) / k)],
                    Position =
                        r => string.IsNullOrEmpty(d.Categories[r]) ? double.NaN : (index[d.Categories[r]!] + 0.5) / k,
                    Text = r => string.IsNullOrEmpty(d.Categories[r]) ? "n/a" : d.Categories[r]!,
                    MaxLabelWidth = TextMetrics.MaxWidth(cats, 10.5)
                };
            }

            var values = d.Values!;
            var extent = new Extent();

            foreach (var v in values)
            {
                extent.Add(v);
            }

            var lo = d.Domain?.Min ?? (extent.Any ? extent.Min : 0);
            var hi = d.Domain?.Max ?? (extent.Any ? extent.Max : 1);

            if (!(hi > lo))
            {
                lo -= 1;
                hi += 1;
            }

            var (scale, tickValues) = AxisFactory.Numeric(lo, hi, 0, 1, AxisKind.Linear, 5, d.Domain);
            var format = d.Format;
            var labels = format is null ? NumberFormat.Axis(tickValues) : [.. tickValues.Select(format)];
            var span = scale.DomainMax - scale.DomainMin;

            return new AxisModel
            {
                Name = d.Name,
                TickLabels = [.. labels],
                TickPositions = [.. tickValues.Select(v => (v - scale.DomainMin) / span)],
                Position = r => double.IsFinite(values[r]) ? (values[r] - scale.DomainMin) / span : double.NaN,
                Text = r => double.IsFinite(values[r])
                    ? format is null ? NumberFormat.Value(values[r]) : format(values[r])
                    : "n/a",
                MaxLabelWidth = TextMetrics.MaxWidth(labels, 10.5)
            };
        }

        private sealed class AxisModel
        {
            public required string Name { get; init; }
            public required string[] TickLabels { get; init; }
            public required double[] TickPositions { get; init; }
            public required Func<int, double> Position { get; init; }
            public required Func<int, string> Text { get; init; }
            public required double MaxLabelWidth { get; init; }
        }
    }
}