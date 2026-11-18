---
layout: default
title: Working Case Rating
nav_order: 16
parent: Case Management
grand_parent: Configuration
---

# Working Case Rating

To obtain case quality feedback from the end user there exists a star rating which is intended to capture the subjective judgement as to the quality of the case. Case evaluation is a grading rather than a strict binary outcome.

Suppose that the quality of a case, which is to say the rules or machine learning that caused the case record creation, is of reasonable (but not exceptional) quality, then it would be judged to be four stars.

## Where it lives

The rating is a property of the case record. In earlier versions the rating was presented as a row of stars in the status bar of the case page, and was set by floating over the stars and clicking to commit.

The case page does not, at present, offer a control for the rating. The Status tile of the Decide pane, described in [Working Case Status](../WorkingCaseStatus/index.html), opens a lightbox titled Status, which is concerned with the status alone and contains no control for the rating. It follows that the rating is set only through the API, as an update of the case record, or by Ask Jooby through the operation CaseUpdate.

{% comment %}TODO(verify): the Status and rating lightbox has no rating control on this branch (Jube.App/Vue/src/cases/detail/DecideStatusDialog.vue); confirm with the owner whether the star rating is to be restored there, and document the control once it exists{% endcomment %}

## Behaviour

The rating is a whole number, and a case is created with a rating of zero, which is to say unrated. Upon the rating being changed, the case record is updated and the change is recorded in the events of the case as Rating changed, with the rating before and the rating after, as described in [Working Case Event Journal](../WorkingCaseEventJournal/index.html).

The rating is a judgement of the quality of the case, and should not be confused with the view of an analyst as to how likely a case is to be confirmed, which is recorded separately as described in [Working Analyst Assessment](../WorkingAnalystAssessment/index.html).

## Validation

The rating must be between 0 and 5 inclusive, otherwise the update is refused with the error code `RatingInclusiveBetween`. As with any other change to the case record, the update is refused where the case is locked to another user.

## Example

The rating is set by reading the case record and returning it with the rating changed:

```http
GET /api/Case/1234
```

```http
PUT /api/Case
Content-Type: application/json

{
  "id": 1234,
  "caseWorkflowStatusGuid": "9b2d7c41-0000-0000-0000-000000000000",
  "closedStatusId": 0,
  "locked": true,
  "lockedUser": "Robert",
  "diary": false,
  "diaryDate": null,
  "rating": 4
}
```

The body is abbreviated to the properties of concern. The whole of the record returned by the GET should be sent back, as the update takes the closed status, the lock, the diary and the status from the same body.

## Permissions and security

Reading and updating the case record requires permission 1, Read Write Case.
