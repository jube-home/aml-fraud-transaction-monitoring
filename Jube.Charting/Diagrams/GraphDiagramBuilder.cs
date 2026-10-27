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

using Jube.Charting.Core;
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    public sealed class GraphDiagramBuilder : ChartBuilder<GraphDiagramBuilder>
    {
        private readonly List<GraphEdge> edges = [];
        private readonly List<GraphNode> nodes = [];
        private bool directed = true;
        private GraphLayoutKind layout = GraphLayoutKind.Auto;
        private string weightName = "Weight";
        private string[] groupOrder = [];
        private double maxExtent = 3000;

        public GraphDiagramBuilder Node(string id, string? label = null, GraphNodeRole role = GraphNodeRole.Feature,
            string? detail = null, string? group = null, bool dashed = false)
        {
            nodes.Add(new GraphNode(id, label ?? id, role, detail) { Group = group, Dashed = dashed });

            return this;
        }

        public GraphDiagramBuilder Edge(string from, string to, double? weight = null, double? probability = null,
            string? label = null, bool dashed = false)
        {
            edges.Add(new GraphEdge(from, to)
                { Weight = weight, Probability = probability, Label = label, Dashed = dashed });

            return this;
        }

        public GraphDiagramBuilder MaxExtent(double pixels)
        {
            maxExtent = Math.Clamp(pixels, 600, 12000);

            return this;
        }

        public GraphDiagramBuilder GroupOrder(params string[] order)
        {
            groupOrder = order;

            return this;
        }

        public GraphDiagramBuilder Layout(GraphLayoutKind kind)
        {
            layout = kind;

            return this;
        }

        public GraphDiagramBuilder Undirected()
        {
            directed = false;

            return this;
        }

        public GraphDiagramBuilder WeightName(string name)
        {
            weightName = name;

            return this;
        }

        public GraphDiagram Build()
        {
            return new GraphDiagram(new GraphDiagramSpec
            {
                Options = Options, Nodes = [.. nodes], Edges = [.. edges], Layout = layout,
                Directed = directed, WeightName = weightName, GroupOrder = groupOrder, MaxExtent = maxExtent
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}