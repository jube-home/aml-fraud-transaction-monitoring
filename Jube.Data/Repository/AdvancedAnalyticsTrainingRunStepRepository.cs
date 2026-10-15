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
    public class AdvancedAnalyticsTrainingRunStepRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        public string UserName { get; } = userName;

        public async Task<AdvancedAnalyticsTrainingRunStep> InsertAsync(AdvancedAnalyticsTrainingRunStep model,
            CancellationToken token = default)
        {
            model.AdvancedAnalyticsInstanceId = await AdvancedAnalyticsRunGuard
                .ResolveInstanceIdAsync(dbContext, tenantRegistryId, model.AdvancedAnalyticsTrainingRunId, token)
                .ConfigureAwait(false);

            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task UpdateAsync(int id, AdvancedAnalyticsStatus status, string error = null,
            CancellationToken token = default)
        {
            var existing = await dbContext.AdvancedAnalyticsTrainingRunStep
                .FirstOrDefaultAsync(w => w.Id == id && w.TenantRegistryId == tenantRegistryId, token)
                .ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var now = DateTime.UtcNow;

            var startedDate = status == AdvancedAnalyticsStatus.Running && existing.StartedDate == null
                ? now
                : existing.StartedDate;

            var terminal = AdvancedAnalyticsTrainingRunRepository.IsTerminal(status);
            var completedDate = terminal ? now : (DateTime?)null;

            double? duration = terminal && startedDate != null
                ? (now - startedDate.Value).TotalMilliseconds
                : null;

            await dbContext.AdvancedAnalyticsTrainingRunStep
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId)
                .Set(s => s.StatusId, status)
                .Set(s => s.StartedDate, startedDate)
                .Set(s => s.CompletedDate, completedDate)
                .Set(s => s.DurationMilliseconds, duration)
                .Set(s => s.Error, error)
                .UpdateAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AdvancedAnalyticsTrainingRunStep>> ListByRunIdAsync(int runId,
            CancellationToken token = default)
        {
            return await dbContext.AdvancedAnalyticsTrainingRunStep
                .Where(w => w.AdvancedAnalyticsTrainingRunId == runId && w.TenantRegistryId == tenantRegistryId)
                .OrderBy(o => o.StepOrder).ThenBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }
    }
}