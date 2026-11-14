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
    public class GraphPatternListRequestDto
    {
        [Description("State to list: 1 New, 2 Seen, 3 Dismissed, 4 Cased; all states when omitted.")]
        [Key(0)]
        public int? StateId { get; set; }

        [Description("Kind to list: 1 FanOut, 2 FanIn, 3 GatherScatter, 4 ScatterGather, 5 TemporalCycle, 6 PassThrough, 7 SmurfTypeOne, 8 SmurfTypeTwo, 9 StructuringWindow, 10 DenseBlock; all kinds when omitted.")]
        [Key(1)]
        public int? KindId { get; set; }

        [Description("Most patterns returned, 1 to 500; default 100.")]
        [Key(2)]
        public int? Limit { get; set; }

    }
}
