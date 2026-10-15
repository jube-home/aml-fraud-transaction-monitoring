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
    public sealed class KDependenceBayesAdapter : IModelDiagramAdapter
    {
        public string Name => "KDependenceBayes";

        public double? Detect(JToken model)
        {
            return model is JObject
                   && ModelJson.Integers(model, "order") is not null
                   && ModelJson.IntegerRows(model, "parents") is not null
                   && ModelJson.Integers(model, "numberOfStates") is not null
                   && ModelJson.Matrix(model, "binEdges") is not null
                   && !ModelJson.Has(model, "parentWeights")
                ? 0.95
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var order = ModelJson.Integers(model, "order")!;
            var parents = ModelJson.IntegerRows(model, "parents")!;
            var count = order.Length;

            if (count == 0 || parents.Length != count)
            {
                throw ModelJson.Inconsistent("the order and the parents must describe the same features");
            }

            if (order.OrderBy(o => o).Where((o, i) => o != i).Any())
            {
                throw ModelJson.Inconsistent("the order is not a permutation of the features");
            }

            var features = options.Variables(null, count);
            var builder = BayesStructure.Start(features, BayesStructure.Priors(model));

            for (var j = 0; j < count; j++)
            {
                builder.Edge(BayesStructure.ClassId, BayesStructure.Feature(j));

                foreach (var p in parents[j])
                {
                    if (p < 0 || p >= count || p == j)
                    {
                        throw ModelJson.Inconsistent("feature " + j + " has an invalid parent " + p);
                    }

                    builder.Edge(BayesStructure.Feature(p), BayesStructure.Feature(j));
                }
            }

            var k = ModelJson.Integer(model, "k") ?? parents.Max(p => p.Length);

            return BayesStructure.Finish(builder, options, "k-dependence Bayes",
                "Each feature has the class and up to " + k + " earlier features as parents", "Weight",
                "Structure of a k-dependence Bayesian classifier with k equal to " + k + " over " + count +
                " features.");
        }
    }
}