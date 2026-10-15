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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class KernelRidgeRegressorEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Which of the five supported kernel functions was fitted: Gaussian, Polynomial, Sigmoid, " +
                     "Laplacian or Linear.")]
        public string Kernel { get; set; } = string.Empty;

        [Description("The complexity parameter used to derive the ridge penalty: the penalty added to the kernel " +
                     "Gram matrix's diagonal is one over this value. Larger values fit the training rows more " +
                     "tightly, at greater risk of overfitting; smaller values shrink the fit towards the training " +
                     "mean.")]
        public double Complexity { get; set; }

        [Description("The gamma used by the fitted kernel (Gaussian, Sigmoid and Laplacian): how far the influence " +
                     "of a single training row reaches. Reported after resolving the automatic default of one over " +
                     "the number of predictors, so this is always the value actually used, never the request's 0 " +
                     "placeholder for 'automatic'.")]
        public double Gamma { get; set; }

        [Description("The polynomial degree used by the fitted kernel when the kernel is Polynomial; null for every " +
                     "other kernel, since degree has no meaning for them.")]
        public int? Degree { get; set; }

        [Description("The number of training rows kept in the kernel expansion. Unlike a support vector machine, " +
                     "kernel ridge regression has no sparsity mechanism: every standardised training row is kept as " +
                     "part of the expansion, so this always equals the number of training rows, and recalling this " +
                     "kind of model means weighting the kernel similarity of a new row against every one of them.")]
        public int TrainingRowsInExpansion { get; set; }

        [Description("The ridge penalty actually added to the diagonal of the kernel Gram matrix before it was " +
                     "solved, equal to one over Complexity.")]
        public double RidgePenalty { get; set; }

        [Description("Metrics computed on the training split.")]
        public KernelRidgeRegressorSplitMetrics? TrainMetrics { get; set; }

        [Description("Metrics computed on the validation split.")]
        public KernelRidgeRegressorSplitMetrics? ValidationMetrics { get; set; }

        [Description("Metrics computed on the held-out test split, or null when there is no test split.")]
        public KernelRidgeRegressorSplitMetrics? TestMetrics { get; set; }
    }
}