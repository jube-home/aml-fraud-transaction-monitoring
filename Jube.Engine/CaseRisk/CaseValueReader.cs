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

namespace Jube.Engine.CaseRisk
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public static class CaseValueReader
    {
        public static double? Read(string caseJson, string valueField)
        {
            if (string.IsNullOrWhiteSpace(caseJson) || string.IsNullOrWhiteSpace(valueField))
            {
                return null;
            }

            try
            {
                var root = JObject.Parse(caseJson);

                var payload = root.Properties()
                    .FirstOrDefault(p => p.Name.Equals("Payload", StringComparison.OrdinalIgnoreCase))?.Value as JObject;

                var token = payload?.Properties()
                    .FirstOrDefault(p => p.Name.Equals(valueField, StringComparison.OrdinalIgnoreCase))?.Value;

                return token?.Type switch
                {
                    JTokenType.Integer or JTokenType.Float => token.Value<double>(),
                    JTokenType.String when double.TryParse(token.Value<string>(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var parsed) => parsed,
                    _ => null
                };
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonException or InvalidCastException)
            {
                return null;
            }
        }
    }
}
