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
    [Description("One entry of the audit of the checklist of a case: an item shown, hidden, ticked, unticked or " +
                 "refused, or the gate blocked, overridden or bypassed.")]
    [MessagePackObject]
    public class CaseChecklistHistoryEntryDto
    {
        [Description("Identifier of the audit entry.")]
        [Key(0)]
        public long Id { get; set; }

        [Description("What happened: 1 Shown, 2 Hidden, 3 Checked, 4 Unchecked, 5 Validation failed, 6 Gate blocked, " +
                     "7 Gate overridden, 8 Gate bypassed, 9 SLA breached.")]
        [Key(1)]
        public int ActionId { get; set; }

        [Description("Name of the item the entry is about. Null for entries about the whole checklist, such as " +
                     "the gate.")]
        [Key(2)]
        public string? ItemName { get; set; }

        [Description("Who or what did it: a user, or System, Case creation, Case escalation or Case expiry.")]
        [Key(3)]
        public string? Actor { get; set; }

        [Description("When it happened, in UTC.")]
        [Key(4)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("What was recorded about it, such as the open items or the reason for an override.")]
        [Key(5)]
        public string? Detail { get; set; }
    }
}
