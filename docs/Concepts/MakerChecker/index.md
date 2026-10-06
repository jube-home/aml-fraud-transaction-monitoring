---
layout: default
title: Maker Checker
nav_order: 14
parent: Concepts
---

# Maker Checker

Jube loads model configuration into the invoke engine from the database, filtered on `Active`. Maker checker separates
changing configuration from releasing it. A **maker** edits; a **checker** approves; only approved configuration reaches
the engine. Every change is recorded as a version, and the version history is available for every approvable entity.

The engine loads a model as one immutable snapshot. A change becomes live when a checker approves it, and the next
synchronisation cycle publishes it. No one needs to trigger a synchronisation by hand.

## What is approvable

Every entity the model synchronisation loads:

Model, Lists and List Values, Dictionaries and Dictionary KVPs, Request XPaths, Inline Scripts (the model-level wiring),
Inline Functions, Gateway Rules, Sanctions, Abstraction Rules, Abstraction Calculations, TTL Counters, HTTP Adaptations,
Exhaustive Search Instances, Activation Rules and Tags.

Outside approval:

- **Suppression** and **activation rule suppression** are operational actions taken by case workers against live
  traffic, not configuration changes. They take effect immediately and are not approval-gated.
- **The global `EntityAnalysisInlineScript`** is a program unit, compiled and deployed through the code release process.
  Wiring a global script into a model is `EntityAnalysisModelInlineScript`, which is approvable.
- **Reprocessing rules** and **case workflow** entities are not loaded by the model synchronisation, so they are not
  approvable.

## Approvals

An approval is a row in its own table, `EntityApproval`, never a flag on the entity. Each row records the kind, the
entity, the version, the state (approved or rejected), an optional note, and who decided and when.

`(kind, entity, version)` identifies a snapshot of a record. Each update writes the superseded row to the version table
and increments the version, so version *N* is the current row and versions *1..N-1* are version rows. An approval for
version 4 remains valid after version 5 supersedes it. The approval never has to be re-keyed.

- **Pending** is the absence of a row. Only approved and rejected decisions are stored.
- **Rejection** blocks that version. A rejected version never takes effect. Only an edit, which creates the next
  version, opens a new approval cycle.
- **Several approvers** can approve one version. `ApprovalsRequired` in `DynamicEnvironment` (default `1`) sets how many
  distinct approvers make a version effective. Each approver counts once.
- **Self-approval is refused.** The approver must differ from the user who made the version. The one exception is the
  landlord tenant, where a user holding both the write and approve permissions may create a change and approve it alone.
  This is an operator privilege.
- Approval and rejection are separate permissions from editing. *Allow Approval* (permission 44) decides, and *View
  Pending Approvals* (permission 45) lists what is waiting. Approval is one role for every kind, not one per kind.
- A version that is already in effect cannot be rejected. The live rule stays live, and the refusal says so. A version
  that is not yet live can be rejected.

## Effective version

The engine runs, for each entity, its effective version. This is the newest version that has reached `ApprovalsRequired`
distinct approvals and has no rejection against it.

- The current version runs when it is approved.
- Otherwise the newest earlier approved version runs. An edit that is not yet approved leaves the last approved version
  running.
- An entity that has never been approved does not run.

Resolution is set-based. The engine reads, for each kind of entity in a model, the approval rows for all of its entities
in one query, then the current rows and the matching version rows. It never resolves one record at a time.

## Deletes, inserts, values and imports

**A delete is a pending change.** Deleting writes the pre-delete row to the version table and increments the version, as
an update does. The entity keeps running at its last approved version until a checker approves the deletion. An approved
deletion removes the entity from the engine and the tree. A rejected deletion keeps the entity running. Deleted records
can be opened, reviewed and revived by an edit.

**An insert is invisible until approved.** A new record is version 1 with no approval, so the engine does not load it
until a checker approves it.

**List values and dictionary KVPs are approved per value.** Each value has its own approval row, so the audit stays
exact. The list or dictionary page shows a value approval panel, with each value's state, its maker, and approve and
reject controls for a checker. *Approve All Pending Values* approves every pending value of a parent in one action.
Values refused on their own are reported and do not stop the others. Pending values are listed first.

**Preservation imports are approved by the importer.** An import stamps every entity it brings in as approved by the
user who performed it. This bypasses the second approver by design, because importing is a privileged administrative
action. An import takes effect on the importer's authority, whatever `ApprovalsRequired` is set to.

## Dependency pre-flight

A logical change often spans several entities, such as an abstraction rule and the activation rule that consumes it.
Each approval is checked against the dependency graph before it is recorded. The graph is built as it would be after the
approval, and the approval is refused when:

- it deletes an entity that an active rule uses (the message names the dependants);
- it approves an entity whose prerequisites are not yet approved (the message lists them in dependency order);
- it names a target that does not exist.

An inactive dependant does not block an approval. Warnings are shown and allowed.

Validation that depends on other entities is re-evaluated at approval, not trusted from the maker's save. The dependency
check is one such validation. Deletion safety and rule parsing are others.

The integrity page shows the same graph, so a checker can see the dependencies before deciding.

## Synchronisation

Synchronisation is driven by a change-detection probe. Each cycle, for each tenant, the engine reads a small set of
scalars. The probe covers:

