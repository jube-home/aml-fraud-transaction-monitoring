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
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalLensReader
    {
        public const string Payload = "payload";
        public const string Rules = "rules";
        public const string Features = "features";
        public const string Models = "models";
        public const string Sanctions = "sanctions";

        public static readonly string[] Lenses = [Payload, Rules, Features, Models, Sanctions];

        private static readonly (string Facet, string Group)[] featureFacets =
        [
            ("abstraction", "abstraction"),
            ("abstractionCalculation", "calculation"),
            ("ttlCounter", "counter"),
            ("dictionary", "dictionary")
        ];

        private static readonly (string Facet, string Group)[] modelFacets =
        [
            ("exhaustiveAdaptation", "exhaustive"),
            ("advancedAnalyticsAdaptation", "advanced")
        ];

        private static readonly (string Facet, string Group)[] sanctionFacets =
        [
            ("sanction", "sanction"),
            ("httpAdaptation", "http")
        ];

        public static bool IsLens(string? lens)
        {
            return lens is not null && Lenses.Contains(lens, StringComparer.Ordinal);
        }

        public static List<string> VisibleActivations(JObject json)
        {
            var names = new List<string>();

            if (json["activation"] is not JObject activations)
            {
                return names;
            }

            foreach (var property in activations.Properties())
            {
                if (property.Value is JObject flags && flags.Value<bool?>("visible") == true)
                {
                    names.Add(property.Name);
                }
            }

            return names;
        }

        public static Dictionary<string, bool> Presence(JObject json)
        {
            return new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                [Payload] = json["payload"] is JObject { HasValues: true },
                [Rules] = VisibleActivations(json).Count > 0,
                [Features] = featureFacets.Any(f => Scalars(json[f.Facet]).Any()),
                [Models] = modelFacets.Any(f => Scalars(json[f.Facet]).Any()),
                [Sanctions] = sanctionFacets.Any(f => Scalars(json[f.Facet]).Any())
            };
        }

        public static void Read(JObject json, string lens, Dictionary<string, object?> row, JournalColumnSet columns,
            IReadOnlyList<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths)
        {
            switch (lens)
            {
                case Rules:
                    ReadRules(json, row, columns);

                    break;
                case Features:
                    ReadFacets(json, featureFacets, Features, row, columns);

                    break;
                case Models:
                    ReadFacets(json, modelFacets, Models, row, columns);

                    break;
                case Sanctions:
                    ReadFacets(json, sanctionFacets, Sanctions, row, columns);

                    break;
                default:
                    ReadPayload(json, xPaths, row, columns);

                    break;
            }
        }

        private static void ReadRules(JObject json, Dictionary<string, object?> row, JournalColumnSet columns)
        {
            if (json["activation"] is not JObject activations)
            {
                return;
            }

            foreach (var property in activations.Properties())
            {
                if (property.Value is not JObject flags || flags.Value<bool?>("visible") != true)
                {
                    continue;
                }

                var name = $"rule:{property.Name}";

                columns.Note(new JournalColumn(name, property.Name, "string", 2, Rules, "rule", false));
                row[name] = flags.Value<bool?>("forced") == true ? "forced" : "fired";
            }
        }

        private static void ReadFacets(JObject json, (string Facet, string Group)[] facets, string lens,
            Dictionary<string, object?> row, JournalColumnSet columns)
        {
            foreach (var (facet, group) in facets)
            {
                foreach (var (key, token) in Scalars(json[facet]))
                {
                    var typed = Typed(token);
                    var name = $"{group}:{key}";

                    columns.Note(new JournalColumn(name, key, TypeOf(typed), 1, lens, group, false));
                    row[name] = typed;
                }
            }
        }

        public static IEnumerable<(string Key, JToken Token)> Scalars(JToken? facet)
        {
            if (facet is not JObject values)
            {
                yield break;
            }

            foreach (var property in values.Properties())
            {
                if (property.Value.Type is JTokenType.Integer or JTokenType.Float or JTokenType.Boolean
                    or JTokenType.String or JTokenType.Date)
                {
                    yield return (property.Name, property.Value);
                }
                else if (property.Value is JObject record
                         && record.GetValue("value", StringComparison.OrdinalIgnoreCase) is
                             { Type: JTokenType.Integer or JTokenType.Float } inner)
                {
                    yield return (property.Name, inner);
                }
            }
        }

        public static object? Typed(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Date => DynamicResultsSchema.ToUtcOffset(token.Value<DateTime>()),
                _ => token.Value<string>()
            };
        }

        private static string TypeOf(object? value)
        {
            return value is null ? "string" : DynamicResultsSchema.ClrTypeToSchemaType(value);
        }

        private static void ReadPayload(JObject json, IReadOnlyList<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths,
            Dictionary<string, object?> row, JournalColumnSet columns)
        {
            foreach (var xPath in xPaths)
            {
                object? typed = null;

                try
                {
                    var token = json.SelectToken(xPath.XPath);

                    if (token is not null && token.Type is not (JTokenType.Object or JTokenType.Array))
                    {
                        typed = Typed(token);
                    }
                }
                catch (Exception e) when (e is JsonException or ArgumentException or InvalidCastException
                                              or FormatException)
                {
                    typed = null;
                }

                columns.Note(new JournalColumn(xPath.Name, xPath.Name, TypeOf(typed), xPath.RoleId, Payload,
                    "payload", xPath.Drill));
                row[xPath.Name] = typed;
            }
        }

        public static Dictionary<string, string> BoldReference(JObject? caseJson,
            IReadOnlyList<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths)
        {
            var reference = new Dictionary<string, string>(StringComparer.Ordinal);

            if (caseJson is null)
            {
                return reference;
            }

            foreach (var xPath in xPaths.Where(xPath => xPath.BoldLineMatched))
            {
                var value = ScalarText(caseJson, xPath.XPath);

                if (value is not null)
                {
                    reference[xPath.Name] = value;
                }
            }

            return reference;
        }

        public static void Format(JObject json, IReadOnlyList<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths,
            Dictionary<string, object?> row, IReadOnlyDictionary<string, string>? reference)
        {
            var cellFormats = new List<Dictionary<string, object?>>();

            foreach (var xPath in xPaths)
            {
                var value = ScalarText(json, xPath.XPath);

                if (value is null)
                {
                    continue;
                }

                if (xPath.ConditionalRegularExpressionFormatting && Matches(xPath.RegularExpression, value))
                {
                    cellFormats.Add(new Dictionary<string, object?>
                    {
                        ["CellFormatKey"] = xPath.Name,
                        ["CellFormatPattern"] = xPath.RegularExpression,
                        ["CellFormatBackColor"] = xPath.ConditionalFormatBackColor,
                        ["CellFormatForeColor"] = xPath.ConditionalFormatForeColor,
                        ["CellFormatForeRow"] = xPath.ForeRowColorScope,
                        ["CellFormatBackRow"] = xPath.BackRowColorScope
                    });
                }

                if (xPath.BoldLineMatched && !row.ContainsKey("BoldLine") && reference is not null
                    && reference.TryGetValue(xPath.Name, out var wanted)
                    && string.Equals(wanted, value, StringComparison.Ordinal))
                {
                    row["BoldLine"] = new Dictionary<string, object?>
                    {
                        ["BoldLineKey"] = xPath.Name,
                        ["BoldLineValue"] = value,
                        ["BoldLineFormatBackColor"] = xPath.BoldLineFormatBackColor,
                        ["BoldLineFormatForeColor"] = xPath.BoldLineFormatForeColor
                    };
                }
            }

            if (cellFormats.Count > 0)
            {
                row["CellFormat"] = cellFormats;
            }
        }

        private static string? ScalarText(JObject json, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                var token = json.SelectToken(path);

                return token is null or { Type: JTokenType.Object or JTokenType.Array or JTokenType.Null }
                    ? null
                    : token.ToString(Formatting.None).Trim('"');
            }
            catch (Exception e) when (e is JsonException or ArgumentException or InvalidCastException
                                          or FormatException)
            {
                return null;
            }
        }

        private static bool Matches(string? pattern, string? value)
        {
            if (string.IsNullOrEmpty(pattern) || value is null)
            {
                return false;
            }

            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(value, pattern,
                    System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100));
            }
            catch (Exception e) when (e is ArgumentException or
                                          System.Text.RegularExpressions.RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
