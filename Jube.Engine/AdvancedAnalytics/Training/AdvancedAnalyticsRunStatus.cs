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

using Jube.Data.Repository.Models;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Interfaces;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    internal static class AdvancedAnalyticsRunStatus
    {
        internal static async Task<bool> IsStoppingOrStoppedOrCancelledAsync(IAdvancedAnalyticsTrialRecorder recorder,
            AdvancedAnalyticsTrainingRunRepository runs, int runId, string cancelledMessage,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (!await recorder.IsStopRequestedAsync(token).ConfigureAwait(false))
            {
                return false;
            }

            await runs.UpdateStatusAsync(runId, AdvancedAnalyticsStatus.Cancelled, cancelledMessage,
                CancellationToken.None).ConfigureAwait(false);

            return true;
        }
    }
}