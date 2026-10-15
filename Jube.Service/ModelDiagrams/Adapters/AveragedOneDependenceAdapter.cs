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
    public sealed class AveragedOneDependenceAdapter : IModelDiagramAdapter
    {
        public string Name => "AveragedOneDependence";

        public double? Detect(JToken model)
        {
            return model is JObject
                   && ModelJson.Doubles(model, "estimatorWeights") is not null
                   && ModelJson.Matrix(model, "superParentSupport") is not null
                   && ModelJson.Integers(model, "numberOfStates") is not null
                   && ModelJson.Has(model, "pairConditionals")
                ? 0.95
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var weights = ModelJson.Doubles(model, "estimatorWeights")!;
            var count = weights.Length;

            if (count == 0 || ModelJson.Integers(model, "numberOfStates")!.Length != count)
            {
                throw ModelJson.Inconsistent("the estimator weights and the states must describe the same features");
            }

            var features = options.Variables(null, count);
            var builder = BayesStructure.Start(features, BayesStructure.Priors(model));
            var used = 0;

            for (var j = 0; j < count; j++)
            {
                builder.Edge(BayesStructure.ClassId, BayesStructure.Feature(j));
            }

            for (var j = 0; j < count; j++)
            {
                if (!(weights[j] > 0) || !double.IsFinite(weights[j]))
                {
                    continue;
                }

                used++;

                for (var i = 0; i < count; i++)
                {
                    if (i != j)
                    {
                        builder.Edge(BayesStructure.Feature(j), BayesStructure.Feature(i), weights[j]);
                    }
                }
            }

            if (used == 0)
            {
                throw new ModelDiagramNotSupportedException(
                    "Every super-parent estimator of this model was dropped for lack of support, so there is no structure to draw. " +
                    ModelDiagramNotSupportedException.SummaryAdvice);
            }

            return BayesStructure.Finish(builder, options, "Averaged one-dependence estimators",
                used + " super-parent estimator" + (used == 1 ? "" : "s") + ", each the parent of every other feature",
                "Estimator weight",
                "Structure of averaged one-dependence estimators over " + count + " features.");
        }
    }
}