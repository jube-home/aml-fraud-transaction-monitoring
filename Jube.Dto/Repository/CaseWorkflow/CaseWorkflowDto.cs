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
namespace Jube.Dto.Repository.CaseWorkflow
{
    [FormEndpoint("CaseWorkflow")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Visualisation", Order = 20, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class CaseWorkflowDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Model this Case Workflow belongs to. Set from the parent Model context; " +
                     "not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of the Case Workflow, shown to investigators for navigation. Unique within " +
                     "the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column -- " +
                     "the server never persists a submitted value and always returns null. The legacy edit form " +
                     "never exposed an input for it either.")]
        public string? Description { get; set; }

        [Description("When true, the Case Workflow participates in case creation -- new cases can be raised " +
                     "against it.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, the case is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        public bool Locked { get; set; }

        [Description("When true, cases raised against this workflow present a visualisation built from the " +
                     "case's transaction payload.")]
        [FormField(Group = "Visualisation", Order = 10, Widget = "switch")]
        public bool EnableVisualisation { get; set; }

        [Description("Identifier of the Visualisation Registry entry to present. Required when " +
                     "EnableVisualisation is true.")]
        [FormField(Group = "Visualisation", Order = 20, Widget = "dropdown")]
        [VisibleWhen(nameof(EnableVisualisation), true)]
        [RequiredWhen(nameof(EnableVisualisation), true)]
        [Lookup("VisualisationRegistry", TextField = "name", ValueField = "guid",
            ParentField = nameof(EntityAnalysisModelId))]
        public Guid VisualisationRegistryGuid { get; set; }

        [Description("Server-assigned globally-unique identifier of this Case Workflow, referenced by cases and " +
                     "activation rules. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Case Workflow.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this Case Workflow. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this Case Workflow. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Case Workflow, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this Case Workflow was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}