---
layout: default
title: Abstraction Rules
nav_order: 10
parent: Models
grand_parent: Configuration
---

# Abstraction Rules

<!-- TODO(docs): this whole page pre-dates the Vue UI; owner to read end to end and re-check every Razor-era sentence (menu paths, tab names, button labels) against the Models story page -->

The Abstraction Rules page is where rules are created for the aggregation of data available in the cache database. The
Abstraction Rule functionality is one of the most intrinsic processes in the model invocation, being responsible for the
creation of aggregate statistics based on the contents of the cache of transaction or event data retained.

The abstraction rules are grouped together by their search keys, which has the effect of only a single key value pair
type query being looked up against the cache, with compiled assemblies being executed for match against the in memory
dataset.

A search key will be used as a predicate in retrieval of data for the subsequent execution of Abstraction rules that use
roll up to that search key. For example, if a transaction is being processed through a model, if an account identifier
is configured to be a search key, a selection of all records in the cache will be returned where that account identifier
matches (put simply it will return all transactions for that customer account). Search Keys are defined in the Request
XPath definition and there can be many search keys - thus cache database queries - per transaction being executed
through the model. The process is different when a Search Key is defined to be retrieved from a cache, as these values
are pre-calculated by the Search Key Cache thread.

In the case of PostgresSQL being used as the cache for Abstraction Rule processing, only the columns that have been
identified as being required in the Abstraction Rules definition (in both rule text and its aggregation key) will be
selected from the database, thus reducing casting and network IO. The columns required and the SQL to be pushed down to
the cache is compiled during model synchronisation for used in real-time processing.

Firstly, Search Keys need to be set up - in this example IP - by revisiting the Request XPath page via Models >>
References >> Request XPath:

<!-- TODO(screenshot): file=model-abstraction-top-of-tree-for-request-xpath.png route=/models state="Request XPath card with the model tree" -->
![Image](model-abstraction-top-of-tree-for-request-xpath.png)

Expand on the model tree on the left hand side to expose all Request XPath for this model, then click on IP:

<!-- TODO(screenshot): file=model-abstraction-selected-ipin-request-xpath.png route=/models state="Request XPath entry for IP selected" -->
![Image](model-abstraction-selected-ipin-request-xpath.png)

For the Request XPath, toggle the switch titled Search Key:

<!-- TODO(screenshot): file=model-abstraction-switched-search-key-on.png route=/models state="Request XPath entry for IP with Search Key switched on" -->
![Image](model-abstraction-switched-search-key-on.png)

Click the Update button towards the base of the page to update the version of the Request XPath entry:

<!-- TODO(screenshot): file=model-abstraction-updated-request-xpath-for-ip.png route=/models state="Request XPath list after saving IP as a Search Key" -->
![Image](model-abstraction-updated-request-xpath-for-ip.png)

With a Search Key now being available for IP, navigate through the menu as Models >> Abstraction >> Abstraction Rules:

<!-- TODO(screenshot): file=model-abstraction-top-of-tree-for-abstraction-rule.png route=/models state="Abstraction Rules card with the model tree" -->
![Image](model-abstraction-top-of-tree-for-abstraction-rule.png)

Click on the model towards the left hand side to add an Abstraction Rule:

<!-- TODO(screenshot): file=model-abstraction-empty-abstraction-rule.png route=/models state="New Abstraction Rule form, Builder tab, all groups visible" -->
![Image](model-abstraction-empty-abstraction-rule.png)

The Page exists to create Abstraction Rules which are VB.net code fragments that will be compiled by the engine.
Abstraction Rules accept data and TTL Counter objects and test the code fragment for equality, returning a boolean flag
to that effect. The name is required and limited to 256 characters, and must be unique within the Model. The Builder or
Coder rule script (whichever surface is selected) is required and limited to 65,536 characters.

