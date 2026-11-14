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
    [Description("The checklists of the status a case is in, brought up to date, with what is still required " +
                 "before the case can leave the status or be closed.")]
    [MessagePackObject]
    public class CaseChecklistDto
    {
        [Description("Identifier of the case.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("The checklists of the status the case is in, ordered by priority then name.")]
        [Key(1)]
        public List<CaseChecklistGroupDto> Checklists { get; set; } = [];

        [Description("How many items are required on the case now: those that are shown or done.")]
        [Key(2)]
        public int Required { get; set; }

        [Description("How many of the required items are done.")]
        [Key(3)]
        public int Done { get; set; }

        [Description("How many of the required items are still open.")]
        [Key(4)]
        public int Open { get; set; }

        [Description("How many open items have breached their SLA.")]
        [Key(5)]
        public int Breached { get; set; }

        [Description("When the checklist of the case was last brought up to date. Null means it never has been.")]
        [Key(6)]
        public DateTimeOffset? ReconciledDate { get; set; }

        [Description("The override a supervisor has granted for the case in its current status, not yet used by a move. Null when there is none.")]
        [Key(7)]
        public CaseChecklistOverrideDto? Override { get; set; }

        [Description("True when no shown item is still open, so nothing here stops the case leaving the status.")]
        [Key(8)]
        public bool Complete { get; set; }
    }
}
