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

using System.Globalization;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class RegressionEvaluationEngine
    {
        public static IRegressionModel Fit(RegressorConfig c, double[][] x, double[] y, RegressionFitContext ctx)
        {
            switch (c.Family)
            {
                case RegressionFamily.LinearRegression:
                    return LinearFitter.Fit(c, x, y, ctx);
                case RegressionFamily.DecisionTree:
                case RegressionFamily.RandomForest:
                case RegressionFamily.GradientBoosting:
                    return TreeRegressionModel.Fit(c, x, y, ctx);
                case RegressionFamily.KNearestNeighbours:
                    return NearestNeighbourRegressionModel.Fit(c, x, y);
                case RegressionFamily.KernelRidge:
                    return KernelRidgeRegressionModel.Fit(c, x, y);
                default:
                    return NeuralRegressionModel.Fit(c, x, y, ctx);
            }
        }

        public static RegressionRun RunOne(RegressorConfig[] candidates, string[] candidateLabels, double[][] x,
            double[] y,
            string[] names, DataSplitIndices split, EvaluationOptions options, int seed, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var trainX = DataSplit.Take(x, split.Train);
            var trainY = DataSplit.Take(y, split.Train);
            var validationX = DataSplit.Take(x, split.Validation);
            var validationY = DataSplit.Take(y, split.Validation);
            var testX = DataSplit.Take(x, split.Test);
            var testY = DataSplit.Take(y, split.Test);
            var warnings = new List<EvaluationWarning>();

            if (trainY.Length <= names.Length + 1 && candidates[0].Family == RegressionFamily.LinearRegression)
            {
                throw new ArgumentException(
                    $"The training split has {trainY.Length} rows for {names.Length} variables; linear regression needs more rows than variables.");
            }

            if (trainY.Distinct().Count() < 2)
            {
                throw new ArgumentException(
                    "The target is constant in the training split, so there is nothing to predict.");
            }

            var first = candidates[0];

            var transform = FeatureTransform.Fit(trainX, names,
                first.NeedsScaling || first.Family == RegressionFamily.LinearRegression);

            var trainT = transform.Apply(trainX);
            var validationT = transform.Apply(validationX);
            var testT = transform.Apply(testX);

            var context = new RegressionFitContext(names, seed, validationT.Length > 0 ? validationT : null,
                validationT.Length > 0 ? validationY : null, transform, token);

            var models = new IRegressionModel[candidates.Length];
            var sweep = new List<RegressionSweepRow>();
            var scores = new double[candidates.Length];
            var hasValidation = validationY.Length >= 5;

            for (var k = 0; k < candidates.Length; k++)
            {
                models[k] = Fit(candidates[k], trainT, trainY, context);

                if (candidates.Length > 1)
                {
                    var trainRmse = RegressionMetrics.RootMeanSquaredError(trainY, models[k].Predict(trainT));

                    var selection = hasValidation
                        ? RegressionMetrics.RootMeanSquaredError(validationY, models[k].Predict(validationT))
                        : trainRmse;

                    scores[k] = double.IsFinite(selection) ? selection : double.PositiveInfinity;

                    sweep.Add(new RegressionSweepRow(candidateLabels[k], ModelMath.Finite(trainRmse),
                        hasValidation ? ModelMath.Finite(selection) : null, false));
                }
            }

            var chosenIndex = 0;

            if (candidates.Length > 1)
            {
                chosenIndex = Enumerable.Range(0, candidates.Length).OrderBy(k => scores[k]).ThenBy(k => k).First();
                sweep[chosenIndex] = sweep[chosenIndex] with { Selected = true };

                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SelectionOnValidation,
                    $"The setting '{candidateLabels[chosenIndex]}' was chosen by validation RMSE; the test split was not used for that choice, so the reported test metrics remain honest."));

                if (!hasValidation)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoValidation,
                        "The validation split is too small, so the setting was chosen on training error and favours flexible models."));
                }
            }

            var chosen = models[chosenIndex];
            var config = candidates[chosenIndex];
            var trainPredictions = chosen.Predict(trainT);
            var validationPredictions = chosen.Predict(validationT);
            var testPredictions = chosen.Predict(testT);
            var baselineValue = trainY.Average();

            var trainMetrics = RegressionMetricsBuilder.Build(trainY, trainPredictions);
            var validationMetrics = RegressionMetricsBuilder.Build(validationY, validationPredictions);
            var testMetrics = RegressionMetricsBuilder.Build(testY, testPredictions);
            var baseline = RegressionMetricsBuilder.Build(testY, testY.Select(_ => baselineValue).ToArray());

            if (transform.ConstantColumns.Length > 0)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.ConstantColumns,
                    $"{transform.ConstantColumns.Length} variable(s) are constant in the training split and carry no information."));
            }

            if (transform.MissingCounts.Any(m => m > 0))
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.MissingValues,
                    "Missing values were replaced with the training median of each variable."));
            }

            AddWarnings(warnings, trainMetrics, testMetrics, testY, names.Length, trainY.Length, config.Family);

            var description = chosen.Describe(names);

            return new RegressionRun
            {
                Config = config,
                SelectedSetting = candidates.Length > 1 ? candidateLabels[chosenIndex] : null,
                Predict = rows => chosen.Predict(transform.Apply(rows)),
                TrainRows = split.Train,
                ValidationRows = split.Validation,
                TestRows = split.Test,
                TrainPredictions = trainPredictions,
                ValidationPredictions = validationPredictions,
                TestPredictions = testPredictions,
                TrainTargets = trainY,
                ValidationTargets = validationY,
                TestTargets = testY,
                TrainMetrics = trainMetrics,
                ValidationMetrics = validationMetrics,
                TestMetrics = testMetrics,
                BaselineMetrics = baseline,
                Description = description,
                Coefficients = chosen.Coefficients(names),
                Warnings = warnings,
                Sweep = sweep.ToArray()
            };
        }

        public static IReadOnlyList<DataSplitIndices> Plan(double[] y, EvaluationOptions options)
        {
            var count = y.Length;
            var placeholder = new double[count];

            if (options.Folds < 2)
            {
                var split = DataSplit.Split(placeholder, 1d - options.ValidationFraction - options.TestFraction,
                    options.ValidationFraction, options.TestFraction, options.Seed, false);

                return [AddLeftovers(split, count)];
            }

            var folds = KFoldSplit.Split(count, options.Folds, options.Seed);
            var result = new List<DataSplitIndices>();

            for (var f = 0; f < folds.Length; f++)
            {
                var part = folds[f].Train;

                var inner = DataSplit.Split(new double[part.Length], 1d - options.ValidationFraction,
                    options.ValidationFraction, 0d, SeededLibrary.Derive(options.Seed, f + 1), false);

                var assigned = new bool[part.Length];

                foreach (var i in inner.Train.Concat(inner.Validation))
                {
                    assigned[i] = true;
                }

                var leftovers = Enumerable.Range(0, part.Length).Where(i => !assigned[i]);

                result.Add(new DataSplitIndices(inner.Train.Concat(leftovers).Select(i => part[i]).ToArray(),
                    inner.Validation.Select(i => part[i]).ToArray(), folds[f].Test));
            }

            return result;
        }

        public static RegressionEvaluationResult Evaluate(RegressorConfig[] candidates, string[] labels, double[][] x,
            double[] y, string[] names, EvaluationOptions options, CancellationToken token)
        {
            var plan = Plan(y, options);
            var runs = new List<RegressionRun>();

            for (var f = 0; f < plan.Count; f++)
            {
                var seed = options.Folds >= 2 ? SeededLibrary.Derive(options.Seed, 31 * (f + 1)) : options.Seed;
                runs.Add(RunOne(candidates, labels, x, y, names, plan[f], options, seed, token));
            }

            return Assemble(runs, candidates[0], x, y, names, options, token);
        }

        public static RegressionEvaluationResult Assemble(IReadOnlyList<RegressionRun> runs, RegressorConfig config,
            double[][] x, double[] y, string[] names, EvaluationOptions options, CancellationToken token)
        {
            var cv = options.Folds >= 2;
            var first = runs[0];
            var warnings = new List<EvaluationWarning>();

            foreach (var warning in runs.SelectMany(r => r.Warnings))
            {
                if (!warnings.Any(w => w.Code == warning.Code))
                {
                    warnings.Add(warning);
                }
            }

            RegressionMetricsResult? test;
            RegressionMetricsResult? baseline;

            if (cv)
            {
                test = RegressionMetricsBuilder.Build(runs.SelectMany(r => r.TestTargets).ToArray(),
                    runs.SelectMany(r => r.TestPredictions).ToArray());

                var means = runs.SelectMany(r => r.TestTargets.Select(_ => r.TrainTargets.Average())).ToArray();
                baseline = RegressionMetricsBuilder.Build(runs.SelectMany(r => r.TestTargets).ToArray(), means);
            }
            else
            {
                test = first.TestMetrics;
                baseline = first.BaselineMetrics;
            }

            var folds = runs.Select((r, i) => new RegressionFoldResult(i + 1, r.TrainRows.Length, r.TestRows.Length,
                r.SelectedSetting, r.TrainMetrics?.RSquared, r.TestMetrics)).ToArray();

            var summary = new List<EvaluationMetricSummary>();

            if (cv)
            {
                summary.Add(ModelMath.Summarise("RSquared",
                    runs.Select(r => r.TestMetrics?.RSquared ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("MeanAbsoluteError",
                    runs.Select(r => r.TestMetrics?.MeanAbsoluteError ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("RootMeanSquaredError",
                    runs.Select(r => r.TestMetrics?.RootMeanSquaredError ?? double.NaN).ToList()));
            }

            var importance = options.ImportanceRepeats > 0 ? Importance(runs, x, y, names, options, token) : [];
            var sweep = first.Sweep;

            if (cv && runs.Any(r => r.Sweep.Length > 0))
            {
                sweep = runs[0].Sweep.Select((s, k) => s with
                {
                    TrainScore = ModelMath.Finite(ModelMath.Mean(runs.Select(r => r.Sweep[k].TrainScore ?? double.NaN)
                        .Where(double.IsFinite).ToList())),
                    ValidationScore = ModelMath.Finite(ModelMath.Mean(runs
                        .Select(r => r.Sweep[k].ValidationScore ?? double.NaN)
                        .Where(double.IsFinite).ToList())),
                    Selected = runs.Count(r => r.Sweep[k].Selected) > runs.Count / 2d
                }).ToArray();
            }

            var result = new RegressionEvaluationResult
            {
                Family = config.Family.ToString(),
                Scheme = options.Folds >= 2
                    ? $"CrossValidation({options.Folds} folds, validation {options.ValidationFraction:0.##} of each training part)"
                    : $"TrainValidationTest({1d - options.ValidationFraction - options.TestFraction:0.##}/{options.ValidationFraction:0.##}/{options.TestFraction:0.##})",
                Seed = options.Seed,
                Settings = config.Settings(),
                Rows = x.Length,
                Features = names.Length,
                TrainRows = cv ? (int)Math.Round(runs.Average(r => r.TrainRows.Length)) : first.TrainRows.Length,
                ValidationRows =
                    cv ? (int)Math.Round(runs.Average(r => r.ValidationRows.Length)) : first.ValidationRows.Length,
                TestRows = cv ? runs.Sum(r => r.TestRows.Length) : first.TestRows.Length,
                Test = test,
                Validation = cv ? null : first.ValidationMetrics,
                Train = cv ? null : first.TrainMetrics,
                BaselineTest = baseline,
                Folds = folds,
                FoldSummary = summary.ToArray(),
                Coefficients = first.Coefficients,
                Sweep = sweep,
                Importance = importance,
                Model = first.Description,
                Warnings = warnings.ToArray(),
                Interpretation = string.Empty
            };

            var text = RegressionInterpretation(result);

            return new RegressionEvaluationResult
            {
                Family = result.Family,
                Scheme = result.Scheme,
                Seed = result.Seed,
                Settings = result.Settings,
                Rows = result.Rows,
                Features = result.Features,
                TrainRows = result.TrainRows,
                ValidationRows = result.ValidationRows,
                TestRows = result.TestRows,
                Test = result.Test,
                Validation = result.Validation,
                Train = result.Train,
                BaselineTest = result.BaselineTest,
                Folds = result.Folds,
                FoldSummary = result.FoldSummary,
                Coefficients = result.Coefficients,
                Sweep = result.Sweep,
                Importance = result.Importance,
                Model = result.Model,
                Warnings = result.Warnings,
                Interpretation = text
            };
        }

        private static void AddWarnings(List<EvaluationWarning> warnings, RegressionMetricsResult? train,
            RegressionMetricsResult? test, double[] testTargets, int variables,
            int trainRows, RegressionFamily family)
        {
            if (test == null)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics."));

                return;
            }

            if (test.Rows < ModelEvaluationLimits.MinimumTestRows)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.TinyTestSet,
                    $"The test split has only {test.Rows} rows; held-out metrics will be noisy."));
            }

            if (trainRows < 10 * variables && family != RegressionFamily.LinearRegression)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SmallSample,
                    $"Only {trainRows} training rows for {variables} variables; {family} is likely to overfit."));
            }

            if (train?.RSquared is { } trainR2 && test.RSquared is { } testR2 && trainR2 - testR2 > 0.2)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.Overfit,
                    $"Training R-squared {trainR2:0.###} is far above test R-squared {testR2:0.###}: the model is overfitting."));
            }

            if (test.RSquared is < 0.02)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoSignal,
                    test.RSquared < 0
                        ? "The model does worse than always predicting the training mean on the test split; the predictors carry no usable signal for this model."
                        : "The test R-squared is close to zero: the predictors explain almost none of the variation in the target."));
            }

            if (test.RSquared is >= 0.9999)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SuspiciouslyPerfect,
                    "The test R-squared is almost 1; check whether a predictor is a copy or a transform of the target."));
            }

            if (testTargets.Any(t => t == 0))
            {
                warnings.Add(new EvaluationWarning("PercentageErrorSkipsZeros",
                    "Some test targets are zero, so MAPE ignores those rows and can be misleading; prefer MAE or RMSE."));
            }

            if (test.ResidualSkewness is { } skew && Math.Abs(skew) > 1.5)
            {
                warnings.Add(new EvaluationWarning("SkewedResiduals",
                    $"Test residuals are strongly skewed (skewness {skew:0.##}); consider modelling a transformed target such as its logarithm."));
            }
        }

        private static DataSplitIndices AddLeftovers(DataSplitIndices split, int count)
        {
            var assigned = new bool[count];

            foreach (var i in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[i] = true;
            }

            var leftovers = Enumerable.Range(0, count).Where(i => !assigned[i]).ToArray();

            return leftovers.Length == 0
                ? split
                : new DataSplitIndices(split.Train.Concat(leftovers).ToArray(), split.Validation, split.Test);
        }

        private static EvaluationImportanceRow[] Importance(IReadOnlyList<RegressionRun> runs, double[][] x, double[] y,
            string[] names, EvaluationOptions options, CancellationToken token)
        {
            var drops = names.Select(_ => new List<double>()).ToArray();
            var any = false;

            for (var f = 0; f < runs.Count; f++)
            {
                var run = runs[f];

                var take = PermutationImportance.RowsFor(run.TestRows.Length, names.Length, options.ImportanceRepeats,
                    runs.Count);

                var chosen = run.TestRows.Take(take).ToArray();
                var rows = DataSplit.Take(x, chosen);
                var targets = DataSplit.Take(y, chosen);

                if (targets.Distinct().Count() < 2)
                {
                    continue;
                }

                var baseline = PermutationImportance.Permute(run.Predict, rows,
                    predictions => RegressionMetrics.RSquared(targets, predictions), true, options.ImportanceRepeats,
                    SeededLibrary.Derive(options.Seed, 5000 + f), drops, token);

                any |= double.IsFinite(baseline);
            }

            return any ? PermutationImportance.Rows(names, drops) : [];
        }

        private static string F(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
        }

        private static string RegressionInterpretation(RegressionEvaluationResult r)
        {
            if (r.Test == null)
            {
                return "No held-out metrics were produced.";
            }

            var parts = new List<string>
            {
                $"{r.Family} on held-out rows: R-squared {F(r.Test.RSquared)}, mean absolute error {F(r.Test.MeanAbsoluteError)}, root mean squared error {F(r.Test.RootMeanSquaredError)}, MAPE {F(r.Test.MeanAbsolutePercentageError)}."
            };

            if (r.BaselineTest != null)
            {
                parts.Add(
                    $"Always predicting the training mean would give a mean absolute error of {F(r.BaselineTest.MeanAbsoluteError)}, so the model reduces the error by {ImprovementText(r.BaselineTest.MeanAbsoluteError, r.Test.MeanAbsoluteError)}.");
            }

            if (r.Train?.RSquared is { } trainR2 && r.Test.RSquared is { } testR2)
            {
                parts.Add(trainR2 - testR2 > 0.2
                    ? $"Training R-squared {F(trainR2)} is much higher than test {F(testR2)}, a sign of overfitting."
                    : $"Training R-squared {F(trainR2)} is close to test {F(testR2)}.");
            }

            if (r.Importance.Length > 0)
            {
                parts.Add("Most influential variables by permutation importance: " +
                          string.Join(", ", r.Importance.Take(3).Select(i => $"{i.Variable} ({F(i.Importance)})")) +
                          ".");
            }

            if (r.Warnings.Length > 0)
            {
                parts.Add($"Read the {r.Warnings.Length} warning(s) before trusting this result.");
            }

            return string.Join(" ", parts);
        }

        private static string ImprovementText(double? baseline, double? model)
        {
            if (!baseline.HasValue || !model.HasValue || baseline.Value <= 0)
            {
                return "an amount that cannot be computed";
            }

            return ((1d - model.Value / baseline.Value) * 100d).ToString("0.#", CultureInfo.InvariantCulture) +
                   " percent";
        }
    }
}