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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB.Data;

    public class RetentionPurgeRepository(DbContext dbContext)
    {
        private const string OrphanedQueue = """
            FROM "TransactionInboundQueue" q
            WHERE NOT EXISTS (SELECT 1 FROM "EntityAnalysisModel" m
                              WHERE m."Guid" = q."EntityAnalysisModelGuid" AND (m."Deleted" IS NULL OR m."Deleted" = 0))
            """;

        private const string Eligible = """
            a."ReferenceDate" < @cutoff
            AND NOT EXISTS (
                SELECT 1 FROM "Case" c
                WHERE c."EntityAnalysisModelInstanceEntryGuid" = a."EntityAnalysisModelInstanceEntryGuid"
                  AND (c."LegalHold" = 1 OR c."ClosedStatusId" IS NULL OR c."ClosedStatusId" <> 3))
            """;

        public async Task<long> CountEligibleArchiveAsync(DateTime cutoff, CancellationToken token = default)
        {
            var counts = await dbContext.QueryToListAsync<long>(
                $"SELECT count(*) FROM \"Archive\" a WHERE {Eligible}", token,
                new DataParameter("cutoff", cutoff)).ConfigureAwait(false);

            return counts.FirstOrDefault();
        }

        public async Task<long> PurgeArchiveChunkAsync(DateTime cutoff, int chunkSize,
            CancellationToken token = default)
        {
            var sql = $"""
                WITH doomed AS (
                    SELECT a."Id" AS id, a."EntityAnalysisModelInstanceEntryGuid" AS guid
                    FROM "Archive" a
                    WHERE {Eligible}
                    ORDER BY a."Id"
                    LIMIT {chunkSize}),
                keys AS (DELETE FROM "ArchiveKey" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM doomed)),
                key_versions AS (DELETE FROM "ArchiveKeyVersion" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM doomed)),
                tags AS (DELETE FROM "ArchiveTag" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM doomed)),
                tag_versions AS (DELETE FROM "ArchiveTagVersion" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM doomed)),
                versions AS (DELETE FROM "ArchiveVersion" WHERE "ArchiveId" IN (SELECT id FROM doomed))
                DELETE FROM "Archive" WHERE "Id" IN (SELECT id FROM doomed) RETURNING 1
                """;

            var deleted = await dbContext.QueryToListAsync<int>(sql, token, new DataParameter("cutoff", cutoff))
                .ConfigureAwait(false);

            return deleted.Count;
        }

        public async Task<long> CountExpiredAskJoobyAsync(CancellationToken token = default)
        {
            var counts = await dbContext.QueryToListAsync<long>(
                "SELECT (SELECT count(*) FROM \"AskJoobyMemory\" WHERE \"ExpiresAt\" < (now() at time zone 'utc')) + (SELECT count(*) FROM \"AskJoobyDataCache\" WHERE \"ExpiresAt\" < (now() at time zone 'utc'))",
                token).ConfigureAwait(false);

            return counts.FirstOrDefault();
        }

        public async Task<long> PurgeExpiredAskJoobyAsync(CancellationToken token = default)
        {
            var memory = await dbContext.ExecuteAsync(
                "DELETE FROM \"AskJoobyMemory\" WHERE \"ExpiresAt\" < (now() at time zone 'utc')", token)
                .ConfigureAwait(false);
            var cache = await dbContext.ExecuteAsync(
                "DELETE FROM \"AskJoobyDataCache\" WHERE \"ExpiresAt\" < (now() at time zone 'utc')", token)
                .ConfigureAwait(false);

            return memory + cache;
        }

        public async Task<long> CountOrphanedQueueAsync(CancellationToken token = default)
        {
            var counts = await dbContext.QueryToListAsync<long>("SELECT count(*) " + OrphanedQueue, token)
                .ConfigureAwait(false);

            return counts.FirstOrDefault();
        }

        public async Task<long> PurgeOrphanedQueueAsync(CancellationToken token = default)
        {
            return await dbContext.ExecuteAsync("DELETE " + OrphanedQueue, token).ConfigureAwait(false);
        }
    }
}
