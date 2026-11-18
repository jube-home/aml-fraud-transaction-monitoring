---
layout: default
title: Working Activations
nav_order: 17
parent: Case Management
grand_parent: Configuration
---

# Working Activations

At the point a case is created a copy of the event is stored alongside the case record. In this event payload there is a list of the Activation Rules that were matched upon, stored in their order of priority and match. The activations are written out in a convenient form representing the one to many relationship that exists between transaction or event (one) and Activation (many), insofar as one transaction or event may have many matches.

## Where it lives

Navigate to a case record from the Cases page, as described in [Navigating Case Record](../WorkingNavigatingCaseRecord/index.html). The Activations tab of earlier versions no longer exists, as the activations have moved under the Journal section of the case page, where they are presented as the Activations band of the [Case Key Journal](../WorkingCaseKeyJournal/index.html), above the grid of transactions.

The move brings with it a widening of what is shown. Whereas the tab wrote out only the activations of the event that created the case, the Activations band presents a card for each rule or model that fired across the whole of the history in the scope of the journal, taken from the payload of each transaction in the Archive. The activations of the case itself remain the reason the case is in the queue, and the band can be narrowed to them with This case's activations.

{% comment %}SHOT id=case-management-working-activations-band
     route=/cases
     steps=open-demo-case, click:Hide the sections, click:Hide the decide panel, open-section:3 Journal, scroll-to:Activations, click:This case's activations
     frame=section:Activations
     state=the Activations band of the Journal section narrowed to This case's activations, showing a card for a rule and a card for a model, each stating the number of transactions it fired on
     conveys=the activations that created the case are cards under the Journal, alongside the wider history of the key
     alt=The Activations band of the Journal narrowed to the activations of the case, with a card for a rule and a card for a model score{% endcomment %}

## Behaviour

The cards of the band, the What fired and Model facts drawers, and the Rules lens of the grid are described in [Working Case Key Journal](../WorkingCaseKeyJournal/index.html). The band can be sorted by Score, Times fired, Most recent or Name.

An activation, put simply, is one rule or one model having matched one transaction, and the question the band answers is which rules and models have been reacting to this key, how often, and how strongly. Each card stands for one rule or one model and is read as follows:

| Part of the card         | Meaning                                                                                                                                                   |
|--------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| The name, and Rule       | The Activation Rule or the model. A card for a rule is marked Rule.                                                                                       |
| Fired on 3 transactions  | The number of transactions in the scope of the journal on which it matched.                                                                               |
| Anchor, and a date       | The most recent transaction on which it matched, upon which the card is anchored. Selecting it opens that transaction.                                    |
| The inputs, for a rule   | The values the rule read on the anchor transaction. Where none were recorded the card states No inputs were recorded for this rule.                       |
| The gauge, for a model   | The score against its threshold, followed by the largest contributions to the score.                                                                      |
| A band, for a model      | Unlikely, Possible, Likely or Very likely, shown only for a score that is a calibrated probability, with what that band has meant among validated cases.  |
| Forced                   | The match was recorded as forced, as described in [Working Case Suppression](../WorkingCaseSuppression/index.html).                                       |
| Shadow                   | The match was recorded as a shadow activation, which is to say it was noted without taking effect.                                                        |
| 2 inputs hidden          | Inputs that the roles of the user are not permitted to see, counted and not named.                                                                        |
| Details, and Model facts | Open the What fired and Model facts drawers for the anchor transaction, Model facts being offered on a card for a model only.                             |

The band opens sorted by Score, and neither the order chosen nor This case's activations outlasts the visit to the case, other than as part of the address of the page. While the cards are being read placeholders are drawn. Where nothing fired, the band states Nothing fired in this view, and with This case's activations on, None of this case's activations fired in this view. Where the cards cannot be read it states The activations could not be loaded, with Try again beside it. Where the scope holds more transactions than were read, the band states The view holds more transactions than were read, so the cards cover the newest ones only.

By way of illustration, a card reading Fired on 11 transactions for a rule concerning cash deposits, on the demonstration case on ACC-441, would say that the rule has matched eleven times within the window and not once, which is a different matter from a single match and is the reason the band looks across the history and not only at the transaction that raised the case.

The order in which the Activation Rules are matched, and thus the order in which they are stored in the payload, follows the Activation Rule processing. Activation Rule processing, and thus priority of Activation, is on the basis of:

* Enabled For Response Elevation and;
* Highest Response Elevation First.
* Enabled for Case Management and;
* Highest Priority Status First.
* Enabled for Notifications.
* Enabled for TTL Counter Increment.
* Alphabetically on Activation Rule Name.

It follows, that on an Activation Rule matching, the order of rules matching will respect the above.

## Visibility

Activations will only be visible if the Visible flag has been set in the [Activation Rule](../../Models/BasicActivationRules/index.html). Visible is a switch in the Output and Sampling group of the Activation Rule, and it is on for a new Activation Rule. It is typically switched off for Activation Rules that exist only to increment a TTL Counter, which would otherwise clutter the journal. An activation that is not visible is not presented in the journal, and is not counted in the Activations column of the grid.

## Permissions and security

The activations are part of the case page and require permission 1, Read Write Case. The inputs of a rule or model that the roles of the user are not permitted to see are counted and never named.
