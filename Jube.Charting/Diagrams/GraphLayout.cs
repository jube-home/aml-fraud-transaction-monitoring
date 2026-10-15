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
using System.Text.RegularExpressions;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams
{
    public static class GraphLayout
    {
        public static GraphLayoutResult Compute(GraphDiagramSpec spec)
        {
            var index = Validate(spec);
            var kind = spec.Layout;

            if (!Enum.IsDefined(kind))
            {
                throw new DiagramNotSupportedException("the graph layout " + kind + " is unknown.");
            }

            if (kind == GraphLayoutKind.Auto)
            {
                kind = spec.Nodes.Count > 0 && spec.Nodes.All(n => n.Role == GraphNodeRole.State) &&
                       spec.Nodes.Count <= DiagramLimits.MaxCircularNodes
                    ? GraphLayoutKind.Circular
                    : GraphLayoutKind.Layered;
            }

            return kind == GraphLayoutKind.Circular ? Circular(spec, index) : Layered(spec, index);
        }

        private const double MaxExtent = 3000;
        private const double NodeHeight = 32;

        private static Dictionary<string, int> Validate(GraphDiagramSpec spec)
        {
            if (spec.Nodes.Count == 0)
            {
                throw new ChartSpecificationException("A graph diagram needs at least one node.");
            }

            if (spec.Nodes.Count > DiagramLimits.MaxGraphNodes)
            {
                throw new DiagramTooLargeException("graph nodes", spec.Nodes.Count, DiagramLimits.MaxGraphNodes,
                    "Draw a summary, or the neighbourhood of the nodes of interest.");
            }

            if (spec.Edges.Count > DiagramLimits.MaxGraphEdges)
            {
                throw new DiagramTooLargeException("graph edges", spec.Edges.Count, DiagramLimits.MaxGraphEdges,
                    "Keep the strongest edges only.");
            }

            var index = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < spec.Nodes.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(spec.Nodes[i].Id) || !index.TryAdd(spec.Nodes[i].Id, i))
                {
                    throw new ChartSpecificationException("Graph node ids must be non-empty and unique.");
                }
            }

            var seen = new HashSet<(int, int)>();

            foreach (var e in spec.Edges)
            {
                if (!index.TryGetValue(e.From, out var a) || !index.TryGetValue(e.To, out var b))
                {
                    throw new ChartSpecificationException("A graph edge refers to a node that is not declared.");
                }

                if (!seen.Add((a, b)))
                {
                    throw new ChartSpecificationException("Graph edges must be unique per ordered node pair.");
                }

                if ((e.Weight is { } w && !double.IsFinite(w)) || (e.Probability is { } p && !(p is >= 0 and <= 1)))
                {
                    throw new ChartSpecificationException(
                        "Edge weights must be finite and probabilities within 0 and 1.");
                }
            }

            return index;
        }

        internal static (double W, double H) NodeSize(GraphNode n)
        {
            var text = TextMetrics.Width(n.Label, 11, true);

            if (n.Role == GraphNodeRole.State)
            {
                var d = Math.Max(46, Math.Min(text, 150) + 20);

                return (d, d);
            }

            return (Math.Clamp(text + 24, 56, 170), NodeHeight);
        }

        private static GraphLayoutResult Layered(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            var n = spec.Nodes.Count;
            var sizes = spec.Nodes.Select(NodeSize).ToArray();
            var edges = spec.Edges.Select(e => (From: index[e.From], To: index[e.To])).ToArray();
            var loops = new HashSet<int>(edges.Where(e => e.From == e.To).Select(e => e.From));
            var reversed = new bool[edges.Length];
            var directed = new List<(int A, int B, int K)>();
            var adjacency = new List<int>[n];

            for (var i = 0; i < n; i++)
            {
                adjacency[i] = [];
            }

            for (var k = 0; k < edges.Length; k++)
            {
                if (edges[k].From != edges[k].To)
                {
                    adjacency[edges[k].From].Add(k);
                }
            }

            var state = new int[n];

            for (var start = 0; start < n; start++)
            {
                if (state[start] != 0)
                {
                    continue;
                }

                var stack = new Stack<(int Node, int Next)>();
                stack.Push((start, 0));
                state[start] = 1;

                while (stack.Count > 0)
                {
                    var (u, next) = stack.Pop();

                    if (next < adjacency[u].Count)
                    {
                        stack.Push((u, next + 1));
                        var k = adjacency[u][next];
                        var v = edges[k].To;

                        if (state[v] == 1)
                        {
                            reversed[k] = true;
                        }
                        else if (state[v] == 0)
                        {
                            state[v] = 1;
                            stack.Push((v, 0));
                        }
                    }
                    else
                    {
                        state[u] = 2;
                    }
                }
            }

            for (var k = 0; k < edges.Length; k++)
            {
                if (edges[k].From == edges[k].To)
                {
                    continue;
                }

                directed.Add(reversed[k] ? (edges[k].To, edges[k].From, k) : (edges[k].From, edges[k].To, k));
            }

            var indegree = new int[n];
            var successors = new List<int>[n];

            for (var i = 0; i < n; i++)
            {
                successors[i] = [];
            }

            foreach (var (a, b, _) in directed)
            {
                indegree[b]++;
                successors[a].Add(b);
            }

            var layer = new int[n];
            var order = new List<int>();
            var queue = new Queue<int>(Enumerable.Range(0, n).Where(i => indegree[i] == 0));
            var remaining = (int[])indegree.Clone();

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                order.Add(u);

                foreach (var v in successors[u].OrderBy(x => x))
                {
                    layer[v] = Math.Max(layer[v], layer[u] + 1);

                    if (--remaining[v] == 0)
                    {
                        queue.Enqueue(v);
                    }
                }
            }

            for (var i = order.Count - 1; i >= 0; i--)
            {
                var u = order[i];

                if (indegree[u] == 0 && successors[u].Count > 0)
                {
                    layer[u] = Math.Max(layer[u], successors[u].Min(v => layer[v]) - 1);
                }
            }

            var layerCount = layer.Max() + 1;
            var ex = new Expanded();

            for (var i = 0; i < n; i++)
            {
                var footprint = sizes[i].W + (loops.Contains(i) ? 56 : 0);
                ex.Add(layer[i], footprint);
            }

            var chains = new List<int>[edges.Length];

            foreach (var (a, b, k) in directed)
            {
                var chain = new List<int> { a };

                for (var l = layer[a] + 1; l < layer[b]; l++)
                {
                    chain.Add(ex.Add(l, 10));
                }

                chain.Add(b);
                chains[k] = chain;

                for (var i = 0; i + 1 < chain.Count; i++)
                {
                    ex.Down[chain[i]].Add(chain[i + 1]);
                    ex.Up[chain[i + 1]].Add(chain[i]);
                }
            }

            var layers = new List<List<int>>();

            for (var l = 0; l < layerCount; l++)
            {
                var currentLayer = l;
                layers.Add([.. Enumerable.Range(0, ex.Layer.Count).Where(i => ex.Layer[i] == currentLayer)]);
            }

            var crossings = ReduceCrossings(layers, ex);
            var x = AssignX(layers, ex, spec);
            var minX = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;

            for (var i = 0; i < n; i++)
            {
                minX = Math.Min(minX, x[i] - sizes[i].W / 2);
                maxX = Math.Max(maxX, x[i] + sizes[i].W / 2 + (loops.Contains(i) ? 40 : 0));
            }

            for (var i = n; i < ex.Layer.Count; i++)
            {
                minX = Math.Min(minX, x[i] - 5);
                maxX = Math.Max(maxX, x[i] + 5);
            }

            var margin = 8.0;

            var labelled = spec.Edges.Count <= spec.EdgeLabelLimit &&
                           spec.Edges.Any(e =>
                               e.Weight.HasValue || e.Probability.HasValue || !string.IsNullOrWhiteSpace(e.Label));

            var layerGap = labelled ? 104.0 : 84.0;
            var layerHeight = new double[layerCount];

            for (var i = 0; i < n; i++)
            {
                layerHeight[layer[i]] = Math.Max(layerHeight[layer[i]], sizes[i].H);
            }

            var layerY = new double[layerCount];
            var y = margin;

            for (var l = 0; l < layerCount; l++)
            {
                layerY[l] = y + Math.Max(layerHeight[l], 1) / 2;
                y += Math.Max(layerHeight[l], 1) + layerGap;
            }

            var width = maxX - minX + 2 * margin;
            var height = y - layerGap + margin;

            if (width > MaxExtent || height > MaxExtent)
            {
                throw new DiagramTooLargeException("graph extent in pixels",
                    (long)Math.Ceiling(Math.Max(width, height)),
                    (long)MaxExtent, "Too many nodes on one level to draw legibly; keep the strongest structure only.");
            }

            var dx = margin - minX;
            var placed = new PlacedGraphNode[n];

            for (var i = 0; i < n; i++)
            {
                placed[i] = new PlacedGraphNode(i, spec.Nodes[i].Id, spec.Nodes[i].Label, spec.Nodes[i].Role, x[i] + dx,
                    layerY[layer[i]], sizes[i].W, sizes[i].H, layer[i]);
            }

            var outCount = new Dictionary<int, List<(int Edge, double Target)>>();
            var inCount = new Dictionary<int, List<(int Edge, double Target)>>();

            foreach (var (a, b, k) in directed)
            {
                var chain = chains[k];
                outCount.TryAdd(a, []);
                inCount.TryAdd(b, []);
                outCount[a].Add((k, x[chain[1]]));
                inCount[b].Add((k, x[chain[^2]]));
            }

            double Attach(Dictionary<int, List<(int Edge, double Target)>> map, int node, int edge)
            {
                var list = map[node].OrderBy(t => t.Target).ThenBy(t => t.Edge).ToList();
                var rank = list.FindIndex(t => t.Edge == edge);
                var step = list.Count > 1 ? Math.Min(14.0, (sizes[node].W - 20) / (list.Count - 1)) : 0;

                return (rank - (list.Count - 1) / 2.0) * step;
            }

            var result = new List<PlacedGraphEdge>();

            for (var k = 0; k < edges.Length; k++)
            {
                var (from, to) = edges[k];

                if (from == to)
                {
                    result.Add(SelfLoopRight(placed[from], k));
                    continue;
                }

                var chain = chains[k];
                var a = chain[0];
                var b = chain[^1];

                var points = new List<(double X, double Y)>
                {
                    (placed[a].X + Attach(outCount, a, k), placed[a].Y + placed[a].Height / 2)
                };

                for (var i = 1; i < chain.Count - 1; i++)
                {
                    points.Add((x[chain[i]] + dx, layerY[ex.Layer[chain[i]]]));
                }

                points.Add((placed[b].X + Attach(inCount, b, k), placed[b].Y - placed[b].Height / 2));

                if (reversed[k])
                {
                    points.Reverse();
                }

                result.Add(BuildLayeredPath(k, from, to, points, reversed[k]));
            }

            return new GraphLayoutResult(placed, result, width, height, GraphLayoutKind.Layered, layerCount, crossings);
        }

        private static PlacedGraphEdge BuildLayeredPath(int k, int from, int to, List<(double X, double Y)> points,
            bool reversed)
        {
            var path = new PathBuilder().Move(points[0].X, points[0].Y);
            var bestLength = -1.0;
            var lx = points[0].X;
            var ly = points[0].Y;

            for (var i = 0; i + 1 < points.Count; i++)
            {
                var (x0, y0) = points[i];
                var (x1, y1) = points[i + 1];
                var ym = (y0 + y1) / 2;
                path.Cubic(x0, ym, x1, ym, x1, y1);
                var length = Math.Abs(y1 - y0);

                if (length > bestLength)
                {
                    bestLength = length;
                    var p = NetworkRenderer.Bezier(x0, y0, x0, ym, x1, ym, x1, y1, 0.5);
                    lx = p.X;
                    ly = p.Y;
                }
            }

            var end = points[^1];
            var before = points[^2];
            var dir = end.Y >= before.Y ? 1.0 : -1.0;

            return new PlacedGraphEdge(k, from, to, path.ToString(), lx, ly, end.X, end.Y, 0, dir, false, reversed,
                points);
        }

        private static PlacedGraphEdge SelfLoopRight(PlacedGraphNode node, int k)
        {
            var x = node.X + node.Width / 2;
            var top = node.Y - 5;
            var bottom = node.Y + 5;

            var path = new PathBuilder().Move(x, top).Cubic(x + 34, top - 20, x + 34, bottom + 20, x, bottom)
                .ToString();

            return new PlacedGraphEdge(k, node.Index, node.Index, path, x + 30, node.Y - 2, x, bottom, -1, 0, true,
                false,
                [(x, top), (x + 26, node.Y), (x, bottom)]);
        }

        private static int ReduceCrossings(List<List<int>> layers, Expanded ex)
        {
            var position = new int[ex.Layer.Count];

            void Reindex()
            {
                foreach (var layer in layers)
                {
                    for (var i = 0; i < layer.Count; i++)
                    {
                        position[layer[i]] = i;
                    }
                }
            }

            Reindex();
            var best = layers.Select(l => l.ToList()).ToList();
            var bestCount = Count(layers, ex, position);

            for (var iteration = 0; iteration < 24 && bestCount > 0; iteration++)
            {
                if (iteration % 2 == 0)
                {
                    for (var l = 1; l < layers.Count; l++)
                    {
                        Sort(layers[l], ex.Up, position);
                        Reindex();
                    }
                }
                else
                {
                    for (var l = layers.Count - 2; l >= 0; l--)
                    {
                        Sort(layers[l], ex.Down, position);
                        Reindex();
                    }
                }

                Transpose(layers, ex, position);
                var count = Count(layers, ex, position);

                if (count < bestCount)
                {
                    bestCount = count;
                    best = [.. layers.Select(l => l.ToList())];
                }
            }

            for (var l = 0; l < layers.Count; l++)
            {
                layers[l] = best[l];
            }

            Reindex();

            return bestCount;
        }

        private static void Sort(List<int> layer, List<List<int>> neighbours, int[] position)
        {
            var keyed = layer.Select((node, i) => (Node: node, Key: neighbours[node].Count == 0
                ? position[node]
                : neighbours[node].Average(m => (double)position[m]), Old: i)).ToList();

            var sorted = keyed.OrderBy(t => t.Key).ThenBy(t => t.Old).Select(t => t.Node).ToList();
            layer.Clear();
            layer.AddRange(sorted);
        }

        private static void Transpose(List<List<int>> layers, Expanded ex, int[] position)
        {
            for (var round = 0; round < 8; round++)
            {
                var improved = false;

                foreach (var layer in layers)
                {
                    for (var i = 0; i + 1 < layer.Count; i++)
                    {
                        var u = layer[i];
                        var v = layer[i + 1];

                        if (Pair(ex, position, u, v) > Pair(ex, position, v, u))
                        {
                            layer[i] = v;
                            layer[i + 1] = u;
                            position[u] = i + 1;
                            position[v] = i;
                            improved = true;
                        }
                    }
                }

                if (!improved)
                {
                    break;
                }
            }
        }

        private static int Pair(Expanded ex, int[] position, int u, int v)
        {
            var count = 0;

            foreach (var neighbours in new[] { ex.Up, ex.Down })
            {
                foreach (var a in neighbours[u])
                {
                    foreach (var b in neighbours[v])
                    {
                        if (position[a] > position[b])
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private static int Count(List<List<int>> layers, Expanded ex, int[] position)
        {
            var total = 0;

            for (var l = 0; l + 1 < layers.Count; l++)
            {
                var edges = new List<(int A, int B)>();

                foreach (var u in layers[l])
                {
                    foreach (var v in ex.Down[u])
                    {
                        edges.Add((position[u], position[v]));
                    }
                }

                for (var i = 0; i < edges.Count; i++)
                {
                    for (var j = i + 1; j < edges.Count; j++)
                    {
                        if ((edges[i].A - edges[j].A) * (edges[i].B - edges[j].B) < 0)
                        {
                            total++;
                        }
                    }
                }
            }

            return total;
        }

        private static double[] AssignX(List<List<int>> layers, Expanded ex, GraphDiagramSpec spec)
        {
            var count = ex.Layer.Count;
            var x = new double[count];
            var real = spec.Nodes.Count;

            double Gap(int a, int b)
            {
                var g = a >= real || b >= real ? 12.0 : 26.0;

                return (ex.Width[a] + ex.Width[b]) / 2 + g;
            }

            foreach (var layer in layers)
            {
                var cursor = 0.0;

                for (var i = 0; i < layer.Count; i++)
                {
                    cursor += i == 0 ? 0 : Gap(layer[i - 1], layer[i]);
                    x[layer[i]] = cursor;
                }
            }

            void Settle(List<int> layer, List<List<int>> neighbours)
            {
                var desired = new double[layer.Count];
                var weight = new double[layer.Count];

                for (var i = 0; i < layer.Count; i++)
                {
                    var node = layer[i];
                    var list = neighbours[node];

                    if (list.Count == 0)
                    {
                        desired[i] = x[node];
                        weight[i] = 0.0001;
                    }
                    else
                    {
                        desired[i] = list.Average(m => x[m]);
                        weight[i] = list.Count * (node >= real ? 4 : 1);
                    }
                }

                var offsets = new double[layer.Count];

                for (var i = 1; i < layer.Count; i++)
                {
                    offsets[i] = offsets[i - 1] + Gap(layer[i - 1], layer[i]);
                }

                var solved = Isotonic([.. desired.Select((d, i) => d - offsets[i])], weight);

                for (var i = 0; i < layer.Count; i++)
                {
                    x[layer[i]] = solved[i] + offsets[i];
                }
            }

            for (var pass = 0; pass < 10; pass++)
            {
                for (var l = 1; l < layers.Count; l++)
                {
                    Settle(layers[l], ex.Up);
                }

                for (var l = layers.Count - 2; l >= 0; l--)
                {
                    Settle(layers[l], ex.Down);
                }
            }

            for (var l = 1; l < layers.Count; l++)
            {
                Settle(layers[l], ex.Up);
            }

            return x;
        }

        private static double[] Isotonic(double[] values, double[] weights)
        {
            var blocks = new List<(double Sum, double Weight, int Count)>();

            for (var i = 0; i < values.Length; i++)
            {
                blocks.Add((values[i] * weights[i], weights[i], 1));

                while (blocks.Count > 1)
                {
                    var b = blocks[^1];
                    var a = blocks[^2];

                    if (a.Sum / a.Weight <= b.Sum / b.Weight)
                    {
                        break;
                    }

                    blocks.RemoveRange(blocks.Count - 2, 2);
                    blocks.Add((a.Sum + b.Sum, a.Weight + b.Weight, a.Count + b.Count));
                }
            }

            var result = new double[values.Length];
            var at = 0;

            foreach (var block in blocks)
            {
                var mean = block.Sum / block.Weight;

                for (var i = 0; i < block.Count; i++)
                {
                    result[at++] = mean;
                }
            }

            return result;
        }

        private static GraphLayoutResult Circular(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            var n = spec.Nodes.Count;

            if (n > DiagramLimits.MaxCircularNodes)
            {
                throw new DiagramTooLargeException("circular graph nodes", n, DiagramLimits.MaxCircularNodes,
                    "Use the layered layout or draw fewer states.");
            }

            var sizes = spec.Nodes.Select(NodeSize).ToArray();
            var maxSize = sizes.Max(s => Math.Max(s.W, s.H));
            var radius = n == 1 ? 0 : n == 2 ? 120 : Math.Max(130, (maxSize + 72) / (2 * Math.Sin(Math.PI / n)));
            var centres = new (double X, double Y)[n];

            for (var i = 0; i < n; i++)
            {
                var angle = n == 2 ? i == 0 ? Math.PI : 0 : -Math.PI / 2 + 2 * Math.PI * i / n;
                centres[i] = (radius * Math.Cos(angle), radius * Math.Sin(angle));
            }

            var edges = spec.Edges.Select(e => (From: index[e.From], To: index[e.To])).ToArray();
            var reciprocal = new HashSet<(int, int)>(edges);
            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;

            void Extend(double x, double y)
            {
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            for (var i = 0; i < n; i++)
            {
                Extend(centres[i].X - sizes[i].W / 2, centres[i].Y - sizes[i].H / 2);
                Extend(centres[i].X + sizes[i].W / 2, centres[i].Y + sizes[i].H / 2);
            }

            var result = new List<PlacedGraphEdge>();

            for (var k = 0; k < edges.Length; k++)
            {
                var (from, to) = edges[k];

                if (from == to)
                {
                    var (cx, cy) = centres[from];
                    var outward = n == 1 ? -Math.PI / 2 : Math.Atan2(cy, cx);
                    var r = Math.Max(sizes[from].W, sizes[from].H) / 2;

                    (double X, double Y) On(double a, double d)
                    {
                        return (cx + d * Math.Cos(outward + a), cy + d * Math.Sin(outward + a));
                    }

                    var p0 = spec.Nodes[from].Role == GraphNodeRole.State ? On(-0.5, r) : On(-0.35, r + 2);
                    var p1 = spec.Nodes[from].Role == GraphNodeRole.State ? On(0.5, r) : On(0.35, r + 2);
                    var c1 = On(-0.62, r + 54);
                    var c2 = On(0.62, r + 54);
                    var path = new PathBuilder().Move(p0.X, p0.Y).Cubic(c1.X, c1.Y, c2.X, c2.Y, p1.X, p1.Y).ToString();
                    var mid = NetworkRenderer.Bezier(p0.X, p0.Y, c1.X, c1.Y, c2.X, c2.Y, p1.X, p1.Y, 0.5);
                    var label = On(0, r + 46);
                    var dir = (X: p1.X - c2.X, Y: p1.Y - c2.Y);
                    var len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                    Extend(mid.X, mid.Y);
                    Extend(label.X - 24, label.Y - 10);
                    Extend(label.X + 24, label.Y + 10);
                    Extend(c1.X, c1.Y);
                    Extend(c2.X, c2.Y);

                    result.Add(new PlacedGraphEdge(k, from, to, path, label.X, label.Y + 3, p1.X, p1.Y, dir.X / len,
                        dir.Y / len, true, false, [p0, mid, p1]));

                    continue;
                }

                var a = centres[from];
                var b = centres[to];
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var both = reciprocal.Contains((to, from));
                var bend = distance * (both ? 0.26 : 0.12);
                var mx = (a.X + b.X) / 2 + dy / distance * bend;
                var my = (a.Y + b.Y) / 2 - dx / distance * bend;
                var start = Clip(centres[from], sizes[from], spec.Nodes[from].Role, (mx, my));
                var end = Clip(centres[to], sizes[to], spec.Nodes[to].Role, (mx, my));
                var q1 = (X: start.X + 2.0 / 3 * (mx - start.X), Y: start.Y + 2.0 / 3 * (my - start.Y));
                var q2 = (X: end.X + 2.0 / 3 * (mx - end.X), Y: end.Y + 2.0 / 3 * (my - end.Y));

                var pathText = new PathBuilder().Move(start.X, start.Y).Cubic(q1.X, q1.Y, q2.X, q2.Y, end.X, end.Y)
                    .ToString();

                var midpoint = NetworkRenderer.Bezier(start.X, start.Y, q1.X, q1.Y, q2.X, q2.Y, end.X, end.Y, 0.5);
                var tx = end.X - q2.X;
                var ty = end.Y - q2.Y;
                var tl = Math.Sqrt(tx * tx + ty * ty);
                Extend(midpoint.X - 24, midpoint.Y - 12);
                Extend(midpoint.X + 24, midpoint.Y + 12);

                result.Add(new PlacedGraphEdge(k, from, to, pathText, midpoint.X, midpoint.Y + 3, end.X, end.Y, tx / tl,
                    ty / tl, false, false, [start, midpoint, end]));
            }

            var margin = 14.0;
            var width = maxX - minX + 2 * margin;
            var height = maxY - minY + 2 * margin;
            var shiftX = margin - minX;
            var shiftY = margin - minY;
            var nodes = new PlacedGraphNode[n];

            for (var i = 0; i < n; i++)
            {
                nodes[i] = new PlacedGraphNode(i, spec.Nodes[i].Id, spec.Nodes[i].Label, spec.Nodes[i].Role,
                    centres[i].X + shiftX, centres[i].Y + shiftY, sizes[i].W, sizes[i].H, 0);
            }

            var shifted = result.Select(e => Shift(e, shiftX, shiftY)).ToList();

            return new GraphLayoutResult(nodes, shifted, width, height, GraphLayoutKind.Circular, 1, 0);
        }

        private static PlacedGraphEdge Shift(PlacedGraphEdge e, double dx, double dy)
        {
            var path = Regex.Replace(e.Path, "(-?[0-9.]+) (-?[0-9.]+)", m =>
                Num.F(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) + dx) + " " +
                Num.F(double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) + dy));

            return e with
            {
                Path = path, LabelX = e.LabelX + dx, LabelY = e.LabelY + dy, TipX = e.TipX + dx, TipY = e.TipY + dy,
                Points = [.. e.Points.Select(p => (p.X + dx, p.Y + dy))]
            };
        }

        private static (double X, double Y) Clip((double X, double Y) centre, (double W, double H) size,
            GraphNodeRole role,
            (double X, double Y) toward)
        {
            var dx = toward.X - centre.X;
            var dy = toward.Y - centre.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length < 1e-9)
            {
                return centre;
            }

            var ux = dx / length;
            var uy = dy / length;

            if (role == GraphNodeRole.State)
            {
                var r = size.W / 2;

                return (centre.X + ux * r, centre.Y + uy * r);
            }

            var tx = Math.Abs(ux) < 1e-9 ? double.PositiveInfinity : size.W / 2 / Math.Abs(ux);
            var ty = Math.Abs(uy) < 1e-9 ? double.PositiveInfinity : size.H / 2 / Math.Abs(uy);
            var t = Math.Min(tx, ty);

            return (centre.X + ux * t, centre.Y + uy * t);
        }

        private sealed class Expanded
        {
            public List<int> Layer { get; } = [];
            public List<double> Width { get; } = [];
            public List<List<int>> Up { get; } = [];
            public List<List<int>> Down { get; } = [];

            public int Add(int layer, double width)
            {
                Layer.Add(layer);
                Width.Add(width);
                Up.Add([]);
                Down.Add([]);

                return Layer.Count - 1;
            }
        }
    }
}