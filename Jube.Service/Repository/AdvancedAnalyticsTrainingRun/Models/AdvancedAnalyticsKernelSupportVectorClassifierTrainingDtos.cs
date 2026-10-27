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
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Training;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsKernelSupportVectorClassifierTrainingDto
    {
        [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
        public int AskJoobyAnalyticsInstanceId { get; set; }

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
                     "NaN marks a missing value. Every predictor is z-scored to the training mean and standard " +
                     "deviation before the kernel is applied, since a kernel's notion of similarity between two " +
                     "rows depends on their raw numeric scale. Used when the input is inline.")]
        public double[][]? Inputs { get; set; }

        [Description("The outcome for each row in order: 1 for the event of interest, otherwise 0. Used when " +
                     "the input is inline.")]
        public int[]? Labels { get; set; }

        [Description("Name of each predictor column, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each predictor comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("The complexity parameter C of the sequential minimal optimisation solver: how strongly a " +
                     "training row that falls on the wrong side of the margin, or inside it, is penalised. A " +
                     "small C tolerates more such rows in exchange for a wider, smoother margin; a large C fits " +
                     "the training rows more tightly and can end up memorising noise. Must be greater than 0.")]
        public double Complexity { get; set; } = 1.0;

        [Description("Which kernel reshapes the notion of similarity between two standardised rows before the " +
                     "maximum-margin boundary is fitted. Gaussian (the default) treats rows as more similar the " +
                     "closer together they sit in ordinary Euclidean space, falling off smoothly in every " +
                     "direction, and can bend the boundary around a cluster of any shape. Polynomial raises the " +
                     "rows' dot product to a fixed power, which lets the boundary curve following combinations " +
                     "and products of the original predictors up to that power. Sigmoid passes the dot product " +
                     "through a hyperbolic tangent, echoing the squashing function of a single-layer neural " +
                     "network. Laplacian is close to Gaussian but falls off with plain distance rather than " +
                     "squared distance, so it decays more sharply near a training row and more gently far from " +
                     "it, giving comparatively more weight to distant rows.")]
        public KernelSupportVectorKernel Kernel { get; set; } = KernelSupportVectorKernel.Gaussian;

        [Description("The gamma of the Gaussian, Sigmoid or Laplacian kernel: how far the influence of a single " +
                     "training row reaches, with a larger gamma reaching a shorter distance and letting the " +
                     "boundary bend more sharply around individual rows. Ignored for the Polynomial kernel. Zero " +
                     "(the default) resolves automatically to one over the number of predictors at training " +
                     "time.")]
        public double Gamma { get; set; }

        [Description("The degree of the Polynomial kernel: the highest power the predictors' combinations are " +
                     "raised to. Ignored for every other kernel. From 1 (a plain dot product) up to 10; higher " +
                     "degrees allow more intricate boundaries at growing risk of overfitting.")]
        public int Degree { get; set; } = 3;

        [Description("Share of the rows held back to fit the probability calibration, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure quality, from 0 up to but not including 1. " +
                     "Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible.")]
        public int Seed { get; set; } = 1;

        [Description("Whether to fit a Platt calibration of the raw kernel decision function into a probability " +
                     "on the validation rows. The decision function is a signed distance to the separating " +
                     "boundary in kernel feature space, not a probability, so leaving this off means the recalled " +
                     "value has no bounded, probability-like meaning; it should normally be left on.")]
        public bool Calibrate { get; set; } = true;

        [Description("Probability at or above which a row counts as positive in the confusion counts reported " +
                     "in the evidence. Null uses 0.5.")]
        public double? DecisionThreshold { get; set; }
    }
}