---
layout: default
title: Rule Pipeline Reference
nav_order: 3
parent: Rule Taxonomy
---

# Rule Pipeline Reference

The complete list of pipeline steps. For what a pipeline is and when to use one, read
[Rule Authoring Methodology](RuleAuthoringMethodology.html) first.

## The step grammar

Every test in the tables below is available in five forms, by prefixing its name:

| Prefix    | Test holds       | Test fails       |
|-----------|------------------|------------------|
| `Match`   | Decide: matched  | Continue         |
| `Reject`  | Decide: rejected | Continue         |
| `Break`   | Decide: broken   | Continue         |
| `Require` | Continue         | Decide: rejected |
| `Ensure`  | Continue         | Decide: broken   |

So `Greater` in the Double table is available as `MatchGreater`, `RejectGreater`, `BreakGreater`, `RequireGreater` and
`EnsureGreater`. Once a pipeline has decided, every step after it is skipped. A pipeline converts to `True` only when it
has decided matched, otherwise to `False`.

The five outcomes are:

| Outcome   | Meaning                                                          |
|-----------|------------------------------------------------------------------|
| Undecided | Nothing has decided yet, steps continue to run                   |
| Matched   | A `Match` step fired; the only outcome that converts to `True`   |
| Rejected  | A `Reject` step fired, or a `Require` guard failed               |
| Broken    | A `Break` step fired, or an `Ensure` precondition failed         |
| Errored   | A step could not evaluate, for example text that is not a number |

## Beginning a pipeline

A test step called on a plain value begins a pipeline. There is no `Start()`:

```vb
Matched = Payload.Amount.MatchGreater(100)
Matched = TTLCounter.CardPaymentsLastHour.RequireGreater(5).Against(Payload.CurrencyAmount).MatchIsJustBelowThreshold(10000, 10)
```

Every test in the tables below exists for a plain `Double`, `Integer`, `String`, `DateTime` and `Boolean` value, and
again on a pipeline, so the first step and the later steps are written the same way.

The steps that only make sense once a pipeline exists cannot begin one: `Against`, `Label`, `OtherwiseMatch`,
`OtherwiseReject`, `Reset`, `Fail`, `AddScore`, `AddScoreWhen`, `CapScore`, `MatchWhen`, `RejectWhen`, `BreakWhen`,
`RequireThat`, `EnsureThat` and the score decisions. Begin with a test step, or with `Start()`, for example
`Payload.Amount.Start().AddScoreWhen(Payload.Amount > 100, 0.5)`.

A chain may also begin with a transformer. `Lower`, `Upper`, the `Parse` steps, `HourOfDay` and `ToZone` begin a
pipeline and are null safe.
`Plus`, `Minus`, `Times` and `DividedBy` on a plain number just return a number (`NaN` if it is undefined), and
`ToDoubleOrNaN()` on text returns a number, `NaN` if it is not one, so they fit a rule that returns a number such as an
Abstraction Calculation. A few transformer names are also plain methods, and on a plain value the plain method is used,
which returns an ordinary value that the next test step then begins from:

| Name                                                  | On a plain value                    | Note                                                                                                                                                                          |
|-------------------------------------------------------|-------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Trim`, `Length`                                      | The `String` member                 | `Trim` throws on null, which faults the rule so it does not match. Use `Payload.Name.Start().Trim()` to be null safe. `Length` is a property, so write `Payload.Name.Length`. |
| `Abs`, `AgeInDays`                                    | The library method                  | Returns a plain number.                                                                                                                                                       |
| `Round`                                               | The library method with no argument | `Round(digits)` on a plain `Double` is read by VB as the static `Double.Round`. Use `Payload.Amount.Start().Round(2)`.                                                        |
| `FuzzyBestScore`, `FuzzyMatchCount`, `FuzzyBestMatch` | The library method                  | Returns a plain number or string.                                                                                                                                             |

## Control steps

These are available on a pipeline of any value type.

| Step                                                                                                  | Description                                                                                                                                      |
|-------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| `Start()`                                                                                             | Optional, and harmless to repeat. Every test step called on a plain value begins a pipeline by itself.                                           |
| `Against(value)`                                                                                      | Switches to testing another value, keeping the outcome, score and label.                                                                         |
| `Match()`, `Reject()`, `Break()`                                                                      | Decide unconditionally, if not already decided.                                                                                                  |
| `Fail(label)`                                                                                         | Decide errored, with a reason.                                                                                                                   |
| `OtherwiseMatch()`, `OtherwiseReject()`                                                               | Decide only if nothing has decided yet (the `Else`).                                                                                             |
| `Reset()`                                                                                             | Return to undecided and clear the score and label.                                                                                               |
| `Label(text)`                                                                                         | Attach a reason that is carried to the decision.                                                                                                 |
| `MatchWhen(condition)`, `RejectWhen(condition)`, `BreakWhen(condition)`                               | The Match, Reject and Break forms driven by any Boolean expression.                                                                              |
| `RequireThat(condition)`, `EnsureThat(condition)`                                                     | The Require and Ensure forms driven by any Boolean expression.                                                                                   |
| `AddScore(weight)`                                                                                    | Add to the score (negative subtracts).                                                                                                           |
| `AddScoreWhen(condition, weight)`                                                                     | Add to the score only if the condition holds.                                                                                                    |
| `CapScore(maximum)`                                                                                   | Limit the score.                                                                                                                                 |
| `MatchIfScoreAtLeast(k)`, `BreakIfScoreAtLeast(k)`, `RejectIfScoreBelow(k)`, `RequireScoreAtLeast(k)` | Decide from the accumulated score.                                                                                                               |
| `IsMatched()`, `IsRejected()`, `IsBroken()`, `IsUndecided()`, `IsErrored()`                           | Read the outcome as a Boolean.                                                                                                                   |
| `ToBoolean()`                                                                                         | The Boolean the pipeline converts to.                                                                                                            |
| `Or`, `And`, `Not`                                                                                    | Combine pipelines, and a pipeline with a Boolean written after it. See [Combining pipelines](RuleAuthoringMethodology.html#combining-pipelines). |
| `ToScore()`                                                                                           | The accumulated score, for rules that return a number.                                                                                           |

## Converting and transforming

These change the value being tested, and leave the outcome untouched.

| Step                                                               | On                       | Description                                                                                                             |
|--------------------------------------------------------------------|--------------------------|-------------------------------------------------------------------------------------------------------------------------|
| `ParseDouble()`, `ParseInteger()`, `ParseDate()`, `ParseBoolean()` | String                   | Read text as another type using the invariant culture. Text that is not valid errors the pipeline rather than throwing. |
| `Trim()`, `Lower()`, `Upper()`                                     | String                   | Trim white space, or change case, invariant. Null stays null.                                                           |
| `Length()`                                                         | String                   | The length as an integer, zero for null.                                                                                |
| `Abs()`, `Round(digits)`, `Plus(x)`, `Minus(x)`, `Times(x)`        | Double                   | Arithmetic on the value.                                                                                                |
| `DividedBy(x)`                                                     | Double                   | Division. A zero divisor errors the pipeline.                                                                           |
| `ToNumber()`                                                       | Double, Integer pipeline | The value as a plain number, `NaN` if the pipeline errored, for a rule that returns a number.                           |
| `HourOfDay()`, `AgeInDays()`                                       | DateTime                 | The hour, or the whole days since the date, as an integer.                                                              |

## Tests

Tests are null safe. A null or `NaN` value fails the test, never throws, except where the test name says it is testing
for null or invalidity (`IsNull`, `IsNotValidEmail`), which are true for null.

### Double tests

Begin from `Payload.<Numeric field>`, or from any token of that type (`TTLCounter`, `Abstraction`, `Dictionary` and so
on).

| Test                         | Arguments                                                                                               | True when                                                                                                                                                       |
|------------------------------|---------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Greater`                    | `double other`                                                                                          | The value is greater than the argument.                                                                                                                         |
| `GreaterOrEqual`             | `double other`                                                                                          | The value is greater than or equal to the argument.                                                                                                             |
| `Less`                       | `double other`                                                                                          | The value is less than the argument.                                                                                                                            |
| `LessOrEqual`                | `double other`                                                                                          | The value is less than or equal to the argument.                                                                                                                |
| `Equal`                      | `double other`                                                                                          | The value equals the argument. `NaN` never equals anything.                                                                                                     |
| `NotEqual`                   | `double other`                                                                                          | The value differs from the argument. `NaN` on either side is false.                                                                                             |
| `Between`                    | `double minimum, double maximum`                                                                        | Strictly between the two bounds (both bounds excluded).                                                                                                         |
| `InRange`                    | `double minimum, double maximum`                                                                        | Between the two bounds, both bounds included.                                                                                                                   |
| `OutsideRange`               | `double minimum, double maximum`                                                                        | Below the minimum or above the maximum. `NaN` is false.                                                                                                         |
| `In`                         | `double... values`                                                                                      | The value is one of the listed numbers.                                                                                                                         |
| `NotIn`                      | `double... values`                                                                                      | The value is none of the listed numbers.                                                                                                                        |
| `IsZero`                     | none                                                                                                    | The value is zero.                                                                                                                                              |
| `IsPositive`                 | none                                                                                                    | The value is greater than zero.                                                                                                                                 |
| `IsNegative`                 | none                                                                                                    | The value is less than zero.                                                                                                                                    |
| `IsNaN`                      | none                                                                                                    | The value is not a number.                                                                                                                                      |
| `IsFinite`                   | none                                                                                                    | The value is neither infinite nor `NaN`.                                                                                                                        |
| `IsWholeNumber`              | none                                                                                                    | The value has no fractional part.                                                                                                                               |
| `IsMultipleOf`               | `double divisor`                                                                                        | The value is an exact multiple of the divisor. A zero divisor is false.                                                                                         |
| `WithinPercentOf`            | `double target, double percent`                                                                         | The value is within the percentage of the target, in either direction.                                                                                          |
| `DeviatesFromByPercent`      | `double target, double percent`                                                                         | The value is further than the percentage from the target.                                                                                                       |
| `IncreasedByMoreThanPercent` | `double previous, double percent`                                                                       | The percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true.                                             |
| `DecreasedByMoreThanPercent` | `double previous, double percent`                                                                       | The percentage decrease from the previous value exceeds the percentage.                                                                                         |
| `ZScoreAbove`                | `double mean, double standardDeviation, double threshold`                                               | The z-score against the mean and standard deviation is above the threshold.                                                                                     |
| `ZScoreBelow`                | `double mean, double standardDeviation, double threshold`                                               | The z-score is below the threshold.                                                                                                                             |
| `ZScoreOutside`              | `double mean, double standardDeviation, double threshold`                                               | The absolute z-score is above the threshold, in either direction.                                                                                               |
| `RatioAbove`                 | `double denominator, double threshold`                                                                  | The value divided by the denominator is above the threshold. A zero denominator is false.                                                                       |
| `RatioBelow`                 | `double denominator, double threshold`                                                                  | The value divided by the denominator is below the threshold. A zero denominator is false.                                                                       |
| `IsJustBelowThreshold`       | `double threshold, double marginPercent`                                                                | Below the threshold but within the margin percentage of it (structuring).                                                                                       |
| `IsJustAboveThreshold`       | `double threshold, double marginPercent`                                                                | Above the threshold but within the margin percentage of it.                                                                                                     |
| `IsRoundAmount`              | `double nearest`                                                                                        | An exact multiple of the rounding unit, for example 1000.                                                                                                       |
| `WithinRadiusKm`             | `double longitude1, double latitude2, double longitude2, double radiusKm`                               | The value is latitude 1. Within the radius in kilometres of the second point.                                                                                   |
| `OutsideRadiusKm`            | `double longitude1, double latitude2, double longitude2, double radiusKm`                               | The value is latitude 1. Further than the radius in kilometres from the second point.                                                                           |
| `DistanceKmAbove`            | `double longitude1, double latitude2, double longitude2, double kilometers`                             | The value is latitude 1. The distance to the second point is above the kilometres.                                                                              |
| `DistanceKmBelow`            | `double longitude1, double latitude2, double longitude2, double kilometers`                             | The value is latitude 1. The distance to the second point is below the kilometres.                                                                              |
| `ImpliedSpeedAbove`          | `double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour` | The value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. |
| `ImpliedSpeedBelow`          | `double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour` | The value is latitude 1. The implied speed is below the limit.                                                                                                  |
| `HasValue`                   | none                                                                                                    | The value is a real number (not `NaN`). Use it after a calculation that can be undefined, such as `RatioOf`.                                                    |
| `HasNoValue`                 | none                                                                                                    | The value is `NaN`, for example an undefined calculation.                                                                                                       |
| `RatioInRange`               | `double denominator, double minimum, double maximum`                                                    | The value divided by the denominator is between the bounds, both included. A zero denominator is false.                                                         |
| `RatioOutsideRange`          | `double denominator, double minimum, double maximum`                                                    | The ratio is below the minimum or above the maximum. A zero denominator is false.                                                                               |
| `IsJustBelowAnyThreshold`    | `double marginPercent, double... thresholds`                                                            | Just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000).                                                          |
| `IsJustAboveAnyThreshold`    | `double marginPercent, double... thresholds`                                                            | Just above any of several limits, within the margin percentage.                                                                                                 |
| `WithinRadiusMiles`          | `double longitude1, double latitude2, double longitude2, double radiusMiles`                            | The value is latitude 1. Within the radius in miles of the second point.                                                                                        |
| `OutsideRadiusMiles`         | `double longitude1, double latitude2, double longitude2, double radiusMiles`                            | The value is latitude 1. Further than the radius in miles from the second point.                                                                                |
| `DistanceMilesAbove`         | `double longitude1, double latitude2, double longitude2, double miles`                                  | The value is latitude 1. The distance to the second point is above the miles.                                                                                   |
| `DistanceMilesBelow`         | `double longitude1, double latitude2, double longitude2, double miles`                                  | The value is latitude 1. The distance to the second point is below the miles.                                                                                   |
| `ImpliedSpeedMphAbove`       | `double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour`      | The value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed.                                                  |
| `ImpliedSpeedMphBelow`       | `double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour`      | The value is latitude 1. The implied speed in miles per hour is below the limit.                                                                                |

