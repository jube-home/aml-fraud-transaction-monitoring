---
layout: default
title: Case Workflow Release Hook
nav_order: 14
parent: Case Management
grand_parent: Configuration
---

# Case Workflow Release Hook
A [Response Elevation Range](../../Models/ResponseElevationRanges/) set to Hold stamps a hold on the transaction. The payment is then released or refused from the Decide pane of the case page, and a Release Hook is the web hook that the release calls in the payment system. Release hooks belong to a Case Workflow, and, because release means different things to different source systems, a Case Workflow may have several, each chosen by a field of the held transaction's payload.

To create one, navigate to Models, then Case Workflows, open the Case Workflow and choose Release hooks. Authoring a release hook needs the Author Case Workflow Release Hooks permission.

| Parameter | Description |
|-----------|-------------|
| Name | Required, up to 256 characters, unique within the Case Workflow without regard to case. Shown to the analyst who releases. |
| Active | Only active hooks are used. |
| Locked | A locked hook cannot be changed or deleted. |
| Source field and source value | Optional. The hook handles a transaction when the payload field equals the value, ignoring case. A hook with no source field handles every transaction that no more specific hook handles. Both are set or neither. |
| Release URL | Required. The address called to release. The scheme, host and port are written out literally; the tokens `[@Payload.FieldName@]`, `[@Hold.Name@]`, `[@Hold.Elevation@]` and `[@Hold.EntryGuid@]` are allowed only in the path and query and every value is URL encoded, so a value can never add a path segment, a parameter or change the host. Only an answer of exactly 200 means released. The address is checked against the [Outbound Host Policy](../../Administration/OutboundHostPolicy/index.md): loopback, private, link-local (cloud metadata) and other internal addresses, and any scheme other than http or https, are refused when saved and again when called, redirects are never followed, and a trusted internal receiver must be listed in `JUBE_HOOK_ALLOWED_HOSTS` or `JUBE_HOOK_ALLOWED_CIDRS`. |
| Refuse URL | Optional, under the same rules. When blank, refusing is recorded without calling anything. |
| Method | POST or GET. |
| Body | Optional, POST only. A template that may also use `[@Decision.Action@]` (release or refuse), `[@Decision.Reason@]` and `[@Decision.User@]`. Every value is escaped for JSON. When blank a fixed JSON body is sent with the action, the transaction, the case, the range, the elevation, the reason and the user. |
| Credential header and variable | Optional, set together. The header that carries the credential and the name of an environment variable, which must start with `JUBE_HOOK_`, that holds it. The credential is never stored, and a hook whose variable is not set is not called. |

## What a release does
Releasing needs the Release Held Payments permission and a reason. The case must not be locked by another user, and the payment must be Held. Jube then calls the hook that handles the transaction:

* Only an answer of exactly 200 records the payment as Released, with who, when, why, the hook and the status, in the `hold` element of the archived transaction. Any other answer, a refused address or a missing credential leaves the payment Held and records the failed attempt.
* Two analysts releasing at once are serialised: the second finds the payment already released and nothing is called twice.
* Every call, successful or not, is also recorded in the case's messages (the dispatch audit) as a Release hook message, without the credential.
* Refusing records Refused in the same way, calling the Refuse URL first when the hook has one.

The Held payment tile appears in the Decide pane only while a payment is held on the case. The journal shows the state of the payment beside the elevation, and the transaction drawer shows the hold, the decision and every message sent about it. The journal itself cannot change a hold.
