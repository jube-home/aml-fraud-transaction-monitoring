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
    public sealed record GradientBoostingRegressorSplitMetrics(
        [property: Description("Which split this is: Train, Validation or Test.")]
        string Split,
        [property: Description("Number of rows in this split.")]
        int Rows,
        [property: Description("Coefficient of determination on this split: the share of the outcome's variance " +
                               "explained by the ensemble, from a ceiling of 1 downward, and negative when the " +
                               "ensemble does worse than always predicting the split's own mean.")]
        double? RSquared,
        [property: Description("R squared adjusted for the number of variables offered to the ensemble, penalising a " +
                               "wide variable set the way ordinary least squares' adjusted R squared does. Tree " +
                               "ensembles do not spend degrees of freedom the same way a linear fit does, so read " +
                               "this as a familiar, conservative cross-check rather than an exact accounting.")]
        double? AdjustedRSquared,
        [property: Description("Root mean squared error of the predictions on this split, in the outcome's own " +
                               "units; lower is better.")]
        double? RootMeanSquaredError,
        [property: Description("Mean absolute error of the predictions on this split, in the outcome's own units; " +
                               "less sensitive to a handful of large misses than the root mean squared error.")]
        double? MeanAbsoluteError,
        [property: Description("Mean signed error (actual minus predicted, averaged) on this split; a value far from " +
                               "zero shows the ensemble is systematically over- or under-predicting on this split.")]
        double? MeanError);
}