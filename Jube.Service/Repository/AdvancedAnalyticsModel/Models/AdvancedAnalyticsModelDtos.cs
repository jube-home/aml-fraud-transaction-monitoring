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

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public class AdvancedAnalyticsPromotionEvidenceDto
    {
        [Description("Identifier of the version the evidence describes.")]
        public int VersionId { get; set; }

        [Description("Sequence number of the version within its model.")]
        public int VersionNumber { get; set; }

        [Description("Identifier of the trained model.")]
        public int TrainedModelId { get; set; }

        [Description("Name of the trained model.")]
        public string? ModelName { get; set; }

        [Description("True when this version is already the live one.")]
        public bool IsActiveVersion { get; set; }

        [Description("True when the owning instance is allocated to a Jube Model, which promotion requires.")]
        public bool InstanceAllocated { get; set; }

        [Description("The Jube Model the owning instance is allocated to, or null when not allocated.")]
        public int? AllocatedEntityAnalysisModelId { get; set; }

        [Description("Identifier of the training run that produced the version, or null.")]
        public int? ProducingRunId { get; set; }

        [Description("Seed that fixed the row split.")]
        public int Seed { get; set; }

        [Description("True when a test split was held back. Without one there is no honest quality figure.")]
        public bool HasHeldOutTestSet { get; set; }

        [Description("Rows and positives of the train, validation and test splits.")]
        public LogisticSplitSizes SplitSizes { get; set; } = null!;

        [Description("Quality on the held-out test split: the figures to judge the version by. Null without one.")]
        public LogisticSplitMetrics? TestMetrics { get; set; }

        [Description("Quality on the rows the model was fitted to, to be compared with the test figures.")]
        public LogisticSplitMetrics? TrainMetrics { get; set; }

        [Description("Whether the probabilities were calibrated and how strongly.")]
        public LogisticCalibrationInfo Calibration { get; set; } = null!;

        [Description("Whether the fit converged and how many events there were per fitted column.")]
        public LogisticFitInfo Fit { get; set; } = null!;

        [Description("The event rates the probabilities are expressed at.")]
        public LogisticPriors Priors { get; set; } = null!;

        [Description("Intercept and predictors with estimates and p-values. A model whose predictors all lack " +
                     "significance has little to recommend it.")]
        public LogisticCoefficient[] Coefficients { get; set; } = [];

        [Description("Every warning the trainer raised. Treat any of them as a reason to look harder.")]
        public LogisticWarning[] Warnings { get; set; } = [];
    }
}