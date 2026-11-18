---
layout: default
title: HTTP API Performance Trace
nav_order: 4
parent: API
grand_parent: Concepts
---

# HTTP API Performance Trace

Every invocation carries an `invokeTaskPerformance` object alongside its normal result, capturing where the time and (on a best effort basis) the memory went while the transaction was processed. It always has two parts, as follows:

```text
invokeTaskPerformance
├─ totalDurationMicroseconds   -- the whole invocation, start to finish
├─ memory                      -- a best-effort end-to-end allocation estimate
├─ taskWrapperStats            -- always present: the individual cache reads and writes queued
│                                 during the invocation (Sanctions, TTL Counters, Abstraction
│                                 Rules with search keys on the read side; the various cache
│                                 upserts/inserts on the write side), each with its own compute
│                                 time and memory
└─ stages                      -- only present when Enable Response Trace is on for the Model: a duration
                                   for every stage of the pipeline (Parse, Inline Functions,
                                   Inline Scripts, Gateway, Cache, Sanctions, TTL Counters,
                                   Abstraction Rules, Abstraction Calculations, Exhaustive
                                   Adaptation, HTTP Adaptations, Activation, joining the read/write
                                   tasks, writing the response, building the Archive payload), and
                                   for every stage that loops over a configured collection of rules
                                   (Gateway Rules, Abstraction Rules, Abstraction Calculations,
                                   Inline Functions, Inline Scripts, Exhaustive models, HTTP
                                   Adaptations, Activation Rules) a further breakdown keyed by rule
                                   name, so a slow stage can be traced to the specific rule or
                                   endpoint responsible for it
```

## Enable Response Trace

`taskWrapperStats` is always included in the direct `/api/invoke` response, since it is a small, bounded object. `stages` can be considerably larger for a Model with many configured rules, henceforth it is only added to the direct response when Enable Response Trace, in the Response Trace group, is switched on for that Model (see [Model Configuration](../../../Configuration/Models/Models/index.html)). The switch only ever echoes an already-captured breakdown into the direct response, and it never controls whether that breakdown is built in the first place. The Archive payload, which is what reaches case creation and webhook automation, always has the full breakdown regardless of the switch, since it is not constrained by the size of the direct response payload in the same manner.

