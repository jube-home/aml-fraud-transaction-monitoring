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
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Svg;

namespace Jube.Charting.Diagrams
{
    internal static class NetworkGeometry
    {
        public const double BaseRadius = 14;
        public const double LabelHeight = 16;
        public const double LabelWidth = 96;
        public const double Margin = 10;

        public static (double W, double H)[] Sizes(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            var n = spec.Nodes.Count;
            var sizes = new (double W, double H)[n];

            if (spec.Size == GraphMeasure.None)
            {
                for (var i = 0; i < n; i++)
                {
                    sizes[i] = (2 * BaseRadius, 2 * BaseRadius);
                }

                return sizes;
            }

            var degree = new double[n];

            foreach (var e in spec.Edges)
            {
                var a = index[e.From];
                var b = index[e.To];
                degree[a]++;

                if (a != b)
                {
                    degree[b]++;
                }
            }

            var values = new double[n];

            for (var i = 0; i < n; i++)
            {
                var size = spec.Nodes[i].Size;

                values[i] = size is { } s && double.IsFinite(s) ? Math.Max(s, 0)
                    : spec.Size == GraphMeasure.Degree ? degree[i] : 0;
            }

            var min = values.Min();
            var max = values.Max();

            for (var i = 0; i < n; i++)
            {
                var share = max > min ? (values[i] - min) / (max - min) : 0;
                var r = Math.Clamp(BaseRadius * (1 + 1.5 * share), BaseRadius, 2.5 * BaseRadius);
                sizes[i] = (2 * r, 2 * r);
            }

            return sizes;
        }

        public static (int From, int To)[] Edges(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            return [.. spec.Edges.Select(e => (From: index[e.From], To: index[e.To]))];
        }

        public static List<int>[] Adjacency(int n, (int From, int To)[] edges)
        {
            var sets = new SortedSet<int>[n];

            for (var i = 0; i < n; i++)
            {
                sets[i] = [];
            }

            foreach (var (from, to) in edges)
            {
                if (from != to)
                {
                    sets[from].Add(to);
                    sets[to].Add(from);
                }
            }

            return [.. sets.Select(s => s.ToList())];
        }

        public static int[][] Distances(int n, List<int>[] adjacency, int cap)
        {
            var result = new int[n][];

            for (var s = 0; s < n; s++)
            {
                var d = new int[n];
                Array.Fill(d, -1);
                d[s] = 0;
                var queue = new Queue<int>();
                queue.Enqueue(s);

                while (queue.Count > 0)
                {
                    var u = queue.Dequeue();

                    if (d[u] >= cap)
                    {
                        continue;
                    }

                    foreach (var v in adjacency[u])
                    {
                        if (d[v] < 0)
                        {
                            d[v] = d[u] + 1;
                            queue.Enqueue(v);
                        }
                    }
                }

                for (var i = 0; i < n; i++)
                {
                    if (d[i] < 0 || d[i] > cap)
                    {
                        d[i] = cap + 1;
                    }
                }

                result[s] = d;
            }

            return result;
        }

        public static (double X, double Y) Direction(int seed)
        {
            var angle = seed * 2.399963229728653;

            return (Math.Cos(angle), Math.Sin(angle));
        }

