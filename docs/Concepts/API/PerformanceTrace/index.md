---
layout: default
title: HTTP API Performance Trace
nav_order: 4
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# HTTP API Performance Trace

Every invocation carries an `invokeTaskPerformance` object alongside its normal result, capturing where the time and
(best effort) memory went while the transaction was processed. It always has two parts:

``` text
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

`taskWrapperStats` is always included in the direct `/api/invoke` response, since it is a small, bounded object.
`stages` can be considerably larger for a Model with many configured rules, so it is only added to the direct response
when **Enable Response Trace** is switched on for that Model
(see [Model Configuration](../../../Configuration/Models/Models/index.html)) -- the switch only ever echoes an
already-captured breakdown into the direct response; it never controls whether that breakdown gets built in the first
place. The Archive payload -- what reaches case creation and webhook automation -- always has the full breakdown
regardless of this switch, since it is not constrained by direct-response payload size the same way.

Building `stages` at all -- not just including it in the direct response -- is real per-invocation cost: a small object
per pipeline stage, plus a further per-rule breakdown (a dictionary keyed by rule name) for any stage that loops over a
configured collection of rules. This is gated by the same **Enable Sampling**/ **Sample Percentage** switch that already
throttles Enable Logs: Info (see [Sampling](../InvocationTraceLog/index.html#sampling)), independently of Enable
Response Trace -- when Enable Logs: Info is off, or it's on with Enable Sampling off, every invocation still gets a full
`stages` build exactly as before; only once Enable Logs: Info *and* Enable Sampling are both on does the sampled
fraction alone pay that cost, so a high-throughput Model with many rules (and, for TTL Counters specifically, several
rule timings written into a shared dictionary from concurrently-running tasks) isn't forced to build and write per-rule
trace detail on every single request. `Enable Response Trace` itself is unaffected by this -- it still only decides
whether an already-built `stages` is *included in the direct response*; the Archive payload keeps the full breakdown for
whichever invocations were sampled, the same as it always has.

## Response Time Pipeline: a lighter always-on alternative

`taskWrapperStats` is bounded but says nothing about the main serial pipeline (parsing, Inline Functions, Gateway,
Abstraction Rules, Activation, writing the response, and so on) -- that detail normally requires `stages`, which in turn
requires either Enable Response Trace (for the direct response) or opening the Archive payload. For a quick read on
where the serial pipeline itself is spending time, without either of those, every invocation response also carries a
`responseTimePipeline` object:

``` text
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

This is always present -- no switch to turn on, and no per-rule breakdown the way `stages`' `items` has -- so it works
as a first, cheap look at where time and memory are going in the serial pipeline before reaching for Enable Response
Trace. `allocatedBytes` carries the same best-effort caveat as `invokeTaskPerformance.memory`: it's only reliable within
a single synchronous stretch on one thread, so a checkpoint immediately after an `await` that resumed on a different
thread can under- or overstate that stage's true allocation -- `threadId` changing between consecutive entries is the
tell that this happened.

## Monitoring stage performance across transactions

Where `stages` is a per-transaction snapshot, two further mechanisms give a cluster-wide, aggregate view over time:

- **`EntityAnalysisModelStagePerformanceCounter`** -- a database table, one row per stage per Model roughly every
  minute, with `TotalMicroseconds`, `MinMicroseconds`, `MaxMicroseconds` and `InvokeCount` for that interval -- the
  min/max let a spike in one invocation be told apart from a sustained shift in the average:

