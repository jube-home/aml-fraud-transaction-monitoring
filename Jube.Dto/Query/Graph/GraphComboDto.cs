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
    public class GraphComboDto
    {
        [Description("Identifier of the collapsed cluster, unique within the response.")]
        [Key(0)]
        public string? Id { get; set; }

        [Description("The node the collapsed nodes were reached through.")]
        [Key(1)]
        public long ViaNodeId { get; set; }

        [Description("The key name shared by the collapsed nodes.")]
        [Key(2)]
        public string? Key { get; set; }

        [Description("How many nodes are collapsed.")]
        [Key(3)]
        public int Count { get; set; }

        [Description("The collapsed node ids.")]
        [Key(4)]
        public List<long> NodeIds { get; set; } = [];
    }
}
