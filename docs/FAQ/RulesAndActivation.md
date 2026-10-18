---
layout: default
title: Rules and Activation
nav_order: 3
parent: FAQ
---

# Rules and Activation

## Rule fundamentals

**Q: What is the difference between abstraction and activation?**

A: Abstraction (feature engineering) builds continuous variables. Activation takes action on them: decline, create a
case, increment a counter, or tick to the Activation Watcher.

**Q: What does an abstraction rule do?**

A: It fetches the history for the search key, filters it to the time window, applies the rule to each historical
transaction, and aggregates. The rule is applied to the history, not the current transaction. The window is silently
shortened by the search key's time-to-live, and collapses to the reference date if the key has no TTL.

**Q: What aggregation functions exist on abstraction rules?**

A: Count, Sum, Distinct Count, Same Count, Min and Max, plus Average, Median, Mode and others. Same Count and Distinct
Count function keys must be fields cached in the payload cache, and Same Count is string-only.

**Q: What is the builder and the coder?**

A: The builder is a no-code point-and-click rule tool with simple AND comparisons. It generates VB.NET in the coder,
which is compiled to native code. Editing beyond what the builder can express turns the builder grey until reset. The
[Rule Taxonomy](../RuleTaxonomy/index.html) rules are written in the coder.

**Q: What happens if a rule fails to compile or errors at runtime?**

A: Each rule row (activation, gateway, abstraction, inline function, inline script) carries a compiled flag and an error
message, and there is a general synchronisation-error table surfaced on the Integrity page. Runtime errors generally
yield "not matched".

## Gateway, elevation and ordering

**Q: What does a gateway rule do?**

A: It filters transactions before heavy processing. It is first-match-wins and ordered by priority. When you control the
integration, it is normally set to match everything.

**Q: What is a response elevation?**

A: The response code and severity. Zero means approved. It acts as a high-water mark: a later matching rule can raise it
but never lower it.

**Q: Why is my response elevation always 0 even though a rule matched?**

A: The gateway rule that matched may have a Max Response Elevation of 0, which caps the output. Raise it above your
rule's elevation value.

**Q: Can one rule depend on another?**

A: Yes. Activation rules run in priority order and each match updates the context, so a later rule can check an earlier
one. There is no drop-out: all activation rules are evaluated.

**Q: Do rule priorities matter?**

A: All activation rules are evaluated regardless; priority only matters for chaining. Priority is consistently a
three-decimal value on gateway, activation, HTTP adaptation, reprocessing and visualisation sources.

## Lists and dictionaries

**Q: How do I handle large lists of values?**

A: Use [Lists](../Configuration/Models/Lists/index.html). Lists belong to the model, synchronise in the background
without any explicit step, and their values are individually approval-gated under Maker Checker (with an "Approve All
Pending Values" shortcut). Each value takes an optional Delete Expiry Date, which acts as a delete when reached. Values
can be uploaded by CSV (add-only), and the Lists page sits in the main menu so the day-to-day maintenance can be
delegated to customer-service roles. For putting a key beyond a rule rather than into its logic, use Overrides instead.

**Q: What are dictionaries for?**

A: Key-value lookups, such as thresholds per type or profile. A common pattern is a composite key (for example
profile|type|currency) built in an inline function, with the dictionary value being a threshold. For rule templates with
business-editable thresholds, dictionaries plus inline functions are the recommended approach. Like lists, dictionaries
are model artefacts: background-synchronised, per-value approval under Maker Checker, optional per-value Delete Expiry
Date, and CSV upload (which, unlike lists, can update existing keys).

## Overrides

**Q: What is suppression, and what are Overrides?**

A: Overrides exclude a key from a rule's normal outcome, or force the outcome, without touching the rule's logic. There
are two kinds: **Suppress** (the rule is treated as not matched for that key) and **Force** (the rule is treated as
matched regardless of its logic). The model-level grid is Suppress-only; Force is opted into per rule via Enable Force
and needs the Apply Force Override permission, and Force wins when overrides conflict. Opt-in is required at both ends:
Enable Override on the Request XPath key and Enable Override on the rule. Forced activations carry a Forced flag in the
archive payload and are counted on a separate Forced Activation Counter per rule. See
[Activation Rule Overrides](../Configuration/Models/ActivationRulesOverride/index.html).

**Q: Do suppressions, lists or overrides expire?**

A: Optionally, yes. Overrides, list values and dictionary values each take an optional Delete Expiry Date, which must be
in the future and behaves as a delete once reached.

**Q: Does suppressing a rule stop its TTL counters incrementing?**

A: No. Suppression affects the rule's outcome for the key; TTL counter increments still happen, so counts stay honest.

**Q: Are overrides exported with Preservation?**

A: No. Overrides are treated as operational production data and are never exported or imported.

## Back-testing and integrity

**Q: How do Rule Back-test and the Integrity page work?**

A: The Integrity page is the observability hub for a model: findings, engine state, dependency analysis and the Pending
Approvals tab with field-level diffs for Maker Checker. Rule back-test replays a candidate rule against history so its
match rate can be inspected before promotion. See
[Model Integrity](../Configuration/Models/ModelIntegrity/index.html).
