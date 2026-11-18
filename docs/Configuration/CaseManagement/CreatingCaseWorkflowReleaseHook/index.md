---
layout: default
title: Case Workflow Release Hook
nav_order: 14
parent: Case Management
grand_parent: Configuration
---

# Case Workflow Release Hook

A [Response Elevation Range](../../Models/ResponseElevationRanges/index.html) set to Hold stamps a hold on the transaction. The payment is then released or refused from the Decide pane of the case page, and a Release Hook is the web hook that the release calls in the payment system. Release hooks belong to a Case Workflow, and, because release means different things to different source systems, a Case Workflow may have several, each chosen by a field of the payload of the held transaction.

## Where it lives

Navigate through the menu to Models, open the model, then the Cases Workflows hub card, open the Case Workflow, then the Release hooks hub card. Creating, changing and deleting a Release Hook follows the common manner described in [Child Objects](../../../Navigation/ChildObjects/index.html), with the exception that the children of a Case Workflow are not subject to approval, and a change takes effect upon being saved.

## Fields

The fields are arranged in the groups Identity, Source, Release and Credential, the latter being collapsed at first.

| Value               | Description                                                                                                                                                                                                                         | Example                                                    |
|---------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------|
| Name                | Required, up to 256 characters, unique within the Case Workflow without regard to case. Shown to the analyst who releases.                                                                                                          | Faster payments release                                    |
| Active              | Only active hooks are used.                                                                                                                                                                                                         | True                                                       |
| Locked              | A locked hook cannot be changed or deleted.                                                                                                                                                                                         | False                                                      |
| Source field        | Optional. The field of the payload by which the hook is chosen.                                                                                                                                                                     | SourceSystem                                               |
| Source value        | The value of the Source field for which the hook handles a transaction. Required when a Source field is given, and not otherwise permitted.                                                                                         | FasterPayments                                             |
| Release URL         | Required. The address called to release.                                                                                                                                                                                            | https://payments.example.com/release/[@Payload.PaymentId@] |
| Refuse URL          | Optional, under the same rules as the Release URL. When blank, refusing is recorded without calling anything.                                                                                                                       |                                                            |
| Http endpoint type  | POST or GET.                                                                                                                                                                                                                        | POST                                                       |
| Body                | Optional, POST only. A template for the body of the request.                                                                                                                                                                        |                                                            |
| Header name         | Optional. The header that carries the credential.                                                                                                                                                                                   | Authorization                                              |
| Credential variable | Optional. The name of an Environment Variable holding the credential, which must start with `JUBE_HOOK_`. The Header name and Credential variable are set together or not at all.                                                  | JUBE_HOOK_PAYMENTS                                         |

### Choosing the hook

The hook handles a transaction when the payload field named by Source field equals the Source value, without regard to case. A hook with no Source field handles every transaction that no more specific hook handles.

### The address and its tokens

In the Release URL and Refuse URL the scheme, host and port are written out literally. The tokens `[@Payload.FieldName@]`, `[@Hold.Name@]`, `[@Hold.Elevation@]` and `[@Hold.EntryGuid@]` are allowed only in the path and query, and every value is URL encoded, so a value can never add a path segment, a parameter or change the host. No other token is accepted.

The address is checked against the [Outbound Host Policy](../../Administration/OutboundHostPolicy/index.html): loopback, private, link-local (cloud metadata) and other internal addresses, and any scheme other than http or https, are refused when saved and again when called, redirects are never followed, and a trusted internal receiver must be listed in the `JUBE_HOOK_ALLOWED_HOSTS` or `JUBE_HOOK_ALLOWED_CIDRS` Environment Variables.

### The body

The Body is a template that may use the tokens of the address and also `[@Decision.Action@]` (release or refuse), `[@Decision.Reason@]` and `[@Decision.User@]`. Every value is escaped for JSON. When the Body is blank a fixed JSON body is sent with the action, the transaction, the case, the range, the elevation, the reason and the user.

### The credential

The credential is never stored with the hook, being read from the Environment Variable named at the time of the call, and a hook whose variable is not set is not called.

## What a release does

Releasing needs a reason. The case must not be locked by another user, and the payment must be Held. Jube then calls the hook that handles the transaction:

* Only an answer of exactly 200 records the payment as Released, with who, when, why, the hook and the status, in the `hold` element of the archived transaction. Any other answer, a refused address or a missing credential leaves the payment Held and records the failed attempt.
* Two analysts releasing at once are serialised: the second finds the payment already released and nothing is called twice.
* Every call, successful or not, is also recorded in the Messages section of the case page (the dispatch audit) as a message of kind Release hook, without the credential.
* Refusing records Refused in the same way, calling the Refuse URL first when the hook has one.

The Held payment tile appears in the Decide pane only while a payment is held on the case. Where no active release hook of the case workflow handles the payment, this is stated, and the payment cannot be released, although refusing is still possible. The journal shows the state of the payment beside the elevation, and the transaction drawer shows the hold, the decision and every message sent about it. The journal itself cannot change a hold.

