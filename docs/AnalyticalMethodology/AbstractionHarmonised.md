---
layout: default
title: Abstraction
nav_order: 11
parent: Analytical Methodology
---

# Abstraction and Transformations

The original Module 6 walked through building derived variables by hand in a spreadsheet against an exported CSV of
historical prices: horizon-based labels, backward-looking summary statistics, threshold-filtered aggregates, ratios,
binary flags, a statistical transform, point references, anchoring, file clean-up and finally random-digit sampling to
cut the file down to a workable size.

None of that spreadsheet mechanics survives in the product, and unlike every other module in this set, the reason isn't
"R got replaced by AdvancedAnalytics." **Abstraction — shaping and moulding raw transaction data into variables before
modelling — is not an analytics-time activity in Jube at all.** It runs earlier, inside the rule engine, against live
transaction data held in the cache, every time a transaction is invoked through a model:

- **Abstraction Rules**
  ([docs/Configuration/Models/AbstractionRules/index.md](../Configuration/Models/AbstractionRules/index.md))
  are what the spreadsheet's "Fill Down over a Scope" procedures actually became. A rule names a Search Key (which slice
  of history to look at), a Search Interval/Value (how far back — the *Scope*), an optional condition to filter the
  matches (the *threshold*), an Offset (First/Last/Skip First/Take Last — picking a position out of that history), and a
  Function Type (Sum, Average, Median, Skew, Kurtosis, Standard Deviation, Count, Distinct Count, Same Count, Actual
  Value, Max, Min, Since) that reduces the matches to one number. It compiles once at model synchronisation and runs
  against the Postgres cache on every invocation, not against an exported file.
- **Abstraction Calculations**
  ([docs/Configuration/Models/AbstractionCalculations/index.md](../Configuration/Models/AbstractionCalculations/index.md))
  are what the spreadsheet's horizontal formulas (ratios, differences, IF conditions, SQRT) became: a VB.NET function
  fragment that reads Payload fields, TTL Counters, Abstraction Rules, Sanctions, Dictionaries and earlier calculations
  (in creation order) through fluent extension methods, and stores one named result —
  `AbstractionCalculation.Name` — that later Activation Rules and models can read.

Both run inside a fixed invocation order (Sanctions/TTL Counters/Abstraction Rules, then Abstraction Calculations, then
Exhaustive, then HTTP Adaptations, then Activation Rules), and both look **backward only**
from the transaction's Reference Date — there is no live "look 24 intervals forward" operation, because a production
invocation has no future to look into. Anywhere the spreadsheet procedure below needed the future (the dependent
variable), the product's equivalent is a labelled historical outcome assembled at training time, not something
Abstraction computes on the live path.

The ten procedures are walked through below at a much lighter weight than other modules: for most, the concept and its
Jube equivalent are a paragraph, because the point of this module is precisely that the mechanics moved elsewhere. Three
procedures — threshold aggregation, ratio construction, and the SQRT transform — keep a short, actually-run R snippet
because the transformation they demonstrate (conditional aggregation, normalisation by division, and a skew-reducing
power transform) is easier to see in a real before/after than to describe in prose.

## Vocabulary and why abstraction matters

The original module's vocabulary is worth keeping even though its spreadsheet mechanics are not. A **variable**
(also a feature or characteristic) is one column being fed to a model; a **record** (also evidence) is one row. Among
the variables, one is the **dependent variable** — the outcome being predicted — and the rest are **independent
variables**, drawn from the acceptance environment the record was captured in. Independent variables are either
**continuous** (an amount, a count, a duration) or **categorical**: discrete (a fixed set of labels), state-based (a
status that changes over time) or binary (a flag). Every Abstraction Rule and Calculation in this guide produces one
variable of one of these kinds, for one record at a time.

The original module made a stronger claim than a vocabulary list: that abstraction — the judgement of which raw signals
to turn into variables at all, not the mechanics of computing them — is where the majority of a predictive analytics
project's real effort and value belongs, ahead of model fitting itself. That judgement is domain creativity applied to
whatever raw signal is available, whether that signal is a transaction record, an image, a sensor reading or free text;
the mechanism below is generic, but knowing what raw material is worth abstracting from in the first place is not
something Abstraction Rules or Calculations can supply on their own.
[Methodology and Taxonomy](MethodologyAndTaxonomy.md) covers where this judgement fits ahead of abstraction, in problem
framing and elicitation.

One named pattern from the original module carries over as a construction rather than a separate mechanism:
**momentum abstraction** compares two abstracted values taken over different windows of the same history — a short
trailing average against a longer one, for example — to express whether a value is accelerating or decelerating rather
than just where it currently sits. Procedure 4 below is exactly this construction: a ratio between two
already-abstracted values (`Average700.RatioOf(...)`), just as applicable to two windows of the same statistic as to a
window against the current value.

## Procedure 1: Create a Dependent Variable in Time Series Data