        public static void Separate((double X, double Y)[] centres, (double W, double H)[] sizes, bool[] fixedNode,
            double padX, double padY)
        {
            var n = centres.Length;

            for (var round = 0; round < 80; round++)
            {
                var moved = false;

                for (var i = 0; i < n; i++)
                {
                    for (var j = i + 1; j < n; j++)
                    {
                        if (fixedNode[i] && fixedNode[j])
                        {
                            continue;
                        }

                        var reqX = (sizes[i].W + sizes[j].W) / 2 + padX;
                        var reqY = (sizes[i].H + sizes[j].H) / 2 + padY;
                        var dx = centres[j].X - centres[i].X;
                        var dy = centres[j].Y - centres[i].Y;

                        if (Math.Abs(dx) >= reqX || Math.Abs(dy) >= reqY)
                        {
                            continue;
                        }

                        if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9)
                        {
                            var (ux, uy) = Direction(i * 31 + j);
                            dx = ux * 0.5;
                            dy = uy * 0.5;
                        }

                        var sx = Math.Abs(dx) < 1e-9 ? double.PositiveInfinity : reqX / Math.Abs(dx);
                        var sy = Math.Abs(dy) < 1e-9 ? double.PositiveInfinity : reqY / Math.Abs(dy);
                        var s = Math.Min(sx, sy) + 0.01;
                        var shiftX = dx * (s - 1);
                        var shiftY = dy * (s - 1);

                        if (fixedNode[i])
                        {
                            centres[j] = (centres[j].X + shiftX, centres[j].Y + shiftY);
                        }
                        else if (fixedNode[j])
                        {
                            centres[i] = (centres[i].X - shiftX, centres[i].Y - shiftY);
                        }
                        else
                        {
                            centres[i] = (centres[i].X - shiftX / 2, centres[i].Y - shiftY / 2);
                            centres[j] = (centres[j].X + shiftX / 2, centres[j].Y + shiftY / 2);
                        }

                        moved = true;
                    }
                }

                if (!moved)
                {
                    return;
                }
            }
        }

        public static (double MinX, double MinY, double MaxX, double MaxY) Bounds((double X, double Y)[] centres,
            (double W, double H)[] sizes)
        {
            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;

            for (var i = 0; i < centres.Length; i++)
            {
                var halfW = Math.Max(sizes[i].W, LabelWidth) / 2;
                minX = Math.Min(minX, centres[i].X - halfW);
                maxX = Math.Max(maxX, centres[i].X + halfW);
                minY = Math.Min(minY, centres[i].Y - sizes[i].H / 2);
                maxY = Math.Max(maxY, centres[i].Y + sizes[i].H / 2 + LabelHeight);
            }

            return (minX, minY, maxX, maxY);
        }

        public static bool FitExtent((double X, double Y)[] centres, (double W, double H)[] sizes, double maxExtent)
        {
            var (minX, minY, maxX, maxY) = Bounds(centres, sizes);
            var limit = maxExtent - 2 * Margin;
            var width = maxX - minX;
            var height = maxY - minY;

            if (width <= limit && height <= limit)
            {
                return false;
            }

            var factor = Math.Min(limit / Math.Max(width, 1), limit / Math.Max(height, 1));
            var cx = (minX + maxX) / 2;
            var cy = (minY + maxY) / 2;

            for (var i = 0; i < centres.Length; i++)
            {
                centres[i] = (cx + (centres[i].X - cx) * factor, cy + (centres[i].Y - cy) * factor);
            }

            return true;
        }

        public static (PlacedGraphNode[] Nodes, double Width, double Height) Place(GraphDiagramSpec spec,
            (double X, double Y)[] centres, (double W, double H)[] sizes, int[] layers, double leftPad,
            double bottomPad, bool keepFrame = false)
        {
            var (minX, minY, maxX, maxY) = Bounds(centres, sizes);
            var shiftX = keepFrame ? Math.Max(0, Margin + leftPad - minX) : Margin + leftPad - minX;
            var shiftY = keepFrame ? Math.Max(0, Margin - minY) : Margin - minY;

            if (keepFrame)
            {
                minX = Math.Min(minX, Margin + leftPad);
                minY = Math.Min(minY, Margin);
            }

            var nodes = new PlacedGraphNode[centres.Length];

            for (var i = 0; i < centres.Length; i++)
            {
                nodes[i] = new PlacedGraphNode(i, spec.Nodes[i].Id, spec.Nodes[i].Label, spec.Nodes[i].Role,
                    centres[i].X + shiftX, centres[i].Y + shiftY, sizes[i].W, sizes[i].H, layers[i]);
            }

            return (nodes, maxX - minX + 2 * Margin + leftPad, maxY - minY + 2 * Margin + bottomPad);
        }

        public static List<PlacedGraphEdge> Links(PlacedGraphNode[] nodes, (int From, int To)[] edges)
        {
            var reciprocal = new HashSet<(int, int)>(edges);
            var result = new List<PlacedGraphEdge>();

            for (var k = 0; k < edges.Length; k++)
            {
                var (from, to) = edges[k];

                result.Add(from == to
                    ? Loop(k, nodes[from], nodes)
                    : Link(k, from, to, nodes[from], nodes[to], reciprocal.Contains((to, from)) ? 0.22 : 0));
            }

            return result;
        }

