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
using Jube.Dictionary;
using Jube.GraphRecallProtocol;
using Newtonsoft.Json;

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
{
    public sealed class GraphArchiveJsonConverter : JsonConverter<PooledDictionary<string, GraphEntity>>
    {
        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, PooledDictionary<string, GraphEntity> value,
            JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();

                return;
            }

            writer.WriteStartObject();

            foreach (var (name, entity) in value)
            {
                if (entity is not { ReportTable: true })
                {
                    continue;
                }

                writer.WritePropertyName(name);
                serializer.Serialize(writer, entity);
            }

            writer.WriteEndObject();
        }

        public override PooledDictionary<string, GraphEntity> ReadJson(JsonReader reader, Type objectType,
            PooledDictionary<string, GraphEntity> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
