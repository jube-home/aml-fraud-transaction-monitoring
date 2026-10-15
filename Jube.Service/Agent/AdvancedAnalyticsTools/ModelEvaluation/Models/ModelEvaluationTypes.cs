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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models
{
    public enum EvaluationFamily
    {
        LogisticRegression = 0,
        DecisionTree = 1,
        RandomForest = 2,
        Bagging = 3,
        AdaBoost = 4,
        GradientBoosting = 5,
        NaiveBayes = 6,
        TreeAugmentedNaiveBayes = 7,
        KNearestNeighbours = 8,
        LinearSupportVector = 9,
        KernelSupportVector = 10,
        NeuralNetwork = 11
    }

    public enum TreeAlgorithm
    {
        C45 = 0,
        Id3 = 1,
        Cart = 2
    }

    public enum TreePruning
    {
        None = 0,
        ReducedError = 1
    }

    public enum ForestMode
    {
        RandomForest = 0,
        Bagging = 1
    }

    public enum BoostingAlgorithm
    {
        AdaBoost = 0,
        GradientBoosting = 1
    }

    public enum NaiveBayesKind
    {
        Gaussian = 0,
        Categorical = 1,
        TreeAugmented = 2
    }

    public enum SvmKernel
    {
        Linear = 0,
        Gaussian = 1,
        Polynomial = 2,
        Sigmoid = 3,
        Laplacian = 4
    }

    public enum ClassImbalanceHandling
    {
        None = 0,
        BalancedWeights = 1,
        Oversample = 2,
        Undersample = 3
    }

    public enum SelectionMetric
    {
        RocAuc = 0,
        AveragePrecision = 1,
        LogLoss = 2,
        BrierScore = 3,
        F1 = 4,
        MatthewsCorrelation = 5
    }

    public enum HyperParameter
    {
        MaxDepth = 0,
        MinLeafSize = 1,
        TreeCount = 2,
        FeatureFraction = 3,
        SampleFraction = 4,
        LearningRate = 5,
        NeighbourCount = 6,
        Complexity = 7,
        KernelWidth = 8,
        HiddenUnits = 9,
        Epochs = 10,
        Regularisation = 11,
        BinCount = 12
    }

    public enum LinearPenalty
    {
        None = 0,
        Ridge = 1,
        Lasso = 2,
        ElasticNet = 3
    }

    public enum RegressionFamily
    {
        LinearRegression = 0,
        DecisionTree = 1,
        RandomForest = 2,
        GradientBoosting = 3,
        KNearestNeighbours = 4,
        KernelRidge = 5,
        NeuralNetwork = 6
    }

    public enum RegressionSelectionMetric
    {
        RSquared = 0,
        MeanAbsoluteError = 1,
        RootMeanSquaredError = 2,
        MeanAbsolutePercentageError = 3
    }

    public enum MulticlassAlgorithm
    {
        MultinomialLogistic = 0,
        DecisionTree = 1,
        RandomForest = 2,
        NaiveBayes = 3,
        KNearestNeighbours = 4,
        NeuralNetwork = 5
    }

    public enum AnomalyAlgorithm
    {
        IsolationForest = 0,
        OneClassSupportVector = 1,
        KMeansDistance = 2,
        GaussianMixtureDensity = 3,
        MahalanobisDistance = 4
    }

    public sealed record EvaluationWarning(string Code, string Message);

    public sealed record EvaluationNumber(string Name, double? Value);

    public sealed record EvaluationSetting(string Name, string Value);

    public sealed record ClassificationMetrics(
        int Rows,
        int Positives,
        double? RocAuc,
        double? RocAucLower95,
        double? RocAucUpper95,
        double? AveragePrecision,
        double? LogLoss,
        double? BrierScore,
        double? ExpectedCalibrationError,
        double? KolmogorovSmirnov,
        double Threshold,
        int TruePositives,
        int FalsePositives,
        int TrueNegatives,
        int FalseNegatives,
        double? Accuracy,
        double? Precision,
        double? Recall,
        double? Specificity,
        double? F1,
        double? BalancedAccuracy,
        double? MatthewsCorrelation);

    public sealed record EvaluationMetricSummary(
        string Metric,
        double? Mean,
        double? StandardDeviation,
        double? Minimum,
        double? Maximum);

    public sealed record EvaluationFoldResult(
        int Fold,
        int TrainRows,
        int ValidationRows,
        int TestRows,
        string? SelectedSetting,
        double? TrainRocAuc,
        ClassificationMetrics? Test);

    public sealed record CalibrationSummary(
        bool Applied,
        string Method,
        double? Slope,
        double? Intercept,
        int RowsUsed,
        string? Reason);

    public sealed record SweepRow(
        string Setting,
        double? TrainScore,
        double? ValidationScore,
        bool Selected);

    public sealed record EvaluationImportanceRow(
        string Variable,
        double Importance,
        double? StandardDeviation,
        int Rank);

    public sealed record ModelDescription(
        string Summary,
        EvaluationNumber[] Facts,
        string[] Rules,
        string? TreeText,
        EvaluationImportanceRow[] IntrinsicImportance);

    public sealed record RegressionMetricsResult(
        int Rows,
        double? RSquared,
        double? MeanAbsoluteError,
        double? RootMeanSquaredError,
        double? MeanAbsolutePercentageError,
        double? SymmetricMeanAbsolutePercentageError,
        double? MedianAbsoluteError,
        double? MaximumAbsoluteError,
        double? Correlation,
        double? ResidualMean,
        double? ResidualStandardDeviation,
        double? ResidualSkewness);

    public sealed record RegressionFoldResult(
        int Fold,
        int TrainRows,
        int TestRows,
        string? SelectedSetting,
        double? TrainRSquared,
        RegressionMetricsResult? Test);

    public sealed record RegressionSweepRow(
        string Setting,
        double? TrainScore,
        double? ValidationScore,
        bool Selected);

    public sealed record ClassMetricsRow(
        int Class,
        int Support,
        double? Precision,
        double? Recall,
        double? F1,
        double? OneVersusRestAuc);

    public sealed record MulticlassMetrics(
        int Rows,
        double? Accuracy,
        double? BalancedAccuracy,
        double? MacroF1,
        double? MicroF1,
        double? WeightedF1,
        double? CohenKappa,
        double? MatthewsCorrelation,
        double? LogLoss,
        double? BrierScore,
        double? MacroRocAuc,
        ClassMetricsRow[] PerClass,
        int[] ClassLabels,
        int[][] ConfusionMatrix);

    public sealed record AnomalyFlaggedRow(int Row, double Score);

    public sealed record AnomalyQuantile(double Quantile, double Score);

    public sealed class AnomalyEvaluationResult
    {
        public required string Algorithm { get; init; }
        public required int Seed { get; init; }
        public required EvaluationSetting[] Settings { get; init; }
        public required int Rows { get; init; }
        public required int Features { get; init; }
        public required int TrainRows { get; init; }
        public required int ValidationRows { get; init; }
        public required int TestRows { get; init; }
        public required double ContaminationUsed { get; init; }
        public required string ContaminationSource { get; init; }
        public required double Threshold { get; init; }
        public required double? EstimatedContamination { get; init; }
        public required double TestFlaggedShare { get; init; }
        public required double TrainFlaggedShare { get; init; }
        public required AnomalyQuantile[] TestScoreQuantiles { get; init; }
        public required AnomalyFlaggedRow[] TopTestRows { get; init; }
        public required bool HasLabels { get; init; }
        public required double? RocAuc { get; init; }
        public required double? AveragePrecision { get; init; }
        public required double? Precision { get; init; }
        public required double? Recall { get; init; }
        public required double? F1 { get; init; }
        public required int TestKnownAnomalies { get; init; }
        public required EvaluationNumber[] Facts { get; init; }
        public required EvaluationWarning[] Warnings { get; init; }
        public required string Interpretation { get; init; }
    }

    public sealed record ContaminationEstimate(string Method, double? Share, string Note);

    public sealed record HyperParameterTrial(
        int Trial,
        EvaluationSetting[] Parameters,
        double? MeanTrainScore,
        double? MeanValidationScore,
        double? ValidationStandardDeviation,
        int Rank,
        bool Best);

    public sealed record NestedFold(
        int OuterFold,
        string SelectedSetting,
        double? InnerScore,
        double? OuterScore);

    public sealed record EvaluationCurvePoint(
        double X,
        string Label,
        double? TrainScore,
        double? ValidationScore,
        double? TrainStandardDeviation,
        double? ValidationStandardDeviation,
        int TrainRows);

    public sealed record PartialDependencePoint(
        double Value,
        double Mean,
        double Lower10,
        double Median,
        double Upper90);

    public sealed record CalibrationMethodRow(
        string Method,
        double? LogLoss,
        double? BrierScore,
        double? ExpectedCalibrationError,
        double? RocAuc,
        double? CalibrationSlope,
        double? CalibrationIntercept,
        double? MeanPrediction);

    public sealed record ComparedModelRow(
        int Rank,
        string Model,
        double? MeanScore,
        double? StandardDeviation,
        double? MeanTrainScore,
        double? OverfitGap,
        double? MeanDifferenceToBest,
        bool IndistinguishableFromBest,
        double? TestRocAuc,
        double? TestAveragePrecision,
        double? TestLogLoss,
        double? TestBrierScore,
        double? TestF1,
        string? Note);

    public sealed record ComparedRegressorRow(
        int Rank,
        string Model,
        double? MeanScore,
        double? StandardDeviation,
        double? MeanTrainScore,
        double? OverfitGap,
        double? MeanDifferenceToBest,
        bool IndistinguishableFromBest,
        double? TestRSquared,
        double? TestMeanAbsoluteError,
        double? TestRootMeanSquaredError,
        string? Note);
}