        public static PlacedGraphEdge Link(int k, int from, int to, PlacedGraphNode a, PlacedGraphNode b, double bend)
        {
            var ra = Math.Max(a.Width, a.Height) / 2;
            var rb = Math.Max(b.Width, b.Height) / 2;
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var distance = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1e-6);
            var mx = (a.X + b.X) / 2 + dy / distance * bend * distance;
            var my = (a.Y + b.Y) / 2 - dx / distance * bend * distance;
            var start = Toward((a.X, a.Y), ra, (mx, my));
            var end = Toward((b.X, b.Y), rb, (mx, my));

            if (bend == 0)
            {
                var path = new PathBuilder().Move(start.X, start.Y).Line(end.X, end.Y).ToString();
                var ux = dx / distance;
                var uy = dy / distance;

                return new PlacedGraphEdge(k, from, to, path, (start.X + end.X) / 2, (start.Y + end.Y) / 2 - 4, end.X,
                    end.Y, ux, uy, false, false, [start, end]);
            }

            var c1 = (X: start.X + 2.0 / 3 * (mx - start.X), Y: start.Y + 2.0 / 3 * (my - start.Y));
            var c2 = (X: end.X + 2.0 / 3 * (mx - end.X), Y: end.Y + 2.0 / 3 * (my - end.Y));
            var curved = new PathBuilder().Move(start.X, start.Y).Cubic(c1.X, c1.Y, c2.X, c2.Y, end.X, end.Y).ToString();
            var mid = NetworkRenderer.Bezier(start.X, start.Y, c1.X, c1.Y, c2.X, c2.Y, end.X, end.Y, 0.5);
            var tx = end.X - c2.X;
            var ty = end.Y - c2.Y;
            var tl = Math.Max(Math.Sqrt(tx * tx + ty * ty), 1e-6);

            return new PlacedGraphEdge(k, from, to, curved, mid.X, mid.Y - 4, end.X, end.Y, tx / tl, ty / tl, false,
                false, [start, mid, end]);
        }

        public static PlacedGraphEdge Loop(int k, PlacedGraphNode node, PlacedGraphNode[] nodes)
        {
            var cx = nodes.Length == 0 ? node.X : nodes.Average(p => p.X);
            var cy = nodes.Length == 0 ? node.Y : nodes.Average(p => p.Y);
            var outward = Math.Abs(node.X - cx) < 1e-6 && Math.Abs(node.Y - cy) < 1e-6
                ? -Math.PI / 2
                : Math.Atan2(node.Y - cy, node.X - cx);
            var r = Math.Max(node.Width, node.Height) / 2;

            (double X, double Y) On(double a, double d)
            {
                return (node.X + d * Math.Cos(outward + a), node.Y + d * Math.Sin(outward + a));
            }

            var p0 = On(-0.5, r);
            var p1 = On(0.5, r);
            var c1 = On(-0.62, r + 40);
            var c2 = On(0.62, r + 40);
            var path = new PathBuilder().Move(p0.X, p0.Y).Cubic(c1.X, c1.Y, c2.X, c2.Y, p1.X, p1.Y).ToString();
            var mid = NetworkRenderer.Bezier(p0.X, p0.Y, c1.X, c1.Y, c2.X, c2.Y, p1.X, p1.Y, 0.5);
            var label = On(0, r + 36);
            var dir = (X: p1.X - c2.X, Y: p1.Y - c2.Y);
            var len = Math.Max(Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y), 1e-6);

            return new PlacedGraphEdge(k, node.Index, node.Index, path, label.X, label.Y + 3, p1.X, p1.Y, dir.X / len,
                dir.Y / len, true, false, [p0, mid, p1]);
        }

        public static (double X, double Y) Toward((double X, double Y) centre, double radius, (double X, double Y) to)
        {
            var dx = to.X - centre.X;
            var dy = to.Y - centre.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length < 1e-9)
            {
                return centre;
            }

            return (centre.X + dx / length * radius, centre.Y + dy / length * radius);
        }
    }
}
