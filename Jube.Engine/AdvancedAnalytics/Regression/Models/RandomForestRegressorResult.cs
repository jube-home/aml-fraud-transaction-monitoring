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

using Jube.Engine.AdvancedAnalytics.Evidence;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Regression.Models
{
    public sealed record RandomForestRegressorSplitMetrics(
        string Split,
        int Rows,
        double? RSquared,
        double? AdjustedRSquared,
        double? RootMeanSquaredError,
        double? MeanAbsoluteError,
        double? MeanError);

    public sealed record RandomForestRegressorPredictions(int[] Rows, double[] Actuals, double[] Predicted);

    public sealed class RandomForestRegressorResult
    {
        public required string Kind { get; init; }
        public required int Seed { get; init; }
        public required RandomForestRegressorOptions Options { get; init; }
        public required string[] Variables { get; init; }
        public required AdvancedAnalyticsSplitSizes SplitSizes { get; init; }
        public required AdvancedAnalyticsVariablePreprocessing[] Preprocessing { get; init; }
        public required double[] FillValues { get; init; }
        public required int[] MissingCounts { get; init; }
        public required RandomForestRegressorEvidenceDetails Details { get; init; }
        public required AdvancedAnalyticsWarning[] Warnings { get; init; }
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
        public RandomForestRegressorPredictions TrainPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public RandomForestRegressorPredictions ValidationPredictions { get; init; } = null!;

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public RandomForestRegressorPredictions TestPredictions { get; init; } = null!;
    }
}