In the Builder, each condition is a field, an operator and its values. The operator drop-down lists every operator that
suits the field's type, under headings: Basic (the builder's usual comparisons) first, then groups such as Text:
validation, Text: model lists, Number: thresholds and limits, Date: calendar and Compare to another field, each in
alphabetical order. Every test in the [Rule Pipeline Reference](../../../RuleTaxonomy/RulePipelineReference.html) is
there, for example Is valid IBAN, In list, Starts with any in list, Is just below any threshold, Ratio of: above, Within
hours of and Is weekend. Once an operator is chosen, the builder shows one box per value it takes, named after the
value; a number or date value can also name a field, for example `TTLCounter.PerAccount`. The full list of operators and
the rule text each produces is
in [Rule and Code Builder](../../../Navigation/RuleBuilderAndCoder/index.html#builder-operators). The same operators are
offered in every rule builder: Gateway, Abstraction, Activation and Reprocessing rules, and the backtest filters.

The Builder and Coder exposes the following collections:

| Value        | Description                                                                                      |
|--------------|--------------------------------------------------------------------------------------------------|
| Payload      | The fields created in the Request XPath page, Inline Function and Inline Script (if registered). |
| TTL Counters | The TLL Counter and their current incremented values.                                            |

The available parameters are as follows:

| Value           | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Example    |
|-----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------|
| Search          | The Search directs whether the rule is to be executed on a list of records having been returned from the Cache or only upon the transaction currently being processed through the model.  False would imply that the scope of the Abstraction Rule is only the current transaction being processed and if it matches,  then a 1 will be returned, otherwise 0 (keeping in mind that Exhaustive or Machine Learning will seek to weight it,  hence the numeric values). If Search is selected, it would imply that a retrieval from the Cache Database need take place and the Abstraction Rule is going to be tested against each transaction returned.                                                                             | True       |
| Search Key      | The Search Key is the basis for several distinct data retrievals to be made of the Cache Database. For example,  if a Search Key is specified to be an IP,  then it would imply that a fetch is going to be made of the Cache Database for all transaction matching on the IP of the current transaction in process, thereafter applying the Abstraction Rule on each of the records to test for a match.                                                                                                                                                                                                                                                                                                                           | IP         |
| Search Interval | The Search Interval is the time threshold reaching back from the Reference Date for the transaction being processed, whereby only transactions exceeding this threshold are eligible for matching. For example,  if a Search Interval is configured for 1 Day (taking the Search Interval and Search Value together) then this would imply that only records greater than the Reference Date minus One Day are eligible for matching. It is worthy of note that the Search Interval and Search Value are not used in the recall from the Cache Database,  and everything matching on the key up to the fetch limit specified in the model definition will be returned. The Search Interval is taken together with the Search Value. | Day        |
| Offset Type     | All data matching on a Search Key is retrieved from the cache. The Offset type helps to select data from a subset of these records. Offsets can be helpful if analysis need be done only on the last x records, for example. In most cases it will be set to none.                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | None       |
| Offset Interval | The number of intervals to be offset by, for example the last x number of records returned from the cache.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Skip First |
| Search Value    | As Search Interval,  with the Search Value being comprising the length of time to reach back from the Reference Date.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | 1          |
| Function Type   | The process of aggregating the matches is different from the process of testing and matching the Abstraction Rules itself.  The process of aggregation takes place after all matching has take place and the transaction matches for the rule have been set aside.  The Function Type is the aggregation to perform and is extensively documented in the Abstraction Function Definitions of this document. For example,  if a Sum function is specified,  then the function will look to count up records using the Function Key field (e.g. Transaction Amount).                                                                                                                                                                  | Sum        |
| Function Key    | The Function Key is available only on certain Function Types and represents the fields inside the transaction that are to be used for aggregation.  For example,  if the Sum Function Type is specified and the Function Key is specified as the Transaction Amount,  the aggregation will seek to create a Sum for all of the Transaction Amounts having matched.                                                                                                                                                                                                                                                                                                                                                                  | AmountUSD  |

When Search is enabled, a Search Key, Search Interval (Seconds/Minutes/Hours/Days) and a recognised Function Type are
required; the Function Key is additionally required unless the Function Type is Count. When Offset is enabled, the
Offset Type must be one of First, Last, Skip First or Take Last, and the Offset Value cannot be negative.

The first step of abstraction aggregation is to further filter the matches by the position (keeping in mind that the
processing order will be ascending or earliest first). The Skip and Fetch feature, which is configured in the
Abstraction Rules definition, allows only a subsection of the matched records to be returned (e.g. only the last record
or only the first record) to the array for which a function will be targeted. The offsets types are as follows:

| Offset Type | Description                                                                                                                                                            | Skip                                                         | Fetch                                               |
|-------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------|-----------------------------------------------------|
| None        | Performs no reduction of the list of matches and aggregation will be performed across all events that have matched the rule.                                           | Nothing                                                      | Fetch All Matches                                   |
| First       | Only the first (earliest) matches,  where the number of matches is the offset value,  will be considered when performing the abstraction aggregation function.         | MatchesSpecified in the Offset Value.                        | One Match                                           |
| Last        | Only the first (most recent) matches,  where the number of matches is the offset value,  will be considered when performing the abstraction aggregation function.      | All Matches with the exception of one plus the Offset Value. | One Match                                           |
| Skip First  | All matches will be considered with the exception of the first (earliest) matches,  where the number of the oldest matches to exclude is the offset value.             | Matches Specified in the Offset Value.                       | All Matches with the exception of the Offset Value. |
| Take Last   | All matches will be considered with the exception of the most recent (last or latest) matches,  where the number of the newest matches to exclude is the offset value. | All Matches with the exception of the Offset Value.          | Matches Specified in the Offset Value.              |

With a final list of matches having been achieved, potentially having been reduced by offsetting, the Abstraction
Functions will be performed, returning a single value from a variety of functions available. The model abstraction has
many different types of calculation, that can be performed against abstraction rule matches. The following table details
the aggregations available and describes its processing logic:

| Value              | Description                                                                                                                                                                                                                                                                                                                                                                                                            |
|--------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Count              | The number of matches in the Abstraction Rule matches collection.  An array for the purposes of aggregation of a specific double value is not required.                                                                                                                                                                                                                                                                |
| Distinct Count     | The distinct count iterates through each transaction having matched and adds the value specified to be distinct to a collection, however,  before adding it to the collection the collection is checked to ensure that the value does not already exist.  After each matching transaction iterated through,  the count of the newly created collection is returned.                                                    |
| Sum                | Using an array of float values which has been created based upon a prescribed field available to the matches,  sum total of the array of float values.                                                                                                                                                                                                                                                                 |
| Average            | Using an array of float values which has been created based upon a prescribed field available to the matches,  Mean of the array of float values.                                                                                                                                                                                                                                                                      |
| Median             | Using an array of float values which has been created based upon a prescribed field available to the matches,  Median or statistical Midpoint in the array of float values.                                                                                                                                                                                                                                            |
| Kurtosis           | Using an array of float values which has been created based upon a prescribed field available to the matches,  Kurtosis or the sharpness of the peak of distribution of the array of float values.                                                                                                                                                                                                                     |
| Skew               | Using an array of float values which has been created based upon a prescribed field available to the matches,  Skewness which is the extent to which there is lean \ tendency towards smaller or larger values in the array of float values.                                                                                                                                                                           |
| Standard Deviation | Using an array of float values which has been created based upon a prescribed field available to the matches,  Standard Deviation or measure of spread of the array of float values.                                                                                                                                                                                                                                   | 
| Mode               | Using an array of float values which has been created based upon a prescribed field available to the matches,  the most commonly observed value in the array of float values.                                                                                                                                                                                                                                          |
| Same Count         | The Same Count is intended to interrogate the matches to identify the number of transactions that have the same field value as that currently been processed.  Each match is iterated through and the value of the prescribed field is tested against that in the history,  in the event it is the same,  a counter will be incremented.  Upon the conclusion of the iteration,  the counter is returned as the value. |
| Actual Value       | Keeping in mind that the matches may have been reduced by skip,  fetch and offset values,  this function will return the actual value of a field \ data at a specific position.  For example,  this function would allow the identification of the value of the 3rd transaction in a customer history.                                                                                                                 |
| Max                | The largest value observed in the offset matches taking a field value name for comparison.                                                                                                                                                                                                                                                                                                                             |
| Min                | The smallest value observed in the offset matches taking a field value name for comparison.                                                                                                                                                                                                                                                                                                                            |
| Since              | Taking the Interval Type,  the number of that interval between the most recent event and the first event observed in the matches.  The current date for reference is taken to be the most recent value observed in the matches.  The comparison is made against the earliest date observed. The value return depends upon the Interval Type specified in the function Second, Hours, Minutes and Days.                 |

Complete the page with the parameters as prescribed above, setting a rule to match on everything as AmountUSD is greater
than 0:

<!-- TODO(screenshot): file=model-abstraction-example-values-for-volume-rule-on-amount-usd.png route=/models state="Abstraction Rule form completed for a volume rule on AmountUSD" -->
![Image](model-abstraction-example-values-for-volume-rule-on-amount-usd.png)

Click the Add button to create a version of this Abstraction Rule:

<!-- TODO(screenshot): file=model-abstraction-added-abstraction-rule-for-volume.png route=/models state="Abstraction Rule list after Save" -->
![Image](model-abstraction-added-abstraction-rule-for-volume.png)

Synchronise the model via Entity >> Synchronisation and repeat the HTTP POST to
<!-- TODO(docs): the menu path or synchronisation step above is Razor-era wording; confirm against the Vue Models story page, and whether synchronisation is still a manual step -->
endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc)
for response as follows:

