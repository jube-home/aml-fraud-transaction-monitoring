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
using Jube.Dto.Query.CaseSearch;
using Jube.Dto.Repository.CaseFile;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("One note recorded against a case.")]
    [MessagePackObject]
    public class CaseNoteSummaryDto
    {
        [Description("The note text, shortened to 400 characters.")]
        [Key(0)]
        public string? Note { get; set; }

        [Description("Who wrote it.")]
        [Key(1)]
        public string? User { get; set; }

        [Description("When it was written (UTC).")]
        [Key(2)]
        public DateTimeOffset? Date { get; set; }

        [Description("The files attached to the note: name, type, size, who uploaded them and when.")]
        [Key(3)]
        public List<CaseFileDto> Files { get; set; } = [];
    }
}
