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

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public class AdvancedAnalyticsPromotionResultDto
    {
        [Description("Identifier of the trained model whose live version changed.")]
        public int TrainedModelId { get; set; }

        [Description("Identifier of the version that is now live.")]
        public int ActiveVersionId { get; set; }

        [Description("Sequence number of the version that is now live.")]
        public int ActiveVersionNumber { get; set; }

        [Description("Identifier of the version that was live before, or null when there was none.")]
        public int? PreviousActiveVersionId { get; set; }

        [Description("Identifier of the instance that owns the model.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("The Jube Model the instance is allocated to, which the live version now serves.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("False when the version was already live and nothing was changed.")]
        public bool Changed { get; set; }

        [Description("The evidence that was on the version and was recorded in the audit trail: an " +
                     "AdvancedAnalyticsPromotionEvidenceDto for a classifier trained as LogisticRegression, or an " +
                     "AdvancedAnalyticsGenericPromotionEvidenceDto for every other model kind.")]
        public object Evidence { get; set; } = null!;
    }
}