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
namespace Jube.Dto.Repository.Archive
{
    public class ArchiveTagDto
    {
        [Description("Identifier of the archived transaction instance being tagged, matching " +
                     "EntityAnalysisModelInstanceEntryGuid on the Archive payload and the Case grid row.")]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("The full set of Tag names now applied to this archived transaction -- replaces whatever " +
                     "tag set was previously stored, not a delta. An empty array clears all tags. Each name must " +
                     "match a Tag registered against the transaction's Model (see EntityAnalysisModelTag).")]
        public string[]? Tag { get; set; }
    }
}