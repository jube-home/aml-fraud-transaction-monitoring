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
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Interfaces;
using Jube.Charting.Scales;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Ticks;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams
{
    public sealed class DistributionPanels(DistributionPanelsSpec spec) : IStyleModeChart
    {
        public DistributionPanelsSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new DistributionPanels(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            Validate();
            var s = Spec;
            var plan = PanelKit.Plan(s.Panels.Count, s.Options.Width, CellHeight, 210, s.MaxColumns);
            var names = s.Panels[0].Series.Select(x => x.Name).ToArray();
            var legend = names.Select((n, i) => new LegendEntry(n, i, LegendShape.Line)).ToArray();

            var summary = "Distribution small multiples: " + s.Panels.Count + " panels comparing " +
                          string.Join(", ", names.Take(4).Select(n => TextMetrics.Truncate(n, 60, 12))) + ".";

            var columnsPer = Math.Clamp(1100 / s.Panels.Count, 4, 28);
            var fit = DiagramFrame.Fit(s.Options, summary, legend, plan.Width, plan.Height, 0, "distribution panels");

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, legend, (long)s.Panels.Count * columnsPer);
                var w = scene.W;
                scene.OpenGroup(fit.X0, fit.Y0);

                for (var p = 0; p < s.Panels.Count; p++)
                {
                    var panel = s.Panels[p];
                    var (ox, oy) = PanelKit.Origin(plan, p);
                    PanelKit.Frame(w, ox, oy, plan, panel.Title);
                    var left = ox + 12;
                    var right = ox + plan.CellWidth - 12;
                    var top = oy + 30;
                    var baseline = oy + plan.CellHeight - 26;
                    var min = panel.Series.Min(x => x.X[0]);
                    var max = panel.Series.Max(x => x.X[^1]);
                    var peak = panel.Series.Max(x => x.Y.Max());
                    var xs = new LinearScale(min, max, left, right);
                    var ys = new LinearScale(0, peak > 0 ? peak * 1.08 : 1, baseline, top);

                    for (var i = 0; i < panel.Series.Count; i++)
                    {
                        var series = panel.Series[i];
                        w.Open("g", ("class", "sr s" + i));

                        if (series.Bars)
                        {
                            for (var b = 0; b < series.Y.Count; b++)
                            {
                                var x0 = xs.Map(series.X[b]);
                                var x1 = xs.Map(series.X[b + 1]);

                                w.Empty("rect", ("class", "db"), ("x", Num.F(x0)), ("y", Num.F(ys.Map(series.Y[b]))),
                                    ("width", Num.F(Math.Max(0.5, x1 - x0 - 0.6))),
                                    ("height", Num.F(Math.Max(0, baseline - ys.Map(series.Y[b])))));
                            }
                        }
                        else
                        {
                            var line = new PathBuilder();
                            var area = new PathBuilder().Move(xs.Map(series.X[0]), baseline);

                            for (var k = 0; k < series.X.Count; k++)
                            {
                                line.Line(xs.Map(series.X[k]), ys.Map(series.Y[k]));
                                area.Line(xs.Map(series.X[k]), ys.Map(series.Y[k]));
                            }

                            area.Line(xs.Map(series.X[^1]), baseline).Close();
                            w.Empty("path", ("class", "ar"), ("d", area.ToString()));
                            w.Empty("path", ("class", "ln thin"), ("d", line.ToString()));
                        }

                        w.Close();
                    }

                    PanelKit.XAxis(w, xs, left, right, baseline, 4);
                    var columns = columnsPer;
                    var width = (right - left) / columns;

                    for (var k = 0; k < columns; k++)
                    {
                        var x = min + (k + 0.5) / columns * (max - min);
                        var px = left + (k + 0.5) * width;

                        var rows = new List<TipRow>
                        {
                            new(TextMetrics.Truncate(panel.Title, 160, 12, true) + " = " + NumberFormat.Value(x), true)
                        };

                        var values = new double[panel.Series.Count];

                        for (var i = 0; i < panel.Series.Count; i++)
                        {
                            values[i] = ValueAt(panel.Series[i], x);

                            if (double.IsFinite(values[i]))
                            {
                                rows.Add(new TipRow(
                                    TextMetrics.Truncate(panel.Series[i].Name, 160, 12) + ": " +
                                    NumberFormat.Value(values[i]), false, i));
                            }
                        }

                        var key = "d" + p + "c" + k;

                        scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)),
                            scene.External ? string.Empty : "cl0");

                        w.Empty("rect", ("class", "hb"), ("x", Num.F(px - width / 2)), ("y", Num.F(top)),
                            ("width", Num.F(width)),
                            ("height", Num.F(baseline - top)));

                        w.Empty("line", ("class", "vl"), ("x1", Num.F(px)), ("y1", Num.F(top)), ("x2", Num.F(px)),
                            ("y2", Num.F(baseline)));

                        for (var i = 0; i < values.Length; i++)
                        {
                            if (double.IsFinite(values[i]))
                            {
                                w.Empty("circle", ("class", "cx s" + i), ("cx", Num.F(px)),
                                    ("cy", Num.F(ys.Map(values[i]))), ("r", "3.5"));
                            }
                        }

                        scene.EndMark(key, fit.X0 + px, fit.Y0 + (top + baseline) / 2, rows, true);
                    }
                }

                scene.CloseGroup();

                return scene.End();
            });
        }

        private const double CellHeight = 168;

        private void Validate()
        {
            var s = Spec;

            if (s.Panels.Count == 0)
            {
                throw new ChartSpecificationException("Distribution panels need at least one panel.");
            }

            if (s.Panels.Count > DiagramLimits.MaxPanels)
            {
                throw new DiagramTooLargeException("distribution panels", s.Panels.Count, DiagramLimits.MaxPanels,
                    "Show the most informative features only.");
            }

            if (s.MaxColumns < 1 || s.MaxColumns > 6)
            {
                throw new ChartSpecificationException("MaxColumns must be between 1 and 6.");
            }

            var count = s.Panels[0].Series.Count;

            if (count == 0)
            {
                throw new ChartSpecificationException("Every panel needs at least one series.");
            }

            if (count > Palette.CategoricalCount)
            {
                throw new DiagramTooLargeException("series per panel", count, Palette.CategoricalCount,
                    "Group the minor series before drawing.");
            }

            foreach (var panel in s.Panels)
            {
                if (panel.Series.Count != count)
                {
                    throw new ChartSpecificationException("Every panel must have the same series, in the same order.");
                }

                foreach (var series in panel.Series)
                {
                    if (series.X.Count > DiagramLimits.MaxPanelPoints)
                    {
                        throw new DiagramTooLargeException("points per series", series.X.Count,
                            DiagramLimits.MaxPanelPoints,
                            "Resample the distribution.");
                    }

                    var expectedY = series.Bars ? series.X.Count - 1 : series.X.Count;

                    if (series.X.Count < 2 || series.Y.Count != expectedY)
                    {
                        throw new ChartSpecificationException(series.Bars
                            ? "A histogram series needs one more edge than heights and at least 2 edges."
                            : "A density series needs the same number of x and y values and at least 2 points.");
                    }

                    for (var i = 0; i < series.X.Count; i++)
                    {
                        if (!double.IsFinite(series.X[i]) || (i > 0 && series.X[i] <= series.X[i - 1]))
                        {
                            throw new ChartSpecificationException(
                                "Distribution x values must be finite and strictly increasing.");
                        }
                    }

                    if (series.Y.Any(y => !double.IsFinite(y) || y < 0))
                    {
                        throw new ChartSpecificationException("Distribution heights must be finite and not negative.");
                    }
                }
            }
        }

        private static double ValueAt(DistributionSeries s, double x)
        {
            if (x < s.X[0] || x > s.X[^1])
            {
                return double.NaN;
            }

            if (s.Bars)
            {
                for (var i = 0; i < s.Y.Count; i++)
                {
                    if (x <= s.X[i + 1])
                    {
                        return s.Y[i];
                    }
                }

                return s.Y[^1];
            }

            for (var i = 1; i < s.X.Count; i++)
            {
                if (x <= s.X[i])
                {
                    var t = (x - s.X[i - 1]) / (s.X[i] - s.X[i - 1]);

                    return s.Y[i - 1] + t * (s.Y[i] - s.Y[i - 1]);
                }
            }

            return s.Y[^1];
        }
    }
}