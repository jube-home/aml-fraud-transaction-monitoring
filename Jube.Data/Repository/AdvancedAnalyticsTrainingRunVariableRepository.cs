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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class AdvancedAnalyticsTrainingRunVariableRepository(
        DbContext dbContext,
        int tenantRegistryId,
        string userName)
    {
        public string UserName { get; } = userName;

        public async Task<AdvancedAnalyticsTrainingRunVariable> InsertAsync(
            AdvancedAnalyticsTrainingRunVariable model, CancellationToken token = default)
        {
            await PrepareAsync(model, token).ConfigureAwait(false);
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task InsertRangeAsync(IReadOnlyCollection<AdvancedAnalyticsTrainingRunVariable> models,
            CancellationToken token = default)
        {
            foreach (var model in models)
            {
                await PrepareAsync(model, token).ConfigureAwait(false);
            }

            await AdvancedAnalyticsRunGuard.InTransactionAsync(dbContext, async () =>
            {
                foreach (var model in models)
                {
                    await dbContext.InsertAsync(model, token: token).ConfigureAwait(false);
                }
            }, token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AdvancedAnalyticsTrainingRunVariable>> ListByRunIdAsync(int runId,
            CancellationToken token = default)
        {
            return await dbContext.AdvancedAnalyticsTrainingRunVariable
                .Where(w => w.AdvancedAnalyticsTrainingRunId == runId && w.TenantRegistryId == tenantRegistryId)
                .OrderBy(o => o.VariableSequence).ThenBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }

        private async Task PrepareAsync(AdvancedAnalyticsTrainingRunVariable model, CancellationToken token)
        {
            model.AdvancedAnalyticsInstanceId = await AdvancedAnalyticsRunGuard
                .ResolveInstanceIdAsync(dbContext, tenantRegistryId, model.AdvancedAnalyticsTrainingRunId, token)
                .ConfigureAwait(false);

            model.TenantRegistryId = tenantRegistryId;
        }
    }
}