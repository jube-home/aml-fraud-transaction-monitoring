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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models
{
    public sealed record DistributionInfo(
        string Name,
        bool IsDiscrete,
        string Description,
        string Support,
        string TypicalUses,
        string[] ParameterNames,
        string[] ParameterDescriptions,
        double[] ExampleParameters,
        bool CanFit,
        string FitMethod);

    public sealed record DistributionSummary(
        string Distribution,
        bool IsDiscrete,
        string[] ParameterNames,
        double[] Parameters,
        double? Mean,
        double? Variance,
        double? StandardDeviation,
        double? Median,
        double? Mode,
        double? Entropy,
        double? SupportLower,
        double? SupportUpper,
        double[] QuantileProbabilities,
        double?[] QuantileValues);

    public sealed record DistributionEvaluation(
        string Distribution,
        string Quantity,
        double[] Parameters,
        double[] Inputs,
        double?[] Values);

    public sealed record DistributionCurveResult(
        string Distribution,
        double[] Parameters,
        double[] X,
        double?[] Density,
        double?[] Cumulative,
        double?[] Survival);

    public sealed record DistributionSampleResult(
        string Distribution,
        double[] Parameters,
        int Seed,
        double[] Values,
        double SampleMean,
        double? SampleStandardDeviation);

    public sealed record DistributionMassTable(
        string Distribution,
        double[] Parameters,
        int[] Values,
        double[] Probability,
        double[] CumulativeProbability,
        double TotalProbability);

    public sealed record DistributionLogLikelihoodResult(
        string Distribution,
        double[] Parameters,
        int SampleSize,
        double? LogLikelihood,
        double? MeanLogLikelihood,
        int ParameterCount,
        double? Aic,
        double? Bic,
        int ZeroLikelihoodCount);

    public sealed record DistributionTailScoreResult(
        string Distribution,
        double[] Values,
        double?[] LowerTail,
        double?[] UpperTail,
        double?[] TwoSided,
        double?[] Surprise);

    public sealed record DistributionFitResult(
        string Distribution,
        bool IsDiscrete,
        string Method,
        string[] ParameterNames,
        double[] Parameters,
        bool[] Estimated,
        int SampleSize,
        double? LogLikelihood,
        int EstimatedParameterCount,
        double? Aic,
        double? Bic,
        double? Mean,
        double? Variance,
        double? Median,
        double? KolmogorovSmirnovStatistic,
        string[] Notes);

    public sealed record DistributionFitCandidate(
        string Distribution,
        bool IsDiscrete,
        string[] ParameterNames,
        double[] Parameters,
        int EstimatedParameterCount,
        double LogLikelihood,
        double Aic,
        double Bic,
        double? KolmogorovSmirnovStatistic,
        double? KolmogorovSmirnovPValue,
        double? AndersonDarlingStatistic,
        double? ChiSquareStatistic,
        int AicRank,
        int BicRank,
        int KolmogorovSmirnovRank,
        int AndersonDarlingRank,
        int ChiSquareRank,
        int AverageRank,
        int Rank);

    public sealed record DistributionFitRanking(
        int SampleSize,
        string RankedBy,
        DistributionFitCandidate[] Candidates,
        string[] Failed,
        string[] Notes);

    public sealed record DistributionFitScoreResult(
        DistributionFitResult Fit,
        double[] Values,
        double?[] LowerTail,
        double?[] UpperTail,
        double?[] TwoSided,
        double?[] Density);

    public sealed record ProbabilityPlotResult(
        string Distribution,
        string PlottingPosition,
        double[] Parameters,
        double[] SampleValues,
        double[] PlottingPositions,
        double?[] TheoreticalQuantiles,
        double?[] TheoreticalProbabilities,
        double?[] EmpiricalProbabilities,
        double? Correlation,
        double? LineSlope,
        double? LineIntercept);

    public sealed record NormalityShapeResult(
        int SampleSize,
        double Mean,
        double StandardDeviation,
        double Skewness,
        double SkewnessStandardError,
        double SkewnessZ,
        double ExcessKurtosis,
        double ExcessKurtosisStandardError,
        double ExcessKurtosisZ,
        double? JarqueBeraStatistic,
        double? JarqueBeraPValue,
        string Verdict,
        string[] Notes);

    public sealed record TailExceedanceRow(
        double Level,
        double Threshold,
        int ExceedanceCount,
        double ExceedanceFraction,
        double? MeanExcess,
        double? MeanOfExceedances,
        double? MaximumExcess);

    public sealed record TailExceedanceResult(
        int SampleSize,
        TailExceedanceRow[] Rows,
        string[] Notes);

    public sealed record TailModelResult(
        int SampleSize,
        double Threshold,
        double ThresholdQuantile,
        int ExceedanceCount,
        double Scale,
        double Shape,
        double LogLikelihood,
        double[] Probabilities,
        double?[] ReturnLevels,
        string TailKind,
        string[] Notes);

    public sealed record MultivariateNormalEvaluation(
        double[] Mean,
        double[][] Covariance,
        double?[] Density,
        double?[] LogDensity,
        double?[] MahalanobisDistance);

    public sealed record MahalanobisResult(
        double[] Distances,
        double[] SquaredDistances,
        double[] ChiSquareTailProbability,
        int Dimension);

    public sealed record MultivariateNormalFitResult(
        int SampleSize,
        int Dimension,
        double[] Mean,
        double[][] Covariance,
        double[][] Correlation,
        double[] StandardDeviations,
        double LogLikelihood);

    public sealed record MultivariateNormalCumulativeResult(
        double[][] Points,
        double?[] LowerOrthantProbability,
        double?[] UpperOrthantProbability);

    public sealed record MultivariateSampleResult(
        int Seed,
        double[][] Rows,
        double[] SampleMean);

    public sealed record DirichletSummary(
        double[] Concentrations,
        double[] Mean,
        double[] Variance,
        double[][] Covariance,
        double Total);

    public sealed record DirichletFitResult(
        int SampleSize,
        double[] Concentrations,
        double[] Mean,
        double Total,
        double LogLikelihood);

    public sealed record MultinomialSummary(
        int Trials,
        double[] Probabilities,
        double[] Mean,
        double[] Variance,
        double[][] Covariance);

    public sealed record MultinomialEvaluation(
        int Trials,
        double[] Probabilities,
        double?[] Probability,
        double?[] LogProbability);

    public sealed record MatrixDistributionSummary(
        string Distribution,
        double DegreesOfFreedom,
        double[][] Mean,
        double[][] VarianceOfEntries);

    public sealed record MatrixDensityResult(
        string Distribution,
        double? Density,
        double? LogDensity);

    public sealed record MatrixSampleResult(
        string Distribution,
        int Seed,
        double[][][] Matrices,
        double[][] MeanMatrix);

    public sealed record DirectionalEvaluation(
        double[] MeanDirection,
        double Concentration,
        double?[] Density);

    public sealed record GaussianMixtureComponent(
        int Index,
        double Weight,
        double Mean,
        double StandardDeviation,
        int AssignedCount);

    public sealed record GaussianMixtureResult(
        int SampleSize,
        int Components,
        GaussianMixtureComponent[] ComponentSummaries,
        double LogLikelihood,
        int ParameterCount,
        double Aic,
        double Bic,
        int[] Assignments,
        double[][]? Responsibilities,
        bool Converged,
        string[] Notes);

    public sealed record MixtureSelectionRow(
        int Components,
        double LogLikelihood,
        int ParameterCount,
        double Aic,
        double Bic,
        bool Converged);

    public sealed record MixtureSelectionResult(
        MixtureSelectionRow[] Rows,
        int BestByBic,
        int BestByAic,
        string[] Notes);

    public sealed record MixtureModelResult(
        string ComponentDistribution,
        int SampleSize,
        double[] Weights,
        double[][] ComponentParameters,
        string[] ParameterNames,
        double LogLikelihood,
        int ParameterCount,
        double Aic,
        double Bic,
        int[] Assignments,
        string[] Notes);

    public sealed record MixtureEvaluation(
        double[] Points,
        double?[] Density,
        double?[] Cumulative,
        double[][] ComponentResponsibilities);
}