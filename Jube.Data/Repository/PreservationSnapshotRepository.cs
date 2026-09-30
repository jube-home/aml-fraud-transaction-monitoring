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

    public class PreservationSnapshotRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public PreservationSnapshotRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<PreservationSnapshot> InsertAsync(PreservationSnapshot model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.TenantRegistryId = tenantRegistryId;
            model.Deleted = 0;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<PreservationSnapshot> UpdateAsync(PreservationSnapshot model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;

            await dbContext.UpdateAsync(model, token: token);

            return model;
        }

        public Task<List<PreservationSnapshot>> GetAsync(int limit = 250, CancellationToken token = default)
        {
            return dbContext.PreservationSnapshot
                .Where(w => w.TenantRegistryId == tenantRegistryId && (w.Deleted == 0 || w.Deleted == null))
                .OrderByDescending(o => o.Id)
                .Take(limit)
                .Select(s => new PreservationSnapshot
                {
                    Id = s.Id,
                    Guid = s.Guid,
                    TenantRegistryId = s.TenantRegistryId,
                    SnapshotSourceId = s.SnapshotSourceId,
                    Name = s.Name,
                    ExportVersion = s.ExportVersion,
                    ExportGuid = s.ExportGuid,
                    EntityAnalysisModelCount = s.EntityAnalysisModelCount,
                    Bytes = s.Bytes,
                    InError = s.InError,
                    CreatedUser = s.CreatedUser,
                    CreatedDate = s.CreatedDate,
                    CompletedDate = s.CompletedDate
                })
                .ToListAsync(token);
        }

        public Task<PreservationSnapshot> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.PreservationSnapshot.FirstOrDefaultAsync(
                w => w.Id == id && w.TenantRegistryId == tenantRegistryId
                                && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await dbContext.PreservationSnapshot.FirstOrDefaultAsync(
                w => w.Id == id && w.TenantRegistryId == tenantRegistryId
                                && (w.Deleted == 0 || w.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            await dbContext.PreservationSnapshot
                .Where(w => w.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }
    }
}