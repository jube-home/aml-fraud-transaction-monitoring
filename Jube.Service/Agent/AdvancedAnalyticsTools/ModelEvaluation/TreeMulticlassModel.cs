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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class TreeMulticlassModel : IMulticlassModel
    {
        private readonly int classes;
        private readonly string kind;
        private readonly int[] labels;
        private readonly CartTree[] trees;

        public TreeMulticlassModel(CartTree[] trees, int classes, int[] labels, string kind)
        {
            this.trees = trees;
            this.classes = classes;
            this.labels = labels;
            this.kind = kind;
        }

        public double[][] Probabilities(double[][] rows)
        {
            var result = new double[rows.Length][];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = new double[classes];

                foreach (var tree in trees)
                {
                    var values = tree.PredictValues(rows[i]);

                    for (var k = 0; k < classes; k++)
                    {
                        sum[k] += values[k] / trees.Length;
                    }
                }

                result[i] = MulticlassMath.Normalise(sum);
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            var importance = new double[names.Length];

            foreach (var tree in trees)
            {
                var one = tree.Importance();

                for (var j = 0; j < importance.Length; j++)
                {
                    importance[j] += one[j] / trees.Length;
                }
            }

            var single = trees.Length == 1;

            string Leaf(double[] values)
            {
                return "class " + labels[Array.IndexOf(values, values.Max())] + " (p=" +
                       values.Max().ToString("0.###", CultureInfo.InvariantCulture) + ")";
            }

            return new ModelDescription(
                $"A {kind} of multiclass CART trees splitting on Gini impurity; the class probabilities are the leaf class shares averaged over the trees.",
                [
                    ModelText.Fact("Trees", trees.Length),
                    ModelText.Fact("MeanDepth", trees.Average(t => t.Height)),
                    ModelText.Fact("MeanLeaves", trees.Average(t => t.LeafCount))
                ],
                single ? trees[0].Rules(names, Leaf, 25) : [],
                single ? trees[0].ToText(names, Leaf) : null,
                ModelText.Rank(names, importance));
        }

        public static TreeMulticlassModel Fit(MulticlassConfig c, double[][] x, int[] y, double[]? w, int classes,
            int[] labels, int seed, CancellationToken token)
        {
            var n = x.Length;
            var p = x[0].Length;
            var flat = new double[n * classes];

            for (var i = 0; i < n; i++)
            {
                flat[i * classes + y[i]] = 1d;
            }

            if (c.Algorithm == MulticlassAlgorithm.DecisionTree)
            {
                var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, Math.Max(2, 2 * c.MinLeaf), p);

                var tree = CartTree.Fit(x, flat, w, Enumerable.Range(0, n).ToArray(), options, new Random(seed),
                    classes);

                return new TreeMulticlassModel([tree], classes, labels, "decision tree");
            }

            var perSplit = c.FeatureFraction > 0
                ? Math.Max(1, (int)Math.Ceiling(c.FeatureFraction * p))
                : Math.Max(1, (int)Math.Floor(Math.Sqrt(p)));

            var forestOptions = new CartTree.Options(c.MaxDepth, c.MinLeaf, Math.Max(2, 2 * c.MinLeaf), perSplit);
            var trees = new CartTree[c.Trees];

            Parallel.For(0, c.Trees, new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 8)),
                CancellationToken = token
            }, t =>
            {
                var random = new Random(SeededLibrary.Derive(seed, t));
                var rows = new int[n];

                for (var i = 0; i < n; i++)
                {
                    rows[i] = random.Next(n);
                }

                trees[t] = CartTree.Fit(x, flat, w, rows, forestOptions, random, classes);
            });

            return new TreeMulticlassModel(trees, classes, labels, "random forest");
        }
    }
}