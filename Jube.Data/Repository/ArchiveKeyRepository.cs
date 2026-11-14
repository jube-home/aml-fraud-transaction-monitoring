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
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Poco;

    public class ArchiveKeyRepository(DbContext dbContext)
    {
        public const byte RequestXPathProcessingTypeId = 1;
        public const string RequestKeyIndexName = "IX_ArchiveKey_Entry_Request";
        private static readonly TimeSpan RequestKeyIndexProbeInterval = TimeSpan.FromMinutes(5);
        private static volatile bool requestKeyIndexReady;
        private static long requestKeyIndexProbedTicks;

        private const string RequestKeyIndexSql = """
            select coalesce(bool_or(i.indisvalid), false)
            from pg_index i
            join pg_class c on c.oid = i.indexrelid
            where c.relname = 'IX_ArchiveKey_Entry_Request' and c.relnamespace = 'public'::regnamespace
            """;

        private const string UpsertRequestKeysSql = """
            insert into "ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueInteger", "KeyValueFloat",
                                      "KeyValueBoolean", "KeyValueDate", "KeyValueLong",
                                      "EntityAnalysisModelInstanceEntryGuid", "Version",
                                      "EntityAnalysisModelsReprocessingRuleInstanceId")
            select 1, u."Key", u."S", u."I", u."F", u."B", u."D", u."L", u."Guid", 1, @rule
            from unnest(@keys::text[], @strings::text[], @integers::int[], @floats::float8[], @booleans::smallint[],
                        @dates::timestamp[], @longs::bigint[], @guids::uuid[]) as u("Key", "S", "I", "F", "B", "D", "L", "Guid")
            on conflict ("EntityAnalysisModelInstanceEntryGuid", "Key") where "ProcessingTypeId" = 1 do update set
                "KeyValueString" = excluded."KeyValueString",
                "KeyValueInteger" = excluded."KeyValueInteger",
                "KeyValueFloat" = excluded."KeyValueFloat",
                "KeyValueBoolean" = excluded."KeyValueBoolean",
                "KeyValueDate" = excluded."KeyValueDate",
                "KeyValueLong" = excluded."KeyValueLong",
                "Version" = coalesce("ArchiveKey"."Version", 1) + 1,
                "EntityAnalysisModelsReprocessingRuleInstanceId" = excluded."EntityAnalysisModelsReprocessingRuleInstanceId"
            where "ArchiveKey"."KeyValueString" is distinct from excluded."KeyValueString"
               or "ArchiveKey"."KeyValueInteger" is distinct from excluded."KeyValueInteger"
               or "ArchiveKey"."KeyValueFloat" is distinct from excluded."KeyValueFloat"
               or "ArchiveKey"."KeyValueBoolean" is distinct from excluded."KeyValueBoolean"
               or "ArchiveKey"."KeyValueDate" is distinct from excluded."KeyValueDate"
               or "ArchiveKey"."KeyValueLong" is distinct from excluded."KeyValueLong"
            """;

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

                var replacesRequestKeys = archiveKeys.Any(k => k.ProcessingTypeId == RequestXPathProcessingTypeId);

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

                stale.AddRange(existing.Values.SelectMany(v => v)
                    .Where(k => replacesRequestKeys || k.ProcessingTypeId != RequestXPathProcessingTypeId)
                    .Select(k => k.Id));
            }

            if (changed.Count > 0)
            {
                await dbContext.BulkCopyAsync(changed.Select(c => Version(c.Existing)), token).ConfigureAwait(false);

                foreach (var (current, replacement) in changed)
                {
                    replacement.Id = current.Id;
                    replacement.Version = current.Version.GetValueOrDefault(1) + 1;
                    await dbContext.UpdateAsync(replacement, token: token).ConfigureAwait(false);
                }
            }

            if (inserts.Count > 0)
            {
                await InsertAsync(inserts, entityAnalysisModelsReprocessingRuleInstanceId, token)
                    .ConfigureAwait(false);
            }

            if (stale.Count > 0)
            {
                await dbContext.ArchiveKeyVersion.Where(d => stale.Contains(d.ArchiveKeyId)).DeleteAsync(token)
                    .ConfigureAwait(false);
                await dbContext.ArchiveKey.Where(d => stale.Contains(d.Id)).DeleteAsync(token).ConfigureAwait(false);
            }
        }

        public Task BulkCopyAsync(List<ArchiveKey> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }

        public async Task<bool> RequestKeyIndexReadyAsync(CancellationToken token = default)
        {
            if (requestKeyIndexReady)
            {
                return true;
            }

            var now = DateTime.UtcNow.Ticks;

            if (now - Interlocked.Read(ref requestKeyIndexProbedTicks) < RequestKeyIndexProbeInterval.Ticks)
            {
                return false;
            }

            Interlocked.Exchange(ref requestKeyIndexProbedTicks, now);

            requestKeyIndexReady = await dbContext.ExecuteAsync<bool>(RequestKeyIndexSql, token).ConfigureAwait(false);

            return requestKeyIndexReady;
        }

        private async Task InsertAsync(List<ArchiveKey> inserts, int? entityAnalysisModelsReprocessingRuleInstanceId,
            CancellationToken token)
        {
            var requestKeys = inserts.Where(k => k.ProcessingTypeId == RequestXPathProcessingTypeId).ToList();
            var otherKeys = inserts.Where(k => k.ProcessingTypeId != RequestXPathProcessingTypeId).ToList();

            if (requestKeys.Count > 0 && await RequestKeyIndexReadyAsync(token).ConfigureAwait(false))
            {
                await dbContext.ExecuteAsync(UpsertRequestKeysSql, token,
                    new DataParameter("rule", entityAnalysisModelsReprocessingRuleInstanceId, DataType.Int32),
                    new DataParameter("keys", requestKeys.Select(k => k.Key).ToArray()),
                    new DataParameter("strings", requestKeys.Select(k => k.KeyValueString).ToArray()),
                    new DataParameter("integers", requestKeys.Select(k => k.KeyValueInteger).ToArray()),
                    new DataParameter("floats", requestKeys.Select(k => k.KeyValueFloat).ToArray()),
                    new DataParameter("booleans", requestKeys.Select(k => (short?)k.KeyValueBoolean).ToArray()),
                    new DataParameter("dates", requestKeys.Select(k => k.KeyValueDate).ToArray()),
                    new DataParameter("longs", requestKeys.Select(k => k.KeyValueLong).ToArray()),
                    new DataParameter("guids", requestKeys.Select(k => k.EntityAnalysisModelInstanceEntryGuid).ToArray()))
                    .ConfigureAwait(false);
            }
            else
            {
                otherKeys = inserts;
            }

            if (otherKeys.Count > 0)
            {
                await dbContext.BulkCopyAsync(otherKeys, token).ConfigureAwait(false);
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
                Version = existing.Version ?? 1,
                EntityAnalysisModelsReprocessingRuleInstanceId = existing.EntityAnalysisModelsReprocessingRuleInstanceId
            };
        }
    }
}
