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

using System.ComponentModel;
using System.Text.Json;
using Jube.AskJooby.DataCache;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;

namespace Jube.AskJooby.Tools.Cache
{
    public sealed class CacheAssembledTableTool(IToolResultCache cache)
    {
        private const int PreviewRowCount = 3;

        [Description(
            "Caches rows YOU already know -- gathered from your own tool calls, or assembled from " +
            "several specialists' hand-back answers -- as one table, and returns a data_pointer in " +
            "the same shape RunSqlQuery does. Use this whenever you have combined several separate " +
            "facts (one count per specialist, one row per sub-agent) and the caller might want to " +
            "chart or export them: RenderChart and ExportCachedTableToCsv only ever read from a " +
            "cached table, never from values sitting in your own reasoning, and repeating the " +
            "numbers in prose is a dead end for both. This costs one extra call now and means you " +
            "never have to re-derive the data if asked to visualise or export it a moment later. " +
            "Afterwards use InspectDataSchema/PreviewDataRows/QueryCachedData with the data_pointer " +
            "this returns to look at what you cached.")]
        public async Task<string> CacheAssembledTable(
            [Description(
                "JSON array of objects, one per row, all sharing the same keys -- e.g. " +
                "[{\"RuleType\":\"Activation Rule\",\"Count\":9}, {\"RuleType\":\"Gateway Rule\"," +
                "\"Count\":1}]. Every object must carry the same set of keys.")]
            string rowsJson,
            [Description("A short label for what this table represents.")]
            string? label = null)
        {
            var rows = ParseRows(rowsJson);
            var columns = BuildColumns(rows);
            var pointer = await cache.StoreAsync(new CachedTable(columns, rows), label);

            var preview = rows.Take(PreviewRowCount)
                .Select(row => CachedRowText.Format(columns, row));

            return $"{rows.Count} row(s), {columns.Count} column(s). data_pointer: \"{pointer}\". " +
                   $"Columns: {string.Join(", ", columns.Select(c => c.Name))}. " +
                   $"First {Math.Min(PreviewRowCount, rows.Count)}: {string.Join(" | ", preview)}. " +
                   "Use InspectDataSchema/PreviewDataRows/QueryCachedData with this data_pointer to " +
                   "explore further, or RenderChart/ExportCachedTableToCsv to chart or export it.";
        }

        private static List<Dictionary<string, object?>> ParseRows(string rowsJson)
        {
            using var document = JsonDocument.Parse(rowsJson);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("rowsJson must be a JSON array of objects.");
            }

            var rows = new List<Dictionary<string, object?>>();
            string[]? firstRowKeys = null;

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException("Every element of rowsJson must be an object.");
                }

                var row = new Dictionary<string, object?>();

                foreach (var property in element.EnumerateObject())
                {
                    row[property.Name] = ToScalar(property.Value);
                }

                var sortedKeys = row.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
                firstRowKeys ??= sortedKeys;

                if (!sortedKeys.SequenceEqual(firstRowKeys))
                {
                    throw new ArgumentException(
                        "Every row in rowsJson must carry the same set of keys. The first row had " +
                        $"[{string.Join(", ", firstRowKeys)}], a later row had " +
                        $"[{string.Join(", ", sortedKeys)}].");
                }

                rows.Add(row);
            }

            if (rows.Count == 0)
            {
                throw new ArgumentException("rowsJson must contain at least one row.");
            }

            return rows;
        }

        private static object? ToScalar(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.TryGetInt64(out var integer) ? integer : value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value.GetRawText()
            };
        }

        private static List<CachedColumn> BuildColumns(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        {
            return rows[0].Keys.Select(key => new CachedColumn(key, InferType(rows, key))).ToList();
        }

        private static string InferType(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, string key)
        {
            foreach (var row in rows)
            {
                if (row.TryGetValue(key, out var value) && value is not null)
                {
                    return value.GetType().Name;
                }
            }

            return "unknown";
        }
    }
}