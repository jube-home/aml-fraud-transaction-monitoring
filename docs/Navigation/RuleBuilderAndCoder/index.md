---
layout: default
title: Rule and Code Builder
nav_order: 8
parent: Navigation
---

# Rule and Code Builder

Several pages in the platform rely on the construction of code and in most cases this is facilitated by a point and
click construction tool. In most cases the code constricted is of vb.net fragment dialect, unless it is used for
reporting, case management or Exhaustive training, in which case it is an SQL query fragment:

<!-- TODO(screenshot): file=rule-authoring-builder-rule.png route=/models/activation-rules state="Activation rule open, Builder tab with two conditions" -->
![Image](BuilderRule.png)

In the following example the Rule and Code Constructor is embedded in the Activation Rules page and has a comprehensive
range of data available:

<!-- TODO(screenshot): file=rule-authoring-activation-rule-builder.png route=/models/activation-rules state="Activation Rules page with the embedded Rule and Code Builder" -->
![Image](ActivationRuleBuilder.png)

<!-- TODO(screenshot): file=rule-authoring-available-data.png route=/models/activation-rules state="Field drop-down open showing every group of available data" -->
![Image](AvailableData.png)

There are oftentimes two modes to create rules, being Builder (which is point and click) and Coder (which is the
handcrafting of the vb.net). Clicking on the Coder tab will allow for the VB.Net code fragment to be handcrafted:

<!-- TODO(screenshot): file=rule-authoring-click-for-coder.png route=/models/activation-rules state="Builder above the Coder, rule text shown in the Coder" -->
![Image](ClickForCoder.png)

The Builder and Coder is available for the creation of VB.Net fragments in:

* Gateway Rules.
* Activation Rules.
* Abstraction Rules.
* Reprocessing Rules.

Inline Functions can be used in all of these rules, including Reprocessing Rules, and appear in the Builder's field list
alongside the Payload fields.

For reasons of creating SQL fragments and not VB.Net fragments. Only the Builder is available in:

* Case Workflow Filters.
* Cases Search.
* Exhaustive Adaptation Class Definition.

An example of only the builder being available is as follows for Models >> Cases Workflows >> Cases Workflow Filter:

<!-- TODO(screenshot): file=rule-authoring-only-builder-available.png route=/models/cases-workflows state="Case Workflow Filter with the builder only" -->
![Image](OnlyBuilderAvailable.png)

Only the Coder is available for the creation of VB.Net fragments in:

* Inline Functions.
* Abstraction Calculations.

An example of only the coder being available is as follows for Models >> References >> Inline Functions:

<!-- TODO(screenshot): file=rule-authoring-only-coder-available.png route=/models/references/inline-functions state="Inline Function with the coder only" -->
![Image](OnlyCoderAvailable.png)

The default Coder value will be taken from the Builder, being the VB.Net fragment equivalent:

<!-- TODO(screenshot): file=rule-authoring-default-code-from-builder.png route=/models/activation-rules state="Coder showing the text written by the Builder" -->
![Image](DefaultCodeFromBuilder.png)

As typing, the freehand code will be continually parsed for integrity:

<!-- TODO(screenshot): file=rule-authoring-errors-parsed.png route=/models/activation-rules state="Coder with a squiggle and the compile status badge reading Errors" -->
![Image](ErrorsParsed.png)

With errors being displayed in the tool tip along the coder guttering. Parsing will happen on the conclusion of each
typing burst, until such time as the rule compiles:

<!-- TODO(screenshot): file=rule-authoring-compiled-rule.png route=/models/activation-rules state="Coder with the compile status badge reading Compiled" -->
![Image](CompiledRule.png)

To facilitate the development of Coder rules, completions are populated on each keypress, returning model
configurations:

<!-- TODO(screenshot): file=rule-authoring-completions.png route=/models/activation-rules state="Coder with the completion list open" -->
![Image](Completions.png)

Only what the engine would actually run is offered: an item that has been made inactive (a Request XPath, Inline Script,
Inline Function, Dictionary, List, TTL Counter, Sanction, Abstraction Rule, Abstraction Calculation, HTTP Adaptation,
Exhaustive instance or Tag whose Active switch is off) does not appear in the completions, the field lists of the rule
builders, or the fields offered for reporting, and appears again as soon as it is made active. The Active switch of the
model itself does not hide its fields, so that rules can be built for a model that is still a draft.

If the Coder diverges from what was originally created in the Builder, then the Builder tab will be disabled:

<!-- TODO(screenshot): file=rule-authoring-divergence.png route=/models/activation-rules state="Builder locked after the Coder text was edited, with the Reset button" -->
![Image](Divergence.png)

