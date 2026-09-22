---
layout: default
title: Velocity and Volume
nav_order: 1
parent: Rule Taxonomy
---

# Velocity and Volume

**Definition:** raw frequency (velocity) or magnitude (volume) of events. *How often* or *how much* is the signal itself.

**Construction:** a TTL Counter, windowed, keyed on the entity, read by an Activation Rule and written by an Activation
Rule. See the [Rule Taxonomy](../RuleTaxonomy.html) for how this type differs from its neighbours, and the
[Rule Authoring Methodology](../RuleAuthoringMethodology.html) for how the rule fragment itself is written.

| Question an analyst asks                             | Rule Taxonomy type that answers it                |
|------------------------------------------------------|---------------------------------------------------|
| How many card payments in the last hour?             | Velocity (count)                                  |
| How much value moved out of the account in 24 hours? | Volume (sum)                                      |
| How many *different* devices in 7 days?              | Not this type: [Cardinality](Cardinality.html)    |
| Is this hour unusual *for this customer*?            | Not this type: [Behavioral Baseline and Ratio](BehavioralBaselineAndRatio.html) |
| Is the amount just under a reporting limit?          | Not this type: [Structuring](StructuringOrThresholdAvoidance.html), though it consumes a velocity counter for the repetition |

## Velocity and Volume: TTL Counters or Abstraction Rules

There are two ways to build a Velocity or Volume rule in Jube: TTL Counters and Abstraction Rules.

Abstraction Rules keep the full event picture in the cache and carry a significant in-memory and cache performance
penalty. A TTL Counter delivers the same count or sum with a much lighter footprint that can be retained for a much
longer period, backed by an efficient deprecation algorithm. It follows that **Abstraction Rules are strongly
discouraged for Velocity and Volume rules.** They are reserved for what a counter cannot express: cardinality (same,
distinct, take first, take last) and aggregations beyond count and sum (average, min, max, median, standard deviation).
See [TTL Counters](../../Configuration/Models/TTLCounters/index.html) and
[Abstraction Rules](../../Configuration/Models/AbstractionRules/index.html).

A TTL Counter can only count and sum. If the question needs a distinct count or an average, this is the wrong type.

## Velocity and Volume: Construction Steps

A Velocity or Volume rule in Jube is built in four steps: define the TTL Counter (Models >> Abstraction >> TTL Counters),
increment it from an Activation Rule, threshold it in a second Activation Rule, then set the Priority order and test.

### Step 1: Define the TTL Counter for a Velocity or Volume Rule

The TTL Counter definition answers four questions. Get these right first, as a counter cannot be re-windowed retrospectively.

| TTL Counter design question | TTL Counter field                                  | Guidance                                                                                                                                                                                     |
|-----------------------------|----------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Counted **for whom**?       | TTL Counter Data Name                              | The payload key that groups the counter (`AccountId`, `CardId`, `IP`). This is the entity. Required, and the counter is skipped on an event whose payload lacks the key.                    |
| Counted **over how long**?  | TTL Counter Interval Type and Value                | The window. A one day rule is a `Days` / `1` counter. Seconds, Minutes, Hours, Days, Months and Years are valid.                                                                            |
| Counting **events or value**? | Sum and TTL Counter Data Value                   | Off: each increment adds one (velocity). On: each increment adds the named payload Float or Integer, for example `CurrencyAmount` (volume).                                                 |
| How **precisely** does it wash off? | Online Aggregation, Live Forever, Resolution Interval | Covered in the precision options for TTL Counters. |                                                                                                                                                                            |

TTL Counter window precision options for Velocity and Volume rules:

* **Default (Online Aggregation off).** The counter is pre-aggregated and a background job decrements it once entries
  lapse. Cheap on recall, but the decrement is slightly latent, by up to the Resolution Interval. Suitable for windows
  measured in hours to years.
