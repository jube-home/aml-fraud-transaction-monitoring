---
layout: default
title: Graph Directed Workflows
parent: Rule Taxonomy
nav_order: 13
---

# Graph Directed Workflows

One workflow per rule taxonomy type, held in code as
`Jube.AskJooby/Workflows/RuleTaxonomyWorkflows.cs`, so that building a rule is a graph an agent walks rather than a page
it has to find, read, and then remember in the right order.

## Why a graph and not a page

A rule in Jube is never one object. A Velocity rule is a TTL Counter, the Activation Rule that increments it and the
Activation Rule that reads it, in that order, with a Priority relationship between the last two. A Cardinality rule is a
Search Key, then an Abstraction Rule, then an Activation Rule. The order is not a convention; the platform enforces it
by refusing anything else, because a fragment naming a counter that does not yet exist will not compile.

That refusal is correct and it is also unhelpful to anyone who did not know the order going in. An agent that authors
the activation half of a rule before the counter it reads exists has no way to tell a correct refusal from a broken
platform, and can mistake a missing counter for a platform fault rather than recognise that the counter it referenced
simply has not been created yet.

Encoding the order as edges removes the judgement. Each node names the artefacts it `Produces` and the artefacts it
`Requires`, so a step whose inputs do not exist is not reachable, and
`RuleTaxonomyWorkflowCoverageTests` fails the build if any edge dangles.

## Why the semantics travel with the workflow

Retrieval is exactly what is missing when it is most needed: a turn's search allowance is shared by every Joobrik in a
fan out, and an agent several levels down can find it already spent, receiving the refusal before a single Bite and
quoting nothing at all as a result.

So each workflow carries its own `Definition`, `Construction`, `Disambiguation` and `Exemplars`, lifted from the
taxonomy's own columns. A request can be matched to a type with nothing loaded but this one file. The coverage test
reads the taxonomy table out of `index.md` and fails if a documented type has no workflow.

## The ordering rules, which are the point

Two types use the same mechanism in opposite directions, and each names getting it backwards as its first fault:

| Type                | Ordering rule                                                        | What going wrong looks like                                                                   |
|---------------------|----------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| Velocity and Volume | The incrementing rule goes **ahead** of the reading rule in Priority | The count excludes the current event, so "the sixth payment in the hour" fires on the seventh |
| Event Sequencing    | The gate is evaluated **before** its own increment                   | The current event counts as its own predecessor and one event satisfies the whole chain       |

Behavioral Baseline inverts Velocity again: where its baseline is a TTL Counter, that counter is incremented *after* the
reading rule, or the current event sits inside the baseline it is being compared against.

Every workflow ends at the same node. Synchronisation is where fragments are compiled, and a fragment that does not
compile saves without complaint and then never matches — so no workflow is finished at the write. The last step re-reads
each object and confirms `Compiled` is set and
`CompileError` is null.

## What each node carries

| Field                   | Purpose                                                                                              |
|-------------------------|------------------------------------------------------------------------------------------------------|
| `Agent`                 | The slug that owns the step, asserted to exist in `AgentDefinitions.All`                             |
| `Tools`                 | Catalogued operation names, asserted to exist in `ServiceToolCatalogue`                              |
| `Produces` / `Requires` | The edges                                                                                            |
| `Hazard`                | What goes wrong if the step is skipped or done out of order, from the type's own Common Faults table |

`Hazard` is on the node rather than left in the documentation because it is wanted at the moment of acting, which is the
moment the documentation may be out of reach.

These are definitions, not a runtime: nothing walks them automatically. A workflow is read by an orchestrator and
followed, which is still a great deal better than inferring the order — the failure this exists to prevent is a failure
of knowledge, not of execution.
