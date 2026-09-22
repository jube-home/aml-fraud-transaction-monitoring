---
layout: default
title: Unstructured Text Screening
nav_order: 7
parent: Rule Taxonomy
---

# Unstructured Text Screening

**Definition:** the signal is hidden in free text, not a structured field.

**Construction:** `String` extensions (`Contains`, `ContainsAny`, `ContainsAll`) against a `Payload` text field, checked
against a `List` of terms. Use it when a structured check,
per [Static and Match Screening](StaticAndMatchScreening.html), would not see the signal. See
the [Rule Taxonomy](../RuleTaxonomy.html).

| Question an analyst asks                                          | Rule Taxonomy type that answers it                                        |
|-------------------------------------------------------------------|---------------------------------------------------------------------------|
| Does the payment reference mention crypto or gift cards?          | Unstructured Text Screening: `ContainsAny`                                |
| Does the narrative mention both an invoice and an urgent request? | Unstructured Text Screening: `ContainsAll`                                |
| Is the account number on the deny list?                           | Not this type: [Static and Match Screening](StaticAndMatchScreening.html) |
| Is the name close to a sanctioned party?                          | Not this type: Sanctions, in Static and Match Screening                   |

## Unstructured Text Screening: Construction Steps

Identify the free text field in the payload (a narrative, reference, memo or description), decide whether the terms are
few and stable or many and changing, then write an Activation Rule using the `String` extensions. Terms that change
belong in a List, so that they can be maintained without editing the rule. The `String` extension family is discovered
by reflection from `Jube.Dictionary/Extensions`, and is described in
[Rule Compilation Tokens and Extensions](../../Configuration/Models/RuleCompilationAlgorithm/index.html).

### Step 1: Match a Small Fixed Set of Terms

A few terms are written inline. Any one of them, ignoring case (`ContainsAny` is the case sensitive form). Every test is
null safe, so an empty narrative fails the test and does not fault the rule. See the
[Rule Pipeline Reference](../RulePipelineReference.html):

```vb
Matched = Payload.Narrative.MatchContainsAnyIgnoreCase("crypto", "gift card", "wire")
```

All of them, for a combination that only matters together:

```vb
Matched = Payload.Narrative.MatchContainsAll("urgent", "invoice")
```

`ContainsAll` has no ignore case form, so lower the text first with `Lower()`, and write the terms in lower case.

### Step 2: Match a Maintained List of Terms

Where the terms are maintained by analysts, hold them in a List
(see [Lists](../../Configuration/Models/Lists/index.html))
and use the List extensions of the fluent family. For example, a case insensitive match against any term in a List:

```vb
Matched = Payload.Narrative.MatchContainsAnyInListIgnoreCase(List.HighRiskTerms)
```

The related methods (`ContainsAllInList`, `ContainsNoneInList`, `StartsWithAnyInList`, `InList`, `NotInList`,
`EmailDomainInList`) are in `Jube.Dictionary/Extensions` with a `Flow.String.` prefix, and their behaviour is defined
there.

### Step 3: Normalise Before Matching

Free text is dirty, and evasion is deliberate. Transform before testing. `Lower()` and `Upper()` begin a
pipeline and are null safe. `Trim()` on a plain value is the plain `string` method, which throws on null and so faults the
rule, so use `Payload.Name.Start().Trim()` where null is possible. The richer string transformers (`RemoveDiacritics`, `Replace`, `ReplaceByEmpty`, `Extract`) are plain
extension methods, applied before the pipeline steps, per the [Rule Authoring Methodology](../RuleAuthoringMethodology.html). Where
a field is an identifier and not prose, `EmailDomainEqual`, `EmailDomainIn`, `EmailAliasNormalisedEqual`, `IsValidEmail`,
`IsValidIban`, `IsValidBic` and `IsValidLuhn` are tests.

```vb
Matched = Payload.Narrative.RemoveDiacritics().MatchContainsAnyIgnoreCase("crypto", "gift card")
```

### Step 4: Fuzzy Matching

Exact and substring matching miss deliberate or accidental misspelling, such as `crypt0`, `g1ft card` or `Bitcoinn`.
Fuzzy matching accepts a near miss. The fuzzy steps fall into three groups, by what is being compared with what.

**A short field against a term or another field.** `LevenshteinAtMost(other, distance)` accepts a number of single
character edits (insertions, deletions, substitutions), so zero is identical and smaller is closer.
`SimilarityAtLeast(other, threshold)` is the same comparison as a 0 to 1 score, easier to threshold when the strings vary
in length. `JaroWinklerAtLeast(other, threshold)` favours a shared start, often better for names, and `SoundexEqual(other)`
compares how the words sound. These compare **two whole strings**:

```vb
Matched = Payload.PaymentReference.MatchLevenshteinAtMost("bitcoin", 1)
```

```vb
Matched = Payload.PaymentReference.MatchSimilarityAtLeast("gift card", 0.85)
```

