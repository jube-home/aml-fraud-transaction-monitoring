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

namespace Jube.Data.Query.GetSimilarCaseComponentsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;

    public class GetSimilarCaseComponentsQuery(DbContext dbContext)
    {
        public const string Statement = """
            WITH total AS (
                SELECT COALESCE(MAX(f."CaseCount"), 0) AS "TotalCases"
                FROM "CaseSimilarityComponentFrequency" f
                WHERE f."CaseWorkflowGuid" = @workflow AND f."KindId" = 0 AND f."Component" = ''),
            mine AS (
                SELECT c."KindId", c."Component", f."CaseCount"
                FROM "CaseSimilarityComponent" c
                JOIN "CaseSimilarityComponentFrequency" f
                  ON f."CaseWorkflowGuid" = c."CaseWorkflowGuid" AND f."KindId" = c."KindId"
                 AND f."Component" = c."Component"
                WHERE c."CaseId" = @case AND c."CaseWorkflowGuid" = @workflow),
            seeds AS (
                SELECT m."KindId", m."Component"
                FROM mine m
                WHERE m."CaseCount" BETWEEN 2 AND @seedCap
                ORDER BY m."CaseCount", m."KindId", m."Component"
                LIMIT @seeds),
            candidates AS (
                SELECT o."CaseId"
                FROM seeds s
                CROSS JOIN LATERAL (
                    SELECT x."CaseId"
                    FROM "CaseSimilarityComponent" x
                    WHERE x."CaseWorkflowGuid" = @workflow AND x."KindId" = s."KindId"
                      AND x."Component" = s."Component" AND x."CaseId" <> @case
                    LIMIT @seedCap) o
                GROUP BY o."CaseId"
                ORDER BY COUNT(*) DESC, o."CaseId" DESC
                LIMIT @candidates)
            SELECT @case AS "CaseId", m."KindId", m."Component", m."CaseCount", t."TotalCases"
            FROM mine m CROSS JOIN total t
            UNION ALL
            SELECT c."CaseId", c."KindId", c."Component", f."CaseCount", t."TotalCases"
            FROM candidates k
            JOIN "CaseSimilarityComponent" c ON c."CaseId" = k."CaseId"
            JOIN "CaseSimilarityComponentFrequency" f
              ON f."CaseWorkflowGuid" = c."CaseWorkflowGuid" AND f."KindId" = c."KindId"
             AND f."Component" = c."Component"
            CROSS JOIN total t
            """;

        public Task<List<GetSimilarCaseComponentsQueryDto>> ExecuteAsync(int caseId, Guid caseWorkflowGuid,
            int seeds, int seedCap, int candidates, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetSimilarCaseComponentsQueryDto>(Statement, token,
                new DataParameter("case", caseId, DataType.Int32),
                new DataParameter("workflow", caseWorkflowGuid, DataType.Guid),
                new DataParameter("seeds", seeds, DataType.Int32),
                new DataParameter("seedCap", seedCap, DataType.Int32),
                new DataParameter("candidates", candidates, DataType.Int32));
        }
    }
}
