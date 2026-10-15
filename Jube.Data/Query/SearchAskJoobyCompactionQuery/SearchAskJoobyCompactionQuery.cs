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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB.Data;

namespace Jube.Data.Query.SearchAskJoobyCompactionQuery
{
    public class SearchAskJoobyCompactionQuery(DbContext dbContext)
    {
        private const int ReciprocalRankFusionK = 60;
        private const int CandidateLimit = 40;

        private const string CandidatesCte = """
                                             candidates AS (
                                                 SELECT acp."Id", acp."AskJoobyConversationId" AS "ConversationId",
                                                        ac."Title", acp."Before", acp."Embedding",
                                                        acp."CreatedDate"
                                                 FROM "AskJoobyCompaction" acp
                                                 JOIN "AskJoobyConversation" ac ON ac."Id" = acp."AskJoobyConversationId"
                                                 WHERE (ac."Deleted" = 0 OR ac."Deleted" IS NULL)
                                                   AND ac."TenantRegistryId" = @tenantRegistryId
                                                   AND (@scope = 'System' OR ac."CreatedUser" = @userName)
                                                   AND (@conversationId = 0 OR acp."AskJoobyConversationId" = @conversationId)
                                             )
                                             """;

        private const string WeightedTsvector =
            "(setweight(to_tsvector('english', coalesce(\"Title\", '')), 'A') || " +
            "setweight(to_tsvector('english', coalesce(\"Before\", '')), 'B'))";

        private const string HybridSql = $"""
                                          WITH {CandidatesCte},
                                          text_search AS (
                                              SELECT "Id",
                                                     ROW_NUMBER() OVER (ORDER BY ts_rank({WeightedTsvector},
                                                         plainto_tsquery('english', @queryText)) DESC) AS "Rank"
                                              FROM candidates
                                              WHERE {WeightedTsvector} @@ plainto_tsquery('english', @queryText)
                                              LIMIT @candidateLimit
                                          ),
                                          vector_search AS (
                                              SELECT "Id",
                                                     ROW_NUMBER() OVER (ORDER BY "Embedding" <=> @embeddingText::vector) AS "Rank"
                                              FROM candidates
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
                                          SELECT c."Id", c."ConversationId", c."Title", c."Before",
                                                 c."CreatedDate", f."Score"
                                          FROM fused f
                                          JOIN candidates c ON c."Id" = f."Id"
                                          ORDER BY f."Score" DESC
                                          LIMIT @take
                                          """;

        private const string TextOnlySql = $"""
                                            WITH {CandidatesCte}
                                            SELECT "Id", "ConversationId", "Title", "Before", "CreatedDate",
                                                   ts_rank({WeightedTsvector}, plainto_tsquery('english', @queryText)) AS "Score"
                                            FROM candidates
                                            WHERE {WeightedTsvector} @@ plainto_tsquery('english', @queryText)
                                            ORDER BY "Score" DESC
                                            LIMIT @take
                                            """;

        public Task<List<SearchAskJoobyCompactionQueryDto>> ExecuteAsync(string queryText, string embeddingText,
            string scope, int tenantRegistryId, string userName, int conversationId, int take,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(embeddingText))
            {
                return dbContext.QueryToListAsync<SearchAskJoobyCompactionQueryDto>(TextOnlySql, token,
                    new DataParameter("queryText", queryText),
                    new DataParameter("scope", scope),
                    new DataParameter("tenantRegistryId", tenantRegistryId),
                    new DataParameter("userName", userName),
                    new DataParameter("conversationId", conversationId),
                    new DataParameter("take", take));
            }

            return dbContext.QueryToListAsync<SearchAskJoobyCompactionQueryDto>(HybridSql, token,
                new DataParameter("queryText", queryText),
                new DataParameter("embeddingText", embeddingText),
                new DataParameter("scope", scope),
                new DataParameter("tenantRegistryId", tenantRegistryId),
                new DataParameter("userName", userName),
                new DataParameter("conversationId", conversationId),
                new DataParameter("candidateLimit", CandidateLimit),
                new DataParameter("rrfK", ReciprocalRankFusionK),
                new DataParameter("take", take));
        }
    }
}