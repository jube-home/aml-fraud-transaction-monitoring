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
    using Npgsql;
    using Poco;

    public class CaseReportProfileRepository(DbContext dbContext)
    {
        public Task<CaseReportProfile> CurrentAsync(int tenantRegistryId, string name, CancellationToken token = default)
        {
            return dbContext.CaseReportProfile.Where(w => w.TenantRegistryId == tenantRegistryId && w.Name == name)
                .OrderByDescending(o => o.Version).FirstOrDefaultAsync(token);
        }

        public Task<CaseReportProfile> VersionAsync(int tenantRegistryId, string name, int version,
            CancellationToken token = default)
        {
            return dbContext.CaseReportProfile.FirstOrDefaultAsync(w => w.TenantRegistryId == tenantRegistryId
                                                                         && w.Name == name && w.Version == version, token);
        }

        public Task<List<CaseReportProfile>> ListCurrentAsync(int tenantRegistryId, CancellationToken token = default)
        {
            return (from p in dbContext.CaseReportProfile
                where p.TenantRegistryId == tenantRegistryId
                      && p.Version == dbContext.CaseReportProfile.Where(x => x.TenantRegistryId == p.TenantRegistryId
                                                                           && x.Name == p.Name).Max(x => x.Version)
                orderby p.Name
                select p).ToListAsync(token);
        }

        public async Task<bool> AppendAsync(CaseReportProfile model, int currentVersion,
            CancellationToken token = default)
        {
            model.Version = currentVersion + 1;
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
