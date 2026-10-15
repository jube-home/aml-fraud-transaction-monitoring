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
using System.Threading.Tasks;
using Accord;
using Accord.MachineLearning;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Density.Models;

namespace Jube.Engine.AdvancedAnalytics.Density
{
    public static class GaussianMixtureDensityTrainer
    {
        public const int MinimumTestRows = 20;
        public const double NearConstantShare = 0.995;
        public const double MinimumRowsPerComponentRatio = 5d;

        public static GaussianMixtureDensityResult Train(double[][] inputs, string[] variableNames,
            GaussianMixtureDensityOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new GaussianMixtureDensityOptions();
            options.Validate();
            ValidateData(inputs, variableNames);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;
            var dummyLabels = new double[inputs.Length];

            var split = DataSplit.Split(dummyLabels, trainFraction, options.ValidationFraction, options.TestFraction,
                options.Seed, false);

            var assigned = new bool[inputs.Length];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[row] = true;
            }

            var leftovers = Enumerable.Range(0, inputs.Length).Where(i => !assigned[i]).ToArray();

            if (leftovers.Length > 0)
            {
                split = new DataSplitIndices([.. split.Train, .. leftovers], split.Validation, split.Test);
            }

            return TrainWithSplit(inputs, variableNames, options, split, cancellationToken);
        }

        public static GaussianMixtureDensityResult TrainWithSplit(double[][] inputs, string[] variableNames,
            GaussianMixtureDensityOptions options, DataSplitIndices split,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, variableNames);
            ValidateSplit(split, inputs.Length);
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = new List<AdvancedAnalyticsWarning>();
            var variableCount = variableNames.Length;

            var trainRows = split.Train;
            var validationRows = split.Validation;
            var testRows = split.Test;

            var trainX = DataSplit.Take(inputs, trainRows);
            var validationX = DataSplit.Take(inputs, validationRows);
            var testX = DataSplit.Take(inputs, testRows);

            if (trainX.Length < 2)
            {
                throw new ArgumentException("The training split has fewer than two rows.");
            }

