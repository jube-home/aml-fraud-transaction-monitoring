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
    public class GraphNeighbourhoodDto
    {
        [Description("The seed node, or null when the key value has no node yet.")]
        [Key(0)]
        public GraphNodeDto? Seed { get; set; }

        [Description("Nodes drawn, the seed included.")]
        [Key(1)]
        public List<GraphNodeDto> Nodes { get; set; } = [];

        [Description("Edges among the drawn nodes.")]
        [Key(2)]
        public List<GraphEdgeDto> Edges { get; set; } = [];

        [Description("Clusters collapsed because the cap was reached.")]
        [Key(3)]
        public List<GraphComboDto> Combos { get; set; } = [];

        [Description("Edge definitions of the registry, for the legend and the filter.")]
        [Key(4)]
        public List<GraphEdgeDefinitionDto> Definitions { get; set; } = [];

        [Description("Hub neighbours hidden by the hub policy.")]
        [Key(5)]
        public int HiddenHubs { get; set; }

        [Description("Nodes beyond the cap, collapsed into the combos.")]
        [Key(6)]
        public int OverCap { get; set; }

        [Description("Start of the window applied (UTC).")]
        [Key(7)]
        public DateTimeOffset From { get; set; }

        [Description("End of the window applied (UTC).")]
        [Key(8)]
        public DateTimeOffset To { get; set; }

        [Description("Hops applied.")]
        [Key(9)]
        public int Hops { get; set; }

        [Description("When the edge build last ran for the registry (UTC); null when it has not run.")]
        [Key(10)]
        public DateTimeOffset? BuiltDate { get; set; }

        [Description("True when the registry has no active edge definitions.")]
        [Key(11)]
        public bool NoDefinitions { get; set; }

        [Key(12)]
        public bool Relevance { get; set; }
    }
}
