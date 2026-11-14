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
using Jube.Dto.Options;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseWorkflowXPath
{
    [FormEndpoint("CaseWorkflowXPath")]
    [FormRoles]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Extraction", Order = 20)]
    [FormGroup("Analytics", Order = 25)]
    [FormGroup("BoldLineMatching", Order = 30, Label = "Bold Line Matching", Collapsed = true)]
    [FormGroup("ConditionalFormatting", Order = 40, Label = "Conditional Formatting", Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class CaseWorkflowXPathDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Case Workflow this XPath extraction belongs to. Set from the parent " +
                     "Case Workflow context; not user-editable.")]
        [Key(0)]
        public int CaseWorkflowId { get; set; }

        [Description("Display name of the extraction, shown as the column heading in the Case Key Journal grid.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Merchant name")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("When true, the extraction participates in Case Key Journal presentation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [Key(2)]
        public bool Active { get; set; }

        [Description("When true, this extraction is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [Key(3)]
        public bool Locked { get; set; }

        [Description("The model field the extraction reads, chosen from the request XPaths, inline functions, " +
                     "abstraction rules, abstraction calculations, TTL counters, HTTP adaptations and activation " +
                     "rules of the workflow's model (the model itself stands for its entry key). The JSON path, " +
                     "the archive key and the data type all derive from it, and the extraction can be drilled " +
                     "exactly when the field is flagged Archive Key.")]
        [FormField(Group = "Extraction", Order = 10, Widget = "select")]
        [Lookup("/api/CaseWorkflowXPath/Fields", TextField = "label", ValueField = "guid",
            ParentField = nameof(CaseWorkflowId))]
        [Key(4)]
        public Guid? SourceGuid { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column -- " +
                     "the legacy page never bound this field to a control, the server never persists a " +
                     "submitted value, and it always returns null.")]
        [Key(5)]
        public string? DisplayName { get; set; }

        [Description("The kind of model entity the source field is, by the EntityApprovalKind numbering: 1 entry " +
                     "key, 6 request XPath, 8 inline function, 11 abstraction rule, 12 abstraction calculation, " +
                     "13 TTL counter, 14 HTTP adaptation, 16 activation rule. Set by the server from the chosen " +
                     "field; read-only.")]
        [Key(6)]
        public int SourceKindId { get; set; }

        [Description("What kind of field this is to the Case Key Journal analytics and to network mapping: " +
                     "0 = Inferred from sampled values, 1 = Measure, 2 = Category, 3 = Counterparty, " +
                     "4 = Identifier, 5 = Time, 6 = Geography. Anything other than 0 overrides the inference.")]
        [FormField(Group = "Analytics", Order = 10, Widget = "select")]
        [Options(typeof(JournalFieldRole))]
        [Key(7)]
        public int RoleId { get; set; }

        [Description("When true the field splits the tenant peer distributions, so that a value is compared with " +
                     "peers in the same segment, such as a customer tier.")]
        [FormField(Group = "Analytics", Order = 20, Widget = "switch")]
        [Key(8)]
        public bool Segment { get; set; }

        [Description("When true, the extracted value for this row is compared to the same value in the case " +
                     "creation payload; on a match the row is rendered using the Bold Line colours.")]
        [FormField(Group = "BoldLineMatching", Order = 10, Widget = "switch")]
        [RequiredWhen(nameof(BoldLineMatched), true)]
        [Key(9)]
        public bool BoldLineMatched { get; set; }

        [Description("Text colour applied to the row when Bold Line Matched is true and the value matches. " +
                     "Required when Bold Line Matched is enabled.")]
        [FormField(Group = "BoldLineMatching", Order = 20, Widget = "colour")]
        [VisibleWhen(nameof(BoldLineMatched), true)]
        [RequiredWhen(nameof(BoldLineMatched), true)]
        [Key(10)]
        public string? BoldLineFormatForeColor { get; set; }

        [Description("Background colour applied to the row when Bold Line Matched is true and the value " +
                     "matches. Required when Bold Line Matched is enabled.")]
        [FormField(Group = "BoldLineMatching", Order = 30, Widget = "colour")]
        [VisibleWhen(nameof(BoldLineMatched), true)]
        [RequiredWhen(nameof(BoldLineMatched), true)]
        [Key(11)]
        public string? BoldLineFormatBackColor { get; set; }

        [Description("When true, the extracted value is additionally matched against a regular expression to " +
                     "drive per-row or per-cell conditional formatting.")]
        [FormField(Group = "ConditionalFormatting", Order = 10, Widget = "switch")]
        [Key(12)]
        public bool ConditionalRegularExpressionFormatting { get; set; }

        [Description("The regular expression evaluated against the extracted value. Required when Conditional " +
                     "Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 20, Placeholder = "^HIGH.*")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [Key(13)]
        public string? RegularExpression { get; set; }

        [Description("Text colour applied on a Regular Expression match. Required when Conditional Formatting " +
                     "is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 30, Widget = "colour")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [Key(14)]
        public string? ConditionalFormatForeColor { get; set; }

        [Description("When true, the fore colour on a Regular Expression match is applied to the whole row " +
                     "rather than just the matched cell. Required when Conditional Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 40, Widget = "switch")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [Key(15)]
        public bool ForeRowColorScope { get; set; }

        [Description("Background colour applied on a Regular Expression match. Required when Conditional " +
                     "Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 50, Widget = "colour")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [Key(16)]
        public string? ConditionalFormatBackColor { get; set; }

        [Description("When true, the back colour on a Regular Expression match is applied to the whole row " +
                     "rather than just the matched cell. Required when Conditional Formatting is enabled.")]
        [FormField(Group = "ConditionalFormatting", Order = 60, Widget = "switch")]
        [VisibleWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [RequiredWhen(nameof(ConditionalRegularExpressionFormatting), true)]
        [Key(17)]
        public bool BackRowColorScope { get; set; }

        [Description("Server-assigned globally-unique identifier of this extraction. Read-only.")]
        [Key(18)]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing row.")]
        [Key(19)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(20)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this row. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(21)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this row. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(22)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(23)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(24)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this row, if any. Read-only.")]
        [Key(25)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this row was soft-deleted, if any. Read-only.")]
        [Key(26)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}