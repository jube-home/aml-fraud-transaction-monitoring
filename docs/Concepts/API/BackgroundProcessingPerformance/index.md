---
layout: default
title: Background Processing Performance
nav_order: 12
parent: API
grand_parent: Concepts
---

# Background Processing Performance

[HTTP API Performance Trace](../PerformanceTrace/index.html) covers the invoke pipeline itself, which is to say the serial, per-request stages through which a transaction passes on the thread that received it. Two further pipelines run entirely off that thread, each on its own background queue, being the Archiver (serialising and persisting the Archive payload of the invocation, then handing off to case creation) and case creation itself (looking up the priority of an existing case, persisting the workflow status, then optionally a notification, an HTTP callback or both). Both are given the same cluster-wide, aggregate rollup and warn threshold capture as the invoke pipeline, and both feed a shared `CaptureQueueHealth` mechanism that also covers a handful of other bounded in-memory queues elsewhere in the platform.

Each table described on this page is browsable in the user interface by navigating through the menu to Administration, then Performance, which is presented to the landlord only, where the counters are grouped into families.

## `ArchiverStagePerformanceCounter`

One row per Archiver stage per Model roughly every minute, with `TotalMicroseconds`, `MinMicroseconds`, `MaxMicroseconds` and `InvokeCount` for that interval, which is the same shape as `EntityAnalysisModelStagePerformanceCounter` in [HTTP API Performance Trace](../PerformanceTrace/index.html#monitoring-stage-performance-across-transactions), applied to the four stages of the Archiver instead of those of the invoke pipeline:

| stageId | Stage                 | Description                                                                                                                                                                                                                                                                   |
|---------|-----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1       | BuildArchiveJson      | Serialising the Archive payload of the invocation to JSON.                                                                                                                                                                                                                    |
| 2       | CaseCreationDispatch  | Handing that payload off to case creation (synchronous reprocessing recall, or enqueuing for the normal asynchronous path).                                                                                                                                                   |
| 3       | RdbmsArchiveWrite     | The per-item RDBMS archive write (a reprocessing update) or buffered insert.                                                                                                                                                                                                  |
| 4       | BulkCopyArchiveBuffer | The periodic buffered bulk copy of accumulated archive rows, timed and flushed independently of any single invocation (`EntityAnalysisModelInstanceEntryGuid` is null in `ArchiverWarning` below for this stage specifically, since it operates on a batch rather than one item). |

```sql
select "StageName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "ArchiverStagePerformanceCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection via `GET /api/ArchiverStagePerformanceCounter` (landlord tenant only, any other caller receiving 403), or as Archiver stage performance counter in the Engine family of the Performance page. The endpoint accepts the following parameters:

| Parameter                | Description                                                                                                                                                                                                                                  |
|--------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| from, to                 | A range against `CreatedDate`, each defaulting independently to the last hour.                                                                                                                                                               |
| stageId                  | An exact match on the stage, as numbered in the table above.                                                                                                                                                                                 |
| take                     | The maximum number of rows returned, clamped to 100000.                                                                                                                                                                                      |
| samplePercentage         | From 0 to 100, clamped, for the purposes of an agent drawing an unbiased baseline sample.                                                                                                                                                    |
| sortField, sortDirection | Any column the grid displays. `sortDirection` is case-insensitive, `asc` being ascending and anything else, including omitted, descending. An unrecognised or omitted `sortField` falls back to most-recent-first.                           |

The response is an envelope of the form `{ rows, total, statistics }`, where `rows` respects `take`, `total` is the full filtered count regardless of `take`, and `statistics` (Min, Max, Mean, Median and StandardDeviation plus a 10-bucket histogram, computed over the full filtered set capped at 100000) covers `totalMicroseconds`, `minMicroseconds`, `maxMicroseconds` and `invokeCount`. `stageId` itself, being a category code rather than a measured quantity, is excluded. Every endpoint documented on this page shares this same envelope, sort and statistics shape, henceforth the sections below only set out the specific filters and statistics columns of each table.

Each stage duration is also recorded to `jube.engine.archiver.stage.duration`, an OpenTelemetry histogram metric (milliseconds), tagged by `stage`, and exported whenever `EnableOpenTelemetry` is set. It is a separate instrument from `jube.engine.stage.duration`, since the Archiver runs off the invoke pipeline entirely and has its own, unrelated stage vocabulary.

## `ArchiverWarning`

One row per Archiver stage invocation that took longer than `ArchiverWarnThresholdMilliseconds` (see [Environment Variables](../../EnvironmentVariables/index.html)), which defaults to 10 seconds, being deliberately much higher than an in-thread invoke trace threshold, since RDBMS writes and buffered bulk copies are routinely slow by nature. The row carries the Model, the specific invocation (`EntityAnalysisModelInstanceEntryGuid`, null for `BulkCopyArchiveBuffer`), which stage was slow, and its actual duration, such that a slow Archiver backlog can be traced to the specific item and cause, and not only to the aggregate stage statistics above.

Up to the last 100000 rows are also browsable without a database connection via `GET /api/ArchiverWarning` (landlord tenant only, any other caller receiving 403), or as Archiver warnings in the Engine family of the Performance page. The endpoint accepts the same `from`, `to`, `stageId`, `take`, `samplePercentage`, `sortField` and `sortDirection` as `ArchiverStagePerformanceCounter` above, and `statistics` covers `durationMicroseconds` only.

Each breach also increments `jube.engine.archiver.warn.count`, an OpenTelemetry counter, tagged by `stage`, mirroring `jube.engine.logs.warn.count` (see [Invocation Trace Log](../InvocationTraceLog/index.html)) for the same count of breaches upon which an alert can watch without a database query.

## `CaseCreationStagePerformanceCounter`

The same per-minute rollup, for the stages of case creation. It is not scoped to any Model, as case creation is a single global queue shared by every model:

| stageId | Stage                          | Description                                                                                                                              |
|---------|--------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| 1       | ExistingCasePriorityLookup     | Checking whether a case already exists for the workflow, key and value and, if so, whether the new priority supersedes it.               |
| 2       | WorkflowStatusLookupAndPersist | Looking up the target workflow status and inserting or updating the case row.                                                            |
| 3       | Notification                   | Only present when the resulting workflow status has notifications enabled.                                                               |
| 4       | HttpEndpoint                   | Only present when the resulting workflow status has an HTTP callback enabled.                                                            |

```sql
select "StageName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "CaseCreationStagePerformanceCounter"
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection via `GET /api/CaseCreationStagePerformanceCounter` (landlord tenant only, any other caller receiving 403), or as Case creation stage performance counter in the Engine family of the Performance page. The endpoint accepts the same `from`, `to`, `stageId` (as numbered in the table above), `take`, `samplePercentage`, `sortField` and `sortDirection` as above, and `statistics` covers `totalMicroseconds`, `minMicroseconds`, `maxMicroseconds` and `invokeCount`.

Each stage duration is also recorded to `jube.engine.casecreation.stage.duration`, an OpenTelemetry histogram metric (milliseconds), tagged by `stage`, on the same live-export basis as `jube.engine.archiver.stage.duration` above.

## `CaseCreationWarning`

One row per case creation stage invocation that took longer than `CaseCreationWarnThresholdMilliseconds` (see [Environment Variables](../../EnvironmentVariables/index.html)), which also defaults to 10 seconds, for the same reason as the threshold of the Archiver, keeping in mind that notification and webhook callbacks in particular are routinely slow by nature. The row carries the case workflow, the case key and value, which stage was slow, its duration and, when the slow stage was `Notification` or `HttpEndpoint`, the actual destination or URL involved.

Up to the last 100000 rows are also browsable without a database connection via `GET /api/CaseCreationWarning` (landlord tenant only, any other caller receiving 403), or as Case creation warnings in the Engine family of the Performance page. The endpoint accepts the same `from`, `to`, `stageId`, `take`, `samplePercentage`, `sortField` and `sortDirection` as `CaseCreationStagePerformanceCounter` above, plus a case-insensitive substring `search` against `CaseKeyValue`, and `statistics` covers `durationMicroseconds` only.

Each breach also increments `jube.engine.casecreation.warn.count`, an OpenTelemetry counter, tagged by `stage`, mirroring `jube.engine.archiver.warn.count` above.

## `ModelInvokeWarning`

The database-backed counterpart of the trace log warn threshold of the invoke pipeline (see [Invocation Trace Log](../InvocationTraceLog/index.html) and `jube.engine.logs.warn.count`), being one row per trace point whose gap since the previous trace point exceeded the Warn Threshold Milliseconds of that Model. The row is enqueued from exactly the same call site that increments the OpenTelemetry counter, so the two never disagree about what counts as a warn event. It carries the Model, the specific invocation, the message text of the trace point, and both the elapsed-since-start and the elapsed-since-previous-trace-point durations.

Up to the last 100000 rows are also browsable without a database connection via `GET /api/ModelInvokeWarning` (landlord tenant only, any other caller receiving 403), or as Model invoke warnings in the Logs family of the Performance page. The endpoint accepts `from` and `to` (against `OccurredDate`, each defaulting independently to the last hour), a case-insensitive substring `search` against the Model name or message, `take`, `samplePercentage`, `sortField` and `sortDirection`. `entityAnalysisModelGuid` (an exact match) is also accepted, although `search` against the Model name already covers the common case. `statistics` covers `elapsedMicroseconds` and `sinceLastEntryMicroseconds`.

## `CaptureQueueHealth`

The three warning captures above (`ArchiverWarning`, `CaseCreationWarning` and `ModelInvokeWarning`), together with three more elsewhere in the platform (`RespSentinelEvent`, `RespConnectionEvent` and the in-process `OpenTelemetryMetric` aggregation described in [Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html#local-capture-of-jubes-own-opentelemetry-metrics)), share the same shape, being a bounded in-memory queue (20000 entries, or 10000 distinct series for `OpenTelemetryMetric`) to which something enqueues and which a background flush drains once a minute. `CaptureQueueHealth` is the health check over all six of them, having one row per queue per minute, with `QueueDepth` (how many entries were sitting in the queue at the moment it flushed, normally near zero) and `DroppedCount` (how many were dropped because the queue was full since the previous flush, always zero in healthy operation). A `QueueDepth` that never falls, or any `DroppedCount` that is not zero, is worth investigating immediately, as either means that real data is being delayed or silently discarded.

Unlike the stage and warning tables above, this table never skips a row even when nothing happened. A flatline at zero depth and zero drops is itself the useful signal, being distinct from an absence of data because nothing happened, and an operator watching the page needs a continuous timeline for each queue, not only rows when something goes wrong.

```sql
select "QueueName", "QueueDepth", "DroppedCount", "CreatedDate"
from "CaptureQueueHealth"
where "DroppedCount" > 0
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection via `GET /api/CaptureQueueHealth` (landlord tenant only, any other caller receiving 403), or as Capture queue health in the Engine family of the Performance page. The endpoint accepts `from` and `to` (against `CreatedDate`), `take`, `samplePercentage` and an exact-match `queueId`, as follows:

| queueId | Queue               |
|---------|---------------------|
| 1       | ModelInvokeWarning  |
| 2       | CaseCreationWarning |
| 3       | ArchiverWarning     |
| 4       | RespSentinelEvent   |
| 5       | RespConnectionEvent |
| 6       | OpenTelemetryMetric |
