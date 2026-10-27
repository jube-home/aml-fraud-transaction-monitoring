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

using System.Text.RegularExpressions;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Service.Query.AdvancedAnalyticsChart;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;

namespace Jube.Service.Charts
{
    internal sealed record AdvancedAnalyticsNarrativeFacts(
        string? ModelName,
        int VersionNumber,
        string Kind,
        string FriendlyKind,
        int Seed,
        IReadOnlyList<string> Variables,
        AdvancedAnalyticsSplitSizes? SplitSizes,
        IReadOnlyList<AdvancedAnalyticsWarning> Warnings,
        AdvancedAnalyticsQualitySummary? Quality,
        bool HasCalibrationCurve,
        int RocPointCount)
    {
        public static AdvancedAnalyticsNarrativeFacts Build(AdvancedAnalyticsChartSource source)
        {
            var evidence = source.Evidence;
            var details = evidence?.GetType().GetProperty("Details")?.GetValue(evidence);

            var hasCalibrationCurve =
                evidence is AdvancedAnalyticsLogisticEvidence { TestMetrics.ReliabilityBins.Length: > 0 };

            return new AdvancedAnalyticsNarrativeFacts(
                source.ModelName,
                source.VersionNumber,
                evidence?.Kind ?? string.Empty,
                FriendlyKindName(evidence?.Kind ?? string.Empty),
                evidence?.Seed ?? 0,
                evidence?.CommonVariables ?? [],
                evidence?.CommonSplitSizes,
                evidence?.CommonWarnings ?? [],
                AdvancedAnalyticsNarrativeMetrics.FromDetails(details),
                hasCalibrationCurve,
                source.Roc.Count);
        }

        private static string FriendlyKindName(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind))
            {
                return "model";
            }

            var trimmed = kind.EndsWith("Pipeline", StringComparison.Ordinal)
                ? kind[..^"Pipeline".Length]
                : kind;

            var spaced = Regex.Replace(trimmed, "(?<=[a-z0-9])(?=[A-Z])", " ");

            return spaced.Length == 0 ? "model" : spaced;
        }
    }
}