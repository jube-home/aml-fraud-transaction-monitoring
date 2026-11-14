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

    public class SanctionRescreenRepository(DbContext dbContext)
    {
        public async Task<int> GetWatermarkAsync(CancellationToken token = default)
        {
            var max = await dbContext.SanctionRescreenRun.Where(w => w.EndDate != null).MaxAsync(m => (int?)m.UpToImportId, token)
                .ConfigureAwait(false);

            return max ?? -1;
        }

        public async Task<int> GetMaxEntryImportIdAsync(CancellationToken token = default)
        {
            var max = await dbContext.SanctionEntry
                .Where(w => w.Deleted == 0 || w.Deleted == null)
                .MaxAsync(m => m.LastImportId, token).ConfigureAwait(false);

            return max ?? 0;
        }

        public async Task<List<int>> ListEntryIdsChangedAfterAsync(int watermark, CancellationToken token = default)
        {
            return await dbContext.SanctionEntry
                .Where(w => (w.Deleted == 0 || w.Deleted == null) && w.LastImportId > watermark)
                .Select(s => s.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<SanctionRescreenRun> InsertRunAsync(int upToImportId, CancellationToken token = default)
        {
            var run = new SanctionRescreenRun { UpToImportId = upToImportId, StartDate = DateTime.UtcNow };
            run.Id = await dbContext.InsertWithInt32IdentityAsync(run, token: token).ConfigureAwait(false);

            return run;
        }

        public Task<int> CompleteRunAsync(SanctionRescreenRun run, CancellationToken token = default)
        {
            return dbContext.SanctionRescreenRun.Where(w => w.Id == run.Id)
                .Set(s => s.EndDate, DateTime.UtcNow)
                .Set(s => s.ChecksScreened, run.ChecksScreened)
                .Set(s => s.NamesScreened, run.NamesScreened)
                .Set(s => s.EntriesConsidered, run.EntriesConsidered)
                .Set(s => s.Hits, run.Hits)
                .UpdateAsync(token);
        }

        public async Task<bool> InsertHitIfNewAsync(SanctionRescreenHit hit, CancellationToken token = default)
        {
            try
            {
                await dbContext.InsertAsync(hit, token: token).ConfigureAwait(false);

                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return false;
            }
        }

        public Task<List<string>> ListScreenedNamesAsync(int entityAnalysisModelId, string field, DateTime since,
            int maximum, CancellationToken token = default)
        {
            const string sql = """
                SELECT DISTINCT a."Json" -> 'payload' ->> @field AS name
                FROM "Archive" a
                WHERE a."EntityAnalysisModelId" = @model
                  AND a."ReferenceDate" >= @since
                  AND a."Json" -> 'payload' ->> @field IS NOT NULL
                LIMIT @maximum
                """;

            return dbContext.QueryToListAsync<string>(sql, token,
                new DataParameter("field", field),
                new DataParameter("model", entityAnalysisModelId),
                new DataParameter("since", since),
                new DataParameter("maximum", maximum));
        }

        public Task<List<string>> ListPermissionHoldersAsync(int tenantRegistryId, int permissionSpecificationId,
            CancellationToken token = default)
        {
            const string sql = """
                SELECT DISTINCT u."Name"
                FROM "UserRegistry" u
                JOIN "UserInTenant" t ON t."User" = u."Name" AND t."TenantRegistryId" = @tenant
                JOIN "RoleRegistry" rr ON rr."Guid" = u."RoleRegistryGuid" AND (rr."Deleted" IS NULL OR rr."Deleted" = 0)
                JOIN "RoleRegistryPermission" p ON p."RoleRegistryId" = rr."Id"
                    AND p."PermissionSpecificationId" = @permission AND (p."Deleted" IS NULL OR p."Deleted" = 0)
                WHERE (u."Deleted" IS NULL OR u."Deleted" = 0) AND (u."Active" IS NULL OR u."Active" = 1)
                """;

            return dbContext.QueryToListAsync<string>(sql, token,
                new DataParameter("tenant", tenantRegistryId),
                new DataParameter("permission", permissionSpecificationId));
        }
    }
}
