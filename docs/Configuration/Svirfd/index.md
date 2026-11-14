---
layout: default
title: Svirfd
nav_order: 10
parent: Configuration
---

# Svirfd

<!-- TODO(screenshot): file=svirfd-page-record.png route=/svirfd state="8.8.8.8 looked up, map, facts and narrative visible" -->
<!-- TODO(screenshot): file=svirfd-page-state-notice.png route=/svirfd state="FellBackToRemote notice above the search box" -->
<!-- TODO(docs): the Razor-era statements on this page were not re-verified against the running Vue page -->

The **Svirfd** page, a top-level menu item, looks up any IPv4 or IPv6 address and shows the full IP intelligence record
from [svirfd.com](https://svirfd.com): who announces the address, where it is and how sure the record is, the evidence
behind the city, VPN, Tor and relay lists, cloud and connection type, what traceroutes have observed and a plain-English
narrative.

It needs the **View Svirfd** permission (72) and is hidden when `EnableSvirfd` is False.

- Enter an address and press Check. The address is kept in the URL, so a result can be shared as a link.
- The map draws the located point, the round-trip bound of every probe, the hubs of the operator and the data centres
  considered.
- A null is shown as not claimed and never as false: a VPN flag of null means no list that was checked names the
  address, and the lists checked are shown.
- An address typed here is looked up fresh (the answer cache is skipped) and, when the installation is in Remote mode,
  is sent to svirfd.com; private and loopback addresses are refused without being sent.
  See [What leaves the installation](../Models/Svirfd/index.md).
- Addresses, AS numbers, prefixes and ranges are shown left to right inside right-to-left locales (Arabic, Urdu).
- Every result carries the svirfd.com credit and its acknowledgements.

The same record component appears in the Case Key Journal when an IP key of a transaction is opened, with the members
that have changed since the transaction was archived marked.

See [Svirfd on a model](../Models/Svirfd/index.md) to look addresses up during invocation,
and [Environment Variables](../../Concepts/EnvironmentVariables/index.md) for the Svirfd settings.

> Data: svirfd.com, built from RIR delegated statistics, RouteViews, RIPE NCC RIS, open traceroutes, Wikidata (CC0) and
> operator-published lists. Credit svirfd.com where this data is shown.