* **Online Aggregation on.** Every event is counted in real time from the individual entries. Exact to the second, but
  more expensive on recall. Use it only where the rule is hypersensitive to the edge of the window, such as a 60 second
  burst rule. Because it targets the transaction's Reference Date, it is correct for back-dated or replayed data.
* **Live Forever.** The counter is never decremented. That is a lifetime total, not a velocity, and belongs to
  [Behavioral Baseline and Ratio](BehavioralBaselineAndRatio.html) territory (for example, lifetime deposits as a
  denominator).
* **Resolution Interval (Minutes, Hours or Days).** How coarsely entries are bucketed for storage and decrement. Pick
  it at a fraction of the window: a one day window with `Hours` resolution is accurate to the hour, whereas a
  one hour window with `Days` resolution is meaningless.

The window is measured against the model's own Reference Date, not wall-clock time. A model that stops receiving
transactions stops ageing its counters, so a quiet system does not appear to "cool down".

The TTL Counter name is what a rule reads as `TTLCounter.<Name>`, so name it for what it means, including the window,
for example `CardPaymentsLastHour` or `OutboundValue24h`. Names are unique within a model (case-insensitive).

A TTL Counter must also be enabled at the model level (Enable Ttl Counter); otherwise the process is stepped over and
an INFO message is logged.

### Step 2: Increment the TTL Counter from an Activation Rule

A counter is not incremented by the payload arriving. It is incremented **as the consequence of an Activation Rule
matching**, by ticking Increment TTL Counter on that rule and selecting the counter. See
[TTL Counter Activation Rule Incrementation](../../Configuration/Models/TTLCounterActivationRuleIncrementation/index.html).

This has three consequences that drive how Velocity and Volume rules are shaped:

* **The condition on the incrementing rule is the count's filter.** "Card payments" is an incrementing rule with
  `Matched = Payload.TxnType.MatchEqual("CardPayment")`. To count every event, use a rule that always matches, for example
  `Matched = True`. Filtering here, rather than in the reading rule, means the counter holds exactly what it is named
  for.
* **The counter can be incremented across models**, provided the grouping key is present in both payloads. This is the
  bridge to [Event Sequencing](EventSequencing.html).
* **Priority decides whether the reading Activation Rule counts the current event.** See Step 4: Order the Activation Rules by Priority and Test.

### Step 3: Threshold the TTL Counter in an Activation Rule

TTL Counters are recalled once per invocation, in a single request to the cache, and are then available to every rule as
`TTLCounter.<Name>`, and are numbers, so the numeric steps apply directly. Thresholding is a pipeline, per the
[Rule Authoring Methodology](../RuleAuthoringMethodology.html), and every step named here is listed in the
[Rule Pipeline Reference](../RulePipelineReference.html). A pipeline that ends without deciding does not match. A velocity
threshold on a count counter:

```vb
Matched = TTLCounter.CardPaymentsLastHour.MatchGreater(5)
```

A volume band on a sum counter, using the inclusive `InRange` test:

```vb
Matched = TTLCounter.OutboundValue24h.MatchInRange(10000, 50000)
```

A volume limit on a sum counter. The counter already includes the current amount when the incrementing rule has an earlier Priority:

```vb
Matched = TTLCounter.OutboundValue24h.MatchGreater(50000)
```

A TTL Counter combined with a payload structuring check. `RequireGreater` is the guard on the counter, and `Against`
switches the pipeline to the payload value, keeping the state:

```vb
Matched = TTLCounter.CardPaymentsLastHour.RequireGreater(5).Against(Payload.CurrencyAmount).MatchIsJustBelowThreshold(10000, 10)
```

A guard clause selecting Velocity and Volume thresholds by product segment, keeping the evaluation itself in the expression:

```vb
If Payload.ProductType = "Corporate" Then
    Matched = TTLCounter.OutboundValue24h.MatchGreater(250000)
Else
    Matched = TTLCounter.OutboundValue24h.MatchGreater(25000)
End If
```

Two TTL Counters over different windows express **acceleration**, a burst against the recent norm, without leaving the
Velocity and Volume type. The share of the day's payments that fell in the last hour is a derived number, so it is an
Abstraction Calculation `HourShareOfDay`, with the function:

