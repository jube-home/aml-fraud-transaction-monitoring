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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;
    using Poco;

    public class ArchiveKeyRepository(DbContext dbContext)
    {
        public Task ReplaceAsync(Guid entityAnalysisModelInstanceEntryGuid, IReadOnlyList<ArchiveKey> archiveKeys,
            int? entityAnalysisModelsReprocessingRuleInstanceId, CancellationToken token = default)
        {
            return ReplaceBatchAsync([(entityAnalysisModelInstanceEntryGuid, archiveKeys)],
                entityAnalysisModelsReprocessingRuleInstanceId, token);
        }

        public async Task ReplaceBatchAsync(
            IReadOnlyList<(Guid EntityAnalysisModelInstanceEntryGuid, IReadOnlyList<ArchiveKey> ArchiveKeys)> records,
            int? entityAnalysisModelsReprocessingRuleInstanceId, CancellationToken token = default)
        {
            if (records.Count == 0)
            {
                return;
            }

            var guids = records.Select(r => r.EntityAnalysisModelInstanceEntryGuid).Distinct().ToList();
            var existingByRecord = (await dbContext.ArchiveKey
                    .Where(w => guids.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                    .ToListAsync(token).ConfigureAwait(false))
                .GroupBy(k => k.EntityAnalysisModelInstanceEntryGuid)
                .ToDictionary(g => g.Key, g => g.GroupBy(k => (k.ProcessingTypeId, k.Key))
                    .ToDictionary(k => k.Key, k => k.OrderBy(x => x.Id).ToList()));

            var inserts = new List<ArchiveKey>();
            var changed = new List<(ArchiveKey Existing, ArchiveKey Replacement)>();
            var stale = new List<long>();

            foreach (var (guid, archiveKeys) in records)
            {
                var existing = existingByRecord.GetValueOrDefault(guid) ??
                               new Dictionary<(byte?, string), List<ArchiveKey>>();

                foreach (var archiveKey in archiveKeys
                             .GroupBy(k => (k.ProcessingTypeId, k.Key))
                             .Select(g => g.Last()))
                {
                    archiveKey.EntityAnalysisModelInstanceEntryGuid = guid;
                    archiveKey.EntityAnalysisModelsReprocessingRuleInstanceId =
                        entityAnalysisModelsReprocessingRuleInstanceId;

                    if (!existing.Remove((archiveKey.ProcessingTypeId, archiveKey.Key), out var matches))
                    {
                        archiveKey.Version = 1;
                        inserts.Add(archiveKey);
                        continue;
                    }

                    stale.AddRange(matches.Skip(1).Select(k => k.Id));

                    if (!SameValue(matches[0], archiveKey))
                    {
                        changed.Add((matches[0], archiveKey));
                    }
                }

                stale.AddRange(existing.Values.SelectMany(v => v).Select(k => k.Id));
            }

            if (changed.Count > 0)
            {
                await dbContext.BulkCopyAsync(changed.Select(c => Version(c.Existing)), token).ConfigureAwait(false);

                foreach (var (current, replacement) in changed)
                {
                    replacement.Id = current.Id;
                    replacement.Version = current.Version.GetValueOrDefault() + 1;
                    await dbContext.UpdateAsync(replacement, token: token).ConfigureAwait(false);
                }
            }

            if (inserts.Count > 0)
            {
                await dbContext.BulkCopyAsync(inserts, token).ConfigureAwait(false);
            }

            if (stale.Count > 0)
            {
                await dbContext.ArchiveKeyVersion.Where(d => stale.Contains(d.ArchiveKeyId)).DeleteAsync(token)
                    .ConfigureAwait(false);
                await dbContext.ArchiveKey.Where(d => stale.Contains(d.Id)).DeleteAsync(token).ConfigureAwait(false);
            }
        }

        private static bool SameValue(ArchiveKey current, ArchiveKey replacement)
        {
            return current.KeyValueString == replacement.KeyValueString
                   && current.KeyValueInteger == replacement.KeyValueInteger
                   && Nullable.Equals(current.KeyValueFloat, replacement.KeyValueFloat)
                   && current.KeyValueBoolean == replacement.KeyValueBoolean
                   && current.KeyValueDate == replacement.KeyValueDate
                   && current.KeyValueLong == replacement.KeyValueLong;
        }

        private static ArchiveKeyVersion Version(ArchiveKey existing)
        {
            return new ArchiveKeyVersion
            {
                ArchiveKeyId = existing.Id,
                EntityAnalysisModelInstanceEntryGuid = existing.EntityAnalysisModelInstanceEntryGuid,
                ProcessingTypeId = existing.ProcessingTypeId,
                Key = existing.Key,
                KeyValueString = existing.KeyValueString,
                KeyValueInteger = existing.KeyValueInteger,
                KeyValueFloat = existing.KeyValueFloat,
                KeyValueBoolean = existing.KeyValueBoolean,
                KeyValueDate = existing.KeyValueDate,
                KeyValueLong = existing.KeyValueLong,
                Version = existing.Version,
                EntityAnalysisModelsReprocessingRuleInstanceId = existing.EntityAnalysisModelsReprocessingRuleInstanceId
            };
        }

        public Task BulkCopyAsync(List<ArchiveKey> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }
    }
}