<!-- TODO(screenshot): file=model-abstraction-response-payload-with-volume.png route=/models state="Invocation response payload with the Volume abstraction value" -->
![Image](model-abstraction-response-payload-with-volume.png)

It can be seen that the sum of all events in the last day has been aggregated, and this will increase on each event, in
the following example:

<!-- TODO(screenshot): file=model-abstraction-further-increment.png route=/models state="Second invocation response showing the abstraction value advanced" -->
![Image](model-abstraction-further-increment.png)

This value is available for subsequent Activation Rules and crucially, as a variable for machine learning.

## Iteration order

The history for a Search Key is always fetched as the most recent transactions up to the fetch limit. Iteration Order
decides the order in which that history, with the transaction being processed added to it, is then presented:

| Value      | Description                                                                                                                             |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| Ascending  | Oldest first, the default and the behaviour of every rule created before this setting existed. The transaction being processed is last. |
| Descending | Most recent first. The transaction being processed is first.                                                                            |

The order is one sequence from start to finish. It is the order the rule is tested in, the order the Offset reduction
works on and the order the function receives its values in. Under Descending, Offset First keeps the most recent
match, Offset Last keeps the oldest, and Actual Value returns the oldest match kept. A rule that does not use the
cursor matches exactly the same transactions in either order; only their sequence changes.

