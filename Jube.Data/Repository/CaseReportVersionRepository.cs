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

    public class CaseReportVersionRepository(DbContext dbContext)
    {
        public Task<List<CaseReportVersion>> ListAsync(long caseReportId, CancellationToken token = default)
        {
            return dbContext.CaseReportVersion.Where(w => w.CaseReportId == caseReportId)
                .OrderByDescending(o => o.Version).ToListAsync(token);
        }

        public Task<CaseReportVersion> GetAsync(long caseReportId, int version, CancellationToken token = default)
        {
            return dbContext.CaseReportVersion.FirstOrDefaultAsync(w => w.CaseReportId == caseReportId
                                                                         && w.Version == version, token);
        }

        public async Task InsertAsync(CaseReportVersion model, CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);
        }

        public Task<int> UpdateDataAsync(long id, string dataJson, CancellationToken token = default)
        {
            return dbContext.CaseReportVersion.Where(w => w.Id == id && w.Xml == null)
                .Set(s => s.DataJson, dataJson).UpdateAsync(token);
        }

        public Task<int> RecordValidationAsync(long id, string dataJson, string issuesJson, string xml,
            string sha256, CancellationToken token = default)
        {
            return dbContext.CaseReportVersion.Where(w => w.Id == id && w.Xml == null)
                .Set(s => s.DataJson, dataJson).Set(s => s.IssuesJson, issuesJson)
                .Set(s => s.Xml, xml).Set(s => s.PayloadSha256, sha256).UpdateAsync(token);
        }
    }
}