            var fill = new double[variableCount];
            var means = new double[variableCount];
            var deviations = new double[variableCount];
            var excluded = new string[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var column = new double[trainX.Length];

                for (var i = 0; i < column.Length; i++)
                {
                    column[i] = trainX[i][j];
                }

                var finite = column.Where(double.IsFinite).ToArray();

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

                    warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureDensityWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split and was excluded from the mixture."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureDensityWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows; its contribution to the density will be unstable."));
                }
            }

            var activeInputs = Enumerable.Range(0, variableCount).Where(j => excluded[j] == null).ToArray();

            if (activeInputs.Length == 0)
            {
                throw new ArgumentException(
                    "Every variable is constant in the training split; there is nothing to fit.");
            }

            var standardise = options.Standardise;

            double[] Design(double[] row)
            {
                var design = new double[activeInputs.Length];

                for (var k = 0; k < activeInputs.Length; k++)
                {
                    var j = activeInputs[k];
                    var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                    design[k] = standardise ? Standardisation.ZScore(value, means[j], deviations[j]) : value;
                }

                return design;
            }

            var trainDesign = trainX.Select(Design).ToArray();
            cancellationToken.ThrowIfCancellationRequested();

            var d = activeInputs.Length;
            var minimumRows = options.Components * (d + 1);

            if (trainDesign.Length < minimumRows)
            {
                throw new ArgumentException(
                    $"The training split has {trainDesign.Length} row(s) for {options.Components} component(s) over " +
                    $"{d} active variable(s); at least {minimumRows} rows are needed to fit a mean and covariance per " +
                    "component. Request fewer components or provide more rows.");
            }

            var rowsPerComponent = trainDesign.Length / (double)options.Components;

            if (rowsPerComponent < MinimumRowsPerComponentRatio * (d + 1))
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureDensityWarningCodes.LowRowsPerComponent,
                    $"There are {rowsPerComponent:0.#} training rows per component, below " +
                    $"{MinimumRowsPerComponentRatio * (d + 1):0}; the fitted covariances are likely unstable."));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var previousSeed = Generator.ThreadSeed;
            Generator.ThreadSeed = options.Seed;

            GaussianClusterCollection clusters;
            int iterations;

            try
            {
                var model = new GaussianMixtureModel(options.Components)
                {
                    Options = new NormalOptions { Regularization = options.RegularizationEpsilon },
                    MaxIterations = options.MaxIterations,
                    Tolerance = options.Tolerance,
                    ComputeLogLikelihood = true,
                    ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 }
                };

                try
                {
                    clusters = model.Learn(trainDesign);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                                               or NonPositiveDefiniteMatrixException or AggregateException)
                {
                    throw new ArgumentException(
                        $"The mixture could not be fitted: {ex.Message} Request fewer components, more rows or a " +
                        "different seed.", ex);
                }

                iterations = model.Iterations;
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }

            var converged = options.MaxIterations <= 0 || iterations < options.MaxIterations;

            if (!converged)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureDensityWarningCodes.NonConvergence,
                    $"The fit did not converge within {options.MaxIterations} iterations (tolerance {options.Tolerance:g}); treat the mixture as unreliable."));
            }

            var weights = clusters.Proportions;
            var componentMeans = clusters.Means;
            var componentCovariances = clusters.Covariance;
            var components = new GaussianMixtureDensityComponent[options.Components];
            var snapshotComponents = new GaussianMixtureDensitySnapshotComponent[options.Components];
            var anyRegularised = false;

            for (var k = 0; k < options.Components; k++)
            {
                var covariance = ToJagged(componentCovariances[k]);
                var inverse = SmallLinearAlgebra.Inverse(covariance);

                if (inverse == null)
                {
                    anyRegularised = true;

                    var trace = 0d;

                    for (var a = 0; a < d; a++)
                    {
                        trace += covariance[a][a];
                    }

                    var averageVariance = Math.Max(trace / d, 1e-12);

                    for (var a = 0; a < d; a++)
                    {
                        covariance[a][a] += options.RegularizationEpsilon * averageVariance;
                    }

                    inverse = SmallLinearAlgebra.Inverse(covariance);

                    if (inverse == null)
                    {
                        throw new InvalidOperationException(
                            $"The covariance matrix of component {k} is singular and could not be regularised; " +
                            "reduce the component count or the variables, or provide more rows.");
                    }
                }

                var cholesky = SmallLinearAlgebra.Cholesky(covariance) ??
                               throw new InvalidOperationException(
                                   $"The covariance matrix of component {k} could not be decomposed after regularisation.");

                var logDeterminant = 0d;

                for (var a = 0; a < d; a++)
                {
                    logDeterminant += 2d * Math.Log(cholesky[a][a]);
                }

                components[k] = new GaussianMixtureDensityComponent(weights[k], (double[])componentMeans[k].Clone(),
                    inverse, logDeterminant);

                snapshotComponents[k] = new GaussianMixtureDensitySnapshotComponent
                {
                    Weight = weights[k],
                    Mean = (double[])componentMeans[k].Clone(),
                    InverseCovariance = inverse,
                    LogDeterminant = logDeterminant
                };
            }

            if (anyRegularised)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureDensityWarningCodes.CovarianceRegularised,
                    $"At least one component's training covariance was singular; a ridge of " +
                    $"{options.RegularizationEpsilon:g} times its average variance was added to its diagonal before it " +
                    "could be inverted."));
            }

            var pipeline = new GaussianMixtureDensityPipeline(standardise, fill, means, deviations, activeInputs,
                components);

            double MeanLogDensity(double[][] rows)
            {
                var total = 0d;

                for (var i = 0; i < rows.Length; i++)
                {
                    total += pipeline.LogDensity(rows[i]);
                }

                return total / rows.Length;
            }

            var trainMeanLogLikelihood = MeanLogDensity(trainX);
            var validationMeanLogLikelihood = validationX.Length > 0 ? MeanLogDensity(validationX) : (double?)null;
            var testMeanLogLikelihood = testX.Length > 0 ? MeanLogDensity(testX) : (double?)null;

            var perComponentParameters = d + d * (d + 1) / 2 + 1;
            var parameterCount = options.Components * perComponentParameters - 1;
            var totalTrainLogLikelihood = trainMeanLogLikelihood * trainX.Length;
            var bic = -2d * totalTrainLogLikelihood + parameterCount * Math.Log(trainX.Length);
            var aic = -2d * totalTrainLogLikelihood + 2d * parameterCount;

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureDensityWarningCodes.NoTestSet,
                    "There is no test split, so there is no held-out log-likelihood check; do not promote on training diagnostics alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureDensityWarningCodes.TinyTestSet,
                    $"The test split has only {testX.Length} rows; the held-out log-likelihood will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    excluded[j] != null, excluded[j]);
            }

            var componentSummaries = new GaussianMixtureDensityComponentSummary[options.Components];

            for (var k = 0; k < options.Components; k++)
            {
                componentSummaries[k] = new GaussianMixtureDensityComponentSummary(weights[k],
                    (double[])componentMeans[k].Clone(), ToJagged(componentCovariances[k]));
            }

            var details = new GaussianMixtureDensityEvidenceDetails
            {
                Components = options.Components,
                ComponentSummaries = componentSummaries,
                TrainMeanLogLikelihood = trainMeanLogLikelihood,
                ValidationMeanLogLikelihood = validationMeanLogLikelihood,
                TestMeanLogLikelihood = testMeanLogLikelihood,
                Bic = bic,
                Aic = aic,
                ParameterCount = parameterCount,
                Converged = converged,
                Iterations = iterations
            };

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsGaussianMixtureDensityPipelineSnapshot>
                .Create(
                    GaussianMixtureDensityRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsGaussianMixtureDensityPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        ActiveInputs = activeInputs,
                        Components = snapshotComponents
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new GaussianMixtureDensityResult
            {
                Kind = GaussianMixtureDensityRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                Details = details,
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static double[][] ToJagged(double[,] matrix)
        {
            var n = matrix.GetLength(0);
            var jagged = new double[n][];

            for (var i = 0; i < n; i++)
            {
                jagged[i] = new double[n];

                for (var j = 0; j < n; j++)
                {
                    jagged[i][j] = matrix[i, j];
                }
            }

            return jagged;
        }

        private static void ValidateData(double[][] inputs, string[] variableNames)
        {
            ArgumentNullException.ThrowIfNull(inputs);
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

            if (inputs.Length < 2)
            {
                throw new ArgumentException("At least two rows are needed.");
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