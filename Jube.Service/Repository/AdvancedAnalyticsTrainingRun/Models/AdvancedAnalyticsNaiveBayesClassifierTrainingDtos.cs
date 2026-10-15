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
    public sealed class AdvancedAnalyticsNaiveBayesClassifierTrainingDto
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

        [Description("The outcome for each row in order: 1 for the positive class, 0 for the negative class. " +
                     "Both classes must be present. Used when the input is inline.")]
        public int[]? Labels { get; set; }

        [Description("Name of each predictor column, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each predictor comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Share of the rows held back to fit the probability calibration, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure quality, from 0 up to but not including 1. " +
                     "Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible.")]
        public int Seed { get; set; } = 1;

        [Description("The constant used in place of a class-conditional variance that would otherwise be zero, " +
                     "undefined or infinite, expressed as a share of that predictor's overall training variance.")]
        public double VarianceRegularisation { get; set; } = 1e-9;

        [Description("Whether to fit a Platt calibration from the raw log-posterior-odds to a probability on the " +
                     "validation split when both classes are present there.")]
        public bool Calibrate { get; set; } = true;

        [Description("Probability at or above which the confusion counts in the evidence classify a row as the " +
                     "positive class. Purely diagnostic; recall always returns the probability itself.")]
        public double DecisionThreshold { get; set; } = 0.5;
    }
}