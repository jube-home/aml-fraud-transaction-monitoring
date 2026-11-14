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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.EntityAnalysisModelAbstractionRule
{
    [FormEndpoint("EntityAnalysisModelAbstractionRule")]
    [FormApproval]
    [AggregationParametersEditor(Parameters = nameof(SearchFunctionParameters), Function = nameof(SearchFunctionTypeId))]
    [RuleEditor(RuleParseType.AbstractionRule, ScriptType = nameof(RuleScriptTypeId), Builder = nameof(BuilderRuleScript), BuilderJson = nameof(Json), Coder = nameof(CoderRuleScript))]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Rule", Order = 20)]
    [FormGroup("Search and Aggregation", Order = 30)]
    [FormGroup("Offset", Order = 40, Collapsed = true)]
    [FormGroup("Output", Order = 50)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class EntityAnalysisModelAbstractionRuleDto : IUpdated, IActivatable, ILockable, ITreeChild,
        IRuleBuilderJson
    {
        [Description("Identifier of the Model this Abstraction Rule is registered against. Set from the parent " +
                     "Model context; not user-editable.")]
        [Key(0)]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Abstraction Rule. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Declines in 24 hours")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Which rule authoring surface is authoritative for this rule: 1 = visual Builder (requires " +
                     "BuilderRuleScript and Json), 2 = hand-written Coder (requires CoderRuleScript). The two " +
                     "surfaces are mutually exclusive -- only the selected surface's script is required.")]
        [FormField(Group = "Rule", Order = 10)]
        [Key(2)]
        public int RuleScriptTypeId { get; set; }

        [Description("The hand-written Coder-surface source for this rule. Required when RuleScriptTypeId is 2 " +
                     "(Coder); not required when RuleScriptTypeId is 1 (Builder).")]
        [FormField(Group = "Rule", Order = 40)]
        [RequiredWhen(nameof(RuleScriptTypeId), 2)]
        [Key(3)]
        public string? CoderRuleScript { get; set; }

        [Description("When true, this Abstraction Rule is tested against every record returned from the cache " +
                     "for the given Search Key, rather than only the transaction currently being processed. " +
                     "Enables the Search Key, Search Value, Search Interval, Function and Offset settings below.")]
        [FormField(Group = "Search and Aggregation", Order = 10, Widget = "switch")]
        [NewDefault(true)]
        [Key(4)]
        public bool Search { get; set; }

        [Description("The Request XPath, Inline Script or Inline Function data element (flagged as a Search Key " +
                     "on the parent Model) used to retrieve matching cache records for this Model. Required when " +
                     "Search is enabled.")]
        [FormField(Group = "Search and Aggregation", Order = 20, Widget = "select")]
        [VisibleWhen(nameof(Search), true)]
        [RequiredWhen(nameof(Search), true)]
        [Lookup("/api/GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        [Key(5)]
        public string? SearchKey { get; set; }

        [Description("Length of the Search Interval, taken together with Search Interval, that a cache record's " +
                     "timestamp must fall within (reaching back from the transaction's reference date) to be " +
                     "eligible for matching. Only enforced when Search is enabled.")]
        [FormField(Group = "Search and Aggregation", Order = 30, Widget = "number")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault(1)]
        [Key(6)]
        public int SearchValue { get; set; }

        [Description("Unit of Search Value: Seconds (s), Minutes (n), Hours (h) or Days (d). Only enforced when " +
                     "Search is enabled.")]
        [FormField(Group = "Search and Aggregation", Order = 40, Widget = "radio")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault("h")]
        [Options(typeof(IntervalUnit), IntervalUnit.Second, IntervalUnit.Minute, IntervalUnit.Hour, IntervalUnit.Day)]
        [Key(7)]
        public string? SearchInterval { get; set; }

        [Description("The aggregation function applied to the matched, offset-reduced cache records: 1 Count, " +
                     "2 Distinct Count, 3 Sum, 4 Average, 5 Median, 6 Kurtosis, 7 Skew, 8 Standard Deviation, " +
                     "11 Mode, 12 Same Count, 13 Actual Value, 14 Max, 15 Min, 16 Since, 17 Geometric Mean, " +
                     "18 Harmonic Mean, 19 Trimmed Mean, 20 Winsorised Mean, 21 Exponentially Weighted Mean, " +
                     "22 Root Mean Square, 23 Variance, 24 Range, 25 Interquartile Range, 26 Percentile, " +
                     "27 Median Absolute Deviation, 28 Mean Absolute Deviation, 29 Coefficient Of Variation, " +
                     "30 Entropy, 31 Gini Coefficient, 32 Nth Value, 33 First To Last Change, 34 Largest Step, " +
                     "35 Count Above, 36 Count Below, 37 Share Above, 38 Sum Above, 39 Longest Run Above, " +
                     "40 Last Value Z-Score, 41 Last Value Modified Z-Score, 42 Last Value Percentile Rank, " +
                     "43 Last Value To Mean Ratio, 44 Trend Slope, 45 Autocorrelation, 46 Cursor Result (returns " +
                     "the number the rule assigned to Cursor.Result during the scan, so a rule can compute its " +
                     "own aggregation; it needs no Function Key). Functions 17 to 45 take their static parameters " +
                     "from SearchFunctionParameters.")]
        [FormField(Group = "Search and Aggregation", Order = 50, Widget = "select")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault(1)]
        [Options(typeof(SearchFunctionType))]
        [Key(8)]
        public int SearchFunctionTypeId { get; set; }

        [Description("The data element that the Function Type aggregates over (e.g. the field to Sum or Average). " +
                     "Not applicable when Function Type is Count (1) or Cursor Result (46). Distinct Count (2) and " +
                     "Same Count (12) use a string-typed key.")]
        [FormField(Group = "Search and Aggregation", Order = 60, Widget = "select")]
        [VisibleWhen(nameof(Search), true)]
        [VisibleWhen(nameof(SearchFunctionTypeId), 1, 46, Op = ConditionOp.NotIn)]
        [Lookup("/api/GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        [RequiredWhen(nameof(Search), true)]
        [RequiredWhen(nameof(SearchFunctionTypeId), 1, 46, Op = ConditionOp.NotIn)]
        [Key(9)]
        public string? SearchFunctionKey { get; set; }

        [Description("When true, the matched cache records are reduced to a sub-range (First/Last/Skip " +
                     "First/Take Last, by Offset Value) before the Function Type aggregation is applied. Only " +
                     "meaningful when Search is enabled.")]
        [FormField(Group = "Offset", Order = 10, Widget = "switch")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault(false)]
        [Key(10)]
        public bool Offset { get; set; }

        [Description("How the matched cache records are reduced when Offset is enabled: 1 First, 2 Last, " +
                     "3 Skip First, 4 Take Last.")]
        [FormField(Group = "Offset", Order = 20, Widget = "radio")]
        [VisibleWhen(nameof(Search), true)]
        [VisibleWhen(nameof(Offset), true)]
        [RequiredWhen(nameof(Offset), true)]
        [NewDefault(1)]
        [Options(typeof(OffsetType))]
        [Key(11)]
        public int OffsetTypeId { get; set; }

        [Description("The number of records the Offset Type reduction applies to (e.g. skip the first N, or take " +
                     "the last N).")]
        [FormField(Group = "Offset", Order = 30, Widget = "number")]
        [VisibleWhen(nameof(Search), true)]
        [VisibleWhen(nameof(Offset), true)]
        [NewDefault(0)]
        [Key(12)]
        public int OffsetValue { get; set; }

        [Description("When true, every match of this Abstraction Rule is persisted to the Archive Key table for " +
                     "later analysis.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch", Label = "Archive Key")]
        [NewDefault(false)]
        [Key(13)]
        public bool ReportTable { get; set; }

        [Description("Whether the response payload is returned for a match on this Abstraction Rule.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [Key(14)]
        public bool ResponsePayload { get; set; }

        [Description("Legacy inherited-rule linkage, retained for backward compatibility. Not surfaced on the " +
                     "current page and not populated by it -- see the migration report.")]
        [Key(15)]
        public int InheritedId { get; set; }

        [Description("Globally-addressable identifier for this row, stable across environments and the form "
                     + "in which every other area refers to it. Server-assigned. Read-only.")]
        [Key(16)]
        public Guid Guid { get; set; }

        [Description("When true, this Abstraction Rule participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [Key(17)]
        public bool Active { get; set; }

        [Description("When true, this Abstraction Rule is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        [Key(18)]
        public bool Locked { get; set; }

        [Description("The compiled Builder/Coder rule script tested for a match against the Payload and, when " +
                     "Search is enabled, each cache record returned for the Search Key; must return True on " +
                     "match, False otherwise. Required when RuleScriptTypeId is 1 (Builder); not required when " +
                     "RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 20)]
        [RequiredWhen(nameof(RuleScriptTypeId), 1)]
        [Key(19)]
        public string BuilderRuleScript { get; set; } = string.Empty;

        [Description("The Query Builder JSON definition backing BuilderRuleScript. Required when RuleScriptTypeId " +
                     "is 1 (Builder); not required when RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 30)]
        [RequiredWhen(nameof(RuleScriptTypeId), 1)]
        [Key(20)]
        public string Json { get; set; } = string.Empty;

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(21)]
        public int Id { get; set; }

        [Description("User who created this Abstraction Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(22)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Rule was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        [Key(23)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Abstraction Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(24)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Rule was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        [Key(25)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(26)]
        public int Version { get; set; }

        [Description("User who deleted this Abstraction Rule, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        [Key(27)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Rule was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        [Key(28)]
        public DateTimeOffset? DeletedDate { get; set; }

        [Description("Order in which the cache records are presented to the rule cursor, the Offset reduction " +
                     "and the aggregation function: 1 Ascending (oldest first, the default) or 2 Descending " +
                     "(most recent first). The fetch is unchanged; it is always the most recent records up to " +
                     "the Search Key fetch limit. Under Descending, Cursor.Position 1 and Offset First are the " +
                     "most recent record, and order-sensitive functions such as Actual Value read the oldest " +
                     "record kept.")]
        [FormField(Group = "Search and Aggregation", Order = 45, Widget = "radio")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault(1)]
        [Options(typeof(IterationOrder))]
        [Key(29)]
        public int IterationOrderId { get; set; } = 1;

        [Description("Static parameters for the selected Function Type, as a map of parameter name to number, " +
                     "for example trimFraction 0.1. A true or false parameter is given as 1 or 0. Leave empty to " +
                     "use the default of every parameter. Only functions 17 and above take parameters: 19 Trimmed Mean " +
                     "trimFraction (0 to under 0.5, default 0.1); 20 Winsorised Mean winsorFraction (0 to under " +
                     "0.5, default 0.05); 21 Exponentially Weighted Mean alpha (over 0 to 1, default 0.3); " +
                     "23 Variance and 29 Coefficient Of Variation unbiased (1 or 0, default 1); 26 Percentile " +
                     "percentile (0 to 100, default 95); 27 Median Absolute Deviation scaleToNormal (1 or 0, " +
                     "default 1); 28 Mean Absolute Deviation aroundMedian (1 or 0, default 0); 32 Nth Value position " +
                     "(1 or more, default 1); 35 Count Above, 36 Count Below, 37 Share Above, 38 Sum Above and " +
                     "39 Longest Run Above threshold (default 0); 45 Autocorrelation lag (1 or more, default 1).")]
        [FormField(Group = "Search and Aggregation", Order = 65)]
        [VisibleWhen(nameof(Search), true)]
        [Key(30)]
        public Dictionary<string, double>? SearchFunctionParameters { get; set; }
    }
}