```vb
Matched = TTLCounter.CardPaymentsLastHour.RatioOf(TTLCounter.CardPayments24h).ZeroIfUndefined()
```

and the Activation Rule compares it, guarded by a minimum count:

```vb
Matched = TTLCounter.CardPaymentsLastHour.RequireGreaterOrEqual(5).Against(AbstractionCalculation.HourShareOfDay).MatchGreaterOrEqual(0.5)
```

Scope boundary: where the question becomes "is this unusual for *this* entity" rather than "is this over a fixed number", it has
become [Behavioral Baseline and Ratio](BehavioralBaselineAndRatio.html), which owns ratio construction such as
`RatioAbove` and `PercentageChangeFrom`.

### Step 4: Order the Activation Rules by Priority and Test

TTL Counters are recalled at the start of the invocation, and are then **updated inside the same invocation** as each
incrementing Activation Rule matches: the in-invocation counter value is increased immediately and the cache write is
queued. Activation Rules are evaluated in **Priority** order, so priority decides whether a rule sees the current event:

| Incrementing Activation Rule Priority relative to the reading Activation Rule | TTL Counter value the reading rule sees | Use for                                                                   |
|---------------------------------------------------------|----------------------------------------|---------------------------------------------------------------------------|
| **Earlier** (lower Priority number)                     | Prior events **plus the current event** | Velocity and volume rules: "the fifth payment in the hour" includes this one |
| **Later** (higher Priority number)                      | Prior events **only**                   | Sequenced patterns that must not count the current event, see [Event Sequencing](EventSequencing.html) |

Rules of thumb for TTL Counter incrementation and Priority:

* **For a Velocity or Volume rule, put the incrementing rule ahead of the reading rule.** `TTLCounter.CardPaymentsLastHour > 5`
  then fires on the sixth payment, with the sixth counted.
* **The incrementing rule only counts if it matches.** If its filter is not matched on this event, the counter is not
  moved, and the reading rule sees prior events only, whatever the priority.
* **The reading rule can be the incrementing rule.** A rule that both matches on a threshold and increments a counter
  increments only after it matches, so it never sees its own increment. Split counting and alerting into two rules.
* **A counter that is not moved does not change.** Incrementation is skipped if the payload lacks the counter's Data Name
  key, and for a payload being reprocessed through a reprocessing rule instance, so a replay does not double count.
  Where idempotency across retries is required, enable the `ActivationRuleIdempotency` setting.
* **Priority also orders Activation Rule Chaining.** A later rule can read an earlier rule's match as `Activation.<Name>`.
* **Suppression applies to the reading rule, not the counter.** A suppressed reading rule still allows the incrementing
  rule to feed the counter.
* **State the convention in the rule name or description** ("includes current event"), as the off-by-one between
  "prior events" and "including this one" is the most common source of disagreement in review.

Testing a Velocity or Volume rule: use the model validation and test endpoints adjacent to the Activation Rule service (see the authoring note in the
[Rule Taxonomy](../RuleTaxonomy.html)). A velocity rule needs a *sequence* of invocations, not a single payload, so a
useful test posts N events for one entity, asserts the alert on the event that crosses the threshold, then posts an
event for a *different* entity to prove the key isolates counts. Assert the priority behaviour too, by swapping the two
Priorities and confirming the alert moves one event later.

## Velocity and Volume: Where to Look in the Source

This page is a construction guide. TTL Counter behaviour is defined in the following places, which are the authority where
this page and the code disagree:

