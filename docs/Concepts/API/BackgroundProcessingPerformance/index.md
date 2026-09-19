---
layout: default
title: Background Processing Performance
nav_order: 12
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Background Processing Performance

[HTTP API Performance Trace](../PerformanceTrace/index.html) covers the invoke pipeline itself -- the serial,
per-request stages a transaction passes through on the thread that received it. Two further pipelines run entirely off
that thread, each on its own background queue: the **Archiver** (serialising and persisting the invocation's Archive
payload, then handing off to case creation) and **case creation** itself (looking up existing case priority, persisting
the workflow status, then optionally a notification and/or HTTP callback). Both get the same cluster-wide, aggregate
rollup and warn-threshold capture treatment as the invoke pipeline, and both feed a shared `CaptureQueueHealth`
mechanism that also covers a handful of other bounded in-memory queues elsewhere in the platform.

## `ArchiverStagePerformanceCounter`

One row per Archiver stage per Model roughly every minute, with `TotalMicroseconds`, `MinMicroseconds`,
`MaxMicroseconds` and `InvokeCount` for that interval -- the same shape as `EntityAnalysisModelStagePerformanceCounter`
in [HTTP API Performance Trace](../PerformanceTrace/index.html#monitoring-stage-performance-across-transactions),
applied to the Archiver's own four stages instead of the invoke pipeline's:

- **BuildArchiveJson** -- serialising the invocation's Archive payload to JSON.
- **CaseCreationDispatch** -- handing that payload off to case creation (synchronous reprocessing recall, or enqueuing
  for the normal asynchronous path).
- **RdbmsArchiveWrite** -- the per-item RDBMS archive write (a reprocessing update) or buffered insert.
- **BulkCopyArchiveBuffer** -- the periodic buffered bulk copy of accumulated archive rows, timed and flushed
  independently of any single invocation (`EntityAnalysisModelInstanceEntryGuid` is null in `ArchiverWarning` below for
  this stage specifically, since it operates on a batch rather than one item).

``` sql
select "StageName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "ArchiverStagePerformanceCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection: `GET /api/ArchiverStagePerformanceCounter`
(landlord tenant only, any other caller receives 403), or via the **Administration > Performance > Archiver Stage
Performance Counter** page in the UI. It accepts `from`/`to` (against `CreatedDate`, each defaulting independently to
the last hour), an exact-match `stageId` (`1`=`BuildArchiveJson`, `2`=`CaseCreationDispatch`,
`3`=`RdbmsArchiveWrite`, `4`=`BulkCopyArchiveBuffer`), `take` (clamped to 100000), `samplePercentage` (0-100, clamped --
not surfaced in the UI toolbar; for an agent drawing an unbiased baseline sample) and `sortField`/`sortDirection`
(any column the grid displays; `sortDirection` case-insensitive, `asc` for ascending and anything else including omitted
for descending; an unrecognised or omitted `sortField` falls back to most-recent-first). The response is an envelope
`{ rows, total, statistics }` -- `rows` respects `take`, `total` is the full filtered count regardless of
`take`, and `statistics` (Min/Max/Mean/Median/StandardDeviation plus a 10-bucket histogram, computed over the full
filtered set capped at 100000) covers `totalMicroseconds`, `minMicroseconds`, `maxMicroseconds` and `invokeCount` --
`stageId` itself, being a category code rather than a measured quantity, is excluded. Every endpoint documented on this
page shares this same envelope, sort and statistics shape; the notes below only call out each table's specific filters
and statistics columns.

Each stage duration is also recorded to **`jube.engine.archiver.stage.duration`** -- an OpenTelemetry histogram metric
(milliseconds), tagged by `stage`, exported whenever `EnableOpenTelemetry` is set. A separate instrument from
`jube.engine.stage.duration` since the Archiver runs off the invoke pipeline entirely and has its own, unrelated stage
vocabulary.

## `ArchiverWarning`

One row per Archiver stage invocation that took longer than `ArchiverWarnThresholdMilliseconds` (see
[Environment Variables](../../EnvironmentVariables/index.html); defaults to 10 seconds -- deliberately much higher than
an in-thread invoke trace threshold, since RDBMS writes and buffered bulk copies are routinely slow by nature). Carries
the Model, the specific invocation (`EntityAnalysisModelInstanceEntryGuid`, null for `BulkCopyArchiveBuffer`), which
stage was slow, and its actual duration -- so a slow Archiver backlog can be traced to the specific item and cause, not
just the aggregate stage stats above.

Up to the last 100000 rows are also browsable without a database connection: `GET /api/ArchiverWarning` (landlord tenant
only, any other caller receives 403), or via the **Administration > Performance > Archiver Warning** page in the UI. It
accepts the same `from`/`to`, `stageId`, `take`, `samplePercentage` and `sortField`/`sortDirection` as
`ArchiverStagePerformanceCounter` above; `statistics` covers `durationMicroseconds` only.

Each breach also increments **`jube.engine.archiver.warn.count`** -- an OpenTelemetry counter, tagged by `stage`,
mirroring `jube.engine.logs.warn.count` (see [Invocation Trace Log](../InvocationTraceLog/index.html)) for the same
"count of breaches" signal an alert can watch without a database query.

## `CaseCreationStagePerformanceCounter`

The same per-minute rollup, for case creation's own stages. Not scoped to any Model -- case creation is a single global
queue shared by every model:

- **ExistingCasePriorityLookup** -- checking whether a case already exists for this workflow/key/value, and if so
  whether the new priority supersedes it.
- **WorkflowStatusLookupAndPersist** -- looking up the target workflow status and inserting or updating the case row.
- **Notification** -- only present when the resulting workflow status has notifications enabled.
- **HttpEndpoint** -- only present when the resulting workflow status has an HTTP callback enabled.

``` sql
select "StageName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "CaseCreationStagePerformanceCounter"
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection:
`GET /api/CaseCreationStagePerformanceCounter` (landlord tenant only, any other caller receives 403), or via the
**Administration > Performance > Case Creation Stage Performance Counter** page in the UI. It accepts the same
`from`/`to`, `stageId` (`1`=`ExistingCasePriorityLookup`, `2`=`WorkflowStatusLookupAndPersist`, `3`=`Notification`,
`4`=`HttpEndpoint`), `take`, `samplePercentage` and `sortField`/`sortDirection` as above; `statistics` covers
`totalMicroseconds`, `minMicroseconds`, `maxMicroseconds` and `invokeCount`.

Each stage duration is also recorded to **`jube.engine.casecreation.stage.duration`** -- an OpenTelemetry histogram
metric (milliseconds), tagged by `stage`, on the same live-export basis as `jube.engine.archiver.stage.duration` above.

## `CaseCreationWarning`

One row per case creation stage invocation that took longer than `CaseCreationWarnThresholdMilliseconds` (see
[Environment Variables](../../EnvironmentVariables/index.html); also defaults to 10 seconds, for the same reason as
Archiver's threshold -- notification/webhook callbacks in particular are routinely slow by nature). Carries the case
workflow, case key/value, which stage was slow, its duration, and -- when the slow stage was `Notification` or
`HttpEndpoint` -- the actual destination or URL involved.

Up to the last 100000 rows are also browsable without a database connection: `GET /api/CaseCreationWarning` (landlord
tenant only, any other caller receives 403), or via the **Administration > Performance > Case Creation Warning** page in
the UI. It accepts the same `from`/`to`, `stageId`, `take`, `samplePercentage` and `sortField`/`sortDirection` as
`CaseCreationStagePerformanceCounter` above, plus a case-insensitive substring `search` against `CaseKeyValue`;
`statistics` covers `durationMicroseconds` only.

Each breach also increments **`jube.engine.casecreation.warn.count`** -- an OpenTelemetry counter, tagged by `stage`,
mirroring `jube.engine.archiver.warn.count` above.

## `ModelInvokeWarning`

The database-backed counterpart of the invoke pipeline's own trace-log warn threshold (see
[Invocation Trace Log](../InvocationTraceLog/index.html) and `jube.engine.logs.warn.count`) -- one row per trace point
whose gap since the previous trace point exceeded that Model's **Logs: Warn Threshold Milliseconds**. Enqueued from
exactly the same call site that increments the OTel counter, so the two never disagree about what counts as a warn
event. Carries the Model, the specific invocation, the trace point's message text, and both the elapsed-since-start and
elapsed-since-previous-trace-point durations.

Up to the last 100000 rows are also browsable without a database connection: `GET /api/ModelInvokeWarning` (landlord
tenant only, any other caller receives 403), or via the **Administration > Logs > Model Invoke Warning** page in the UI.
It accepts `from`/`to` (against `OccurredDate`, each defaulting independently to the last hour), a case-insensitive
substring `search` against the Model name or message, `take`, `samplePercentage` and
`sortField`/`sortDirection`. `entityAnalysisModelGuid` (an exact match) is also accepted but not surfaced in the UI
toolbar, since `search` against the Model name already covers the common case. `statistics` covers
`elapsedMicroseconds` and `sinceLastEntryMicroseconds`.

## `CaptureQueueHealth`

Every one of the four capture mechanisms above -- `ArchiverWarning`, `CaseCreationWarning`, `ModelInvokeWarning` -- and
two more elsewhere in the platform (`RedisSentinelEvent` and `RedisConnectionEvent`, plus the in-process
`OpenTelemetryMetric` aggregation described in
[Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html#local-capture-of-jubes-own-opentelemetry-metrics))
share the same shape: a bounded in-memory queue (20000 entries, or 10000 distinct series for `OpenTelemetryMetric`)
that something enqueues to and a background flush drains once a minute. `CaptureQueueHealth` is the one row-per-queue,
per-minute health check over all six of them -- `QueueDepth` (how many entries were sitting in the queue at the moment
it flushed; normally near zero) and `DroppedCount` (how many were dropped because the queue was full since the previous
flush; always zero in healthy operation). A `QueueDepth` that never falls, or any non-zero `DroppedCount`, is worth
investigating immediately -- either means real data is being delayed or silently discarded.

Unlike the stage/warning tables above, this one never skips a row even when nothing happened: a flatline at zero
depth/drops is itself the useful signal ("everything's fine"), distinct from "no data because nothing happened" -- an
operator watching this page needs a continuous timeline per queue, not just rows when something goes wrong.

``` sql
select "QueueName", "QueueDepth", "DroppedCount", "CreatedDate"
from "CaptureQueueHealth"
where "DroppedCount" > 0
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection: `GET /api/CaptureQueueHealth` (landlord
tenant only, any other caller receives 403), or via the **Administration > Performance > Capture Queue Health** page in
the UI. It accepts `from`/`to` (against `CreatedDate`), an exact-match `queueId` (`1`=`ModelInvokeWarning`,
`2`=`CaseCreationWarning`, `3`=`ArchiverWarning`, `4`=`RedisSentinelEvent`, `5`=`RedisConnectionEvent`,
`6`=`OpenTelemetryMetric`), `take` and `samplePercentage`.
