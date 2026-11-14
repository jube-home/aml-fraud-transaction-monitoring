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
    public class GraphNodeFactsDto
    {
        [Description("The node.")]
        [Key(0)]
        public GraphNodeDto Node { get; set; } = new();

        [Description("Edge definitions the node takes part in, with counts.")]
        [Key(1)]
        public List<GraphNodeDefinitionCountDto> Definitions { get; set; } = [];

        [Description("Plain sentences stating why the node matters, each from a computed fact.")]
        [Key(2)]
        public GraphCommunityDto? Community { get; set; }

        [Key(3)]
        public List<GraphPatternDto> Patterns { get; set; } = [];

        [Key(4)]
        public List<string> Sentences { get; set; } = [];
    }
}