Clicking the Reset link will restore the Builder contents to its Coder representation:

<!-- TODO(screenshot): file=rule-authoring-reset-to-builder.png route=/models/activation-rules state="Reset button about to be pressed" -->
![Image](ResetToBuilder.png)

Upon clicking Reset, the Coder will be overwritten to the parsed contents of the Builder, and the tab will become
enabled once again:

<!-- TODO(screenshot): file=rule-authoring-builder-enabled.png route=/models/activation-rules state="Builder enabled again after Reset" -->
![Image](BuilderEnabled.png)

## Fields available to each rule

The fields offered depend on the kind of rule, because the invocation pipeline works them out in stages and a rule can
only use what has been worked out before it runs:

| Rule                            | Fields it may use                                                                                               |
|---------------------------------|-----------------------------------------------------------------------------------------------------------------|
| Gateway Rule, Reprocessing Rule | Payload fields, inline functions, dictionary values and lists.                                                  |
| Abstraction Rule                | As a Gateway Rule, but only payload fields that are cached.                                                     |
| Activation Rule                 | Everything: also TTL counters, abstractions, sanctions, calculations, adaptations and earlier activation rules. |

The fields offered and the fields the rule compiler accepts differ in one respect: an Abstraction Rule is offered TTL
counters and sanctions, and an Abstraction Calculation is offered abstraction calculations, but neither will compile
with them. A rule using one is refused when it is saved, in the Builder as in the Coder.

## Checking a rule when it is saved

A rule is checked again when it is saved. A rule whose text does not parse or compile is refused, with the line and
position of each error, and nothing is saved. The check follows the engine: it checks the Builder or Coder text,
whichever is selected, and only restricts a Builder Abstraction Rule to cached payload fields, so it never refuses a
rule the engine would accept. Reprocessing Rules are checked as Gateway Rules.

## Builder operators

Each condition in the Builder is a field, an operator and the operator's values. The operator drop-down only lists the
operators that suit the field's type, under headings: **Basic** first, then the groups below, each in alphabetical
order. The same operators are offered in every rule builder: Gateway, Abstraction, Activation and Reprocessing Rules,
and the backtest filters.

### Basic

The builder's usual comparisons. Rules saved before the other groups were added keep exactly the rule text they always
produced.

| Operator         | Allowed for                   | Values   | Rule text produced                                     |
|------------------|-------------------------------|----------|--------------------------------------------------------|
| equal            | Text, numbers, dates, Boolean | value    | `field = value`                                        |
| not equal        | Text, numbers, dates, Boolean | value    | `field <> value`                                       |
| in               | Text, numbers                 | values   | `field.MatchIn(a, b, …).ToBoolean()`                   |
| not in           | Text, numbers                 | values   | `field.MatchNotIn(a, b, …).ToBoolean()`                |
| less             | Numbers, dates                | value    | `field < value`                                        |
| less or equal    | Numbers, dates                | value    | `field <= value`                                       |
| greater          | Numbers, dates                | value    | `field > value`                                        |
| greater or equal | Numbers, dates                | value    | `field >= value`                                       |
| between          | Numbers, dates                | from, to | `field.MatchInRange(from, to).ToBoolean()` (inclusive) |
| not between      | Numbers, dates                | from, to | `field.MatchInRange(from, to).ToBoolean() = False`     |
| begins with      | Text                          | value    | `field.StartsWith(value)`                              |
| not begins with  | Text                          | value    | `field.StartsWith(value) = False`                      |
| contains         | Text                          | value    | `field.Contains(value)`                                |
| not contains     | Text                          | value    | `field.MatchContainsNone(value).ToBoolean()`           |
| ends with        | Text                          | value    | `field.EndsWith(value)`                                |
| not ends with    | Text                          | value    | `field.EndsWith(value) = False`                        |
| is empty         | Text                          | none     | `field.MatchIsNullOrEmpty().ToBoolean()`               |
| is not empty     | Text                          | none     | `field.MatchIsNotNullOrEmpty().ToBoolean()`            |
| is null          | Text                          | none     | `field.MatchIsNull().ToBoolean()`                      |
| is not null      | Text                          | none     | `field.IsNotNull()`                                    |

### Lists

A model list has one operator, **has**, which tests whether the list contains the value of a text field chosen from a
list, for example `List.HighRisk.contains(Payload.Country)`.

### Compare to another field

Equal, not equal, greater, greater or equal, less and less or equal can also compare a field with another field of the
same type, chosen from a list, for example `Payload.BillingCountry <> Payload.IpCountry`. Equal and not equal work for
text, numbers and dates; the others for numbers and dates.

