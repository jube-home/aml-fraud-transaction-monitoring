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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Analytics.Aggregation
{
    public static class AggregationParameters
    {
        public static bool TryParse(string? json, out IReadOnlyDictionary<string, double> parameters,
            out string? error)
        {
            var parsed = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            parameters = parsed;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            JToken token;

            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonException)
            {
                error = "The function parameters are not valid JSON.";

                return false;
            }

            if (token.Type == JTokenType.Null)
            {
                return true;
            }

            if (token is not JObject values)
            {
                error = "The function parameters must be a JSON object of parameter names and values.";

                return false;
            }

            foreach (var property in values.Properties())
            {
                double? value = property.Value.Type switch
                {
                    JTokenType.Integer or JTokenType.Float => property.Value.Value<double>(),
                    JTokenType.Boolean => property.Value.Value<bool>() ? 1 : 0,
                    _ => null
                };

                if (value == null)
                {
                    error = $"The function parameter {property.Name} must be a number or true or false.";

                    return false;
                }

                parsed[property.Name] = value.Value;
            }

            return true;
        }

        public static IReadOnlyList<string> Validate(int aggregationId, string? json)
        {
            if (!TryParse(json, out var parameters, out var error))
            {
                return [error!];
            }

            if (AggregationCatalogue.TryGet(aggregationId, out var descriptor))
            {
                return descriptor.Validate(parameters);
            }

            return parameters.Count == 0 ? [] : ["This function takes no parameters."];
        }

        public static IReadOnlyList<string> Validate(int aggregationId,
            IReadOnlyDictionary<string, double>? parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return [];
            }

            return AggregationCatalogue.TryGet(aggregationId, out var descriptor)
                ? descriptor.Validate(parameters)
                : ["This function takes no parameters."];
        }

        public static IReadOnlyDictionary<string, double>? Read(string? json)
        {
            return TryParse(json, out var parameters, out _) && parameters.Count > 0 ? parameters : null;
        }

        public static string? Write(IReadOnlyDictionary<string, double>? parameters)
        {
            return parameters == null || parameters.Count == 0
                ? null
                : JsonConvert.SerializeObject(parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        public static Func<double[], double>? Bind(int aggregationId, string? json)
        {
            if (!AggregationCatalogue.TryGet(aggregationId, out var descriptor))
            {
                return null;
            }

            return TryParse(json, out var parameters, out var error)
                ? descriptor.Bind(parameters)
                : throw new ArgumentException(error, nameof(json));
        }
    }
}
