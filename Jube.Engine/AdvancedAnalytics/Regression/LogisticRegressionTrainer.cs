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
using Accord.Math;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public static class LogisticRegressionTrainer
    {
        public const int MinimumTestRows = 100;
        public const int MinimumTestClassRows = 10;
        public const double MinimumEventsPerVariable = 10d;
        public const double HighVarianceInflation = 10d;
        public const double SevereImbalanceRatio = 100d;
        public const double HugeStandardisedCoefficient = 10d;
        public const double NearConstantShare = 0.995;
        public const double CalibrationSlopeLow = 0.7;
        public const double CalibrationSlopeHigh = 1.3;
        public const double SignificanceLevel = 0.05;
        public const double Z95 = 1.959963984540054;

        public static LogisticRegressionResult Train(double[][] inputs, int[] labels, string[] variableNames,
            LogisticRegressionOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new LogisticRegressionOptions();
            options.Validate();
            ValidateData(inputs, labels, variableNames, options);

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

        public static LogisticRegressionResult TrainWithSplit(double[][] inputs, int[] labels, string[] variableNames,
            LogisticRegressionOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, labels, variableNames, options);
            ValidateSplit(split, labels.Length);
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = new List<LogisticWarning>();
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

            if (trainY.Length <= variableCount)
            {
                throw new ArgumentException("The training split has no more rows than there are variables.");
            }

            var fill = new double[variableCount];
            var means = new double[variableCount];
            var deviations = new double[variableCount];
            var missingCounts = new int[variableCount];
            var excluded = new string[variableCount];
            var indicators = new List<int>();

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
                means[j] = scaler.Mean;
                deviations[j] = scaler.StandardDeviation;

                var groups = finite.GroupBy(v => v).Select(g => g.Count()).ToArray();

                if (groups.Length == 1)
                {
                    excluded[j] = "Constant";

                    warnings.Add(new LogisticWarning(LogisticWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split and was excluded from the fit."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new LogisticWarning(LogisticWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows; its coefficient will be unstable."));
                }

                if (options.MissingValuePolicy == LogisticMissingValuePolicy.ImputeMedianWithIndicators &&
                    missingCounts[j] > 0)
                {
                    indicators.Add(j);
                }
            }

            var activeInputs = Enumerable.Range(0, variableCount).Where(j => excluded[j] == null).ToArray();
            var columnCount = activeInputs.Length + indicators.Count;

            if (activeInputs.Length == 0)
            {
                throw new ArgumentException(
                    "Every variable is constant in the training split; there is nothing to fit.");
            }

            var standardise = options.Standardise;
            var trainingCount = trainY.Length;

            double[] Design(double[] row)
            {
                var design = new double[columnCount];
                var position = 0;

                foreach (var j in activeInputs)
                {
                    var value = double.IsFinite(row[j]) ? row[j] : fill[j];

                    if (standardise)
                    {
                        value = Standardisation.ZScore(value, means[j], deviations[j]);
                    }

                    design[position++] = value;
                }

                foreach (var j in indicators)
                {
                    design[position++] = double.IsFinite(row[j]) ? 0d : 1d;
                }

                return design;
            }

            var design = new double[trainingCount][];

            for (var i = 0; i < trainingCount; i++)
            {
                design[i] = Design(trainX[i]);
            }

            var trainWeights = ClassWeights(trainY, options.ClassWeighting);

            cancellationToken.ThrowIfCancellationRequested();

            var fitOutcome = options.RidgeLambda > 0
                ? FitPenalised(design, trainY, trainWeights, options, cancellationToken)
                : FitIrls(design, trainY, trainWeights, options, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var beta = fitOutcome.Beta;

            if (beta.Any(v => !double.IsFinite(v)))
            {
                throw new InvalidOperationException(
                    "The fit diverged to non-finite coefficients, which usually means the classes are separable.");
            }

            var hessian = InformationMatrix(design, beta, trainWeights, options.RidgeLambda);
            var maximumGradient = MaximumGradient(design, trainY, beta, trainWeights, options.RidgeLambda);
            var covariance = SmallLinearAlgebra.Inverse(hessian);

            var fullWeights = new double[variableCount + indicators.Count];

            for (var c = 0; c < activeInputs.Length; c++)
            {
                fullWeights[activeInputs[c]] = beta[c + 1];
            }

            for (var k = 0; k < indicators.Count; k++)
            {
                fullWeights[variableCount + k] = beta[activeInputs.Length + k + 1];
            }

            var trainRate = trainPositives / (double)trainingCount;
            var reject = options.MissingValuePolicy == LogisticMissingValuePolicy.Reject;

            double[] Logits(LogisticRegressionPipeline pipeline, double[][] rows)
            {
                return [.. rows.Select(pipeline.FinalLogit)];
            }

            var preliminary = new LogisticRegressionPipeline(standardise, reject, fill, means, deviations,
                [.. indicators], fullWeights, beta[0], null, 0.5, 0.5);

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

                warnings.Add(new LogisticWarning(LogisticWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the model is uncalibrated."));
            }
            else
            {
                var validationScores = Logits(preliminary, validationX);

                var validationWeights = options.ClassWeighting == LogisticClassWeighting.Balanced
                    ? ClassWeights(validationY, options.ClassWeighting)
                    : null;

                try
                {
                    calibrator = PlattCalibration.Fit(validationScores, validationY, validationWeights,
                        cancellationToken: cancellationToken);

                    calibrationRows = validationY.Length;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new LogisticWarning(LogisticWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var balanced = options.ClassWeighting == LogisticClassWeighting.Balanced;
            var validationRate = validationY.Length == 0 ? trainRate : validationPositives / (double)validationY.Length;
            var effectivePrior = balanced ? 0.5 : calibrator.HasValue ? validationRate : trainRate;
            var pooled = (trainPositives + validationPositives) / (double)(trainingCount + validationY.Length);
            var baseRate = options.BaseRateOverride ?? pooled;
            var baseSource = options.BaseRateOverride.HasValue ? "Override" : "TrainAndValidationRate";

            if (effectivePrior <= 0 || effectivePrior >= 1)
            {
                effectivePrior = trainRate;
            }

            if (baseRate <= 0 || baseRate >= 1)
            {
                throw new ArgumentException("The base rate must be strictly between 0 and 1.");
            }

            var pipeline = new LogisticRegressionPipeline(standardise, reject, fill, means, deviations,
                [.. indicators], fullWeights, beta[0], calibrator, effectivePrior, baseRate);

            var threshold = options.DecisionThreshold ??
                            (options.Costs?.MinimumRiskThreshold() ?? 0.5);

            var thresholdSource = options.DecisionThreshold.HasValue ? "Explicit"
                : options.Costs.HasValue ? "BayesMinimumRisk" : "Default";

            var trainLogits = Logits(pipeline, trainX);
            var validationLogits = Logits(pipeline, validationX);
            var testLogits = Logits(pipeline, testX);

            var trainMetrics = Metrics("Train", trainLogits, trainY, threshold, thresholdSource, options.Costs);
            var validationMetrics = Metrics("Validation", validationLogits, validationY, threshold, thresholdSource,
                options.Costs);
            var testMetrics = Metrics("Test", testLogits, testY, threshold, thresholdSource, options.Costs);

            var table = BuildCoefficientTable(variableNames, activeInputs, indicators, beta, covariance, means,
                deviations, standardise);

            var significances = table.Skip(1).Select(r => r.PValue).ToArray();

            var correlationInfo = VarianceInflation(trainX, activeInputs, fill);

            var preprocessing = new LogisticVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var activePosition = Array.IndexOf(activeInputs, j);

                preprocessing[j] = new LogisticVariablePreprocessing(variableNames[j], missingCounts[j], fill[j],
                    means[j], deviations[j], indicators.Contains(j), excluded[j] != null, excluded[j],
                    activePosition < 0 ? null : correlationInfo?[activePosition]);
            }

            var predictorCount = columnCount;
            var events = Math.Min(trainPositives, trainNegatives);
            var eventsPerVariable = events / (double)predictorCount;
            var converged = fitOutcome.Converged;

            AddFitWarnings(warnings, options, converged, beta, standardise,
                deviations, activeInputs, trainLogits, trainY, eventsPerVariable, covariance, trainY.Length,
                trainPositives, options.ClassWeighting, table, variableNames, correlationInfo, activeInputs);

            if (testMetrics == null)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows ||
                     Math.Min(testMetrics.Positives, testMetrics.Rows - testMetrics.Positives) < MinimumTestClassRows)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows with {testMetrics.Positives} positives; held-out metrics will be noisy."));
            }

            if (calibrator is { } fitted)
            {
                if (!fitted.IsIncreasing)
                {
                    warnings.Add(new LogisticWarning(LogisticWarningCodes.CalibratorNotIncreasing,
                        "The calibrator has a non-positive slope: on the validation split the model ranks cases the wrong way round."));
                }

                if (fitted.Slope < CalibrationSlopeLow || fitted.Slope > CalibrationSlopeHigh)
                {
                    warnings.Add(new LogisticWarning(LogisticWarningCodes.CalibrationSlopeFar,
                        $"The validation calibration slope is {fitted.Slope:0.###}, far from 1: the raw scores were over- or under-confident."));
                }
            }
            else if (testMetrics?.CalibrationSlope is { } testSlope &&
                     (testSlope < CalibrationSlopeLow || testSlope > CalibrationSlopeHigh))
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.CalibrationSlopeFar,
                    $"The test calibration slope is {testSlope:0.###}, far from 1: the probabilities are over- or under-confident."));
            }

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLogisticRegressionPipelineSnapshot>.Create(
                LogisticRegressionRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsLogisticRegressionPipelineSnapshot
                {
                    Standardise = standardise,
                    RejectMissing = reject,
                    FillValues = pipeline.Fill,
                    Means = pipeline.Means,
                    StandardDeviations = pipeline.StandardDeviations,
                    IndicatorInputs = pipeline.IndicatorInputs,
                    Weights = pipeline.Weights,
                    Intercept = pipeline.Intercept,
                    TrainingPrior = effectivePrior,
                    BaseRate = baseRate,
                    Significances = significances
                },
                calibrator.HasValue
                    ? new AdvancedAnalyticsCalibratorSnapshot
                    {
                        Method = LogisticRegressionRecall.CalibratorMethod,
                        Parameters = calibrator.Value.ToPrimitives()
                    }
                    : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new LogisticRegressionResult
            {
                Kind = LogisticRegressionRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new LogisticSplitSizes(trainY.Length, validationY.Length, testY.Length, trainPositives,
                    validationPositives, testY.Sum()),
                TrainMetrics = trainMetrics,
                ValidationMetrics = validationMetrics,
                TestMetrics = testMetrics,
                Coefficients = table,
                Preprocessing = preprocessing,
                Calibration = new LogisticCalibrationInfo(calibrator.HasValue,
                    calibrator?.Slope, calibrator?.Intercept, calibrationRows, calibrationReason),
                Priors = new LogisticPriors(trainRate, effectivePrior, baseRate, baseSource),
                Fit = new LogisticFitInfo(converged, fitOutcome.Iterations, options.MaximumIterations,
                    maximumGradient, options.RidgeLambda > 0, options.RidgeLambda, predictorCount, eventsPerVariable,
                    StandardErrorBasis(options)),
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone(),
                TrainPredictions = new LogisticPredictions((int[])trainRows.Clone(), trainY, Sigmoids(trainLogits)),
                ValidationPredictions = new LogisticPredictions((int[])validationRows.Clone(), validationY,
                    Sigmoids(validationLogits)),
                TestPredictions = new LogisticPredictions((int[])testRows.Clone(), testY, Sigmoids(testLogits)),
                Weights = pipeline.Weights,
                Intercept = pipeline.Intercept
            };
        }

        private static string StandardErrorBasis(LogisticRegressionOptions options)
        {
            var basis =
                "Inverse of the observed information X'WX at the estimate, on the standardised scale and mapped to the original scale by the delta method";

            if (options.RidgeLambda > 0)
            {
                basis +=
                    ", including the ridge term lambda on every non-intercept coefficient (penalised observed information)";
            }

            if (options.ClassWeighting == LogisticClassWeighting.Balanced)
            {
                basis += "; class weights are treated as frequency weights, so the errors are approximate";
            }

            return basis + ".";
        }

        private static double[] Sigmoids(double[] logits)
        {
            return [.. logits.Select(LogisticRegressionPipeline.Sigmoid)];
        }

        private static void ValidateData(double[][] inputs, int[] labels, string[] variableNames,
            LogisticRegressionOptions options)
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
                throw new ArgumentException(
                    $"There are {inputs.Length} input rows but {labels.Length} labels.");
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

                if (options.MissingValuePolicy == LogisticMissingValuePolicy.Reject)
                {
                    for (var j = 0; j < inputs[i].Length; j++)
                    {
                        if (!double.IsFinite(inputs[i][j]))
                        {
                            throw new ArgumentException(
                                $"Row {i}, variable '{variableNames[j]}' is missing (NaN or infinite) and the missing value policy is Reject.");
                        }
                    }
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

            if (inputs.Length <= variableNames.Length)
            {
                throw new ArgumentException(
                    $"There are {inputs.Length} rows for {variableNames.Length} variables; more rows than variables are needed.");
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

        private static double[] ClassWeights(int[] labels, LogisticClassWeighting weighting)
        {
            if (weighting == LogisticClassWeighting.None)
            {
                return null;
            }

            var positives = labels.Sum();
            var negatives = labels.Length - positives;

            if (positives == 0 || negatives == 0)
            {
                return null;
            }

            var positiveWeight = labels.Length / (2d * positives);
            var negativeWeight = labels.Length / (2d * negatives);

            return [.. labels.Select(l => l == 1 ? positiveWeight : negativeWeight)];
        }

        private static FitOutcome FitIrls(double[][] design, int[] y, double[] weights,
            LogisticRegressionOptions options, CancellationToken token)
        {
            var learner = new IterativeReweightedLeastSquares<LogisticRegression>
            {
                MaxIterations = 1,
                Tolerance = 0,
                Regularization = 0,
                ComputeStandardErrors = false,
                Token = token
            };

            LogisticRegression model = null;
            var converged = false;
            var iterations = 0;

            for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
            {
                token.ThrowIfCancellationRequested();
                model = learner.Learn(design, y, weights);
                token.ThrowIfCancellationRequested();
                iterations = iteration;

                var largest = 0d;

                foreach (var update in learner.Updates)
                {
                    largest = double.IsNaN(update) ? double.NaN : Math.Max(largest, Math.Abs(update));
                }

                if (double.IsNaN(largest) || double.IsInfinity(largest))
                {
                    throw new InvalidOperationException(
                        "The fit diverged to non-finite coefficients, which usually means the classes are separable.");
                }

                if (largest <= options.Tolerance)
                {
                    converged = true;
                    break;
                }
            }

            var beta = new double[design[0].Length + 1];
            beta[0] = model!.Intercept;
            Array.Copy(model.Weights, 0, beta, 1, model.Weights.Length);

            return new FitOutcome(beta, converged, iterations);
        }

        private static FitOutcome FitPenalised(double[][] design, int[] y, double[] weights,
            LogisticRegressionOptions options, CancellationToken token)
        {
            var n = design.Length;
            var p = design[0].Length + 1;
            var lambda = options.RidgeLambda;
            var beta = new double[p];
            var positives = 0d;
            var total = 0d;

            for (var i = 0; i < n; i++)
            {
                var w = weights?[i] ?? 1d;
                total += w;
                positives += w * y[i];
            }

            beta[0] = Math.Log(positives / (total - positives));

            double Objective(double[] b)
            {
                var sum = 0d;

                for (var i = 0; i < n; i++)
                {
                    var eta = b[0];

                    for (var c = 1; c < p; c++)
                    {
                        eta += b[c] * design[i][c - 1];
                    }

                    var softplus = eta > 0 ? eta + Math.Log(1d + Math.Exp(-eta)) : Math.Log(1d + Math.Exp(eta));
                    sum += (weights?[i] ?? 1d) * (softplus - y[i] * eta);
                }

                for (var c = 1; c < p; c++)
                {
                    sum += lambda / 2d * b[c] * b[c];
                }

                return sum;
            }

            var current = Objective(beta);
            var converged = false;
            var iterations = 0;

            for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
            {
                token.ThrowIfCancellationRequested();
                iterations = iteration;

                var gradient = new double[p];
                var hessian = new double[p][];

                for (var c = 0; c < p; c++)
                {
                    hessian[c] = new double[p];
                }

                var x = new double[p];
                x[0] = 1d;

                for (var i = 0; i < n; i++)
                {
                    var eta = beta[0];

                    for (var c = 1; c < p; c++)
                    {
                        x[c] = design[i][c - 1];
                        eta += beta[c] * x[c];
                    }

                    var w = weights?[i] ?? 1d;
                    var probability = LogisticRegressionPipeline.Sigmoid(eta);
                    var error = w * (probability - y[i]);
                    var curvature = w * probability * (1d - probability);

                    for (var a = 0; a < p; a++)
                    {
                        gradient[a] += error * x[a];

                        for (var b = 0; b <= a; b++)
                        {
                            hessian[a][b] += curvature * x[a] * x[b];
                        }
                    }
                }

                for (var a = 0; a < p; a++)
                for (var b = a + 1; b < p; b++)
                {
                    hessian[a][b] = hessian[b][a];
                }

                for (var c = 1; c < p; c++)
                {
                    gradient[c] += lambda * beta[c];
                    hessian[c][c] += lambda;
                }

                hessian[0][0] += 1e-12;

                var direction = SmallLinearAlgebra.Solve(hessian, gradient);

                if (direction == null)
                {
                    throw new InvalidOperationException("The penalised information matrix is not positive definite.");
                }

                var step = 1d;
                var accepted = false;
                var next = new double[p];

                while (step >= 1e-12)
                {
                    for (var c = 0; c < p; c++)
                    {
                        next[c] = beta[c] - step * direction[c];
                    }

                    var value = Objective(next);

                    if (value <= current + 1e-12 * Math.Abs(current))
                    {
                        current = value;
                        Array.Copy(next, beta, p);
                        accepted = true;
                        break;
                    }

                    step /= 2d;
                }

                if (!accepted)
                {
                    converged = gradient.Max(Math.Abs) / total < 1e-8;
                    break;
                }

                if (direction.Max(Math.Abs) * step <= options.Tolerance)
                {
                    converged = true;
                    break;
                }
            }

            return new FitOutcome(beta, converged, iterations);
        }

        private static double[][] InformationMatrix(double[][] design, double[] beta, double[] weights, double lambda)
        {
            var p = beta.Length;
            var hessian = new double[p][];

            for (var c = 0; c < p; c++)
            {
                hessian[c] = new double[p];
            }

            var x = new double[p];
            x[0] = 1d;

            for (var i = 0; i < design.Length; i++)
            {
                var eta = beta[0];

                for (var c = 1; c < p; c++)
                {
                    x[c] = design[i][c - 1];
                    eta += beta[c] * x[c];
                }

                var probability = LogisticRegressionPipeline.Sigmoid(eta);
                var curvature = (weights?[i] ?? 1d) * probability * (1d - probability);

                for (var a = 0; a < p; a++)
                for (var b = 0; b <= a; b++)
                {
                    hessian[a][b] += curvature * x[a] * x[b];
                }
            }

            for (var a = 0; a < p; a++)
            {
                for (var b = a + 1; b < p; b++)
                {
                    hessian[a][b] = hessian[b][a];
                }

                if (a > 0)
                {
                    hessian[a][a] += lambda;
                }
            }

            return hessian;
        }

        private static double MaximumGradient(double[][] design, int[] y, double[] beta, double[] weights,
            double lambda)
        {
            var p = beta.Length;
            var gradient = new double[p];
            var total = 0d;

            for (var i = 0; i < design.Length; i++)
            {
                var eta = beta[0];

                for (var c = 1; c < p; c++)
                {
                    eta += beta[c] * design[i][c - 1];
                }

                var w = weights?[i] ?? 1d;
                total += w;
                var error = w * (LogisticRegressionPipeline.Sigmoid(eta) - y[i]);
                gradient[0] += error;

                for (var c = 1; c < p; c++)
                {
                    gradient[c] += error * design[i][c - 1];
                }
            }

            for (var c = 1; c < p; c++)
            {
                gradient[c] += lambda * beta[c];
            }

            return gradient.Max(Math.Abs) / total;
        }

        private static LogisticCoefficient[] BuildCoefficientTable(string[] names, int[] activeInputs,
            List<int> indicators, double[] beta, double[][] covariance, double[] means, double[] deviations,
            bool standardise)
        {
            var p = beta.Length;
            var rows = new List<LogisticCoefficient>();

            double[] Jacobian(int c)
            {
                var t = new double[p];

                if (c < 0)
                {
                    t[0] = 1d;

                    for (var k = 0; k < activeInputs.Length; k++)
                    {
                        var j = activeInputs[k];
                        t[k + 1] = standardise && deviations[j] > 0 ? -means[j] / deviations[j] : 0d;
                    }

                    return t;
                }

                if (c < activeInputs.Length)
                {
                    var j = activeInputs[c];
                    t[c + 1] = standardise && deviations[j] > 0 ? 1d / deviations[j] : 1d;

                    return t;
                }

                t[c + 1] = 1d;

                return t;
            }

            LogisticCoefficient Row(string name, string kind, int c)
            {
                var t = Jacobian(c);
                var estimate = 0d;

                for (var a = 0; a < p; a++)
                {
                    estimate += t[a] * beta[a];
                }

                double? standardError = null;

                if (covariance != null)
                {
                    var variance = 0d;

                    for (var a = 0; a < p; a++)
                    for (var b = 0; b < p; b++)
                    {
                        variance += t[a] * t[b] * covariance[a][b];
                    }

                    if (variance > 0 && double.IsFinite(variance))
                    {
                        standardError = Math.Sqrt(variance);
                    }
                }

                return Describe(name, kind, estimate, standardError);
            }

            rows.Add(Row("(Intercept)", "Intercept", -1));

            var activeLookup = activeInputs.Select((j, k) => (j, k)).ToDictionary(t => t.j, t => t.k);

            for (var j = 0; j < names.Length; j++)
            {
                rows.Add(activeLookup.TryGetValue(j, out var position)
                    ? Row(names[j], "Predictor", position)
                    : new LogisticCoefficient(names[j], "Predictor", 0d, null, null, null, null, null, null));
            }

            for (var k = 0; k < indicators.Count; k++)
            {
                rows.Add(Row(names[indicators[k]] + "__missing", "MissingIndicator", activeInputs.Length + k));
            }

            return [.. rows];
        }

        internal static LogisticCoefficient Describe(string name, string kind, double estimate, double? standardError)
        {
            double? z = null;
            double? p = null;
            double? lower = null;
            double? upper = null;

            if (standardError is { } se and > 0)
            {
                z = estimate / se;
                p = Special.Erfc(Math.Abs(z.Value) / Math.Sqrt(2d));
                lower = Math.Exp(estimate - Z95 * se);
                upper = Math.Exp(estimate + Z95 * se);
            }

            var ratio = Math.Exp(estimate);

            return new LogisticCoefficient(name, kind, estimate, standardError, z, p, ratio,
                lower.HasValue && double.IsFinite(lower.Value) ? lower : null,
                upper.HasValue && double.IsFinite(upper.Value) ? upper : null);
        }

        private static double?[] VarianceInflation(double[][] trainX, int[] activeInputs, double[] fill)
        {
            var q = activeInputs.Length;
            var n = trainX.Length;
            var result = new double?[q];

            if (q == 1)
            {
                result[0] = 1d;

                return result;
            }

            var columns = new double[q][];

            for (var k = 0; k < q; k++)
            {
                columns[k] = new double[n];

                for (var i = 0; i < n; i++)
                {
                    var value = trainX[i][activeInputs[k]];
                    columns[k][i] = double.IsFinite(value) ? value : fill[activeInputs[k]];
                }
            }

            var means = columns.Select(c => c.Average()).ToArray();
            var sds = new double[q];

            for (var k = 0; k < q; k++)
            {
                sds[k] = Math.Sqrt(columns[k].Sum(v => (v - means[k]) * (v - means[k])));
            }

            var correlation = new double[q][];

            for (var a = 0; a < q; a++)
            {
                correlation[a] = new double[q];

                for (var b = 0; b < q; b++)
                {
                    var sum = 0d;

                    for (var i = 0; i < n; i++)
                    {
                        sum += (columns[a][i] - means[a]) * (columns[b][i] - means[b]);
                    }

                    correlation[a][b] = a == b ? 1d : sum / (sds[a] * sds[b]);
                }
            }

            var inverse = SmallLinearAlgebra.Inverse(correlation);

            if (inverse == null)
            {
                return result;
            }

            for (var k = 0; k < q; k++)
            {
                result[k] = inverse[k][k];
            }

            return result;
        }

        private static void AddFitWarnings(List<LogisticWarning> warnings, LogisticRegressionOptions options,
            bool converged, double[] beta, bool standardise, double[] deviations,
            int[] activeInputs, double[] trainLogits, int[] trainY, double eventsPerVariable, double[][] covariance,
            int trainRows, int trainPositives, LogisticClassWeighting weighting, LogisticCoefficient[] table,
            string[] names, double?[] vifs, int[] active)
        {
            if (!converged)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.NonConvergence,
                    $"The fit did not converge within {options.MaximumIterations} iterations (tolerance {options.Tolerance:g}); treat the coefficients as unreliable."));
            }

            var trainAuc = BinaryMetrics.RocAuc(trainLogits, trainY);

            if (trainAuc >= 1d)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.PerfectSeparation,
                    "The model ranks every training positive above every training negative: the classes are perfectly separable, the maximum likelihood estimates do not exist and the coefficients and their errors are not meaningful. Use a ridge penalty or fewer variables."));
            }

            var huge = new List<string>();

            for (var c = 1; c < beta.Length; c++)
            {
                var scale = c - 1 < activeInputs.Length && !standardise ? deviations[activeInputs[c - 1]] : 1d;
                var magnitude = Math.Abs(beta[c]) * (standardise ? 1d : Math.Max(scale, 1e-12));

                if (magnitude > HugeStandardisedCoefficient)
                {
                    huge.Add(c - 1 < activeInputs.Length ? names[activeInputs[c - 1]] : "a missing indicator");
                }
            }

            if (huge.Count > 0)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.HugeCoefficients,
                    $"Standardised coefficients above {HugeStandardisedCoefficient:0} in size for: {string.Join(", ", huge)}; this is the signature of quasi-separation or extreme collinearity."));
            }

            if (eventsPerVariable < MinimumEventsPerVariable)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.LowEventsPerVariable,
                    $"There are {eventsPerVariable:0.##} events (rarer class rows) per fitted predictor in the training split, below {MinimumEventsPerVariable:0}; estimates are likely overfitted and unstable."));
            }

            if (vifs == null || vifs.Any(v => v == null))
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.HighVariance,
                    "Some predictors are perfectly collinear (variance inflation is infinite); their individual coefficients are not identifiable."));
            }
            else
            {
                var high = Enumerable.Range(0, vifs.Length).Where(k => vifs[k]!.Value > HighVarianceInflation)
                    .Select(k => $"{names[active[k]]} ({vifs[k]!.Value:0.#})").ToArray();

                if (high.Length > 0)
                {
                    warnings.Add(new LogisticWarning(LogisticWarningCodes.HighVariance,
                        $"Variance inflation factors above {HighVarianceInflation:0}: {string.Join(", ", high)}; the coefficients of these predictors share information and are individually unstable."));
                }
            }

            var minority = Math.Min(trainPositives, trainRows - trainPositives);
            var majority = Math.Max(trainPositives, trainRows - trainPositives);

            if (weighting == LogisticClassWeighting.None && majority / (double)minority > SevereImbalanceRatio)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.SevereImbalance,
                    $"The training classes are imbalanced beyond 1:{SevereImbalanceRatio:0} and no class weighting was used; consider Balanced weights."));
            }

            if (covariance == null)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.UndefinedStandardErrors,
                    "The information matrix is singular, so standard errors and p-values could not be computed."));
            }
            else
            {
                var testable = table.Where(r => r.Kind != "Intercept" && r.PValue.HasValue).ToArray();

                if (testable.Length > 0 && testable.All(r => r.PValue!.Value > SignificanceLevel))
                {
                    warnings.Add(new LogisticWarning(LogisticWarningCodes.NoSignificantPredictors,
                        $"No predictor has a Wald p-value below {SignificanceLevel}; the model has no statistically supported signal."));
                }
            }

            if (weighting == LogisticClassWeighting.Balanced || options.RidgeLambda > 0)
            {
                warnings.Add(new LogisticWarning(LogisticWarningCodes.ApproximateStandardErrors,
                    "Standard errors and p-values are approximate because the fit uses class weights or a ridge penalty."));
            }
        }

        private static LogisticSplitMetrics Metrics(string split, double[] logits, int[] labels, double threshold,
            string thresholdSource, CostMatrix? costs)
        {
            if (labels.Length == 0)
            {
                return null;
            }

            var probabilities = Sigmoids(logits);
            var positives = labels.Sum();
            var bothClasses = positives > 0 && positives < labels.Length;

            double? Number(double value)
            {
                return double.IsFinite(value) ? value : null;
            }

            double? auc = null, se = null, lower = null, upper = null, ap = null;

            if (bothClasses)
            {
                var result = BinaryMetrics.RocAucWithStandardError(logits, labels);
                auc = Number(result.Auc);
                se = Number(result.StandardError);

                if (se.HasValue)
                {
                    var interval = result.Interval();
                    lower = interval.Lower;
                    upper = interval.Upper;
                }

                ap = Number(BinaryMetrics.AveragePrecision(logits, labels));
            }

            var bins = BinaryMetrics.Reliability(probabilities, labels)
                .Select(b => new LogisticReliabilityBin(b.Lower, b.Upper, b.Count, Number(b.MeanPredicted),
                    Number(b.ObservedRate)))
                .ToArray();

            var confusion = BinaryMetrics.Confusion(probabilities, labels, threshold);
            var line = CalibrationLine(logits, labels);

            return new LogisticSplitMetrics(split, labels.Length, positives, auc, se, lower, upper, ap,
                Number(BinaryMetrics.LogLoss(probabilities, labels)),
                Number(BinaryMetrics.BrierScore(probabilities, labels)),
                Number(BinaryMetrics.ExpectedCalibrationError(probabilities, labels)), bins, threshold,
                thresholdSource, confusion.TruePositives, confusion.FalsePositives, confusion.TrueNegatives,
                confusion.FalseNegatives, Number(confusion.Precision), Number(confusion.Recall),
                Number(confusion.F1), Number(confusion.Mcc),
                costs.HasValue ? Number(confusion.ExpectedCost(costs.Value)) : null,
                line?.Slope, line?.Intercept, probabilities.Average());
        }

        private static (double Slope, double Intercept)? CalibrationLine(double[] logits, int[] labels)
        {
            var positives = labels.Sum();

            if (positives == 0 || positives == labels.Length || logits.Distinct().Count() < 2)
            {
                return null;
            }

            var clipped = logits.Select(v => Math.Min(Math.Max(v, -30d), 30d)).ToArray();
            var fit = UnivariateLogistic.Fit(clipped, labels);

            return fit.Success ? (fit.Slope, fit.Intercept) : null;
        }

        private readonly record struct FitOutcome(double[] Beta, bool Converged, int Iterations);
    }
}