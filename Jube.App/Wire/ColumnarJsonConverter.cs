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
using System.Text.Json.Serialization;

namespace Jube.App.Wire
{
    public sealed class ColumnarJsonConverter<TValue> : JsonConverter<List<Dictionary<string, TValue>>>
    {
        public override List<Dictionary<string, TValue>> Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new NotSupportedException("The columnar JSON form is written by the server and never read.");
        }

        public override void Write(Utf8JsonWriter writer, List<Dictionary<string, TValue>> value,
            JsonSerializerOptions options)
        {
            var keys = new List<string>();
            var seen = new HashSet<string>();
            List<int> nullRows = null;

            for (var row = 0; row < value.Count; row++)
            {
                if (value[row] is null)
                {
                    (nullRows ??= []).Add(row);

                    continue;
                }

                foreach (var key in value[row].Keys)
                {
                    if (seen.Add(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            Dictionary<int, List<int>> missing = null;

            writer.WriteStartObject();
            writer.WriteNumber("count", value.Count);

            writer.WriteStartArray("keys");

            foreach (var key in keys)
            {
                writer.WriteStringValue(key);
            }

            writer.WriteEndArray();

            writer.WriteStartArray("columns");

            for (var column = 0; column < keys.Count; column++)
            {
                writer.WriteStartArray();

                for (var row = 0; row < value.Count; row++)
                {
                    if (value[row] is not null && value[row].TryGetValue(keys[column], out var cell))
                    {
                        JsonSerializer.Serialize(writer, cell, options);

                        continue;
                    }

                    writer.WriteNullValue();

                    if (value[row] is not null)
                    {
                        ((missing ??= new Dictionary<int, List<int>>()).TryGetValue(column, out var rows)
                            ? rows
                            : missing[column] = []).Add(row);
                    }
                }

                writer.WriteEndArray();
            }

            writer.WriteEndArray();

            if (missing is not null)
            {
                writer.WriteStartArray("missing");

                foreach (var (column, rows) in missing)
                {
                    writer.WriteStartArray();
                    writer.WriteNumberValue(column);
                    writer.WriteStartArray();

                    foreach (var row in rows)
                    {
                        writer.WriteNumberValue(row);
                    }

                    writer.WriteEndArray();
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
            }

            if (nullRows is not null)
            {
                writer.WriteStartArray("nulls");

                foreach (var row in nullRows)
                {
                    writer.WriteNumberValue(row);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
