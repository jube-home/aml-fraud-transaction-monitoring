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
    public sealed class AdvancedAnalyticsGaussianMixtureClustererTrainingDto
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

        [Description("Number of Gaussian components to fit, from 1 up to the number of rows.")]
        public int Components { get; set; } = 3;

        [Description("Whether every component's covariance is fitted as diagonal, treating every variable as " +
                     "independent within the component, rather than as a full matrix that also captures " +
                     "correlation between variables. A diagonal fit needs fewer rows per component and is " +
                     "cheaper, at the cost of being unable to represent a component that is a diagonal or " +
                     "elongated cloud running across two or more variables at once. Default false (full " +
                     "covariance).")]
        public bool DiagonalCovariance { get; set; }

        [Description("Small constant added to the diagonal of every fitted covariance matrix so it stays " +
                     "invertible even with few rows or near-collinear variables. Raise it if fitting fails with " +
                     "too little data; leave it at the default otherwise. Default 0.000001.")]
        public double Regularization { get; set; } = 1e-6;

        [Description("Maximum number of expectation-maximisation iterations. Default 100.")]
        public int MaximumIterations { get; set; } = 100;

        [Description("Convergence tolerance on the change in log-likelihood between one iteration and the next. " +
                     "Default 0.001.")]
        public double Tolerance { get; set; } = 1e-3;

        [Description("Whether variables are z-scored with training statistics before fitting. Default true.")]
        public bool Standardise { get; set; } = true;

        [Description("Share of the rows held back for a validation diagnostic split, from 0 up to but not " +
                     "including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out fit quality, from 0 up to but not " +
                     "including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split and the fit's random starting point reproducible.")]
        public int Seed { get; set; } = 1;
    }
}