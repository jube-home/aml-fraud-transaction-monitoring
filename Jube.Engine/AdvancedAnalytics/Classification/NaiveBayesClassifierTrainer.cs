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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Accord.MachineLearning.Bayes;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public static class NaiveBayesClassifierTrainer
    {
        public const int MinimumTestRows = 100;
        public const int MinimumTestClassRows = 10;
        public const double NearConstantShare = 0.995;
        public const double CalibrationSlopeLow = 0.7;
        public const double CalibrationSlopeHigh = 1.3;
        public const double SevereImbalanceRatio = 100d;
        public const double AbsoluteVarianceFloor = 1e-12;

        public static NaiveBayesClassifierResult Train(double[][] inputs, int[] labels, string[] variableNames,
            NaiveBayesClassifierOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new NaiveBayesClassifierOptions();
            options.Validate();
            ValidateData(inputs, labels, variableNames);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;

            var split = DataSplit.Split([.. labels.Select(l => (double)l)], trainFraction,
                options.ValidationFraction, options.TestFraction, options.Seed);

            var assigned = new bool[labels.Length];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[row] = true;
            }

            var leftovers = Enumerable.Range(0, labels.Length).Where(i => !assigned[i]).ToArray();

            if (leftovers.Length > 0)
            {
                split = new DataSplitIndices([.. split.Train, .. leftovers], split.Validation, split.Test);
            }

            return TrainWithSplit(inputs, labels, variableNames, options, split, cancellationToken);
        }

        public static NaiveBayesClassifierResult TrainWithSplit(double[][] inputs, int[] labels, string[] variableNames,
            NaiveBayesClassifierOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, labels, variableNames);
            ValidateSplit(split, labels.Length);
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = new List<AdvancedAnalyticsWarning>();
            var variableCount = variableNames.Length;

            var trainRows = split.Train;
            var validationRows = split.Validation;
            var testRows = split.Test;

            var trainX = DataSplit.Take(inputs, trainRows);
            var trainY = DataSplit.Take(labels, trainRows);
            var validationX = DataSplit.Take(inputs, validationRows);
            var validationY = DataSplit.Take(labels, validationRows);
            var testX = DataSplit.Take(inputs, testRows);
            var testY = DataSplit.Take(labels, testRows);

            var trainPositives = trainY.Sum();
            var trainNegatives = trainY.Length - trainPositives;

            if (trainPositives == 0 || trainNegatives == 0)
            {
                throw new ArgumentException("The training split must contain both classes.");
            }

            var fill = new double[variableCount];
            var missingCounts = new int[variableCount];
            var overallMeans = new double[variableCount];
            var overallDeviations = new double[variableCount];
            var overallVariance = new double[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var column = new double[trainX.Length];

                for (var i = 0; i < column.Length; i++)
                {
                    column[i] = trainX[i][j];
                }

                var finite = column.Where(double.IsFinite).ToArray();
                missingCounts[j] = column.Length - finite.Length;

                if (finite.Length == 0)
                {
                    throw new ArgumentException(
                        $"Variable '{variableNames[j]}' has no observed value in the training split.");
                }

                fill[j] = Imputer.Fit(column, ImputationStrategy.Median).FillValue;
                var scaler = Standardisation.Fit(column);
                overallMeans[j] = scaler.Mean;
                overallDeviations[j] = scaler.StandardDeviation;
                overallVariance[j] = scaler.StandardDeviation * scaler.StandardDeviation;

                var groups = finite.GroupBy(v => v).Select(g => g.Count()).ToArray();

                if (groups.Length == 1)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to the posterior."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            double[] Fill(double[] row)
            {
                var filled = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    filled[j] = double.IsFinite(row[j]) ? row[j] : fill[j];
                }

                return filled;
            }

            var trainDesign = trainX.Select(Fill).ToArray();

            var rawVariance1 = PerClassVariance(trainDesign, trainY, 1, variableCount);
            var rawVariance0 = PerClassVariance(trainDesign, trainY, 0, variableCount);
            var floored = new bool[variableCount];
            IFittingOptions[] innerOptions = new IFittingOptions[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var floor = Math.Max(overallVariance[j] * options.VarianceRegularisation, AbsoluteVarianceFloor);
                innerOptions[j] = new NormalOptions { Regularization = floor };

                floored[j] = !double.IsFinite(rawVariance1[j]) || rawVariance1[j] <= 0 ||
                             !double.IsFinite(rawVariance0[j]) || rawVariance0[j] <= 0;
            }

            if (floored.Any(f => f))
            {
                warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.VarianceFloored,
                    "The class-conditional variance was floored with the regularisation constant for: " +
                    string.Join(", ", Enumerable.Range(0, variableCount).Where(j => floored[j])
                        .Select(j => variableNames[j])) + "."));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var learner = new NaiveBayesLearning<NormalDistribution>
            {
                ParallelOptions = { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken },
                Options = new IndependentOptions { InnerOptions = innerOptions }
            };

            NaiveBayes<NormalDistribution> model;

            try
            {
                model = learner.Learn(trainDesign, trainY);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    "The Gaussian fit could not be completed: " + exception.Message, exception);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var mean1 = new double[variableCount];
            var variance1 = new double[variableCount];
            var mean0 = new double[variableCount];
            var variance0 = new double[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                mean1[j] = model.Distributions[1, j].Mean;
                variance1[j] = model.Distributions[1, j].Variance;
                mean0[j] = model.Distributions[0, j].Mean;
                variance0[j] = model.Distributions[0, j].Variance;
            }

            var priorLogOdds = Math.Log(trainPositives) - Math.Log(trainNegatives);

            var preliminary = new NaiveBayesClassifierPipeline(fill, mean1, variance1, mean0, variance0, priorLogOdds,
                null);

            Calibrator? calibrator = null;
            string calibrationReason = null;
            var calibrationRows = 0;
            var validationPositives = validationY.Sum();

            if (!options.Calibrate)
            {
                calibrationReason = "Calibration was switched off.";
            }
            else if (validationY.Length == 0 || validationPositives == 0 || validationPositives == validationY.Length)
            {
                calibrationReason = "The validation split does not contain both classes.";

                warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the native posterior is reported instead."));
            }
            else
            {
                var validationRawScores = validationX.Select(preliminary.RawScore).ToArray();

                try
                {
                    calibrator = PlattCalibration.Fit(validationRawScores, validationY,
                        cancellationToken: cancellationToken);

                    calibrationRows = validationY.Length;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = new NaiveBayesClassifierPipeline(fill, mean1, variance1, mean0, variance0, priorLogOdds,
                calibrator);

            if (calibrator is { } fitted)
            {
                if (!fitted.IsIncreasing)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.CalibratorNotIncreasing,
                        "The calibrator has a non-positive slope: on the validation split the model ranks cases the wrong way round."));
                }
                else if (fitted.Slope < CalibrationSlopeLow || fitted.Slope > CalibrationSlopeHigh)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.CalibratorNotIncreasing,
                        $"The validation calibration slope is {fitted.Slope:0.###}, far from 1: the native posterior was over- or under-confident."));
                }
            }

            var trainMetrics = Metrics("Train", pipeline, trainX, trainY, options.DecisionThreshold);
            var validationMetrics =
                Metrics("Validation", pipeline, validationX, validationY, options.DecisionThreshold);
            var testMetrics = Metrics("Test", pipeline, testX, testY, options.DecisionThreshold);

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows ||
                     Math.Min(testMetrics.Positives, testMetrics.Rows - testMetrics.Positives) < MinimumTestClassRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows with {testMetrics.Positives} positives; held-out metrics will be noisy."));
            }

            var minority = Math.Min(trainPositives, trainNegatives);
            var majority = Math.Max(trainPositives, trainNegatives);

            if (majority / (double)minority > SevereImbalanceRatio)
            {
                warnings.Add(new AdvancedAnalyticsWarning(NaiveBayesWarningCodes.SevereImbalance,
                    $"The training classes are imbalanced beyond 1:{SevereImbalanceRatio:0}; the class priors reflect this and may dominate the posterior for weak predictors."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];
            var featureSummaries = new NaiveBayesFeatureSummary[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], overallMeans[j],
                    overallDeviations[j], false, null);

                featureSummaries[j] = new NaiveBayesFeatureSummary(variableNames[j], mean1[j], variance1[j], mean0[j],
                    variance0[j], floored[j]);
            }

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsNaiveBayesClassifierPipelineSnapshot>.Create(
                NaiveBayesClassifierRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsNaiveBayesClassifierPipelineSnapshot
                {
                    FillValues = fill,
                    Mean1 = mean1,
                    Variance1 = variance1,
                    Mean0 = mean0,
                    Variance0 = variance0,
                    PriorLogOdds = priorLogOdds
                },
                calibrator.HasValue
                    ? new AdvancedAnalyticsCalibratorSnapshot
                    {
                        Method = NaiveBayesClassifierRecall.CalibratorMethod,
                        Parameters = calibrator.Value.ToPrimitives()
                    }
                    : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new NaiveBayesClassifierResult
            {
                Kind = NaiveBayesClassifierRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                Details = new NaiveBayesEvidenceDetails
                {
                    PriorLogOdds = priorLogOdds,
                    TrainPositiveRate = trainPositives / (double)trainY.Length,
                    VarianceRegularisation = options.VarianceRegularisation,
                    FeatureSummaries = featureSummaries,
                    Calibration = new NaiveBayesCalibrationInfo(calibrator.HasValue, calibrator?.Slope,
                        calibrator?.Intercept, calibrationRows, calibrationReason),
                    TrainMetrics = trainMetrics,
                    ValidationMetrics = validationMetrics,
                    TestMetrics = testMetrics
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static double[] PerClassVariance(double[][] design, int[] labels, int targetClass, int variableCount)
        {
            var result = new double[variableCount];
            var rows = Enumerable.Range(0, labels.Length).Where(i => labels[i] == targetClass).ToArray();

            for (var j = 0; j < variableCount; j++)
            {
                var column = rows.Select(i => design[i][j]).ToArray();

                if (column.Length < 2)
                {
                    result[j] = 0d;
                    continue;
                }

                var mean = column.Average();
                result[j] = column.Sum(v => (v - mean) * (v - mean)) / (column.Length - 1);
            }

            return result;
        }

        private static NaiveBayesSplitMetrics Metrics(string split, NaiveBayesClassifierPipeline pipeline,
            double[][] inputs, int[] labels, double threshold)
        {
            if (labels.Length == 0)
            {
                return null;
            }

            var rawScores = inputs.Select(pipeline.RawScore).ToArray();
            var probabilities = inputs.Select(pipeline.Probability).ToArray();
            var positives = labels.Sum();
            var bothClasses = positives > 0 && positives < labels.Length;

            double? Number(double value)
            {
                return double.IsFinite(value) ? value : null;
            }

            double? auc = null, ap = null;

            if (bothClasses)
            {
                auc = Number(BinaryMetrics.RocAuc(rawScores, labels));
                ap = Number(BinaryMetrics.AveragePrecision(rawScores, labels));
            }

            var confusion = BinaryMetrics.Confusion(probabilities, labels, threshold);

            return new NaiveBayesSplitMetrics(split, labels.Length, positives, auc, ap,
                Number(BinaryMetrics.LogLoss(probabilities, labels)),
                Number(BinaryMetrics.BrierScore(probabilities, labels)),
                Number(BinaryMetrics.ExpectedCalibrationError(probabilities, labels)), threshold,
                confusion.TruePositives, confusion.FalsePositives, confusion.TrueNegatives, confusion.FalseNegatives,
                Number(confusion.Precision), Number(confusion.Recall), Number(confusion.F1), Number(confusion.Mcc));
        }

        private static void ValidateData(double[][] inputs, int[] labels, string[] variableNames)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(labels);
            ArgumentNullException.ThrowIfNull(variableNames);

            if (variableNames.Length == 0)
            {
                throw new ArgumentException("At least one variable is needed.");
            }

            if (variableNames.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Variable names must not be blank.");
            }

            if (variableNames.Distinct(StringComparer.Ordinal).Count() != variableNames.Length)
            {
                throw new ArgumentException("Variable names must be unique.");
            }

            if (inputs.Length != labels.Length)
            {
                throw new ArgumentException($"There are {inputs.Length} input rows but {labels.Length} labels.");
            }

            if (inputs.Length == 0)
            {
                throw new ArgumentException("There are no rows.");
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                if (inputs[i].Length != variableNames.Length)
                {
                    throw new ArgumentException(
                        $"Row {i} has {inputs[i].Length} values but there are {variableNames.Length} variable names (rows must all be the same length).");
                }
            }

            if (labels.Any(l => l != 0 && l != 1))
            {
                throw new ArgumentException("Labels must be 0 or 1.");
            }

            if (labels.Sum() == 0 || labels.Sum() == labels.Length)
            {
                throw new ArgumentException("Both classes must be present in the labels.");
            }
        }

        private static void ValidateSplit(DataSplitIndices split, int rowCount)
        {
            var seen = new bool[rowCount];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                if (row < 0 || row >= rowCount)
                {
                    throw new ArgumentException($"Split row {row} is out of range.");
                }

                if (seen[row])
                {
                    throw new ArgumentException($"Split row {row} appears in more than one split.");
                }

                seen[row] = true;
            }
        }
    }
}