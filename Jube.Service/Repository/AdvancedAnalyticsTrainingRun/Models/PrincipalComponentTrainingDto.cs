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
    public sealed class PrincipalComponentTrainingDto
    {
        [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
        public int AskJoobyAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value. Principal component analysis has no labels: every column is a " +
                     "feature.")]
        public double[][]? Inputs { get; set; }

        [Description("Name of each variable, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                     "before the fit, so that a variable measured on a larger numeric scale does not dominate the " +
                     "components purely because of its units. This is principal component analysis on the " +
                     "correlation matrix rather than the covariance matrix. Default true.")]
        public bool Standardise { get; set; } = true;

        [Description("Cumulative share of the total variance the retained components must explain together, " +
                     "strictly greater than 0 and at most 1. Components are kept, most important first, until " +
                     "this share is reached. Default 0.95.")]
        public double VarianceThreshold { get; set; } = 0.95;

        [Description("Optional hard cap on how many components are retained, applied after the variance " +
                     "threshold; whichever limit is reached first wins. Null leaves the count to the variance " +
                     "threshold alone.")]
        public int? MaximumComponents { get; set; }

        [Description("Share of the rows held back for a validation diagnostic split rather than used to fit the " +
                     "components, from 0 up to but not including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out reconstruction error, from 0 up to " +
                     "but not including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible.")]
        public int Seed { get; set; } = 1;
    }
}