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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Poco;

    public class ArchiveRepository(DbContext dbContext)
    {
        public Task<int?> GetTenantRegistryIdByEntityAnalysisModelInstanceEntryGuidAsync(
            Guid entityAnalysisModelInstanceEntryGuid,
            CancellationToken token = default)
        {
            return dbContext.Archive
                .Where(w => w.EntityAnalysisModelInstanceEntryGuid == entityAnalysisModelInstanceEntryGuid)
                .Select(s => s.EntityAnalysisModel.TenantRegistryId)
                .FirstOrDefaultAsync(token);
        }

        public Task<Archive> GetByEntityAnalysisModelInstanceEntryGuidAsync(
            Guid entityAnalysisModelInstanceEntryGuid, int entityAnalysisModelId, int tenantRegistryId,
            CancellationToken token = default)
        {
            return dbContext.Archive.FirstOrDefaultAsync(w =>
                w.EntityAnalysisModelInstanceEntryGuid == entityAnalysisModelInstanceEntryGuid
                && w.EntityAnalysisModelId == entityAnalysisModelId
                && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId, token);
        }

        public async Task<Archive> UpdateTagsByEntityAnalysisModelInstanceEntryGuidAsync(
            Guid entityAnalysisModelInstanceEntryGuid,
            string[] tags,
            CancellationToken token = default)
        {
            var archive = await dbContext.Archive.FirstOrDefaultAsync(w =>
                              w.EntityAnalysisModelInstanceEntryGuid == entityAnalysisModelInstanceEntryGuid, token)
                          ?? throw new InvalidOperationException(
                              $"No archive record found for {entityAnalysisModelInstanceEntryGuid}.");

            var jObject = JObject.Parse(archive.Json);
            jObject["tag"] = new JArray([.. tags]);
            archive.Json = jObject.ToString(Formatting.None);

            await dbContext.UpdateAsync(archive, token: token);

            return archive;
        }

        public async Task UpdateAsync(Archive model, CancellationToken token = default)
        {
            var guid = model.EntityAnalysisModelInstanceEntryGuid;

            await dbContext.Archive
                .Where(w => w.EntityAnalysisModelInstanceEntryGuid == guid)
                .InsertAsync(dbContext.ArchiveVersion, existing => new ArchiveVersion
                {
                    ArchiveId = existing.Id,
                    Json = existing.Json,
                    EntityAnalysisModelInstanceEntryGuid = existing.EntityAnalysisModelInstanceEntryGuid,
                    EntryKeyValue = existing.EntryKeyValue,
                    ResponseElevation = existing.ResponseElevation,
                    EntityAnalysisModelActivationRuleId = existing.EntityAnalysisModelActivationRuleId,
                    EntityAnalysisModelId = existing.EntityAnalysisModelId,
                    ActivationRuleCount = existing.ActivationRuleCount,
                    CreatedDate = existing.CreatedDate,
                    ReferenceDate = existing.ReferenceDate,
                    Version = existing.Version,
                    EntityAnalysisModelsReprocessingRuleInstanceId =
                        existing.EntityAnalysisModelsReprocessingRuleInstanceId
                }, token).ConfigureAwait(false);

            var updated = await dbContext.Archive
                .Where(w => w.EntityAnalysisModelInstanceEntryGuid == guid)
                .Set(s => s.Json, model.Json)
                .Set(s => s.ResponseElevation, model.ResponseElevation)
                .Set(s => s.EntityAnalysisModelActivationRuleId, model.EntityAnalysisModelActivationRuleId)
                .Set(s => s.ActivationRuleCount, model.ActivationRuleCount)
                .Set(s => s.EntryKeyValue, model.EntryKeyValue)
                .Set(s => s.EntityAnalysisModelsReprocessingRuleInstanceId,
                    model.EntityAnalysisModelsReprocessingRuleInstanceId)
                .Set(s => s.Version, s => (s.Version ?? 0) + 1)
                .Set(s => s.CreatedDate, DateTime.UtcNow)
                .UpdateAsync(token).ConfigureAwait(false);

            if (updated == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task<IReadOnlyList<Guid>> UpdateBatchAsync(IReadOnlyList<Archive> models,
            CancellationToken token = default)
        {
            if (models.Count == 0)
            {
                return [];
            }

            var guids = models.Select(m => m.EntityAnalysisModelInstanceEntryGuid).ToArray();

            await dbContext.ExecuteAsync(
                "INSERT INTO \"ArchiveVersion\" (\"ArchiveId\", \"Json\", \"EntityAnalysisModelInstanceEntryGuid\", " +
                "\"EntryKeyValue\", \"ResponseElevation\", \"EntityAnalysisModelActivationRuleId\", " +
                "\"EntityAnalysisModelId\", \"ActivationRuleCount\", \"CreatedDate\", \"ReferenceDate\", \"Version\", " +
                "\"EntityAnalysisModelsReprocessingRuleInstanceId\") " +
                "SELECT \"Id\", \"Json\", \"EntityAnalysisModelInstanceEntryGuid\", \"EntryKeyValue\", " +
                "\"ResponseElevation\", \"EntityAnalysisModelActivationRuleId\", \"EntityAnalysisModelId\", " +
                "\"ActivationRuleCount\", \"CreatedDate\", \"ReferenceDate\", \"Version\", " +
                "\"EntityAnalysisModelsReprocessingRuleInstanceId\" FROM \"Archive\" " +
                "WHERE \"EntityAnalysisModelInstanceEntryGuid\" = ANY(@guids)",
                token, new DataParameter("guids", guids)).ConfigureAwait(false);

            return await dbContext.QueryToListAsync<Guid>(
                "UPDATE \"Archive\" a SET \"Json\" = v.json::jsonb, \"ResponseElevation\" = v.elevation, " +
                "\"EntityAnalysisModelActivationRuleId\" = v.rule, \"ActivationRuleCount\" = v.rules, " +
                "\"EntryKeyValue\" = v.entry, \"EntityAnalysisModelsReprocessingRuleInstanceId\" = v.instance, " +
                "\"Version\" = COALESCE(a.\"Version\", 0) + 1, \"CreatedDate\" = @now " +
                "FROM unnest(@guids, @jsons, @elevations, @rules, @counts, @entries, @instances) " +
                "AS v(guid, json, elevation, rule, rules, entry, instance) " +
                "WHERE a.\"EntityAnalysisModelInstanceEntryGuid\" = v.guid " +
                "RETURNING a.\"EntityAnalysisModelInstanceEntryGuid\"",
                token,
                new DataParameter("guids", guids),
                new DataParameter("jsons", models.Select(m => m.Json).ToArray()),
                new DataParameter("elevations", models.Select(m => m.ResponseElevation).ToArray()),
                new DataParameter("rules", models.Select(m => m.EntityAnalysisModelActivationRuleId).ToArray()),
                new DataParameter("counts", models.Select(m => m.ActivationRuleCount).ToArray()),
                new DataParameter("entries", models.Select(m => m.EntryKeyValue).ToArray()),
                new DataParameter("instances",
                    models.Select(m => m.EntityAnalysisModelsReprocessingRuleInstanceId).ToArray()),
                new DataParameter("now", DateTime.UtcNow, DataType.DateTime2)).ConfigureAwait(false);
        }

        public async Task<long> GetCountsByReferenceDateAsync(Guid entityAnalysisModelGuid, DateTime referenceDate,
            CancellationToken token = default)
        {
            return await dbContext.Archive
                .CountAsync(w => w.EntityAnalysisModel.Guid == entityAnalysisModelGuid
                                 && w.ReferenceDate >= referenceDate, token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<string>> GetJsonByEntityAnalysisModelIdRandomLimitAsync(int entityAnalysisModelId,
            int limit, CancellationToken token = default)
        {
            return await dbContext.Archive
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.EntityAnalysisModelInstanceEntryGuid).Select(s => s.Json)
                .Take(limit).ToListAsync(token).ConfigureAwait(false);
        }

        public Task BulkCopyAsync(List<Archive> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }
    }
}