### Every pipeline test

Every test in the [Rule Pipeline Reference](../../RuleTaxonomy/RulePipelineReference.html) that can be called on a plain
text, whole number, number, date or Boolean value is an operator, written as its `Match` step followed by
`.ToBoolean()`, for example `Payload.Iban.MatchIsValidIban().ToBoolean()` or
`Payload.Amount.MatchIsJustBelowAnyThreshold(10, 1000, 10000).ToBoolean()`. The list is read from the extension library
when Jube starts, so a new pipeline test becomes an operator with no other change. Its label is the step's name in words
(`MatchIsJustBelowAnyThreshold` is **Is just below any threshold**; `RatioOfAbove` is **Ratio of: above**).

A test that a Basic operator already covers is not offered twice. The pipeline's `Between` is offered as **Between
exclusive**, because unlike **between** it excludes its ends, and `IsEmpty` as **Is empty string**.

| Group                                      | For     | Examples                                                                                                                              |
|--------------------------------------------|---------|---------------------------------------------------------------------------------------------------------------------------------------|
| Text: comparison                           | Text    | Equals, ignoring case; In, ignoring case                                                                                              |
| Text: contains and patterns                | Text    | Contains any, Contains all, Contains ignoring case, Matches (a regular expression), Is JSON with the path                             |
| Text: model lists                          | Text    | In list, Not in list, Starts with any in list, Ends with any in list, Contains any in list, Contains none in list                     |
| Text: shape and length                     | Text    | Is numeric, Is alpha, Is all same character, Length in range, Has sequential digits, Shannon entropy above                            |
| Text: validation                           | Text    | Is valid IBAN, BIC, card number (Luhn), card expiry, email; ABA, country and currency codes, CUSIP, ISIN, LEI, SEDOL, UUID, JSON, XML |
| Text: email                                | Text    | Email domain equal, Email domain in, Email domain in list, Email alias normalised equal                                               |
| Text: network                              | Text    | Is valid IP, Is a private, public, loopback or reserved IP address, Is an IP address in the CIDR range                                |
| Text: fuzzy similarity                     | Text    | Soundex, Jaro-Winkler, Levenshtein, Damerau, Dice, token set and token sort, somehow like; each with its threshold                    |
| Number: comparison and value               | Numbers | Is zero, positive, negative, whole number, even, odd, prime; Is multiple of; Has value                                                |
| Number: thresholds and limits              | Numbers | Is just below or above a threshold or any of several, Is round amount, Headroom to, Shortfall below, Excess over                      |
| Number: change and growth                  | Numbers | Increased or decreased by more than a percentage, Deviates from by percent, Relative change, Growth factor, Slope                     |
| Number: ratios and shares                  | Numbers | Ratio of, Inverse ratio, Log ratio, Percent of, Share of sum, Complement, Imbalance with                                              |
| Number: statistics                         | Numbers | Z score, Modified z score, Coefficient of variation, Min–max normalise, Mean, sum, spread, Herfindahl and entropy with others         |
| Number: geography                          | Numbers | Distance, Within or outside a radius (km or miles), Implied speed                                                                     |
| Date: comparison and windows               | Dates   | Before, After, Same day as, Within minutes, hours or days of, More than days after                                                    |
| Date: calendar                             | Dates   | Is weekday, Is weekend, Is start of month, Is end of month, Day of week in                                                            |
| Date: time of day and business hours (UTC) | Dates   | Is morning, Is afternoon, Within or outside (weekday) business hours, in UTC                                                          |
| Date: time zones                           | Dates   | The same weekday, weekend and business hours tests in a named time zone                                                               |
| Date: relative to when the rule runs       | Dates   | Is past, Is future, Is today, Older than days, Younger than days                                                                      |
| Boolean                                    | Boolean | Is true, Is false                                                                                                                     |

The groups for time of day, time zones, the clock and fuzzy matching are labelled plainly because their answer depends
on the time zone, on when the rule runs, or on a threshold rather than an exact match. Their answer for a transaction
can change when it is reprocessed or backtested later.

A few validation checks have no pipeline test and are written as the plain extension call, for example
`Payload.Isin.IsValidIsin()`: ABA routing number, country code, currency code, CUSIP, domain name, ISIN, JSON, LEI,
SEDOL, UUID and XML validity, Is JSON with the path, Is an IP address in the CIDR range, and the private, public,
loopback and reserved IP address checks.

