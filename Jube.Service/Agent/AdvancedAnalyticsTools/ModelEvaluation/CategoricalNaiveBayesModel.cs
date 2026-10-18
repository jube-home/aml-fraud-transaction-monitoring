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
    internal sealed class CategoricalNaiveBayesModel : IBinaryModel
    {
        private readonly Discretiser discretiser;
        private readonly double[] divergence;
        private readonly double[] logPriors;
        private readonly double[][][] logProbabilities;

        private CategoricalNaiveBayesModel(Discretiser discretiser, double[][][] logProbabilities, double[] logPriors,
            double[] divergence)
        {
            this.discretiser = discretiser;
            this.logProbabilities = logProbabilities;
            this.logPriors = logPriors;
            this.divergence = divergence;
        }

        public bool OutputsProbability => false;

        public double[] Score(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = logPriors[1] - logPriors[0];

                for (var j = 0; j < rows[i].Length; j++)
                {
                    var code = discretiser.Code(j, rows[i][j]);
                    sum += logProbabilities[1][j][code] - logProbabilities[0][j][code];
                }

                result[i] = sum;
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            var total = divergence.Sum();
            var importance = total > 0 ? divergence.Select(d => d / total).ToArray() : divergence;

            return new ModelDescription(
                "Categorical naive Bayes on quantile-binned predictors with Laplace smoothing; the score is the class log-odds, calibrated afterwards. Importance is each variable's Jeffreys divergence between the class-conditional bin distributions.",
                [ModelText.Fact("PriorPositive", Math.Exp(logPriors[1]))],
                [],
                null,
                ModelText.Rank(names, importance));
        }

        public static CategoricalNaiveBayesModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w,
            string[] names)
        {
            var p = names.Length;
            var discretiser = Discretiser.Fit(x, names, c.Bins);
            var symbols = discretiser.Symbols;
            var alpha = c.ResolvedRegularisation(1d);
            var weights = w ?? Enumerable.Repeat(1d, x.Length).ToArray();
            var counts = new[] { new double[p][], new double[p][] };
            var totals = new double[2];

            for (var k = 0; k < 2; k++)
            {
                for (var j = 0; j < p; j++)
                {
                    counts[k][j] = new double[symbols[j]];
                }
            }

            for (var i = 0; i < x.Length; i++)
            {
                totals[y[i]] += weights[i];

                for (var j = 0; j < p; j++)
                {
                    counts[y[i]][j][discretiser.Code(j, x[i][j])] += weights[i];
                }
            }

            var logProbabilities = new[] { new double[p][], new double[p][] };
            var divergence = new double[p];

            for (var j = 0; j < p; j++)
            {
                var probabilities = new double[2][];

                for (var k = 0; k < 2; k++)
                {
                    probabilities[k] = new double[symbols[j]];
                    logProbabilities[k][j] = new double[symbols[j]];

                    for (var s = 0; s < symbols[j]; s++)
                    {
                        var probability = (counts[k][j][s] + alpha) / (totals[k] + alpha * symbols[j]);
                        probabilities[k][s] = probability;
                        logProbabilities[k][j][s] = Math.Log(probability);
                    }
                }

                for (var s = 0; s < symbols[j]; s++)
                {
                    divergence[j] += (probabilities[1][s] - probabilities[0][s]) *
                                     (logProbabilities[1][j][s] - logProbabilities[0][j][s]);
                }
            }

            var all = totals[0] + totals[1];

            return new CategoricalNaiveBayesModel(discretiser, logProbabilities,
                [Math.Log(totals[0] / all), Math.Log(totals[1] / all)], divergence);
        }
    }
}