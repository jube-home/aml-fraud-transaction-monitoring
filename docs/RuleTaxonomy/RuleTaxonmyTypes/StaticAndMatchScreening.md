---
layout: default
title: Static and Match Screening
nav_order: 6
parent: Rule Taxonomy
---

# Static and Match Screening

**Definition:** a deterministic or fuzzy match against a known reference set.

**Construction:** a `List` for internal reference data, and a `Sanction` for regulatory lists. A Sanction returns a
value, retrieved by `GetValueOrThrow`, which is compared, and is not a Boolean. Device blacklists are covered here, and
need no separate class. See the [Rule Taxonomy](../RuleTaxonomy.html).

| Question an analyst asks                                       | Rule Taxonomy type that answers it                                                      |
|----------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| Is this account on our internal deny list?                     | Static Screening: a List                                                                |
| Is this device on the blacklist?                               | Static Screening: a List of device identifiers                                          |
| Is this name close to a sanctioned party?                      | Match Screening: a Sanction                                                             |
| Is this name close to a name on our internal watch list?       | Match Screening: `SomehowInList` against a List                                         |
| Does the narrative mention a risky term?                       | Not this type: [Unstructured Text Screening](UnstructuredTextScreening.html)           |

## Static Screening: Lists

A List is maintained in the Lists page (or by CSV upload, database or API), and referenced from rules by name, without
embedding the values in the rule. Lists are synchronised by the engine in the background continuously and do not need a
model synchronisation, so a new value applies almost immediately. A List Value can be given a Delete Expiry Date, after
which it lapses, which suits temporary blocks. See [Lists](../../Configuration/Models/Lists/index.html).

A rule reads a List as `List.<Name>`, which is a collection of strings, and tests membership with the `InList` steps of a
pipeline (see the [Rule Pipeline Reference](../RulePipelineReference.html)). `InList` is case sensitive, and
`InListIgnoreCase` is not. Both are null safe, so a missing value fails the test and does not fault the rule:

```vb
Matched = Payload.AccountId.MatchInList(List.BadCustomers)
```

Combined with a threshold, using `Against` to switch to the amount, keeping the state:

```vb
Matched = Payload.AccountId.RequireInList(List.BadCustomers).Against(Payload.CurrencyAmount).MatchGreater(100)
```

An Accept list exemption, rejecting an event whose device is on a trusted List before any other step runs:

```vb
Matched = Payload.DeviceId.RejectInList(List.TrustedDevices).Against(Payload.CurrencyAmount).MatchGreater(1000)
```

List matching is a whole value match, not a substring or fuzzy match. A Gateway Rule using a List is the cheapest place to stop or route an event, which is also
where Accept lists (allow lists) belong, so that trusted parties skip later processing. See
[Gateway Rules](../../Configuration/Models/GatewayRules/index.html).

## Static Screening: Dictionary Lookups

Where the reference data has a numeric value per key, such as a country risk score, use a Dictionary in place of a List.
A Dictionary pairs a key from the payload with a value, and returns zero if the key is absent, so encode risk so that
zero is the safe default. See [Dictionary](../../Configuration/Models/Dictionaries/index.html).

```vb
Matched = Dictionary.CountryRisk.MatchGreaterOrEqual(4)
```

## Match Screening: Sanctions

A Sanction definition names a payload field holding a *multipart string* (a name of space separated tokens, order
insensitive), a maximum Levenshtein Distance, and Max Distance Ratio and Max Coverage Ratio controls. The engine performs
the fuzzy match against the loaded regulatory lists during the invocation, before rules run, and makes the aggregated
distance available as `Sanction.<Name>`. See [Sanctions](../../Configuration/Sanctions/index.html) and
[Sanction Searching](../../Configuration/Sanctions/SanctionSearching/index.html).

The value is a **distance**, so smaller is closer, and zero is an exact match. Compare it, and do not treat it as a
Boolean:

```vb
Matched = Sanction.PartyName.MatchLessOrEqual(1)
```

A tiered response is two Activation Rules on the same value, each with its own response elevation. An exact match:

```vb
Matched = Sanction.PartyName.MatchIsZero()
```

A near match, using the inclusive `InRange` test:

```vb
Matched = Sanction.PartyName.MatchInRange(1, 2)
```

When there is no match, or the multipart field is absent from the payload, no value is added for the definition, and
reading `Sanction.<Name>` throws. A faulting fragment is logged and does not match, per the methodology, so a rule of
this shape is safe, but it logs an exception for every event with no match. That guarding lives in the Sanctions stage
of the invocation pipeline, not in an extension method, and is covered there. The aggregation over multiple matches is
set on the Sanction definition (Sum, Average, Count, Max, Min, First or Last). See the
[Rule Pipeline Reference](../RulePipelineReference.html) and
`Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/SanctionsExtensions.cs`.

## Match Screening: Fuzzy Matching Against an Internal List

Sanctions cover regulatory lists. For an internal reference set, such as a watch list of names, use the fuzzy List steps,
which compare the payload value with every entry of a List and need no Sanction definition. The default profile folds case,
accents and punctuation, then accepts a Jaro-Winkler or token sort similarity of at least 0.92:

```vb
Matched = Payload.CustomerName.MatchSomehowInList(List.WatchList)
```

A tuned profile is written as an options string, where each option is described in the
[Rule Pipeline Reference](../RulePipelineReference.html). This one tolerates a swapped pair of letters, drops titles, and
skips very short names, where similarity is noisy:

```vb
Matched = Payload.CustomerName.MatchSomehowInListWith(List.WatchList, "damerau=1,soundex,notitles,minlength=4")
```

Single algorithm steps exist for the simple cases (`LevenshteinInList`, `DamerauInList`, `SimilarInList`,
`JaroWinklerInList`, `NormalisedInList`). An invalid options string fails closed, so nothing matches. Every event is
compared with every entry, so for a large List add `firstletter` or `minlength`, and put an exact `MatchInList` first.

## Static and Match Screening: Common Faults

Symptoms of a misbuilt screening rule, with the likely cause.

| Symptom in a screening rule                      | Likely cause                                                                                                     |
|--------------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| A List never matches                             | Case or whitespace differs. Use `InListIgnoreCase`, or normalise the value with `Trim` and `Lower` in the pipeline. |
| The Sanction rule never matches                  | The Distance is too tight; the multipart field is not in the payload; the sanctions lists have not been loaded.  |
| The Sanction rule matches too much               | Distance is too loose for short names. Tighten Max Distance Ratio.                                              |
| A List change is not applied                     | Lists sync in the background, but a Dictionary needs a model synchronisation.                                    |
