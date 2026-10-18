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
    public sealed record LinearWarning(string Code, string Message);

    public sealed record LinearSplitSizes(int Train, int Validation, int Test);

    public sealed record LinearSplitMetrics(
        string Split,
        int Rows,
        double? RSquared,
        double? AdjustedRSquared,
        double? RootMeanSquaredError,
        double? MeanAbsoluteError,
        double? MeanError,
        double? ResidualStandardError);

    public sealed record LinearCoefficient(
        string Name,
        string Kind,
        double Estimate,
        double? StandardError,
        double? TStatistic,
        double? PValue);

    public sealed record LinearVariablePreprocessing(
        string Name,
        int MissingInTrain,
        double FillValue,
        double Mean,
        double StandardDeviation,
        bool HasMissingIndicator,
        bool Excluded,
        string ExclusionReason,
        double? VarianceInflationFactor);

    public sealed record LinearFitInfo(
        int PredictorCount,
        double ObservationsPerVariable,
        int ResidualDegreesOfFreedom,
        string StandardErrorBasis);

    public sealed record LinearPredictions(int[] Rows, double[] Actuals, double[] Predicted);

    public sealed class LinearRegressionResult
    {
        public required string Kind { get; init; }
        public required int Seed { get; init; }
        public required LinearRegressionOptions Options { get; init; }
        public required string[] Variables { get; init; }
        public required LinearSplitSizes SplitSizes { get; init; }
        public required LinearSplitMetrics TrainMetrics { get; init; }
        public required LinearSplitMetrics ValidationMetrics { get; init; }
        public required LinearSplitMetrics TestMetrics { get; init; }
        public required LinearCoefficient[] Coefficients { get; init; }
        public required LinearVariablePreprocessing[] Preprocessing { get; init; }
        public required LinearFitInfo Fit { get; init; }
        public required LinearWarning[] Warnings { get; init; }
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
        public LinearPredictions TrainPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public LinearPredictions ValidationPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public LinearPredictions TestPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public double[] Weights { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public double Intercept { get; init; }
    }
}