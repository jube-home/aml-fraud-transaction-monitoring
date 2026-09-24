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

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Extraction;
    using Newtonsoft.Json.Linq;

    public sealed record RequestField(string Name, string XPath, int DataTypeId, string DefaultValue, bool Encrypted);

    public sealed record RequestReferences(
        string EntryXPath,
        string ReferenceDateXPath,
        int ReferenceDatePayloadLocationTypeId);

    public static class InvocationContextBuilder
    {
        public const string Extraction = "Extraction";
        private const string InlineFunctions = "InlineFunctions";
        private const string InlineScripts = "InlineScripts";
        private const string GatewayRules = "GatewayRules";
        private const string DictionaryLookups = "DictionaryLookups";
        private const string Sanctions = "Sanctions";
        private const string TtlCounters = "TtlCounters";
        public const string AbstractionRulesWithSearchKeys = "AbstractionRulesWithSearchKeys";
        private const string AbstractionRulesWithoutSearchKeys = "AbstractionRulesWithoutSearchKeys";
        private const string AbstractionCalculations = "AbstractionCalculations";
        private const string ExhaustiveAdaptations = "ExhaustiveAdaptations";
        public const string HttpAdaptations = "HttpAdaptations";
        private const string ActivationRules = "ActivationRules";

        private static readonly string[] valueStages =
        [
            Extraction, InlineFunctions, InlineScripts, GatewayRules, DictionaryLookups, Sanctions, TtlCounters,
            AbstractionRulesWithSearchKeys, AbstractionRulesWithoutSearchKeys, AbstractionCalculations,
            ExhaustiveAdaptations, HttpAdaptations, ActivationRules
        ];

        private static readonly (string Stage, string Note)[] excludedStages =
        [
            ("CacheStorage", "Writes the payload to the cache; storage is never performed for a context."),
            ("PayloadJournal", "Journals the payload under its search keys; storage is never performed."),
            ("TtlCounterIncrements", "Increments TTL counters on activation; storage is never performed."),
            ("ResponseElevation", "Records response elevation counters; storage is never performed."),
            ("CaseCreation", "Creates cases on activation; storage is never performed."),
            ("ActivationWatcher", "Streams activations to the watcher; notification is never performed."),
            ("Notifications", "Sends activation notifications; notification is never performed."),
            ("ResponseAndAsynchronousQueue", "Writes the response and queues asynchronous messages; never performed."),
            ("ArchiveStorage", "Archives the transaction; storage is never performed.")
        ];

        private static readonly Dictionary<string, string> archiveKeys = new(StringComparer.Ordinal)
        {
            ["Payload"] = "payload",
            ["TTLCounter"] = "ttlCounter",
            ["Abstraction"] = "abstraction",
            ["Sanction"] = "sanction",
            ["AbstractionCalculation"] = "abstractionCalculation",
            ["ExhaustiveAdaptation"] = "exhaustiveAdaptation",
            ["HTTPAdaptation"] = "httpAdaptation",
            ["Dictionary"] = "dictionary",
            ["Activation"] = "activation"
        };

        public static InvocationContext Blank(int entityAnalysisModelId,
            IEnumerable<InvocationContextField> fields, IEnumerable<RequestField> requestFields, bool assumeLocal,
            DateTime utcNow)
        {
            var context = new InvocationContext
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Source = InvocationContextSource.Blank
            };
            var defaults = requestFields
                .Where(r => !string.IsNullOrEmpty(r.DefaultValue))
                .GroupBy(r => "Payload." + r.Name)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var field in fields)
            {
                if (defaults.TryGetValue(field.Name, out var request) &&
                    RequestFieldExtraction.TryConvert(request.DataTypeId, request.DefaultValue, true,
                        request.DefaultValue, assumeLocal, utcNow, out var converted))
                {
                    context.Values[field.Name] = new InvocationValue(field, converted, InvocationValueOrigin.Default);
                    continue;
                }

                context.Values[field.Name] = new InvocationValue(field, null, InvocationValueOrigin.Unset);
            }

            AddStages(context, _ => (InvocationStageStatus.NotComputed,
                "Not computed; set the values this stage would produce with an overlay."));
            return context;
        }

        public static InvocationContext FromRequestJson(int entityAnalysisModelId, JObject json,
            IEnumerable<InvocationContextField> fields, IEnumerable<RequestField> requestFields,
            RequestReferences references, bool assumeLocal, DateTime utcNow)
        {
            ArgumentNullException.ThrowIfNull(json);
            ArgumentNullException.ThrowIfNull(references);

            var context = new InvocationContext
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Source = InvocationContextSource.RequestJson,
                EntryId = EntryId(json, references.EntryXPath),
                ReferenceDate = RequestFieldExtraction.ReferenceDate(json,
                    references.ReferenceDatePayloadLocationTypeId, references.ReferenceDateXPath, assumeLocal, utcNow)
            };

            var byName = requestFields.GroupBy(r => "Payload." + r.Name).ToDictionary(g => g.Key, g => g.First());

            foreach (var field in fields)
            {
                if (!byName.TryGetValue(field.Name, out var request))
                {
                    context.Values[field.Name] = new InvocationValue(field, null, InvocationValueOrigin.Unset);
                    continue;
                }

                context.Values[field.Name] = Extract(json, field, request, assumeLocal, utcNow);
            }

            AddStages(context, stage => stage switch
            {
                Extraction => (InvocationStageStatus.Computed,
                    "Request XPaths read from the JSON with the engine's own extraction and defaults."),
                HttpAdaptations => (InvocationStageStatus.Excluded,
                    "External HTTP adaptations are not called; set their values with an overlay."),
                _ => (InvocationStageStatus.NotComputed,
                    "Not computed; set the values this stage would produce with an overlay.")
            });
            return context;
        }

        public static InvocationContext FromArchive(int entityAnalysisModelId, Guid entryGuid, JObject archive,
            IEnumerable<InvocationContextField> fields)
        {
            ArgumentNullException.ThrowIfNull(archive);

            var context = new InvocationContext
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Source = InvocationContextSource.Archive,
                EntityAnalysisModelInstanceEntryGuid = entryGuid,
                EntryId = archive["entityInstanceEntryId"]?.ToString(),
                ReferenceDate = archive["referenceDate"]?.Type == JTokenType.Date
                    ? archive["referenceDate"].Value<DateTime>()
                    : null
            };

            foreach (var field in fields)
            {
                context.Values[field.Name] = FromArchiveValue(archive, field);
            }

            AddStages(context, _ => (InvocationStageStatus.FromArchive,
                "Values as they were when the transaction was invoked and archived."));
            return context;
        }

        public static IReadOnlyList<string> Overlay(InvocationContext context,
            IEnumerable<KeyValuePair<string, string>> values)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(values);

            var errors = new List<string>();
            foreach (var (rawName, text) in values)
            {
                var name = rawName ?? string.Empty;
                if (!context.Values.TryGetValue(name, out var existing))
                {
                    errors.Add($"'{name}' is not a name in this model's context.");
                    continue;
                }

                if (text == null)
                {
                    context.Values[name] = existing with { Value = null, Origin = InvocationValueOrigin.Overlay };
                    continue;
                }

                if (!TryConvertText(existing.Field.DataType, text, out var converted))
                {
                    errors.Add($"'{text}' is not a valid {existing.Field.DataType} for '{name}'.");
                    continue;
                }

                context.Values[name] = existing with
                {
                    Value = converted, Origin = InvocationValueOrigin.Overlay, Note = null
                };
            }

            return errors;
        }

        public static bool TryConvertText(string dataType, string text, out object value)
        {
            switch (dataType)
            {
                case "integer":
                    var intOk = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i);
                    value = intOk ? i : null;
                    return intOk;
                case "double":
                    var doubleOk = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var d);
                    value = doubleOk ? d : null;
                    return doubleOk;
                case "datetime":
                    var dateOk = DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var dt);
                    value = dateOk ? dt.UtcDateTime : null;
                    return dateOk;
                case "boolean":
                    var boolOk = bool.TryParse(text, out var b) || text is "0" or "1";
                    value = boolOk ? b || text == "1" : null;
                    return boolOk;
                default:
                    value = text;
                    return true;
            }
        }

        public static string FormatValue(object value)
        {
            return value switch
            {
                null => null,
                DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
                bool flag => flag ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        private static InvocationValue Extract(JObject json, InvocationContextField field, RequestField request,
            bool assumeLocal, DateTime utcNow)
        {
            var selection = RequestFieldExtraction.Select(json, request.XPath, request.DefaultValue);
            if (selection.Value == null)
            {
                return new InvocationValue(field, null, InvocationValueOrigin.Unset,
                    selection.Error != null
                        ? $"The XPath {request.XPath} could not be read: {selection.Error.Message}"
                        : $"Not present at {request.XPath} and there is no default.");
            }

            if (!RequestFieldExtraction.TryConvert(request.DataTypeId, selection.Value, selection.DefaultFallback,
                    request.DefaultValue, assumeLocal, utcNow, out var converted))
            {
                return new InvocationValue(field, null, InvocationValueOrigin.Unset,
                    $"'{selection.Value}' at {request.XPath} is not a valid {field.DataType}; the engine leaves it unset.");
            }

            var note = request.Encrypted
                ? "The engine encrypts this field before rules see it; the plain value is shown."
                : null;
            return new InvocationValue(field, converted,
                selection.DefaultFallback ? InvocationValueOrigin.Default : InvocationValueOrigin.Extracted, note);
        }

        private static InvocationValue FromArchiveValue(JObject archive, InvocationContextField field)
        {
            if (!archiveKeys.TryGetValue(field.Namespace, out var key) || archive[key] is not JObject group)
            {
                return new InvocationValue(field, null, InvocationValueOrigin.Unset,
                    "Not recorded in the archived transaction.");
            }

            if (field.Namespace == "Activation")
            {
                return new InvocationValue(field, group[field.Key] != null, InvocationValueOrigin.Archive);
            }

            var token = group[field.Key];
            if (field.Namespace == "HTTPAdaptation" && token is JObject adaptation)
            {
                var error = adaptation["error"]?.ToString();
                token = adaptation["value"];
                if (token == null || token.Type == JTokenType.Null)
                {
                    return new InvocationValue(field, null, InvocationValueOrigin.Archive,
                        string.IsNullOrEmpty(error) ? "The adaptation returned no value." : error);
                }
            }

            if (token == null || token.Type == JTokenType.Null)
            {
                return new InvocationValue(field, null, InvocationValueOrigin.Unset,
                    "Not recorded in the archived transaction.");
            }

            var text = token.Type == JTokenType.Date
                ? token.Value<DateTime>().ToString("o", CultureInfo.InvariantCulture)
                : token.ToString();
            return TryConvertText(field.DataType, text, out var converted)
                ? new InvocationValue(field, converted, InvocationValueOrigin.Archive)
                : new InvocationValue(field, text, InvocationValueOrigin.Archive,
                    $"The archived value is not a valid {field.DataType}; shown as recorded.");
        }

        private static string EntryId(JObject json, string entryXPath)
        {
            try
            {
                return string.IsNullOrEmpty(entryXPath) ? string.Empty : json.SelectToken(entryXPath)?.ToString() ?? "";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return string.Empty;
            }
        }

        private static void AddStages(InvocationContext context,
            Func<string, (InvocationStageStatus Status, string Note)> status)
        {
            foreach (var stage in valueStages)
            {
                var (stageStatus, note) = status(stage);
                context.Stages.Add(new InvocationStage(stage, stageStatus, note));
            }

            foreach (var (stage, note) in excludedStages)
            {
                context.Stages.Add(new InvocationStage(stage, InvocationStageStatus.Excluded, note));
            }
        }
    }
}