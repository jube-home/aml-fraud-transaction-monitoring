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

using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Exceptions;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class LogisticRegressionPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "LogisticRegressionPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
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

            var intercept = ModelJson.Number(payload, "intercept") ??
                            throw ModelJson.Inconsistent("the payload has no intercept");

            if (weights.Length != inputCount + indicators.Length)
            {
                throw ModelJson.Inconsistent("the payload has " + weights.Length + " weights for " + inputCount +
                                             " variables and " + indicators.Length + " missing indicators");
            }

            if (indicators.Any(i => i < 0 || i >= inputCount))
            {
                throw ModelJson.Inconsistent("a missing indicator refers to a variable that does not exist");
            }

            var evidence = Evidence(envelope);
            var caption = Caption(envelope, payload, intercept);

            if (evidence.Count > 0)
            {
                return CoefficientMapping.Build(evidence, options, "Logistic regression odds ratios", caption,
                    "Odds ratio",
                    false, true, "Odds ratios with 95% intervals of a logistic regression model. " + caption);
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

                entries.Add(Entry(names[j], weights[j], errors?[j]));
            }

            for (var k = 0; k < indicators.Length; k++)
            {
                entries.Add(Entry(names[indicators[k]] + " is missing", weights[inputCount + k],
                    errors?[inputCount + k]));
            }

            if (entries.Count == 0)
            {
                throw new ModelDiagramNotSupportedException(
                    "The model has no fitted variables to draw. " + ModelDiagramNotSupportedException.SummaryAdvice);
            }

            var scale = standardise ? "Standardised log-odds" : "Log-odds per unit";
            var basis = standardise
                ? "per standard deviation of each variable"
                : "on the original scale of each variable";

            var note = omitted > 0
                ? " " + omitted + " constant variable" + (omitted == 1 ? "" : "s") + " omitted."
                : "";

            return CoefficientMapping.Build(entries, options, "Logistic regression: " + scale.ToLowerInvariant(),
                caption,
                scale, true, false, scale + " " + basis + " of a logistic regression model. " + caption + note);
        }

        private static CoefficientEntry Entry(string name, double estimate, double? error)
        {
            return new CoefficientEntry(name, estimate) { StandardError = error };
        }

        internal static string Caption(JObject envelope, JObject payload, double intercept)
        {
            var parts = new List<string> { "Intercept " + CoefficientMapping.Short(intercept) };

            if (ModelJson.Number(payload, "trainingPrior") is { } prior)
            {
                parts.Add("prior " + CoefficientMapping.Short(prior));
            }

            if (ModelJson.Number(payload, "baseRate") is { } rate)
            {
                parts.Add("base rate " + CoefficientMapping.Short(rate));
            }

            var text = string.Join(", ", parts) + ".";

            if (ModelJson.Get(envelope, "calibrator") is JObject calibrator)
            {
                var method = ModelJson.String(calibrator, "method");
                var p = ModelJson.Doubles(calibrator, "parameters");

                var label = method is null || method.Length > 24 || !method.All(char.IsAsciiLetterOrDigit)
                    ? "Custom"
                    : method;

                text += " Calibrator " + label;

                if (label == "Platt" && p is { Length: >= 2 })
                {
                    text += " (slope " + CoefficientMapping.Short(p[0]) + ", intercept " +
                            CoefficientMapping.Short(p[1]) +
                            ")";
                }

                text += ".";
            }
            else
            {
                text += " No calibrator.";
            }

            return text;
        }

        private static List<CoefficientEntry> Evidence(JObject envelope)
        {
            var result = new List<CoefficientEntry>();

            if (ModelJson.Get(ModelJson.Get(envelope, "evidence"), "coefficients") is not JArray rows)
            {
                return result;
            }

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

                var oddsRatio = ModelJson.Number(row, "oddsRatio") ?? Math.Exp(estimate.Value);
                var low = ModelJson.Number(row, "oddsRatioLower95");
                var high = ModelJson.Number(row, "oddsRatioUpper95");

                result.Add(new CoefficientEntry(name, oddsRatio)
                {
                    Low = low.HasValue && high.HasValue ? low : null,
                    High = low.HasValue && high.HasValue ? high : null,
                    PValue = ModelJson.Number(row, "pValue")
                });
            }

            return result;
        }
    }
}