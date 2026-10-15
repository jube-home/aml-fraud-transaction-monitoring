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
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("AdvancedAnalyticsTrainingRuns")]
    public class AdvancedAnalyticsTrainingRunTools(AdvancedAnalyticsSharedTrainingRunService service)
    {
        [AdvancedAnalyticsTool("GetAdvancedAnalyticsTrainingRunStatus")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads where a queued training run has got to: Pending while it waits for a worker, Running while it " +
            "trains, then Succeeded, Failed or Cancelled, which are final. Works for a training run submitted by " +
            "any family's Submit tool, not just one kind of model, since a run's status, elapsed time and error are " +
            "the same shape whatever was being trained. Poll it every few seconds until Finished is true, then read " +
            "the result.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> GetAdvancedAnalyticsTrainingRunStatusAsync(
            [Description(
                "The run id returned when the training was submitted, whichever family's Submit tool returned it.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.GetRunAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("CancelAdvancedAnalyticsTrainingRun")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Cancels a training run submitted by any family's Submit tool. A run still waiting for a worker is " +
            "cancelled at once; a running one is asked to stop and ends as Cancelled at its next step boundary, and " +
            "no model is stored for it. A finished run is final and is returned unchanged.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> CancelAdvancedAnalyticsTrainingRunAsync(
            [Description(
                "The run id returned when the training was submitted, whichever family's Submit tool returned it.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.CancelRunAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetAdvancedAnalyticsTrainingRunResult")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads the outcome of a finished training run submitted by any family's Submit tool: the run's final " +
            "status, and, once it has succeeded, the identifiers of the trained model and the new immutable version " +
            "it was filed under, plus the evidence recorded for that version, in whichever shape is specific to the " +
            "kind of model that was actually trained (its variables, split sizes, warnings, and family-specific " +
            "details such as coefficients, cluster summaries or component weights). Training never makes the " +
            "resulting version live; promotion to an active version is a separate step reached through the model " +
            "tools, not this one. Call this once GetAdvancedAnalyticsTrainingRunStatus reports the run as Finished; " +
            "calling it earlier returns the status with everything else empty.")]
        public Task<AdvancedAnalyticsSharedTrainingRunResultDto> GetAdvancedAnalyticsTrainingRunResultAsync(
            [Description(
                "The run id returned when the training was submitted, whichever family's Submit tool returned it.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.GetRunResultAsync(runId, cancellationToken);
        }
    }
}