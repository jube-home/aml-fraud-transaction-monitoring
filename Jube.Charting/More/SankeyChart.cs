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
using Jube.Charting.Theme;
using Jube.Charting.Theme.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class SankeyChart(SankeyChartSpec spec) : IStyleModeChart
    {
        public const int MaxNodes = 200;
        public const int MaxLinks = 2000;
        private const int PathNodeLimit = 40;
        private const int PathLinkLimit = 100;

        public SankeyChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new SankeyChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;

            if (!(s.NodeWidth is >= 2 and <= 60) || !(s.NodePadding is >= 0 and <= 60))
            {
                throw new ChartSpecificationException("Node width must be 2 to 60 and node padding 0 to 60.");
            }

            var nodes = new List<N>();
            var lookup = new Dictionary<string, int>(StringComparer.Ordinal);

            int Node(string name, string? colour)
            {
                if (!lookup.TryGetValue(name, out var i))
                {
                    i = nodes.Count;
                    lookup[name] = i;
                    nodes.Add(new N { Index = i, Name = name, Color = colour });

                    if (nodes.Count > MaxNodes)
                    {
                        throw new ChartLimitException("sankey node count", nodes.Count, MaxNodes);
                    }
                }
                else if (colour is not null)
                {
                    nodes[i].Color = colour;
                }

                return i;
            }

            foreach (var node in s.Nodes)
            {
                Node(node.Name, ColourCheck.Normalise(node.Color));
            }

            if (s.Links.Count > MaxLinks)
            {
                throw new ChartLimitException("sankey link count", s.Links.Count, MaxLinks);
            }

            var links = new List<L>();

            foreach (var link in s.Links)
            {
                if (!double.IsFinite(link.Value) || link.Value <= 0)
                {
                    continue;
                }

                var a = Node(link.Source, null);
                var b = Node(link.Target, null);

                if (a == b)
                {
                    throw new ChartSpecificationException("A sankey link must join two different nodes.");
                }

                links.Add(new L { Index = links.Count, Source = a, Target = b, Value = link.Value });
            }

            foreach (var l in links)
            {
                nodes[l.Source].Outgoing.Add(l.Index);
                nodes[l.Target].Incoming.Add(l.Index);
                nodes[l.Source].Out += l.Value;
                nodes[l.Target].In += l.Value;
            }

            foreach (var n in nodes)
            {
                n.Value = Math.Max(n.In, n.Out);
            }

            var maxLayer = AssignLayers(nodes, links, s.Justify);
            var format = s.ValueFormat ?? NumberFormat.Value;

            var summary = "Sankey chart with " + nodes.Count + " nodes and " + links.Count + " links" +
                          (links.Count > 0
                              ? ", total flow " + format(nodes.Where(n => n.In == 0).Sum(n => n.Out))
                              : string.Empty) + ".";

            var colours = Guard.Colours(nodes.Select(n => n.Color));

            var canvas = new ChartCanvas(s.Options with { Legend = LegendPosition.None }, id, summary, [], nodes.Count,
                colours);

            var marks = nodes.Count + links.Count;
            var pathMode = nodes.Count <= PathNodeLimit && links.Count <= PathLinkLimit;
            var related = Related(nodes, links, pathMode);
            canvas.Family = StyleFamily.Sankey;
            canvas.ExtraCss = Css(canvas.K, nodes, links, pathMode).ToString();
            canvas.LimitInteractive(marks);
            var plot = canvas.Frame(0, 0);
            canvas.Begin();

            Layout(nodes, links, maxLayer, plot, s);

            var gap = maxLayer > 0 ? (plot.Width - s.NodeWidth) / maxLayer : plot.Width;

            foreach (var n in nodes)
            {
                var labelRight = n.Layer * 2 <= maxLayer && !(maxLayer > 0 && n.Layer == maxLayer);
                var room = Math.Max(gap - s.NodeWidth - 10, 30);

                var text = TextMetrics.Truncate(n.Name, room, 11);

                if (text.Length == 0 || n.Y1 - n.Y0 < 3)
                {
                    continue;
                }

                canvas.W.Element("text", text, ("class", labelRight ? "sk mid" : "sk end mid"),
                    ("x", Num.F(labelRight ? n.X0 + s.NodeWidth + 6 : n.X0 - 6)), ("y", Num.F(n.Center)));
            }

            foreach (var l in links)
            {
                var a = nodes[l.Source];
                var b = nodes[l.Target];
                var x0 = a.X0 + s.NodeWidth;
                var x1 = b.X0;
                var xm = (x0 + x1) / 2;

                var path = new PathBuilder().Move(x0, l.SourceY).Cubic(xm, l.SourceY, xm, l.TargetY, x1, l.TargetY)
                    .Line(x1, l.TargetY + l.Width)
                    .Cubic(xm, l.TargetY + l.Width, xm, l.SourceY + l.Width, x0, l.SourceY + l.Width);

                var rows = new List<TipRow>
                {
                    new(a.Name + " to " + b.Name, true, a.Index),
                    new("Flow: " + format(l.Value)),
                    new(NumberFormat.Percent(a.Out > 0 ? l.Value / a.Out : 0) + " of " + a.Name + " outflow"),
                    new(NumberFormat.Percent(b.In > 0 ? l.Value / b.In : 0) + " of " + b.Name + " inflow")
                };

                var external = canvas.External;

                var classes =
                    "lk " + (StyleCaps.Keep(external, l.Index, StyleCaps.SankeyLinks) ? "l" + l.Index : string.Empty)
                          + (pathMode
                              ? " " + StyleCaps.Tokens(external, "k", related.LinkLinks[l.Index], StyleCaps.SankeyLinks)
                              : string.Empty)
                          + " " + StyleCaps.Tokens(external, "m", related.LinkNodes[l.Index], StyleCaps.SankeyNodes);

                canvas.OpenMark(a.Index, string.Join(", ", rows.Select(r => r.Text)), classes.Trim());
                canvas.W.Empty("path", ("class", "lkp"), ("d", path.Close().ToString()));
                canvas.Tooltip(xm, (l.SourceY + l.TargetY + l.Width) / 2, rows, true, 8);
                canvas.CloseMark();
            }

            foreach (var n in nodes)
            {
                var rows = new List<TipRow>
                {
                    new(n.Name, true, n.Index),
                    new("Inflow: " + format(n.In)),
                    new("Outflow: " + format(n.Out))
                };

                var classes = "nd " + (StyleCaps.Keep(canvas.External, n.Index, StyleCaps.SankeyNodes)
                                  ? "n" + n.Index
                                  : string.Empty) + " " +
                              StyleCaps.Tokens(canvas.External, "a", related.NodeNodes[n.Index], StyleCaps.SankeyNodes);

                canvas.OpenMark(n.Index, string.Join(", ", rows.Select(r => r.Text)), classes.Trim());

                canvas.W.Empty("rect", ("class", "bar"), ("x", Num.F(n.X0)), ("y", Num.F(n.Y0)),
                    ("width", Num.F(s.NodeWidth)),
                    ("height", Num.F(Math.Max(n.Y1 - n.Y0, 1))), ("rx", "2"));

                var right = n.X0 + s.NodeWidth + 6 + 60 < plot.Right || n.Layer == 0 || maxLayer == 0;
                canvas.Tooltip(right ? n.X0 + s.NodeWidth : n.X0, n.Center, rows, right, 6);
                canvas.CloseMark();
            }

            if (links.Count == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            return canvas.End();
        }

        private static CssRules Css(string k, List<N> nodes, List<L> links, bool pathMode)
        {
            var css = new CssRules(k);

            for (var i = 0; i < nodes.Count; i++)
            {
                DiagramRules.SankeyNode(css, i);
            }

            if (pathMode)
            {
                for (var j = 0; j < links.Count; j++)
                {
                    DiagramRules.SankeyLink(css, j);
                }
            }

            return css;
        }

        private static (List<int>[] NodeNodes, List<int>[] LinkNodes, List<int>[] LinkLinks, List<int>[] NodeLinks)
            Related(
                List<N> nodes, List<L> links, bool pathMode)
        {
            var n = nodes.Count;
            var down = new HashSet<int>[n];
            var up = new HashSet<int>[n];

            for (var i = 0; i < n; i++)
            {
                down[i] = [];
                up[i] = [];
            }

            if (pathMode)
            {
                var order = nodes.OrderByDescending(x => x.Layer).ToList();

                foreach (var node in order)
                {
                    foreach (var li in node.Outgoing)
                    {
                        var t = links[li].Target;
                        down[node.Index].Add(t);
                        down[node.Index].UnionWith(down[t]);
                    }
                }

                foreach (var node in nodes.OrderBy(x => x.Layer))
                {
                    foreach (var li in node.Incoming)
                    {
                        var src = links[li].Source;
                        up[node.Index].Add(src);
                        up[node.Index].UnionWith(up[src]);
                    }
                }
            }

            var nodeNodes = new List<int>[n];
            var nodeLinks = new List<int>[n];

            for (var i = 0; i < n; i++)
            {
                if (pathMode)
                {
                    var set = new SortedSet<int>(down[i]) { i };
                    set.UnionWith(up[i]);
                    nodeNodes[i] = [.. set];

                    var i1 = i;
                    nodeLinks[i] =
                    [
                        .. links.Where(l =>
                                l.Source == i1 || down[i1].Contains(l.Source) || l.Target == i1 ||
                                up[i1].Contains(l.Target))
                            .Select(l => l.Index)
                    ];
                }
                else
                {
                    nodeNodes[i] = [i];
                    nodeLinks[i] = [.. nodes[i].Outgoing.Concat(nodes[i].Incoming).OrderBy(x => x)];
                }
            }

            var linkNodes = new List<int>[links.Count];
            var linkLinks = new List<int>[links.Count];

            for (var j = 0; j < links.Count; j++)
            {
                linkNodes[j] = [];
            }

            for (var i = 0; i < n; i++)
            {
                foreach (var li in nodeLinks[i])
                {
                    linkNodes[li].Add(i);
                }
            }

            for (var j = 0; j < links.Count; j++)
            {
                if (!pathMode)
                {
                    linkLinks[j] = [j];
                    continue;
                }

                var target = links[j].Target;
                var source = links[j].Source;

                var j1 = j;
                linkLinks[j] =
                [
                    .. links.Where(l => l.Index == j1 || l.Source == target || down[target].Contains(l.Source) ||
                                        l.Target == source || up[source].Contains(l.Target))
                        .Select(l => l.Index)
                ];
            }

            return (nodeNodes, linkNodes, linkLinks, nodeLinks);
        }

        private static int AssignLayers(List<N> nodes, List<L> links, bool justify)
        {
            var indegree = nodes.Select(n => n.Incoming.Count).ToArray();
            var queue = new Queue<int>(nodes.Where(n => indegree[n.Index] == 0).Select(n => n.Index));
            var processed = 0;

            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                processed++;

                foreach (var li in nodes[i].Outgoing)
                {
                    var t = links[li].Target;
                    nodes[t].Layer = Math.Max(nodes[t].Layer, nodes[i].Layer + 1);

                    if (--indegree[t] == 0)
                    {
                        queue.Enqueue(t);
                    }
                }
            }

            if (processed != nodes.Count)
            {
                throw new ChartSpecificationException("Sankey links must not form a cycle.");
            }

            var max = nodes.Count == 0 ? 0 : nodes.Max(n => n.Layer);

            if (justify)
            {
                foreach (var n in nodes.Where(n => n.Outgoing.Count == 0 && n.Incoming.Count > 0))
                {
                    n.Layer = max;
                }
            }

            return max;
        }

        private static void Layout(List<N> nodes, List<L> links, int maxLayer, Rect plot, SankeyChartSpec s)
        {
            var layers = Enumerable.Range(0, maxLayer + 1).Select(l => nodes.Where(n => n.Layer == l).ToList())
                .ToList();

            var ky = double.PositiveInfinity;

            foreach (var layer in layers)
            {
                var total = layer.Sum(n => n.Value);

                if (total > 0)
                {
                    ky = Math.Min(ky, Math.Max(plot.Height - (layer.Count - 1) * s.NodePadding, 4) / total);
                }
            }

            if (!double.IsFinite(ky))
            {
                ky = 1;
            }

            var step = maxLayer > 0 ? (plot.Width - s.NodeWidth) / maxLayer : 0;

            foreach (var n in nodes)
            {
                n.X0 = plot.Left + n.Layer * step;
            }

            foreach (var layer in layers)
            {
                var y = plot.Top;

                foreach (var n in layer)
                {
                    n.Y0 = y;
                    n.Y1 = y + n.Value * ky;
                    y = n.Y1 + s.NodePadding;
                }
            }

            foreach (var l in links)
            {
                l.Width = l.Value * ky;
            }

            for (var iteration = 0; iteration < 8; iteration++)
            {
                for (var li = 1; li <= maxLayer; li++)
                {
                    Relax(layers[li], n => n.Incoming.Select(i => (nodes[links[i].Source].Center, links[i].Value)),
                        plot,
                        s.NodePadding);
                }

                for (var li = maxLayer - 1; li >= 0; li--)
                {
                    Relax(layers[li], n => n.Outgoing.Select(i => (nodes[links[i].Target].Center, links[i].Value)),
                        plot,
                        s.NodePadding);
                }
            }

            foreach (var layer in layers)
            {
                Collide(layer, plot, s.NodePadding);
            }

            foreach (var n in nodes)
            {
                var y = n.Y0;

                foreach (var li in n.Outgoing.OrderBy(i => nodes[links[i].Target].Center).ThenBy(i => i))
                {
                    links[li].SourceY = y;
                    y += links[li].Width;
                }

                y = n.Y0;

                foreach (var li in n.Incoming.OrderBy(i => nodes[links[i].Source].Center).ThenBy(i => i))
                {
                    links[li].TargetY = y;
                    y += links[li].Width;
                }
            }
        }

        private static void Relax(List<N> layer, Func<N, IEnumerable<(double Center, double Value)>> neighbours,
            Rect plot,
            double padding)
        {
            foreach (var n in layer)
            {
                var list = neighbours(n).ToList();
                var weight = list.Sum(t => t.Value);

                if (weight > 0)
                {
                    var target = list.Sum(t => t.Center * t.Value) / weight;
                    var dy = target - n.Center;
                    n.Y0 += dy;
                    n.Y1 += dy;
                }
            }

            Collide(layer, plot, padding);
        }

        private static void Collide(List<N> layer, Rect plot, double padding)
        {
            layer.Sort((a, b) => Math.Abs(a.Y0 - b.Y0) > 0.0001 ? a.Y0.CompareTo(b.Y0) : a.Index.CompareTo(b.Index));
            var y = plot.Top;

            foreach (var n in layer)
            {
                var dy = y - n.Y0;

                if (dy > 0)
                {
                    n.Y0 += dy;
                    n.Y1 += dy;
                }

                y = n.Y1 + padding;
            }

            var overflow = y - padding - plot.Bottom;

            if (overflow > 0)
            {
                y = plot.Bottom;

                for (var i = layer.Count - 1; i >= 0; i--)
                {
                    var n = layer[i];
                    var dy = n.Y1 - y;

                    if (dy > 0)
                    {
                        n.Y0 -= dy;
                        n.Y1 -= dy;
                    }

                    y = n.Y0 - padding;
                }
            }
        }

        private sealed class N
        {
            public readonly List<int> Incoming = [];
            public readonly List<int> Outgoing = [];
            public string? Color;
            public double In;
            public int Index;
            public int Layer;
            public string Name = string.Empty;
            public double Out;
            public double Value;
            public double X0;
            public double Y0;
            public double Y1;
            public double Center => (Y0 + Y1) / 2;
        }

        private sealed class L
        {
            public int Index;
            public int Source;
            public double SourceY;
            public int Target;
            public double TargetY;
            public double Value;
            public double Width;
        }
    }
}