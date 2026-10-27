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
    public sealed class GradientBoostingRegressorEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of boosting rounds requested; every round fits one shallow regression tree to the " +
                     "current residuals.")]
        public int Rounds { get; set; }

        [Description("Number of boosting rounds actually fitted; equal to Rounds unless the run was cancelled.")]
        public int TreesFitted { get; set; }

        [Description("Maximum depth allowed for every regression tree.")]
        public int MaxDepth { get; set; }

        [Description("Shrinkage applied to every tree's output before it is added to the running prediction; a " +
                     "smaller learning rate needs more rounds to reach the same fit but generalises better.")]
        public double LearningRate { get; set; }

        [Description("Minimum number of training rows required on each side of a candidate split for it to be " +
                     "accepted.")]
        public int MinimumLeafSize { get; set; }

        [Description("Share of the training rows drawn, without replacement, to fit each round's tree. A value " +
                     "below 1 is stochastic gradient boosting: each tree sees a fresh random subset, which trades a " +
                     "little bias for a steadier, less overfit ensemble and a faster fit.")]
        public double SampleFraction { get; set; }

        [Description("The additive model's starting point before any tree is added: the mean of the training " +
                     "outcome. Every tree's contribution is a shrinkage-scaled adjustment layered on top of this " +
                     "base, so it doubles as the exact base value of the additive, path-contribution explanation.")]
        public double BaseValue { get; set; }

        [Description("Mean squared error of the training rows against the running prediction at the end of every " +
                     "round, in round order; the learning curve of the fit. A curve that flattens well before the " +
                     "last round suggests fewer rounds, or a smaller learning rate, would do just as well.")]
        public double[] TrainingLossByRound { get; set; } = [];

        [Description("Quality on the rows the ensemble was fitted to. Compare with the test figures to spot " +
                     "overfitting.")]
        public GradientBoostingRegressorSplitMetrics? TrainMetrics { get; set; }

        [Description("Quality on the rows held back as a validation split.")]
        public GradientBoostingRegressorSplitMetrics? ValidationMetrics { get; set; }

        [Description("Quality on rows never used for fitting. This is the honest measure; it is null when no test " +
                     "split was held back.")]
        public GradientBoostingRegressorSplitMetrics? TestMetrics { get; set; }

        [Description("Total variance reduction attributed to each variable, summed over every split in every tree " +
                     "and normalised to sum to 1 across variables, in the same order as the model's variables.")]
        public double[] FeatureImportances { get; set; } = [];
    }
}