---
layout: default
title: Bayesian Networks
nav_order: 19
parent: Analytical Methodology
has_children: true
---

# Bayesian Networks

A Bayesian network is two things bolted together: a directed acyclic graph (DAG) over a set of variables, and a joint
probability distribution over those variables that factorises exactly along the graph's arcs — each variable's local
distribution depends only on its own parents in the graph, not on every other variable directly. The graph is a compact
statement of conditional independence; the joint distribution is what you get by multiplying every node's local
(conditional) distribution together. Nothing here is specific to any one estimation method — the same
DAG-plus-factorisation idea underlies a network built entirely from hand-entered belief, one fitted from data by
counting or regression, and one whose structure itself is learned from data.

## Why this matters for prescriptive work specifically

A trained classifier or regressor — the kind covered everywhere else in this guide — answers one question:
given a row of predictor values, what is the outcome. It has an input side and an output side, and nothing in between
that can itself be queried. A Bayesian network's nodes are not split into predictors and one outcome; any node can be
conditioned on (evidence set) and any other node's distribution read off in response, in either direction along the
graph. That is what makes the network useful for a different class of question — not "is this case fraud" but "if this
population's behaviour changed in one place, what happens elsewhere in the network, and by how much" — the explanatory
and what-if reasoning that gives this subsection its place under Prescriptive Analytics rather than under the predictive
model families.

See [Prescriptive Analytics](../PrescriptiveAnalytics/index.md) for the scope boundary this whole subsection sits
inside: `AdvancedAnalytics` has no native, per-transaction Bayesian-network recall path; the R script job pattern makes
the batch, expert-reviewed case genuinely runnable instead (see
[Explanatory Role Inside AdvancedAnalytics](ExplanatoryRoleInsideAdvancedAnalytics.md)). Tree-Augmented Naive Bayes,
covered in [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), is the one shipped model that genuinely
overlaps this material, and is cross-referenced rather than repeated wherever the overlap is real. Everything in this
subsection is worked in `bnlearn`, the R package Marco Scutari wrote and maintains, following the methodology of his and
Radhakrishnan Nagarajan's, Marco Scutari's and Sophie Lèbre's books on the subject (principally *Bayesian Networks with
R* by Scutari and Denis) and the package's own documentation.
