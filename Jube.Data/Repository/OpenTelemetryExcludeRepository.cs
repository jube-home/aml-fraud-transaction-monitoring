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
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class OpenTelemetryExcludeRepository(DbContext dbContext, string userName = null)
    {
        public Task<List<OpenTelemetryExclude>> GetAsync(CancellationToken token = default)
        {
            return dbContext.OpenTelemetryExclude
                .Where(w => w.Deleted == 0 || w.Deleted == null)
                .OrderByDescending(o => o.Id)
                .ToListAsync(token);
        }

        public Task<List<OpenTelemetryExclude>> GetAllActiveAsync(CancellationToken token = default)
        {
            return dbContext.OpenTelemetryExclude
                .Where(w => (w.Deleted == 0 || w.Deleted == null) && w.Active == 1)
                .ToListAsync(token);
        }

        public Task<OpenTelemetryExclude> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.OpenTelemetryExclude.FirstOrDefaultAsync(w =>
                w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<OpenTelemetryExclude> InsertAsync(OpenTelemetryExclude model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<OpenTelemetryExclude> UpdateAsync(OpenTelemetryExclude model,
            CancellationToken token = default)
        {
            var existing = await dbContext.OpenTelemetryExclude.FirstOrDefaultAsync(w =>
                w.Id == model.Id && (w.Deleted == 0 || w.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            if (existing.Locked == 1)
            {
                throw new InvalidOperationException("The row is locked and cannot be updated.");
            }

            var version = (existing.Version ?? 0) + 1;
            var updatedDate = DateTime.UtcNow;

            await dbContext.OpenTelemetryExclude
                .Where(w => w.Id == model.Id)
                .Set(s => s.Name, model.Name)
                .Set(s => s.Active, model.Active)
                .Set(s => s.Locked, model.Locked)
                .Set(s => s.UpdatedDate, updatedDate)
                .Set(s => s.UpdatedUser, userName)
                .Set(s => s.Version, version)
                .UpdateAsync(token);

            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedDate = updatedDate;
            model.UpdatedUser = userName;
            model.Version = version;
            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await dbContext.OpenTelemetryExclude.FirstOrDefaultAsync(w =>
                w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            if (existing.Locked == 1)
            {
                throw new InvalidOperationException("The row is locked and cannot be deleted.");
            }

            await dbContext.OpenTelemetryExclude
                .Where(w => w.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }
    }
}