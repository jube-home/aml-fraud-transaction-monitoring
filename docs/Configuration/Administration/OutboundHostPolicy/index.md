---
layout: default
title: Outbound Host Policy
nav_order: 5
parent: Administration
grand_parent: Configuration
---

# Outbound Host Policy

Several features make the server call an address that an administrator typed in: the Case Workflow Release Hook, the web hook of a Response Elevation Range and the SQL mode of Case Workflow dispatch (HTTP targets). Left unchecked, a person who can edit one of those could point the server at something only the server can reach, such as the cloud metadata service at `169.254.169.254`, a database or an internal admin console (server side request forgery). The outbound host policy stops that, for these paths, by default.

## What is blocked

| Blocked | Examples |
| --- | --- |
| Loopback | `127.0.0.0/8`, `::1`, `localhost` and `*.localhost` |
| Link-local, including cloud metadata | `169.254.0.0/16`, `fe80::/10` |
| Private | `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `fc00::/7` |
| Shared address space | `100.64.0.0/10` |
| Unspecified, multicast, broadcast, reserved and documentation ranges | `0.0.0.0/8`, `224.0.0.0/4`, `240.0.0.0/4`, `255.255.255.255`, `::`, `ff00::/8`, `2001:db8::/32` |
| Tunnelled forms of the above | IPv4-mapped IPv6 (`::ffff:10.0.0.1`), `64:ff9b::/96`, 6to4 `2002::/16`, Teredo `2001::/32` |
| Any scheme other than `http` or `https` | `file:`, `ftp:`, `gopher:` |
| A user name or password in the URL | `https://user:pass@host/` |

Unusual spellings of an address are understood and blocked the same way: `http://2130706433/`, `http://0x7f000001/`, `http://0177.0.0.1/` and `http://127.1/` are all `127.0.0.1`.

## Where it is enforced

1. **When the configuration is saved.** A literal host or address that the policy blocks is refused with a message that points here. This catches mistakes early; it cannot catch a name that resolves to an internal address later.
2. **When the call is made.** The host name is resolved at that moment, every address is checked, and the connection is made to a validated address itself, so the name cannot be changed to point somewhere else between the check and the connection (DNS rebinding). A name that resolves only to blocked addresses is refused; where it resolves to a mixture, only the permitted addresses are used.
3. **Redirects are never followed.** A 3xx answer is recorded as a refusal. The `Location` is not requested.

A refusal is recorded without the address: a release or refuse is recorded as a Refused leg on the held payment, and the hold stays Held; a dispatch is recorded as Refused in the dispatch audit; a range web hook is recorded as a Refused leg. The record carries only a code (`hostBlocked`, `resolvesToBlockedAddress`, `hostNotResolved`, `redirectRefused`, `schemeNotAllowed`, `userInfoNotAllowed`).

## Allowing a trusted internal receiver

Nothing is allowed by default. If a receiver legitimately lives on a private network, list it explicitly with these settings, which are read like every other setting (environment variable or the `DynamicEnvironment` defaults) and are empty by default:

| Setting | Value |
| --- | --- |
| `JUBE_HOOK_ALLOWED_HOSTS` | Comma, semicolon or space separated host names. An entry matches that exact host, or any subdomain when written `*.corp.example`. A host that is listed is permitted whatever it resolves to, so list only names you control. |
| `JUBE_HOOK_ALLOWED_CIDRS` | Comma, semicolon or space separated ranges such as `10.20.0.0/16` or a single address. Only a blocked address that falls inside a listed range is permitted. An entry that is not a valid range is ignored. |

Prefer a narrow range or a single host over a broad one. Neither setting can be used as the name of a hook credential variable.

## Not covered

The policy applies to the three paths above only. These outbound calls, which take an address from configuration, are not behind it and follow whatever the host network allows:

- the legacy payload mode of Case Workflow dispatch: macro, action, status and form HTTP endpoints, and the status change notifier, which use a shared client that follows redirects;
- Model HTTP adaptation callbacks;
- SMS and e-mail notifications (the SMS gateway address and the SMTP host);
- the HTTP location of a sanctions source;
- the AskJooby web search, reranker and company registry clients, and the R script server client.
