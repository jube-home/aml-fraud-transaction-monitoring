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
    public class PreservationOrphanedUsersDto
    {
        [Description("How many active users would no longer resolve to a role after the import and be moved to the " +
                     "Orphaned Preservation Import role.")]
        [Key(0)]
        public int Count { get; set; }

        [Description("Their names, up to fifty.")]
        [Key(1)]
        public List<string> Names { get; set; } = [];

        [Description("True when the user previewing the import is one of them: they stay active, everybody else " +
                     "affected is deactivated.")]
        [Key(2)]
        public bool ImporterStaysActive { get; set; }
    }
}