## The rule cursor

An Abstraction Rule with Search enabled is tested once for each transaction in the history, in Iteration Order. In the
Coder, the rule can see where it is in that walk through `Cursor`, and can steer the walk with the Visual Basic loop
statements `Continue For` and `Exit For`. The rule still answers for each transaction by setting `Matched` or by
`Return True` and `Return False`, exactly as before.

| Word               | Description                                                                                                                                             |
|--------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Cursor.Position`  | One-based position of the transaction being tested. 1 is the first transaction in Iteration Order.                                                      |
| `Cursor.Index`     | Zero-based position of the transaction being tested; always one less than `Cursor.Position`.                                                            |
| `Cursor.Size`      | How many transactions the cursor will walk: the history inside the rule's search window, including the transaction being processed.                     |
| `Cursor.Matches`   | How many transactions this rule has accepted so far in the walk.                                                                                        |
| `Cursor.IsFirst`   | True on the first transaction of the walk.                                                                                                              |
| `Cursor.IsLast`    | True on the last transaction of the walk.                                                                                                               |
| `Cursor.IsCurrent` | True when the transaction being tested is the one being processed now, rather than one from the history.                                                |
| `Cursor.Result`    | A number the rule can set and read as the walk proceeds. The Cursor Result function returns its final value as the abstraction value.                   |
| `Continue For`     | Finishes with this transaction and moves to the next. The transaction is accepted if `Matched` was already set to True.                                 |
| `Exit For`         | Finishes with this transaction and ends the walk; no further transactions are tested. The transaction is accepted if `Matched` was already set to True. |

A rule that uses the cursor is tested only against the transactions inside its own search window, so `Cursor.Position`
and `Cursor.Size` describe that window and not the whole fetch. The rule cannot open loops of its own: `For`, `While`,
`Do` and `GoTo` are refused, as are `Exit` and `Continue` in any other form. The cursor words are refused in Gateway
Rules, Activation Rules, Abstraction Calculations and Inline Functions, and they are not offered in the Builder.

To aggregate the third and fifth transaction in the window, with the Function Type set to Sum on the amount:

```vb
Matched = Cursor.Position = 3 OR Cursor.Position = 5
If Cursor.Position >= 5 Then Exit For
```

Set Iteration Order to Descending and the same rule aggregates the third and fifth most recent transaction. To
aggregate the third and fifth declined transaction instead, count the declines as the walk proceeds in a
[rule variable](#rule-variables):

```vb
Static declines As Integer
If Payload.ResponseCode <> "05" Then Continue For
declines += 1
Matched = declines = 3 OR declines = 5
If declines >= 5 Then Exit For
```

To find how many of the most recent transactions it took to move 10,000, set Iteration Order to Descending and the
Function Type to Count:

```vb
Static total As Double
total += Payload.Amount
Matched = True
If total >= 10000 Then Exit For
```

## Rule variables

An Abstraction Rule written in the Coder can declare variables of its own, with the two Visual Basic words for the two
lifetimes a rule needs:

| Declaration           | Lifetime                                                                                                                                                                             |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Dim name As Type`    | The transaction being tested. The variable starts afresh for the next transaction.                                                                                                   |
| `Static name As Type` | The walk. The variable keeps its value from one transaction to the next, and starts afresh for every walk. It is never shared between two walks or two transactions being processed. |

