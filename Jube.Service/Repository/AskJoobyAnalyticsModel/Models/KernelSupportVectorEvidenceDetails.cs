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
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class KernelSupportVectorEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description(
            "Which of the four supported kernel functions was fitted: Gaussian, Polynomial, Sigmoid or Laplacian.")]
        public string Kernel { get; set; } = string.Empty;

        [Description("The complexity parameter C used by the sequential minimal optimisation solver: the trade-off " +
                     "between a wider margin and fewer training misclassifications. Larger values fit the training " +
                     "rows more tightly, at greater risk of overfitting.")]
        public double Complexity { get; set; }

        [Description("The gamma used by the fitted kernel (Gaussian, Sigmoid and Laplacian): how far the influence " +
                     "of a single training row reaches. Reported after resolving the automatic default of one over " +
                     "the number of predictors, so this is always the value actually used, never the request's 0 " +
                     "placeholder for 'automatic'.")]
        public double Gamma { get; set; }

        [Description("The polynomial degree used by the fitted kernel when the kernel is Polynomial; null for every " +
                     "other kernel, since degree has no meaning for them.")]
        public int? Degree { get; set; }

        [Description("The number of training rows the sequential minimal optimisation solver kept as support " +
                     "vectors. Recalling a kernel support vector machine means weighting the kernel similarity of a " +
                     "new row against every one of these stored support vector rows, exactly as recalling a k " +
                     "nearest neighbours model means searching its stored training rows; there is no separate " +
                     "compact parameter set the way there is for a fitted line. A larger, noisier or more " +
                     "overlapping training split, or a larger complexity C, typically keeps a larger share of the " +
                     "training rows as support vectors and so makes a larger snapshot; this is an expected " +
                     "characteristic of this family, not a defect.")]
        public int SupportVectorCount { get; set; }

        [Description("Whether, and how, the raw decision function (the signed distance to the separating boundary " +
                     "in kernel feature space) was Platt-calibrated into a probability on the validation split. The " +
                     "raw decision function is not itself a probability and, left uncalibrated, should not be read " +
                     "as one.")]
        public KernelSupportVectorCalibrationInfo Calibration { get; set; } = null!;

        [Description("Metrics computed on the training split.")]
        public KernelSupportVectorSplitMetrics? TrainMetrics { get; set; }

        [Description("Metrics computed on the validation split.")]
        public KernelSupportVectorSplitMetrics? ValidationMetrics { get; set; }

        [Description("Metrics computed on the held-out test split, or null when there is no test split.")]
        public KernelSupportVectorSplitMetrics? TestMetrics { get; set; }
    }
}