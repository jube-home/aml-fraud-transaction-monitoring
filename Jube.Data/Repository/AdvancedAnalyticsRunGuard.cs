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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Repository
{
    internal static class AdvancedAnalyticsRunGuard
    {
        public static async Task<int> ResolveInstanceIdAsync(DbContext dbContext, int tenantRegistryId, int runId,
            CancellationToken token)
        {
            var instanceId = await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.Id == runId
                            && w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0)
                .Select(s => (int?)s.AskJoobyAnalyticsInstanceId)
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (instanceId == null)
            {
                throw new KeyNotFoundException();
            }

            return instanceId.Value;
        }

        public static async Task EnsureTrialInRunAsync(DbContext dbContext, int tenantRegistryId, int runId,
            int? trialId, CancellationToken token)
        {
            if (trialId == null)
            {
                return;
            }

            var exists = await dbContext.AdvancedAnalyticsTrainingRunTrial
                .AnyAsync(w => w.Id == trialId
                               && w.TenantRegistryId == tenantRegistryId
                               && w.AdvancedAnalyticsTrainingRunId == runId, token).ConfigureAwait(false);

            if (!exists)
            {
                throw new KeyNotFoundException();
            }
        }

        public static async Task InTransactionAsync(DbContext dbContext, Func<Task> action, CancellationToken token)
        {
            if (dbContext.Transaction != null)
            {
                await action().ConfigureAwait(false);

                return;
            }

            await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            try
            {
                await action().ConfigureAwait(false);
                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
    }
}