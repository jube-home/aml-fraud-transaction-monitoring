---
layout: default
title: Rule Extension Tools
parent: Rule Taxonomy
nav_order: 15
---

# RuleExtensionTools: searching and validating the rule language's extension-method surface

`RuleExtensionTools` (`Jube.Service/Agent/RuleExtensionTools/RuleExtensionTools.cs`) gives the rule-authoring and
test-bench agents three methods for working with Jube's rule-language extension methods -- the Visual Basic-callable
methods a rule fragment chains off a payload field or an abstraction (`Payload.Narrative.RemoveDiacritics()`,
`Abstraction.AverageAmount.IsJustBelowThreshold(...)`, and the like). The surface these methods search is large -- the
tool's own description puts it at "over a thousand methods" -- and a rule fragment that calls one with a guessed
argument list "compiles into nothing," so these three tools exist specifically to let an agent confirm a method's real
signature before it ever reaches a rule.

## Where the catalogue comes from

`RuleExtensionCatalogue.All` (`Jube.Service/Agent/RuleExtensionTools/RuleExtensionCatalogue.cs`) is built by reflection,
not maintained as a hand-written list, from two sources:

* **Every public static method on `Jube.Dictionary.Extensions.Extensions`** -- the compiled rule-DSL extension method
  library; the reflected set the tool actually searches is the method's own live count at the time
  `RuleExtensionCatalogue.All` is built, read straight off the compiled assembly rather than off any separate list that
  could drift from it.
* **Dynamically discovered evaluations** (`DiscoverDynamicEvaluations`) -- extension surface resolved at runtime rather
  than purely by static reflection over one fixed type.

Each discovered method becomes one `RuleExtensionSignature` (its name, the VB-visible instance type it is called on, and
its return type); an overloaded method name appears once per overload, each with its own signature, which is exactly
what lets `RuleExtensionMethodGet` return "every overload of one named method" rather than just one.

## The three methods

* **`RuleExtensionMethodSearch(query = "", instanceType = "", maximumResults = 60)`** -- free-text search over the
  catalogue by name: `query` is split into words, and every word must appear somewhere in a method's name (in any order,
  case-insensitive) for it to match -- the tool's own description gives `"fuzzy"`, `"iban"`, `"round"` and
  `"business hours"` as example queries. `instanceType` narrows to methods callable on one Visual Basic type (`String`,
  `Double`, `Integer`, `DateTime`, `Boolean`, ...) -- "the type of whatever sits to the left of the dot at that point in
  the pipeline." Leaving both empty returns the instance type's entire offered surface, up to
  `maximumResults` (clamped to the range 1-400).
* **`RuleExtensionMethodGet(name)`** -- every overload of one exact method name, each with its own Visual Basic
  signature, the type it is called on, and the type it returns. The tool's own description frames this as the step to
  confirm an exact argument list before writing the call, once `RuleExtensionMethodSearch` has narrowed down which
  method is wanted.
* **`RuleExtensionMethodValidate(expression, receiverType = "String")`** -- checks a whole chained fragment (for example
  `Payload.Narrative.RemoveDiacritics().ContainsAnyIgnoreCase("crypto","wallet")`) against the real method surface
  *before it goes anywhere near a rule*: `RuleExtensionChainValidator.Validate` splits the expression into its chained
  segments, tracks the VB type the expression holds at each link (starting from `receiverType` -- the type of the
  payload field or abstraction on the left, `String` unless the value is numeric, a date, or a flag), and resolves each
  link against the catalogue in turn. A leading path segment that is not itself a catalogued method name
  (`Payload.Narrative`, `Abstraction.AverageAmount`) is treated as the receiver being operated on, not as a failed
  lookup. The result (`RuleExtensionValidationResult`: `Valid`, the original `Expression` and
  `Receiver`, the chain's final `ResultType`, a `Steps` list recording what each link resolved to, and a
  `Problems` list of anything that did not) names exactly which link in the chain broke and what type the expression
  held at that point -- the tool's own description calls this "free," "exact," and something "a whole-rule validation
  cannot do," because a whole-rule compile failure reports where the rule failed to compile, not which link of one
  chained expression inside it was the cause.

## Why this exists as a separate tool family from whole-rule validation

The tool's own description for `RuleExtensionMethodSearch` states the cost this family exists to avoid directly: a
guessed argument list for a method out of over a thousand "compiles into nothing." `RuleExtensionMethodValidate`'s
description goes further, calling validations in general "gold" and this one specifically free, exact, and able to
localise a failure to one link of a chain -- which a rule's own whole-expression compiler, by the nature of reporting a
single compile error for the whole rule, cannot do as precisely. Searching and validating a fragment against this
catalogue before it is pasted into an actual rule turns a failure that would otherwise only surface once the whole rule
is compiled (or, worse, once it runs against real data) into one resolved before the rule is touched at all.

## Where this is wired

Unlike the AskJooby chat tools documented elsewhere in `docs/AskJooby/`, `RuleExtensionTools` is not wired through
`Jube.AskJooby/Agents/Basic/AgentDefinitions.cs` at all -- it is bound by name, through a small reflection-based binder
(`RuleExtensionToolBinder.Bind`, `Jube.Service/Agent/RuleExtensionTools/RuleExtensionToolBinder.cs`), to the specific
set of agents named in `RuleExtensionAgentToolNames.Agents`
(`Jube.Service/Agent/RuleExtensionTools/RuleExtensionAgentToolNames.cs`): the Author and Test Bench specialists for
Activation Rules (and their Override variant), Abstraction Rules, Abstraction Calculations, Gateway Rules, Inline
Functions, Inline Scripts, and Reprocessing Rules -- the full set of rule-authoring surfaces in
`docs/Configuration/Models` and `docs/Configuration/Reprocessing` whose rule fragments can chain one of these extension
methods. `AskJoobyConversationService` calls this binder once per agent in that list, alongside the equivalent binders
for the Advanced Analytics tool families (`docs/AnalyticalMethodology/Reference/ToolCatalogue/`), and merges the result
into whatever tools that agent already holds rather than replacing them.

## Cross-reference

* `docs/RuleTaxonomy/RuleAuthoringMethodology.md` -- the rule fragments these three tools exist to validate before they
  are written into a real rule.
* `Jube.Dictionary/Extensions/` -- the compiled extension-method library the catalogue's static half reflects over.