``` sql
select "StageName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "EntityAnalysisModelStagePerformanceCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection:
`GET /api/EntityAnalysisModelStagePerformanceCounter` (landlord tenant only, any other caller receives 403; the landlord
sees every tenant's rows), or via the **Performance > Model Stage Performance Counter** page in the UI.

- **`jube.engine.stage.duration`** -- an OpenTelemetry histogram metric (milliseconds), tagged by `stage` and `model`,
  exported alongside the existing `jube.service.operation.duration` metric whenever `EnableOpenTelemetry` is set
  (see [Environment Variables](../../EnvironmentVariables/index.html)). This is metrics only -- there is no
  per-invocation trace span, consistent with `/api/invoke` already being excluded from request-level tracing given its
  volume.

Both are populated regardless of whether Enable Response Trace is on for a given Model -- the switch only affects what
is returned to the direct HTTP caller.

## Monitoring the Response Time Pipeline across transactions

The Response Time Pipeline gets the same cluster-wide, aggregate treatment, since it is always on and every stage's
compute and memory cost is otherwise only visible per-invocation:

- **`EntityAnalysisModelResponseTimePipelineCounter`** -- a database table, one row per stage per Model roughly every
  minute, with `TotalMicroseconds`, `MinMicroseconds`, `MaxMicroseconds`, `TotalAllocatedBytes`, `MinAllocatedBytes`,
  `MaxAllocatedBytes` and `InvokeCount` for that interval (the same microsecond unit as
  `EntityAnalysisModelStagePerformanceCounter` above). `SequenceNumber` is the stage's fixed position in the serial
  pipeline, so a query can `ORDER BY` it to reconstruct the pipeline's shape even though the table itself is flat (one
  row per stage, not one nested document per Model):

``` sql
select "StageName", "SequenceNumber", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds",
       "TotalAllocatedBytes", "MinAllocatedBytes", "MaxAllocatedBytes", "InvokeCount", "CreatedDate"
