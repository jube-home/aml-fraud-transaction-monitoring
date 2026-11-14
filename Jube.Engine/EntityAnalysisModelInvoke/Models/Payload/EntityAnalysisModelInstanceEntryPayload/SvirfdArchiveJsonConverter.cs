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
using Jube.Svirfd;
using Newtonsoft.Json;
using Svirfd;

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
{
    public sealed class SvirfdArchiveJsonConverter : JsonConverter<IReadOnlyDictionary<string, IpResponse>>
    {
        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, IReadOnlyDictionary<string, IpResponse> value,
            JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();

                return;
            }

            writer.WriteStartObject();

            foreach (var (name, record) in value)
            {
                if (record == null)
                {
                    continue;
                }

                writer.WritePropertyName(name);
                writer.WriteRawValue(SvirfdJson.Serialize(record));
            }

            writer.WriteEndObject();
        }

        public override IReadOnlyDictionary<string, IpResponse> ReadJson(JsonReader reader, Type objectType,
            IReadOnlyDictionary<string, IpResponse> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
