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
    using LinqToDB.Async;
    using Npgsql;
    using Poco;

    public class SanctionExemptionRepository(DbContext dbContext)
    {
        public const short Pending = 1;
        public const short Approved = 2;
        public const short Rejected = 3;
        public const short Retracted = 4;

        public async Task<SanctionExemption> InsertAsync(SanctionExemption model, CancellationToken token = default)
        {
            try
            {
                model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

                return model;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return null;
            }
        }

        public Task<SanctionExemption> GetAsync(int tenantRegistryId, int id, CancellationToken token = default)
        {
            return dbContext.SanctionExemption
                .FirstOrDefaultAsync(w => w.Id == id && w.TenantRegistryId == tenantRegistryId, token);
        }

        public Task<List<SanctionExemption>> ListAsync(int tenantRegistryId, short? stateId, int take,
            CancellationToken token = default)
        {
            return dbContext.SanctionExemption
                .Where(w => w.TenantRegistryId == tenantRegistryId && (stateId == null || w.StateId == stateId))
                .OrderByDescending(o => o.Id)
                .Take(take)
                .ToListAsync(token);
        }

        public Task<int> DecideAsync(int tenantRegistryId, int id, short expectedState, short newState, string user,
            string note, CancellationToken token = default)
        {
            return dbContext.SanctionExemption
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId && w.StateId == expectedState)
                .Set(s => s.StateId, newState)
                .Set(s => s.DecidedUser, user)
                .Set(s => s.DecidedDate, DateTime.UtcNow)
                .Set(s => s.DecisionNote, note)
                .UpdateAsync(token);
        }

        public Task<List<SanctionExemption>> ListApprovedAsync(CancellationToken token = default)
        {
            return dbContext.SanctionExemption
                .Where(w => w.StateId == Approved)
                .ToListAsync(token);
        }
    }
}
