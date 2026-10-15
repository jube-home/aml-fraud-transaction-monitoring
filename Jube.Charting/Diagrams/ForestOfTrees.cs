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
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Ticks;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams
{
    public sealed class ForestOfTrees(ForestOfTreesSpec spec) : IStyleModeChart
    {
        private const double CellWidth = 300;
        private const double TitleHeight = 24;

        public ForestOfTreesSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ForestOfTrees(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var layouts = Layouts();
            var shown = layouts.Count;
            var total = Math.Max(s.TotalTrees, s.Trees.Count);

            var importances = s.Importances.Where(i => double.IsFinite(i.Value) && i.Value >= 0)
                .OrderByDescending(i => i.Value).Take(s.MaxImportances).ToArray();

            if (s.Importances.Any(i => !double.IsFinite(i.Value) || i.Value < 0))
            {
                throw new ChartSpecificationException("Feature importances must be finite and not negative.");
            }

            var cols = shown <= 2 ? shown : shown <= 4 ? 2 : 3;
            var rows = (shown + cols - 1) / cols;
            var scales = layouts.Select(l => Math.Min(1.0, (CellWidth - 8) / Math.Max(l.Width, 1))).ToArray();

            var cellHeight =
                layouts
                    .Select((l, i) =>
                        l.Height * scales[i] + (l.Nodes.Any(n => n.Kind == TreeNodeKind.Collapsed) ? 4 : 0))
                    .Max() + TitleHeight + 14;

            var gridWidth = cols * CellWidth + (cols - 1) * 16;
            var gridHeight = rows * cellHeight + (rows - 1) * 8;
            var barRow = 22.0;

            var labelW = importances.Length > 0
                ? Math.Min(TextMetrics.MaxWidth(importances.Select(i => i.Name), 12), 200)
                : 0;

            var importanceHeight = importances.Length > 0 ? 30 + importances.Length * barRow : 0;
            var contentW = Math.Max(gridWidth, importances.Length > 0 ? Math.Min(gridWidth, 640) : 0);
            var contentH = gridHeight + (importances.Length > 0 ? 26 + importanceHeight : 0);
            var legend = s.ClassNames.Select((n, i) => new LegendEntry(n, i)).ToArray();

            var summary = "Forest of " + total + " trees; the first " + shown + " are drawn" +
                          (importances.Length > 0
                              ? " with a feature importance strip led by " +
                                TextMetrics.Truncate(importances[0].Name, 80, 12)
                              : string.Empty) + ".";

            var fit = DiagramFrame.Fit(s.Options, summary, legend, contentW, contentH, 0, "forest");
            var marks = layouts.Sum(l => l.Nodes.Count + l.Edges.Count) + importances.Length;

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, legend, marks);
                var w = scene.W;
                var nodeBase = 0;
                var edgeBase = 0;
                var ox = fit.X0 + (Math.Max(contentW, gridWidth) - gridWidth) / 2;

                for (var t = 0; t < shown; t++)
                {
                    var col = t % cols;
                    var row = t / cols;
                    var x = ox + col * (CellWidth + 16);
                    var y = fit.Y0 + row * (cellHeight + 8);
                    var layout = layouts[t];
                    var title = "Tree " + (t + 1);

                    var detail = layout.TotalNodes + " nodes" + (layout.Nodes.Any(n => n.Kind == TreeNodeKind.Collapsed)
                        ? ", cut at depth " + layout.DepthShown
                        : string.Empty);

                    w.Empty("rect", ("class", "pf"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(CellWidth)),
                        ("height", Num.F(cellHeight)), ("rx", "6"));

                    w.Element("text", title, ("class", "ph"), ("x", Num.F(x + 10)), ("y", Num.F(y + 17)));

                    w.Element("text",
                        TextMetrics.Truncate(detail, CellWidth - 30 - TextMetrics.Width(title, 12, true), 10.5),
                        ("class", "kn end"),
                        ("x", Num.F(x + CellWidth - 10)), ("y", Num.F(y + 17)));

                    var scale = scales[t];
                    var tx = x + (CellWidth - layout.Width * scale) / 2;

                    TreeRenderer.Draw(scene,
                        s.Trees[t] with { ClassNames = s.ClassNames.Count > 0 ? s.ClassNames : s.Trees[t].ClassNames },
                        layout, tx, y + TitleHeight + 6, t,
                        ref nodeBase, ref edgeBase, scale);
                }

                if (importances.Length > 0)
                {
                    var y0 = fit.Y0 + gridHeight + 26;
                    var max = importances.Max(i => i.Value);
                    var barX = ox + labelW + 12;
                    var barW = Math.Max(160, Math.Min(gridWidth, 640) - labelW - 12 - 70);

                    w.Element("text", s.ImportanceName + " (first " + shown + " of " + total + " trees drawn)",
                        ("class", "ph"), ("x", Num.F(ox)), ("y", Num.F(y0 + 12)));

                    var sum = importances.Sum(i => i.Value);

                    for (var i = 0; i < importances.Length; i++)
                    {
                        var item = importances[i];
                        var y = y0 + 26 + i * barRow;
                        var length = max > 0 ? Math.Max(1, barW * item.Value / max) : 1;

                        var rows2 = new List<TipRow>
                        {
                            new(TextMetrics.Truncate(item.Name, 240, 12, true), true),
                            new(s.ImportanceName + ": " + NumberFormat.Value(item.Value)),
                            new("Rank " + (i + 1) + (sum > 0
                                ? ", " + NumberFormat.Percent(item.Value / sum) + " of shown"
                                : string.Empty))
                        };

                        var key = "f" + i;
                        scene.OpenMark(key, string.Join(", ", rows2.Select(r => r.Text)), "rw s0");

                        w.Empty("rect", ("class", "hl"), ("x", Num.F(ox)), ("y", Num.F(y)),
                            ("width", Num.F(labelW + 12 + barW + 60)), ("height", Num.F(barRow)));

                        w.Element("text", TextMetrics.Truncate(item.Name, labelW, 12), ("class", "end mid"),
                            ("x", Num.F(ox + labelW)), ("y", Num.F(y + barRow / 2)));

                        w.Empty("rect", ("class", "rb"), ("x", Num.F(barX)), ("y", Num.F(y + 4)),
                            ("width", Num.F(length)),
                            ("height", Num.F(barRow - 8)), ("rx", "2"));

                        w.Element("text", NumberFormat.Value(item.Value), ("class", "tk mid"),
                            ("x", Num.F(barX + length + 6)), ("y", Num.F(y + barRow / 2)));

                        scene.EndMark(key, barX + length, y + barRow / 2, rows2, true);
                    }

                    if (scene.Interactive)
                    {
                        scene.Rules(DiagramRules.RowLink(scene.K));
                        scene.Rules(DiagramRules.RowTransition(scene.K));
                    }
                }

                return scene.End();
            });
        }

        public IReadOnlyList<TreeLayoutResult> Layouts()
        {
            var s = Spec;

            if (s.Trees.Count == 0)
            {
                throw new ChartSpecificationException("A forest diagram needs at least one tree.");
            }

            if (s.MaxTrees < 1 || s.MaxTrees > DiagramLimits.MaxForestTrees)
            {
                throw new ChartSpecificationException(
                    "MaxTrees must be between 1 and " + DiagramLimits.MaxForestTrees + ".");
            }

            if (s.TreeDepth < 1 || s.TreeDepth > 6)
            {
                throw new ChartSpecificationException("Forest TreeDepth must be between 1 and 6.");
            }

            if (s.ClassNames.Count > 8)
            {
                throw new DiagramNotSupportedException("a forest with more than 8 classes cannot be coloured legibly.");
            }

            return
            [
                .. s.Trees.Take(s.MaxTrees).Select(t => TreeLayout.Compute(
                    t with
                    {
                        MaxDepth = s.TreeDepth, ClassNames = s.ClassNames.Count > 0 ? s.ClassNames : t.ClassNames
                    },
                    true))
            ];
        }
    }
}