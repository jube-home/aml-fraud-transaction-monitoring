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

using System.ComponentModel;
using MessagePack;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.Graph
{
    [MessagePackObject]
    public class GraphNodeDto
    {
        [Description("Server-assigned identifier of the node.")]
        [Key(0)]
        public long Id { get; set; }

        [Description("The report key name, which is also the node type.")]
        [Key(1)]
        public string? Key { get; set; }

        [Description("The key value.")]
        [Key(2)]
        public string? Value { get; set; }

        [Description("True when the node is a hub.")]
        [Key(3)]
        public bool Hub { get; set; }

        [Key(4)]
        public int HubOverrideId { get; set; }

        [Description("Distinct neighbours over all edges.")]
        [Key(5)]
        public int Degree { get; set; }

        [Description("Distinct neighbours over money-flow edges.")]
        [Key(6)]
        public int DegreeMoney { get; set; }

        [Description("Distinct neighbours over identity-link edges.")]
        [Key(7)]
        public int DegreeIdentity { get; set; }

        [Description("Neighbours that are hubs.")]
        [Key(8)]
        public int HubNeighbourCount { get; set; }

        [Description("Connected component, when computed.")]
        [Key(9)]
        public long? ComponentId { get; set; }

        [Description("Size of the connected component, when computed.")]
        [Key(10)]
        public int? ComponentSize { get; set; }

        [Description("Community, when computed.")]
        [Key(11)]
        public long? CommunityId { get; set; }

        [Description("k-core number, when computed.")]
        [Key(12)]
        public int? CoreNumber { get; set; }

        [Description("PageRank on the hub-pruned tenant graph, when computed.")]
        [Key(13)]
        public double? PageRank { get; set; }

        [Description("Local clustering coefficient, when computed.")]
        [Key(14)]
        public double? ClusteringCoefficient { get; set; }

        [Description("Hop distance to the nearest confirmed true positive, when computed and within the cap.")]
        [Key(15)]
        public int? DistanceToConfirmed { get; set; }

        [Description("Confirmed true positives within one hop, when computed.")]
        [Key(16)]
        public int? ConfirmedWithin1 { get; set; }

        [Description("Confirmed true positives within two hops, when computed.")]
        [Key(17)]
        public int? ConfirmedWithin2 { get; set; }

        [Description("Confirmed true positives within three hops, when computed.")]
        [Key(18)]
        public int? ConfirmedWithin3 { get; set; }

        [Description("Seed propagation score, when computed. A score, not a probability.")]
        [Key(19)]
        public double? PropagationScore { get; set; }

        [Key(20)]
        public double? HubScore { get; set; }

        [Key(21)]
        public double? AuthorityScore { get; set; }

        [Key(22)]
        public int? DistanceToNotConfirmed { get; set; }

        [Key(23)]
        public double? Interest { get; set; }

        [Key(24)]
        public DateTimeOffset? ComputedDate { get; set; }

        [Key(25)]
        public string? NearestTag { get; set; }

        [Key(26)]
        public int? NearestTagDistance { get; set; }

        [Key(27)]
        public List<GraphNodeTagDto> Tags { get; set; } = [];

        [Description("Earliest archived transaction the node appeared on (UTC).")]
        [Key(28)]
        public DateTimeOffset FirstSeen { get; set; }

        [Description("Latest archived transaction the node appeared on (UTC).")]
        [Key(29)]
        public DateTimeOffset LastSeen { get; set; }

        [Description("Hops from the seed in this neighbourhood; 0 for the seed.")]
        [Key(30)]
        public int Depth { get; set; }

        [Description("The node this node was reached through, when not the seed.")]
        [Key(31)]
        public long? ViaNodeId { get; set; }

        [Description("True for the seed node.")]
        [Key(32)]
        public bool Seed { get; set; }

        [Description("Cases on this node's key.")]
        [Key(33)]
        public GraphNodeCasesDto Cases { get; set; } = new();
    }
}
