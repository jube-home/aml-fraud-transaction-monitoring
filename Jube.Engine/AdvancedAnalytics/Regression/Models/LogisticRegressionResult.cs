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

using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Regression.Models
{
    public sealed record LogisticWarning(string Code, string Message);

    public sealed record LogisticSplitSizes(
        int Train,
        int Validation,
        int Test,
        int TrainPositives,
        int ValidationPositives,
        int TestPositives);

    public sealed record LogisticReliabilityBin(
        double Lower,
        double Upper,
        int Count,
        double? MeanPredicted,
        double? ObservedRate);

    public sealed record LogisticSplitMetrics(
        string Split,
        int Rows,
        int Positives,
        double? RocAuc,
        double? RocAucStandardError,
        double? RocAucLower95,
        double? RocAucUpper95,
        double? AveragePrecision,
        double? LogLoss,
        double? BrierScore,
        double? ExpectedCalibrationError,
        LogisticReliabilityBin[] ReliabilityBins,
        double Threshold,
        string ThresholdSource,
        int TruePositives,
        int FalsePositives,
        int TrueNegatives,
        int FalseNegatives,
        double? Precision,
        double? Recall,
        double? F1,
        double? MatthewsCorrelation,
        double? ExpectedCostAtThreshold,
        double? CalibrationSlope,
        double? CalibrationIntercept,
        double? MeanPredictedProbability);

    public sealed record LogisticCoefficient(
        string Name,
        string Kind,
        double Estimate,
        double? StandardError,
        double? WaldZ,
        double? PValue,
        double? OddsRatio,
        double? OddsRatioLower95,
        double? OddsRatioUpper95);

    public sealed record LogisticVariablePreprocessing(
        string Name,
        int MissingInTrain,
        double FillValue,
        double Mean,
        double StandardDeviation,
        bool HasMissingIndicator,
        bool Excluded,
        string ExclusionReason,
        double? VarianceInflationFactor);

    public sealed record LogisticCalibrationInfo(
        bool Applied,
        double? Slope,
        double? Intercept,
        int RowsUsed,
        string Reason);

    public sealed record LogisticPriors(
        double TrainingPositiveRate,
        double EffectivePrior,
        double BaseRate,
        string BaseRateSource);

    public sealed record LogisticFitInfo(
        bool Converged,
        int Iterations,
        int MaximumIterations,
        double MaximumGradient,
        bool Penalised,
        double RidgeLambda,
        int PredictorCount,
        double EventsPerVariable,
        string StandardErrorBasis);

    public sealed record LogisticPredictions(int[] Rows, int[] Labels, double[] Probabilities);

    public sealed class LogisticRegressionResult
    {
        public required string Kind { get; init; }
        public required int Seed { get; init; }
        public required LogisticRegressionOptions Options { get; init; }
        public required string[] Variables { get; init; }
        public required LogisticSplitSizes SplitSizes { get; init; }
        public required LogisticSplitMetrics TrainMetrics { get; init; }
        public required LogisticSplitMetrics ValidationMetrics { get; init; }
        public required LogisticSplitMetrics TestMetrics { get; init; }
        public required LogisticCoefficient[] Coefficients { get; init; }
        public required LogisticVariablePreprocessing[] Preprocessing { get; init; }
        public required LogisticCalibrationInfo Calibration { get; init; }
        public required LogisticPriors Priors { get; init; }
        public required LogisticFitInfo Fit { get; init; }
        public required LogisticWarning[] Warnings { get; init; }
        public required string SnapshotJson { get; init; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int[] TrainRows { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int[] ValidationRows { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int[] TestRows { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public LogisticPredictions TrainPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public LogisticPredictions ValidationPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public LogisticPredictions TestPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public double[] Weights { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public double Intercept { get; init; }
    }
}