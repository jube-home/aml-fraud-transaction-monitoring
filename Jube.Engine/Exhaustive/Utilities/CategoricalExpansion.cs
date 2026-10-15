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
using System.Linq;
using Jube.Analytics.Categorical;
using Jube.Analytics.Categorical.Models;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

namespace Jube.Engine.Exhaustive.Utilities
{
    public static class CategoricalExpansion
    {
        internal const byte PayloadProcessingTypeId = 1;

        internal static Dictionary<int, Variable> Expand(
            IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto> fields,
            Dictionary<int, Variable> variables,
            IReadOnlyList<string> jsonList,
            IReadOnlyList<string> additionalFitJson,
            CategoricalOptions options,
            out CategoricalPlan plan)
        {
            var candidates = new List<(GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto Field,
                CategoricalCounter Counter)>();

            var known = new HashSet<string>(variables.Values.Select(s => s.Name), StringComparer.Ordinal);

            foreach (var field in fields)
            {
                if (field.ProcessingTypeId != PayloadProcessingTypeId || string.IsNullOrWhiteSpace(field.Name)
                                                                      || string.IsNullOrWhiteSpace(
                                                                          field.ValueJsonPath) ||
                                                                      known.Contains(field.Name)
                                                                      || !IsCandidateType(field.DataTypeId, options,
                                                                          field.Name))
                {
                    continue;
                }

                if (candidates.Any(a => a.Field.Name == field.Name))
                {
                    continue;
                }

                candidates.Add((field, new CategoricalCounter(options.MaxDistinctTracked)));
            }

            if (candidates.Count == 0)
            {
                plan = new CategoricalPlan([], []);

                return variables;
            }

            foreach (var json in additionalFitJson == null ? jsonList : jsonList.Concat(additionalFitJson))
            {
                var jObject = JObject.Parse(json);

                foreach (var (field, counter) in candidates)
                {
                    counter.Add(CategoricalValue.FromToken(SelectToken(jObject, field.ValueJsonPath)));
                }
            }

            plan = CategoricalPlanner.Fit(
            [
                .. candidates.Select(s => new CategoricalFieldCandidate(s.Field.Name, s.Counter,
                    s.Field.DataTypeId == 1, s.Field.DataTypeId is not (0 or 1)))
            ], options);

            var expanded = new Dictionary<int, Variable>(variables);
            var index = expanded.Count;

            foreach (var encoder in plan.Encoders)
            {
                var path = candidates.First(f => f.Field.Name == encoder.SourceName).Field.ValueJsonPath;

                for (var i = 0; i < encoder.Indicators.Count; i++)
                {
                    expanded[index++] = new Variable
                    {
                        Name = encoder.Indicators[i].Name,
                        ProcessingTypeId = PayloadProcessingTypeId,
                        ValueJsonPath = path,
                        CategoricalEncoder = encoder,
                        CategoricalIndicatorIndex = i
                    };
                }
            }

            return expanded;
        }

        internal static bool IsCandidateType(int dataTypeId, CategoricalOptions options, string name)
        {
            return dataTypeId is 0 or 1 || options.Include.Contains(name, StringComparer.Ordinal);
        }

        internal static JToken SelectToken(JObject jObject, string path)
        {
            try
            {
                return jObject.SelectToken(path);
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException)
            {
                return null;
            }
        }
    }
}