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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;
    using Npgsql;
    using Poco;

    public class CaseServiceLevelBreachRepository(DbContext dbContext)
    {
        public const short FirstView = 1;
        public const short Ownership = 2;
        public const short Resolution = 3;
        public const short StatusDwell = 4;

        private const string Select = """
            SELECT c."Id" AS "CaseId", c."CaseWorkflowGuid", c."LockedUser", m."TenantRegistryId"
            FROM "Case" c
            JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" AND (w."Deleted" IS NULL OR w."Deleted" = 0)
            JOIN "CaseWorkflowStatus" s ON s."Guid" = c."CaseWorkflowStatusGuid" AND (s."Deleted" IS NULL OR s."Deleted" = 0)
            JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId"
            WHERE c."ClosedStatusId" = 0
              AND m."TenantRegistryId" IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM "CaseServiceLevelBreach" b WHERE b."CaseId" = c."Id" AND b."DimensionId" = @dimension)
            """;

        private const string Utc = "(now() at time zone 'utc')";

        private static string Predicate(short dimension)
        {
            return dimension switch
            {
                FirstView => $"""
                    AND w."FirstViewTargetMinutes" IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM "CaseEvent" e WHERE e."CaseId" = c."Id" AND e."CaseEventTypeId" IN (2, 4, 16))
                    AND c."CreatedDate" < {Utc} - make_interval(mins => w."FirstViewTargetMinutes")
                    """,
                Ownership => $"""
                    AND w."OwnershipTargetMinutes" IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM "CaseEvent" e WHERE e."CaseId" = c."Id" AND e."CaseEventTypeId" IN (2, 3, 6, 14))
                    AND c."CreatedDate" < {Utc} - make_interval(mins => w."OwnershipTargetMinutes")
                    """,
                Resolution => $"""
                    AND w."ResolutionTargetMinutes" IS NOT NULL
                    AND c."CreatedDate" < {Utc} - make_interval(mins => w."ResolutionTargetMinutes")
                    """,
                StatusDwell => $"""
                    AND s."MaxDwellMinutes" IS NOT NULL
                    AND EXISTS (SELECT 1 FROM "CaseStatusTransition" t WHERE t."CaseId" = c."Id" AND t."ExitedDate" IS NULL
                                AND t."EnteredDate" < {Utc} - make_interval(mins => s."MaxDwellMinutes"))
                    """,
                _ => throw new ArgumentOutOfRangeException(nameof(dimension))
            };
        }

        public Task<List<CaseServiceLevelBreachCandidate>> FindUnrecordedAsync(short dimension, int take,
            CancellationToken token = default)
        {
            var sql = $"{Select} {Predicate(dimension)} ORDER BY c.\"Id\" LIMIT {take}";

            return dbContext.QueryToListAsync<CaseServiceLevelBreachCandidate>(sql, token,
                new DataParameter("dimension", dimension));
        }

        public async Task<bool> RecordAsync(int caseId, short dimension, CancellationToken token = default)
        {
            try
            {
                await dbContext.InsertAsync(new CaseServiceLevelBreach
                {
                    CaseId = caseId, DimensionId = dimension, BreachedDate = DateTime.UtcNow
                }, token: token).ConfigureAwait(false);

                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return false;
            }
        }

        public Task<List<string>> ListSupervisorsAsync(int tenantRegistryId, Guid caseWorkflowGuid,
            int supervisionPermission, CancellationToken token = default)
        {
            const string sql = """
                SELECT DISTINCT u."Name"
                FROM "UserRegistry" u
                JOIN "UserInTenant" t ON t."User" = u."Name" AND t."TenantRegistryId" = @tenant
                JOIN "RoleRegistry" rr ON rr."Guid" = u."RoleRegistryGuid" AND (rr."Deleted" IS NULL OR rr."Deleted" = 0)
                JOIN "RoleRegistryPermission" p ON p."RoleRegistryId" = rr."Id"
                    AND p."PermissionSpecificationId" = @permission
                    AND (p."Deleted" IS NULL OR p."Deleted" = 0)
                JOIN "CaseWorkflowRole" cr ON cr."RoleRegistryGuid" = rr."Guid" AND cr."CaseWorkflowGuid" = @workflow
                    AND (cr."Deleted" IS NULL OR cr."Deleted" = 0)
                WHERE (u."Deleted" IS NULL OR u."Deleted" = 0) AND (u."Active" IS NULL OR u."Active" = 1)
                """;

            return dbContext.QueryToListAsync<string>(sql, token,
                new DataParameter("tenant", tenantRegistryId),
                new DataParameter("permission", supervisionPermission),
                new DataParameter("workflow", caseWorkflowGuid));
        }
    }

    public class CaseServiceLevelBreachCandidate
    {
        public int CaseId { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public string LockedUser { get; set; }
        public int TenantRegistryId { get; set; }
    }
}
