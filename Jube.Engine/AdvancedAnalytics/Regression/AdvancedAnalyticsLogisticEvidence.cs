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
using System.Linq;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class AdvancedAnalyticsLogisticEvidence : IAdvancedAnalyticsEvidence
    {
        public string[] Variables { get; set; } = [];
        public LogisticSplitSizes SplitSizes { get; set; } = null!;
        public LogisticSplitMetrics TrainMetrics { get; set; }
        public LogisticSplitMetrics ValidationMetrics { get; set; }
        public LogisticSplitMetrics TestMetrics { get; set; }
        public LogisticCoefficient[] Coefficients { get; set; } = [];
        public LogisticVariablePreprocessing[] Preprocessing { get; set; } = [];
        public LogisticCalibrationInfo Calibration { get; set; } = null!;
        public LogisticPriors Priors { get; set; } = null!;
        public LogisticFitInfo Fit { get; set; } = null!;
        public LogisticWarning[] Warnings { get; set; } = [];
        public string Kind { get; set; } = string.Empty;
        public int Seed { get; set; }

        IReadOnlyList<string> IAdvancedAnalyticsEvidence.CommonVariables => Variables;

        AdvancedAnalyticsSplitSizes IAdvancedAnalyticsEvidence.CommonSplitSizes =>
            new AdvancedAnalyticsSplitSizes(SplitSizes.Train, SplitSizes.Validation,
                SplitSizes.Test);

        IReadOnlyList<AdvancedAnalyticsWarning> IAdvancedAnalyticsEvidence.CommonWarnings =>
            [.. Warnings.Select(w => new AdvancedAnalyticsWarning(w.Code, w.Message))];

        IReadOnlyList<AdvancedAnalyticsVariablePreprocessing> IAdvancedAnalyticsEvidence.CommonPreprocessing =>
        [
            .. Preprocessing.Select(p =>
                new AdvancedAnalyticsVariablePreprocessing(p.Name, p.Mean, p.StandardDeviation, p.Excluded,
                    p.ExclusionReason))
        ];
    }
}