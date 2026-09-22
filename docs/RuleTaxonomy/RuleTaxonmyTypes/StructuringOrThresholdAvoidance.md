---
layout: default
title: Structuring or Threshold Avoidance
nav_order: 9
parent: Rule Taxonomy
---

# Structuring or Threshold Avoidance

**Definition:** values engineered to dodge a known limit, repeated.

**Construction:** `Double` threshold extensions (`IsJustBelowThreshold`, `IsRoundAmount`) combined with
[Velocity and Volume](VelocityAndVolume.html) or [Cardinality](Cardinality.html) for the repetition. Use it when the risk
is evasion of a specific numeric control, and not a generic anomaly, which is
[Behavioral Baseline and Ratio](BehavioralBaselineAndRatio.html). See the [Rule Taxonomy](../RuleTaxonomy.html).

| Question an analyst asks                                                | Rule Taxonomy type that answers it                                                |
|-------------------------------------------------------------------------|-----------------------------------------------------------------------------------|
| Are deposits repeatedly just under the 10,000 reporting limit?          | Structuring: `IsJustBelowThreshold` with a TTL Counter for the repetition          |
| Is an account always sending round amounts?                             | Structuring: `IsRoundAmount` with a TTL Counter for the repetition                 |
| Are amounts unusually large for this customer?                          | Not this type: [Behavioral Baseline and Ratio](BehavioralBaselineAndRatio.html)   |
| Is a single amount just under the limit, once?                          | Structuring without repetition: a weak signal on its own, see below                |

## Structuring: The Single Event Signal is Weak

A single amount just below a limit is weak evidence: most such amounts are legitimate. The signal is the **repetition**,
so the rule has two parts. The first is a payload predicate that recognises an engineered value. The second is a counter
of how often that has happened for the entity within a window. The two parts are separate Activation Rules, joined by a
TTL Counter, following the shape of [Velocity and Volume](VelocityAndVolume.html).

## Structuring: Construction Steps

### Step 1: Recognise the Engineered Value

`IsJustBelowThreshold(threshold, marginPercent)` is true when the amount is below the threshold and within the margin
percentage of it (`IsJustAboveThreshold` is the mirror, for a limit that triggers on exceeding it). For a 10,000 limit with a 10 percent margin, amounts from 9,000 up to but excluding 10,000 are
recognised:

```vb
Matched = Payload.CurrencyAmount.MatchIsJustBelowThreshold(10000, 10)
```

`IsRoundAmount(nearest)` is true when the amount is an exact multiple of the value given, for example 500. Round amounts
are a separate signal, and are often combined with a threshold band, using `Require` as the guard. See the
[Rule Pipeline Reference](../RulePipelineReference.html):

```vb
Matched = Payload.CurrencyAmount.RequireIsRoundAmount(500).MatchInRange(5000, 9999)
```

Where several limits apply, for example 3,000, 10,000 and 15,000, `IsJustBelowAnyThreshold(marginPercent, thresholds...)` tests
all of them in one step, and takes the margin first:

```vb
Matched = Payload.CurrencyAmount.MatchIsJustBelowAnyThreshold(10, 3000, 10000, 15000)
```

The threshold, margin and rounding unit must come from the data and the control in question, per the
[Rule Taxonomy](../RuleTaxonomy.html), and not from a list of best practice values.

### Step 2: Count the Repetition with a TTL Counter

Define a TTL Counter keyed on the entity, windowed to the period the control is measured over (for example `Days` / `7`),
and increment it from the Activation Rule of Step 1. The Activation Rule that recognises the engineered value is the
incrementing rule. Priority follows the rules in [Velocity and Volume](VelocityAndVolume.html): incrementing earlier in
the Priority order means the current event is included in the count.

### Step 3: Alert on the Repetition

```vb
Matched = TTLCounter.JustBelowLimit7d.MatchGreaterOrEqual(3)
```

Where the repetition has to be across different values or destinations, and not merely a count, use an Abstraction Rule
with Distinct Count, per [Cardinality](Cardinality.html), for example several just below limit amounts split across many
beneficiaries. Where several payments sum past the limit inside a window, use a Sum TTL Counter, and threshold it
against the limit directly:

```vb
Matched = TTLCounter.DepositValue24h.RequireGreater(10000).Against(TTLCounter.DepositCount24h).RequireGreaterOrEqual(3).Against(Payload.CurrencyAmount).MatchIsJustBelowThreshold(10000, 10)
```

## Structuring: Common Faults

Symptoms of a misbuilt Structuring rule, with the likely cause.

| Symptom in a Structuring rule                  | Likely cause                                                                                                  |
|------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| Too many alerts on single events               | No repetition condition. A single amount below a limit is weak evidence on its own.                            |
| Structured amounts just over the margin missed | The margin is too narrow for how the limit is being avoided. Derive the margin from the data.                 |
| The count includes the alerting event or not   | Priority of the incrementing rule relative to the alerting rule. See Velocity and Volume.                     |
| The limit is a sum across accounts             | The counter key is the account, not the beneficial owner. Key the counter on the entity the control applies to.|
