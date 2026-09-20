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
using Jube.Dto.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.CaseWorkflowDisplay
{
    [FormEndpoint("CaseWorkflowDisplay")]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Template", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class CaseWorkflowDisplayDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Case Workflow this Display belongs to. Set from the parent Case " +
                     "Workflow context; not user-editable.")]
        public int CaseWorkflowId { get; set; }

        [Description("Display name of the template, shown to investigators when choosing a Display for a Case.")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column -- " +
                     "the server never persists a submitted value and always returns null.")]
        public string? Description { get; set; }

        [Description("When true, the Display is available for selection on a Case.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, the Display is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        public bool Locked { get; set; }

        [Description("HTML template rendered on the Case page, with token substitution against the Case's " +
                     "archived transaction data. Required.")]
        [FormField(Group = "Template", Order = 10, Widget = "code-editor")]
        public string? Html { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column on " +
                     "this area -- the server never persists a submitted value and always returns the default. " +
                     "Unlike CaseWorkflowAction/CaseNote, this Display concept never had HTTP-endpoint dispatch.")]
        public bool EnableHttpEndpoint { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        public string? HttpEndpoint { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        public int HttpEndpointTypeId { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        public bool EnableNotification { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        public int NotificationType { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        public string? NotificationDestination { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        public string? NotificationSubject { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        public string? NotificationBody { get; set; }

        [Description("Server-assigned globally-unique identifier of this Display, referenced by Case Workflow " +
                     "Display Roles. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Display.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this Display. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this Display. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Display, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this Display was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}