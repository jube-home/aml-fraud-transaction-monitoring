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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.TreeChildren
{
    [Description("A child node of a model tree (Entity Analysis Model children such as rules, tags, lists, " +
                 "dictionaries). Colour is 'green' for an active node and 'red' otherwise.")]
    public class EntityAnalysisModelTreeChildDto : ITreeChild
    {
        [Description("Integer Id of the parent Entity Analysis Model; null for nodes addressed by model Guid.")]
        public int? EntityAnalysisModelId { get; set; }

        [Description("Guid of the parent Entity Analysis Model; only set for the List and Dictionary nodes.")]
        public Guid? EntityAnalysisModelGuid { get; set; }

        [Description("Integer Id of the child row.")]
        public int Key { get; set; }

        [Description("Name of the child row.")]
        public string? Name { get; set; }

        [Description("'green' when the child is active, otherwise 'red'.")]
        public string? Color { get; set; }
    }
}