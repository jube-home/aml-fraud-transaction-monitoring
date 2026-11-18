---
layout: default
title: Sanction Exemptions
nav_order: 4
parent: Sanctions
grand_parent: Configuration
---

# Sanction Exemptions (good-guy list)

A customer called Ivan Petrov will match a sanctioned Ivan Petrov at distance zero, every time he transacts, however carefully an analyst has established that they are different people. A sanction exemption records that judgement once, such that the match stops recurring, without weakening the list or the matcher for anyone else.

An exemption names three things: the screened name, the sanction list and the reference of the entry within it. While the exemption is approved and has not lapsed, that one entry is left out of the matches for that name, in that tenant, and only that entry: a different entry of the same list, the same entry matched by a different name, and the screening of another tenant are all unaffected. The name is compared in the manner that the matcher compares it, so `PETROV, Ivan`, `ivan petrov` and `Petrov-Ivan` are the same name. It follows that an exemption is for the text of the screened name and not for the customer, which is to say that a customer screened under a different spelling, folding to different tokens, needs a second exemption.

## Where it lives

Sanction exemptions are administered through the API only. There is no page for them in the user interface, nor an action on the matches shown in a case, and the operations are as described in Programming interface below.

{% comment %}TODO(verify): owner to decide whether an Exemptions tab is to be added to the Sanctions page, with an action on the matches shown in the case journal, and whether the holders of the approval permission are to be notified of a pending request, as at present they are not.{% endcomment %}

## Four eyes

An exemption weakens a control, henceforth it needs two people. Someone holding the sanctions permission requests it, giving the reason (required, and at most 1,000 characters) and optionally a date on which it lapses. It does nothing until a different person holding the approval permission approves it: nobody can approve their own request, and an approver who is also the requester is refused. A rejected or retracted exemption never takes effect, and frees the name and entry for a new request. The requester, or any approver, can retract a pending or an approved exemption.

| State            | Id | Meaning                                               |
|------------------|----|-------------------------------------------------------|
| Pending approval | 1  | Asked for, not yet decided. No effect.                |
| Approved         | 2  | In force until it lapses or is retracted.             |
| Rejected         | 3  | Declined by an approver. No effect.                   |
| Retracted        | 4  | Withdrawn by the requester or an approver. No effect. |

To carry the example through: an analyst holding permission 29 requests an exemption for the screened name Ivan Petrov against reference 12345 of source 1, and the exemption is created as Pending approval, during which Ivan Petrov continues to match as before. A second person holding permission 44 approves it, whereupon it is Approved, and within a minute a transaction in the name of Ivan Petrov no longer counts entry 12345 among its matches; where that was the only match, the sanction check has no value for the transaction at all. Should the same analyst attempt the approval, it is refused. Upon the ExpiryDate passing, or the exemption being retracted, the match returns.

An exemption is a single row with its decision, and has no version history beyond that. Exemptions are not carried by Preservation between environments, and are not available to the AI agent tools.

## How it takes effect

The engine reloads the approved exemptions on every sanctions load cycle, and on every tick of the change poll when EnableSanctionLoaderChangePoll is True (once a minute by default). It follows that an approval takes effect within a minute with the poll on, and within SanctionLoaderWait (an hour by default) with it off.

Sanction results are cached for each check. The key of the cache entry includes a fingerprint of the approved exemptions of the tenant, so approving or retracting an exemption makes every earlier cached result unusable at once, and the next screening is computed again. The one thing the fingerprint cannot see is an exemption lapsing by the clock: a result computed while an exemption was in force may be served from the cache until its own cache interval ends.

The matches that remain, and the aggregate computed from them, are what rules, the response and the archive see, so an exempted entry is absent from the match detail of the transaction.

## Fields

A request for an exemption carries the following:

| Value                  | Description                                                                                       | Example                         |
|------------------------|---------------------------------------------------------------------------------------------------|---------------------------------|
| ScreenedName           | The name as screened, compared in the manner that the matcher compares it.                        | Ivan Petrov                     |
| SanctionEntrySourceId  | The identifier of the sanction list (the source) holding the entry.                               | 1                               |
| SanctionEntryReference | The reference of the entry within that list.                                                      | 12345                           |
| Reason                 | Why the customer is judged not to be the listed party. Required, and at most 1,000 characters.    | Date of birth differs, verified |
| ExpiryDate             | Optional. The date on which the exemption lapses.                                                 | 2027-01-01T00:00:00Z            |

An approval or a rejection carries the Id of the exemption and, optionally, a Note of at most 1,000 characters, which is recorded with the decision.

## Programming interface

| Operation                                  | Needs                                       |
|--------------------------------------------|---------------------------------------------|
| `GET /api/SanctionExemption?stateId=`      | Sanctions or approval permission            |
| `POST /api/SanctionExemption/Request`      | Sanctions permission (29)                   |
| `POST /api/SanctionExemption/Approve`      | Approval permission (44), not the requester |
| `POST /api/SanctionExemption/Reject`       | Approval permission (44), not the requester |
| `POST /api/SanctionExemption/Retract/{id}` | The requester, or the approval permission   |

## Example

A request for an exemption, made by a user holding permission 29:

```http
POST https://localhost:5001/api/SanctionExemption/Request
Content-Type: application/json

{
  "screenedName": "Ivan Petrov",
  "sanctionEntrySourceId": 1,
  "sanctionEntryReference": "12345",
  "reason": "Date of birth differs, verified against passport",
  "expiryDate": "2027-01-01T00:00:00Z"
}
```

The approval, made by a different user holding permission 44:

```http
POST https://localhost:5001/api/SanctionExemption/Approve
Content-Type: application/json

{
  "id": 1,
  "note": "Evidence reviewed"
}
```

## Permissions and security

Requesting an exemption requires permission 29, View Sanction, and approving or rejecting one requires permission 44, Allow Approval, with the further condition that the approver is not the requester. Either permission allows the reading of exemptions. An operation that conflicts with the state of the exemption is refused with a 409. Exemptions are scoped to the tenant of the caller.
