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
    internal sealed class GaussianBayesMulticlassModel : IMulticlassModel
    {
        private readonly double[] logPriors;
        private readonly double[][] means;
        private readonly double[][] variances;

        private GaussianBayesMulticlassModel(double[][] means, double[][] variances, double[] logPriors)
        {
            this.means = means;
            this.variances = variances;
            this.logPriors = logPriors;
        }

        public double[][] Probabilities(double[][] rows)
        {
            var result = new double[rows.Length][];

            for (var i = 0; i < rows.Length; i++)
            {
                var logs = new double[logPriors.Length];

                for (var c = 0; c < logs.Length; c++)
                {
                    var sum = logPriors[c];

                    for (var j = 0; j < rows[i].Length; j++)
                    {
                        var d = rows[i][j] - means[c][j];
                        sum += -0.5 * (Math.Log(2d * Math.PI * variances[c][j]) + d * d / variances[c][j]);
                    }

                    logs[c] = sum;
                }

                var max = logs.Max();
                result[i] = MulticlassMath.Normalise(logs.Select(l => Math.Exp(l - max)).ToArray());
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            var separation = new double[names.Length];

            for (var j = 0; j < names.Length; j++)
            {
                var all = means.Select(m => m[j]).ToArray();
                var pooled = Math.Sqrt(variances.Average(v => v[j]));
                separation[j] = pooled > 0 ? (all.Max() - all.Min()) / pooled : 0d;
            }

            var total = separation.Sum();

            return new ModelDescription(
                "Gaussian naive Bayes: each predictor is a normal distribution within each class, independent given the class. Importance is the range of the class means in pooled standard deviations.",
                [ModelText.Fact("Classes", logPriors.Length)],
                [],
                null,
                ModelText.Rank(names, total > 0 ? separation.Select(s => s / total).ToArray() : separation));
        }

        public static GaussianBayesMulticlassModel Fit(MulticlassConfig c, double[][] x, int[] y, double[]? w,
            int classes)
        {
            var p = x[0].Length;
            var weights = w ?? Enumerable.Repeat(1d, x.Length).ToArray();
            var totals = new double[classes];
            var means = Enumerable.Range(0, classes).Select(_ => new double[p]).ToArray();
            var variances = Enumerable.Range(0, classes).Select(_ => new double[p]).ToArray();

            for (var i = 0; i < x.Length; i++)
            {
                totals[y[i]] += weights[i];

                for (var j = 0; j < p; j++)
                {
                    means[y[i]][j] += weights[i] * x[i][j];
                }
            }

            for (var k = 0; k < classes; k++)
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
                var column = x.Select(r => r[j]).ToArray();
                var mean = column.Average();
                overall = Math.Max(overall, column.Sum(v => (v - mean) * (v - mean)) / column.Length);
            }

            var floor = Math.Max(c.VarianceSmoothing * overall, 1e-12);
            var sum = totals.Sum();

            for (var k = 0; k < classes; k++)
            {
                for (var j = 0; j < p; j++)
                {
                    variances[k][j] = variances[k][j] / totals[k] + floor;
                }
            }

            return new GaussianBayesMulticlassModel(means, variances, totals.Select(t => Math.Log(t / sum)).ToArray());
        }
    }
}