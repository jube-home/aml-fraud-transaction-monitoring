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
    public sealed class HiddenNaiveBayesAdapter : IModelDiagramAdapter
    {
        public string Name => "HiddenNaiveBayes";

        public double? Detect(JToken model)
        {
            return model is JObject
                   && ModelJson.Matrix(model, "parentWeights") is not null
                   && ModelJson.Integers(model, "numberOfStates") is not null
                   && ModelJson.Has(model, "pairConditionals")
                ? 0.95
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var weights = ModelJson.Matrix(model, "parentWeights")!;
            var count = weights.Length;

            if (count == 0 || !ModelJson.IsRectangular(weights, count, count))
            {
                throw ModelJson.Inconsistent("the parent weights must be a square matrix");
            }

            var features = options.Variables(null, count);
            var builder = BayesStructure.Start(features, BayesStructure.Priors(model));

            for (var i = 0; i < count; i++)
            {
                builder.Edge(BayesStructure.ClassId, BayesStructure.Feature(i));
            }

            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < count; j++)
                {
                    if (i != j && weights[i][j] > 0 && double.IsFinite(weights[i][j]))
                    {
                        builder.Edge(BayesStructure.Feature(j), BayesStructure.Feature(i), weights[i][j]);
                    }
                }
            }

            return BayesStructure.Finish(builder, options, "Hidden naive Bayes",
                "Each feature has the class and a weighted hidden parent mixing the other features",
                "Hidden parent weight",
                "Structure of a hidden naive Bayes classifier over " + count + " features.");
        }
    }
}