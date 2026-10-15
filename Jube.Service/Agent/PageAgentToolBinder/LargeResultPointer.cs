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

using System.Collections;
using System.Globalization;
using System.Text.Json;
using Jube.AskJooby.DataCache;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.DataCache.Models;

namespace Jube.Service.Agent.PageAgentToolBinder
{
    internal static class LargeResultPointer
    {
        private const int PreviewRowCount = 3;

        public static async Task<object?> ReduceAsync(object? result, string toolName, IToolResultCache? cache,
            int maxCharacters, JsonSerializerOptions? options, CancellationToken token)
        {
            if (result is null || cache is null || maxCharacters <= 0)
            {
                return result;
            }

            string serialised;

            try
            {
                serialised = JsonSerializer.Serialize(result, options);
            }
            catch (NotSupportedException)
            {
                return result;
            }

            if (serialised.Length <= maxCharacters)
            {
                return result;
            }

            var table = BuildTable(result, serialised);
            var pointer = await cache.StoreAsync(table, toolName, token: token).ConfigureAwait(false);

            var preview = table.Rows.Take(PreviewRowCount)
                .Select(row => CachedRowText.Format(table.Columns, row, Describe));

            return $"{table.Rows.Count} row(s), {table.Columns.Count} column(s), too large to return inline "
                   + $"({serialised.Length.ToString(CultureInfo.InvariantCulture)} characters). "
                   + $"data_pointer: \"{pointer}\". "
                   + $"Columns: {string.Join(", ", table.Columns.Select(column => column.Name))}. "
                   + $"First {Math.Min(PreviewRowCount, table.Rows.Count)}: {string.Join(" | ", preview)}. "
                   + "Use InspectDataSchema/PreviewDataRows/QueryCachedData with this data_pointer to read "
                   + "or filter it. Do not ask for the whole thing back -- filter it down to what you "
                   + "actually need to answer the question.";
        }

        private static CachedTable BuildTable(object result, string serialised)
        {
            if (result is JsonElement element)
            {
                return BuildTableFromJson(element, serialised);
            }

            if (result is IEnumerable enumerable and not string)
            {
                var items = enumerable.Cast<object?>().ToList();
                var rows = items.Select(ToRow).ToList();

                return new CachedTable(BuildColumns(rows), rows);
            }

            var single = ToRow(result);

            return new CachedTable(BuildColumns([single]), [single]);

            IReadOnlyDictionary<string, object?> ToRow(object? item)
            {
                if (item is null)
                {
                    return new Dictionary<string, object?> { ["Value"] = null };
                }

                var type = item.GetType();

                if (type.IsPrimitive || item is string or decimal or DateTime or DateTimeOffset or Guid)
                {
                    return new Dictionary<string, object?> { ["Value"] = Normalise(item) };
                }

                var properties = type.GetProperties()
                    .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                    .ToList();

                if (properties.Count == 0)
                {
                    return new Dictionary<string, object?> { ["Value"] = serialised };
                }

                var row = new Dictionary<string, object?>();

                foreach (var property in properties)
                {
                    try
                    {
                        row[property.Name] = Normalise(property.GetValue(item));
                    }
                    catch (Exception)
                    {
                        row[property.Name] = null;
                    }
                }

                return row;
            }
        }

        private static CachedTable BuildTableFromJson(JsonElement element, string serialised)
        {
            var array = element;

            if (element.ValueKind == JsonValueKind.Object)
            {
                var found = false;

                foreach (var property in element.EnumerateObject())
                {
                    if (!string.Equals(property.Name, "rows", StringComparison.OrdinalIgnoreCase) ||
                        property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    array = property.Value;
                    found = true;
                    break;
                }

                if (!found)
                {
                    var only = new Dictionary<string, object?> { ["Value"] = serialised };

                    return new CachedTable(BuildColumns([only]), [only]);
                }
            }

            if (array.ValueKind != JsonValueKind.Array)
            {
                var scalar = new Dictionary<string, object?> { ["Value"] = serialised };

                return new CachedTable(BuildColumns([scalar]), [scalar]);
            }

            var rows = new List<IReadOnlyDictionary<string, object?>>();

            foreach (var item in array.EnumerateArray())
            {
                var row = new Dictionary<string, object?>();

                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in item.EnumerateObject())
                    {
                        row[property.Name] = Normalise(property.Value);
                    }
                }
                else
                {
                    row["Value"] = Normalise(item);
                }

                rows.Add(row);
            }

            return new CachedTable(BuildColumns(rows), rows);
        }

        private static object? Normalise(object? value)
        {
            if (value is JsonElement json)
            {
                return json.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => json.GetString(),
                    JsonValueKind.Number => json.TryGetDouble(out var number) ? number : json.GetRawText(),
                    _ => json.GetRawText()
                };
            }

            switch (value)
            {
                case null:
                    return null;
                case string or bool:
                    return value;
                case DateTime or DateTimeOffset or Guid:
                    return value.ToString();
                case decimal decimalValue:
                    return (double)decimalValue;
            }

            var type = value.GetType();

            if (type.IsPrimitive)
            {
                return value is char ? value.ToString() : Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            if (type.IsEnum)
            {
                return value.ToString();
            }

            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch (NotSupportedException)
            {
                return value.ToString();
            }
        }

        private static List<CachedColumn> BuildColumns(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        {
            if (rows.Count == 0)
            {
                return [];
            }

            var keys = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                foreach (var key in row.Keys)
                {
                    if (seen.Add(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            return keys
                .Select(key => new CachedColumn(key, InferType(rows, key)))
                .ToList();
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

        private static string Describe(object? value)
        {
            var text = value?.ToString() ?? "null";

            return text.Length <= 80 ? text : text[..80] + "...";
        }
    }
}