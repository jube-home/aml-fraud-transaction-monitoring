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
    public class GraphExplodeDto
    {
        [Description("The node the lanes are around.")]
        [Key(0)]
        public GraphNodeDto? Node { get; set; }

        [Description("One lane per counterpart, most money first.")]
        [Key(1)]
        public List<GraphExplodeLaneDto> Lanes { get; set; } = [];

        [Description("Start of the window applied (UTC).")]
        [Key(2)]
        public DateTimeOffset From { get; set; }

        [Description("End of the window applied (UTC).")]
        [Key(3)]
        public DateTimeOffset To { get; set; }
    }
}