The type is one of `Double`, `Integer`, `String`, `Boolean` and `DateTime`, and a variable starts at zero, empty text,
False or the earliest date. A `Dim` may be given a starting value from any expression the rule could otherwise write,
which is worked out each time the line is reached. A `Static` may be given a literal starting value of its own type: a
number, text in double quotes, or True or False.

```vb
Dim doubled As Double = Payload.Amount * 2
Static limit As Double = 10000
Static previous As String = ""
Matched = doubled > limit AND previous <> Payload.Country
previous = Payload.Country
```

A variable is read, assigned and has methods called on it like a field: `total += Payload.Amount`,
`previous <> Payload.Country`, `doubled.Round(2)`. To accept a transaction whose country differs from the one before it:

```vb
Static previous As String = ""
Matched = previous <> "" AND previous <> Payload.Country
previous = Payload.Country
```

The rules for a declaration are deliberately narrow, because a declared name is a word the rule author has chosen
rather than one the installation has allowed:

- A declaration is a line of its own, anywhere in the rule. A variable is in reach of the whole rule wherever it is
  declared, and a name can be declared once.
- A name is letters and digits starting with a letter, at most 64 characters, and a rule may declare 32 variables. A
  Visual Basic reserved word, and a name the rule wrapper uses itself such as `Cursor`, `Matched` or `Data`, is
  refused.
- A declared name can never follow a dot, and can never stand where a type or another name is expected. `Cursor.total`
  is refused however `total` was declared, so declaring a name does not make any member of any object reachable.