{% comment %}SHOT id=case-workflow-release-hook-held-payment
     route=/cases
     steps=open-demo-case, open-decide-tile:Held payment
     frame=dialog
     state=the Held payment dialog of a demonstration case with one payment Held, showing the transaction, the elevation range, the hook that handles it, the reason box and the controls to release or refuse
     conveys=a held payment is released or refused from the case, and the release calls the hook chosen for the transaction
     alt=The Held payment dialog in the case page listing a held payment with the release hook that handles it and a reason to be entered before releasing or refusing{% endcomment %}

## Validation

Validation takes place in the server. A Release Hook is refused where:

* The Name is empty, longer than 256 characters, or already in use within the Case Workflow, without regard to case.
* The Source field is not a plain field name, which is to say a letter, digit or underscore followed by up to 127 letters, digits, underscores, spaces, full stops, colons or hyphens.
* The Source field is given without a Source value, the Source value is given without a Source field, or the Source value is longer than 200 characters.
* The Release URL is empty, or the Release URL or Refuse URL is not a well formed address, uses a token other than those listed or a token outside the path and query, or names a host refused by the Outbound Host Policy.
* The Http endpoint type is neither POST nor GET.
* The Body is longer than 8,000 characters, uses a token other than those listed, or is given for a GET.
* The Header name is given without the Credential variable or the reverse, the Header name is not a valid header name, or the Credential variable does not start with `JUBE_HOOK_`.

The messages returned are as follows, every problem being reported at once against the field to which it belongs:

| Condition                                                | Message                                                                                                                                                         |
|----------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Name empty                                               | A name is required.                                                                                                                                             |
| Name longer than 256 characters                          | The name must be 256 characters or fewer.                                                                                                                       |
| Name already in use in the Case Workflow                 | A release hook with this name already exists in the case workflow.                                                                                              |
| Source field not a plain field name                      | The source field must be a plain payload field name.                                                                                                            |
| Source field without a Source value                      | A source value is required when a source field is given.                                                                                                        |
| Source value without a Source field                      | A source value needs a source field.                                                                                                                            |
| Source value longer than 200 characters                  | The source value must be 200 characters or fewer.                                                                                                               |
| Release URL empty                                        | A release URL is required.                                                                                                                                      |
| Release URL or Refuse URL not well formed                | The release URL must be an absolute http or https URL whose scheme, host and port are written out literally, with tokens only in the path and query. The same is stated of the refuse URL. |
| Release URL or Refuse URL refused by the Outbound Host Policy | The release URL is blocked by the outbound host policy: it names a loopback, link-local, metadata, private, shared, unique-local or reserved address, localhost, or a scheme other than http or https. List a trusted internal receiver in JUBE_HOOK_ALLOWED_HOSTS or JUBE_HOOK_ALLOWED_CIDRS. The same is stated of the refuse URL. |
| A token other than those listed                          | The token Case.Id is not one this release hook can use.                                                                                                         |
| Http endpoint type neither POST nor GET                  | The method must be POST or GET.                                                                                                                                 |
| Body longer than 8,000 characters                        | The body must be 8000 characters or fewer.                                                                                                                      |
| Body given for a GET                                     | A body can only be sent with POST.                                                                                                                              |
| Credential variable without a Header name                | The credential header name is required when a credential variable is given.                                                                                     |
| Header name without a Credential variable                | The credential variable is required when a credential header name is given.                                                                                     |
| Header name not valid                                    | The credential header name must be a plain header name and not a reserved one.                                                                                  |
| Credential variable of the wrong form                    | The credential variable must be an environment variable name that starts with JUBE_HOOK_ followed by capital letters, digits or underscores.                    |
| Parent Case Workflow missing                             | The case workflow was not found.                                                                                                                                |

Keep in mind that a token which is not accepted in the Refuse URL is reported against the Release URL, both addresses being checked together. For a new hook Active is off and the Http endpoint type is POST.

## Example

A hook for payments originating from a Faster Payments source system, releasing by POST with a credential, would be completed as follows:

| Value               | Example                                                    |
|---------------------|------------------------------------------------------------|
| Name                | Faster payments release                                    |
| Active              | True                                                       |
| Source field        | SourceSystem                                               |
| Source value        | FasterPayments                                             |
| Release URL         | https://payments.example.com/release/[@Payload.PaymentId@] |
| Refuse URL          | https://payments.example.com/refuse/[@Payload.PaymentId@]  |
| Http endpoint type  | POST                                                       |
| Header name         | Authorization                                              |
| Credential variable | JUBE_HOOK_PAYMENTS                                         |

With a Body as follows, keeping in mind that each value is escaped for JSON:

```json
{
  "action": "[@Decision.Action@]",
  "reason": "[@Decision.Reason@]",
  "user": "[@Decision.User@]",
  "range": "[@Hold.Name@]",
  "elevation": "[@Hold.Elevation@]",
  "entry": "[@Hold.EntryGuid@]"
}
```

The Environment Variable JUBE_HOOK_PAYMENTS is then set, in the environment of the application, to the value to be sent in the Authorization header.

## Permissions and security

Creating, changing and deleting a Release Hook requires permission 69, Author Case Workflow Release Hooks. Releasing or refusing a held payment in the case page requires permission 68, Release Held Payments.

A Release Hook does not have an allocation of roles of its own, hence it is available for every case of its Case Workflow that the user is otherwise able to work.
