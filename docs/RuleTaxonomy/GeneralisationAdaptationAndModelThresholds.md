---
layout: default
title: Generalisation, Adaptation and Model Thresholds
nav_order: 5
parent: Rule Taxonomy
---

# Generalisation, Adaptation and Model Thresholds

Rules detect what someone thought of. Models generalise from what the data contains. Jube supports both, in the same
invocation, and an Activation Rule is where they meet: a model produces a score, and an Activation Rule decides what
score matters. This page lays out how to use a model score in an Activation Rule, how to choose the threshold, and when
a model is the right tool and a rule is not.

It is a cross-cutting topic, not a detection class of the [Rule Taxonomy](RuleTaxonomy.html). A model score is an
*input*
to a rule, in the same way as a TTL Counter or a Dictionary value, and is compared like any other number, as set out in
the
[Rule Authoring Methodology](RuleAuthoringMethodology.html).

## Generalisation, Adaptation and Model Thresholds: The Idea

A rule has a threshold chosen by a person, for example "more than 4 declined transactions in a day". The choice is
usually anecdotal, it does not change as behaviour changes, and rules accumulate until they cannot be managed. A model
replaces many such thresholds with a single learned combination of the same continuous values (counts, sums, ratios,
deviations), expressed as one score, so that risk appetite is set by varying one threshold on that score. The weights
adapt when the model is retrained on new data.
See [Exhaustive Adaptation Concepts](../Configuration/ExhaustiveAdaptation/Concepts/index.html).

Three terms are used consistently on this page:

| Term            | Meaning in Jube                                                                                                                                              |
|-----------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Generalisation  | A model scoring events it was not trained on, and catching variants that no single rule describes                                                            |
| Adaptation      | Retraining or replacing a model as data changes, so the score keeps its meaning. Also the name of the Jube entity that carries a score (the Adaptation)      |
| Model threshold | The score at or above which an Activation Rule matches, which is a business decision about risk appetite and alert capacity, and not a property of the model |

## Model Recall Routes

A model reaches an Activation Rule by one of three routes. All three produce a number that an Activation Rule reads, and
all three take the same resolved inputs from the invocation.

| Route                           | Where the model runs                                               | Trained                                                                                                   | Token in a rule                        | Status                              |
|---------------------------------|--------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|----------------------------------------|-------------------------------------|
| Exhaustive Adaptation           | In the Jube process                                                | In Jube, by the Exhaustive training algorithm (neural networks)                                           | `ExhaustiveAdaptation.<Name>`          | Documented and available            |
| HTTP Adaptation (remote recall) | At a remote HTTP endpoint (R Plumber, Python Flask or any service) | Outside Jube                                                                                              | `HttpAdaptation.<Name>`                | Documented and available            |
| AskJooby Analytics              | In the Jube process                                                | In Jube, by the AskJooby Analytics service, chosen from a wide toolset by an agent-assisted trial process | Not yet available in rules (see below) | In development on a separate branch |

See [Exhaustive Adaptation](../Configuration/ExhaustiveAdaptation/index.html),
[Exhaustive Adaptation Recall](../Configuration/ExhaustiveAdaptation/Recall/index.html),
[HTTP Adaptation](../Configuration/Models/HTTPAdaptation/index.html) and the
[HTTP Adaptation Protocol](../Configuration/Models/HTTPAdaptationProtocol/index.html).

## Where a Model Sits in the Invocation

Models are recalled after every value they read has been produced, and before Activation Rules are evaluated:

| Order | Stage                                                                                  | Notes                                                                                |
|-------|----------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|
| 1     | Inline Functions, Inline Scripts, Gateway Rules                                        | Derived payload values                                                               |
| 2     | Sanctions, TTL Counters, Abstraction Rules                                             | Counters, aggregates, sanction distances                                             |
| 3     | [Abstraction Calculations](../Configuration/Models/AbstractionCalculations/index.html) | Ratios and derived numbers                                                           |
| 4     | Exhaustive Adaptation                                                                  | In-process neural networks                                                           |
| 5     | AskJooby Analytics Adaptation                                                          | In-process models, recalled right after Exhaustive (on the branch where it is built) |
| 6     | HTTP Adaptations                                                                       | Remote models, in ascending Priority                                                 |
| 7     | Activation Rules                                                                       | Read everything above, in Priority order                                             |

Three consequences follow. A model can read anything in stages 1 to 3, so the counters, aggregates and ratios that rules
use are also model features. A model does not choose to read another route's score, except that an HTTP Adaptation is
sent the whole payload as resolved so far, including the scores of lower Priority HTTP Adaptations already recalled. And
an Activation Rule can read every score, and combine it with rules and with the Activation Rules of higher priority.

