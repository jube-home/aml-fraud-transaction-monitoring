---
layout: default
title: Rule Authoring Methodology
nav_order: 2
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

```vb
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
  does nothing does not match. The two forms below are equivalent:

  ```vb
  Matched = Payload.CurrencyAmount > 123.45
  ```

  ```vb
  If (Payload.CurrencyAmount > 123.45) Then
      Return True
  End If
  ```

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
[Static and Match Screening](RuleTaxonmyTypes/StaticAndMatchScreening.html) and
[Unstructured Text Screening](RuleTaxonmyTypes/UnstructuredTextScreening.html) for the patterns that use them.

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
in [Event Sequencing](RuleTaxonmyTypes/EventSequencing.html). See
[Dictionary](../Configuration/Models/Dictionaries/index.html).

### Choosing Between Reference Data Constructs

| Requirement                                     | Use                                                                                       |
|-------------------------------------------------|-------------------------------------------------------------------------------------------|
| Is this value on a set that analysts maintain?  | List                                                                                      |
| Enrich the event with a score or number per key | Dictionary                                                                                |
| Regulatory list with fuzzy name matching        | Sanction, see [Static and Match Screening](RuleTaxonmyTypes/StaticAndMatchScreening.html) |
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
| Simpler rules      | The rule is a comparison, which is what the [taxonomy](RuleTaxonomy.html) and the pipeline steps are for.                                                                                                                                                |

Written inline, a rule mixes calculation and decision:

```vb
Matched = TTLCounter.DeclinedPayments24h.MatchRatioAbove(TTLCounter.Payments24h, 0.5)
```

Written as an Abstraction Calculation `DeclineRate24h`, with the function:

```vb
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

The statement form assigns inside an `If`:

```vb
If Payload.CurrencyAmount > 123.45 Then
    Matched = True
End If
```

The expression form assigns the result of the evaluation directly, which removes the `If` altogether:

```vb
Matched = Payload.CurrencyAmount > 123.45
```

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
Matched = Payload.Email.IsGmailEmail
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
If Payload.ProductType = "Corporate" Then
    Matched = Payload.CurrencyAmount.InRange(567.89, 987.65)
Else
    Matched = Payload.CurrencyAmount.InRange(123.45, 543.21)
End If
```

Nesting follows the same principle:

```vb
If Payload.ProductType = "Corporate" Then
    If Payload.TimeOnBooks < 90 Then
        Matched = Payload.CurrencyAmount.InRange(567.89, 987.65)
    Else
        Matched = Payload.CurrencyAmount.InRange(456.78, 876.54)
    End If
Else
    Matched = Payload.CurrencyAmount.InRange(123.45, 543.21)
End If
```

`Select Case` is available for the same purpose where there are more than two branches.

The permitted control-flow tokens are those on the hard coded list (`If`, `Then`, `End If`, `Select`, `Case`,
`End Select`, `Not`, `AND`, `OR`, `Return`, `True`, `False`) plus anything registered in the `RuleScriptToken` table. A
construct that is absent from both, such as `Else`/`ElseIf` on an instance that has not registered them, fails the
integrity check and the rule is refused, so confirm the token exists on the target instance before relying on it. A rule
that fails the check is not compiled, is logged at `ERROR`, and its `Compiled` / `CompileError` columns show why.

The same selection written with pipelines keeps `If` only for the choice of thresholds:

```vb
If Payload.ProductType = "Corporate" Then
    Matched = Payload.CurrencyAmount.RequireInRange(567.89, 987.65).OtherwiseMatch()
Else
    Matched = Payload.CurrencyAmount.RequireInRange(123.45, 543.21).OtherwiseMatch()
End If
```

As a matter of practice, keep `If` to the minimum needed to select between expressions and put the evaluation in the
expression. This keeps rules short, uniform and easy to compare against the [taxonomy](RuleTaxonomy.html).

## Edge Cases and Open Decisions

These are known edges of the extension methods, and decisions deliberately left for later work. None changes behaviour
today.

* **A stored calculation keeps whatever the function returns.** In a rule an undefined calculation such as `RatioOf`
  with a zero denominator is `NaN`, which fails every comparison, and an Abstraction Calculation stores that `NaN` as it
  is. The removed arithmetic types stored zero, so the migrated functions end in `ZeroIfUndefined()`. A stored zero
  cannot be told apart from an undefined result, so where an undefined value must fail a comparison, leave the `NaN` in
  place or compute it inline in the rule. Whether the engine should normalise a stored `NaN` itself is undecided, and it
  is a change to stored data and reporting, not to the extension methods.
* **An HTTP Adaptation value is a nullable number.** `Value.OrNaN()` and `Value.OrZero()` read it, but the `Value` token
  has to be permitted on the instance, as it is not one of the parser's hard coded tokens. A `HttpAdaptationOrNaN`
  token, or a `double?` form of the pipeline steps, would remove the explicit `.Value`, and would be a parser change.
* **A pipeline cannot follow a plain Boolean.** `Payload.Count > 5 And Payload.Amount.MatchGreater(100)` does not
  compile, because VB cannot choose between the conversions, while
  `Payload.Amount.MatchGreater(100) And Payload.Count > 5` does.
  `Or` and `And` do not short circuit, and there is no `AndAlso` or `OrElse` between pipelines.
* **`Round(digits)` on a plain number** binds to the static `Double.Round` in VB. Begin with `Start()`.
* **Stateful sources are out of scope here.** The extension methods are stateless. Anything that needs to remember
  something between events is a TTL Counter, and the rule token for the AskJooby Analytics models is still to be
  decided, after which the model thresholds page will need its name.

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

For the rule patterns built on this methodology, see the [Rule Taxonomy](RuleTaxonomy.html).
