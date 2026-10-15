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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Interfaces;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class DbAdvancedAnalyticsTrialRecorder(
        DbContext dbContext,
        int tenantRegistryId,
        string userName,
        int runId) : IAdvancedAnalyticsTrialRecorder
    {
        private readonly AdvancedAnalyticsTrainingRunRepository runs = new(dbContext, tenantRegistryId, userName);

        private readonly AdvancedAnalyticsTrainingRunTrialRepository
            trials = new(dbContext, tenantRegistryId, userName);

        public Task<bool> IsStopRequestedAsync(CancellationToken token = default)
        {
            return runs.IsStopRequestedAsync(runId, token);
        }

        public async Task<AdvancedAnalyticsTrainingRunTrial> InsertTrialAsync(string hyperparametersJson,
            CancellationToken token = default)
        {
            var existing = await trials.ListByRunIdAsync(runId, token).ConfigureAwait(false);

            return await trials.InsertAsync(new AdvancedAnalyticsTrainingRunTrial
            {
                AdvancedAnalyticsTrainingRunId = runId,
                TrialNumber = existing.Count() + 1,
                StatusId = AdvancedAnalyticsStatus.Running,
                Hyperparameters = hyperparametersJson
            }, token).ConfigureAwait(false);
        }

        public Task CompleteTrialAsync(int trialId, AdvancedAnalyticsStatus status, double? score, string error,
            CancellationToken token = default)
        {
            return trials.UpdateAsync(trialId, status, score, error, token);
        }

        public Task MarkSelectedAsync(int trialId, CancellationToken token = default)
        {
            return trials.MarkSelectedAsync(runId, trialId, token);
        }
    }
}