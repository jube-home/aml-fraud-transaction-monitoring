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


using System.Text;
using Jube.Preservation.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Preservation.Diff
{
    public static class PreservationSummariser
    {
        public static IReadOnlyList<SummarySection> Summarise(JObject payload)
        {
            var models = Array(payload, "EntityAnalysisModel");
            var lists = Nested(models, "EntityAnalysisModelList");
            var dictionaries = Nested(models, "EntityAnalysisModelDictionary");
            var exhaustiveInstances = Nested(models, "ExhaustiveSearchInstance");
            var exhaustiveDatasets = Array(payload, "ExhaustiveInstanceCsv").Concat(Array(payload, "ExhaustiveInstanceJson"))
                .ToList();

            var modelBytes = Bytes(models) - Bytes(lists) - Bytes(dictionaries) - Bytes(exhaustiveInstances);

            return
            [
                new SummarySection("models", models.Count, Objects(models) - models.Count - Objects(lists) -
                                                              Objects(dictionaries) - Objects(exhaustiveInstances),
                    Math.Max(modelBytes, 0)),
                new SummarySection("visualisations", Array(payload, "VisualisationRegistry").Count,
                    Objects(Array(payload, "VisualisationRegistry")) - Array(payload, "VisualisationRegistry").Count,
                    Bytes(Array(payload, "VisualisationRegistry"))),
                new SummarySection("roles", Array(payload, "RoleRegistry").Count,
                    Objects(Array(payload, "RoleRegistry")) - Array(payload, "RoleRegistry").Count,
                    Bytes(Array(payload, "RoleRegistry"))),
                new SummarySection("lists", lists.Count, Objects(lists) - lists.Count, Bytes(lists)),
                new SummarySection("dictionaries", dictionaries.Count, Objects(dictionaries) - dictionaries.Count,
                    Bytes(dictionaries)),
                new SummarySection("exhaustive", exhaustiveInstances.Count + exhaustiveDatasets.Count,
                    Objects(exhaustiveInstances) - exhaustiveInstances.Count + Objects(exhaustiveDatasets) -
                    exhaustiveDatasets.Count, Bytes(exhaustiveInstances) + Bytes(exhaustiveDatasets)),
                new SummarySection("advancedAnalytics", payload["AdvancedAnalytics"] is JObject ? 1 : 0,
                    payload["AdvancedAnalytics"] is JObject analytics ? Objects([analytics]) - 1 : 0,
                    payload["AdvancedAnalytics"] is JObject ? Bytes([payload["AdvancedAnalytics"]!]) : 0),
                new SummarySection("approvals", Array(payload, "EntityApproval").Count, 0,
                    Bytes(Array(payload, "EntityApproval")))
            ];
        }

        private static List<JToken> Array(JObject source, string name)
        {
            return source[name] is JArray array ? [.. array] : [];
        }

        private static List<JToken> Nested(List<JToken> parents, string name)
        {
            return [.. parents.OfType<JObject>().SelectMany(p => Array(p, name))];
        }

        private static int Objects(IEnumerable<JToken> tokens)
        {
            return tokens.Sum(Count);
        }

        private static int Count(JToken token)
        {
            var total = token is JObject ? 1 : 0;

            foreach (var child in token.Children())
            {
                total += Count(child is JProperty property ? property.Value : child);
            }

            return total;
        }

        private static long Bytes(IEnumerable<JToken> tokens)
        {
            return tokens.Sum(t => Encoding.UTF8.GetByteCount(t.ToString(Formatting.None)));
        }
    }
}
