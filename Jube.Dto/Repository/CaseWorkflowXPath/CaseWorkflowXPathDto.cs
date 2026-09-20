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
namespace Jube.Dto.Repository.CaseWorkflowXPath
{
    [FormEndpoint("CaseWorkflowXPath")]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Extraction", Order = 20)]
    [FormGroup("BoldLineMatching", Order = 30, Label = "Bold Line Matching", Collapsed = true)]
    [FormGroup("ConditionalFormatting", Order = 40, Label = "Conditional Formatting", Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class CaseWorkflowXPathDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Case Workflow this XPath extraction belongs to. Set from the parent " +
                     "Case Workflow context; not user-editable.")]
        public int CaseWorkflowId { get; set; }

        [Description("Display name of the extraction, shown as the column heading in the Case Key Journal grid.")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, the extraction participates in Case Key Journal presentation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, this extraction is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        public bool Locked { get; set; }

        [Description("The JSONPath expression used to extract the value from the event/transaction payload " +
                     "JSON for presentation in the Case Key Journal.")]
        [FormField(Group = "Extraction", Order = 10)]
        public string? XPath { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column -- " +
                     "the legacy page never bound this field to a control, the server never persists a " +
                     "submitted value, and it always returns null.")]
        public string? DisplayName { get; set; }

        [Description("When true, the extracted value is treated as a Drill field: clicking it fills the Case " +
                     "Key Journal with every event whose extracted value matches. The field should be indexed " +
                     "(Archive carries a GIN index by default) since a Drill query scans by this extracted value.")]
        [FormField(Group = "Extraction", Order = 20, Widget = "switch")]
        public bool Drill { get; set; }

        [Description("When true, the extracted value for this row is compared to the same value in the case " +
                     "creation payload; on a match the row is rendered using the Bold Line colours.")]
        [FormField(Group = "BoldLineMatching", Order = 10, Widget = "switch")]
        [RequiredWhen(nameof(BoldLineMatched), true)]
        public bool BoldLineMatched { get; set; }

        [Description("Text colour applied to the row when Bold Line Matched is true and the value matches. " +
                     "Required when Bold Line Matched is enabled.")]
        [FormField(Group = "BoldLineMatching", Order = 20, Widget = "colour")]
        [VisibleWhen(nameof(BoldLineMatched), true)]
        [RequiredWhen(nameof(BoldLineMatched), true)]
        public string? BoldLineFormatForeColor { get; set; }

        [Description("Background colour applied to the row when Bold Line Matched is true and the value " +
                     "matches. Required when Bold Line Matched is enabled.")]
        [FormField(Group = "BoldLineMatching", Order = 30, Widget = "colour")]
        [VisibleWhen(nameof(BoldLineMatched), true)]
        [RequiredWhen(nameof(BoldLineMatched), true)]
        public string? BoldLineFormatBackColor { get; set; }

        [Description("When true, the extracted value is additionally matched against a regular expression to " +
                     "drive per-row or per-cell conditional formatting.")]
        [FormField(Group = "ConditionalFormatting", Order = 10, Widget = "switch")]
        public bool ConditionalRegularExpressionFormatting { get; set; }

        [Description("The regular expression evaluated against the extracted value. Required when Conditional " +
                     "Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 20)]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        public string? RegularExpression { get; set; }

        [Description("Text colour applied on a Regular Expression match. Required when Conditional Formatting " +
                     "is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 30, Widget = "colour")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        public string? ConditionalFormatForeColor { get; set; }

        [Description("When true, the fore colour on a Regular Expression match is applied to the whole row " +
                     "rather than just the matched cell. Required when Conditional Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 40, Widget = "switch")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        public bool ForeRowColorScope { get; set; }

        [Description("Background colour applied on a Regular Expression match. Required when Conditional " +
                     "Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 50, Widget = "colour")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        public string? ConditionalFormatBackColor { get; set; }

        [Description("When true, the back colour on a Regular Expression match is applied to the whole row " +
                     "rather than just the matched cell. Required when Conditional Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 60, Widget = "switch")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        public bool BackRowColorScope { get; set; }

        [Description("Server-assigned globally-unique identifier of this extraction. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing row.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this row. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this row. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this row, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this row was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}