### Integer tests

Begin from `Payload.<Integer field>`, or from any token of that type (`TTLCounter`, `Abstraction`, `Dictionary` and so
on).

| Test             | Arguments                  | True when                                                  |
|------------------|----------------------------|------------------------------------------------------------|
| `Greater`        | `int other`                | Greater than the argument.                                 |
| `GreaterOrEqual` | `int other`                | Greater than or equal to the argument.                     |
| `Less`           | `int other`                | Less than the argument.                                    |
| `LessOrEqual`    | `int other`                | Less than or equal to the argument.                        |
| `Equal`          | `int other`                | Equals the argument.                                       |
| `NotEqual`       | `int other`                | Differs from the argument.                                 |
| `Between`        | `int minimum, int maximum` | Strictly between the bounds.                               |
| `InRange`        | `int minimum, int maximum` | Between the bounds, both included.                         |
| `OutsideRange`   | `int minimum, int maximum` | Below the minimum or above the maximum.                    |
| `In`             | `int... values`            | One of the listed integers.                                |
| `NotIn`          | `int... values`            | None of the listed integers.                               |
| `IsZero`         | none                       | Zero.                                                      |
| `IsPositive`     | none                       | Greater than zero.                                         |
| `IsNegative`     | none                       | Less than zero.                                            |
| `IsEven`         | none                       | An even number.                                            |
| `IsOdd`          | none                       | An odd number.                                             |
| `IsMultipleOf`   | `int divisor`              | An exact multiple of the divisor. A zero divisor is false. |
| `IsPrime`        | none                       | A prime number.                                            |

### String tests

Begin from `Payload.<String field>`, or from any token of that type (`TTLCounter`, `Abstraction`, `Dictionary` and so
on).

