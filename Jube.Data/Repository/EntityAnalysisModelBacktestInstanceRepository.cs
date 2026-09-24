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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class EntityAnalysisModelBacktestInstanceRepository(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<EntityAnalysisModelBacktestInstance> InsertAsync(EntityAnalysisModelBacktestInstance instance,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(instance);

            instance.TenantRegistryId = tenantRegistryId;
            instance.Guid = instance.Guid == Guid.Empty ? Guid.NewGuid() : instance.Guid;
            instance.CreatedDate = DateTime.UtcNow;
            instance.Id = await dbContext.InsertWithInt32IdentityAsync(instance, token: token).ConfigureAwait(false);
            return instance;
        }

        public Task<EntityAnalysisModelBacktestInstance> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelBacktestInstance.FirstOrDefaultAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0, token);
        }

        public Task<List<EntityAnalysisModelBacktestInstance>> GetByRuleAsync(int entityAnalysisModelId,
            string ruleType,
            int? ruleId, int take, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelBacktestInstance
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.EntityAnalysisModelId == entityAnalysisModelId
                                                                   && w.Deleted == 0
                                                                   && (ruleType == null || w.RuleType == ruleType)
                                                                   && (ruleId == null || w.RuleId == ruleId))
                .OrderByDescending(o => o.CreatedDate)
                .ThenByDescending(o => o.Id)
                .Take(take)
                .ToListAsync(token);
        }

        public async Task<BacktestInstanceStatus?> StopAsync(int id, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;
            var cancelled = await dbContext.EntityAnalysisModelBacktestInstance
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId &&
                            w.Status == (short)BacktestInstanceStatus.Pending)
                .Set(s => s.Status, (short)BacktestInstanceStatus.Cancelled)
                .Set(s => s.CompletedDate, now)
                .UpdateAsync(token).ConfigureAwait(false);
            if (cancelled == 0)
            {
                await dbContext.EntityAnalysisModelBacktestInstance
                    .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId &&
                                w.Status == (short)BacktestInstanceStatus.Running)
                    .Set(s => s.Status, (short)BacktestInstanceStatus.Cancelling)
                    .UpdateAsync(token).ConfigureAwait(false);
            }

            return (BacktestInstanceStatus?)(await GetByIdAsync(id, token).ConfigureAwait(false))?.Status;
        }

        public static Task<EntityAnalysisModelBacktestInstance> GetClaimedAsync(DbContext dbContext, int id,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelBacktestInstance.FirstOrDefaultAsync(w => w.Id == id, token);
        }

        public static async Task<BacktestInstanceStatus?> HeartbeatAsync(DbContext dbContext, int id, long scanned,
            long evaluated, double progress, CancellationToken token = default)
        {
            await dbContext.EntityAnalysisModelBacktestInstance
                .Where(w => w.Id == id && (w.Status == (short)BacktestInstanceStatus.Running ||
                                           w.Status == (short)BacktestInstanceStatus.Cancelling))
                .Set(s => s.HeartbeatDate, DateTime.UtcNow)
                .Set(s => s.Scanned, scanned)
                .Set(s => s.Evaluated, evaluated)
                .Set(s => s.Progress, progress)
                .UpdateAsync(token).ConfigureAwait(false);

            var status = await dbContext.EntityAnalysisModelBacktestInstance.Where(w => w.Id == id)
                .Select(s => (short?)s.Status).FirstOrDefaultAsync(token).ConfigureAwait(false);
            return (BacktestInstanceStatus?)status;
        }

        public static async Task<bool> CompleteAsync(DbContext dbContext, int id, BacktestInstanceStatus status,
            long scanned, long evaluated, string result, string error, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;
            var updated = await dbContext.EntityAnalysisModelBacktestInstance
                .Where(w => w.Id == id && (w.Status == (short)BacktestInstanceStatus.Running ||
                                           w.Status == (short)BacktestInstanceStatus.Cancelling))
                .Set(s => s.Status, (short)status)
                .Set(s => s.Scanned, scanned)
                .Set(s => s.Evaluated, evaluated)
                .Set(s => s.Progress, status == BacktestInstanceStatus.Succeeded ? 1d : 0d)
                .Set(s => s.Result, result)
                .Set(s => s.Error, error)
                .Set(s => s.CompletedDate, now)
                .Set(s => s.HeartbeatDate, now)
                .UpdateAsync(token).ConfigureAwait(false);
            return updated > 0;
        }

        public static async Task<int> RecoverStaleAsync(DbContext dbContext, TimeSpan stale,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;
            var threshold = now - stale;
            var failed = await dbContext.EntityAnalysisModelBacktestInstance
                .Where(w => w.Status == (short)BacktestInstanceStatus.Running &&
                            (w.HeartbeatDate ?? w.StartedDate) < threshold)
                .Set(s => s.Status, (short)BacktestInstanceStatus.Failed)
                .Set(s => s.Error, "The engine stopped while the backtest was running.")
                .Set(s => s.CompletedDate, now)
                .UpdateAsync(token).ConfigureAwait(false);
            var cancelled = await dbContext.EntityAnalysisModelBacktestInstance
                .Where(w => w.Status == (short)BacktestInstanceStatus.Cancelling &&
                            (w.HeartbeatDate ?? w.StartedDate) < threshold)
                .Set(s => s.Status, (short)BacktestInstanceStatus.Cancelled)
                .Set(s => s.CompletedDate, now)
                .UpdateAsync(token).ConfigureAwait(false);
            return failed + cancelled;
        }
    }
}