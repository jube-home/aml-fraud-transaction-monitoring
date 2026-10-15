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
    public sealed class AdvancedAnalyticsGradientBoostingRegressorTrainingDto
    {
        [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value.")]
        public double[][]? Inputs { get; set; }

        [Description("The continuous outcome for each row, in the same order as the rows.")]
        public double[]? Outputs { get; set; }

        [Description("Name of each predictor column, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each predictor comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of boosting rounds: every round fits one shallow regression tree to the residual " +
                     "between the outcome and the ensemble's current prediction, then adds a shrunk copy of that " +
                     "tree's output onto the running prediction. More rounds keep reducing the training loss, so " +
                     "the honest stopping point is read from the test metrics rather than assumed. Default 100.")]
        public int Rounds { get; set; } = 100;

        [Description("Maximum number of splits from the root to a leaf in every round's regression tree. " +
                     "Gradient boosting deliberately uses shallow trees, each one only a small, weak correction " +
                     "that many rounds accumulate; a deeper tree fits sharper local patterns in a single round " +
                     "but leaves less room for the boosting process itself to do the learning. Default 3.")]
        public int MaxDepth { get; set; } = 3;

        [Description("Shrinkage factor multiplying every tree's output before it is added to the running " +
                     "prediction. A smaller learning rate needs more rounds to reach the same fit but generally " +
                     "generalises better, trading fitting time for a steadier ensemble; a larger rate reaches a " +
                     "given training loss in fewer rounds but is more prone to overfitting individual rounds' " +
                     "noise. Default 0.1.")]
        public double LearningRate { get; set; } = 0.1;

        [Description("Minimum number of training rows required on each side of a candidate split in a round's " +
                     "regression tree for it to be accepted; a larger value forces coarser, more stable splits. " +
                     "Default 5.")]
        public int MinimumLeafSize { get; set; } = 5;

        [Description("Share of the training rows drawn, without replacement, to fit each round's regression " +
                     "tree. The default of 1 fits every round on the whole training split; a smaller share is " +
                     "stochastic gradient boosting, giving every tree a fresh random subset of rows so the " +
                     "rounds are less correlated with one another, at the cost of a noisier per-round fit. " +
                     "Default 1.")]
        public double SampleFraction { get; set; } = 1d;

        [Description("Share of the rows held back for a validation diagnostic split, reported alongside the " +
                     "training and test metrics but not used to fit the trees, from 0 up to but not including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out quality, from 0 up to but not " +
                     "including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split and every round's row subsample reproducible.")]
        public int Seed { get; set; } = 1;
    }
}