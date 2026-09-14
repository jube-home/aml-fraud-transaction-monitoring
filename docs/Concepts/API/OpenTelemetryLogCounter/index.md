---
layout: default
title: OpenTelemetry Log Counter
nav_order: 9
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# OpenTelemetry Log Counter

[Infrastructure Health Metrics](../InfrastructureHealthMetrics/index.html) already captures a lot of unstructured log
text purely for browsing — `ApplicationLogEntry`, `PostgresLogEntry` and `ContainerLogEntry` messages, `RedisSlowOperation`
commands, `EtcdClusterEvent`/`PatroniClusterEvent` transitions. None of it feeds OpenTelemetry: noticing a recurring
pattern (a specific Postgres deadlock, a container crash string) has meant reading rows, not watching a dashboard.

OpenTelemetry Log Counter closes that gap. The `OpenTelemetryLogCounter` administration page defines rules — **Name**
(the OpenTelemetry counter to increment) and **Regex** (a .NET regular expression) — checked against every one of those
sources as it is captured. A match increments a counter named after the rule, created the first time it is ever
matched. No code change or restart is required to add, edit or disable a rule.

## Where it is checked

A single rule set, polled from the database roughly once a minute and swapped in atomically, is checked against:

- `ApplicationLogEntry.Message` (log4net WARN/ERROR/FATAL capture)
- `PostgresLogEntry.Message` (tailed Postgres server log lines)
- `RedisSlowOperation.Command`
- `ContainerLogEntry.Message` — checked independently by the separate `Jube.Monitoring` sidecar process, which is
  deliberately isolated from `Jube.App`/`Jube.Engine` (see [Infrastructure Health
  Metrics](../InfrastructureHealthMetrics/index.html#docker-monitoring-sidecar-jubemonitoring)) and so polls its own
  copy of the same rule table directly, once per sampling cycle
- `EtcdClusterEvent`/`PatroniClusterEvent` — these have no single free-text field, so the regex is checked against a
  synthesized `"<EventType> <PreviousValue> -> <NewValue>"` (Patroni additionally includes `Reason`)

A rule that is never matched never creates its counter — there is no cost beyond the regex check itself for a rule
that turns out not to be useful.

## Reducing noise

Every counter this feature creates, along with every other named OpenTelemetry instrument already in the codebase,
can be turned off from export without a redeploy via [OpenTelemetry Exclude](../OpenTelemetryExclude/index.html) —
see that page for how the platform avoids being overwhelmed by an ever-growing set of counters.

## Sending the data somewhere

These counters are ordinary instruments on the `Jube.Engine` OpenTelemetry Meter (or, for `ContainerLogEntry` matches,
`Jube.Monitoring`'s own Meter) — they export exactly like every other counter in the system once `EnableOpenTelemetry`
is on. See [Environment Variables](../../EnvironmentVariables/index.html) for `EnableOpenTelemetry`/
`OpenTelemetryBackendEndpoint`, and `Jube.OpenTelemetryListener` for a disposable local receiver to confirm data is
actually arriving during testing.
