---
layout: default
title: Reference
nav_order: 23
parent: Analytical Methodology
has_children: true
---

# Reference

As-built methodology notes that other pages in this section cite as evidence: the product's own numeric data
representation, the Exhaustive search engine's mechanism and known limitations, how its scores are calibrated into
probabilities, the catalogue decisions behind the Bayesian classifier family, and the statistical primitives and
hypothesis tests shared across the product's training and analysis tools. These are code audits and design records,
not R-procedure comparisons, so they sit apart from the rest of this section.

## Tool Catalogue

The pages above describe this product's statistical and model-training methodology conceptually. The semantic tool
layer in `Jube.Service/Agent/AdvancedAnalyticsTools` -- 817 individual `[AdvancedAnalyticsTool("...")]`-attributed
methods an LLM agent calls directly by that literal name -- is a separate, much larger surface that none of the
pages above name tool-by-tool, and which `docs/AgentHierarchy/tool-inventory.md` does not cover at all (that
inventory documents a different, CRUD-style tool surface). The Tool Catalogue pages in this section name every one of those 817 tools
individually: what each computes, its parameters, its return shape, and real edge-case behaviour, cross-referencing
a conceptual page above instead of repeating it wherever one already covers the same tool.
