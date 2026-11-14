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

using System.Runtime.Serialization;
using Jube.Dto.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalMerge
    {
        public const int MaximumLanes = 3;
        public const int MaximumKeyLength = 256;
        public const int MaximumValueLength = 1024;

        private static readonly Dictionary<string, ArchiveKeyLookupOperator> Operators = typeof(ArchiveKeyLookupOperator)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .ToDictionary(
                f => f.GetCustomAttributes(typeof(EnumMemberAttribute), false).Cast<EnumMemberAttribute>().First()
                    .Value!,
                f => (ArchiveKeyLookupOperator)f.GetValue(null)!, StringComparer.Ordinal);

        public static List<JournalLane>? Parse(string? merge)
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

                var parsed = new List<JournalLane>();

                foreach (var lane in lanes)
                {
                    if (lane is not JObject item || item["key"] is not { Type: JTokenType.String } key
                                                 || item["value"] is not { Type: JTokenType.String } value)
                    {
                        return null;
                    }

                    var op = ArchiveKeyLookupOperator.Equal;

                    if (item["op"] is { Type: JTokenType.String } opToken
                        && !Operators.TryGetValue(opToken.Value<string>()!, out op))
                    {
                        return null;
                    }

                    string? upper = null;

                    if (item["to"] is { } toToken && toToken.Type != JTokenType.Null)
                    {
                        if (toToken.Type != JTokenType.String)
                        {
                            return null;
                        }

                        upper = toToken.Value<string>();
                    }

                    parsed.Add(new JournalLane(key.Value<string>()!, value.Value<string>()!, op, upper));
                }

                return parsed;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialise(IEnumerable<JournalLane> lanes)
        {
            var array = new JArray();

            foreach (var lane in lanes)
            {
                var item = new JObject { ["key"] = lane.Key, ["value"] = lane.Value };

                if (lane.Operator != ArchiveKeyLookupOperator.Equal)
                {
                    item["op"] = Operators.First(pair => pair.Value == lane.Operator).Key;
                }

                if (lane.Upper is not null)
                {
                    item["to"] = lane.Upper;
                }

                array.Add(item);
            }

            return array.ToString(Formatting.None);
        }

        public static bool IsValid(List<JournalLane>? lanes)
        {
            return lanes is not null && lanes.Count <= MaximumLanes
                                     && lanes.All(lane => !string.IsNullOrWhiteSpace(lane.Key)
                                                          && lane.Key.Length <= MaximumKeyLength
                                                          && !lane.Key.Any(char.IsControl)
                                                          && IsValidValue(lane.Value)
                                                          && (lane.Operator == ArchiveKeyLookupOperator.Between
                                                              ? IsValidValue(lane.Upper)
                                                              : lane.Upper is null));
        }

        private static bool IsValidValue(string? value)
        {
            return !string.IsNullOrEmpty(value) && value.Length <= MaximumValueLength && !value.Contains('\0');
        }
    }
}
