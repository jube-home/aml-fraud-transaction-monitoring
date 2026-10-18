---
layout: default
title: Prescriptive Analytics
nav_order: 21
parent: Analytical Methodology
has_children: true
---

# Prescriptive Analytics

Descriptive statistics summarise what happened. Predictive models (the classifiers, regressors, clusterers and anomaly
detectors covered elsewhere in this guide) score what is likely to happen to a given record. Prescriptive analytics asks
a different question: given a model or a network of relationships, what should change, and what follows if it does —
across a whole population of hypothetical cases, not one real record at a time.

## Scope: explanatory, not a recall path

Monte Carlo simulation reuses a model that already has a real `AdvancedAnalytics` recall path (see
`MonteCarloSimulation.md`'s own mapping notes), so it is prescriptive in purpose but runs on production infrastructure
end to end.

The Bayesian network material does not have a *native tool-layer* path. `AdvancedAnalytics` has one shipped
Bayesian-classifier-shaped model, Tree-Augmented Naive Bayes (`AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService`,
see [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md)); it has no general Bayesian network as an
`[AdvancedAnalyticsTool]` — arbitrary structure, hand-set or learned CPTs, multi-node querying and intervention —
because general Bayesian-network structure search is out of scope
(`docs/AnalyticalMethodology/Reference/ModelCatalogue.md`). `BayesianNetworks/` gives explanatory power rather
than a native tool-layer path: the conceptual and mathematical grounding for reasoning about causal structure,
conditional independence and intervention, worked entirely in `bnlearn`, applied on top of the tooling that does
exist (TAN, and the descriptive/inferential tool groups).

The R script execution job pattern (`Jube.R`, the sandboxed R server, plus the `RScriptTools` submit/status/result
agent tools — see [R Server](../RServer/index.md)) is the real way to run this: an agent can submit an arbitrary
`bnlearn` script (structure learning, hand-set or learned CPTs, `cpquery` in either direction, `mutilated()`
intervention) as a job, poll it, and read back the result — exactly the operations `BayesianNetworks/` catalogues as
`bnlearn`-only with no native tool-layer path. [Governance, KPIs and Model Validation](../GovernanceKpisAndModelValidation.md)
carries the full compliance guidance naming Bayesian networks as "the primary probabilistic framework for both
anomaly detection and prescriptive analytics," specifically as the fallback methodology for weak or absent labels:
unsupervised, probabilistic detection via conditional probabilities over feature states, refined by supervised
Bayesian models once reliable labels exist. That is a batch, exploratory, expert-reviewed use — not a per-transaction
recall requirement — and the R script job pattern is a genuine way to run it. See
[Explanatory Role Inside AdvancedAnalytics](../BayesianNetworks/ExplanatoryRoleInsideAdvancedAnalytics.md) for how this
sits alongside TAN. What remains genuinely absent, and is not needed for this fallback-methodology use case, is a
live, per-transaction recall path — that stays a `bnlearn`-only, job-pattern operation, not a real-time one.
