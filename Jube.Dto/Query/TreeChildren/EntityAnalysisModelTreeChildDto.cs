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
using Jube.Dto.Interfaces;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.TreeChildren
{
    [Description("A child node of a model tree (Entity Analysis Model children such as rules, tags, lists, " +
                 "dictionaries). Colour is 'green' for an active node and 'red' otherwise.")]
    [MessagePackObject]
    public class EntityAnalysisModelTreeChildDto : ITreeChild
    {
        [Description("Integer Id of the parent Entity Analysis Model; null for nodes addressed by model Guid.")]
        [Key(0)]
        public int? EntityAnalysisModelId { get; set; }

        [Description("Guid of the parent Entity Analysis Model; only set for the List and Dictionary nodes.")]
        [Key(1)]
        public Guid? EntityAnalysisModelGuid { get; set; }

        [Description("Integer Id of the child row.")]
        [Key(2)]
        public int Key { get; set; }

        [Description("Name of the child row.")]
        [Key(3)]
        public string? Name { get; set; }

        [Description("'green' when the child is active, otherwise 'red'.")]
        [Key(4)]
        public string? Color { get; set; }

        [Description(
            "True when the child row is deleted. A deleted row stays in the tree so it can be opened and restored.")]
        [Key(5)]
        public bool Deleted { get; set; }
    }
}