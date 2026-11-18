---
layout: default
title: OTLP Dispatch Counter
nav_order: 11
parent: API
grand_parent: Concepts
---

# OTLP Dispatch Counter

Once `EnableOpenTelemetry` is on, the instance is itself sending data out over the network to wherever `OpenTelemetryBackendEndpoint` points (or to the endpoint that the OpenTelemetry SDK resolves from its own environment variables). That export path needs the same backpressure discipline as everything else in the telemetry suite, which is to say that a dead or slow backend must never be allowed to buffer without bound, and that every drop it causes must be observable rather than silent.

## What is guaranteed

There is no unbounded buffering. Traces and logs are exported through the bounded batch processors of the OpenTelemetry .NET SDK (`BatchActivityExportProcessor` and `BatchLogRecordExportProcessor`), which are constructed explicitly with a fixed `MaxQueueSize` of 2048 rather than left to whatever the defaults of `AddOtlpExporter()` happen to be. Metrics have no queue to bound in the first place, as the `PeriodicExportingMetricReader` takes a fresh snapshot of the current aggregation state on every export interval and sends that, henceforth a failed export loses the data point of that one interval rather than accumulating a backlog.

A dead backend fails fast, not slow. Each individual export call is bounded by a short timeout (`OtlpExporterTimeoutMilliseconds`, being 5 seconds), and the per-flush timeout of the batch processor is bounded too (10 seconds), which is long enough for one real attempt, yet short enough that a stuck export frees the background export thread to try the next batch instead of sitting on one dead connection.

A full queue drops the newest item, and it does not block the caller. This is the default behaviour of the SDK for its batch processors (a bounded ring buffer, not a growing list), and nothing changes that behaviour; it is only made observable, as set out below, keeping in mind that by default a dropped item is otherwise silent.

## What is captured

Every export attempt, be it successful, failed or (for traces and logs) refused outright because the queue was already full, is captured in two ways, in the manner of a live instrument together with a per-minute table that every other counter in the suite follows.

The live OpenTelemetry instruments are as follows, exported as any other counter once `EnableOpenTelemetry` is on, and captured locally regardless via `OpenTelemetryMetricCapture` (see [Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html#local-capture-of-jubes-own-opentelemetry-metrics)):

| Instrument                                   | Tags                |
|----------------------------------------------|---------------------|
| `jube.service.otel.export.dispatch.count`    | `signal`, `outcome` |
| `jube.service.otel.export.dispatch.duration` |                     |
| `jube.service.otel.export.dropped.count`     | `signal`            |

The `OtlpDispatchCounter` table meanwhile holds one row per distinct signal per one-minute window, with the following columns:

| Column                                                      | Description                                                                               |
|-------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| SignalId                                                    | The signal: 1 for traces, 2 for metrics and 3 for logs.                                   |
| Count                                                       | The number of export attempts in the window.                                              |
| SuccessCount                                                | The number of those attempts that succeeded.                                              |
| FailureCount                                                | The number of those attempts that failed.                                                 |
| ItemCount                                                   | The spans, log records or metric points attempted.                                        |
| DroppedCount                                                | The items dropped owing to a full queue.                                                  |
| TotalMicroseconds, MinMicroseconds, MaxMicroseconds         | The total, the least and the greatest duration of the export attempts in the window.      |
| CreatedDate                                                 | When the row was written.                                                                 |
| Instance                                                    | The node that wrote the row.                                                              |

The table is browsable via `GET /api/OtlpDispatchCounter`, or in the user interface by navigating through the menu to Administration, then Performance, where OTLP dispatch counter is presented in the Telemetry family. The conventions are those of every other counter table in the suite, in that it is restricted to the landlord, is not tenant-scoped, and the endpoint accepts `take`, `from`, `to` and `samplePercentage`, with `signalId` to restrict the rows to one signal. Where omitted, `from` defaults to one hour before the present.

`DroppedCount` is captured differently from the rest. The batch processors of the SDK drop items internally with no public API by which to observe it, henceforth the capture listens to the self-diagnostics EventSource of the SDK (`"OpenTelemetry-Sdk"`, event `ExistsDroppedExportProcessorItems`) rather than being derived from anything `DispatchTrackingExporter` itself sees, as a drop happens inside the queue, before an item ever reaches the actual network call. `DroppedCount` is always 0 for the metrics signal, which has no queue to overflow.

A `FailureCount` rising against a flat or falling `SuccessCount` is a dead or unreachable backend. A nonzero `DroppedCount` means that the queue filled faster than it could drain, which implies either that the backend is too slow for the current volume, or that too many instruments are being exported at all (see [OpenTelemetry Exclude](../OpenTelemetryExclude/index.html) to prune them).
