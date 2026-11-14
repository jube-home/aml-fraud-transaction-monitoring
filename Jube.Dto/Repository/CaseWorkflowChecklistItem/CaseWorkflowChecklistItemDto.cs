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

namespace Jube.Dto.Repository.CaseWorkflowChecklistItem
{
    [FormEndpoint("CaseWorkflowChecklistItem")]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowChecklistGuid), NaturalKey = nameof(Name))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Show", Order = 20)]
    [FormGroup("Validation", Order = 30)]
    [FormGroup("ServiceLevel", Order = 40, Label = "Service Level")]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class CaseWorkflowChecklistItemDto : IUpdated, IActivatable
    {
        [Description("Identifier of the Case Workflow this item's checklist belongs to. Server-assigned from the " +
                     "checklist; read-only.")]
        [Key(0)]
        public int CaseWorkflowId { get; set; }

        [Description("Guid of the checklist that carries this item. Set from the parent checklist context; not " +
                     "user-editable.")]
        [Key(1)]
        public Guid CaseWorkflowChecklistGuid { get; set; }

        [Description("Display name of the item, shown to investigators on a case.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Record an investigation note")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("Ordering among the items of the checklist. A lower number is listed first, as for Case " +
                     "Workflow Status; ties are broken by name. Zero or more.")]
        [FormField(Group = "Identity", Order = 20, Widget = "number", Placeholder = "e.g. 1")]
        [ListColumn(Order = 20, Title = "Priority")]
        [NewDefault(0)]
        [Key(3)]
        public int Priority { get; set; }

        [Description("When true, the item is carried by its checklist. An inactive item is not shown on cases.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [ListColumn(Order = 40, Title = "Active")]
        [Key(4)]
        public bool Active { get; set; }

        [Description("When true, the show condition is evaluated and the item is shown on a case only while it " +
                     "passes. When false the item is always shown on a case in its status, and any show SQL is " +
                     "retained but not run, so it can be switched back on.")]
        [FormField(Group = "Show", Order = 10, Widget = "switch")]
        [Key(5)]
        public bool EnableShow { get; set; }

        [Description("A single SELECT that returns exactly one boolean; the item is shown on a case only while it " +
                     "is true. Required when EnableShow is true, and retained when it is false. It may use the " +
                     "bound parameters @Id, the case id, and @CaseKeyValue. It runs read only, against the tenant's " +
                     "own rows, with a short timeout, and any failure counts as not shown. Authoring SQL needs its " +
                     "own permission.")]
        [FormField(Group = "Show", Order = 20, Widget = "code")]
        [VisibleWhen(nameof(EnableShow), true)]
        [RequiredWhen(nameof(EnableShow), true)]
        [Key(6)]
        public string? ShowSql { get; set; }

        [Description("When true, the validation condition is run when the item is ticked and the tick is refused " +
                     "unless it passes. When false the item can always be ticked, and any validation SQL and " +
                     "message are retained but not run, so they can be switched back on.")]
        [FormField(Group = "Validation", Order = 10, Widget = "switch")]
        [Key(7)]
        public bool EnableValidation { get; set; }

        [Description("A single SELECT that returns exactly one boolean; the item can be ticked only while it is " +
                     "true. Required when EnableValidation is true, and retained when it is false. Same parameters " +
                     "and limits as the show condition; any failure counts as a failed validation. Authoring SQL " +
                     "needs its own permission.")]
        [FormField(Group = "Validation", Order = 20, Widget = "code")]
        [VisibleWhen(nameof(EnableValidation), true)]
        [RequiredWhen(nameof(EnableValidation), true)]
        [Key(8)]
        public string? ValidateSql { get; set; }

        [Description("Optional text shown to the investigator when the validation SQL fails, in place of the " +
                     "standard message. Applies only while EnableValidation is true.")]
        [FormField(Group = "Validation", Order = 30)]
        [VisibleWhen(nameof(EnableValidation), true)]
        [Key(9)]
        public string? ValidateMessage { get; set; }

        [Description("Optional. Minutes allowed from the moment the item is shown on a case to its being ticked; " +
                     "the item is shown in red once breached. Leave blank for no SLA.")]
        [FormField(Group = "ServiceLevel", Order = 10, Widget = "number", Placeholder = "e.g. 60")]
        [ListColumn(Order = 30, Title = "SLA minutes")]
        [Key(10)]
        public int? SlaMinutes { get; set; }

        [Description("Server-assigned globally-unique identifier of this item. Read-only.")]
        [Key(11)]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing item.")]
        [Key(12)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(13)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this item. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(14)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this item. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(15)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(16)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(17)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this item, if any. Read-only.")]
        [Key(18)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this item was soft-deleted, if any. Read-only.")]
        [Key(19)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
