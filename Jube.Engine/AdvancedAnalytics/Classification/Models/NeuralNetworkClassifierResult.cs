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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Classification.Models
{
    public sealed record NeuralNetworkSplitMetrics(
        string Split,
        int Rows,
        int Positives,
        double? RocAuc,
        double? AveragePrecision,
        double? LogLoss,
        double? BrierScore,
        double? ExpectedCalibrationError);

    public sealed record NeuralNetworkCalibrationInfo(
        bool Applied,
        double? Slope,
        double? Intercept,
        int RowsUsed,
        string Reason);

    public sealed class NeuralNetworkClassifierResult
    {
        public string Kind { get; set; } = string.Empty;
        public int Seed { get; set; }
        public string[] Variables { get; set; } = [];
        public NeuralNetworkClassifierOptions Options { get; set; } = null!;
        public AdvancedAnalyticsSplitSizes SplitSizes { get; set; } = null!;
        public AdvancedAnalyticsVariablePreprocessing[] Preprocessing { get; set; } = [];
        public double[] FillValues { get; set; } = [];
        public int[] MissingCounts { get; set; } = [];
        public NeuralNetworkEvidenceDetails Details { get; set; } = null!;
        public AdvancedAnalyticsWarning[] Warnings { get; set; } = [];
        public string SnapshotJson { get; set; } = string.Empty;

        public int[] TrainRows { get; set; } = [];
        public int[] ValidationRows { get; set; } = [];
        public int[] TestRows { get; set; } = [];

        public AdvancedAnalyticsEvidence<NeuralNetworkEvidenceDetails> ToEvidence()
        {
            return new AdvancedAnalyticsEvidence<NeuralNetworkEvidenceDetails>
            {
                Kind = Kind,
                Seed = Seed,
                Variables = Variables,
                SplitSizes = SplitSizes,
                Preprocessing = Preprocessing,
                Warnings = Warnings,
                Details = Details
            };
        }
    }
}