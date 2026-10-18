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

using Jube.Charting.Core.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Charting.Diagrams.Models
{
    public sealed record GraphNode(
        string Id,
        string Label,
        GraphNodeRole Role = GraphNodeRole.Feature,
        string? Detail = null);

    public sealed record GraphEdge(string From, string To)
    {
        public double? Weight { get; init; }
        public double? Probability { get; init; }
        public string? Label { get; init; }
        public bool Dashed { get; init; }
    }

    public sealed record GraphDiagramSpec
    {
        public ChartOptions Options { get; init; } = new();
        public IReadOnlyList<GraphNode> Nodes { get; init; } = [];
        public IReadOnlyList<GraphEdge> Edges { get; init; } = [];
        public GraphLayoutKind Layout { get; init; } = GraphLayoutKind.Auto;
        public bool Directed { get; init; } = true;
        public string WeightName { get; init; } = "Weight";
        public int EdgeLabelLimit { get; init; } = 40;
    }
}