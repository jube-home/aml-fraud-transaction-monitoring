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

using System.Collections;
using Jube.Service.CaseDispatch.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.CaseDispatch
{
    public static class CaseRowsContext
    {
        public static IReadOnlyDictionary<string, object?> Roots(CaseRows rows)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["rows"] = rows.Rows.Cast<object?>().ToList(),
                ["columns"] = rows.Columns.Select((c, i) => (object?)new Dictionary<string, object?>
                {
                    ["name"] = c.Name, ["ordinal"] = i + 1, ["type"] = c.DataType
                }).ToList(),
                ["rowCount"] = rows.Rows.Count,
                ["truncated"] = rows.Truncated
            };
        }

        public static string ToJson(CaseRows rows)
        {
            return JsonConvert.SerializeObject(rows.Rows.Select(r => r.ToDictionary(p => p.Key, p => p.Value)));
        }

        public static CaseRows FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CaseRows.None;
            }

            var array = JArray.Parse(json);
            var rows = new List<IReadOnlyDictionary<string, object?>>();
            var names = new List<string>();

            foreach (var item in array.OfType<JObject>())
            {
                var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in item.Properties())
                {
                    map[property.Name] = Convert(property.Value);

                    if (!names.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        names.Add(property.Name);
                    }
                }

                rows.Add(map);
            }

            return new CaseRows([.. names.Select(n => (n, string.Empty))], rows, false, 0);
        }

        private static object? Convert(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => (decimal)token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Null or JTokenType.Undefined => null,
                JTokenType.Array => token.Select(Convert).ToList(),
                JTokenType.Object => token.Children<JProperty>().ToDictionary(p => p.Name, p => Convert(p.Value),
                    StringComparer.OrdinalIgnoreCase),
                _ => token.ToString()
            };
        }

        public static IReadOnlyList<IReadOnlyDictionary<string, object?>> AsList(IEnumerable rows)
        {
            return rows.Cast<IReadOnlyDictionary<string, object?>>().ToList();
        }
    }
}
