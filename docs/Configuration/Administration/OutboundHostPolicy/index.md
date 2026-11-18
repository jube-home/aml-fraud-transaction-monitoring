---
layout: default
title: Outbound Host Policy
nav_order: 5
parent: Administration
grand_parent: Configuration
---

# Outbound Host Policy

Several features cause the server to call an address that an administrator has typed in: the Case Workflow Release Hook, the web hook of a Response Elevation Range and the SQL mode of Case Workflow dispatch (HTTP targets). Left unchecked, a person who can edit one of those could point the server at something only the server can reach, such as the cloud metadata service at `169.254.169.254`, a database or an internal administration console, which is to say server side request forgery. The outbound host policy stops that, for these paths, by default.

The policy is not administered in the user interface. It is always in force, and the only settings are the two Environment Variables that allow a trusted internal receiver, as described below.

## What is blocked

| Blocked                                                              | Examples                                                                                        |
|----------------------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| Loopback                                                             | `127.0.0.0/8`, `::1`, `localhost` and `*.localhost`                                             |
| Link-local, including cloud metadata                                 | `169.254.0.0/16`, `fe80::/10`                                                                   |
| Private                                                              | `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `fc00::/7`                                     |
| Shared address space                                                 | `100.64.0.0/10`                                                                                 |
| Unspecified, multicast, broadcast, reserved and documentation ranges | `0.0.0.0/8`, `224.0.0.0/4`, `240.0.0.0/4`, `255.255.255.255`, `::`, `ff00::/8`, `2001:db8::/32` |
| Tunnelled forms of the above                                         | IPv4-mapped IPv6 (`::ffff:10.0.0.1`), `64:ff9b::/96`, 6to4 `2002::/16`, Teredo `2001::/32`      |
| Any scheme other than `http` or `https`                              | `file:`, `ftp:`, `gopher:`                                                                      |
| A user name or password in the URL                                   | `https://user:pass@host/`                                                                       |

Unusual spellings of an address are understood and blocked in the same manner, such that `http://2130706433/`, `http://0x7f000001/`, `http://0177.0.0.1/` and `http://127.1/` are all taken as `127.0.0.1`.

## Where it is enforced

The policy is enforced at three points, as follows:

1. When the configuration is saved, a literal host or address that the policy blocks is refused, with a message that points to this page. This catches mistakes early, although it cannot catch a name that resolves to an internal address later.
2. When the call is made, the host name is resolved at that moment, every address is checked, and the connection is made to a validated address itself, such that the name cannot be changed to point somewhere else between the check and the connection (DNS rebinding). A name that resolves only to blocked addresses is refused, and where it resolves to a mixture, only the permitted addresses are used.
3. Redirects are never followed. A 3xx answer is recorded as a refusal, and the `Location` is not requested.

A refusal is recorded without the address: a release or refuse is recorded as a Refused leg on the held payment, and the hold stays Held, a dispatch is recorded as Refused in the dispatch audit, and a range web hook is recorded as a Refused leg. The record carries only a code, being one of `hostBlocked`, `resolvesToBlockedAddress`, `hostNotResolved`, `redirectRefused`, `schemeNotAllowed` or `userInfoNotAllowed`.

## Allowing a trusted internal receiver

Nothing is allowed by default. If a receiver legitimately lives on a private network, it is listed explicitly with the following settings, which are read in the manner of every other setting (an Environment Variable, or otherwise the default) and are empty by default:

| Value                     | Description                                                                                                                                                                                                                                  | Example            |
|---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|
| `JUBE_HOOK_ALLOWED_HOSTS` | Host names separated by a comma, a semicolon or a space. An entry matches that exact host, or any subdomain when written with a leading wildcard. A host that is listed is permitted whatever it resolves to, so only names under the control of the operator should be listed. | `*.corp.example`   |
| `JUBE_HOOK_ALLOWED_CIDRS` | Ranges, or single addresses, separated by a comma, a semicolon or a space. Only a blocked address that falls inside a listed range is permitted. An entry that is not a valid range is ignored.                                            | `10.20.0.0/16`     |

A narrow range or a single host is to be preferred over a broad one. Neither setting can be used as the name of a hook credential variable.

## Sanctions sources

The automatic [Sanctions Loader](../../Sanctions/SanctionsLoader/index.html) downloads the HTTP Location of each sanctions source through the same policy, with the same blocked addresses and the same two allow lists. It differs in one respect only: redirects are followed by the loader itself, to a maximum of five, each hop being checked against the policy, such that a published list that redirects continues to load while a redirect into the internal network is refused.

## Not covered

The policy applies to the paths above only. The following outbound calls, which also take an address from configuration, are not behind it and follow whatever the host network allows:

- The legacy payload mode of Case Workflow dispatch: macro, action, status and form HTTP endpoints, and the status change notifier, which use a shared client that follows redirects.
- Model HTTP adaptation callbacks.
- SMS and e-mail notifications (the address of the SMS gateway and the SMTP host).
- The AskJooby web search, reranker and company registry clients, and the R script server client.

## HTTP Adaptation

HTTP Adaptation does not use the allow lists on this page. The installation wide `HttpAdaptationUrl` is set by the operator and is trusted, whereas a URL specific to a Tenant, set by the landlord, is held to the same list of blocked addresses (https only, no loopback, link-local, private or other internal ranges, no credentials, no redirects, and the address checked at connection time), with `HttpAdaptationAllowInsecureTargets` as the override for development, as described in [HTTP Adaptation](../../Models/HTTPAdaptation/index.html).
