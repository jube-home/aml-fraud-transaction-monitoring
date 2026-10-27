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

using System.ComponentModel;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class AdvancedAnalyticsLogisticEvidence : IAdvancedAnalyticsEvidence
    {
        [Description("Names of the predictors in the order the model expects them.")]
        public string[] Variables { get; set; } = [];

        [Description("How many rows, and how many positives, sat in the train, validation and test splits.")]
        public LogisticSplitSizes SplitSizes { get; set; } = null!;

        [Description("Quality on the rows the model was fitted to. Compare with the test figures to spot " +
                     "overfitting.")]
        public LogisticSplitMetrics? TrainMetrics { get; set; }

        [Description("Quality on the rows used to fit the probability calibration.")]
        public LogisticSplitMetrics? ValidationMetrics { get; set; }

        [Description("Quality on rows never used for fitting or calibration. This is the honest measure; it is " +
                     "null when no test split was held back.")]
        public LogisticSplitMetrics? TestMetrics { get; set; }

        [Description("Intercept and one row per predictor with estimate, standard error, p-value and odds ratio.")]
        public LogisticCoefficient[] Coefficients { get; set; } = [];

        [Description("How each predictor was cleaned and scaled, including missing counts and variance " +
                     "inflation.")]
        public LogisticVariablePreprocessing[] Preprocessing { get; set; } = [];

        [Description("Whether probabilities were calibrated and with what slope and intercept.")]
        public LogisticCalibrationInfo Calibration { get; set; } = null!;

        [Description("The event rates the probabilities are expressed at.")]
        public LogisticPriors Priors { get; set; } = null!;

        [Description("Whether the fit converged, and how many iterations it needed.")]
        public LogisticFitInfo Fit { get; set; } = null!;

        [Description("Problems the trainer detected. Any of them can be a reason not to make the model live.")]
        public LogisticWarning[] Warnings { get; set; } = [];

        [Description("The model family that produced this evidence.")]
        public string Kind { get; set; } = string.Empty;

        [Description("Seed that fixed the split of rows, so the same seed reproduces the same held-out rows.")]
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