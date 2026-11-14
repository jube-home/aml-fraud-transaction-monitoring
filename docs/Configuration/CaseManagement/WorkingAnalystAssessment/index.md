---
layout: default
title: Working Analyst Assessment
nav_order: 37
parent: Case Management
grand_parent: Configuration
---

# Working Analyst Assessment

The likelihood on a case is computed from evidence. An analyst who has read the case may see it differently, and that
difference is worth keeping. The Your assessment panel on the case page lets an analyst record their own view of how
likely the case is to be confirmed: Low, Medium or High, with a reason.

The view is kept beside the computed likelihood. It never replaces the computed value, it never changes the priority,
the lane or the order of the case in a queue, and it is not used to train a model. Only the outcome a case is closed
with does that.

## Recording a view

The panel is at the foot of the Case detail views section.

1. Choose Low, Medium or High.
2. Say why. A reason is required and may be up to 1,000 characters.
3. Choose Record assessment.

The view then shows in the panel with who recorded it and when, the reason, and the computed likelihood that was in
force at that moment. The Likelihood figure at the top of the section gains a line, for example "Analyst view: High",
so the two can be read together.

Recording another view makes the new one current. The earlier ones stay listed under Earlier views.

## Withdrawing a view

The analyst who recorded a view can withdraw it with Withdraw, for example after recording it on the wrong case. A
withdrawn view stays in Earlier views, marked Withdrawn, and the view before it becomes current again. Nobody can
withdraw a view recorded by someone else; they record their own instead.

## In the Events timeline

Recording a view writes an Analyst assessment recorded event to the case, showing the view that stood before and the
new one. Withdrawing writes an Analyst assessment withdrawn event showing the view withdrawn. The events hold the views
only; the reason stays in the panel.

## Who sees what

Recording, withdrawing and reading views need the Read Write Case permission, and the case must be visible to you.
Every analyst who can see the case sees every view recorded on it.

The programming interface is `GET /api/CaseAnalystAssessment/ByCaseId/{caseId}`,
`POST /api/CaseAnalystAssessment/Record` and `POST /api/CaseAnalystAssessment/Withdraw`.

## What is stored

Each view is a row of `CaseAnalystAssessment` holding the case, the view, the reason, the analyst, the time, and the
sequence and value of the computed likelihood in force when it was recorded. Rows are never changed except to mark a
withdrawal, so the views can later be compared with how the cases were decided.

<!-- TODO(docs): the comparison of analyst views with outcomes, and with the computed likelihood, is recorded but not yet reported anywhere. -->
<!-- TODO(docs): a view does not appear in the Cases page columns or in exports. -->
<!-- TODO(docs): AskJooby has no tool to read or record a view. -->
<!-- TODO(screenshot): file=case-analyst-assessment.png route=/cases/{id} state="the Your assessment panel with a High view recorded, its reason, the computed likelihood at the time and one earlier withdrawn view" -->
