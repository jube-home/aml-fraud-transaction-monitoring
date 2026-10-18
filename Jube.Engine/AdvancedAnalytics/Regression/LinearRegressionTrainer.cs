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
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Regression.Linear;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public static class LinearRegressionTrainer
    {
        public const int MinimumTestRows = 30;
        public const double MinimumObservationsPerVariable = 10d;
        public const double HighVarianceInflation = 10d;
        public const double NearConstantShare = 0.995;
        public const double SignificanceLevel = 0.05;

        public static LinearRegressionResult Train(double[][] inputs, double[] outputs, string[] variableNames,
            LinearRegressionOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new LinearRegressionOptions();
            options.Validate();
            ValidateData(inputs, outputs, variableNames, options);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;

            var split = DataSplit.Split(outputs, trainFraction, options.ValidationFraction, options.TestFraction,
                options.Seed, false);

            var assigned = new bool[outputs.Length];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[row] = true;
            }

            var leftovers = Enumerable.Range(0, outputs.Length).Where(i => !assigned[i]).ToArray();

            if (leftovers.Length > 0)
            {
                split = new DataSplitIndices([.. split.Train, .. leftovers], split.Validation, split.Test);
            }

            return TrainWithSplit(inputs, outputs, variableNames, options, split, cancellationToken);
        }

        public static LinearRegressionResult TrainWithSplit(double[][] inputs, double[] outputs, string[] variableNames,
            LinearRegressionOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, outputs, variableNames, options);
            ValidateSplit(split, outputs.Length);
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = new List<LinearWarning>();
            var variableCount = variableNames.Length;

            var trainRows = split.Train;
            var validationRows = split.Validation;
            var testRows = split.Test;

            var trainX = DataSplit.Take(inputs, trainRows);
            var trainY = DataSplit.Take(outputs, trainRows);
            var validationX = DataSplit.Take(inputs, validationRows);
            var validationY = DataSplit.Take(outputs, validationRows);
            var testX = DataSplit.Take(inputs, testRows);
            var testY = DataSplit.Take(outputs, testRows);

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

                    warnings.Add(new LinearWarning(LinearWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split and was excluded from the fit."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new LinearWarning(LinearWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows; its coefficient will be unstable."));
                }

                if (options.MissingValuePolicy == LinearMissingValuePolicy.ImputeMedianWithIndicators &&
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
            var reject = options.MissingValuePolicy == LinearMissingValuePolicy.Reject;

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

            cancellationToken.ThrowIfCancellationRequested();

            var ols = new OrdinaryLeastSquares { UseIntercept = true };
            var regression = ols.Learn(design, (double[])trainY.Clone());
            var information = ols.GetInformationMatrix();

            cancellationToken.ThrowIfCancellationRequested();

            var intercept = regression.Intercept;
            var weights = regression.Weights;

            if (!double.IsFinite(intercept) || weights.Any(w => !double.IsFinite(w)))
            {
                throw new InvalidOperationException(
                    "The fit produced non-finite coefficients, which usually means the predictors are perfectly collinear.");
            }

            var fullWeights = new double[variableCount + indicators.Count];

            for (var c = 0; c < activeInputs.Length; c++)
            {
                fullWeights[activeInputs[c]] = weights[c];
            }

            for (var k = 0; k < indicators.Count; k++)
            {
                fullWeights[variableCount + k] = weights[activeInputs.Length + k];
            }

            var pipeline = new LinearRegressionPipeline(standardise, reject, fill, means, deviations,
                [.. indicators], fullWeights, intercept);

            double[] Predict(double[][] rows)
            {
                return [.. rows.Select(pipeline.Predict)];
            }

            var trainPredicted = Predict(trainX);
            var validationPredicted = Predict(validationX);
            var testPredicted = Predict(testX);

            var predictorCount = columnCount;
            var residualDf = trainingCount - predictorCount - 1;
            var rss = 0d;

            for (var i = 0; i < trainingCount; i++)
            {
                var e = trainY[i] - trainPredicted[i];
                rss += e * e;
            }

            var sigma2 = residualDf > 0 ? rss / residualDf : double.NaN;

            var table = BuildCoefficientTable(variableNames, activeInputs, indicators, intercept, weights, information,
                sigma2, residualDf);

            var correlationInfo = VarianceInflation(trainX, activeInputs, fill);

            var preprocessing = new LinearVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var activePosition = Array.IndexOf(activeInputs, j);

                preprocessing[j] = new LinearVariablePreprocessing(variableNames[j], missingCounts[j], fill[j],
                    means[j], deviations[j], indicators.Contains(j), excluded[j] != null, excluded[j],
                    activePosition < 0 ? null : correlationInfo?[activePosition]);
            }

            var observationsPerVariable = predictorCount == 0 ? double.NaN : trainingCount / (double)predictorCount;

            var trainMetrics = Metrics("Train", trainY, trainPredicted, predictorCount);
            var validationMetrics = Metrics("Validation", validationY, validationPredicted, predictorCount);
            var testMetrics = Metrics("Test", testY, testPredicted, predictorCount);

            AddFitWarnings(warnings, trainMetrics?.RSquared, observationsPerVariable, table, correlationInfo,
                activeInputs, variableNames);

            if (testMetrics == null)
            {
                warnings.Add(new LinearWarning(LinearWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new LinearWarning(LinearWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLinearRegressionPipelineSnapshot>.Create(
                MultipleLinearRegressionRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsLinearRegressionPipelineSnapshot
                {
                    Standardise = standardise,
                    RejectMissing = reject,
                    FillValues = pipeline.Fill,
                    Means = pipeline.Means,
                    StandardDeviations = pipeline.StandardDeviations,
                    IndicatorInputs = pipeline.IndicatorInputs,
                    Weights = pipeline.Weights,
                    Intercept = pipeline.Intercept
                });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new LinearRegressionResult
            {
                Kind = MultipleLinearRegressionRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new LinearSplitSizes(trainY.Length, validationY.Length, testY.Length),
                TrainMetrics = trainMetrics,
                ValidationMetrics = validationMetrics,
                TestMetrics = testMetrics,
                Coefficients = table,
                Preprocessing = preprocessing,
                Fit = new LinearFitInfo(predictorCount, observationsPerVariable, residualDf, StandardErrorBasis()),
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone(),
                TrainPredictions = new LinearPredictions((int[])trainRows.Clone(), trainY, trainPredicted),
                ValidationPredictions = new LinearPredictions((int[])validationRows.Clone(), validationY,
                    validationPredicted),
                TestPredictions = new LinearPredictions((int[])testRows.Clone(), testY, testPredicted),
                Weights = pipeline.Weights,
                Intercept = pipeline.Intercept
            };
        }

        private static string StandardErrorBasis()
        {
            return "Inverse of X'X from the ordinary least squares normal equations, scaled by the training " +
                   "residual variance, on the scale the model was fitted at (standardised when Standardise is true).";
        }

        private static void ValidateData(double[][] inputs, double[] outputs, string[] variableNames,
            LinearRegressionOptions options)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(outputs);
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

            if (inputs.Length != outputs.Length)
            {
                throw new ArgumentException(
                    $"There are {inputs.Length} input rows but {outputs.Length} outputs.");
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

                if (options.MissingValuePolicy == LinearMissingValuePolicy.Reject)
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

            if (outputs.Any(o => !double.IsFinite(o)))
            {
                throw new ArgumentException("Outputs must be finite numbers.");
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

        private static LinearCoefficient[] BuildCoefficientTable(string[] names, int[] activeInputs,
            List<int> indicators, double intercept, double[] weights, double[][] information, double sigma2,
            int residualDf)
        {
            var rows = new List<LinearCoefficient>();
            var columnCount = weights.Length;

            LinearCoefficient Row(string name, string kind, double estimate, int infoIndex)
            {
                double? standardError = null;

                if (residualDf > 0 && double.IsFinite(sigma2) && sigma2 >= 0 && infoIndex < information.Length)
                {
                    var variance = sigma2 * information[infoIndex][infoIndex];

                    if (variance > 0 && double.IsFinite(variance))
                    {
                        standardError = Math.Sqrt(variance);
                    }
                }

                return Describe(name, kind, estimate, standardError, residualDf);
            }

            rows.Add(Row("(Intercept)", "Intercept", intercept, columnCount));

            var activeLookup = activeInputs.Select((j, k) => (j, k)).ToDictionary(t => t.j, t => t.k);

            for (var j = 0; j < names.Length; j++)
            {
                rows.Add(activeLookup.TryGetValue(j, out var position)
                    ? Row(names[j], "Predictor", weights[position], position)
                    : new LinearCoefficient(names[j], "Predictor", 0d, null, null, null));
            }

            for (var k = 0; k < indicators.Count; k++)
            {
                rows.Add(Row(names[indicators[k]] + "__missing", "MissingIndicator", weights[activeInputs.Length + k],
                    activeInputs.Length + k));
            }

            return [.. rows];
        }

        internal static LinearCoefficient Describe(string name, string kind, double estimate, double? standardError,
            int residualDf)
        {
            double? t = null;
            double? p = null;

            if (standardError is { } se and > 0)
            {
                t = estimate / se;

                if (residualDf > 0)
                {
                    p = new TDistribution(residualDf).ComplementaryDistributionFunction(Math.Abs(t.Value)) * 2d;
                }
            }

            return new LinearCoefficient(name, kind, estimate, standardError, t, p);
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

        private static void AddFitWarnings(List<LinearWarning> warnings, double? trainRSquared,
            double observationsPerVariable, LinearCoefficient[] table, double?[] vifs, int[] active, string[] names)
        {
            if (!trainRSquared.HasValue)
            {
                warnings.Add(new LinearWarning(LinearWarningCodes.NoVariation,
                    "The outcome has no variation in the training split, so R squared is undefined."));
            }
            else if (trainRSquared.Value > 0.9999)
            {
                warnings.Add(new LinearWarning(LinearWarningCodes.AlmostPerfectFit,
                    "The fit is almost perfect (R squared above 0.9999) on the training split; standard errors and p-values are unreliable because the residual variance is close to zero."));
            }

            if (double.IsFinite(observationsPerVariable) && observationsPerVariable < MinimumObservationsPerVariable)
            {
                warnings.Add(new LinearWarning(LinearWarningCodes.LowObservationsPerVariable,
                    $"There are {observationsPerVariable:0.##} training rows per fitted predictor, below {MinimumObservationsPerVariable:0}; estimates are likely overfitted and unstable."));
            }

            if (vifs == null || vifs.Any(v => v == null))
            {
                warnings.Add(new LinearWarning(LinearWarningCodes.HighVariance,
                    "Some predictors are perfectly collinear (variance inflation is infinite); their individual coefficients are not identifiable."));
            }
            else
            {
                var high = Enumerable.Range(0, vifs.Length).Where(k => vifs[k]!.Value > HighVarianceInflation)
                    .Select(k => $"{names[active[k]]} ({vifs[k]!.Value:0.#})").ToArray();

                if (high.Length > 0)
                {
                    warnings.Add(new LinearWarning(LinearWarningCodes.HighVariance,
                        $"Variance inflation factors above {HighVarianceInflation:0}: {string.Join(", ", high)}; the coefficients of these predictors share information and are individually unstable."));
                }
            }

            if (table.All(r => r.StandardError == null))
            {
                warnings.Add(new LinearWarning(LinearWarningCodes.UndefinedStandardErrors,
                    "Standard errors could not be computed; the residual degrees of freedom are exhausted or the design is singular."));
            }
            else
            {
                var testable = table.Where(r => r is { Kind: "Predictor", PValue: not null }).ToArray();

                if (testable.Length > 0 && testable.All(r => r.PValue!.Value > SignificanceLevel))
                {
                    warnings.Add(new LinearWarning(LinearWarningCodes.NoSignificantPredictors,
                        $"No predictor has a p-value below {SignificanceLevel}; the model has no statistically supported signal."));
                }
            }
        }

        private static LinearSplitMetrics Metrics(string split, double[] actual, double[] predicted,
            int predictorCount)
        {
            if (actual.Length == 0)
            {
                return null;
            }

            var n = actual.Length;
            var mean = actual.Average();
            var sse = 0d;
            var sst = 0d;
            var sae = 0d;
            var sumError = 0d;

            for (var i = 0; i < n; i++)
            {
                var e = actual[i] - predicted[i];
                sse += e * e;
                sst += (actual[i] - mean) * (actual[i] - mean);
                sae += Math.Abs(e);
                sumError += e;
            }

            double? rSquared = sst > 0 ? 1d - sse / sst : null;
            var denominatorDf = n - predictorCount - 1;

            double? adjustedRSquared = rSquared.HasValue && denominatorDf > 0
                ? 1d - (1d - rSquared.Value) * (n - 1) / denominatorDf
                : null;

            double? residualStandardError = denominatorDf > 0 ? Math.Sqrt(sse / denominatorDf) : null;

            return new LinearSplitMetrics(split, n, rSquared, adjustedRSquared, Math.Sqrt(sse / n), sae / n,
                sumError / n, residualStandardError);
        }
    }
}