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
    [Description("What an export of the caller's tenant would carry, section by section, so each include switch " +
                 "shows what it adds. Nothing is changed.")]
    public class PreservationSummaryDto
    {
        [Description("One entry for each section.")]
        public List<PreservationSummarySectionDto> Sections { get; set; } = [];

        [Description("The size of everything serialised, before compression and encryption.")]
        public long TotalBytes { get; set; }

        [Description("The fewest characters an export password may have; the server refuses a shorter one.")]
        public int MinimumPasswordLength { get; set; }
    }
}
