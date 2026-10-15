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

using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public sealed record ReductionOptions
    {
        public ReductionMode Mode { get; init; } = ReductionMode.None;
        public ReductionScaling Scaling { get; init; } = ReductionScaling.None;
        public double MissingRateThreshold { get; init; } = 0.5;
        public double FrequencyRatioCutoff { get; init; } = 19d;
        public double UniqueValuePercentCutoff { get; init; } = 10d;
        public bool DropZeroMedianAbsoluteDeviation { get; init; }
        public double? CorrelationThreshold { get; init; } = 0.95;
        public ReductionGrouping Grouping { get; init; } = ReductionGrouping.HierarchicalCorrelation;
        public double VariableClusteringEigenvalue { get; init; } = 1d;
        public ReductionProjection Projection { get; init; } = ReductionProjection.None;
        public double ExplainedVariance { get; init; } = 0.95;
        public int CorrelationRowCap { get; init; } = 5000;
        public static ReductionOptions Off => new();

        public static ReductionOptions Recommended => new()
        {
            Mode = ReductionMode.Standard,
            Scaling = ReductionScaling.Robust
        };

        public void Validate()
        {
            if (!(MissingRateThreshold > 0 && MissingRateThreshold <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(MissingRateThreshold), "Must be in (0, 1].");
            }

            if (!(FrequencyRatioCutoff >= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(FrequencyRatioCutoff), "Must be at least 1.");
            }

            if (!(UniqueValuePercentCutoff > 0 && UniqueValuePercentCutoff <= 100))
            {
                throw new ArgumentOutOfRangeException(nameof(UniqueValuePercentCutoff), "Must be in (0, 100].");
            }

            if (CorrelationThreshold is { } c && !(c > 0 && c <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(CorrelationThreshold), "Must be in (0, 1].");
            }

            if (!(ExplainedVariance > 0 && ExplainedVariance <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(ExplainedVariance), "Must be in (0, 1].");
            }

            if (!(VariableClusteringEigenvalue > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(VariableClusteringEigenvalue), "Must be positive.");
            }

            if (CorrelationRowCap < 10)
            {
                throw new ArgumentOutOfRangeException(nameof(CorrelationRowCap), "Must be at least 10.");
            }
        }
    }
}