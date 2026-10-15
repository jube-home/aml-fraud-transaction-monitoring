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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jube.App.Code
{
    public class FiniteDoubleJsonConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadFinite(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal static double ReadFinite(ref Utf8JsonReader reader)
        {
            double value;

            if (reader.TokenType == JsonTokenType.String)
            {
                if (!double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture,
                        out value))
                {
                    throw new JsonException("The value could not be read as a number.");
                }
            }
            else
            {
                value = reader.GetDouble();
            }

            if (!double.IsFinite(value))
            {
                throw new JsonException("The value must be a finite number.");
            }

            return value;
        }
    }
}
