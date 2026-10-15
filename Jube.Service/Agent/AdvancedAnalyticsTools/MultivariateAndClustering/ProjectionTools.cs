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
using Accord.Statistics;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    [AdvancedAnalyticsToolGroup("MultivariateAndClustering")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ProjectionTools
    {
        [AdvancedAnalyticsTool("RunPrincipalComponentAnalysis")]
        [Description(
            "Runs principal component analysis on numeric rows: it finds the orthogonal directions that capture the most variance and returns eigenvalues (variance per component), explained variance ratios and their running total, loadings (the unit length direction of each component in terms of the original variables) and optionally the scores (each row's coordinates on the components). " +
            "Use Correlation (default) when variables are on different scales, Covariance when they share units. Scaled loadings are loadings times the square root of the eigenvalue; for Correlation they are the correlation of each variable with each component, and communalities (per variable) are the share of its variance the returned components explain. " +
            "The ComponentsForNPercent fields say how many components reach that cumulative share, and KaiserComponentCount counts eigenvalues above their mean (above 1 for Correlation). Sign is normalised so each loading vector's largest entry is positive. " +
            "Needs at least 2 rows; at most 20000 rows and 500 columns; constant columns are rejected for Correlation. With more columns than rows only min(rows, columns) components exist.")]
        public PrincipalComponentAnalysisResult RunPrincipalComponentAnalysis(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 20000.")]
            double[][] data,
            [Description(
                "Correlation standardises every variable first (scale free, default); Covariance works on centred data in original units.")]
            PrincipalComponentBasis basis = PrincipalComponentBasis.Correlation,
            [Description(
                "How many components to return loadings and scores for, from 1 up to the number available. 0 (default) returns all of them.")]
            int components = 0,
            [Description(
                "Whether to return the score of every row on the components. Default true; set false to save space.")]
            bool returnScores = true)
        {
            var fit = PrincipalComponentFit.Create(data, nameof(data), basis);
            var k = fit.ResolveComponents(components);

            return fit.ToResult(data, k, returnScores);
        }

        [AdvancedAnalyticsTool("InterpretPrincipalComponents")]
        [Description(
            "Names what each principal component means by listing, for the first few components, the original variables with the largest absolute loadings, plus each variable's communality. " +
            "Use it after RunPrincipalComponentAnalysis to explain components in words: a component with large positive loadings on 'amount' and 'count' is a volume component. " +
            "Loading is the eigenvector weight (unit length per component); SquaredLoadingShare is its square, the share of that component's direction the variable holds. Communalities give the share of each variable's variance captured by the components listed. Variable names are optional and default to V0, V1 and so on.")]
        public PrincipalComponentInterpretationResult InterpretPrincipalComponents(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 20000.")]
            double[][] data,
            [Description("One unique name per column, in column order. Leave empty to use V0, V1 and so on.")]
            string[]? variableNames = null,
            [Description("Correlation (default) or Covariance basis; see RunPrincipalComponentAnalysis.")]
            PrincipalComponentBasis basis = PrincipalComponentBasis.Correlation,
            [Description(
                "How many leading components to describe, at least 1; fewer are described if fewer exist. Default 3.")]
            int components = 3,
            [Description("How many variables to list per component, ordered by absolute loading. Default 5.")]
            int topVariables = 5)
        {
            var fit = PrincipalComponentFit.Create(data, nameof(data), basis);
            var names = MultivariateGuard.Names(variableNames, nameof(variableNames), data[0].Length);
            MultivariateGuard.Bounded(components, nameof(components), 1, MultivariateLimits.MaximumColumns);
            var k = Math.Min(components, fit.Available);
            MultivariateGuard.Bounded(topVariables, nameof(topVariables), 1, MultivariateLimits.MaximumColumns);

            var described = new ComponentInterpretation[k];

            for (var c = 0; c < k; c++)
            {
                var loadings = fit.Loadings[c];

                var top = Enumerable.Range(0, loadings.Length)
                    .OrderByDescending(j => Math.Abs(loadings[j]))
                    .ThenBy(j => j)
                    .Take(topVariables)
                    .Select(j => new ComponentVariableLoading(names[j], loadings[j], loadings[j] * loadings[j]))
                    .ToArray();

                described[c] = new ComponentInterpretation(c, fit.Eigenvalues[c], fit.Proportions[c], top);
            }

            return new PrincipalComponentInterpretationResult(data.Length, basis.ToString(), described, names,
                fit.Communalities(k));
        }

        [AdvancedAnalyticsTool("ProjectOntoPrincipalComponents")]
        [Description(
            "Learns principal components from a reference data set and projects other rows onto them, returning one score vector per new row. " +
            "The new rows are centred (and scaled for Correlation) with the reference means and standard deviations, so scores are comparable with those of the reference. Use it to place new cases in a component space learned from history. " +
            "Reference needs at least 2 rows; both sets must have the same number of columns.")]
        public double[][] ProjectOntoPrincipalComponents(
            [Description("Reference rows used to learn the components. At least 2 rows, at most 20000.")]
            double[][] referenceData,
            [Description("Rows to project, with the same number of columns as referenceData. At most 20000.")]
            double[][] newData,
            [Description("Correlation (default) or Covariance basis.")]
            PrincipalComponentBasis basis = PrincipalComponentBasis.Correlation,
            [Description("How many components to score on, 1 up to the number available. 0 (default) uses all.")]
            int components = 0)
        {
            var fit = PrincipalComponentFit.Create(referenceData, nameof(referenceData), basis);
            MultivariateGuard.Matrix(newData, nameof(newData), 1, MultivariateLimits.MaximumProjectionRows);
            RequireSameColumns(referenceData, newData, nameof(newData));

            return fit.Score(newData, fit.ResolveComponents(components));
        }

        [AdvancedAnalyticsTool("WhitenDataWithPca")]
        [Description(
            "Decorrelates and rescales data so that the returned columns are uncorrelated and each has variance 1 (whitening): rows are projected on the principal components and every score column is divided by the square root of its eigenvalue. " +
            "Use it before methods that assume spherical data. Components whose eigenvalue is below 1e-12 of the largest are dropped because dividing by them would amplify noise, so the number of returned columns can be smaller than the number of variables for collinear data. ColumnStandardDeviations confirms the result is 1 for each returned column.")]
        public WhiteningResult WhitenDataWithPca(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 20000.")]
            double[][] data,
            [Description("Correlation (default) standardises first; Covariance only centres.")]
            PrincipalComponentBasis basis = PrincipalComponentBasis.Correlation,
            [Description("Maximum number of components to keep. 0 (default) keeps every non negligible one.")]
            int components = 0)
        {
            var fit = PrincipalComponentFit.Create(data, nameof(data), basis);
            var usable = fit.Eigenvalues.Count(e => e > fit.Eigenvalues[0] * 1e-12);
            var k = components == 0 ? usable : Math.Min(fit.ResolveComponents(components), usable);

            if (k < 1)
            {
                throw new ArgumentException(
                    "All variance is zero, so there is nothing to whiten. Check that the columns are not all constant.",
                    nameof(data));
            }

            var scores = fit.Score(data, k);
            var whitened = new double[scores.Length][];

            for (var i = 0; i < scores.Length; i++)
            {
                whitened[i] = new double[k];

                for (var c = 0; c < k; c++)
                {
                    whitened[i][c] = scores[i][c] / Math.Sqrt(fit.Eigenvalues[c]);
                }
            }

            var means = whitened.Mean(0);

            return new WhiteningResult(data.Length, k, fit.Eigenvalues.Take(k).ToArray(), whitened,
                whitened.StandardDeviation(means));
        }

        [AdvancedAnalyticsTool("ComputePrincipalComponentDiagnostics")]
        [Description(
            "Scores how unusual each row is relative to a principal component model learned from a reference set, using two standard statistics. " +
            "HotellingT2 is the sum of squared component scores each divided by its eigenvalue: it measures how far the row lies within the retained component space (large means extreme along the main patterns). " +
            "SquaredPredictionError is the squared distance between the row and its projection on the retained components in centred (and scaled) units: it measures how badly the retained components reconstruct the row (large means the row breaks the usual correlation pattern). " +
            "HotellingT2Cutoff is the chi-square limit for the chosen significance with as many degrees of freedom as components, a large sample approximation; IsAbnormalT2 flags rows above it. Test rows default to the reference rows.")]
        public PrincipalComponentDiagnosticsResult ComputePrincipalComponentDiagnostics(
            [Description("Reference rows used to learn the components. At least 2 rows, at most 20000.")]
            double[][] referenceData,
            [Description(
                "Rows to score, same number of columns as referenceData. Leave empty to score the reference rows themselves.")]
            double[][]? testData = null,
            [Description("Correlation (default) or Covariance basis.")]
            PrincipalComponentBasis basis = PrincipalComponentBasis.Correlation,
            [Description("How many leading components form the model, 1 up to the number available. Default 2.")]
            int components = 2,
            [Description("Significance level for the T squared cutoff, between 0.0001 and 0.5. Default 0.01.")]
            double significance = 0.01)
        {
            var fit = PrincipalComponentFit.Create(referenceData, nameof(referenceData), basis);
            var k = fit.ResolveComponents(components);
            MultivariateGuard.BoundedDouble(significance, nameof(significance), 0.0001, 0.5);

            var rows = testData ?? referenceData;

            if (testData != null)
            {
                MultivariateGuard.Matrix(testData, nameof(testData), 1, MultivariateLimits.MaximumProjectionRows);
                RequireSameColumns(referenceData, testData, nameof(testData));
            }

            var scores = fit.Score(rows, k);
            var t2 = new double[rows.Length];
            var spe = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var total = 0.0;

                for (var j = 0; j < rows[i].Length; j++)
                {
                    var value = (rows[i][j] - fit.Means[j]) / fit.Scale[j];
                    total += value * value;
                }

                var captured = 0.0;

                for (var c = 0; c < k; c++)
                {
                    if (fit.Eigenvalues[c] > fit.Eigenvalues[0] * 1e-12)
                    {
                        t2[i] += scores[i][c] * scores[i][c] / fit.Eigenvalues[c];
                    }

                    captured += scores[i][c] * scores[i][c];
                }

                spe[i] = Math.Max(0.0, total - captured);
            }

            var cutoff = new ChiSquareDistribution(k).InverseDistributionFunction(1.0 - significance);

            return new PrincipalComponentDiagnosticsResult(rows.Length, k, t2, spe, cutoff,
                t2.Select(v => v > cutoff).ToArray());
        }

        [AdvancedAnalyticsTool("ReconstructFromPrincipalComponents")]
        [Description(
            "Compresses rows onto the leading principal components learned from a reference set and maps them back to the original variables, returning the reconstructed rows and how much each row lost. " +
            "SquaredError is the squared distance between each row and its reconstruction in the original units, so rows that do not follow the dominant correlation pattern have a large error; this makes it an anomaly score that is directly interpretable. " +
            "RetainedVarianceRatio is the share of the reference variance the kept components carry. Use it to denoise data, to check how many components are enough, or to find rows the model cannot explain. Test rows default to the reference rows.")]
        public PrincipalComponentReconstructionResult ReconstructFromPrincipalComponents(
            [Description("Reference rows used to learn the components. At least 2 rows, at most 20000.")]
            double[][] referenceData,
            [Description(
                "Rows to reconstruct, same number of columns as referenceData. Leave empty to reconstruct the reference rows.")]
            double[][]? testData = null,
            [Description("Correlation (default) or Covariance basis.")]
            PrincipalComponentBasis basis = PrincipalComponentBasis.Correlation,
            [Description("How many leading components to keep, 1 up to the number available. Default 1.")]
            int components = 1)
        {
            var fit = PrincipalComponentFit.Create(referenceData, nameof(referenceData), basis);
            var k = fit.ResolveComponents(components);
            var rows = testData ?? referenceData;

            if (testData != null)
            {
                MultivariateGuard.Matrix(testData, nameof(testData), 1, MultivariateLimits.MaximumProjectionRows);
                RequireSameColumns(referenceData, testData, nameof(testData));
            }

            var scores = fit.Score(rows, k);
            var reconstructed = new double[rows.Length][];
            var errors = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                reconstructed[i] = new double[fit.Means.Length];

                for (var j = 0; j < fit.Means.Length; j++)
                {
                    var value = 0.0;

                    for (var c = 0; c < k; c++)
                    {
                        value += scores[i][c] * fit.Loadings[c][j];
                    }

                    reconstructed[i][j] = fit.Means[j] + value * fit.Scale[j];
                    var difference = rows[i][j] - reconstructed[i][j];
                    errors[i] += difference * difference;
                }
            }

            return new PrincipalComponentReconstructionResult(rows.Length, k, reconstructed, errors,
                fit.Cumulative[k - 1]);
        }

        [AdvancedAnalyticsTool("RunKernelPrincipalComponentAnalysis")]
        [Description(
            "Runs kernel principal component analysis: like principal component analysis but in the implicit feature space of a kernel, so it can unfold curved or clustered structure that straight components cannot (for example concentric rings with a Gaussian kernel). " +
            "It returns the eigenvalues of the centred kernel matrix, their explained variance ratios (relative to the components the analysis can extract, which number at most the count of input variables) and the score of every row on the requested components. " +
            "At most 1000 rows because the kernel matrix has rows squared cells. Pick kernelSigma near the typical distance between neighbouring points for a Gaussian kernel; scores signs are arbitrary but repeatable.")]
        public KernelPrincipalComponentResult RunKernelPrincipalComponentAnalysis(
            [Description("Rows of finite numbers, all the same length. At least 3 rows, at most 1000.")]
            double[][] data,
            [Description(KernelText.Kernel)] SimilarityKernelKind kernel = SimilarityKernelKind.Gaussian,
            [Description(KernelText.Sigma)] double kernelSigma = 1,
            [Description(KernelText.Gamma)] double kernelGamma = 1,
            [Description(KernelText.Constant)] double? kernelConstant = null,
            [Description(KernelText.Degree)] int kernelDegree = 2,
            [Description(KernelText.Alpha)] double kernelAlpha = 1,
            [Description(KernelText.Beta)] double kernelBeta = 1,
            [Description(
                "How many components to return scores for, at least 1. Default 2. Fewer are returned if fewer exist.")]
            int components = 2,
            [Description(
                "Correlation standardises the variables before applying the kernel; Covariance only centres them. Default Correlation.")]
            PrincipalComponentBasis basis = PrincipalComponentBasis.Correlation,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 3, MultivariateLimits.MaximumKernelRows);
            MultivariateGuard.Bounded(components, nameof(components), 1, MultivariateLimits.MaximumColumns);

            if (basis == PrincipalComponentBasis.Correlation)
            {
                MultivariateGuard.RequireVariation(data, nameof(data));
            }

            var function = KernelFactory.Create(kernel, kernelSigma, kernelGamma, kernelConstant, kernelDegree,
                kernelAlpha, kernelBeta, data[0].Length);

            cancellationToken.ThrowIfCancellationRequested();

            var analysis = new KernelPrincipalComponentAnalysis(function,
                basis == PrincipalComponentBasis.Correlation
                    ? PrincipalComponentMethod.Standardize
                    : PrincipalComponentMethod.Center);
            analysis.Learn(data);

            var available = analysis.Eigenvalues.Length;
            var k = Math.Min(components, available);
            var raw = analysis.Transform(data);
            var scores = new double[raw.Length][];

            for (var i = 0; i < raw.Length; i++)
            {
                scores[i] = new double[k];
            }

            for (var c = 0; c < k; c++)
            {
                var largest = 0.0;

                for (var i = 0; i < raw.Length; i++)
                {
                    if (Math.Abs(raw[i][c]) > Math.Abs(largest))
                    {
                        largest = raw[i][c];
                    }
                }

                var sign = largest < 0 ? -1.0 : 1.0;

                for (var i = 0; i < raw.Length; i++)
                {
                    scores[i][c] = sign * raw[i][c];
                }
            }

            return new KernelPrincipalComponentResult(data.Length, k, kernel.ToString(),
                analysis.Eigenvalues.Take(k).ToArray(), analysis.ComponentProportions.Take(k).ToArray(),
                analysis.CumulativeProportions.Take(k).ToArray(), scores);
        }

        [AdvancedAnalyticsTool("RunIndependentComponentAnalysis")]
        [Description(
            "Runs independent component analysis (blind source separation): it finds statistically independent, non Gaussian source signals whose linear mixture explains the observed columns, for example separating overlapping behaviours from combined activity measurements. " +
            "It returns the estimated sources (one column per component for every row), the mixing matrix (how sources combine into the observed variables) and the demixing matrix (the inverse map). The order, sign and scale of the sources are arbitrary, a known property of the method, so interpret shapes and relationships rather than magnitudes. " +
            "The optimisation starts from a random guess controlled by seed, so a fixed seed gives repeatable results; iterations are bounded by maxIterations (at most 1000). Needs at least as many rows as columns and at least 2 columns.")]
        public IndependentComponentResult RunIndependentComponentAnalysis(
            [Description(
                "Rows of finite numbers, all the same length, at least 2 columns. At least 3 rows, at most 20000.")]
            double[][] data,
            [Description(
                "How many independent components to extract, between 1 and the number of columns. 0 (default) uses every column.")]
            int components = 0,
            [Description(
                "Parallel (default) estimates all components together; Deflation extracts them one at a time.")]
            IndependentComponentMethod algorithm = IndependentComponentMethod.Parallel,
            [Description("Seed for the random starting point; reuse it to repeat a run. Default 1.")]
            int seed = 1,
            [Description("Maximum number of optimisation iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance, above zero. Default 0.001.")]
            double tolerance = 0.001,
            [Description("Whether to standardise each column first as well as centring it. Default false.")]
            bool standardise = false,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 3, MultivariateLimits.MaximumProjectionRows, 2);

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            MultivariateGuard.Positive(tolerance, nameof(tolerance));
            var columns = data[0].Length;
            var k = components == 0 ? columns : MultivariateGuard.Bounded(components, nameof(components), 1, columns);

            if (data.Length < columns)
            {
                throw new ArgumentException(
                    $"data has {data.Length} rows but {columns} columns; independent component analysis needs at least as many rows as columns.",
                    nameof(data));
            }

            if (standardise)
            {
                MultivariateGuard.RequireVariation(data, nameof(data));
            }

            using var scope = new SeededRandomScope(seed);
            cancellationToken.ThrowIfCancellationRequested();

            var analysis = new IndependentComponentAnalysis(
                standardise ? AnalysisMethod.Standardize : AnalysisMethod.Center,
                algorithm == IndependentComponentMethod.Parallel
                    ? IndependentComponentAlgorithm.Parallel
                    : IndependentComponentAlgorithm.Deflation)
            {
                Iterations = maximumIterations,
                Tolerance = tolerance,
                Token = cancellationToken,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken },
                NumberOfOutputs = k
            };

            var demix = analysis.Learn(MultivariateGuard.Copy(data));
            var sources = demix.Transform(MultivariateGuard.Copy(data));

            return new IndependentComponentResult(data.Length, k, algorithm.ToString(), analysis.Means, sources,
                analysis.MixingMatrix, analysis.DemixingMatrix);
        }

        internal static void RequireSameColumns(double[][] reference, double[][] other, string name)
        {
            if (reference[0].Length != other[0].Length)
            {
                throw new ArgumentException(
                    $"{name} has {other[0].Length} columns but the reference data has {reference[0].Length}; they must match.",
                    name);
            }
        }
    }
}