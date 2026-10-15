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
using System.Text;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class CartTree
    {
        public Node Root { get; private set; } = new();
        public int FeatureCount { get; private set; }
        public int Outputs { get; private set; } = 1;
        public int LeafCount => Nodes().Count(n => n.IsLeaf);
        public int Height => Nodes().Max(n => n.Depth);

        public static CartTree Fit(double[][] x, double[] y, double[]? weights, int[] rows, Options options,
            Random random,
            int outputs = 1)
        {
            var tree = new CartTree { FeatureCount = x.Length == 0 ? 0 : x[0].Length, Outputs = outputs };

            if (rows.Length == 0)
            {
                throw new ArgumentException("A tree cannot be fitted to zero rows.");
            }

            var features = Enumerable.Range(0, tree.FeatureCount).ToArray();
            tree.Root = Build(x, y, weights, rows, 0, options, random, features, outputs);

            return tree;
        }

        public Node Leaf(double[] row)
        {
            var node = Root;

            while (!node.IsLeaf)
            {
                node = row[node.Feature] <= node.Threshold ? node.Left! : node.Right!;
            }

            return node;
        }

        public double Predict(double[] row)
        {
            return Leaf(row).Values[0];
        }

        public double[] PredictValues(double[] row)
        {
            return Leaf(row).Values;
        }

        public double[] Predict(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = Predict(rows[i]);
            }

            return result;
        }

        public IEnumerable<Node> Nodes()
        {
            var stack = new Stack<Node>();
            stack.Push(Root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;

                if (!node.IsLeaf)
                {
                    stack.Push(node.Right!);
                    stack.Push(node.Left!);
                }
            }
        }

        public double[] Importance()
        {
            var totals = new double[FeatureCount];

            foreach (var node in Nodes().Where(n => !n.IsLeaf))
            {
                totals[node.Feature] += node.Gain;
            }

            var sum = totals.Sum();

            return sum > 0 ? totals.Select(t => t / sum).ToArray() : totals;
        }

        public void PruneReducedError(double[][] validationX, double[] validationY)
        {
            Prune(Root, Enumerable.Range(0, validationX.Length).ToArray(), validationX, validationY, Outputs);
        }

        public string ToText(string[] names, Func<double[], string> leaf)
        {
            var builder = new StringBuilder();
            Write(Root, names, leaf, builder, 0);

            return builder.ToString().TrimEnd();
        }

        public string[] Rules(string[] names, Func<double[], string> leaf, int maximum)
        {
            var rules = new List<string>();
            Collect(Root, names, leaf, new List<string>(), rules, maximum);

            return rules.ToArray();
        }

        private static Node Build(double[][] x, double[] y, double[]? w, int[] idx, int depth, Options options,
            Random random, int[] features, int outputs)
        {
            double sumW = 0;
            var sumWy = new double[outputs];
            var sumWyy = new double[outputs];

            foreach (var i in idx)
            {
                var wi = w == null ? 1d : w[i];
                sumW += wi;

                for (var k = 0; k < outputs; k++)
                {
                    var value = y[i * outputs + k];
                    sumWy[k] += wi * value;
                    sumWyy[k] += wi * value * value;
                }
            }

            var node = new Node
            {
                Values = sumWy.Select(v => sumW > 0 ? v / sumW : 0d).ToArray(),
                Weight = sumW,
                Count = idx.Length,
                Depth = depth
            };

            var sse = 0d;

            for (var k = 0; k < outputs; k++)
            {
                sse += sumWyy[k] - (sumW > 0 ? sumWy[k] * sumWy[k] / sumW : 0d);
            }

            if (depth >= options.MaxDepth || idx.Length < options.MinSplit || idx.Length < 2 * options.MinLeaf ||
                sse <= 1e-12 || sumW <= 0)
            {
                return node;
            }

            var order = (int[])features.Clone();

            for (var i = order.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            var parentTerm = 0d;

            for (var k = 0; k < outputs; k++)
            {
                parentTerm += sumWy[k] * sumWy[k] / sumW;
            }

            var bestGain = options.MinGain;
            var bestFeature = -1;
            var bestThreshold = 0d;
            var evaluated = 0;
            var keys = new double[idx.Length];
            var items = new int[idx.Length];
            var leftWy = new double[outputs];

            foreach (var f in order)
            {
                if (evaluated >= options.FeaturesPerSplit && bestFeature >= 0)
                {
                    break;
                }

                for (var k = 0; k < idx.Length; k++)
                {
                    keys[k] = x[idx[k]][f];
                    items[k] = idx[k];
                }

                Array.Sort(keys, items);
                evaluated++;

                double leftW = 0;
                Array.Clear(leftWy);
                var leftCount = 0;

                for (var k = 0; k < idx.Length - 1; k++)
                {
                    var i = items[k];
                    var wi = w == null ? 1d : w[i];
                    leftW += wi;

                    for (var o = 0; o < outputs; o++)
                    {
                        leftWy[o] += wi * y[i * outputs + o];
                    }

                    leftCount++;

                    if (keys[k] == keys[k + 1])
                    {
                        continue;
                    }

                    var rightCount = idx.Length - leftCount;

                    if (leftCount < options.MinLeaf || rightCount < options.MinLeaf)
                    {
                        continue;
                    }

                    var rightW = sumW - leftW;

                    if (leftW <= 0 || rightW <= 0)
                    {
                        continue;
                    }

                    var gain = -parentTerm;

                    for (var o = 0; o < outputs; o++)
                    {
                        var rightWy = sumWy[o] - leftWy[o];
                        gain += leftWy[o] * leftWy[o] / leftW + rightWy * rightWy / rightW;
                    }

                    if (gain > bestGain)
                    {
                        bestGain = gain;
                        bestFeature = f;
                        bestThreshold = keys[k] + (keys[k + 1] - keys[k]) / 2d;

                        if (bestThreshold >= keys[k + 1])
                        {
                            bestThreshold = keys[k];
                        }
                    }
                }
            }

            if (bestFeature < 0)
            {
                return node;
            }

            var left = new List<int>();
            var right = new List<int>();

            foreach (var i in idx)
            {
                if (x[i][bestFeature] <= bestThreshold)
                {
                    left.Add(i);
                }
                else
                {
                    right.Add(i);
                }
            }

            if (left.Count == 0 || right.Count == 0)
            {
                return node;
            }

            node.Feature = bestFeature;
            node.Threshold = bestThreshold;
            node.Gain = bestGain;
            node.Left = Build(x, y, w, left.ToArray(), depth + 1, options, random, features, outputs);
            node.Right = Build(x, y, w, right.ToArray(), depth + 1, options, random, features, outputs);

            return node;
        }

        private static double Prune(Node node, int[] idx, double[][] x, double[] y, int outputs)
        {
            var leafError = 0d;

            foreach (var i in idx)
            {
                for (var k = 0; k < outputs; k++)
                {
                    var d = y[i * outputs + k] - node.Values[k];
                    leafError += d * d;
                }
            }

            if (node.IsLeaf)
            {
                return leafError;
            }

            var left = idx.Where(i => x[i][node.Feature] <= node.Threshold).ToArray();
            var right = idx.Where(i => x[i][node.Feature] > node.Threshold).ToArray();
            var subtreeError = Prune(node.Left!, left, x, y, outputs) + Prune(node.Right!, right, x, y, outputs);

            if (leafError <= subtreeError)
            {
                node.Left = null;
                node.Right = null;
                node.Feature = -1;

                return leafError;
            }

            return subtreeError;
        }

        private static void Write(Node node, string[] names, Func<double[], string> leaf, StringBuilder builder,
            int indent)
        {
            var pad = string.Concat(Enumerable.Repeat("|   ", indent));

            if (node.IsLeaf)
            {
                builder.Append(pad).Append("|--- ").Append(leaf(node.Values)).Append(" (rows=").Append(node.Count)
                    .Append(")\n");

                return;
            }

            var name = names[node.Feature];
            var threshold = node.Threshold.ToString("0.######", CultureInfo.InvariantCulture);
            builder.Append(pad).Append("|--- ").Append(name).Append(" <= ").Append(threshold).Append('\n');
            Write(node.Left!, names, leaf, builder, indent + 1);
            builder.Append(pad).Append("|--- ").Append(name).Append(" > ").Append(threshold).Append('\n');
            Write(node.Right!, names, leaf, builder, indent + 1);
        }

        private static void Collect(Node node, string[] names, Func<double[], string> leaf, List<string> path,
            List<string> rules, int maximum)
        {
            if (rules.Count >= maximum)
            {
                return;
            }

            if (node.IsLeaf)
            {
                var conditions = path.Count == 0 ? "always" : string.Join(" AND ", path);
                rules.Add($"IF {conditions} THEN {leaf(node.Values)} (rows={node.Count})");

                return;
            }

            var name = names[node.Feature];
            var threshold = node.Threshold.ToString("0.######", CultureInfo.InvariantCulture);
            path.Add($"{name} <= {threshold}");
            Collect(node.Left!, names, leaf, path, rules, maximum);
            path.RemoveAt(path.Count - 1);
            path.Add($"{name} > {threshold}");
            Collect(node.Right!, names, leaf, path, rules, maximum);
            path.RemoveAt(path.Count - 1);
        }

        internal sealed class Node
        {
            public int Count;
            public int Depth;
            public int Feature = -1;
            public double Gain;
            public Node? Left;
            public Node? Right;
            public double Threshold;
            public double[] Values = [0d];
            public double Weight;

            public double Value
            {
                get => Values[0];
                set => Values[0] = value;
            }

            public bool IsLeaf => Left == null;
        }

        internal sealed record Options(
            int MaxDepth,
            int MinLeaf,
            int MinSplit,
            int FeaturesPerSplit,
            double MinGain = 1e-12);
    }
}