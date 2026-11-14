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
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetGraphBuildPairsQuery
{
    public class GetGraphBuildPairsQuery(DbContext dbContext)
    {
        private const string Sql = """
            WITH defs AS (
                SELECT d."Guid" AS "Def", d."KindId", d."FromKey", d."ToKey", d."AmountKey",
                       COALESCE(d."FromNodeKey", d."FromKey") AS "FromNodeKey", COALESCE(d."ToNodeKey", d."ToKey") AS "ToNodeKey",
                       COALESCE(d."MinimumAmount", 0) AS "MinimumAmount", COALESCE(d."Restricted", 0) AS "Restricted"
                FROM "GraphRegistryEdge" d
                JOIN "GraphRegistry" r ON r."Id" = d."GraphRegistryId"
                WHERE r."TenantRegistryId" = @tenant AND r."Active" = 1 AND COALESCE(r."Deleted", 0) = 0
                  AND d."Active" = 1 AND COALESCE(d."Deleted", 0) = 0
                  AND d."Guid" = ANY(@defs::uuid[])
            )
            SELECT d."Def" AS "Definition", d."KindId", d."Restricted",
                   a."Id" AS "ArchiveId", a."EntityAnalysisModelInstanceEntryGuid",
                   COALESCE(a."ReferenceDate", a."CreatedDate") AS "OccurredDate",
                   d."FromNodeKey" AS "FromKey",
                   COALESCE(f."KeyValueString", f."KeyValueInteger"::text, f."KeyValueLong"::text, f."KeyValueFloat"::text) AS "FromValue",
                   d."ToNodeKey" AS "ToKey",
                   COALESCE(t."KeyValueString", t."KeyValueInteger"::text, t."KeyValueLong"::text, t."KeyValueFloat"::text) AS "ToValue",
                   COALESCE(am."KeyValueFloat", am."KeyValueInteger"::float8, am."KeyValueLong"::float8) AS "Amount",
                   GREATEST(f."Version", t."Version", am."Version") AS "KeyVersion"
            FROM unnest(@guids::uuid[]) AS g("Guid")
            JOIN "Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" = g."Guid"
            JOIN "EntityAnalysisModel" m ON m."Id" = a."EntityAnalysisModelId" AND m."TenantRegistryId" = @tenant
            JOIN defs d ON TRUE
            JOIN "ArchiveKey" f ON f."EntityAnalysisModelInstanceEntryGuid" = g."Guid" AND f."Key" = d."FromKey"
            JOIN "ArchiveKey" t ON t."EntityAnalysisModelInstanceEntryGuid" = g."Guid" AND t."Key" = d."ToKey"
            LEFT JOIN "ArchiveKey" am ON d."AmountKey" IS NOT NULL
                AND am."EntityAnalysisModelInstanceEntryGuid" = g."Guid" AND am."Key" = d."AmountKey"
            WHERE d."KindId" <> 2
               OR COALESCE(am."KeyValueFloat", am."KeyValueInteger"::float8, am."KeyValueLong"::float8, 0) >= d."MinimumAmount"
            """;

        public Task<List<GetGraphBuildPairsQueryDto>> ExecuteAsync(int tenantRegistryId, Guid[] definitions,
            Guid[] entryGuids, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphBuildPairsQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("defs", definitions),
                new DataParameter("guids", entryGuids));
        }
    }
}
