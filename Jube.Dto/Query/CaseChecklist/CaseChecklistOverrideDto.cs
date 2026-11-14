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

namespace Jube.Dto.Query.CaseChecklist
{
    [Description("A recorded override of the checklist gate of a case, granted by a supervisor and consumed by the " +
                 "move that uses it.")]
    [MessagePackObject]
    public class CaseChecklistOverrideDto
    {
        [Description("Identifier of the override.")]
        [Key(0)]
        public long Id { get; set; }

        [Description("Identifier of the case.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("The reason given for the override.")]
        [Key(2)]
        public string Reason { get; set; } = string.Empty;

        [Description("The supervisor who granted the override.")]
        [Key(3)]
        public string GrantedUser { get; set; } = string.Empty;

        [Description("When the override was granted, in UTC.")]
        [Key(4)]
        public DateTimeOffset GrantedDate { get; set; }

        [Description("The names of the items that were shown and not ticked when the override was granted.")]
        [Key(5)]
        public List<string> OpenItems { get; set; } = [];
    }
}
