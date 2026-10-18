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

using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class IsolationForestModel : IAnomalyModel
    {
        public string Summary =>
            "Isolation forest: random trees isolate points with random splits; points that need few splits to isolate are anomalous. The score lies in (0, 1) and values near 1 are anomalies.";

        public EvaluationNumber[] Facts()
        {
            return [ModelText.Fact("Trees", trees.Length), ModelText.Fact("SubsampleSize", sampleSize)];
        }

        public double[] Score(double[][] rows)
        {
            var normaliser = AveragePath(sampleSize);
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var mean = trees.Average(t => Path(t, rows[i]));
                result[i] = Math.Pow(2d, -mean / Math.Max(normaliser, 1e-9));
            }

            return result;
        }

        public static IsolationForestModel Fit(double[][] x, int trees, int seed, int subsample,
            CancellationToken token)
        {
            var size = Math.Min(subsample, x.Length);
            var limit = (int)Math.Ceiling(Math.Log2(Math.Max(size, 2)));
            var built = new Node[trees];

            for (var t = 0; t < trees; t++)
            {
                token.ThrowIfCancellationRequested();
                var random = new Random(SeededLibrary.Derive(seed, t));
                var rows = Enumerable.Range(0, x.Length).OrderBy(_ => random.Next()).Take(size).ToArray();
                built[t] = Build(x, rows, 0, limit, random);
            }

            return new IsolationForestModel(built, size);
        }

        private readonly int sampleSize;
        private readonly Node[] trees;

        private IsolationForestModel(Node[] trees, int sampleSize)
        {
            this.trees = trees;
            this.sampleSize = sampleSize;
        }

        private static double AveragePath(int n)
        {
            if (n <= 1)
            {
                return 0d;
            }

            if (n == 2)
            {
                return 1d;
            }

            return 2d * (Math.Log(n - 1) + 0.5772156649015329) - 2d * (n - 1) / n;
        }

        private static Node Build(double[][] x, int[] rows, int depth, int limit, Random random)
        {
            var node = new Node { Size = rows.Length };

            if (depth >= limit || rows.Length <= 1)
            {
                return node;
            }

            var p = x[0].Length;

            for (var attempt = 0; attempt < p * 2; attempt++)
            {
                var feature = random.Next(p);
                var low = double.MaxValue;
                var high = double.MinValue;

                foreach (var i in rows)
                {
                    low = Math.Min(low, x[i][feature]);
                    high = Math.Max(high, x[i][feature]);
                }

                if (high <= low)
                {
                    continue;
                }

                var split = low + random.NextDouble() * (high - low);
                var left = rows.Where(i => x[i][feature] < split).ToArray();
                var right = rows.Where(i => x[i][feature] >= split).ToArray();

                if (left.Length == 0 || right.Length == 0)
                {
                    continue;
                }

                node.Feature = feature;
                node.Split = split;
                node.Left = Build(x, left, depth + 1, limit, random);
                node.Right = Build(x, right, depth + 1, limit, random);

                return node;
            }

            return node;
        }

        private static double Path(Node node, double[] row)
        {
            var depth = 0d;

            while (node.Left != null)
            {
                node = row[node.Feature] < node.Split ? node.Left : node.Right!;
                depth++;
            }

            return depth + AveragePath(node.Size);
        }

        private sealed class Node
        {
            public int Feature = -1;
            public Node? Left;
            public Node? Right;
            public int Size;
            public double Split;
        }
    }
}