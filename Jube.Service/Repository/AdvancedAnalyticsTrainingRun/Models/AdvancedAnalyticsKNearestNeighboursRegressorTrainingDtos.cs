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

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsKNearestNeighboursRegressorTrainingDto
    {
        [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value. Every training row is retained inside the model itself, so a " +
                     "recall searches these very rows for the ones nearest to whatever is being scored.")]
        public double[][]? Inputs { get; set; }

        [Description("The continuous target for each row, in the same order as the rows.")]
        public double[]? Outputs { get; set; }

        [Description("Name of each predictor column, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each predictor comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of nearest training rows (k) whose targets are averaged to predict a new row's " +
                     "target, from 1 up to the number of training rows. A small k follows the local shape of the " +
                     "training rows closely but is noisy and sensitive to an unusual neighbour; a large k gives a " +
                     "smoother, more stable average but blurs across whatever structure relates the predictors to " +
                     "the target, eventually washing it out altogether. There is no single correct choice; it " +
                     "should be set by comparing held-out performance at a few values.")]
        public int NeighbourCount { get; set; } = 15;

        [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                     "before distances are computed. Distance-based methods such as this one measure how far " +
                     "apart two rows are using every variable's raw numeric scale at once, so a variable that " +
                     "happens to be recorded in larger units, such as an amount in the thousands next to a ratio " +
                     "between zero and one, would dominate every distance calculation purely because of its " +
                     "units rather than because it is actually more informative. Standardising first puts every " +
                     "variable on the same footing.")]
        public bool Standardise { get; set; } = true;

        [Description("Share of the rows held back for a validation diagnostic split, reported alongside the " +
                     "training and test metrics but never averaged into a prediction, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure quality, from 0 up to but not including 1. " +
                     "Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible.")]
        public int Seed { get; set; } = 1;
    }
}