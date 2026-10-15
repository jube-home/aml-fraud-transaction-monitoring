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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models
{
    public enum StepwiseDirection
    {
        Forward = 0,
        Backward = 1,
        Both = 2
    }

    public enum ModelSelectionCriterion
    {
        Aic = 0,
        Bic = 1,
        PValue = 2,
        AdjustedRSquared = 3,
        MallowsCp = 4
    }

    public enum HeteroscedasticityVariant
    {
        Studentised = 0,
        Classical = 1
    }

    public sealed record RegressionCoefficientRow
    {
        public string Term { get; init; } = string.Empty;
        public double Estimate { get; init; }
        public double StandardError { get; init; }
        public double TStatistic { get; init; }
        public double PValue { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public double StandardisedEstimate { get; init; }
        public double VarianceInflationFactor { get; init; }
        public double PartialRSquared { get; init; }
    }

    public sealed record LinearRegressionResult
    {
        public RegressionCoefficientRow[] Coefficients { get; init; } = Array.Empty<RegressionCoefficientRow>();
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
        public int PredictorCount { get; init; }
        public bool Intercept { get; init; }
        public double ResidualDegreesOfFreedom { get; init; }
        public double RSquared { get; init; }
        public double AdjustedRSquared { get; init; }
        public double MultipleR { get; init; }
        public double ResidualStandardError { get; init; }
        public double FStatistic { get; init; }
        public double FDegreesOfFreedomModel { get; init; }
        public double FDegreesOfFreedomResidual { get; init; }
        public double FpValue { get; init; }
        public double ResidualSumOfSquares { get; init; }
        public double TotalSumOfSquares { get; init; }
        public double LogLikelihood { get; init; }
        public double Aic { get; init; }
        public double Aicc { get; init; }
        public double Bic { get; init; }
        public double DurbinWatson { get; init; }
        public double ConfidenceLevel { get; init; }
        public double[]? FittedValues { get; init; }
        public double[]? Residuals { get; init; }
        public PredictionRow[]? Predictions { get; init; }
        public double XCentre { get; init; }
        public string[] Warnings { get; init; } = Array.Empty<string>();
    }

    public sealed record PredictionRow
    {
        public double[] Input { get; init; } = Array.Empty<double>();
        public double Prediction { get; init; }
        public double StandardErrorOfMean { get; init; }
        public double MeanLower { get; init; }
        public double MeanUpper { get; init; }
        public double PredictionLower { get; init; }
        public double PredictionUpper { get; init; }
        public double Leverage { get; init; }
    }

    public sealed record LinearPredictionResult
    {
        public PredictionRow[] Predictions { get; init; } = Array.Empty<PredictionRow>();
        public double ConfidenceLevel { get; init; }
        public double ResidualStandardError { get; init; }
        public double ResidualDegreesOfFreedom { get; init; }
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
        public int ExtrapolatedRows { get; init; }
        public string[] Warnings { get; init; } = Array.Empty<string>();
    }

    public sealed record PolynomialDegreeScore
    {
        public int Degree { get; init; }
        public double RSquared { get; init; }
        public double AdjustedRSquared { get; init; }
        public double ResidualStandardError { get; init; }
        public double Aic { get; init; }
        public double Bic { get; init; }
        public double LeaveOneOutRmse { get; init; }
        public double IncrementalFpValue { get; init; }
        public bool Fitted { get; init; }
    }

    public sealed record PolynomialDegreeSelection
    {
        public PolynomialDegreeScore[] Degrees { get; init; } = Array.Empty<PolynomialDegreeScore>();
        public int BestByAic { get; init; }
        public int BestByBic { get; init; }
        public int BestByAdjustedRSquared { get; init; }
        public int BestByLeaveOneOut { get; init; }
        public int BestBySequentialFTest { get; init; }
        public int Observations { get; init; }
        public double XCentre { get; init; }
    }

    public sealed record RidgeCoefficientRow
    {
        public string Term { get; init; } = string.Empty;
        public double Estimate { get; init; }
        public double StandardisedEstimate { get; init; }
        public double ApproximateStandardError { get; init; }
        public double OrdinaryLeastSquaresEstimate { get; init; }
    }

    public sealed record RidgeRegressionResult
    {
        public double Lambda { get; init; }
        public RidgeCoefficientRow[] Coefficients { get; init; } = Array.Empty<RidgeCoefficientRow>();
        public double Intercept { get; init; }
        public double EffectiveDegreesOfFreedom { get; init; }
        public double RSquared { get; init; }
        public double ResidualStandardError { get; init; }
        public double GeneralisedCrossValidation { get; init; }
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
        public double[]? FittedValues { get; init; }
        public string[] Warnings { get; init; } = Array.Empty<string>();
    }

    public sealed record RidgePathPoint
    {
        public double Lambda { get; init; }
        public double[] Estimates { get; init; } = Array.Empty<double>();
        public double[] StandardisedEstimates { get; init; } = Array.Empty<double>();
        public double Intercept { get; init; }
        public double EffectiveDegreesOfFreedom { get; init; }
        public double RSquared { get; init; }
        public double GeneralisedCrossValidation { get; init; }
    }

    public sealed record RidgePathResult
    {
        public string[] Variables { get; init; } = Array.Empty<string>();
        public RidgePathPoint[] Path { get; init; } = Array.Empty<RidgePathPoint>();
        public double BestLambdaByGcv { get; init; }
        public int BestIndex { get; init; }
        public int Observations { get; init; }
    }

    public sealed record NonNegativeLeastSquaresResult
    {
        public string[] Variables { get; init; } = Array.Empty<string>();
        public double[] Coefficients { get; init; } = Array.Empty<double>();
        public string[] ActiveVariables { get; init; } = Array.Empty<string>();
        public double ResidualSumOfSquares { get; init; }
        public double UncentredRSquared { get; init; }
        public double RootMeanSquaredError { get; init; }
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
        public double[]? FittedValues { get; init; }
    }

    public sealed record RansacRegressionResult
    {
        public LinearRegressionResult InlierModel { get; init; } = new();
        public LinearRegressionResult OrdinaryModel { get; init; } = new();
        public int[] InlierRows { get; init; } = Array.Empty<int>();
        public int[] OutlierRows { get; init; } = Array.Empty<int>();
        public double InlierFraction { get; init; }
        public double Threshold { get; init; }
        public int TrialsPerformed { get; init; }
        public int Seed { get; init; }
    }

    public sealed record MultivariateRegressionResult
    {
        public string[] Outputs { get; init; } = Array.Empty<string>();
        public LinearRegressionResult[] Models { get; init; } = Array.Empty<LinearRegressionResult>();
    }

    public sealed record PlsComponentSummary
    {
        public int Component { get; init; }
        public double PredictorVarianceExplained { get; init; }
        public double CumulativePredictorVarianceExplained { get; init; }
        public double ResponseVarianceExplained { get; init; }
        public double CumulativeResponseVarianceExplained { get; init; }
    }

    public sealed record PartialLeastSquaresResult
    {
        public string[] Variables { get; init; } = Array.Empty<string>();
        public int Components { get; init; }
        public double[] Coefficients { get; init; } = Array.Empty<double>();
        public double Intercept { get; init; }
        public double RSquared { get; init; }
        public double ResidualStandardError { get; init; }
        public PlsComponentSummary[] ComponentSummaries { get; init; } = Array.Empty<PlsComponentSummary>();
        public double[] VariableImportance { get; init; } = Array.Empty<double>();
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
    }

    public sealed record NestedModelComparison
    {
        public string[] FullVariables { get; init; } = Array.Empty<string>();
        public string[] ReducedVariables { get; init; } = Array.Empty<string>();
        public string[] DroppedVariables { get; init; } = Array.Empty<string>();
        public double FStatistic { get; init; }
        public double DegreesOfFreedomDifference { get; init; }
        public double ResidualDegreesOfFreedomFull { get; init; }
        public double PValue { get; init; }
        public double RssFull { get; init; }
        public double RssReduced { get; init; }
        public double RSquaredFull { get; init; }
        public double RSquaredReduced { get; init; }
        public double AicFull { get; init; }
        public double AicReduced { get; init; }
        public double BicFull { get; init; }
        public double BicReduced { get; init; }
        public string Conclusion { get; init; } = string.Empty;
    }

    public sealed record StepwiseStep
    {
        public int Step { get; init; }
        public string Action { get; init; } = string.Empty;
        public string Variable { get; init; } = string.Empty;
        public double CriterionValue { get; init; }
        public double PValue { get; init; }
        public string[] ModelAfterStep { get; init; } = Array.Empty<string>();
    }

    public sealed record StepwiseRegressionResult
    {
        public string[] SelectedVariables { get; init; } = Array.Empty<string>();
        public string[] RemovedVariables { get; init; } = Array.Empty<string>();
        public StepwiseStep[] Steps { get; init; } = Array.Empty<StepwiseStep>();
        public LinearRegressionResult FinalModel { get; init; } = new();
        public string Criterion { get; init; } = string.Empty;
    }

    public sealed record SubsetModelScore
    {
        public string[] Variables { get; init; } = Array.Empty<string>();
        public int Size { get; init; }
        public double RSquared { get; init; }
        public double AdjustedRSquared { get; init; }
        public double Aic { get; init; }
        public double Bic { get; init; }
        public double MallowsCp { get; init; }
        public double ResidualStandardError { get; init; }
    }

    public sealed record BestSubsetResult
    {
        public SubsetModelScore[] TopModels { get; init; } = Array.Empty<SubsetModelScore>();
        public SubsetModelScore[] BestPerSize { get; init; } = Array.Empty<SubsetModelScore>();
        public int ModelsEvaluated { get; init; }
        public int ModelsSkippedAsCollinear { get; init; }
        public string Criterion { get; init; } = string.Empty;
    }

    public sealed record CrossValidationFold
    {
        public int Fold { get; init; }
        public int TrainRows { get; init; }
        public int TestRows { get; init; }
        public double Rmse { get; init; }
        public double Mae { get; init; }
        public double RSquared { get; init; }
    }

    public sealed record LinearCrossValidationResult
    {
        public CrossValidationFold[] Folds { get; init; } = Array.Empty<CrossValidationFold>();
        public double CrossValidatedRmse { get; init; }
        public double CrossValidatedMae { get; init; }
        public double CrossValidatedRSquared { get; init; }
        public double InSampleRmse { get; init; }
        public double InSampleRSquared { get; init; }
        public double LeaveOneOutRmse { get; init; }
        public double PressStatistic { get; init; }
        public double PredictedRSquared { get; init; }
        public int Observations { get; init; }
        public int Seed { get; init; }
    }

    public sealed record InfluenceRow
    {
        public int Row { get; init; }
        public double Fitted { get; init; }
        public double Residual { get; init; }
        public double Leverage { get; init; }
        public double StandardisedResidual { get; init; }
        public double StudentisedResidual { get; init; }
        public double CooksDistance { get; init; }
        public double Dffits { get; init; }
        public double CovarianceRatio { get; init; }
        public double[] Dfbetas { get; init; } = Array.Empty<double>();
        public string[] Flags { get; init; } = Array.Empty<string>();
    }

    public sealed record InfluenceDiagnosticsResult
    {
        public InfluenceRow[] Rows { get; init; } = Array.Empty<InfluenceRow>();
        public InfluenceRow[] MostInfluential { get; init; } = Array.Empty<InfluenceRow>();
        public string[] Terms { get; init; } = Array.Empty<string>();
        public double LeverageThreshold { get; init; }
        public double CooksDistanceThreshold { get; init; }
        public double DffitsThreshold { get; init; }
        public double DfbetasThreshold { get; init; }
        public double StudentisedResidualThreshold { get; init; }
        public int HighLeverageCount { get; init; }
        public int InfluentialCount { get; init; }
        public int OutlierCount { get; init; }
        public double PressStatistic { get; init; }
        public double LeaveOneOutRmse { get; init; }
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
        public int[] KeptRowIndices { get; init; } = Array.Empty<int>();
    }

    public sealed record DiagnosticTestResult
    {
        public string Test { get; init; } = string.Empty;
        public double Statistic { get; init; }
        public double DegreesOfFreedom { get; init; }
        public double DegreesOfFreedom2 { get; init; }
        public double PValue { get; init; }
        public string Distribution { get; init; } = string.Empty;
        public string NullHypothesis { get; init; } = string.Empty;
        public string Conclusion { get; init; } = string.Empty;
        public int Observations { get; init; }
        public double AuxiliaryRSquared { get; init; }
        public double Estimate1 { get; init; }
        public double Estimate2 { get; init; }
    }

    public sealed record ResidualDurbinWatsonResult
    {
        public double DurbinWatson { get; init; }
        public double FirstOrderAutocorrelation { get; init; }
        public double ApproximateZ { get; init; }
        public double ApproximatePValue { get; init; }
        public string Interpretation { get; init; } = string.Empty;
        public int Observations { get; init; }
    }

    public sealed record ResidualNormalityResult
    {
        public int Observations { get; init; }
        public double Mean { get; init; }
        public double StandardDeviation { get; init; }
        public double Skewness { get; init; }
        public double ExcessKurtosis { get; init; }
        public double JarqueBeraStatistic { get; init; }
        public double JarqueBeraPValue { get; init; }
        public double ShapiroWilkStatistic { get; init; }
        public double ShapiroWilkPValue { get; init; }
        public double MinimumResidual { get; init; }
        public double MaximumResidual { get; init; }
        public double FractionBeyondTwoSigma { get; init; }
        public double FractionBeyondThreeSigma { get; init; }
        public string Conclusion { get; init; } = string.Empty;
    }

    public sealed record CollinearityRow
    {
        public string Variable { get; init; } = string.Empty;
        public double VarianceInflationFactor { get; init; }
        public double Tolerance { get; init; }
        public double RSquaredOnOthers { get; init; }
        public double StandardErrorInflation { get; init; }
        public string Severity { get; init; } = string.Empty;
    }

    public sealed record VarianceInflationResult
    {
        public CollinearityRow[] Variables { get; init; } = Array.Empty<CollinearityRow>();
        public double MaximumVif { get; init; }
        public double MeanVif { get; init; }
        public int Observations { get; init; }
        public string Summary { get; init; } = string.Empty;
    }

    public sealed record ConditionIndexRow
    {
        public int Index { get; init; }
        public double SingularValue { get; init; }
        public double ConditionIndex { get; init; }
        public double[] VarianceProportions { get; init; } = Array.Empty<double>();
        public string Severity { get; init; } = string.Empty;
    }

    public sealed record ConditionDiagnosticsResult
    {
        public string[] Terms { get; init; } = Array.Empty<string>();
        public ConditionIndexRow[] Rows { get; init; } = Array.Empty<ConditionIndexRow>();
        public double ConditionNumber { get; init; }
        public double CorrelationMatrixConditionNumber { get; init; }
        public double Determinant { get; init; }
        public int NumericalRank { get; init; }
        public int Observations { get; init; }
        public string Summary { get; init; } = string.Empty;
    }

    public sealed record AddedVariablePlotResult
    {
        public double[] XResiduals { get; init; } = Array.Empty<double>();
        public double[] YResiduals { get; init; } = Array.Empty<double>();
        public double Slope { get; init; }
        public double PartialCorrelation { get; init; }
        public int Observations { get; init; }
        public int[] KeptRowIndices { get; init; } = Array.Empty<int>();
    }

    public sealed record AssumptionCheck
    {
        public string Assumption { get; init; } = string.Empty;
        public string Test { get; init; } = string.Empty;
        public double Statistic { get; init; }
        public double PValue { get; init; }
        public bool Passed { get; init; }
        public string Comment { get; init; } = string.Empty;
    }

    public sealed record AssumptionReport
    {
        public AssumptionCheck[] Checks { get; init; } = Array.Empty<AssumptionCheck>();
        public int FailedCount { get; init; }
        public double RSquared { get; init; }
        public int Observations { get; init; }
        public string Summary { get; init; } = string.Empty;
    }

    public sealed record NonlinearParameterRow
    {
        public string Parameter { get; init; } = string.Empty;
        public double Estimate { get; init; }
        public double StandardError { get; init; }
        public double TStatistic { get; init; }
        public double PValue { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
    }

    public sealed record NonlinearCurveResult
    {
        public string Family { get; init; } = string.Empty;
        public string Formula { get; init; } = string.Empty;
        public NonlinearParameterRow[] Parameters { get; init; } = Array.Empty<NonlinearParameterRow>();
        public bool Converged { get; init; }
        public double ResidualSumOfSquares { get; init; }
        public double ResidualStandardError { get; init; }
        public double RSquared { get; init; }
        public double AdjustedRSquared { get; init; }
        public double Aic { get; init; }
        public double Bic { get; init; }
        public double LogLikelihood { get; init; }
        public double ResidualDegreesOfFreedom { get; init; }
        public double[] StartValuesUsed { get; init; } = Array.Empty<double>();
        public double[]? FittedValues { get; init; }
        public double[]? Residuals { get; init; }
        public CurvePrediction[]? Predictions { get; init; }
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
        public string[] Warnings { get; init; } = Array.Empty<string>();
    }

    public sealed record CurvePrediction
    {
        public double X { get; init; }
        public double Prediction { get; init; }
        public double StandardErrorOfMean { get; init; }
        public double MeanLower { get; init; }
        public double MeanUpper { get; init; }
        public double PredictionLower { get; init; }
        public double PredictionUpper { get; init; }
    }

    public sealed record CurveFamilyComparisonRow
    {
        public string Family { get; init; } = string.Empty;
        public bool Fitted { get; init; }
        public string Note { get; init; } = string.Empty;
        public double RSquared { get; init; }
        public double ResidualStandardError { get; init; }
        public double Aic { get; init; }
        public double Bic { get; init; }
        public double AkaikeWeight { get; init; }
        public double[] Parameters { get; init; } = Array.Empty<double>();
        public string[] ParameterNames { get; init; } = Array.Empty<string>();
        public int Rank { get; init; }
    }

    public sealed record CurveFamilyComparisonResult
    {
        public CurveFamilyComparisonRow[] Families { get; init; } = Array.Empty<CurveFamilyComparisonRow>();
        public string BestByAic { get; init; } = string.Empty;
        public string BestByBic { get; init; } = string.Empty;
        public int Observations { get; init; }
    }

    public sealed record GlmCoefficientRow
    {
        public string Term { get; init; } = string.Empty;
        public double Estimate { get; init; }
        public double StandardError { get; init; }
        public double Statistic { get; init; }
        public double PValue { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public double ExponentiatedEstimate { get; init; }
        public double ExponentiatedLower { get; init; }
        public double ExponentiatedUpper { get; init; }
    }

    public sealed record GeneralisedLinearModelResult
    {
        public string Family { get; init; } = string.Empty;
        public string Link { get; init; } = string.Empty;
        public GlmCoefficientRow[] Coefficients { get; init; } = Array.Empty<GlmCoefficientRow>();
        public bool Converged { get; init; }
        public int Iterations { get; init; }
        public double Deviance { get; init; }
        public double NullDeviance { get; init; }
        public double PseudoRSquared { get; init; }
        public double ResidualDegreesOfFreedom { get; init; }
        public double NullDegreesOfFreedom { get; init; }
        public double Dispersion { get; init; }
        public double PearsonChiSquare { get; init; }
        public double LogLikelihood { get; init; }
        public double Aic { get; init; }
        public double Bic { get; init; }
        public double LikelihoodRatioStatistic { get; init; }
        public double LikelihoodRatioPValue { get; init; }
        public int Observations { get; init; }
        public int RowsDropped { get; init; }
        public double[]? FittedMeans { get; init; }
        public double[]? DevianceResiduals { get; init; }
        public string[] Warnings { get; init; } = Array.Empty<string>();
    }

    public sealed record GlmPredictionRow
    {
        public double[] Input { get; init; } = Array.Empty<double>();
        public double LinearPredictor { get; init; }
        public double LinearPredictorStandardError { get; init; }
        public double Mean { get; init; }
        public double MeanLower { get; init; }
        public double MeanUpper { get; init; }
    }

    public sealed record GlmPredictionResult
    {
        public GlmPredictionRow[] Predictions { get; init; } = Array.Empty<GlmPredictionRow>();
        public string Family { get; init; } = string.Empty;
        public string Link { get; init; } = string.Empty;
        public double ConfidenceLevel { get; init; }
        public bool Converged { get; init; }
    }

    public sealed record LinkTransformResult
    {
        public string Link { get; init; } = string.Empty;
        public string Direction { get; init; } = string.Empty;
        public double[] Values { get; init; } = Array.Empty<double>();
        public int InvalidCount { get; init; }
    }

    public sealed record InformationCriterionRow
    {
        public string Model { get; init; } = string.Empty;
        public double LogLikelihood { get; init; }
        public int Parameters { get; init; }
        public double Aic { get; init; }
        public double Aicc { get; init; }
        public double Bic { get; init; }
        public double DeltaAic { get; init; }
        public double DeltaBic { get; init; }
        public double AkaikeWeight { get; init; }
        public double BicWeight { get; init; }
    }

    public sealed record InformationCriterionComparison
    {
        public InformationCriterionRow[] Models { get; init; } = Array.Empty<InformationCriterionRow>();
        public string BestByAic { get; init; } = string.Empty;
        public string BestByBic { get; init; } = string.Empty;
        public int Observations { get; init; }
    }
}