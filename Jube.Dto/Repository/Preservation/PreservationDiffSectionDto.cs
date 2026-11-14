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

namespace Jube.Dto.Repository.Preservation
{
    [MessagePackObject]
    public class PreservationDiffSectionDto
    {
        [Description("The section of the export body, such as EntityAnalysisModel or RoleRegistry.")]
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("Entities, counting every nested one, that exist on the right only.")]
        [Key(1)]
        public int Added { get; set; }

        [Description("Entities, counting every nested one, that exist on the left only.")]
        [Key(2)]
        public int Removed { get; set; }

        [Description("Entities on both sides with at least one property different.")]
        [Key(3)]
        public int Changed { get; set; }

        [Description("Entities on both sides that are identical.")]
        [Key(4)]
        public int Unchanged { get; set; }
    }
}
