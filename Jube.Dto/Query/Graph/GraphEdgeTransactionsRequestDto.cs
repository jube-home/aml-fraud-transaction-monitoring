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
    public class GraphEdgeTransactionsRequestDto
    {
        [Description("The edge whose transactions are listed.")]
        [Key(0)]
        public long EdgeId { get; set; }

        [Description("Start of the window (UTC); the edge's first transaction when omitted.")]
        [Key(1)]
        public DateTime? From { get; set; }

        [Description("End of the window (UTC); now when omitted.")]
        [Key(2)]
        public DateTime? To { get; set; }

        [Description("Most transactions returned, newest first; default 500.")]
        [Key(3)]
        public int? Limit { get; set; }
    }
}
