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

using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed record EvaluationOptions(
        double ValidationFraction,
        double TestFraction,
        int Folds,
        int Seed,
        bool Calibrate,
        double? DecisionThreshold,
        int ImportanceRepeats);

    internal static class BinaryEvaluationEngine
    {
        public static BinaryRun RunOne(LearnerConfig[] candidates, string[] candidateLabels, double[][] x, int[] y,
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
            var positives = trainY.Count(v => v == 1);

            if (positives == 0 || positives == trainY.Length)
            {
                throw new ArgumentException("The training split must contain both classes.");
            }

            if (candidates[0].Family == EvaluationFamily.LogisticRegression)
            {
                return RunLogistic(candidates[0], x, y, names, split, options, seed, token);
            }

            var first = candidates[0];
            var transform = FeatureTransform.Fit(trainX, names, first.NeedsScaling);
            var trainT = transform.Apply(trainX);
            var validationT = transform.Apply(validationX);
            var testT = transform.Apply(testX);
            var validationBoth = validationY.Any(v => v == 1) && validationY.Any(v => v == 0);

            var context = new FitContext(names, seed, validationT.Length > 0 ? validationT : null,
                validationT.Length > 0 ? validationY : null, token);

            var chosenIndex = 0;
            var sweep = new List<SweepRow>();
            var models = new IBinaryModel[candidates.Length];
            var scores = new double[candidates.Length];

            for (var k = 0; k < candidates.Length; k++)
            {
                var candidate = candidates[k];
                var prepared = ImbalanceHandler.Apply(candidate, trainT, trainY, seed);

                if (k == 0 && prepared.Note != null)
                {
                    warnings.Add(new EvaluationWarning(
                        candidate.Imbalance == ClassImbalanceHandling.BalancedWeights && !candidate.SupportsWeights
                            ? EvaluationWarningCodes.WeightsUnsupported
                            : EvaluationWarningCodes.Resampled, prepared.Note));
                }

                models[k] = BinaryLearners.Fit(candidate, prepared.X, prepared.Y, prepared.W, context);

                if (candidates.Length > 1)
                {
                    var rawTrain = models[k].Score(trainT);
                    var rawValidation = validationBoth ? models[k].Score(validationT) : rawTrain;
                    var against = validationBoth ? validationY : trainY;
                    var trainAuc = BinaryMetrics.RocAuc(rawTrain, trainY);
                    var selectionAuc = BinaryMetrics.RocAuc(rawValidation, against);
                    scores[k] = double.IsNaN(selectionAuc) ? double.NegativeInfinity : selectionAuc;

                    sweep.Add(new SweepRow(candidateLabels[k], ModelMath.Finite(trainAuc),
                        validationBoth ? ModelMath.Finite(selectionAuc) : null, false));
                }
            }

            if (candidates.Length > 1)
            {
                chosenIndex = Enumerable.Range(0, candidates.Length).OrderByDescending(k => scores[k]).ThenBy(k => k)
                    .First();

                sweep[chosenIndex] = sweep[chosenIndex] with { Selected = true };

                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SelectionOnValidation,
                    $"The setting '{candidateLabels[chosenIndex]}' was chosen by ROC AUC on the validation split; the test split was not used for that choice, so the reported test metrics remain honest."));

                if (!validationBoth)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoValidation,
                        "The validation split does not contain both classes, so the setting was chosen on training scores and is likely to favour flexible models."));
                }
            }

            var chosen = models[chosenIndex];
            var config = candidates[chosenIndex];
            var rawTrainFinal = chosen.Score(trainT);
            var rawValidationFinal = chosen.Score(validationT);
            var rawTestFinal = chosen.Score(testT);

            Calibrator? calibrator = null;
            string? reason = null;
            var mode = chosen.OutputsProbability ? CalibrationInput.LogitOfBoundedScore : CalibrationInput.RawScore;

            if (!options.Calibrate)
            {
                reason = "Calibration was switched off.";
            }
            else if (!validationBoth)
            {
                reason = "The validation split does not contain both classes.";

                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so probabilities are uncalibrated."));
            }
            else
            {
                try
                {
                    calibrator = PlattCalibration.Fit(rawValidationFinal, validationY, null, mode,
                        cancellationToken: token);
                }
                catch (InvalidOperationException exception)
                {
                    reason = exception.Message;

                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            if (calibrator is { IsIncreasing: false })
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.CalibratorNotIncreasing,
                    "The calibrator has a non-positive slope: on the validation split the model ranks cases the wrong way round."));
            }

            var outputsProbability = chosen.OutputsProbability;
            var applied = calibrator;

            double[] ToProbability(double[] raw)
            {
                if (applied.HasValue)
                {
                    return applied.Value.Apply(raw).Select(v => Math.Min(1d, Math.Max(0d, v))).ToArray();
                }

                return outputsProbability
                    ? raw.Select(v => Math.Min(1d, Math.Max(0d, v))).ToArray()
                    : raw.Select(ModelMath.Sigmoid).ToArray();
            }

            if (calibrator == null && config.Imbalance != ClassImbalanceHandling.None)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.UncalibratedAfterResampling,
                    "Class imbalance handling shifts the predicted probabilities away from the real event rate and no calibration was fitted, so read the probabilities as scores, not as risks."));
            }

            var trainProbability = ToProbability(rawTrainFinal);
            var validationProbability = ToProbability(rawValidationFinal);
            var testProbability = ToProbability(rawTestFinal);

            double threshold;
            string thresholdSource;

            if (options.DecisionThreshold.HasValue)
            {
                threshold = options.DecisionThreshold.Value;
                thresholdSource = "Explicit";
            }
            else if (validationBoth)
            {
                threshold = ThresholdSelection.MaximiseF1(validationProbability, validationY);
                thresholdSource = "MaximumF1OnValidation";
            }
            else
            {
                threshold = 0.5;
                thresholdSource = "Default";
            }

            var trainMetrics = ClassificationMetricsBuilder.Build(trainProbability, trainY, threshold);
            var validationMetrics = ClassificationMetricsBuilder.Build(validationProbability, validationY, threshold);
            var testMetrics = ClassificationMetricsBuilder.Build(testProbability, testY, threshold);

            AddDataWarnings(warnings, config, transform, trainT.Length, positives, names.Length);
            AddResultWarnings(warnings, config, trainMetrics, testMetrics, positives, trainY.Length - positives);

            var description = chosen.Describe(names);

            var mapped = new BinaryRun
            {
                Config = config,
                SelectedSetting = candidates.Length > 1 ? candidateLabels[chosenIndex] : null,
                PredictRaw = rows => chosen.Score(transform.Apply(rows)),
                Predict = rows => ToProbability(chosen.Score(transform.Apply(rows))),
                RawIsProbability = outputsProbability,
                Calibrator = calibrator,
                TrainRows = split.Train,
                ValidationRows = split.Validation,
                TestRows = split.Test,
                TrainProbability = trainProbability,
                ValidationProbability = validationProbability,
                TestProbability = testProbability,
                ValidationRaw = rawValidationFinal,
                TestRaw = rawTestFinal,
                TrainLabels = trainY,
                ValidationLabels = validationY,
                TestLabels = testY,
                Threshold = threshold,
                ThresholdSource = thresholdSource,
                Calibration = new CalibrationSummary(calibrator.HasValue, calibrator.HasValue ? "Platt" : "None",
                    calibrator?.Slope, calibrator?.Intercept, calibrator.HasValue ? validationY.Length : 0, reason),
                TrainMetrics = trainMetrics,
                ValidationMetrics = validationMetrics,
                TestMetrics = testMetrics,
                Description = description,
                Warnings = warnings,
                Sweep = sweep.ToArray()
            };

            return mapped;
        }

        public static void AddResultWarnings(List<EvaluationWarning> warnings, LearnerConfig config,
            ClassificationMetrics? train, ClassificationMetrics? test, int trainPositives, int trainNegatives)
        {
            if (test == null)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics."));

                return;
            }

            var minority = Math.Min(test.Positives, test.Rows - test.Positives);

            if (test.Rows < ModelEvaluationLimits.MinimumTestRows ||
                minority < ModelEvaluationLimits.MinimumTestClassRows)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.TinyTestSet,
                    $"The test split has {test.Rows} rows with {test.Positives} positives; held-out metrics will be noisy."));
            }

            if (trainPositives < 30)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.FewPositives,
                    $"Only {trainPositives} positive rows are available for training; treat every metric as indicative."));
            }

            var ratio = trainPositives == 0 || trainNegatives == 0
                ? double.PositiveInfinity
                : Math.Max(trainPositives, trainNegatives) / (double)Math.Min(trainPositives, trainNegatives);

            if (ratio >= ModelEvaluationLimits.SevereImbalanceRatio && config.Imbalance == ClassImbalanceHandling.None)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SevereImbalance,
                    $"The classes are imbalanced {ratio:0}:1 and no imbalance handling was used; prefer average precision over accuracy and consider BalancedWeights."));
            }

            if (test.RocAuc.HasValue)
            {
                if (train?.RocAuc is { } trainAuc && trainAuc - test.RocAuc.Value > ModelEvaluationLimits.OverfitGap)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.Overfit,
                        $"Training ROC AUC {trainAuc:0.###} is far above test ROC AUC {test.RocAuc.Value:0.###}: the model is overfitting; reduce its flexibility or add data."));
                }

                if (test.RocAuc.Value >= ModelEvaluationLimits.SuspiciousAuc)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SuspiciouslyPerfect,
                        "The test ROC AUC is almost perfect; check for a predictor that leaks the outcome (a field only known after the event) before trusting it."));
                }
                else if (test.RocAuc.Value < 0.45)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.AntiPredictive,
                        "The test ROC AUC is below 0.5: the model ranks cases the wrong way round on unseen data."));
                }
                else if (test.RocAuc.Value < 0.55)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoSignal,
                        "The test ROC AUC is close to 0.5: the predictors show no detectable signal for this outcome with this model."));
                }
            }
        }

        public static DataSplitIndices Split(int[] labels, EvaluationOptions options, int seed)
        {
            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;

            var split = DataSplit.Split(labels.Select(l => (double)l).ToArray(), trainFraction,
                options.ValidationFraction,
                options.TestFraction, seed);

            var assigned = new bool[labels.Length];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[row] = true;
            }

            var leftovers = Enumerable.Range(0, labels.Length).Where(i => !assigned[i]).ToArray();

            return leftovers.Length == 0
                ? split
                : new DataSplitIndices(split.Train.Concat(leftovers).ToArray(), split.Validation, split.Test);
        }

        public static IReadOnlyList<DataSplitIndices> FoldSplits(int[] labels, EvaluationOptions options)
        {
            var doubles = labels.Select(l => (double)l).ToArray();
            var folds = KFoldSplit.Stratified(doubles, options.Folds, options.Seed);
            var result = new List<DataSplitIndices>();

            for (var f = 0; f < folds.Length; f++)
            {
                var trainPart = folds[f].Train;

                var inner = DataSplit.Split(trainPart.Select(i => doubles[i]).ToArray(),
                    1d - options.ValidationFraction, options.ValidationFraction, 0d,
                    SeededLibrary.Derive(options.Seed, f + 1));

                var assigned = new bool[trainPart.Length];

                foreach (var i in inner.Train.Concat(inner.Validation))
                {
                    assigned[i] = true;
                }

                var leftovers = Enumerable.Range(0, trainPart.Length).Where(i => !assigned[i]).ToArray();
                var train = inner.Train.Concat(leftovers).Select(i => trainPart[i]).ToArray();
                var validation = inner.Validation.Select(i => trainPart[i]).ToArray();
                result.Add(new DataSplitIndices(train, validation, folds[f].Test));
            }

            return result;
        }

        public static IReadOnlyList<DataSplitIndices> Plan(int[] labels, EvaluationOptions options)
        {
            return options.Folds >= 2
                ? FoldSplits(labels, options)
                : [Split(labels, options, options.Seed)];
        }

        public static string SchemeName(EvaluationOptions options)
        {
            return options.Folds >= 2
                ? $"StratifiedCrossValidation({options.Folds} folds, validation {options.ValidationFraction:0.##} of each training part)"
                : $"TrainValidationTest({1d - options.ValidationFraction - options.TestFraction:0.##}/{options.ValidationFraction:0.##}/{options.TestFraction:0.##})";
        }

        public static ClassifierEvaluationResult Evaluate(LearnerConfig[] candidates, string[] labels, double[][] x,
            int[] y,
            string[] names, EvaluationOptions options, CancellationToken token)
        {
            var plan = Plan(y, options);
            var runs = new List<BinaryRun>();

            for (var f = 0; f < plan.Count; f++)
            {
                var seed = options.Folds >= 2 ? SeededLibrary.Derive(options.Seed, 31 * (f + 1)) : options.Seed;
                runs.Add(RunOne(candidates, labels, x, y, names, plan[f], options, seed, token));
            }

            return Assemble(runs, candidates[0], x, y, names, options, token);
        }

        public static ClassifierEvaluationResult Assemble(IReadOnlyList<BinaryRun> runs, LearnerConfig config,
            double[][] x,
            int[] y, string[] names, EvaluationOptions options, CancellationToken token)
        {
            var cv = options.Folds >= 2;
            var first = runs[0];
            var pooled = cv ? ClassificationMetricsBuilder.Pool(runs) : first.TestMetrics;
            var warnings = new List<EvaluationWarning>();

            foreach (var warning in runs.SelectMany(r => r.Warnings))
            {
                if (!warnings.Any(w => w.Code == warning.Code))
                {
                    warnings.Add(warning);
                }
            }

            var folds = runs.Select((r, i) => new EvaluationFoldResult(i + 1, r.TrainRows.Length,
                r.ValidationRows.Length,
                r.TestRows.Length, r.SelectedSetting, r.TrainMetrics?.RocAuc, r.TestMetrics)).ToArray();

            var summary = new List<EvaluationMetricSummary>();

            if (cv)
            {
                summary.Add(ModelMath.Summarise("RocAuc",
                    runs.Select(r => r.TestMetrics?.RocAuc ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("AveragePrecision",
                    runs.Select(r => r.TestMetrics?.AveragePrecision ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("LogLoss",
                    runs.Select(r => r.TestMetrics?.LogLoss ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("BrierScore",
                    runs.Select(r => r.TestMetrics?.BrierScore ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("F1", runs.Select(r => r.TestMetrics?.F1 ?? double.NaN).ToList()));
                var aucs = summary[0];

                if (aucs.StandardDeviation is > 0.1)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.FoldsDisagree,
                        $"ROC AUC varies a lot between folds (standard deviation {aucs.StandardDeviation:0.###}); the estimate is unstable."));
                }
            }

            var importance = options.ImportanceRepeats > 0
                ? PermutationImportance.ComputeMany(runs, x, y, names, options, token)
                : [];

            var sweep = first.Sweep;

            if (cv && runs.Any(r => r.Sweep.Length > 0))
            {
                sweep = runs[0].Sweep
                    .Select((s, k) => s with
                    {
                        TrainScore = ModelMath.Finite(ModelMath.Mean(runs
                            .Select(r => r.Sweep[k].TrainScore ?? double.NaN)
                            .Where(double.IsFinite).ToList())),
                        ValidationScore = ModelMath.Finite(ModelMath.Mean(runs
                            .Select(r => r.Sweep[k].ValidationScore ?? double.NaN).Where(double.IsFinite).ToList())),
                        Selected = runs.Count(r => r.Sweep[k].Selected) > runs.Count / 2d
                    }).ToArray();
            }

            var positives = y.Count(v => v == 1);

            var result = new ClassifierEvaluationResult
            {
                Family = config.Family.ToString(),
                Scheme = SchemeName(options),
                Seed = options.Seed,
                Settings = config.Settings(),
                Rows = x.Length,
                Features = names.Length,
                Positives = positives,
                PositiveRate = positives / (double)y.Length,
                TrainRows = cv ? (int)Math.Round(runs.Average(r => r.TrainRows.Length)) : first.TrainRows.Length,
                ValidationRows =
                    cv ? (int)Math.Round(runs.Average(r => r.ValidationRows.Length)) : first.ValidationRows.Length,
                TestRows = cv ? runs.Sum(r => r.TestRows.Length) : first.TestRows.Length,
                Test = pooled,
                Validation = cv ? null : first.ValidationMetrics,
                Train = cv ? null : first.TrainMetrics,
                Folds = folds,
                FoldSummary = summary.ToArray(),
                Calibration = first.Calibration,
                Sweep = sweep,
                Importance = importance,
                Model = first.Description,
                Warnings = warnings.ToArray(),
                Interpretation = ""
            };

            return WithInterpretation(result, config);
        }

        private static BinaryRun RunLogistic(LearnerConfig config, double[][] x, int[] y, string[] names,
            DataSplitIndices split, EvaluationOptions options, int seed, CancellationToken token)
        {
            var warnings = new List<EvaluationWarning>();

            var logisticOptions = new LogisticRegressionOptions
            {
                ValidationFraction = options.ValidationFraction,
                TestFraction = options.TestFraction,
                Seed = seed,
                RidgeLambda = config.ResolvedRegularisation(0d),
                ClassWeighting = config.Imbalance == ClassImbalanceHandling.BalancedWeights
                    ? LogisticClassWeighting.Balanced
                    : LogisticClassWeighting.None,
                Calibrate = options.Calibrate,
                DecisionThreshold = options.DecisionThreshold
            };

            if (config.Imbalance is ClassImbalanceHandling.Oversample or ClassImbalanceHandling.Undersample)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.WeightsUnsupported,
                    "Logistic regression handles class imbalance through balanced weights; oversampling and undersampling were not applied."));
            }

            var trained = LogisticRegressionTrainer.TrainWithSplit(x, y, names, logisticOptions, split, token);
            var recall = LogisticRegressionRecall.FromSnapshot(trained.SnapshotJson);
            var trainY = DataSplit.Take(y, split.Train);
            var validationY = DataSplit.Take(y, split.Validation);
            var testY = DataSplit.Take(y, split.Test);
            var threshold = trained.TestMetrics?.Threshold ?? 0.5;
            var thresholdSource = trained.TestMetrics?.ThresholdSource ?? "Default";

            if (!options.DecisionThreshold.HasValue && validationY.Any(v => v == 1) && validationY.Any(v => v == 0))
            {
                threshold = ThresholdSelection.MaximiseF1(trained.ValidationPredictions.Probabilities, validationY);
                thresholdSource = "MaximumF1OnValidation";
            }

            var trainMetrics =
                ClassificationMetricsBuilder.Build(trained.TrainPredictions.Probabilities, trainY, threshold);
            var validationMetrics =
                ClassificationMetricsBuilder.Build(trained.ValidationPredictions.Probabilities, validationY, threshold);
            var testMetrics =
                ClassificationMetricsBuilder.Build(trained.TestPredictions.Probabilities, testY, threshold);

            foreach (var warning in trained.Warnings)
            {
                warnings.Add(new EvaluationWarning(warning.Code, warning.Message));
            }

            var positives = trainY.Count(v => v == 1);
            AddResultWarnings(warnings, config, trainMetrics, testMetrics, positives, trainY.Length - positives);

            var coefficients = trained.Coefficients.Where(c => c.Kind == "Predictor").ToArray();

            var importance = names.Select(n =>
            {
                var row = coefficients.FirstOrDefault(c => c.Name == n);

                return row?.WaldZ is { } z && double.IsFinite(z) ? Math.Abs(z) : 0d;
            }).ToArray();

            var facts = coefficients.Select(c => ModelText.Fact("Coefficient:" + c.Name, c.Estimate)).ToList();
            facts.Add(ModelText.Fact("Intercept", trained.Intercept));

            var description = new ModelDescription(
                "Logistic regression from TrainLogisticRegression, evaluated on the same splits as the other families. Coefficients are on the original variable scale; importance is the absolute Wald z statistic.",
                facts.ToArray(),
                [],
                null,
                ModelText.Rank(names, importance));

            return new BinaryRun
            {
                Config = config,
                PredictRaw = rows => recall.PredictProbabilities(rows),
                Predict = rows => recall.PredictProbabilities(rows),
                RawIsProbability = true,
                Calibrator = null,
                TrainRows = split.Train,
                ValidationRows = split.Validation,
                TestRows = split.Test,
                TrainProbability = trained.TrainPredictions.Probabilities,
                ValidationProbability = trained.ValidationPredictions.Probabilities,
                TestProbability = trained.TestPredictions.Probabilities,
                ValidationRaw = trained.ValidationPredictions.Probabilities,
                TestRaw = trained.TestPredictions.Probabilities,
                TrainLabels = trainY,
                ValidationLabels = validationY,
                TestLabels = testY,
                Threshold = threshold,
                ThresholdSource = thresholdSource,
                Calibration = new CalibrationSummary(trained.Calibration.Applied,
                    trained.Calibration.Applied ? "Platt" : "None",
                    trained.Calibration.Slope, trained.Calibration.Intercept, trained.Calibration.RowsUsed,
                    trained.Calibration.Reason),
                TrainMetrics = trainMetrics,
                ValidationMetrics = validationMetrics,
                TestMetrics = testMetrics,
                Description = description,
                Warnings = warnings,
                Sweep = []
            };
        }

        private static void AddDataWarnings(List<EvaluationWarning> warnings, LearnerConfig config,
            FeatureTransform transform, int trainRows, int positives, int variables)
        {
            if (transform.ConstantColumns.Length > 0)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.ConstantColumns,
                    $"{transform.ConstantColumns.Length} variable(s) are constant in the training split and carry no information."));
            }

            if (transform.MissingCounts.Any(m => m > 0))
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.MissingValues,
                    "Missing values were replaced with the training median of each variable; no missing flags are added, so missingness itself is not modelled."));
            }

            var events = Math.Min(positives, trainRows - positives);

            if (config.Family is EvaluationFamily.NeuralNetwork or EvaluationFamily.KernelSupportVector
                    or EvaluationFamily.GradientBoosting or EvaluationFamily.RandomForest &&
                events / (double)Math.Max(variables, 1) < 10d)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.LowEventsPerVariable,
                    $"Only {events} rows of the rarer class per {variables} variables in training; a flexible model like {config.Family} is likely to overfit."));
            }
        }

        private static ClassifierEvaluationResult WithInterpretation(ClassifierEvaluationResult result,
            LearnerConfig config)
        {
            var text = InterpretationWriter.Classifier(result, config);

            return new ClassifierEvaluationResult
            {
                Family = result.Family,
                Scheme = result.Scheme,
                Seed = result.Seed,
                Settings = result.Settings,
                Rows = result.Rows,
                Features = result.Features,
                Positives = result.Positives,
                PositiveRate = result.PositiveRate,
                TrainRows = result.TrainRows,
                ValidationRows = result.ValidationRows,
                TestRows = result.TestRows,
                Test = result.Test,
                Validation = result.Validation,
                Train = result.Train,
                Folds = result.Folds,
                FoldSummary = result.FoldSummary,
                Calibration = result.Calibration,
                Sweep = result.Sweep,
                Importance = result.Importance,
                Model = result.Model,
                Warnings = result.Warnings,
                Interpretation = text
            };
        }
    }
}