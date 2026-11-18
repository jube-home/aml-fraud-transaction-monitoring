---
layout: default
title: OpenTelemetry Exclude
nav_order: 10
parent: API
grand_parent: Concepts
---

# OpenTelemetry Exclude

The set of OpenTelemetry instruments in the platform only grows over time. The [OpenTelemetry Log Counter](../OpenTelemetryLogCounter/index.html) alone can create an unbounded number of new counters, on top of every fixed instrument already in the codebase (`jube.engine.*`, `jube.service.*`, `jube.cache.*`, `jube.data.*`, and the built-in ASP.NET, runtime and process meters), and left unchecked it is in exactly this manner that a collector or backend comes to be overwhelmed. OpenTelemetry Exclude is the release valve, being a list of instrument names, matched in full without regard to case and with no wildcards, that should stop being exported, without a redeploy.

## Where it lives

Navigate through the menu to Administration, then Telemetry settings, which is presented to the landlord only, and select the Exclusions tab. Creating, changing and deleting an exclusion follows the common manner described in [Child Objects](../../../Navigation/ChildObjects/index.html). An exclusion is a Name, being the exact name of the instrument to stop exporting (e.g. jube.engine.logs.warn.count), together with Active, which must be on for the exclusion to be enforced, and Locked.

## What it affects, and how live it is

The exclude list is enforced in two ways, which differ in how soon a change takes effect:

| Instruments                                            | Enforcement                                                                                                                                                                              |
|--------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| The dynamic counters of the OpenTelemetry Log Counter  | Fully live. Before the counter of a rule is even created, the current exclude list is consulted, and an excluded rule name never starts incrementing.                                    |
| Every other, pre-existing named counter                | Enforced via an OpenTelemetry `View` that drops the metric stream, resolved by the .NET OpenTelemetry SDK the first time it observes each instrument.                                    |

It follows that for the pre-existing counters an exclusion is live for an instrument not yet actively exporting, whereas an instrument already exporting before being added to the exclude list keeps exporting until the process restarts, as the SDK does not re-evaluate the configuration of a stream on every export cycle, only when the instrument first becomes active. This is to be treated in the same manner as the other best-effort trade-offs of the telemetry suite, none of which is a guarantee of durability (for example the cursor of a sampler being reset on restart), which is to say that a counter known to be noisy should be excluded as early as possible, and a full stop for one already exporting may need a restart.

## Where it is not needed

`Jube.Monitoring`, the Docker-socket sidecar, has no fixed named counters of its own beyond the dynamic ones that the OpenTelemetry Log Counter creates locally there. Those are already gated by the same exclude list, which the sidecar polls independently, henceforth no separate exclusion mechanism exists for that process.

## Reducing volume is one half of the problem

Pruning counters here reduces how much is exported. It says nothing about whether the export itself is keeping up with a real backend, for which see [OTLP Dispatch Counter](../OtlpDispatchCounter/index.html), where it is described how a dead or slow backend is bounded (never buffered without limit) and made observable (dispatch success and failure, response times, and drops owing to a full queue).
