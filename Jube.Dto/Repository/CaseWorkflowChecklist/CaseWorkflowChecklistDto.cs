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
using Jube.Dto.Repository.CaseWorkflowChecklistItem;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseWorkflowChecklist
{
    [FormChild(typeof(CaseWorkflowChecklistItemDto), Label = "Items", Hub = "", Order = 10)]
    [FormEndpoint("CaseWorkflowChecklist")]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowStatusId), NaturalKey = nameof(Name))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class CaseWorkflowChecklistDto : IUpdated, IActivatable
    {
        [Description("Identifier of the Case Workflow Status this checklist belongs to. Set from the parent Case " +
                     "Workflow Status context; not user-editable.")]
        [Key(0)]
        public int CaseWorkflowStatusId { get; set; }

        [Description("Guid of the Case Workflow Status that carries this checklist. Optional when the status is " +
                     "given by its identifier; the server resolves it. Every item shown on a case in that status " +
                     "must be ticked before the case can leave the status or be closed.")]
        [Key(1)]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Display name of the checklist, shown to investigators on a case in the status.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Initial triage")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("Ordering among the checklists of the status. A lower number is listed first, as for Case " +
                     "Workflow Status; ties are broken by name. Blank is stored as 0.")]
        [FormField(Group = "Identity", Order = 20, Widget = "number", Placeholder = "e.g. 1")]
        [ListColumn(Order = 20, Title = "Priority")]
        [Key(3)]
        public int? Priority { get; set; }

        [Description("When true, the checklist is carried by the status. An inactive checklist is not shown on " +
                     "cases.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [ListColumn(Order = 30, Title = "Active")]
        [Key(4)]
        public bool Active { get; set; }

        [Description("Server-assigned globally-unique identifier of this checklist. Read-only.")]
        [Key(5)]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing checklist.")]
        [Key(6)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(7)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this checklist. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(8)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this checklist. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(9)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(10)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(11)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this checklist, if any. Read-only.")]
        [Key(12)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this checklist was soft-deleted, if any. Read-only.")]
        [Key(13)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