## Feeding a Model: Features Come From Rules

A model is only as good as its inputs, and its inputs are the values Jube already computes. Each input is identified by
its processing type and name:

| Processing type         | Example input                           | Comes from                                                                               |
|-------------------------|-----------------------------------------|------------------------------------------------------------------------------------------|
| Payload                 | `Payload.Amount`                        | The event, an Inline Function, or an Inline Script                                       |
| Dictionary              | `Dictionary.CountryRisk`                | A [Dictionary](../Configuration/Models/Dictionaries/index.html)                          |
| TTL Counter             | `TTLCounter.Declined24h`                | A [TTL Counter](../Configuration/Models/TTLCounters/index.html)                          |
| Sanction                | `Sanction.PartyName`                    | A [Sanction](../Configuration/Sanctions/index.html) distance                             |
| Abstraction             | `Abstraction.DistinctDevices7d`         | An [Abstraction Rule](../Configuration/Models/AbstractionRules/index.html)               |
| Abstraction Calculation | `AbstractionCalculation.DeclineRate24h` | An [Abstraction Calculation](../Configuration/Models/AbstractionCalculations/index.html) |

This is why a ratio should be built as an Abstraction Calculation: the same named value is a rule input and a model
input, so the rule and the model agree on what it means, and a score can be compared with the ratio that drives it.

Three practices protect a model in production:

* **Lock the inputs.** A model depends on the exact definition of each input it was trained on. Changing a TTL Counter
  window or an Abstraction Rule after training changes the model's behaviour without retraining it. Treat a deployed
  model's inputs as frozen, and change them by training a new model.
* **Know what a missing input becomes.** An Exhaustive model that cannot find an input by processing type and name falls
  back to the mean created by training statistics, which changes model performance. The AskJooby Analytics stage passes
  an absent input as `NaN` and the model imputes as it was trained. An HTTP endpoint receives the payload as it stands.
* **Prefer defined numbers.** A ratio with a zero divisor is `NaN`, which a model cannot use, unless the calculation
  ends in
  `.ZeroIfUndefined()`, which stores zero, and a model reads that as a real zero. Choose deliberately, and add a count
  as a second input, so the model can tell zero from unknown.

## Reading a Model Score in an Activation Rule

An Exhaustive score is a number, and is compared like any other number:

```vb
If (ExhaustiveAdaptation.FraudScore > 0.8) Then
    Return True
End If
```

The score can be combined with rule inputs, using `And`:

```vb
If (ExhaustiveAdaptation.FraudScore >= 0.5 And AbstractionCalculation.DeclineRate24h > 0.3) Then
    Return True
End If
```

An HTTP Adaptation is not a bare number in a rule. It is an object with a `Value`, which is null when the endpoint
returned an error or nothing, so decide what null means. `OrNaN()` reads it as a number and gives `NaN` when it is null,
and `NaN` fails every comparison, so a suppressed score never matches whichever way the rule is written:

```vb
If (HttpAdaptation.RemoteScore.Value.OrNaN() > 0.8) Then
    Return True
End If
```

```vb
If (HttpAdaptation.RemoteScore.Value.OrNaN() < 0.2) Then
    Return True
End If
```

`OrZero()` reads a suppressed score as zero instead. That makes a rule that fires on a **high** score simply not fire,
but it makes a rule that fires on a **low** score fire on a missing score, so use it only when zero is the answer you
want:

```vb
If (HttpAdaptation.RemoteScore.Value.OrZero() > 0.8) Then
    Return True
End If
```

`HasValue()` and `HasNoValue()` test a suppressed score explicitly after `OrNaN()`. `GetValueOrDefault` is not
permitted, which is what `OrZero()` and `OrNaN()` replace. **The `Value` token is not among the hard coded tokens of the
parser**, so on an instance where `Value` is not registered in the `RuleScriptToken` table, a rule that reads
`HttpAdaptation.<Name>.Value` is refused by the integrity check as a security restricted token, and its `CompileError`
says so. Register `Value` there, or confirm it is present, before relying on an HTTP Adaptation in a rule (see
[Rule Compilation Tokens and Extensions](../Configuration/Models/RuleCompilationAlgorithm/index.html)).

Scores from different routes are combined with `Or` and `And`, and negated with `Not`:

```vb
If (ExhaustiveAdaptation.FraudScore > 0.8 Or HttpAdaptation.RemoteScore.Value.OrNaN() > 0.8) Then
    Return True
End If
```

### A Missing Score Reads as Zero