- A declared name hides anything else of the same name for the whole rule, so a variable called `File` is that
  variable and never the framework type.
- Variables are for Abstraction Rules. `Dim` and `Static` are refused in Gateway Rules, Activation Rules, Abstraction
  Calculations and Inline Functions, and they are not offered in the Builder.

A rule that declares a `Static` variable depends on the walk in the same way as a rule that uses the cursor: it is
tested only against the transactions inside its own search window. A rule that declares only `Dim` variables does not
depend on the walk and is treated as it always was. Where an Abstraction Rule has no Search Key it is tested against
the transaction being processed alone, and a `Static` variable then behaves as a `Dim`.

An installation that had already added `Dim`, `Static` and its own variable names to the allowed words keeps exactly
the behaviour it had: a rule that was accepted before this feature existed is compiled as it always was, and only a
rule that would otherwise have been refused is read as a declaration described here.

## Returning a value the rule computes itself

Function Type Cursor Result returns whatever number the rule left in `Cursor.Result` when the walk ended, instead of
aggregating a field. It needs no Function Key and ignores the Offset reduction, and which transactions the rule
accepted does not affect it. The value starts at zero for each walk. To return half of every amount in the window,
weighted and summed in one pass:

```vb
Cursor.Result = Cursor.Result + Payload.Amount * 0.5
```

A rule that assigns `Cursor.Result` does not also have to set `Matched`.

## Function catalogue and parameters

In addition to the functions described above, the following functions aggregate the Function Key over the final list
of matches. Some take static parameters. The form shows one input for each parameter of the selected Function Type,
with its default and allowed range; over the API they are the `searchFunctionParameters` map of parameter name to
number, for example `{"trimFraction": 0.1}`, where a true or false parameter is given as 1 or 0. A parameter left
out takes its default, and the map can be left empty to take every default. A function that cannot be computed for the
matches it is given, for example a z-score over
fewer than three values, returns zero. As with the functions above, a match that does not carry the Function Key
contributes zero.

