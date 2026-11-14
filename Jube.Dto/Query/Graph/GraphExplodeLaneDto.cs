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
    public class GraphExplodeLaneDto
    {
        [Description("The counterpart node.")]
        [Key(0)]
        public GraphNodeDto Node { get; set; } = new();

        [Description("Transactions behind the lane over its lifetime.")]
        [Key(1)]
        public int Count { get; set; }

        [Description("Sum of the amount over its lifetime.")]
        [Key(2)]
        public double Total { get; set; }

        [Description("Earliest transaction (UTC).")]
        [Key(3)]
        public DateTimeOffset First { get; set; }

        [Description("Latest transaction (UTC).")]
        [Key(4)]
        public DateTimeOffset Last { get; set; }

        [Description("Names of the edge definitions behind the lane.")]
        [Key(5)]
        public List<string> Definitions { get; set; } = [];

        [Description("Edge ids behind the lane, for the Journal filter.")]
        [Key(6)]
        public List<long> EdgeIds { get; set; } = [];
    }
}
