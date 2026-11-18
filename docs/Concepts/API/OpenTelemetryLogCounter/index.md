---
layout: default
title: OpenTelemetry Log Counter
nav_order: 9
parent: API
grand_parent: Concepts
---

# OpenTelemetry Log Counter

[Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html) already captures a good deal of unstructured log text purely for the purposes of browsing, being the `ApplicationLogEntry`, `PostgresLogEntry` and `ContainerLogEntry` messages, the `RespSlowOperation` commands, and the `EtcdClusterEvent` and `PatroniClusterEvent` transitions. None of it feeds OpenTelemetry, and it follows that noticing a recurring pattern (a specific Postgres deadlock, a container crash string) has been a matter of reading rows rather than watching a dashboard.

OpenTelemetry Log Counter closes that gap by way of rules, each being a Name (the OpenTelemetry counter to increment) and a Regex (a .NET regular expression), which are checked against every one of those sources as it is captured. A match increments a counter named after the rule, the counter being created the first time the rule is ever matched. No code change or restart is required to add, edit or disable a rule.

## Where it lives

Navigate through the menu to Administration, then Telemetry settings, which is presented to the landlord only, and select the Log counters tab. Creating, changing and deleting a rule follows the common manner described in [Child Objects](../../../Navigation/ChildObjects/index.html).

## Fields

| Value  | Description                                                                              | Example                          |
|--------|------------------------------------------------------------------------------------------|----------------------------------|
| Name   | The name of the OpenTelemetry counter to increment upon a match.                         | jube.logcounter.postgres_deadlock |
| Regex  | The .NET regular expression checked against each captured source.                        | deadlock detected                |
| Active | Whether the rule is checked.                                                             | True                             |
| Locked | Whether the rule is protected from change.                                               | False                            |

## Where it is checked

A single rule set, polled from the database roughly once a minute and swapped in atomically, is checked against the following:

| Source                                    | What the Regex is checked against                                                                                                                                                                                                                                                                                                                                             |
|-------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ApplicationLogEntry`                     | `Message`, being the log4net WARN, ERROR and FATAL capture.                                                                                                                                                                                                                                                                                                                   |
| `PostgresLogEntry`                        | `Message`, being the tailed Postgres server log lines.                                                                                                                                                                                                                                                                                                                        |
| `RespSlowOperation`                       | `Command`.                                                                                                                                                                                                                                                                                                                                                                    |
| `ContainerLogEntry`                       | `Message`, checked independently by the separate `Jube.Monitoring` sidecar process, which is deliberately isolated from `Jube.App` and `Jube.Engine` (see [Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html#docker-monitoring-sidecar-jubemonitoring)) and so polls its own copy of the same rule table directly, once per sampling cycle.         |
| `EtcdClusterEvent`, `PatroniClusterEvent` | These have no single free-text field, henceforth the Regex is checked against a synthesised `"<EventType> <PreviousValue> -> <NewValue>"`, to which Patroni additionally adds `Reason`.                                                                                                                                                                                      |

A rule that is never matched never creates its counter, and it follows that there is no cost beyond the regular expression check itself for a rule that turns out not to be useful.

## Reducing noise

Every counter created in this manner, along with every other named OpenTelemetry instrument already in the codebase, can be turned off from export without a redeploy via [OpenTelemetry Exclude](../OpenTelemetryExclude/index.html), where it is described how the platform avoids being overwhelmed by an ever-growing set of counters.

## Sending the data somewhere

The counters are ordinary instruments on the `Jube.Engine` OpenTelemetry Meter or, for `ContainerLogEntry` matches, on the Meter of `Jube.Monitoring` itself, which is to say that they are exported exactly as every other counter in the system once `EnableOpenTelemetry` is on. See [Environment Variables](../../EnvironmentVariables/index.html) for `EnableOpenTelemetry` and `OpenTelemetryBackendEndpoint`, and the `Jube.OpenTelemetryListener` project for a disposable local receiver with which to confirm that data is actually arriving during testing.
