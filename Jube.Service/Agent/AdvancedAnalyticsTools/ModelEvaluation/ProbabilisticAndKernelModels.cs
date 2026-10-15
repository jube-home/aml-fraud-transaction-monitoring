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
    internal sealed class GaussianNaiveBayesModel : IBinaryModel
    {
        private readonly double[] logPriors;
        private readonly double[][] means;
        private readonly double[][] variances;

        private GaussianNaiveBayesModel(double[][] means, double[][] variances, double[] logPriors)
        {
            this.means = means;
            this.variances = variances;
            this.logPriors = logPriors;
        }

        public bool OutputsProbability => false;

        public double[] Score(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = LogLikelihood(rows[i], 1) - LogLikelihood(rows[i], 0);
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            var separation = new double[names.Length];

            for (var j = 0; j < names.Length; j++)
            {
                var pooled = Math.Sqrt((variances[0][j] + variances[1][j]) / 2d);
                separation[j] = pooled > 0 ? Math.Abs(means[1][j] - means[0][j]) / pooled : 0d;
            }

            var total = separation.Sum();
            var importance = total > 0 ? separation.Select(s => s / total).ToArray() : separation;

            return new ModelDescription(
                "Gaussian naive Bayes: each predictor is modelled as a normal distribution within each class and treated as independent given the class; the score is the class log-odds, calibrated afterwards. Importance is the standardised distance between the class means.",
                [
                    ModelText.Fact("PriorPositive", Math.Exp(logPriors[1])),
                    ModelText.Fact("PriorNegative", Math.Exp(logPriors[0]))
                ],
                [],
                null,
                ModelText.Rank(names, importance));
        }

        public static GaussianNaiveBayesModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w)
        {
            var p = x[0].Length;
            var means = new[] { new double[p], new double[p] };
            var variances = new[] { new double[p], new double[p] };
            var totals = new double[2];
            var weights = w ?? Enumerable.Repeat(1d, x.Length).ToArray();

            for (var i = 0; i < x.Length; i++)
            {
                totals[y[i]] += weights[i];

                for (var j = 0; j < p; j++)
                {
                    means[y[i]][j] += weights[i] * x[i][j];
                }
            }

            for (var k = 0; k < 2; k++)
            {
                for (var j = 0; j < p; j++)
                {
                    means[k][j] /= totals[k];
                }
            }

            for (var i = 0; i < x.Length; i++)
            {
                for (var j = 0; j < p; j++)
                {
                    var d = x[i][j] - means[y[i]][j];
                    variances[y[i]][j] += weights[i] * d * d;
                }
            }

            var overall = 0d;

            for (var j = 0; j < p; j++)
            {
                var grand = (totals[0] * means[0][j] + totals[1] * means[1][j]) / (totals[0] + totals[1]);
                var v = 0d;

                for (var i = 0; i < x.Length; i++)
                {
                    v += weights[i] * (x[i][j] - grand) * (x[i][j] - grand);
                }

                overall = Math.Max(overall, v / (totals[0] + totals[1]));
            }

            var floor = Math.Max(c.ResolvedRegularisation(1e-6) * overall, 1e-12);

            for (var k = 0; k < 2; k++)
            {
                for (var j = 0; j < p; j++)
                {
                    variances[k][j] = variances[k][j] / totals[k] + floor;
                }
            }

            var all = totals[0] + totals[1];

            return new GaussianNaiveBayesModel(means, variances,
                [Math.Log(totals[0] / all), Math.Log(totals[1] / all)]);
        }

        private double LogLikelihood(double[] row, int c)
        {
            var sum = logPriors[c];

            for (var j = 0; j < row.Length; j++)
            {
                var d = row[j] - means[c][j];
                sum += -0.5 * (Math.Log(2d * Math.PI * variances[c][j]) + d * d / variances[c][j]);
            }

            return sum;
        }
    }
}