```vb
Matched = Payload.PayeeName.MatchJaroWinklerAtLeast(Payload.AccountHolderName, 0.9)
```

**A short field against a maintained List.** The `...InList` forms compare the value with every entry of a List, so the
terms are maintained outside the rule: `LevenshteinAtMostAnyInList`, `SimilarityAtLeastAnyInList`,
`JaroWinklerAtLeastAnyInList`, and the fuller `SomehowInList`, which folds case, accents and punctuation and accepts a
Jaro-Winkler or token sort similarity of at least 0.92:

```vb
Matched = Payload.MerchantName.MatchSomehowInList(List.HighRiskMerchants)
```

**A term inside long text.** A whole string comparison finds nothing in a long narrative, as the distance from the
narrative to the term is large. `ContainsFuzzy(term, threshold)` instead looks for a run of words in the text that is at
least the Jaro-Winkler threshold similar to the term, so a misspelt name inside a narrative is found. `ContainsFuzzyAny`
takes several terms, `ContainsFuzzyAnyInList(list, threshold)` takes a List, and `ContainsSomehowInList` is the fuller
form:

```vb
Matched = Payload.Narrative.MatchContainsFuzzy("bitcoin", 0.9)
```

```vb
Matched = Payload.Narrative.MatchContainsFuzzyAnyInList(List.HighRiskTerms, 0.9)
```

An options string tunes the fuzzy List steps that end in `With` (`SomehowInListWith`, `ContainsSomehowInListWith`): the
algorithm (Levenshtein, Damerau, Jaro-Winkler, Dice, token sort, token set, Soundex, initials), the normalisation (case,
accents, punctuation, titles, company suffixes), and a minimum length. Each option is defined in the
[Rule Pipeline Reference](../RulePipelineReference.html). An invalid options string fails closed:

```vb
Matched = Payload.Narrative.MatchContainsSomehowInListWith(List.HighRiskTerms, "lev=1,minlength=5")
```

| Question in text screening                                    | Step to use                                                      |
|---------------------------------------------------------------|------------------------------------------------------------------|
| Is a short field a near miss of one known term?               | `LevenshteinAtMost`, or `SimilarityAtLeast` for a 0 to 1 score   |
| Is a short field a near miss of any term in a List?           | `SomehowInList`, or `LevenshteinAtMostAnyInList`                 |
| Do two name fields nearly match?                              | `JaroWinklerAtLeast`, or `SimilarityAtLeast`                     |
| Do two names sound the same?                                  | `SoundexEqual`                                                   |
| Is a term, or a misspelling of it, inside long text?          | `ContainsFuzzy`, `ContainsFuzzyAnyInList`, `ContainsSomehowInList` |
| Do I want the score itself, to threshold or to store?         | `FuzzyBestScore`, `FuzzyMatchCount`, `FuzzyBestMatch`            |
| Is a name a near match to a regulatory list?                  | Not this type: Sanctions, in [Static and Match Screening](StaticAndMatchScreening.html) |

A fuzzy List match compares every event with every entry, so for a large List put an exact `InList` first, and use
`minlength` for short values, where similarity is noisy. Sanctions use the same principle as a purpose built, indexed
engine over regulatory lists, token by token and order insensitive, with distance and ratio controls. Prefer it for names
against regulatory lists, and use these steps for the reference and narrative fields that Sanctions does not cover.

### Step 5: Structural Signals

`ShannonEntropyAbove(threshold)` measures how random a string is, and is a useful signal for generated or obfuscated text.
`Matches(pattern)` is a regular expression test for structured fragments hidden in text, and never throws on a bad
pattern. `HasSequentialDigits(minimumRunLength)` and `IsAllSameCharacter` flag synthetic values.

```vb
Matched = Payload.Narrative.MatchShannonEntropyAbove(4.5)
```

## Unstructured Text Screening: Common Faults

Symptoms of a misbuilt text rule, with the likely cause.

| Symptom in a text rule                  | Likely cause                                                                                                  |
|-----------------------------------------|---------------------------------------------------------------------------------------------------------------|
| A known term is missed                  | Case, accents or spacing differ. Normalise the text first, or use a case insensitive comparison.              |
| Far too many matches                    | Substring matching: a short term matches inside longer words. Use longer terms or a pattern match.            |
| A null narrative behaves unexpectedly   | Pipeline tests are null safe and fail on null. Only `IsNull`, `IsNullOrEmpty` and the `IsNotValid...` tests are true for null. |
| The term list is embedded in many rules | Move the terms to a List and reference it.                                                                    |
| A misspelt term is missed               | Exact matching. Use `LevenshteinAtMost` on a short field, or `ContainsFuzzy` for long text.                    |
| A fuzzy rule matches unrelated text     | The tolerance is too loose for the term's length. Raise the threshold, or add `minlength` in an options string. |
| A fuzzy rule is slow                    | A large List is compared on every event. Put an exact `InList` first, or add `firstletter`.                    |
