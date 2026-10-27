/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
    public sealed record GradientBoostingSplitMetrics(
        [property: Description("Which split this is: Train, Validation or Test.")]
        string Split,
        [property: Description("Number of rows in this split.")]
        int Rows,
        [property: Description("Number of positive rows (label 1) in this split.")]
        int Positives,
        [property: Description("Area under the ROC curve on this split, or null when the split has only one class.")]
        double? RocAuc,
        [property: Description("Average precision (area under the precision-recall curve) on this split.")]
        double? AveragePrecision,
        [property: Description("Log loss (cross entropy) of the calibrated probabilities on this split; lower is " +
                               "better.")]
        double? LogLoss,
        [property: Description("Brier score (mean squared error of the calibrated probabilities against the 0/1 " +
                               "label) on this split; lower is better.")]
        double? BrierScore,
        [property: Description("The probability threshold used to turn a probability into a positive/negative " +
                               "decision for the confusion counts below.")]
        double Threshold,
        [property: Description("Rows with label 1 correctly predicted positive at the threshold.")]
        int TruePositives,
        [property: Description("Rows with label 0 incorrectly predicted positive at the threshold.")]
        int FalsePositives,
        [property: Description("Rows with label 0 correctly predicted negative at the threshold.")]
        int TrueNegatives,
        [property: Description("Rows with label 1 incorrectly predicted negative at the threshold.")]
        int FalseNegatives,
        [property: Description("True positives over predicted positives at the threshold.")]
        double? Precision,
        [property: Description("True positives over actual positives at the threshold.")]
        double? Recall,
        [property: Description("Harmonic mean of precision and recall at the threshold.")]
        double? F1,
        [property: Description("Matthews correlation coefficient of the confusion matrix at the threshold, from -1 " +
                               "to 1.")]
        double? Mcc);
}