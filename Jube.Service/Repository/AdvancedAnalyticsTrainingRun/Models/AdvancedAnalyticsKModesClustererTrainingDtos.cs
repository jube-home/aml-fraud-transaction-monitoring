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
using Jube.Engine.AdvancedAnalytics.Clustering.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsKModesClustererTrainingDto
    {
        [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
        public int AskJoobyAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "Every value must already be a small integer-coded category (for example a country code or " +
                     "a transaction-type code), not a continuous measurement; a value that is not a whole number " +
                     "is rounded to the nearest one. NaN marks a missing value. Clustering has no labels: every " +
                     "column is a category that contributes to the mismatch count between rows.")]
        public double[][]? Inputs { get; set; }

        [Description("Name of each variable, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of clusters to fit, from 1 up to the number of rows.")]
        public int Clusters { get; set; } = 3;

        [Description("KModesPlusPlus (default, spreads the starting modes) or Uniform (random rows).")]
        public KModesSeeding Seeding { get; set; } = KModesSeeding.KModesPlusPlus;

        [Description("Maximum number of assign-then-recompute-mode iterations. Default 100.")]
        public int MaximumIterations { get; set; } = 100;

        [Description("Convergence tolerance on the change in the modes between one iteration and the next. " +
                     "Because every mode is a whole-number category, in practice the fit stops as soon as no " +
                     "mode changes at all. Default 0.00001.")]
        public double Tolerance { get; set; } = 1e-5;

        [Description("Share of the rows held back for a validation diagnostic split, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out cluster quality, from 0 up to but " +
                     "not including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split and the choice of starting modes reproducible.")]
        public int Seed { get; set; } = 1;
    }
}