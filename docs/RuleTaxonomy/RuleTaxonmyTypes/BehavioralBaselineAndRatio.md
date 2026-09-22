---
layout: default
title: Behavioral Baseline and Ratio
nav_order: 4
parent: Rule Taxonomy
---

# Behavioral Baseline and Ratio

**Definition:** is this normal for this entity, or is this proportion right? Self-referential deviation, or any
proportion-of-two-counters question. The mechanism is the same either way.

**Construction:** an Abstraction Rule with Function Type Average, Median, Standard Deviation, Sum, Max or Min over a
Search Interval, optionally two Abstraction reads combined through an Abstraction Calculation before thresholding. A
ratio of two TTL Counters is also in scope. See the [Rule Taxonomy](../RuleTaxonomy.html) for how this type differs from its
neighbours.

| Question an analyst asks                                             | Rule Taxonomy type that answers it                                        |
|----------------------------------------------------------------------|---------------------------------------------------------------------------|
| Is this amount far above this customer's usual amount?               | Behavioral Baseline: Average and Standard Deviation, then a z-score        |
| Are refunds an unusual proportion of sales?                          | Behavioral Baseline: a ratio of two Abstraction Sums                       |
| Is today's volume more than double the 30 day daily norm?            | Behavioral Baseline: a TTL Counter compared to a longer window counter     |
| Is the count over a fixed number?                                    | Not this type: [Velocity and Volume](VelocityAndVolume.html)              |
| Are amounts clustered just below a limit?                            | Not this type: [Structuring](StructuringOrThresholdAvoidance.html)        |

## Behavioral Baseline: Three Construction Patterns

The taxonomy has one mechanism and three shapes. Choose the shape by where the baseline comes from.

| Shape                                   | Baseline source                                                     | Where the comparison is made                       |
|-----------------------------------------|---------------------------------------------------------------------|----------------------------------------------------|
| Deviation from the entity's own history | Abstraction Rule: Average, Median, Standard Deviation over a window  | An Abstraction Calculation, compared by the rule   |
| Ratio of two measures                   | Two Abstraction Rules, or two TTL Counters                          | An Abstraction Calculation, read by the rule       |
| Short window against long window       | Two TTL Counters over different windows                             | An Abstraction Calculation, compared by the rule   |

## Behavioral Baseline: Construction Steps

### Step 1: Build the Baseline Measure

For a baseline drawn from history, build an Abstraction Rule with the entity as the Search Key, a Search Interval long
enough to be representative (for example 90 Days), and the Function Type for the statistic, using the Function Key of
the field measured (for example `CurrencyAmount`). Build one Abstraction Rule per statistic, for example
`AvgAmount90d` and `StdDevAmount90d`. Measures that need only a count or sum should use TTL Counters, per
[Velocity and Volume](VelocityAndVolume.html), as they are far lighter than an Abstraction Rule. See
[Abstraction Rules](../../Configuration/Models/AbstractionRules/index.html).

### Step 2: Create the Ratio as an Abstraction Calculation

An Abstraction Calculation is the **preferred way to create a ratio for Activation Rules**. It is a function fragment that
uses the fluent calculation methods, for example refunds divided by sales, and the operands can be a payload field, a TTL
Counter, an Abstraction Rule, a Sanction, a Dictionary value or an earlier calculation:

```vb
Matched = Abstraction.Refunds.RatioOf(Abstraction.Sales).ZeroIfUndefined()
```

The fragment is the only form: the earlier Add, Subtract, Divide and Multiply types have been removed. The result is defined
once, named, and available to every rule as `AbstractionCalculation.<Name>`, and to models as an input, so the rule and the
model agree on what the ratio is. See
[Abstraction Calculations](../../Configuration/Models/AbstractionCalculations/index.html) and
[Generalisation, Adaptation and Model Thresholds](../GeneralisationAdaptationAndModelThresholds.html).

