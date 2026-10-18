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
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Diagrams
{
    internal static class GraphRenderer
    {
        public static string Render(GraphDiagramSpec spec, GraphLayoutResult layout)
        {
            var roles = spec.Nodes.Select(n => n.Role).Distinct().OrderBy(r => (int)r).ToArray();
            var legend = roles.Select(r => new LegendEntry(RoleName(r), Series(r))).ToArray();
            var hasProbability = spec.Edges.Any(e => e.Probability.HasValue);
            var weights = spec.Edges.Where(e => e.Weight.HasValue).Select(e => e.Weight!.Value).ToArray();
            var hasWeight = weights.Length > 0;
            var signed = weights.Any(w => w < 0);
            var maxAbs = hasWeight ? Math.Max(weights.Max(Math.Abs), 1e-12) : 0;

            var summary = (layout.Kind == GraphLayoutKind.Circular ? "State graph" : "Graph") + " diagram with " +
                          spec.Nodes.Count + " nodes and " + spec.Edges.Count + " " +
                          (spec.Directed ? "directed " : string.Empty) + "edges" +
                          (layout.Kind == GraphLayoutKind.Layered
                              ? " in " + layout.LayerCount + " levels"
                              : string.Empty) +
                          (hasProbability ? "; edge thickness shows probability" :
                              hasWeight ? "; edge thickness shows " +
                                          TextMetrics.Truncate(spec.WeightName, 60, 12).ToLowerInvariant() :
                              string.Empty) +
                          ".";

            var keyText = new List<string>();

            if (hasProbability)
            {
                keyText.Add("thickness = probability");
            }

            if (hasWeight && !signed)
            {
                keyText.Add("thickness = " + TextMetrics.Truncate(spec.WeightName, 260, 10.5).ToLowerInvariant());
            }

            var keyLine = string.Join("; ", keyText);

            var keyWidth = signed
                ? Diverge.KeyWidth(maxAbs, TextMetrics.Truncate(spec.WeightName, 90, 11)) + 150
                : TextMetrics.Width(keyLine, 10.5);

            var bottom = signed || keyLine.Length > 0 ? 26.0 : 0;

            var fit = DiagramFrame.Fit(spec.Options, summary, legend, Math.Max(layout.Width, keyWidth), layout.Height,
                bottom, "graph");

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, legend, layout.Nodes.Count + layout.Edges.Count);
                var w = scene.W;
                var ox = fit.X0 + (Math.Max(layout.Width, keyWidth) - layout.Width) / 2;
                var oy = fit.Y0;
                var n = layout.Nodes.Count;
                var neighbours = new HashSet<int>[n];
                var incoming = new int[n];
                var outgoing = new int[n];

                for (var i = 0; i < n; i++)
                {
                    neighbours[i] = [];
                }

                foreach (var e in layout.Edges)
                {
                    if (e.From != e.To)
                    {
                        neighbours[e.From].Add(e.To);
                        neighbours[e.To].Add(e.From);
                    }

                    outgoing[e.From]++;
                    incoming[e.To]++;
                }

                var showLabels = spec.Edges.Count <= spec.EdgeLabelLimit;
                var placedLabels = new List<(double X0, double X1, double Y0, double Y1)>();

                foreach (var p in layout.Nodes)
                {
                    placedLabels.Add((p.X - p.Width / 2, p.X + p.Width / 2, p.Y - p.Height / 2, p.Y + p.Height / 2));
                }

                scene.OpenGroup(ox, oy);

                foreach (var e in layout.Edges)
                {
                    var src = spec.Edges[e.Index];
                    var a = layout.Nodes[e.From];
                    var b = layout.Nodes[e.To];

                    var value = src.Probability.HasValue ? "Probability: " + NumberFormat.Value(src.Probability.Value)
                        : src.Weight.HasValue ? TextMetrics.Truncate(spec.WeightName, 80, 12) + ": " +
                                                NumberFormat.Value(src.Weight.Value) : null;

                    var rows = new List<TipRow>
                    {
                        new(
                            TextMetrics.Truncate(a.Label, 100, 12) + (spec.Directed ? " to " : " and ") +
                            TextMetrics.Truncate(b.Label, 100, 12), true)
                    };

                    if (value is not null)
                    {
                        rows.Add(new TipRow(value));
                    }

                    if (src is { Weight: not null, Probability: not null })
                    {
                        rows.Add(new TipRow(TextMetrics.Truncate(spec.WeightName, 80, 12) + ": " +
                                            NumberFormat.Value(src.Weight.Value)));
                    }

                    if (!string.IsNullOrWhiteSpace(src.Label))
                    {
                        rows.Add(new TipRow(TextMetrics.Truncate(src.Label, 240, 12)));
                    }

                    var thickness = src.Probability.HasValue ? 1 + 4.5 * src.Probability.Value
                        : src.Weight.HasValue ? Diverge.Width(src.Weight.Value, maxAbs, 0.9, 4.5) : 1.6;

                    var cls = "ep";

                    if (src.Weight.HasValue && signed && !src.Probability.HasValue)
                    {
                        cls += " sd" + Diverge.Bin(src.Weight.Value, maxAbs);
                    }
                    else
                    {
                        cls += " et";
                    }

                    if (src.Dashed)
                    {
                        cls += " ed";
                    }

                    var key = "x" + e.Index;

                    scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)),
                        "eg " + StyleCaps.Tokens(scene.External, "e", [e.From, e.To]));

                    w.Empty("path", ("class", "eh"), ("d", e.Path));
                    w.Empty("path", ("class", cls), ("d", e.Path), scene.StrokeWidth(thickness));

                    if (spec.Directed)
                    {
                        Arrow(w, e, thickness);
                    }

                    var text = !string.IsNullOrWhiteSpace(src.Label) ? src.Label
                        : src.Probability.HasValue ? NumberFormat.Value(src.Probability.Value)
                        : src.Weight.HasValue ? NumberFormat.Value(src.Weight.Value) : null;

                    if (showLabels && text is not null)
                    {
                        var shown = TextMetrics.Truncate(text, 90, 11);
                        var half = TextMetrics.Width(shown, 11) / 2 + 2;
                        var ly = e.LabelY;

                        foreach (var nudge in new[] { 0.0, -14, 14, -28, 28 })
                        {
                            var clash = placedLabels.Any(r => e.LabelX - half < r.X1 && e.LabelX + half > r.X0 &&
                                                              e.LabelY + nudge - 11 < r.Y1 &&
                                                              e.LabelY + nudge + 3 > r.Y0);

                            if (!clash)
                            {
                                ly = e.LabelY + nudge;
                                break;
                            }
                        }

                        placedLabels.Add((e.LabelX - half, e.LabelX + half, ly - 11, ly + 3));
                        w.Element("text", shown, ("class", "ant ctr"), ("x", Num.F(e.LabelX)), ("y", Num.F(ly)));
                    }

                    scene.EndMark(key, ox + e.LabelX, oy + e.LabelY, rows, true);
                }

                foreach (var p in layout.Nodes)
                {
                    var src = spec.Nodes[p.Index];

                    var rows = new List<TipRow>
                        { new(TextMetrics.Truncate(p.Label, 240, 12, true), true), new(RoleName(p.Role)) };

                    if (!string.IsNullOrWhiteSpace(src.Detail))
                    {
                        rows.Add(new TipRow(TextMetrics.Truncate(src.Detail, 240, 12)));
                    }

                    rows.Add(new TipRow("Edges in: " + incoming[p.Index] + ", out: " + outgoing[p.Index]));
                    var key = "n" + p.Index;

                    scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)),
                        "nd s" + Series(p.Role) + " " +
                        StyleCaps.Tokens(scene.External, "j", neighbours[p.Index].OrderBy(j => j)));

                    Shape(w, p, "nb");
                    Shape(w, p, "nt");

                    w.Element("text", TextMetrics.Truncate(p.Label, p.Width - 12, 11, true), ("class", "lbl ctr mid"),
                        ("x", Num.F(p.X)), ("y", Num.F(p.Y)));

                    scene.EndMark(key, ox + p.X + p.Width / 2, oy + p.Y, rows, true);

                    if (scene.Interactive)
                    {
                        scene.Rules(DiagramRules.NodeHover(scene.K, p.Index));
                    }
                }

                scene.CloseGroup();
                var keyY = fit.Y0 + layout.Height + 8;

                if (signed)
                {
                    var name = TextMetrics.Truncate(spec.WeightName, 90, 11);
                    Diverge.Key(scene, fit.X0, keyY, maxAbs, name);

                    w.Element("text", "thickness = magnitude", ("class", "kn"),
                        ("x", Num.F(fit.X0 + Diverge.KeyWidth(maxAbs, name) + 10)), ("y", Num.F(keyY + 11)));
                }
                else if (keyLine.Length > 0)
                {
                    w.Element("text", keyLine, ("class", "kn"), ("x", Num.F(fit.X0)), ("y", Num.F(keyY + 11)));
                }

                return scene.End();
            });
        }

        private static int Series(GraphNodeRole role)
        {
            return role switch
            {
                GraphNodeRole.Class => 0, GraphNodeRole.Feature => 2, GraphNodeRole.Latent => 4,
                GraphNodeRole.State => 3,
                _ => 7
            };
        }

        private static string RoleName(GraphNodeRole role)
        {
            return role switch
            {
                GraphNodeRole.Class => "Class", GraphNodeRole.Feature => "Feature",
                GraphNodeRole.Latent => "Latent variable",
                GraphNodeRole.State => "State", _ => "Other"
            };
        }

        private static void Arrow(SvgWriter w, PlacedGraphEdge e, double thickness)
        {
            var length = 8 + 1.2 * thickness;
            var half = 3.8 + 0.6 * thickness;
            var bx = e.TipX - e.DirX * length;
            var by = e.TipY - e.DirY * length;
            var px = -e.DirY * half;
            var py = e.DirX * half;
            var path = new PathBuilder().Move(e.TipX, e.TipY).Line(bx + px, by + py).Line(bx - px, by - py).Close();
            w.Empty("path", ("class", "ah"), ("d", path.ToString()));
        }

        private static void Shape(SvgWriter w, PlacedGraphNode p, string cls)
        {
            var dash = p.Role == GraphNodeRole.Latent ? " dash" : string.Empty;

            if (p.Role == GraphNodeRole.State)
            {
                w.Empty("circle", ("class", cls), ("cx", Num.F(p.X)), ("cy", Num.F(p.Y)), ("r", Num.F(p.Width / 2)));

                return;
            }

            var rx = p.Role switch
            {
                GraphNodeRole.Class => 6.0,
                GraphNodeRole.Other => 3.0,
                _ => p.Height / 2
            };

            w.Empty("rect", ("class", cls + dash), ("x", Num.F(p.X - p.Width / 2)), ("y", Num.F(p.Y - p.Height / 2)),
                ("width", Num.F(p.Width)), ("height", Num.F(p.Height)), ("rx", Num.F(rx)));
        }
    }
}