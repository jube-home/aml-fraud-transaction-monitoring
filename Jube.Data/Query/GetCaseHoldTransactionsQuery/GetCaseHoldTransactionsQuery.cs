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
using Jube.Data.Query.ArchiveKeyLookup;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetCaseHoldTransactionsQuery
{
    public class GetCaseHoldTransactionsQuery(DbContext dbContext)
    {
        public const int Maximum = 50;

        public Task<List<GetCaseHoldTransactionsQueryDto>> ExecuteAsync(int entityAnalysisModelId,
            int tenantRegistryId, Guid entityAnalysisModelInstanceEntryGuid, ArchiveKeyPredicate caseKey,
            CancellationToken token = default)
        {
            var parameters = new List<DataParameter>
            {
                new("model", entityAnalysisModelId, DataType.Int32),
                new("tenant", tenantRegistryId, DataType.Int32),
                new("entry", entityAnalysisModelInstanceEntryGuid, DataType.Guid),
                new("limit", Maximum, DataType.Int32)
            };

            var keyCondition = caseKey is null
                ? "false"
                : ArchiveKeyLookupSql.Exists(caseKey, "a", value =>
                {
                    parameters.Add(new DataParameter($"p{parameters.Count}", value));

                    return $"@p{parameters.Count - 1}";
                });

            var sql = "SELECT a.\"EntityAnalysisModelInstanceEntryGuid\", a.\"ReferenceDate\", a.\"CreatedDate\", " +
                      "a.\"EntryKeyValue\", a.\"Json\"::text AS \"Json\" " +
                      "FROM \"Archive\" a " +
                      "INNER JOIN \"EntityAnalysisModel\" m ON m.\"Id\" = a.\"EntityAnalysisModelId\" " +
                      "WHERE a.\"EntityAnalysisModelId\" = @model AND m.\"TenantRegistryId\" = @tenant " +
                      "AND a.\"Json\" ? 'hold' " +
                      $"AND (a.\"EntityAnalysisModelInstanceEntryGuid\" = @entry OR {keyCondition}) " +
                      "ORDER BY a.\"ReferenceDate\" DESC NULLS LAST, a.\"Id\" DESC LIMIT @limit";

            return dbContext.QueryToListAsync<GetCaseHoldTransactionsQueryDto>(sql, token, parameters.ToArray());
        }
    }
}