Every derived number in this type, whether a ratio, a deviation or a growth, is created the same way. As a standard,
use an Abstraction Calculation, because it can be reused and it organises concerns, although an inline calculation in an
Activation Rule remains fully supported. See
[Derived Values: Use an Abstraction Calculation](../RuleAuthoringMethodology.html#derived-values-use-an-abstraction-calculation).
An undefined result is `NaN` in a calculation unless the function ends in `.ZeroIfUndefined()`, which stores zero. A rule that
fires on a *low* value needs a count guard when it reads a stored zero. That is covered under Abstraction Calculations.

### Step 3: Threshold the Stored Value in an Activation Rule

Each measure below is an Abstraction Calculation, shown with its function, and the Activation Rule beneath it only compares.

A z-score of the current amount against the entity's own history, as a calculation `AmountZScore90d`:

```vb
Matched = Payload.CurrencyAmount.ZScore(Abstraction.AvgAmount90d, Abstraction.StdDevAmount90d).ZeroIfUndefined()
```

```vb
Matched = AbstractionCalculation.AmountZScore90d.MatchGreater(3)
```

A percentage increase against the entity's average, as a calculation `AmountIncreaseVsAverage90d`:

```vb
Matched = Payload.CurrencyAmount.PercentageChangeFrom(Abstraction.AvgAmount90d).ZeroIfUndefined()
```

```vb
Matched = AbstractionCalculation.AmountIncreaseVsAverage90d.MatchGreater(200)
```

A ratio of refunds to sales, as a calculation `RefundToSalesRatio`:

```vb
Matched = Abstraction.Refunds.RatioOf(Abstraction.Sales).ZeroIfUndefined()
```

```vb
Matched = AbstractionCalculation.RefundToSalesRatio.MatchGreater(0.15)
```

A ratio of two TTL Counters, as a calculation `DeclineRate24h`. The same value serves a threshold rule and a band rule, which
is the reuse the calculation is for, for example a decline rate that is suspiciously steady:

```vb
Matched = TTLCounter.DeclinedPayments24h.RatioOf(TTLCounter.Payments24h).ZeroIfUndefined()
```

```vb
Matched = AbstractionCalculation.DeclineRate24h.MatchGreater(0.5)
```

```vb
Matched = AbstractionCalculation.DeclineRate24h.MatchInRange(0.28, 0.32)
```

A short window against a long window, as a calculation `Volume1hToDailyNorm`, using the last hour's volume against the
30 day daily average:

```vb
Matched = TTLCounter.Volume1h.RatioOf(TTLCounter.Volume30d.DividedBy(30)).ZeroIfUndefined()
```

```vb
Matched = AbstractionCalculation.Volume1hToDailyNorm.MatchGreater(2)
```

Thresholds should be derived from the actual distribution of the data, and not from best practice lists, per the
[Rule Taxonomy](../RuleTaxonomy.html). The stored value is what makes that possible, because it is recorded and can be
reported.

## Behavioral Baseline: Cold Start and Priority

An entity with no history has no baseline. Guard against this in the rule, for example with a `Require` step on a minimum count of
prior events before a deviation rule is allowed to match, otherwise every new customer deviates:

```vb
Matched = Abstraction.Count90d.RequireGreaterOrEqual(30).Against(AbstractionCalculation.AmountZScore90d).MatchGreater(3)
```

Where the baseline is a
TTL Counter, incrementation timing follows the Priority rules in [Velocity and Volume](VelocityAndVolume.html): a
counter incremented earlier in Priority order includes the current event in the baseline, which biases a comparison of
the current event against itself, so increment the baseline counter after the reading rule.

## Behavioral Baseline: Common Faults

Symptoms of a misbuilt Behavioral Baseline rule, with the likely cause.

| Symptom in a Behavioral Baseline rule          | Likely cause                                                                                                        |
|------------------------------------------------|---------------------------------------------------------------------------------------------------------------------|
| Every new customer alerts                      | No cold start guard; the baseline is zero or absent.                                                                |
| The ratio is zero, or the rule does not fire   | The denominator is zero. The calculation is `NaN`, or zero with `ZeroIfUndefined()`, and a `NaN` fails every comparison. |
| The deviation never fires                      | The baseline includes the current event because the baseline counter was incremented ahead of the reading rule.     |
| The baseline is unstable                       | The Search Interval is too short, or truncated by the fetch limit in the model definition.                          |
