---
layout: default
title: Sanctions Loader
nav_order: 1
parent: Sanctions
grand_parent: Configuration
---

# Sanctions Loader

Sanctions are published by various bodies and provide a list of names for which business is prohibited. The
functionality offered by Jube allows the loading of Sanctions lists into the engine for matching using fuzzy logic
(Levenshtein Distance) to create matches. Jube also allows this matching to be embedded into the Entity Analysis Model
recall in real time. Sanctions data is stored in the engine, in memory, so to assure that the recall is extremely fast.

Sanctions requires the EnableSanction Environment Variable being set to True:

```text
EnableSanction=True
```

On the enabling of sanctions, a thread will be started. This thread should be started on all servers that require online
recall of sanctions, either directly or via model invocation.

There are many different types of sanctions file available, published from the EU, US, UN etc. The specification of
sanctions files are stored in the SanctionEntrySource database table the name, delimiter and presence of several
mappings detailed later in the loading process:

```sql
select * from "SanctionEntrySource"
```

<!-- TODO(screenshot): file=sanctions-sanctions-entry-source.png route=/administration/sanctions-loader state="replaces the Razor-era SanctionsEntrySource.png; capture from the Vue UI" -->
![Image](sanctions-sanctions-entry-source.png)

By default there are four file definitions in place, alongside their locations on the internet:

* US Office Of Foreign Assets Control (OFAC) Specially Designated National (SDN).
* Bank of England (BOE) Sanctions.
* European Union (EU) Sanctions.
* US Office Of Foreign Assets Control (OFAC) Specially Designated National (SDN) alternative names.

The seeded Bank of England (BOE) definition reads the UK Sanctions List: the file starts with a report date line and a
header line (Skip 2), the name is spread across the Name 6 and Name 1 to Name 5 columns (MultiPartStringIndex 4 to 9)
and the reference is the Unique ID column (ReferenceIndex 1). The seeded alternative names definition reads the comma
delimited OFAC alt.csv, taking the alias from column 3 and the SDN entity number from column 0 as the reference. Both
definitions were corrected by a migration that only rewrites a definition still carrying the original seeded values, so
a definition changed by hand is left alone. For the corrected BOE definition the migration also soft deletes the
entries built from the old column mapping, and the next load inserts them again from the correct columns.

New entries to the SanctionEntrySource table can be accepted as following definition:

| Value                     | Description                                                                                                                                                                                                                              | Example                                         |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------|
| Name                      | The reference for the sanctions source.                                                                                                                                                                                                  | SDN                                             |
| Severity                  | An integer number to express notional severity.                                                                                                                                                                                          | 1                                               |
| Directory Location        | In the absence of internet or HTTP polling the local file system directory to poll for same file.  All files in the directory will be consumed.  Intended for use only on the basis that security disallows access to the HTTP resource. | /Sanctions/SDN                                  |
| Delimiter                 | The delimiter separating the field in the file.                                                                                                                                                                                          | ,                                               |
| Multipart String Index    | The zero based field location for the multipart string.  I the event of many elements needing to be concatenated,  seperated with a comma, whereby concatenation will be seperated by space.                                             | 1                                               |
| Reference Index           | The zero based field location for the reference of the entry                                                                                                                                                                             | 0                                               |
| Enable Directory Location | A bit value to instruct polling of a Directory Location for new files.                                                                                                                                                                   | 0                                               |
| Enable HTTP Location      | A bit value to instruct polling of a HTTP location for latest file.                                                                                                                                                                      | 1                                               |
| HTTP Location             | The location to poll for current sanctions files.                                                                                                                                                                                        | https://www.treasury.gov/ofac/downloads/sdn.csv |
| Skip                      | The first rows to skip on account of having header data.                                                                                                                                                                                 | 0                                               |
| AliasIndex                | Optional. The zero based column holding other names of the same entity. Each alias is loaded as its own entry with the same reference as the entity, so a match on any name identifies the entity.                                       | 3                                               |
| AliasSeparator            | Optional. The text that separates the aliases inside the AliasIndex column. A semicolon when AliasIndex is set and this is empty.                                                                                                        | ;                                               |
| FilterIndex               | Optional. The zero based column to filter rows on, for example the entity type.                                                                                                                                                          | 1                                               |
| FilterValues              | Optional. Comma separated values of the FilterIndex column that are loaded, compared ignoring case. Any other row is not loaded, and an entry loaded earlier for such a row is removed at the next load. Empty loads every row.          | Person,Organization                             |

To enable the loading process, the following Environment Variables need to be set:

| Value                          | Description                                                                                                                                                                                                                                                                                                                                                                               |
|--------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| EnableSanctionLoader           | A flag indicating if sanctions should be loaded via this instance of the engine and inserted into the SanctionEntry database table,  noting that this table will be synchronised to memory.                                                                                                                                                                                               |
| SanctionLoaderWait             | Interval in millisecond between polling and synchronizing new sanctions files.  A key concept is regular polling of the official locations with a view to perpetual validation and merging of the new records with the database.                                                                                                                                                          |
| EnableSanctionLoaderChangePoll | True (default) enables the check described under SanctionLoaderChangePoll. False restores a single wait of SanctionLoaderWait. It is independent of EnableSanctionLoader, which only gates loading of the HTTP and directory sources.                                                                                                                                                     |
| SanctionLoaderChangePoll       | Interval in milliseconds (default 60000, one minute) at which the engine checks the import audit for a new import, such as an upload from the UI, and refreshes the sanctions cache straight away, so a new list is searchable within seconds rather than after SanctionLoaderWait. A missing, non numeric, zero or negative value is read as 60000. Nothing is downloaded by this check. |

