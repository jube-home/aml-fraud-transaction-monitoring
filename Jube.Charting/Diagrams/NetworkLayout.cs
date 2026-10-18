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
using Jube.Charting.Text;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams
{
    public static class NetworkLayout
    {
        public static NetworkLayoutResult Compute(NetworkDiagramSpec spec)
        {
            var layers = spec.Layers;

            if (layers.Count < 2)
            {
                throw new ChartSpecificationException("A network diagram needs at least an input and an output layer.");
            }

            if (layers.Count > DiagramLimits.MaxNetworkLayers)
            {
                throw new DiagramTooLargeException("network layers", layers.Count, DiagramLimits.MaxNetworkLayers,
                    "Draw a summary of the network or split it into stages.");
            }

            long totalNodes = layers.Sum(l => l.Nodes.Count);

            if (totalNodes > DiagramLimits.MaxNetworkInputNodes)
            {
                throw new DiagramTooLargeException("network nodes", totalNodes, DiagramLimits.MaxNetworkInputNodes,
                    "Draw a summary of the network.");
            }

            long totalEdges = 0;

            for (var l = 1; l < layers.Count; l++)
            {
                totalEdges += (long)layers[l].Nodes.Count * layers[l - 1].Nodes.Count;
            }

            if (totalEdges > DiagramLimits.MaxNetworkInputEdges)
            {
                throw new DiagramTooLargeException("network connections", totalEdges,
                    DiagramLimits.MaxNetworkInputEdges,
                    "Draw a summary of the network.");
            }

            var weighted = layers.Skip(1).Any(l => l.Weights is not null);

            if (weighted && !layers.Skip(1)
                    .Any(l => l.Weights is not null && l.Weights.Any(r => r.Any(double.IsFinite))))
            {
                throw new DiagramNotSupportedException(
                    "the network has no finite weights, so there is nothing to draw.");
            }

            var cap = Math.Max(4, Math.Min(spec.MaxNodesPerLayer, DiagramLimits.MaxVisibleNetworkNodes / layers.Count));
            var edgeCap = Math.Max(10, spec.MaxEdges);
            List<int[]> keep;

            while (true)
            {
                keep = SelectNodes(spec, cap);

                if (weighted || cap <= 4)
                {
                    break;
                }

                long unweighted = 0;

                for (var l = 1; l < layers.Count; l++)
                {
                    unweighted += (long)keep[l].Length * keep[l - 1].Length;
                }

                if (unweighted <= edgeCap)
                {
                    break;
                }

                cap--;
            }

            var nodes = new List<LaidNode>();
            var indexOf = new Dictionary<(int Layer, int Node), int>();
            var maxCount = keep.Max(k => k.Length);
            var pitch = maxCount <= 6 ? 46.0 : maxCount <= 14 ? 34.0 : 27.0;
            var radius = Math.Clamp(pitch * 0.36, 7, 14);
            var bodyHeight = maxCount * pitch;
            var leftLabel = Math.Min(TextMetrics.MaxWidth(layers[0].Nodes.Take(cap + 1).Select(n => n.Label), 11), 150);

            var rightLabel = Math.Min(TextMetrics.MaxWidth(layers[^1].Nodes.Take(cap + 1).Select(n => n.Label), 11),
                150);

            var columnX = new double[layers.Count];
            var x0 = leftLabel + (leftLabel > 0 ? 14 : 0) + radius + 4;

            for (var l = 0; l < layers.Count; l++)
            {
                columnX[l] = x0 + l * ColumnGap;
            }

            for (var l = 0; l < layers.Count; l++)
            {
                var ids = keep[l];
                var top = HeaderHeight + (bodyHeight - ids.Length * pitch) / 2;

                for (var s = 0; s < ids.Length; s++)
                {
                    var y = top + (s + 0.5) * pitch;
                    var role = RoleOf(spec, l);

                    if (ids[s] < 0)
                    {
                        var hidden = layers[l].Nodes.Count - (ids.Length - 1);
                        var text = "+" + hidden;
                        var half = Math.Max(radius, TextMetrics.Width(text, 10) / 2 + 6);
                        indexOf[(l, -1)] = nodes.Count;

                        nodes.Add(new LaidNode(nodes.Count, l, s, text, columnX[l], y, half, radius, true, hidden, role,
                            null, hidden));
                    }
                    else
                    {
                        indexOf[(l, ids[s])] = nodes.Count;
                        var source = layers[l].Nodes[ids[s]];

                        nodes.Add(new LaidNode(nodes.Count, l, s, source.Label, columnX[l], y, radius, radius, false, 0,
                            role, source.Bias, 1, source.Emphasis, source.EmphasisText));
                    }
                }
            }

            var edges = new List<LaidEdge>();

            for (var l = 1; l < layers.Count; l++)
            {
                var w = layers[l].Weights;

                foreach (var to in keep[l])
                {
                    foreach (var from in keep[l - 1])
                    {
                        var (weight, count) = EdgeWeight(spec, l, from, to, keep[l], keep[l - 1]);

                        if (count == 0)
                        {
                            continue;
                        }

                        var aggregate = from < 0 || to < 0;

                        edges.Add(new LaidEdge(indexOf[(l - 1, from)], indexOf[(l, to)],
                            w is null ? double.NaN : weight, aggregate, count));
                    }
                }
            }

            var before = edges.Count;

            if (edges.Count > edgeCap)
            {
                edges = Elide(edges, edgeCap);
            }

            var width = columnX[^1] + radius + (rightLabel > 0 ? rightLabel + 14 : 0) + 4;
            var height = HeaderHeight + bodyHeight + 8;

            return new NetworkLayoutResult(nodes, edges, columnX, width, height, HeaderHeight, leftLabel, rightLabel,
                (int)totalNodes, totalEdges, before);
        }

        private const double ColumnGap = 190;
        private const double HeaderHeight = 44;

        internal static NetworkRole RoleOf(NetworkDiagramSpec spec, int layer)
        {
            return spec.Layers[layer].Role ?? (layer == 0 ? NetworkRole.Input
                : layer == spec.Layers.Count - 1 ? NetworkRole.Output : NetworkRole.Hidden);
        }

        private static List<int[]> SelectNodes(NetworkDiagramSpec spec, int cap)
        {
            var layers = spec.Layers;
            var result = new List<int[]>();

            for (var l = 0; l < layers.Count; l++)
            {
                var n = layers[l].Nodes.Count;

                if (n <= cap)
                {
                    result.Add([.. Enumerable.Range(0, n)]);
                    continue;
                }

                var score = new double[n];

                for (var i = 0; i < n; i++)
                {
                    score[i] = Importance(spec, l, i);
                }

                var chosen = Enumerable.Range(0, n).OrderByDescending(i => score[i]).ThenBy(i => i).Take(cap - 1)
                    .OrderBy(i => i).ToList();
                chosen.Add(-1);
                result.Add([.. chosen]);
            }

            return result;
        }

        private static double Importance(NetworkDiagramSpec spec, int l, int i)
        {
            var layers = spec.Layers;
            var total = 0.0;
            var own = layers[l].Weights;

            if (l > 0 && own is not null)
            {
                total += own[i].Where(double.IsFinite).Sum(Math.Abs);
            }

            if (l + 1 < layers.Count && layers[l + 1].Weights is { } next)
            {
                foreach (var row in next)
                {
                    if (i < row.Count && double.IsFinite(row[i]))
                    {
                        total += Math.Abs(row[i]);
                    }
                }
            }

            return total;
        }

        private static (double Weight, int Count) EdgeWeight(NetworkDiagramSpec spec, int l, int from, int to,
            int[] toKeep, int[] fromKeep)
        {
            var layers = spec.Layers;
            var w = layers[l].Weights;

            if (w is null)
            {
                return (double.NaN, 1);
            }

            var fromSet = from >= 0 ? [from] : Hidden(fromKeep, layers[l - 1].Nodes.Count);
            var toSet = to >= 0 ? [to] : Hidden(toKeep, layers[l].Nodes.Count);
            var sum = 0.0;
            var count = 0;

            foreach (var t in toSet)
            {
                var row = w[t];

                foreach (var f in fromSet)
                {
                    var v = row[f];

                    if (double.IsFinite(v))
                    {
                        sum += v;
                        count++;
                    }
                }
            }

            return (count == 0 ? 0 : sum / count, count);
        }

        private static List<int> Hidden(int[] keep, int total)
        {
            var shown = new HashSet<int>(keep.Where(k => k >= 0));
            var result = new List<int>(total - shown.Count);

            for (var i = 0; i < total; i++)
            {
                if (!shown.Contains(i))
                {
                    result.Add(i);
                }
            }

            return result;
        }

        private static List<LaidEdge> Elide(List<LaidEdge> edges, int cap)
        {
            var chosen = new HashSet<int>();
            var strongestIn = new Dictionary<int, int>();

            for (var i = 0; i < edges.Count; i++)
            {
                var e = edges[i];

                if (!strongestIn.TryGetValue(e.To, out var best) ||
                    Math.Abs(edges[best].Weight) < Math.Abs(e.Weight))
                {
                    strongestIn[e.To] = i;
                }
            }

            foreach (var i in strongestIn.Values.OrderBy(i => i))
            {
                if (chosen.Count < cap / 2)
                {
                    chosen.Add(i);
                }
            }

            foreach (var i in Enumerable.Range(0, edges.Count)
                         .OrderByDescending(i => Math.Abs(edges[i].Weight)).ThenBy(i => i))
            {
                if (chosen.Count >= cap)
                {
                    break;
                }

                chosen.Add(i);
            }

            return [.. chosen.OrderBy(i => i).Select(i => edges[i])];
        }
    }
}