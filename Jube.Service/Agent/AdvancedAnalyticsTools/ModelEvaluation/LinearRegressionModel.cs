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

using Accord.Math;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class LinearRegressionModel : IRegressionModel
    {
        private readonly double[] beta;
        private readonly double intercept;
        private readonly int nonZero;
        private readonly string penalty;
        private readonly FeatureTransform transform;

        public LinearRegressionModel(double[] beta, double intercept, FeatureTransform transform, string penalty)
        {
            this.beta = beta;
            this.intercept = intercept;
            this.transform = transform;
            this.penalty = penalty;
            nonZero = beta.Count(b => Math.Abs(b) > 1e-12);
        }

        public double[] Predict(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = intercept;

                for (var j = 0; j < beta.Length; j++)
                {
                    sum += beta[j] * rows[i][j];
                }

                result[i] = sum;
            }

            return result;
        }

        public EvaluationNumber[] Coefficients(string[] names)
        {
            var list = new List<EvaluationNumber>();
            var originalIntercept = intercept;

            for (var j = 0; j < names.Length; j++)
            {
                var sd = transform.Deviations[j];
                var original = sd > 0 ? beta[j] / sd : 0d;
                originalIntercept -= sd > 0 ? beta[j] * transform.Means[j] / sd : 0d;
                list.Add(ModelText.Fact(names[j], original));
            }

            list.Insert(0, ModelText.Fact("Intercept", originalIntercept));

            return list.ToArray();
        }

        public ModelDescription Describe(string[] names)
        {
            var importance = beta.Select(Math.Abs).ToArray();
            var total = importance.Sum();
            var normalised = total > 0 ? importance.Select(v => v / total).ToArray() : importance;

            return new ModelDescription(
                $"Linear regression ({penalty}) on standardised predictors; coefficients are reported on the original scale (change in the target per one unit of the variable). Importance is the share of absolute standardised coefficient.",
                [ModelText.Fact("NonZeroCoefficients", nonZero), ModelText.Fact("Variables", names.Length)],
                [],
                null,
                ModelText.Rank(names, normalised));
        }
    }
}