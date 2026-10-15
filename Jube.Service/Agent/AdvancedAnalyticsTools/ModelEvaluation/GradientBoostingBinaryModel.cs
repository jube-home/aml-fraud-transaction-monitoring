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
    internal sealed class GradientBoostingBinaryModel : IBinaryModel
    {
        private readonly double baseScore;
        private readonly double rate;
        private readonly double[] trainLoss;
        private readonly CartTree[] trees;

        public GradientBoostingBinaryModel(CartTree[] trees, double baseScore, double rate, double[] trainLoss)
        {
            this.trees = trees;
            this.baseScore = baseScore;
            this.rate = rate;
            this.trainLoss = trainLoss;
        }

        public bool OutputsProbability => false;

        public double[] Score(double[][] rows)
        {
            return ScoreWith(rows, trees.Length);
        }

        public ModelDescription Describe(string[] names)
        {
            var importance = new double[names.Length];

            foreach (var tree in trees)
            {
                var one = tree.Importance();

                for (var j = 0; j < importance.Length; j++)
                {
                    importance[j] += one[j] / Math.Max(1, trees.Length);
                }
            }

            var facts = new List<EvaluationNumber>
            {
                ModelText.Fact("Rounds", trees.Length),
                ModelText.Fact("BaseLogOdds", baseScore),
                ModelText.Fact("FirstRoundTrainLogLoss", trainLoss.Length > 0 ? trainLoss[0] : double.NaN),
                ModelText.Fact("LastRoundTrainLogLoss", trainLoss.Length > 0 ? trainLoss[^1] : double.NaN)
            };

            return new ModelDescription(
                "Gradient boosted trees on the logistic loss: each shallow tree fits the gradient of the previous rounds and a Newton step sets its leaf values; the score is the summed log-odds, calibrated afterwards.",
                facts.ToArray(),
                [],
                null,
                ModelText.Rank(names, importance));
        }

        public double[] ScoreWith(double[][] rows, int rounds)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = baseScore;

                for (var m = 0; m < Math.Min(rounds, trees.Length); m++)
                {
                    sum += rate * trees[m].Predict(rows[i]);
                }

                result[i] = sum;
            }

            return result;
        }

        public static GradientBoostingBinaryModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w,
            FitContext ctx)
        {
            var n = x.Length;
            var p = x[0].Length;
            var weights = w ?? Enumerable.Repeat(1d, n).ToArray();
            var totalWeight = weights.Sum();
            var positiveWeight = Enumerable.Range(0, n).Sum(i => weights[i] * y[i]);
            var prior = Math.Min(Math.Max(positiveWeight / totalWeight, 1e-6), 1d - 1e-6);
            var baseScore = Math.Log(prior / (1d - prior));
            var margin = Enumerable.Repeat(baseScore, n).ToArray();
            var residual = new double[n];
            var perSplit = c.FeatureFraction > 0 ? Math.Max(1, (int)Math.Ceiling(c.FeatureFraction * p)) : p;
            var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, 2 * c.MinLeaf, perSplit);
            var sampleSize = Math.Max(2, (int)Math.Round(c.SampleFraction * n));
            var trees = new List<CartTree>();
            var trainLoss = new List<double>();

            for (var round = 0; round < c.Trees; round++)
            {
                ctx.Token.ThrowIfCancellationRequested();
                var random = new Random(SeededLibrary.Derive(ctx.Seed, round));
                var probability = new double[n];

                for (var i = 0; i < n; i++)
                {
                    probability[i] = ModelMath.Sigmoid(margin[i]);
                    residual[i] = y[i] - probability[i];
                }

                int[] rows;

                if (sampleSize >= n)
                {
                    rows = Enumerable.Range(0, n).ToArray();
                }
                else
                {
                    rows = Enumerable.Range(0, n).OrderBy(_ => random.Next()).Take(sampleSize).ToArray();
                }

                var tree = CartTree.Fit(x, residual, weights, rows, options, random);
                var numerator = new Dictionary<CartTree.Node, double>();
                var denominator = new Dictionary<CartTree.Node, double>();

                foreach (var i in rows)
                {
                    var leaf = tree.Leaf(x[i]);
                    numerator.TryGetValue(leaf, out var a);
                    denominator.TryGetValue(leaf, out var b);
                    numerator[leaf] = a + weights[i] * residual[i];
                    denominator[leaf] = b + weights[i] * probability[i] * (1d - probability[i]);
                }

                foreach (var leaf in tree.Nodes().Where(nd => nd.IsLeaf))
                {
                    numerator.TryGetValue(leaf, out var a);
                    denominator.TryGetValue(leaf, out var b);
                    leaf.Value = b > 1e-12 ? Math.Max(-8d, Math.Min(8d, a / b)) : 0d;
                }

                for (var i = 0; i < n; i++)
                {
                    margin[i] += c.LearningRate * tree.Predict(x[i]);
                }

                trees.Add(tree);
                var loss = 0d;

                for (var i = 0; i < n; i++)
                {
                    loss += weights[i] * (ModelMath.Softplus(margin[i]) - y[i] * margin[i]);
                }

                trainLoss.Add(loss / totalWeight);
            }

            return new GradientBoostingBinaryModel(trees.ToArray(), baseScore, c.LearningRate, trainLoss.ToArray());
        }
    }
}