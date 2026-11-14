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
    public class GraphFlowRequestDto
    {
        [Description("Guid of the Graph Registry whose money-flow definitions are followed.")]
        [Key(0)]
        public Guid RegistryGuid { get; set; }

        [Description("Node id the money leaves (forward) or arrives at (backward).")]
        [Key(1)]
        public long NodeId { get; set; }

        [Description("Archive id of the transaction the walk starts from; its time and amount seed the first tranche.")]
        [Key(2)]
        public long? ArchiveId { get; set; }

        [Description("Start time (UTC) when no archive id is given; the window start when omitted.")]
        [Key(3)]
        public DateTime? At { get; set; }

        [Description("Amount of the first tranche when no archive id is given; any amount when omitted.")]
        [Key(4)]
        public double? Amount { get; set; }

        [Description("True follows money forward from the node, false traces it backward; default true.")]
        [Key(5)]
        public bool? Forward { get; set; }

        [Description("Hours allowed between one hop and the next, 1 to 720; default 72.")]
        [Key(6)]
        public int? HopWindowHours { get; set; }

        [Description("Fraction a tranche may shrink by per hop, 0 to 0.9; default 0.2.")]
        [Key(7)]
        public double? Tolerance { get; set; }

        [Description("Most hops followed, 1 to 6; default 4.")]
        [Key(8)]
        public int? Depth { get; set; }

        [Description("Most onward transactions followed per node, 1 to 50; default 10.")]
        [Key(9)]
        public int? FanOut { get; set; }

        [Description("Comma-separated Guids of the money-flow edge definitions to follow; all active ones when omitted.")]
        [Key(10)]
        public string? Edges { get; set; }

        [Description("Start of the window (UTC); the registry default window when omitted.")]
        [Key(11)]
        public DateTime? From { get; set; }

        [Description("End of the window (UTC); now when omitted.")]
        [Key(12)]
        public DateTime? To { get; set; }

    }
}
