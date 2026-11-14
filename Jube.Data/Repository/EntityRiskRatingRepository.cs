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

    public class EntityRiskRatingRepository(DbContext dbContext)
    {
        public Task<EntityRiskRating> CurrentAsync(int tenantRegistryId, string keyName, string keyValue,
            CancellationToken token = default)
        {
            return dbContext.EntityRiskRating
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.KeyName == keyName && w.KeyValue == keyValue)
                .OrderByDescending(o => o.Version).FirstOrDefaultAsync(token);
        }

        public Task<List<EntityRiskRating>> HistoryAsync(int tenantRegistryId, string keyName, string keyValue,
            int take, CancellationToken token = default)
        {
            return dbContext.EntityRiskRating
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.KeyName == keyName && w.KeyValue == keyValue)
                .OrderByDescending(o => o.Version).Take(take).ToListAsync(token);
        }

        public async Task<bool> AppendAsync(EntityRiskRating model, int expectedVersion,
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

        public Task<List<EntityRiskRating>> ListDueAsync(int tenantRegistryId, DateTime now, int take,
            CancellationToken token = default)
        {
            const string sql = """
                SELECT c.* FROM "EntityRiskRating" c
                WHERE c."TenantRegistryId" = @tenant AND c."ReviewByDate" IS NOT NULL AND c."ReviewByDate" <= @now
                  AND NOT EXISTS (SELECT 1 FROM "EntityRiskRating" n
                                  WHERE n."TenantRegistryId" = c."TenantRegistryId" AND n."KeyName" = c."KeyName"
                                    AND n."KeyValue" = c."KeyValue" AND n."Version" > c."Version")
                ORDER BY c."ReviewByDate", c."Id" LIMIT @take
                """;

            return dbContext.QueryToListAsync<EntityRiskRating>(sql, token,
                new DataParameter("tenant", tenantRegistryId), new DataParameter("now", now),
                new DataParameter("take", take));
        }

        public Task<List<EntityRiskDueCount>> CountDueByTenantAsync(DateTime now, CancellationToken token = default)
        {
            const string sql = """
                SELECT c."TenantRegistryId", COUNT(*)::int AS "Due" FROM "EntityRiskRating" c
                WHERE c."ReviewByDate" IS NOT NULL AND c."ReviewByDate" <= @now
                  AND NOT EXISTS (SELECT 1 FROM "EntityRiskRating" n
                                  WHERE n."TenantRegistryId" = c."TenantRegistryId" AND n."KeyName" = c."KeyName"
                                    AND n."KeyValue" = c."KeyValue" AND n."Version" > c."Version")
                GROUP BY c."TenantRegistryId"
                """;

            return dbContext.QueryToListAsync<EntityRiskDueCount>(sql, token, new DataParameter("now", now));
        }
    }

    public class EntityRiskDueCount
    {
        public int TenantRegistryId { get; set; }
        public int Due { get; set; }
    }
}
