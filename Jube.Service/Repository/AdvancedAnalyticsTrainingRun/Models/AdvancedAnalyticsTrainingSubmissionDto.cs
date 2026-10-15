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
    public sealed class AdvancedAnalyticsTrainingSubmissionDto
    {
        [Description("Numeric identifier of the queued training run. Poll it with the run status read.")]
        public int RunId { get; set; }

        [Description("Globally unique identifier of the queued training run.")]
        public Guid RunGuid { get; set; }

        [Description("Status of the run right after submission: Pending until a background worker picks it up.")]
        public AdvancedAnalyticsStatus Status { get; set; }

        [Description("How to follow the run: read its status until it is Succeeded, Failed or Cancelled, then read " +
                     "its result. Nothing has been trained yet when this returns.")]
        public string HowToPoll { get; set; } = string.Empty;
    }
}