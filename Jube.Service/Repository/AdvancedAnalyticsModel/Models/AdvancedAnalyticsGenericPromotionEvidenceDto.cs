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
using Jube.Engine.AdvancedAnalytics.Evidence;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public class AdvancedAnalyticsGenericPromotionEvidenceDto
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

        [Description("What sort of model this is, for example a clusterer.")]
        public string Kind { get; set; } = string.Empty;

        [Description("Seed that fixed the row split.")]
        public int Seed { get; set; }

        [Description("Rows in the train, validation and test splits.")]
        public AdvancedAnalyticsSplitSizes? SplitSizes { get; set; }

        [Description("Every warning the trainer raised. Treat any of them as a reason to look harder.")]
        public AdvancedAnalyticsWarning[] Warnings { get; set; } = [];
    }
}