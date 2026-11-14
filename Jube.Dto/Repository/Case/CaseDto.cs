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

namespace Jube.Dto.Repository.Case
{
    [MessagePackObject]
    public class CaseDto
    {
        [Description("Server-assigned identifier of the case. Read-only; required when updating an existing case.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("When set, the case is due for diary follow-up on or after this date/time.")]
        [Key(1)]
        public DateTimeOffset? DiaryDate { get; set; }

        [Description("Identifier of the CaseWorkflowStatus the case currently sits in.")]
        [Key(2)]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [Key(3)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("When true, the case is locked to LockedUser and other users cannot edit it.")]
        [Key(4)]
        public bool Locked { get; set; }

        [Description("User the case is locked to. Required when Locked is true.")]
        [Key(5)]
        public string? LockedUser { get; set; }

        [Description("Server-assigned timestamp the case was locked. Read-only.")]
        [Key(6)]
        public DateTimeOffset? LockedDate { get; set; }

        [Description(
            "Closed-status code for the case: 0 open, 1 suspended, 2 reopened, 3 closed, 4 bypassed pending diary.")]
        [Key(7)]
        public byte ClosedStatusId { get; set; }

        [Description("Server-assigned timestamp the case was closed. Read-only.")]
        [Key(8)]
        public DateTimeOffset? ClosedDate { get; set; }

        [Description("Server-assigned user who closed the case. Read-only.")]
        [Key(9)]
        public string? ClosedUser { get; set; }

        [Description("The key field name (e.g. account number) this case's transactions are grouped by.")]
        [Key(10)]
        public string? CaseKey { get; set; }

        [Description("When true, the case is suspended on a diary and will not be actioned until DiaryDate.")]
        [Key(11)]
        public bool Diary { get; set; }

        [Description("Server-assigned user who most recently set the diary. Read-only.")]
        [Key(12)]
        public string? DiaryUser { get; set; }

        [Description("Investigator's 1-5 rating of the case's suspicion level. 0 means not yet rated.")]
        [Key(13)]
        public byte Rating { get; set; }

        [Description("The transaction payload JSON captured at case-creation time. Read-only after creation.")]
        [Key(14)]
        public string? Json { get; set; }

        [Description(
            "The value of CaseKey's field for this case's grouped transactions (e.g. the account number itself).")]
        [Key(15)]
        public string? CaseKeyValue { get; set; }

        [Description("Server-assigned closed-status code before the most recent transition. Read-only.")]
        [Key(16)]
        public byte LastClosedStatus { get; set; }

        [Description("Transient, request-only snapshot of the case's editable form fields at the moment of update -- " +
                     "not persisted on the Case row itself. Drives the notification/HTTP-endpoint payload when the " +
                     "case's CaseWorkflowStatusGuid changes and null mutes that dispatch.")]
        [Key(17)]
        public string? Payload { get; set; }

        [Description("Why the case was closed, as a CaseCloseReason: 0 none, 1 false positive known customer " +
                     "activity, 2 false positive rule too broad, 3 duplicate, 4 insufficient evidence, 5 suspicion " +
                     "confirmed and reported, 6 fraud confirmed loss prevented, 7 customer explained, 8 other. " +
                     "Recorded only by the update that closes the case; ignored on any other update and cleared " +
                     "when the case is reopened.")]
        [Key(18)]
        public byte ClosedReasonId { get; set; }

        [Description("Optional free text, up to 1000 characters, recorded with the close reason. Recorded only by " +
                     "the update that closes the case; ignored on any other update and cleared when the case " +
                     "is reopened.")]
        [Key(19)]
        public string? ClosedReasonNote { get; set; }
    }
}