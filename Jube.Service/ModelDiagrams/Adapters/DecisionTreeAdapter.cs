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
    public sealed class DecisionTreeAdapter : IModelDiagramAdapter
    {
        public string Name => "DecisionTree";

        public double? Detect(JToken model)
        {
            return DecisionTreeReader.LooksLikeTree(model) && !ModelJson.Has(model, "trees") ? 0.95 : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var features = DecisionTreeReader.Features(model, options);
            var classes = DecisionTreeReader.Classes(model);
            var reader = new DecisionTreeReader(features, classes);
            var root = reader.Read(model);
            options.RequireWithin(reader.Nodes, "tree nodes");

            return new TreeDiagram(new TreeDiagramSpec
            {
                Options = options.ToChartOptions("Decision tree", reader.Nodes + " nodes",
                    "Decision tree with " + reader.Nodes + " nodes over " + features.Length + " attributes."),
                Root = root,
                ClassNames = classes
            });
        }
    }
}