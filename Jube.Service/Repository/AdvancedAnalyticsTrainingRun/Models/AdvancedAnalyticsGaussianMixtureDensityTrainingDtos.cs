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
    public sealed class AdvancedAnalyticsGaussianMixtureDensityTrainingDto
    {
        [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value. Density estimation is unsupervised: every column is a variable " +
                     "describing the shape of the data, there is no label.")]
        public double[][]? Inputs { get; set; }

        [Description("Name of each variable, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of Gaussian components mixed together to describe the rows' shape. Each component " +
                     "is its own weighted multivariate Gaussian bump; more components can trace a more irregular, " +
                     "multi-modal shape at the cost of needing more rows to fit reliably and being easier to " +
                     "over-fit. Compare BIC and AIC across a few component counts rather than guessing one.")]
        public int Components { get; set; } = 3;

        [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                     "before the fit, so a variable measured on a larger numeric scale does not dominate the " +
                     "fitted covariances purely because of its units. Default true.")]
        public bool Standardise { get; set; } = true;

        [Description("Maximum number of Expectation-Maximisation iterations. Default 100.")]
        public int MaxIterations { get; set; } = 100;

        [Description("Convergence tolerance on the Expectation-Maximisation log-likelihood improvement. Default 1e-5.")]
        public double Tolerance { get; set; } = 1e-5;

        [Description("Each component's covariance matrix must be inverted to score a row, and with few rows " +
                     "relative to the number of variables, or with near-duplicate variables, a component's raw " +
                     "covariance can be singular or numerically unstable to invert. This shrinkage parameter is " +
                     "the ridge added to a component's covariance diagonal, as a share of its average variance, " +
                     "used both during the fit itself and, only if still needed afterwards, again when the " +
                     "fitted covariance is inverted for recall. Default 1e-6.")]
        public double RegularizationEpsilon { get; set; } = 1e-6;

        [Description("Share of the rows held back for a validation diagnostic split, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out log-likelihood, from 0 up to but " +
                     "not including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split reproducible. The mixture fit itself is only reproducible " +
                     "at single-threaded parallelism and, even then, is not guaranteed to reproduce bit for bit " +
                     "between runs; treat repeated fits on the same rows as close rather than identical.")]
        public int Seed { get; set; } = 1;
    }
}