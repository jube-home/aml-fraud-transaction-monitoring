---
layout: default
title: Waf Attack Log
nav_order: 13
parent: API
grand_parent: Concepts
---

# Waf Attack Log

The mechanism of the [Web Application Firewall](../../WebApplicationFirewall/index.html), which is to say its three tables, the transport-agnostic inspection core and the HTTP middleware that calls it, is described on that page. This page describes the evidence log that it produces, being the `WafAttack` table, and how it is browsed. It is of the same shape as [Application Log Entry](../ApplicationLogEntry/index.html), and is likewise restricted to the landlord.

## What is captured

Every signature match, whether the request was dropped or only detected, becomes one row in the `WafAttack` table, having the following columns:

| Column        | Description                                                                                                                                                                                                                                                       |
|---------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CreatedDate   | The UTC timestamp at which the match was captured.                                                                                                                                                                                                                |
| Transport     | `Http` or `WebSocket`.                                                                                                                                                                                                                                            |
| Route         | The request route upon which the match occurred.                                                                                                                                                                                                                  |
| Method        | The HTTP method.                                                                                                                                                                                                                                                  |
| RemoteIp      | The address of the caller, where known.                                                                                                                                                                                                                           |
| UserName      | The user making the request, where known.                                                                                                                                                                                                                         |
| SignatureName | The name of the `WafSignature` that matched.                                                                                                                                                                                                                      |
| Category      | The OWASP category of that signature.                                                                                                                                                                                                                             |
| MatchedField  | The JSON path or field name that carried the matched value (e.g. `$.customer.name`).                                                                                                                                                                              |
| MatchedValue  | A truncated copy of the value that matched the pattern of the signature.                                                                                                                                                                                          |
| Action        | `Dropped`, where the request was refused, or `Detected`, where the request continued, the `Drop` flag of the signature being clear.                                                                                                                               |
| CorrelationId | The trace identifier of the request, for the purposes of cross-referencing against [Application Log Entry](../ApplicationLogEntry/index.html) or the [HTTP API Invocation Trace Log](../InvocationTraceLog/index.html) should the same request also have logged elsewhere. |
| Instance      | The node that captured the match.                                                                                                                                                                                                                                 |

Nothing is captured while the WAF is disabled (`WafEnabled=False`; it is True by default), as the middleware then passes every request straight through without invoking the inspector at all, and there is henceforth nothing for the table to hold until it is switched on.

## How it is written

Matches are held in a bounded in-memory queue and flushed to the database as a single bulk insert on the interval given by `WafFlushInterval` and `WafFlushIntervalValue` (10 seconds by default), in the same pattern as the capture queue of Application Log Entry. The queue is capped at 20,000 pending entries, such that a burst of matches during an attack, or a flush cycle temporarily unable to reach the database, causes the overflow to be dropped rather than growing memory without bound or slowing down the request that triggered the capture. It is the newest match, arriving upon a full queue, that is dropped.

It is the newest matches that are dropped: once 20,000 entries are pending, a further match is not queued, and those already held are kept until they are flushed.

## Browsing it

The log is browsable via `GET /api/WafAttack`, or in the user interface by navigating through the menu to Administration, then Performance, where WAF attack log is presented in the Logs family, alongside Application log. Both are restricted to the landlord tenant, with any other caller of the endpoint receiving 403.

The endpoint accepts the same parameters as that of Application Log Entry, working in the same manner and pushed down to the database:

| Parameter        | Description                                                                                                                                                  |
|------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| take             | The maximum number of rows to return, most recent first, clamped to 100,000, which is also the default.                                                      |
| from             | The start of a UTC date and time range against `CreatedDate`, inclusive. Where omitted it defaults to one hour before the present.                           |
| to               | The end of that range, inclusive. Where omitted it defaults to the present.                                                                                  |
| search           | A case-insensitive substring match against `MatchedValue`, `Route`, `SignatureName`, `Category`, `MatchedField`, `UserName` or `RemoteIp`.                   |
| samplePercentage | From 0 to 100, clamped, for the purposes of drawing an unbiased random baseline sample of matches, independent of the normal most-recent-first ordering.     |

The response statistics are always empty, as every column is a string, a timestamp or an identifier, and there are henceforth no continuous measured columns to summarise.

{% comment %}TODO(verify): confirm on a running system the filters and the export offered by the WAF attack log page in the user interface; this page formerly stated a toolbar above the grid exposing the from, to and search filters, plus CSV export, with samplePercentage not exposed{% endcomment %}
