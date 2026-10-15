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
    public sealed record NetworkNode(
        string Label,
        double? Bias = null,
        double? Emphasis = null,
        string? EmphasisText = null);

    public sealed record NetworkLayer
    {
        public string? Name { get; init; }
        public string? Activation { get; init; }
        public NetworkRole? Role { get; init; }
        public IReadOnlyList<NetworkNode> Nodes { get; init; } = [];
        public IReadOnlyList<IReadOnlyList<double>>? Weights { get; init; }
    }

    public sealed record NetworkDiagramSpec
    {
        public ChartOptions Options { get; init; } = new();
        public IReadOnlyList<NetworkLayer> Layers { get; init; } = [];
        public int MaxNodesPerLayer { get; init; } = DiagramLimits.DefaultMaxNodesPerLayer;
        public int MaxEdges { get; init; } = DiagramLimits.DefaultMaxNetworkEdges;
        public int WeightLabelLimit { get; init; } = 24;
        public string? EmphasisTitle { get; init; }
        public NetworkDetail Detail { get; init; } = NetworkDetail.Full;
    }
}