| TTL Counter concern                                            | Source file or documentation page                                                                                      |
|----------------------------------------------------------------|---------------------------------------------------------------------------------------------------|
| Recall of counters at the start of the invocation             | `Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/TtlCounterExtensions.cs`                |
| Increment on Activation Rule match, in-invocation update, idempotency, reprocessing skip | `Jube.Engine/.../Context/Extensions/ActivationRules/ActivationRuleTtlCounterExtensions.cs` |
| Where increment sits in the Activation Rule Priority iteration | `Jube.Engine/.../Context/Extensions/ActivationRules/IterateActivationRulesExtensions.cs`         |
| Background decrement of lapsed counters                        | `Jube.Engine/EntityAnalysisModelManager/BackgroundTasks/TaskStarters/TtlCounterAdministrationTaskStarter.cs` |
| Cache storage of counters and entries                          | `Jube.Cache/Redis/CacheTtlCounterRepository.cs`                                                   |
| Definition, validation and the increment fields                | [TTL Counters](../../Configuration/Models/TTLCounters/index.html), [TTL Counter Activation Rule Incrementation](../../Configuration/Models/TTLCounterActivationRuleIncrementation/index.html), [Activation Rules](../../Configuration/Models/BasicActivationRules/index.html) |

## Worked Example: Card Velocity Rule (Count TTL Counter)

Requirement: alert when a card is used more than five times in an hour.

1. **TTL Counter** `CardPaymentsLastHour`: TTL Counter Data Name `CardId`, Interval `Hours` / `1`, Resolution `Minutes`,
   Sum off, Online Aggregation off.
2. **Incrementing Activation Rule** `CardPaymentSeen`, Priority 1: `Matched = Payload.TxnType.MatchEqual("CardPayment")`, with
   Increment TTL Counter ticked and `CardPaymentsLastHour` selected. No response elevation: it exists only to count.
3. **Alerting Activation Rule** `CardVelocity1Hour`, Priority 2: `Matched = TTLCounter.CardPaymentsLastHour.MatchGreater(5)`.

Because `CardPaymentSeen` has the earlier Priority, the current payment is already counted when `CardVelocity1Hour` is
evaluated, so the alert fires on the sixth payment in the hour. Reversing the two Priorities would move it to the seventh.

## Worked Example: Outbound Volume Rule (Sum TTL Counter)

Requirement: alert when more than 50,000 leaves an account in 24 hours.

1. **TTL Counter** `OutboundValue24h`: Data Name `AccountId`, Interval `Days` / `1`, Resolution `Hours`, **Sum on** with
   TTL Counter Data Value `CurrencyAmount`.
2. **Incrementing Activation Rule** `OutboundSeen`: `Matched = Payload.Direction.MatchEqual("Outbound")`, Increment TTL Counter
   ticked.
3. **Alerting Activation Rule** `OutboundVolume24h`:
   `Matched = TTLCounter.OutboundValue24h.MatchGreater(50000)`, with the earlier-Priority incrementing rule having added the current amount.

## Velocity and Volume: Common Faults

Symptoms of a misbuilt TTL Counter Velocity or Volume rule, with the likely cause.

| Symptom in a Velocity or Volume rule                 | Likely cause                                                                                                  |
|------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| Counter is always `0`                                | No Activation Rule increments it; the model was not synchronised; Enable Ttl Counter is off; the payload lacks the Data Name key. |
| Alert fires one event later than expected            | The incrementing rule has a later Priority than the reading rule, or its filter did not match on this event.  |
| Volume counter counts events, not value              | Sum is off, or TTL Counter Data Value is unset.                                                               |
| Counts never fall away                               | Live Forever is on; or the model has stopped receiving events, so its Reference Date has not advanced.        |
| Counts fall away later than the window               | Resolution Interval is coarse relative to the window; or Online Aggregation is off and the decrement job is behind. |
| Counts differ between two entities that are one person | The Data Name key is not the entity. Correct the key, or move to [Link and Network](LinkAndNetwork.html).     |
| Retried invocations inflate counts                   | Enable `ActivationRuleIdempotency`. Reprocessing already skips incrementation by design.                     |

## Velocity and Volume: Departures From This Type

Anything that needs a distinct count, an average, a percentile, or a comparison against the entity's own history is not
this type, and should not be forced into a counter with extra windows. Departures from the documented taxonomy are
unsupported.
