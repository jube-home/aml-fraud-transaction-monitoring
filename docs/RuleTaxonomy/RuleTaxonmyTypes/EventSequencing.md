---
layout: default
title: Event Sequencing
nav_order: 5
parent: Rule Taxonomy
---

# Event Sequencing

**Definition:** a chain of states or events that matters only together, not individually, within one model or across
linked models. It enforces a *one after another* approach.

**Construction:** within a model, a `Dictionary` (KVP) holds the last state and is compared to the current `Payload`.
Across models, a TTL Counter is incremented in one model and read in a linked model through a common key, which is
possible because a TTL Counter can be incremented across models. Lifecycle chains, channel hopping and takeover chains are
domain dressing on this one mechanism, and are not separate classes. See the [Rule Taxonomy](../RuleTaxonomy.html).

## Event Sequencing: Priority Enforces One After Another

Event Sequencing reuses the TTL Counter mechanism of [Velocity and Volume](VelocityAndVolume.html), and priority is
decisive. Counters are updated inside the invocation as each incrementing Activation Rule matches, and Activation Rules
are evaluated in ascending Priority order (a lower number is evaluated first). For a genuine one-after sequence the rule
that tests the chain must be evaluated **before** the rule that increments the step counter for the current event, so it
has a high order (evaluated early) Priority. Otherwise the current event counts as its own predecessor and the sequence
is satisfied by a single event.

## Event Sequencing: Cross-Model Example

Alert when a customer has more than three failed logins, then adds a new beneficiary, then makes a financial transaction
over a certain amount. Each step is a different model, joined by a common key such as `CustomerId`, and each step is
armed only when the step before it has happened.

| Step | Model                 | Rule that increments the counter                                                    | Counter incremented (defined in the model that reads it) |
|------|-----------------------|-------------------------------------------------------------------------------------|----------------------------------------------------------|
| 1    | Login model           | Failed login: `Matched = Payload.LoginResult.MatchEqual("Failed")`                            | `FailedLogins` in the beneficiary model                  |
| 2    | Beneficiary model     | New beneficiary, gated on step 1: `Matched = TTLCounter.FailedLogins.RequireGreater(3).Against(Payload.IsNewBeneficiary).MatchIsTrue()`  | `BeneficiaryAfterFailedLogins` in the transaction model |
| 3    | Transaction model     | Alert, gated on step 2: `Matched = TTLCounter.BeneficiaryAfterFailedLogins.RequireGreater(0).Against(Payload.CurrencyAmount).MatchGreater(5000)` | None. This is the alerting Activation Rule.              |

The gate in each step is what makes it a sequence rather than three independent counts: the step 2 counter cannot
increment unless the step 1 counter had already crossed its threshold, and the step 3 alert cannot fire unless step 2 had
incremented. The TTL Counter interval on each counter bounds how long the chain may take, for example `Hours` / `24`.

## Event Sequencing: Construction Steps

Define a TTL Counter per link in the chain, in the model that will read it, keyed on the common entity. Increment each
counter from an Activation Rule in the model where that event occurs, choosing the target model in the Increment TTL
Counter options (see
[TTL Counter Activation Rule Incrementation](../../Configuration/Models/TTLCounterActivationRuleIncrementation/index.html)).
Gate each incrementing rule on the previous link's counter. Set Priority so that gates are evaluated before their own
increment. Threshold the last counter in the alerting rule. The common key must be present in the payload of every model
in the chain, otherwise the increment is skipped for that event.

## Event Sequencing: Within-Model Example

Within one model, a Dictionary KVP holding the last state for the entity is compared with the current payload, for
example alerting when a status moves from `Dormant` directly to a high value withdrawal:

```vb
Matched = Dictionary.LastAccountStatus.RequireIsZero().Against(Payload.CurrencyAmount).MatchGreater(5000)
```

A Dictionary is populated during model synchronisation from the user interface, a CSV upload, the database or the API. It
is not written by the invocation itself, so it holds state that something external maintains. Where the events themselves
must maintain the state, use the cross-model TTL Counter mechanism above. The value of a Dictionary lookup is a number, and is zero when the key is absent from the payload or the Dictionary, so
choose encodings where zero is the safe default. See [Dictionary](../../Configuration/Models/Dictionaries/index.html).

## Event Sequencing: Common Faults

Symptoms of a misbuilt Event Sequencing rule, with the likely cause.

| Symptom in an Event Sequencing rule           | Likely cause                                                                                                          |
|-----------------------------------------------|-----------------------------------------------------------------------------------------------------------------------|
| The alert fires on a single event             | The gate is evaluated after its own increment, so the event satisfies its own predecessor. Lower the gate's Priority number. |
| The alert never fires                         | The common key is missing from a model's payload, so the increment is skipped; or the window is shorter than the chain. |
| Steps fire in the wrong order                 | The increment for a later step is not gated on the earlier step's counter.                                             |
| Chain fires for unrelated customers           | The TTL Counter Data Name is not the shared entity key.                                                               |
