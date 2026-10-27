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
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Dto.Query.CaseKeyJournal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalTransactionReader
    {
        private const string PayloadSource = "Payload";

        private static readonly (string Facet, string Group)[] valueFacets =
        [
            ("abstraction", "abstraction"),
            ("abstractionCalculation", "calculation"),
            ("ttlCounter", "counter"),
            ("dictionary", "dictionary"),
            ("sanction", "sanction"),
            ("httpAdaptation", "http")
        ];

        public static HashSet<string> VisibleNames(IEnumerable<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var xPath in xPaths)
            {
                names.Add(xPath.Name);

                var last = xPath.XPath?.Split('.', '/', '[', ']', '\'', '"')
                    .LastOrDefault(part => !string.IsNullOrWhiteSpace(part));

                if (!string.IsNullOrWhiteSpace(last))
                {
                    names.Add(last);
                }
            }

            return names;
        }

        public static List<JournalTransactionActivationDto> Activations(JObject json)
        {
            var activations = new List<JournalTransactionActivationDto>();

            if (json["activation"] is not JObject facet)
            {
                return activations;
            }

            foreach (var property in facet.Properties())
            {
                if (property.Value is JObject flags && flags.Value<bool?>("visible") == true)
                {
                    activations.Add(new JournalTransactionActivationDto
                    {
                        Name = property.Name, Forced = flags.Value<bool?>("forced") == true
                    });
                }
            }

            return activations;
        }

        public static List<JournalTransactionValueDto> Values(JObject json)
        {
            var values = new List<JournalTransactionValueDto>();

            foreach (var (facet, group) in valueFacets)
            {
                foreach (var (key, token) in JournalLensReader.Scalars(json[facet]))
                {
                    values.Add(new JournalTransactionValueDto
                    {
                        Group = group, Name = key, Value = JournalLensReader.Typed(token)
                    });
                }
            }

            return values;
        }

        public static List<JournalTransactionKeyDto> Keys(JObject json,
            IEnumerable<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths)
        {
            var keys = new List<JournalTransactionKeyDto>();

            foreach (var xPath in xPaths.Where(x => x.Drill || x.RoleId == 4))
            {
                string? value = null;

                try
                {
                    var token = json.SelectToken(xPath.XPath);

                    if (token is not null && token.Type is not (JTokenType.Object or JTokenType.Array
                            or JTokenType.Null))
                    {
                        value = token.ToString();
                    }
                }
                catch (Exception e) when (e is JsonException or ArgumentException)
                {
                    value = null;
                }

                keys.Add(new JournalTransactionKeyDto
                {
                    Name = xPath.Name, Value = value, Drill = xPath.Drill, PayloadKey = PayloadKey(xPath.XPath)
                });
            }

            return keys;
        }

        public static string? PayloadKey(string? xPath)
        {
            if (string.IsNullOrWhiteSpace(xPath))
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(xPath.Trim(),
                @"^(?:\$\.?)?payload(?:\.(?<name>[A-Za-z0-9_]+)|\[['""](?<name>[^'""\]]+)['""]\])$",
                System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100));

            return match.Success ? match.Groups["name"].Value : null;
        }

        public static List<JournalAdaptationDto> Adaptations(JObject json, ISet<string> visibleNames)
        {
            var adaptations = new List<JournalAdaptationDto>();

            Gather(adaptations, "exhaustive", json["exhaustiveAdaptation"], json["exhaustiveAdaptationDetail"],
                visibleNames);
            Gather(adaptations, "advanced", json["advancedAnalyticsAdaptation"],
                json["advancedAnalyticsAdaptationDetail"], visibleNames);
            Gather(adaptations, "http", null, json["httpAdaptation"], visibleNames);

            return adaptations;
        }

        private static void Gather(List<JournalAdaptationDto> into, string source, JToken? values, JToken? details,
            ISet<string> visibleNames)
        {
            var names = new List<string>();

            if (values is JObject valueObject)
            {
                names.AddRange(valueObject.Properties().Select(property => property.Name));
            }

            if (details is JObject detailObject)
            {
                names.AddRange(detailObject.Properties().Select(property => property.Name));
            }

            foreach (var name in names.Distinct(StringComparer.Ordinal))
            {
                var detail = (details as JObject)?.GetValue(name, StringComparison.Ordinal) as JObject;
                var plain = (values as JObject)?.GetValue(name, StringComparison.Ordinal);

                var value = Number(detail?.GetValue("value", StringComparison.OrdinalIgnoreCase))
                            ?? Number(plain);

                into.Add(Build(source, name, value, detail, visibleNames));
            }
        }

        public static JournalAdaptationDto Build(string source, string name, double? value, JObject? detail,
            ISet<string> visibleNames)
        {
            var adaptation = new JournalAdaptationDto { Source = source, Name = name, Value = value };

            if (detail is null)
            {
                return adaptation;
            }

            adaptation.Error = Text(detail, "error");
            adaptation.HumanLabel = Text(detail, "humanLabel");
            adaptation.Narrative = Text(detail, "narrative");
            adaptation.Model = Model(Object(detail, "model"));
            adaptation.Result = Result(Object(detail, "result"));
            adaptation.Calibration = Calibration(Object(detail, "calibration"));
            adaptation.Contribution = Contribution(Object(detail, "contribution"), visibleNames);
            adaptation.Journey = Journey(Object(detail, "journey"), visibleNames);

            return adaptation;
        }

        private static JournalAdaptationModelDto? Model(JObject? model)
        {
            return model is null
                ? null
                : new JournalAdaptationModelDto
                {
                    Name = Text(model, "name"),
                    Family = Text(model, "family"),
                    Version = Text(model, "version"),
                    ArtifactHash = Text(model, "artifactHash"),
                    TrainedDate = Date(model, "trainedDate"),
                    FeatureCount = Integer(model, "featureCount")
                };
        }

        private static JournalAdaptationResultDto? Result(JObject? result)
        {
            return result is null
                ? null
                : new JournalAdaptationResultDto
                {
                    Threshold = Number(result, "threshold"),
                    Activated = Flag(result, "activated"),
                    ExpectedPositiveRate = Number(result, "expectedPositiveRate")
                };
        }

        private static JournalCalibrationDto? Calibration(JObject? calibration)
        {
            if (calibration is null)
            {
                return null;
            }

            var dto = new JournalCalibrationDto
            {
                Space = Text(calibration, "space"),
                Calibrated = Flag(calibration, "calibrated"),
                Method = Text(calibration, "method"),
                ValidatedDate = Date(calibration, "validatedDate"),
                Sample = Integer(calibration, "sample"),
                Brier = Number(calibration, "brier"),
                Intercept = Number(calibration, "intercept"),
                Slope = Number(calibration, "slope")
            };

            if (Property(calibration, "band") is JArray bands)
            {
                dto.Bands = bands.OfType<JObject>().Select(band => new JournalCalibrationBandDto
                {
                    Lower = Number(band, "lower"),
                    Upper = Number(band, "upper"),
                    Expected = Number(band, "expected"),
                    Observed = Number(band, "observed"),
                    Count = Integer(band, "count")
                }).ToList();
            }

            return dto;
        }

        private static JournalContributionDto? Contribution(JObject? contribution, ISet<string> visibleNames)
        {
            if (contribution is null)
            {
                return null;
            }

            var dto = new JournalContributionDto
            {
                Space = Text(contribution, "space"),
                Method = Text(contribution, "method"),
                Exact = Flag(contribution, "exact"),
                BaseValue = Number(contribution, "baseValue")
            };

            if (Property(contribution, "items") is not JArray items)
            {
                return dto;
            }

            foreach (var item in items.OfType<JObject>())
            {
                var source = Text(item, "source");
                var name = Text(item, "name");

                if (IsHidden(source, name, visibleNames))
                {
                    dto.Items.Add(new JournalContributionItemDto { Hidden = true, Source = source });
                    dto.HiddenItems++;

                    continue;
                }

                var weight = Number(item, "weight");
                var direction = Number(item, "direction");

                dto.Items.Add(new JournalContributionItemDto
                {
                    Name = name,
                    Weight = weight,
                    Direction = direction,
                    Significance = Number(item, "significance"),
                    Source = source,
                    HumanLabel = Text(item, "humanLabel"),
                    Signed = Signed(weight, direction)
                });
            }

            return dto;
        }

        public static double? Signed(double? weight, double? direction)
        {
            if (weight is null)
            {
                return null;
            }

            return direction is { } sign && sign != 0 ? Math.Abs(weight.Value) * Math.Sign(sign) : weight;
        }

        private static List<JournalJourneyNodeDto> Journey(JObject? journey, ISet<string> visibleNames)
        {
            var nodes = new List<JournalJourneyNodeDto>();

            if (journey is null || Property(journey, "path") is not JArray path)
            {
                return nodes;
            }

            foreach (var node in path.OfType<JObject>())
            {
                var source = Text(node, "source");
                var feature = Text(node, "feature");

                if (IsHidden(source, feature, visibleNames))
                {
                    nodes.Add(new JournalJourneyNodeDto { Hidden = true, Source = source });

                    continue;
                }

                nodes.Add(new JournalJourneyNodeDto
                {
                    Feature = feature,
                    Operator = Text(node, "operator"),
                    Threshold = Number(node, "threshold"),
                    ThresholdCategory = Text(node, "thresholdCategory"),
                    Source = source,
                    HumanLabel = Text(node, "humanLabel")
                });
            }

            return nodes;
        }

        public static bool IsHidden(string? source, string? name, ISet<string> visibleNames)
        {
            return string.Equals(source, PayloadSource, StringComparison.OrdinalIgnoreCase)
                   && (string.IsNullOrEmpty(name) || !visibleNames.Contains(name));
        }

        private static JToken? Property(JObject owner, string name)
        {
            return owner.GetValue(name, StringComparison.OrdinalIgnoreCase);
        }

        private static JObject? Object(JObject owner, string name)
        {
            return Property(owner, name) as JObject;
        }

        private static string? Text(JObject owner, string name)
        {
            return Property(owner, name) is { Type: JTokenType.String } token ? token.Value<string>() : null;
        }

        private static double? Number(JObject owner, string name)
        {
            return Number(Property(owner, name));
        }

        private static double? Number(JToken? token)
        {
            return token is { Type: JTokenType.Integer or JTokenType.Float } && double.IsFinite(token.Value<double>())
                ? token.Value<double>()
                : null;
        }

        private static int? Integer(JObject owner, string name)
        {
            return Property(owner, name) is { Type: JTokenType.Integer } token ? token.Value<int>() : null;
        }

        private static bool? Flag(JObject owner, string name)
        {
            return Property(owner, name) is { Type: JTokenType.Boolean } token ? token.Value<bool>() : null;
        }

        private static DateTimeOffset? Date(JObject owner, string name)
        {
            return Property(owner, name) switch
            {
                { Type: JTokenType.Date } token => new DateTimeOffset(
                    DateTime.SpecifyKind(token.Value<DateTime>(), DateTimeKind.Utc), TimeSpan.Zero),
                { Type: JTokenType.String } token when DateTimeOffset.TryParse(token.Value<string>(),
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) => parsed,
                _ => null
            };
        }
    }
}
