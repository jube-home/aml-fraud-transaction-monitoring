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
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal static class BayesStructure
    {
        public const string ClassId = "class";

        public static GraphDiagramBuilder Start(IReadOnlyList<string> features, string classDetail)
        {
            var builder = Diagram.Graph().Node(ClassId, "Class", GraphNodeRole.Class, classDetail);

            for (var i = 0; i < features.Count; i++)
            {
                builder.Node(Feature(i), features[i]);
            }

            return builder;
        }

        public static string Feature(int index)
        {
            return "f" + index;
        }

        public static string Priors(JToken model)
        {
            var priors = ModelJson.Doubles(model, "priors");

            return priors is null
                ? "Class prior unknown"
                : "Class priors: " + string.Join(", ", priors.Select(ModelJson.Format));
        }

        public static bool IsSquare(double[][] matrix, int size)
        {
            return matrix.Length == size && matrix.All(r => r.Length == size);
        }

        public static GraphDiagram Finish(GraphDiagramBuilder builder, ModelDiagramOptions options, string title,
            string subtitle, string weightName, string description)
        {
            var built = builder.WeightName(weightName).Build();
            options.RequireWithin(built.Spec.Nodes.Count + built.Spec.Edges.Count, "graph nodes and edges");

            return new GraphDiagram(built.Spec with { Options = options.ToChartOptions(title, subtitle, description) });
        }

        public static bool HasNumbers(JToken model, string name)
        {
            return ModelJson.Doubles(model, name) is not null;
        }
    }
}