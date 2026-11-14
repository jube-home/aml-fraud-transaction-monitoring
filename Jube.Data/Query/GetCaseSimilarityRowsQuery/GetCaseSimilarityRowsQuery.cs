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

namespace Jube.Data.Query.GetCaseSimilarityRowsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using ArchiveKeyLookup;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;

    public class GetCaseSimilarityRowsQuery(DbContext dbContext)
    {
        public static string Statement(ArchiveKeyPredicate caseKey, Func<object, string> bind)
        {
            return "select a.\"Id\", a.\"EntityAnalysisModelInstanceEntryGuid\", a.\"Json\"::text as \"Json\" " +
                   ArchiveKeyLookupSql.From(caseKey, bind) +
                   "and a.\"EntityAnalysisModelId\" = @model " +
                   "and a.\"CreatedDate\" >= @from and a.\"CreatedDate\" <= @to " +
                   "order by a.\"Id\" desc limit @take";
        }

        public Task<List<GetCaseSimilarityRowsQueryDto>> ExecuteAsync(int entityAnalysisModelId,
            ArchiveKeyPredicate caseKey, DateTime from, DateTime to, int take, CancellationToken token = default)
        {
            var parameters = new List<DataParameter>
            {
                new("model", entityAnalysisModelId, DataType.Int32),
                new("from", from, DataType.DateTime2),
                new("to", to, DataType.DateTime2),
                new("take", take, DataType.Int32)
            };

            var sql = Statement(caseKey, value =>
            {
                parameters.Add(new DataParameter($"p{parameters.Count}", value));

                return $"@p{parameters.Count - 1}";
            });

            return dbContext.QueryToListAsync<GetCaseSimilarityRowsQueryDto>(sql, token, [.. parameters]);
        }
    }
}
