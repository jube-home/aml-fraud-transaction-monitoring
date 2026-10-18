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
    public sealed class BaggingEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of decision trees fitted, each on its own bootstrap sample.")]
        public int TreeCount { get; set; }

        [Description("Maximum depth allowed for every tree.")]
        public int MaxDepth { get; set; }

        [Description("Minimum number of training rows required on each side of a split.")]
        public int MinLeafSize { get; set; }

        [Description("Share of the training rows drawn, with replacement, into each tree's bootstrap sample.")]
        public double SampleFraction { get; set; }

        [Description("Area under the ROC curve of the out-of-bag predictions on the training split: for each row, " +
                     "the average prediction of only the trees whose bootstrap sample happened to leave it out. This " +
                     "is an honest, no-extra-cost estimate of generalisation. Null when too few training rows were " +
                     "ever left out of bag to compute it.")]
        public double? OutOfBagRocAuc { get; set; }

        [Description("Quality on the rows the ensemble was fitted to. Compare with the test figures to spot " +
                     "overfitting.")]
        public BaggingSplitMetrics? TrainMetrics { get; set; }

        [Description("Quality on the rows used to fit the probability calibration.")]
        public BaggingSplitMetrics? ValidationMetrics { get; set; }

        [Description("Quality on rows never used for fitting or calibration. This is the honest measure; it is null " +
                     "when no test split was held back.")]
        public BaggingSplitMetrics? TestMetrics { get; set; }

        [Description("Whether the ensemble's raw vote fraction was calibrated to a probability by Platt scaling.")]
        public bool Calibrated { get; set; }

        [Description("Slope of the Platt calibration fitted on the validation split, or null when not calibrated.")]
        public double? CalibrationSlope { get; set; }

        [Description("Intercept of the Platt calibration fitted on the validation split, or null when not " +
                     "calibrated.")]
        public double? CalibrationIntercept { get; set; }

        [Description("Mean decrease in node impurity attributed to each variable, summed over every split in every " +
                     "tree and normalised to sum to 1 across variables, in the same order as the model's variables. " +
                     "This is a global, ensemble-wide importance, distinct from the per-row contributions in an " +
                     "individual recall's explanation.")]
        public double[] FeatureImportances { get; set; } = [];
    }
}