```text
EnableSanctionLoader=True
SanctionLoaderWait=60000
```

By default the sanctions loader process is disabled so not to make connections to the internet without explicit
instruction by the end user. Via Migration, there is only a single example sanction for Robert Mugabe loaded by default
for the purpose of documentation and training.

In enabling Environment Variables for the loader, the files will be loaded and their contents inserted into the
SanctionEntry database table:

```sql
select *
from "SanctionEntry"
```

<!-- TODO(screenshot): file=sanctions-public-sanctions-in-table.png route=/administration/sanctions-loader state="replaces the Razor-era PublicSanctionsInTable.png; capture from the Vue UI" -->
![Image](sanctions-public-sanctions-in-table.png)

The SanctionEntry database table is the initial target for the synchronisation on loading of the instance. On perpetual
processing of sanctions files the entries will be merged. An entry is validated by creating an MD5 hash - of the source,
the multipart string value and the reference, taken together - then checking its existence in the SanctionEntry database
table, and thereafter if new, adding it to both the SanctionEntry database table and the in memory copy of it. A changed
name or reference for what is otherwise "the same" entry therefore produces a different hash, and is treated as a new
entry rather than an update to the old one.

## Import and rejection audit trail

Every run of the loader - for every SanctionEntrySource in turn - records a SanctionEntryImport row describing what
happened: total rows read, and how many were Inserted, Revived, Unchanged, Removed and Rejected, along with whether the
run was Successful and, if not, the error message. Individual bad rows are recorded to SanctionEntryRejection with the
row number, raw data and a rejection ReasonId, rather than only failing the run as a whole:

| ReasonId | Meaning                                                                                                                     |
|----------|-----------------------------------------------------------------------------------------------------------------------------|
| 1        | InsufficientFields - the row did not contain enough delimited fields to be parsed.                                          |
| 2        | NoReferenceIndexConfigured - the SanctionEntrySource has no Reference Index configured, so no reference could be extracted. |
| 3        | ParseError - an unexpected error occurred while parsing the row.                                                            |

Only the ReasonId is stored, not the underlying exception message - rows sharing the same failure mode would otherwise
duplicate near-identical free text with no real diagnostic gain over the enum itself.

The four outcomes recorded per entry are:

| Outcome   | Meaning                                                                                                                                                                                                                                                                                                                                             |
|-----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Inserted  | The entry's hash was not previously seen for this source - a brand new row.                                                                                                                                                                                                                                                                         |
| Unchanged | The entry's hash matches an existing, active row - no write is made.                                                                                                                                                                                                                                                                                |
| Revived   | The entry's hash matches an existing row that had previously been soft-deleted (see below) - the row is restored in place rather than re-inserted, so its Id is preserved.                                                                                                                                                                          |
| Removed   | An existing active entry for this source was not present in this run's file at all, and is soft-deleted (SanctionEntry.Deleted / DeletedDate / DeletedUser set) rather than physically removed. If a file produces zero rows at all, removal is skipped entirely for that run, so a broken or empty download cannot mass-delete a source's entries. |

Because Removed entries are soft-deleted rather than physically deleted, an entry that disappears from a published list
and later reappears (Revived) keeps its original Id and any history associated with it, rather than starting over as a
new row.

## Manual upload

In addition to the automatic polling loader above, a source's file can be uploaded manually from Administration >>
Sanctions Loader, which requires Landlord permission. This is useful where a source only publishes on an ad hoc basis,
or where outbound internet access from the Jube servers is restricted and files are instead obtained and vetted out of
band before being loaded.

Manual upload shares the same import path as the automatic loader described above - the same hash-based
Inserted/Unchanged/Revived/Removed reconciliation, and the same SanctionEntryImport/SanctionEntryRejection audit rows
are written - so a manually uploaded file behaves identically to one picked up automatically, except that CreatedUser on
the resulting audit trail records the logged-in user's name rather than the background loader.

The search reads the engine's in memory copy, so a successful upload is searchable once that copy has been refreshed.
Between full loader cycles (every SanctionLoaderWait, hourly by default) the engine checks the successful imports in
SanctionEntryImport every SanctionLoaderChangePoll (one minute by default); when a new successful import is seen it
reloads the stop tokens and the entries and brings the memory copy into line with the database in both directions:
new entries are added, and entries that have been removed or soft deleted (for example because the new file no longer
contains them) leave memory too. A failed import, or a source that answered an error, does not change the copy.

## Stop Tokens

