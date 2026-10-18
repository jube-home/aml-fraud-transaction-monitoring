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
    internal sealed class AdaBoostBinaryModel : IBinaryModel
    {
        private readonly double[] alphas;
        private readonly double[] errors;
        private readonly CartTree[] trees;

        public AdaBoostBinaryModel(CartTree[] trees, double[] alphas, double[] errors)
        {
            this.trees = trees;
            this.alphas = alphas;
            this.errors = errors;
        }

        public bool OutputsProbability => false;

        public double[] Score(double[][] rows)
        {
            var total = alphas.Sum();
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = 0d;

                for (var m = 0; m < trees.Length; m++)
                {
                    sum += alphas[m] * (trees[m].Predict(rows[i]) >= 0.5 ? 1d : -1d);
                }

                result[i] = total > 0 ? sum / total : 0d;
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            var importance = new double[names.Length];

            for (var m = 0; m < trees.Length; m++)
            {
                var one = trees[m].Importance();

                for (var j = 0; j < importance.Length; j++)
                {
                    importance[j] += alphas[m] * one[j];
                }
            }

            var sum = importance.Sum();

            if (sum > 0)
            {
                importance = importance.Select(v => v / sum).ToArray();
            }

            return new ModelDescription(
                "Discrete AdaBoost: shallow trees fitted in sequence, each reweighting the rows the previous ones got wrong; the score is the normalised weighted vote, calibrated afterwards.",
                [
                    ModelText.Fact("Rounds", trees.Length),
                    ModelText.Fact("FirstRoundWeightedError", errors.Length > 0 ? errors[0] : double.NaN),
                    ModelText.Fact("LastRoundWeightedError", errors.Length > 0 ? errors[^1] : double.NaN)
                ],
                [],
                null,
                ModelText.Rank(names, importance));
        }

        public static AdaBoostBinaryModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w, FitContext ctx)
        {
            var n = x.Length;
            var target = y.Select(v => (double)v).ToArray();
            var weights = new double[n];
            var start = w == null ? Enumerable.Repeat(1d, n).ToArray() : w;
            var total = start.Sum();

            for (var i = 0; i < n; i++)
            {
                weights[i] = start[i] / total;
            }

            var all = Enumerable.Range(0, n).ToArray();
            var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, 2 * c.MinLeaf, x[0].Length);
            var trees = new List<CartTree>();
            var alphas = new List<double>();
            var errors = new List<double>();

            for (var round = 0; round < c.Trees; round++)
            {
                ctx.Token.ThrowIfCancellationRequested();
                var random = new Random(SeededLibrary.Derive(ctx.Seed, round));
                var tree = CartTree.Fit(x, target, weights, all, options, random);
                var wrong = new bool[n];
                var error = 0d;

                for (var i = 0; i < n; i++)
                {
                    var predicted = tree.Predict(x[i]) >= 0.5 ? 1 : 0;
                    wrong[i] = predicted != y[i];

                    if (wrong[i])
                    {
                        error += weights[i];
                    }
                }

                if (error >= 0.5)
                {
                    if (trees.Count == 0)
                    {
                        trees.Add(tree);
                        alphas.Add(1e-6);
                        errors.Add(error);
                    }

                    break;
                }

                var clamped = Math.Max(error, 1e-10);
                var alpha = c.LearningRate * 0.5 * Math.Log((1d - clamped) / clamped);
                trees.Add(tree);
                alphas.Add(alpha);
                errors.Add(error);

                if (error <= 0)
                {
                    break;
                }

                var sum = 0d;

                for (var i = 0; i < n; i++)
                {
                    weights[i] *= Math.Exp(wrong[i] ? alpha : -alpha);
                    sum += weights[i];
                }

                for (var i = 0; i < n; i++)
                {
                    weights[i] /= sum;
                }
            }

            return new AdaBoostBinaryModel(trees.ToArray(), alphas.ToArray(), errors.ToArray());
        }
    }
}