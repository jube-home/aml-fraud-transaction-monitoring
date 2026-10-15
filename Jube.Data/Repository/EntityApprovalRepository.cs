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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;
    using Poco;
    using Query.Models;

    public class EntityApprovalRepository
    {
        public const int EntityIdChunkSize = 500;

        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityApprovalRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityApprovalRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityApprovalRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<List<EntityApprovalRow>> GetRowsByKindAsync(EntityApprovalKind kind,
            CancellationToken token = default)
        {
            var kindId = (int)kind;

            return dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.EntityId != null && w.EntityVersion != null && w.StateId != null)
                .Select(s => new EntityApprovalRow(s.EntityId.Value, s.EntityVersion.Value,
                    (EntityApprovalState)s.StateId.Value, s.CreatedUser, s.Note, s.ReturnedNote))
                .ToListAsync(token);
        }

        public Task<List<EntityApprovalRow>> GetRowsByKindAndEntityIdsAsync(EntityApprovalKind kind,
            ICollection<int> entityIds, CancellationToken token = default)
        {
            var kindId = (int)kind;

            return GetChunkedByEntityIdsAsync(entityIds, chunk => dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.EntityId != null && w.EntityVersion != null && w.StateId != null
                            && chunk.Contains(w.EntityId.Value))
                .Select(s => new EntityApprovalRow(s.EntityId.Value, s.EntityVersion.Value,
                    (EntityApprovalState)s.StateId.Value, s.CreatedUser, s.Note, s.ReturnedNote))
                .ToListAsync(token));
        }

        public async Task<List<EntityApproval>> GetByKindAndEntityIdsAsync(EntityApprovalKind kind,
            ICollection<int> entityIds, CancellationToken token = default)
        {
            var kindId = (int)kind;

            var approvals = await GetChunkedByEntityIdsAsync(entityIds, chunk => dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.EntityId != null
                            && chunk.Contains(w.EntityId.Value))
                .ToListAsync(token)).ConfigureAwait(false);

            return approvals.OrderBy(o => o.Id).ToList();
        }

        private static async Task<List<T>> GetChunkedByEntityIdsAsync<T>(ICollection<int> entityIds,
            Func<List<int>, Task<List<T>>> query)
        {
            if (entityIds == null || entityIds.Count == 0)
            {
                return [];
            }

            var results = new List<T>();

            foreach (var chunk in entityIds.Distinct().Chunk(EntityIdChunkSize))
            {
                results.AddRange(await query([.. chunk]).ConfigureAwait(false));
            }

            return results;
        }

        public Task<List<EntityApproval>> GetHistoryAsync(EntityApprovalKind kind, int entityId,
            CancellationToken token = default)
        {
            var kindId = (int)kind;

            return dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && w.EntityId == entityId
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .ToListAsync(token);
        }

        public Task<EntityApproval> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityApproval.FirstOrDefaultAsync(
                w => w.Id == id && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue), token);
        }

        public Task<int> RetractAsync(EntityApprovalKind kind, int entityId, int version, string retractedBy,
            string note = null, CancellationToken token = default)
        {
            var kindId = (int)kind;
            var stateId = (int)EntityApprovalState.Rejected;

            var query = dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && w.EntityId == entityId
                            && w.EntityVersion == version
                            && w.StateId == stateId
                            && w.ReturnedDate == null
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue))
                .Set(s => s.ReturnedDate, DateTime.UtcNow)
                .Set(s => s.ReturnedUser, retractedBy)
                .Set(s => s.ReturnKindId, (int)EntityApprovalReturnKind.Retracted);

            if (note != null)
            {
                query = query.Set(s => s.ReturnedNote, note);
            }

            return query.UpdateAsync(token);
        }

        public Task<int> ResubmitAsync(EntityApprovalKind kind, int entityId, int version, string resubmittedBy,
            string note = null, CancellationToken token = default)
        {
            var kindId = (int)kind;
            var stateId = (int)EntityApprovalState.Rejected;

            var query = dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId
                            && w.EntityId == entityId
                            && w.EntityVersion == version
                            && w.StateId == stateId
                            && w.ReturnedDate == null
                            && (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue))
                .Set(s => s.ReturnedDate, DateTime.UtcNow)
                .Set(s => s.ReturnedUser, resubmittedBy)
                .Set(s => s.ReturnKindId, (int)EntityApprovalReturnKind.Resubmitted);

            if (note != null)
            {
                query = query.Set(s => s.ReturnedNote, note);
            }

            return query.UpdateAsync(token);
        }

        public Task<int> DeleteByTenantRegistryIdAsync(int tenantRegistryIdOfSweep, CancellationToken token = default)
        {
            return dbContext.EntityApproval
                .Where(w => w.TenantRegistryId == tenantRegistryIdOfSweep)
                .DeleteAsync(token);
        }

        public async Task<int> GetMaxIdAsync(int tenantRegistryIdOfWatermark, CancellationToken token = default)
        {
            var max = await dbContext.EntityApproval
                .Where(w => w.TenantRegistryId == tenantRegistryIdOfWatermark)
                .Select(s => (int?)s.Id)
                .MaxAsync(token).ConfigureAwait(false);

            return max ?? 0;
        }

        public async Task<EntityApproval> InsertAsync(EntityApproval model, CancellationToken token = default)
        {
            model.CreatedUser ??= userName;
            model.CreatedDate ??= DateTime.UtcNow;
            model.TenantRegistryId ??= tenantRegistryId;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public Task<int> InsertManyAsync(IEnumerable<EntityApproval> models, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(models);

            var prepared = models.Select(s =>
            {
                s.CreatedUser ??= userName;
                s.CreatedDate ??= DateTime.UtcNow;
                s.TenantRegistryId ??= tenantRegistryId;
                s.Guid = s.Guid == Guid.Empty ? Guid.NewGuid() : s.Guid;

                return s;
            }).ToList();

            return prepared.Count == 0 ? Task.FromResult(0) : BulkCopyAsync(prepared, token);
        }

        private async Task<int> BulkCopyAsync(List<EntityApproval> prepared, CancellationToken token)
        {
            var result = await dbContext.BulkCopyAsync(prepared, token).ConfigureAwait(false);

            return (int)result.RowsCopied;
        }
    }
}