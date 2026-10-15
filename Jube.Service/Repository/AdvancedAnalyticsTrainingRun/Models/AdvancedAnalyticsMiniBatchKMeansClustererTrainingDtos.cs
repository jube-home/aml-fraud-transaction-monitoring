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
    public sealed class AdvancedAnalyticsMiniBatchKMeansClustererTrainingDto
    {
        [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

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

        [Description("Number of rows drawn into each random mini-batch used to update the centroids on every " +
                     "iteration, from 1 up to the number of training rows. Smaller batches make each iteration " +
                     "cheaper but noisier; larger batches behave more like ordinary k-means at a higher cost per " +
                     "iteration. Default 100.")]
        public int BatchSize { get; set; } = 100;

        [Description("Number of rows used, per candidate centroid set, to seed and then score the starting " +
                     "centroids before mini-batch training begins, at least the number of clusters. Leave unset " +
                     "to default to three times the cluster count, automatically capped at the number of training " +
                     "rows.")]
        public int? InitializationBatchSize { get; set; }

        [Description("Number of independent random candidate centroid sets tried before training begins, keeping " +
                     "whichever has the lowest distortion on its own held-out batch. Higher values reduce the " +
                     "chance of starting from an unlucky set of centroids at the cost of more setup time. " +
                     "Default 1.")]
        public int NumberOfInitializations { get; set; } = 1;

        [Description("KMeansPlusPlus (default, spreads the starting centres) or Uniform (random rows).")]
        public KMeansSeeding Seeding { get; set; } = KMeansSeeding.KMeansPlusPlus;

        [Description("Maximum number of mini-batch iterations. Default 100.")]
        public int MaximumIterations { get; set; } = 100;

        [Description("Convergence tolerance on the relative change in centres between one iteration and the " +
                     "next. Default 0.00001.")]
        public double Tolerance { get; set; } = 1e-5;

        [Description("Whether variables are z-scored with training statistics before clustering. Default true.")]
        public bool Standardise { get; set; } = true;

        [Description("Share of the rows held back for a validation diagnostic split, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out cluster quality, from 0 up to but " +
                     "not including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split, the mini-batch draws and the starting centres " +
                     "reproducible.")]
        public int Seed { get; set; } = 1;
    }
}