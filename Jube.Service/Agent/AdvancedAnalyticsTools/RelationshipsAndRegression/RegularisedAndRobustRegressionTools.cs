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
using Accord;
using Accord.MachineLearning;
using Accord.Math.Random;
using Accord.Statistics;
using Accord.Statistics.Analysis;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Models.Regression.Linear;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    [AdvancedAnalyticsToolGroup("RelationshipsAndRegression")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class RegularisedAndRobustRegressionTools
    {
        [AdvancedAnalyticsTool("FitRidgeRegression")]
        [Description(
            "Fits a ridge (L2 penalised) linear regression, which shrinks coefficients towards zero to stabilise estimates when predictors are collinear or numerous relative to the rows. " +
            "Predictors are standardised to unit sample variance and the outcome centred, then the sum of squared residuals plus lambda times the sum of squared standardised coefficients is minimised; lambda 0 reproduces ordinary least squares and larger values shrink harder. " +
            "It returns coefficients on the original scale and on the standardised scale (comparable across predictors), the intercept, the effective degrees of freedom excluding the intercept (how many parameters the penalised fit really uses), R squared, the residual standard error, the generalised cross-validation score n*RSS/(n-1-EffectiveDegreesOfFreedom)^2 (lower is better, use it to compare lambdas; the 1 is the unpenalised intercept) and approximate standard errors that ignore shrinkage bias. " +
            "No p-values are given because shrunken estimates are biased. Use TraceRidgeRegularisationPath to choose lambda.")]
        public RidgeRegressionResult FitRidgeRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Penalty strength on the standardised coefficients, zero or more. Typical useful values run from about 0.01 to n times 10; 0 is ordinary least squares.")]
            double lambda = 1,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Series)] bool includeFittedValues = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RequireLambda(lambda, nameof(lambda));
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 3);
            RequireRidgeSize(data);
            var setup = RidgeSetup.Create(data.X, data.Y);
            var solution = RidgeSolution.Solve(setup, lambda);
            double[] ols;

            try
            {
                ols = lambda == 0 ? solution.Coefficients : RidgeSolution.Solve(setup, 0).Coefficients;
            }
            catch (ArgumentException)
            {
                ols = Enumerable.Repeat(double.NaN, data.P).ToArray();
            }

            var warnings = new List<string>();

            if (data.Dropped > 0)
            {
                warnings.Add($"{data.Dropped} row(s) with missing values were dropped before fitting.");
            }

            if (data.N <= data.P + 1)
            {
                warnings.Add(
                    "There are not many more rows than predictors; the generalised cross-validation score is unreliable.");
            }

            return new RidgeRegressionResult
            {
                Lambda = lambda,
                Coefficients = data.Names.Select((n, j) => new RidgeCoefficientRow
                {
                    Term = n,
                    Estimate = solution.Coefficients[j],
                    StandardisedEstimate = solution.Standardised[j],
                    ApproximateStandardError = solution.StandardErrors[j],
                    OrdinaryLeastSquaresEstimate = ols[j]
                }).ToArray(),
                Intercept = solution.Intercept,
                EffectiveDegreesOfFreedom = solution.EffectiveDegreesOfFreedom,
                RSquared = solution.RSquared,
                ResidualStandardError = solution.ResidualStandardError,
                GeneralisedCrossValidation = solution.Gcv,
                Observations = data.N,
                RowsDropped = data.Dropped,
                FittedValues = includeFittedValues ? solution.Fitted : null,
                Warnings = warnings.ToArray()
            };
        }

        [AdvancedAnalyticsTool("TraceRidgeRegularisationPath")]
        [Description(
            "Fits ridge regression across a whole range of penalty strengths so you can see how each coefficient shrinks and pick lambda. " +
            "For every lambda it returns the coefficients (original and standardised scale), intercept, effective degrees of freedom, R squared and the generalised cross-validation score; BestLambdaByGcv and BestIndex identify the lambda with the lowest GCV, a sensible default that balances fit against complexity. " +
            "Give your own lambdas, or leave them empty to get pathLength values spaced geometrically from 1e-4 to 1e2 times n-1 plus lambda 0. Coefficients that change sign or swing wildly at small lambda mark collinear predictors.")]
        public RidgePathResult TraceRidgeRegularisationPath(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Optional list of lambda values (each zero or more). Leave empty for an automatic geometric grid.")]
            double[]? lambdas = null,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description("Number of automatic lambdas when none are given, from 3 to 200. Default 25.")]
            int pathLength = 25,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 3);
            RequireRidgeSize(data);

            if (pathLength < 3 || pathLength > 200)
            {
                throw new ArgumentException("pathLength must be between 3 and 200.", nameof(pathLength));
            }

            double[] grid;

            if (lambdas == null || lambdas.Length == 0)
            {
                var top = 1e2 * (data.N - 1d);
                var bottom = 1e-4 * (data.N - 1d);

                grid = new[] { 0d }.Concat(Enumerable.Range(0, pathLength - 1)
                    .Select(i => bottom * Math.Pow(top / bottom, i / (pathLength - 2d)))).ToArray();
            }
            else
            {
                if (lambdas.Length > 500)
                {
                    throw new ArgumentException("At most 500 lambdas are accepted.", nameof(lambdas));
                }

                foreach (var l in lambdas)
                {
                    RequireLambda(l, nameof(lambdas));
                }

                grid = lambdas;
            }

            var setup = RidgeSetup.Create(data.X, data.Y);
            var path = new List<RidgePathPoint>();

            foreach (var l in grid)
            {
                RidgeSolution solution;

                try
                {
                    solution = RidgeSolution.Solve(setup, l);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                path.Add(new RidgePathPoint
                {
                    Lambda = l,
                    Estimates = solution.Coefficients,
                    StandardisedEstimates = solution.Standardised,
                    Intercept = solution.Intercept,
                    EffectiveDegreesOfFreedom = solution.EffectiveDegreesOfFreedom,
                    RSquared = solution.RSquared,
                    GeneralisedCrossValidation = solution.Gcv
                });
            }

            if (path.Count == 0)
            {
                throw new ArgumentException(
                    "No lambda produced a solution. The predictors are perfectly collinear; supply a positive lambda.",
                    nameof(lambdas));
            }

            var best = path.Select((p, i) => (p, i)).Where(t => !double.IsNaN(t.p.GeneralisedCrossValidation))
                .OrderBy(t => t.p.GeneralisedCrossValidation).Select(t => t.i).DefaultIfEmpty(0).First();

            return new RidgePathResult
            {
                Variables = data.Names,
                Path = path.ToArray(),
                BestLambdaByGcv = path[best].Lambda,
                BestIndex = best,
                Observations = data.N
            };
        }

        [AdvancedAnalyticsTool("FitNonNegativeLeastSquares")]
        [Description(
            "Fits a linear model with no intercept in which every coefficient is constrained to be zero or positive (Lawson-Hanson non-negative least squares). " +
            "Use it when negative contributions make no sense: mixing proportions, additive cost components, attributing a total to non-negative drivers. Coefficients pushed to exactly 0 are the drivers the data does not support; ActiveVariables lists those with positive coefficients. " +
            "It returns the coefficients, residual sum of squares, RMSE and the uncentred R squared (1 - RSS / sum of squares of y, because there is no intercept). No standard errors or p-values are produced because the constraint makes ordinary inference invalid.")]
        public NonNegativeLeastSquaresResult FitNonNegativeLeastSquares(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Series)] bool includeFittedValues = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 2);
            var scale = Enumerable.Range(0, data.P).Max(j => Math.Abs(data.X.Select((r, i) => r[j] * data.Y[i]).Sum()));

            var regression =
                new NonNegativeLeastSquares { Tolerance = 1e-10 * Math.Max(1, scale) }.Learn(data.X, data.Y);

            var coefficients = (double[])regression.Weights.Clone();
            var fitted = data.X.Select(r => r.Zip(coefficients, (v, c) => v * c).Sum()).ToArray();
            var rss = data.Y.Zip(fitted, (y, f) => (y - f) * (y - f)).Sum();
            var total = data.Y.Sum(v => v * v);

            return new NonNegativeLeastSquaresResult
            {
                Variables = data.Names,
                Coefficients = coefficients,
                ActiveVariables = data.Names.Where((_, j) => coefficients[j] > 0).ToArray(),
                ResidualSumOfSquares = rss,
                UncentredRSquared = total > 0 ? 1 - rss / total : double.NaN,
                RootMeanSquaredError = Math.Sqrt(rss / data.N),
                Observations = data.N,
                RowsDropped = data.Dropped,
                FittedValues = includeFittedValues ? fitted : null
            };
        }

        [AdvancedAnalyticsTool("FitRansacLinearRegression")]
        [Description(
            "Fits a linear regression that resists gross outliers using RANSAC: it repeatedly fits the model to a random minimal subset of rows, counts how many rows lie within a residual threshold of that fit (the inliers), keeps the fit with the most inliers, and refits by least squares on those inliers only. " +
            "Use it when a minority of rows are corrupted (fraud amounts entered wrongly, sensor glitches) and an ordinary fit is dragged off. It returns InlierModel (full coefficient table and statistics on the inliers), OrdinaryModel (the plain fit on all rows for comparison), the inlier and outlier row indices, the inlier fraction, the threshold used, the trials performed and the seed. " +
            "The threshold is a residual size in y units; leave it empty to use 2.5 times the robust scale (1.4826 x median absolute deviation) of the ordinary residuals. Results are reproducible for a given seed. Inference on the inlier model ignores the selection step, so treat its p-values as optimistic.")]
        public RansacRegressionResult FitRansacLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Largest absolute residual for a row to count as an inlier, in units of the outcome. Leave empty for the automatic robust threshold.")]
            double? inlierThreshold = null,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description("Random seed that makes the subset sampling reproducible. Default 1.")]
            int seed = 1,
            [Description("Maximum number of random subsets to try, from 10 to 100000. Default 1000.")]
            int maximumTrials = 1000,
            [Description(
                "Desired probability of drawing at least one outlier-free subset, between 0.5 and 0.9999. Default 0.99.")]
            double successProbability = 0.99,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));

            if (maximumTrials < 10 || maximumTrials > 100000)
            {
                throw new ArgumentException("maximumTrials must be between 10 and 100000.", nameof(maximumTrials));
            }

            if (!(successProbability >= 0.5 && successProbability <= 0.9999))
            {
                throw new ArgumentException("successProbability must be between 0.5 and 0.9999.",
                    nameof(successProbability));
            }

            if (inlierThreshold.HasValue && !(inlierThreshold.Value > 0 && !double.IsInfinity(inlierThreshold.Value)))
            {
                throw new ArgumentException("inlierThreshold must be a positive finite number, or empty for automatic.",
                    nameof(inlierThreshold));
            }

            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 5);
            var k = data.P + 1;

            if (data.N < k + 2)
            {
                throw new ArgumentException($"At least {k + 2} rows are needed for {k} coefficients.", nameof(inputs));
            }

            var ordinary = LinearFit.Fit(data.X, data.Y, null, data.Names, true);
            var threshold = inlierThreshold ?? AutomaticThreshold(ordinary.Residuals);

            int[]? inliers;
            int trials;
            var previousSeed = Generator.ThreadSeed;

            try
            {
                Generator.ThreadSeed = seed;

                var ransac = new RANSAC<double[]>(k, threshold, successProbability)
                {
                    MaxEvaluations = maximumTrials,
                    MaxSamplings = 200,
                    Fitting = sample => FitSubset(data, sample),
                    Degenerate = sample => IsDegenerate(data, sample),
                    Distances = (model, t) => Enumerable.Range(0, data.N)
                        .Where(i => Math.Abs(data.Y[i] - Evaluate(model, data.X[i])) < t).ToArray()
                };

                ransac.Compute(data.N, out inliers);
                trials = ransac.TrialsPerformed;
            }
            catch (ConvergenceException)
            {
                throw new ArgumentException(
                    "No well-conditioned random subset could be drawn. Check for constant or duplicated predictor columns.",
                    nameof(inputs));
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }

            if (inliers == null || inliers.Length <= k)
            {
                throw new ArgumentException(
                    $"Only {inliers?.Length ?? 0} row(s) were within the threshold {threshold:0.####}, too few to fit {k} coefficients. Raise inlierThreshold or leave it empty.",
                    nameof(inlierThreshold));
            }

            var order = inliers.OrderBy(i => i).ToArray();

            var inlierFit = LinearFit.Fit(order.Select(i => data.X[i]).ToArray(),
                order.Select(i => data.Y[i]).ToArray(),
                null, data.Names, true);

            var outliers = Enumerable.Range(0, data.N).Except(order).ToArray();

            return new RansacRegressionResult
            {
                InlierModel = LinearReporting.Summarise(inlierFit, data.Names, order.Select(i => data.X[i]).ToArray(),
                    confidenceLevel, false, data.Dropped),
                OrdinaryModel = LinearReporting.Summarise(ordinary, data.Names, data.X, confidenceLevel, false,
                    data.Dropped),
                InlierRows = order.Select(i => data.KeptRows[i]).ToArray(),
                OutlierRows = outliers.Select(i => data.KeptRows[i]).ToArray(),
                InlierFraction = order.Length / (double)data.N,
                Threshold = threshold,
                TrialsPerformed = trials,
                Seed = seed
            };
        }

        [AdvancedAnalyticsTool("FitMultivariateRegressionByPartialLeastSquares")]
        [Description(
            "Fits a partial least squares (PLS) regression of one numeric outcome on many, possibly collinear, predictors by extracting a few latent components that maximise covariance with the outcome (SIMPLS algorithm on standardised predictors). " +
            "Use it when predictors outnumber or nearly equal the rows or are highly correlated, where ordinary least squares breaks down. It returns the coefficients on the original scale and the intercept for the chosen number of components, R squared, residual standard error, the share of predictor and response variance each component explains (cumulative shares show how many components are worthwhile) and the Variable Importance in Projection per predictor (values above about 1 mark influential predictors). " +
            "With as many components as predictors the result equals ordinary least squares. Choose fewer components to regularise.")]
        public PartialLeastSquaresResult FitMultivariateRegressionByPartialLeastSquares(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Number of latent components to keep, from 1 up to the smaller of the predictor count and rows minus 1. Default 2.")]
            int components = 2,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 4);
            var maximum = Math.Min(data.N - 1, data.P);

            if (components < 1 || components > maximum)
            {
                throw new ArgumentException(
                    $"components must be between 1 and {maximum} (the smaller of the predictor count and rows minus 1).",
                    nameof(components));
            }

            for (var j = 0; j < data.P; j++)
            {
                if (!(data.X.Select(r => r[j]).ToArray().StandardDeviation() > 0))
                {
                    throw new ArgumentException(
                        $"Predictor '{data.Names[j]}' has no variation and cannot be standardised. Remove it.",
                        nameof(inputs));
                }
            }

            var analysis = new PartialLeastSquaresAnalysis(AnalysisMethod.Standardize,
                PartialLeastSquaresAlgorithm.SIMPLS) { NumberOfLatentFactors = components };

            var regression = analysis.Learn(data.X, data.Y.Select(v => new[] { v }).ToArray());
            var coefficients = regression.Weights.Select(r => r[0]).ToArray();
            var intercept = regression.Intercepts[0];
            var fitted = data.X.Select(r => intercept + r.Zip(coefficients, (v, c) => v * c).Sum()).ToArray();
            var rss = data.Y.Zip(fitted, (y, f) => (y - f) * (y - f)).Sum();
            var mean = data.Y.Average();
            var tss = data.Y.Sum(v => (v - mean) * (v - mean));
            var summaries = new List<PlsComponentSummary>();

            for (var c = 0; c < components; c++)
            {
                summaries.Add(new PlsComponentSummary
                {
                    Component = c + 1,
                    PredictorVarianceExplained = analysis.Factors[c].PredictorProportion,
                    CumulativePredictorVarianceExplained = analysis.Factors[c].PredictorCumulativeProportion,
                    ResponseVarianceExplained = analysis.Factors[c].DependentProportion,
                    CumulativeResponseVarianceExplained = analysis.Factors[c].DependentCumulativeProportion
                });
            }

            var importance = Enumerable.Range(0, data.P)
                .Select(j => analysis.Factors[components - 1].VariableImportance[j])
                .ToArray();

            var dof = data.N - components - 1d;

            return new PartialLeastSquaresResult
            {
                Variables = data.Names,
                Components = components,
                Coefficients = coefficients,
                Intercept = intercept,
                RSquared = tss > 0 ? 1 - rss / tss : double.NaN,
                ResidualStandardError = dof > 0 ? Math.Sqrt(rss / dof) : double.NaN,
                ComponentSummaries = summaries.ToArray(),
                VariableImportance = importance,
                Observations = data.N,
                RowsDropped = data.Dropped
            };
        }

        [AdvancedAnalyticsTool("CrossValidateLinearRegression")]
        [Description(
            "Estimates how well a linear regression will predict rows it has not seen, by k-fold cross-validation with a seeded shuffle, and also gives the exact leave-one-out figures from the hat matrix. " +
            "It returns per-fold RMSE, MAE and R squared, the pooled cross-validated RMSE, MAE and R squared (compare with the in-sample RMSE and R squared to see how much the model overfits: a big gap means overfitting), the leave-one-out RMSE, the PRESS statistic (sum of squared leave-one-out prediction errors) and the predicted R squared = 1 - PRESS / total sum of squares. " +
            "Every fold refits the model on the training rows only. Folds must be between 2 and the number of rows, and each training set must have more rows than coefficients.")]
        public LinearCrossValidationResult CrossValidateLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description("Number of folds, from 2 to the number of rows. Default 5.")]
            int folds = 5,
            [Description("Random seed for the shuffle that assigns rows to folds. Default 1.")]
            int seed = 1,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 4);
            var k = data.P + (includeIntercept ? 1 : 0);

            if (folds < 2 || folds > data.N)
            {
                throw new ArgumentException($"folds must be between 2 and the number of rows ({data.N}).",
                    nameof(folds));
            }

            var full = LinearFit.Fit(data.X, data.Y, null, data.Names, includeIntercept);
            var random = new Random(seed);
            var order = Enumerable.Range(0, data.N).OrderBy(_ => random.Next()).ToArray();
            var predictions = new double[data.N];
            var rows = new List<CrossValidationFold>();

            for (var f = 0; f < folds; f++)
            {
                var test = order.Where((_, position) => position % folds == f).ToArray();
                var train = order.Where((_, position) => position % folds != f).ToArray();

                if (train.Length <= k)
                {
                    throw new ArgumentException(
                        $"Fold {f + 1} leaves {train.Length} training row(s) for {k} coefficients. Use fewer folds or more rows.",
                        nameof(folds));
                }

                var fit = LinearFit.Fit(train.Select(i => data.X[i]).ToArray(), train.Select(i => data.Y[i]).ToArray(),
                    null, data.Names, includeIntercept);

                var sse = 0d;
                var sae = 0d;
                var testMean = test.Average(i => data.Y[i]);
                var sst = 0d;

                foreach (var i in test)
                {
                    predictions[i] = fit.Predict(data.X[i]);
                    var e = data.Y[i] - predictions[i];
                    sse += e * e;
                    sae += Math.Abs(e);
                    sst += (data.Y[i] - testMean) * (data.Y[i] - testMean);
                }

                rows.Add(new CrossValidationFold
                {
                    Fold = f + 1,
                    TrainRows = train.Length,
                    TestRows = test.Length,
                    Rmse = Math.Sqrt(sse / test.Length),
                    Mae = sae / test.Length,
                    RSquared = sst > 0 ? 1 - sse / sst : double.NaN
                });
            }

            var pooledSse = data.Y.Zip(predictions, (y, p) => (y - p) * (y - p)).Sum();
            var pooledSae = data.Y.Zip(predictions, (y, p) => Math.Abs(y - p)).Sum();
            var mean = data.Y.Average();
            var pooledSst = data.Y.Sum(y => (y - mean) * (y - mean));
            var press = 0d;

            for (var i = 0; i < data.N; i++)
            {
                var e = full.Residuals[i] / Math.Max(1e-12, 1 - full.Leverage[i]);
                press += e * e;
            }

            return new LinearCrossValidationResult
            {
                Folds = rows.ToArray(),
                CrossValidatedRmse = Math.Sqrt(pooledSse / data.N),
                CrossValidatedMae = pooledSae / data.N,
                CrossValidatedRSquared = pooledSst > 0 ? 1 - pooledSse / pooledSst : double.NaN,
                InSampleRmse = Math.Sqrt(full.Rss / data.N),
                InSampleRSquared = full.RSquared,
                LeaveOneOutRmse = Math.Sqrt(press / data.N),
                PressStatistic = press,
                PredictedRSquared = pooledSst > 0 ? 1 - press / pooledSst : double.NaN,
                Observations = data.N,
                Seed = seed
            };
        }

        private static void RequireLambda(double lambda, string name)
        {
            if (double.IsNaN(lambda) || double.IsInfinity(lambda) || lambda < 0)
            {
                throw new ArgumentException(
                    $"{name} must be a finite number of zero or more. Use 0 for ordinary least squares.", name);
            }
        }

        private static void RequireRidgeSize(PreparedRegressionData data)
        {
            if (data.P > 500)
            {
                throw new ArgumentException("At most 500 predictor columns are accepted for ridge regression.",
                    "inputs");
            }

            if (data.N < 3)
            {
                throw new ArgumentException("At least 3 rows are needed.", "inputs");
            }
        }

        private static double AutomaticThreshold(double[] residuals)
        {
            var abs = residuals.Select(Math.Abs).OrderBy(v => v).ToArray();

            var median = abs.Length % 2 == 1
                ? abs[abs.Length / 2]
                : (abs[abs.Length / 2 - 1] + abs[abs.Length / 2]) / 2;

            var scale = 1.4826 * median;

            if (!(scale > 0))
            {
                scale = residuals.Select(Math.Abs).DefaultIfEmpty(0).Max() * 1e-6;
            }

            return Math.Max(2.5 * scale, 1e-12);
        }

        private static double[] FitSubset(PreparedRegressionData data, int[] sample)
        {
            var fit = new OrdinaryLeastSquares { UseIntercept = true }.Learn(
                sample.Select(i => (double[])data.X[i].Clone()).ToArray(), sample.Select(i => data.Y[i]).ToArray());

            return new[] { fit.Intercept }.Concat(fit.Weights).ToArray();
        }

        private static bool IsDegenerate(PreparedRegressionData data, int[] sample)
        {
            var terms = new[] { "(Intercept)" }.Concat(data.Names).ToArray();

            try
            {
                LinearFit.EnsureFullRank(sample.Select(i => new[] { 1d }.Concat(data.X[i]).ToArray()).ToArray(), terms);

                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }

        private static double Evaluate(double[] model, double[] x)
        {
            var sum = model[0];

            for (var j = 0; j < x.Length; j++)
            {
                sum += model[j + 1] * x[j];
            }

            return sum;
        }
    }
}