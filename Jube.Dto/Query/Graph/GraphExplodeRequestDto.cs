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
    public class GraphExplodeRequestDto
    {
        [Description("Guid of the Graph Registry whose edge definitions are read.")]
        [Key(0)]
        public Guid RegistryGuid { get; set; }

        [Description("The node whose one-hop neighbourhood is listed as lanes.")]
        [Key(1)]
        public long NodeId { get; set; }

        [Description("Start of the window (UTC); the registry default window when omitted.")]
        [Key(2)]
        public DateTime? From { get; set; }

        [Description("End of the window (UTC); now when omitted.")]
        [Key(3)]
        public DateTime? To { get; set; }

        [Description("Comma-separated Guids of the edge definitions to include; all when omitted.")]
        [Key(4)]
        public string? Edges { get; set; }

        [Description("Most lanes returned; default 200, at most 1000.")]
        [Key(5)]
        public int? Limit { get; set; }
    }
}
