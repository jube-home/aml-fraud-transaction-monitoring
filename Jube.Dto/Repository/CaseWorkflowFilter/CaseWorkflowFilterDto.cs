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

namespace Jube.Dto.Repository.CaseWorkflowFilter
{
    [FormEndpoint("CaseWorkflowFilter")]
    [FormRoles]
    [CaseFilterEditor(Select = nameof(SelectJson), Filter = nameof(FilterJson))]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Filter", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class CaseWorkflowFilterDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Case Workflow this Filter belongs to. Set from the parent Case " +
                     "Workflow context; not user-editable.")]
        [Key(0)]
        public int CaseWorkflowId { get; set; }

        [Description(
            "Display name of the saved filter, shown to investigators when choosing a Filter for a Case search.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Open cases for my team")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column -- " +
                     "the server never persists a submitted value and always returns null.")]
        [Key(2)]
        public string? Description { get; set; }

        [Description("When true, the Filter is available for selection on a Case search.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [Key(3)]
        public bool Active { get; set; }

        [Description("When true, the Filter is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [Key(4)]
        public bool Locked { get; set; }

        [Description("The query-builder's selected-column configuration, as JSON. Required.")]
        [FormField(Group = "Filter", Order = 10)]
        [Key(5)]
        public string? SelectJson { get; set; }

        [Description("The query-builder's filter-condition configuration, as JSON. Required.")]
        [FormField(Group = "Filter", Order = 20)]
        [Key(6)]
        public string? FilterJson { get; set; }

        [Description("The query-builder's parameter/token values, as JSON.")]
        [FormField(Group = "Filter", Order = 30, ReadOnly = true)]
        [Key(7)]
        public string? FilterTokens { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column on " +
                     "this area -- the server never persists a submitted value and always returns the default. " +
                     "This Filter concept never had HTTP-endpoint dispatch.")]
        [Key(8)]
        public bool EnableHttpEndpoint { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(9)]
        public string? HttpEndpoint { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(10)]
        public int HttpEndpointTypeId { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(11)]
        public bool EnableNotification { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(12)]
        public int NotificationType { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(13)]
        public string? NotificationDestination { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(14)]
        public string? NotificationSubject { get; set; }

        [Description("Inert -- see EnableHttpEndpoint. Never persisted.")]
        [Key(15)]
        public string? NotificationBody { get; set; }

        [Description("Server-assigned globally-unique identifier of this Filter, referenced by Case Workflow " +
                     "Filter Roles. Read-only.")]
        [Key(16)]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Filter.")]
        [Key(17)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(18)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this Filter. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(19)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this Filter. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(20)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(21)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(22)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Filter, if any. Read-only.")]
        [Key(23)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this Filter was soft-deleted, if any. Read-only.")]
        [Key(24)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}