/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Text.Json;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.DataCache.Models;
using Jube.Data.Repository;
using Jube.Service.AskJoobyDataCache.Interfaces;

namespace Jube.Service.AskJoobyDataCache
{
    public sealed class AskJoobyDataCacheClient(
        AskJoobyDataCacheRepository repository,
        int? conversationId,
        int maxRows = 20000,
        TimeSpan? defaultTtl = null,
        IAdvancedAnalyticsCachedTableReader? advancedAnalyticsCache = null) : IToolResultCache
    {
        private readonly TimeSpan defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(60);

        public async Task<string> StoreAsync(CachedTable table, string? label = null, TimeSpan? ttl = null,
            CancellationToken token = default)
        {
            if (table.Rows.Count > maxRows)
            {
                throw new ArgumentException(
                    $"Cannot cache {table.Rows.Count} rows -- the limit is {maxRows}. Aggregate, page, or " +
                    "filter the result down before caching it.");
            }

            var pointerKey = $"tbl_{Guid.NewGuid():N}";
            var columnsJson = JsonSerializer.Serialize(table.Columns);
            var dataJson = JsonSerializer.Serialize(table.Rows);
            var expiresAt = DateTime.UtcNow.Add(ttl ?? defaultTtl);

            await repository.InsertAsync(pointerKey, columnsJson, dataJson, table.Rows.Count, label, conversationId,
                expiresAt, token).ConfigureAwait(false);

            return pointerKey;
        }

        public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
        {
            if (advancedAnalyticsCache is not null && Guid.TryParse(pointerKey, out var pointer))
            {
                return advancedAnalyticsCache.GetAsync(pointer, token);
            }

            return GetFromOwnStoreAsync(pointerKey, token);
        }

        private async Task<CachedTable?> GetFromOwnStoreAsync(string pointerKey, CancellationToken token)
        {
            var row = await repository.GetByPointerKeyAsync(pointerKey, token).ConfigureAwait(false);

            if (row is null)
            {
                return null;
            }

            var columns = JsonSerializer.Deserialize<List<CachedColumn>>(row.ColumnsJson) ?? [];
            var rawRows = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(row.DataJson) ?? [];
            var rows = rawRows.Select(NormalizeRow).ToList();

            return new CachedTable(columns, rows);
        }

        private static IReadOnlyDictionary<string, object?> NormalizeRow(Dictionary<string, object?> row)
        {
            var normalized = new Dictionary<string, object?>(row.Count);

            foreach (var (key, value) in row)
            {
                normalized[key] = value is JsonElement element ? NormalizeElement(element) : value;
            }

            return normalized;
        }

        private static object? NormalizeElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }
    }
}