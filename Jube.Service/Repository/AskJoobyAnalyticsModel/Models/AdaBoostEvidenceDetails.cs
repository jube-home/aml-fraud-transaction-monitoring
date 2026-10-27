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
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class AdaBoostEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of boosting rounds requested.")]
        public int Rounds { get; set; }

        [Description("Number of rounds actually fitted: this can be fewer than the number requested when boosting " +
                     "stopped early because a round's weak learner was no better than chance on that round's " +
                     "reweighted training rows, which ends the series honestly rather than folding in a useless or " +
                     "harmful vote.")]
        public int RoundsFitted { get; set; }

        [Description("Maximum depth allowed for every round's tree. AdaBoost classically boosts weak learners, so " +
                     "this is deliberately shallow (a stump at depth 1, or a small tree at depth 2 or 3), leaving the " +
                     "boosting rounds, not any one tree's depth, to do the work of fitting the data.")]
        public int MaxDepth { get; set; }

        [Description("Minimum number of training rows required on each side of a split in every round's tree.")]
        public int MinimumLeafSize { get; set; }

        [Description("The weighted misclassification rate of each round's tree on the training rows as they were " +
                     "weighted going into that round, in fitting order. A well-behaved series drifts up towards, but " +
                     "stays below, one half as rounds progress, because reweighting keeps steering each new tree onto " +
                     "whatever the ensemble so far still gets wrong.")]
        public double[] RoundErrors { get; set; } = [];

        [Description("Each round's vote weight (alpha), in fitting order: half the log odds of that round's tree " +
                     "being right rather than wrong on its weighted training rows. A confident, low-error tree earns " +
                     "a large alpha and dominates the final vote; a barely-better-than-chance tree earns an alpha " +
                     "close to zero and contributes almost nothing.")]
        public double[] RoundAlphas { get; set; } = [];

        [Description("Quality on the rows the ensemble was fitted to. Compare with the test figures to spot " +
                     "overfitting.")]
        public AdaBoostSplitMetrics? TrainMetrics { get; set; }

        [Description("Quality on the rows used to fit the probability calibration.")]
        public AdaBoostSplitMetrics? ValidationMetrics { get; set; }

        [Description("Quality on rows never used for fitting or calibration. This is the honest measure; it is null " +
                     "when no test split was held back.")]
        public AdaBoostSplitMetrics? TestMetrics { get; set; }

        [Description("Whether the ensemble's raw decision-function margin was calibrated to a probability by Platt " +
                     "scaling.")]
        public bool Calibrated { get; set; }

        [Description("Slope of the Platt calibration fitted on the validation split, or null when not calibrated.")]
        public double? CalibrationSlope { get; set; }

        [Description("Intercept of the Platt calibration fitted on the validation split, or null when not " +
                     "calibrated.")]
        public double? CalibrationIntercept { get; set; }

        [Description("Share of the ensemble's alpha-weighted impurity reduction attributed to each variable, summed " +
                     "over every split in every round's tree and normalised to sum to 1 across variables, in the same " +
                     "order as the model's variables. A split fitted in a high-alpha round counts for more than the " +
                     "same split would in a round that earned little vote weight.")]
        public double[] FeatureImportances { get; set; } = [];
    }
}