Every operator is written with `.ToBoolean()` because Visual Basic will not combine a plain comparison written before a
pipeline with `AND` or `OR` (`Payload.Amount > 1 AND Payload.Amount.MatchInRange(1, 5)` does not compile, while the
reverse order does). `.ToBoolean()` makes every operator an ordinary Boolean, so conditions combine in any order and
under NOT.

### Values

Once an operator is chosen, the Builder shows one box per value it takes, with the value's name as its placeholder: a
text box, or a list to choose from for a model list or another field. Where a value takes several items, such as the
values of **in** or the thresholds of **Is just below any threshold**, separate them with commas, for example `GB, FR`.

A value that is a number, a whole number or a date may also name a field of that type, and the rule then compares
against the field, for example `TTLCounter.PerAccount` as the first value of **Ratio of: above** is written
`Payload.Amount.MatchRatioOfAbove(TTLCounter.PerAccount, 2)`. Text values are always taken literally.

Text is written as a VB.NET string: it is wrapped in double quotes and a double quote inside it is doubled, so
`say "hi"` becomes `"say ""hi"""`. A date is written as ISO text, such as
`2026-01-31T00:00:00Z` (a date without a time zone is read as UTC), and becomes
`"2026-01-31T00:00:00Z".ToIsoDateTime()` in the rule text, because Visual Basic date literals are not allowed in rules.

### How the Builder becomes rule text

The Builder writes the Coder text from the same templates the server uses, so the rule text is the same wherever the
rule was built:

- The conditions of a group are joined with ` AND ` or ` OR `, as chosen for the group.
- A nested group is written in parentheses, `( … )`.
- A group with NOT selected is written as `NOT ( … )`.
- The whole expression is wrapped as `If (expression) Then`, `Return True`, `End If`.

For example, a group with NOT selected, holding **Payload.Amount greater 1** and an OR group of **Payload.Country equal
GB** and **Payload.Country equal FR**, becomes:

```
If (NOT ( Payload.Amount > 1 AND ( Payload.Country = "GB" OR Payload.Country = "FR" )  )) Then
  Return True
End If
```

## Right-to-left languages

The Coder is always left-to-right, in an Arabic or Urdu page as in an English one, because code (`Payload.Amount > 100`)
reads left-to-right. Its gutter is on the left and the line starts on the left. Messages from the compiler are shown
with their own direction, so an English compiler message is not scrambled inside an Arabic page. The Builder around it
follows the page direction.

<!-- TODO(docs): confirm in an Arabic and an Urdu session that the Coder, the completion list and the squiggle tooltips look right, and add a screenshot of each. -->
<!-- TODO(screenshot): file=rule-authoring-coder-arabic.png route=/models/activation-rules state="Arabic locale, Coder showing left-to-right code under an Arabic Builder" -->

## Checking while typing

The Coder checks the rule shortly after each burst of typing. Each user may make `ParseMaxPerUserPerWindow` checks (120)
and a tenant `ParseMaxPerTenantPerWindow` (600) in each `ParseWindowSeconds` (60); past the limit the server answers
HTTP 429 and the Coder quietly checks again ten seconds later.

## Help while writing in the Coder

The Coder knows the rule dialect, the fields of the model and the words the rule type allows, and uses them to help as
the rule is written. None of it changes what a rule means or which rules compile: the server remains the only judge, and
a rule saved before this help existed reads, compiles and runs exactly as it did.

<!-- TODO(screenshot): file=rule-authoring-coder-colours.png route=/models/abstraction-rules state="Coder showing a cursor rule: coloured words, the three gutter marks and a warning squiggle" -->

### Colours

Each word is coloured by the part it plays, the same in the light and the dark theme:

| Part         | Examples                                   | Shown as                                    |
|--------------|--------------------------------------------|---------------------------------------------|
| Language     | `If`, `Then`, `And`, `Not`, `Static`, `As` | Navy, bold                                  |
| The answer   | `Matched`, `Return`                        | Navy, bold, on a pale tint                  |
| Scan control | `Exit For`, `Continue For`                 | Navy, bold, with a dotted underline         |
| Data         | `Payload.Amount`, `Cursor.Position`        | Teal; the object (`Payload`, `Cursor`) bold |
| Method       | `.StartsWith(…)`, `.Round(…)`              | Purple                                      |
| Literal      | `1000`, `"GB"`, `True`                     | Brown                                       |
| Comment      | `' why this rule exists`                   | Grey, italic                                |

Colour is never the only signal: the answer has its tint, the scan words their underline, and the gutter repeats both.

### Gutter marks

To the left of the line numbers, a small mark shows what a line does to the flow of the rule. Hovering a mark names it.

