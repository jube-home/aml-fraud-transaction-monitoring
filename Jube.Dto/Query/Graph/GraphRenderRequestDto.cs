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
    public class GraphRenderRequestDto
    {
        [Description("The neighbourhood to draw.")]
        [Key(0)]
        public GraphNeighbourhoodRequestDto Query { get; set; } = new();

        [Description("Layout, as GraphLayout: 1 Force, 2 Radial, 3 Flow, 4 Hierarchy; the registry default when omitted.")]
        [Key(1)]
        public GraphFlowRequestDto? Flow { get; set; }

        [Key(2)]
        public int? LayoutId { get; set; }

        [Description("What drives node size: 0 nothing, 1 degree, 2 amount, 3 weight; default 1.")]
        [Key(3)]
        public int? MeasureId { get; set; }

        [Description("Positions from the previous render, so the layout moves only what it must.")]
        [Key(4)]
        public List<GraphAnchorDto> Anchors { get; set; } = [];

        [Description("True for the dark theme.")]
        [Key(5)]
        public bool Dark { get; set; }

        [Description("Optional caption lines drawn under the canvas, such as the path sentences.")]
        [Key(6)]
        public List<string> Captions { get; set; } = [];
    }
}
