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
    public sealed record DecisionTreeRegressorSplitMetrics(
        [property: Description("Which split this is measured on: Train, Validation or Test.")]
        string Split,
        [property: Description("Number of rows in this split.")]
        int Rows,
        [property: Description(
            "Coefficient of determination of the predicted value against the actual outcome: 1 is a " +
            "perfect fit, 0 matches always predicting the training mean, and it can run negative when the " +
            "tree does worse than that. Null when the split has no variation in the outcome.")]
        double? RSquared,
        [property: Description(
            "R squared adjusted for the number of leaves the tree fitted, penalising a tree that only " +
            "looks better because it has more leaves to work with. Null whenever R squared itself is null " +
            "or the split is too small relative to the leaf count for the adjustment to be defined.")]
        double? AdjustedRSquared,
        [property: Description(
            "Root mean squared error between the predicted and actual outcome, in the same units as the " +
            "outcome. Lower is better, 0 is a perfect fit.")]
        double? RootMeanSquaredError,
        [property: Description(
            "Mean absolute error between the predicted and actual outcome, in the same units as the " +
            "outcome, less sensitive to a handful of large misses than the root mean squared error.")]
        double? MeanAbsoluteError,
        [property: Description(
            "Mean signed error, actual minus predicted, averaged over the split. Positive means the tree " +
            "tends to under-predict here, negative means it tends to over-predict; a well fitted split " +
            "should sit close to zero.")]
        double? MeanError);
}