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

using System.Globalization;
using System.Text.Json.Nodes;

namespace Jube.RegulatoryReport
{
    internal static class JsonPath
    {
        public static JsonNode? Resolve(JsonNode root, JsonNode current, string expression)
        {
            var path = expression.Trim();

            if (path is "" or ".")
            {
                return current;
            }

            JsonNode? node;

            if (path == "$")
            {
                return root;
            }

            if (path.StartsWith("$.", StringComparison.Ordinal))
            {
                node = root;
                path = path[2..];
            }
            else
            {
                node = current;
            }

            foreach (var segment in Segments(path))
            {
                if (node is null)
                {
                    return null;
                }

                node = segment.Index is { } index
                    ? node is JsonArray array && index >= 0 && index < array.Count ? array[index] : null
                    : node is JsonObject obj && obj.TryGetPropertyValue(segment.Name!, out var child) ? child : null;
            }

            return node;
        }

        public static string? Scalar(JsonNode? node)
        {
            if (node is not JsonValue value)
            {
                return null;
            }

            if (value.TryGetValue<string>(out var text))
            {
                return text;
            }

            if (value.TryGetValue<bool>(out var flag))
            {
                return flag ? "true" : "false";
            }

            return Convert.ToString(value.ToJsonString().Trim('"'), CultureInfo.InvariantCulture);
        }

        public static bool Truthy(JsonNode? node)
        {
            return node switch
            {
                null => false,
                JsonArray array => array.Count > 0,
                JsonObject obj => obj.Count > 0,
                JsonValue value when value.TryGetValue<bool>(out var flag) => flag,
                JsonValue value when value.TryGetValue<string>(out var text) => !string.IsNullOrWhiteSpace(text),
                _ => true
            };
        }

        private static IEnumerable<(string? Name, int? Index)> Segments(string path)
        {
            foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var bracket = part.IndexOf('[');

                if (bracket < 0)
                {
                    yield return (part, null);

                    continue;
                }

                if (bracket > 0)
                {
                    yield return (part[..bracket], null);
                }

                var rest = part[bracket..];

                while (rest.StartsWith('['))
                {
                    var close = rest.IndexOf(']');

                    if (close < 0 || !int.TryParse(rest[1..close], NumberStyles.None, CultureInfo.InvariantCulture,
                            out var index))
                    {
                        yield return (rest, null);

                        yield break;
                    }

                    yield return (null, index);
                    rest = rest[(close + 1)..];
                }
            }
        }
    }
}
