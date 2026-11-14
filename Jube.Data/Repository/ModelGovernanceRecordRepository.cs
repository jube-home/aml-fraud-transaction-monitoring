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
    using LinqToDB.Data;
    using Npgsql;
    using Poco;

    public class ModelGovernanceRecordRepository(DbContext dbContext)
    {
        public Task<ModelGovernanceRecord> CurrentAsync(int tenantRegistryId, short kindId, Guid modelGuid,
            CancellationToken token = default)
        {
            return dbContext.ModelGovernanceRecord
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.KindId == kindId && w.ModelGuid == modelGuid)
                .OrderByDescending(o => o.Version).FirstOrDefaultAsync(token);
        }

        public Task<List<ModelGovernanceRecord>> HistoryAsync(int tenantRegistryId, short kindId, Guid modelGuid,
            int take, CancellationToken token = default)
        {
            return dbContext.ModelGovernanceRecord
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.KindId == kindId && w.ModelGuid == modelGuid)
                .OrderByDescending(o => o.Version).Take(take).ToListAsync(token);
        }

        public Task<List<ModelGovernanceRecord>> ListCurrentAsync(int tenantRegistryId,
            CancellationToken token = default)
        {
            const string sql = """
                SELECT DISTINCT ON ("KindId","ModelGuid") *
                FROM "ModelGovernanceRecord" WHERE "TenantRegistryId" = @tenant
                ORDER BY "KindId","ModelGuid","Version" DESC
                """;

            return dbContext.QueryToListAsync<ModelGovernanceRecord>(sql, token,
                new DataParameter("tenant", tenantRegistryId));
        }

        public async Task<bool> AppendAsync(ModelGovernanceRecord model, int expectedVersion,
            CancellationToken token = default)
        {
            model.Version = expectedVersion + 1;
            model.CreatedDate = DateTime.UtcNow;

            try
            {
                await dbContext.InsertAsync(model, token: token).ConfigureAwait(false);

                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return false;
            }
        }
    }
}
