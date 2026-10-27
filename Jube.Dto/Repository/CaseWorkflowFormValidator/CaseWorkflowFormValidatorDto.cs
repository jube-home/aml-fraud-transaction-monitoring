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

namespace Jube.Dto.Repository.CaseWorkflowFormValidator
{
    [FormEndpoint("CaseWorkflowFormValidator")]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Rule", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class CaseWorkflowFormValidatorDto : IUpdated, IActivatable
    {
        [Description("Identifier of the Case Workflow this validator's Form belongs to. Set from the parent Case " +
                     "Workflow context; not user-editable.")]
        public int CaseWorkflowId { get; set; }

        [Description("Guid of the Case Workflow Form this validator checks the submission of. The validator runs " +
                     "when that Form is submitted.")]
        [FormField(Group = "Identity", Order = 5, Widget = "select", Label = "Form")]
        [Lookup("/api/CaseWorkflowForm/ByCaseWorkflowId", TextField = "name", ValueField = "guid",
            ParentField = nameof(CaseWorkflowId))]
        [ListColumn(Hidden = true)]
        public Guid CaseWorkflowFormGuid { get; set; }

        [Description("Display name of the validator, shown in the list of rules of the Form.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Amount is positive")]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, the validator runs when the Form is submitted.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 40, Title = "Active")]
        public bool Active { get; set; }

        [Description("The name attribute of the element in the Form template that this validator checks. It is " +
                     "paired with the element by name, ignoring case, and must be a field of the template.")]
        [FormField(Group = "Identity", Order = 30, Placeholder = "e.g. amount")]
        [ListColumn(Order = 20, Title = "Field")]
        public string? FieldName { get; set; }

        [Description("The kind of rule: 1 = Required, 2 = Required when another field equals a value, " +
                     "3 = Range of numbers or dates, 4 = Length in characters, 5 = Pattern as a regular " +
                     "expression, 6 = Compare with another field, 7 = In a model list, 8 = In a model dictionary, " +
                     "9 = SQL that must return true.")]
        [FormField(Group = "Rule", Order = 10, Widget = "select")]
        [Options(typeof(CaseWorkflowFormValidatorKind))]
        [ListColumn(Order = 30, Title = "Kind")]
        [NewDefault(1)]
        public int KindId { get; set; }

        [Description("First argument, by kind. Required when another field equals a value: the other field's " +
                     "name. Range: the minimum, blank for none. Length: the minimum number of characters, blank " +
                     "for none. Pattern: the regular expression. Compare: the relation, one of eq, ne, lt, le, gt " +
                     "or ge. In a model list or dictionary: its name.")]
        [FormField(Group = "Rule", Order = 20)]
        [VisibleWhen(nameof(KindId), 2, 3, 4, 5, 6, 7, 8)]
        public string? Argument1 { get; set; }

        [Description("Second argument, by kind. Required when another field equals a value: the value. Range: " +
                     "the maximum, blank for none. Length: the maximum number of characters, blank for none. " +
                     "Compare: the name of the other field.")]
        [FormField(Group = "Rule", Order = 30)]
        [VisibleWhen(nameof(KindId), 2, 3, 4, 6)]
        public string? Argument2 { get; set; }

        [Description("For the SQL kind, a single SELECT that returns exactly one boolean; the rule passes only " +
                     "when it is true, so cast the result to boolean. It may use the bound parameters @value, " +
                     "@CaseId, @CaseKeyValue and @ followed by the name of any field of the Form. It runs read " +
                     "only, against the tenant's own rows, with a short timeout, and any failure counts as a " +
                     "failed validation. Authoring a SQL validator needs its own permission.")]
        [FormField(Group = "Rule", Order = 40, Widget = "code")]
        [VisibleWhen(nameof(KindId), 9)]
        public string? Sql { get; set; }

        [Description("Optional text shown to the investigator when the rule fails, in place of the standard " +
                     "message for the kind.")]
        [FormField(Group = "Rule", Order = 50)]
        public string? Message { get; set; }

        [Description("Server-assigned globally-unique identifier of this validator. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing validator.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this validator. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this validator. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this validator, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this validator was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
