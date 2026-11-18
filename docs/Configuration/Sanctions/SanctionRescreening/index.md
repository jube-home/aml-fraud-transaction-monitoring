---
layout: default
title: Sanction Rescreening
nav_order: 5
parent: Sanctions
grand_parent: Configuration
---

# Sanction Rescreening

A name is screened when its transaction arrives. If the sanctions list is updated tomorrow with an entry that the name matches, nothing in the original screening would ever notice. Rescreening closes that gap: when a list import adds or revives entries, the names that were screened recently are checked against just those entries.

## Where it lives

Rescreening is a background process of the engine and has no page of its own. It is switched on by Environment Variable, its results are held in the database, and its only presence in the user interface is the notification raised upon new matches being found. The settings are as follows:

| Value                        | Description                                                                                        | Example |
|------------------------------|----------------------------------------------------------------------------------------------------|---------|
| EnableSanctionRescreen       | Set to True to switch rescreening on. It is False by default.                                      | True    |
| SanctionRescreenDays         | The number of days of archived transactions from which the screened names are read, 30 by default. | 30      |
| SanctionRescreenMaximumNames | The greatest number of distinct names read for each sanction check, 100000 by default.             | 100000  |

```text
EnableSanctionRescreen=True
```

## What it does

A pass runs at the end of each sanctions load cycle, and when the change poll sees a new import, as described in [Sanctions Loader](../SanctionsLoader/index.html). The pass proceeds as follows:

1. Every entry records the import that last inserted or revived it (`SanctionEntry.LastImportId`). A pass handles the entries from imports newer than the last pass, which is its watermark (`SanctionRescreenRun.UpToImportId`).
2. For every sanction check configured on every active model, it reads the distinct values of the name field of that check from the archived transactions of the last SanctionRescreenDays, up to SanctionRescreenMaximumNames for each check.
3. It screens those names against the changed entries only, with the distance, the ratios, the match algorithm, the minimum similarity and the transliteration of the check itself, leaving out any pairing covered by an approved [Sanction Exemption](../SanctionExemptions/index.html).
4. Each new match is recorded once in `SanctionRescreenHit` (tenant, name, entry), with the listed name, the reference, the distance and the pass that found it.
5. The users holding the sanctions permission in a tenant with new matches receive one notification in the user interface for the pass, reading Rescreening found, followed by the number of new possible sanction matches.

By way of example, a transaction in the name of Maria Gonzales is processed on Monday and matches nothing. On Wednesday a new file is loaded which, for the first time, lists Maria Gonzales. The original screening is not repeated, yet the pass following that import reads Maria Gonzales among the names screened in the last 30 days, compares it with the entries the import added, records one row in `SanctionRescreenHit` at a distance of 0, and notifies the users of that tenant holding the sanctions permission with the wording Rescreening found 1 new possible sanction matches. A later pass does not record the same name and entry a second time.

The first time it is switched on it does not rescreen the whole list: it takes the existing list as its starting point, and only later changes are rescreened.

## Reading the results

The hits are in the `SanctionRescreenHit` table, and the passes in `SanctionRescreenRun`, readable with the AskJooby SQL tool or a Visualisation datasource. Each hit gives the screened name, the entry and its list, and the distance:

```sql
select *
from "SanctionRescreenHit"
order by "Id" desc
```

A hit says that a name screened earlier would now match. It does not create a case, nor does it reach the Cases page, being recorded and notified only.

## Limitations

The following should be kept in mind when relying upon rescreening:

* The names come from the archived payload field of the check. It follows that a check whose field is subject to field encryption cannot be rescreened, as the archive holds ciphertext, and that a name screened as the result of an inline function is not in the payload.
* Results already in the screening cache are not invalidated by a change to a list, rather they age out at the cache interval of the check.
* Matches use the distance and ratios of the check, so a transaction screened at a distance of 1 is rescreened at a distance of 1. The aggregates that the live check computes, such as Count and Average, are not computed again, only whether a new entry now matches.
* Only entries inserted or revived are rescreened. An entry whose list data changed under the same hash is not, and removals are not reported.
* The pass reads the archive for each check, which on a very large archive with a long window is heavy, the window and the cap on names being the controls.

{% comment %}TODO(verify): owner to decide whether a page listing the hits is to be built, with a link to the transactions of the name and an action to request an exemption, as at present the hits can be read from the database only.{% endcomment %}

## Permissions and security

The notification of a pass is sent to the users holding permission 29, View Sanction, in a tenant having new matches. Hits are recorded against the tenant of the model whose sanction check screened the name.
