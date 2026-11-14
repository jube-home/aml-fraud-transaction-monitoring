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

namespace Jube.Engine.Retention
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Repository;

    public sealed record RetentionPurgeResult(long Archive, long AskJooby, long Queue, bool DryRun);

    public static class RetentionPurge
    {
        public static async Task<RetentionPurgeResult> RunAsync(DbContext dbContext, int archiveDays, bool dryRun,
            int chunkSize, DateTime now, CancellationToken token = default)
        {
            var repository = new RetentionPurgeRepository(dbContext);
            var archive = 0L;

            if (archiveDays > 0)
            {
                var cutoff = now.AddDays(-archiveDays);

                if (dryRun)
                {
                    archive = await repository.CountEligibleArchiveAsync(cutoff, token).ConfigureAwait(false);
                }
                else
                {
                    long chunk;

                    do
                    {
                        token.ThrowIfCancellationRequested();
                        chunk = await repository.PurgeArchiveChunkAsync(cutoff, chunkSize, token)
                            .ConfigureAwait(false);
                        archive += chunk;
                    } while (chunk == chunkSize);
                }
            }

            var askJooby = dryRun
                ? await repository.CountExpiredAskJoobyAsync(token).ConfigureAwait(false)
                : await repository.PurgeExpiredAskJoobyAsync(token).ConfigureAwait(false);

            var queue = dryRun
                ? await repository.CountOrphanedQueueAsync(token).ConfigureAwait(false)
                : await repository.PurgeOrphanedQueueAsync(token).ConfigureAwait(false);

            return new RetentionPurgeResult(archive, askJooby, queue, dryRun);
        }
    }
}
