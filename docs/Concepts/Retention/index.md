---
layout: default
title: Data Retention and Legal Hold
nav_order: 16
parent: Concepts
---

# Data Retention and Legal Hold

By default Jube keeps archived transactions for ever. A deployer who must not (a data protection retention limit, or
simple
storage cost) turns on the retention purge, and a supervisor protects what must be kept by placing a case under legal
hold.

## What the retention purge removes

When EnableRetentionPurge is True the engine runs the purge every WaitRetentionPurge milliseconds (daily by default).
Each pass:

1. Removes archived transactions whose reference date is more than RetentionArchiveDays old, together with their archive
   keys,
   tags and versions. Zero days, the default, removes nothing.
2. Removes expired AskJooby memory and data cache rows, which were previously only hidden once expired.
3. Removes inbound queue rows whose model has been deleted or no longer exists. Nothing can ever process them, so they
   would otherwise sit in the queue for ever. The dry run counts them without removing them, and a model that is still
   live is never touched.

A transaction is **never** removed while a case that was raised from it is any of these:

| Case state           | Why it is protected                    |
|----------------------|----------------------------------------|
| Open                 | Someone is still working it.           |
| Suspended (any kind) | It will come back to an analyst.       |
| Under legal hold     | A supervisor has said it must be kept. |

A transaction raised into a case that is closed and not under hold is removed after its age limit; the case itself, with
its
notes, files, events and the payload copy it was created with, is not touched.

The purge is off by default and, when first turned on, **only counts**: RetentionPurgeDryRun defaults to True, which
logs how many
archived transactions it would remove and removes nothing. Read the logged counts, then set RetentionPurgeDryRun to
False.
Removal is in chunks of RetentionPurgeDeleteLimit so a large backlog does not hold a long lock.

The purge connects with PurgeConnectionString when it is set, falling back to ConnectionString. In the cluster the
`jube_app` role
has no delete rights by design, and `jube_purge` is the role that does; `Jube.Cluster/database.sh` grants it delete on
exactly the
tables the purge touches (the archive tables, the graph tables, the two AskJooby tables and the inbound queue) and read
on `Case`.

## Legal hold

A supervisor (the permission used for reassigning and breaking locks) places a case under legal hold with `POST
/api/CaseSupervision/LegalHold`, giving the case, `hold: true` and a reason of up to 500 characters, and lifts it with `hold:
false`. The reason, the user and the time are recorded on the case (`LegalHold`, `LegalHoldReason`, `LegalHoldUser`,
`LegalHoldDate`). A hold can be placed on an open or a closed case. The case page shows a Legal hold card to
supervisors, with the current state and a place or lift action, and each change writes a timeline event (Legal Hold
Placed with the reason, Legal Hold Lifted). `GET /api/CaseSupervision/LegalHold/{caseId}` reads the state. Lifting or
overwriting the hold that a filed regulatory report placed needs the report permission (76) as well as the supervisor
permission, and users without a report permission (75, 76 or 77) are not shown the reason, user or date of any hold.
Submitting a regulatory report places a hold automatically, with the reason "Regulatory report filed", and the placement
then writes no timeline event, so the filing is not disclosed.

<!-- TODO(screenshot): the Legal hold card on the case page, held and not held. -->

<!-- TODO(docs): the hold protects only the transaction a case was raised from, not every transaction of the same account; protecting the whole key history is a stronger rule the owner may want. -->
<!-- TODO(docs): RetentionArchiveDays is one value for the whole deployment; the plan was one per tenant and per model. -->
<!-- TODO(docs): not purged yet, and still kept for ever: cases, case files and notes, graph nodes and edges, the transaction cache, AskJooby conversations, messages and runs, application logs beyond their own purge. -->
<!-- TODO(docs): field encryption uses one key for the whole deployment, so erasing a person by destroying a key is not possible; erasure has to delete rows. -->
<!-- TODO(docs): the first run on a large archive may take a long time and generate WAL; run the dry run first and consider RetentionPurgeDeleteLimit. -->

## Erasing a data subject on request

A person asks for their data to be erased. Jube identifies the person by an **archive key**: the name of a key the model
archives (for example `AccountId`) and its value. Two different people are involved, and the second must be someone
else.

1. A holder of **Request Erasure and Export Subject Data** (permission 78) previews the subject with
   `POST /api/Erasure/Preview`, which says how many archived transactions match and, if the erasure could not go ahead,
   why. They then raise the request with `POST /api/Erasure/Request`, giving the key name, key value and a reason such
   as the data subject request reference.
2. A different person holding **Approve Erasure** (permission 79) approves it with `POST /api/Erasure/Approve` or
   rejects it, with a reason, with `POST /api/Erasure/Reject`. The person who asked can withdraw a pending request with
   `POST /api/Erasure/Retract/{guid}` but can never decide it.
3. The engine, not the web request, performs an approved erasure (`EnableErasureExecution`, on by default, every
   `WaitErasureExecution` milliseconds), on `PurgeConnectionString` when set. This is deliberate: in a cluster the
   `jube_app` role has no delete rights, and the request that approves it never holds them.

An erasure is **refused**, whole and with a reason, when any of these is true. The check is made when the request is
raised and again when the engine runs it, so a hold placed in between still stops it.

| Reason                       | Meaning                                                                                                                      |
|------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| Under legal hold             | A case raised from one of the subject's transactions, or opened on the same key value, is under legal hold.                  |
| In an open case              | Such a case is open or suspended. Close it first.                                                                            |
| Inside the retention minimum | A transaction of the subject is newer than `ErasureRetentionMinimumDays` (1825, five years, by default; 0 means no minimum). |
| Nothing found                | No archived transaction carries that key and value for the tenant.                                                           |

When it proceeds it removes, in one transaction: the subject's archived transactions with their keys, key versions,
tags, tag versions and versions; the graph nodes whose key and value are the subject's and every edge touching them; and
the payload copy held on closed cases raised from those transactions (the case, its notes, files and events stay). The
request row is kept as the record of the erasure: who asked, who approved, why, when, and the counts removed. It holds
the key value itself, so treat it as personal data too.

What erasure does **not** do, honestly: it removes whole transactions, so another person named inside the same
transaction's payload is erased with it; it does not reach the transaction cache, which expires by itself; and field
encryption uses one key for the whole deployment, so a person cannot be erased by destroying a key, only by deleting
rows.

## Subject access export

`POST /api/Erasure/Export` (the same permission 78) returns every archived transaction of a subject, oldest first, as
one JSON bundle: for each its entry guid, model, dates and the archived record, which holds the payload and the values
Jube derived from it. At most 10,000 transactions are returned and the bundle says when it was cut short. Case data and
regulatory report data are deliberately **not** included, because disclosing them could tip a customer off. Every export
is audited.

<!-- TODO(screenshot): the Erasure page: the request list, the preview of a subject and the approve and reject dialog. -->
