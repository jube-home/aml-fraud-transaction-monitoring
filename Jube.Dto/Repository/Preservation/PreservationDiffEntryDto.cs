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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.Preservation
{
    public class PreservationDiffEntryDto
    {
        [Description("The section of the export body the entity belongs to, such as EntityAnalysisModel.")]
        public string Section { get; set; } = string.Empty;

        [Description("Where the entity sits: the section, then each nesting step as the property name with the " +
                     "entity's name in square brackets, for example EntityAnalysisModelRequestXpath[Amount].")]
        public List<string> Path { get; set; } = [];

        [Description("Whether the entity was added, removed or changed, from PreservationDiffEntryKind.")]
        public PreservationDiffEntryKind KindId { get; set; }

        [Description("For a changed entity, the properties that differ. Empty for an added or removed entity.")]
        public List<PreservationDiffChangeDto> Changes { get; set; } = [];

        [Description("For an added or removed entity, how many entities nested beneath it came with it.")]
        public int NestedCount { get; set; }
    }
}
