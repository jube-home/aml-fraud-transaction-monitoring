---
layout: default
title: Response Elevation Ranges
nav_order: 13
parent: Models
grand_parent: Configuration
---

# Response Elevation Ranges
The receiving system expects the Response Elevation as an integer, and an Activation Rule may only elevate the response to a level that a Response Elevation Range has been defined for. A Response Elevation Range names a band of levels and says what reaching it means: a decline, a hold, or neither.

To create a Response Elevation Range, navigate to Models, open the Model and choose Response Elevation Ranges:

| Parameter | Description |
|-----------|-------------|
| Name | Required, up to 256 characters and unique within the Model without regard to case. The Name is shown beside the integer level wherever the level is shown, and is how the range is picked in an Activation Rule. |
| From | The lowest integer level in the range, inclusive. Not negative. |
| To | The highest integer level in the range, inclusive. Not below From. Set equal to From for a single level. |
| Decline | The outcome of reaching this range is a decline. |
| Hold | The outcome of reaching this range is that the payment is held for review. A decline never holds, so Decline and Hold cannot both be on. |
| Content, Redirect, Fore colour, Back colour | The presentation sent with the response when the range is reached. Colours are six digit hex values such as `#A1B2C3`. |
| Web hook | An asynchronous web hook called when the range is reached: an absolute http or https URL and the method, POST or GET. The scheme, host and port are written out literally; the tokens `[@Payload.FieldName@]`, `[@Hold.Name@]`, `[@Hold.Elevation@]` and `[@Hold.EntryGuid@]` are allowed only in the path and query, and every value is URL encoded. The address is checked against the [Outbound Host Policy](../../Administration/OutboundHostPolicy/index.md): loopback, private, link-local (cloud metadata) and other internal addresses, and any scheme other than http or https, are refused when saved and again when called, redirects are never followed, and a trusted internal receiver must be listed in `JUBE_HOOK_ALLOWED_HOSTS` or `JUBE_HOOK_ALLOWED_CIDRS`. |
| Credential header and variable | Optional. The name of a request header, such as `Authorization` or `X-Api-Key`, and the name of an environment variable that starts with `JUBE_HOOK_` whose value is sent in it. Both are set or neither. The value is never stored in the database, never written to the Archive and never read from a variable without that prefix. |
| Notification | An asynchronous notification sent when the range is reached: Email or SMS, the destination, the subject and the body. |

Ranges of one Model must not overlap, and the message names the range that clashes. A range that is not Active is not sent out.

## Ranges and Activation Rules
The response elevation of an Activation Rule is picked from a list of the active ranges of the Model, each shown by Name and its levels, and the Activation Rule stores the integer. The rule cannot be saved with a level that is outside every active range. When the Model is synchronised the Engine applies the same test, so a rule whose level falls in no active range does not elevate the response and a warning is logged.

## What the Engine does when a range is reached
The highest level reached by a transaction resolves to exactly one range. The Name is added to the response beside the integer, and when the range is a Hold or a Decline the Engine stamps a `hold` element in the payload that is archived with the transaction, so everything about the state of a payment lives in the Archive:

| Element | Meaning |
|---------|---------|
| `stateId` | 1 Held, 2 Declined, 3 Released, 4 Refused. A Decline is stamped Declined and can never be released or refused. |
| `rangeId`, `rangeName`, `rangeMinimum`, `rangeMaximum`, `elevation` | The range that stamped it and the level reached. |
| `stampedDate` | When the hold was stamped. |
| `decision` | Who released or refused the payment, when, why, through which release hook and with which HTTP status. |
| `legs` | Every message sent about the hold, each with its kind (email, SMS, web hook, release hook, refuse hook), its outcome (queued, sent, failed, not sent), the time, the destination without its query string, the HTTP status and any error. |

A range that is neither a Hold nor a Decline names the elevation but stamps nothing. Reprocessing never stamps a hold and never changes one that exists: the existing `hold` element is carried over unchanged.

Every later change to the element, a message leg being recorded or a payment being released or refused, rewrites the archived payload, raises the version of the Archive row and keeps the earlier state in the Archive version table, which is the audit trail of the payment. The date of the transaction is not moved.

### Asynchronous messages
The Notification of a range is sent through the same path as every other notification. The Web hook has its own queue and sender, and chooses between the in-process queue and AMQP in exactly the way notifications do: where notifications go over AMQP, web hooks do too, on the `jubeWebhooks` queue. The size of the web hook queue is written with the other queue balances and appears as Webhook in the asynchronous queue balance. An address whose host is not written out literally, or that the Outbound Host Policy blocks, is refused, recorded as a Refused leg and never called. The outcome of each message is recorded in `legs` once the archived row exists; the Engine retries for about a minute while the Archive catches up.

The notifications on an Activation Rule are no longer read by the Engine; recreate them on a range.

## Releasing a held payment
A payment in the Held state is released or refused from the Decide pane of the case page, in the Held payment lightbox, by someone with the Release Held Payments permission. Both need a reason. The web hook that releases is configured in the [Case Workflow Release Hook](../../CaseManagement/CreatingCaseWorkflowReleaseHook/).

## Existing models
Response Elevation was a floating point number before ranges were introduced. The upgrade rounds each value to an integer and creates a single level range for every distinct rounded value in a Model, named `Level` followed by the integer, carrying the content, redirect and colours of the first Activation Rule that used it. Where two different values round to the same integer they share a range and the upgrade logs a notice, so those rules should be reviewed by hand. No range is a decline or a hold until it is set to be one.
