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

using System.ComponentModel;
using Accord.Math;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    [AdvancedAnalyticsToolGroup("RelationshipsAndRegression")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class LinearRegressionTools
    {
        [AdvancedAnalyticsTool("FitSimpleLinearRegression")]
        [Description(
            "Fits a straight line y = intercept + slope * x by ordinary least squares and reports how well and how reliably it fits. " +
            "The coefficient table gives, for the intercept and the slope, the estimate, standard error, t statistic, two-sided p-value (small means the coefficient differs from 0) and confidence interval, plus the standardised slope (equal to Pearson r) and partial R squared. " +
            "Model-level fields are R squared, adjusted R squared, the residual standard error (typical size of a miss, in units of y), the overall F test, AIC/AICc/BIC (Gaussian likelihood, counting the variance as a parameter) and the Durbin-Watson statistic (near 2 means no serial correlation in row order). " +
            "Optionally give predictAt to receive the fitted value at those x with a confidence interval for the mean and a wider prediction interval for a single new observation. Read Warnings before trusting the p-values. For several predictors use FitMultipleLinearRegression.")]
        public LinearRegressionResult FitSimpleLinearRegression(
            [Description(RegressionText.X)] double[] x,
            [Description(RegressionText.Y)] double[] y,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(
                "Optional x values at which to predict, returned in Predictions with intervals. Leave empty for none.")]
            double[]? predictAt = null,
            [Description(RegressionText.Series)] bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            RegressionInput.RequireSameLength(x, y, nameof(x), nameof(y));

            var data = RegressionInput.Prepare(x.Select(v => new[] { v }).ToArray(), y, new[] { "x" }, null,
                dropIncompleteRows, 3, nameof(x), nameof(y));

            var fit = LinearFit.Fit(data.X, data.Y, null, data.Names, includeIntercept);
            var predictions = PredictSingle(fit, predictAt, confidenceLevel, 0);

            return LinearReporting.Summarise(fit, data.Names, data.X, confidenceLevel, includeFittedAndResiduals,
                data.Dropped, 0, predictions);
        }

        [AdvancedAnalyticsTool("FitMultipleLinearRegression")]
        [Description(
            "Fits an ordinary least squares regression of one numeric outcome on several numeric predictors and returns the full inference table. " +
            "Each coefficient row has the estimate (change in the outcome per unit of that predictor with the others held fixed), standard error, t statistic, two-sided p-value, confidence interval, standardised estimate (change in outcome standard deviations per predictor standard deviation, comparable across predictors), variance inflation factor (above about 10 signals collinearity) and partial R squared. " +
            "Model fields are R squared, adjusted R squared, residual standard error, the overall F test, residual sum of squares, Gaussian log-likelihood with AIC, AICc and BIC, and Durbin-Watson. " +
            "Perfectly collinear or constant columns are rejected with a message. Use FitWeightedLinearRegression for unequal reliability, FitRidgeRegression for collinear predictors, RegressionInfluenceDiagnostics and CheckLinearRegressionAssumptions to validate the fit, and TrainLogisticRegression for a 0/1 outcome.")]
        public LinearRegressionResult FitMultipleLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(RegressionText.Series)] bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 3);
            var fit = LinearFit.Fit(data.X, data.Y, null, data.Names, includeIntercept);

            return LinearReporting.Summarise(fit, data.Names, data.X, confidenceLevel, includeFittedAndResiduals,
                data.Dropped);
        }

        [AdvancedAnalyticsTool("FitWeightedLinearRegression")]
        [Description(
            "Fits a weighted least squares regression, minimising the weighted sum of squared residuals, so rows with larger weights pull the line harder. " +
            "Use it when rows have unequal reliability, for example weights equal to 1/variance for measurements of different precision, or to down-weight suspect rows. Weights are treated as precision weights (they do not change the row count used for degrees of freedom). " +
            "The result has the same layout as FitMultipleLinearRegression: coefficient table with standard errors, t statistics, p-values, confidence intervals, plus R squared (weighted), adjusted R squared, residual standard error on the weighted scale, F test, AIC/BIC and Durbin-Watson. Fitted values and residuals are on the original scale. Every weight must be greater than 0.")]
        public LinearRegressionResult FitWeightedLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Weights)] double[] weights,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(RegressionText.Series)] bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));

            if (weights == null)
            {
                throw new ArgumentException("weights is required. Supply one positive weight per row.",
                    nameof(weights));
            }

            var data = RegressionInput.Prepare(inputs, outputs, variableNames, weights, dropIncompleteRows, 3);
            var fit = LinearFit.Fit(data.X, data.Y, data.Weights, data.Names, includeIntercept);

            return LinearReporting.Summarise(fit, data.Names, data.X, confidenceLevel, includeFittedAndResiduals,
                data.Dropped);
        }

        [AdvancedAnalyticsTool("FitPolynomialRegression")]
        [Description(
            "Fits y as a polynomial of x of the chosen degree (degree 2 is a parabola, 3 a cubic) by least squares, for curved but smooth relationships with one predictor. " +
            "With centreX true (default) the terms are powers of (x - XCentre), where XCentre is the mean of x, which avoids numerical trouble when x is large (for example years); coefficients are then in terms of the centred variable and predictions are unaffected. Use false to get coefficients on raw x. " +
            "The result matches FitSimpleLinearRegression (coefficient table, R squared, F test, AIC/BIC, Durbin-Watson). Optionally give predictAt for fitted values with mean and prediction intervals. High degrees overfit and extrapolate wildly: use SelectPolynomialDegree to choose the degree and keep it well below the number of distinct x values.")]
        public LinearRegressionResult FitPolynomialRegression(
            [Description(RegressionText.X)] double[] x,
            [Description(RegressionText.Y)] double[] y,
            [Description("Highest power of x to include, from 1 to 12. Degree 1 is a straight line.")]
            int degree = 2,
            [Description(
                "True (default) uses powers of x minus its mean for numerical stability; false uses raw powers of x.")]
            bool centreX = true,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(
                "Optional x values at which to predict, returned in Predictions with intervals. Leave empty for none.")]
            double[]? predictAt = null,
            [Description(RegressionText.Series)] bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            RegressionInput.RequireSameLength(x, y, nameof(x), nameof(y));

            if (degree < 1 || degree > 12)
            {
                throw new ArgumentException("degree must be between 1 and 12.", nameof(degree));
            }

            var single = RegressionInput.Prepare(x.Select(v => new[] { v }).ToArray(), y, new[] { "x" }, null,
                dropIncompleteRows, degree + 2, nameof(x), nameof(y));

            var xs = single.X.Select(r => r[0]).ToArray();
            var centre = centreX ? xs.Average() : 0d;
            var design = LinearReporting.PolynomialDesign(xs, degree, centre);
            var terms = LinearReporting.PolynomialTerms(degree, centreX);
            var fit = LinearFit.Fit(design, single.Y, null, terms, true);
            var predictions = PredictSingle(fit, predictAt, confidenceLevel, degree, centre);

            return LinearReporting.Summarise(fit, terms, design, confidenceLevel, includeFittedAndResiduals,
                single.Dropped, centre, predictions);
        }

        [AdvancedAnalyticsTool("SelectPolynomialDegree")]
        [Description(
            "Helps choose the degree of a polynomial fit of y on x by fitting every degree from 1 up to maxDegree and scoring each. " +
            "For every degree it returns R squared, adjusted R squared, residual standard error, AIC, BIC, the exact leave-one-out cross-validated RMSE (from the hat matrix, so no refitting) and the p-value of the incremental F test against the previous degree. " +
            "The BestBy fields name the degree preferred by AIC, BIC, adjusted R squared and leave-one-out error; BestBySequentialFTest is the degree reached by adding terms while each addition is significant at 5 percent. When they disagree prefer the simpler degree favoured by BIC or leave-one-out. Degrees that cannot be fitted (too few rows or collinear) are marked Fitted false.")]
        public PolynomialDegreeSelection SelectPolynomialDegree(
            [Description(RegressionText.X)] double[] x,
            [Description(RegressionText.Y)] double[] y,
            [Description("Highest degree to try, from 2 to 12. Default 6.")]
            int maxDegree = 6,
            [Description("True (default) centres x on its mean before taking powers for numerical stability.")]
            bool centreX = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            if (maxDegree < 2 || maxDegree > 12)
            {
                throw new ArgumentException("maxDegree must be between 2 and 12.", nameof(maxDegree));
            }

            RegressionInput.RequireSameLength(x, y, nameof(x), nameof(y));

            var single = RegressionInput.Prepare(x.Select(v => new[] { v }).ToArray(), y, new[] { "x" }, null,
                dropIncompleteRows, 4, nameof(x), nameof(y));

            var xs = single.X.Select(r => r[0]).ToArray();
            var centre = centreX ? xs.Average() : 0d;
            var scores = new List<PolynomialDegreeScore>();
            LinearFit? previous = null;
            var sequential = 0;
            var sequentialOpen = true;

            for (var d = 1; d <= maxDegree; d++)
            {
                LinearFit fit;

                try
                {
                    fit = LinearFit.Fit(LinearReporting.PolynomialDesign(xs, d, centre), single.Y, null,
                        LinearReporting.PolynomialTerms(d, centreX), true);
                }
                catch (ArgumentException)
                {
                    scores.Add(new PolynomialDegreeScore
                    {
                        Degree = d,
                        Fitted = false,
                        RSquared = double.NaN,
                        AdjustedRSquared = double.NaN,
                        ResidualStandardError = double.NaN,
                        Aic = double.NaN,
                        Bic = double.NaN,
                        LeaveOneOutRmse = double.NaN,
                        IncrementalFpValue = double.NaN
                    });

                    previous = null;
                    continue;
                }

                var press = 0d;

                for (var i = 0; i < fit.N; i++)
                {
                    var e = fit.Residuals[i] / Math.Max(1e-12, 1 - fit.Leverage[i]);
                    press += e * e;
                }

                var incremental = double.NaN;

                if (previous != null)
                {
                    var f = (previous.Rss - fit.Rss) / (fit.Rss / fit.DfResidual);
                    incremental = RegressionStats.FUpperTail(f, 1, fit.DfResidual);
                }
                else if (d == 1)
                {
                    incremental = fit.FpValue;
                }

                if (sequentialOpen && incremental < 0.05)
                {
                    sequential = d;
                }
                else
                {
                    sequentialOpen = false;
                }

                scores.Add(new PolynomialDegreeScore
                {
                    Degree = d,
                    Fitted = true,
                    RSquared = fit.RSquared,
                    AdjustedRSquared = fit.AdjustedRSquared,
                    ResidualStandardError = fit.Sigma,
                    Aic = fit.Aic,
                    Bic = fit.Bic,
                    LeaveOneOutRmse = Math.Sqrt(press / fit.N),
                    IncrementalFpValue = incremental
                });

                previous = fit;
            }

            var ok = scores.Where(s => s.Fitted).ToList();

            if (ok.Count == 0)
            {
                throw new ArgumentException(
                    "No polynomial degree could be fitted. Supply more rows or check that x varies.", nameof(x));
            }

            return new PolynomialDegreeSelection
            {
                Degrees = scores.ToArray(),
                BestByAic = ok.OrderBy(s => s.Aic).First().Degree,
                BestByBic = ok.OrderBy(s => s.Bic).First().Degree,
                BestByAdjustedRSquared = ok.OrderByDescending(s => s.AdjustedRSquared).First().Degree,
                BestByLeaveOneOut = ok.OrderBy(s => s.LeaveOneOutRmse).First().Degree,
                BestBySequentialFTest = sequential,
                Observations = single.N,
                XCentre = centre
            };
        }

        [AdvancedAnalyticsTool("PredictWithLinearRegression")]
        [Description(
            "Fits a least squares regression to the supplied training rows and immediately predicts the outcome for new rows, with uncertainty. " +
            "For every new row it returns the prediction, the standard error of the mean response, a confidence interval for the average outcome at those predictor values (narrow, about where the line is), a prediction interval for a single new observation (wide, includes the row-level noise) and the leverage of the new row. " +
            "ExtrapolatedRows counts new rows lying outside the region covered by the training data (leverage above every training row); treat those predictions cautiously. Optional weights give weighted least squares; a new observation is assumed to have weight 1 in the prediction interval. The model is refitted on each call and nothing is stored.")]
        public LinearPredictionResult PredictWithLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "New rows to predict for: one array per case with the same number of columns and order as the training predictors.")]
            double[][] newInputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(
                "Optional precision weight per training row for weighted least squares. Leave empty for ordinary least squares.")]
            double[]? weights = null,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, weights, dropIncompleteRows, 3);
            var p = data.P;

            if (newInputs == null || newInputs.Length == 0)
            {
                throw new ArgumentException("newInputs is required: at least one row of predictor values.",
                    nameof(newInputs));
            }

            for (var i = 0; i < newInputs.Length; i++)
            {
                if (newInputs[i] == null || newInputs[i].Length != p)
                {
                    throw new ArgumentException(
                        $"newInputs row {i} has {newInputs[i]?.Length ?? 0} value(s) but the training rows have {p}.",
                        nameof(newInputs));
                }

                if (newInputs[i].Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new ArgumentException($"newInputs row {i} contains a missing or infinite value.",
                        nameof(newInputs));
                }
            }

            var fit = LinearFit.Fit(data.X, data.Y, data.Weights, data.Names, includeIntercept);
            var predictions = LinearReporting.Predict(fit, newInputs, newInputs, confidenceLevel);
            var limit = LinearReporting.MaximumTrainingLeverage(fit) * (1 + 1e-9);
            var extrapolated = predictions.Count(r => r.Leverage > limit);
            var warnings = new List<string>();

            if (extrapolated > 0)
            {
                warnings.Add(
                    $"{extrapolated} new row(s) lie outside the region covered by the training rows, so their predictions are extrapolations.");
            }

            return new LinearPredictionResult
            {
                Predictions = predictions,
                ConfidenceLevel = confidenceLevel,
                ResidualStandardError = fit.Sigma,
                ResidualDegreesOfFreedom = fit.DfResidual,
                Observations = fit.N,
                RowsDropped = data.Dropped,
                ExtrapolatedRows = extrapolated,
                Warnings = warnings.ToArray()
            };
        }

        [AdvancedAnalyticsTool("CompareNestedLinearModels")]
        [Description(
            "Tests whether a group of predictors adds explanatory power, by comparing the full regression against a reduced regression with the named predictors removed (the partial F test). " +
            "It returns the F statistic, its degrees of freedom and the p-value (small means the dropped predictors jointly matter and the full model is justified), the residual sum of squares, R squared, AIC and BIC of both models and a plain-language conclusion. " +
            "Use it to test several coefficients at once, for example a set of dummy variables, which single t tests cannot do. dropVariables must name at least one predictor and leave at least one (or an intercept-only reduced model when an intercept is included).")]
        public NestedModelComparison CompareNestedLinearModels(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Names of the predictor columns to remove from the full model to form the reduced model. Must match variableNames.")]
            string[] dropVariables,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 4);

            if (dropVariables == null || dropVariables.Length == 0)
            {
                throw new ArgumentException("dropVariables must name at least one predictor to remove.",
                    nameof(dropVariables));
            }

            foreach (var name in dropVariables)
            {
                if (!data.Names.Contains(name, StringComparer.Ordinal))
                {
                    throw new ArgumentException(
                        $"'{name}' is not one of the variable names ({string.Join(", ", data.Names)}).",
                        nameof(dropVariables));
                }
            }

            var keep = Enumerable.Range(0, data.P).Where(j => !dropVariables.Contains(data.Names[j])).ToArray();

            if (keep.Length == 0 && !includeIntercept)
            {
                throw new ArgumentException(
                    "Removing every predictor leaves nothing to fit when no intercept is included.",
                    nameof(dropVariables));
            }

            var full = LinearFit.Fit(data.X, data.Y, null, data.Names, includeIntercept);

            var reduced = keep.Length == 0
                ? LinearFit.Fit(data.X.Select(_ => Array.Empty<double>()).ToArray(), data.Y, null,
                    Array.Empty<string>(),
                    true)
                : LinearFit.Fit(data.X.Select(r => keep.Select(j => r[j]).ToArray()).ToArray(), data.Y, null,
                    keep.Select(j => data.Names[j]).ToArray(), includeIntercept);

            var q = full.K - (keep.Length == 0 ? 1 : reduced.K);
            var f = (reduced.Rss - full.Rss) / q / (full.Rss / full.DfResidual);
            var p = RegressionStats.FUpperTail(f, q, full.DfResidual);
            var reducedRSquared = keep.Length == 0 ? 0d : reduced.RSquared;

            return new NestedModelComparison
            {
                FullVariables = data.Names,
                ReducedVariables = keep.Select(j => data.Names[j]).ToArray(),
                DroppedVariables = dropVariables,
                FStatistic = f,
                DegreesOfFreedomDifference = q,
                ResidualDegreesOfFreedomFull = full.DfResidual,
                PValue = p,
                RssFull = full.Rss,
                RssReduced = reduced.Rss,
                RSquaredFull = full.RSquared,
                RSquaredReduced = reducedRSquared,
                AicFull = full.Aic,
                AicReduced = reduced.Aic,
                BicFull = full.Bic,
                BicReduced = reduced.Bic,
                Conclusion = p < 0.05
                    ? $"The dropped predictors jointly improve the fit (F = {f:0.###}, p = {p:0.####}); keep the full model."
                    : $"No significant joint improvement from the dropped predictors (F = {f:0.###}, p = {p:0.####}); the reduced model is adequate."
            };
        }

        [AdvancedAnalyticsTool("FitMultivariateLinearRegression")]
        [Description(
            "Fits the same predictors to several numeric outcomes at once (multivariate multiple regression), returning one complete regression per outcome column. " +
            "Each entry in Models has the coefficient table, R squared, F test, AIC/BIC and Durbin-Watson exactly as FitMultipleLinearRegression would give for that outcome alone; the estimates are identical to fitting each outcome separately, which is the least squares solution. " +
            "Use it to see how a set of drivers affects several measures together. Outcomes are named in outputNames (or y1, y2, ...). Rows with NaN in any outcome or predictor are rejected unless dropIncompleteRows is true.")]
        public MultivariateRegressionResult FitMultivariateLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(
                "Outcome rows: one array per case with one value per outcome column, same row count as inputs.")]
            double[][] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(
                "Optional name of each outcome column, unique and in column order. Leave empty to use y1, y2 and so on.")]
            string[]? outputNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            var outputColumns = RegressionInput.ColumnCount(outputs, nameof(outputs));

            var labels = outputNames == null || outputNames.Length == 0
                ? Enumerable.Range(1, outputColumns).Select(i => "y" + i).ToArray()
                : RegressionInput.ResolveNames(outputNames, outputColumns, nameof(outputNames));

            var columns = RegressionInput.Columns(outputs);
            var stacked = new double[outputs.Length];
            var models = new LinearRegressionResult[outputColumns];

            if (inputs == null || inputs.Length != outputs.Length)
            {
                throw new ArgumentException(
                    $"inputs has {inputs?.Length ?? 0} row(s) but outputs has {outputs.Length}. They must be the same length.",
                    nameof(outputs));
            }

            var incomplete = new bool[outputs.Length];

            for (var i = 0; i < outputs.Length; i++)
            {
                incomplete[i] = outputs[i].Any(double.IsNaN);
            }

            if (incomplete.Any(b => b) && !dropIncompleteRows)
            {
                throw new ArgumentException(
                    $"outputs row {Array.IndexOf(incomplete, true)} has a missing (NaN) value. Set dropIncompleteRows to true or fill the gap first.",
                    nameof(outputs));
            }

            var keepRows = Enumerable.Range(0, outputs.Length).Where(i => !incomplete[i]).ToArray();
            var keptInputs = keepRows.Select(i => inputs[i]).ToArray();

            for (var c = 0; c < outputColumns; c++)
            {
                var y = keepRows.Select(i => columns[c][i]).ToArray();
                var data = RegressionInput.Prepare(keptInputs, y, variableNames, null, dropIncompleteRows, 3);
                var fit = LinearFit.Fit(data.X, data.Y, null, data.Names, includeIntercept);

                models[c] = LinearReporting.Summarise(fit, data.Names, data.X, confidenceLevel, false,
                    data.Dropped + (outputs.Length - keepRows.Length));
            }

            GC.KeepAlive(stacked);

            return new MultivariateRegressionResult { Outputs = labels, Models = models };
        }

        private static PredictionRow[]? PredictSingle(LinearFit fit, double[]? predictAt, double level, int degree,
            double centre = 0)
        {
            if (predictAt == null || predictAt.Length == 0)
            {
                return null;
            }

            if (predictAt.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new ArgumentException("predictAt must contain finite numbers only.", nameof(predictAt));
            }

            var design = degree == 0
                ? predictAt.Select(v => new[] { v }).ToArray()
                : LinearReporting.PolynomialDesign(predictAt, degree, centre);

            return LinearReporting.Predict(fit, design, predictAt.Select(v => new[] { v }).ToArray(), level);
        }
    }
}