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

using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class RandomForestAdapter : IModelDiagramAdapter
    {
        public string Name => "RandomForest";

        public double? Detect(JToken model)
        {
            return model is JObject && ModelJson.Get(model, "trees") is JArray { Count: > 0 } trees
                                    && trees.All(DecisionTreeReader.LooksLikeTree)
                ? 0.95
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var trees = (JArray)ModelJson.Get(model, "trees")!;
            var features = DecisionTreeReader.Features(model, options);
            var classes = DecisionTreeReader.Classes(model);
            var reader = new DecisionTreeReader(features, classes);
            var specs = new List<TreeDiagramSpec>();
            var shown = Math.Min(trees.Count, 6);

            for (var t = 0; t < trees.Count; t++)
            {
                var root = reader.Read(trees[t]);
                options.RequireWithin(reader.Nodes, "forest nodes");

                if (t < shown)
                {
                    specs.Add(new TreeDiagramSpec { Root = root, ClassNames = classes });
                }
            }

            var splits = reader.SplitCounts.Sum();

            var importances = splits == 0
                ? []
                : features.Select((f, i) => new FeatureImportance(f, (double)reader.SplitCounts[i] / splits))
                    .Where(i => i.Value > 0).ToArray();

            return new ForestOfTrees(new ForestOfTreesSpec
            {
                Options = options.ToChartOptions("Random forest",
                    trees.Count + " trees, " + reader.Nodes + " nodes in total",
                    "Random forest of " + trees.Count + " decision trees over " + features.Length + " attributes."),
                Trees = specs,
                ClassNames = classes,
                Importances = importances,
                TotalTrees = trees.Count,
                ImportanceName = "Share of splits"
            });
        }
    }
}