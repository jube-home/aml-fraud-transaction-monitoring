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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Newtonsoft.Json.Linq;

namespace Jube.Test.Infrastructure
{
    internal static class ArchiveKeySeed
    {
        public const string CreatedUser = DatabaseFixture.DatabaseFixture.Prefix + "ArchiveKeySeed";

        public static Task KeysAsync(DbContext dbContext, int modelId, Guid entryGuid, string archiveJson,
            bool registerFields = true)
        {
            var payload = JObject.Parse(archiveJson)["payload"] as JObject ?? new JObject();

            return KeysAsync(dbContext, modelId, entryGuid, payload, registerFields);
        }

        public static async Task KeysAsync(DbContext dbContext, int modelId, Guid entryGuid, JObject payload,
            bool registerFields = true)
        {
            var keys = new List<ArchiveKey>();
            var registered = (await dbContext.EntityAnalysisModelRequestXpath
                    .Where(x => x.EntityAnalysisModelId == modelId && x.Name != null)
                    .OrderBy(x => x.Id).Select(x => new { x.Name, x.DataTypeId, x.ReportTable })
                    .ToListAsync().ConfigureAwait(false))
                .GroupBy(x => x.Name!).ToDictionary(g => g.Key, g => g.First());

            foreach (var property in payload.Properties())
            {
                if (property.Value.Type is JTokenType.Object or JTokenType.Array or JTokenType.Null)
                {
                    continue;
                }

                var inferred = property.Value.Type switch
                {
                    JTokenType.Integer => property.Value.Value<long>() is >= int.MinValue and <= int.MaxValue ? 2 : 3,
                    JTokenType.Float => 3,
                    JTokenType.Date => 4,
                    JTokenType.Boolean => 5,
                    _ => 1
                };

                if (!registered.TryGetValue(property.Name, out var field))
                {
                    if (!registerFields)
                    {
                        continue;
                    }

                    await FieldAsync(dbContext, modelId, property.Name, inferred).ConfigureAwait(false);
                    field = new { property.Name, DataTypeId = (int?)inferred, ReportTable = (byte?)1 };
                }

                if (field.ReportTable != 1)
                {
                    continue;
                }

                var key = new ArchiveKey
                {
                    ProcessingTypeId = 1, Key = property.Name, EntityAnalysisModelInstanceEntryGuid = entryGuid,
                    Version = 1
                };

                var text = property.Value.Type == JTokenType.Date
                    ? property.Value.Value<DateTime>().ToUniversalTime().ToString("O")
                    : property.Value.ToString();

                switch (field.DataTypeId ?? 1)
                {
                    case 2:
                        if (!int.TryParse(text, System.Globalization.NumberStyles.Integer,
                                System.Globalization.CultureInfo.InvariantCulture, out var integer))
                        {
                            continue;
                        }

                        key.KeyValueInteger = integer;
                        break;
                    case 3:
                    case 6:
                    case 7:
                        if (!double.TryParse(text, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var number))
                        {
                            continue;
                        }

                        key.KeyValueFloat = number;
                        break;
                    case 4:
                        if (!DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.AssumeUniversal, out var date))
                        {
                            continue;
                        }

                        key.KeyValueDate = date.UtcDateTime;
                        break;
                    case 5:
                        key.KeyValueBoolean = (byte)(text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1" ? 1 : 0);
                        break;
                    default:
                        key.KeyValueString = text;
                        break;
                }

                keys.Add(key);
            }

            if (keys.Count > 0)
            {
                await dbContext.BulkCopyAsync(keys).ConfigureAwait(false);
            }
        }

        public static async Task FieldAsync(DbContext dbContext, int modelId, string name, int dataTypeId = 1,
            bool archiveKey = true)
        {
            if (await dbContext.EntityAnalysisModelRequestXpath
                    .AnyAsync(x => x.EntityAnalysisModelId == modelId && x.Name == name).ConfigureAwait(false))
            {
                return;
            }

            await dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
            {
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = modelId,
                Name = name,
                XPath = $"$.{name}",
                DataTypeId = dataTypeId,
                ReportTable = (byte)(archiveKey ? 1 : 0),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = CreatedUser,
                CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);
        }

        public static string NameOf(string? xPath)
        {
            var parts = (xPath ?? string.Empty).Split(['.', '/', '[', ']', '\'', '"', '$'],
                StringSplitOptions.RemoveEmptyEntries);

            return parts.Length == 0 ? "Field" : parts[^1];
        }

        public static async Task<Guid> SourceAsync(DbContext dbContext, int modelId, string name, int dataTypeId = 1,
            bool archiveKey = true)
        {
            await FieldAsync(dbContext, modelId, name, dataTypeId, archiveKey).ConfigureAwait(false);

            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(x => x.EntityAnalysisModelId == modelId && x.Name == name).OrderBy(x => x.Id)
                .Select(x => x.Guid).FirstAsync().ConfigureAwait(false);
        }

        public static async Task<Guid> SourceByWorkflowAsync(DbContext dbContext, int? workflowId, string name,
            int dataTypeId = 1, bool archiveKey = true)
        {
            var modelId = await dbContext.CaseWorkflow.Where(w => w.Id == workflowId)
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync().ConfigureAwait(false);

            return modelId is null
                ? Guid.NewGuid()
                : await SourceAsync(dbContext, modelId.Value, name, dataTypeId, archiveKey).ConfigureAwait(false);
        }

        public static async Task KeysAsync(DbContext dbContext, int modelId,
            IEnumerable<(Guid EntryGuid, JObject Payload)> rows)
        {
            var first = true;

            foreach (var (entryGuid, payload) in rows)
            {
                await KeysAsync(dbContext, modelId, entryGuid, payload, first).ConfigureAwait(false);
                first = false;
            }
        }

        public static async Task CleanupAsync(DbContext dbContext, IEnumerable<int>? modelIds = null,
            IEnumerable<long>? archiveIds = null, IEnumerable<Guid>? archiveGuids = null)
        {
            var guids = new HashSet<Guid>(archiveGuids ?? []);

            if (archiveIds is not null)
            {
                var ids = archiveIds.ToList();

                if (ids.Count > 0)
                {
                    guids.UnionWith(await dbContext.Archive.Where(a => ids.Contains(a.Id))
                        .Select(a => a.EntityAnalysisModelInstanceEntryGuid).ToListAsync().ConfigureAwait(false));
                }
            }

            if (guids.Count > 0)
            {
                var list = guids.ToList();
                await dbContext.ArchiveKeyVersion.Where(k => list.Contains(k.EntityAnalysisModelInstanceEntryGuid))
                    .DeleteAsync().ConfigureAwait(false);
                await dbContext.ArchiveKey.Where(k => list.Contains(k.EntityAnalysisModelInstanceEntryGuid))
                    .DeleteAsync().ConfigureAwait(false);
            }

            if (modelIds is not null)
            {
                var models = modelIds.ToList();

                if (models.Count > 0)
                {
                    await dbContext.EntityAnalysisModelRequestXpath
                        .Where(x => x.EntityAnalysisModelId != null && models.Contains(x.EntityAnalysisModelId.Value)
                                                                    && x.CreatedUser == CreatedUser)
                        .DeleteAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
