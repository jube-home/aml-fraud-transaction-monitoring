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
    public sealed class OneClassSupportVectorAnomalyPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "OneClassSupportVectorAnomalyPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var vectors = ModelJson.Matrix(payload, "supportVectors") ??
                          throw ModelJson.Inconsistent("the payload has no support vectors");
            var alphas = ModelJson.Doubles(payload, "alphas") ??
                         throw ModelJson.Inconsistent("the payload has no alphas");

            if (vectors.Length == 0 || alphas.Length != vectors.Length || alphas.Any(a => !double.IsFinite(a)))
            {
                throw ModelJson.Inconsistent("the payload needs one finite alpha per support vector");
            }

            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), vectors[0].Length);
            var kernel = SupportSetProjection.Kernel("Gaussian", ModelJson.Number(payload, "gamma"), null);

            return SupportSetProjection.Build(vectors, names,
                [new ProjectedGroup("Support vectors", Enumerable.Range(0, vectors.Length).ToArray())],
                i => "Support vector " + (i + 1) + ", alpha " + ModelJson.Format(alphas[i]), options,
                "One-class support vector anomaly model", vectors.Length + " support vectors, " + kernel,
                "A one-class support vector machine draws a boundary around the normal training rows. The boundary " +
                "is a weighted sum of " + kernel +
                " similarities to these support vectors, the rows on its edge; a new " +
                "row far from all of them falls outside and scores as anomalous. Each is drawn on the standardised " +
                "scale; hover for its alpha weight.");
        }
    }
}