- the latest approval row for the tenant, across every approvable kind;
- the latest suppression change;
- the earliest future expiry of a suppression, list value or dictionary KVP. A passing expiry is itself a change;
- the exhaustive promoted-trial watermark, since models are promoted asynchronously by the trainer;
- the count and largest identity of live API keys for active users in the tenant, and of live model role grants.
  Issuing, revoking or re-granting a key or a role, or deactivating a user, is picked up at the next cycle;
- the scheduled synchronisation date.

When nothing has changed, the engine does only two writes: its heartbeat and rule counter persistence. When something
has changed, it rebuilds the whole model and publishes it. The gate fires at most once per `ModelSynchronisationWait`
window (10 seconds by default), so approvals landing together produce one rebuild.

Approval therefore publishes: a change goes live at the next cycle after it is approved. The synchronisation schedule
remains available for deliberate bulk promotion.

If reading the API users fails, the cycle fails. The error is recorded, nothing is published, and the watermark is not
recorded. The model stays pending and the next cycle retries. The previous snapshot stays live until the retry succeeds,
so a revoked key remains admitted for as long as the failure lasts.

Compiling a rule does not repeat for unchanged rules. They reuse their cached assembly.

## Model snapshots

Each model is held as one immutable snapshot. It contains every collection that synchronisation owns: activation,
gateway and abstraction rules, TTL counters, sanctions, adaptations, exhaustive models, request XPaths, abstraction
calculations, inline functions and scripts, tags, lists, dictionaries, the suppression collections, and a generation
counter.

- Synchronisation builds the whole snapshot off to the side and publishes it with one atomic write per model. A reader
  sees either the previous generation or the next one, never a mixture of the two.
- Runtime state stays outside the snapshot, so it survives a cutover: counters, the cache, concurrent queues, the shared
  sanctions dictionaries, archive buffers, the abstraction rule cache and the active model registry. Rule counters carry
  forward from the outgoing generation.
- Readers outside an invocation, namely the exhaustive recall callback and the abstraction rule caching task, read the
  snapshot once.

Invocations read the live snapshot at each point they need a collection, not once at entry. A cutover that lands during
an invocation is therefore visible to the rest of that invocation, which can read two generations.

## Upgrade behaviour

The migrations add the approval table and its permission rows, and then:

- stamp an approval for every existing live entity at its current version, so that current models keep running after the
  upgrade;
- approve activation rules only where they had been approved by review (`ReviewStatusId` 4), the only state the engine
  ever activated, so a rule that was pending or rejected before the upgrade does not switch on at the first
  synchronisation;
- drop the activation rule review status.

Without the back-fill, an upgrade would leave every model empty, because unapproved entities are not loaded.

## Approved by Review is retired

Activation rules no longer have a review status. The review status column, its DTO and validation, its permission grant
and its page control are removed. Permission 41, *Allow Approved By Review*, is kept as a row for referential integrity
and is excluded from the permission listing. The archive serialisation keeps the positions of the removed fields unused,
so existing archives still decode into the right fields.

## Version history

Every versioned entity has a version history endpoint. Each endpoint checks the caller's permission and tenant on every
call. An id the caller cannot see returns not found rather than forbidden, so the endpoint does not reveal which ids
exist in another tenant.

## Pages

- **Approval widget.** Each approvable frame shows the entity's approval state, its progress towards
  `ApprovalsRequired`, the approval history and the version history. A checker can approve or reject with a note.
- **Model tree.** Pending entities are coloured orange, and rejected versions purple, for callers who may view pending
  approvals. Pending takes precedence over active and inactive colours. Rejected is purple rather than red, because red
  already means inactive.
- **Lists and dictionaries.** The value approval panel and *Approve All Pending Values* are described above.
- **Pending Approvals tab.** On the Integrity page. It lists pending entities by kind, with the field-level difference
  against the effective version, and approve and reject inline. Lists and dictionaries link to the parent with the
  pending filter applied. The grid refreshes over the existing server-sent event stream.
- **Integrity findings.** A pending approval is reported as information. An approved entity the engine has not loaded is
  a warning, and so is a stale engine instance. An approved entity that depends on an unapproved one is reported as
  `DependencyOnUnapproved`.

## Edge behaviour

- Inline script properties are not checked by the approval pre-flight. They carry no version and default to approved.
- The live dictionary of active models is updated by the synchronisation thread without a lock, and request threads
  enumerate it while that update runs.
- Counters carried forward at publish can miss increments made by invocations still running against the previous
  generation.
- List and dictionary content is outside the synchronisation watermark. A content change that writes no approval row and
  no schedule change is picked up at the next full synchronisation rather than by the gate.
- A bulk approval commits each item as it goes. If an item is deleted concurrently, the items before it stay approved and
  the call returns not found.
- Synchronisation steps other than the API user sync log and continue on error. The API user sync fails the cycle, so a
  tenant whose API user sync fails permanently holds up the other tenants in the same poll.

## Consequences

- Configuration that has never been approved does not run. The back-fill makes this safe for existing installations.
- Approval is the moment of production change. A change cannot be scheduled to go live later; holding an approved change
  is the way to defer it.
- A maker cannot approve or reject their own work, including in a single-user installation. A landlord is exempt and may
  decide their own changes.
