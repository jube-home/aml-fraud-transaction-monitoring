---
layout: default
title: Link and Network
nav_order: 3
parent: Rule Taxonomy
---

# Link and Network

**Definition:** the same cardinality question as [Cardinality](Cardinality.html), but relational: multiple entities
converging on one shared attribute. The risk is about the connection, not about one entity's own behaviour.

**Construction:** an Abstraction Rule with a configuration identical to Cardinality, with the Search Key **inverted** to
the *shared* trait (a device, an address, a beneficiary account) instead of the primary entity, and the Function Key set
to the primary entity. See the [Rule Taxonomy](../RuleTaxonomy.html) for how this type differs from its neighbours.

| Question an analyst asks                                             | Rule Taxonomy type that answers it                                                 |
|----------------------------------------------------------------------|------------------------------------------------------------------------------------|
| How many different customers have used this device?                  | Link and Network: Distinct Count of `CustomerId`, Search Key the device            |
| How many different accounts pay this beneficiary?                    | Link and Network: Distinct Count of `AccountId`, Search Key the beneficiary        |
| How many different devices has this customer used?                   | Not this type: [Cardinality](Cardinality.html), the Search Key is the customer     |
| Is this device on a blacklist?                                       | Not this type: [Static and Match Screening](StaticAndMatchScreening.html)          |

## Link and Network: Inverting the Search Key

Cardinality asks "how many distinct values of X has *this entity* produced". Link and Network asks "how many distinct
entities have produced *this value*". The mechanism is the same Distinct Count, and only the two roles are swapped.

| Abstraction Rule field | Cardinality                          | Link and Network                                    |
|------------------------|--------------------------------------|-----------------------------------------------------|
| Search Key             | The entity (`CustomerId`)            | The shared trait (`DeviceId`)                       |
| Function Key           | The attribute (`DeviceId`)           | The entity (`CustomerId`)                           |
| Function Type          | Distinct Count, or Same Count        | Distinct Count, or Same Count                       |
| Signal                 | One entity showing many values       | Many entities converging on one value               |

Because the recall is keyed on the shared trait, it returns the history of *every* entity that has presented that trait,
including entities unrelated to the customer being processed. That is the point, and it is also why the trait must be a
Search Key and the fetch limit in the model definition matters: a popular shared trait, such as a public Wi-Fi address,
returns a great many records.

## Link and Network: Construction Steps

### Step 1: Make the Shared Trait a Search Key

In Request XPath (Models >> References >> Request XPath), make the shared attribute a Search Key. The same field may
also be a Search Key for a Cardinality rule where it is the entity, as the two are simply different rules on different
keys. Each Search Key is one cache query per invocation.

### Step 2: Define the Abstraction Rule

Set Search on, the Search Key to the shared trait, the Search Interval to the window in which convergence is meaningful
(for example 30 Days), the Function Type to Distinct Count, and the Function Key to the entity. Use the rule fragment
to restrict which events are eligible, for example `Matched = Payload.TxnType.MatchEqual("Login")`, or `Matched = True` for all. See
[Abstraction Rules](../../Configuration/Models/AbstractionRules/index.html).

### Step 3: Threshold in an Activation Rule

A device shared by several customers, read as `Abstraction.<Name>`:

```vb
Matched = Abstraction.DistinctCustomersPerDevice30d.MatchGreater(3)
```

Combined with a payload condition, to raise the signal only when value is moving, using `Require` and `Against` per the
[Rule Authoring Methodology](../RuleAuthoringMethodology.html):

```vb
Matched = Abstraction.DistinctAccountsPerBeneficiary7d.RequireGreater(5).Against(Payload.CurrencyAmount).MatchGreater(500)
```

Exempting known good shared traits, such as an office network, using a List, per
[Reference Data](../RuleAuthoringMethodology.html#reference-data-lists-and-dictionaries):

```vb
Matched = Abstraction.DistinctCustomersPerDevice30d.RequireGreater(3).Against(Payload.DeviceId).MatchNotInList(List.KnownSharedDevices)
```

Where the shared trait is known bad in advance, this is not a network signal but a membership check, per
[Static and Match Screening](StaticAndMatchScreening.html).

### Step 4: Test Link and Network with Several Entities

Post events from several *different* entities that present one shared value, and assert the response on the event that
crosses the threshold. Then post events for one entity across several values, to prove the rule counts entities and not
values. Whether the current event is included in the aggregation should be confirmed on the target instance, per
[Cardinality](Cardinality.html).

## Link and Network: Limits of the Type

This type detects **one hop**: entities that share a single trait directly. It does not traverse a graph, so it cannot
answer "customers linked to a customer who is linked to fraud". Chains of two or more traits are built by stacking rules,
one per trait, and combining them in an Activation Rule with `And` or `Or`. Anything beyond that is a departure from the
documented taxonomy, and is unsupported.

## Link and Network: Common Faults

Symptoms of a misbuilt Link and Network rule, with the likely cause.

| Symptom in a Link and Network rule              | Likely cause                                                                                                            |
|-------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| The Abstraction value is always 0 or 1          | The Search Key is the entity, not the shared trait. The Search Key and Function Key are the wrong way round.            |
| Counts are far too high for a common trait      | The shared trait is not distinctive (a shared gateway, or a default value). Exempt it with a List.                      |
| The count is lower than expected                | The fetch limit in the model definition truncates the records returned for a popular trait.                             |
| Slow invocations                                | Too many Search Keys, or a popular trait returning very large recalls. Narrow the window.                               |
