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

namespace Jube.Charting.Diagrams
{
    public static class ForceLayout
    {
        public const int Iterations = 150;
        public const int DistanceCap = 6;
        public const double AnchorWeight = 1000;

        public static GraphLayoutResult Compute(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            var n = spec.Nodes.Count;
            var sizes = NetworkGeometry.Sizes(spec, index);
            var edges = NetworkGeometry.Edges(spec, index);
            var adjacency = NetworkGeometry.Adjacency(n, edges);
            var distances = NetworkGeometry.Distances(n, adjacency, DistanceCap);
            var unit = sizes.Max(s => Math.Max(s.W, s.H)) + 56;
            var anchors = new (double X, double Y)?[n];

            foreach (var anchor in spec.Anchors ?? [])
            {
                if (index.TryGetValue(anchor.Id, out var i) && double.IsFinite(anchor.X) && double.IsFinite(anchor.Y))
                {
                    anchors[i] = (anchor.X, anchor.Y);
                }
            }

            var centres = Initial(n, adjacency, anchors, unit);

            for (var iteration = 0; iteration < Iterations && n > 1; iteration++)
            {
                var next = new (double X, double Y)[n];

                for (var i = 0; i < n; i++)
                {
                    var sumX = 0.0;
                    var sumY = 0.0;
                    var sumW = 0.0;

                    for (var j = 0; j < n; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        var d = distances[i][j];
                        var w = 1.0 / (d * d);
                        var ideal = d * unit;
                        var dx = centres[i].X - centres[j].X;
                        var dy = centres[i].Y - centres[j].Y;
                        var length = Math.Sqrt(dx * dx + dy * dy);

                        if (length < 1e-9)
                        {
                            (dx, dy) = NetworkGeometry.Direction(i * 97 + j);
                            length = 1;
                        }

                        sumX += w * (centres[j].X + ideal * dx / length);
                        sumY += w * (centres[j].Y + ideal * dy / length);
                        sumW += w;
                    }

                    if (anchors[i] is { } a)
                    {
                        sumX += AnchorWeight * a.X;
                        sumY += AnchorWeight * a.Y;
                        sumW += AnchorWeight;
                    }

                    next[i] = (sumX / sumW, sumY / sumW);
                }

                centres = next;
            }

            var fixedNode = anchors.Select(a => a.HasValue).ToArray();
            NetworkGeometry.Separate(centres, sizes, fixedNode, 12, NetworkGeometry.LabelHeight + 6);

            if (NetworkGeometry.FitExtent(centres, sizes, spec.MaxExtent))
            {
                NetworkGeometry.Separate(centres, sizes, new bool[n], 12, NetworkGeometry.LabelHeight + 6);
            }

            var (nodes, width, height) = NetworkGeometry.Place(spec, centres, sizes, new int[n], 0, 0,
                fixedNode.Any(f => f));
            var links = NetworkGeometry.Links(nodes, edges);

            return new GraphLayoutResult(nodes, links, width, height, GraphLayoutKind.Force, 1, 0);
        }

        private static (double X, double Y)[] Initial(int n, List<int>[] adjacency, (double X, double Y)?[] anchors,
            double unit)
        {
            var centres = new (double X, double Y)[n];
            var radius = Math.Max(unit, n * unit / (2 * Math.PI));

            for (var i = 0; i < n; i++)
            {
                if (anchors[i] is { } a)
                {
                    centres[i] = a;

                    continue;
                }

                var neighbour = adjacency[i].FirstOrDefault(j => anchors[j].HasValue, -1);

                if (neighbour >= 0)
                {
                    var (ux, uy) = NetworkGeometry.Direction(i);
                    var at = anchors[neighbour]!.Value;
                    centres[i] = (at.X + ux * unit * 0.6, at.Y + uy * unit * 0.6);

                    continue;
                }

                var angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                centres[i] = (radius * Math.Cos(angle), radius * Math.Sin(angle));
            }

            return centres;
        }
    }
}
