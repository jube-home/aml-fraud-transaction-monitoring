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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord;
using Accord.MachineLearning;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Clustering.Models;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    public static class GaussianMixtureClustererTrainer
    {
        public const int MinimumTestRows = 20;
        public const double NearConstantShare = 0.995;

        public static GaussianMixtureClustererResult Train(double[][] inputs, string[] variableNames,
            GaussianMixtureClustererOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new GaussianMixtureClustererOptions();
            options.Validate();
            ValidateData(inputs, variableNames, options);

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

        public static GaussianMixtureClustererResult TrainWithSplit(double[][] inputs, string[] variableNames,
            GaussianMixtureClustererOptions options, DataSplitIndices split,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, variableNames, options);
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

            if (trainX.Length < options.Components)
            {
                throw new ArgumentException(
                    $"The training split has {trainX.Length} rows but {options.Components} components were requested.");
            }

            var fill = new double[variableCount];
            var means = new double[variableCount];
            var deviations = new double[variableCount];
            var missingCounts = new int[variableCount];

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
                    warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to " +
                        "the fit."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the " +
                        "training rows."));
                }
            }

            var standardise = options.Standardise;

            double[] Design(double[] row)
            {
                var design = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                    design[j] = standardise ? Standardisation.ZScore(value, means[j], deviations[j]) : value;
                }

                return design;
            }

            var trainDesign = trainX.Select(Design).ToArray();

            var distinctTrainRows = trainDesign
                .Select(row => string.Join("|", row.Select(v => v.ToString("R", CultureInfo.InvariantCulture))))
                .Distinct()
                .Count();

            if (distinctTrainRows < options.Components)
            {
                throw new ArgumentException(
                    $"The training split has {distinctTrainRows} distinct row(s) but {options.Components} components " +
                    "were requested. Reduce the number of components or provide more varied rows.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var previousSeed = Generator.ThreadSeed;
            GaussianClusterCollection learned;

            var model = new GaussianMixtureModel(options.Components)
            {
                MaxIterations = options.MaximumIterations,
                Tolerance = options.Tolerance,
                ComputeLogLikelihood = true,
                Options = new NormalOptions
                {
                    Diagonal = options.DiagonalCovariance,
                    Regularization = options.Regularization
                },
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            try
            {
                Generator.ThreadSeed = options.Seed;

                try
                {
                    var seedingKMeans = new KMeans(options.Components)
                    {
                        ComputeCovariances = true,
                        ComputeError = true,
                        UseSeeding = Seeding.KMeansPlusPlus,
                        Tolerance = options.Tolerance,
                        ParallelOptions = new ParallelOptions
                            { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
                    };
                    seedingKMeans.Learn(trainDesign);
                    model.Initialize(seedingKMeans);

                    learned = model.Learn(trainDesign);
                }
                catch (NonPositiveDefiniteMatrixException ex)
                {
                    throw new ArgumentException(
                        "The fit produced a non positive definite covariance matrix; increase the regularisation, " +
                        "reduce the number of components, or provide more rows.", ex);
                }
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var iterations = model.Iterations;
            var converged = iterations < options.MaximumIterations;
            var mixtureModel = learned.Model;
            var mixtureWeights = (double[])mixtureModel.Coefficients.Clone();
            var componentMeansDesign = mixtureModel.Components.Select(c => (double[])c.Mean.Clone()).ToArray();

            var componentCovariancesDesign =
                mixtureModel.Components.Select(c => (double[,])c.Covariance.Clone()).ToArray();

            var trainLabels = learned.Decide(trainDesign);

            if (!converged)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureWarningCodes.NonConvergence,
                    $"The fit did not converge within {options.MaximumIterations} iterations " +
                    $"(tolerance {options.Tolerance:g})."));
            }

            var trainXFilled = trainX
                .Select(row => row.Select((v, j) => double.IsFinite(v) ? v : fill[j]).ToArray())
                .ToArray();

            var clusterSummaries = BuildClusterSummaries(options.Components, trainLabels, trainXFilled, mixtureWeights,
                variableCount);

            foreach (var summary in clusterSummaries.Where(s => s.Size == 0))
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureWarningCodes.EmptyComponent,
                    $"Component {summary.Index} received no training rows under its highest posterior."));
            }

            var trainLogLikelihood = model.LogLikelihood;
            double? validationLogLikelihood = null;
            double? testLogLikelihood = null;
            double[][] testDesign = null;

            if (validationX.Length > 0)
            {
                var validationDesign = validationX.Select(Design).ToArray();
                validationLogLikelihood = mixtureModel.LogLikelihood(validationDesign);
            }

            if (testX.Length > 0)
            {
                testDesign = [.. testX.Select(Design)];
                testLogLikelihood = mixtureModel.LogLikelihood(testDesign);
            }

            var trainMeanMaxPosterior = MeanMaxPosterior(trainDesign, mixtureModel, options.Components);

            double? testMeanMaxPosterior = testDesign is { Length: > 0 }
                ? MeanMaxPosterior(testDesign, mixtureModel, options.Components)
                : null;

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out diagnostics; do not promote on training " +
                    "figures alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows; held-out diagnostics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(GaussianMixtureWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training " +
                        $"split, imputed with the median ({fill[j]:g6})."));
                }
            }

            var snapshotComponents = new GaussianMixtureSnapshotComponent[options.Components];

            for (var c = 0; c < options.Components; c++)
            {
                snapshotComponents[c] = new GaussianMixtureSnapshotComponent
                {
                    Index = c,
                    MixtureWeight = mixtureWeights[c],
                    Mean = (double[])componentMeansDesign[c].Clone(),
                    Covariance = AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot
                        .ToJagged(componentCovariancesDesign[c])
                };
            }

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot>
                .Create(GaussianMixtureClustererRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        Components = snapshotComponents
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new GaussianMixtureClustererResult
            {
                Kind = GaussianMixtureClustererRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new GaussianMixtureEvidenceDetails
                {
                    Components = options.Components,
                    DiagonalCovariance = options.DiagonalCovariance,
                    Converged = converged,
                    Iterations = iterations,
                    TrainLogLikelihood = trainLogLikelihood,
                    ValidationLogLikelihood = validationLogLikelihood,
                    TestLogLikelihood = testLogLikelihood,
                    TrainMeanMaxPosterior = trainMeanMaxPosterior,
                    TestMeanMaxPosterior = testMeanMaxPosterior,
                    ClusterSummaries = clusterSummaries
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static GaussianMixtureClusterSummary[] BuildClusterSummaries(int components, int[] labels,
            double[][] originalRows, double[] mixtureWeights, int variableCount)
        {
            var sizes = new int[components];
            var originalSums = new double[components][];
            var originalSumsOfSquares = new double[components][];

            for (var c = 0; c < components; c++)
            {
                originalSums[c] = new double[variableCount];
                originalSumsOfSquares[c] = new double[variableCount];
            }

            for (var i = 0; i < labels.Length; i++)
            {
                var c = labels[i];
                sizes[c]++;

                for (var j = 0; j < variableCount; j++)
                {
                    originalSums[c][j] += originalRows[i][j];
                    originalSumsOfSquares[c][j] += originalRows[i][j] * originalRows[i][j];
                }
            }

            var summaries = new GaussianMixtureClusterSummary[components];

            for (var c = 0; c < components; c++)
            {
                var mean = new double[variableCount];
                var variance = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    if (sizes[c] == 0)
                    {
                        mean[j] = double.NaN;
                        variance[j] = double.NaN;
                        continue;
                    }

                    mean[j] = originalSums[c][j] / sizes[c];

                    variance[j] = sizes[c] < 2
                        ? 0d
                        : Math.Max(0d, originalSumsOfSquares[c][j] / sizes[c] - mean[j] * mean[j]);
                }

                summaries[c] = new GaussianMixtureClusterSummary
                {
                    Index = c,
                    Size = sizes[c],
                    MixtureWeight = mixtureWeights[c],
                    Mean = mean,
                    Variance = variance
                };
            }

            return summaries;
        }

        private static double MeanMaxPosterior(double[][] rows,
            MultivariateMixture<MultivariateNormalDistribution> model, int components)
        {
            if (rows.Length == 0)
            {
                return 0d;
            }

            var total = 0d;

            foreach (var row in rows)
            {
                var jointLog = new double[components];

                for (var c = 0; c < components; c++)
                {
                    jointLog[c] = model.LogProbabilityDensityFunction(c, row);
                }

                var max = jointLog.Max();

                if (double.IsNegativeInfinity(max))
                {
                    total += 1d / components;
                    continue;
                }

                var sumExp = jointLog.Sum(v => Math.Exp(v - max));
                total += 1d / sumExp;
            }

            return total / rows.Length;
        }

        private static void ValidateData(double[][] inputs, string[] variableNames,
            GaussianMixtureClustererOptions options)
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
                        $"Row {i} has {inputs[i].Length} values but there are {variableNames.Length} variable names " +
                        "(rows must all be the same length).");
                }
            }

            if (inputs.Length < options.Components)
            {
                throw new ArgumentException(
                    $"There are {inputs.Length} rows but {options.Components} components were requested.");
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