| Value                       | Id | Description                                                                                                                                                                                                                                   | Parameters                                                                                                                                                 | Follows iteration order |
|-----------------------------|----|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------|
| Geometric Mean              | 17 | Geometric mean: the nth root of the product of the values. Suits ratios and growth rates. Every value must be greater than zero, otherwise the result is zero.                                                                                | None                                                                                                                                                       | No                      |
| Harmonic Mean               | 18 | Harmonic mean: the count divided by the sum of reciprocals. Suits rates. Every value must be greater than zero, otherwise the result is zero.                                                                                                 | None                                                                                                                                                       | No                      |
| Trimmed Mean                | 19 | Mean after discarding the lowest and highest share of the sorted values, so a few extreme transactions do not move it.                                                                                                                        | `trimFraction` (default 0.1): Share discarded from each tail, from 0 up to but not including 0.5.                                                          | No                      |
| Winsorised Mean             | 20 | Mean after replacing the lowest and highest share of the sorted values with the nearest retained value, limiting extremes while keeping the count.                                                                                            | `winsorFraction` (default 0.05): Share replaced in each tail, from 0 up to but not including 0.5.                                                          | No                      |
| Exponential Weighted Mean   | 21 | Exponentially weighted mean in iteration order: the last value has weight 1 and each earlier value counts for less by a constant factor, so under Ascending the most recent transactions dominate.                                            | `alpha` (default 0.3): Smoothing factor, greater than 0 and at most 1. Higher values weight the latest records more heavily.                               | Yes                     |
| Root Mean Square            | 22 | Root mean square: the square root of the mean of the squared values. A magnitude measure that gives large values more weight than the plain mean.                                                                                             | None                                                                                                                                                       | No                      |
| Variance                    | 23 | Variance: the average squared distance from the mean.                                                                                                                                                                                         | `unbiased` (default true): True uses the sample estimator (divides by count minus one) and needs at least two values; false uses the population estimator. | No                      |
| Range                       | 24 | Range: the largest value minus the smallest value.                                                                                                                                                                                            | None                                                                                                                                                       | No                      |
| Interquartile Range         | 25 | Interquartile range: the 75th percentile minus the 25th percentile, the spread of the middle half of the values.                                                                                                                              | None                                                                                                                                                       | No                      |
| Percentile                  | 26 | Percentile by linear interpolation between the two nearest sorted values, the spreadsheet PERCENTILE.INC convention.                                                                                                                          | `percentile` (default 95): Percentile to return, from 0 to 100. 50 is the median.                                                                          | No                      |
| Median Absolute Deviation   | 27 | Median absolute deviation: the median distance from the median, a spread measure that extreme values barely move.                                                                                                                             | `scaleToNormal` (default true): True multiplies by 1.4826 so the result is comparable to a standard deviation for normally distributed data.               | No                      |
| Mean Absolute Deviation     | 28 | Mean absolute deviation: the average distance from the centre of the values.                                                                                                                                                                  | `aroundMedian` (default false): True measures distance from the median; false measures distance from the mean.                                             | No                      |
| Coefficient Of Variation    | 29 | Coefficient of variation: the standard deviation divided by the absolute mean, a unit-free measure of how erratic the values are.                                                                                                             | `unbiased` (default true): True uses the sample estimator (divides by count minus one) and needs at least two values; false uses the population estimator. | No                      |
| Entropy                     | 30 | Shannon entropy, in bits, of how often each distinct value occurs. Zero when every value is the same, higher the more evenly the values are spread across distinct amounts.                                                                   | None                                                                                                                                                       | No                      |
| Gini Coefficient            | 31 | Gini coefficient: 0 when every value is equal, approaching 1 when one value accounts for nearly the whole total. Values must not be negative, otherwise the result is zero.                                                                   | None                                                                                                                                                       | No                      |
| Nth Value                   | 32 | The value at a given position in iteration order. Zero when there are fewer values than the position asks for.                                                                                                                                | `position` (default 1): One-based position in iteration order. 1 is the first value.                                                                       | Yes                     |
| First To Last Change        | 33 | The last value minus the first value in iteration order: the net movement across the matched records.                                                                                                                                         | None                                                                                                                                                       | Yes                     |
| Largest Step                | 34 | The largest absolute difference between two consecutive values in iteration order: the biggest single jump from one record to the next.                                                                                                       | None                                                                                                                                                       | Yes                     |
| Count Above                 | 35 | How many values are strictly above the threshold.                                                                                                                                                                                             | `threshold` (default 0): The value to compare against. A value equal to the threshold is not counted.                                                      | No                      |
| Count Below                 | 36 | How many values are strictly below the threshold.                                                                                                                                                                                             | `threshold` (default 0): The value to compare against. A value equal to the threshold is not counted.                                                      | No                      |
| Share Above                 | 37 | The share of values, from 0 to 1, that are strictly above the threshold.                                                                                                                                                                      | `threshold` (default 0): The value to compare against. A value equal to the threshold is not counted.                                                      | No                      |
| Sum Above                   | 38 | The sum of only those values that are strictly above the threshold.                                                                                                                                                                           | `threshold` (default 0): The value to compare against. A value equal to the threshold is not counted.                                                      | No                      |
| Longest Run Above           | 39 | The longest run of consecutive values, in iteration order, that are all strictly above the threshold.                                                                                                                                         | `threshold` (default 0): The value to compare against. A value equal to the threshold is not counted.                                                      | Yes                     |
| Last Value Z-Score          | 40 | How many standard deviations the last value in iteration order sits from the mean of the values before it. Under Ascending the last value is the current transaction, so this scores it against its own history. Needs at least three values. | None                                                                                                                                                       | Yes                     |
| Last Value Modified Z-Score | 41 | Robust score of the last value in iteration order against the values before it, using their median and median absolute deviation, so earlier extremes do not hide a new one. Needs at least three values.                                     | None                                                                                                                                                       | Yes                     |
| Last Value Percentile Rank  | 42 | The percentage, from 0 to 100, of the values before the last one in iteration order that are less than or equal to it. 100 means the last value is the largest seen.                                                                          | None                                                                                                                                                       | Yes                     |
| Last Value To Mean Ratio    | 43 | The last value in iteration order divided by the mean of the values before it. 3 means the last value is three times the earlier average.                                                                                                     | None                                                                                                                                                       | Yes                     |
| Trend Slope                 | 44 | Least-squares slope of the values against their position in iteration order: the average change from one record to the next. Positive means the values are rising.                                                                            | None                                                                                                                                                       | Yes                     |
| Autocorrelation             | 45 | Autocorrelation of the values with themselves a number of records earlier in iteration order, from -1 to 1. Values near 1 mean a repeating or persistent pattern.                                                                             | `lag` (default 1): How many records back to compare each value with. 1 compares each value with the one before it.                                         | Yes                     |