If no score is present, for example because no model has been promoted, or the model failed,
`ExhaustiveAdaptation.<Name>` reads as **zero**, not as an error. This is safe for a rule that fires on a high score. It
is unsafe for any rule that treats a low score as evidence of safety, such as an allow rule, a step-up bypass or a rule
that lowers a response elevation, because a missing score would look like the lowest possible risk. Guard such a rule
with a second, independent condition, and never let a score of zero alone approve anything.

### AskJooby Analytics Scores

An AskJooby Analytics model is recalled in the pipeline and its score is stored in the payload and shown in the
`AskJoobyAnalyticsAdaptation` element of the response, in the same shape as the Exhaustive one. **Reading that score
from an Activation Rule is not yet available**: adding it changes the compiled rule contract (a new parameter in the
generated
`Match` signature, the parser tokens and the field catalogue), and is a pending decision on the branch. Until then, an
AskJooby Analytics score can be reported and compared offline, and a model that must drive a rule should be recalled by
Exhaustive or HTTP Adaptation. This page will name the token when it exists.

## Choosing a Model Threshold

A model score has no natural threshold. The threshold is chosen from data, on the same principle as the
[Rule Taxonomy](RuleTaxonomy.html), and never copied from another institution.

| Method                                | How it works                                                                                                                                         | Use when                                                              |
|---------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| Alert capacity                        | Take the score distribution of recent events, and set the threshold at the score above which the number of alerts the team can work in a day remains | Investigation capacity is the binding constraint, which it usually is |
| Target precision                      | Set the threshold at the lowest score for which the proportion of confirmed fraud among alerts meets the target                                      | Labelled outcomes exist, and false positives are costly               |
| Target recall                         | Set the threshold at the highest score that still catches the target proportion of confirmed fraud                                                   | Missing fraud is costlier than reviewing alerts                       |
| Cost weighted                         | Choose the threshold minimising expected cost, from the cost of an alert and the cost of a miss                                                      | Both costs can be estimated                                           |
| Percentile of the entity's own scores | Compare with the entity's history instead of a global cut                                                                                            | The population is heterogeneous                                       |

Practical points that apply to all of them:

* **Calibration.** A score is a *probability* only if it has been calibrated. A calibrated 0.8 means about 80 percent of
  such events are the target class, so thresholds have a meaning that survives retraining. An uncalibrated score is a
  rank only, and its threshold must be re-derived after every retrain. The HTTP Adaptation Protocol carries a
  `Calibration` element for this purpose, and the AskJooby Analytics service calibrates on a validation split.
* **Thresholds are business decisions.** They belong in the Activation Rule, versioned, with the response elevation and
  case workflow they trigger, so a change is reviewable and reversible. Do not embed a threshold in the model endpoint.
* **Bands, not a single line.** Two or three Activation Rules on the same score give a graded response, for example a
  high threshold that declines and a lower one that raises a case. Each rule has its own response elevation.
* **Retraining moves the distribution.** After a model is replaced, the score distribution changes, and an unchanged
  threshold changes the alert volume. Re-derive the threshold before the new model takes traffic.
* **Class imbalance.** Fraud is rare, so accuracy is not a useful measure. Use precision, recall and alert volume.

A tiered response on one score:

```vb
If (ExhaustiveAdaptation.FraudScore >= 0.9) Then
    Return True
End If
```

```vb
If (ExhaustiveAdaptation.FraudScore >= 0.7 And ExhaustiveAdaptation.FraudScore < 0.9) Then
    Return True
End If
```

## Hybrid Patterns: Rules and Models Together

Rules and models are complementary, and most production configurations use both. Each pattern below is built from
Activation Rules, and each is an ordinary rule over the score and the inputs.

| Pattern                 | Construction                                                                                                          | Why                                                               |
|-------------------------|-----------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------|
| Score threshold         | One Activation Rule on the score                                                                                      | The simplest model use, with one knob for risk appetite           |
| Rule-gated model        | A cheap rule condition (a product, a channel, a minimum history), then threshold the score                            | Applies a model only where it was trained, and saves alert volume |
| Model-confirmed rule    | An Activation Rule for a typology, matched only if the score is also above a lower threshold                          | Reduces the false positives of a noisy rule                       |
| Rule override           | A rule for a hard control (a sanctions hit, a blacklisted device) that matches whatever the score is                  | Deterministic controls must not depend on a model                 |
| Ensemble and boosting   | Several HTTP Adaptations in ascending Priority, where a later one reads an earlier one's `Value`                      | Model chaining, where one model corrects another                  |
| Champion and challenger | A challenger model recalled and reported, with no Activation Rule that acts on it, compared with the champion offline | A new model is proven on live traffic before it drives decisions  |
| Backstop                | A rule that applies when the model has no score                                                                       | Keeps coverage when the model fails                               |

