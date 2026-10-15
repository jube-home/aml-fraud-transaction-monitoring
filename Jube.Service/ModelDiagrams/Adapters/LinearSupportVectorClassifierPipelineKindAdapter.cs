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
    public sealed class LinearSupportVectorClassifierPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "LinearSupportVectorClassifierPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var bias = ModelJson.Number(payload, "bias") ?? throw ModelJson.Inconsistent("the payload has no bias");
            var read = LinearPipelineCoefficients.Read(envelope, payload, options);
            var scale = read.Standardised ? "Standardised weight" : "Weight per unit";
            var caption = "Bias " + CoefficientMapping.Short(bias) + ".";

            return CoefficientMapping.Build(read.Entries, options,
                "Linear support vector classifier: " + scale.ToLowerInvariant() + "s", caption, scale, false, false,
                "Weights of the separating hyperplane of a linear support vector classifier" +
                (read.Standardised ? " on standardised variables" : "") +
                ": a row's margin is the bias plus each weight times its value, and a positive margin is class 1. " +
                caption + LinearPipelineCoefficients.Omitted(read.Omitted));
        }
    }
}