from "EntityAnalysisModelResponseTimePipelineCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc, "SequenceNumber"
```

Dividing `TotalMicroseconds` (or `TotalAllocatedBytes`) by `InvokeCount` for a given interval gives that stage's average
cost, which is the quickest way to spot a stage that has drifted off its usual baseline; `MinMicroseconds`/
`MaxMicroseconds` and `MinAllocatedBytes`/`MaxAllocatedBytes` tell a spike in one invocation apart from a sustained
shift. This table is written on the same per-model, once-a-minute flush cycle -- and over the same database
connection -- as `EntityAnalysisModelStagePerformanceCounter` and `EntityAnalysisModelProcessingCounter` above.

`EntityAnalysisModelProcessingCounter` itself carries the same min/max treatment for the whole invocation:
`MinResponseTimeMicroseconds`/`MaxResponseTimeMicroseconds` alongside its existing `ModelTotalResponseTime`, closing
what was otherwise the one gap left across these tables -- every narrower breakdown (stage, response time pipeline,
task) already had min/max next to its total; the whole-invocation total did not, until now.

Up to the last 100000 rows are also browsable without a database connection:
`GET /api/EntityAnalysisModelResponseTimePipelineCounter` (landlord tenant only, any other caller receives 403; the
landlord sees every tenant's rows), or via the **Performance > Model Response Time Pipeline Counter** page in the UI.

Each entry is also recorded to **`jube.engine.responsetimepipeline.duration`** -- an OpenTelemetry histogram metric
(milliseconds), tagged by `stage`, exported whenever `EnableOpenTelemetry` is set -- the same live-export treatment
`jube.engine.stage.duration` gets above, and likewise metrics-only with no per-invocation span.

## Monitoring Task Performance across transactions

`invokeTaskPerformance.taskWrapperStats` (above) is per-invocation; the same per-task compute time and allocation is
also rolled up cluster-wide, once a minute, in **`EntityAnalysisModelTaskPerformanceCounter`** -- one row per read/write
task per Model per interval, with `Direction` ("Read" or "Write"), `TaskName`, `TotalMicroseconds`, `MinMicroseconds`,
`MaxMicroseconds`, `TotalAllocatedBytes`, `MinAllocatedBytes`, `MaxAllocatedBytes` and `InvokeCount`. Because this is
genuine compute time per task, it is also the most direct way to attribute cloud compute cost to a specific task (e.g.
`SanctionsAsync` versus `CachePayloadUpsertAsync`) rather than to the invocation as a whole:

``` sql
select "Direction", "TaskName", "TotalMicroseconds", "MinMicroseconds", "MaxMicroseconds", "InvokeCount", "CreatedDate"
from "EntityAnalysisModelTaskPerformanceCounter"
where "EntityAnalysisModelGuid" = '90c425fd-101a-420b-91d1-cb7a24a969cc'
order by "CreatedDate" desc
```

Up to the last 100000 rows are also browsable without a database connection:
`GET /api/EntityAnalysisModelTaskPerformanceCounter` (landlord tenant only, any other caller receives 403; the landlord
sees every tenant's rows), or via the **Performance > Model Task Performance Counter** page in the UI.

Each task is also recorded to **`jube.engine.task.duration`** -- an OpenTelemetry histogram metric (milliseconds),
tagged by `direction` and `task`, exported whenever `EnableOpenTelemetry` is set, on the same metrics-only basis as
`jube.engine.stage.duration` above.

All three of these endpoints -- Stage, Response Time Pipeline and Task Performance Counter -- accept `from`/`to` (a UTC
date/time range against `CreatedDate`, inclusive), pushed down to the database via LINQ2DB rather than filtered after
the fetch. `from` and `to` each default independently to the last hour when omitted. In place of a text `search`, Stage
and Response Time Pipeline take an exact-match `stageId` (the `InvokeStage` enum's numeric value -- e.g. `1`=`Parse`,
`5`=`Gateway`), and Task Performance Counter takes `directionId` (`1`=`Read`, `2`=`Write`) and `taskTypeId`
(`Jube.TaskCancellation.TaskHelper.TaskType`'s numeric value): these are indexed int columns, not free text, so
filtering by them never falls back to a sequential scan the way a `StageName`/`TaskName` substring match would on these
high-volume tables. Each also accepts `sortField`/`sortDirection` -- any column the grid displays; `sortDirection` is
case-insensitive (`asc` for ascending, anything else including omitted for descending), and an unrecognised or omitted
`sortField` falls back to the table's own most-recent-first order -- pushed down to the database as an `ORDER BY`, never
applied after the fetch. Each viewer page in the UI exposes the equivalent dropdown (s) alongside a date-time range in a
toolbar above its grid, pre-populated with that same last-hour range, and sorts server-side on column-header click.

They also accept `samplePercentage` (0-100, clamped), applied server-side alongside the existing tenant scope and
`from`/`to`/`stageId`/`directionId`/`taskTypeId` filters rather than after fetching -- each row that already matches
every other criterion (including, for a non-landlord caller, that caller's own tenant) is included independently with
that probability, so `samplePercentage=10` returns roughly a random one-tenth of matching rows rather than the newest
tenth. It is not exposed in any toolbar; it exists for an agent to draw an unbiased random baseline sample of counter
activity early on and compare it against recent activity later.

Each response is an envelope, not a bare array: `{ rows, total, statistics }`. `rows` is the page of results actually
returned (respecting `take`); `total` is the true count of every row matching the filter, regardless of `take`;
`statistics` is a summary-statistics block -- Min/Max/Mean/Median/StandardDeviation and a 10-bucket histogram, computed
via Accord.NET over the full filtered set (capped at 100000, independent of `take`) -- keyed by the camelCase name of
every continuous/measured numeric column on that DTO: `totalMicroseconds`/`minMicroseconds`/`maxMicroseconds`/
`invokeCount` for Stage; those four plus `totalAllocatedBytes`/`minAllocatedBytes`/`maxAllocatedBytes` for Response Time
Pipeline and Task Performance Counter. Categorical numeric columns -- `stageId`, `sequenceNumber`, `directionId`,
`taskTypeId` -- are never included even though they are numeric, since they are codes/positions, not measured
quantities.

The Archiver and case creation background pipelines -- off the invoke pipeline's own thread entirely -- get the
equivalent treatment in [Background Processing Performance](../BackgroundProcessingPerformance/index.html).
