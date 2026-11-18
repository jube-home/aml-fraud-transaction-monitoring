---
layout: default
title: Web Application Firewall
nav_order: 13
parent: Concepts
---

# Web Application Firewall

Jube ships a content-level Web Application Firewall (WAF), being a small, table-driven layer that inspects the content of incoming requests against a set of regular expression signatures, records every match to a `WafAttack` table in bulk and, for signatures marked to do so, refuses the request. It is aimed at the OWASP Top 10 content classes (injection, cross-site scripting, path traversal, template injection, XXE, null-byte) rather than network-level signatures. It is not a substitute for the structural defences upon which Jube already relies, which is to say parameterised queries, the rule-language token allow-list, output encoding, authentication and authorisation, and is instead a defence-in-depth and audit layer on top of them.

It is enabled by default, the `WafEnabled` Environment Variable being True unless set otherwise. Keeping in mind that a dropping signature rejects matching requests with `403 Forbidden`, and that a fraud or AML payload can legitimately contain text that looks like an attack (a transaction narrative or a name containing an apostrophe, angle brackets or SQL-looking words), the seeded signatures should be reviewed, and any needed exceptions added, as part of a deployment.

{% comment %}TODO(verify): this page formerly stated that the WAF is disabled by default (WafEnabled defaulting to False) and should be enabled deliberately. Jube.DynamicEnvironment/DynamicEnvironment.cs and the Environment Variables page both give the default as True. Owner to confirm that enabled by default is intended, given the caution about legitimate payloads being refused.{% endcomment %}

## What is inspected

A single rule set, polled from the database and swapped in atomically, is evaluated against three request surfaces, each having a scope flag such that a signature can be limited to one of them:

| Scope | Value | What is inspected                                                                                                                                                                                                                                                                                                              |
|-------|-------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Path  | 1     | The request path.                                                                                                                                                                                                                                                                                                              |
| Query | 2     | Each query-string value.                                                                                                                                                                                                                                                                                                       |
| Body  | 4     | Each string, number and boolean value of a JSON request body, addressed by a JSON path such as `$.customer.name`. Object keys are inspected too. A body that is not JSON, is larger than `WafMaxInspectBytes`, or belongs to a WebSocket upgrade or streaming request, is not buffered, although its path and query are still inspected. |
| All   | 7     | Path, Query and Body together, which is the default when the `TargetScope` of a signature is null or zero.                                                                                                                                                                                                                    |

The inspection core is transport-agnostic and is intended to be reached from two call sites, such that the rules and the `WafAttack` audit trail are shared:

* The HTTP middleware, for ordinary requests. This also covers the path and query string of the Server-Sent Events streaming endpoints (`/api/Watcher/Stream`, `/api/ServiceChange/Stream`), since they are reached through the same pipeline, although nothing currently inspects the events written onto an open stream once it is established.
* Any future streaming handler, which could call the inspector for each event or frame written onto an open stream.

## Tables

### WafSignature

The signatures. Each row is a `Name`, an OWASP `Category`, a `Description`, a `Pattern` (a .NET regular expression), a `TargetScope` (the bitmask above), a `MatchTimeoutMilliseconds`, a `Drop` flag and an `Active` flag. A signature whose `Drop` is set causes a match to reject the request, whereas a signature with `Drop` cleared is detect-only, which is to say that the match is recorded but the request continues. Invalid patterns are logged and skipped at load, henceforth one bad row cannot stop the WAF loading. The migration seeds a starter set covering SQL injection (union, tautology, stacked, timing), cross-site scripting (script tag, `javascript:` URI, inline event handler), path traversal, OS command injection, template injection, XXE and null-byte.

### WafException

The allow-list, and the escape hatch for false positives. Each row is a `RouteRegex` (required), an optional `FieldRegex`, an optional `WafSignatureId`, and an `Active` flag. An exception suppresses a match when its `RouteRegex` matches the request route and, if a `FieldRegex` is given, it matches the name of the matched field and, if a `WafSignatureId` is given, it is the signature that matched. A row with no `RouteRegex` is skipped, so a blank exception can never silently disable the WAF. It is in this manner that a free-text field on a specific route, a case narrative or a customer name for example, is exempted without weakening the signature everywhere else.

### WafAttack

The evidence log. Every match, whether dropped or detect-only, is queued in memory and bulk-inserted here on the interval given by `WafFlushInterval` and `WafFlushIntervalValue`. Each row records the transport, route, method, remote IP, user (when known), the matched signature and category, the JSON path of the matched field, a truncated copy of the matched value, the action taken (`Dropped` or `Detected`), a correlation id and the instance name. It is browsable in the same manner as [Application Log Entry](../API/ApplicationLogEntry/index.html), and the endpoint and its place in the user interface are described in [Waf Attack Log](../API/WafAttack/index.html).

## Operating it

The WAF is switched off with `WafEnabled=False`, and on again with `WafEnabled=True`.

Signatures and exceptions are added, edited or disabled directly in the tables, there being no page for them in the user interface, and changes take effect at the next poll, on the interval given by `WafRefreshInterval` and `WafRefreshIntervalValue`, with no restart.

Every regular expression, signature and exception alike, is compiled with a match timeout, capped at `WafMaxRegexTimeoutMilliseconds`. A match that times out is treated as a non-match and never hangs a request, which bounds the ReDoS risk of a table-driven pattern.

A rollout is best treated as tune then enforce, which is to say starting with signatures as detect-only (`Drop` cleared), watching `WafAttack` for false positives, adding `WafException` rows for the legitimate ones, and thereafter setting `Drop` where there is confidence in the signature.

See [Environment Variables](../EnvironmentVariables/index.html) for the `Waf*` settings.
