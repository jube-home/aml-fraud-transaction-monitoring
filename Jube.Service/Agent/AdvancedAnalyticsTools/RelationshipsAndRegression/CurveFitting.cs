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
using Accord.Math.Optimization;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class CurveFitting
    {
        public static NonlinearCurveResult Fit(CurveDefinition definition, double[] x, double[] y, double[]? start,
            int maximumIterations, double level, bool includeSeries, double[]? predictAt, int dropped)
        {
            var n = x.Length;
            var p = definition.Parameters.Length;

            if (n < p + 2)
            {
                throw new ArgumentException(
                    $"The {definition.Family} curve has {p} parameters and needs at least {p + 2} points; {n} were supplied.",
                    nameof(x));
            }

            if (definition.RequiresPositiveX && x.Any(v => !(v > 0)))
            {
                throw new ArgumentException(
                    $"The {definition.Family} curve needs every x greater than 0. Remove or shift the non-positive values.",
                    nameof(x));
            }

            if (definition.RequiresNonNegativeX && x.Any(v => v < 0))
            {
                throw new ArgumentException($"The {definition.Family} curve needs every x to be 0 or more.", nameof(x));
            }

            if (!(x.Max() > x.Min()))
            {
                throw new ArgumentException("x is constant, so no curve can be fitted.", nameof(x));
            }

            if (start != null && (start.Length != p || start.Any(v => double.IsNaN(v) || double.IsInfinity(v))))
            {
                throw new ArgumentException(
                    $"startValues must contain {p} finite numbers, in the order {string.Join(", ", definition.Parameters)}.",
                    "startValues");
            }

            var initial = start != null ? (double[])start.Clone() : definition.Start(x, y);

            if (initial.Length != p || initial.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new ArgumentException(
                    "Automatic starting values could not be found for this data. Supply startValues.", "startValues");
            }

            var inputs = x.Select(v => new[] { v }).ToArray();

            var regression = new NonlinearRegression(p, (c, xi) => definition.Evaluate(c, xi[0]),
                (c, xi, r) => definition.Gradient(c, xi[0], r));

            Array.Copy(initial, regression.Coefficients, p);
            var solver = new LevenbergMarquardt(p) { MaxIterations = maximumIterations, Tolerance = 1e-12 };
            var warnings = new List<string>();

            try
            {
                new NonlinearLeastSquares(regression, solver) { ComputeStandardErrors = false }.Learn(inputs, y);
            }
            catch (Exception)
            {
                throw new ArgumentException(
                    $"The {definition.Family} curve could not be fitted from the starting values {string.Join(", ", initial.Select(v => v.ToString("0.####")))}: the search produced non-finite numbers. Try different startValues or another family.",
                    "startValues");
            }

            var theta = (double[])regression.Coefficients.Clone();

            if (theta.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new ArgumentException(
                    $"The {definition.Family} curve did not converge to finite parameters. Try different startValues or another family.",
                    "startValues");
            }

            var fitted = x.Select(v => definition.Evaluate(theta, v)).ToArray();

            if (fitted.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new ArgumentException(
                    $"The {definition.Family} curve produced non-finite values at the solution. Try different startValues or another family.",
                    "startValues");
            }

            var residuals = y.Zip(fitted, (a, b) => a - b).ToArray();
            var rss = residuals.Sum(e => e * e);

            var jacobian = x.Select(v =>
            {
                var g = new double[p];
                definition.Gradient(theta, v, g);

                return g;
            }).ToArray();

            var normal = new double[p][];

            for (var a = 0; a < p; a++)
            {
                normal[a] = new double[p];

                for (var b = 0; b < p; b++)
                {
                    normal[a][b] = jacobian.Sum(r => r[a] * r[b]);
                }
            }

            double[][]? inverse;

            try
            {
                inverse = normal.Inverse();

                if (inverse.Any(r => r.Any(v => double.IsNaN(v) || double.IsInfinity(v))))
                {
                    inverse = null;
                }
            }
            catch (Exception)
            {
                inverse = null;
            }

            var dfResidual = n - p;
            var sigma2 = rss / dfResidual;
            var quantile = RegressionStats.TQuantile(0.5 + level / 2, dfResidual);
            var rows = new NonlinearParameterRow[p];

            for (var j = 0; j < p; j++)
            {
                var se = inverse == null ? double.NaN : Math.Sqrt(Math.Max(0, sigma2 * inverse[j][j]));
                var t = theta[j] / se;

                rows[j] = new NonlinearParameterRow
                {
                    Parameter = definition.Parameters[j],
                    Estimate = theta[j],
                    StandardError = se,
                    TStatistic = t,
                    PValue = RegressionStats.TwoSidedTp(t, dfResidual),
                    ConfidenceLower = theta[j] - quantile * se,
                    ConfidenceUpper = theta[j] + quantile * se
                };
            }

            var mean = y.Average();
            var tss = y.Sum(v => (v - mean) * (v - mean));
            var logLikelihood = rss > 0 ? -n / 2d * (Math.Log(2 * Math.PI) + Math.Log(rss / n) + 1) : double.NaN;
            var parameters = p + 1;

            var gradientNorm = Enumerable.Range(0, p)
                .Select(j => Math.Abs(jacobian.Select((r, i) => r[j] * residuals[i]).Sum()))
                .Max();

            var scale = Math.Sqrt(rss) * Math.Sqrt(normal.Select((r, a) => r[a]).Max());
            var converged = rss == 0 || gradientNorm <= 1e-5 * Math.Max(scale, 1e-300);

            if (!converged)
            {
                warnings.Add(
                    "The search stopped before the gradient vanished; the estimates may not be a true minimum. Raise maximumIterations or supply better startValues.");
            }

            if (inverse == null)
            {
                warnings.Add(
                    "The parameters are not separately identifiable from this data (singular Jacobian), so standard errors are undefined. Simplify the curve or supply more varied data.");
            }

            if (dropped > 0)
            {
                warnings.Add($"{dropped} row(s) with missing values were dropped before fitting.");
            }

            CurvePrediction[]? predictions = null;

            if (predictAt != null && predictAt.Length > 0)
            {
                if (predictAt.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new ArgumentException("predictAt must contain finite numbers only.", nameof(predictAt));
                }

                if (definition.RequiresPositiveX && predictAt.Any(v => !(v > 0)))
                {
                    throw new ArgumentException(
                        $"The {definition.Family} curve needs every predictAt value greater than 0.",
                        nameof(predictAt));
                }

                predictions = predictAt.Select(v =>
                {
                    var g = new double[p];
                    definition.Gradient(theta, v, g);
                    var q = 0d;

                    if (inverse != null)
                    {
                        for (var a = 0; a < p; a++)
                        {
                            for (var b = 0; b < p; b++)
                            {
                                q += g[a] * inverse[a][b] * g[b];
                            }
                        }
                    }
                    else
                    {
                        q = double.NaN;
                    }

                    var value = definition.Evaluate(theta, v);
                    var meanSe = Math.Sqrt(sigma2 * q);
                    var predSe = Math.Sqrt(sigma2 * (1 + q));

                    return new CurvePrediction
                    {
                        X = v,
                        Prediction = value,
                        StandardErrorOfMean = meanSe,
                        MeanLower = value - quantile * meanSe,
                        MeanUpper = value + quantile * meanSe,
                        PredictionLower = value - quantile * predSe,
                        PredictionUpper = value + quantile * predSe
                    };
                }).ToArray();
            }

            return new NonlinearCurveResult
            {
                Family = definition.Family.ToString(),
                Formula = definition.Formula,
                Parameters = rows,
                Converged = converged,
                ResidualSumOfSquares = rss,
                ResidualStandardError = Math.Sqrt(sigma2),
                RSquared = tss > 0 ? 1 - rss / tss : double.NaN,
                AdjustedRSquared = tss > 0 ? 1 - rss / dfResidual / (tss / (n - 1)) : double.NaN,
                Aic = -2 * logLikelihood + 2 * parameters,
                Bic = -2 * logLikelihood + parameters * Math.Log(n),
                LogLikelihood = logLikelihood,
                ResidualDegreesOfFreedom = dfResidual,
                StartValuesUsed = initial,
                FittedValues = includeSeries ? fitted : null,
                Residuals = includeSeries ? residuals : null,
                Predictions = predictions,
                Observations = n,
                RowsDropped = dropped,
                Warnings = warnings.ToArray()
            };
        }
    }
}