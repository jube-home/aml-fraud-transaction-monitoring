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
using System.Text.Json;
using Jube.App.Code.Waf.Models;

namespace Jube.App.Code.Waf
{
    public static class JsonBodyFlattener
    {
        private const int DefaultMaxDepth = 32;
        private const int DefaultMaxValues = 2000;

        public static IReadOnlyList<WafFieldValue> Flatten(string json, int maxDepth = DefaultMaxDepth,
            int maxValues = DefaultMaxValues)
        {
            var values = new List<WafFieldValue>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return values;
            }

            try
            {
                using var document = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = maxDepth
                });

                Walk(document.RootElement, "$", values, maxDepth, maxValues, 0);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                values.Add(new WafFieldValue("$", json.Length > 4096 ? json[..4096] : json, WafTargetScope.Body));
            }

            return values;
        }

        private static void Walk(JsonElement element, string path, List<WafFieldValue> values, int maxDepth,
            int maxValues, int depth)
        {
            if (values.Count >= maxValues || depth > maxDepth)
            {
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (values.Count >= maxValues)
                        {
                            return;
                        }

                        values.Add(new WafFieldValue(path + "." + property.Name, property.Name, WafTargetScope.Body));
                        Walk(property.Value, path + "." + property.Name, values, maxDepth, maxValues, depth + 1);
                    }

                    break;
                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        if (values.Count >= maxValues)
                        {
                            return;
                        }

                        Walk(item, path + "[" + index + "]", values, maxDepth, maxValues, depth + 1);
                        index++;
                    }

                    break;
                case JsonValueKind.String:
                    values.Add(new WafFieldValue(path, element.GetString(), WafTargetScope.Body));
                    break;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    values.Add(new WafFieldValue(path, element.GetRawText(), WafTargetScope.Body));
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    break;
            }
        }
    }
}