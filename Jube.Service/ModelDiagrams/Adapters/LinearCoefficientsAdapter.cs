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
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class LinearCoefficientsAdapter : IModelDiagramAdapter
    {
        public string Name => "LinearCoefficients";

        public double? Detect(JToken model)
        {
            return model is JObject
                   && ModelJson.Doubles(model, "weights") is { Length: > 0 }
                   && ModelJson.Number(model, "intercept") is not null
                   && !ModelJson.Has(model, "payload")
                ? 0.9
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var weights = ModelJson.Doubles(model, "weights")!;
            var intercept = ModelJson.Number(model, "intercept")!.Value;
            var names = options.Variables(null, weights.Length);
            var errors = LinearCoefficients.StandardErrors(model, weights.Length);

            var entries = weights.Select((w, i) => new CoefficientEntry(names[i], w) { StandardError = errors?[i] })
                .ToArray();

            var logistic = ModelJson.Has(model, "link") || ModelJson.Has(model, "linear") ||
                           SerialisationMarkers.Has(ModelJson.String(model, "$type"),
                               SerialisationMarkers.LogisticRegression);

            if (logistic)
            {
                return CoefficientMapping.Build(entries, options, "Logistic regression coefficients",
                    "Log-odds per unit of each variable. Intercept " + ModelJson.Format(intercept) + ".",
                    "Log-odds per unit",
                    true, false, "Log-odds coefficients of a logistic regression model.");
            }

            return CoefficientMapping.Build(entries, options, "Linear regression coefficients",
                "Change in the outcome per unit of each variable. Intercept " + ModelJson.Format(intercept) + ".",
                "Coefficient", false, false, "Coefficients of a multiple linear regression model.");
        }
    }
}