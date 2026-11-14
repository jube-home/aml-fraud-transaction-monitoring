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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    [MessagePackObject]
    public class AdvancedAnalyticsPromotionEvidenceDto
    {
        [Description("Identifier of the version the evidence describes.")]
        [Key(0)]
        public int VersionId { get; set; }

        [Description("Sequence number of the version within its model.")]
        [Key(1)]
        public int VersionNumber { get; set; }

        [Description("Identifier of the trained model.")]
        [Key(2)]
        public int TrainedModelId { get; set; }

        [Description("Name of the trained model.")]
        [Key(3)]
        public string? ModelName { get; set; }

        [Description("True when this version is already the live one.")]
        [Key(4)]
        public bool IsActiveVersion { get; set; }

        [Description("True when the owning instance is allocated to a Jube Model, which promotion requires.")]
        [Key(5)]
        public bool InstanceAllocated { get; set; }

        [Description("The Jube Model the owning instance is allocated to, or null when not allocated.")]
        [Key(6)]
        public int? AllocatedEntityAnalysisModelId { get; set; }

        [Description("Identifier of the training run that produced the version, or null.")]
        [Key(7)]
        public int? ProducingRunId { get; set; }

        [Description("Seed that fixed the row split.")]
        [Key(8)]
        public int Seed { get; set; }

        [Description("True when a test split was held back. Without one there is no honest quality figure.")]
        [Key(9)]
        public bool HasHeldOutTestSet { get; set; }

        [Description("Rows and positives of the train, validation and test splits.")]
        [Key(10)]
        public LogisticSplitSizes SplitSizes { get; set; } = null!;

        [Description("Quality on the held-out test split: the figures to judge the version by. Null without one.")]
        [Key(11)]
        public LogisticSplitMetrics? TestMetrics { get; set; }

        [Description("Quality on the rows the model was fitted to, to be compared with the test figures.")]
        [Key(12)]
        public LogisticSplitMetrics? TrainMetrics { get; set; }

        [Description("Whether the probabilities were calibrated and how strongly.")]
        [Key(13)]
        public LogisticCalibrationInfo Calibration { get; set; } = null!;

        [Description("Whether the fit converged and how many events there were per fitted column.")]
        [Key(14)]
        public LogisticFitInfo Fit { get; set; } = null!;

        [Description("The event rates the probabilities are expressed at.")]
        [Key(15)]
        public LogisticPriors Priors { get; set; } = null!;

        [Description("Intercept and predictors with estimates and p-values. A model whose predictors all lack " +
                     "significance has little to recommend it.")]
        [Key(16)]
        public LogisticCoefficient[] Coefficients { get; set; } = [];

        [Description("Every warning the trainer raised. Treat any of them as a reason to look harder.")]
        [Key(17)]
        public LogisticWarning[] Warnings { get; set; } = [];
    }
}