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
    [Description("One checklist item as it stands on a case.")]
    [MessagePackObject]
    public class CaseChecklistItemDto
    {
        [Description("Identifier of the item on the case.")]
        [Key(0)]
        public long Id { get; set; }

        [Description("Guid of the definition the item came from.")]
        [Key(1)]
        public Guid CaseWorkflowChecklistItemGuid { get; set; }

        [Description("Name of the item.")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("Priority of the item; a lower number is listed first.")]
        [Key(3)]
        public int Priority { get; set; }

        [Description("Where the item stands: 1 Shown (required, not yet ticked), 2 Hidden (not required), " +
                     "3 Done (ticked).")]
        [Key(4)]
        public int StateId { get; set; }

        [Description("When the item was first shown on the case; the SLA clock starts here.")]
        [Key(5)]
        public DateTimeOffset FirstShownDate { get; set; }

        [Description("When the item was most recently shown on the case.")]
        [Key(6)]
        public DateTimeOffset ShownDate { get; set; }

        [Description("When the item is due under its SLA; blank when it has no SLA.")]
        [Key(7)]
        public DateTimeOffset? DueDate { get; set; }

        [Description("When the SLA breach of the item was first recorded; blank when no breach has been recorded.")]
        [Key(8)]
        public DateTimeOffset? SlaBreachedDate { get; set; }

        [Description("When the item was ticked; blank unless it is Done.")]
        [Key(9)]
        public DateTimeOffset? CompletedDate { get; set; }

        [Description("Who ticked the item; blank unless it is Done.")]
        [Key(10)]
        public string? CompletedUser { get; set; }

        [Description("SLA position: 1 None, 2 On track, 3 Breached (open and past due), 4 Completed on time, " +
                     "5 Completed late.")]
        [Key(11)]
        public int SlaStateId { get; set; }

        [Description("Whole minutes left before the SLA is breached; negative once breached; blank when the item " +
                     "has no SLA or is Done.")]
        [Key(12)]
        public int? MinutesRemaining { get; set; }
    }
}
