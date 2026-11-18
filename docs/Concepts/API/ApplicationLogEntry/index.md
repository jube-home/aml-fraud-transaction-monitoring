---
layout: default
title: Application Log Entry
nav_order: 6
parent: API
grand_parent: Concepts
---

# Application Log Entry

Whereas the [HTTP API Invocation Trace Log](../InvocationTraceLog/index.html) captures per-invocation trace points for a single Model, Application Log Entry is the application-wide equivalent for problems, which is to say that it captures every WARN, ERROR and FATAL event logged anywhere in the running process, not only in the invoke pipeline, into a database table. It follows that a real problem with a transaction, or anything else in the process, is available by query without anyone needing to open a log file on a node.

There is nothing to configure and no per-Model switch, as the capture is always on. A dedicated log4net appender is attached directly to the root logger when the process starts, alongside whatever appenders the deployment's own `log4net.config` (or the `Log4NetLogPath` and `Log4NetLogLevel` Environment Variable fallback) already defines, and without changing any of them. It is henceforth a second, independent destination for events that were already going to be logged, and not a replacement for the existing log file or console output.

## What is captured

Only events at WARN level or above are captured, that being the threshold of the appender itself, regardless of the level at which the deployment's own logging configuration is set. Keeping in mind that where the configured level is more restrictive than WARN (e.g. ERROR only), events below that level were never logged in the first place, it follows that they were never available for capture either.

Each captured event becomes one row in the `ApplicationLogEntry` table, having the following columns:

| Column        | Description                                                                                                     |
|---------------|-----------------------------------------------------------------------------------------------------------------|
| OccurredDate  | The UTC timestamp at which the event was logged.                                                                |
| Level         | `WARN`, `ERROR` or `FATAL`.                                                                                     |
| LoggerName    | The fully-qualified class that logged the event, being the log4net logger name and almost always the emitting type. |
| ThreadContext | The thread that logged the event.                                                                               |
| Message       | The rendered log message.                                                                                       |
| Exception     | The full exception text, if the event carried one, otherwise null.                                              |
| CreatedDate   | When the row was flushed to the database.                                                                       |
| Instance      | The node that flushed the row.                                                                                  |

`LoggerName` and `ThreadContext` are carried as their own columns, rather than left folded into `Message`, deliberately. By the time a message is captured it has almost always already been interpolated with per-invocation data (a GUID, a Model id), henceforth grouping by the message text itself rarely finds the recurring problem. Grouping by `LoggerName` instead gives a stable, class-level view of where problems are actually recurring, independent of what varied between occurrences:

```sql
select "LoggerName", "Level", count(*) as occurrences, max("OccurredDate") as last_seen
from "ApplicationLogEntry"
where "OccurredDate" > now() - interval '1 day'
group by "LoggerName", "Level"
order by occurrences desc
```

## How it is written

Captured events are held in a bounded in-memory queue and flushed to the database as a single bulk insert roughly once a minute, on a dedicated background thread which is independent of any Model's own per-minute counter flush, and which therefore continues to run in a deployment having no active Models. The queue is capped at 20,000 pending entries, such that a burst of warnings during an incident, or a flush cycle that is temporarily unable to reach the database, can never turn into unbounded memory growth on the node. Entries enqueued past that bound are dropped rather than blocking the caller that logged them, on the principle that the capture of observability must never be allowed to slow down or fail the work it is observing.

## Browsing it without a database connection

Up to the last 100,000 rows are browsable directly via `GET /api/ApplicationLogEntry`, or in the user interface by navigating through the menu to Administration, then Performance, where Application log is presented in the Logs family. Both are restricted to the landlord tenant, with any other caller of the endpoint receiving 403. Unlike the per-Model counter viewers alongside it, the log is not scoped to a tenant or Model, and the landlord sees every captured row, since the underlying data is not tenant-scoped either.

The endpoint accepts the following parameters in the querystring, each of which is pushed down to the database rather than applied after the fetch:

| Parameter        | Description                                                                                                                                         |
|------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| take             | The maximum number of rows to return, most recent first, clamped to 100,000, which is also the default.                                             |
| from             | The start of a UTC date and time range against `OccurredDate`, inclusive. Where omitted it defaults to one hour before the present.                 |
| to               | The end of that range, inclusive. Where omitted it defaults to the present.                                                                         |
| search           | A case-insensitive substring match against `Message`, `LoggerName` or `Exception`.                                                                  |
| samplePercentage | From 0 to 100, clamped. Each row already matching the other parameters is included independently with that probability.                             |

As `from` and `to` each default independently, a plain call without either returns the entries of the last hour rather than the whole table, and the range is useful once a deployment has been running long enough that the last 100,000 rows no longer cover the incident being looked for.

{% comment %}TODO(verify): this page formerly stated that from and to default to the last 24 hours; Jube.Service/ApplicationLogEntry/ApplicationLogEntryService.cs defaults from to one hour before the present. Confirm on a running system the range with which the Application log page in the user interface is opened, and the names of its filters.{% endcomment %}

It follows from `samplePercentage` being applied to each row independently that `samplePercentage=10` returns roughly a random one-tenth of the matching rows rather than the newest tenth. It exists for the purposes of an agent drawing an unbiased random baseline sample of logged problems early on, to be compared against recent activity later, which the normal most-recent-first ordering of the endpoint cannot provide by itself.

For a related log of a different kind of event, a [Web Application Firewall](../../WebApplicationFirewall/index.html) signature match rather than a WARN, ERROR or FATAL, see [Waf Attack Log](../WafAttack/index.html), which is of the same shape and is presented in the same place in the user interface.
