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
    public class GraphEdgeDefinitionDto
    {
        [Description("Guid of the edge definition.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("Name shown in the legend.")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Kind of edge, as GraphEdgeKind.")]
        [Key(2)]
        public int KindId { get; set; }

        [Description("True when visible only to roles granted on the registry.")]
        [Key(3)]
        public bool Restricted { get; set; }

        [Description("True when the caller may see this definition's restricted edges.")]
        [Key(4)]
        public bool Visible { get; set; }

        [Description("Lucide icon name for the link, or null for a plain line.")]
        [Key(5)]
        public string? IconName { get; set; }

        [Description("Node type of the from node: the configured from node key, else the from key.")]
        [Key(6)]
        public string? FromNodeKey { get; set; }

        [Description("Node type of the to node: the configured to node key, else the to key.")]
        [Key(7)]
        public string? ToNodeKey { get; set; }

        [Description("Lucide icon name configured for the from node type, or null to choose one from the key name.")]
        [Key(8)]
        public string? FromNodeIcon { get; set; }

        [Description("Lucide icon name configured for the to node type, or null to choose one from the key name.")]
        [Key(9)]
        public string? ToNodeIcon { get; set; }
    }
}
