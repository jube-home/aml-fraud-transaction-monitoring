---
layout: default
title: Rule Authoring Methodology
nav_order: 1
parent: Rule Taxonomy
---

# Rule Authoring Methodology

Rules in Jube are VB.NET code fragments, but the language is deliberately constrained. Not everything that can be
written in VB.NET can be written in a rule. Every fragment is put through a token allow-list integrity check (the soft
parse) before it is compiled, and only tokens that are hard coded, registered in `RuleScriptToken`, or discovered as
extension methods are permitted. See
[Rule Compilation Tokens and Extensions](../Configuration/Models/RuleCompilationAlgorithm/index.html) for the mechanism.

The consequence is a methodology rather than a syntax preference: **rules are expressed as a Boolean expression built
from tokens and fluent extension methods, and `If` is used sparingly, as a guard in front of that expression.**

## What a Rule Fragment Is

A fragment is not a whole function. The compiler embeds it inside a generated class, and the wrapper declares the result
variable and returns it:

```text
Public Shared Function Match(...) As Boolean
    Dim Matched as Boolean
    Try
        ' ...the rule fragment is inserted here...
    Catch ex As Exception
        Log.Info(ex.ToString)
    End Try
    Return Matched
End Function
```

Two things follow:

* A rule signals a match in either of two forms, and **both are valid**. It can assign `Matched`, or it can `Return` the
  result directly, because the wrapper ends with `Return Matched`. `Matched` defaults to `False`, so a fragment that
  does nothing does not match. For a single condition the two forms below are equivalent:

  ```vb
  Matched = Payload.CurrencyAmount > 123.45
  ```

  ```vb
  If (Payload.CurrencyAmount > 123.45) Then
      Return True
  End If
  ```

  They stop being equivalent the moment a fragment has more than one statement, and which one to use is then part of
  what the rule means rather than a matter of taste:

    * `Return True` is **pipeline drop-out**. Execution leaves the fragment there and nothing after it runs, so the
      first statement to decide is the one that answers. Use it where a condition is conclusive on its own -- a deny
      list hit, a sanction match -- and later work would either be wasted or could only muddy a decision already made.
    * Assigning `Matched` is **pipeline override**. Execution carries on to the wrapper's `Return Matched`, so a later
      statement can still change the answer and the last assignment wins. Use it where a rule accumulates: a broad
      condition sets the match and a narrower exemption clears it again, or several pipelines are weighed and the one
      written last carries.

  Put plainly: with `Return`, the first decision wins; with `Matched`, the last one does. A fragment that mixes them is
  reading as both, so pick one per fragment and stay with it.

  The parser refuses a fragment that contains neither a `Return` nor an assignment to `Matched`, and it refuses a bare
  expression on its own, such as `Payload.Amount.Start().MatchGreater(100)`, because its result would be evaluated and
  thrown away. The check reads the syntax tree, so a `Return` or `Matched` inside a string literal does not count.

  A `Return` ends the fragment at that point, and statements after it do not run. The expression and pipeline examples
  in the rest of this documentation are written in the assignment form, and model score examples in the `Return` form,
  but any example can be written either way. In a fragment that returns a number (an Abstraction Calculation or an
  Inline Function),
  `Return` the number, or assign it to `Matched`.
* The type of `Matched` depends on where the fragment is used. It is `Boolean` for Activation Rules, Gateway Rules and
  Abstraction Rules (the filter that decides whether an event is included), `Double` for Abstraction Calculations, and
  `Object` of the configured return data type for Inline Functions. See
  [Activation Rules](../Configuration/Models/BasicActivationRules/index.html),
  [Gateway Rules](../Configuration/Models/GatewayRules/index.html),
  [Abstraction Rules](../Configuration/Models/AbstractionRules/index.html),
  [Abstraction Calculations](../Configuration/Models/AbstractionCalculations/index.html) and
  [Inline Functions](../Configuration/Models/InlineFunctions/index.html).

An exception thrown while evaluating a fragment is caught, logged, and leaves `Matched` as it was, which is `False` in
the ordinary case. A rule that faults therefore does not match; it does not halt the invocation.

## Reading Data: Tokens

