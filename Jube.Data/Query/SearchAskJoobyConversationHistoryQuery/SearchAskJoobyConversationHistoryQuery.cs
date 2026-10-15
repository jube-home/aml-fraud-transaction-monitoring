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

namespace Jube.Data.Query.SearchAskJoobyConversationHistoryQuery
{
    public class SearchAskJoobyConversationHistoryQuery(DbContext dbContext)
    {
        private const int ReciprocalRankFusionK = 60;
        private const int CandidateLimit = 40;

        private const string CandidatesCte = """
                                             candidates AS (
                                                 SELECT am."Id", am."AskJoobyConversationId" AS "ConversationId",
                                                        ac."Title", am."Content", am."IsCompact", am."Embedding",
                                                        am."CreatedDate"
                                                 FROM "AskJoobyMessage" am
                                                 JOIN "AskJoobyConversation" ac ON ac."Id" = am."AskJoobyConversationId"
                                                 WHERE am."EntryTypeId" = 1
                                                   AND (ac."Deleted" = 0 OR ac."Deleted" IS NULL)
                                                   AND ac."TenantRegistryId" = @tenantRegistryId
                                                   AND (@scope = 'System' OR ac."CreatedUser" = @userName)
                                             )
                                             """;

        private const string WeightedTsvector =
            "(setweight(to_tsvector('english', coalesce(\"Title\", '')), 'A') || " +
            "setweight(to_tsvector('english', coalesce(\"Content\", '')), 'B'))";

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
                                          SELECT c."Id", c."ConversationId", c."Title", c."Content", c."IsCompact",
                                                 c."CreatedDate", f."Score"
                                          FROM fused f
                                          JOIN candidates c ON c."Id" = f."Id"
                                          ORDER BY f."Score" DESC
                                          LIMIT @take
                                          """;

        private const string TextOnlySql = $"""
                                            WITH {CandidatesCte}
                                            SELECT "Id", "ConversationId", "Title", "Content", "IsCompact", "CreatedDate",
                                                   ts_rank({WeightedTsvector}, plainto_tsquery('english', @queryText)) AS "Score"
                                            FROM candidates
                                            WHERE {WeightedTsvector} @@ plainto_tsquery('english', @queryText)
                                            ORDER BY "Score" DESC
                                            LIMIT @take
                                            """;

        public Task<List<SearchAskJoobyConversationHistoryQueryDto>> ExecuteAsync(string queryText,
            string embeddingText, string scope, int tenantRegistryId, string userName, int take,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(embeddingText))
            {
                return dbContext.QueryToListAsync<SearchAskJoobyConversationHistoryQueryDto>(TextOnlySql, token,
                    new DataParameter("queryText", queryText),
                    new DataParameter("scope", scope),
                    new DataParameter("tenantRegistryId", tenantRegistryId),
                    new DataParameter("userName", userName),
                    new DataParameter("take", take));
            }

            return dbContext.QueryToListAsync<SearchAskJoobyConversationHistoryQueryDto>(HybridSql, token,
                new DataParameter("queryText", queryText),
                new DataParameter("embeddingText", embeddingText),
                new DataParameter("scope", scope),
                new DataParameter("tenantRegistryId", tenantRegistryId),
                new DataParameter("userName", userName),
                new DataParameter("candidateLimit", CandidateLimit),
                new DataParameter("rrfK", ReciprocalRankFusionK),
                new DataParameter("take", take));
        }
    }
}