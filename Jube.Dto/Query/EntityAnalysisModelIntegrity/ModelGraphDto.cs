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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.EntityAnalysisModelIntegrity
{
    [Description("The model's dependency graph as nodes and edges, ready to draw.")]
    public class ModelGraphDto
    {
        [Description("The entities.")] public List<ModelGraphNodeDto> Nodes { get; set; } = [];

        [Description("The uses between them.")]
        public List<ModelGraphEdgeDto> Edges { get; set; } = [];

        [Description("True when the graph had more than MaxNodes nodes and the least connected were left out; " +
                     "focus on an entity to see its neighbourhood.")]
        public bool Truncated { get; set; }

        [Description("The most nodes returned.")]
        public int MaxNodes { get; set; }

        [Description("The node the graph was centred on, when one was given.")]
        public string? Focus { get; set; }
    }
}