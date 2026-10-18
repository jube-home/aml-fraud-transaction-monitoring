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
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class LinearReporting
    {
        public static double DurbinWatson(double[] residuals)
        {
            var numerator = 0d;
            var denominator = residuals.Sum(e => e * e);

            for (var i = 1; i < residuals.Length; i++)
            {
                var d = residuals[i] - residuals[i - 1];
                numerator += d * d;
            }

            return denominator > 0 ? numerator / denominator : double.NaN;
        }

        public static double[] Vifs(double[][] x)
        {
            var p = x.Length == 0 ? 0 : x[0].Length;

            if (p == 0)
            {
                return Array.Empty<double>();
            }

            if (p == 1)
            {
                return new[] { 1d };
            }

            try
            {
                var inverse = x.Correlation().Inverse();

                return Enumerable.Range(0, p).Select(i => inverse[i][i]).ToArray();
            }
            catch (Exception)
            {
                return Enumerable.Repeat(double.NaN, p).ToArray();
            }
        }

        public static LinearRegressionResult Summarise(LinearFit fit, string[] predictorNames, double[][] x,
            double level,
            bool includeSeries, int dropped, double xCentre = 0, PredictionRow[]? predictions = null)
        {
            var offset = fit.Intercept ? 1 : 0;
            var p = predictorNames.Length;
            var quantile = RegressionStats.TQuantile(0.5 + level / 2, fit.DfResidual);
            var vifs = Vifs(x);
            var sdY = fit.Y.StandardDeviation();
            var rows = new RegressionCoefficientRow[fit.K];

            for (var a = 0; a < fit.K; a++)
            {
                var se = fit.StandardError(a);
                var t = fit.Beta[a] / se;
                var isPredictor = a >= offset;
                var j = a - offset;
                var sdX = isPredictor ? x.Select(r => r[j]).ToArray().StandardDeviation() : double.NaN;

                rows[a] = new RegressionCoefficientRow
                {
                    Term = fit.Terms[a],
                    Estimate = fit.Beta[a],
                    StandardError = se,
                    TStatistic = t,
                    PValue = RegressionStats.TwoSidedTp(t, fit.DfResidual),
                    ConfidenceLower = fit.Beta[a] - quantile * se,
                    ConfidenceUpper = fit.Beta[a] + quantile * se,
                    StandardisedEstimate = isPredictor && sdY > 0 ? fit.Beta[a] * sdX / sdY : double.NaN,
                    VarianceInflationFactor = isPredictor ? vifs[j] : double.NaN,
                    PartialRSquared = t * t / (t * t + fit.DfResidual)
                };
            }

            var warnings = new List<string>();

            if (fit.N < 10 * fit.K)
            {
                warnings.Add(
                    $"Only {fit.N} rows for {fit.K} coefficients (fewer than 10 rows per coefficient); estimates and p-values may be unstable.");
            }

            var worst = vifs.Where(v => !double.IsNaN(v)).DefaultIfEmpty(0).Max();

            if (p > 1 && worst > 10)
            {
                warnings.Add(
                    $"The largest variance inflation factor is {worst:0.##}; predictors are strongly collinear so individual coefficients are imprecise. See VarianceInflationFactors.");
            }

            if (!double.IsNaN(fit.RSquared) && fit.RSquared > 0.9999)
            {
                warnings.Add(
                    "The fit is almost perfect (R squared above 0.9999); standard errors, p-values and the F test are unreliable because the residual variance is close to zero.");
            }

            if (double.IsNaN(fit.RSquared))
            {
                warnings.Add("The outcome has no variation, so R squared and the F test are undefined.");
            }

            if (!fit.Intercept)
            {
                warnings.Add(
                    "No intercept was fitted, so R squared is measured against zero rather than the mean and is not comparable with models that have an intercept.");
            }

            if (dropped > 0)
            {
                warnings.Add($"{dropped} row(s) with missing values were dropped before fitting.");
            }

            return new LinearRegressionResult
            {
                Coefficients = rows,
                Observations = fit.N,
                RowsDropped = dropped,
                PredictorCount = p,
                Intercept = fit.Intercept,
                ResidualDegreesOfFreedom = fit.DfResidual,
                RSquared = fit.RSquared,
                AdjustedRSquared = fit.AdjustedRSquared,
                MultipleR = double.IsNaN(fit.RSquared) ? double.NaN : Math.Sqrt(Math.Max(0, fit.RSquared)),
                ResidualStandardError = fit.Sigma,
                FStatistic = fit.FStatistic,
                FDegreesOfFreedomModel = fit.DfModel,
                FDegreesOfFreedomResidual = fit.DfResidual,
                FpValue = fit.FpValue,
                ResidualSumOfSquares = fit.Rss,
                TotalSumOfSquares = fit.Tss,
                LogLikelihood = fit.LogLikelihood,
                Aic = fit.Aic,
                Aicc = fit.Aicc,
                Bic = fit.Bic,
                DurbinWatson = DurbinWatson(fit.Residuals),
                ConfidenceLevel = level,
                FittedValues = includeSeries ? fit.Fitted : null,
                Residuals = includeSeries ? fit.Residuals : null,
                Predictions = predictions,
                XCentre = xCentre,
                Warnings = warnings.ToArray()
            };
        }

        public static PredictionRow[] Predict(LinearFit fit, double[][] designInputs, double[][] displayInputs,
            double level)
        {
            var quantile = RegressionStats.TQuantile(0.5 + level / 2, fit.DfResidual);
            var rows = new PredictionRow[designInputs.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var q = fit.QuadraticForm(designInputs[i]);
                var prediction = fit.Predict(designInputs[i]);
                var meanSe = fit.Sigma * Math.Sqrt(q);
                var predSe = fit.Sigma * Math.Sqrt(1 + q);

                rows[i] = new PredictionRow
                {
                    Input = displayInputs[i],
                    Prediction = prediction,
                    StandardErrorOfMean = meanSe,
                    MeanLower = prediction - quantile * meanSe,
                    MeanUpper = prediction + quantile * meanSe,
                    PredictionLower = prediction - quantile * predSe,
                    PredictionUpper = prediction + quantile * predSe,
                    Leverage = q
                };
            }

            return rows;
        }

        public static double MaximumTrainingLeverage(LinearFit fit)
        {
            var max = 0d;

            for (var i = 0; i < fit.N; i++)
            {
                max = Math.Max(max, fit.Leverage[i] / (fit.Weights?[i] ?? 1d));
            }

            return max;
        }

        public static double[][] PolynomialDesign(double[] x, int degree, double centre)
        {
            return x.Select(v =>
            {
                var row = new double[degree];
                var t = v - centre;

                for (var k = 0; k < degree; k++)
                {
                    row[k] = Math.Pow(t, k + 1);
                }

                return row;
            }).ToArray();
        }

        public static string[] PolynomialTerms(int degree, bool centred)
        {
            var baseName = centred ? "(x-centre)" : "x";

            return Enumerable.Range(1, degree).Select(k => k == 1 ? baseName : $"{baseName}^{k}").ToArray();
        }

        public static void RequireAll(double[][] rows, string name)
        {
            foreach (var row in rows)
            {
                if (row.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new ArgumentException($"{name} must contain finite numbers only.", name);
                }
            }
        }
    }
}