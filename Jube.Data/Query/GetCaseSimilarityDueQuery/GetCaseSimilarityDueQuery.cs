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

namespace Jube.Data.Query.GetCaseSimilarityDueQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;

    public class GetCaseSimilarityDueQuery(DbContext dbContext)
    {
        public const string Statement = """
            SELECT c."Id" AS "CaseId", c."CaseKey", c."CaseKeyValue", c."CaseWorkflowGuid",
                   s."EntityAnalysisModelId", s."TenantRegistryId", c."CreatedDate", c."ClosedDate",
                   c."ClosedStatusId", c."RiskLaneId", c."RiskValue"
            FROM "Case" c
            LEFT JOIN LATERAL (
                SELECT m."Id" AS "EntityAnalysisModelId", m."TenantRegistryId"
                FROM "CaseWorkflow" w
                JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId" AND COALESCE(m."Deleted", 0) = 0
                WHERE w."Guid" = c."CaseWorkflowGuid" AND COALESCE(w."Deleted", 0) = 0
                ORDER BY w."Id" DESC
                LIMIT 1) s ON true
            WHERE (c."SimilarityCheckedDate" IS NULL OR COALESCE(c."ClosedStatusId", 0) IN (0, 1)
                   OR c."ClosedDate" > c."SimilarityCheckedDate")
              AND (c."SimilarityCheckedDate" IS NULL OR c."SimilarityCheckedDate" < @stale
                   OR (COALESCE(c."ClosedStatusId", 0) NOT IN (0, 1) AND c."ClosedDate" > c."SimilarityCheckedDate"))
            ORDER BY c."SimilarityCheckedDate" NULLS FIRST, c."Id"
            LIMIT @take
            """;

        public Task<List<GetCaseSimilarityDueQueryDto>> ExecuteAsync(DateTime staleBefore, int take,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetCaseSimilarityDueQueryDto>(Statement, token,
                new DataParameter("stale", staleBefore, DataType.DateTime2),
                new DataParameter("take", take, DataType.Int32));
        }
    }
}
