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

namespace Jube.Dto.Query.EntityAnalysisModelIntegrity
{
    [Description("The model's dependency graph as nodes and edges, ready to draw.")]
    [MessagePackObject]
    public class ModelGraphDto
    {
        [Description("The entities.")] [Key(0)] public List<ModelGraphNodeDto> Nodes { get; set; } = [];

        [Description("The uses between them.")]
        [Key(1)]
        public List<ModelGraphEdgeDto> Edges { get; set; } = [];

        [Description("True when the graph had more than MaxNodes nodes and the least connected were left out; " +
                     "focus on an entity to see its neighbourhood.")]
        [Key(2)]
        public bool Truncated { get; set; }

        [Description("The most nodes returned.")]
        [Key(3)]
        public int MaxNodes { get; set; }

        [Description("The node the graph was centred on, when one was given.")]
        [Key(4)]
        public string? Focus { get; set; }

        [Description("When more entities than this are connected, the diagram groups each kind into one box with " +
                     "a count until it is expanded; entities with findings are always drawn on their own.")]
        [Key(5)]
        public int CollapseAbove { get; set; }
    }
}