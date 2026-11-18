---
layout: default
title: Rules and Activation
nav_order: 3
parent: FAQ
---

# Rules and Activation

## Rule fundamentals

**Q: What is the difference between abstraction and activation?**

A: Abstraction (feature engineering) builds continuous variables. Activation takes action on them: decline, create a case, increment a counter, or tick to the Activation Watcher.

**Q: What does an abstraction rule do?**

A: It fetches the history for the search key, filters it to the time window, applies the rule to each historical transaction, and aggregates. The rule is applied to the history, not the current transaction. The window is silently shortened by the search key's time-to-live, and collapses to the reference date if the key has no TTL.

**Q: What aggregation functions exist on abstraction rules?**

A: Count, Sum, Distinct Count, Same Count, Min and Max, plus Average, Median, Mode, Percentile, Since and a good many others, there being over forty in all. Same Count and Distinct Count function keys must be fields cached in the payload cache, and Same Count is string-only. See [Abstraction Rules](../Configuration/Models/AbstractionRules/index.html).

**Q: What is the builder and the coder?**

A: The builder is a no-code point-and-click rule tool, constructing a rule as groups of conditions joined by AND or OR, nested to any depth and negated with NOT. It generates VB.NET in the coder, which is compiled to native code. Editing the code by hand beyond what the builder would write locks the builder, which is dimmed until Reset to builder is clicked, and from that point it is the coder text that the engine runs. The [Rule Taxonomy](../RuleTaxonomy/index.html) rules are written in the coder. See [Rule Builder and Coder](../Navigation/RuleBuilderAndCoder/index.html).

**Q: What happens if a rule fails to compile or errors at runtime?**

A: Each rule (activation, gateway, abstraction, abstraction calculation, inline function, and each inline script in the library) carries a compiled flag and an error message, and a rule the engine could not compile at its last synchronisation is presented as a finding in the section What is wrong of [Model Integrity](../Configuration/Models/ModelIntegrity/index.html). Runtime errors generally yield "not matched".

## Gateway, elevation and ordering

**Q: What does a gateway rule do?**

A: It filters transactions before heavy processing. It is first-match-wins and ordered by priority. When you control the integration, it is normally set to match everything.

**Q: What is a response elevation?**

A: The response code and severity. Zero means approved. It acts as a high-water mark: a later matching rule can raise it but never lower it.

**Q: Why is my response elevation always 0 even though a rule matched?**

A: The gateway rule that matched may have a Max Response Elevation of 0, which caps the output. Raise it above your rule's elevation value. Failing that, look for an override suppressing the rule for the key in question.

**Q: Can one rule depend on another?**

A: Yes. Activation rules run in priority order and each match updates the context, so a later rule can check an earlier one. There is no drop-out: all activation rules are evaluated.

**Q: Do rule priorities matter?**

A: All activation rules are evaluated regardless; priority only matters for chaining. Priority is a decimal number, not less than zero, on gateway, activation, HTTP adaptation, reprocessing and visualisation sources alike, so there is always room to place a rule between two others.

## Lists and dictionaries

**Q: How do I handle large lists of values?**

A: Use [Lists](../Configuration/Models/Lists/index.html). Lists belong to the model, being one of its hub cards, synchronise in the background without any explicit step, and their values are individually approval-gated under Maker Checker (with Approve all pending as a shortcut). Each value takes an optional Delete Expiry Date, which acts as a delete when reached. Values can be loaded in bulk by CSV (add-only), at present through the API only. Lists are no longer an item in the main menu, yet the day-to-day maintenance can still be delegated to customer-service roles, as a role granted only the Read Write List permission is presented with Models and can maintain Lists. For putting a key beyond a rule rather than into its logic, use Overrides instead.

**Q: What are dictionaries for?**

A: Key-value lookups, such as thresholds per type or profile. A common pattern is a composite key (for example profile|type|currency) built in an inline function, with the dictionary value being a threshold. For rule templates with business-editable thresholds, dictionaries plus inline functions are the recommended approach. Like lists, [dictionaries](../Configuration/Models/Dictionaries/index.html) are model artefacts: background-synchronised, per-value approval under Maker Checker, optional per-value Delete Expiry Date, and CSV upload through the API (which, unlike lists, can update existing keys).

## Overrides

**Q: What is suppression, and what are Overrides?**

A: Overrides exclude a key from a rule's normal outcome, or force the outcome, without touching the rule's logic. There are two kinds: **Suppress** (the rule is still evaluated and still matches, but its consequences, being the response elevation, notifications, case creation and the Activation Watcher, are withheld for that key) and **Force** (the rule is treated as matched regardless of its logic). The model-level grid is Suppress-only; Force is opted into per rule via Enable Force, needs the Apply Force Override permission and the approval of a second user, and Force wins when overrides conflict. Opt-in is required at both ends: Enable Override on the Request XPath key and Enable Override on the rule. Forced activations carry a Forced flag in the archive payload and are counted on a separate Forced Activation Counter per rule. Override is an item in the main menu. See [Activation Rule Overrides](../Configuration/Models/ActivationRulesOverride/index.html).

**Q: Do suppressions, lists or overrides expire?**

A: Optionally, yes. Overrides, list values and dictionary values each take an optional Delete Expiry Date, which must be in the future and behaves as a delete once reached.

**Q: Does suppressing a rule stop its TTL counters incrementing?**

A: No. Suppression affects the rule's outcome for the key; TTL counter increments still happen, so counts stay honest.

**Q: Are overrides exported with Preservation?**

A: No. Overrides are treated as operational production data and are never exported or imported.

## Back-testing and integrity

**Q: How do Rule Back-test and Integrity work?**

A: Integrity is the observability hub for a model, opened from the Integrity card of the model: What is wrong (the findings), Is the engine running it (engine state, and synchronisation by hand), What it depends on (the dependencies, drawn as a diagram) and What is waiting for approval (where a checker can approve or reject for the whole model). Backtest, found beneath the form of an Activation Rule along with Dependencies and Integrity for that rule alone, replays a candidate rule against history so its match rate can be inspected before promotion. See [Model Integrity](../Configuration/Models/ModelIntegrity/index.html) and [Backtest](../Configuration/Models/Backtest/index.html).
