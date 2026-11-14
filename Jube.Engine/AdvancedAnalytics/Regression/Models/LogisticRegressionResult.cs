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

using MessagePack;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Regression.Models
{
    [MessagePackObject]
    public sealed record LogisticWarning([property: Key(0)] string Code, [property: Key(1)] string Message);

    [MessagePackObject]
    public sealed record LogisticSplitSizes(
        [property: Key(0)] int Train,
        [property: Key(1)] int Validation,
        [property: Key(2)] int Test,
        [property: Key(3)] int TrainPositives,
        [property: Key(4)] int ValidationPositives,
        [property: Key(5)] int TestPositives);

    [MessagePackObject]
    public sealed record LogisticReliabilityBin(
        [property: Key(0)] double Lower,
        [property: Key(1)] double Upper,
        [property: Key(2)] int Count,
        [property: Key(3)] double? MeanPredicted,
        [property: Key(4)] double? ObservedRate);

    [MessagePackObject]
    public sealed record LogisticSplitMetrics(
        [property: Key(0)] string Split,
        [property: Key(1)] int Rows,
        [property: Key(2)] int Positives,
        [property: Key(3)] double? RocAuc,
        [property: Key(4)] double? RocAucStandardError,
        [property: Key(5)] double? RocAucLower95,
        [property: Key(6)] double? RocAucUpper95,
        [property: Key(7)] double? AveragePrecision,
        [property: Key(8)] double? LogLoss,
        [property: Key(9)] double? BrierScore,
        [property: Key(10)] double? ExpectedCalibrationError,
        [property: Key(11)] LogisticReliabilityBin[] ReliabilityBins,
        [property: Key(12)] double Threshold,
        [property: Key(13)] string ThresholdSource,
        [property: Key(14)] int TruePositives,
        [property: Key(15)] int FalsePositives,
        [property: Key(16)] int TrueNegatives,
        [property: Key(17)] int FalseNegatives,
        [property: Key(18)] double? Precision,
        [property: Key(19)] double? Recall,
        [property: Key(20)] double? F1,
        [property: Key(21)] double? MatthewsCorrelation,
        [property: Key(22)] double? ExpectedCostAtThreshold,
        [property: Key(23)] double? CalibrationSlope,
        [property: Key(24)] double? CalibrationIntercept,
        [property: Key(25)] double? MeanPredictedProbability);

    [MessagePackObject]
    public sealed record LogisticCoefficient(
        [property: Key(0)] string Name,
        [property: Key(1)] string Kind,
        [property: Key(2)] double Estimate,
        [property: Key(3)] double? StandardError,
        [property: Key(4)] double? WaldZ,
        [property: Key(5)] double? PValue,
        [property: Key(6)] double? OddsRatio,
        [property: Key(7)] double? OddsRatioLower95,
        [property: Key(8)] double? OddsRatioUpper95);

    [MessagePackObject]
    public sealed record LogisticVariablePreprocessing(
        [property: Key(0)] string Name,
        [property: Key(1)] int MissingInTrain,
        [property: Key(2)] double FillValue,
        [property: Key(3)] double Mean,
        [property: Key(4)] double StandardDeviation,
        [property: Key(5)] bool HasMissingIndicator,
        [property: Key(6)] bool Excluded,
        [property: Key(7)] string ExclusionReason,
        [property: Key(8)] double? VarianceInflationFactor);

    [MessagePackObject]
    public sealed record LogisticCalibrationInfo(
        [property: Key(0)] bool Applied,
        [property: Key(1)] double? Slope,
        [property: Key(2)] double? Intercept,
        [property: Key(3)] int RowsUsed,
        [property: Key(4)] string Reason);

    [MessagePackObject]
    public sealed record LogisticPriors(
        [property: Key(0)] double TrainingPositiveRate,
        [property: Key(1)] double EffectivePrior,
        [property: Key(2)] double BaseRate,
        [property: Key(3)] string BaseRateSource);

    [MessagePackObject]
    public sealed record LogisticFitInfo(
        [property: Key(0)] bool Converged,
        [property: Key(1)] int Iterations,
        [property: Key(2)] int MaximumIterations,
        [property: Key(3)] double MaximumGradient,
        [property: Key(4)] bool Penalised,
        [property: Key(5)] double RidgeLambda,
        [property: Key(6)] int PredictorCount,
        [property: Key(7)] double EventsPerVariable,
        [property: Key(8)] string StandardErrorBasis);

    [MessagePackObject]
    public sealed record LogisticPredictions([property: Key(0)] int[] Rows, [property: Key(1)] int[] Labels, [property: Key(2)] double[] Probabilities);

    [MessagePackObject]
    public sealed class LogisticRegressionResult
    {
        [Key(0)]
        public required string Kind { get; init; }
        [Key(1)]
        public required int Seed { get; init; }
        [Key(2)]
        public required LogisticRegressionOptions Options { get; init; }
        [Key(3)]
        public required string[] Variables { get; init; }
        [Key(4)]
        public required LogisticSplitSizes SplitSizes { get; init; }
        [Key(5)]
        public required LogisticSplitMetrics TrainMetrics { get; init; }
        [Key(6)]
        public required LogisticSplitMetrics ValidationMetrics { get; init; }
        [Key(7)]
        public required LogisticSplitMetrics TestMetrics { get; init; }
        [Key(8)]
        public required LogisticCoefficient[] Coefficients { get; init; }
        [Key(9)]
        public required LogisticVariablePreprocessing[] Preprocessing { get; init; }
        [Key(10)]
        public required LogisticCalibrationInfo Calibration { get; init; }
        [Key(11)]
        public required LogisticPriors Priors { get; init; }
        [Key(12)]
        public required LogisticFitInfo Fit { get; init; }
        [Key(13)]
        public required LogisticWarning[] Warnings { get; init; }
        [Key(14)]
        public required string SnapshotJson { get; init; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(15)]
        public int[] TrainRows { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(16)]
        public int[] ValidationRows { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(17)]
        public int[] TestRows { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(18)]
        public LogisticPredictions TrainPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(19)]
        public LogisticPredictions ValidationPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(20)]
        public LogisticPredictions TestPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(21)]
        public double[] Weights { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Key(22)]
        public double Intercept { get; init; }
    }
}