| Mark     | Meaning                                                 |
|----------|---------------------------------------------------------|
| Circle   | The line sets the answer (`Matched = …` or `Return …`). |
| Triangle | The line skips to the next record (`Continue For`).     |
| Square   | The line ends the scan (`Exit For`).                    |

### Completions

Completions are grouped under headings: **Fields**, **Members**, **Methods**, **Keywords**, **Snippets** and, in an
Abstraction Rule, **Variables** and **Patterns**.

- After an object and a dot, only what that object has is offered. `Cursor.` lists the cursor's properties and methods,
  each with its type and a sentence saying what it means. A field lists the methods that suit its type, and a method
  lists the methods that suit what it returns, so a chain such as `Payload.Name.Trim().StartsWith(` is guided at every
  step. Every method is offered with the sentence that describes it, lifted from the method itself, so `IsMatch` says
  that it checks the text against a regular expression and `MatchGreater` says what is true and what it decides.
- In an Abstraction Rule,
  the [variables the rule declares](../../Configuration/Models/AbstractionRules/index.html#rule-variables)
  are offered under **Variables** with their type and how long they live, and a variable followed by a dot lists the
  methods that suit its type. At the start of a line, **Snippets** also offers the `Dim` and `Static` declarations.
- At the start of a line in an Abstraction Rule, the **Patterns** group offers whole cursor rules to fill in: the Nth
  record, the first N matching records, a running total until a limit kept in a `Static` variable, skipping the
  transaction being processed, and returning a value the rule computes itself.
- The words offered are the words the rule type allows. `Cursor`, `Exit For`, `Continue For`, `Dim` and `Static` are
  offered in an Abstraction Rule only.

Hovering a word shows what it is: a field shows its type and group, a method each way it can be called and the
sentence that describes that call, a cursor member or a scan word the sentence that describes it, and a declared
variable its type and whether it is kept for the transaction being tested or for the whole scan. While the arguments of
a call are being typed, the signature help above the line shows the matching signatures with the current parameter in
bold, the sentence that describes the call, and what the current parameter is for when that is recorded.

### Warnings before the compiler answers

The Coder marks three kinds of slip with an amber squiggle as soon as they are typed, without waiting for the server:

| Warning                                 | Raised when                                                                           | Quick fix                       |
|-----------------------------------------|---------------------------------------------------------------------------------------|---------------------------------|
| *X is not a field of this model*        | An object that has known fields is given a field it does not have.                    | **Use** the nearest field name. |
| *X is not allowed in this kind of rule* | A word is not among the words the rule type allows, nor a variable the rule declares. | None.                           |
| *Cursor has no member called X*         | A member is asked of an object that does not have it.                                 | None.                           |

Hover the squiggle to read the warning and press the quick fix. A warning is advice: it never blocks saving, and where
the compiler reports the same place its message is shown instead. Objects whose fields the Coder has not been told about
are left alone, so a warning is only raised where the Coder can be sure.

One compiler error also carries a quick fix. Where a rule works out a value and does nothing with it, such as a line
reading `Payload.Amount > 1000`, the error offers **Assign to Matched**, which makes the line `Matched = Payload.Amount >
1000`.

### Closing a block

Pressing Enter at the end of an `If … Then` line or a `Select Case` line that has not yet been closed adds the matching
`End If` or `End Select` below and leaves the caret, indented, on the line between. A block that is already closed, and
a single-line `If … Then …`, are left as they are.

## Operator names in other languages

The operator, group and value names shown in the Builder are translated for the operators written by hand (Basic,
Lists, Compare to another field, validation and network checks) and for every group. The operators generated from the
extension library are shown in English in every language until a translation is added.

<!-- TODO(docs): confirm that the generated pipeline operators should stay English, or ask for their translations. -->

## Notes for the owner

<!-- TODO(docs): the Builder is described as a Builder tab and Coder tab in the text above; the Vue page stacks the Builder above the Coder with no tabs. Reword the paragraphs that mention clicking a Coder tab. -->
<!-- TODO(docs): the old Razor pictures above are stale. Every one has a screenshot marker. -->
<!-- TODO(docs): check that the list of rule types that show the Builder (Gateway, Activation, Abstraction, Reprocessing) matches showsBuilder in Jube.App/Vue/src/ruleBuilder/filters.ts. -->
<!-- TODO(docs): the Builder operator tables are copied from the server catalogue; regenerate them from BuilderOperators.cs before release. -->
<!-- TODO(docs): numbers are written with Western digits and a decimal point whatever the page language; state this once the owner has confirmed. -->
