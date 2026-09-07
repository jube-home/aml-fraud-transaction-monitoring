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

namespace Jube.Dto.EntityAnalysisModelReprocessingRule
{
    [FormEndpoint("EntityAnalysisModelReprocessingRule")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Rule", Order = 20)]
    [FormGroup("Reprocessing", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelReprocessingRuleDto : IUpdated, IActivatable, ILockable, ITreeChild,
        IRuleBuilderJson
    {
        [Description("Identifier of the Model this Reprocessing Rule is registered against. Set from the parent " +
                     "Model context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Reprocessing Rule. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("The relative order in which eligible Reprocessing Rules are evaluated against an Archive " +
                     "record during a reprocessing run; lower values are evaluated first.")]
        [FormField(Group = "Identity", Order = 40, Widget = "number")]
        [NewDefault(0)]
        public double Priority { get; set; }

        [Description("Which rule authoring surface is authoritative for this rule: 1 = visual Builder (requires " +
                     "BuilderRuleScript and Json), 2 = hand-written Coder (requires CoderRuleScript). The two " +
                     "surfaces are mutually exclusive -- only the selected surface's script is required.")]
        [FormField(Group = "Rule", Order = 10)]
        [Forms.Editor("RuleBuilder")]
        public byte RuleScriptTypeId { get; set; }

        [Description("The compiled Builder/Coder rule script tested for a match against each Archive record " +
                     "replayed during a reprocessing run; must return True on match, False otherwise. Required " +
                     "when RuleScriptTypeId is 1 (Builder); not required when RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 20)]
        [Forms.Editor("RuleBuilder")]
        public string BuilderRuleScript { get; set; } = string.Empty;

        [Description("The Query Builder JSON definition backing BuilderRuleScript. Required when RuleScriptTypeId " +
                     "is 1 (Builder); not required when RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 30)]
        [Forms.Editor("RuleBuilder")]
        public string Json { get; set; } = string.Empty;

        [Description("The hand-written Coder-surface source for this rule. Required when RuleScriptTypeId is 2 " +
                     "(Coder); not required when RuleScriptTypeId is 1 (Builder).")]
        [FormField(Group = "Rule", Order = 40)]
        [Forms.Editor("RuleBuilder")]
        public string? CoderRuleScript { get; set; }

        [Description("When true, this Reprocessing Rule participates in reprocessing runs.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Reprocessing Rule is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Percentage (0-100) of matched Archive records to randomly sample into a reprocessing " +
                     "instance, rather than reprocessing every match.")]
        [FormField(Group = "Reprocessing", Order = 10, Widget = "slider")]
        [NewDefault(100)]
        public double ReprocessingSample { get; set; }

        [Description("The length of the reprocessing window, taken together with ReprocessingInterval, reaching " +
                     "back from the reprocessing run's reference date.")]
        [FormField(Group = "Reprocessing", Order = 20, Widget = "number")]
        [NewDefault(0)]
        public int ReprocessingValue { get; set; }

        [Description("Unit of ReprocessingValue: Minutes (n), Hours (h), Days (d) or Months (m).")]
        [FormField(Group = "Reprocessing", Order = 30, Widget = "radio")]
        [NewDefault("d")]
        public string? ReprocessingInterval { get; set; }

        [Description("Server-assigned row identifier. A successful Update assigns a new Id -- this Reprocessing " +
                     "Rule is superseded rather than updated in place. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this row. Server-assigned. Read-only. Reset to the calling user on every " +
                     "Update, since Update supersedes the row rather than updating it in place -- see the " +
                     "migration report.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this row was created. Server-assigned. Read-only. Reset to the Update time " +
                     "on every Update -- see CreatedUser and the migration report.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("No backing column -- always null when read back (pre-existing quirk, see migration " +
                     "report). Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("No backing column -- always null when read back (pre-existing quirk, see migration " +
                     "report). Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every Update. Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this row, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this row was soft-deleted, if applicable. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
