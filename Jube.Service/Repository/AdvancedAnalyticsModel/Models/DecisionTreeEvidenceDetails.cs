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

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class DecisionTreeEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Maximum tree depth that was allowed.")]
        public int MaxDepthAllowed { get; set; }

        [Description("Depth the tree actually reached, which may be lower than the allowed maximum when no split " +
                     "improved on stopping early.")]
        public int TreeDepth { get; set; }

        [Description("Number of leaf nodes in the fitted tree.")]
        public int Leaves { get; set; }

        [Description("Total number of nodes, leaves and internal split nodes together.")]
        public int Nodes { get; set; }

        [Description("Minimum training rows a leaf was allowed to hold.")]
        public int MinimumLeafSize { get; set; }

        [Description("The tree's unconditional prediction before any split is applied: the Laplace-smoothed share " +
                     "of training rows with label 1. Every split contribution in the explanation is measured " +
                     "relative to this starting point.")]
        public double RootProbability { get; set; }

        [Description("Quality on the rows the tree was fitted to.")]
        public DecisionTreeSplitMetrics? TrainMetrics { get; set; }

        [Description("Quality on the rows used to fit the probability calibration.")]
        public DecisionTreeSplitMetrics? ValidationMetrics { get; set; }

        [Description("Quality on rows never used for fitting or calibration. This is the honest measure; it is " +
                     "null when no test split was held back.")]
        public DecisionTreeSplitMetrics? TestMetrics { get; set; }

        [Description("Whether probabilities were calibrated and with what slope and intercept.")]
        public bool Calibrated { get; set; }

        [Description("Platt calibration slope on the logit of the raw leaf probability, or null when uncalibrated.")]
        public double? CalibrationSlope { get; set; }

        [Description("Platt calibration intercept, or null when uncalibrated.")]
        public double? CalibrationIntercept { get; set; }

        [Description("Number of validation rows the calibration was fitted on.")]
        public int CalibrationRowsUsed { get; set; }

        [Description("Why calibration was skipped or could not be fitted, or null when it was applied.")]
        public string? CalibrationSkippedReason { get; set; }

        [Description("Every variable ranked by its share of the tree's total impurity reduction; variables never " +
                     "split on appear with zero importance.")]
        public DecisionTreeVariableImportance[] Importances { get; set; } = [];
    }
}