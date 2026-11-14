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

namespace Jube.Dto.Repository.CaseResearch
{
    [MessagePackObject]
    public class CaseResearchSourceDto
    {
        [Description("Server-assigned identifier of the source. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Optional title of the source, up to 300 characters.")]
        [Key(1)]
        public string? Title { get; set; }

        [Description("The http or https address of the source, up to 2000 characters, with no credentials, " +
                     "spaces or angle brackets. Only attach real addresses of sources that were actually " +
                     "retrieved. Jube stores the address as written and never fetches it.")]
        [Key(2)]
        public string Url { get; set; } = string.Empty;

        [Description("When the source was retrieved (UTC). Defaults to now when omitted.")]
        [Key(3)]
        public DateTimeOffset? RetrievedDate { get; set; }
    }
}
