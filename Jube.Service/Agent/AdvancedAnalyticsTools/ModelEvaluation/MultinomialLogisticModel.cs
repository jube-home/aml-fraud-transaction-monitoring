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

using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class MultinomialLogisticModel : IMulticlassModel
    {
        private readonly MultinomialLogisticRegression model;

        private MultinomialLogisticModel(MultinomialLogisticRegression model)
        {
            this.model = model;
        }

        public double[][] Probabilities(double[][] rows)
        {
            return rows.Select(r => MulticlassMath.Normalise(model.Probabilities(r))).ToArray();
        }

        public ModelDescription Describe(string[] names)
        {
            var strength = new double[names.Length];

            for (var j = 0; j < names.Length; j++)
            {
                foreach (var coefficients in model.Coefficients)
                {
                    strength[j] += Math.Abs(coefficients[j + 1]);
                }
            }

            var total = strength.Sum();

            return new ModelDescription(
                "Multinomial (softmax) logistic regression on standardised predictors with the last class as reference. Importance is the summed absolute standardised coefficient across classes.",
                [
                    ModelText.Fact("Classes", model.NumberOfOutputs),
                    ModelText.Fact("Parameters", model.NumberOfParameters)
                ],
                [],
                null,
                ModelText.Rank(names, total > 0 ? strength.Select(s => s / total).ToArray() : strength));
        }

        public static MultinomialLogisticModel Fit(double[][] x, int[] y, int classes, CancellationToken token)
        {
            var regression = new MultinomialLogisticRegression(x[0].Length, classes);
            var learner = new LowerBoundNewtonRaphson(regression) { MaxIterations = 100, Token = token };

            try
            {
                learner.Learn(x, y);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new ArgumentException(
                    "The multinomial logistic fit failed, usually because the classes are perfectly separable or predictors are collinear; use another algorithm or fewer predictors.",
                    nameof(x), exception);
            }

            if (regression.Coefficients.Any(row => row.Any(v => !double.IsFinite(v))))
            {
                throw new ArgumentException(
                    "The multinomial logistic fit diverged, usually because the classes are perfectly separable; use another algorithm or fewer predictors.",
                    nameof(x));
            }

            return new MultinomialLogisticModel(regression);
        }
    }
}