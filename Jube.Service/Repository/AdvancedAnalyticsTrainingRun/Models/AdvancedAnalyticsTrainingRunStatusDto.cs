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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsTrainingRunStatusDto
    {
        [Description("Numeric identifier of the training run.")]
        public int RunId { get; set; }

        [Description("Globally unique identifier of the training run.")]
        public Guid RunGuid { get; set; }

        [Description("Numeric identifier of the Advanced Analytics instance the run belongs to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Pending, Running, Succeeded, Failed, Cancelling or Cancelled. The last three of Succeeded, " +
                     "Failed and Cancelled are final.")]
        public AdvancedAnalyticsStatus Status { get; set; }

        [Description("Whether the run is immediate (Online) or was queued for a background worker (Batch).")]
        public AdvancedAnalyticsJobMode JobMode { get; set; }

        [Description("Seconds since the run started, or since it was submitted while it is still queued. Fixed " +
                     "once the run has finished.")]
        public double ElapsedSeconds { get; set; }

        [Description("Why the run failed or was cancelled, or null.")]
        public string? Error { get; set; }

        [Description("Whether the run is in a final status.")]
        public bool Finished { get; set; }

        [Description("When the run was submitted.")]
        public DateTime CreatedDate { get; set; }

        [Description("When a worker started the run, or null while it is queued.")]
        public DateTime? StartedDate { get; set; }

        [Description("When the run reached a final status, or null.")]
        public DateTime? CompletedDate { get; set; }

        [Description("The trial parameters as JSON: the options, variable names, model name and the kind of input. " +
                     "Never the rows themselves. Null in list reads.")]
        public string? Parameters { get; set; }

        [Description("Identifier of the version the run produced, or null until it has succeeded.")]
        public int? TrainedModelVersionId { get; set; }
    }
}