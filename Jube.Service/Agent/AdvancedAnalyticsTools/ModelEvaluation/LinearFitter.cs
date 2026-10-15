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

using Accord.Statistics.Models.Regression.Linear;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class LinearFitter
    {
        public static (double[] Beta, double Intercept) FitPenalised(double[][] x, double[] y, LinearPenalty penalty,
            double lambda, double l1Ratio)
        {
            var n = x.Length;
            var p = x[0].Length;
            var meanY = y.Average();
            var centred = y.Select(v => v - meanY).ToArray();
            var beta = new double[p];
            var residual = (double[])centred.Clone();

            var alpha = penalty switch
            {
                LinearPenalty.Lasso => 1d,
                LinearPenalty.Ridge => 0d,
                LinearPenalty.ElasticNet => l1Ratio,
                _ => 0d
            };

            var norms = new double[p];

            for (var j = 0; j < p; j++)
            {
                for (var i = 0; i < n; i++)
                {
                    norms[j] += x[i][j] * x[i][j];
                }

                norms[j] /= n;
            }

            for (var iteration = 0; iteration < 2000; iteration++)
            {
                var largest = 0d;

                for (var j = 0; j < p; j++)
                {
                    if (norms[j] <= 1e-12)
                    {
                        continue;
                    }

                    var rho = 0d;

                    for (var i = 0; i < n; i++)
                    {
                        rho += x[i][j] * residual[i];
                    }

                    rho = rho / n + norms[j] * beta[j];
                    var threshold = lambda * alpha;
                    var soft = rho > threshold ? rho - threshold : rho < -threshold ? rho + threshold : 0d;
                    var updated = soft / (norms[j] + lambda * (1d - alpha));
                    var change = updated - beta[j];

                    if (change != 0d)
                    {
                        for (var i = 0; i < n; i++)
                        {
                            residual[i] -= x[i][j] * change;
                        }

                        beta[j] = updated;
                        largest = Math.Max(largest, Math.Abs(change));
                    }
                }

                if (largest < 1e-10)
                {
                    break;
                }
            }

            return (beta, meanY);
        }

        public static LinearRegressionModel Fit(RegressorConfig c, double[][] x, double[] y, RegressionFitContext ctx)
        {
            if (c.Penalty == LinearPenalty.None)
            {
                var learner = new OrdinaryLeastSquares { UseIntercept = true };
                MultipleLinearRegression regression;

                try
                {
                    regression = learner.Learn(x, y);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    throw new ArgumentException(
                        "The predictors are perfectly collinear so ordinary least squares has no unique solution; use a Ridge penalty or drop redundant variables.",
                        nameof(x));
                }

                var weights = (double[])regression.Weights.Clone();

                if (weights.Any(v => !double.IsFinite(v)) || !double.IsFinite(regression.Intercept))
                {
                    throw new ArgumentException(
                        "The predictors are perfectly collinear so ordinary least squares has no unique solution; use a Ridge penalty or drop redundant variables.",
                        nameof(x));
                }

                return new LinearRegressionModel(weights, regression.Intercept, ctx.Transform,
                    "ordinary least squares");
            }

            var fitted = FitPenalised(x, y, c.Penalty, c.Lambda, c.L1Ratio);

            return new LinearRegressionModel(fitted.Beta, fitted.Intercept, ctx.Transform,
                c.Penalty + " penalty, strength " + LearnerConfig.Format(c.Lambda));
        }
    }
}