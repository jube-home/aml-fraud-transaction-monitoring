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

using System.Collections.Generic;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Models;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Engine.AdvancedAnalytics.Evidence
{
    public sealed record AdvancedAnalyticsSplitSizes(int Train, int Validation, int Test);

    public sealed record AdvancedAnalyticsVariablePreprocessing(
        string Name,
        double Mean,
        double StandardDeviation,
        bool Excluded,
        string ExclusionReason,
        VariableTransformationKind Transformation = VariableTransformationKind.None);

    public sealed record AdvancedAnalyticsWarning(string Code, string Message);

    public sealed class AdvancedAnalyticsEvidence<TDetails> : IAdvancedAnalyticsEvidence
        where TDetails : IAdvancedAnalyticsEvidenceDetails
    {
        public string[] Variables { get; set; } = [];
        public AdvancedAnalyticsSplitSizes SplitSizes { get; set; } = null!;
        public AdvancedAnalyticsVariablePreprocessing[] Preprocessing { get; set; } = [];
        public AdvancedAnalyticsWarning[] Warnings { get; set; } = [];
        public TDetails Details { get; set; } = default!;
        public string Kind { get; set; } = string.Empty;
        public int Seed { get; set; }

        IReadOnlyList<string> IAdvancedAnalyticsEvidence.CommonVariables => Variables;
        AdvancedAnalyticsSplitSizes IAdvancedAnalyticsEvidence.CommonSplitSizes => SplitSizes;
        IReadOnlyList<AdvancedAnalyticsWarning> IAdvancedAnalyticsEvidence.CommonWarnings => Warnings;

        IReadOnlyList<AdvancedAnalyticsVariablePreprocessing> IAdvancedAnalyticsEvidence.CommonPreprocessing =>
            Preprocessing;
    }
}