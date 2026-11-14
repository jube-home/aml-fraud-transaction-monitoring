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

namespace Jube.Dto.Query.CaseSearch
{
    [Description("One case in the triage list with its risk, why it needs attention and where it stands against " +
                 "its service levels.")]
    [MessagePackObject]
    public class CaseSearchRowDto
    {
        [Description("Identifier of the case.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("Name of the case key, such as AccountId.")]
        [Key(1)]
        public string? CaseKey { get; set; }

        [Description("Value of the case key.")]
        [Key(2)]
        public string? CaseKeyValue { get; set; }

        [Description("When the case was created (UTC).")]
        [Key(3)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("Minutes since the case was created, or until it closed.")]
        [Key(4)]
        public double AgeMinutes { get; set; }

        [Description("Guid of the Case Workflow.")]
        [Key(5)]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Name of the Case Workflow.")]
        [Key(6)]
        public string? CaseWorkflowName { get; set; }

        [Description("Guid of the Case Workflow Status the case is in.")]
        [Key(7)]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Name of the Status.")]
        [Key(8)]
        public string? StatusName { get; set; }

        [Description("Hex text colour of the Status.")]
        [Key(9)]
        public string? ForeColor { get; set; }

        [Description("Hex background colour of the Status.")]
        [Key(10)]
        public string? BackColor { get; set; }

        [Description("When the case entered its current status (UTC).")]
        [Key(11)]
        public DateTimeOffset? StatusEnteredDate { get; set; }

        [Description("True when the case is locked; the lock is its assignment.")]
        [Key(12)]
        public bool Locked { get; set; }

        [Description("The user the case is locked to.")]
        [Key(13)]
        public string? LockedUser { get; set; }

        [Description("True when the case is locked to the caller.")]
        [Key(14)]
        public bool Mine { get; set; }

        [Description("Closed status: 0 Open, 1 Suspend open, 2 Suspend closed, 3 Closed, 4 Suspend bypass.")]
        [Key(15)]
        public int ClosedStatusId { get; set; }

        [Description("True when a diary is set.")]
        [Key(16)]
        public bool Diary { get; set; }

        [Description("When the diary falls due (UTC).")]
        [Key(17)]
        public DateTimeOffset? DiaryDate { get; set; }

        [Description("What the diary is for: 1 Review, 2 Customer response, 3 Regulatory deadline, 4 Payment hold. " +
                     "Meaningful only while a diary is set.")]
        [Key(18)]
        public int DiaryTypeId { get; set; }

        [Description("Name of the diary type.")]
        [Key(19)]
        public string? DiaryType { get; set; }

        [Description("True when the case was picked at random for a quality review and is still open. Such a case " +
                     "is ordered ahead of ordinary standard cases.")]
        [Key(20)]
        public bool QaSample { get; set; }

        [Description("Money involved, read from the workflow value field; null when none.")]
        [Key(21)]
        public double? Value { get; set; }

        [Description("The latest risk of the case.")]
        [Key(22)]
        public CaseRiskSnapshotDto Risk { get; set; } = new();

        [Description("Why the case needs the caller again.")]
        [Key(23)]
        public CaseAttentionDto Attention { get; set; } = new();

        [Description("Where the case stands against its service levels.")]
        [Key(24)]
        public CaseServiceLevelDto ServiceLevel { get; set; } = new();

        [Description("When anyone first looked at the case (UTC).")]
        [Key(25)]
        public DateTimeOffset? FirstViewedDate { get; set; }

        [Description("Who first looked at the case.")]
        [Key(26)]
        public string? FirstViewedBy { get; set; }

        [Description("When someone first took ownership by locking the case (UTC).")]
        [Key(27)]
        public DateTimeOffset? FirstOwnedDate { get; set; }

        [Description("Who first took ownership.")]
        [Key(28)]
        public string? FirstOwnedBy { get; set; }

        [Description("How many checklist items of the status the case is in are shown and still open. Zero for a " +
                     "closed case or one whose checklist has never been brought up to date.")]
        [Key(29)]
        public int ChecklistOpen { get; set; }

        [Description("How many of the open checklist items have breached their SLA.")]
        [Key(30)]
        public int ChecklistBreached { get; set; }

        [Description("The most recent audit event on the case: 1 Automatic Unlock through 16 Peek.")]
        [Key(31)]
        public int? LastEventTypeId { get; set; }

        [Description("Who caused the most recent audit event.")]
        [Key(32)]
        public string? LastEventUser { get; set; }

        [Description("When the most recent audit event happened (UTC).")]
        [Key(33)]
        public DateTimeOffset? LastEventDate { get; set; }
    }
}
