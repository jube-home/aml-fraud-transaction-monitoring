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
    [Description("The difference between two states of a tenant's configuration. Identifiers, audit stamps and " +
                 "references to other entities are left out because they change when a snapshot is restored; " +
                 "everything else is compared.")]
    [MessagePackObject]
    public class PreservationDiffDto
    {
        [Description("Identifier of the audit row that recorded this comparison. Zero when it was not recorded.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("What was compared, from PreservationDiffKind.")]
        [Key(1)]
        public PreservationDiffKind DiffKindId { get; set; }

        [Description("The left side, the earlier state.")]
        [Key(2)]
        public PreservationDiffSideDto Left { get; set; } = new();

        [Description("The right side, the later state or the state being moved to.")]
        [Key(3)]
        public PreservationDiffSideDto Right { get; set; } = new();

        [Description("True when nothing differs.")]
        [Key(4)]
        public bool Identical { get; set; }

        [Description("Entities, counting every nested one, that exist on the right only.")]
        [Key(5)]
        public int Added { get; set; }

        [Description("Entities, counting every nested one, that exist on the left only.")]
        [Key(6)]
        public int Removed { get; set; }

        [Description("Entities on both sides with at least one property different.")]
        [Key(7)]
        public int Changed { get; set; }

        [Description("Entities on both sides that are identical.")]
        [Key(8)]
        public int Unchanged { get; set; }

        [Description("How many references to other entities differed and were left out of the changes.")]
        [Key(9)]
        public int ReferencesIgnored { get; set; }

        [Description("True when there were more differences than are listed in Entries. The counts are complete.")]
        [Key(10)]
        public bool Truncated { get; set; }

        [Description("The counts for each section of the export body.")]
        [Key(11)]
        public List<PreservationDiffSectionDto> Sections { get; set; } = [];

        [Description("The differences, one entry for each entity added, removed or changed, up to a limit.")]
        [Key(12)]
        public List<PreservationDiffEntryDto> Entries { get; set; } = [];
    }
}
