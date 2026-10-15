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
    internal static class NetworkRenderer
    {
        public static string Render(NetworkDiagramSpec spec, NetworkLayoutResult layout)
        {
            var layers = spec.Layers;
            var weighted = layout.Edges.Any(e => double.IsFinite(e.Weight));
            var maxAbs = weighted ? layout.Edges.Where(e => double.IsFinite(e.Weight)).Max(e => Math.Abs(e.Weight)) : 0;
            var hiddenNodes = layout.Nodes.Where(n => n.IsAggregate).Sum(n => n.HiddenCount);
            var note = string.Empty;

            if (hiddenNodes > 0 || layout.DrawnEdgesBeforeElision > layout.Edges.Count)
            {
                var parts = new List<string>();

                if (hiddenNodes > 0)
                {
                    parts.Add(hiddenNodes + " nodes aggregated");
                }

                if (layout.DrawnEdgesBeforeElision > layout.Edges.Count)
                {
                    parts.Add("strongest " + layout.Edges.Count + " of " + layout.DrawnEdgesBeforeElision +
                              " connections shown");
                }

                note = string.Join("; ", parts);
            }

            var roles = layout.Nodes.Select(n => n.Role).Distinct().OrderBy(r => (int)r).ToArray();
            var legend = roles.Select(r => new LegendEntry(RoleName(r), Series(r))).ToArray();
            var counts = string.Join(", ", layers.Select(l => l.Nodes.Count));

            var summary = "Neural network diagram with " + layers.Count + " layers of " + counts + " nodes and " +
                          layout.TotalEdges + " connections" + (weighted
                              ? "; connection thickness shows weight magnitude and colour its sign."
                              : ".") +
                          (note.Length > 0 ? " " + note + "." : string.Empty);

            var emphasised = !string.IsNullOrWhiteSpace(spec.EmphasisTitle) &&
                             layout.Nodes.Any(n => n is { IsAggregate: false, Emphasis: not null });

            var bottom = (weighted || note.Length > 0 ? 30.0 : 0) + (emphasised ? 24 : 0);
            var keyWidth = weighted ? Diverge.KeyWidth(maxAbs, "Weight") : 0;
            var contentWidth = Math.Max(layout.Width, keyWidth);

            if (emphasised)
            {
                contentWidth = Math.Max(contentWidth, EmphasisKeyWidth(spec.EmphasisTitle!));
            }

            var fit = DiagramFrame.Fit(spec.Options, summary, legend, contentWidth, layout.Height, bottom, "network");

            return ChartRun.Execute(fit.Options,
                id => Draw(spec, layout, fit, id, summary, legend, weighted, maxAbs, note, emphasised));
        }

        internal const int EmphasisBins = 9;

        private static int Series(NetworkRole role)
        {
            return role switch { NetworkRole.Input => 0, NetworkRole.Output => 1, _ => 2 };
        }

        private static string RoleName(NetworkRole role)
        {
            return role switch { NetworkRole.Input => "Input", NetworkRole.Output => "Output", _ => "Hidden" };
        }

        private static string Draw(NetworkDiagramSpec spec, NetworkLayoutResult layout, FrameFit fit, string id,
            string summary, LegendEntry[] legend, bool weighted, double maxAbs, string note, bool emphasised)
        {
            var layers = spec.Layers;
            var scene = DiagramFrame.Begin(fit, id, summary, legend, layout.Nodes.Count + layout.Edges.Count);
            var w = scene.W;
            var keyWidth = weighted ? Diverge.KeyWidth(maxAbs, "Weight") : 0;
            var ox = fit.X0 + (Math.Max(layout.Width, keyWidth) - layout.Width) / 2;
            var oy = fit.Y0;
            scene.OpenGroup(ox, oy);

            for (var l = 0; l < layers.Count; l++)
            {
                var name = layers[l].Name ?? DefaultName(spec, l);

                w.Element("text", TextMetrics.Truncate(name, 160, 12, true), ("class", "ph ctr"),
                    ("x", Num.F(layout.ColumnX[l])), ("y", "12"));

                if (!string.IsNullOrWhiteSpace(layers[l].Activation))
                {
                    w.Element("text", TextMetrics.Truncate(layers[l].Activation, 160, 10.5), ("class", "kn ctr"),
                        ("x", Num.F(layout.ColumnX[l])), ("y", "27"));
                }
            }

            var neighbours = new HashSet<int>[layout.Nodes.Count];

            for (var i = 0; i < neighbours.Length; i++)
            {
                neighbours[i] = [];
            }

            foreach (var e in layout.Edges)
            {
                neighbours[e.From].Add(e.To);
                neighbours[e.To].Add(e.From);
            }

            var incoming = new int[layout.Nodes.Count];
            var outgoing = new int[layout.Nodes.Count];

            foreach (var e in layout.Edges)
            {
                outgoing[e.From]++;
                incoming[e.To]++;
            }

            var insideFits = new bool[layers.Count];

            for (var l = 1; l < layers.Count - 1; l++)
            {
                insideFits[l] = layout.Nodes.Where(m => m.Layer == l && !m.IsAggregate)
                    .All(m => TextMetrics.Width(m.Label, 10) <= 2 * m.HalfWidth - 5);
            }

            var labelEdges = layout.Edges.Count <= spec.WeightLabelLimit && weighted;

            for (var i = 0; i < layout.Edges.Count; i++)
            {
                var e = layout.Edges[i];
                var a = layout.Nodes[e.From];
                var b = layout.Nodes[e.To];
                var x1 = a.X + a.HalfWidth;
                var x2 = b.X - b.HalfWidth;
                var xm = (x1 + x2) / 2;
                var d = new PathBuilder().Move(x1, a.Y).Cubic(xm, a.Y, xm, b.Y, x2, b.Y).ToString();
                var hasWeight = double.IsFinite(e.Weight);
                var from = a.IsAggregate ? "hidden " + a.Label + " nodes" : a.Label;
                var to = b.IsAggregate ? "hidden " + b.Label + " nodes" : b.Label;
                var rows = new List<TipRow> { new(Clip(from) + " to " + Clip(to), true) };

                if (hasWeight)
                {
                    rows.Add(new TipRow((e.IsAggregate ? "Mean weight: " : "Weight: ") + NumberFormat.Value(e.Weight)));

                    if (e.IsAggregate)
                    {
                        rows.Add(new TipRow("Averaged over " + e.Count + " connections"));
                    }

                    rows.Add(new TipRow("Colour: " + (e.Weight > 0 ? "red, a positive weight" :
                                            e.Weight < 0 ? "blue, a negative weight" : "neutral, a weight near zero") +
                                        "; thickness shows its magnitude relative to the strongest connection."));
                }

                var label = string.Join(", ", rows.Select(r => r.Text));
                var key = "x" + i;
                scene.OpenMark(key, label, "eg " + StyleCaps.Tokens(scene.External, "e", [e.From, e.To]));
                w.Empty("path", ("class", "eh"), ("d", d));
                var stroke = hasWeight ? scene.StrokeWidth(Diverge.Width(e.Weight, maxAbs)) : ("style", null);

                w.Empty("path",
                    ("class",
                        hasWeight
                            ? "ep sd" + Diverge.Bin(e.Weight, maxAbs) + (e.IsAggregate ? " ed" : string.Empty)
                            : "ep en"),
                    ("d", d), stroke);

                if (labelEdges)
                {
                    var t = 0.2 + 0.15 * (i % 5);
                    var p = Bezier(x1, a.Y, xm, a.Y, xm, b.Y, x2, b.Y, t);

                    w.Element("text", NumberFormat.Value(e.Weight), ("class", "ant ctr"), ("x", Num.F(p.X)),
                        ("y", Num.F(p.Y - 4)));
                }

                var ty = (a.Y + b.Y) / 2;
                scene.EndMark(key, ox + xm, oy + ty, rows);
            }

            foreach (var n in layout.Nodes)
            {
                var rows = new List<TipRow>();

                if (n.IsAggregate)
                {
                    rows.Add(new TipRow(n.HiddenCount + " more nodes in " + LayerName(spec, n.Layer), true));
                    rows.Add(new TipRow("Shown as one node; connections are mean weights"));
                }
                else
                {
                    rows.Add(new TipRow(Clip(n.Label), true));
                    rows.Add(new TipRow(RoleName(n.Role) + " layer: " + LayerName(spec, n.Layer)));

                    if (n.Bias is { } bias)
                    {
                        rows.Add(new TipRow("Bias: " + NumberFormat.Value(bias)));
                    }

                    if (!string.IsNullOrWhiteSpace(layers[n.Layer].Activation))
                    {
                        rows.Add(new TipRow("Activation: " + Clip(layers[n.Layer].Activation!)));
                    }

                    if (n.Layer > 0 || incoming[n.Index] > 0)
                    {
                        rows.Add(new TipRow("Connections in: " + incoming[n.Index] + ", out: " + outgoing[n.Index]));
                    }

                    if (emphasised && n.Emphasis is { } strength)
                    {
                        rows.Add(new TipRow(Clip(spec.EmphasisTitle!) + ": " +
                                            (n.EmphasisText ??
                                             NumberFormat.Percent(strength, 0) + " of the strongest")));

                        rows.Add(new TipRow("Colour: shaded darker the higher this value is, lightest for the least " +
                                            "notable nodes."));
                    }
                }

                var label = string.Join(", ", rows.Select(r => r.Text));
                var key = "n" + n.Index;
                var neighbourClasses = StyleCaps.Tokens(scene.External, "j", neighbours[n.Index].OrderBy(j => j));
                scene.OpenMark(key, label, "nd s" + Series(n.Role) + " " + neighbourClasses);
                Shape(w, n, "nb");
                Shape(w, n, emphasised && n.Emphasis is { } shade ? "nt nq" + EmphasisBin(shade) : "nt");

                if (n.IsAggregate)
                {
                    w.Element("text", n.Label, ("class", "lbl ctr mid"), ("x", Num.F(n.X)), ("y", Num.F(n.Y)));
                }
                else if (n.Layer > 0 && n.Layer < layers.Count - 1 && insideFits[n.Layer])
                {
                    w.Element("text", n.Label, ("class", "lbl ctr mid"), ("x", Num.F(n.X)), ("y", Num.F(n.Y)));
                }

                scene.EndMark(key, ox + n.X, oy + n.Y, rows, true);

                if (n is { Layer: 0, IsAggregate: false })
                {
                    w.Element("text", TextMetrics.Truncate(n.Label, layout.LeftLabelWidth, 11),
                        ("class", "lbl end mid"),
                        ("x", Num.F(n.X - n.HalfWidth - 8)), ("y", Num.F(n.Y)));
                }
                else if (n.Layer == layers.Count - 1 && !n.IsAggregate)
                {
                    w.Element("text", TextMetrics.Truncate(n.Label, layout.RightLabelWidth, 11), ("class", "lbl mid"),
                        ("x", Num.F(n.X + n.HalfWidth + 8)), ("y", Num.F(n.Y)));
                }
            }

            scene.CloseGroup();
            var keyY = fit.Y0 + layout.Height + 8;

            if (weighted)
            {
                Diverge.Key(scene, fit.X0, keyY, maxAbs, "Weight");

                w.Element("text", "thickness = magnitude", ("class", "kn"),
                    ("x", Num.F(fit.X0 + keyWidth + 10)), ("y", Num.F(keyY + 11)));
            }

            if (note.Length > 0)
            {
                w.Element("text", note, ("class", "kn"), ("x", Num.F(fit.X0)),
                    ("y", Num.F(keyY + (weighted ? 28 : 11))));
            }

            if (emphasised)
            {
                var emphasisY = keyY + (weighted ? 24 : 0) + (note.Length > 0 ? 17 : 0);
                EmphasisKey(w, fit.X0, emphasisY, spec.EmphasisTitle!);
                scene.Rules(DiagramRules.NodeEmphasis(scene.K));
            }

            AddHoverRules(scene, layout);

            return scene.End();
        }

        internal static int EmphasisBin(double value)
        {
            return Math.Clamp((int)Math.Round(Math.Clamp(value, 0, 1) * (EmphasisBins - 1)), 0, EmphasisBins - 1);
        }

        private static double EmphasisKeyWidth(string title)
        {
            return TextMetrics.Width(Clip(title), 11) + 10 + TextMetrics.Width("low", 10.5) + 6 + EmphasisBins * 16 +
                   6 +
                   TextMetrics.Width("high", 10.5);
        }

        private static void EmphasisKey(SvgWriter w, double x, double y, string title)
        {
            w.Element("text", Clip(title), ("class", "key"), ("x", Num.F(x)), ("y", Num.F(y + 11)));
            var x0 = x + TextMetrics.Width(Clip(title), 11) + 10;
            w.Element("text", "low", ("class", "kn"), ("x", Num.F(x0)), ("y", Num.F(y + 11)));
            var bar = x0 + TextMetrics.Width("low", 10.5) + 6;

            for (var i = 0; i < EmphasisBins; i++)
            {
                w.Empty("rect", ("class", "cell q" + i), ("x", Num.F(bar + i * 16)), ("y", Num.F(y)),
                    ("width", "16"), ("height", "14"));
            }

            w.Element("text", "high", ("class", "kn"), ("x", Num.F(bar + EmphasisBins * 16 + 6)), ("y", Num.F(y + 11)));
        }

        private static void AddHoverRules(Scene scene, NetworkLayoutResult layout)
        {
            if (!scene.Interactive)
            {
                return;
            }

            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                scene.Rules(DiagramRules.NodeHover(scene.K, i));
            }
        }

        private static void Shape(SvgWriter w, LaidNode n, string cls)
        {
            if (n.IsAggregate)
            {
                w.Empty("rect", ("class", cls + " dash"), ("x", Num.F(n.X - n.HalfWidth)),
                    ("y", Num.F(n.Y - n.HalfHeight)),
                    ("width", Num.F(2 * n.HalfWidth)), ("height", Num.F(2 * n.HalfHeight)),
                    ("rx", Num.F(n.HalfHeight)));
            }
            else
            {
                w.Empty("circle", ("class", cls), ("cx", Num.F(n.X)), ("cy", Num.F(n.Y)), ("r", Num.F(n.HalfWidth)));
            }
        }

        private static string Clip(string text)
        {
            return TextMetrics.Truncate(text, 240, 12);
        }

        private static string LayerName(NetworkDiagramSpec spec, int layer)
        {
            return spec.Layers[layer].Name ?? DefaultName(spec, layer);
        }

        private static string DefaultName(NetworkDiagramSpec spec, int layer)
        {
            var role = NetworkLayout.RoleOf(spec, layer);

            if (role != NetworkRole.Hidden)
            {
                return RoleName(role);
            }

            var ordinal = 1;

            for (var i = 0; i < layer; i++)
            {
                if (NetworkLayout.RoleOf(spec, i) == NetworkRole.Hidden)
                {
                    ordinal++;
                }
            }

            return "Hidden " + ordinal;
        }

        internal static (double X, double Y) Bezier(double x0, double y0, double x1, double y1, double x2, double y2,
            double x3, double y3, double t)
        {
            var u = 1 - t;

            return (u * u * u * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x3,
                u * u * u * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y3);
        }
    }
}