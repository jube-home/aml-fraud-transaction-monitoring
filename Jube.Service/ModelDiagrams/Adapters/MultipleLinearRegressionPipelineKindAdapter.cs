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
    public sealed class MultipleLinearRegressionPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "MultipleLinearRegressionPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var intercept = ModelJson.Number(payload, "intercept") ??
                            throw ModelJson.Inconsistent("the payload has no intercept");

            var read = LinearPipelineCoefficients.Read(envelope, payload, options);
            var scale = read.Standardised ? "Standardised coefficient" : "Coefficient per unit";
            var basis = read.Standardised
                ? "change in the prediction per standard deviation of each variable"
                : "change in the prediction per unit of each variable";

            var caption = "Intercept " + CoefficientMapping.Short(intercept) + ".";

            return CoefficientMapping.Build(read.Entries, options,
                "Multiple linear regression: " + scale.ToLowerInvariant(),
                caption, scale, false, false,
                scale + "s of a multiple linear regression model: the " + basis + ", holding the others fixed. " +
                caption + LinearPipelineCoefficients.Omitted(read.Omitted));
        }
    }
}