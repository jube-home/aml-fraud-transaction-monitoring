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
    public sealed class AdvancedAnalyticsKMedoidsClustererTrainingDto
    {
        [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
        public int AskJoobyAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value. Clustering has no labels: every column is a feature.")]
        public double[][]? Inputs { get; set; }

        [Description("Name of each variable, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of clusters to fit, from 1 up to the number of rows.")]
        public int Clusters { get; set; } = 3;

        [Description("PamBuild (default, a greedy build that picks the most representative rows as starting " +
                     "medoids) or Uniform (random rows).")]
        public KMedoidsSeeding Seeding { get; set; } = KMedoidsSeeding.PamBuild;

        [Description("Maximum number of swap iterations. Default 100.")]
        public int MaximumIterations { get; set; } = 100;

        [Description("Whether every variable is z-scored with training statistics before clustering. Default true.")]
        public bool Standardise { get; set; } = true;

        [Description("Share of the rows held back for a validation diagnostic split, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out cluster quality, from 0 up to but " +
                     "not including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split and the choice of starting medoids reproducible.")]
        public int Seed { get; set; } = 1;
    }
}