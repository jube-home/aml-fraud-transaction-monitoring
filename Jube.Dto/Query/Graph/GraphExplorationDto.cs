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
    public class GraphExplorationDto
    {
        [Description("Server-assigned identifier; 0 to create.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Name shown in the rail; unique per registry for the user.")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Guid of the Graph Registry the exploration is on.")]
        [Key(2)]
        public Guid GraphRegistryGuid { get; set; }

        [Description("The Graph page address (query string) that restores the view.")]
        [Key(3)]
        public string? Address { get; set; }

        [Description("Optional pinned node positions as JSON, passed to the layout as anchors.")]
        [Key(4)]
        public string? Pinned { get; set; }

        [Description("When the exploration was saved (UTC).")]
        [Key(5)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("When the exploration was last changed (UTC).")]
        [Key(6)]
        public DateTimeOffset? UpdatedDate { get; set; }
    }
}
