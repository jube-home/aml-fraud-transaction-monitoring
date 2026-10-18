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
    public sealed class KernelSupportVectorClassifierPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "KernelSupportVectorClassifierPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var vectors = ModelJson.Matrix(payload, "supportVectors") ??
                          throw ModelJson.Inconsistent("the payload has no support vectors");
            var coefficients = ModelJson.Doubles(payload, "coefficients") ??
                               throw ModelJson.Inconsistent("the payload has no coefficients");

            if (vectors.Length == 0 || coefficients.Length != vectors.Length ||
                coefficients.Any(c => !double.IsFinite(c)))
            {
                throw ModelJson.Inconsistent("the payload needs one finite coefficient per support vector");
            }

            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), vectors[0].Length);

            var kernel = SupportSetProjection.Kernel(ModelJson.String(payload, "kernel"),
                ModelJson.Number(payload, "gamma"),
                ModelJson.Integer(payload, "degree"));

            var bias = ModelJson.Number(payload, "bias");
            var indexes = Enumerable.Range(0, vectors.Length).ToArray();

            return SupportSetProjection.Build(vectors, names,
                [
                    new ProjectedGroup("Class 0 support vectors", indexes.Where(i => coefficients[i] < 0).ToArray()),
                    new ProjectedGroup("Class 1 support vectors", indexes.Where(i => coefficients[i] >= 0).ToArray())
                ],
                i => "Support vector " + (i + 1) + ", coefficient " + ModelJson.Format(coefficients[i]),
                options, "Kernel support vector classifier",
                vectors.Length + " support vectors, " + kernel,
                "The decision function of a kernel support vector classifier is the bias" +
                (bias is { } b && double.IsFinite(b) ? " " + ModelJson.Format(b) : "") +
                " plus a weighted sum of the " +
                kernel +
                " similarity to each support vector: the training rows on or inside the margin. Each is drawn " +
                "on the standardised scale and coloured by the class it pulls towards; hover for its coefficient.");
        }
    }
}