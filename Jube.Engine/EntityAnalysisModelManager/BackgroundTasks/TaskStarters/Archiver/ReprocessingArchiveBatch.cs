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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Archiver
{
    public sealed class ReprocessingArchiveBatch(int entityAnalysisModelsReprocessingRuleInstanceId)
    {
        private readonly List<(Archive Archive, IReadOnlyList<ArchiveKey> Keys)> entries = [];

        public int Count => entries.Count;

        public void Add(EntityAnalysisModelInstanceEntryPayload payload, string json)
        {
            entries.Add((new Archive
            {
                Json = json,
                EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                ResponseElevation = payload.ResponseElevation.Value,
                EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                EntryKeyValue = payload.EntityInstanceEntryId,
                EntityAnalysisModelsReprocessingRuleInstanceId = entityAnalysisModelsReprocessingRuleInstanceId
            }, payload.ArchiveKeys ?? []));
        }

        public async Task<int> FlushAsync(DbContext dbContext, CancellationToken token = default)
        {
            if (entries.Count == 0)
            {
                return 0;
            }

            try
            {
                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);
                try
                {
                    var updated = (await new ArchiveRepository(dbContext)
                            .UpdateBatchAsync(entries.Select(e => e.Archive).ToList(), token).ConfigureAwait(false))
                        .ToHashSet();

                    await new ArchiveKeyRepository(dbContext).ReplaceBatchAsync(entries
                            .Where(e => updated.Contains(e.Archive.EntityAnalysisModelInstanceEntryGuid))
                            .Select(e => (e.Archive.EntityAnalysisModelInstanceEntryGuid, e.Keys)).ToList(),
                        entityAnalysisModelsReprocessingRuleInstanceId, token).ConfigureAwait(false);

                    await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);

                    return entries.Count(e => !updated.Contains(e.Archive.EntityAnalysisModelInstanceEntryGuid));
                }
                catch
                {
                    await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    throw;
                }
            }
            finally
            {
                entries.Clear();
            }
        }
    }
}