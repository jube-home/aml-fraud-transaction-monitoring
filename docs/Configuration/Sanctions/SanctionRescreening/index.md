---
layout: default
title: Sanction Rescreening
nav_order: 5
parent: Sanctions
grand_parent: Configuration
---

# Sanction Rescreening

A name is screened when its transaction arrives. If the sanctions list is updated tomorrow with an entry that name
matches, nothing
in the original screening would ever notice. Rescreening closes that gap: when a list import adds or revives entries,
the
names that were screened recently are checked against just those entries.

Set EnableSanctionRescreen to True to switch it on.

## What it does

1. Every entry records the import that last inserted or revived it (`SanctionEntry.LastImportId`). A pass handles the
   entries from
   imports newer than the last pass, which is its watermark (`SanctionRescreenRun.UpToImportId`).
2. For every sanction check configured on every active model, it reads the distinct values of that check's name field
   from
   the archived transactions of the last SanctionRescreenDays (30 by default), up to SanctionRescreenMaximumNames
   (100,000) per check.
3. It screens those names against the changed entries only, with the check's own distance and ratios, leaving out any
   pairing
   covered by an approved [Sanction Exemption](../SanctionExemptions/index.html).
4. Each new match is recorded once in `SanctionRescreenHit` (tenant, name, entry), with the listed name, the reference,
   the
   distance and the pass that found it.
5. The users who hold the sanctions permission in a tenant with new matches get one in-app notification for the pass.

The first time it is switched on it does not rescreen the whole list: it takes the existing list as its starting point
and only
later changes are rescreened. A pass runs at the end of each sanctions load cycle and when the change poll sees a new
import.

## Reading the results

The hits are in the `SanctionRescreenHit` table (and the passes in `SanctionRescreenRun`), readable with the AskJooby
SQL tool or a
Visualisation datasource; each hit gives the screened name, the entry and its list, and the distance. The hit says a
name that was
screened earlier would now match; it does not create a case.

<!-- TODO(docs): a hit does not open a case or reach the Cases page; the owner's decision was that it is recorded and notified only. A page listing the hits, with a link to the transactions of the name and an action to request an exemption, is not built. -->
<!-- TODO(docs): the names come from the archived payload field of the check, so a check whose field is field-encrypted cannot be rescreened (the archive holds ciphertext), and a name that was screened as an inline function result is not in the payload. -->
<!-- TODO(docs): results already in the screening cache are not invalidated by a list change; they age out at the check's own cache interval. Add a list version to the cache key as the exemption fingerprint is. -->
<!-- TODO(docs): matches use the check's distance and ratios, so a transaction screened at distance 1 is rescreened at distance 1; the Count and Average aggregates the live check computes are not recomputed, only whether a new entry now matches. -->
<!-- TODO(docs): only entries inserted or revived are rescreened; an entry whose list data changed under the same hash is not, and removals are not reported. -->
<!-- TODO(docs): the pass reads the archive per check, which on a very large archive with a long window is heavy; the window and the name cap are the controls today. -->
<!-- TODO(screenshot): file=sanction-rescreen-notification.png route=/ state="the notification bell showing the rescreening found new possible sanction matches" -->