A rule-gated model, using `And` as the guard:

```vb
If (Payload.ProductType = "Card" And ExhaustiveAdaptation.FraudScore > 0.8) Then
    Return True
End If
```

A model-confirmed rule, where a counter typology must also have a moderate score:

```vb
If (TTLCounter.CardPayments1h > 5 And ExhaustiveAdaptation.FraudScore > 0.4) Then
    Return True
End If
```

## Rules or Models: The Trade-Off

| Consideration         | Rule                                           | Model                                                                                               |
|-----------------------|------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| Explainability        | The condition is the explanation               | Needs contribution or journey reporting (HTTP Adaptation Protocol carries these) to explain a score |
| Cold start            | Works from day one, from a known typology      | Needs data, and labelled or anomaly-derived classes, before it is worth deploying                   |
| Novel behaviour       | Misses what nobody described                   | Generalises to near variants, and to combinations no rule states                                    |
| Threshold             | Chosen by hand, per rule, and rarely revisited | One threshold on one score, chosen from data, and revisited on retraining                           |
| Drift                 | Efficacy wanes silently as behaviour changes   | Detectable through score distribution and performance, and correctable by retraining                |
| Volume of maintenance | Grows with every typology                      | Grows with the number of models, which is far fewer                                                 |
| Latency and cost      | Negligible                                     | Small for an in-process model, larger for a remote call with a network round trip                   |
| Governance            | A rule version is easy to review               | A model needs validation, versioning and a promotion process                                        |
| Hard controls         | Right for a deterministic control              | Wrong for one, because a score is a probability                                                     |

As a working rule: use a **rule** for a deterministic control, a known typology with clear thresholds, and for the first
days of a new product. Use a **model** to combine many continuous values into a single risk score, to catch what rules
miss, and to replace a growing set of hand-set thresholds. Use **both** whenever a model is present, because the rules
are the model's inputs, its guards and its backstop.

## Failure, Latency and Fail-Safe Design

A model is a dependency that can be absent. Decide for each rule what an absent score should do.

| Situation                     | Exhaustive and AskJooby Analytics                                                                                                                                          | HTTP Adaptation                                                                                           |
|-------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| No model promoted or deployed | Absent, and an Exhaustive score reads as zero                                                                                                                              | Not applicable                                                                                            |
| The model throws              | The exception is traced and swallowed, the score is absent for that invocation, and the rest of the invocation continues                                                   | The endpoint error suppresses `Value`, which is null, and a rule that does not handle null does not match |
| Slow model                    | The AskJooby Analytics stage has a per-invocation time budget, checked between models (a model not started leaves no score). Exhaustive has no stage budget on this branch | The call is part of the response time, so budget for the round trip                                       |

Design so that the absence of a score fails towards review, not towards approval: fire on high scores, guard low-score
rules with independent evidence, and add a backstop rule for the case where the model has not answered.

## Testing a Model Rule

Test a model rule with the model validation and test endpoints described in the authoring note of the
[Rule Taxonomy](RuleTaxonomy.html). Test three cases for each rule: a score above the threshold, a score below it, and
no score at all. Use the mock endpoints under `/api/MockHttpAdaptation` to exercise a remote model's shapes, including a
suppressed score and a malformed body, without standing up a service. Assert what happens when a score is absent, as
that is the case most often left untested.

## Generalisation, Adaptation and Model Thresholds: Where to Look in the Source

| Concern                                                 | Source file or documentation page                                                                     |
|---------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| Stage order in the invocation                           | `Jube.Engine/EntityAnalysisModelInvoke/EntityAnalysisModelInvoke.cs`                                  |
| Abstraction Calculations, and the values they read      | `Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/AbstractionCalculationsExtensions.cs`       |
| Exhaustive recall and input matching                    | `Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/ExhaustiveAdaptationsExtensions.cs`         |
| HTTP Adaptation recall and Priority                     | [HTTP Adaptation Protocol](../Configuration/Models/HTTPAdaptationProtocol/index.html)                 |
| A missing score reading as zero                         | `Jube.Dictionary/PooledDictionary.cs` (indexer returns the default for an absent key)                 |
| Rule tokens `ExhaustiveAdaptation` and `HttpAdaptation` | [Rule Compilation Tokens and Extensions](../Configuration/Models/RuleCompilationAlgorithm/index.html) |
| Comparison and pipeline steps                           | [Rule Pipeline Reference](RulePipelineReference.html)                                                 |
