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

namespace Jube.Data.Query
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;

    public class GetNextEntityAnalysisModelBacktestInstanceQuery(
        DbContext dbContext,
        string claimedBy,
        int perTenantCap)
    {
        public const long ClaimLockKey = 7355608120002;

        private const string AdvisoryLockSql = "SELECT pg_advisory_xact_lock(@Key)";

        private const string ClaimNextSql = """
                                            UPDATE "EntityAnalysisModelBacktestInstance" AS r
                                            SET "Status" = 2,
                                                "StartedDate" = @Now,
                                                "HeartbeatDate" = @Now,
                                                "ClaimedBy" = @ClaimedBy,
                                                "ClaimedDate" = @Now
                                            WHERE r."Id" = (
                                                SELECT p."Id"
                                                FROM "EntityAnalysisModelBacktestInstance" AS p
                                                WHERE p."Status" = 1
                                                  AND p."Deleted" = 0
                                                  AND p."Id" = (
                                                      SELECT q."Id"
                                                      FROM "EntityAnalysisModelBacktestInstance" AS q
                                                      WHERE q."TenantRegistryId" = p."TenantRegistryId"
                                                        AND q."Status" = 1
                                                        AND q."Deleted" = 0
                                                      ORDER BY q."CreatedDate", q."Id"
                                                      LIMIT 1)
                                                  AND (SELECT COUNT(*)
                                                       FROM "EntityAnalysisModelBacktestInstance" AS x
                                                       WHERE x."TenantRegistryId" = p."TenantRegistryId"
                                                         AND x."Status" IN (2, 5)
                                                         AND x."Deleted" = 0) < @PerTenantCap
                                                ORDER BY (SELECT MAX(s."StartedDate")
                                                          FROM "EntityAnalysisModelBacktestInstance" AS s
                                                          WHERE s."TenantRegistryId" = p."TenantRegistryId") ASC NULLS FIRST,
                                                         p."CreatedDate", p."Id"
                                                LIMIT 1
                                                FOR UPDATE SKIP LOCKED)
                                              AND r."Status" = 1
                                            RETURNING r."Id"
                                            """;

        public async Task<int?> ExecuteAsync(CancellationToken token = default)
        {
            await dbContext.BeginTransactionAsync(IsolationLevel.ReadCommitted, token).ConfigureAwait(false);
            try
            {
                await new CommandInfo(dbContext, AdvisoryLockSql,
                        new DataParameter("Key", ClaimLockKey, DataType.Int64))
                    .ExecuteAsync(token).ConfigureAwait(false);

                var claimed = (await new CommandInfo(dbContext, ClaimNextSql,
                            new DataParameter("Now", DateTime.UtcNow, DataType.DateTime),
                            new DataParameter("ClaimedBy", claimedBy, DataType.Text),
                            new DataParameter("PerTenantCap", Math.Max(1, perTenantCap), DataType.Int32))
                        .QueryAsync<int>(token).ConfigureAwait(false))
                    .ToList();

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                return claimed.Count == 0 ? null : claimed[0];
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
    }
}