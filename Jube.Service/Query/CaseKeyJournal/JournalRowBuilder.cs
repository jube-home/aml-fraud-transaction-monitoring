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


using Jube.Data.Query.DynamicResultsSchema;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalRowBuilder
    {
        public static Dictionary<string, object?> Base(GetCaseKeyJournalRowsQueryDto record, JObject json,
            bool pinned, string? lane)
        {
            var activations = JournalLensReader.VisibleActivations(json);

            var row = new Dictionary<string, object?>
            {
                ["Id"] = record.Id,
                ["EntityAnalysisModelInstanceEntryGuid"] = record.EntityAnalysisModelInstanceEntryGuid.ToString(),
                ["ReferenceDate"] = record.ReferenceDate.HasValue
                    ? DynamicResultsSchema.ToUtcOffset(record.ReferenceDate.Value)
                    : null,
                ["CreatedDate"] = record.CreatedDate.HasValue
                    ? DynamicResultsSchema.ToUtcOffset(record.CreatedDate.Value)
                    : null,
                ["ResponseElevation"] = record.ResponseElevation,
                ["ResponseElevationName"] = json["responseElevation"]?["name"]?.ToString(),
                ["HoldState"] = json["hold"]?["stateId"]?.Value<int?>(),
                ["EntryKeyValue"] = record.EntryKeyValue,
                ["Activations"] = string.Join(", ", activations),
                ["ActivationCount"] = activations.Count,
                ["Tag"] = json["tag"] is JArray tags ? tags.Select(tag => tag.ToString()).ToList() : [],
                ["Pinned"] = pinned
            };

            if (lane is not null)
            {
                row["Lane"] = lane;
            }

            return row;
        }

        public static JObject Parse(string? json)
        {
            try
            {
                return JObject.Parse(json ?? "{}");
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return new JObject();
            }
        }
    }
}
