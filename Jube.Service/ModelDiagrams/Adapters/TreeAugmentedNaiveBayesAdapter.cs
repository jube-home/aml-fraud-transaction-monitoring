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
    public sealed class TreeAugmentedNaiveBayesAdapter : IModelDiagramAdapter
    {
        public string Name => "TreeAugmentedNaiveBayes";

        public double? Detect(JToken model)
        {
            return model is JObject
                   && BayesStructure.HasNumbers(model, "priors")
                   && ModelJson.Integers(model, "parents") is not null
                   && ModelJson.Matrix(model, "intercepts") is not null
                   && ModelJson.Matrix(model, "slopes") is not null
                   && ModelJson.Matrix(model, "variances") is not null
                   && ModelJson.Matrix(model, "mutualInformation") is not null
                ? 0.95
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var parents = ModelJson.Integers(model, "parents")!;
            var information = ModelJson.Matrix(model, "mutualInformation")!;
            var count = parents.Length;

            if (count == 0 || !ModelJson.IsRectangular(information, count, count))
            {
                throw ModelJson.Inconsistent("the mutual information matrix must be " + count + " by " + count);
            }

            Validate(parents);
            var features = options.Variables(null, count);
            var builder = BayesStructure.Start(features, BayesStructure.Priors(model));

            for (var i = 0; i < count; i++)
            {
                builder.Edge(BayesStructure.ClassId, BayesStructure.Feature(i));
            }

            for (var i = 0; i < count; i++)
            {
                if (parents[i] >= 0)
                {
                    var weight = information[i][parents[i]];

                    builder.Edge(BayesStructure.Feature(parents[i]), BayesStructure.Feature(i),
                        double.IsFinite(weight) && weight >= 0 ? weight : null);
                }
            }

            return BayesStructure.Finish(builder, options, "Tree-augmented naive Bayes",
                "The class and at most one other feature are the parents of each feature",
                "Conditional mutual information",
                "Structure of a tree-augmented naive Bayes classifier over " + count + " features.");
        }

        internal static void Validate(int[] parents)
        {
            for (var i = 0; i < parents.Length; i++)
            {
                if (parents[i] < -1 || parents[i] >= parents.Length || parents[i] == i)
                {
                    throw ModelJson.Inconsistent("feature " + i + " has an invalid parent " + parents[i]);
                }

                var steps = 0;

                for (var p = parents[i]; p >= 0; p = parents[p])
                {
                    if (++steps > parents.Length)
                    {
                        throw ModelJson.Inconsistent("the parents contain a cycle");
                    }
                }
            }
        }
    }
}