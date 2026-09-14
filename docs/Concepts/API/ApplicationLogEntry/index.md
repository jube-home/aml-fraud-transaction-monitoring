---
layout: default
title: Application Log Entry
nav_order: 6
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Application Log Entry

[HTTP API Invocation Trace Log](../InvocationTraceLog/index.html) captures per-invocation trace points for a single
Model. Application Log Entry is the application-wide equivalent for problems: it captures every WARN, ERROR and FATAL
event logged anywhere in the running process -- not just the invoke pipeline -- into a database table, so a real problem
with a transaction (or anything else in the process) shows up in a query without anyone needing to open a log file on a
node.

There is nothing to configure and no per-Model switch: this is always on. It attaches a dedicated log4net appender
directly to the root logger when the process starts, alongside whatever appenders the deployment's own `log4net.config`
(or the `Log4NetLogPath`/`Log4NetLogLevel` environment-variable fallback) already defines, without changing any of
them -- it is a second, independent destination for events that were already going to be logged, not a replacement for
the existing log file or console output.

## What is captured

Only events at WARN level or above are captured (the appender's own threshold), regardless of what level the
deployment's own logging configuration is set to -- if the configured level is more restrictive than WARN (e.g. ERROR
only), events below that level were never logged in the first place and so were never available to capture either.

Each captured event becomes one row in **`ApplicationLogEntry`**:

- **`OccurredDate`** -- UTC timestamp the event was logged.
- **`Level`** -- `WARN`, `ERROR` or `FATAL`.
- **`LoggerName`** -- the fully-qualified class that logged it (log4net's logger name, almost always the emitting type).
- **`ThreadContext`** -- the thread that logged it.
- **`Message`** -- the rendered log message.
- **`Exception`** -- the full exception text, if the event carried one; otherwise null.
- **`CreatedDate`** / **`Instance`** -- when this row was flushed, and which node flushed it.

`LoggerName` and `ThreadContext` are carried as their own columns rather than left folded into `Message` deliberately:
by the time a message reaches here it has almost always already been interpolated with per-invocation data (a GUID, a
Model id), so grouping by the message text itself rarely finds the recurring problem. Grouping by `LoggerName` instead
gives a stable, class-level view of where problems are actually recurring, independent of what varied between
occurrences:

``` sql
select "LoggerName", "Level", count(*) as occurrences, max("OccurredDate") as last_seen
from "ApplicationLogEntry"
where "OccurredDate" > now() - interval '1 day'
group by "LoggerName", "Level"
order by occurrences desc
```

## How it is written

Captured events are held in a bounded in-memory queue and flushed to the database as a single bulk insert roughly once a
minute, on a dedicated background thread independent of any Model's own per-minute counter flush -- so this keeps
running even in a deployment with no active Models. The queue is capped (20,000 pending entries) so that a burst of
warnings during an incident, or a flush cycle that is temporarily unable to reach the database, can never turn into
unbounded memory growth on the node; entries enqueued past that bound are dropped rather than blocking the caller that
logged them, since observability capture must never be allowed to slow down or fail the work it is observing.

## Browsing it without a database connection

Up to the last 100,000 rows are browsable directly: `GET /api/ApplicationLogEntry` (requires the **View Counter and
Balance** permission), or via the **Performance > Application Log Entry** page in the UI. Unlike the per-Model counter
viewers alongside it, this is not scoped to a tenant or Model -- any caller with the permission sees every captured row,
since the underlying data is not tenant-scoped either.

The same endpoint accepts `from`/`to` (a UTC date/time range against `OccurredDate`, inclusive) and `search` (a
case-insensitive substring match against `Message`, `LoggerName` or `Exception`), both pushed down to the database
rather than filtered after the fact -- useful once a deployment has been running long enough that the last 100,000 rows
no longer cover the incident you're looking for. `from` and `to` each default independently to the last 24 hours when
omitted, so a plain call without either returns the last day's entries rather than the whole table. The UI page exposes
the same three filters from a toolbar above the grid, pre-populated with that same last-day range.

In the endpoint, it also accepts `samplePercentage` (0-100, clamped), applied server-side as an additional filter
alongside `from`/` to `/`search` rather than after fetching -- each already-matching row is included independently with
that probability, so `samplePercentage=10` returns roughly a random one-tenth of matching rows rather than the newest tenth. This is not
exposed in the UI toolbar; it exists for an agent to draw an unbiased random baseline sample of logged problems early on
and compare it against recent activity later, which the endpoint's normal most-recent-first ordering can't provide by
itself.