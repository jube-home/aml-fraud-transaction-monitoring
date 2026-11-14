---
layout: default
title: Sanction Exemptions
nav_order: 4
parent: Sanctions
grand_parent: Configuration
---

# Sanction Exemptions (good-guy list)

A customer called Ivan Petrov will match a sanctioned Ivan Petrov at distance zero, every time he transacts, however
carefully an analyst has established that they are different people. A sanction exemption records that judgement once,
so the
match stops recurring, without weakening the list or the matcher for anyone else.

An exemption names three things: the **screened name**, the **sanction list** and the **reference of the entry** within
it.
While the exemption is approved and has not lapsed, that one entry is left out of the matches for that name, in that
tenant, and
only that entry: a different entry of the same list, the same entry matched by a different name, and another tenant's
screening
are all unaffected. The name is compared the way the matcher compares it, so `PETROV, Ivan`, `ivan petrov` and
`Petrov-Ivan` are
the same name.

## Four eyes

An exemption weakens a control, so it needs two people. Someone with the sanctions permission requests it, giving the
reason (required, at most
1,000 characters) and optionally a date on which it lapses. It does nothing until a different person with the approval
permission
approves it; nobody can approve their own request, and an approver who is also the requester is refused. A rejected or
retracted exemption never takes effect and frees the name and entry for a new request. The requester, or any approver,
can
retract a pending or an approved exemption.

| State            | Meaning                                               |
|------------------|-------------------------------------------------------|
| Pending approval | Asked for, not yet decided. No effect.                |
| Approved         | In force until it lapses or is retracted.             |
| Rejected         | Declined by an approver. No effect.                   |
| Retracted        | Withdrawn by the requester or an approver. No effect. |

## How it takes effect

The engine reloads the approved exemptions on every sanctions load cycle, and on every change poll tick when
EnableSanctionLoaderChangePoll is True (once a minute by default), so an approval takes effect within a minute with the
poll on and
within SanctionLoaderWait (an hour by default) with it off.

Sanction results are cached per check. The cache entry's key includes a fingerprint of the tenant's approved exemptions,
so
approving or retracting an exemption makes every earlier cached result unusable at once and the next screening is
recomputed.
The one thing the fingerprint cannot see is an exemption lapsing by the clock: a result computed while an exemption was
in force
may be served from the cache until its own cache interval ends.

The matches that remain, and the aggregate computed from them, are what rules, the response and the archive see, so an
exempted entry is simply absent from the match detail of the transaction.

## Programming interface

| Operation                                  | Needs                                       |
|--------------------------------------------|---------------------------------------------|
| `GET /api/SanctionExemption?stateId=`      | Sanctions or approval permission            |
| `POST /api/SanctionExemption/Request`      | Sanctions permission (29)                   |
| `POST /api/SanctionExemption/Approve`      | Approval permission (44), not the requester |
| `POST /api/SanctionExemption/Reject`       | Approval permission (44), not the requester |
| `POST /api/SanctionExemption/Retract/{id}` | The requester, or the approval permission   |

<!-- TODO(docs): there is no page or case-page action for this yet; the interface above is all there is. Add an Exemptions tab to the Sanctions page and a "this is not them" action on the matches shown in the case journal. -->
<!-- TODO(docs): the approver is not notified of a pending request; raise an inbox notification (the notification table and bell exist) to the holders of the approval permission. -->
<!-- TODO(docs): exemptions are not part of Preservation (move between environments) and have no version history beyond the single row and its decision. -->
<!-- TODO(docs): exemptions are not agent tools; decide whether AskJooby should be able to request one (never decide one). -->
<!-- TODO(docs): the exemption is per screened name text, not per customer; a customer who is screened under a different spelling that folds to different tokens needs a second exemption. -->
<!-- TODO(screenshot): file=sanction-exemptions-list.png route=/sanctions state="the exemptions list with a pending request and an approved one" -->
