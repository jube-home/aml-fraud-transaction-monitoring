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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models
{
    public enum PrincipalComponentBasis
    {
        Covariance,
        Correlation
    }

    public enum IndependentComponentMethod
    {
        Parallel,
        Deflation
    }

    public sealed record PrincipalComponentAnalysisResult(
        int Rows,
        int Variables,
        int ComponentsReturned,
        string Basis,
        double[] Means,
        double[] StandardDeviations,
        double[] Eigenvalues,
        double[] ExplainedVarianceRatio,
        double[] CumulativeExplainedVarianceRatio,
        double[] SingularValues,
        double[][] Loadings,
        double[][] ScaledLoadings,
        double[] Communalities,
        double[][] Scores,
        int ComponentsFor80Percent,
        int ComponentsFor90Percent,
        int ComponentsFor95Percent,
        int ComponentsFor99Percent,
        int KaiserComponentCount);

    public sealed record ComponentVariableLoading(string Variable, double Loading, double SquaredLoadingShare);

    public sealed record ComponentInterpretation(
        int Component,
        double Eigenvalue,
        double ExplainedVarianceRatio,
        ComponentVariableLoading[] TopVariables);

    public sealed record PrincipalComponentInterpretationResult(
        int Rows,
        string Basis,
        ComponentInterpretation[] Components,
        string[] Variables,
        double[] Communalities);

    public sealed record PrincipalComponentDiagnosticsResult(
        int Rows,
        int ComponentsUsed,
        double[] HotellingT2,
        double[] SquaredPredictionError,
        double HotellingT2Cutoff,
        bool[] IsAbnormalT2);

    public sealed record PrincipalComponentReconstructionResult(
        int Rows,
        int ComponentsUsed,
        double[][] Reconstructed,
        double[] SquaredError,
        double RetainedVarianceRatio);

    public sealed record WhiteningResult(
        int Rows,
        int Components,
        double[] Eigenvalues,
        double[][] WhitenedData,
        double[] ColumnStandardDeviations);

    public sealed record KernelPrincipalComponentResult(
        int Rows,
        int ComponentsReturned,
        string Kernel,
        double[] Eigenvalues,
        double[] ExplainedVarianceRatio,
        double[] CumulativeExplainedVarianceRatio,
        double[][] Scores);

    public sealed record IndependentComponentResult(
        int Rows,
        int Components,
        string Algorithm,
        double[] Means,
        double[][] Sources,
        double[][] MixingMatrix,
        double[][] DemixingMatrix);

    public sealed record DiscriminantAnalysisResult(
        int Rows,
        int Variables,
        int Classes,
        int[] ClassLabels,
        int[] ClassCounts,
        double[] ClassPrevalence,
        double[][] ClassMeans,
        double[] Eigenvalues,
        double[] ExplainedVarianceRatio,
        double[] CumulativeExplainedVarianceRatio,
        double[][] DiscriminantVectors,
        double[][] ProjectedClassMeans,
        double[][] Scores,
        int[] PredictedLabels,
        double TrainingAccuracy);

    public sealed record DiscriminantClassificationResult(
        int Rows,
        int[] ClassLabels,
        int[] PredictedLabels,
        double[][] Scores);

    public sealed record MahalanobisOutlierResult(
        int Rows,
        int Variables,
        int Rank,
        double[] Centre,
        double[] Distances,
        double[] SquaredDistances,
        double[] PValues,
        double Cutoff,
        double SquaredCutoff,
        bool[] IsOutlier,
        int[] OutlierRows,
        int OutlierCount);

    public sealed record EmbeddingResult(
        int Rows,
        int Dimensions,
        double Perplexity,
        double Theta,
        double[][] Coordinates);
}