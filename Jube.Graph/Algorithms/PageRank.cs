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

using Jube.Graph.Model;

namespace Jube.Graph.Algorithms
{
    public static class PageRank
    {
        public static double[] Compute(CompressedGraph graph, double damping = 0.85, double tolerance = 1e-9,
            int maxIterations = 100)
        {
            ArgumentNullException.ThrowIfNull(graph);

            if (damping <= 0 || damping >= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(damping));
            }

            var n = graph.NodeCount;

            if (n == 0)
            {
                return [];
            }

            var outWeight = new double[n];

            for (var u = 0; u < n; u++)
            {
                outWeight[u] = graph.WeightedDegree(u);
            }

            var rank = new double[n];
            Array.Fill(rank, 1d / n);
            var next = new double[n];

            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                Array.Clear(next);
                var dangling = 0d;

                for (var u = 0; u < n; u++)
                {
                    if (outWeight[u] <= 0)
                    {
                        dangling += rank[u];

                        continue;
                    }

                    var share = rank[u] / outWeight[u];

                    for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                    {
                        next[graph.Neighbours[e]] += share * graph.Weights[e];
                    }
                }

                var baseline = (1d - damping) / n + damping * dangling / n;
                var delta = 0d;

                for (var u = 0; u < n; u++)
                {
                    next[u] = baseline + damping * next[u];
                    delta += Math.Abs(next[u] - rank[u]);
                }

                (rank, next) = (next, rank);

                if (delta < tolerance)
                {
                    break;
                }
            }

            return rank;
        }

        public static double[] Personalised(CompressedGraph graph, Seeds seeds, double alpha = 0.15,
            double epsilon = 1e-6)
        {
            ArgumentNullException.ThrowIfNull(graph);
            ArgumentNullException.ThrowIfNull(seeds);

            if (alpha <= 0 || alpha >= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(alpha));
            }

            if (epsilon <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(epsilon));
            }

            var n = graph.NodeCount;
            var estimate = new double[n];
            var residual = new double[n];
            var restart = seeds.Positive().Distinct().OrderBy(i => i).ToArray();

            if (restart.Length == 0)
            {
                restart = seeds.Indices.Distinct().OrderBy(i => i).ToArray();
            }

            if (restart.Length == 0 || n == 0)
            {
                return estimate;
            }

            var outWeight = new double[n];

            for (var u = 0; u < n; u++)
            {
                outWeight[u] = graph.WeightedDegree(u);
            }

            var queued = new bool[n];
            var queue = new Queue<int>();

            foreach (var s in restart)
            {
                residual[s] += 1d / restart.Length;
            }

            foreach (var s in restart)
            {
                if (Ready(s, residual, outWeight, graph, epsilon) && !queued[s])
                {
                    queued[s] = true;
                    queue.Enqueue(s);
                }
            }

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                queued[u] = false;

                if (!Ready(u, residual, outWeight, graph, epsilon))
                {
                    continue;
                }

                var r = residual[u];
                residual[u] = 0d;
                estimate[u] += alpha * r;

                if (outWeight[u] <= 0)
                {
                    var share = (1d - alpha) * r / restart.Length;

                    foreach (var s in restart)
                    {
                        residual[s] += share;
                        Enqueue(s, residual, outWeight, graph, epsilon, queued, queue);
                    }

                    continue;
                }

                var push = (1d - alpha) * r / outWeight[u];

                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    var v = graph.Neighbours[e];
                    residual[v] += push * graph.Weights[e];
                    Enqueue(v, residual, outWeight, graph, epsilon, queued, queue);
                }
            }

            return estimate;
        }

        private static bool Ready(int u, double[] residual, double[] outWeight, CompressedGraph graph, double epsilon)
        {
            var scale = outWeight[u] > 0 ? graph.Degree(u) : 1;

            return residual[u] >= epsilon * scale;
        }

        private static void Enqueue(int v, double[] residual, double[] outWeight, CompressedGraph graph,
            double epsilon, bool[] queued, Queue<int> queue)
        {
            if (queued[v] || !Ready(v, residual, outWeight, graph, epsilon))
            {
                return;
            }

            queued[v] = true;
            queue.Enqueue(v);
        }
    }
}
