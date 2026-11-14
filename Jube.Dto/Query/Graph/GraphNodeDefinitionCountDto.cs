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
    public class GraphNodeDefinitionCountDto
    {
        [Description("Guid of the edge definition.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("Name of the edge definition.")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Distinct counterparts through this definition.")]
        [Key(2)]
        public int Counterparts { get; set; }

        [Description("Transactions through this definition.")]
        [Key(3)]
        public int Count { get; set; }

        [Description("Sum of the amount through this definition.")]
        [Key(4)]
        public double Total { get; set; }
    }
}
