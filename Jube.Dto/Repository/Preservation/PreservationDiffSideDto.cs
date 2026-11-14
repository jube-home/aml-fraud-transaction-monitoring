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
    public class PreservationDiffSideDto
    {
        [Description("The snapshot this side is, or null when it is the live tenant or a package.")]
        [Key(0)]
        public int? SnapshotId { get; set; }

        [Description("A label for the side: the snapshot's name, Live for the live tenant, or the package's guid.")]
        [Key(1)]
        public string Label { get; set; } = string.Empty;

        [Description("When the snapshot was taken. Null for the live tenant and for a package.")]
        [Key(2)]
        public DateTime? CreatedDate { get; set; }
    }
}
