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

using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;

namespace Jube.Service.Query.CaseWorkflowDisplayExecution
{
    public static class CaseWorkflowTemplateRoots
    {
        public static IReadOnlyDictionary<string, object?> Build(Data.Poco.Case existingCase, Data.Poco.CaseWorkflowStatus status,
            EntityAnalysisModelInstanceEntryPayload payload)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Case"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = existingCase.Id,
                    ["CaseKey"] = existingCase.CaseKey,
                    ["CaseKeyValue"] = existingCase.CaseKeyValue,
                    ["Status"] = status.Name,
                    ["Priority"] = status.Priority.HasValue ? (int)status.Priority.Value : null,
                    ["Created"] = existingCase.CreatedDate
                },
                ["Payload"] = payload.Payload == null
                    ? Empty()
                    : Typed(payload.Payload.Select(p => new KeyValuePair<string, object?>(p.Key, Value(p.Value)))),
                ["Abstraction"] = Numbers(payload.Abstraction),
                ["Dictionary"] = Numbers(payload.Dictionary),
                ["TtlCounter"] = Numbers(payload.TtlCounter),
                ["Sanction"] = Numbers(payload.Sanction),
                ["ExhaustiveAdaptation"] = Numbers(payload.ExhaustiveAdaptation),
                ["AdvancedAnalyticsAdaptation"] = Numbers(payload.AdvancedAnalyticsAdaptation),
                ["HttpAdaptation"] = payload.HttpAdaptation == null
                    ? Empty()
                    : Typed(payload.HttpAdaptation.Select(p =>
                        new KeyValuePair<string, object?>(p.Key, p.Value?.Value))),
                ["Activation"] = payload.Activation == null
                    ? Empty()
                    : Typed(payload.Activation.Select(p => new KeyValuePair<string, object?>(p.Key, true)))
            };
        }

        private static Dictionary<string, object?> Empty()
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, object?> Typed(IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            var result = Empty();

            foreach (var (key, value) in pairs)
            {
                result[key] = value;
            }

            return result;
        }

        private static Dictionary<string, object?> Numbers(IEnumerable<KeyValuePair<string, double>>? source)
        {
            return source == null
                ? Empty()
                : Typed(source.Select(p => new KeyValuePair<string, object?>(p.Key, p.Value)));
        }

        private static object? Value(InternalValue value)
        {
            return value.Type switch
            {
                InternalValue.ValueType.String => value.AsString(),
                InternalValue.ValueType.Int => value.AsInt(),
                InternalValue.ValueType.Double => value.AsDouble(),
                InternalValue.ValueType.Bool => value.AsBool(),
                InternalValue.ValueType.DateTime => value.AsDateTime(),
                InternalValue.ValueType.Guid => value.AsGuid().ToString(),
                _ => null
            };
        }
    }
}
