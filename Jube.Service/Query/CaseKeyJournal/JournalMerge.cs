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

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalMerge
    {
        public const int MaximumLanes = 3;
        public const int MaximumKeyLength = 256;
        public const int MaximumValueLength = 1024;

        public static List<(string Key, string Value)>? Parse(string? merge)
        {
            if (string.IsNullOrWhiteSpace(merge))
            {
                return [];
            }

            try
            {
                if (JToken.Parse(merge) is not JArray lanes)
                {
                    return null;
                }

                var parsed = new List<(string Key, string Value)>();

                foreach (var lane in lanes)
                {
                    if (lane is not JObject item || item["key"] is not { Type: JTokenType.String } key
                                                 || item["value"] is not { Type: JTokenType.String } value)
                    {
                        return null;
                    }

                    parsed.Add((key.Value<string>()!, value.Value<string>()!));
                }

                return parsed;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static bool IsValid(List<(string Key, string Value)>? lanes)
        {
            return lanes is not null && lanes.Count <= MaximumLanes
                                     && lanes.All(lane => !string.IsNullOrWhiteSpace(lane.Key)
                                                          && lane.Key.Length <= MaximumKeyLength
                                                          && !lane.Key.Any(char.IsControl)
                                                          && !string.IsNullOrEmpty(lane.Value)
                                                          && lane.Value.Length <= MaximumValueLength
                                                          && !lane.Value.Contains('\0'));
        }
    }
}
