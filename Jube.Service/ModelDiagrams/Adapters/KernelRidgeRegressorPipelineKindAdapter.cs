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
    public sealed class KernelRidgeRegressorPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "KernelRidgeRegressorPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var rows = ModelJson.Matrix(payload, "supportRows") ??
                       throw ModelJson.Inconsistent("the payload has no support rows");
            var alpha = ModelJson.Doubles(payload, "alpha") ?? throw ModelJson.Inconsistent("the payload has no alpha");

            if (rows.Length == 0 || alpha.Length != rows.Length || alpha.Any(a => !double.IsFinite(a)))
            {
                throw ModelJson.Inconsistent("the payload needs one finite dual weight per support row");
            }

            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), rows[0].Length);

            var kernel = SupportSetProjection.Kernel(ModelJson.String(payload, "kernel"),
                ModelJson.Number(payload, "gamma"),
                ModelJson.Integer(payload, "degree"));

            var mean = ModelJson.Number(payload, "mean");
            var indexes = Enumerable.Range(0, rows.Length).ToArray();

            return SupportSetProjection.Build(rows, names,
                [
                    new ProjectedGroup("Pulls the prediction down", indexes.Where(i => alpha[i] < 0).ToArray()),
                    new ProjectedGroup("Pulls the prediction up", indexes.Where(i => alpha[i] >= 0).ToArray())
                ],
                i => "Support row " + (i + 1) + ", dual weight " + ModelJson.Format(alpha[i]), options,
                "Kernel ridge regressor", rows.Length + " support rows, " + kernel,
                "A kernel ridge regressor predicts the training mean" +
                (mean is { } m && double.IsFinite(m) ? " " + ModelJson.Format(m) : "") +
                " plus a weighted sum of the " +
                kernel + " similarity to each stored training row. Each row is drawn on the standardised scale and " +
                "coloured by whether its dual weight pulls nearby predictions up or down; hover for the weight.");
        }
    }
}