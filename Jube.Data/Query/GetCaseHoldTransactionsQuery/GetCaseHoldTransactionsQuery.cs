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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetCaseHoldTransactionsQuery
{
    public class GetCaseHoldTransactionsQuery(DbContext dbContext)
    {
        public const int Maximum = 50;

        private const string Sql = """
                                   SELECT a."EntityAnalysisModelInstanceEntryGuid", a."ReferenceDate", a."CreatedDate",
                                          a."EntryKeyValue", a."Json"::text AS "Json"
                                   FROM "Archive" a
                                   INNER JOIN "EntityAnalysisModel" m ON m."Id" = a."EntityAnalysisModelId"
                                   WHERE a."EntityAnalysisModelId" = @model
                                     AND m."TenantRegistryId" = @tenant
                                     AND jsonb_exists(a."Json", 'hold')
                                     AND (a."EntityAnalysisModelInstanceEntryGuid" = @entry
                                          OR a."Json" @> @text::jsonb
                                          OR a."Json" @> @typed::jsonb)
                                   ORDER BY a."ReferenceDate" DESC NULLS LAST, a."Id" DESC
                                   LIMIT @limit
                                   """;

        public Task<List<GetCaseHoldTransactionsQueryDto>> ExecuteAsync(int entityAnalysisModelId,
            int tenantRegistryId, Guid entityAnalysisModelInstanceEntryGuid, string caseKey, string caseKeyValue,
            CancellationToken token = default)
        {
            var key = caseKey ?? string.Empty;
            var value = caseKeyValue ?? string.Empty;

            return dbContext.QueryToListAsync<GetCaseHoldTransactionsQueryDto>(Sql, token,
                new DataParameter("model", entityAnalysisModelId, DataType.Int32),
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("entry", entityAnalysisModelInstanceEntryGuid, DataType.Guid),
                new DataParameter("text", GetCaseKeyJournalRowsQuery.GetCaseKeyJournalRowsQuery.Containment(key, value), DataType.Text),
                new DataParameter("typed", GetCaseKeyJournalRowsQuery.GetCaseKeyJournalRowsQuery.TypedContainment(key, value), DataType.Text),
                new DataParameter("limit", Maximum, DataType.Int32));
        }
    }
}
