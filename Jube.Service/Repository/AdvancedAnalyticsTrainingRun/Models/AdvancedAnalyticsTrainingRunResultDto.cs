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
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsTrainingRunResultDto
    {
        [Description("The current status of the run.")]
        public AdvancedAnalyticsTrainingRunStatusDto Run { get; set; } = new();

        [Description("Identifier of the trained model the version was filed under, or null unless it succeeded.")]
        public int? TrainedModelId { get; set; }

        [Description("Identifier of the new immutable version, or null unless it succeeded.")]
        public int? TrainedModelVersionId { get; set; }

        [Description("Sequence number of the version within its model, or null unless it succeeded.")]
        public int? VersionNumber { get; set; }

        [Description("Always false. Training never makes a version live; promotion is a separate decision.")]
        public bool Activated { get; set; }

        [Description("The complete evidence needed to decide on promotion: held-out metrics, coefficients, " +
                     "calibration and warnings. Null unless the run succeeded.")]
        public AdvancedAnalyticsLogisticEvidence? Evidence { get; set; }
    }
}