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
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Poco;

    public class TransactionInboundQueueRepository(DbContext dbContext)
    {
        private const string ClaimSql = """
            UPDATE "TransactionInboundQueue" q
            SET "StatusId" = 2, "Attempts" = q."Attempts" + 1, "LockedBy" = @node,
                "LockedUntil" = (now() at time zone 'utc') + make_interval(secs => @visibility)
            WHERE q."Id" IN (
                SELECT c."Id" FROM "TransactionInboundQueue" c
                WHERE c."AvailableDate" <= (now() at time zone 'utc')
                  AND (c."StatusId" = 1 OR (c."StatusId" = 2 AND c."LockedUntil" < (now() at time zone 'utc')))
                ORDER BY c."Id"
                LIMIT @batch
                FOR UPDATE SKIP LOCKED)
            RETURNING q."Id", q."EntityAnalysisModelGuid", q."Payload", q."Attempts"
            """;

        public async Task InsertAsync(TransactionInboundQueue row, CancellationToken token = default)
        {
            row.StatusId = (short)TransactionInboundQueueStatus.Pending;
            row.CreatedDate = DateTime.UtcNow;
            row.AvailableDate = row.CreatedDate;
            row.Id = await dbContext.InsertWithInt64IdentityAsync(row, token: token).ConfigureAwait(false);
        }

        public async Task<List<ClaimedInboundRow>> ClaimAsync(string node, int batch, int visibilitySeconds,
            int maximumAttempts = 0, CancellationToken token = default)
        {
            if (maximumAttempts > 0)
            {
                var dead = (short)TransactionInboundQueueStatus.Dead;
                var processing = (short)TransactionInboundQueueStatus.Processing;
                var now = DateTime.UtcNow;

                await dbContext.TransactionInboundQueue
                    .Where(w => w.StatusId == processing && w.LockedUntil < now && w.Attempts >= maximumAttempts)
                    .Set(s => s.StatusId, dead).Set(s => s.LockedUntil, (DateTime?)null)
                    .Set(s => s.LockedBy, (string)null)
                    .Set(s => s.LastError, "Gave up after the node stopped while processing it.")
                    .UpdateAsync(token).ConfigureAwait(false);
            }

            return await dbContext.QueryToListAsync<ClaimedInboundRow>(ClaimSql, token, new DataParameter("node", node),
                new DataParameter("visibility", visibilitySeconds), new DataParameter("batch", batch)).ConfigureAwait(false);
        }

        public Task<int> CompleteAsync(long id, CancellationToken token = default)
        {
            return dbContext.TransactionInboundQueue.Where(w => w.Id == id).DeleteAsync(token);
        }

        public Task<int> RetryAsync(long id, int delaySeconds, string error, CancellationToken token = default)
        {
            var pending = (short)TransactionInboundQueueStatus.Pending;
            var available = DateTime.UtcNow.AddSeconds(delaySeconds);

            return dbContext.TransactionInboundQueue.Where(w => w.Id == id)
                .Set(s => s.StatusId, pending).Set(s => s.AvailableDate, available)
                .Set(s => s.LockedUntil, (DateTime?)null).Set(s => s.LockedBy, (string)null)
                .Set(s => s.LastError, Clip(error)).UpdateAsync(token);
        }

        public Task<int> KillAsync(long id, string error, CancellationToken token = default)
        {
            var dead = (short)TransactionInboundQueueStatus.Dead;

            return dbContext.TransactionInboundQueue.Where(w => w.Id == id)
                .Set(s => s.StatusId, dead).Set(s => s.LockedUntil, (DateTime?)null)
                .Set(s => s.LockedBy, (string)null).Set(s => s.LastError, Clip(error)).UpdateAsync(token);
        }

        public async Task<InboundQueueDepth> DepthAsync(CancellationToken token = default)
        {
            var rows = await dbContext.TransactionInboundQueue.GroupBy(g => g.StatusId)
                .Select(s => new { Status = s.Key, Count = s.Count() }).ToListAsync(token).ConfigureAwait(false);

            int Count(TransactionInboundQueueStatus status) =>
                rows.FirstOrDefault(r => r.Status == (short)status)?.Count ?? 0;

            return new InboundQueueDepth(Count(TransactionInboundQueueStatus.Pending),
                Count(TransactionInboundQueueStatus.Processing), Count(TransactionInboundQueueStatus.Dead));
        }

        public async Task<InboundQueueDepth> DepthForTenantAsync(int tenantRegistryId,
            CancellationToken token = default)
        {
            var rows = await (from q in dbContext.TransactionInboundQueue
                    join m in dbContext.EntityAnalysisModel on q.EntityAnalysisModelGuid equals m.Guid
                    where m.TenantRegistryId == tenantRegistryId && (m.Deleted == 0 || m.Deleted == null)
                    group q by q.StatusId
                    into g
                    select new { Status = g.Key, Count = g.Count() }).ToListAsync(token).ConfigureAwait(false);

            int Count(TransactionInboundQueueStatus status) =>
                rows.FirstOrDefault(r => r.Status == (short)status)?.Count ?? 0;

            return new InboundQueueDepth(Count(TransactionInboundQueueStatus.Pending),
                Count(TransactionInboundQueueStatus.Processing), Count(TransactionInboundQueueStatus.Dead));
        }

        public Task<List<TransactionInboundQueue>> ListDeadForTenantAsync(int tenantRegistryId, int take,
            CancellationToken token = default)
        {
            var dead = (short)TransactionInboundQueueStatus.Dead;

            return (from q in dbContext.TransactionInboundQueue
                    join m in dbContext.EntityAnalysisModel on q.EntityAnalysisModelGuid equals m.Guid
                    where q.StatusId == dead && m.TenantRegistryId == tenantRegistryId
                                             && (m.Deleted == 0 || m.Deleted == null)
                    orderby q.Id
                    select new TransactionInboundQueue
                    {
                        Id = q.Id, EntityAnalysisModelGuid = q.EntityAnalysisModelGuid, StatusId = q.StatusId,
                        Attempts = q.Attempts, LastError = q.LastError, EnqueuedBy = q.EnqueuedBy,
                        CreatedDate = q.CreatedDate, AvailableDate = q.AvailableDate
                    }).Take(take).ToListAsync(token);
        }

        public async Task<bool> DeadBelongsToTenantAsync(int tenantRegistryId, long id,
            CancellationToken token = default)
        {
            var dead = (short)TransactionInboundQueueStatus.Dead;

            return await (from q in dbContext.TransactionInboundQueue
                    join m in dbContext.EntityAnalysisModel on q.EntityAnalysisModelGuid equals m.Guid
                    where q.Id == id && q.StatusId == dead && m.TenantRegistryId == tenantRegistryId
                          && (m.Deleted == 0 || m.Deleted == null)
                    select q.Id).AnyAsync(token).ConfigureAwait(false);
        }

        public async Task<int> PendingBoundedAsync(int limit, CancellationToken token = default)
        {
            var pending = (short)TransactionInboundQueueStatus.Pending;

            return await dbContext.TransactionInboundQueue.Where(w => w.StatusId == pending).Take(limit)
                .CountAsync(token).ConfigureAwait(false);
        }

        public Task<List<TransactionInboundQueue>> ListDeadAsync(int take, CancellationToken token = default)
        {
            var dead = (short)TransactionInboundQueueStatus.Dead;

            return dbContext.TransactionInboundQueue.Where(w => w.StatusId == dead).OrderBy(o => o.Id).Take(take)
                .Select(s => new TransactionInboundQueue
                {
                    Id = s.Id, EntityAnalysisModelGuid = s.EntityAnalysisModelGuid, StatusId = s.StatusId,
                    Attempts = s.Attempts, LastError = s.LastError, EnqueuedBy = s.EnqueuedBy,
                    CreatedDate = s.CreatedDate, AvailableDate = s.AvailableDate
                }).ToListAsync(token);
        }

        public Task<int> RequeueDeadAsync(long id, CancellationToken token = default)
        {
            var dead = (short)TransactionInboundQueueStatus.Dead;
            var pending = (short)TransactionInboundQueueStatus.Pending;
            var now = DateTime.UtcNow;

            return dbContext.TransactionInboundQueue.Where(w => w.Id == id && w.StatusId == dead)
                .Set(s => s.StatusId, pending).Set(s => s.Attempts, 0).Set(s => s.AvailableDate, now)
                .Set(s => s.LastError, (string)null).UpdateAsync(token);
        }

        public Task<int> DeleteDeadAsync(long id, CancellationToken token = default)
        {
            var dead = (short)TransactionInboundQueueStatus.Dead;

            return dbContext.TransactionInboundQueue.Where(w => w.Id == id && w.StatusId == dead).DeleteAsync(token);
        }

        private static string Clip(string error)
        {
            return string.IsNullOrEmpty(error) ? null : error.Length > 500 ? error[..500] : error;
        }
    }

    public class ClaimedInboundRow
    {
        public long Id { get; set; }
        public Guid EntityAnalysisModelGuid { get; set; }
        public byte[] Payload { get; set; }
        public int Attempts { get; set; }
    }

    public sealed record InboundQueueDepth(int Pending, int Processing, int Dead);
}
