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
    public class GraphEdgeDto
    {
        [Description("Server-assigned identifier of the edge.")]
        [Key(0)]
        public long Id { get; set; }

        [Description("Guid of the edge definition that derived this edge.")]
        [Key(1)]
        public Guid DefinitionGuid { get; set; }

        [Description("Name of the edge definition.")]
        [Key(2)]
        public string? DefinitionName { get; set; }

        [Description("Kind of edge, as GraphEdgeKind.")]
        [Key(3)]
        public int KindId { get; set; }

        [Description("From node.")]
        [Key(4)]
        public long FromNodeId { get; set; }

        [Description("To node.")]
        [Key(5)]
        public long ToNodeId { get; set; }

        [Description("Archived transactions behind the edge over its lifetime.")]
        [Key(6)]
        public int Count { get; set; }

        [Description("Sum of the amount over its lifetime, for money flow.")]
        [Key(7)]
        public double AmountSum { get; set; }

        [Description("Transactions behind the edge within the requested window.")]
        [Key(8)]
        public int WindowCount { get; set; }

        [Description("Sum of the amount within the requested window.")]
        [Key(9)]
        public double WindowAmount { get; set; }

        [Description("Earliest transaction behind the edge (UTC).")]
        [Key(10)]
        public DateTimeOffset FirstSeen { get; set; }

        [Description("Latest transaction behind the edge (UTC).")]
        [Key(11)]
        public DateTimeOffset LastSeen { get; set; }

        [Description("Earliest transaction within the window (UTC).")]
        [Key(12)]
        public DateTimeOffset? WindowFirst { get; set; }

        [Description("Latest transaction within the window (UTC).")]
        [Key(13)]
        public DateTimeOffset? WindowLast { get; set; }

        [Description("The edge weight; see the design document for the formula.")]
        [Key(14)]
        public double Weight { get; set; }

        [Description("Specificity term of the weight: small for an identifier shared by many.")]
        [Key(15)]
        public double Specificity { get; set; }

        [Description("True when the edge is visible only to roles granted on the registry.")]
        [Key(16)]
        public bool Restricted { get; set; }

        [Description("Archive ids of the newest transactions behind the edge within the window, at most fifty.")]
        [Key(17)]
        public List<long> ArchiveIds { get; set; } = [];
    }
}
