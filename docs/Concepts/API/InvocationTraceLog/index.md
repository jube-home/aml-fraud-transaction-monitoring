---
layout: default
title: HTTP API Invocation Trace Log
nav_order: 5
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# HTTP API Invocation Trace Log

Understanding what happened step by step during an invocation has typically meant enabling log4net INFO logging and
finding the right log file on the right node — an operational change that presures disk IO and slows response times, so
it is naturally disabled in production. Two independent switches capture the same step-by-step trace points into a
`logs` array on the invocation payload instead, so the trace can be pulled straight from the Archive or direct response
to build a report, with no backend or log file access needed.

## Enabling Log Models

The [Model Configuration](../../../Configuration/Models/Models/index.html) has two independent Logs switches -- not
mutually exclusive levels of one switch, since they answer different questions and a model can have either, both, or
neither on:

- **Enable Logs: Info** — every trace point in the invocation pipeline is captured, for a sampled subset of invocations
  (see Sampling below). Off by default, and log4net's own INFO logging (if separately enabled in the logging
  configuration) is completely unaffected either way.
- **Enable Logs: Warn Threshold** — only trace points whose gap since the previously captured entry exceeds a Warn
  Threshold in milliseconds are captured, evaluated on *every* invocation (never sampled), so a busy invocation only
  surfaces where time actually blocked. Off by default.

Turning either on never changes what log4net writes -- the two are independent sinks fed from the same call sites. The
intention is that log4net be relegated to developer tooling only.

Warn Threshold Milliseconds measures the gap since the previous captured entry, not since the invocation started --
e.g. a threshold of 250 means "only capture a trace point if at least 250ms have passed since the last one that was
captured," so a model that logs at every stage boundary will only surface the boundaries where something was actually
slow. Independent of Enable Logs: Info -- the Warn threshold check runs regardless of whether Info is also on, and a
single trace point can be captured because of Info sampling, the Warn threshold, or both at once (see Shape below for
how a captured entry records which).

## Sampling

Enable Logs: Info captures every trace point on every invocation by default, which is exactly the disk/CPU/payload-size
cost that made log4net INFO logging impractical to leave on in production in the first place, notwithstanding the bulk
asyncronous insert is substantially more efficent. Enable Sampling (visible only when Enable Logs: Info is on) lets Info
run continuously at a small fraction of that cost: turn it on and set **Sample Percentage**
(0-100, e.g. `0.02` for roughly 1 in 5000 invocations) and only a random sample of invocations actually get their trace
points captured -- the rest behave as if Enable Logs: Info were off. A sampled invocation still gets *every* trace
point, not a random subset of points within it, so a captured invocation's trace is always complete. Sampling volume is
itself a useful performance signal, reported independently of Warn breaches (see Monitoring below).

Enable Sampling and Sample Percentage only ever take effect while Enable Logs: Info is actually on -- if Enable Logs:
Info is off, every invocation is treated as sampled regardless of what Enable Sampling/Sample Percentage happen to be
set to underneath (e.g. left over from an earlier test with Enable Logs: Info on). The admin UI only hides the Enable
Sampling control when Enable Logs: Info is off; it does not clear it, so this matters for a Model that has ever had
sampling configured.

Sampling only ever throttles Info. It has no effect on Enable Logs: Warn Threshold: Warn already only captures anomalies
(gaps exceeding the threshold), so sampling it would risk missing the very breaches it exists to catch -- Warn evaluates
every invocation regardless of Enable Sampling.

The same per-invocation sample decision also gates the Response Time Pipeline, Task Performance Counter, and
`invokeTaskPerformance.stages` (Enable Response Trace's own per-stage/per-rule breakdown) capture for that Model (see
[HTTP API Performance Trace](../../PerformanceTrace/index.html)) -- all three are otherwise always-on and have no
Off/Info/Warn level of their own to exempt. `stages` being built at all is itself real per-invocation cost (a
`StageDuration`/`StageTiming` object per pipeline stage, plus a further per-rule dictionary for stages that loop
over a configured collection of rules), so gating it the same way avoids paying that cost on every invocation at
high throughput regardless of whether Enable Response Trace happens to be on for that Model -- Enable Response Trace
only ever controls whether an already-captured `stages` survives into the *direct* response; the Archive payload keeps
the full breakdown regardless, for whichever invocations were sampled. This means a sampled invocation's `logs`,
`responseTimePipeline`, `invokeTaskPerformance.taskWrapperStats` and `invokeTaskPerformance.stages` all line up
with each other, so a report built from one sampled invocation can correlate its trace against its own timing and
memory data rather than against a different, unrelated set of sampled invocations.

Whether a given invocation was selected is recorded on the payload itself as `isSampled` (a top-level boolean, alongside
`logs`), so a report never has to re-derive it from whether `logs.entries` happens to be present. `isSampled` is `true`
whenever Enable Logs: Info is off, Enable Sampling is off (nothing to throttle), or the random draw selected this
invocation, and it is unaffected by Warn mode's own independent capture.

## Shape

``` text
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

The Archive payload always carries the full `logs` array whenever either Enable Logs: Info or Enable Logs: Warn
Threshold is on, the same treatment given to the performance `stages` breakdown (see
[HTTP API Performance Trace](../../PerformanceTrace/index.html)). The direct `/api/invoke` response only includes it
when **Enable Logs In Response** is also switched on for the Model -- independently of which of the two switches is on,
so a Model can capture Warn-only anomalies into the Archive for later reporting while never adding them to the direct
response, or the reverse.

## Monitoring capture volume across transactions

Each switch reports its own capture volume as an independent OpenTelemetry counter, tagged by `model`, exported
alongside `jube.engine.stage.duration` whenever `EnableOpenTelemetry` is set
(see [Environment Variables](../../EnvironmentVariables/index.html)):

- **`jube.engine.logs.warn.count`** -- incremented every time Enable Logs: Warn Threshold is on and a trace point's gap
  since the last captured entry exceeds the threshold. Gives a cluster-wide rate of "unexpectedly slow gaps between
  trace points" without needing to open the Archive payload for any single transaction.
- **`jube.engine.logs.info.count`** -- incremented every time Enable Logs: Info captures a trace point (i.e. this
  invocation was sampled). Since sampling volume is itself an important performance signal -- how much of the
  Info-level capture cost a model's traffic is actually paying -- this makes it directly observable rather than only
  ever inferable from Sample Percentage's configured value.

The two are independent: a trace point captured for both reasons at once increments both counters, correctly, since it
really did happen because of both.
