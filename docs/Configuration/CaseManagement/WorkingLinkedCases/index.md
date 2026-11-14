---
layout: default
title: Working Linked Cases
nav_order: 31
parent: Case Management
grand_parent: Configuration
---

# Working Linked Cases

Two cases are often one story: two accounts that received money from the same scam, a customer and the business they
control. Cases are grouped by their key, so cases on different keys are not related unless an analyst says so. A link
records
that.

On the case page, under the tags, the Linked cases box lists the cases linked to this one with their key value, whether
they
are closed and the reason given. To add a link, type the other case's number, optionally say why, and choose Link. To
remove one,
choose the cross beside it. A link is symmetrical: it appears on both cases, and removing it from either side removes it
from both.

## Who sees what

Linking and viewing links needs the Read Write Case permission. Creating a link needs both cases to be visible to you,
meaning your
role is granted the workflow and the current status of each. A link made by someone with wider access is shown to you
only
for the cases you can see; the existence and key of a case you cannot see are never revealed through a link.

The programming interface is `GET /api/CaseLink/ByCaseId/{caseId}`, `POST /api/CaseLink/Link` and
`POST /api/CaseLink/Unlink`, each taking the
two case numbers; a case cannot be linked to itself, a pair can only be linked once, and a reason is at most 500
characters.

<!-- TODO(docs): linking writes no CaseEvent, so the case timeline does not show it; add event types and journal labels. -->
<!-- TODO(docs): a link is not carried into the Case Key Journal, the graph or the case risk; consider treating linked cases as one network for the graph and for ranking. -->
<!-- TODO(docs): Preservation does not move links between environments, which is right for case data but worth stating once Preservation of case data is decided. -->
<!-- TODO(docs): opening a case without an alert (an ad hoc investigation) is not built; a case can only be created by a rule or by the existing case creation from a key value. -->
<!-- TODO(docs): links are not agent tools; decide whether AskJooby should be able to propose one. -->
<!-- TODO(screenshot): file=case-linked-cases.png route=/cases/{id} state="the Linked cases box with one closed linked case and the add row" -->
