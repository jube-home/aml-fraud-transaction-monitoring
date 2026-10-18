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

using System.Globalization;
using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class TreeAugmentedNaiveBayesClassifierPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "TreeAugmentedNaiveBayesClassifierPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var parents = ModelJson.Integers(payload, "parents") ??
                          throw ModelJson.Inconsistent("the payload has no parents");

            var count = parents.Length;

            if (count == 0)
            {
                throw ModelJson.Inconsistent("the payload has no variables");
            }

            TreeAugmentedNaiveBayesAdapter.Validate(parents);
            var slope0 = ModelJson.Doubles(payload, "slope0");
            var slope1 = ModelJson.Doubles(payload, "slope1");
            var slopes = slope0 is not null && slope1 is not null && slope0.Length == count && slope1.Length == count;
            var priorLogOdds = ModelJson.Number(payload, "priorLogOdds");

            var detail = priorLogOdds is { } p && double.IsFinite(p)
                ? "Class prior P(1) " + CoefficientMapping.Short(1 / (1 + Math.Exp(-p)))
                : "Class prior unknown";

            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), count);
            var builder = BayesStructure.Start(names, detail);

            for (var i = 0; i < count; i++)
            {
                builder.Edge(BayesStructure.ClassId, BayesStructure.Feature(i));
            }

            var linked = 0;

            for (var i = 0; i < count; i++)
            {
                if (parents[i] < 0)
                {
                    continue;
                }

                linked++;

                builder.Edge(BayesStructure.Feature(parents[i]), BayesStructure.Feature(i), label: slopes
                    ? "slope " + Significant(slope0![i]) + " / " + Significant(slope1![i])
                    : null);
            }

            return BayesStructure.Finish(builder, options, "Tree-augmented naive Bayes",
                "The class and at most one other variable are the parents of each variable",
                "Parent variable",
                "Structure of a tree-augmented naive Bayes classifier over " + count + " variables: " + linked +
                " variables depend on a parent variable as well as on the class. Each such variable is normal within " +
                "a class with a mean linear in its parent; the edge label gives that slope for class 0 / class 1.");
        }

        private static string Significant(double value)
        {
            return value.ToString("G3", CultureInfo.InvariantCulture);
        }
    }
}