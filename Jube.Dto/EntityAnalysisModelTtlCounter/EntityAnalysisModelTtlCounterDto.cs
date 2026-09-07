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

namespace Jube.Dto.EntityAnalysisModelTtlCounter
{
    [FormEndpoint("EntityAnalysisModelTtlCounter")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Decrement", Order = 20)]
    [FormGroup("Output", Order = 30, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelTtlCounterDto : IUpdated, IActivatable, ILockable, ITreeChild, IGuidIdentified
    {
        [Description("Identifier of the Model this TTL Counter is registered against. Set from the parent Model " +
                     "context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this TTL Counter. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("The key extracted from the invocation payload used to group counter entries (e.g. " +
                     "AccountId). Required regardless of whether the counter lives forever.")]
        [FormField(Group = "Identity", Order = 40)]
        [Lookup("/api/GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        public string? TtlCounterDataName { get; set; }

        [Description("Whether the TTL Counter should ever be decremented. When true, no counter entry is created " +
                     "to wind the counter back and the Interval/Value/Resolution fields are not applicable.")]
        [FormField(Group = "Decrement", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableLiveForever { get; set; }

        [Description("The unit of time (s = seconds, n = minutes, h = hours, d = days, m = months, y = years) " +
                     "combined with TtlCounterValue to determine how long a counter entry lives before it is " +
                     "decremented. Not applicable when EnableLiveForever is true.")]
        [FormField(Group = "Decrement", Order = 20, Widget = "radio")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        public string? TtlCounterInterval { get; set; }

        [Description("The number of TtlCounterInterval units a counter entry lives before it is decremented. Not " +
                     "applicable when EnableLiveForever is true.")]
        [FormField(Group = "Decrement", Order = 30)]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        public int TtlCounterValue { get; set; }

        [Description("Truncation applied to the reference date when grouping decrement entries in cache (n = " +
                     "minutes, h = hours, d = days). Not applicable when EnableLiveForever is true.")]
        [FormField(Group = "Decrement", Order = 40, Widget = "radio")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        public string? ResolutionInterval { get; set; }

        [Description("Whether a numeric value extracted from the invocation payload (TtlCounterDataValue) " +
                     "increments the counter, rather than incrementing by one per match. Not applicable when " +
                     "EnableLiveForever is true.")]
        [FormField(Group = "Decrement", Order = 50, Widget = "switch")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        [NewDefault(false)]
        public bool EnableSum { get; set; }

        [Description("The Integer or Float field in the invocation payload to increment the counter by. Required " +
                     "when EnableSum is true.")]
        [FormField(Group = "Decrement", Order = 60)]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        [VisibleWhen(nameof(EnableSum), true)]
        [RequiredWhen(nameof(EnableSum), true)]
        [Lookup("/api/GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        public string? TtlCounterDataValue { get; set; }

        [Description("Whether the aggregated TTL Counter value for a match is written back into the response " +
                     "payload.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("Whether this TTL Counter's activity is written to the reporting table.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("Whether counter entries are aggregated in real time on every event (more expensive, less " +
                     "latent) rather than via the pre-aggregated cache summary (cheaper, can be slightly stale).")]
        [FormField(Group = "Output", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool OnlineAggregation { get; set; }

        [Description("When true, this TTL Counter participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("Globally-addressable identifier for this TTL Counter, exposed for lookup by other areas " +
                     "(e.g. the Activation Rule TTL Counter increment picker). Server-assigned. Read-only.")]
        public Guid Guid { get; set; }

        [Description("When true, this TTL Counter is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this TTL Counter. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this TTL Counter was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this TTL Counter. Server-assigned. Read-only. No backing column -- " +
                     "always null when read back (pre-existing quirk, see migration report).")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this TTL Counter was last updated. Server-assigned. Read-only. No backing " +
                     "column -- always null when read back (pre-existing quirk, see migration report).")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this TTL Counter, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this TTL Counter was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}