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

namespace Jube.Dto.Query.CaseByCaseKeyValue
{
    [Description("A Case matched by its Case Key and Case Key Value, restricted to Cases whose Case Workflow and " +
                 "Case Workflow Status the caller's role is entitled to see.")]
    [MessagePackObject]
    public class CaseByCaseKeyValueDto
    {
        [Description("Server-assigned integer identifier of the Case.")]
        [Key(0)]
        public int Id { get; set; }

        [Description(
            "Guid of the Entity Analysis Model Instance Entry (the archived transaction) that created the Case.")]
        [Key(1)]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("Date the Case is diarised until; the default DateTime value when unset.")]
        [Key(2)]
        public DateTime DiaryDate { get; set; }

        [Description("Guid of the Case Workflow the Case belongs to.")]
        [Key(3)]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Name of the Case Workflow Status the Case is currently in.")]
        [Key(4)]
        public string? CaseWorkflowStatusName { get; set; }

        [Description("Date the Case was created; the default DateTime value when unset.")]
        [Key(5)]
        public DateTime CreatedDate { get; set; }

        [Description("True when the Case is currently locked by a user.")]
        [Key(6)]
        public bool Locked { get; set; }

        [Description("User holding the lock on the Case; empty when unlocked.")]
        [Key(7)]
        public string? LockedUser { get; set; }

        [Description("Date the Case was locked; the default DateTime value when unset.")]
        [Key(8)]
        public DateTime LockedDate { get; set; }

        [Description("Identifier of the closed status the Case was closed with; zero when open.")]
        [Key(9)]
        public byte ClosedStatusId { get; set; }

        [Description("Date the Case was closed; the default DateTime value when unset.")]
        [Key(10)]
        public DateTime ClosedDate { get; set; }

        [Description("User that closed the Case; empty when open.")]
        [Key(11)]
        public string? ClosedUser { get; set; }

        [Description("Case Key naming the payload field the Case is keyed on.")]
        [Key(12)]
        public string? CaseKey { get; set; }

        [Description("True when the Case has been diarised.")]
        [Key(13)]
        public bool Diary { get; set; }

        [Description("User that diarised the Case; empty when not diarised.")]
        [Key(14)]
        public string? DiaryUser { get; set; }

        [Description("Rating assigned to the Case.")]
        [Key(15)]
        public byte Rating { get; set; }

        [Description("Case Key Value the Case is keyed on.")]
        [Key(16)]
        public string? CaseKeyValue { get; set; }

        [Description("Closed status the Case last held before it was reopened; zero when never closed.")]
        [Key(17)]
        public byte LastClosedStatus { get; set; }

        [Description("Date the closed status was last migrated; the default DateTime value when unset.")]
        [Key(18)]
        public DateTime ClosedStatusMigrationDate { get; set; }

        [Description("Foreground colour of the current Case Workflow Status.")]
        [Key(19)]
        public string? ForeColor { get; set; }

        [Description("Background colour of the current Case Workflow Status.")]
        [Key(20)]
        public string? BackColor { get; set; }
    }
}