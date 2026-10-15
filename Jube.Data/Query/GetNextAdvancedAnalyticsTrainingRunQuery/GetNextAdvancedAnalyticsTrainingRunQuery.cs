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
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetNextAdvancedAnalyticsTrainingRunQuery
{
    public class GetNextAdvancedAnalyticsTrainingRunQuery(
        DbContext dbContext,
        string claimedBy,
        int perTenantCap,
        DateTime? now = null)
    {
        public const long ClaimLockKey = 7355608120001;
        private const string AdvisoryLockSql = "SELECT pg_advisory_xact_lock(@Key)";

        private const string ClaimNextSql = """
                                            UPDATE "AdvancedAnalyticsTrainingRun" AS r
                                            SET "StatusId" = 2,
                                                "StartedDate" = @Now,
                                                "HeartbeatDate" = @Now,
                                                "UpdatedDate" = @Now,
                                                "ClaimedBy" = @ClaimedBy,
                                                "ClaimedDate" = @Now
                                            WHERE r."Id" = (
                                                SELECT p."Id"
                                                FROM "AdvancedAnalyticsTrainingRun" AS p
                                                WHERE p."StatusId" = 1
                                                  AND p."Deleted" = 0
                                                  AND p."JobMode" = 2
                                                  AND p."Id" = (
                                                      SELECT q."Id"
                                                      FROM "AdvancedAnalyticsTrainingRun" AS q
                                                      WHERE q."TenantRegistryId" = p."TenantRegistryId"
                                                        AND q."StatusId" = 1
                                                        AND q."Deleted" = 0
                                                        AND q."JobMode" = 2
                                                      ORDER BY q."CreatedDate", q."Id"
                                                      LIMIT 1)
                                                  AND (SELECT COUNT(*)
                                                       FROM "AdvancedAnalyticsTrainingRun" AS x
                                                       WHERE x."TenantRegistryId" = p."TenantRegistryId"
                                                         AND x."StatusId" IN (2, 5)
                                                         AND x."Deleted" = 0
                                                         AND x."JobMode" = 2) < @PerTenantCap
                                                ORDER BY (SELECT MAX(s."StartedDate")
                                                          FROM "AdvancedAnalyticsTrainingRun" AS s
                                                          WHERE s."TenantRegistryId" = p."TenantRegistryId") NULLS FIRST,
                                                         p."CreatedDate", p."Id"
                                                LIMIT 1
                                                FOR UPDATE SKIP LOCKED)
                                              AND r."StatusId" = 1
                                            RETURNING r."Id"
                                            """;

        public async Task<GetNextAdvancedAnalyticsTrainingRunQueryDto> ExecuteAsync(CancellationToken token = default)
        {
            var stamp = now ?? DateTime.UtcNow;

            await dbContext.BeginTransactionAsync(IsolationLevel.ReadCommitted, token).ConfigureAwait(false);

            try
            {
                await new CommandInfo(dbContext, AdvisoryLockSql,
                        new DataParameter("Key", ClaimLockKey, DataType.Int64))
                    .ExecuteAsync(token).ConfigureAwait(false);

                var claimed = (await new CommandInfo(dbContext, ClaimNextSql,
                            new DataParameter("Now", stamp, DataType.DateTime),
                            new DataParameter("ClaimedBy", claimedBy, DataType.Text),
                            new DataParameter("PerTenantCap", Math.Max(1, perTenantCap), DataType.Int32))
                        .QueryAsync<int>(token).ConfigureAwait(false))
                    .ToList();

                if (claimed.Count == 0)
                {
                    await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);

                    return null;
                }

                var dto = await dbContext.AdvancedAnalyticsTrainingRun
                    .Where(w => w.Id == claimed[0])
                    .Select(s => new GetNextAdvancedAnalyticsTrainingRunQueryDto
                    {
                        Id = s.Id,
                        Guid = s.Guid,
                        TenantRegistryId = s.TenantRegistryId,
                        AdvancedAnalyticsInstanceId = s.AdvancedAnalyticsInstanceId,
                        UserName = s.UserName,
                        Arguments = s.Arguments,
                        InputPointer = s.InputPointer,
                        Seed = s.Seed,
                        Kind = s.KindId,
                        ModelKindId = s.ModelKindId
                    })
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);

                return dto;
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
    }
}