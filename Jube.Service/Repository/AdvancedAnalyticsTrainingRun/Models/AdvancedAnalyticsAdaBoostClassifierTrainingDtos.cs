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
using Jube.Engine.AdvancedAnalytics.Training;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsAdaBoostClassifierTrainingDto
    {
        [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Where the training rows come from. Leave empty to train on the Inputs and Labels given here " +
                     "(inline). Set the kind to CachePointer with a cache pointer to train on rows held in the " +
                     "Advanced Analytics cache, whose last column is the label unless a label column is named. " +
                     "The Dataset kind is reserved and is not supported yet.")]
        public AdvancedAnalyticsTrainingInput? Input { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value. Used when the input is inline.")]
        public double[][]? Inputs { get; set; }

        [Description("The outcome for each row, in the same order as the rows: 1 for the event of interest and " +
                     "0 otherwise. Used when the input is inline.")]
        public int[]? Labels { get; set; }

        [Description("Name of each variable, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of boosting rounds: each round fits one shallow tree on the full training split, " +
                     "weighted to favour whatever the ensemble so far still gets wrong, and casts a vote sized by " +
                     "how well that round's tree did. A round may fit fewer than this number when a tree turns " +
                     "out no better than chance, which ends the series rather than folding in a useless vote. " +
                     "Default 100.")]
        public int Rounds { get; set; } = 100;

        [Description("Maximum number of splits from the root to a leaf in every round's tree. AdaBoost classically " +
                     "boosts weak learners rather than strong ones, so this is deliberately shallow: 1 fits a " +
                     "single-split stump, 2 or 3 a small tree; a deep tree fitted in an early round can already " +
                     "separate the training rows well enough that later rounds have nothing left to correct, " +
                     "which defeats the point of boosting. Default 2.")]
        public int MaxDepth { get; set; } = 2;

        [Description("Minimum number of training rows required on each side of a candidate split for it to be " +
                     "accepted; a larger value forces coarser, more stable splits and shorter effective trees. " +
                     "Default 5.")]
        public int MinimumLeafSize { get; set; } = 5;

        [Description("Whether the ensemble's raw decision-function margin (from -1, unanimous vote for the " +
                     "negative class, to +1, unanimous vote for the positive class) is calibrated to a " +
                     "probability by Platt scaling, fitted on the validation split, exactly as the other " +
                     "classifier families are. Default true.")]
        public bool Calibrate { get; set; } = true;

        [Description("Probability threshold used to turn a calibrated probability into a positive/negative " +
                     "decision for the confusion counts in the evidence. Defaults to 0.5 when not given.")]
        public double? DecisionThreshold { get; set; }

        [Description("Share of the rows held back for a validation split used to fit the probability " +
                     "calibration, from 0 up to but not including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out quality, from 0 up to but not " +
                     "including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible. The boosting rounds themselves have nothing " +
                     "random left to seed: every round exhaustively searches every variable for its best weighted " +
                     "split, so the same rows and weights always fit the same tree.")]
        public int Seed { get; set; } = 1;
    }
}