---
layout: default
title: Cardinality
nav_order: 2
parent: Rule Taxonomy
---

# Cardinality (Same and Different)

**Definition:** how many distinct, or how many identical, values an entity has produced for an attribute. *Variety*, not
volume, is the signal.

**Construction:** an Abstraction Rule with the Search Key set to the entity, and the Function Type set to Distinct Count
or Same Count. Cardinality relies heavily on transaction data being stored in the cache, and on the range of aggregation
functions that breadth of cached data makes available. See the [Rule Taxonomy](../RuleTaxonomy.html) for how this type
differs from its neighbours.

| Question an analyst asks                                          | Rule Taxonomy type that answers it                                               |
|-------------------------------------------------------------------|----------------------------------------------------------------------------------|
| How many different devices has this account used in 7 days?       | Cardinality: Distinct Count on the device field, Search Key the account          |
| How many times has this card been used at this same merchant?     | Cardinality: Same Count on the merchant field, Search Key the card               |
| How many different accounts has this device been used by?         | Not this type: [Link and Network](LinkAndNetwork.html), the same rule with the Search Key inverted to the device |
| How many payments in the last hour, regardless of variety?        | Not this type: [Velocity and Volume](VelocityAndVolume.html)                      |

## Cardinality: Abstraction Rules, Not TTL Counters

A TTL Counter can only count and sum, so it cannot answer a question of variety. Cardinality therefore uses
[Abstraction Rules](../../Configuration/Models/AbstractionRules/index.html), which keep the event picture in the cache and
evaluate a function over the matching records. This carries a cache and memory cost that a TTL Counter does not, which is
why Abstraction Rules are reserved for cardinality and complex aggregations, and not used for plain counts.

## Cardinality: Construction Steps

A Cardinality rule is built in four steps: make the entity a Search Key, define the Abstraction Rule, threshold it in an
Activation Rule, then test it with a sequence of events.

### Step 1: Make the Entity a Search Key

The entity being profiled must be a Search Key in the Request XPath definition (Models >> References >> Request XPath).
A Search Key drives a single keyed retrieval of matching records from the cache, so the number of Search Keys is the
number of cache queries per invocation. Abstraction Rules that share a Search Key share the one retrieval.

### Step 2: Define the Abstraction Rule for Cardinality

| Abstraction Rule field | Cardinality setting                                                                                                                                              |
|------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Search                 | On. The rule is tested against records returned from the cache, not only the current transaction.                                                                |
| Search Key             | The entity whose variety is measured, for example `AccountId`. Inverted to a shared attribute, this becomes [Link and Network](LinkAndNetwork.html).                |
| Search Interval        | The window reaching back from the Reference Date, for example 7 Days. Applied to records after retrieval, not in the cache query, so retrieval is bounded by the fetch limit in the model definition. |
| Rule fragment          | A Boolean filter selecting which cached events are eligible, for example `Matched = Payload.TxnType.MatchEqual("CardPayment")`. Use `Matched = True` to include everything. |
| Function Type          | **Distinct Count** for "how many different values", or **Same Count** for "how many times has this current value been seen before".                              |
| Function Key           | The payload field whose values are counted, for example `DeviceId`.                                                                                              |
| Offset Type and Value  | Optional. First, Last, Skip First or Take Last narrow the matches to a position, for example the last five events.                                               |

Distinct Count returns the number of unique values of the Function Key across the matches. Same Count returns how many
matches carry the same Function Key value as the transaction currently being processed. Other Function Types (Average,
Median, Max, Min, Standard Deviation, Since, Actual Value, First and Last positions via Offset) belong to
[Behavioral Baseline and Ratio](BehavioralBaselineAndRatio.html), or to the "take first" and "take last" cardinality
questions, for example "is this the first time this account has used this country".

### Step 3: Threshold the Abstraction Rule in an Activation Rule

The aggregated value is available to Activation Rules as `Abstraction.<Name>`, and is a number, so the numeric pipeline
steps apply. See the [Rule Pipeline Reference](../RulePipelineReference.html). A Distinct Count threshold:

```vb
Matched = Abstraction.DistinctDevices7d.MatchGreater(3)
```

A Same Count used to detect repetition against a single counterparty:

```vb
Matched = Abstraction.SameBeneficiary30d.MatchGreaterOrEqual(5)
```

A cardinality signal combined with a payload condition, using `Require` as the guard and `Against` to switch value,
per the [Rule Authoring Methodology](../RuleAuthoringMethodology.html):

```vb
Matched = Abstraction.DistinctDevices7d.RequireGreater(3).Against(Payload.CurrencyAmount).MatchGreater(1000)
```

### Step 4: Test Cardinality with a Sequence of Events

Cardinality is a property of history, so a test must post several events for one entity with differing values of the
attribute, then assert the response on the event that crosses the threshold. Confirm on the target instance whether the
current event is included in the aggregation, using a two event case, as this decides whether the threshold is
`> 3` or `>= 3`. The behaviour is defined in
`Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/AbstractionRulesWithSearchKeysExtensions.cs`, which is
the authority where this page and the code disagree.

## Cardinality: Common Faults

Symptoms of a misbuilt Cardinality rule, with the likely cause.

| Symptom in a Cardinality rule                       | Likely cause                                                                                                              |
|-----------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| The Abstraction value is always 0                   | The entity field is not a Search Key; the model was not synchronised; the rule fragment filter excludes every event.     |
| The count is lower than expected for a long window  | The fetch limit in the model definition truncates retrieval before the Search Interval is applied.                        |
| Cardinality slows the invocation                    | Too many Search Keys, or an unbounded fetch limit. Each Search Key is a cache query.                                      |
| A plain count is being built as Distinct Count      | The question is Velocity and Volume. Use a TTL Counter instead.                                                           |
