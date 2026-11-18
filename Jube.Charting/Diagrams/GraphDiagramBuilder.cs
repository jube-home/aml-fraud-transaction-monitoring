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
        private GraphDirection direction;
        private int maxPerLayer;
        private readonly List<GraphAnchor> anchors = [];
        private readonly Dictionary<string, int> depths = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> lanes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> times = new(StringComparer.Ordinal);
        private string? seedId;
        private GraphMeasure sizeBy;

        public GraphDiagramBuilder Node(string id, string? label = null, GraphNodeRole role = GraphNodeRole.Feature,
            string? detail = null, string? group = null, bool dashed = false)
        {
            nodes.Add(new GraphNode(id, label ?? id, role, detail) { Group = group, Dashed = dashed });

            return this;
        }

        public GraphDiagramBuilder Node(string id, string? label, GraphNodeShape shape, double? size = null,
            string? group = null, IReadOnlyList<GraphBadge>? badges = null, string? detail = null, string? icon = null)
        {
            nodes.Add(new GraphNode(id, label ?? id, GraphNodeRole.Other, detail)
                { Group = group, Shape = shape, Size = size, Badges = badges, Icon = icon });

            return this;
        }

        public GraphDiagramBuilder Node(GraphNode node)
        {
            nodes.Add(node);

            return this;
        }

        public GraphDiagramBuilder Edge(string from, string to, double? weight = null, double? probability = null,
            string? label = null, bool dashed = false)
        {
            edges.Add(new GraphEdge(from, to)
                { Weight = weight, Probability = probability, Label = label, Dashed = dashed });

            return this;
        }

        public GraphDiagramBuilder Edge(string from, string to, DateTimeOffset first, DateTimeOffset? last = null,
            int count = 1, double? amount = null, string? id = null)
        {
            edges.Add(new GraphEdge(from, to)
                { Id = id, First = first, Last = last ?? first, Count = count, Weight = amount, Directed = true });

            return this;
        }

        public GraphDiagramBuilder Edge(GraphEdge edge)
        {
            edges.Add(edge);

            return this;
        }

        public GraphDiagramBuilder Anchor(string id, double x, double y)
        {
            anchors.Add(new GraphAnchor(id, x, y));

            return this;
        }

        public GraphDiagramBuilder Anchors(IEnumerable<GraphAnchor> positions)
        {
            anchors.AddRange(positions);

            return this;
        }

        public GraphDiagramBuilder Seed(string id)
        {
            seedId = id;

            return this;
        }

        public GraphDiagramBuilder Depth(string id, int depth)
        {
            depths[id] = depth;

            return this;
        }

        public GraphDiagramBuilder Lane(string id, string lane)
        {
            lanes[id] = lane;

            return this;
        }

        public GraphDiagramBuilder Time(string id, DateTimeOffset when)
        {
            times[id] = when;

            return this;
        }

        public GraphDiagramBuilder SizeBy(GraphMeasure measure)
        {
            sizeBy = measure;

            return this;
        }

        public GraphDiagramBuilder MaxExtent(double pixels)
        {
            maxExtent = Math.Clamp(pixels, 600, 12000);

            return this;
        }

        public GraphDiagramBuilder Direction(GraphDirection value)
        {
            direction = value;

            return this;
        }

        public GraphDiagramBuilder MaxPerLayer(int count)
        {
            maxPerLayer = Math.Max(0, count);

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
                Directed = directed, WeightName = weightName, GroupOrder = groupOrder, MaxExtent = maxExtent,
                Direction = direction, MaxPerLayer = maxPerLayer,
                Anchors = anchors.Count == 0 ? null : [.. anchors], SeedId = seedId,
                Depths = depths.Count == 0 ? null : new Dictionary<string, int>(depths, StringComparer.Ordinal),
                Lanes = lanes.Count == 0 ? null : new Dictionary<string, string>(lanes, StringComparer.Ordinal),
                Times = times.Count == 0
                    ? null
                    : new Dictionary<string, DateTimeOffset>(times, StringComparer.Ordinal),
                Size = sizeBy
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}