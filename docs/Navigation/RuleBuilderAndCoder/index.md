---
layout: default
title: Rule and Code Builder
nav_order: 8
parent: Navigation
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Rule and Code Builder

Several pages in the platform rely on the construction of code and in most cases this is facilitated by a point and
click construction tool. In most cases the code constricted is of vb.net fragment dialect, unless it is used for
reporting, case management or Exhaustive training, in which case it is an SQL query fragment:

![Image](BuilderRule.png)

In the following example the Rule and Code Constructor is embedded in the Activation Rules page and has a comprehensive
range of data available:

![Image](ActivationRuleBuilder.png)

![Image](AvailableData.png)

There are oftentimes two modes to create rules, being Builder (which is point and click) and Coder (which is the
handcrafting of the vb.net). Clicking on the Coder tab will allow for the VB.Net code fragment to be handcrafted:

![Image](ClickForCoder.png)

The Builder and Coder is available for the creation of VB.Net fragments in:

* Gateway Rules.
* Activation Rules.
* Inline Functions.
* Abstraction Rules.

For reasons of creating SQL fragments and not VB.Net fragments. Only the Builder is available in:

* Case Workflow Filters.
* Cases Search.
* Exhaustive Adaptation Class Definition.

An example of only the builder being available is as follows for Models >> Cases Workflows >> Cases Workflow Filter:

![Image](OnlyBuilderAvailable.png)

Only the Coder is available for the creation of VB.Net fragments in:

* Inline Functions.
* Abstraction Calculations.

An example of only the coder being available is as follows for Models >> References >> Inline Functions:

![Image](OnlyCoderAvailable.png)

The default Coder value will be taken from the Builder, being the VB.Net fragment equivalent:

![Image](DefaultCodeFromBuilder.png)

As typing, the freehand code will be continually parsed for integrity:

![Image](ErrorsParsed.png)

With errors being displayed in the tool tip along the coder guttering. Parsing will happen on the conclusion of each
typing burst, until such time as the rule compiles:

![Image](CompiledRule.png)

To facilitate the development of Coder rules, completions are populated on each keypress, returning model
configurations:

![Image](Completions.png)

Only what the engine would actually run is offered: an item that has been made inactive (a Request XPath, Inline Script,
Inline Function, Dictionary, List, TTL Counter, Sanction, Abstraction Rule, Abstraction Calculation, HTTP Adaptation,
Exhaustive instance or Tag whose Active switch is off) does not appear in the completions, the field lists of the rule
builders, or the fields offered for reporting, and appears again as soon as it is made active. The Active switch of the
model itself does not hide its fields, so that rules can be built for a model that is still a draft.

If the Coder diverges from what was originally created in the Builder, then the Builder tab will be disabled:

![Image](Divergence.png)

Clicking the Reset link will restore the Builder contents to its Coder representation:

![Image](ResetToBuilder.png)

Upon clicking Reset, the Coder will be overwritten to the parsed contents of the Builder, and the tab will become
enabled once again:

![Image](BuilderEnabled.png)