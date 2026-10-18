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
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class AdvancedAnalyticsTrainingRunRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        private const string RecoverStaleSql = """
                                               UPDATE "AdvancedAnalyticsTrainingRun"
                                               SET "StatusId" = CASE WHEN "StatusId" = 5 THEN 6 ELSE 4 END,
                                                   "Error" = @Error,
                                                   "CompletedDate" = @Now,
                                                   "UpdatedDate" = @Now
                                               WHERE "StatusId" IN (2, 5)
                                                 AND "Deleted" = 0
                                                 AND "JobMode" = 2
                                                 AND COALESCE("HeartbeatDate", "StartedDate", "CreatedDate") < @Cutoff
                                               RETURNING "Id"
                                               """;

        private const string HeartbeatSql = """
                                            UPDATE "AdvancedAnalyticsTrainingRun"
                                            SET "HeartbeatDate" = @Now
                                            WHERE "Id" = @Id
                                              AND "StatusId" IN (2, 5)
                                            RETURNING "StatusId"
                                            """;

        public static async Task<IReadOnlyList<int>> RecoverStaleAsync(DbContext context, DateTime cutoff,
            DateTime now, string error, CancellationToken token = default)
        {
            return
            [
                .. await new CommandInfo(context, RecoverStaleSql,
                        new DataParameter("Cutoff", cutoff, DataType.DateTime),
                        new DataParameter("Now", now, DataType.DateTime),
                        new DataParameter("Error", error, DataType.Text))
                    .QueryAsync<int>(token).ConfigureAwait(false)
            ];
        }

        public static async Task<AdvancedAnalyticsStatus?> HeartbeatAsync(DbContext context, int id, DateTime now,
            CancellationToken token = default)
        {
            var status = (await new CommandInfo(context, HeartbeatSql,
                        new DataParameter("Now", now, DataType.DateTime),
                        new DataParameter("Id", id, DataType.Int32))
                    .QueryAsync<int>(token).ConfigureAwait(false))
                .ToList();

            return status.Count == 0 ? null : (AdvancedAnalyticsStatus)status[0];
        }

        public Task<AdvancedAnalyticsTrainingRun> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainingRun.FirstOrDefaultAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0, token);
        }

        public Task<AdvancedAnalyticsTrainingRun> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainingRun.FirstOrDefaultAsync(w =>
                w.Guid == guid && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0, token);
        }

        public async Task<IEnumerable<AdvancedAnalyticsTrainingRun>> ListAsync(int? advancedAnalyticsInstanceId = null,
            AdvancedAnalyticsStatus? status = null, int take = 100, CancellationToken token = default)
        {
            return await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && (advancedAnalyticsInstanceId == null
                                || w.AdvancedAnalyticsInstanceId == advancedAnalyticsInstanceId)
                            && (status == null || w.StatusId == status))
                .OrderByDescending(o => o.Id)
                .Take(Math.Clamp(take, 1, 1000))
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<AdvancedAnalyticsTrainingRun> InsertAsync(AdvancedAnalyticsTrainingRun model,
            CancellationToken token = default)
        {
            var instanceVisible = await dbContext.AdvancedAnalyticsInstance.AnyAsync(w =>
                    w.Id == model.AdvancedAnalyticsInstanceId
                    && w.TenantRegistryId == tenantRegistryId
                    && w.Deleted == 0, token)
                .ConfigureAwait(false);

            if (!instanceVisible)
            {
                throw new KeyNotFoundException();
            }

            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.TenantRegistryId = tenantRegistryId;
            model.UserName = userName ?? model.UserName;
            model.CreatedUser = userName ?? model.CreatedUser;
            model.StatusId = AdvancedAnalyticsStatus.Pending;
            model.Progress = 0;
            model.CreatedDate = DateTime.UtcNow;
            model.StartedDate = null;
            model.CompletedDate = null;
            model.UpdatedDate = null;
            model.Error = null;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task<bool> UpdateStatusAsync(int id, AdvancedAnalyticsStatus status, string error = null,
            CancellationToken token = default)
        {
            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            if (IsTerminal(existing.StatusId))
            {
                return false;
            }

            var now = DateTime.UtcNow;

            var startedDate = status == AdvancedAnalyticsStatus.Running && existing.StartedDate == null
                ? now
                : existing.StartedDate;

            var completedDate = IsTerminal(status) ? now : (DateTime?)null;

            var terminalStatuses = new List<AdvancedAnalyticsStatus>
            {
                AdvancedAnalyticsStatus.Succeeded, AdvancedAnalyticsStatus.Failed, AdvancedAnalyticsStatus.Cancelled
            };

            var records = await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.Id == id
                            && w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && !terminalStatuses.Contains(w.StatusId))
                .Set(s => s.StatusId, status)
                .Set(s => s.StartedDate, startedDate)
                .Set(s => s.CompletedDate, completedDate)
                .Set(s => s.Error, error)
                .Set(s => s.UpdatedDate, now)
                .UpdateAsync(token).ConfigureAwait(false);

            return records > 0;
        }

        public async Task UpdateProgressAsync(int id, double progress, CancellationToken token = default)
        {
            var clamped = double.IsNaN(progress) ? 0 : Math.Clamp(progress, 0, 1);

            var records = await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Progress, clamped)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task<bool> IsStopRequestedAsync(int id, CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainingRun.AnyAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId
                           && (w.StatusId == AdvancedAnalyticsStatus.Cancelling
                               || w.StatusId == AdvancedAnalyticsStatus.Cancelled), token);
        }

        public Task<bool> IsFinishedAsync(int id, CancellationToken token = default)
        {
            var terminalStatuses = new List<AdvancedAnalyticsStatus>
            {
                AdvancedAnalyticsStatus.Succeeded, AdvancedAnalyticsStatus.Failed, AdvancedAnalyticsStatus.Cancelled
            };

            return dbContext.AdvancedAnalyticsTrainingRun.AnyAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId
                           && terminalStatuses.Contains(w.StatusId), token);
        }

        public async Task<AdvancedAnalyticsCancelOutcome> StopAsync(int id, string error,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            var pending = await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.Id == id
                            && w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && w.StatusId == AdvancedAnalyticsStatus.Pending)
                .Set(s => s.StatusId, AdvancedAnalyticsStatus.Cancelled)
                .Set(s => s.CompletedDate, now)
                .Set(s => s.Error, error)
                .Set(s => s.UpdatedDate, now)
                .UpdateAsync(token).ConfigureAwait(false);

            if (pending > 0)
            {
                return AdvancedAnalyticsCancelOutcome.CancelledPending;
            }

            var running = await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.Id == id
                            && w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && w.StatusId == AdvancedAnalyticsStatus.Running)
                .Set(s => s.StatusId, AdvancedAnalyticsStatus.Cancelling)
                .Set(s => s.UpdatedDate, now)
                .UpdateAsync(token).ConfigureAwait(false);

            if (running > 0)
            {
                return AdvancedAnalyticsCancelOutcome.CancellationRequested;
            }

            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                return AdvancedAnalyticsCancelOutcome.NotFound;
            }

            return existing.StatusId == AdvancedAnalyticsStatus.Cancelling
                ? AdvancedAnalyticsCancelOutcome.AlreadyCancelling
                : AdvancedAnalyticsCancelOutcome.AlreadyFinished;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public static bool IsTerminal(AdvancedAnalyticsStatus status)
        {
            return status is AdvancedAnalyticsStatus.Succeeded
                or AdvancedAnalyticsStatus.Failed
                or AdvancedAnalyticsStatus.Cancelled;
        }
    }
}