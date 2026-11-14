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

namespace Jube.Data.Query.ReplaceCaseSimilarityComponentsQuery
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;

    public class ReplaceCaseSimilarityComponentsQuery(DbContext dbContext)
    {
        public const string Statement = """
            WITH wanted AS (
                SELECT DISTINCT t."KindId", t."Component"
                FROM unnest(@kinds::smallint[], @components::text[]) AS t("KindId", "Component")),
            before AS (
                SELECT EXISTS (SELECT 1 FROM "CaseSimilarityComponent" c WHERE c."CaseId" = @case) AS "Had"),
            removed AS (
                DELETE FROM "CaseSimilarityComponent" c
                WHERE c."CaseId" = @case
                  AND NOT EXISTS (SELECT 1 FROM wanted w
                                  WHERE w."KindId" = c."KindId" AND w."Component" = c."Component")
                RETURNING c."KindId", c."Component"),
            added AS (
                INSERT INTO "CaseSimilarityComponent" ("CaseId", "CaseWorkflowGuid", "KindId", "Component")
                SELECT @case, @workflow, w."KindId", w."Component" FROM wanted w
                ON CONFLICT ("CaseId", "KindId", "Component") DO NOTHING
                RETURNING "KindId", "Component"),
            stamped AS (
                UPDATE "Case" SET "SimilarityCheckedDate" = @now WHERE "Id" = @case RETURNING "Id"),
            deltas AS (
                SELECT a."KindId", a."Component", 1 AS "Delta" FROM added a
                UNION ALL
                SELECT r."KindId", r."Component", -1 FROM removed r
                UNION ALL
                SELECT 0::smallint, '',
                       (CASE WHEN EXISTS (SELECT 1 FROM wanted) THEN 1 ELSE 0 END)
                       - (CASE WHEN b."Had" THEN 1 ELSE 0 END)
                FROM before b),
            counted AS (
                INSERT INTO "CaseSimilarityComponentFrequency" AS f
                    ("CaseWorkflowGuid", "KindId", "Component", "CaseCount")
                SELECT @workflow, d."KindId", d."Component", d."Delta" FROM deltas d WHERE d."Delta" <> 0
                ON CONFLICT ("CaseWorkflowGuid", "KindId", "Component")
                    DO UPDATE SET "CaseCount" = GREATEST(0, f."CaseCount" + EXCLUDED."CaseCount")
                RETURNING f."Id")
            SELECT (SELECT COUNT(*) FROM added)::integer + (SELECT COUNT(*) FROM removed)::integer
                   + 0 * (SELECT COUNT(*) FROM stamped)::integer + 0 * (SELECT COUNT(*) FROM counted)::integer
            """;

        public Task<int> ExecuteAsync(int caseId, Guid caseWorkflowGuid, short[] kinds, string[] components,
            DateTime now, CancellationToken token = default)
        {
            return dbContext.ExecuteAsync<int>(Statement, token,
                new DataParameter("case", caseId, DataType.Int32),
                new DataParameter("workflow", caseWorkflowGuid, DataType.Guid),
                new DataParameter("kinds", kinds),
                new DataParameter("components", components),
                new DataParameter("now", now, DataType.DateTime2));
        }
    }
}
