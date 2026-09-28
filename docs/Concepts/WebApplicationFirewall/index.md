---
layout: default
title: Web Application Firewall
nav_order: 13
parent: Concepts
---

# Web Application Firewall

Jube ships a content-level Web Application Firewall (WAF): a small, table-driven layer that inspects the content of
incoming requests against a set of regular-expression signatures, records every match to a `WafAttack` table in bulk,
and — for signatures marked to do so — refuses the request. It is aimed at the OWASP Top 10 content classes (injection,
cross-site scripting, path traversal, template injection, XXE, null-byte) rather than network-level signatures. It is
**not** a substitute for the structural defences Jube already relies on — parameterised queries, the rule-language token
allow-list, output encoding, authentication and authorisation — it is a defence-in-depth and audit layer on top of them.

It is **disabled by default** (`WafEnabled` defaults to False). Because a dropping signature rejects matching requests
with `403 Forbidden`, and because a fraud/AML payload can legitimately contain text that looks like an attack (a
transaction narrative or a name containing an apostrophe, angle brackets or SQL-looking words), it should be enabled
deliberately, after the seeded signatures have been reviewed and any needed exceptions added.

## What is inspected

A single rule set — polled from the database and swapped in atomically — is evaluated against three request surfaces,
each with a scope flag so a signature can be limited to one of them:

- **Path** (`Path`, value 1): the request path.
- **Query** (`Query`, value 2): each query-string value.
- **Body** (`Body`, value 4): each string, number and boolean value of a JSON request body, addressed by a JSON path
  such as `$.customer.name`. Object keys are inspected too. A body that is not JSON, is larger than
  `WafMaxInspectBytes`, or belongs to a WebSocket upgrade or streaming request, is not buffered; its path and query are
  still inspected.

`All` (value 7) is Path + Query + Body and is the default when a signature's `TargetScope` is null or zero.

The same inspection core is transport-agnostic and is reached from two call sites, so the rules and the `WafAttack`
audit trail are shared:

1. The **HTTP middleware**, for ordinary requests — this also covers the path and query string of the Server-Sent
   Events streaming endpoints (`/api/Watcher/Stream`, `/api/ServiceChange/Stream`), since they are reached through the
   same pipeline; nothing currently inspects the events written onto an open stream once it is established.
2. Any future **streaming handler**, which could call the inspector per event or frame written onto an open stream.

## Tables

### WafSignature

The signatures. Each row is a `Name`, an OWASP `Category`, a `Description`, a `Pattern` (a .NET regular expression), a
`TargetScope` (the bitmask above), a `MatchTimeoutMilliseconds`, a `Drop` flag and an `Active` flag. A signature whose
`Drop` is set causes a match to reject the request; a signature with `Drop` cleared is **detect-only** — the match is
recorded but the request continues. Invalid patterns are logged and skipped at load, so one bad row cannot stop the WAF
loading. The migration seeds a starter set covering SQL injection (union, tautology, stacked, timing), cross-site
scripting (script tag, `javascript:` URI, inline event handler), path traversal, OS command injection, template
injection, XXE and null-byte.

### WafException

The allow-list, and the escape hatch for false positives. Each row is a `RouteRegex` (required), an optional
`FieldRegex`, an optional `WafSignatureId`, and an `Active` flag. An exception suppresses a match when its `RouteRegex`
matches the request route **and** (if a `FieldRegex` is given) it matches the matched field's name **and** (if a
`WafSignatureId` is given) it is the signature that matched. A row with no `RouteRegex` is skipped, so a blank exception
can never silently disable the WAF. This is how a free-text field on a specific route — a case narrative, a customer
name — is exempted without weakening the signature everywhere else.

### WafAttack

The evidence log. Every match, whether dropped or detect-only, is queued in memory and bulk-inserted here on the
`WafFlushInterval`/`WafFlushIntervalValue` interval. Each row records the transport, route, method, remote IP, user
(when known), the matched signature and category, the matched field's JSON path, a truncated copy of the matched value,
the action taken (`Dropped` or `Detected`), a correlation id and the instance name. It is browsable the same way
as [Application Log Entry](../API/ApplicationLogEntry/index.html) — see [Waf Attack Log](../API/WafAttack/index.html)
for the endpoint and UI page.

## Operating it

- Enable it with `WafEnabled=True` once the signatures are reviewed.
- Add, edit or disable signatures and exceptions directly in the tables; changes take effect at the next
  `WafRefreshInterval`/`WafRefreshIntervalValue` poll with no restart.
- Every regular expression — signature and exception alike — is compiled with a match timeout, capped at
  `WafMaxRegexTimeoutMilliseconds`. A match that times out is treated as a non-match and never hangs a request, which
  bounds the ReDoS risk of a table-driven pattern.
- Treat a rollout as tune-then-enforce: start with signatures as detect-only (`Drop` cleared), watch `WafAttack` for
  false positives, add `WafException` rows for the legitimate ones, then set `Drop` where you are confident.

See [Environment Variables](../EnvironmentVariables/index.html) for the `Waf*` settings.
