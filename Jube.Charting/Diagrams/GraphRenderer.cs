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
using Jube.Charting.Diagrams.Icons;
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
            var groups = GroupSeries(spec);
            var network = IsNetwork(layout.Kind);

            var legend = groups.Count > 0
                ? groups.Select(g => new LegendEntry(g.Key, g.Value)).OrderBy(e => e.Series).ToArray()
                : roles.Select(r => new LegendEntry(RoleName(r), Series(r))).ToArray();
            var hasProbability = spec.Edges.Any(e => e.Probability.HasValue);
            var weights = layout.Edges.Select(e => Weight(spec, e)).Where(w => w.HasValue).Select(w => w!.Value)
                .ToArray();
            var hasWeight = weights.Length > 0;
            var signed = weights.Any(w => w < 0);
            var maxAbs = hasWeight ? Math.Max(weights.Max(Math.Abs), 1e-12) : 0;

            var summary = KindName(layout.Kind) + " diagram with " +
                          spec.Nodes.Count + " nodes and " + spec.Edges.Count + " " +
                          (spec.Directed ? "directed " : string.Empty) + "edges" +
                          (layout.Kind == GraphLayoutKind.Layered
                              ? " in " + layout.LayerCount + " levels"
                              : layout.Kind == GraphLayoutKind.Flow
                                  ? " in " + layout.LayerCount + " lanes"
                                  : layout.Kind == GraphLayoutKind.Radial
                                      ? " in " + layout.LayerCount + " rings"
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

                    if (!network && !spec.Nodes[p.Index].Shape.HasValue)
                    {
                        continue;
                    }

                    var half = Math.Min(NetworkGeometry.LabelWidth, TextMetrics.Width(p.Label, 11, true)) / 2 + 2;
                    placedLabels.Add((p.X - half, p.X + half, p.Y + p.Height / 2 + 2, p.Y + p.Height / 2 + 16));

                    if (spec.Nodes[p.Index].Badges is { Count: > 0 })
                    {
                        placedLabels.Add((p.X + p.Width / 2 - 4, p.X + p.Width / 2 + 44, p.Y - p.Height / 2 - 6,
                            p.Y - p.Height / 2 + 10));
                    }
                }

                scene.OpenGroup(ox, oy);

                if (network)
                {
                    scene.Rules(DiagramRules.Network(scene.K));
                }

                var marker = network && layout.Edges.Any(e => Directed(spec, e)) ? id + "-ar" : null;

                if (marker is not null)
                {
                    w.Open("defs");

                    w.Open("marker", ("id", marker), ("viewBox", "0 0 10 10"), ("refX", "9"), ("refY", "5"),
                        ("markerWidth", "9"), ("markerHeight", "9"), ("markerUnits", "userSpaceOnUse"),
                        ("orient", "auto"));

                    w.Empty("path", ("class", "ah"), ("d", "M0 0L10 5L0 10Z"));
                    w.Close();
                    w.Close();
                }

                if (layout.Kind == GraphLayoutKind.Flow)
                {
                    FlowFrame(spec, layout, w);
                }

                foreach (var e in layout.Edges)
                {
                    var src = spec.Edges[e.Index];
                    var a = layout.Nodes[e.From];
                    var b = layout.Nodes[e.To];
                    var sources = e.Merged ?? [e.Index];
                    var count = sources.Sum(s => spec.Edges[s].Count ?? 1);
                    var weight = Weight(spec, e);
                    var first = sources.Select(s => spec.Edges[s].First).Where(t => t.HasValue).Min();
                    var last = sources.Select(s => spec.Edges[s].Last ?? spec.Edges[s].First).Where(t => t.HasValue)
                        .Max();
                    var directed = Directed(spec, e);
                    var tagged = network || src.Id is not null || src.First.HasValue || src.Last.HasValue ||
                                 src.Count.HasValue || src.Directed.HasValue;

                    var value = src.Probability.HasValue ? "Probability: " + NumberFormat.Value(src.Probability.Value)
                        : weight.HasValue ? TextMetrics.Truncate(spec.WeightName, 80, 12) + ": " +
                                            NumberFormat.Value(weight.Value) : null;

                    var rows = new List<TipRow>
                    {
                        new(
                            TextMetrics.Truncate(a.Label, 100, 12) + (directed ? " to " : " and ") +
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

                    if (count > 1)
                    {
                        rows.Add(new TipRow("Events: " + count.ToString(CultureInfo.InvariantCulture)));
                    }

                    if (first is { } firstTime)
                    {
                        rows.Add(new TipRow("First: " + Stamp(firstTime)));
                    }

                    if (last is { } lastTime && last != first)
                    {
                        rows.Add(new TipRow("Last: " + Stamp(lastTime)));
                    }

                    var thickness = src.Probability.HasValue ? 1 + 4.5 * src.Probability.Value
                        : weight.HasValue ? Diverge.Width(weight.Value, maxAbs, 0.9, 4.5) : 1.6;

                    var cls = "ep";

                    if (weight.HasValue && signed && !src.Probability.HasValue)
                    {
                        cls += " sd" + Diverge.Bin(weight.Value, maxAbs);
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

                    var attributes = new List<(string Name, string? Value)>
                        { ("data-from", a.Id), ("data-to", b.Id) };

                    if (tagged)
                    {
                        attributes.Add(("data-edge", src.Id ?? key));
                        attributes.Add(("data-kind", src.Label ?? (directed ? "directed" : "undirected")));
                        attributes.Add(("data-first", first is { } f ? Unix(f) : null));
                        attributes.Add(("data-last", last is { } l ? Unix(l) : null));
                    }

                    scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)),
                        "eg " + StyleCaps.Tokens(scene.External, "e", [e.From, e.To]), [.. attributes]);

                    w.Empty("path", ("class", "eh"), ("d", e.Path));

                    w.Empty("path", ("class", cls), ("d", e.Path), scene.StrokeWidth(thickness),
                        ("marker-end", marker is not null && directed ? "url(#" + marker + ")" : null));

                    if (directed && marker is null)
                    {
                        Arrow(w, e, thickness);
                    }

                    var edgeIcon = showLabels && GraphIcons.Exists(src.Icon) ? src.Icon : null;

                    if (edgeIcon is not null)
                    {
                        w.Empty("circle", ("class", "nb"), ("cx", Num.F(e.LabelX)), ("cy", Num.F(e.LabelY)),
                            ("r", "10"));
                        Icon(w, e.LabelX, e.LabelY, 13, edgeIcon);
                    }

                    var text = count > 1
                        ? "×" + count.ToString(CultureInfo.InvariantCulture) +
                          (weight.HasValue ? " " + NumberFormat.Value(weight.Value) : string.Empty)
                        : !string.IsNullOrWhiteSpace(src.Label)
                            ? src.Label
                            : src.Probability.HasValue
                                ? NumberFormat.Value(src.Probability.Value)
                                : weight.HasValue
                                    ? NumberFormat.Value(weight.Value)
                                    : null;

                    if (edgeIcon is not null && count <= 1)
                    {
                        text = null;
                    }

                    if (showLabels && text is not null)
                    {
                        var shown = TextMetrics.Truncate(text, 90, 11);
                        var half = TextMetrics.Width(shown, 11) / 2 + 2;
                        var ly = e.LabelY + (edgeIcon is null ? 0 : 17);

                        foreach (var nudge in new[] { 0.0, -14, 14, -28, 28 })
                        {
                            var baseline = e.LabelY + (edgeIcon is null ? 0 : 17);
                            var clash = placedLabels.Any(r => e.LabelX - half < r.X1 && e.LabelX + half > r.X0 &&
                                                              baseline + nudge - 11 < r.Y1 &&
                                                              baseline + nudge + 3 > r.Y0);

                            if (!clash)
                            {
                                ly = baseline + nudge;
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
                    var glyph = network || src.Shape.HasValue;
                    var shape = src.Shape ?? GraphNodeShape.Circle;

                    var rows = new List<TipRow>
                        { new(TextMetrics.Truncate(p.Label, 240, 12, true), true), new(RoleName(p.Role)) };

                    if (!string.IsNullOrWhiteSpace(src.Detail))
                    {
                        rows.Add(new TipRow(TextMetrics.Truncate(src.Detail, 240, 12)));
                    }

                    rows.Add(new TipRow("Edges in: " + incoming[p.Index] + ", out: " + outgoing[p.Index]));
                    var key = "n" + p.Index;

                    var seriesIndex = src.Group is not null && groups.TryGetValue(src.Group, out var grouped)
                        ? grouped
                        : Series(p.Role);

                    if (src.Group is not null)
                    {
                        rows.Insert(1, new TipRow(TextMetrics.Truncate(src.Group, 240, 12)));
                        rows.RemoveAt(2);
                    }

                    var seen = spec.Times is { } times && times.TryGetValue(src.Id, out var when)
                        ? when
                        : (DateTimeOffset?)null;

                    if (seen is { } seenAt)
                    {
                        rows.Add(new TipRow("First seen: " + Stamp(seenAt)));
                    }

                    if (src.Badges is { Count: > 0 } badges)
                    {
                        rows.Add(new TipRow(string.Join(", ", badges.Select(BadgeText))));
                    }

                    var attributes = new List<(string Name, string? Value)> { ("data-node", p.Id) };

                    if (glyph)
                    {
                        attributes.Add(("data-kind", shape.ToString().ToLowerInvariant()));
                    }

                    if (seen is { } at)
                    {
                        attributes.Add(("data-first", Unix(at)));
                    }

                    scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)),
                        "nd s" + seriesIndex + " " +
                        StyleCaps.Tokens(scene.External, "j", neighbours[p.Index].OrderBy(j => j)), [.. attributes]);

                    if (glyph)
                    {
                        Glyph(w, p, "nb", shape, src.Dashed);
                        Glyph(w, p, "nt", shape, src.Dashed);
                        Icon(w, p, src.Icon);

                        w.Element("text", TextMetrics.Truncate(p.Label, NetworkGeometry.LabelWidth, 11, true),
                            ("class", "lbl ctr"), ("x", Num.F(p.X)), ("y", Num.F(p.Y + p.Height / 2 + 12)));

                        Badges(w, p, src.Badges);
                    }
                    else
                    {
                        if (src.Emphasis != GraphEmphasis.None)
                        {
                            Ring(w, p, src.Emphasis == GraphEmphasis.Error ? 7 : 3);
                        }

                        Shape(w, p, "nb", src.Dashed);
                        Shape(w, p, "nt", src.Dashed);

                        w.Element("text", TextMetrics.Truncate(p.Label, p.Width - 12, 11, true),
                            ("class", "lbl ctr mid"),
                            ("x", Num.F(p.X)), ("y", Num.F(p.Y)));
                    }

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

        private static bool IsNetwork(GraphLayoutKind kind)
        {
            return kind is GraphLayoutKind.Force or GraphLayoutKind.Radial or GraphLayoutKind.Flow;
        }

        private static string KindName(GraphLayoutKind kind)
        {
            return kind switch
            {
                GraphLayoutKind.Circular => "State graph",
                GraphLayoutKind.Force => "Network graph",
                GraphLayoutKind.Radial => "Radial graph",
                GraphLayoutKind.Flow => "Flow graph",
                _ => "Graph"
            };
        }

        private static double? Weight(GraphDiagramSpec spec, PlacedGraphEdge e)
        {
            if (e.Merged is null)
            {
                return spec.Edges[e.Index].Weight;
            }

            var values = e.Merged.Select(s => spec.Edges[s].Weight).Where(v => v.HasValue).Select(v => v!.Value)
                .ToArray();

            return values.Length == 0 ? null : values.Sum();
        }

        private static bool Directed(GraphDiagramSpec spec, PlacedGraphEdge e)
        {
            return (e.Merged ?? [e.Index]).Any(s => spec.Edges[s].Directed ?? spec.Directed);
        }

        private static string Unix(DateTimeOffset when)
        {
            return when.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }

        private static string Stamp(DateTimeOffset when)
        {
            return when.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        private static string BadgeText(GraphBadge badge)
        {
            if (!string.IsNullOrWhiteSpace(badge.Text))
            {
                return badge.Kind == GraphBadgeKind.Cluster ? "×" + badge.Text : badge.Text;
            }

            return badge.Kind switch
            {
                GraphBadgeKind.Confirmed => "★",
                GraphBadgeKind.OpenCase => "○",
                GraphBadgeKind.Records => "#",
                GraphBadgeKind.Cluster => "×",
                GraphBadgeKind.Pattern => "✓",
                _ => "hub"
            };
        }

        private static void Badges(SvgWriter w, PlacedGraphNode p, IReadOnlyList<GraphBadge>? badges)
        {
            if (badges is null || badges.Count == 0)
            {
                return;
            }

            var x = p.X + p.Width / 2 - 4;
            var y = p.Y - p.Height / 2 + 2;

            foreach (var badge in badges.Take(4))
            {
                var text = TextMetrics.Truncate(BadgeText(badge), 40, 9, true);
                var width = Math.Max(14, TextMetrics.Width(text, 9, true) + 8);

                w.Empty("rect", ("class", "nb"), ("x", Num.F(x)), ("y", Num.F(y - 7)), ("width", Num.F(width)),
                    ("height", "14"), ("rx", "7"));

                w.Element("text", text, ("class", "lbl ctr mid fs10"), ("x", Num.F(x + width / 2)), ("y", Num.F(y)));
                x += width + 2;
            }
        }

        private static void FlowFrame(GraphDiagramSpec spec, GraphLayoutResult layout, SvgWriter w)
        {
            var labelX = NetworkGeometry.Margin + FlowLayout.LaneLabelWidth(spec) - 8;

            foreach (var lane in layout.Nodes.GroupBy(p => p.Layer).OrderBy(g => g.Key))
            {
                var firstNode = lane.OrderBy(p => p.Index).First();
                var y = lane.Average(p => p.Y);

                w.Element("text", TextMetrics.Truncate(FlowLayout.LaneOf(spec, firstNode.Index), 160, 11, true),
                    ("class", "key end mid"), ("x", Num.F(labelX)), ("y", Num.F(y)));

                w.Empty("line", ("class", "gl"), ("x1", Num.F(labelX + 8)), ("y1", Num.F(y)),
                    ("x2", Num.F(layout.Width - NetworkGeometry.Margin)), ("y2", Num.F(y)));
            }

            var index = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < spec.Nodes.Count; i++)
            {
                index[spec.Nodes[i].Id] = i;
            }

            var times = FlowLayout.NodeTimes(spec, index);
            var known = times.Where(t => t.HasValue).Select(t => t!.Value).ToList();

            if (known.Count == 0)
            {
                return;
            }

            var span = known.Max() - known.Min();
            var lastEnd = double.NegativeInfinity;
            var y0 = layout.Height - 6;

            foreach (var column in layout.Nodes.Where(p => times[p.Index].HasValue).GroupBy(p => p.X)
                         .OrderBy(g => g.Key))
            {
                var label = FlowLayout.FormatTime(column.Min(p => times[p.Index]!.Value), span);
                var half = TextMetrics.Width(label, 11) / 2;

                if (column.Key - half < lastEnd + 6)
                {
                    continue;
                }

                lastEnd = column.Key + half;
                w.Element("text", label, ("class", "tk ctr"), ("x", Num.F(column.Key)), ("y", Num.F(y0)));
            }
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

        private static Dictionary<string, int> GroupSeries(GraphDiagramSpec spec)
        {
            var present = spec.Nodes.Where(n => n.Group is not null).Select(n => n.Group!).Distinct().ToList();
            var ordered = spec.GroupOrder.ToList();
            var map = new Dictionary<string, int>();
            var next = ordered.Count;

            foreach (var name in present)
            {
                var position = ordered.IndexOf(name);

                map[name] = name.Equals("Missing", StringComparison.OrdinalIgnoreCase) ? 7
                    : position >= 0 ? position % 7
                    : next++ % 7;
            }

            return map;
        }

        private static void Glyph(SvgWriter w, PlacedGraphNode p, string cls, GraphNodeShape shape, bool dashed)
        {
            var dash = dashed ? " dash" : string.Empty;
            var h = Math.Min(p.Width, p.Height) / 2;

            switch (shape)
            {
                case GraphNodeShape.Square:
                    w.Empty("rect", ("class", cls + dash), ("x", Num.F(p.X - h * 0.9)), ("y", Num.F(p.Y - h * 0.9)),
                        ("width", Num.F(h * 1.8)), ("height", Num.F(h * 1.8)), ("rx", "3"));

                    return;
                case GraphNodeShape.Diamond:
                    w.Empty("path", ("class", cls + dash), ("d", new PathBuilder().Move(p.X, p.Y - h)
                        .Line(p.X + h, p.Y).Line(p.X, p.Y + h).Line(p.X - h, p.Y).Close().ToString()));

                    return;
                case GraphNodeShape.Triangle:
                    w.Empty("path", ("class", cls + dash), ("d", new PathBuilder().Move(p.X, p.Y - h)
                        .Line(p.X + h * 0.95, p.Y + h * 0.6).Line(p.X - h * 0.95, p.Y + h * 0.6).Close().ToString()));

                    return;
                case GraphNodeShape.Hexagon:
                    var hex = new PathBuilder();

                    for (var i = 0; i < 6; i++)
                    {
                        var angle = Math.PI / 6 + i * Math.PI / 3;
                        var x = p.X + h * Math.Cos(angle);
                        var y = p.Y + h * Math.Sin(angle);

                        if (i == 0)
                        {
                            hex.Move(x, y);
                        }
                        else
                        {
                            hex.Line(x, y);
                        }
                    }

                    w.Empty("path", ("class", cls + dash), ("d", hex.Close().ToString()));

                    return;
                case GraphNodeShape.Stack:
                    if (cls == "nb")
                    {
                        foreach (var offset in new[] { 6.0, 3.0 })
                        {
                            w.Empty("rect", ("class", cls), ("x", Num.F(p.X - h * 0.9 + offset)),
                                ("y", Num.F(p.Y - h * 0.9 - offset)), ("width", Num.F(h * 1.8)),
                                ("height", Num.F(h * 1.8)), ("rx", "4"));
                        }
                    }

                    w.Empty("rect", ("class", cls + dash), ("x", Num.F(p.X - h * 0.9)), ("y", Num.F(p.Y - h * 0.9)),
                        ("width", Num.F(h * 1.8)), ("height", Num.F(h * 1.8)), ("rx", "4"));

                    return;
                case GraphNodeShape.Circle:
                default:
                    w.Empty("circle", ("class", cls + dash), ("cx", Num.F(p.X)), ("cy", Num.F(p.Y)), ("r", Num.F(h)));

                    return;
            }
        }

        private static void Icon(SvgWriter w, PlacedGraphNode p, string? name)
        {
            Icon(w, p.X, p.Y, Math.Min(p.Width, p.Height) * 0.7, name);
        }

        private static void Icon(SvgWriter w, double x, double y, double size, string? name)
        {
            var elements = GraphIcons.Resolve(name);

            if (elements is null)
            {
                return;
            }

            var transform = "translate(" + Num.F(x - size / 2) + " " + Num.F(y - size / 2) + ") scale(" +
                            Num.F(size / 24) + ")";

            w.Open("g", ("class", "ic"), ("transform", transform), ("aria-hidden", "true"));

            foreach (var element in elements)
            {
                w.Empty(element.Tag, element.Attributes.Select(a => (a.Name, (string?)a.Value)).ToArray());
            }

            w.Close();
        }

        private static void Ring(SvgWriter w, PlacedGraphNode p, int series)
        {
            const double gap = 4;

            w.Empty("rect", ("class", "nb s" + series), ("x", Num.F(p.X - p.Width / 2 - gap)),
                ("y", Num.F(p.Y - p.Height / 2 - gap)), ("width", Num.F(p.Width + 2 * gap)),
                ("height", Num.F(p.Height + 2 * gap)), ("rx", Num.F(p.Height / 2 + gap)));
        }

        private static void Shape(SvgWriter w, PlacedGraphNode p, string cls, bool dashed = false)
        {
            var dash = p.Role == GraphNodeRole.Latent || dashed ? " dash" : string.Empty;

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