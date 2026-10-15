/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Query.SearchAskJoobyRagChunkQuery;
using LinqToDB.Data;

namespace Jube.Data.Query.GetAskJoobyRagChunkByQualifiedNameQuery
{
    public class GetAskJoobyRagChunkByQualifiedNameQuery(DbContext dbContext)
    {
        private const string Sql = """
                                   SELECT "Id", "Hash", "ParentHash", "CategoryId", "KindId", "SourceFile",
                                          "StartLine", "EndLine", "QualifiedName", "Content", 0::float8 AS "Score"
                                   FROM "AskJoobyRagChunk"
                                   WHERE "QualifiedName" = ANY(@qualifiedNames)
                                   """;

        public Task<List<SearchAskJoobyRagChunkQueryDto>> ExecuteAsync(
            IReadOnlyCollection<string> qualifiedNames, CancellationToken token = default)
        {
            if (qualifiedNames.Count == 0)
            {
                return Task.FromResult(new List<SearchAskJoobyRagChunkQueryDto>());
            }

            return dbContext.QueryToListAsync<SearchAskJoobyRagChunkQueryDto>(Sql, token,
                new DataParameter("qualifiedNames", qualifiedNames.ToArray()));
        }
    }
}