Rules read data through dot-notation tokens, for example `Payload.CurrencyAmount`, `TTLCounter.Name`,
`Abstraction.Name`, `AbstractionCalculation.Name`, `List.Name`, `Dictionary.Name`, `Sanction.Name`,
`Activation.Name`, `ExhaustiveAdaptation.Name` and `HttpAdaptation.Name`. The compiler rewrites each to a typed internal
form before the integrity check (for example `Payload.CurrencyAmount` becomes `Data("CurrencyAmount").AsDouble()`, with
the accessor chosen from the field's data type). Internal names such as `Data`, `KVP` and `Calculation` are never typed
by a rule author. The full mapping is in
[Rule Author Syntax vs. Internal Token Representation](../Configuration/Models/RuleCompilationAlgorithm/index.html).

Because the payload accessor is typed, `Payload.CurrencyAmount` is a `Double` and the `Double` extension methods are
available directly on it.

## Reference Data: Lists and Dictionaries

Rules should not embed reference data. Values that are maintained over time, such as deny lists, high risk terms or
country risk scores, are held in a List or a Dictionary and referenced by name, so that analysts change the data without
editing, recompiling or re-versioning the rule. Embedding a large set of literals in a rule fragment is a departure from
this methodology.

| Reference data construct | Token in a rule     | What it returns                                                                  | Use it for                                                                  |
|--------------------------|---------------------|----------------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| List                     | `List.<Name>`       | A collection of strings, tested with the `InList` and `...InList` pipeline steps | Membership: deny lists, allow lists, device blacklists, terms to screen for |
| Dictionary               | `Dictionary.<Name>` | A number looked up by a key taken from the payload                               | Enrichment: a risk score per country, merchant category or product          |

### Lists

A List is a named set of string values, maintained in the Lists page, by CSV upload, by database or by API, and each
value may carry a Delete Expiry Date after which it lapses. Lists are synchronised by the engine in the background
continuously, so a change applies almost immediately without a model synchronisation. Membership is a whole value match,
case sensitive with `InList` and case insensitive with `InListIgnoreCase`, and is null safe:

```vb
Matched = Payload.AccountId.MatchInList(List.BadCustomers)
```

A List also feeds the substring steps, so that a maintained set of terms is checked against free text:

```vb
Matched = Payload.Narrative.MatchContainsAnyInListIgnoreCase(List.HighRiskTerms)
```

A List is a set of strings only. It carries no value per entry, so it cannot express "this country scores 4". That is a
Dictionary. See [Lists](../Configuration/Models/Lists/index.html), and
[Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html) and
[Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html) for the patterns that use them.

### Dictionaries

A Dictionary pairs a key with a numeric value. Each Dictionary definition names the payload field that is the key. On
each invocation the engine looks that key up and makes the value available as `Dictionary.<Name>`. If the key field is
absent from the payload or the key is not in the Dictionary, the value is zero, so there is always a value. **Encode
Dictionary values so that zero is the safe default**, for example risk scores where zero means no known risk.

```vb
Matched = Dictionary.CountryRisk.MatchGreaterOrEqual(4)
```

Dictionaries are loaded into engine memory during model synchronisation, so a change needs a model synchronisation, and
they suit hundreds of thousands of entries rather than very large datasets. The invocation does not write to a
Dictionary; the state it holds is maintained externally. State that events must maintain themselves is a TTL Counter, as
in [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html). See
[Dictionary](../Configuration/Models/Dictionaries/index.html).

### Choosing Between Reference Data Constructs

| Requirement                                     | Use                                                                                       |
|-------------------------------------------------|-------------------------------------------------------------------------------------------|
| Is this value on a set that analysts maintain?  | List                                                                                      |
| Enrich the event with a score or number per key | Dictionary                                                                                |
| Regulatory list with fuzzy name matching        | Sanction, see [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html) |
| A handful of stable values that never change    | Inline literals in `In` or `ContainsAny` are acceptable                                   |
| State written by the events themselves          | TTL Counter                                                                               |

## Derived Values: Use an Abstraction Calculation

A number that is worked out from other numbers, such as a ratio, a difference, a share, a growth rate, a deviation or a
z-score, should **as a standard** be defined once as an
[Abstraction Calculation](../Configuration/Models/AbstractionCalculations/index.html), and an Activation Rule should
then only compare the result. Working the number out inside a rule remains a first class citizen, and is fully
supported, because the fluent methods make it a single line and there are places a calculation cannot reach. The
standard is to endeavour to use an Abstraction Calculation, because the value can be reused, and because it organises
concerns: the calculation says what the number is, and the rule says what to do about it.

| Reason             | Why an Abstraction Calculation is better                                                                                                                                                                                                                 |
|--------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Reuse              | The value is defined once and named. Every Activation Rule reads `AbstractionCalculation.<Name>`, so the definition is changed in one place, and no two rules drift apart.                                                                               |
| Coverage           | The value exists for the whole invocation, and not only inside one rule. It can be a model input, appear in the response payload, be archived for SQL reporting with Report Table, and be seen in the case, which an expression buried in a rule cannot. |
| Observability      | The number that a rule compared is visible, so an analyst can see that a ratio was 0.34 against a threshold of 0.3, and tune the threshold from the recorded distribution.                                                                               |
| One evaluation     | The value is computed once per invocation, and not again in each rule that needs it.                                                                                                                                                                     |
| Organised concerns | The calculation defines the number, and the rule makes the decision, so each can be read, tested and changed on its own.                                                                                                                                 |
| Simpler rules      | The rule is a comparison, which is what the [taxonomy](index.html) and the pipeline steps are for.                                                                                                                                                |

Written inline, a rule mixes calculation and decision:

```vb
Matched = TTLCounter.DeclinedPayments24h.MatchRatioAbove(TTLCounter.Payments24h, 0.5)
```

Written as an Abstraction Calculation `DeclineRate24h`, with the function:

```vb calculation
Matched = TTLCounter.DeclinedPayments24h.RatioOf(TTLCounter.Payments24h).ZeroIfUndefined()
```

the rule only compares:

```vb
Matched = AbstractionCalculation.DeclineRate24h.MatchGreater(0.5)
```

What stays in an Activation Rule is the decision: comparing a value with a threshold, a range or a list, testing a
property of a value (`IsJustBelowThreshold`, `IsRoundAmount`, a distance from a point), and guarding with `Require`.
What moves to an Abstraction Calculation is the arithmetic that produces the value being compared. A calculation reads
any payload field, TTL Counter, Abstraction Rule, Sanction, Dictionary value or earlier calculation, but not a model
score, because models are recalled after calculations. Choose deliberately between an undefined result stored as zero
(`ZeroIfUndefined()`) and `NaN`, as set out in the calculation documentation, and guard a rule that reads a stored zero
from below with a count.

Work the number out inside the rule where a calculation is not available. Gateway Rules run before calculations do, so
they cannot read one. A number that involves a model score (the gap between two scores, or the mean of an Exhaustive and
an HTTP score) can only be worked out in the Activation Rule, because models are recalled after calculations. The one
step derive-and-compare tests in the [Rule Pipeline Reference](RulePipelineReference.html) exist for these cases, and
for a rule that is genuinely used once.

## The Fluent Approach

Extension methods live in `Jube.Dictionary.Extensions` and are discovered by reflection so that they become permitted
tokens. Any advanced rule capability is delivered this way, and the library is expanded each release. `Do`, `While`
and `For` loops are not available; iteration is the job of an extension method. The catalogue, by type, is in
[Extensions](../Configuration/Models/RuleCompilationAlgorithm/index.html), and administrator-curated additions that need
no release are described in
[Curated Dynamic Expressions](../Configuration/Models/CuratedDynamicExpressions/index.html).

Extension methods fall into two kinds, and a chain is a mix of both:

* **Transformers** return a value of the same or another type, so the chain can continue. For example
  `Payload.Email.NormalizeEmailAlias()` returns a string, and `Payload.Amount.Clamp(0, 10000)` returns a double.
* **Predicates** return a `Boolean`, which ends the chain. For example `Between`, `InRange`, `In`, `NotIn`,
  `Contains`, `ContainsAny`, `IsWithinRadiusKm` and `IsJustBelowThreshold`.

A plain predicate returns a Boolean and the surrounding expression decides what to do with it; predicates are combined
with `And` and `Or`, and negated with `Not`, all of which are permitted tokens. That form has no notion of breaking out
of a chain part way. For that, Jube provides pipelines, described next.

### Pipelines and Breaks

A pipeline is a linear, forward only chain that carries a state alongside the value. Every step looks at the state
first: once the pipeline has reached a decision, every step after it is skipped. That decision is the break. The rule
reads left to right as a series of guards, and the expressions after a break are never evaluated by the pipeline.

A pipeline begins at the first step called on a value, most usefully a payload field, with nothing to declare, and each
step is named **prefix + test**. The prefix says what happens to the pipeline, and the suffix says what is tested:

| Prefix    | When the test holds  | When the test fails  | Typical use                             |
|-----------|----------------------|----------------------|-----------------------------------------|
| `Match`   | Decide: **matched**  | Continue             | The condition that fires the rule       |
| `Reject`  | Decide: **rejected** | Continue             | An exclusion (this is not a match)      |
| `Break`   | Decide: **broken**   | Continue             | Stop evaluating without a verdict       |
| `Require` | Continue             | Decide: **rejected** | A guard, the bouncer of the methodology |
| `Ensure`  | Continue             | Decide: **broken**   | A precondition, stop if not satisfied   |

The result converts to a Boolean, and only a pipeline that has decided *matched* is `True`. A pipeline that ends without
deciding is `False`, so a rule fails closed. Rejected, broken and errored are all `False`, and are kept distinct so that
tests and logs can tell a guard that said no from a pipeline that was stopped.

```vb
Matched = Payload.CurrencyAmount.RequireInRange(123.45, 543.21).MatchGreater(400)
```

Steps are typed to the value being tested, so `Payload.CurrencyAmount` offers the numeric tests, and a string field
offers the string tests, with the same grammar. To test another field part way along, switch the value with
`Against`, which keeps the state, so a decision already made still holds:

```vb
Matched = Payload.CurrencyAmount.RequireInRange(123.45, 543.21).Against(Payload.MerchantCountry).RequireNotIn("GB", "IE").OtherwiseMatch()
```

`OtherwiseMatch()` and `OtherwiseReject()` act only if nothing has decided yet, which is the closest equivalent of
`Else`.

Anything that is already a Boolean, including every existing extension method and every token comparison, can be brought
into a pipeline with `MatchWhen`, `RejectWhen`, `BreakWhen`, `RequireThat` and `EnsureThat`:

```vb
Matched = Payload.Email.RequireIsNotNullOrEmpty().RequireThat(Payload.Email.IsValidEmail()).MatchEndsWith("gmail.com")
```

Pipelines can also accumulate a score with `AddScore`, `AddScoreWhen` and `CapScore`, and decide on it with
`MatchIfScoreAtLeast`, `RejectIfScoreBelow`, `BreakIfScoreAtLeast` and `RequireScoreAtLeast`. Values that arrive as text
are converted with `ParseDouble`, `ParseInteger`, `ParseDate` and `ParseBoolean`, which produce an errored, and
therefore non-matching, pipeline rather than an exception when the text is not valid.

Pipelines take no delegates or lambdas, which is deliberate: it keeps the token allow-list meaningful. Note also that VB
evaluates the arguments of a step before the call, so a break saves the later steps, but does not avoid the cost of
reading a token that is passed as an argument. The complete list of steps is in the
[Rule Pipeline Reference](RulePipelineReference.html).

### Comparison: statements versus expression

The statement form returns from inside an `If`, which leaves the rule the moment it has decided rather than carrying on
through whatever follows:

```vb
If Payload.CurrencyAmount > 123.45 Then
    Return True
End If
```

The expression form assigns the result of the evaluation directly, which removes the `If` altogether:

```vb
Matched = Payload.CurrencyAmount > 123.45
```

The two are interchangeable here because there is one condition. Where a fragment has several, the statement form drops
out at the first decision and the expression form lets a later line override an earlier one -- see
[What a Rule Fragment Is](#what-a-rule-fragment-is) for which to reach for.

Where a value must fall within a range, use the range predicate. `InRange` is inclusive of both ends, whereas
`Between` is exclusive of both ends, so choose deliberately:

```vb
Matched = Payload.CurrencyAmount.InRange(123.45, 543.21)
```

Multiple conditions are combined with `And` / `Or` (with `Not` to negate):

```vb
Matched = Payload.CurrencyAmount.InRange(123.45, 543.21) And Not Payload.MerchantCountry.In("GB", "IE")
```

A transformer chain feeding a predicate reads left to right:

```vb
Matched = Payload.Narrative.ContainsAny("crypto", "gift card", "wire")
```

```vb
Matched = Payload.Email.NormalizeEmailAlias() = Payload.PreviousEmail.NormalizeEmailAlias()
```

### Extending with C# lambda expressions

Where the compiled library has no suitable method, the fluent vocabulary can be extended, without a release, with C#
lambda expressions. An administrator curates a named C# expression, evaluated against a single bound parameter called
`value` (effectively the lambda `value => ...`), in the `DictionaryEvalExpression` table. It is then used in a rule
exactly like a compiled extension method:

```vb
Matched = Payload.Email.Start().MatchEmailDomainEqual("gmail.com")
```

The expression is C#, not VB.NET, and is compiled once during model synchronisation by an embedded interpreter with a
deliberately small surface (members of `value`, `System.Math`, `System.Convert` and LINQ `Enumerable`). The feature is
off by default (`EnableDynamicEval`), and a curated name that is not registered fails the integrity check like any other
unknown token. Compiled extension methods that take delegates, such as `IfTrue(Action)` and `IfFalse(Action)`, also
accept lambdas. The mechanism, schema and safeguards are described in
[Curated Dynamic Expressions](../Configuration/Models/CuratedDynamicExpressions/index.html); anything used often should
be promoted to a proper extension method.

### Combining pipelines

Pipelines combine with `Or`, `And` and `Not`, and each is `True` only if it decided matched:

```vb
Matched = Payload.Amount.MatchGreater(10000) Or Payload.Narrative.MatchContainsAnyInList(List.HighRiskTerms)
Matched = TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).MatchGreater(0.3) And Payload.Country.MatchNotIn("GB", "IE")
Matched = Not Payload.Amount.MatchGreater(100)
```

The two pipelines may be of different types, and a pipeline may be combined with an ordinary Boolean expression, for
example `Payload.Amount.MatchGreater(100) And Payload.Count > 5`. Two rules apply:

* **Put the pipeline first.** A plain Boolean on the left, as in
  `Payload.Count > 5 And Payload.Amount.MatchGreater(100)`, does not compile, because VB cannot choose between the
  conversions. Swap the two sides, or wrap the pipeline with
  `IsMatched()`.
* **`Not` is true for everything except a matched pipeline.** An undecided, rejected, broken or errored pipeline is not
  matched, so `Not` of it is `True`. Use `Not` only where that is what you mean, and prefer a `Reject` step to a
  negation.

`X.IsMatched() Or Y.IsMatched()` also works everywhere, and is the form to use when unsure. Both sides of `Or` and `And`
are always evaluated. They do not short circuit, so `AndAlso` and `OrElse` are not available between pipelines, and a
guard that protects a later step belongs inside one pipeline as a `Require` step.

### Combining data sources

Because tokens and extension methods are all just typed values, evaluations across data sources need no special
constructs:

```vb
Matched = TTLCounter.CardPaymentsLastHour > 5 And Payload.CurrencyAmount.IsJustBelowThreshold(10000, 10)
```

## Guard Clauses

A guard clause (a bouncer) is the pattern of checking preconditions first and dropping out early. Inside a pipeline that
is `Require` and `Ensure`. What a linear pipeline cannot express is "use these thresholds for one product and those
thresholds for another". That is the legitimate use of `If`, selecting which expression applies, with the evaluation
itself still done by the expression:

```vb
If (Payload.ProductType = "Corporate" And Payload.CurrencyAmount.InRange(567.89, 987.65)) Then
    Return True
End If
If (Payload.ProductType <> "Corporate" And Payload.CurrencyAmount.InRange(123.45, 543.21)) Then
    Return True
End If
```

Nesting follows the same principle:

```vb
If (Payload.ProductType = "Corporate" And Payload.TenureDays < 90 And Payload.CurrencyAmount.InRange(567.89, 987.65)) Then
    Return True
End If
If (Payload.ProductType = "Corporate" And Payload.TenureDays >= 90 And Payload.CurrencyAmount.InRange(456.78, 876.54)) Then
    Return True
End If
If (Payload.ProductType <> "Corporate" And Payload.CurrencyAmount.InRange(123.45, 543.21)) Then
    Return True
End If
```

`Select Case` is available for the same purpose where there are more than two branches.

The permitted control-flow tokens are those on the hard coded list (`If`, `Then`, `Else`, `ElseIf`, `End If`, `Select`,
`Case`, `End Select`, `Not`, `AND`, `OR`, `Return`, `True`, `False`) plus anything registered in the `RuleScriptToken`
table. `Else` and `ElseIf` are supported. A construct that is in neither list fails the integrity check and the rule is
refused. A rule that fails the check is not compiled, is logged at `ERROR`, and its `Compiled` / `CompileError` columns
show why.

Where a choice has more than one branch, prefer early return, or a pipeline that assigns `Matched`, over `Else` and
`ElseIf`. `Else` and `ElseIf` are supported for the cases where the branching reads more clearly that way.

The same selection written with pipelines keeps `If` only for the choice of thresholds:

```vb
If (Payload.ProductType = "Corporate" And Payload.CurrencyAmount.InRange(567.89, 987.65)) Then
    Return True
End If
If (Payload.ProductType <> "Corporate" And Payload.CurrencyAmount.InRange(123.45, 543.21)) Then
    Return True
End If
```

As a matter of practice, keep `If` to the minimum needed to select between expressions and put the evaluation in the
expression. This keeps rules short, uniform and easy to compare against the [taxonomy](index.html).

## Early Return and Else

A rule is read from the top, and the first decision wins. Each case is therefore written as a guard that returns as soon
as it has decided, and otherwise falls through to the next case. This is the preferred form. `Else` and `ElseIf` are
supported as well, for a choice between two or more branches that each assign `Matched`. Prefer early return where it
reads as well, because each guard then stands alone:

```vb
If (Payload.ProductType = "Corporate" And Payload.CurrencyAmount.InRange(567.89, 987.65)) Then
    Return True
End If
If (Payload.ProductType <> "Corporate" And Payload.CurrencyAmount.InRange(123.45, 543.21)) Then
    Return True
End If
```

Each case stands on its own and reads without knowing the others, and no branch has to be remembered against its
opposite. Early return is the form the taxonomy examples use throughout.

The same choice written with `Else` is also valid:

```vb
If Payload.ProductType = "Corporate" Then
    Matched = Payload.CurrencyAmount.InRange(567.89, 987.65)
Else
    Matched = Payload.CurrencyAmount.InRange(123.45, 543.21)
End If
```

## Edge Cases

These are known edges of the extension methods. None changes behaviour today.

* **A stored calculation keeps whatever the function returns.** In a rule an undefined calculation such as `RatioOf`
  with a zero denominator is `NaN`, which fails every comparison, and an Abstraction Calculation stores that `NaN` as it
  is. Functions whose result is undefined when the denominator is zero end in `ZeroIfUndefined()`, which stores zero
  instead. A stored zero cannot be told apart from an undefined result, so where an undefined value must fail a
  comparison, leave the `NaN` in place or compute it inline in the rule.
* **An HTTP Adaptation value is a nullable number.** `Value.OrNaN()` and `Value.OrZero()` read it, but the `Value` token
  has to be permitted on the instance, as it is not one of the parser's hard coded tokens.
* **A pipeline cannot follow a plain Boolean.** `Payload.Count > 5 And Payload.Amount.MatchGreater(100)` does not
  compile, because VB cannot choose between the conversions, while
  `Payload.Amount.MatchGreater(100) And Payload.Count > 5` does.
  `Or` and `And` do not short circuit, and there is no `AndAlso` or `OrElse` between pipelines.
* **`Round(digits)` on a plain number** binds to the static `Double.Round` in VB. Begin with `Start()`.
* **Stateful sources are out of scope here.** The extension methods are stateless. Anything that needs to remember
  something between events is a TTL Counter. The AdvancedAnalytics model score is not available as a rule token; see
  [Generalisation, Adaptation and Model Thresholds](GeneralisationAdaptationAndModelThresholds.html).

## Performance and Safety

Each fragment is compiled once to a native delegate and cached by an MD5 hash of the generated class text, so identical
rules share one assembly. Extension methods are reviewed for real-time performance, which is the reason looping
constructs are withheld. The compile and cache algorithm is described in
[Rule Compilation Tokens and Extensions](../Configuration/Models/RuleCompilationAlgorithm/index.html).

## Where This Applies

The methodology applies to every place a VB.NET fragment is accepted: Activation Rules, Gateway Rules, Abstraction
Rules, Abstraction Calculations and Inline Functions. Only the type of `Matched` differs, as set out above. Inline
Scripts are the exception: they are complete classes stored in the database, including `Imports` statements, that
implement a specific interface, and are documented separately in
[Inline Scripts](../Configuration/Models/InlineScripts/index.html).

For the rule patterns built on this methodology, see the [Rule Taxonomy](index.html).

## Examples by Sub-Class

> **Validated.** Every fragment in this section is compiled individually through the Jube Parser, with its payload
> fields and its TTL Counter, Abstraction, Calculation, List and Sanction tokens declared, so each extension method and
> argument type is checked against the engine. Compilation does not validate a rule's logic or its thresholds, which are
> illustrative placeholders to be tuned on data.

### C1. Identity & Demographic (derived continuous variables)

| #     | Example                                           | Description                                                                                          | Jube construction                                                                                                                                                                                                                                                                                                                                      | Fragment                                                                                                                          | Notes                                                                                                                                                                                                            |
|-------|---------------------------------------------------|------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| C1-01 | **Customer age at transaction**                   | Derived from date of birth. Used by very young/very old thresholds in baseline and sequencing rules. | Customer age from date of birth, tested on the transaction date. Upstream field: Payload.DateOfBirth, supplied before Jube or by an inline-script hook. 6570 days is an illustrative placeholder for 18 years, ignoring leap days; the minor cut-off is a jurisdiction policy.                                                                         | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.DateOfBirth, 6570)`                                                  | Check: Age cut-offs for minors and very old customers depend on jurisdiction and product capacity rules; verify with compliance. Tag: Cross-cutting · Both · Customer                                            |
| C1-02 | **Account age in days**                           | Days since opening. Used to condition velocity thresholds (new accounts) and bust-out rules.         | Account age in days, tested on the transaction date. Upstream field: Payload.AccountOpenedDate, supplied before Jube or by an inline-script hook. The 30-day new-account window is an illustrative placeholder; the velocity condition is a separate counter rule.                                                                                     | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.AccountOpenedDate, 30)`                                              | Check: The new-account window (for example 30 days) is a policy choice; calibrate it from observed fraud timing. Tag: Cross-cutting · Both · Account                                                             |
| C1-03 | **Customer tenure in days**                       | Relationship length.                                                                                 | Customer tenure in days, tested on the transaction date. Upstream field: Payload.OnboardingDate, supplied before Jube or by an inline-script hook. The 90-day window is an illustrative placeholder.                                                                                                                                                   | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.OnboardingDate, 90)`                                                 | Tag: Cross-cutting · Both · Customer                                                                                                                                                                             |
| C1-04 | **Days since last credential change**             | Recency of password/phone/email change.                                                              | Credential changed within the last N days. Upstream field: Payload.LastCredentialChangeDate, supplied before Jube or by an inline-script hook. The 7-day window is an illustrative placeholder.                                                                                                                                                        | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.LastCredentialChangeDate, 7)`                                        | Tag: Cross-cutting · Fraud · Account                                                                                                                                                                             |
| C1-05 | **Days since last device enrolment**              | Recency of device registration.                                                                      | Device enrolled within the last N days. Upstream field: Payload.LastDeviceEnrolmentDate, supplied before Jube or by an inline-script hook. The 7-day window is an illustrative placeholder.                                                                                                                                                            | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.LastDeviceEnrolmentDate, 7)`                                         | Tag: Cross-cutting · Fraud · Account                                                                                                                                                                             |
| C1-06 | **Days since beneficiary added**                  | Age of payee.                                                                                        | Beneficiary added within the last N days. Upstream field: Payload.BeneficiaryAddedDate, supplied before Jube or by an inline-script hook. The 7-day new-payee window is an illustrative placeholder.                                                                                                                                                   | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.BeneficiaryAddedDate, 7)`                                            | Check: The new-payee window (for example 7 days) is a policy choice; calibrate it from fraud timing. Tag: Cross-cutting · Fraud · Beneficiary                                                                    |
| C1-07 | **Days since last transaction (dormancy)**        | Dormancy indicator.                                                                                  | Dormancy: transaction more than N days after the last transaction. Upstream field: Payload.LastTransactionDate, supplied before Jube or by an inline-script hook. The 180-day dormancy period is an illustrative placeholder.                                                                                                                          | `Matched = Payload.TransactionDate.MatchMoreThanDaysAfter(Payload.LastTransactionDate, 180)`                                      | Check: Dormancy periods are set by product policy and local regulation; verify the period applied to each jurisdiction. Tag: Cross-cutting · AML · Account                                                       |
| C1-08 | **Days since last KYC refresh**                   | KYC staleness.                                                                                       | KYC refresh overdue: transaction more than the tier interval after the last refresh. Upstream fields: Payload.LastKycRefreshDate and Payload.KycRefreshIntervalDays (the tier interval, from a table keyed on CustomerId), supplied before Jube or by an inline-script hook. Intervals are set by the applicable regime and firm policy.               | `Matched = Payload.TransactionDate.MatchMoreThanDaysAfter(Payload.LastKycRefreshDate, Payload.KycRefreshIntervalDays)`            | Check: Refresh intervals by risk tier come from the applicable AML regime and firm policy; verify the intervals in force. Tag: Cross-cutting · AML · Customer                                                    |
| C1-09 | **Time since login**                              | Seconds between login and payment.                                                                   | Payload.LoginTime is carried on the payment, so the test is event-scoped and needs no counter. The two-minute window is a placeholder to derive from data, and WithinMinutesOf is symmetric, so pair it with a direction test if the direction matters.                                                                                                | `Matched = Payload.TransactionDate.MatchWithinMinutesOf(Payload.LoginTime, 2)`                                                    | Tag: Cross-cutting · Fraud · Session                                                                                                                                                                             |
| C1-10 | **Hour-of-day and day-of-week features**          | Temporal context from timestamp.                                                                     | Both are direct tests on the event timestamp. The hour is a transformer that begins a pipeline and tests the hour set, and weekend is its own DateTime test, Payload.TransactionDate.MatchIsWeekend(). As model features the same derivations must be supplied as payload fields or model inputs, and the hour is read from the timestamp as supplied. | `Matched = Payload.TransactionDate.HourOfDay().MatchIn(0, 1, 2, 3, 4, 5)`                                                         | Tag: Cross-cutting · Both · Event                                                                                                                                                                                |
| C1-11 | **Local time at counterparty location**           | Time zone-adjusted hour.                                                                             | Payload.CounterpartyZone is a placeholder field holding an IANA zone id that the caller supplies, since a Dictionary returns numbers and cannot map a country to a zone. The test reads the timestamp as UTC and converts it, so daylight saving is handled by the zone id, and the 8 to 18 window is a policy choice.                                 | `Matched = Payload.TransactionDate.MatchOutsideBusinessHoursInZone(Payload.CounterpartyZone, 8, 18)`                              | Tag: Cross-cutting · Both · Event                                                                                                                                                                                |
| C1-12 | **Age versus product**                            | e.g. minor account with adult-sized flow; elderly with crypto.                                       | Customer is a minor and the amount exceeds the product limit. Upstream fields: Payload.CustomerAge (derived upstream from date of birth), Payload.ProductMinorLimit (the product limit, from a table keyed on ProductCode), supplied before Jube or by an inline-script hook. The age 18 is an illustrative placeholder.                               | `If (Payload.CurrencyAmount > Payload.ProductMinorLimit And Payload.CustomerAge < 18) Then Return True`                           | Tag: Cross-cutting · Both · Customer                                                                                                                                                                             |
| C1-13 | **Age of company at transaction**                 | Corporate age.                                                                                       | Company age in days, tested on the transaction date. Upstream field: Payload.IncorporationDate, supplied before Jube or by an inline-script hook. The 365-day window is an illustrative placeholder.                                                                                                                                                   | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.IncorporationDate, 365)`                                             | Tag: Cross-cutting · AML · Entity                                                                                                                                                                                |
| C1-14 | **Age of email address/domain**                   | Email domain registration age.                                                                       | Email domain registration age below N days. Upstream field: Payload.EmailDomainAgeDays, supplied before Jube or by an inline-script hook. The 30-day cut-off is an illustrative placeholder.                                                                                                                                                           | `Matched = Payload.EmailDomainAgeDays.MatchLess(30)`                                                                              | Check: Registry data licensing and freshness vary by vendor; verify the source. Tag: Cross-cutting · Fraud · Email                                                                                               |
| C1-15 | **Age of phone number / carrier tenure**          | Recently issued numbers.                                                                             | Phone number issued less than N days ago. Upstream field: Payload.PhoneNumberAgeDays, supplied before Jube or by an inline-script hook. The 90-day cut-off is an illustrative placeholder.                                                                                                                                                             | `Matched = Payload.PhoneNumberAgeDays.MatchLess(90)`                                                                              | Tag: Cross-cutting · Fraud · Phone                                                                                                                                                                               |
| C1-16 | **Identity-document age/issue date**              | Newly issued documents.                                                                              | Identity document issued within the last N days, tested on the transaction date. Upstream field: Payload.DocumentIssueDate, supplied before Jube or by an inline-script hook. The 30-day window is an illustrative placeholder.                                                                                                                        | `Matched = Payload.TransactionDate.MatchWithinDaysOf(Payload.DocumentIssueDate, 30)`                                              | Check: The recently-issued window is a policy choice. Tag: Cross-cutting · Fraud · Document                                                                                                                      |
| C1-17 | **Name-similarity score between related parties** | Edit-distance between names (payer/payee).                                                           | Both names are on the payment, so the comparison is event-scoped. Payload.PayerName is a placeholder field name, and Payload.PayeeName is the field the Jube docs use for this test. Jaro-Winkler favours shared prefixes, and the 0.9 threshold must be tuned on labelled name pairs.                                                                 | `Matched = Payload.PayerName.MatchJaroWinklerAtLeast(Payload.PayeeName, 0.9)`                                                     | Tag: Cross-cutting · Both · Pair                                                                                                                                                                                 |
| C1-18 | **Address-distance between related parties**      | Distance between addresses.                                                                          | Distance between payer and payee addresses. Upstream fields: Payload.PayerLatitude, Payload.PayerLongitude, Payload.PayeeLatitude and Payload.PayeeLongitude, supplied before Jube or by an inline-script hook. Distance 50 km is an illustrative placeholder.                                                                                         | `Matched = Payload.PayerLatitude.MatchDistanceKmAbove(Payload.PayerLongitude, Payload.PayeeLatitude, Payload.PayeeLongitude, 50)` | Tag: Cross-cutting · Both · Pair                                                                                                                                                                                 |
| C1-19 | **Declared income band / occupation risk score**  | Demographic risk attributes.                                                                         | Dictionary.OccupationRiskScore is a numeric lookup keyed on Payload.OccupationCode, as configured on the Dictionary, and it is batch-refreshed from the customer file. An income band uses the same shape keyed on Payload.CustomerId. Declared income is self-reported and stale, so treat it as a weak input.                                        | `Matched = Dictionary.OccupationRiskScore.MatchGreaterOrEqual(4)`                                                                 | Check: Declared income and occupation risk tiers are firm-defined; confirm the scale used by the Dictionary. Tag: Cross-cutting · AML · Customer                                                                 |
| C1-20 | **Customer risk rating**                          | Rating consumed as a conditional input to thresholds.                                                | The velocity is a TTL counter, incremented by an Activation Rule on each card payment, and the customer rating is a Dictionary keyed on Payload.CustomerId. The rule requires the counter first, so the rating only decides when the velocity has fired; the rating is batch-refreshed at load.                                                        | `Matched = TTLCounter.CardPaymentsLastHour.RequireGreater(5).Against(Dictionary.CustomerRiskRating).MatchGreaterOrEqual(4)`       | Tag: Cross-cutting · AML · Customer                                                                                                                                                                              |
| C1-21 | **Vulnerability flag**                            | Customer flagged as vulnerable (affects thresholds and treatment).                                   | A membership List of customer ids is the natural shape for a flag, and it changes without republishing the rule. Treatment differences belong in the per-rule thresholds, not in the flag itself.                                                                                                                                                      | `Matched = Payload.CustomerId.MatchInList(List.VulnerableCustomers)`                                                              | Check: Vulnerable-customer treatment obligations (for example UK Consumer Duty or equivalent) are regulatory; verify the obligation and the flag's source with compliance. Tag: Cross-cutting · Fraud · Customer |

### C2. Reference Data (Lists and Dictionaries)

| #     | Example                                           | Description                                                                  | Jube construction                                                                                                                                                                                                                                                                                                                                                 | Fragment                                                                                                                  | Notes                                                                                                                                                                                                                                                                                                                  |
|-------|---------------------------------------------------|------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| C2-01 | **Sanctions lists**                               | Consolidated international lists, refreshed daily.                           | Consolidated lists load through Sanctions configuration, not through a fragment, and the daily refresh is a loader task. The Sanction value is retrieved by GetValueOrThrow and compared, not read as a Boolean.                                                                                                                                                  | `Matched = Sanction.PartyName.MatchLessOrEqual(1)`                                                                        | Check: Sanctions list sources and screening obligations (OFAC, UN, EU, HMT and others) differ by jurisdiction; verify coverage and refresh cadence with compliance. Tag: Reference · AML                                                                                                                               |
| C2-02 | **PEP and adverse media lists**                   | Third-party data.                                                            | A PEP name definition is a Sanction compared with a distance, and party-level adverse media is a membership List keyed on the customer id. The two are combined with Or in one Activation Rule.                                                                                                                                                                   | `Matched = Sanction.PartyName.MatchLessOrEqual(1) Or Payload.CustomerId.MatchInList(List.AdverseMediaParties)`            | Check: PEP definitions differ by jurisdiction (domestic and foreign PEPs, family members and close associates); verify the definition with compliance. Tag: Reference · AML                                                                                                                                            |
| C2-03 | **High-risk country lists (FATF, EU, internal)**  | Country tiers.                                                               | The high-risk country membership is a List, and tiered country risk is numeric reference data in a Dictionary keyed on Payload.CountryCode, as in the methodology's country risk example.                                                                                                                                                                         | `Matched = Payload.CountryCode.MatchInList(List.HighRiskCountries)`                                                       | Check: FATF grey and black lists and the EU high-risk third-country list change several times a year; load the versions in force and record their effective dates. Tag: Reference · AML                                                                                                                                |
| C2-04 | **MCC risk list**                                 | Merchant category risk tiers.                                                | A risk tier per merchant category is a Dictionary keyed on Payload.MerchantCategoryCode, so the tier is a number. A plain set of blocked codes would be a List instead.                                                                                                                                                                                           | `Matched = Dictionary.MccRisk.MatchGreaterOrEqual(3)`                                                                     | Tag: Reference · Both                                                                                                                                                                                                                                                                                                  |
| C2-05 | **BIN table**                                     | Issuer, country, card type, prepaid flag.                                    | Single event. The first six digits of the card, from the processor, are supplied as BinPrefix and tested against the prepaid BIN List. BIN data is licensed and refreshed by its provider.                                                                                                                                                                        | `Matched = Payload.BinPrefix.MatchInList(List.PrepaidBinPrefixes)`                                                        | Check: BIN data is licensed and refreshed by the vendor; verify the licence and refresh terms. Tag: Reference · Fraud                                                                                                                                                                                                  |
| C2-06 | **Device, IP, email, phone blacklists**           | Technical attribute lists.                                                   | The device and IP membership Lists are combined with Or in one Activation Rule, and the email and phone Lists take the same shape in their own Activation Rules, because chaining more than two Or terms does not compile. Deterministic blocks should be rule overrides that do not depend on a model.                                                           | `Matched = Payload.DeviceId.MatchInList(List.DeviceBlacklist) Or Payload.IpAddress.MatchInList(List.IpBlacklist)`         | Tag: Reference · Fraud                                                                                                                                                                                                                                                                                                 |
| C2-07 | **Mule account list**                             | Known mule accounts/IBANs.                                                   | A membership List of mule accounts maintained by operators. Normalise the beneficiary account to the same form as the list entries before the lookup, so that entries and payload agree.                                                                                                                                                                          | `Matched = Payload.BeneficiaryAccount.MatchInList(List.MuleAccounts)`                                                     | Tag: Reference · Both                                                                                                                                                                                                                                                                                                  |
| C2-08 | **Trusted payee/device allow-lists**              | Suppression lists.                                                           | The trusted payee List is used as a suppression at the head of the pipeline. The Break step decides the pipeline as broken, so the later Match cannot fire, and the rule does not match. Keep sanctions and hard blocks in rules outside the suppressed pipeline, so a suppression never hides them.                                                              | `Matched = Payload.BeneficiaryAccount.BreakInList(List.TrustedPayees).Against(Payload.CurrencyAmount).MatchGreater(5000)` | Tag: Reference · Both                                                                                                                                                                                                                                                                                                  |
| C2-09 | **Disposable email domain list**                  | Throwaway email providers.                                                   | The disposable domain List is external and changes often, so refresh it on a schedule. The step tests the domain of the email against the List of domain strings.                                                                                                                                                                                                 | `Matched = Payload.Email.MatchEmailDomainInList(List.DisposableEmailDomains)`                                             | Tag: Reference · Fraud                                                                                                                                                                                                                                                                                                 |
| C2-10 | **Hosting/data-centre ASN list**                  | Non-residential networks.                                                    | A List of hosting CIDR ranges is tested against the IP address in one step, which is true when any range contains it, and the step takes the List token directly. An ASN only list would need Payload.Asn from IP enrichment, which is external.                                                                                                                  | `Matched = Payload.IpAddress.MatchIsInAnyCidr(List.HostingCidrs)`                                                         | Check: Cloud and hosting provider ranges change frequently; verify the source and refresh cadence. Tag: Reference · Fraud                                                                                                                                                                                              |
| C2-11 | **Keyword lists for text screening**              | Term lists per typology (scam, terrorism, drugs).                            | One List per typology, each its own rule. Keyword matching is brittle, so pair the rule with the text classifier score before acting on it.                                                                                                                                                                                                                       | `Matched = Payload.Narrative.MatchContainsAnyInListIgnoreCase(List.ScamTerms)`                                            | Tag: Reference · Both                                                                                                                                                                                                                                                                                                  |
| C2-12 | **Customer last-state dictionary (KVP)**          | Latest state per entity for sequencing (e.g. last event type, last country). | Single event. The previous country is supplied upstream from the caller's own state store as PreviousCountryCode, and compared with the current country.                                                                                                                                                                                                          | `If (Payload.CountryCode <> Payload.PreviousCountryCode) Then Return True`                                                | Tag: Reference · Both                                                                                                                                                                                                                                                                                                  |
| C2-13 | **Holiday and calendar lists**                    | Public holidays by country.                                                  | One List per country holding the public holiday dates as yyyy-MM-dd keys, and the rule tests the local date in that form. Payload.LocalDayKey is a placeholder field that the caller fills with the local date. Weekday tests use the zone form of the DateTime test, IsWeekdayInZone, and business-day logic should use the bank calendar, which differs.        | `Matched = Payload.LocalDayKey.MatchInList(List.PublicHolidaysGB)`                                                        | Check: Public-holiday and bank-holiday calendars differ by jurisdiction and change each year; load the official calendar for each country and year. Tag: Reference · Both                                                                                                                                              |
| C2-14 | **Currency reporting thresholds by jurisdiction** | Limits used by structuring rules.                                            | The reporting threshold is numeric reference data in a Dictionary keyed on Payload.JurisdictionCode, and the percent-below calculation reads it against the amount, so the test needs no literal threshold in the rule, and the 0 to 10 percent band is illustrative. Aggregate structuring adds a TTL counter sum over a window read against the same threshold. | `Matched = Payload.CurrencyAmount.PercentBelowThreshold(Dictionary.ReportingThreshold).MatchInRange(0, 10)`               | Check: Reporting thresholds are regulatory and change. US Currency Transaction Report threshold is USD 10,000 (31 CFR 1010.311). EU AMLR sets a EUR 10,000 cash-payment cap applying from 2027. Other jurisdictions differ; verify each threshold, currency and aggregation rule with compliance. Tag: Reference · AML |
| C2-16 | **Entity-to-peer-group mapping**                  | Segment mappings for baselines.                                              | The Dictionary is keyed on Payload.CustomerId and holds the peer group's velocity limit, so one lookup gives both the mapping and the group threshold. The counter is compared with that limit through a difference calculation, which is positive when the counter is over the limit. A group-id keyed Dictionary would need Payload.PeerGroupId on the event.   | `Matched = TTLCounter.CardPaymentsLastHour.DifferenceFrom(Dictionary.PeerGroupVelocityLimit).MatchGreater(0)`             | Tag: Reference · AML                                                                                                                                                                                                                                                                                                   |

### C3. Generalisation, Adaptation & Model Thresholds

| #     | Example                                               | Description                                                                                     | Jube construction                                                                                                                                                                                                                                                                                                                      | Fragment                                                                                                                 | Notes                                                                                                                           |
|-------|-------------------------------------------------------|-------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|
| C3-01 | **Supervised model score threshold**                  | Fraud/AML model score is thresholded just like a continuous value (e.g. score ≥ 0.9 → decline). | The score is a model output, read as ExhaustiveAdaptation.FraudScore and compared directly, so the cut-off lives in the Activation Rule, is versioned, and is re-derived after retraining.                                                                                                                                             | `Matched = ExhaustiveAdaptation.FraudScore >= 0.9`                                                                       | Check: The 0.9 cut-off is illustrative, not calibrated; derive it from alert capacity or target precision. Tag: Model · Both    |
| C3-02 | **Anomaly-detection score threshold**                 | Isolation forest/autoencoder score compared with cut-off.                                       | The anomaly score is an HTTP Adaptation, read as a nullable number through OrNaN. Anomaly scores are ranks unless calibrated, so set the cut-off from the score distribution.                                                                                                                                                          | `Matched = HTTPAdaptation.AnomalyScore.OrNaN().MatchGreater(0.8)`                                                        | Check: The 0.8 cut-off is illustrative, not calibrated. Tag: Model · Both                                                       |
| C3-03 | **Score band routing**                                | Low/medium/high bands route to accept/challenge/decline.                                        | Two Activation Rules on the same score give a decline band and a challenge band, each with its own response elevation. The challenge band is its own rule, Matched = ExhaustiveAdaptation.FraudScore >= 0.7 And ExhaustiveAdaptation.FraudScore < 0.9.                                                                                 | `Matched = ExhaustiveAdaptation.FraudScore >= 0.9`                                                                       | Check: Band edges are illustrative. Tag: Model · Both                                                                           |
| C3-05 | **Counter-derived model features**                    | Primary taxonomy counters (velocity, cardinality, ratios) used as model inputs.                 | Counters are direct model inputs. The input processing types are Payload, Dictionary, TTLCounter, Abstraction and AbstractionCalculation, each identified by its type and name, and recall reads them without any fragment. The fragment shows the same counter read by an Activation Rule; recall needs only the variable list entry. | `Matched = TTLCounter.CardPaymentsLastHour.MatchGreater(5)`                                                              | Check: Confirm in the deployed model configuration that each counter name is listed with its processing type. Tag: Model · Both |
| C3-06 | **Champion/challenger rule thresholds**               | Competing thresholds evaluated in parallel.                                                     | The champion acts through its Activation Rule. The challenger is recalled and reported with no Activation Rule that acts on it, so it is proven on live traffic before it drives decisions and is compared offline.                                                                                                                    | `Matched = ExhaustiveAdaptation.FraudScore >= 0.9`                                                                       | Tag: Model · Both                                                                                                               |
| C3-08 | **Combined rule and score**                           | Rule-based gate AND model score above a lower cut-off (reduce false positives).                 | A counter gates the rule and the model score confirms it, so the score reduces the false positives of a noisy counter. The counter is required first, then the score is read against it.                                                                                                                                               | `Matched = TTLCounter.CardPaymentsLastHour.RequireGreater(5).Against(ExhaustiveAdaptation.FraudScore).MatchGreater(0.4)` | Check: The 0.4 gate is illustrative. Tag: Model · Both                                                                          |
| C3-09 | **Graph-embedding or community-risk score threshold** | Network model score as an input to link rules.                                                  | The community risk score is returned by the external graph model as the HTTP Adaptation CommunityRisk, read through OrNaN. The graph computation itself stays outside Jube.                                                                                                                                                            | `Matched = HTTPAdaptation.CommunityRisk.OrNaN().MatchGreater(0.8)`                                                       | Tag: Model · AML                                                                                                                |
| C3-10 | **Text-classifier score on narrative**                | NLP score thresholded in class 7 rules.                                                         | The narrative score is returned by the NLP classifier as the HTTP Adaptation NarrativeScore, read through OrNaN. The classifier runs outside the rule.                                                                                                                                                                                 | `Matched = HTTPAdaptation.NarrativeScore.OrNaN().MatchGreater(0.8)`                                                      | Check: The 0.8 cut-off is illustrative. Tag: Model · Both                                                                       |
| C3-11 | **Behavioural-biometric score threshold**             | Session score used in baseline rules.                                                           | The session score arrives either as a vendor payload field, Payload.BiometricScore, or as an HTTP Adaptation read with OrNaN, and the threshold is A either way. A low score is the risk direction.                                                                                                                                    | `Matched = Payload.BiometricScore.MatchLess(0.2)`                                                                        | Check: Vendor score scales and cut-offs differ; use the vendor's documented scale. Tag: Model · Fraud                           |

## Conventions Used in the Examples

The examples in the taxonomy pages follow a few conventions, so that each one can be read on its own.

- **Upstream fields.** Enrichment, vendor verdicts and derived values are produced before Jube, or by an inline-script
  hook that exposes them as a payload field. An example that tests such a field assumes the field is supplied on the
  event, and the construction line names it. Jube does not compute the field.
- **Placeholder names.** Field, List, Dictionary and TTL Counter names are placeholders chosen to read well. Use the
  names your payload actually carries.
- **Flags.** A boolean payload field is named as a flag, for example `CredentialBreachedFlag`, and is tested with
  `MatchIsTrue()` or `MatchIsFalse()`.
- **Thresholds.** Every number is an illustrative placeholder, to be tuned on data, and is never a recommendation.
- **Types.** Jube types a payload field by the data type declared for it in the request XPath, not by its name. The
  names in the examples are chosen so the reader can see the intended type.
