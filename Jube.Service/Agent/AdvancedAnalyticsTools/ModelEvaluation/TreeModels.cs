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

using System.Text;
using Accord.MachineLearning.DecisionTrees;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class LibraryTreeModel : IBinaryModel
    {
        private readonly string algorithm;
        private readonly Discretiser? discretiser;
        private readonly double prior;
        private readonly Dictionary<DecisionNode, Stats> stats = new();
        private readonly DecisionTree tree;

        public LibraryTreeModel(DecisionTree tree, Discretiser? discretiser, double[][] trainX, int[] trainY,
            double[]? weights, string algorithm)
        {
            this.tree = tree;
            this.discretiser = discretiser;
            this.algorithm = algorithm;

            double totalWeight = 0, positiveWeight = 0;

            for (var i = 0; i < trainX.Length; i++)
            {
                var w = weights == null ? 1d : weights[i];
                var input = discretiser == null ? trainX[i] : discretiser.Apply(trainX[i]);
                var path = new List<DecisionNode>();
                Walk(input, path);

                foreach (var node in path)
                {
                    if (!stats.TryGetValue(node, out var s))
                    {
                        s = new Stats();
                        stats[node] = s;
                    }

                    s.Total += w;
                    s.Positive += w * trainY[i];
                }

                totalWeight += w;
                positiveWeight += w * trainY[i];
            }

            prior = totalWeight > 0 ? positiveWeight / totalWeight : 0.5;
            AverageWeight = trainX.Length == 0 ? 1d : totalWeight / trainX.Length;
        }

        public bool OutputsProbability => true;

        public double[] Score(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var input = discretiser == null ? rows[i] : discretiser.Apply(rows[i]);
                result[i] = Probability(Walk(input, null));
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            var builder = new StringBuilder();
            var rules = new List<string>();
            var importance = new double[names.Length];
            var leaves = 0;
            var depth = 0;
            Text(tree.Root, names, builder, 0, new List<string>(), rules, importance, ref leaves, ref depth);

            var sum = importance.Sum();

            if (sum > 0)
            {
                for (var i = 0; i < importance.Length; i++)
                {
                    importance[i] /= sum;
                }
            }

            return new ModelDescription(
                $"A {algorithm} tree using information gain ratio; leaf values are the training share of the positive class with light smoothing.",
                [ModelText.Fact("Depth", depth), ModelText.Fact("Leaves", leaves)],
                rules.Take(25).ToArray(),
                builder.ToString().TrimEnd(),
                ModelText.Rank(names, importance));
        }

        public void Prune(double[][] validationX, int[] validationY)
        {
            var rows = Enumerable.Range(0, validationX.Length).ToArray();
            var inputs = validationX.Select(r => discretiser == null ? r : discretiser.Apply(r)).ToArray();
            Prune(tree.Root, rows, inputs, validationY);
        }

        private double AverageWeight { get; }

        private DecisionNode Walk(double[] input, List<DecisionNode>? path)
        {
            var node = tree.Root;
            path?.Add(node);

            while (!node.IsLeaf)
            {
                var attribute = node.Branches.AttributeIndex;
                var value = input[attribute];
                DecisionNode? next = null;

                foreach (var child in node.Branches)
                {
                    if (child.Compute(value))
                    {
                        next = child;
                        break;
                    }
                }

                if (next == null)
                {
                    break;
                }

                node = next;
                path?.Add(node);
            }

            return node;
        }

        private double Probability(DecisionNode node)
        {
            if (!stats.TryGetValue(node, out var s) || s.Total <= 0)
            {
                return prior;
            }

            return (s.Positive + AverageWeight * prior) / (s.Total + AverageWeight);
        }

        private double Prune(DecisionNode node, int[] rows, double[][] inputs, int[] labels)
        {
            var p = Probability(node);
            var leafError = 0d;

            foreach (var i in rows)
            {
                leafError += (labels[i] - p) * (labels[i] - p);
            }

            if (node.IsLeaf)
            {
                return leafError;
            }

            var attribute = node.Branches.AttributeIndex;
            var assigned = new Dictionary<DecisionNode, List<int>>();

            foreach (var child in node.Branches)
            {
                assigned[child] = new List<int>();
            }

            var subtreeError = 0d;

            foreach (var i in rows)
            {
                var routed = false;

                foreach (var child in node.Branches)
                {
                    if (child.Compute(inputs[i][attribute]))
                    {
                        assigned[child].Add(i);
                        routed = true;
                        break;
                    }
                }

                if (!routed)
                {
                    subtreeError += (labels[i] - p) * (labels[i] - p);
                }
            }

            foreach (var child in node.Branches.ToArray())
            {
                subtreeError += Prune(child, assigned[child].ToArray(), inputs, labels);
            }

            if (leafError <= subtreeError)
            {
                node.Branches = null!;

                return leafError;
            }

            return subtreeError;
        }

        private static string Operator(ComparisonKind kind)
        {
            return kind switch
            {
                ComparisonKind.Equal => "==",
                ComparisonKind.NotEqual => "!=",
                ComparisonKind.GreaterThan => ">",
                ComparisonKind.GreaterThanOrEqual => ">=",
                ComparisonKind.LessThan => "<",
                ComparisonKind.LessThanOrEqual => "<=",
                _ => "?"
            };
        }

        private string Condition(DecisionNode parent, DecisionNode child, string[] names)
        {
            var attribute = parent.Branches.AttributeIndex;

            if (discretiser != null && child.Value.HasValue)
            {
                return discretiser.Describe(attribute, (int)child.Value.Value);
            }

            return
                $"{names[attribute]} {Operator(child.Comparison)} {ModelEvaluationGuard.Format(child.Value ?? double.NaN)}";
        }

        private void Text(DecisionNode node, string[] names, StringBuilder builder, int indent, List<string> path,
            List<string> rules, double[] importance, ref int leaves, ref int depth)
        {
            var pad = string.Concat(Enumerable.Repeat("|   ", indent));
            depth = Math.Max(depth, indent);

            if (node.IsLeaf)
            {
                leaves++;
                var count = stats.TryGetValue(node, out var s) ? s.Total : 0d;

                builder.Append(pad).Append("|--- ").Append(ModelText.Probability(Probability(node))).Append(" (weight=")
                    .Append(ModelEvaluationGuard.Format(count)).Append(")\n");

                rules.Add(
                    $"IF {(path.Count == 0 ? "always" : string.Join(" AND ", path))} THEN {ModelText.Probability(Probability(node))}");

                return;
            }

            if (stats.TryGetValue(node, out var here) && here.Total > 0)
            {
                var gain = Gini(here) * here.Total;

                foreach (var child in node.Branches)
                {
                    if (stats.TryGetValue(child, out var cs))
                    {
                        gain -= Gini(cs) * cs.Total;
                    }
                }

                importance[node.Branches.AttributeIndex] += Math.Max(0d, gain);
            }

            foreach (var child in node.Branches)
            {
                var condition = Condition(node, child, names);
                builder.Append(pad).Append("|--- ").Append(condition).Append('\n');
                path.Add(condition);
                Text(child, names, builder, indent + 1, path, rules, importance, ref leaves, ref depth);
                path.RemoveAt(path.Count - 1);
            }
        }

        private static double Gini(Stats s)
        {
            if (s.Total <= 0)
            {
                return 0d;
            }

            var p = s.Positive / s.Total;

            return 2d * p * (1d - p);
        }

        private sealed class Stats
        {
            public double Positive;
            public double Total;
        }
    }
}