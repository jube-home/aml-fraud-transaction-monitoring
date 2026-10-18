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
using LinqToDB.Data;

namespace Jube.Data.Query.SearchAskJoobyMemoryQuery
{
    public class SearchAskJoobyMemoryQuery(DbContext dbContext, double halfLifeDays = 30)
    {
        private const int ReciprocalRankFusionK = 60;
        private const int CandidateLimit = 40;

        private const string DecayFactor =
            "POWER(0.5, EXTRACT(EPOCH FROM (now() - COALESCE(\"CreatedDate\", 'epoch'::timestamp))) " +
            "/ 86400.0 / @halfLifeDays)";

        private const string VisibilityPredicate = """
                                                   (("ScopeId" = 2 AND "TenantRegistryId" = @tenantRegistryId)
                                                     OR ("ScopeId" = 1 AND "TenantRegistryId" = @tenantRegistryId
                                                         AND "CreatedUser" = @userName)
                                                     OR ("ScopeId" = 3 AND "TenantRegistryId" = @tenantRegistryId
                                                         AND "AskJoobyConversationId" = @conversationId::int))
                                                     AND ("ExpiresAt" IS NULL OR "ExpiresAt" > now())
                                                     AND (@since::timestamp IS NULL OR "CreatedDate" >= @since::timestamp)
                                                     AND (@includeReflections = true OR "KindId" != 2)
                                                   """;

        private const string HybridSql = $"""
                                          WITH text_search AS (
                                              SELECT "Id",
                                                     ROW_NUMBER() OVER (ORDER BY ts_rank(to_tsvector('english', "Content"),
                                                         plainto_tsquery('english', @queryText)) DESC) AS "Rank"
                                              FROM "AskJoobyMemory"
                                              WHERE {VisibilityPredicate}
                                                AND to_tsvector('english', "Content") @@ plainto_tsquery('english', @queryText)
                                              LIMIT @candidateLimit
                                          ),
                                          vector_search AS (
                                              SELECT "Id",
                                                     ROW_NUMBER() OVER (ORDER BY "Embedding" <=> @embeddingText::vector) AS "Rank"
                                              FROM "AskJoobyMemory"
                                              WHERE {VisibilityPredicate}
                                                AND "Embedding" IS NOT NULL
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
                                          SELECT m."Id", m."ScopeId", m."CreatedUser", m."Content", m."StatusId",
                                                 m."CreatedDate", m."ExpiresAt", m."KindId",
                                                 f."Score" * {DecayFactor} AS "Score"
                                          FROM fused f
                                          JOIN "AskJoobyMemory" m ON m."Id" = f."Id"
                                          ORDER BY "Score" DESC
                                          LIMIT @take
                                          """;

        private const string TextOnlySql = $"""
                                            SELECT "Id", "ScopeId", "CreatedUser", "Content", "StatusId",
                                                   "CreatedDate", "ExpiresAt", "KindId",
                                                   ts_rank(to_tsvector('english', "Content"), plainto_tsquery('english', @queryText))
                                                       * {DecayFactor} AS "Score"
                                            FROM "AskJoobyMemory"
                                            WHERE {VisibilityPredicate}
                                              AND to_tsvector('english', "Content") @@ plainto_tsquery('english', @queryText)
                                            ORDER BY "Score" DESC
                                            LIMIT @take
                                            """;

        public Task<List<SearchAskJoobyMemoryQueryDto>> ExecuteAsync(string queryText, string embeddingText,
            int tenantRegistryId, string userName, int take, DateTime? since = null,
            bool includeReflections = false, int? conversationId = null, CancellationToken token = default)
        {
            var sinceParameter = new DataParameter("since", since.HasValue ? since.Value : DBNull.Value);
            var halfLifeParameter = new DataParameter("halfLifeDays", halfLifeDays > 0 ? halfLifeDays : 30);
            var includeReflectionsParameter = new DataParameter("includeReflections", includeReflections);

            var conversationIdParameter = new DataParameter("conversationId",
                conversationId.HasValue ? conversationId.Value : DBNull.Value);

            if (string.IsNullOrWhiteSpace(embeddingText))
            {
                return dbContext.QueryToListAsync<SearchAskJoobyMemoryQueryDto>(TextOnlySql, token,
                    new DataParameter("queryText", queryText),
                    new DataParameter("tenantRegistryId", tenantRegistryId),
                    new DataParameter("userName", userName),
                    new DataParameter("take", take),
                    sinceParameter,
                    halfLifeParameter,
                    includeReflectionsParameter,
                    conversationIdParameter);
            }

            return dbContext.QueryToListAsync<SearchAskJoobyMemoryQueryDto>(HybridSql, token,
                new DataParameter("queryText", queryText),
                new DataParameter("embeddingText", embeddingText),
                new DataParameter("tenantRegistryId", tenantRegistryId),
                new DataParameter("userName", userName),
                new DataParameter("candidateLimit", CandidateLimit),
                new DataParameter("rrfK", ReciprocalRankFusionK),
                new DataParameter("take", take),
                sinceParameter,
                halfLifeParameter,
                includeReflectionsParameter,
                conversationIdParameter);
        }
    }
}