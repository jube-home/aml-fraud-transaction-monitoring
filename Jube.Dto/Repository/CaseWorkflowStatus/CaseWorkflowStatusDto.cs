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
using Jube.Dto.Forms;
using Jube.Dto.Options;
using Jube.Dto.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseWorkflowStatus
{
    [FormEndpoint("CaseWorkflowStatus")]
    [CaseSqlEditor(Sql = nameof(Sql))]
    [FormRoles]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Display", Order = 20)]
    [FormGroup("HttpEndpoint", Order = 30, Collapsed = true)]
    [FormGroup("Notification", Order = 40, Collapsed = true)]
    [FormGroup("Outcome", Order = 50, Collapsed = true)]
    [FormGroup("ServiceLevel", Order = 60, Collapsed = true)]
    [FormGroup("Escalation", Order = 70, Collapsed = true)]
    [FormGroup("Sql", Order = 5, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class CaseWorkflowStatusDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Case Workflow this Status belongs to. Set from the parent Case " +
                     "Workflow context; not user-editable.")]
        public int CaseWorkflowId { get; set; }

        [Description("Display name of the Status, shown to investigators when allocating or changing a Case's " +
                     "status. Unique within the Case Workflow (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Awaiting customer response")]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, the Status is available for allocation to a Case.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, the Status is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        public bool Locked { get; set; }

        [Description("Escalation priority of this Status: 1 = Ultra High, 2 = High, 3 = Medium, 4 = Low, " +
                     "5 = Ultra Low. A Case's priority may be raised as its Status escalates, but never lowered.")]
        [FormField(Group = "Display", Order = 10, Widget = "select")]
        [ListColumn(Order = 30, Title = "Priority")]
        [Options(typeof(CasePriority))]
        public int Priority { get; set; }

        [Description("What closing a Case in this Status says about the alert: 0 = Not an outcome, 1 = Confirmed " +
                     "(a true positive), 2 = Not confirmed (a false positive). The case risk likelihood is " +
                     "calibrated against closed Cases in Confirmed and Not confirmed Statuses; suspend-bypass " +
                     "closures are never counted.")]
        [FormField(Group = "Outcome", Order = 10, Widget = "select")]
        [ListColumn(Order = 40, Title = "Outcome")]
        [Options(typeof(CaseStatusOutcome))]
        public int OutcomeId { get; set; }

        [Description("Service level: the most minutes a Case may stay in this Status before it counts as overdue " +
                     "there. Leave empty for no target.")]
        [FormField(Group = "ServiceLevel", Order = 10, Placeholder = "e.g. 240")]
        public int? MaxDwellMinutes { get; set; }

        [Description("Escalation automation: what makes an open Case in this Status move on by itself: 1 = Time " +
                     "in this Status reaches the threshold in minutes, 2 = the Case is older than the Case " +
                     "Workflow resolution target, 3 = the Case lane is at or more urgent than the threshold lane " +
                     "(1 Live money, 2 Mandatory, 3 Deadline near), 4 = the Case Priority is at or above the " +
                     "threshold, 5 = the Case diary deadline has passed. Leave empty for no automatic escalation.")]
        [FormField(Group = "Escalation", Order = 10, Widget = "select")]
        [Options(typeof(CaseEscalationTrigger))]
        [RequiredWhen(nameof(EscalationTargetCaseWorkflowStatusGuid), Op = ConditionOp.IsNotEmpty)]
        public int? EscalationTriggerId { get; set; }

        [Description("Escalation automation: the minutes, lane (1 to 3) or Priority (1 to 100) that the trigger " +
                     "compares against. Required for triggers 1, 3 and 4; not used by 2 and 5.")]
        [FormField(Group = "Escalation", Order = 20, Placeholder = "e.g. 240")]
        [VisibleWhen(nameof(EscalationTriggerId), 1, 3, 4, Op = ConditionOp.In)]
        [RequiredWhen(nameof(EscalationTriggerId), 1, 3, 4, Op = ConditionOp.In)]
        public int? EscalationThreshold { get; set; }

        [Description("Escalation automation: the Status of the same Case Workflow the Case moves to. Required " +
                     "when a trigger is set. It must be a stronger priority than this Status (a lower Priority " +
                     "number) so that automatic escalation can never loop.")]
        [FormField(Group = "Escalation", Order = 30, Widget = "select")]
        [Lookup("/api/CaseWorkflowStatus/ByCaseWorkflowId", TextField = "name", ValueField = "guid",
            ParentField = nameof(CaseWorkflowId), AllowEmpty = true)]
        [RequiredWhen(nameof(EscalationTriggerId), Op = ConditionOp.IsNotEmpty)]
        public Guid? EscalationTargetCaseWorkflowStatusGuid { get; set; }

        [Description("Escalation automation: when true, releases the lock held on the Case as it moves, so " +
                     "that another analyst may take it.")]
        [FormField(Group = "Escalation", Order = 40, Widget = "switch")]
        public bool EscalationReleaseLock { get; set; }

        [Description("Hex colour of the Status text shown on the Case status bar, e.g. #0000FF.")]
        [FormField(Group = "Display", Order = 20, Widget = "colour")]
        public string? ForeColor { get; set; }

        [Description("Hex colour of the Status background shown on the Case status bar, e.g. #880808.")]
        [FormField(Group = "Display", Order = 30, Widget = "colour")]
        public string? BackColor { get; set; }

        [Description("When true, a change to this Status dispatches a webhook.")]
        [FormField(Group = "HttpEndpoint", Order = 10, Widget = "switch")]
        public bool EnableHttpEndpoint { get; set; }

        [Description("Webhook URL to call. Required when EnableHttpEndpoint is true.")]
        [FormField(Group = "HttpEndpoint", Order = 20, Placeholder = "https://example.com/hooks/case")]
        [VisibleWhen(nameof(EnableHttpEndpoint), true)]
        [RequiredWhen(nameof(EnableHttpEndpoint), true)]
        public string? HttpEndpoint { get; set; }

        [Description("HTTP method used for the webhook call: 1 = POST, 2 = GET. Required when " +
                     "EnableHttpEndpoint is true.")]
        [FormField(Group = "HttpEndpoint", Order = 30, Widget = "radio")]
        [VisibleWhen(nameof(EnableHttpEndpoint), true)]
        [Options(typeof(HttpEndpointType))]
        [NewDefault(1)]
        public int HttpEndpointTypeId { get; set; }

        [Description("When true, a change to this Status dispatches a notification.")]
        [FormField(Group = "Notification", Order = 10, Widget = "switch")]
        public bool EnableNotification { get; set; }

        [Description("Notification channel type: 1 = Email, 2 = SMS. Required when EnableNotification is true.")]
        [FormField(Group = "Notification", Order = 20, Widget = "radio")]
        [VisibleWhen(nameof(EnableNotification), true)]
        [Options(typeof(NotificationType))]
        [NewDefault(1)]
        public int NotificationTypeId { get; set; }

        [Description("Notification destination (e.g. an email address). Required when EnableNotification is " +
                     "true.")]
        [FormField(Group = "Notification", Order = 30, Placeholder = "name@example.com")]
        [VisibleWhen(nameof(EnableNotification), true)]
        [RequiredWhen(nameof(EnableNotification), true)]
        public string? NotificationDestination { get; set; }

        [Description("Notification subject line.")]
        [FormField(Group = "Notification", Order = 40, Placeholder = "Case [@CaseKey] needs review")]
        [VisibleWhen(nameof(EnableNotification), true)]
        public string? NotificationSubject { get; set; }

        [Description("Notification body. Supports token substitution from the Case whose Status changed.")]
        [FormField(Group = "Notification", Order = 50, Widget = "textarea", Placeholder = "Please review the case for [@AccountId].")]
        [VisibleWhen(nameof(EnableNotification), true)]
        public string? NotificationBody { get; set; }

        [Description("Optional. A single SELECT run for a case, read only against the tenant's own rows, whose rows " +
                     "the layout and the notification and HTTP templates are drawn from, as rows, with tokens such " +
                     "as [@ rows[0].Name @]. It may use the bound parameters @Id, the id of the case, and " +
                     "@CaseKeyValue. When empty, the earlier behaviour of drawing from the case's stored payload " +
                     "applies. Authoring SQL needs its own permission.")]
        [FormField(Group = "Sql", Order = 10, Widget = "code")]
        public string? Sql { get; set; }

        [Description("Server-assigned globally-unique identifier of this Status, referenced by Case Workflow " +
                     "Status Roles and by Cases that hold this Status. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Status.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this Status. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this Status. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Status, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this Status was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}