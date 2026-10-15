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

using Jube.Service.ModelDiagrams.Exceptions;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal sealed record LinearPipelineEntries(
        IReadOnlyList<CoefficientEntry> Entries,
        int Omitted,
        bool Standardised);

    internal static class LinearPipelineCoefficients
    {
        public static LinearPipelineEntries Read(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var stored = ModelJson.Strings(envelope, "variablesInOrder");
            var inputCount = stored?.Length ?? ModelJson.Integer(envelope, "inputCount") ?? 0;

            if (inputCount < 1)
            {
                throw ModelJson.Inconsistent("the snapshot lists no variables");
            }

            var names = options.Variables(stored, inputCount);

            var weights = ModelJson.Doubles(payload, "weights") ??
                          throw ModelJson.Inconsistent("the payload has no weights");

            var indicators = ModelJson.Integers(payload, "indicatorInputs") ?? [];

            var standardise = ModelJson.Get(payload, "standardise") is { Type: JTokenType.Boolean } s &&
                              s.Value<bool>();

            var deviations = ModelJson.Doubles(payload, "standardDeviations");

            if (weights.Length != inputCount + indicators.Length)
            {
                throw ModelJson.Inconsistent("the payload has " + weights.Length + " weights for " + inputCount +
                                             " variables and " + indicators.Length + " missing indicators");
            }

            if (indicators.Any(i => i < 0 || i >= inputCount))
            {
                throw ModelJson.Inconsistent("a missing indicator refers to a variable that does not exist");
            }

            var (evidence, dropped) = Evidence(envelope);

            if (evidence.Count > 0)
            {
                return new LinearPipelineEntries(evidence, dropped, standardise);
            }

            var errors = LinearCoefficients.StandardErrors(payload, weights.Length);
            var entries = new List<CoefficientEntry>();
            var omitted = 0;

            for (var j = 0; j < inputCount; j++)
            {
                if (standardise && deviations is not null && j < deviations.Length && deviations[j] == 0)
                {
                    omitted++;
                    continue;
                }

                entries.Add(new CoefficientEntry(names[j], weights[j]) { StandardError = errors?[j] });
            }

            for (var k = 0; k < indicators.Length; k++)
            {
                entries.Add(new CoefficientEntry(names[indicators[k]] + " is missing", weights[inputCount + k])
                    { StandardError = errors?[inputCount + k] });
            }

            if (entries.Count == 0)
            {
                throw new ModelDiagramNotSupportedException(
                    "The model has no fitted variables to draw. " + ModelDiagramNotSupportedException.SummaryAdvice);
            }

            return new LinearPipelineEntries(entries, omitted, standardise);
        }

        private static (List<CoefficientEntry> Entries, int Dropped) Evidence(JObject envelope)
        {
            var result = new List<CoefficientEntry>();
            var dropped = 0;

            if (ModelJson.Get(ModelJson.Get(envelope, "evidence"), "coefficients") is not JArray rows)
            {
                return (result, dropped);
            }

            const string missingSuffix = "__missing";

            foreach (var row in rows)
            {
                var kind = ModelJson.String(row, "kind");

                if (string.Equals(kind, "Intercept", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = ModelJson.String(row, "name");
                var estimate = ModelJson.Number(row, "estimate");

                if (name is null || estimate is null)
                {
                    throw ModelJson.Inconsistent("an evidence coefficient needs a name and an estimate");
                }

                var error = ModelJson.Number(row, "standardError");
                var usable = error is { } e && double.IsFinite(e) && e > 0 ? e : (double?)null;

                if (usable is null && estimate == 0 &&
                    string.Equals(kind, "Predictor", StringComparison.OrdinalIgnoreCase))
                {
                    dropped++;
                    continue;
                }

                if (string.Equals(kind, "MissingIndicator", StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(missingSuffix, StringComparison.Ordinal))
                {
                    name = name[..^missingSuffix.Length] + " is missing";
                }

                result.Add(new CoefficientEntry(name, estimate.Value)
                {
                    StandardError = usable,
                    PValue = ModelJson.Number(row, "pValue")
                });
            }

            return (result, dropped);
        }

        public static string Omitted(int omitted)
        {
            return omitted > 0 ? " " + omitted + " constant variable" + (omitted == 1 ? "" : "s") + " omitted." : "";
        }
    }
}