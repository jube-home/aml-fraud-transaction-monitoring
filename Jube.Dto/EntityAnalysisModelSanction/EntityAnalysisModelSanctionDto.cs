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
using Jube.Dto.Wire;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.EntityAnalysisModelSanction
{
    [FormEndpoint("EntityAnalysisModelSanction")]
    [FormApproval]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Matching", Order = 20)]
    [FormGroup("Caching", Order = 30, Collapsed = true)]
    [FormGroup("Output", Order = 40, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class EntityAnalysisModelSanctionDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Model this Sanction check is registered against. Set from the parent " +
                     "Model context; not user-editable.")]
        [Key(0)]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Sanction check registration. Unique within the Model " +
                     "(case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. OFAC SDN screening")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Name of the data element on the parent Model supplying the multi-part string (a full name " +
                     "separated by spaces only) to check against the sanctions lists.")]
        [FormField(Group = "Matching", Order = 10, Widget = "select")]
        [Lookup("/api/GetEntityAnalysisPotentialMultiPartStringNames", TextField = "value", ValueField = "value",
            ParentField = nameof(EntityAnalysisModelId))]
        [ListColumn(Order = 20, Title = "Multipart String")]
        [Key(2)]
        public string? MultipartStringDataName { get; set; }

        [Description("Maximum Levenshtein Distance for the fuzzy-matching step; a candidate whose edit distance " +
                     "exceeds this is not considered a match.")]
        [FormField(Group = "Matching", Order = 20, Widget = "slider")]
        [ListColumn(Order = 30, Title = "Distance")]
        [NewDefault(2)]
        [Key(3)]
        public int Distance { get; set; }

        [Description("How the distances of every matched sanction entry are combined into the single value made " +
                     "available for rule evaluation: 1 = Sum, 2 = Average (default), 3 = Count, 4 = Max, 5 = Min, " +
                     "6 = First, 7 = Last, 8 = Confidence. Leave unset to use the server default (Average).")]
        [FormField(Group = "Matching", Order = 30, Widget = "select")]
        [Options(typeof(SanctionAggregationType))]
        [NewDefault((byte)2)]
        [Key(4)]
        public byte? AggregationTypeId { get; set; }

        [Description("Overrides the server-wide maximum distance ratio for this Sanction check only, scaling " +
                     "down the allowed Levenshtein distance in proportion to the shorter token length being " +
                     "compared. Leave unset to use the server-wide default.")]
        [FormField(Group = "Matching", Order = 40, Widget = "slider")]
        [NewDefault(0.3)]
        [Key(5)]
        public double? MaxDistanceRatio { get; set; }

        [Description("Overrides the server-wide maximum coverage ratio for this Sanction check only, rejecting a " +
                     "candidate match whose token count differs too greatly from the input's token count. Leave " +
                     "unset to use the server-wide default.")]
        [FormField(Group = "Matching", Order = 50, Widget = "number")]
        [NewDefault(2.0)]
        [Key(6)]
        public double? MaxCoverageRatio { get; set; }

        [Description("How a screened name is compared with a list entry: 1 = Levenshtein edit distance per name " +
                     "part (default), 2 = Jaro-Winkler similarity per name part, 3 = Token set, the whole name " +
                     "compared ignoring the order of its parts, each part paired with its closest part by Jaro-Winkler and the score reduced for every part left unmatched. Leave unset for Levenshtein.")]
        [FormField(Group = "Matching", Order = 35, Widget = "select")]
        [Options(typeof(SanctionMatchAlgorithm))]
        [NewDefault((byte)1)]
        [Key(22)]
        public byte? MatchAlgorithmId { get; set; }

        [Description("For Jaro-Winkler and Token set only, the least similarity from 0 to 1 that counts as a " +
                     "match. Leave unset for 0.9. Levenshtein ignores it.")]
        [FormField(Group = "Matching", Order = 45, Widget = "slider")]
        [NewDefault(0.9)]
        [Key(23)]
        public double? MinimumSimilarity { get; set; }

        [Description("When true, Cyrillic and Greek script is transliterated to Latin on both the screened name " +
                     "and the list entries before they are compared, so a name written in Cyrillic can match " +
                     "its Latin spelling.")]
        [FormField(Group = "Matching", Order = 55, Widget = "switch")]
        [Key(24)]
        public bool Transliterate { get; set; }

        [Description("Interval unit for how long a matched multipart string's distance is cached: 's' (seconds), " +
                     "'n' (minutes), 'h' (hours) or 'd' (days).")]
        [FormField(Group = "Caching", Order = 10, Widget = "radio")]
        [Options(typeof(IntervalUnit), IntervalUnit.Second, IntervalUnit.Minute, IntervalUnit.Hour, IntervalUnit.Day)]
        [NewDefault('h')]
        [Key(7)]
        [MessagePackFormatter(typeof(CharAsStringFormatter))]
        public char CacheInterval { get; set; }

        [Description("Length of time, in CacheInterval units, that a cached distance for a multipart string " +
                     "remains valid.")]
        [FormField(Group = "Caching", Order = 20)]
        [NewDefault(1)]
        [Key(8)]
        public int CacheValue { get; set; }

        [Description("When true, a matching row is written to the sanctions archive key table for this transaction.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch", Label = "Archive Key")]
        [NewDefault(false)]
        [Key(9)]
        public bool ReportTable { get; set; }

        [Description("When true, the response payload is returned for a match on this Sanction check.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [Key(10)]
        public bool ResponsePayload { get; set; }

        [Description("Globally-addressable identifier for this row, stable across environments and the form "
                     + "in which every other area refers to it. Server-assigned. Read-only.")]
        [Key(11)]
        public Guid Guid { get; set; }

        [Description("When true, this Sanction check participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [Key(12)]
        public bool Active { get; set; }

        [Description("When true, this Sanction check registration is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        [Key(13)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(14)]
        public int Id { get; set; }

        [Description("User who created this Sanction check registration. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(15)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Sanction check registration was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        [Key(16)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Sanction check registration. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(17)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Sanction check registration was last updated. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        [Key(18)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(19)]
        public int Version { get; set; }

        [Description("User who deleted this Sanction check registration, if soft-deleted. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        [Key(20)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Sanction check registration was soft-deleted, if applicable. " +
                     "Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        [Key(21)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}