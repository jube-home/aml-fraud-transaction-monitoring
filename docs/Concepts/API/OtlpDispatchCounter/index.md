---
layout: default
title: OTLP Dispatch Counter
nav_order: 11
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# OTLP Dispatch Counter

Once `EnableOpenTelemetry` is on, this instance is itself sending data out over the network to whatever
`OpenTelemetryBackendEndpoint` points at (or the OTel SDK's own env-var-resolved endpoint). That export path needs the
same backpressure discipline as everything else in this suite: **a dead or slow backend must never be allowed to
buffer without bound, and every drop it causes must be observable, not silent.**

## What is guaranteed

- **No unbounded buffering.** Traces and logs are exported through the OTel .NET SDK's own bounded batch
  processors (`BatchActivityExportProcessor`/`BatchLogRecordExportProcessor`), constructed explicitly here with a
  fixed `MaxQueueSize` (2048) rather than left to whatever `AddOtlpExporter()`'s own defaults happen to be. Metrics
  have no queue to bound in the first place -- `PeriodicExportingMetricReader` takes a fresh snapshot of the current
  aggregation state on every export interval and sends that, so a failed export simply loses that one interval's
  data point rather than accumulating a backlog.
- **A dead backend fails fast, not slow.** Each individual export call is bounded by a short timeout
  (`OtlpExporterTimeoutMilliseconds`, 5 seconds), and the batch processor's own per-flush timeout is bounded too
  (10 seconds) -- long enough for one real attempt, short enough that a stuck export frees the background export
  thread to try the next batch instead of sitting on one dead connection.
- **A full queue drops the newest item, it does not block the caller.** This is the OTel SDK's own default
  behaviour for its batch processors (a bounded ring buffer, not a growing list) -- nothing here changes that
  behaviour, it only makes it observable (see below), since by default a dropped item is otherwise silent.

## What is captured

Every export attempt -- successful, failed, or (for traces/logs) refused outright because the queue was already
full -- is captured two ways, the same "live instrument + per-minute table" shape every other counter in this suite
uses:

- **Live OpenTelemetry instruments** (`jube.service.otel.export.dispatch.count`, tagged `signal`/`outcome`;
  `jube.service.otel.export.dispatch.duration`; `jube.service.otel.export.dropped.count`, tagged `signal`) --
  exported like any other counter once `EnableOpenTelemetry` is on, and locally captured regardless via
  `OpenTelemetryMetricCapture` (see [Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html#local-capture-of-jubes-own-opentelemetry-metrics)).
- **`OtlpDispatchCounter`** -- one row per distinct Signal (`traces`/`metrics`/`logs`) per one-minute window, with
  `Count`, `SuccessCount`, `FailureCount`, `ItemCount` (spans/log records/metric points attempted), `DroppedCount`
  and `Total`/`Min`/`MaxMicroseconds` for that window. Browsable via `GET /api/OtlpDispatchCounter` and the
  **OTLP Dispatch Counter** administration page, same conventions as every other counter table in this suite
  (permission-gated, not tenant-scoped, `take`/`from`/`to`/`search`/`samplePercentage`).

`DroppedCount` is captured differently from the rest: the OTel SDK's batch processors drop items internally with no
public API to observe it, so this specifically listens to the SDK's own self-diagnostics EventSource
(`"OpenTelemetry-Sdk"`, event `ExistsDroppedExportProcessorItems`) rather than being derived from anything
`DispatchTrackingExporter` itself sees -- a drop happens *inside* the queue, before an item ever reaches the actual
network call. `DroppedCount` is always 0 for the `metrics` Signal, which has no queue to overflow.

A `FailureCount` rising against a flat or falling `SuccessCount` is a dead/unreachable backend. A nonzero
`DroppedCount` means the queue filled faster than it could drain -- either the backend is too slow for the current
volume, or too many instruments are being exported at all (see [OpenTelemetry Exclude](../OpenTelemetryExclude/index.html)
to prune that).
