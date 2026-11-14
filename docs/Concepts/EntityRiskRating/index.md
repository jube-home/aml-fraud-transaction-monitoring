---
layout: default
title: Entity Risk Rating
nav_order: 18
parent: Concepts
---

# Entity Risk Rating

Jube is not the customer master, so it does not decide who a customer is. It can, however, hold the risk rating of an
entity key (an account, a customer id, a counterparty) that the institution supplies or an analyst sets, keep every
change, and remind the right people when a rating is due a review.

## What is stored

A rating belongs to a tenant and a key: a key name (for example `Account`) and a key value, written exactly as it is
stored against transactions and cases, so the ciphertext for an encrypted key. It has a tier (1 Low, 2 Medium, 3 High),
an optional numeric score, a source (1 Imported, 2 Set by hand), an optional reason, and an optional review-by date.

The table (`EntityRiskRating`) is append only. Each change is a new version, starting at 1, and the highest version is
current, so the history of a rating is the table itself. Nothing is edited or deleted in place.

## Getting ratings in

| Call                                              | Permission                                               | What it does                                                                                                                                                                                                                                                                                                       |
|---------------------------------------------------|----------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `POST /api/EntityRiskRating/Import`               | Read Write Case Workflow and Approve Entity Risk Ratings | Up to 500 ratings. Each is checked on its own; a rating that equals the current one is skipped as unchanged, otherwise it is written as a new version with source Imported. The reply gives how many were written, how many were unchanged, and why each refused rating was refused, by its position in the batch. |
| `POST /api/EntityRiskRating/Set`                  | Read Write Case Workflow                                 | Proposes a manual rating; a reason is required. Nothing changes until a second person approves.                                                                                                                                                                                                                    |
| `GET /api/EntityRiskRating/Requests`              | Read Write Case                                          | The pending proposals.                                                                                                                                                                                                                                                                                             |
| `POST /api/EntityRiskRating/Approve` and `Reject` | Approve Entity Risk Ratings                              | Approve writes the version; reject needs a reason. Never your own proposal.                                                                                                                                                                                                                                        |
| `POST /api/EntityRiskRating/Retract/{guid}`       | Read Write Case Workflow                                 | The proposer withdraws a pending proposal.                                                                                                                                                                                                                                                                         |

The version, source and user are always set by the server; anything sent for them is ignored.

## Reading them

`GET /api/EntityRiskRating/ByKey?keyName=...&keyValue=...` (Read Write Case permission) returns the current rating and
up to 50 versions, newest first. `GET /api/EntityRiskRating/Due` returns the current ratings whose review-by date has
passed, oldest first. A key that has never been rated returns no current rating, which is not the same as the lowest
tier.

## Reviews that fall due

Set `EnableEntityRiskReview` to `True` and, once a day (`WaitEntityRiskReview`), every user holding the Read Write Case
Workflow permission in a tenant with ratings past their review-by date receives one notification giving how many are
due. A rating stops being due when a newer version with a later (or no) review-by date is written.

## The page

Administration > Entity risk ratings looks up the rating of a key, shows who set the current tier and why, and, for a
user holding the Read Write Case Workflow permission, proposes a change with a tier, an optional score and review date,
and the required reason. Changes waiting for approval are listed below it: a different user holding Approve Entity Risk
Ratings sees Approve and Reject (reject needs a reason) and the proposer sees Retract. The ratings past their review
date are listed at the bottom.

<!-- TODO(screenshot): The Entity risk ratings page with a pending proposal. -->

## Approving a manual change

A rating set by hand is a proposal, not a change. `POST /api/EntityRiskRating/Set` (workflow permission 18) records a
**pending request** with a required reason, and the rating is untouched. A different person holding **Approve Entity
Risk Ratings** (permission 80) approves it with `POST /api/EntityRiskRating/Approve`, which writes the new version, or
rejects it with a reason with `POST /api/EntityRiskRating/Reject`. `GET /api/EntityRiskRating/Requests` lists what is
waiting. The proposer can withdraw their own request with `POST /api/EntityRiskRating/Retract/{guid}` but can never
decide it. Only one request per key can be pending, and an approval is refused if the rating changed since the proposal
(an import, say), so nobody approves a change against a rating they have not seen. The version written records the
proposer as its maker and the request records the approver. Imported ratings are institution-supplied and do not need a
second approver, but because an import writes ratings directly the caller must hold Approve Entity Risk Ratings as well,
so the approval step cannot be bypassed through the import. Approving, rejecting and retracting a request and writing
the rating happen in one database transaction.

<!-- TODO(screenshot): The notification bell showing the entity risk reviews due message. -->

<!-- TODO(docs): not built: a score computed from case outcomes, the key's graph attributes and rule activations; using the rating inside detection rules (an EntityRisk payload root) and in the case risk calculation; raising a review case and a tier-change notification or webhook; and a per-entity panel on the Case Key Journal. -->
