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
using System.Text;
using System.Text.Json;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public sealed record InvokeResponse(
        int Status,
        byte[] BodyBytes,
        string? ContentType,
        // ReSharper disable once NotAccessedPositionalProperty.Global
        long? ContentLength,
        // ReSharper disable once NotAccessedPositionalProperty.Global
        IReadOnlyDictionary<string, string> Headers)
    {
        private JsonDocument? json;

        public string Body => Encoding.UTF8.GetString(BodyBytes);

        public JsonElement Json => (json ??= JsonDocument.Parse(BodyBytes)).RootElement;

        public JsonElement? Find(params string[] path)
        {
            var current = Json;
            foreach (var name in path)
            {
                if (current.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var found = current.EnumerateObject()
                    .Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    .Select(p => (JsonElement?)p.Value).FirstOrDefault();
                if (found == null)
                {
                    return null;
                }

                current = found.Value;
            }

            return current;
        }

        public string? String(params string[] path)
        {
            var element = Find(path);
            return element is { ValueKind: JsonValueKind.String } ? element.Value.GetString() : element?.ToString();
        }

        private double? Number(params string[] path)
        {
            var element = Find(path);
            return element is { ValueKind: JsonValueKind.Number } ? element.Value.GetDouble() : null;
        }

        public IReadOnlyList<string> PropertyNames =>
            Json.ValueKind == JsonValueKind.Object ? Json.EnumerateObject().Select(p => p.Name).ToList() : [];

        public IReadOnlyList<string> ActivatedRules =>
            Find("Activation") is { ValueKind: JsonValueKind.Object } activation
                ? activation.EnumerateObject().Select(p => p.Name).ToList()
                : [];

        public double ResponseElevationValue => Number("ResponseElevation", "Value") ?? 0;

        public string? PayloadField(string name)
        {
            return String("Payload", name);
        }
    }
}