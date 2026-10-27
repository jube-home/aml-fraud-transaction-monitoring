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
    [Description("The difference between two states of a tenant's configuration. Identifiers, audit stamps and " +
                 "references to other entities are left out because they change when a snapshot is restored; " +
                 "everything else is compared.")]
    public class PreservationDiffDto
    {
        [Description("Identifier of the audit row that recorded this comparison. Zero when it was not recorded.")]
        public int Id { get; set; }

        [Description("What was compared, from PreservationDiffKind.")]
        public PreservationDiffKind DiffKindId { get; set; }

        [Description("The left side, the earlier state.")]
        public PreservationDiffSideDto Left { get; set; } = new();

        [Description("The right side, the later state or the state being moved to.")]
        public PreservationDiffSideDto Right { get; set; } = new();

        [Description("True when nothing differs.")]
        public bool Identical { get; set; }

        [Description("Entities, counting every nested one, that exist on the right only.")]
        public int Added { get; set; }

        [Description("Entities, counting every nested one, that exist on the left only.")]
        public int Removed { get; set; }

        [Description("Entities on both sides with at least one property different.")]
        public int Changed { get; set; }

        [Description("Entities on both sides that are identical.")]
        public int Unchanged { get; set; }

        [Description("How many references to other entities differed and were left out of the changes.")]
        public int ReferencesIgnored { get; set; }

        [Description("True when there were more differences than are listed in Entries. The counts are complete.")]
        public bool Truncated { get; set; }

        [Description("The counts for each section of the export body.")]
        public List<PreservationDiffSectionDto> Sections { get; set; } = [];

        [Description("The differences, one entry for each entity added, removed or changed, up to a limit.")]
        public List<PreservationDiffEntryDto> Entries { get; set; } = [];
    }
}
