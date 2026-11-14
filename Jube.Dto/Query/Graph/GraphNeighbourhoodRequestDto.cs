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
    public class GraphNeighbourhoodRequestDto
    {
        [Description("Guid of the Graph Registry whose edge definitions are traversed.")]
        [Key(0)]
        public Guid RegistryGuid { get; set; }

        [Description("Report key name of the seed node, such as AccountId.")]
        [Key(1)]
        public string? Key { get; set; }

        [Description("Value of the seed node, such as the account number.")]
        [Key(2)]
        public string? Value { get; set; }

        [Description("Hops from the seed, 1 to 3; the registry default when omitted.")]
        [Key(3)]
        public int? Hops { get; set; }

        [Description("Most neighbours followed per node per direction, 1 to 100; default 25.")]
        [Key(4)]
        public int? FanOut { get; set; }

        [Description("Start of the window (UTC); the registry default window when omitted.")]
        [Key(5)]
        public DateTime? From { get; set; }

        [Description("End of the window (UTC); now when omitted.")]
        [Key(6)]
        public DateTime? To { get; set; }

        [Description("Edges whose total amount is below this are not followed; default 0.")]
        [Key(7)]
        public double? MinimumAmount { get; set; }

        [Description("Comma-separated Guids of the edge definitions to follow; all active definitions when omitted.")]
        [Key(8)]
        public string? Edges { get; set; }

        [Description("When true, traversal passes through hubs; default false.")]
        [Key(9)]
        public bool? IncludeHubs { get; set; }

        [Description("Comma-separated node ids already expanded by the analyst, each followed one more hop.")]
        [Key(10)]
        public string? Expanded { get; set; }

        [Description("When true, nodes are ranked by interest propagated from the seed (confirmed cases and nearness to them boost, hubs damp) and the cap keeps the most relevant rather than the nearest; default false.")]
        [Key(11)]
        public bool? Relevance { get; set; }

        [Description("Most nodes returned before the rest collapse into clusters; the registry render cap when omitted.")]
        [Key(12)]
        public int? Cap { get; set; }
    }
}
