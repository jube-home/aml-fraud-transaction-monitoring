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
using System.Globalization;
using Newtonsoft.Json;

namespace Jube.App.Code
{
    public class FiniteDoubleNewtonsoftConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double) || objectType == typeof(double?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                if (objectType == typeof(double?))
                {
                    return null;
                }

                throw new JsonSerializationException("The value must be a finite number.");
            }

            var value = reader.TokenType switch
            {
                JsonToken.Integer or JsonToken.Float => Convert.ToDouble(reader.Value,
                    CultureInfo.InvariantCulture),
                JsonToken.String when double.TryParse((string)reader.Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => throw new JsonSerializationException("The value could not be read as a number.")
            };

            if (!double.IsFinite(value))
            {
                throw new JsonSerializationException("The value must be a finite number.");
            }

            return value;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue((double)value);
            }
        }
    }
}
