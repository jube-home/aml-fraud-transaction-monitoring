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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class DecisionTreeSplitMetrics
    {
        [Description("Which split this is measured on: Train, Validation or Test.")]
        public string Split { get; set; } = string.Empty;

        [Description("Number of rows in this split.")]
        public int Rows { get; set; }

        [Description("Number of positive rows (label 1) in this split.")]
        public int Positives { get; set; }

        [Description("Area under the ROC curve of the final (calibrated, when calibration succeeded) probability " +
                     "against the label. 0.5 is no better than chance, 1 is a perfect ranking. Null when the split " +
                     "has only one class.")]
        public double? RocAuc { get; set; }

        [Description("Mean negative log likelihood of the label under the final probability. Lower is better; a " +
                     "constant model predicting the training rate scores log(2) at a 50% rate.")]
        public double? LogLoss { get; set; }

        [Description("Mean squared difference between the final probability and the 0/1 label. Lower is better, 0 " +
                     "is perfect.")]
        public double? BrierScore { get; set; }

        [Description("Probability at or above which a row counts as predicted positive for the counts below.")]
        public double Threshold { get; set; }

        [Description("Rows with label 1 correctly predicted positive at the threshold.")]
        public int TruePositives { get; set; }

        [Description("Rows with label 0 wrongly predicted positive at the threshold.")]
        public int FalsePositives { get; set; }

        [Description("Rows with label 0 correctly predicted negative at the threshold.")]
        public int TrueNegatives { get; set; }

        [Description("Rows with label 1 wrongly predicted negative at the threshold.")]
        public int FalseNegatives { get; set; }

        [Description("True positives over predicted positives at the threshold. 0 when nothing was predicted " +
                     "positive.")]
        public double? Precision { get; set; }

        [Description("True positives over actual positives at the threshold. 0 when the split has no positives.")]
        public double? Recall { get; set; }

        [Description("Harmonic mean of precision and recall at the threshold.")]
        public double? F1 { get; set; }

        [Description("Matthews correlation coefficient of the confusion table at the threshold, between -1 and 1; 0 " +
                     "is chance or an undefined denominator.")]
        public double? MatthewsCorrelation { get; set; }
    }
}