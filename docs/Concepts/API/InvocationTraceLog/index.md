---
layout: default
title: HTTP API Invocation Trace Log
nav_order: 5
parent: API
grand_parent: Concepts
---

# HTTP API Invocation Trace Log

Understanding what happened step by step during an invocation has typically meant enabling log4net INFO logging and finding the right log file on the right node, being an operational change that pressures disk IO and slows response times, henceforth it is naturally disabled in production. There instead exist switches in the Model which capture the same step-by-step trace points into a `logs` array on the invocation payload, such that the trace can be pulled straight from the Archive or the direct response to build a report, with no backend or log file access needed.

## Enabling Log Models

The [Model Configuration](../../../Configuration/Models/Models/index.html) has, in its Logs group, a master switch, Enable Logs, beneath which sit two independent Logs switches. Neither of the two captures unless Enable Logs is also on, and turning Enable Logs off disables both immediately without losing their own settings underneath, so that it doubles as a single, unambiguous kill switch. The two switches are not mutually exclusive levels of one switch, since they answer different questions, and a model can have either, both, or neither on:

| Switch                      | Behaviour                                                                                                                                                                                                                                                           |
|-----------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Enable Logs: Info           | Every trace point in the invocation pipeline is captured, for a sampled subset of invocations (see Sampling below). Off by default, and the INFO logging of log4net itself (if separately enabled in the logging configuration) is completely unaffected either way. |
| Enable Logs: Warn Threshold | Only trace points whose gap since the previously captured entry exceeds a Warn Threshold in milliseconds are captured, evaluated on every invocation (never sampled), so that a busy invocation only surfaces where time actually blocked. Off by default.          |

{% comment %}TODO(verify): confirm the labels of the Logs group of the Model properties form as shown on screen (Enable Logs, Enable Logs: Info, Enable Sampling, Sample Percentage, Enable Logs: Warn Threshold, Warn Threshold Milliseconds, Enable Logs In Response); they are derived from the property names EnableLogs, EnableLogsInfo, EnableSampling, SamplePercentage, EnableLogsWarnThreshold, LogsWarnThresholdMilliseconds and EnableLogsInResponse, and the names used on this page are those of the field descriptions{% endcomment %}

Turning either on never changes what log4net writes, the two being independent sinks fed from the same call sites. The intention is that log4net be relegated to developer tooling only.

Warn Threshold Milliseconds, which is 250 for a new Model, measures the gap since the previous captured entry, not since the invocation started. A threshold of 250 therefore means that a trace point is captured only if at least 250ms have passed since the last one that was captured, and it follows that a model that logs at every stage boundary will only surface the boundaries where something was actually slow. The Warn threshold check is independent of Enable Logs: Info, in that it runs regardless of whether Info is also on, and a single trace point can be captured because of Info sampling, the Warn threshold, or both at once (see Shape below for how a captured entry records which).

## Sampling

Enable Logs: Info captures every trace point on every invocation by default, which is exactly the disk, CPU and payload size cost that made log4net INFO logging impractical to leave on in production in the first place, notwithstanding that the bulk asynchronous insert is substantially more efficient. Enable Sampling (visible only when Enable Logs: Info is on) allows Info to run continuously at a small fraction of that cost. Upon turning it on and setting Sample Percentage (0 to 100, e.g. `0.02` for roughly 1 in 5000 invocations), only a random sample of invocations actually have their trace points captured, and the rest behave as if Enable Logs: Info were off. A sampled invocation still has every trace point captured, not a random subset of points within it, so the trace of a captured invocation is always complete. Sampling volume is itself a useful performance signal, reported independently of Warn breaches (see Monitoring below).

Enable Sampling and Sample Percentage only ever take effect while Enable Logs: Info is actually on. If Enable Logs: Info is off, every invocation is treated as sampled regardless of what Enable Sampling and Sample Percentage happen to be set to underneath (for example, left over from an earlier test with Enable Logs: Info on). The user interface only hides the Enable Sampling control when Enable Logs: Info is off, and does not clear it, which matters for a Model that has ever had sampling configured.

Sampling only ever throttles Info, and it has no effect on Enable Logs: Warn Threshold. Keeping in mind that Warn already only captures anomalies (gaps exceeding the threshold), sampling it would risk missing the very breaches it exists to catch, henceforth Warn evaluates every invocation regardless of Enable Sampling.