| Test                           | Arguments                                     | True when                                                                                                                                    |
|--------------------------------|-----------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| `IsNull`                       | none                                          | The value is null.                                                                                                                           |
| `IsEmpty`                      | none                                          | Present but zero length.                                                                                                                     |
| `IsNullOrEmpty`                | none                                          | Null or zero length.                                                                                                                         |
| `IsNullOrWhiteSpace`           | none                                          | Null, empty or only white space.                                                                                                             |
| `IsNotNullOrEmpty`             | none                                          | Present and not zero length.                                                                                                                 |
| `IsNotNullOrWhiteSpace`        | none                                          | Present with visible content.                                                                                                                |
| `Equal`                        | `string other`                                | Equals the argument, case sensitive. Null never equals.                                                                                      |
| `EqualIgnoreCase`              | `string other`                                | Equals the argument, ignoring case.                                                                                                          |
| `NotEqual`                     | `string other`                                | Both present and different.                                                                                                                  |
| `In`                           | `string... values`                            | One of the listed strings, case sensitive.                                                                                                   |
| `InIgnoreCase`                 | `string... values`                            | One of the listed strings, ignoring case.                                                                                                    |
| `NotIn`                        | `string... values`                            | Present and none of the listed strings.                                                                                                      |
| `InList`                       | `List list`                                   | A member of the List token, case sensitive.                                                                                                  |
| `InListIgnoreCase`             | `List list`                                   | A member of the List token, ignoring case.                                                                                                   |
| `NotInList`                    | `List list`                                   | Present and not a member of the List token.                                                                                                  |
| `Contains`                     | `string substring`                            | Contains the substring, case sensitive.                                                                                                      |
| `ContainsIgnoreCase`           | `string substring`                            | Contains the substring, ignoring case.                                                                                                       |
| `ContainsAny`                  | `string... values`                            | Contains at least one of the listed strings.                                                                                                 |
| `ContainsAnyIgnoreCase`        | `string... values`                            | Contains at least one of the listed strings, ignoring case.                                                                                  |
| `ContainsAll`                  | `string... values`                            | Contains every listed string.                                                                                                                |
| `ContainsNone`                 | `string... values`                            | Present and contains none of the listed strings.                                                                                             |
| `ContainsAnyInList`            | `List list`                                   | Contains at least one entry of the List token.                                                                                               |
| `ContainsAnyInListIgnoreCase`  | `List list`                                   | Contains at least one entry of the List token, ignoring case.                                                                                |
| `ContainsAllInList`            | `List list`                                   | Contains every entry of the List token.                                                                                                      |
| `ContainsNoneInList`           | `List list`                                   | Present and contains no entry of the List token.                                                                                             |
| `StartsWith`                   | `string prefix`                               | Starts with the prefix.                                                                                                                      |
| `EndsWith`                     | `string suffix`                               | Ends with the suffix.                                                                                                                        |
| `StartsWithAnyInList`          | `List list`                                   | Starts with any entry of the List token.                                                                                                     |
| `Matches`                      | `string pattern`                              | Matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception.                                               |
| `LengthEqual`                  | `int length`                                  | Has exactly the length.                                                                                                                      |
| `LengthGreater`                | `int length`                                  | Longer than the length.                                                                                                                      |
| `LengthLess`                   | `int length`                                  | Shorter than the length.                                                                                                                     |
| `LengthInRange`                | `int minimum, int maximum`                    | Length between the bounds, both included.                                                                                                    |
| `IsNumeric`                    | none                                          | Non-empty, digits only.                                                                                                                      |
| `IsAlpha`                      | none                                          | Non-empty, letters only.                                                                                                                     |
| `IsAlphaNumeric`               | none                                          | Non-empty, letters and digits only.                                                                                                          |
| `IsAllSameCharacter`           | none                                          | Non-empty and one repeated character.                                                                                                        |
| `HasSequentialDigits`          | `int minimumRunLength`                        | Has an ascending or descending run of digits at least the minimum length (synthetic identifiers).                                            |
| `ShannonEntropyAbove`          | `double threshold`                            | Non-empty and entropy above the threshold (generated looking values).                                                                        |
| `ShannonEntropyBelow`          | `double threshold`                            | Non-empty and entropy below the threshold.                                                                                                   |
| `IsValidEmail`                 | none                                          | A well formed email address.                                                                                                                 |
| `IsNotValidEmail`              | none                                          | Null, empty or not a well formed email address.                                                                                              |
| `IsValidIp`                    | none                                          | A valid IPv4 address.                                                                                                                        |
| `IsNotValidIp`                 | none                                          | Null, empty or not a valid IPv4 address.                                                                                                     |
| `IsValidIban`                  | none                                          | Passes the IBAN checksum.                                                                                                                    |
| `IsNotValidIban`               | none                                          | Null or fails the IBAN checksum.                                                                                                             |
| `IsValidBic`                   | none                                          | A valid BIC or SWIFT code.                                                                                                                   |
| `IsNotValidBic`                | none                                          | Null or not a valid BIC.                                                                                                                     |
| `IsValidLuhn`                  | none                                          | Passes the Luhn checksum.                                                                                                                    |
| `IsNotValidLuhn`               | none                                          | Null or fails the Luhn checksum.                                                                                                             |
| `IsValidCardExpiry`            | none                                          | A card expiry that has not elapsed.                                                                                                          |
| `IsNotValidCardExpiry`         | none                                          | Null, malformed or elapsed.                                                                                                                  |
| `EmailDomainEqual`             | `string domain`                               | The domain of the email equals the argument, ignoring case.                                                                                  |
| `EmailDomainIn`                | `string... domains`                           | The domain of the email is one of the listed domains.                                                                                        |
| `EmailDomainInList`            | `List list`                                   | The domain of the email is an entry of the List token.                                                                                       |
| `EmailAliasNormalisedEqual`    | `string other`                                | Equal to the argument after removing case and any plus tag (multi-accounting).                                                               |
| `SoundexEqual`                 | `string other`                                | Sounds the same as the argument.                                                                                                             |
| `JaroWinklerAtLeast`           | `string other, double threshold`              | Jaro-Winkler similarity to the argument is at least the threshold.                                                                           |
| `SimilarityAtLeast`            | `string other, double threshold`              | Levenshtein based similarity is at least the threshold, from 0 to 1.                                                                         |
| `LevenshteinAtMost`            | `string other, int distance`                  | Edit distance to the argument is at most the distance.                                                                                       |
| `EndsWithAnyInList`            | `List list`                                   | Ends with any entry of the List token.                                                                                                       |
| `ContainsAllIgnoreCase`        | `string... values`                            | Contains every listed string, ignoring case.                                                                                                 |
| `ContainsNoneIgnoreCase`       | `string... values`                            | Present and contains none of the listed strings, ignoring case.                                                                              |
| `ContainsAllInListIgnoreCase`  | `List list`                                   | Contains every entry of the List token, ignoring case.                                                                                       |
| `ContainsNoneInListIgnoreCase` | `List list`                                   | Present and contains no entry of the List token, ignoring case.                                                                              |
| `ContainsFuzzy`                | `string term, double threshold`               | A run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative).                  |
| `ContainsFuzzyAny`             | `double threshold, string... terms`           | As `ContainsFuzzy`, for any of the listed terms. The threshold comes first.                                                                  |
| `ContainsFuzzyAnyInList`       | `List list, double threshold`                 | As `ContainsFuzzy`, for any entry of the List token.                                                                                         |
| `LevenshteinAtMostAnyInList`   | `List list, int distance`                     | Within the edit distance of any entry of the List token, whole string against whole string.                                                  |
| `SimilarityAtLeastAnyInList`   | `List list, double threshold`                 | At least the similarity of any entry of the List token.                                                                                      |
| `JaroWinklerAtLeastAnyInList`  | `List list, double threshold`                 | At least the Jaro-Winkler similarity of any entry of the List token.                                                                         |
| `SomehowInList`                | `List list`                                   | Fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. |
| `SomehowInListWith`            | `List list, string options`                   | Fuzzily in the List token using an [options string](#fuzzy-list-matching-options).                                                           |
| `SomehowLike`                  | `string other`                                | Fuzzily like a single value, using the default profile.                                                                                      |
| `SomehowLikeWith`              | `string other, string options`                | Fuzzily like a single value, using an options string.                                                                                        |
| `ContainsSomehowInList`        | `List list`                                   | Some entry of the List token is found fuzzily inside the longer text, using the default profile.                                             |
| `ContainsSomehowInListWith`    | `List list, string options`                   | Some entry is found fuzzily inside the longer text, using an options string (scope is always `within`).                                      |
| `NormalisedInList`             | `List list`                                   | Equal to an entry after folding case, accents and punctuation.                                                                               |
| `LevenshteinInList`            | `List list, int maxDistance`                  | Within the edit distance of an entry (insertions, deletions, substitutions).                                                                 |
| `DamerauInList`                | `List list, int maxDistance`                  | Within the edit distance of an entry, counting a swap of adjacent letters as one edit.                                                       |
| `SimilarInList`                | `List list, double threshold`                 | The Levenshtein ratio to an entry is at least the threshold.                                                                                 |
| `JaroWinklerInList`            | `List list, double threshold`                 | The Jaro-Winkler similarity to an entry is at least the threshold.                                                                           |
| `DiceInList`                   | `List list, double threshold`                 | The bigram (Dice) overlap with an entry is at least the threshold.                                                                           |
| `SoundexInList`                | `List list`                                   | Every word sounds like the same word of an entry, in order.                                                                                  |
| `TokenSortInList`              | `List list, double threshold`                 | The same words as an entry in any order, allowing typos.                                                                                     |
| `TokenSetInList`               | `List list, double threshold`                 | The share of an entry's words that are found, allowing typos and missing words.                                                              |
| `InitialsInList`               | `List list`                                   | Matches an entry where a first name may be an initial (`J Smith` for `John Smith`).                                                          |
| `FuzzyScoreAtLeast`            | `List list, double threshold, string options` | The best score across the List token, from 0 to 1, is at least the threshold. Options string may be null.                                    |
| `FuzzyMatchCountAtLeast`       | `List list, int count, string options`        | At least the count of entries in the List token match. Options string may be null.                                                           |

### DateTime tests

Begin from `Payload.<Date field>`, or from any token of that type (`TTLCounter`, `Abstraction`, `Dictionary` and so on).

| Test                                | Arguments                                   | True when                                                                                                                |
|-------------------------------------|---------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| `IsPast`                            | none                                        | Earlier than now.                                                                                                        |
| `IsFuture`                          | none                                        | Later than now.                                                                                                          |
| `IsToday`                           | none                                        | On the current date.                                                                                                     |
| `IsWeekday`                         | none                                        | Monday to Friday.                                                                                                        |
| `IsWeekend`                         | none                                        | Saturday or Sunday.                                                                                                      |
| `IsMorning`                         | none                                        | Before 12:00.                                                                                                            |
| `IsAfternoon`                       | none                                        | From 12:00.                                                                                                              |
| `Before`                            | `DateTime other`                            | Earlier than the argument.                                                                                               |
| `After`                             | `DateTime other`                            | Later than the argument.                                                                                                 |
| `Between`                           | `DateTime rangeStart, DateTime rangeEnd`    | Strictly between the two dates.                                                                                          |
| `InRange`                           | `DateTime rangeStart, DateTime rangeEnd`    | Between the two dates, both included.                                                                                    |
| `SameDayAs`                         | `DateTime other`                            | On the same calendar date as the argument.                                                                               |
| `WithinBusinessHours`               | `int startHour, int endHour`                | The hour is from the start hour up to, but not including, the end hour.                                                  |
| `OutsideBusinessHours`              | `int startHour, int endHour`                | The hour is outside those hours.                                                                                         |
| `OlderThanDays`                     | `int days`                                  | The date is more than the number of days in the past.                                                                    |
| `YoungerThanDays`                   | `int days`                                  | The date is fewer than the number of days in the past.                                                                   |
| `WithinMinutesOf`                   | `DateTime other, double minutes`            | Within the minutes of the argument, in either direction.                                                                 |
| `WithinHoursOf`                     | `DateTime other, double hours`              | Within the hours of the argument, in either direction.                                                                   |
| `WithinDaysOf`                      | `DateTime other, double days`               | Within the days of the argument, in either direction.                                                                    |
| `MoreThanMinutesAfter`              | `DateTime other, double minutes`            | More than the minutes after the argument.                                                                                |
| `MoreThanDaysAfter`                 | `DateTime other, double days`               | More than the days after the argument.                                                                                   |
| `DayOfWeekIn`                       | `int... days`                               | Day of week is one of the numbers, where 0 is Sunday and 6 is Saturday.                                                  |
| `IsStartOfMonth`                    | none                                        | The first day of the month.                                                                                              |
| `IsEndOfMonth`                      | none                                        | The last day of the month.                                                                                               |
| `WithinBusinessHoursInZone`         | `string zoneId, int startHour, int endHour` | The value, read as UTC, falls in the hours in that time zone (for example `America/New_York`). An unknown zone is false. |
| `OutsideBusinessHoursInZone`        | `string zoneId, int startHour, int endHour` | The value, read as UTC, falls outside the hours in that time zone. An unknown zone is false.                             |
| `WithinWeekdayBusinessHours`        | `int startHour, int endHour`                | A weekday, within the hours, using the value's own clock.                                                                |
| `OutsideWeekdayBusinessHours`       | `int startHour, int endHour`                | A weekend, or outside the hours, using the value's own clock.                                                            |
| `WithinWeekdayBusinessHoursInZone`  | `string zoneId, int startHour, int endHour` | A weekday in that time zone and within the hours there. An unknown zone is false.                                        |
| `OutsideWeekdayBusinessHoursInZone` | `string zoneId, int startHour, int endHour` | A weekend in that time zone, or outside the hours there. An unknown zone is false.                                       |
| `IsWeekdayInZone`                   | `string zoneId`                             | A weekday in that time zone. An unknown zone is false.                                                                   |
| `IsWeekendInZone`                   | `string zoneId`                             | A weekend in that time zone. An unknown zone is false.                                                                   |

### Boolean tests

Begin from `Payload.<Boolean field>`, or from any token of that type (`TTLCounter`, `Abstraction`, `Dictionary` and so
on).

| Test      | Arguments | True when           |
|-----------|-----------|---------------------|
| `IsTrue`  | none      | The value is true.  |
| `IsFalse` | none      | The value is false. |

## Fuzzy list matching options

Any step ending in `With`, and the string extensions `IsSomehowInList`, `IsSomehowLike`, `ContainsSomehowInList`,
`FuzzyBestScore`, `FuzzyMatchCount` and `FuzzyBestMatch`, take an **options string**: a comma separated list of the
options below. It is plain text, because rules cannot name types. Options may be in any order, are case insensitive, and
an invalid string fails closed (nothing matches) rather than throwing. Use `IsValidFuzzyOptions()` and
`FuzzyOptionsError()`
on an options string to check it while authoring. A blank or null string means the default profile.

```vb
Matched = Payload.Name.IsSomehowInList(List.Whatever)
Matched = Payload.Name.IsSomehowInList(List.Whatever, "damerau=1,soundex,notitles,minlength=4")
Matched = Payload.Narrative.ContainsSomehowInList(List.Whatever, "lev=2,nosuffixes")
Matched = Payload.Name.FuzzyBestScore(List.Whatever, "tokensort,initials") > 0.9
```

### Algorithms

Name one or more. With no algorithm, the default profile applies (Jaro-Winkler or token sort, both at 0.92). By default
any algorithm passing is a match; add `all` to require every one.

| Option                   | Threshold                                 | Matches when                                                                                 |
|--------------------------|-------------------------------------------|----------------------------------------------------------------------------------------------|
| `exact`                  | none                                      | Identical after normalisation                                                                |
| `levenshtein=N`, `lev=N` | Whole number of edits, 0 to 10, default 1 | At most N insertions, deletions or substitutions                                             |
| `damerau=N`, `osa=N`     | As above                                  | As above, and swapping two adjacent letters is one edit (`Jhon` for `John`)                  |
| `similarity=T`, `sim=T`  | 0 to 1, default 0.85                      | The Levenshtein ratio is at least T                                                          |
| `jarowinkler=T`, `jw=T`  | 0 to 1, default 0.92                      | Jaro-Winkler similarity is at least T, favouring a shared start                              |
| `dice=T`, `bigram=T`     | 0 to 1, default 0.85                      | Overlap of two letter pairs is at least T                                                    |
| `soundex`                | none                                      | Every word sounds the same as the entry's word in that position                              |
| `tokensort=T`, `sort=T`  | 0 to 1, default 0.92                      | The same words in any order (`Smith John`), allowing typos                                   |
| `tokenset=T`, `set=T`    | 0 to 1, default 0.85                      | The share of the entry's words found, allowing typos and missing words                       |
| `initials`               | none                                      | Same words, where a word may be an initial, with at least one full word matching (`J Smith`) |

### Normalisation

Applied to both the value and each entry before comparing.

| Option                        | Effect                                                                | Default |
|-------------------------------|-----------------------------------------------------------------------|---------|
| `nocase` / `case`             | Ignore case, or make it significant                                   | Ignore  |
| `nodiacritics` / `diacritics` | Fold accents and letters such as `ß`, `ø`, `ł`, or keep them          | Fold    |
| `nopunct` / `punct`           | Delete apostrophes and turn other punctuation into spaces, or keep it | Strip   |
| `nospaces` / `keepspaces`     | Remove all spaces (`JohnSmith`), or keep them                         | Keep    |
| `notitles`                    | Drop leading titles such as Mr, Dr, Prof, Sir, Herr                   | Off     |
| `nosuffixes`                  | Drop trailing company forms such as Ltd, LLC, Inc, GmbH, PLC          | Off     |

Word based algorithms (`soundex`, `tokensort`, `tokenset`, `initials`) need spaces, so do not combine them with
`nospaces`.

### Scope, blocking and combination

| Option                  | Effect                                                                                                                                    |
|-------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| `whole` (default)       | The whole value is compared with each entry                                                                                               |
| `within`                | Each entry is searched for inside the value, a run of words at a time, so a name inside a narrative is found. Never matches inside a word |
| `all` / `any` (default) | Require every algorithm to pass, or any one                                                                                               |
| `minlength=N`           | Skip values shorter than N characters after normalisation. Recommended for short names, where similarity is noisy                         |
| `firstletter`           | Only compare with entries that start with the same letter. Faster, and stricter                                                           |

### Results other than yes or no

| Extension                        | Returns                                                             |
|----------------------------------|---------------------------------------------------------------------|
| `FuzzyBestScore(list, options)`  | The best score across the list, from 0 to 1, even if nothing passed |
| `FuzzyMatchCount(list, options)` | How many entries pass                                               |
| `FuzzyBestMatch(list, options)`  | The original text of the best entry, or null                        |

The same three exist as pipeline steps that change the value being tested (a number, a count, and a string), so the
result feeds the ordinary tests: `Payload.Name.FuzzyBestScore(List.Whatever, "jw").MatchGreaterOrEqual(0.9)`.

### Performance

The list is normalised once per normalisation setting and cached against the list object, and is rebuilt when the list
changes (its size, or its first, middle or last entry). Every event still compares against every entry, so for very
large lists prefer `firstletter`, `minlength`, or a cheaper algorithm first in a pipeline, and put `exact` checks before
fuzzy ones.

### Reading an HTTP Adaptation without an exception

`HttpAdaptation.Name.Value.OrNaN()` reads the adaptation's value as a number and is `NaN` when it was suppressed, so it
fails every comparison. `OrZero()` reads a suppressed value as zero instead. `GetValueOrDefault` is not a permitted
token.

## Calculations on continuous values

These calculations, and the one step derive-and-compare tests that use them, are first class in a rule. As a standard,
though, endeavour to write the calculation in an Abstraction Calculation and let the Activation Rule compare the stored
result, because it can be reused and organises concerns (see
[Derived Values: Use an Abstraction Calculation](RuleAuthoringMethodology.html#derived-values-use-an-abstraction-calculation)).
Work it out in the rule where a calculation is not available, in a Gateway Rule or where a model score is involved. The
examples below show the calculation in a rule so that its result is visible.

A calculation is called on a number, takes the other numbers it needs as arguments, and returns a number. That is the
reading order of a rule: `Payload.Amount.RatioOf(Payload.AnotherAmount)` is the amount as a ratio of the other amount.
Any number token can be the receiver or an argument, so counters, abstractions, dictionary values and payload fields mix
freely. The result is an ordinary number, so the next step is a comparison, or another calculation.

```vb
Matched = TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).MatchGreater(0.3)
Matched = TTLCounter.DeclinedSpend.ShareOfSumWith(TTLCounter.ApprovedSpend).MatchInRange(0.25, 0.5)
Matched = TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).Complement().MatchLess(0.7)
```

Every calculation is also available as four comparisons in all five prefixes, with the extra values first and the bounds
last (the bounds come first when the calculation takes a list of values, because a list must be last):

| Comparison                                               | True when the calculation is             |
|----------------------------------------------------------|------------------------------------------|
| `<Calculation>Above(..., bound)`                         | Above the bound                          |
| `<Calculation>Below(..., bound)`                         | Below the bound                          |
| `<Calculation>InRange(..., lowerBound, upperBound)`      | Between the bounds, both included        |
| `<Calculation>OutsideRange(..., lowerBound, upperBound)` | Below the lower or above the upper bound |

So `TTLCounter.DeclinedSpend.MatchRatioOfAbove(TTLCounter.TotalSpend, 0.3)` is the same test as the first example, and
`TTLCounter.DeclinedSpend.MatchSumWithAbove(900, TTLCounter.ApprovedSpend)` puts the bound first because `SumWith` takes
a list.

**A calculation that is undefined returns `NaN`, never zero and never infinity.** That is a division by zero, a log of a
number that is not positive, a range whose ends are equal, an empty total, and any `NaN` or infinite input. `NaN` fails
every comparison, so a rule fails closed on an empty counter instead of matching or faulting. To test for it use
`HasNoValue()` or `HasValue()`. To turn an undefined result into zero, for example so that an Abstraction Calculation
stores a defined number, end the calculation with `ZeroIfUndefined()`.

| Calculation                | Arguments                                                            | Returns                                                                                                  |
|----------------------------|----------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------|
| `RatioOf`                  | `number denominator`                                                 | The value divided by the denominator.                                                                    |
| `PercentOf`                | `number total`                                                       | The value as a percentage of the total.                                                                  |
| `ShareOfSumWith`           | `number other`                                                       | The value's share of the value plus the other (a decline ratio is declined over declined plus approved). |
| `Complement`               | none                                                                 | One minus the value (an approval ratio from a decline ratio).                                            |
| `LogRatioOf`               | `number denominator`                                                 | The natural log of the value over the denominator, symmetric for increases and decreases.                |
| `RatioOfRatios`            | `number denominator, number otherNumerator, number otherDenominator` | The value over its denominator, divided by another numerator over its own denominator.                   |
| `InverseRatioOf`           | `number other`                                                       | The other value over this value.                                                                         |
| `DifferenceFrom`           | `number other`                                                       | The value minus the other.                                                                               |
| `AbsoluteDifferenceFrom`   | `number other`                                                       | The size of the gap between the value and the other.                                                     |
| `PercentDifferenceFrom`    | `number other`                                                       | The gap between the two as a percentage of their average size, the same either way round.                |
| `RelativeChangeFrom`       | `number previous`                                                    | The change from the previous value as a fraction of it.                                                  |
| `GrowthFactorFrom`         | `number previous`                                                    | The value as a multiple of the previous value.                                                           |
| `SlopeFrom`                | `number previous, number elapsed`                                    | The change from the previous value per unit of time.                                                     |
| `CompoundGrowthRate`       | `number previous, number periods`                                    | The steady growth rate per period that takes the previous value to this one.                             |
| `PercentDeviationFromMean` | `number mean`                                                        | The percentage the value sits above or below the mean.                                                   |
| `ModifiedZScore`           | `number median, number medianAbsoluteDeviation`                      | A robust z-score from the median and the median absolute deviation, unmoved by outliers.                 |
| `CoefficientOfVariation`   | `number mean`                                                        | The value, as a standard deviation, over the size of the mean.                                           |
| `MinMaxNormalise`          | `number minimum, number maximum`                                     | Where the value sits between the minimum (0) and maximum (1).                                            |
| `PercentOfRange`           | `number minimum, number maximum`                                     | Where the value sits between the minimum (0) and maximum (100).                                          |
| `SumWith`                  | `number... others`                                                   | The value plus the others.                                                                               |
| `MeanWith`                 | `number... others`                                                   | The average of the value and the others.                                                                 |
| `MaxWith`                  | `number... others`                                                   | The largest of the value and the others.                                                                 |
| `MinWith`                  | `number... others`                                                   | The smallest of the value and the others.                                                                |
| `SpreadWith`               | `number... others`                                                   | The gap between the largest and smallest of the value and the others.                                    |
| `WeightedMeanWith`         | `number weight, number other, number otherWeight`                    | The weighted average of the value and another value.                                                     |
| `GeometricMeanWith`        | `number other`                                                       | The geometric mean of the value and another, for growth factors and multiples.                           |
| `HarmonicMeanWith`         | `number other`                                                       | The harmonic mean of the value and another, for averaging rates.                                         |
| `ShareOfMax`               | `number... others`                                                   | The value over the largest of it and the others.                                                         |
| `HerfindahlWith`           | `number... others`                                                   | How concentrated the total is in a few of the value and the others, from 1 over the count to 1.          |
| `EntropyOfSharesWith`      | `number... others`                                                   | The Shannon entropy in bits of how the total is shared across the value and the others.                  |
| `HeadroomTo`               | `number limit`                                                       | The limit minus the value, the room left.                                                                |
| `ExcessOver`               | `number limit`                                                       | How far the value is over the limit, zero when within it.                                                |
| `ShortfallBelow`           | `number minimum`                                                     | How far the value is under the minimum, zero when at or above it.                                        |
| `DistanceToThreshold`      | `number threshold`                                                   | The gap between the value and the threshold in either direction.                                         |
| `PercentBelowThreshold`    | `number threshold`                                                   | How far under the threshold the value is as a percentage of it, negative when over.                      |
| `ImbalanceWith`            | `number other`                                                       | How one sided the value and the other are, from -1 (all the other) through 0 to 1 (all this value).      |
| `RetentionAfter`           | `number outflow`                                                     | The share of the value kept after the outflow, negative when more went out than came in.                 |

### Recipes

| Business question                                    | Rule                                                                                                                             |
|------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------|
| Decline ratio by value                               | `TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).MatchGreater(0.3)`                                                      |
| Decline ratio against approved plus declined         | `TTLCounter.DeclinedSpend.ShareOfSumWith(TTLCounter.ApprovedSpend).MatchGreater(0.3)`                                            |
| Approval ratio                                       | `TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).Complement().MatchLess(0.7)`                                            |
| Decline ratio by count against by value              | `TTLCounter.DeclinedCount.RatioOfRatios(TTLCounter.TotalCount, TTLCounter.DeclinedSpend, TTLCounter.TotalSpend).MatchGreater(2)` |
| Chargeback or refund ratio                           | `TTLCounter.Refunds.RatioOf(TTLCounter.Sales).MatchGreater(0.05)`                                                                |
| Average ticket size                                  | `TTLCounter.Spend.RatioOf(TTLCounter.Transactions).MatchGreater(500)`                                                            |
| Spend per day                                        | `TTLCounter.Spend7Days.RatioOf(7).MatchGreater(1000)`                                                                            |
| Spend acceleration, 1 hour against the daily average | `TTLCounter.Spend1Hour.RatioOf(TTLCounter.Spend24Hours.RatioOf(24)).MatchGreater(4)`                                             |
| Percentage of a card limit used                      | `Payload.Balance.PercentOf(Payload.CreditLimit).MatchGreater(90)`                                                                |
| Room left under a limit                              | `Payload.Balance.HeadroomTo(Payload.CreditLimit).MatchLess(100)`                                                                 |
| Amount over a limit                                  | `Payload.Amount.ExcessOver(Payload.DailyLimit).MatchGreater(0)`                                                                  |
| Amount short of a minimum                            | `Payload.Amount.ShortfallBelow(Payload.MinimumSpend).MatchGreater(0)`                                                            |
| Money passing straight through (mule)                | `TTLCounter.Outbound.RatioOf(TTLCounter.Inbound).MatchInRange(0.9, 1.05)`                                                        |
| Share of inflow kept                                 | `TTLCounter.Inbound.RetentionAfter(TTLCounter.Outbound).MatchLess(0.1)`                                                          |
| One sided flow, in against out                       | `TTLCounter.Inbound.ImbalanceWith(TTLCounter.Outbound).MatchGreater(0.8)`                                                        |
| Cash out against cash in                             | `TTLCounter.Withdrawals.RatioOf(TTLCounter.Deposits).MatchGreater(0.95)`                                                         |
| Spike against the customer's own mean                | `Payload.Amount.PercentDeviationFromMean(Abstraction.MeanAmount).MatchGreater(300)`                                              |
| Outlier that ignores outliers in the baseline        | `Payload.Amount.ModifiedZScore(Abstraction.MedianAmount, Abstraction.MadAmount).MatchGreater(3.5)`                               |
| Noisy customer, spread against mean                  | `Abstraction.StdDevAmount.CoefficientOfVariation(Abstraction.MeanAmount).MatchGreater(1.5)`                                      |
| Growth on last period                                | `TTLCounter.SpendThisWeek.RelativeChangeFrom(TTLCounter.SpendLastWeek).MatchGreater(2)`                                          |
| Multiple of last period                              | `TTLCounter.SpendThisWeek.GrowthFactorFrom(TTLCounter.SpendLastWeek).MatchGreater(3)`                                            |
| Steady growth per period                             | `Payload.Balance.CompoundGrowthRate(Payload.OpeningBalance, 6).MatchGreater(0.5)`                                                |
| Rate of change per hour                              | `Payload.Balance.SlopeFrom(Payload.PreviousBalance, Payload.HoursElapsed).MatchGreater(1000)`                                    |
| Symmetric growth or shrinkage                        | `TTLCounter.SpendThisWeek.LogRatioOf(TTLCounter.SpendLastWeek).MatchOutsideRange(-1, 1)`                                         |
| Two systems disagree                                 | `Payload.LedgerBalance.PercentDifferenceFrom(Payload.CoreBalance).MatchGreater(2)`                                               |
| Invoice against purchase order                       | `Payload.InvoiceAmount.AbsoluteDifferenceFrom(Payload.PurchaseOrderAmount).MatchGreater(50)`                                     |
| Just under a reporting threshold                     | `Payload.Amount.PercentBelowThreshold(10000).MatchInRange(0, 10)`                                                                |
| Distance to a threshold either side                  | `Payload.Amount.DistanceToThreshold(10000).MatchLess(250)`                                                                       |
| One merchant dominates spend                         | `TTLCounter.TopMerchantSpend.ShareOfMax(TTLCounter.SecondMerchantSpend, TTLCounter.ThirdMerchantSpend).MatchGreater(0.9)`        |
| Spend concentrated across merchants                  | `TTLCounter.MerchantA.HerfindahlWith(TTLCounter.MerchantB, TTLCounter.MerchantC).MatchGreater(0.6)`                              |
| Spend spread evenly (structuring)                    | `TTLCounter.MerchantA.EntropyOfSharesWith(TTLCounter.MerchantB, TTLCounter.MerchantC).MatchGreater(1.5)`                         |
| Largest of several amounts                           | `Payload.Amount.MaxWith(Payload.PreviousAmount, Payload.Amount2).MatchGreater(9000)`                                             |
| Combined total across products                       | `TTLCounter.CardSpend.SumWith(TTLCounter.AccountSpend, TTLCounter.LoanSpend).MatchGreater(50000)`                                |
| Combined average                                     | `TTLCounter.CardSpend.MeanWith(TTLCounter.AccountSpend).MatchGreater(5000)`                                                      |
| Gap between highest and lowest of several            | `Payload.Amount.SpreadWith(Payload.Amount2, Payload.Amount3).MatchGreater(10000)`                                                |
| Blended risk score                                   | `Payload.ModelScore.WeightedMeanWith(0.7, Payload.RulesScore, 0.3).MatchGreater(0.8)`                                            |
| Average of rates                                     | `Payload.RateA.HarmonicMeanWith(Payload.RateB).MatchGreater(0.4)`                                                                |
| Average of growth multiples                          | `Payload.FactorA.GeometricMeanWith(Payload.FactorB).MatchGreater(1.5)`                                                           |
| Position within a range                              | `Payload.Amount.MinMaxNormalise(Abstraction.MinAmount, Abstraction.MaxAmount).MatchGreater(1)`                                   |
| Percent of a range                                   | `Payload.Amount.PercentOfRange(Abstraction.MinAmount, Abstraction.MaxAmount).MatchGreater(100)`                                  |
| How many times a limit fits                          | `Payload.Amount.InverseRatioOf(Payload.DailyLimit).MatchLess(2)`                                                                 |

## Using existing extension methods

Every existing extension method that returns a Boolean can be used in a pipeline through `MatchWhen`, `RejectWhen`,
`BreakWhen`, `RequireThat` and `EnsureThat`, so nothing in the
[extension catalogue](../Configuration/Models/RuleCompilationAlgorithm/index.html) is out of reach:

```vb
Matched = Payload.Amount.RequireGreater(0).RequireThat(Payload.Amount.IsRoundAmount(1000)).MatchGreater(9000)
```
