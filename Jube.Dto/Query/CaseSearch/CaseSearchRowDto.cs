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

namespace Jube.Dto.Query.CaseSearch
{
    [Description("One case in the triage list with its risk, why it needs attention and where it stands against " +
                 "its service levels.")]
    public class CaseSearchRowDto
    {
        [Description("Identifier of the case.")]
        public int CaseId { get; set; }

        [Description("Name of the case key, such as AccountId.")]
        public string? CaseKey { get; set; }

        [Description("Value of the case key.")]
        public string? CaseKeyValue { get; set; }

        [Description("When the case was created (UTC).")]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("Minutes since the case was created, or until it closed.")]
        public double AgeMinutes { get; set; }

        [Description("Guid of the Case Workflow.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Name of the Case Workflow.")]
        public string? CaseWorkflowName { get; set; }

        [Description("Guid of the Case Workflow Status the case is in.")]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Name of the Status.")]
        public string? StatusName { get; set; }

        [Description("Hex text colour of the Status.")]
        public string? ForeColor { get; set; }

        [Description("Hex background colour of the Status.")]
        public string? BackColor { get; set; }

        [Description("When the case entered its current status (UTC).")]
        public DateTimeOffset? StatusEnteredDate { get; set; }

        [Description("True when the case is locked; the lock is its assignment.")]
        public bool Locked { get; set; }

        [Description("The user the case is locked to.")]
        public string? LockedUser { get; set; }

        [Description("True when the case is locked to the caller.")]
        public bool Mine { get; set; }

        [Description("Closed status: 0 Open, 1 Suspend open, 2 Suspend closed, 3 Closed, 4 Suspend bypass.")]
        public int ClosedStatusId { get; set; }

        [Description("True when a diary is set.")]
        public bool Diary { get; set; }

        [Description("When the diary falls due (UTC).")]
        public DateTimeOffset? DiaryDate { get; set; }

        [Description("What the diary is for: 1 Review, 2 Customer response, 3 Regulatory deadline, 4 Payment hold. " +
                     "Meaningful only while a diary is set.")]
        public int DiaryTypeId { get; set; }

        [Description("Name of the diary type.")]
        public string? DiaryType { get; set; }

        [Description("True when the case was picked at random for a quality review and is still open. Such a case " +
                     "is ordered ahead of ordinary standard cases.")]
        public bool QaSample { get; set; }

        [Description("Money involved, read from the workflow value field; null when none.")]
        public double? Value { get; set; }

        [Description("The latest risk of the case.")]
        public CaseRiskSnapshotDto Risk { get; set; } = new();

        [Description("Why the case needs the caller again.")]
        public CaseAttentionDto Attention { get; set; } = new();

        [Description("Where the case stands against its service levels.")]
        public CaseServiceLevelDto ServiceLevel { get; set; } = new();

        [Description("When anyone first looked at the case (UTC).")]
        public DateTimeOffset? FirstViewedDate { get; set; }

        [Description("Who first looked at the case.")]
        public string? FirstViewedBy { get; set; }

        [Description("When someone first took ownership by locking the case (UTC).")]
        public DateTimeOffset? FirstOwnedDate { get; set; }

        [Description("Who first took ownership.")]
        public string? FirstOwnedBy { get; set; }

        [Description("The most recent audit event on the case: 1 Automatic Unlock through 16 Peek.")]
        public int? LastEventTypeId { get; set; }

        [Description("Who caused the most recent audit event.")]
        public string? LastEventUser { get; set; }

        [Description("When the most recent audit event happened (UTC).")]
        public DateTimeOffset? LastEventDate { get; set; }
    }
}
