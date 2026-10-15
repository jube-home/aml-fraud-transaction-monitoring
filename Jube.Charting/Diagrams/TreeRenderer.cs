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
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    internal static class TreeRenderer
    {
        public static string Render(TreeDiagramSpec spec, TreeLayoutResult layout)
        {
            var legend = spec.ClassNames.Select((n, i) => new LegendEntry(n, i)).ToArray();
            var collapsed = layout.Nodes.Where(n => n.Kind == TreeNodeKind.Collapsed).ToArray();
            var leaves = layout.Nodes.Count(n => n.Kind == TreeNodeKind.Leaf);

            var summary = "Decision tree with " + layout.Nodes.Count + " drawn nodes and " + leaves + " leaves" +
                          (collapsed.Length > 0
                              ? "; " + collapsed.Length + " subtrees are collapsed below depth " + layout.DepthShown
                              : string.Empty) + (spec.ClassNames.Count > 0
                              ? ". Classes: " +
                                string.Join(", ", spec.ClassNames.Select(c => TextMetrics.Truncate(c, 80, 12))) + "."
                              : ".");

            var hasFractions = layout.Edges.Any(e => double.IsFinite(e.Fraction));
            var note = new List<string>();

            if (hasFractions)
            {
                note.Add("edge width shows the share of samples");
            }

            if (collapsed.Length > 0)
            {
                note.Add(collapsed.Sum(c => c.HiddenNodes) + " nodes collapsed into " + collapsed.Length +
                         " triangles");
            }

            var noteText = string.Join("; ", note);
            var bottom = noteText.Length > 0 ? 24.0 : 0;

            var fit = DiagramFrame.Fit(spec.Options, summary, legend, layout.Width,
                layout.Height + (collapsed.Length > 0 ? 14 : 0), bottom, "tree");

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, legend, layout.Nodes.Count + layout.Edges.Count);
                var nodeBase = 0;
                var edgeBase = 0;
                Draw(scene, spec, layout, fit.X0, fit.Y0, 0, ref nodeBase, ref edgeBase);

                if (noteText.Length > 0)
                {
                    scene.W.Element("text", noteText, ("class", "kn"), ("x", Num.F(fit.X0)),
                        ("y", Num.F(fit.Y0 + layout.Height + (collapsed.Length > 0 ? 14 : 0) + 18)));
                }

                return scene.End();
            });
        }

        public static void Draw(Scene scene, TreeDiagramSpec spec, TreeLayoutResult layout, double ox, double oy,
            int tree,
            ref int nodeBase, ref int edgeBase, double scale = 1)
        {
            var ext = scene.External;
            var nb = ext ? 0 : nodeBase;
            var eb = ext ? 0 : edgeBase;
            var w = scene.W;
            var n = layout.Nodes.Count;
            var compact = layout.Compact;
            var horizontal = layout.Orientation == TreeOrientation.LeftRight;
            var children = new List<int>[n];

            for (var i = 0; i < n; i++)
            {
                children[i] = [];
            }

            foreach (var e in layout.Edges)
            {
                children[e.From].Add(e.To);
            }

            var descendants = new List<int>[n];

            for (var i = n - 1; i >= 0; i--)
            {
                var list = new List<int>();

                foreach (var c in children[i])
                {
                    list.Add(c);
                    list.AddRange(descendants[c]);
                }

                descendants[i] = list;
            }

            var values = layout.Nodes.Where(m => m.Source is { Value: not null, ClassCounts: not { Count: > 0 } })
                .Select(m => m.Source.Value!.Value).Where(double.IsFinite).ToArray();

            (double Min, double Max)? valueRange = values.Length > 0 ? (values.Min(), values.Max()) : null;
            var tr = "tr" + tree;
            var rootSamples = spec.Root!.Samples;
            scene.OpenGroup(ox, oy, scale, scene.External ? "tree" : null);

            for (var k = 0; k < layout.Edges.Count; k++)
            {
                var e = layout.Edges[k];
                var a = layout.Nodes[e.From];
                var b = layout.Nodes[e.To];
                double x1, y1, x2, y2;
                string d;

                if (horizontal)
                {
                    x1 = a.X + a.Width / 2;
                    y1 = a.Y;
                    x2 = b.X - b.Width / 2;
                    y2 = b.Y;
                    var xm = (x1 + x2) / 2;
                    d = new PathBuilder().Move(x1, y1).Cubic(xm, y1, xm, y2, x2, y2).ToString();
                }
                else
                {
                    x1 = a.X;
                    y1 = a.Y + a.Height / 2;
                    x2 = b.X;
                    y2 = b.Y - b.Height / 2;
                    var ym = (y1 + y2) / 2;
                    d = new PathBuilder().Move(x1, y1).Cubic(x1, ym, x2, ym, x2, y2).ToString();
                }

                var width = double.IsFinite(e.Fraction)
                    ? compact ? 0.8 + 2.8 * e.Fraction : 1.2 + 4.5 * e.Fraction
                    : compact
                        ? 1.2
                        : 1.8;

                var rows = new List<TipRow>();
                var condition = TreeText.Condition(a.Source);

                rows.Add(new TipRow(
                    TextMetrics.Truncate(condition, 240, 12) +
                    (string.IsNullOrEmpty(e.Label) ? string.Empty : ": " + e.Label), true));

                if (b.Source.Samples is { } samples)
                {
                    rows.Add(new TipRow("Samples: " + NumberFormat.Value(samples) +
                                        (rootSamples is > 0
                                            ? " (" + NumberFormat.Percent(samples / rootSamples.Value) + " of root)"
                                            : string.Empty)));
                }

                AddDistribution(rows, b.Source, spec, 3);
                var key = (ext && tree > 0 ? "t" + tree : string.Empty) + "x" + (eb + k);
                var subtree = StyleCaps.Tokens(ext, "a", new[] { e.To }.Concat(descendants[e.To]).Select(m => nb + m));

                scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)),
                    (ext
                        ? "eg " + StyleCaps.Tokens(true, "e", [e.From, e.To])
                        : "eg " + tr + " e" + (nb + e.From) + " e" + (nb + e.To)) + " " + subtree);

                w.Empty("path", ("class", "eh"), ("d", d));
                w.Empty("path", ("class", "ep et"), ("d", d), scene.StrokeWidth(width));

                if (!compact && !string.IsNullOrEmpty(e.Label))
                {
                    var p = NetworkRenderer.Bezier(x1, y1, horizontal ? (x1 + x2) / 2 : x1,
                        horizontal ? y1 : (y1 + y2) / 2,
                        horizontal ? (x1 + x2) / 2 : x2, horizontal ? y2 : (y1 + y2) / 2, x2, y2, 0.7);

                    w.Element("text", TextMetrics.Truncate(e.Label, 70, 11), ("class", "ant ctr"), ("x", Num.F(p.X)),
                        ("y", Num.F(p.Y - (horizontal ? 5 : 2))));
                }

                scene.EndMark(key, ox + scale * (x1 + x2) / 2, oy + scale * (y1 + y2) / 2, rows, true);
            }

            for (var i = 0; i < n; i++)
            {
                var p = layout.Nodes[i];
                var src = p.Source;
                var rows = new List<TipRow>();
                var title = p.Kind == TreeNodeKind.Leaf ? TreeText.Prediction(src, spec) : TreeText.Condition(src);
                rows.Add(new TipRow(TextMetrics.Truncate(title, 240, 12, true), true));

                rows.Add(new TipRow(
                    (p.Kind == TreeNodeKind.Leaf ? "Leaf" :
                        p.Kind == TreeNodeKind.Collapsed ? "Collapsed subtree" : "Split") + " at depth " + p.Depth));

                if (src.Samples is { } s)
                {
                    rows.Add(new TipRow("Samples: " + NumberFormat.Value(s) + (rootSamples is > 0
                        ? " (" + NumberFormat.Percent(s / rootSamples.Value) + " of root)"
                        : string.Empty)));
                }

                if (src.Impurity is { } imp)
                {
                    rows.Add(new TipRow("Impurity (" + TextMetrics.Truncate(spec.ImpurityName, 80, 12) + "): " +
                                        NumberFormat.Value(imp)));
                }

                if (src.Value is { } val)
                {
                    rows.Add(new TipRow(TextMetrics.Truncate(spec.ValueName, 80, 12) + ": " + NumberFormat.Value(val)));
                }

                AddDistribution(rows, src, spec, 8);

                if (p.Kind == TreeNodeKind.Collapsed)
                {
                    rows.Add(new TipRow(p.HiddenNodes + " nodes, " + p.HiddenLeaves + " leaves in this subtree"));
                }

                var key = (ext && tree > 0 ? "t" + tree : string.Empty) + "n" + (nb + i);
                var g = nb + i;
                var neighbour = new List<int>();

                if (p.Parent >= 0)
                {
                    neighbour.Add(p.Parent);
                }

                neighbour.AddRange(children[i]);

                var classes = "nd " + (ext ? string.Empty : tr + " ") + "s" + TreeText.Majority(src) % 8 + " " +
                              StyleCaps.Tokens(ext, "j", neighbour.Select(m => nb + m)) + " " +
                              StyleCaps.Tokens(ext, "a", descendants[i].Select(m => nb + m));

                scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)), classes.Trim());
                DrawNode(w, p, spec, compact, horizontal, valueRange);
                scene.EndMark(key, ox + scale * (p.X + p.Width / 2), oy + scale * p.Y, rows, true);

                if (scene.Interactive && !ext)
                {
                    scene.Rules(DiagramRules.TreeHover(scene.K, g, tr));
                }
            }

            scene.CloseGroup();
            nodeBase += n;
            edgeBase += layout.Edges.Count;
        }

        private static void AddDistribution(List<TipRow> rows, TreeNode node, TreeDiagramSpec spec, int max)
        {
            if (node.ClassCounts is not { Count: > 0 })
            {
                return;
            }

            var total = node.ClassCounts.Sum();

            var order = Enumerable.Range(0, node.ClassCounts.Count).OrderByDescending(i => node.ClassCounts[i])
                .ThenBy(i => i)
                .Take(max).ToArray();

            foreach (var i in order)
            {
                var name = i < spec.ClassNames.Count ? spec.ClassNames[i] : "class " + i;

                rows.Add(new TipRow(TextMetrics.Truncate(name, 160, 12) + ": " +
                                    NumberFormat.Value(node.ClassCounts[i]) +
                                    (total > 0
                                        ? " (" + NumberFormat.Percent(node.ClassCounts[i] / total) + ")"
                                        : string.Empty), false, i % 8));
            }
        }

        private static void DrawNode(SvgWriter w, PlacedTreeNode p, TreeDiagramSpec spec, bool compact, bool horizontal,
            (double Min, double Max)? valueRange)
        {
            var x = p.X - p.Width / 2;
            var y = p.Y - p.Height / 2;

            if (p.Kind == TreeNodeKind.Collapsed)
            {
                var tri = horizontal
                    ? new PathBuilder().Move(x, p.Y).Line(x + p.Width, y).Line(x + p.Width, y + p.Height).Close()
                    : new PathBuilder().Move(p.X, y).Line(x + p.Width, y + p.Height).Line(x, y + p.Height).Close();

                w.Empty("path", ("class", "tri"), ("d", tri.ToString()));

                if (!compact)
                {
                    w.Element("text", "+" + p.HiddenNodes, ("class", "lbl ctr mid"),
                        ("x", Num.F(p.X + (horizontal ? p.Width / 6 : 0))),
                        ("y", Num.F(p.Y + (horizontal ? 0 : 8))));

                    if (p.Source.Samples is { } samples)
                    {
                        w.Element("text", "n = " + NumberFormat.Value(samples), ("class", "kn ctr"), ("x", Num.F(p.X)),
                            ("y", Num.F(y + p.Height + 12)));
                    }
                }

                return;
            }

            if (compact)
            {
                if (p.Kind == TreeNodeKind.Split)
                {
                    w.Empty("circle", ("class", "nb"), ("cx", Num.F(p.X)), ("cy", Num.F(p.Y)),
                        ("r", Num.F(p.Width / 2)));
                    w.Empty("circle", ("class", "nt"), ("cx", Num.F(p.X)), ("cy", Num.F(p.Y)),
                        ("r", Num.F(p.Width / 2)));
                }
                else
                {
                    w.Empty("rect", ("class", "nb"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(p.Width)),
                        ("height", Num.F(p.Height)), ("rx", "3"));
                    w.Empty("rect", ("class", "nt lf"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(p.Width)),
                        ("height", Num.F(p.Height)), ("rx", "3"));
                }

                return;
            }

            var rx = p.Kind == TreeNodeKind.Leaf ? "16" : "6";

            w.Empty("rect", ("class", "nb"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(p.Width)),
                ("height", Num.F(p.Height)), ("rx", rx));

            w.Empty("rect", ("class", p.Kind == TreeNodeKind.Leaf ? "nt lf" : "nt"), ("x", Num.F(x)), ("y", Num.F(y)),
                ("width", Num.F(p.Width)), ("height", Num.F(p.Height)), ("rx", rx));

            var line1 = p.Kind == TreeNodeKind.Leaf
                ? TreeText.Prediction(p.Source, spec)
                : TreeText.Condition(p.Source);

            var line2 = TreeText.Stats(p.Source, spec);
            var hasBar = p.Source.ClassCounts is { Count: > 0 } && p.Source.ClassCounts.Sum() > 0;
            var textTop = y + (hasBar || (valueRange is not null && p.Source.Value.HasValue) ? 15 : 19);

            w.Element("text", TextMetrics.Truncate(line1, p.Width - 14, 11, true), ("class", "lbl ctr"),
                ("x", Num.F(p.X)),
                ("y", Num.F(textTop)));

            if (line2.Length > 0)
            {
                w.Element("text", TextMetrics.Truncate(line2, p.Width - 14, 10.5), ("class", "kn ctr"),
                    ("x", Num.F(p.X)),
                    ("y", Num.F(textTop + 14)));
            }

            if (valueRange is { } range && !hasBar && p.Source.Value is { } value && double.IsFinite(value))
            {
                var track = p.Width - 20;
                var fraction = range.Max > range.Min ? (value - range.Min) / (range.Max - range.Min) : 0.5;

                w.Empty("rect", ("class", "vt"), ("x", Num.F(x + 10)), ("y", Num.F(y + p.Height - 9)),
                    ("width", Num.F(track)),
                    ("height", "4"));

                w.Empty("rect", ("class", "cl s0"), ("x", Num.F(x + 10)), ("y", Num.F(y + p.Height - 9)),
                    ("width", Num.F(Math.Max(2, track * fraction))), ("height", "4"));
            }

            if (hasBar)
            {
                var counts = p.Source.ClassCounts!;
                var total = counts.Sum();
                var barWidth = p.Width - 20;
                var cursor = x + 10;

                for (var i = 0; i < counts.Count; i++)
                {
                    var segment = barWidth * counts[i] / total;

                    if (segment < 0.4)
                    {
                        continue;
                    }

                    w.Empty("rect", ("class", "cl s" + i % 8), ("x", Num.F(cursor)), ("y", Num.F(y + p.Height - 9)),
                        ("width", Num.F(segment)), ("height", "4"));

                    cursor += segment;
                }
            }
        }
    }
}