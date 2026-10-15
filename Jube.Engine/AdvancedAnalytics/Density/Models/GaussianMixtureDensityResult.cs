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

namespace Jube.Engine.AdvancedAnalytics.Density.Models
{
    public sealed record GaussianMixtureDensityComponentSummary(
        double Weight,
        double[] Mean,
        double[][] Covariance);

    public sealed class GaussianMixtureDensityResult
    {
        public required string Kind { get; init; }
        public required int Seed { get; init; }
        public required GaussianMixtureDensityOptions Options { get; init; }
        public required string[] Variables { get; init; }
        public required AdvancedAnalyticsSplitSizes SplitSizes { get; init; }
        public required AdvancedAnalyticsVariablePreprocessing[] Preprocessing { get; init; }
        public required GaussianMixtureDensityEvidenceDetails Details { get; init; }
        public required AdvancedAnalyticsWarning[] Warnings { get; init; }
        public required string SnapshotJson { get; init; }

        public int[] TrainRows { get; init; } = [];
        public int[] ValidationRows { get; init; } = [];
        public int[] TestRows { get; init; } = [];

        public AdvancedAnalyticsEvidence<GaussianMixtureDensityEvidenceDetails> ToEvidence()
        {
            return new AdvancedAnalyticsEvidence<GaussianMixtureDensityEvidenceDetails>
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