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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Query.Models;
using LinqToDB.Data;

namespace Jube.Data.Query.GetErasureSubjectBlockersQuery
{
    public class GetErasureSubjectBlockersQuery(DbContext dbContext)
    {
        private const string Sql = $"""
            WITH subject AS ({ErasureSubjectSql.SubjectEntries})
            SELECT
                (SELECT count(*) FROM subject) AS "ArchiveRows",
                (SELECT count(*) FROM "Case" c WHERE c."LegalHold" = 1
                    AND (c."EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject)
                         OR (c."CaseKey" = @key AND c."CaseKeyValue" = @value
                             AND c."CaseWorkflowGuid" IN (SELECT "Guid" FROM "CaseWorkflow" WHERE "TenantRegistryId" = @tenant)))) AS "LegalHoldCases",
                (SELECT count(*) FROM "Case" c WHERE c."LegalHold" IS DISTINCT FROM 1
                    AND (c."ClosedStatusId" IS NULL OR c."ClosedStatusId" <> 3)
                    AND (c."EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject)
                         OR (c."CaseKey" = @key AND c."CaseKeyValue" = @value
                             AND c."CaseWorkflowGuid" IN (SELECT "Guid" FROM "CaseWorkflow" WHERE "TenantRegistryId" = @tenant)))) AS "OpenCases",
                (SELECT count(*) FROM "Archive" a WHERE a."EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject)
                    AND a."ReferenceDate" > @retained) AS "WithinRetention"
            """;

        public async Task<GetErasureSubjectBlockersQueryDto> ExecuteAsync(int tenantRegistryId, string keyName,
            string keyValue, int retentionMinimumDays, DateTime now, CancellationToken token = default)
        {
            var rows = await dbContext.QueryToListAsync<GetErasureSubjectBlockersQueryDto>(Sql, token,
                ErasureSubjectSql.Parameters(tenantRegistryId, keyName, keyValue,
                    new DataParameter("retained", now.AddDays(-retentionMinimumDays)))).ConfigureAwait(false);

            return rows.FirstOrDefault() ?? new GetErasureSubjectBlockersQueryDto();
        }
    }
}