The same per-invocation sample decision also gates the capture of the Response Time Pipeline, the Task Performance Counter and `invokeTaskPerformance.stages` (the per-stage and per-rule breakdown of Enable Response Trace) for that Model (see [HTTP API Performance Trace](../PerformanceTrace/index.html)), all three of which are otherwise always on and have no Off, Info or Warn level of their own to exempt. The building of `stages` at all is itself a real cost for each invocation (a `StageDuration` or `StageTiming` object for each pipeline stage, plus a further per-rule dictionary for stages that loop over a configured collection of rules), so gating it in the same manner avoids paying that cost on every invocation at high throughput, regardless of whether Enable Response Trace happens to be on for that Model. Enable Response Trace only ever controls whether an already-captured `stages` survives into the direct response, as the Archive payload keeps the full breakdown regardless, for whichever invocations were sampled. It follows that the `logs`, `responseTimePipeline`, `invokeTaskPerformance.taskWrapperStats` and `invokeTaskPerformance.stages` of a sampled invocation all line up with each other, such that a report built from one sampled invocation can correlate its trace against its own timing and memory data rather than against a different, unrelated set of sampled invocations.

Whether a given invocation was selected is recorded on the payload itself as `isSampled` (a top-level boolean, alongside `logs`), so a report never has to derive it again from whether `logs.entries` happens to be present. `isSampled` is `true` whenever Enable Logs: Info is off, Enable Sampling is off (there being nothing to throttle), or the random draw selected the invocation, and it is unaffected by the independent capture of Warn mode.

## Shape

```text
logs
├─ anchorDate   -- the invocation's start time (UTC), matching the payload's CreatedDate
└─ entries[]
   ├─ elapsedMicroseconds          -- elapsed time since the invocation started
   ├─ sinceLastEntryMicroseconds   -- elapsed time since the previous captured entry
   ├─ capturedByInfoSampling       -- true if this entry exists because Enable Logs: Info was on and this
   │                                  invocation was sampled.
   ├─ capturedByWarnThreshold      -- true if this entry exists because sinceLastEntryMicroseconds was at or
   │                                  above the configured Warn threshold. Not mutually exclusive with
   │                                  capturedByInfoSampling above -- both can be true on the same entry when
   │                                  both switches are on and this invocation's trace point happens to
   │                                  qualify under both, and a reader attributing volume to one cause or the
   │                                  other reads these two flags directly rather than re-deriving cause from
   │                                  the gap size or the Model's current configuration.
   ├─ threadId                     -- the managed thread that recorded this entry. A change in threadId
   │                                  between consecutive entries means the invocation resumed on a different
   │                                  thread after an await -- useful context, since it's also when a
   │                                  best-effort memory reading elsewhere in the payload (allocatedBytes on
   │                                  the Response Time Pipeline) is least reliable.
   └─ message                      -- the trace point's message, without the surrounding invocation/model identifiers
```

## Where it surfaces

The Archive payload always carries the full `logs` array whenever either Enable Logs: Info or Enable Logs: Warn Threshold is on, which is the same treatment given to the performance `stages` breakdown (see [HTTP API Performance Trace](../PerformanceTrace/index.html)). The direct `/api/invoke` response only includes it when Enable Logs In Response is also switched on for the Model, independently of which of the two switches is on. It follows that a Model can capture Warn-only anomalies into the Archive for later reporting while never adding them to the direct response, or the reverse.

## Monitoring capture volume across transactions

Each switch reports its own capture volume as an independent OpenTelemetry counter, tagged by `model`, and exported alongside `jube.engine.stage.duration` whenever `EnableOpenTelemetry` is set (see [Environment Variables](../../EnvironmentVariables/index.html)):

| Counter                       | Description                                                                                                                                                                                                                                                                                                                                                                                 |
|-------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `jube.engine.logs.warn.count` | Incremented every time Enable Logs: Warn Threshold is on and the gap of a trace point since the last captured entry exceeds the threshold. It gives a cluster-wide rate of unexpectedly slow gaps between trace points without needing to open the Archive payload for any single transaction.                                                                                              |
| `jube.engine.logs.info.count` | Incremented every time Enable Logs: Info captures a trace point (which is to say that the invocation was sampled). As sampling volume is itself an important performance signal, being how much of the Info-level capture cost the traffic of a model is actually paying, the counter makes it directly observable rather than only ever inferable from the configured Sample Percentage. |

The two are independent, and a trace point captured for both reasons at once increments both counters, correctly, since it really did happen because of both.
