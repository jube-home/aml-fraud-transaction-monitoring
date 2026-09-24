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
    [Description("An entity in the model's dependency graph.")]
    public class ModelGraphNodeDto
    {
        [Description("The node's id, Kind:Id, or Missing:Name for a name that resolves to nothing.")]
        public string Id { get; set; } = string.Empty;

        [Description("The entity's name.")] public string Label { get; set; } = string.Empty;

        [Description("The kind of entity, or Missing.")]
        public string Kind { get; set; } = string.Empty;

        [Description("Whether the entity is active.")]
        public bool Active { get; set; }

        [Description("More about the node.")] public string? Detail { get; set; }
    }
}