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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class AdvancedAnalyticsTrainingRunTrialRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        public string UserName { get; } = userName;

        public Task<AdvancedAnalyticsTrainingRunTrial> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainingRunTrial.FirstOrDefaultAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId, token);
        }

        public async Task<AdvancedAnalyticsTrainingRunTrial> InsertAsync(AdvancedAnalyticsTrainingRunTrial model,
            CancellationToken token = default)
        {
            model.AdvancedAnalyticsInstanceId = await AdvancedAnalyticsRunGuard
                .ResolveInstanceIdAsync(dbContext, tenantRegistryId, model.AdvancedAnalyticsTrainingRunId, token)
                .ConfigureAwait(false);

            model.TenantRegistryId = tenantRegistryId;
            model.Selected = 0;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task UpdateAsync(int id, AdvancedAnalyticsStatus status, double? score, string error = null,
            CancellationToken token = default)
        {
            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var now = DateTime.UtcNow;
            var terminal = AdvancedAnalyticsTrainingRunRepository.IsTerminal(status);
            var completedDate = terminal ? now : (DateTime?)null;
            double? duration = terminal ? (now - existing.CreatedDate).TotalMilliseconds : null;

            await dbContext.AdvancedAnalyticsTrainingRunTrial
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId)
                .Set(s => s.StatusId, status)
                .Set(s => s.Score, score)
                .Set(s => s.CompletedDate, completedDate)
                .Set(s => s.DurationMilliseconds, duration)
                .Set(s => s.Error, error)
                .UpdateAsync(token).ConfigureAwait(false);
        }

        public async Task MarkSelectedAsync(int runId, int trialId, CancellationToken token = default)
        {
            await AdvancedAnalyticsRunGuard.ResolveInstanceIdAsync(dbContext, tenantRegistryId, runId, token)
                .ConfigureAwait(false);

            await AdvancedAnalyticsRunGuard.EnsureTrialInRunAsync(dbContext, tenantRegistryId, runId, trialId, token)
                .ConfigureAwait(false);

            await AdvancedAnalyticsRunGuard.InTransactionAsync(dbContext, async () =>
            {
                await dbContext.AdvancedAnalyticsTrainingRunTrial
                    .Where(w => w.AdvancedAnalyticsTrainingRunId == runId && w.TenantRegistryId == tenantRegistryId)
                    .Set(s => s.Selected, (byte)0)
                    .UpdateAsync(token).ConfigureAwait(false);

                await dbContext.AdvancedAnalyticsTrainingRunTrial
                    .Where(w => w.Id == trialId && w.TenantRegistryId == tenantRegistryId)
                    .Set(s => s.Selected, (byte)1)
                    .UpdateAsync(token).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AdvancedAnalyticsTrainingRunTrial>> ListByRunIdAsync(int runId,
            CancellationToken token = default)
        {
            return await dbContext.AdvancedAnalyticsTrainingRunTrial
                .Where(w => w.AdvancedAnalyticsTrainingRunId == runId && w.TenantRegistryId == tenantRegistryId)
                .OrderBy(o => o.TrialNumber).ThenBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }
    }
}