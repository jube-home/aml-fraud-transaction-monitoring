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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseWorkflowDisplay
{
    [FormEndpoint("CaseWorkflowDisplay")]
    [CaseSqlEditor(Sql = nameof(Sql))]
    [FormRoles]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Template", Order = 20)]
    [FormGroup("Sql", Order = 5, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class CaseWorkflowDisplayDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Case Workflow this Display belongs to. Set from the parent Case " +
                     "Workflow context; not user-editable.")]
        [Key(0)]
        public int CaseWorkflowId { get; set; }

        [Description("Display name of the template, shown to investigators when choosing a Display for a Case.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Transaction summary")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column -- " +
                     "the server never persists a submitted value and always returns null.")]
        [Key(2)]
        public string? Description { get; set; }

        [Description("When true, the Display is available for selection on a Case.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [Key(3)]
        public bool Active { get; set; }

        [Description("When true, the Display is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [Key(4)]
        public bool Locked { get; set; }

        [Description("HTML template rendered on the Case page, with token substitution against the Case's " +
                     "archived transaction data. Required.")]
        [FormField(Group = "Template", Order = 10, Widget = "code")]
        [Key(5)]
        public string? Html { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column on " +
                     "this area -- the server never persists a submitted value and always returns the default. " +
                     "Unlike CaseWorkflowAction/CaseNote, this Display concept never had HTTP-endpoint dispatch.")]
        [Key(6)]
        public bool EnableHttpEndpoint { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(7)]
        public string? HttpEndpoint { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(8)]
        public int HttpEndpointTypeId { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(9)]
        public bool EnableNotification { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(10)]
        public int NotificationType { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(11)]
        public string? NotificationDestination { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(12)]
        public string? NotificationSubject { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(13)]
        public string? NotificationBody { get; set; }

        [Description("Optional. A single SELECT run for a case, read only against the tenant's own rows, whose rows " +
                     "the layout and the notification and HTTP templates are drawn from, as rows, with tokens such " +
                     "as [@ rows[0].Name @]. It may use the bound parameters @Id, the id of the case, and " +
                     "@CaseKeyValue. When empty, the earlier behaviour of drawing from the case's stored payload " +
                     "applies. Authoring SQL needs its own permission.")]
        [FormField(Group = "Sql", Order = 10, Widget = "code")]
        [Key(14)]
        public string? Sql { get; set; }

        [Description("Server-assigned globally-unique identifier of this Display, referenced by Case Workflow " +
                     "Display Roles. Read-only.")]
        [Key(15)]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Display.")]
        [Key(16)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(17)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this Display. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(18)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this Display. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(19)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(20)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(21)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Display, if any. Read-only.")]
        [Key(22)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this Display was soft-deleted, if any. Read-only.")]
        [Key(23)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}