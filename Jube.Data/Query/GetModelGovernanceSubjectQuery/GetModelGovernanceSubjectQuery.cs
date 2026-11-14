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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB.Data;

namespace Jube.Data.Query.GetModelGovernanceSubjectQuery
{
    public class GetModelGovernanceSubjectQuery(DbContext dbContext)
    {
        private const string Entity = """
            SELECT m."Id" AS "EntityAnalysisModelId", m."Name" AS "Name", NULL::double precision AS "Score",
                   NULL::integer AS "Models", NULL::timestamp AS "CompletedDate"
            FROM "EntityAnalysisModel" m
            WHERE m."Guid" = @guid AND m."TenantRegistryId" = @tenant AND (m."Deleted" IS NULL OR m."Deleted" = 0)
            """;

        private const string Exhaustive = """
            SELECT e."EntityAnalysisModelId", e."Name", e."Score", e."Models", e."CompletedDate"
            FROM "ExhaustiveSearchInstance" e
            JOIN "EntityAnalysisModel" m ON m."Id" = e."EntityAnalysisModelId"
            WHERE e."Guid" = @guid AND m."TenantRegistryId" = @tenant AND (m."Deleted" IS NULL OR m."Deleted" = 0)
              AND (e."Deleted" IS NULL OR e."Deleted" = 0)
            """;

        public async Task<GetModelGovernanceSubjectQueryDto> ExecuteAsync(int tenantRegistryId, short kindId,
            Guid modelGuid, CancellationToken token = default)
        {
            var rows = await dbContext.QueryToListAsync<GetModelGovernanceSubjectQueryDto>(
                kindId == 1 ? Entity : Exhaustive, token, new DataParameter("tenant", tenantRegistryId),
                new DataParameter("guid", modelGuid)).ConfigureAwait(false);

            return rows.FirstOrDefault();
        }
    }
}
