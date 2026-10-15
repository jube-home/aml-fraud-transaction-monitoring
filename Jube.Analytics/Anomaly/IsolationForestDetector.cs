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

namespace Jube.Analytics.Anomaly
{
    public sealed class IsolationForestDetector(int trees = 100, int subsampleSize = 256) : AnomalyDetectorBase
    {
        private IsolationTree[] forest = [];
        private double normaliser = 1d;
        public override string Name => "IsolationForest";
        public int Trees { get; } = trees > 0 ? trees : throw new ArgumentOutOfRangeException(nameof(trees));

        public int SubsampleSize { get; } =
            subsampleSize > 1 ? subsampleSize : throw new ArgumentOutOfRangeException(nameof(subsampleSize));

        public static double AveragePathLength(double n)
        {
            if (n <= 1d)
            {
                return 0d;
            }

            if (n <= 2d)
            {
                return 1d;
            }

            return 2d * (Math.Log(n - 1d) + 0.5772156649015329) - 2d * (n - 1d) / n;
        }

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            var psi = Math.Min(SubsampleSize, data.Length);
            var limit = (int)Math.Ceiling(Math.Log2(psi));
            var built = new IsolationTree[Trees];

            Parallel.For(0, Trees, AnomalyInput.Options(token), t =>
            {
                var rng = new Random(AnomalyInput.DeriveSeed(seed, t));
                var sample = new int[psi];
                var pool = Enumerable.Range(0, data.Length).ToArray();

                for (var i = 0; i < psi; i++)
                {
                    var j = i + rng.Next(pool.Length - i);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                    sample[i] = pool[i];
                }

                built[t] = IsolationTree.Build(data, sample, limit, rng);
            });

            forest = built;
            normaliser = AveragePathLength(psi);
        }

        protected override double ScoreCore(double[] row)
        {
            var total = 0d;

            foreach (var tree in forest)
            {
                total += tree.PathLength(row);
            }

            var mean = total / forest.Length;

            return Math.Pow(2d, -mean / normaliser);
        }

        private sealed class IsolationTree
        {
            private readonly List<int> feature = [];
            private readonly List<int> left = [];
            private readonly List<int> right = [];
            private readonly List<int> size = [];
            private readonly List<double> split = [];

            public static IsolationTree Build(double[][] data, int[] sample, int limit, Random rng)
            {
                var tree = new IsolationTree();
                tree.Grow(data, sample, 0, limit, rng);

                return tree;
            }

            public double PathLength(double[] row)
            {
                var node = 0;
                var depth = 0;

                while (feature[node] >= 0)
                {
                    node = row[feature[node]] < split[node] ? left[node] : right[node];
                    depth++;
                }

                return depth + AveragePathLength(size[node]);
            }

            private int Grow(double[][] data, int[] rows, int depth, int limit, Random rng)
            {
                var node = feature.Count;
                feature.Add(-1);
                split.Add(0d);
                left.Add(-1);
                right.Add(-1);
                size.Add(rows.Length);

                if (depth >= limit || rows.Length <= 1)
                {
                    return node;
                }

                var d = data[0].Length;
                var order = Enumerable.Range(0, d).ToArray();

                for (var i = 0; i < d; i++)
                {
                    var j = i + rng.Next(d - i);
                    (order[i], order[j]) = (order[j], order[i]);
                    var f = order[i];
                    var min = double.PositiveInfinity;
                    var max = double.NegativeInfinity;

                    foreach (var r in rows)
                    {
                        var v = data[r][f];

                        if (v < min)
                        {
                            min = v;
                        }

                        if (v > max)
                        {
                            max = v;
                        }
                    }

                    if (!(max > min))
                    {
                        continue;
                    }

                    var cut = min + rng.NextDouble() * (max - min);

                    if (cut <= min || cut >= max)
                    {
                        cut = (min + max) / 2d;
                    }

                    var l = new List<int>();
                    var rr = new List<int>();

                    foreach (var r in rows)
                    {
                        if (data[r][f] < cut)
                        {
                            l.Add(r);
                        }
                        else
                        {
                            rr.Add(r);
                        }
                    }

                    if (l.Count == 0 || rr.Count == 0)
                    {
                        continue;
                    }

                    feature[node] = f;
                    split[node] = cut;
                    left[node] = Grow(data, l.ToArray(), depth + 1, limit, rng);
                    right[node] = Grow(data, rr.ToArray(), depth + 1, limit, rng);

                    return node;
                }

                return node;
            }
        }
    }
}