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

using System.Globalization;
using Jube.CaseRisk.Similarity;
using Jube.Data.Query.ArchiveKeyLookup;
using Jube.Dto.Options;
using Jube.Service.Query.CaseKeyJournal;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseSimilarity
{
    public static class CaseSimilarityVectorReader
    {
        private const int DateDataTypeId = 4;
        private const int LatitudeDataTypeId = 6;
        private const int LongitudeDataTypeId = 7;

        private static readonly string[] featureFacets = ["abstraction", "abstractionCalculation", "ttlCounter"];
        private static readonly string[] modelFacets = ["exhaustiveAdaptation", "advancedAnalyticsAdaptation"];

        public static List<SimilarityComponent> Read(IReadOnlyList<JObject> rowsNewestFirst,
            IReadOnlyDictionary<string, ArchiveKeyField> payloadFields, Func<string, string, string> hash,
            SimilaritySettings settings)
        {
            var rules = new HashSet<string>(StringComparer.Ordinal);
            var texts = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            var numbers = new SortedDictionary<string, List<double>>(StringComparer.Ordinal);
            var models = new SortedDictionary<string, double>(StringComparer.Ordinal);

            foreach (var row in rowsNewestFirst)
            {
                rules.UnionWith(JournalLensReader.VisibleActivations(row));
                ReadPayload(row, payloadFields, texts, numbers);
                ReadModels(row, models);
            }

            var components = new List<SimilarityComponent>();

            components.AddRange(SimilarityVectorBuilder.Flags(CaseSimilarityKind.Rule, rules));

            foreach (var (name, value) in models)
            {
                if (SimilarityBanding.Share(value) is { } band)
                {
                    components.Add(new SimilarityComponent(CaseSimilarityKind.Model, name + ":max=" + band));
                }
            }

            foreach (var (name, values) in texts)
            {
                var kind = payloadFields.TryGetValue(name, out var field) && field.IsArchiveKey
                    ? CaseSimilarityKind.Entity
                    : CaseSimilarityKind.Category;

                components.AddRange(SimilarityVectorBuilder.Values(kind, name,
                    values.Distinct(StringComparer.Ordinal).Select(value => hash(name, value)), settings));
            }

            foreach (var (name, values) in numbers)
            {
                components.AddRange(SimilarityVectorBuilder.Measures(CaseSimilarityKind.Measure, name, values));
            }

            if (rowsNewestFirst.Count > 0)
            {
                components.AddRange(ReadFeatures(rowsNewestFirst[0]));
            }

            return Bound(components, settings);
        }

        public static List<SimilarityComponent> Bound(IEnumerable<SimilarityComponent> components,
            SimilaritySettings settings)
        {
            return
            [
                .. components
                    .Where(component => component.Value.Length <= settings.MaximumComponentLength)
                    .Distinct()
                    .OrderBy(component => component.Kind)
                    .ThenBy(component => component.Value, StringComparer.Ordinal)
                    .Take(settings.MaximumComponents)
            ];
        }

        private static void ReadPayload(JObject row, IReadOnlyDictionary<string, ArchiveKeyField> payloadFields,
            SortedDictionary<string, List<string>> texts, SortedDictionary<string, List<double>> numbers)
        {
            foreach (var (name, token) in JournalLensReader.Scalars(row["payload"]))
            {
                payloadFields.TryGetValue(name, out var field);

                if (field?.DataTypeId is DateDataTypeId or LatitudeDataTypeId or LongitudeDataTypeId)
                {
                    continue;
                }

                var identifier = token.Type == JTokenType.Integer && field?.IsArchiveKey == true;

                if (identifier || token.Type is JTokenType.String or JTokenType.Boolean)
                {
                    var text = Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Add(texts, name, text);
                    }
                }
                else if (token.Type is JTokenType.Integer or JTokenType.Float)
                {
                    Add(numbers, name, token.Value<double>());
                }
            }
        }

        private static void ReadModels(JObject row, SortedDictionary<string, double> models)
        {
            foreach (var facet in modelFacets)
            {
                foreach (var (name, token) in JournalLensReader.Scalars(row[facet]))
                {
                    if (token.Type is not (JTokenType.Integer or JTokenType.Float))
                    {
                        continue;
                    }

                    var value = token.Value<double>();

                    models[name] = models.TryGetValue(name, out var highest) ? Math.Max(highest, value) : value;
                }
            }
        }

        private static IEnumerable<SimilarityComponent> ReadFeatures(JObject latest)
        {
            foreach (var facet in featureFacets)
            {
                foreach (var (name, token) in JournalLensReader.FacetScalars(latest, facet))
                {
                    if (token.Type is JTokenType.Integer or JTokenType.Float
                        && SimilarityBanding.Band(token.Value<double>()) is { } band)
                    {
                        yield return new SimilarityComponent(CaseSimilarityKind.Feature,
                            facet + ":" + name + "=" + band);
                    }
                }
            }
        }

        private static void Add<T>(SortedDictionary<string, List<T>> values, string name, T value)
        {
            if (!values.TryGetValue(name, out var list))
            {
                list = [];
                values[name] = list;
            }

            list.Add(value);
        }
    }
}
