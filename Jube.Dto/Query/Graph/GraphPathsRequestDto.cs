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
    public class GraphPathsRequestDto
    {
        [Description("Guid of the Graph Registry whose edge definitions are traversed.")]
        [Key(0)]
        public Guid RegistryGuid { get; set; }

        [Description("Node id of the start of the paths.")]
        [Key(1)]
        public long FromNodeId { get; set; }

        [Description("Node id of the end of the paths.")]
        [Key(2)]
        public long ToNodeId { get; set; }

        [Description("Most hops per path, 1 to 6; default 4.")]
        [Key(3)]
        public int? MaxHops { get; set; }

        [Description("Most paths returned, 1 to 20; default 5.")]
        [Key(4)]
        public int? K { get; set; }

        [Description("When true, paths may pass through hubs; default false.")]
        [Key(5)]
        public bool? IncludeHubs { get; set; }

        [Description("Comma-separated Guids of the edge definitions to follow; all active definitions when omitted.")]
        [Key(6)]
        public string? Edges { get; set; }

        [Description("Start of the window (UTC) for the evidence behind the edges; the registry default window when omitted.")]
        [Key(7)]
        public DateTime? From { get; set; }

        [Description("End of the window (UTC); now when omitted.")]
        [Key(8)]
        public DateTime? To { get; set; }

    }
}
