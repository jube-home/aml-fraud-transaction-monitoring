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
    public sealed class AdvancedAnalyticsRandomForestRegressorTrainingDto
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

        [Description("Name of each variable, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of regression trees to bag into the forest. More trees give a steadier, lower " +
                     "variance average prediction at a roughly linear cost in fitting time, with diminishing " +
                     "returns well before this default. Default 100.")]
        public int TreeCount { get; set; } = 100;

        [Description("Maximum number of splits from the root to a leaf in every tree. A deeper tree can fit " +
                     "sharper, more local patterns but is more likely to memorise noise in its own bootstrap " +
                     "sample; a forest tolerates deeper trees than a single tree would because their individual " +
                     "overfitting tends to average out across trees. Default 6.")]
        public int MaxDepth { get; set; } = 6;

        [Description("Minimum number of training rows required on each side of a candidate split for it to be " +
                     "accepted; a larger value forces coarser, more stable splits and shorter effective trees. " +
                     "Default 5.")]
        public int MinimumLeafSize { get; set; } = 5;

        [Description("Share of the variables randomly offered as split candidates at every node, decorrelating " +
                     "the trees so the forest's average is steadier than any one tree: with the default of 0 the " +
                     "count is one third of the variable count, the usual rule of thumb for regression forests " +
                     "(as opposed to the square root used for classification); a value between 0 (exclusive) and " +
                     "1 sets the share directly, with 1 offering every variable at every split (a bagged ensemble " +
                     "of unrestricted trees rather than a true random forest).")]
        public double FeatureFraction { get; set; }

        [Description("Share of the training rows drawn, with replacement, into each tree's bootstrap sample. The " +
                     "default of 1 draws as many rows as the training split holds (some repeated, some left out); " +
                     "a smaller share makes each tree see less data and the trees more different from one " +
                     "another, and leaves more rows out of bag to estimate held-out quality from. Default 1.")]
        public double SampleFraction { get; set; } = 1d;

        [Description("Share of the rows held back for a validation split, reported alongside the training and " +
                     "test metrics but never used to grow a tree, from 0 up to but not including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out quality, from 0 up to but not " +
                     "including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split and every tree's bootstrap sample and split-candidate " +
                     "selection reproducible.")]
        public int Seed { get; set; } = 1;
    }
}