The original procedure locates, for each row, the price 24 five-minute intervals (two hours) ahead, and stores that as
the label to be predicted. This is the one procedure with **no** AdvancedAnalytics or Abstraction Rule equivalent, by
design: Abstraction Rules and Calculations only aggregate the window ending at the current Reference Date and reaching
backward — there is no forward horizon parameter to configure. In the product, a dependent variable is a labelled
outcome (for example a confirmed fraud or AML disposition recorded against a case) assembled into a training dataset,
not a value computed live by the rule engine.

**Tool equivalent:** none — labelling is a training-data-assembly concern, outside both R's horizon formula
and the Abstraction engine.

## Procedure 2: Create an Independent Variable Based on a Basic Summary Statistic

`=AVERAGE(E25:E726)` over the trailing 700 intervals is exactly an Abstraction Rule with Search enabled, a Search Key, a
Search Interval/Value describing the 700-interval Scope, no filter condition, and Function Type Average (or Sum, Median,
Standard Deviation, Skew, Kurtosis, Mode, Max, Min, Count, Distinct Count — the same menu as the spreadsheet's Excel
functions, listed in the Abstraction Rules doc's Function Type table).

**Tool equivalent:** Abstraction Rule, Function Type = Average (or the sibling aggregation the rule
needs) — the rule engine's own vertical abstraction, not an AdvancedAnalytics tool.

## Procedure 3: Create an Independent Variable Based on Threshold Aggregation

`=AVERAGEIF(E25:E726, ">" & E25)` narrows the same 700-interval scope to only the prices above the current
Interval_Close before averaging. This is the concept an Abstraction Rule's own match condition exists for: the rule's
condition (its "Search" test) filters which cache records enter the matches collection, and only then does the Function
Type aggregate them — so a threshold and an aggregation are two settings on the same rule, not two spreadsheet
functions.

```R
set.seed(1)

n <- 726
interval_close <- round(cumsum(rnorm(n, mean = 0, sd = 0.0004)) + 1.1000, 5)

reference_row <- 25
reference_value <- interval_close[reference_row]

scope <- interval_close[reference_row:(reference_row + 700)]

average_700 <- mean(scope)
averageif_700 <- mean(scope[scope > reference_value])

cat(sprintf("Reference Interval_Close (row %d): %.5f\n", reference_row, reference_value))
cat(sprintf("Average_700 (unconditional scope mean): %.5f\n", average_700))
cat(sprintf("AverageIf_700 (mean of scope > reference): %.5f\n", averageif_700))
cat(sprintf("Count included in AverageIf_700: %d of %d\n",
            sum(scope > reference_value), length(scope)))
```

Console output:

```
Reference Interval_Close (row 25): 1.10169
Average_700 (unconditional scope mean): 1.10299
AverageIf_700 (mean of scope > reference): 1.10410
Count included in AverageIf_700: 559 of 701
```

**Tool equivalent:** Abstraction Rule with a match condition (equivalent to the spreadsheet's
`">" & E25` filter) plus Function Type = Average — the filter and the aggregation are both configured on the one rule,
evaluated against the cache.

## Procedure 4: Creating a Ratio Independent Variable in Horizontal Abstraction

`=I25/E25` (Average_700 over the current Interval_Close) is a ratio between two already-abstracted values. This is the
one construction the product's own documentation names as a preferred pattern: an Abstraction Calculation using the
fluent `RatioOf` method, read back as `AbstractionCalculation.Name` by any later Activation Rule or model.

```R
set.seed(1)

n <- 726
interval_close <- round(cumsum(rnorm(n, mean = 0, sd = 0.0004)) + 1.1000, 5)

reference_row <- 25
reference_value <- interval_close[reference_row]
scope <- interval_close[reference_row:(reference_row + 700)]
average_700 <- mean(scope)

ratio_to_average <- average_700 / reference_value

cat(sprintf("Interval_Close (reference): %.5f\n", reference_value))
cat(sprintf("Average_700: %.5f\n", average_700))
cat(sprintf("Ratio (Average_700 / Interval_Close): %.6f\n", ratio_to_average))
```

Console output:

```
Interval_Close (reference): 1.10169
Average_700: 1.10299
Ratio (Average_700 / Interval_Close): 1.001178
```

**Tool equivalent:** an Abstraction Calculation, for example
`Matched = Abstraction.Average700.RatioOf(Payload.IntervalClose).ZeroIfUndefined()`, stored once as
`AbstractionCalculation.Average700Ratio` and reused by every rule or model that needs it — the product's stated
preference over writing the same ratio inline in each Activation Rule. `Plus`, `Minus` and `Times` cover the
spreadsheet's Add/Subtract/Multiply siblings the same way.

## Procedure 5: Creating a Binary Independent Variable in Horizontal Abstraction

`=IF(E25 > I25, 1, 0)` — a binary flag for whether the current price is above its own trailing average — is an
Activation Rule (or an Abstraction Calculation feeding one) comparing the current value against the abstracted one,
using `MatchGreater` in place of the spreadsheet's `>`, with `MatchLess`, `MatchGreaterOrEqual`,
`MatchLessOrEqual` and `MatchNotEqual` covering the rest of the IF function's conditional operators.

**Tool equivalent:** Activation Rule condition, for example
`Matched = Payload.IntervalClose.MatchGreater(Abstraction.Average700)` — the rule pipeline's ordinary comparison test,
not a separate binary-variable construction.

## Procedure 6: Creating a Statistical Transformation Using SQRT and Observing Improvement

The spreadsheet used `SKEW` to gauge lean, then `SQRT` to reduce it. The Jube rule DSL carries the same transform as a
fluent `Sqrt` method usable inside an Abstraction Calculation, and skewness itself is one of the Abstraction Rule's own
Function Types, so the "before" measurement can come straight from a rule rather than a separate formula.

```R
library(e1071)

set.seed(1)

average_700 <- rgamma(20000, shape = 2, rate = 6) + 1.05

skew_before <- skewness(average_700, type = 2)

average_700_sqrt <- sqrt(average_700)
skew_after <- skewness(average_700_sqrt, type = 2)

cat(sprintf("Skew(Average_700): %.4f\n", skew_before))
cat(sprintf("Skew(Average_700_SQRT): %.4f\n", skew_after))
```

Console output:

```
Skew(Average_700): 1.3054
Skew(Average_700_SQRT): 1.0439
```

**Tool equivalent:** Abstraction Rule, Function Type = Skew, to measure the lean; an Abstraction
Calculation using the fluent `Sqrt` method, for example `Matched = Abstraction.Average700.Sqrt()`, to apply the
transform. `Log`, `Log10` and the ordinary arithmetic methods cover the spreadsheet's LOG/ABS/POWER siblings. For
diagnosing the shape of a column at rest (not as a live per-invocation value), the AdvancedAnalytics semantic layer also
exposes `Skewness` and a general `TransformValues` tool (natural log, log1p, square root, cube root, square, reciprocal
and others) in
`Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics.SpreadAndShapeTools` and
`.TableAndTransformTools` — useful for offline analysis of a column pulled from the cache, distinct from the live
rule-engine transform above.

## Procedure 7: Creating a Point Independent Variable in Time Series Data

`=E426` (the price exactly 300 intervals into the scope) is a single positional reference into history. This is what the
Abstraction Rule's Offset settings and the Actual Value Function Type are for: Offset Type Skip First (or Take Last)
with an Offset Value picks out a specific position in the matches, and Actual Value returns that record's field — for
example, the value of the 3rd transaction in a customer's history, per the Function Type table in the Abstraction Rules
doc.

**Tool equivalent:** Abstraction Rule, Offset Type = Skip First/Take Last with the point's offset as the
Offset Value, Function Type = Actual Value.

## Procedure 8: Anchor an Independent Variable and a Dependent Variable

Subtracting the current Interval_Close from a raw forward value (`=(E2)-E25`) so the model predicts change rather than
an absolute level is exactly the caution the Abstraction Calculations doc gives as the reason ratios (and, by the same
logic, differences) are the preferred model input over a raw value: build the same
`Minus`/`RatioOf` construction shown in Procedures 3–4 against the current Payload value, so the abstracted variable is
anchored to "now" rather than absolute. As in Procedure 1, the dependent side of this pairing (the future value being
anchored) has no live equivalent — only the independent side, built from values already in the cache, is something an
Abstraction Calculation can express.

**Tool equivalent:** Abstraction Calculation using `Minus` or `RatioOf` against the current Payload value,
for the independent side only; none for the forward-looking dependent side (see Procedure 1).

## Procedure 9: Clean Up, Remove Formulas and Save the Abstraction File

Stripping formulas to values and trimming the incomplete top/bottom rows was necessary because the spreadsheet was a
static file being handed to a separate modelling step. Jube has no equivalent step because there is no intermediate
file: Abstraction Rules and Calculations write their results directly into the invocation payload and (optionally) an
archive table, and training data is assembled straight from the cache and abstraction outputs into
`AdvancedAnalyticsCache`'s packed `double[]` matrix (see
[Numeric Data Representation](Reference/FoundationData.md)) — already numeric, with no formula
cells to flatten and no incomplete-window rows to hunt down by hand, since a rule's Search Interval and Offset already
define what counts as a complete window.

**Tool equivalent:** none needed — `AdvancedAnalyticsCache` holds the assembled matrix directly; there is
no intermediate file to clean.

## Procedure 10: Assign a Random Digit for Sampling

Filling a column with `RANDBETWEEN(0,100)`, sorting by it and deleting the excess rows is a manual, unseeded simple
random sample used to cut an unwieldy file down to size. The AdvancedAnalytics data-preparation layer does the same job
as one reproducible call instead of a sort-and-delete-by-hand procedure.

**Tool equivalent:** tool `"SampleSimpleRandom"` in
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.SamplingTools` — draws a simple random sample of
row positions reproducibly from a seed, in place of an unseeded RANDBETWEEN-and-sort. The same class also offers
`SampleStratified`, `SampleSystematic`, `SampleReservoir`, `SampleWeighted` and `SampleClusters` for sampling designs
the spreadsheet procedure did not attempt.
