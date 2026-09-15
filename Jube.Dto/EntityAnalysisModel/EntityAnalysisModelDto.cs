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

namespace Jube.Dto.EntityAnalysisModel
{
    [FormEndpoint("EntityAnalysisModel")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Entry & Reference Date", Order = 20)]
    [FormGroup("Cache", Order = 30, Collapsed = true)]
    [FormGroup("Response Elevation Limit", Order = 40, Collapsed = true)]
    [FormGroup("Activation Watcher", Order = 50, Collapsed = true)]
    [FormGroup("Archiving & Counters", Order = 60, Collapsed = true)]
    [FormGroup("Implicit Async", Order = 70, Collapsed = true)]
    [FormGroup("Trace", Order = 75, Collapsed = true)]
    [FormGroup("Logs", Order = 76, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelDto : IUpdated, IActivatable, ILockable, IGuidIdentified
    {
        [Description("Display name of the model. Unique within the tenant (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; init; }

        [Description("Name of the field, as extracted from the HTTP POST body, that represents the entry " +
                     "identifier (for example a transaction identifier).")]
        [FormField(Group = "Entry & Reference Date", Order = 10)]
        public string? EntryName { get; init; }

        [Description("JSONPath specifying the location of the entry identifier in the HTTP POST body.")]
        [FormField(Group = "Entry & Reference Date", Order = 20)]
        public string? EntryXPath { get; init; }

        [Description("Not currently editable in the user interface and not persisted -- carried on the DTO for " +
                     "parity with the legacy payload shape only. Always 0 on read; any value sent is discarded.")]
        [FormField(Group = "Entry & Reference Date", Order = 25, Widget = "radio", ReadOnly = true)]
        [NewDefault((byte)1)]
        public byte EntryPayloadLocationTypeId { get; set; }

        [Description("Name of the field, as extracted from the HTTP POST body or from the current server time, " +
                     "that represents the reference date (for example a transaction date/time).")]
        [FormField(Group = "Entry & Reference Date", Order = 30)]
        public string? ReferenceDateName { get; init; }

        [Description("Where the reference date is read from: Body (extracted via Reference Date XPath) or Now " +
                     "(the server's current UTC time).")]
        [FormField(Group = "Entry & Reference Date", Order = 40, Widget = "radio")]
        [NewDefault((byte)1)]
        public byte ReferenceDatePayloadLocationTypeId { get; set; }

        [Description("JSONPath specifying the location of the reference date in the HTTP POST body. Required " +
                     "unless Reference Date Payload Location is Now.")]
        [FormField(Group = "Entry & Reference Date", Order = 50)]
        [VisibleWhen(nameof(ReferenceDatePayloadLocationTypeId), (byte)1)]
        [RequiredWhen(nameof(ReferenceDatePayloadLocationTypeId), (byte)1)]
        public string? ReferenceDateXPath { get; set; }

        [Description("When true, entry data backing Abstraction Rules is stored in the cache.")]
        [FormField(Group = "Cache", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableCache { get; init; }

        [Description("Maximum number of records returned from the cache per entry lookup, to bound the cost of " +
                     "very active entities.")]
        [FormField(Group = "Cache", Order = 20, Widget = "number")]
        [NewDefault(100)]
        public int CacheFetchLimit { get; set; }

        [Description("Unit of Cache TTL Interval Value: Seconds, Minutes, Hours or Days.")]
        [FormField(Group = "Cache", Order = 30, Widget = "radio")]
        [NewDefault('h')]
        public char CacheTtlInterval { get; set; }

        [Description("Offset, in Cache TTL Interval units, before the current reference date at which cached " +
                     "entries are eligible for deletion.")]
        [FormField(Group = "Cache", Order = 40, Widget = "number")]
        [NewDefault(3)]
        public int CacheTtlIntervalValue { get; init; }

        [Description("When true, Sanction search results are served from a time-limited cache instead of " +
                     "recomputing the Levenshtein distance search on every request.")]
        [FormField(Group = "Cache", Order = 50, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableSanctionCache { get; init; }

        [Description("The maximum response elevation allowed in the response payload for this model; a rule " +
                     "specifying a higher value is clipped to this maximum.")]
        [FormField(Group = "Response Elevation Limit", Order = 10, Widget = "number")]
        [NewDefault(10.0)]
        public double MaxResponseElevation { get; init; }

        [Description("When true, the number of response elevations set within a rolling period is capped; once " +
                     "the threshold is reached, further elevations return zero.")]
        [FormField(Group = "Response Elevation Limit", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableResponseElevationLimit { get; init; }

        [Description("Unit of the response elevation limit's rolling period: Seconds, Minutes, Hours or Days.")]
        [FormField(Group = "Response Elevation Limit", Order = 30, Widget = "radio")]
        [VisibleWhen(nameof(EnableResponseElevationLimit), true)]
        [RequiredWhen(nameof(EnableResponseElevationLimit), true)]
        [NewDefault('d')]
        public char MaxResponseElevationInterval { get; init; }

        [Description("Length, in Max Response Elevation Interval units, of the rolling period over which " +
                     "elevations are counted.")]
        [FormField(Group = "Response Elevation Limit", Order = 40, Widget = "number")]
        [VisibleWhen(nameof(EnableResponseElevationLimit), true)]
        [RequiredWhen(nameof(EnableResponseElevationLimit), true)]
        [NewDefault(1)]
        public int MaxResponseElevationValue { get; init; }

        [Description("Maximum number of response elevations permitted within the rolling period before further " +
                     "elevations are suppressed to zero.")]
        [FormField(Group = "Response Elevation Limit", Order = 50, Widget = "number")]
        [VisibleWhen(nameof(EnableResponseElevationLimit), true)]
        [RequiredWhen(nameof(EnableResponseElevationLimit), true)]
        [NewDefault(100)]
        public int MaxResponseElevationThreshold { get; init; }

        [Description("When true, activations for this model are streamed to the Activation Watcher, subject to " +
                     "the sample rate and the rolling-period threshold below.")]
        [FormField(Group = "Activation Watcher", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableActivationWatcher { get; set; }

        [Description("Unit of the Activation Watcher's rolling period: Seconds, Minutes, Hours or Days.")]
        [FormField(Group = "Activation Watcher", Order = 20, Widget = "radio")]
        [VisibleWhen(nameof(EnableActivationWatcher), true)]
        [RequiredWhen(nameof(EnableActivationWatcher), true)]
        [NewDefault('d')]
        public char MaxActivationWatcherInterval { get; set; }

        [Description("Length, in Max Activation Watcher Interval units, of the rolling period over which " +
                     "activations sent to the Activation Watcher are counted.")]
        [FormField(Group = "Activation Watcher", Order = 30, Widget = "number")]
        [VisibleWhen(nameof(EnableActivationWatcher), true)]
        [RequiredWhen(nameof(EnableActivationWatcher), true)]
        [NewDefault(1)]
        public int MaxActivationWatcherValue { get; set; }

        [Description("Maximum number of activations streamed to the Activation Watcher within the rolling " +
                     "period before further activations are withheld from the stream.")]
        [FormField(Group = "Activation Watcher", Order = 40, Widget = "number")]
        [VisibleWhen(nameof(EnableActivationWatcher), true)]
        [RequiredWhen(nameof(EnableActivationWatcher), true)]
        [NewDefault(100)]
        public int MaxActivationWatcherThreshold { get; set; }

        [Description("Proportion (0 to 1) of activations randomly sampled for the Activation Watcher's " +
                     "streaming overview of risk.")]
        [FormField(Group = "Activation Watcher", Order = 50, Widget = "percent")]
        [VisibleWhen(nameof(EnableActivationWatcher), true)]
        [RequiredWhen(nameof(EnableActivationWatcher), true)]
        [NewDefault(1.0)]
        public double ActivationWatcherSample { get; set; }

        [Description("When true, model activity is retained as TTL Counter entries in the cache, backing TTL " +
                     "Counter rules.")]
        [FormField(Group = "Archiving & Counters", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableTtlCounter { get; init; }

        [Description("When true, model activity is archived to the RDBMS archive tables in Postgres.")]
        [FormField(Group = "Archiving & Counters", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableRdbmsArchive { get; init; }

        [Description("When true, activations for this model are retained in the activation archive, backing " +
                     "case creation and downstream reporting.")]
        [FormField(Group = "Archiving & Counters", Order = 30, Widget = "switch")]
        public bool EnableActivationArchive { get; init; }

        [Description("Not currently editable in the user interface and not persisted -- carried on the DTO for " +
                     "parity with a planned Elasticsearch archive feature only. Always false on read; any value " +
                     "sent is discarded.")]
        [FormField(Group = "Archiving & Counters", Order = 40, Widget = "switch", ReadOnly = true)]
        public bool EnableElasticsearchArchive { get; set; }

        [Description("When true, a synchronous transaction invocation waits inline for the model to finish " +
                     "processing, up to the configured timeout, before falling back to the same response shape " +
                     "populated with whatever has been computed so far. The caller can then poll the callback " +
                     "endpoint (/api/invoke/EntityAnalysisModel/Callback/{guid}) for the completed result.")]
        [FormField(Group = "Implicit Async", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableImplicitAsync { get; set; }

        [Description("Number of milliseconds a synchronous invocation waits inline before falling back to an " +
                     "in-progress snapshot of the response, when Implicit Async is enabled.")]
        [FormField(Group = "Implicit Async", Order = 20, Widget = "number")]
        [VisibleWhen(nameof(EnableImplicitAsync), true)]
        [RequiredWhen(nameof(EnableImplicitAsync), true)]
        [NewDefault(5000)]
        public int ImplicitAsyncTimeoutMilliseconds { get; set; }

        [Description("When true, invocation responses for this model echo the full per-stage and per-rule " +
                     "performance breakdown (Stages) into the direct HTTP response, in addition to the " +
                     "always-present task wrapper stats. This breakdown is always captured to the Archive " +
                     "payload regardless of this switch -- Enable Response Trace only controls whether the " +
                     "already-captured detail is also returned directly, since it can be large for models with " +
                     "many rules.")]
        [FormField(Group = "Response Trace", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableTrace { get; set; }

        [Description("Master switch for this model's invocation log capture -- Enable Logs: Info and Enable " +
                     "Logs: Warn Threshold below (and anything added under this group in future) only capture " +
                     "when this is also on. Turning it off disables both immediately without losing their own " +
                     "settings underneath, so it doubles as a single, unambiguous kill switch. This does not " +
                     "affect log4net's own INFO logging, which remains controlled separately by the logging " +
                     "configuration.")]
        [FormField(Group = "Logs", Order = 5, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableLogs { get; set; }

        [Description("Captures the same step-by-step trace normally only visible via log4net INFO logging into a " +
                     "logs array on the invocation payload instead, for a sampled subset of invocations (see " +
                     "Enable Sampling/Sample Percentage below) -- lets full step-by-step detail run continuously " +
                     "in production at a small fraction of its normal disk/CPU/payload-size cost. Independent of " +
                     "Enable Logs: Warn Threshold below -- the two are separate monitors, not mutually exclusive " +
                     "levels of one switch, and can be on together, individually, or not at all. Each captured " +
                     "entry records which of the two caused it (CapturedByInfoSampling/CapturedByWarnThreshold), " +
                     "so a trace point captured by both at once is still reportable as either. This does not " +
                     "affect log4net's own INFO logging, which remains controlled separately by the logging " +
                     "configuration.")]
        [FormField(Group = "Logs", Order = 10, Widget = "switch")]
        [VisibleWhen(nameof(EnableLogs), true)]
        [NewDefault(false)]
        public bool EnableLogsInfo { get; set; }

        [Description("When true, only a random sample of invocations actually have Enable Logs: Info's trace " +
                     "points captured (see Sample Percentage) rather than every one -- lets Info-level detail " +
                     "run continuously in production at a small fraction of its normal disk/CPU/payload-size " +
                     "cost. Has no effect on Enable Logs: Warn Threshold, which always evaluates every " +
                     "invocation regardless of this switch, since sampling it would risk missing the very " +
                     "breaches it exists to catch.")]
        [FormField(Group = "Logs", Order = 15, Widget = "switch")]
        [VisibleWhen(nameof(EnableLogsInfo), true)]
        [NewDefault(false)]
        public bool EnableSampling { get; set; }

        [Description("Percentage of invocations captured at Info level when Enable Sampling is on, e.g. 0.02 " +
                     "means roughly 1 in 5000 invocations. Also gates the always-on Response Time Pipeline and " +
                     "Task Performance Counter capture for this model, so a report can correlate sampled logs " +
                     "with the timing/memory data captured for the same invocations. Sampling volume is itself " +
                     "an important performance signal, reported independently via the jube.engine.logs.info.count " +
                     "metric.")]
        [FormField(Group = "Logs", Order = 16, Widget = "slider")]
        [VisibleWhen(nameof(EnableLogsInfo), true)]
        [RequiredWhen(nameof(EnableSampling), true)]
        [NewDefault(100)]
        public double SamplePercentage { get; set; }

        [Description("Captures a trace point into the same logs array as Enable Logs: Info, but only when the " +
                     "gap since the previous captured entry exceeds Warn Threshold Milliseconds below -- an " +
                     "always-on anomaly watch (every invocation, never sampled) rather than a representative " +
                     "sample. Independent of Enable Logs: Info above -- the two are separate monitors that can " +
                     "be on together, individually, or not at all.")]
        [FormField(Group = "Logs", Order = 18, Widget = "switch")]
        [VisibleWhen(nameof(EnableLogs), true)]
        [NewDefault(false)]
        public bool EnableLogsWarnThreshold { get; set; }

        [Description("Milliseconds a trace point's gap since the last captured entry must exceed to be " +
                     "captured, when Enable Logs: Warn Threshold is on.")]
        [FormField(Group = "Logs", Order = 20, Widget = "slider")]
        [VisibleWhen(nameof(EnableLogsWarnThreshold), true)]
        [RequiredWhen(nameof(EnableLogsWarnThreshold), true)]
        [NewDefault(250)]
        public int LogsWarnThresholdMilliseconds { get; set; }

        [Description("When true, invocation responses for this model include the captured logs array, in " +
                     "addition to it always being included in the Archive payload whenever either Enable Logs: " +
                     "Info or Enable Logs: Warn Threshold is on. Has no effect when both are off, since nothing " +
                     "is ever captured to include.")]
        [FormField(Group = "Logs", Order = 30, Widget = "switch")]
        [VisibleWhen(nameof(EnableLogs), true)]
        [NewDefault(false)]
        public bool EnableLogsInResponse { get; set; }

        [Description("When true, the model participates in transaction invocation and rule synchronisation.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("Server-assigned globally unique identifier that addresses this model's transaction " +
                     "invocation endpoint (/api/invoke/EntityAnalysisModel/{guid}). Read-only.")]
        [FormField(Group = "Identity", Order = 20, ReadOnly = true, Widget = "guid")]
        public Guid Guid { get; set; }

        [Description("When true, the model is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 40, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this model. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this model was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this model. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this model was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this model, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this model was soft-deleted, if applicable. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}