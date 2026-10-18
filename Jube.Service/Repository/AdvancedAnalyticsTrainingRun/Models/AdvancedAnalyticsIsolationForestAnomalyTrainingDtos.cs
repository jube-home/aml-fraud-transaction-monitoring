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
    public sealed class AdvancedAnalyticsIsolationForestAnomalyTrainingDto
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

        [Description("Optional outcome for each row in order: 1 for a known anomaly, 0 for known normal. Leave " +
                     "empty to train unsupervised on rows assumed mostly normal; when given, and both a normal " +
                     "and an anomalous row fall in the validation split, the raw score is calibrated into a " +
                     "probability.")]
        public int[]? Labels { get; set; }

        [Description("Name of each predictor column, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each predictor comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of random trees grown for the forest. Default 100.")]
        public int TreeCount { get; set; } = 100;

        [Description("Number of rows drawn without replacement to grow each tree, capped at the number of " +
                     "training rows available. Default 256, the value used in the original isolation forest " +
                     "paper.")]
        public int SubsampleSize { get; set; } = 256;

        [Description("Share of the rows held back to fit the probability calibration when labels are given, from " +
                     "0 up to but not including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure quality, from 0 up to but not including 1. " +
                     "Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the per-tree subsampling and splits, and the row split, reproducible.")]
        public int Seed { get; set; } = 1;

        [Description("Whether to fit a Platt calibration from the raw score to a probability when labels are " +
                     "given and both classes fall in the validation split.")]
        public bool Calibrate { get; set; } = true;
    }
}