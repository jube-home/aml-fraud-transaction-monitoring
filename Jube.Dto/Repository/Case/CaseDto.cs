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
namespace Jube.Dto.Repository.Case
{
    public class CaseDto
    {
        [Description("Server-assigned identifier of the case. Read-only; required when updating an existing case.")]
        public int Id { get; set; }

        [Description("When set, the case is due for diary follow-up on or after this date/time.")]
        public DateTimeOffset? DiaryDate { get; set; }

        [Description("Identifier of the CaseWorkflowStatus the case currently sits in.")]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("When true, the case is locked to LockedUser and other users cannot edit it.")]
        public bool Locked { get; set; }

        [Description("User the case is locked to. Required when Locked is true.")]
        public string? LockedUser { get; set; }

        [Description("Server-assigned timestamp the case was locked. Read-only.")]
        public DateTimeOffset? LockedDate { get; set; }

        [Description(
            "Closed-status code for the case: 0 open, 1 suspended, 2 reopened, 3 closed, 4 bypassed pending diary.")]
        public byte ClosedStatusId { get; set; }

        [Description("Server-assigned timestamp the case was closed. Read-only.")]
        public DateTimeOffset? ClosedDate { get; set; }

        [Description("Server-assigned user who closed the case. Read-only.")]
        public string? ClosedUser { get; set; }

        [Description("The key field name (e.g. account number) this case's transactions are grouped by.")]
        public string? CaseKey { get; set; }

        [Description("When true, the case is suspended on a diary and will not be actioned until DiaryDate.")]
        public bool Diary { get; set; }

        [Description("Server-assigned user who most recently set the diary. Read-only.")]
        public string? DiaryUser { get; set; }

        [Description("Investigator's 1-5 rating of the case's suspicion level. 0 means not yet rated.")]
        public byte Rating { get; set; }

        [Description("The transaction payload JSON captured at case-creation time. Read-only after creation.")]
        public string? Json { get; set; }

        [Description(
            "The value of CaseKey's field for this case's grouped transactions (e.g. the account number itself).")]
        public string? CaseKeyValue { get; set; }

        [Description("Server-assigned closed-status code before the most recent transition. Read-only.")]
        public byte LastClosedStatus { get; set; }

        [Description("Transient, request-only snapshot of the case's editable form fields at the moment of update -- " +
                     "not persisted on the Case row itself. Drives the notification/HTTP-endpoint payload when the " +
                     "case's CaseWorkflowStatusGuid changes and null suppresses that dispatch.")]
        public string? Payload { get; set; }
    }
}