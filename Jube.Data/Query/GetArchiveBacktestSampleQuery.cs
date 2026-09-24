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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Models;

    public class GetArchiveBacktestSampleQuery(DbContext dbContext, int tenantRegistryId)
    {
        private const int TagChunk = 1000;

        public Task<List<ArchiveBacktestSampleRow>> ExecuteAsync(int entityAnalysisModelId, DateTime? from,
            DateTime? to,
            int limit, CancellationToken token = default)
        {
            return ExecutePageAsync(entityAnalysisModelId, from, to, limit, null, token);
        }

        public async Task<List<ArchiveBacktestSampleRow>> ExecutePageAsync(int entityAnalysisModelId, DateTime? from,
            DateTime? to,
            int pageSize, ArchiveBacktestSampleRow after, CancellationToken token = default)
        {
            var afterDate = after?.ReferenceDate;
            var afterId = after?.Id ?? long.MaxValue;
            var rows = await dbContext.Archive
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.Json != null
                            && w.ReferenceDate != null
                            && (!from.HasValue || w.ReferenceDate >= from.Value)
                            && (!to.HasValue || w.ReferenceDate <= to.Value)
                            && (afterDate == null || (w.ReferenceDate <= afterDate &&
                                                      !(w.ReferenceDate == afterDate && w.Id >= afterId))))
                .OrderByDescending(o => o.ReferenceDate)
                .ThenByDescending(o => o.Id)
                .Take(pageSize)
                .Select(s => new ArchiveBacktestSampleRow
                {
                    Id = s.Id,
                    EntityAnalysisModelInstanceEntryGuid = s.EntityAnalysisModelInstanceEntryGuid,
                    EntryKeyValue = s.EntryKeyValue,
                    ReferenceDate = s.ReferenceDate,
                    Json = s.Json
                })
                .ToListAsync(token)
                .ConfigureAwait(false);

            var tags = new Dictionary<Guid, List<string>>();
            foreach (var chunk in rows.Select(r => r.EntityAnalysisModelInstanceEntryGuid).Chunk(TagChunk))
            {
                var guids = chunk.ToList();
                foreach (var tag in await dbContext.ArchiveTag
                             .Where(w => guids.Contains(w.EntityAnalysisModelInstanceEntryGuid)
                                         && (w.Deleted == null || w.Deleted == 0)
                                         && w.Name != null)
                             .Select(s => new { s.EntityAnalysisModelInstanceEntryGuid, s.Name })
                             .ToListAsync(token)
                             .ConfigureAwait(false))
                {
                    if (!tags.TryGetValue(tag.EntityAnalysisModelInstanceEntryGuid, out var names))
                    {
                        names = [];
                        tags.Add(tag.EntityAnalysisModelInstanceEntryGuid, names);
                    }

                    names.Add(tag.Name);
                }
            }

            foreach (var row in rows)
            {
                row.Tags = tags.TryGetValue(row.EntityAnalysisModelInstanceEntryGuid, out var names) ? names : [];
            }

            return rows;
        }

        public async Task<ArchiveBacktestSampleRow> ExecuteEntryAsync(int entityAnalysisModelId,
            Guid entityAnalysisModelInstanceEntryGuid,
            CancellationToken token = default)
        {
            var row = await dbContext.Archive
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelInstanceEntryGuid == entityAnalysisModelInstanceEntryGuid
                            && w.Json != null)
                .Select(s => new ArchiveBacktestSampleRow
                {
                    Id = s.Id,
                    EntityAnalysisModelInstanceEntryGuid = s.EntityAnalysisModelInstanceEntryGuid,
                    EntryKeyValue = s.EntryKeyValue,
                    ReferenceDate = s.ReferenceDate,
                    Json = s.Json
                })
                .FirstOrDefaultAsync(token)
                .ConfigureAwait(false);
            if (row == null)
            {
                return null;
            }

            row.Tags = await dbContext.ArchiveTag
                .Where(w => w.EntityAnalysisModelInstanceEntryGuid == entityAnalysisModelInstanceEntryGuid
                            && (w.Deleted == null || w.Deleted == 0) && w.Name != null)
                .Select(s => s.Name)
                .ToListAsync(token)
                .ConfigureAwait(false);
            return row;
        }
    }
}