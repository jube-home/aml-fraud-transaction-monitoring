---
layout: default
title: OpenTelemetry Exclude
nav_order: 10
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# OpenTelemetry Exclude

The platform's set of OpenTelemetry instruments only grows over time — [OpenTelemetry Log
Counter](../OpenTelemetryLogCounter/index.html) alone can create an unbounded number of new counters, on top of every
fixed instrument already in the codebase (`jube.engine.*`, `jube.service.*`, `jube.cache.*`, `jube.data.*`, the
built-in ASP.NET/runtime/process meters, ...). Left unchecked, that is exactly how a collector/backend ends up
overwhelmed. OpenTelemetry Exclude is the release valve: an administration page listing instrument names — exact
match only, no wildcards — that should simply stop being exported, without a redeploy.

## What it affects, and how live it is

- **OpenTelemetry Log Counter's own dynamic counters** — fully live. Before a rule's counter is even created, the
  current exclude list is consulted; an excluded rule name never starts incrementing.
- **Every other, pre-existing named counter** — enforced via an OpenTelemetry `View` that drops the metric stream,
  resolved by the .NET OpenTelemetry SDK the first time it observes each instrument. This is live for an instrument
  not yet actively exporting, but **an instrument already exporting before being added to the exclude list keeps
  exporting until the process restarts** — the SDK does not re-evaluate a stream's configuration on every export
  cycle, only when the instrument first becomes active. Treat this the same as this suite's other best-effort,
  not-a-durability-guarantee tradeoffs (e.g. a sampler's cursor reset on restart): a genuinely noisy counter should
  be excluded as early as possible, and a full stop for one already mid-flight may need a restart.

## Where it is not needed

`Jube.Monitoring` (the Docker-socket sidecar) has no fixed named counters of its own beyond the dynamic ones
OpenTelemetry Log Counter creates locally there — those are already gated by the same exclude list it polls
independently, so no separate exclusion mechanism exists for that process.

## Reducing volume is one half of the problem

Pruning counters here reduces how much gets exported. It says nothing about whether the export itself is keeping up
with a real backend — see [OTLP Dispatch Counter](../OtlpDispatchCounter/index.html) for how a dead or slow backend
is bounded (never buffered without limit) and made observable (dispatch success/failure, response times, and drops
due to a full queue).
