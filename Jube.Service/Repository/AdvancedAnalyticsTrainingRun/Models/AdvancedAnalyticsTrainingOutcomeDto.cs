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
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsTrainingOutcomeDto
    {
        [Description("Numeric identifier of the training run that was recorded.")]
        public int RunId { get; set; }

        [Description("Globally unique identifier of the training run.")]
        public Guid RunGuid { get; set; }

        [Description("Final status of the run: Succeeded, or Failed when the trainer refused or could not fit.")]
        public AdvancedAnalyticsStatus Status { get; set; }

        [Description("Why the run failed, or null when it succeeded.")]
        public string? Error { get; set; }

        [Description("Identifier of the trained model the new version was filed under, or null when the run failed.")]
        public int? TrainedModelId { get; set; }

        [Description("Globally unique identifier of that trained model, or null when the run failed.")]
        public Guid? TrainedModelGuid { get; set; }

        [Description("Identifier of the new immutable version, or null when the run failed.")]
        public int? TrainedModelVersionId { get; set; }

        [Description("Sequence number of the new version within its model, or null when the run failed.")]
        public int? VersionNumber { get; set; }

        [Description("Always false. Training never makes a version live; that is a separate promotion decision.")]
        public bool Activated { get; set; }

        [Description("The full evidence of the run: held-out metrics, coefficient significance, calibration, " +
                     "warnings and the model snapshot. Null when the run failed.")]
        public LogisticRegressionResult? Result { get; set; }
    }
}