---
layout: default
title: Waf Attack Log
nav_order: 13
parent: API
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Waf Attack Log

The mechanism of the [Web Application Firewall](../../WebApplicationFirewall/index.html) — its three tables, the
transport-agnostic inspection core, and the HTTP middleware that calls it — is described on that page. This page
describes the **evidence log** it produces: `WafAttack`, and how to browse it. It is the same shape as
[Application Log Entry](../ApplicationLogEntry/index.html), and shares its permission.

## What is captured

Every signature match — whether the request was dropped or only detected — becomes one row in **`WafAttack`**:

- **`CreatedDate`** -- UTC timestamp the match was captured.
- **`Transport`** -- `Http` or `WebSocket`.
- **`Route`** -- the request route the match occurred on.
- **`Method`** -- the HTTP method.
- **`RemoteIp`** / **`UserName`** -- the caller, where known.
- **`SignatureName`** / **`Category`** -- which `WafSignature` matched, and its OWASP category.
- **`MatchedField`** -- the JSON path or field name that carried the matched value (e.g. `$.customer.name`).
- **`MatchedValue`** -- a truncated copy of the value that matched the signature's pattern.
- **`Action`** -- `Dropped` (the request was refused) or `Detected` (the request continued; the signature's `Drop`
  flag was clear).
- **`CorrelationId`** -- the request's trace identifier, for cross-referencing against
  [Application Log Entry](../ApplicationLogEntry/index.html) or
  the [HTTP API Invocation Trace Log](../InvocationTraceLog/index.html) if the same request also logged elsewhere.
- **`Instance`** -- which node captured it.

Nothing is captured while the WAF is disabled (`WafEnabled=False`, the default): the middleware passes every request
straight through without invoking the inspector at all, so there is nothing for this table to hold until it is
switched on.

## How it is written

Matches are held in a bounded in-memory queue and flushed to the database as a single bulk insert on the
`WafFlushInterval`/`WafFlushIntervalValue` interval (default 10 seconds), the same pattern as Application Log Entry's
own capture queue. The queue is capped at 20,000 pending entries, so a burst of matches during an attack, or a flush
cycle temporarily unable to reach the database, drops the oldest overflow rather than growing memory without bound or
slowing down the request that triggered the capture.

## Browsing it

`GET /api/WafAttack` (landlord tenant only, any other caller receives 403), or via **Administration > Performance >
Logs > Waf Attack Log** in the UI, right alongside Application Log Entry.

The endpoint accepts the same filters, working the same way: `from`/`to` (a UTC date/time range against `CreatedDate`,
inclusive, each defaulting independently to the last hour when omitted) and `search` (a case-insensitive substring
match, here against `MatchedValue`, `Route`, `SignatureName`, `Category`, `MatchedField`, `UserName` or `RemoteIp`),
both pushed down to the database. The UI page exposes the same filters from a toolbar above the grid, plus CSV export.

It also accepts `samplePercentage` (0-100, clamped) for the same reason as Application Log Entry's -- drawing an
unbiased random baseline sample of matches, independent of the normal most-recent-first ordering. Not exposed in the UI
toolbar.

Response statistics are always empty: every column here is a string, a timestamp or an identifier, so there are no
continuous measured columns to summarise.
