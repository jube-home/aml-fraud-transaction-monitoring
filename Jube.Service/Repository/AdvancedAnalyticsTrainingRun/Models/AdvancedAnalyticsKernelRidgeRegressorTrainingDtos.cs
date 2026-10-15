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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsKernelRidgeRegressorTrainingDto
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

        [Description("The continuous outcome for each row, in the same order as the rows.")]
        public double[]? Outputs { get; set; }

        [Description("Name of each predictor column, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each predictor comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("The complexity parameter behind the ridge penalty: the penalty added to the kernel Gram " +
                     "matrix's diagonal before it is solved is one over this value. A small complexity shrinks " +
                     "the fit heavily towards the training mean and tolerates a noisier system; a large " +
                     "complexity fits the training rows more tightly and can end up memorising noise. Must be " +
                     "greater than 0.")]
        public double Complexity { get; set; } = 1.0;

        [Description("Which kernel reshapes the notion of similarity between two standardised rows before the " +
                     "ridge-regularised fit is solved. Gaussian (the default) treats rows as more similar the " +
                     "closer together they sit in ordinary Euclidean space, falling off smoothly in every " +
                     "direction. Polynomial raises the rows' dot product to a fixed power, letting the fitted " +
                     "surface curve following combinations and products of the original predictors up to that " +
                     "power. Sigmoid passes the dot product through a hyperbolic tangent. Laplacian is close to " +
                     "Gaussian but falls off with plain distance rather than squared distance, giving " +
                     "comparatively more weight to distant rows. Linear reduces the fit to an ordinary dot " +
                     "product, so the fitted surface is a flat plane, the same shape an ordinary linear " +
                     "regression would fit but solved through the ridge-regularised kernel system.")]
        public KernelRidgeRegressorKernel Kernel { get; set; } = KernelRidgeRegressorKernel.Gaussian;

        [Description("The gamma of the Gaussian, Sigmoid or Laplacian kernel: how far the influence of a single " +
                     "training row reaches. Ignored for the Polynomial and Linear kernels. Zero (the default) " +
                     "resolves automatically to one over the number of predictors at training time.")]
        public double Gamma { get; set; }

        [Description("The degree of the Polynomial kernel: the highest power the predictors' combinations are " +
                     "raised to. Ignored for every other kernel. From 1 (a plain dot product) up to 10; higher " +
                     "degrees allow more intricate surfaces at growing risk of overfitting.")]
        public int Degree { get; set; } = 3;

        [Description("Share of the rows held back purely to measure quality of the fit, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure quality, from 0 up to but not including 1. " +
                     "Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible.")]
        public int Seed { get; set; } = 1;
    }
}