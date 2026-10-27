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
    public sealed class AdvancedAnalyticsMeanShiftClustererTrainingDto
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

        [Description("The radius (in the units the rows are fitted in, after standardising when Standardise is " +
                     "true) that every row looks around itself when it climbs towards the nearest peak in the " +
                     "density of the rows. Must be greater than zero. There is no single correct value: a small " +
                     "bandwidth finds many narrow, separate peaks and a large one merges everything into few, " +
                     "broad peaks, so it is usually worth trying a handful of values and comparing the discovered " +
                     "mode count against what the data is expected to contain. Default 2.")]
        public double Bandwidth { get; set; } = 2.0;

        [Description("The density kernel that weighs nearby rows while a row climbs towards a mode: Uniform " +
                     "(default) weighs every row inside the bandwidth equally and is the fastest; Gaussian and " +
                     "Epanechnikov weigh closer rows more heavily, which can smooth the discovered modes at the " +
                     "cost of more computation.")]
        public MeanShiftKernel Kernel { get; set; } = MeanShiftKernel.Uniform;

        [Description("Cap on how many of the nearest rows are considered while a row climbs towards a mode. 0 " +
                     "(default) considers every row within the bandwidth with no cap, which is exact but can be " +
                     "slow on many rows; a positive value trades a little accuracy for speed on larger datasets.")]
        public int MaxNeighbours { get; set; }

        [Description("Maximum number of climbing steps a row may take before it is accepted where it stands even " +
                     "if it has not fully settled. Default 100.")]
        public int MaximumIterations { get; set; } = 100;

        [Description("How little a row may move in one climbing step, relative to the bandwidth, before that " +
                     "row is treated as having reached its mode. Default 0.001.")]
        public double Tolerance { get; set; } = 1e-3;

        [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                     "before clustering, so that the bandwidth is not dominated by whichever variable happens to " +
                     "be measured on the largest numeric scale. Default true.")]
        public bool Standardise { get; set; } = true;

        [Description("Share of the rows held back for a validation diagnostic split, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out mode quality, from 0 up to but not " +
                     "including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible. Mean shift itself has no random starting " +
                     "point, so the same rows and bandwidth always discover the same modes regardless of seed; " +
                     "only which rows fall into the train, validation and test splits depends on it.")]
        public int Seed { get; set; } = 1;
    }
}