---
layout: default
title: Svirfd
nav_order: 30
parent: Models
grand_parent: Configuration
---

# Svirfd

Svirfd looks up an IP address for IP intelligence from [svirfd.com](https://svirfd.com) on every invocation of a model,
and surfaces the whole record to rules, the response and the archive. It is built on the `Svirfd` NuGet package.

> Data: svirfd.com, built from RIR delegated statistics, RouteViews, RIPE NCC RIS, open traceroutes, Wikidata (CC0) and
> operator-published lists. Credit svirfd.com where this data is shown.

## The IP data type

A Request XPath has the data type **IP**. It is held as text and behaves as a String everywhere a String does:
Dictionary lookups, Sanctions, TTL Counters, Abstraction Rule search keys, the case key, suppression and Archive Key.
Setting a field to IP does nothing more on its own, and an IP field stays available whatever `EnableSvirfd` says.

<!-- TODO(screenshot): file=model-network-svirfd-entries.png route=/models/models/:id state="Network hub, Svirfd tab, two entries, one active" -->

## Configuring a lookup

Svirfd is a child of the model, in the Network hub beside Graph Recall. Each entry has:

| Field            | Meaning                                                                                                                    |
|------------------|----------------------------------------------------------------------------------------------------------------------------|
| Name             | The name rules read the record by: `Svirfd.{Name}.{Member}`. Letters and digits only, starting with a letter.              |
| Field            | A Request XPath of the model with data type IP.                                                                            |
| Active           | Whether the address is looked up on every invocation.                                                                      |
| Response Payload | Return the whole record in the response JSON under `Svirfd`.                                                               |
| Archive          | Write the record into the archive JSON under `svirfd`, which reporting, training and the case journal read. On by default. |

Entries are maker-checker approved like every other model child and travel with a model export.

A value that is not an address, or a lookup that cannot be answered, leaves the entry absent: every member reads as
`Nothing` and a rule never fails because of it.

## Reading the record in rules

Every rule type can read the record: Gateway, Abstraction, Activation, Inline Function, Inline Script, case search and
the Exhaustive and Advanced Analytics variables.

```
If Svirfd.Customer.Privacy.Vpn = True Or Svirfd.Customer.Privacy.Tor = True Then
    Return True
End If
```

| Member        | Example                                                                                                              |
|---------------|----------------------------------------------------------------------------------------------------------------------|
| Scalars       | `Svirfd.Customer.Asn.Name`, `Svirfd.Customer.Location.CityConfidence`, `Svirfd.Customer.Routing.Prefix`              |
| Nested blocks | `Svirfd.Customer.Connection.Hosting`, `Svirfd.Customer.Cloud.Provider`, `Svirfd.Customer.Registry.Country`           |
| First origin  | `Svirfd.Customer.Routing.Origin.Rpki`, `Svirfd.Customer.Routing.Origin.Peers`                                        |
| Array lengths | `Svirfd.Customer.Lists.Count`, `Svirfd.Customer.Privacy.ListsChecked.Count`                                          |
| Distance      | `Svirfd.Customer.DistanceKm(latitude, longitude)`, `DistanceMiles(...)`, `WithinKm(latitude, longitude, kilometres)` |

A `Nothing` is not `False`. In the record a null means no public source says, so a VPN flag of null is not a statement
that the address is not a VPN; `Privacy.ListsChecked` names the lists that were consulted. A city is only as good as its
confidence and evidence: do not act on `Location.City` when `Location.CityConfidence` is `none` or `low`.

`DistanceKm` uses the located coordinates, falling back to the hub and then the default point, and answers `Nothing`
when the record places the address nowhere. The distance is a Haversine great-circle distance, so it composes with the
other geospatial extensions.

The builder lists every member under the group **Svirfd** with its type, and the coder offers them as completions.

## Inline scripts

An Inline Script receives the whole invocation context, so it reads the record, and can look up any address it derives:

```csharp
var record = context.EntityAnalysisModelInstanceEntryPayload.Svirfd["IP"];
var vpn = record?.Privacy?.Vpn;
var other = await context.EntityAnalysisModel.Services.Svirfd.LookupAsync("203.0.113.9", CancellationToken.None);
```

## The case journal

For a transaction that has an archived record, the Case Key Journal shows the record's members as the `svirfd` columns,
and on an IP key of the transaction card opens the **Svirfd** panel: a fresh lookup of the same address beside the
archived record, with every member that has changed since, marked.

## Modes and limits

See the Svirfd variables in [Environment Variables](../../../Concepts/EnvironmentVariables/index.md). Without a key the
client calls the public API on the open tier, which is one lookup a second per address; that is enough to trial the
feature, and `EnableSvirfdRemoteEngineLookup` can switch per-transaction lookups off. A Premium key loads the whole
index into the process, so a lookup is a memory read, with no rate limit and no address leaving the server.

Turning `EnableSvirfd` off removes the lookup, the Svirfd page, the case journal panel and the Ask Jooby tool, because
the structure of a record cannot be verified without it. It does not remove the IP data type from models.

### Staying up when the feed does not

With a Premium key the client loads the whole index and a lookup is a memory read. If that download fails at start (a
rate limit, the index still loading on the service, a network failure), the client does not stop: it answers every
lookup from the public API at once, reports the state `FellBackToRemote`, and retries the download in the background,
pausing `SvirfdLocalRetryPause` first and doubling up to ten minutes. When the snapshot loads it switches to Local. A
key the service refuses stays on the API. The Svirfd page shows the state while it is not `LocalReady`.

`SvirfdCacheDirectory` keeps the downloaded snapshot (about 75 MB) so that a restart loads it from disk and fetches only
what changed, instead of downloading it all again. Point it at a folder that survives a restart; the Docker compose file
mounts a volume for it. If the service has not been heard from for `SvirfdStaleAfter`, the state becomes `Stale` and the
last good index keeps answering. Every request names Jube in its user agent.

### Time zone

Each record carries `Timezone.Id`, the IANA zone of the address (for example `Europe/Warsaw`) and `Timezone.Basis`,
whether the country has one zone or the located city chose it. It is null when the country has several zones and no city
is known. Rules read it as `Svirfd.IP.Timezone.Id`, which is useful to set beside the zone a visitor's browser reports.

## What leaves the installation

IP addresses are personal data under most data protection
regimes. <!-- TODO(docs): the owner and the data protection officer must confirm this section, the lawful basis wording and whether a data processing agreement with svirfd.com is needed -->

- **Local and Directory modes** never send a transaction address anywhere: the index is held in the process (Local
  downloads the public snapshot; Directory reads files with no network at all).
- **Remote mode** (the open tier, or a key without the Premium feed grant) sends one HTTPS request per address to
  `SvirfdBaseUrl`, and only when a model has an active Svirfd entry and `EnableSvirfdRemoteEngineLookup` is True. The
  request carries the address in the path, the API key, and a user agent naming the Svirfd package and Jube. No other
  field of the payload, no tenant, model, user or transaction identifier is sent.
- Private, loopback, link-local, carrier-grade NAT, multicast and unspecified addresses (for example `10.0.0.5`,
  `192.168.1.1`, `fe80::1`) are never sent: they have no public record, and sending them would disclose the internal
  address plan.
- Answers are cached in memory for `SvirfdRemoteCacheMinutes`, shared by every tenant of the installation because an
  address record is public data, not tenant data. The cache is not written to disk.
- Remote lookups run through the shared `Jube.ResilientHttp` pipeline: a timeout (`SvirfdRemoteLookupTimeout`, at least
  50 ms), a breaker per service host that opens after repeated transient failures (the `ResilientHttp*` settings), and
  no retry, because a lookup happens inside a transaction and the open tier allows one a second. On top of that Svirfd
  keeps the pauses the service itself dictates: its `Retry-After`, an exponential pause after a rate limit, five seconds
  after a timeout or failure, and off until restart when the key is refused.

<!-- TODO(docs): confirm the retry decision (none) with the owner; the interactive Svirfd page could take a small retry budget -->

- To send nothing, set `EnableSvirfdRemoteEngineLookup` to False (Remote lookups during invocation stop; the Svirfd page
  still looks up the address a user types), set `EnableSvirfd` to False, or use Directory mode. The two environment
  switches are installation-wide. A tenant also opts in by configuring a Svirfd entry on a model, and the platform
  operator can switch remote lookups off for one tenant (next bullet).
- **Per-tenant switch (landlord only).** On the Tenants page (Administration > Tenants, visible only to a
  landlord user), the `SvirfdRemoteLookupsEnabled` switch under "DataProtection" is on by default for every existing and
  new tenant. When it is switched off, the engine makes no remote Svirfd lookup for that tenant's transactions, the
  Svirfd page and the case journal comparison make none for that tenant's users, and no address leaves the installation
  for that tenant. Local and Directory lookups, and the behaviour when Svirfd is offline, are unchanged. The effective
  rule is both allow: `EnableSvirfd` and `EnableSvirfdRemoteEngineLookup` on the installation, and the switch on the
  tenant. The switch is read when models are synchronised, so a change takes effect on the next sync (the Svirfd page
  and journal read it on each request). A tenant administrator cannot change it: the tenant's data-protection posture is
  decided by the platform operator, exactly like the HTTP adaptation settings. Cached remote answers are never served to
  a tenant whose switch is off. The Svirfd status shows
  `TenantRemoteLookupsEnabled`. <!-- TODO(docs): confirm with the owner and the data protection officer that landlord-only is the intended control, and whether tenants should be able to switch it off themselves (see review/svirfd-tenant-switch.md) --> <!-- TODO(screenshot): Tenants page, the DataProtection group with the Svirfd remote lookups switch -->
- `SvirfdBaseUrl` must be an `https` address with no embedded credentials (plain `http` is accepted only for a loopback
  host such as a local test server). Otherwise the client is not created, the error is logged and lookups are off, so
  the API key and addresses are never sent in clear. It is an operator setting; no tenant or API caller can change it.
- The API key is read from the environment (it may be a `[@SvirfdApiKey@]` secret file token), is never stored in the
  database, never returned by `/api/Svirfd/Status` and never written to the log by
  Jube. <!-- TODO(docs): confirm the Svirfd package never logs or echoes the key in exception messages, which the status page shows in the last error -->

<!-- TODO(docs): the Svirfd package is pinned to exactly 0.0.3 in Jube.Svirfd.csproj ([0.0.3]); document the upgrade procedure and the 0.0.3 brief once the owner confirms it -->

## Licence

The package is MIT. The data is licensed separately for internal use under
the [svirfd.com licence](https://svirfd.com/licence): credit svirfd.com wherever the data is shown, do not redistribute
it, and destroy local copies when a Premium subscription ends. Jube shows the credit on every surface that shows a
record.
