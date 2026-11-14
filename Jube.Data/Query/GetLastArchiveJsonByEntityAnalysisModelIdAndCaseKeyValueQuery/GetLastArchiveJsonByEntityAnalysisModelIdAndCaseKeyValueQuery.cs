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

namespace Jube.Data.Query.GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using ArchiveKeyLookup;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;

    public class GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery(DbContext dbContext)
    {
        public async Task<(string Json, Guid EntityAnalysisModelInstanceEntryGuid)?> ExecuteAsync(
            int entityAnalysisModelId, ArchiveKeyPredicate caseKey, CancellationToken token = default)
        {
            var parameters = new List<DataParameter> { new("model", entityAnalysisModelId, DataType.Int32) };

            var sql = "select a.\"Json\"::text as \"Json\", a.\"EntityAnalysisModelInstanceEntryGuid\" " +
                      ArchiveKeyLookupSql.From(caseKey, value =>
                      {
                          parameters.Add(new DataParameter($"p{parameters.Count}", value));

                          return $"@p{parameters.Count - 1}";
                      }) +
                      "and a.\"EntityAnalysisModelId\" = @model order by a.\"Id\" desc limit 1";

            var rows = await dbContext.QueryToListAsync<GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQueryDto>(
                sql, token, parameters.ToArray()).ConfigureAwait(false);

            return rows.Count == 0 ? null : (rows[0].Json, rows[0].EntityAnalysisModelInstanceEntryGuid);
        }
    }
}
