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

using Jube.Dto.Hold;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Jube.Data.Repository.Models.Hold
{
    public static class ResponseHoldJson
    {
        public const string Key = "hold";

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });

        public static ResponseHold Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return Read(JObject.Parse(json));
        }

        public static ResponseHold Read(JObject json)
        {
            return json[Key] is JObject hold ? hold.ToObject<ResponseHold>(Serializer) : null;
        }

        public static string Write(string json, ResponseHold hold)
        {
            var jObject = JObject.Parse(json);
            jObject[Key] = JObject.FromObject(hold, Serializer);

            return jObject.ToString(Formatting.None);
        }

        public static string Preserve(string existingJson, string json)
        {
            var existing = ReadToken(existingJson);

            if (existing == null)
            {
                return json;
            }

            var jObject = JObject.Parse(json);
            jObject[Key] = existing;

            return jObject.ToString(Formatting.None);
        }

        private static JToken ReadToken(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JObject.Parse(json)[Key]?.DeepClone();
        }
    }
}
