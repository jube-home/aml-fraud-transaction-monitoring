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
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class GaussianMixtureEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of Gaussian components the model was fitted with.")]
        public int Components { get; set; }

        [Description("Whether every component was fitted with an independent (diagonal) covariance that ignores " +
                     "correlation between variables, rather than a full covariance matrix.")]
        public bool DiagonalCovariance { get; set; }

        [Description("Whether the fit converged before the iteration cap.")]
        public bool Converged { get; set; }

        [Description("Number of iterations the fit took.")]
        public int Iterations { get; set; }

        [Description("Total log-likelihood of the training rows under the fitted mixture. Higher (less negative) " +
                     "means the mixture explains the training rows better; it always rises as components are added " +
                     "so it is not by itself a reason to add more.")]
        public double TrainLogLikelihood { get; set; }

        [Description("Total log-likelihood of the validation split under the fitted mixture, or null when it has no " +
                     "rows.")]
        public double? ValidationLogLikelihood { get; set; }

        [Description("Total log-likelihood of the held-out test split under the fitted mixture, or null when it has " +
                     "no rows.")]
        public double? TestLogLikelihood { get; set; }

        [Description("Average, over the training rows, of each row's highest posterior probability across every " +
                     "component: how confidently rows are typically assigned, from close to one divided by the " +
                     "number of components (no row confidently assigned to any one component) to 1 (every row " +
                     "unambiguously assigned).")]
        public double TrainMeanMaxPosterior { get; set; }

        [Description("Average, over the held-out test rows, of each row's highest posterior probability. Null when " +
                     "the test split is empty.")]
        public double? TestMeanMaxPosterior { get; set; }

        [Description("Per-component size, mixture weight, mean and variance, computed on the training split.")]
        public GaussianMixtureClusterSummary[] ClusterSummaries { get; set; } = [];
    }
}