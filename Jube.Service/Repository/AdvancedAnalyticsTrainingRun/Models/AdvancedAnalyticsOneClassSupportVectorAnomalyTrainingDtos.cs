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
    public sealed class AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingDto
    {
        [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value. Every predictor is z-scored to the training mean and standard " +
                     "deviation before the kernel is applied, since a kernel's notion of similarity between two " +
                     "rows depends on their raw numeric scale.")]
        public double[][]? Inputs { get; set; }

        [Description("Optional outcome for each row in order: 1 for a known anomaly, 0 for known normal. The " +
                     "boundary itself is always fitted unsupervised on the rows alone; a label is used only, " +
                     "after fitting, to calibrate the raw score into a probability and to report held-out " +
                     "quality, never to influence where the boundary is drawn. Leave empty to train without " +
                     "either of those two secondary uses.")]
        public int[]? Labels { get; set; }

        [Description("Name of each predictor column, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each predictor comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("The expected fraction of training rows the fitted boundary is allowed to treat as margin " +
                     "errors, and so a lower bound on the fraction of rows that end up kept as support vectors. " +
                     "A small Nu draws a wide, generous boundary around almost every training row, at the cost " +
                     "of only reacting to the more flagrant departures from it; a large Nu draws a tight " +
                     "boundary that already treats a good share of the training rows as unusual. Must be " +
                     "strictly between 0 and 1.")]
        public double Nu { get; set; } = 0.1;

        [Description("The gamma of the Gaussian kernel: how far the influence of a single training row reaches, " +
                     "with a larger gamma reaching a shorter distance and letting the boundary bend more sharply " +
                     "around individual rows. Zero (the default) resolves automatically to one over the number " +
                     "of predictors at training time.")]
        public double Gamma { get; set; }

        [Description("Share of the rows held back to fit the probability calibration when labels are given, from " +
                     "0 up to but not including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure quality, from 0 up to but not including 1. " +
                     "Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible.")]
        public int Seed { get; set; } = 1;

        [Description("Whether to fit a Platt calibration from the raw score to a probability when labels are " +
                     "given and both classes fall in the validation split.")]
        public bool Calibrate { get; set; } = true;
    }
}