The building of `stages` at all, and not only its inclusion in the direct response, is a real cost for each invocation, being a small object for each pipeline stage, plus a further per-rule breakdown (a dictionary keyed by rule name) for any stage that loops over a configured collection of rules. It is gated by the same Enable Sampling and Sample Percentage that already throttle Enable Logs: Info (see [Sampling](../InvocationTraceLog/index.html#sampling)), independently of Enable Response Trace. When Enable Logs: Info is off, or is on with Enable Sampling off, every invocation still has a full `stages` built; only once Enable Logs: Info and Enable Sampling are both on does the sampled fraction alone pay that cost. It follows that a high-throughput Model with many rules (and, for TTL Counters specifically, several rule timings written into a shared dictionary from concurrently running tasks) is not forced to build and write per-rule trace detail on every single request. Enable Response Trace itself is unaffected, in that it still only decides whether an already-built `stages` is included in the direct response, and the Archive payload keeps the full breakdown for whichever invocations were sampled.

## Response Time Pipeline: a lighter always-on alternative

`taskWrapperStats` is bounded but says nothing about the main serial pipeline (parsing, Inline Functions, Gateway, Abstraction Rules, Activation, writing the response, and so on), as that detail normally requires `stages`, which in turn requires either Enable Response Trace (for the direct response) or the opening of the Archive payload. For a quick read on where the serial pipeline itself is spending time, without either of those, every invocation response also carries a `responseTimePipeline` object:

```text
responseTimePipeline
└─ entries[]
   ├─ stage                 -- the checkpoint name (Parse, InlineFunctions, InlineScripts, Gateway,
   │                            CacheDbStorage, JoinReadTasks, AbstractionRulesWithoutSearchKeys,
   │                            AbstractionCalculations, ExhaustiveAdaptation, HttpAdaptation, Activation,
   │                            JoinWriteTasks, WriteResponse, BuildArchivePayload)
   ├─ elapsedMicroseconds    -- microseconds elapsed since the invocation started, at that checkpoint
   │                            (the same unit as invokeTaskPerformance's compute time figures)
   ├─ durationMicroseconds   -- this stage's own cost -- the gap since the previous checkpoint
   ├─ allocatedBytes         -- best-effort bytes allocated during this stage
   └─ threadId               -- the managed thread that recorded this checkpoint
```

The object is present without any switch needing to be turned on, although its capture is subject to the same per-invocation sample decision described in [Sampling](../InvocationTraceLog/index.html#sampling), and it has no per-rule breakdown in the manner of the `items` of `stages`. It therefore serves as a first, cheap look at where time and memory are going in the serial pipeline before reaching for Enable Response Trace. `allocatedBytes` carries the same best-effort caveat as `invokeTaskPerformance.memory`, in that it is only reliable within a single synchronous stretch on one thread, so a checkpoint immediately after an `await` that resumed on a different thread can understate or overstate the true allocation of that stage. A `threadId` changing between consecutive entries is the indication that this has happened.

## Monitoring stage performance across transactions

Where `stages` is a snapshot of a single transaction, two further mechanisms give a cluster-wide, aggregate view over time.

The first is `EntityAnalysisModelStagePerformanceCounter`, a database table having one row per stage per Model roughly every minute, with `TotalMicroseconds`, `MinMicroseconds`, `MaxMicroseconds` and `InvokeCount` for that interval, where the minimum and maximum allow a spike in one invocation to be told apart from a sustained shift in the average:

```sql
select "StageName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "EntityAnalysisModelStagePerformanceCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection via `GET /api/EntityAnalysisModelStagePerformanceCounter` (landlord tenant only, any other caller receiving 403; the landlord sees the rows of every tenant), or in the user interface by navigating through the menu to Administration, then Performance, where Model stage performance counter is presented in the Engine family.

The second is `jube.engine.stage.duration`, an OpenTelemetry histogram metric (milliseconds), tagged by `stage` and `model`, and exported alongside the existing `jube.service.operation.duration` metric whenever `EnableOpenTelemetry` is set (see [Environment Variables](../../EnvironmentVariables/index.html)). It is metrics only, there being no per-invocation trace span, which is consistent with `/api/invoke` already being excluded from request-level tracing given its volume.

Both are populated regardless of whether Enable Response Trace is on for a given Model, as the switch only affects what is returned to the direct HTTP caller.

## Monitoring the Response Time Pipeline across transactions

The Response Time Pipeline is given the same cluster-wide, aggregate treatment, since the compute and memory cost of every stage is otherwise only visible for each invocation.

`EntityAnalysisModelResponseTimePipelineCounter` is a database table having one row per stage per Model roughly every minute, with `TotalMicroseconds`, `MinMicroseconds`, `MaxMicroseconds`, `TotalAllocatedBytes`, `MinAllocatedBytes`, `MaxAllocatedBytes` and `InvokeCount` for that interval (the same microsecond unit as `EntityAnalysisModelStagePerformanceCounter` above). `SequenceNumber` is the fixed position of the stage in the serial pipeline, so a query can `ORDER BY` it to reconstruct the shape of the pipeline even though the table itself is flat (one row per stage, not one nested document per Model):

```sql
select "StageName", "SequenceNumber", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds",
       "TotalAllocatedBytes", "MinAllocatedBytes", "MaxAllocatedBytes", "InvokeCount", "CreatedDate"
from "EntityAnalysisModelResponseTimePipelineCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc, "SequenceNumber"
```

Dividing `TotalMicroseconds` (or `TotalAllocatedBytes`) by `InvokeCount` for a given interval gives the average cost of that stage, which is the quickest way to spot a stage that has drifted off its usual baseline, while `MinMicroseconds` and `MaxMicroseconds`, and `MinAllocatedBytes` and `MaxAllocatedBytes`, tell a spike in one invocation apart from a sustained shift. The table is written on the same per-model, once-a-minute flush cycle, and over the same database connection, as `EntityAnalysisModelStagePerformanceCounter` and `EntityAnalysisModelProcessingCounter`.

`EntityAnalysisModelProcessingCounter` itself carries the same minimum and maximum treatment for the whole invocation, being `MinResponseTimeMicroseconds` and `MaxResponseTimeMicroseconds` alongside its `ModelTotalResponseTime`. It follows that every breakdown, be it the whole invocation or the narrower stage, response time pipeline and task, has a minimum and maximum next to its total.

Up to the last 100000 rows are also browsable without a database connection via `GET /api/EntityAnalysisModelResponseTimePipelineCounter` (landlord tenant only, any other caller receiving 403; the landlord sees the rows of every tenant), or in the user interface by navigating through the menu to Administration, then Performance, where Model response time pipeline counter is presented in the Engine family.

Each entry is also recorded to `jube.engine.responsetimepipeline.duration`, an OpenTelemetry histogram metric (milliseconds), tagged by `stage`, and exported whenever `EnableOpenTelemetry` is set, which is the same live-export treatment as that of `jube.engine.stage.duration` above, and likewise metrics only with no per-invocation span.

## Monitoring Task Performance across transactions

`invokeTaskPerformance.taskWrapperStats`, as above, is per-invocation. The same per-task compute time and allocation is also rolled up cluster-wide, once a minute, in `EntityAnalysisModelTaskPerformanceCounter`, having one row per read or write task per Model per interval, with `Direction` ("Read" or "Write"), `TaskName`, `TotalMicroseconds`, `MinMicroseconds`, `MaxMicroseconds`, `TotalAllocatedBytes`, `MinAllocatedBytes`, `MaxAllocatedBytes` and `InvokeCount`. As this is genuine compute time for each task, it is also the most direct way to attribute the cost of cloud compute to a specific task (e.g. `SanctionsAsync` versus `CachePayloadUpsertAsync`) rather than to the invocation as a whole:

```sql
select "Direction", "TaskName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "EntityAnalysisModelTaskPerformanceCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection via `GET /api/EntityAnalysisModelTaskPerformanceCounter` (landlord tenant only, any other caller receiving 403; the landlord sees the rows of every tenant), or in the user interface by navigating through the menu to Administration, then Performance, where Model task performance counter is presented in the Engine family.

Each task is also recorded to `jube.engine.task.duration`, an OpenTelemetry histogram metric (milliseconds), tagged by `direction` and `task`, and exported whenever `EnableOpenTelemetry` is set, on the same metrics-only basis as `jube.engine.stage.duration` above.

All three of these endpoints, which is to say Stage, Response Time Pipeline and Task Performance Counter, accept the following parameters, each pushed down to the database via LINQ2DB rather than applied after the fetch:

| Parameter                  | Description                                                                                                                                                                                                                                                                         |
|----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| from, to                   | A UTC date and time range against `CreatedDate`, inclusive. Each defaults independently to the last hour when omitted.                                                                                                                                                              |
| stageId                    | Stage and Response Time Pipeline only, in place of a text `search`. An exact match on the numeric value of the `InvokeStage` enum (e.g. `1` for `Parse`, `5` for `Gateway`).                                                                                                         |
| directionId, taskTypeId    | Task Performance Counter only. `directionId` is `1` for `Read` and `2` for `Write`, and `taskTypeId` is the numeric value of `Jube.TaskCancellation.TaskHelper.TaskType`.                                                                                                           |
| sortField, sortDirection   | Any column the grid displays. `sortDirection` is case-insensitive (`asc` for ascending, anything else including omitted for descending), and an unrecognised or omitted `sortField` falls back to the most-recent-first order of the table. It is applied in the database as an `ORDER BY`. |
| take                       | The maximum number of rows returned.                                                                                                                                                                                                                                                |
| samplePercentage           | From 0 to 100, clamped. Each row already matching every other criterion is included independently with that probability.                                                                                                                                                            |

The identifiers are indexed integer columns, not free text, so filtering by them never falls back to a sequential scan in the way that a substring match on `StageName` or `TaskName` would on these high-volume tables. It follows from `samplePercentage` being applied to each row independently that `samplePercentage=10` returns roughly a random one-tenth of matching rows rather than the newest tenth; it exists for the purposes of an agent drawing an unbiased random baseline sample of counter activity early on, to be compared against recent activity later.

{% comment %}TODO(verify): confirm on a running system the filters offered above the grid of each counter in Administration, Performance; this page formerly stated a toolbar with the equivalent dropdowns and a date and time range, opened with the last hour, sorting on the server upon a column header being clicked, and samplePercentage not exposed{% endcomment %}

Each response is an envelope, not a bare array, of the form `{ rows, total, statistics }`. `rows` is the page of results actually returned (respecting `take`), `total` is the true count of every row matching the filter, regardless of `take`, and `statistics` is a block of summary statistics, being Min, Max, Mean, Median and StandardDeviation and a 10-bucket histogram, computed via the analytics library over the full filtered set (capped at 100000, independent of `take`), and keyed by the camelCase name of every continuous, measured numeric column, as follows:

| Endpoint                                         | Columns in statistics                                                                                                                    |
|--------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| Stage                                            | `totalMicroseconds`, `minMicroseconds`, `maxMicroseconds`, `invokeCount`                                                                 |
| Response Time Pipeline, Task Performance Counter | Those four, plus `totalAllocatedBytes`, `minAllocatedBytes`, `maxAllocatedBytes`                                                         |

Categorical numeric columns (`stageId`, `sequenceNumber`, `directionId`, `taskTypeId`) are never included even though they are numeric, since they are codes and positions, not measured quantities.

The Archiver and case creation background pipelines, which run off the thread of the invoke pipeline entirely, are given the equivalent treatment in [Background Processing Performance](../BackgroundProcessingPerformance/index.html).
