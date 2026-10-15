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
using LinqToDB.Data;

namespace Jube.Data.Query.SearchAskJoobyRagChunkQuery
{
    public class SearchAskJoobyRagChunkQuery(DbContext dbContext)
    {
        private const int ReciprocalRankFusionK = 60;
        private const int CandidateLimit = 40;

        private const string OrTsQuery =
            "to_tsquery('english', replace(plainto_tsquery('english', @queryText)::text, ' & ', ' | '))";

        private const string HybridSql = $"""
                                          WITH text_search AS (
                                              SELECT "Id",
                                                     ROW_NUMBER() OVER (ORDER BY ts_rank(to_tsvector('english', "Content"),
                                                         {OrTsQuery}) DESC) AS "Rank"
                                              FROM "AskJoobyRagChunk"
                                              WHERE to_tsvector('english', "Content") @@ {OrTsQuery}
                                              LIMIT @candidateLimit
                                          ),
                                          vector_search AS (
                                              SELECT "Id",
                                                     ROW_NUMBER() OVER (ORDER BY "Embedding" <=> @embeddingText::vector) AS "Rank"
                                              FROM "AskJoobyRagChunk"
                                              WHERE "Embedding" IS NOT NULL
                                              ORDER BY "Embedding" <=> @embeddingText::vector
                                              LIMIT @candidateLimit
                                          ),
                                          fused AS (
                                              SELECT "Id", SUM(1.0 / (@rrfK + "Rank")) AS "Score"
                                              FROM (
                                                  SELECT "Id", "Rank" FROM text_search
                                                  UNION ALL
                                                  SELECT "Id", "Rank" FROM vector_search
                                              ) combined
                                              GROUP BY "Id"
                                          )
                                          SELECT c."Id", c."Hash", c."ParentHash", c."CategoryId", c."KindId", c."SourceFile",
                                                 c."StartLine", c."EndLine", c."QualifiedName", c."Content", f."Score"
                                          FROM fused f
                                          JOIN "AskJoobyRagChunk" c ON c."Id" = f."Id"
                                          ORDER BY f."Score" DESC
                                          LIMIT @take
                                          """;

        private const string TextOnlySql = $"""
                                            SELECT "Id", "Hash", "ParentHash", "CategoryId", "KindId", "SourceFile",
                                                   "StartLine", "EndLine", "QualifiedName", "Content",
                                                   ts_rank(to_tsvector('english', "Content"), {OrTsQuery}) AS "Score"
                                            FROM "AskJoobyRagChunk"
                                            WHERE to_tsvector('english', "Content") @@ {OrTsQuery}
                                              AND ts_rank(to_tsvector('english', "Content"), {OrTsQuery}) >= @minScore
                                            ORDER BY "Score" DESC
                                            LIMIT @take
                                            """;

        public Task<List<SearchAskJoobyRagChunkQueryDto>> ExecuteAsync(string queryText, string embeddingText,
            int take, double minScore = 0, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(embeddingText))
            {
                return dbContext.QueryToListAsync<SearchAskJoobyRagChunkQueryDto>(TextOnlySql, token,
                    new DataParameter("queryText", queryText),
                    new DataParameter("minScore", minScore),
                    new DataParameter("take", take));
            }

            return dbContext.QueryToListAsync<SearchAskJoobyRagChunkQueryDto>(HybridSql, token,
                new DataParameter("queryText", queryText),
                new DataParameter("embeddingText", embeddingText),
                new DataParameter("candidateLimit", CandidateLimit),
                new DataParameter("rrfK", ReciprocalRankFusionK),
                new DataParameter("take", take));
        }
    }
}