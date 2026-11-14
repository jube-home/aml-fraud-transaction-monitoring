---
layout: default
title: Model Governance and Model Cards
nav_order: 18
parent: Concepts
---

# Model Governance and Model Cards

A model that decides things about people needs an owner, a stated purpose, a view on how risky it is and a description
of how a person oversees it. Jube keeps that as a **governance statement** per model, lists every model with the state
of its statement in an **inventory**, and produces a **model card**, one self-contained page a reviewer or auditor can
keep.

## The governance statement

A statement has: the accountable **owner**, the **intended purpose** (what the model is for and the decisions it
informs), a **risk tier** (Low, Medium or High), the **human oversight** (how a person reviews or can override what it
does) and an optional **next review date**. All four text fields are required; the owner is at most 200 characters and
the two statements at most 2,000.

It is append only. Every save is a new version, the highest is current, and the earlier versions stay with who saved
them and when. Two statements saved at the same moment cannot overwrite each other: the second save is refused and asked
to try again.

It is kept for two kinds of model: an **entity analysis model** (the model that holds the rules) and an **Exhaustive
adaptation**. The permission is Read Write Model, for reading and for saving alike.

## The inventory

`GET /api/ModelGovernance` lists every model in the tenant, whether or not it has a statement, with its owner, tier,
next review date and a review status:

| Status       | Meaning                                                                      |
|--------------|------------------------------------------------------------------------------|
| Not recorded | No statement has been saved.                                                 |
| Current      | A statement exists and its review is more than 30 days away, or has no date. |
| Due soon     | The next review date is within 30 days.                                      |
| Overdue      | The next review date has passed.                                             |

The Model governance page (Administration) shows the inventory, lets you open and save a statement, and downloads the
card.

## The model card

`GET /api/ModelGovernance/Card?kindId=1&modelGuid=...` returns the card as one HTML file, with its styles and its charts
inline and no scripts, so it opens anywhere, prints well and follows the reader's light or dark setting. It holds the
model's name and identity, the current statement and which version of how many it is, and, for an entity analysis model,
the drift history: a chart of the population stability index for each score against the 0.1 and 0.25 thresholds, and the
latest checks as a table. For an Exhaustive adaptation it holds the best score, the number of models tried and when
training completed. Every value is escaped, so nothing a user typed into a statement or a name can run in the card.

The card is generated on request from what is stored; nothing about it is stored. It is a snapshot, so it records the
date and the user who made it.

## What it is not

<!-- TODO(docs): not built: a governance statement for Advanced Analytics models; approval of a statement by a second person; the baseline score distribution stored at promotion, input-variable drift and AUC or calibration against outcomes on the card; the promotion evidence and approver on the card; and a PDF form of the card, which the browser's print dialog gives. -->

A statement is a record, not a control: saving one does not stop a model from running, and an overdue review does not
switch it off.

<!-- TODO(screenshot): The Model governance page: the inventory with its review status, and a statement being edited. -->
<!-- TODO(screenshot): A model card, light and dark. -->
