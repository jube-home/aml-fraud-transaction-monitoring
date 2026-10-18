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
using Accord.MachineLearning.Clustering;
using Accord.Math.Decompositions;
using Accord.Statistics;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    [AdvancedAnalyticsToolGroup("MultivariateAndClustering")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class DiscriminantAndEmbeddingTools
    {
        [AdvancedAnalyticsTool("RunLinearDiscriminantAnalysis")]
        [Description(
            "Runs linear discriminant analysis: it finds the directions along which known classes are best separated (large between class spread relative to within class spread) and returns them with each row's coordinates. " +
            "There are at most classes minus 1 useful directions. It reports class sizes, prevalence, class means, the eigenvalue of each direction (bigger means more separation; the between class scatter is weighted equally per class by the method), each direction's share of separation and running total, the direction vectors, the class means in the projected space, the projected rows and the training accuracy of assigning every row to the nearest projected class mean. " +
            "Training accuracy is optimistic because it is measured on the same rows; use it to see separability, not as a performance estimate. Needs at least 2 classes with at least 2 rows each, more rows than classes, and columns that are not perfectly collinear within classes. Labels can be any integers. At most 20000 rows.")]
        public DiscriminantAnalysisResult RunLinearDiscriminantAnalysis(
            [Description("Rows of finite numbers, all the same length. At most 20000 rows.")]
            double[][] data,
            [Description(
                "One integer class label per row, in row order; at least 2 distinct labels with at least 2 rows each.")]
            int[] labels,
            [Description(
                "How many discriminant directions to keep, from 1 up to min(classes minus 1, columns). 0 (default) keeps the maximum.")]
            int components = 0,
            [Description("Whether to return the projected coordinates of every row. Default true.")]
            bool returnScores = true)
        {
            MultivariateGuard.Matrix(data, nameof(data), 4, MultivariateLimits.MaximumProjectionRows);
            MultivariateGuard.Labels(labels, nameof(labels), data.Length, nameof(data));
            var (mapped, distinct) = MapLabels(labels, nameof(labels));
            var maximum = Math.Min(distinct.Length - 1, data[0].Length);
            var k = components == 0 ? maximum : MultivariateGuard.Bounded(components, nameof(components), 1, maximum);

            var analysis = new LinearDiscriminantAnalysis { NumberOfOutputs = k };
            var pipeline = analysis.Learn(data, mapped);
            RequireFinite(analysis.Eigenvalues, nameof(data));

            var scores = Slice(analysis.Transform(data), k);
            var vectors = analysis.DiscriminantVectors.Take(k).Select(v => (double[])v.Clone()).ToArray();
            var projected = analysis.Classes.Select(c => c.ProjectionMean.Take(k).ToArray()).ToArray();
            NormaliseDiscriminantSigns(vectors, scores, projected);

            var predicted = pipeline.Decide(data);
            var correct = predicted.Where((p, i) => p == mapped[i]).Count();

            return new DiscriminantAnalysisResult(
                data.Length,
                data[0].Length,
                distinct.Length,
                distinct,
                analysis.Classes.Select(c => c.Count).ToArray(),
                analysis.Classes.Select(c => c.Prevalence).ToArray(),
                analysis.Classes.Select(c => c.Mean).ToArray(),
                analysis.Eigenvalues.Take(k).ToArray(),
                analysis.DiscriminantProportions.Take(k).ToArray(),
                analysis.CumulativeProportions.Take(k).ToArray(),
                vectors,
                projected,
                returnScores ? scores : Array.Empty<double[]>(),
                predicted.Select(p => distinct[p]).ToArray(),
                correct / (double)data.Length);
        }

        [AdvancedAnalyticsTool("RunKernelDiscriminantAnalysis")]
        [Description(
            "Runs kernel discriminant analysis: linear discriminant analysis carried out in the feature space of a kernel, so classes that are separated by a curved boundary can be pulled apart. " +
            "It returns the eigenvalue of each direction, its share of separation, the projected coordinates of every row, the classes' projected means and the training accuracy of assigning each row to the nearest projected class mean; class means and direction vectors live in the kernel space and are returned empty. " +
            "Accuracy is measured on the training rows and is optimistic, so read it as separability. The regularisation term keeps the within class matrix invertible; raise it if the call reports a singular matrix. At most 1000 rows because the kernel matrix has rows squared cells; at most min(classes minus 1, columns) directions are returned.")]
        public DiscriminantAnalysisResult RunKernelDiscriminantAnalysis(
            [Description("Rows of finite numbers, all the same length. At most 1000 rows.")]
            double[][] data,
            [Description(
                "One integer class label per row, in row order; at least 2 distinct labels with at least 2 rows each.")]
            int[] labels,
            [Description(KernelText.Kernel)] SimilarityKernelKind kernel = SimilarityKernelKind.Gaussian,
            [Description(KernelText.Sigma)] double kernelSigma = 1,
            [Description(KernelText.Gamma)] double kernelGamma = 1,
            [Description(KernelText.Constant)] double? kernelConstant = null,
            [Description(KernelText.Degree)] int kernelDegree = 2,
            [Description(KernelText.Alpha)] double kernelAlpha = 1,
            [Description(KernelText.Beta)] double kernelBeta = 1,
            [Description(
                "How many directions to keep, from 1 up to min(classes minus 1, columns). 0 (default) keeps the maximum.")]
            int components = 0,
            [Description("Ridge added to the within class matrix, at least 0. Default 0.0001.")]
            double regularization = 1e-4,
            [Description("Whether to return the projected coordinates of every row. Default true.")]
            bool returnScores = true,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 4, MultivariateLimits.MaximumKernelRows);
            MultivariateGuard.Labels(labels, nameof(labels), data.Length, nameof(data));
            MultivariateGuard.BoundedDouble(regularization, nameof(regularization), 0, 1e6);
            var (mapped, distinct) = MapLabels(labels, nameof(labels));
            var maximum = Math.Min(distinct.Length - 1, data[0].Length);
            var k = components == 0 ? maximum : MultivariateGuard.Bounded(components, nameof(components), 1, maximum);

            var function = KernelFactory.Create(kernel, kernelSigma, kernelGamma, kernelConstant, kernelDegree,
                kernelAlpha, kernelBeta, data[0].Length);

            cancellationToken.ThrowIfCancellationRequested();

            var analysis = new KernelDiscriminantAnalysis(function)
            {
                NumberOfOutputs = k,
                Regularization = regularization,
                Token = cancellationToken
            };

            KernelDiscriminantAnalysis.Pipeline pipeline;

            try
            {
                pipeline = analysis.Learn(data, mapped);
            }
            catch (SingularMatrixException)
            {
                throw new ArgumentException(
                    "The within class matrix is singular. Raise regularization (for example to 0.01) or change the kernel parameters.",
                    nameof(regularization));
            }

            RequireFinite(analysis.Eigenvalues, nameof(data));
            var scores = Slice(analysis.Transform(data), k);
            var projected = analysis.Classes.Select(c => c.ProjectionMean.Take(k).ToArray()).ToArray();
            NormaliseDiscriminantSigns(Array.Empty<double[]>(), scores, projected);

            var predicted = pipeline.Decide(data);
            var correct = predicted.Where((p, i) => p == mapped[i]).Count();

            return new DiscriminantAnalysisResult(
                data.Length,
                data[0].Length,
                distinct.Length,
                distinct,
                analysis.Classes.Select(c => c.Count).ToArray(),
                analysis.Classes.Select(c => c.Prevalence).ToArray(),
                Array.Empty<double[]>(),
                analysis.Eigenvalues.Take(k).ToArray(),
                analysis.DiscriminantProportions.Take(k).ToArray(),
                analysis.CumulativeProportions.Take(k).ToArray(),
                Array.Empty<double[]>(),
                projected,
                returnScores ? scores : Array.Empty<double[]>(),
                predicted.Select(p => distinct[p]).ToArray(),
                correct / (double)data.Length);
        }

        [AdvancedAnalyticsTool("ClassifyWithLinearDiscriminant")]
        [Description(
            "Learns a linear discriminant model from labelled training rows and assigns each new row to the class whose projected mean is nearest, without saving anything. " +
            "It returns the predicted label per new row and, per row, one score per class in the order of ClassLabels (the negative squared distance to that class mean in the discriminant space, so larger means a better fit). " +
            "Use it as a quick, interpretable baseline classifier or to place new cases among known groups. Training needs at least 2 classes with at least 2 rows each.")]
        public DiscriminantClassificationResult ClassifyWithLinearDiscriminant(
            [Description("Training rows of finite numbers, all the same length. At most 20000 rows.")]
            double[][] trainingData,
            [Description("One integer class label per training row.")]
            int[] trainingLabels,
            [Description("Rows to classify, with the same number of columns as trainingData. At most 20000 rows.")]
            double[][] newData,
            [Description(
                "How many discriminant directions to use, 1 up to min(classes minus 1, columns). 0 (default) uses the maximum.")]
            int components = 0)
        {
            MultivariateGuard.Matrix(trainingData, nameof(trainingData), 4, MultivariateLimits.MaximumProjectionRows);
            MultivariateGuard.Labels(trainingLabels, nameof(trainingLabels), trainingData.Length, nameof(trainingData));
            MultivariateGuard.Matrix(newData, nameof(newData), 1, MultivariateLimits.MaximumProjectionRows);
            ProjectionTools.RequireSameColumns(trainingData, newData, nameof(newData));
            var (mapped, distinct) = MapLabels(trainingLabels, nameof(trainingLabels));
            var maximum = Math.Min(distinct.Length - 1, trainingData[0].Length);
            var k = components == 0 ? maximum : MultivariateGuard.Bounded(components, nameof(components), 1, maximum);

            var analysis = new LinearDiscriminantAnalysis { NumberOfOutputs = k };
            var pipeline = analysis.Learn(trainingData, mapped);
            RequireFinite(analysis.Eigenvalues, nameof(trainingData));

            return new DiscriminantClassificationResult(newData.Length, distinct,
                pipeline.Decide(newData).Select(p => distinct[p]).ToArray(), pipeline.Scores(newData));
        }

        [AdvancedAnalyticsTool("DetectMultivariateOutliersMahalanobis")]
        [Description(
            "Flags rows that are unusual as a combination of variables using the Mahalanobis distance, which measures how many standard deviations a row lies from the centre while accounting for the correlations between variables (so a case can be an outlier because two normally correlated variables disagree). " +
            "For each row it returns the distance, its square, and a p-value from the chi-square distribution with one degree of freedom per independent dimension. The cutoff is the distance whose chi-square tail probability equals the significance level; rows above it are flagged. " +
            "The centre and covariance are estimated from the same rows, so this is the classical (not robust) estimate: several extreme rows can inflate the covariance and hide each other, and small samples make the chi-square approximation loose. Rank is the number of independent dimensions found; when columns are perfectly collinear a pseudo inverse is used and Rank is lower than the column count. Needs at least 3 rows, at most 20000.")]
        public MahalanobisOutlierResult DetectMultivariateOutliersMahalanobis(
            [Description("Rows of finite numbers, all the same length. At least 3 rows, at most 20000.")]
            double[][] data,
            [Description(
                "Tail probability that defines the cutoff, between 0.0000001 and 0.5. Default 0.001 (flags rows beyond the 99.9th percentile).")]
            double significance = 0.001)
        {
            MultivariateGuard.Matrix(data, nameof(data), 3, MultivariateLimits.MaximumProjectionRows);
            MultivariateGuard.BoundedDouble(significance, nameof(significance), 1e-7, 0.5);

            return MahalanobisAgainst(data, data, significance);
        }

        [AdvancedAnalyticsTool("ScoreMahalanobisDistanceAgainstReference")]
        [Description(
            "Measures how unusual new rows are compared with a reference population using the Mahalanobis distance: the centre and covariance are learned from the reference rows only and every new row is scored against them, which avoids the masking problem of scoring rows against a set they contaminate. " +
            "It returns, per new row, the distance, squared distance, chi-square p-value and whether the distance exceeds the cutoff for the chosen significance. Use it to test new transactions or customers against a clean history. Reference needs at least 3 rows; both sets must have the same number of columns; at most 20000 rows each.")]
        public MahalanobisOutlierResult ScoreMahalanobisDistanceAgainstReference(
            [Description("Reference rows that define the normal population. At least 3 rows, at most 20000.")]
            double[][] referenceData,
            [Description("Rows to score, with the same number of columns as referenceData. At most 20000.")]
            double[][] newData,
            [Description("Tail probability that defines the cutoff, between 0.0000001 and 0.5. Default 0.001.")]
            double significance = 0.001)
        {
            MultivariateGuard.Matrix(referenceData, nameof(referenceData), 3, MultivariateLimits.MaximumProjectionRows);
            MultivariateGuard.Matrix(newData, nameof(newData), 1, MultivariateLimits.MaximumProjectionRows);
            ProjectionTools.RequireSameColumns(referenceData, newData, nameof(newData));
            MultivariateGuard.BoundedDouble(significance, nameof(significance), 1e-7, 0.5);

            return MahalanobisAgainst(referenceData, newData, significance);
        }

        [AdvancedAnalyticsTool("EmbedWithTsne")]
        [Description(
            "Embeds high dimensional rows into 2 or 3 dimensions with t-SNE so clusters and structure can be seen or plotted: nearby rows stay nearby, but distances between far apart groups and cluster sizes are not meaningful. " +
            "Perplexity is roughly the number of neighbours each point considers and must be below one third of (rows minus 1); typical values are 5 to 50. Theta trades speed for accuracy in the Barnes-Hut approximation (0 is exact and slow, 0.5 is the usual choice). The optimisation runs a fixed 1000 iterations from a random start controlled by seed, so a fixed seed gives repeatable coordinates. " +
            "At most 2000 rows and 500 columns. Different seeds give different but equally valid layouts, so compare structure, not coordinates.")]
        public EmbeddingResult EmbedWithTsne(
            [Description("Rows of finite numbers, all the same length. At least 10 rows, at most 2000.")]
            double[][] data,
            [Description("Number of output dimensions, 2 (default) or 3.")]
            int dimensions = 2,
            [Description(
                "Effective neighbourhood size, above 1 and below (rows minus 1) divided by 3. Default 30, reduced automatically to fit small samples.")]
            double perplexity = 30,
            [Description(
                "Barnes-Hut accuracy parameter between 0 and 1; 0 is exact but slow for large inputs. Default 0.5.")]
            double theta = 0.5,
            [Description("Seed for the random starting layout; reuse it to repeat a run. Default 1.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 10, MultivariateLimits.MaximumEmbeddingRows);
            MultivariateGuard.Bounded(dimensions, nameof(dimensions), 2, 3);
            MultivariateGuard.BoundedDouble(theta, nameof(theta), 0, 1);
            MultivariateGuard.BoundedDouble(perplexity, nameof(perplexity), 1.0001, 1000);

            var limit = (data.Length - 1) / 3.0 - 1e-9;
            var effective = Math.Min(perplexity, limit);

            if (effective <= 1)
            {
                throw new ArgumentException(
                    $"data has {data.Length} rows which is too few for t-SNE; provide at least 10 rows.", nameof(data));
            }

            using var scope = new SeededRandomScope(seed);
            cancellationToken.ThrowIfCancellationRequested();

            var tsne = new TSNE { Perplexity = effective, Theta = theta, NumberOfOutputs = dimensions };
            var coordinates = tsne.Transform(MultivariateGuard.Copy(data));

            return new EmbeddingResult(data.Length, dimensions, effective, theta, coordinates);
        }

        private static MahalanobisOutlierResult MahalanobisAgainst(double[][] reference, double[][] rows,
            double significance)
        {
            var centre = reference.Mean(0);
            var covariance = reference.Covariance(centre);
            var svd = new JaggedSingularValueDecomposition(covariance, true, true, false);
            var rank = svd.Rank;

            if (rank < 1)
            {
                throw new ArgumentException(
                    "The reference rows have no variation in any column, so distances are undefined.",
                    "data");
            }

            var precision = svd.Inverse();
            var d2 = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var delta = new double[centre.Length];

                for (var j = 0; j < centre.Length; j++)
                {
                    delta[j] = rows[i][j] - centre[j];
                }

                var sum = 0.0;

                for (var a = 0; a < delta.Length; a++)
                {
                    var inner = 0.0;

                    for (var b = 0; b < delta.Length; b++)
                    {
                        inner += precision[a][b] * delta[b];
                    }

                    sum += delta[a] * inner;
                }

                d2[i] = Math.Max(0.0, sum);
            }

            var distribution = new ChiSquareDistribution(rank);
            var squaredCutoff = distribution.InverseDistributionFunction(1.0 - significance);
            var flags = d2.Select(v => v > squaredCutoff).ToArray();

            return new MahalanobisOutlierResult(
                rows.Length,
                centre.Length,
                rank,
                centre,
                d2.Select(Math.Sqrt).ToArray(),
                d2,
                d2.Select(v => distribution.ComplementaryDistributionFunction(v)).ToArray(),
                Math.Sqrt(squaredCutoff),
                squaredCutoff,
                flags,
                Enumerable.Range(0, flags.Length).Where(i => flags[i]).ToArray(),
                flags.Count(f => f));
        }

        private static (int[] Mapped, int[] Distinct) MapLabels(int[] labels, string name)
        {
            var distinct = labels.Distinct().OrderBy(v => v).ToArray();

            if (distinct.Length < 2)
            {
                throw new ArgumentException(
                    $"{name} holds only {distinct.Length} distinct label(s); discriminant analysis needs at least 2 classes.",
                    name);
            }

            var index = distinct.Select((label, i) => (label, i)).ToDictionary(t => t.label, t => t.i);

            foreach (var group in labels.GroupBy(v => v))
            {
                if (group.Count() < 2)
                {
                    throw new ArgumentException(
                        $"Label {group.Key} has only one row; every class needs at least 2 rows so its spread can be measured.",
                        name);
                }
            }

            if (labels.Length <= distinct.Length)
            {
                throw new ArgumentException($"{name} needs more rows than classes.", name);
            }

            return (labels.Select(v => index[v]).ToArray(), distinct);
        }

        private static double[][] Slice(double[][] matrix, int columns)
        {
            return matrix.Select(row => row.Take(columns).ToArray()).ToArray();
        }

        private static void RequireFinite(double[] values, string name)
        {
            if (values.Length == 0 || values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new ArgumentException(
                    "The within class scatter is singular or the classes are perfectly separable, so no stable discriminant exists. Remove collinear or constant columns, or provide more rows per class.",
                    name);
            }
        }

        private static void NormaliseDiscriminantSigns(double[][] vectors, double[][] scores, double[][] projectedMeans)
        {
            var count = scores.Length > 0 ? scores[0].Length : 0;

            for (var c = 0; c < count; c++)
            {
                var largest = 0.0;

                if (vectors.Length > c)
                {
                    foreach (var value in vectors[c])
                    {
                        if (Math.Abs(value) > Math.Abs(largest))
                        {
                            largest = value;
                        }
                    }
                }
                else
                {
                    foreach (var row in scores)
                    {
                        if (Math.Abs(row[c]) > Math.Abs(largest))
                        {
                            largest = row[c];
                        }
                    }
                }

                if (largest >= 0)
                {
                    continue;
                }

                if (vectors.Length > c)
                {
                    for (var j = 0; j < vectors[c].Length; j++)
                    {
                        vectors[c][j] = -vectors[c][j];
                    }
                }

                foreach (var row in scores)
                {
                    row[c] = -row[c];
                }

                foreach (var mean in projectedMeans)
                {
                    mean[c] = -mean[c];
                }
            }
        }
    }
}