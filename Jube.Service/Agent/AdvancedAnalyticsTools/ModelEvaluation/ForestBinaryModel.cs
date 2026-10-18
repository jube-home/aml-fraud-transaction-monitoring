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

using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class ForestBinaryModel : IBinaryModel
    {
        private readonly string kind;
        private readonly double? outOfBagAuc;
        private readonly CartTree[] trees;

        public ForestBinaryModel(CartTree[] trees, double? outOfBagAuc, string kind)
        {
            this.trees = trees;
            this.outOfBagAuc = outOfBagAuc;
            this.kind = kind;
        }

        public bool OutputsProbability => true;

        public double[] Score(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = 0d;

                foreach (var tree in trees)
                {
                    sum += tree.Predict(rows[i]);
                }

                result[i] = sum / trees.Length;
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

            var facts = new List<EvaluationNumber>
            {
                ModelText.Fact("Trees", trees.Length),
                ModelText.Fact("MeanDepth", trees.Average(t => t.Height)),
                ModelText.Fact("MeanLeaves", trees.Average(t => t.LeafCount))
            };

            if (outOfBagAuc.HasValue)
            {
                facts.Add(ModelText.Fact("OutOfBagRocAuc", outOfBagAuc.Value));
            }

            return new ModelDescription(
                $"A {kind} of CART trees, each grown on a bootstrap sample; the score is the mean of the leaf probabilities. The out of bag ROC AUC is an honest estimate from the rows each tree did not see.",
                facts.ToArray(),
                [],
                null,
                ModelText.Rank(names, importance));
        }

        public static ForestBinaryModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w, FitContext ctx)
        {
            var n = x.Length;
            var p = x[0].Length;
            var bagging = c.Family == EvaluationFamily.Bagging;

            var perSplit = bagging
                ? p
                : c.FeatureFraction > 0
                    ? Math.Max(1, (int)Math.Ceiling(c.FeatureFraction * p))
                    : Math.Max(1, (int)Math.Floor(Math.Sqrt(p)));

            var target = y.Select(v => (double)v).ToArray();
            var sample = Math.Max(1, (int)Math.Round(c.SampleFraction * n));
            var trees = new CartTree[c.Trees];
            var bags = new bool[c.Trees][];
            var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, 2 * c.MinLeaf, perSplit);

            Parallel.For(0, c.Trees, new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 8)),
                CancellationToken = ctx.Token
            }, t =>
            {
                var random = new Random(SeededLibrary.Derive(ctx.Seed, t));
                var rows = new int[sample];
                var inBag = new bool[n];

                for (var i = 0; i < sample; i++)
                {
                    rows[i] = random.Next(n);
                    inBag[rows[i]] = true;
                }

                bags[t] = inBag;
                trees[t] = CartTree.Fit(x, target, w, rows, options, random);
            });

            double? oob = null;
            var sums = new double[n];
            var counts = new int[n];

            for (var t = 0; t < trees.Length; t++)
            {
                for (var i = 0; i < n; i++)
                {
                    if (!bags[t][i])
                    {
                        sums[i] += trees[t].Predict(x[i]);
                        counts[i]++;
                    }
                }
            }

            var covered = Enumerable.Range(0, n).Where(i => counts[i] > 0).ToArray();

            if (covered.Length > 10)
            {
                var scores = covered.Select(i => sums[i] / counts[i]).ToArray();
                var labels = covered.Select(i => y[i]).ToArray();
                var auc = BinaryMetrics.RocAuc(scores, labels);
                oob = double.IsNaN(auc) ? null : auc;
            }

            return new ForestBinaryModel(trees, oob, bagging ? "bagged ensemble" : "random forest");
        }
    }
}