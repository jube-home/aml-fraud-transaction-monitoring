---
layout: default
title: Rule Taxonomy
nav_order: 6
has_children: true
---

# Rule Taxonomy

While it is commonplace for consultants to turn up with a list of best practice rules this a disigenious practice as the
corpus of rules is universally subject to drift. Typically they are lifted directly from clients over a decades long
process and bare little relation to what is really going on in the data, which begs some uncomfortable questions on
intelectual property and provenence also. It is very important that an existing rule corpus is not used, except in the
absence of data with clear intelectual property origination and provenence, and even then, sparingly while data builds
up. Instead, rules should be crafted based on the actual data and typically conform to a generalised common rule
taxonmy. This documentation lays out the supported rule taxonomoes in Jube and scaffolds reference impliementations in
Jube. While reference names, filtering and threshold might change the taxonomy won't - these are the supported
taxononies - and rule creation can be trivially digested and modified on a generalised basis. For the avoidance of
doubt, departures from documented Rule Taxonomy, notwithstdaning Jubes flexibility, should be considered unsupported. We
need to keep things sane, especially in the case where this documenttion is expressed to AskJooby via RAG Chunks (so
called, Pasture Bites),  which is its natural expression in this day and age.

The primary Rule Taxonomy is as follows:

| # | Rule Taxonomy Type                    | Generalized Definition                                                                                       | Generalized Construction                                                                                                                                                                | Disambiguation                                                                                                                               |
|---|---------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | **Velocity & Volume**                 | Raw frequency/magnitude of events.                                                                           | TTL Counter, windowed, keyed on the entity.                                                                                                                                             | Use when *how much/how often* is the signal itself.                                                                                          |
| 2 | **Cardinality (Same & Different)**    | How many distinct — or how many identical — values an entity has produced for an attribute.                  | Abstraction Rule, Search Key = the entity, Function = Same Count (12) or Distinct Count (2).                                                                                            | Use when *variety*, not volume, is the signal.                                                                                               |
| 3 | **Link & Network**                    | Same cardinality question, but relational — multiple entities converging on one shared attribute.            | Identical Abstraction Rule config to #2 — invert the Search Key to the *shared* trait (device, account) instead of the primary entity.                                                  | Use when the risk is relational rather than about one entity's own behaviour.                                                                |
| 4 | **Behavioral Baseline & Ratio**       | Is this normal for this entity, or is this proportion right?                                                 | Abstraction Rule Function = Average/Median/StdDev/Sum/Max/Min over a Search Interval; optionally two Abstraction reads combined via Calculation in Activation Rule before thresholding. | Use for self-referential deviation or any proportion-of-two-counters question — same mechanism either way.                                   |
| 5 | **Event Sequencing**                  | A chain of states/events matters only together, not individually — within one model or across linked models. | Within-model: `Dictionary`/KVP holds last state, compared to current `Payload`. Cross-model: a TTL Counter incremented in one model, read in a linked model via a common key.           | Covers lifecycle chains, channel-hopping, and takeover chains alike — these are domain dressing on the same mechanism, not separate classes. |
| 6 | **Static & Match Screening**          | Deterministic/fuzzy match against a known reference set.                                                     | `List` for internal reference data; `Sanction` for regulatory lists (returns a value via `GetValueOrThrow` — compare it, it's not boolean).                                             | Covers device blacklists too — no separate class needed.                                                                                     |
| 7 | **Unstructured Text Screening**       | The signal is hidden in free text, not a structured field.                                                   | `String` extensions (`Contains`/`ContainsAny`/`ContainsAll`) against a `Payload` text field, checked against a `List` of terms.                                                         | Use when a structured check (#6) wouldn't see it.                                                                                            |
| 8 | **Geospatial & Context**              | Physical distance, travel, or boundary.                                                                      | `Double` geospatial extensions directly on `Payload` coordinates for a single comparison; aggregated via Abstraction Rule when comparing against a longer-term pattern.                 | Use for anything location-based.                                                                                                             |
| 9 | **Structuring / Threshold Avoidance** | Values engineered to dodge a known limit, repeated.                                                          | `Double` threshold extensions (`IsJustBelowThreshold`/`IsRoundAmount`) combined with #1 or #2 for the repetition.                                                                       | Use when the risk is evasion of a specific numeric control, not generic anomaly (that's #4).                                                 |

**Cross-cutting, not a detection class:** Identity & Demographic — derived continuous variables (age, tenure) via
`Payload`/`DateTime` extensions, consumed as an *input* to any of the above rather than a pattern in its own right.

**Cross-cutting, not a detection class:** Reference Data — `List` (string membership) and `Dictionary` (a numeric value
looked up by a payload key) hold maintained reference data outside the rule, and are consumed as an *input* to any of
the above, most prominently #6 and #7, but equally as a segment, exemption or score in #1 to #5 and #8 to #9. See
[Reference Data: Lists and Dictionaries](RuleAuthoringMethodology.html#reference-data-lists-and-dictionaries).

**Cross-cutting, not a detection class:** Generalisation, Adaptation and Model Thresholds — a model score (Exhaustive,
HTTP Adaptation or AskJooby Analytics) is an *input* to an Activation Rule, thresholded like any other continuous value,
and its inputs are the counters, aggregates and ratios (Abstraction Calculations) of #1 to #9. See
[Generalisation, Adaptation and Model Thresholds](GeneralisationAdaptationAndModelThresholds.html).

**Authoring note:** this taxonomy ignores the point-and-click Builder entirely which exists only to assemble VB.NET and
is a uder interace concept more than a taxonmy concept — every rule is expressed as a hand-written VB.Net Coder
fragment. Model validation endpoints exist to not only validate the models, including several validations that include
the compilation of the rule fragment, but also test endpoints to facilitate the stress testing of a given rule fragment.
These are always adjacent to the other functions in the service layer domain (e.g. EntityAnalysisModelActivationRule).