Functions marked as following iteration order read the values in sequence, so their result changes with Iteration
Order and with the Offset reduction. Under Ascending the last value is the transaction being processed, which is what
the Last Value functions score against the history before it.

## The search window

The window an abstraction rule aggregates over ends at the Reference Date of the transaction being processed and
reaches back by the Search Interval and Search Value. The Search Key's own TTL Interval, set on the Request XPath page,
can shorten it: the window starts at the later of the rule's interval and the search key's TTL, counted back from the
Reference Date. The engine does this silently, so a rule counting over the last 30 days on a search key whose history
is kept for 7 days counts over 7 days. A search key with no TTL set keeps no history for the window, which collapses to
the Reference Date itself. [Model Integrity](../ModelIntegrity/index.html) reports both cases.

The history is fetched from the cache for the search key's value before the window is applied, up to the lower of the
model's Cache Fetch Limit and the search key's Fetch Limit. When that limit is reached, older transactions that fall
inside the window are not considered. The transaction being processed is added to the fetched history before the rule
is tested, so it counts towards its own aggregation. A search key retrieved from a cache (Search Key: Cache) does not
fetch history for each transaction; the value calculated in the background by the Search Key Cache thread is used.

## Validation

* Name is required, 256 characters or fewer, and unique within the Model (case-insensitive).
* The rule script, whether Builder or Coder, must be 65,536 characters or fewer and must compile; a script that does not
  compile is refused with the error positions.
* When Search is on, a Search Key is required, Search Interval must be Seconds, Minutes, Hours or Days, and Search Value
  must be zero or more and within the limit for the interval: 2,000,000,000 seconds, 52,560,000 minutes, 876,000 hours
  or 36,500 days.
* When Offset is on, Offset Value must be between 0 and 1,000,000.
* When Search is on, Iteration Order must be Ascending or Descending.
* Function Parameters must name only parameters of the selected Function Type, with each value a number inside that
  parameter's range (1 or 0 for a true or false parameter). Parameters given to a function that takes none are refused.
* The Model must exist in the caller's tenant and must not be deleted. Editing needs permission 13.

<!-- TODO(docs): confirm permission 13 against the Roles and Permissions pages, and whether Seconds and Minutes should be offered at all in the Search Interval picker -->
<!-- TODO(docs): the Search Value and Offset Value upper bounds were added in this review (model-abstraction); owner to confirm the limits are acceptable -->

## Rule scripts are user supplied code

Builder and Coder scripts are translated and compiled against an allow-list of tokens; names and constructs outside it
are refused when the rule is saved and again when the engine loads it. Scripts are limited to 65,536 characters and a
fixed nesting depth. The simulation runner executes them with a timeout; whether the live engine applies one is not
verified here.

<!-- TODO(docs): owner to confirm the wording of this paragraph against the Rule Builder and Coder page; the corpus is tested in RuleParseSecurityCorpusTests and RuleParseNestingDepthTests -->
<!-- TODO(screenshot): file=model-abstraction-rule-coder-error.png route=/models state="Abstraction Rule Coder tab showing a compile error refused on save" -->
