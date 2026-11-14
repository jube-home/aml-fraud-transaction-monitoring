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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Query.Models;
using LinqToDB.Data;

namespace Jube.Data.Query.GetSubjectArchiveExportQuery
{
    public class GetSubjectArchiveExportQuery(DbContext dbContext)
    {
        private const string Sql = $"""
            WITH subject AS ({ErasureSubjectSql.SubjectEntries})
            SELECT a."EntityAnalysisModelInstanceEntryGuid" AS "EntryGuid",
                   a."EntityAnalysisModelId" AS "ModelId",
                   a."ReferenceDate" AS "ReferenceDate",
                   a."CreatedDate" AS "CreatedDate",
                   a."Json"::text AS "Json"
            FROM "Archive" a
            WHERE a."EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject)
            ORDER BY a."ReferenceDate", a."Id"
            LIMIT @limit
            """;

        public Task<List<GetSubjectArchiveExportQueryDto>> ExecuteAsync(int tenantRegistryId, string keyName,
            string keyValue, int limit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetSubjectArchiveExportQueryDto>(Sql, token,
                ErasureSubjectSql.Parameters(tenantRegistryId, keyName, keyValue, new DataParameter("limit", limit)));
        }
    }
}
