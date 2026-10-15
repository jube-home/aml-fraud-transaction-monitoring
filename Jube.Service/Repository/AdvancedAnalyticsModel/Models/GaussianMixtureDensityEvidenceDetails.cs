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
using Jube.Engine.AdvancedAnalytics.Density.Models;
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class GaussianMixtureDensityEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of Gaussian components the mixture was fitted with, as requested.")]
        public int Components { get; set; }

        [Description("Per-component weight, mean and covariance on the training (standardised, if requested) " +
                     "scale, in the same order the model was fitted. Weights sum to 1 across all components.")]
        public GaussianMixtureDensityComponentSummary[] ComponentSummaries { get; set; } = [];

        [Description("Mean per-row log-density on the rows the mixture was fitted to. Higher (less negative) " +
                     "means the fitted mixture places more probability mass where the training rows actually sit.")]
        public double TrainMeanLogLikelihood { get; set; }

        [Description("Mean per-row log-density on the validation split, or null when it has no rows.")]
        public double? ValidationMeanLogLikelihood { get; set; }

        [Description("Mean per-row log-density on rows never used to fit the mixture, the honest held-out " +
                     "diagnostic. A validation-to-test gap much larger than the train-to-validation gap suggests the " +
                     "mixture has over-fitted the training rows' particular noise. Null when there is no test split.")]
        public double? TestMeanLogLikelihood { get; set; }

        [Description("Bayesian Information Criterion computed from the total (not mean) training log-likelihood and " +
                     "the number of free parameters (each component's weight, mean and full covariance): " +
                     "-2 * totalTrainLogLikelihood + parameterCount * ln(trainRows). Lower is better; unlike the raw " +
                     "log-likelihood it penalises adding components purely because more components can only fit the " +
                     "training rows at least as well.")]
        public double Bic { get; set; }

        [Description("Akaike Information Criterion: -2 * totalTrainLogLikelihood + 2 * parameterCount. Lower is " +
                     "better; penalises added components less harshly than BIC, so the two can disagree on the best " +
                     "component count.")]
        public double Aic { get; set; }

        [Description("Number of free parameters counted for BIC and AIC: each component contributes its mean, its " +
                     "full (not diagonal) covariance and one weight, with the weights constrained to sum to 1.")]
        public int ParameterCount { get; set; }

        [Description("Whether the Expectation-Maximisation fit finished within MaxIterations rather than being cut " +
                     "off while still improving.")]
        public bool Converged { get; set; }

        [Description("Number of Expectation-Maximisation iterations the fit actually used.")]
        public int Iterations { get; set; }
    }
}