Before matching, common honorific and religious/cultural titles - tokens such as those equivalent to "Sheikh" or
"Imam" that are common in some naming conventions - are stripped from both the searched string and every sanction entry,
so that including or omitting such a title does not itself affect whether names match. The list of stop tokens lives in
the SanctionStopToken database table, seeded with an illustrative starting set via migration, and is loaded into memory
at the start of every loader cycle alongside sanction entries and files. The memory copy follows the table in both
directions: a token that is added or whose category changes takes effect, and a token that is deleted from the table
stops being applied, on the next cycle, without a restart.

## Download policy and safety limits

<!-- TODO(docs): owner to read this whole section; every statement below was written from the code, not from a running system. -->

The automatic loader downloads each HTTP Location through the same outbound host policy used by webhooks and case
hooks (see [Outbound Host Policy](../../Administration/OutboundHostPolicy/index.html)): only http and https, no user
info in
the URL, and every address the host name resolves to is checked at connection time, so a source that resolves to a
loopback, private, link-local or cloud metadata address is refused unless the operator has allow-listed it with
`JUBE_HOOK_ALLOWED_HOSTS` or `JUBE_HOOK_ALLOWED_CIDRS`. Redirects are followed by the loader itself, at most five, and
each
hop is checked against the same policy, which keeps the real OFAC address (it redirects) working while preventing a
redirect into the internal network. TLS certificates are validated and the environment proxy is not used.
<!-- TODO(docs): confirm the proxy statement with the owner; sites that must egress through a proxy cannot use the HTTP Location today. -->

| Limit                | Value                                                                                                          | Behaviour when exceeded                                                                                        |
|----------------------|----------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------|
| Download size        | 128 MiB                                                                                                        | The download is abandoned; the import is recorded as unsuccessful and nothing is delisted.                     |
| Download time        | 5 minutes in total                                                                                             | As above.                                                                                                      |
| Rows in one file     | 5,000,000                                                                                                      | Reading stops; the import is recorded as unsuccessful and nothing is delisted.                                 |
| Manual upload size   | 64 MiB                                                                                                         | The upload is refused with a validation error and no import row is written.                                    |
| Delisting in one run | A run that would remove more than half of a source's active entries (sources with at least 100 active entries) | Removal is skipped and a warning logged; additions still apply. A truncated or wrong file is the likely cause. |

An import whose file could not be read to the end (a dropped connection, an unreadable file) is recorded with
Successful 0 and never removes entries, and a manual upload that ends this way is shown as an error rather than as
loaded. The list files are CSV and are read by a delimited-text parser: no XML or archive is ever opened, so XML
external
entities, entity expansion, zip-slip and decompression bombs do not apply.
<!-- TODO(docs): the SDN, BOE and EU lists are CSV today; if XML or zip formats are ever added, a hardened reader is needed first. -->

Sources are configured only in the database (there is no screen to edit an HTTP Location), so the download address is
operator controlled and never tenant controlled. Sanction entries and sources carry no tenant: they are landlord-owned
reference data shared by every tenant, and only a Landlord user can list sources or upload a file. There is no
tenant-level manual entry of sanctions.
<!-- TODO(docs): owner to confirm that tenant-specific manual sanction entries are intentionally not offered; nothing in the docs or UI promises them (the root ManualSanctions.png is a Razor-era screenshot of the manual search page). -->

## Loading a list with aliases and a mix of entity types

Many lists put each entity on one row with its other names together in one column. An OpenSanctions export in the simple
CSV format, for example, has the columns `id, schema, name, aliases, birth_date, countries, addresses, identifiers,
sanctions, phones, emails, dataset, first_seen, last_seen, last_change`, with the aliases separated by semicolons and
the
`schema` column saying whether the row is a Person, an Organization, a Vessel and so on. Such a list is described by one
row in SanctionEntrySource:

| Value                | Setting               |
|----------------------|-----------------------|
| Delimiter            | `,`                   |
| MultiPartStringIndex | `2` (name)            |
| ReferenceIndex       | `0` (id)              |
| Skip                 | `1` (header)          |
| AliasIndex           | `3` (aliases)         |
| AliasSeparator       | `;`                   |
| FilterIndex          | `1` (schema)          |
| FilterValues         | `Person,Organization` |

Politically exposed persons are carried in such lists as ordinary Person rows, and are screened exactly as any other
entry is; which list you load, and what you are licensed to load, decides whether PEPs are screened at all.

<!-- TODO(docs): the licence of any list is the deployer's to check. The OpenSanctions data is not free for commercial use, for example; this page does not grant or imply a licence. -->
<!-- TODO(docs): the source columns above are set in the database; there is no endpoint or Vue page to edit a source yet. Add them to the Sanctions Loader administration page. -->
<!-- TODO(docs): the importer still writes three information level log lines per row, which at a million rows is heavy; lower them to debug. -->
<!-- TODO(docs): the download is still held fully in memory with a 128 MiB cap, which a full OpenSanctions export can exceed; stream it to a temporary file. -->
<!-- TODO(docs): an alias that reduces to a single token because the rest are one letter initials, such as "I. Petrov", matches any name containing that surname at distance zero; consider requiring two tokens for an alias. -->
