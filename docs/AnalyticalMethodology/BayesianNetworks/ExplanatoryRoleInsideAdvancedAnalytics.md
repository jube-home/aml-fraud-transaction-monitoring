---
layout: default
title: Explanatory Role Inside AdvancedAnalytics
nav_order: 8
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Explanatory Role Inside AdvancedAnalytics

## What is real and callable today

One Bayesian-network-shaped capability ships in the product: Tree-Augmented Naive Bayes (TAN). Training runs through
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService`,
invoked as an agent tool by
`Jube.Service.Agent.AdvancedAnalyticsTools.TreeAugmentedNaiveBayesClassifierTrainingRunTools` /
`"SubmitTreeAugmentedNaiveBayesClassifierTraining"`
([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 7). It genuinely runs the same Chow-Liu
maximum-weight-spanning-tree algorithm over class-conditional mutual information that `tree.bayes`
runs in this subsection, fits a Gaussian per predictor conditioned on the class and, where a predictor has one, its tree
parent, and reports a calibrated probability.

`CompareModels` on `Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelSelectionTools`, with
`EvaluationFamily.TreeAugmentedNaiveBayes` selected, cross-validates that classifier against up to eleven other families
on identical folds. That is the whole of what is shipped: one class variable, every other variable a predictor beneath
it, one designated tree structure discovered once per training run, one forward probability per scored row.

## What is not

Everything else worked through in the earlier files of this section — graphical model foundations, constraint-based,
score-based and hybrid structure learning, parameter learning, inference, and network validation — is `bnlearn`-only,
with no product path:

- **Arbitrary structure**, hand-drawn (`model2network`) or learned by general-purpose search (`hc`, `tabu`) — any DAG
  shape over any set of nodes, not one fixed class-plus-predictors tree. Out of scope per
  `docs/AnalyticalMethodology/Reference/ModelCatalogue.md`: "General Bayesian network with structure search... Drop:
  KDB, TAN, AODE and HNB give most of the benefit."

- **Hand-set or learned conditional probability tables over more than a classifier-shaped tree** —
  `custom.fit` for subjective belief tables with no data behind them, or `bn.fit` fitting an arbitrary structure's
  tables from data, both covered in [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md). TAN's training
  service fits its own tables automatically as part of one fixed procedure; there is no tool that fits or accepts a
  table for a standalone node outside that procedure.

- **Multi-node querying** — `cpquery` conditioning on any node and reading off any other node's posterior, in either
  direction ([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 2's
  forward-prediction-and-explanation pair). A trained classifier, TAN included, only ever answers the one forward
  question its training fixed: the class variable's probability given a full row of predictor values. There is no notion
  of picking a different node as the query target, because there are no other queryable nodes — the predictors are
  terminal inputs, not addressable points in a graph.

- **Intervention** — `mutilated()` and the conditioning-versus-`do()` distinction worked through in
  [NetworkValidationAveragingAndCausality.md](NetworkValidationAveragingAndCausality.md). Nothing in
  `AdvancedAnalyticsTools` deletes an arc, fixes a node and re-queries the rest of a graph, because nothing in
  `AdvancedAnalyticsTools` has arcs to delete.

None of this is an oversight so much as a considered scope boundary: the catalogue's own reasoning for excluding general
structure search is that the classification benefit is already covered more cheaply by KDB, TAN, AODE and HNB-style
methods, not that a general network was overlooked.

## How this actually runs today, outside the classifier path

"No product path" above means no native `[AdvancedAnalyticsTool]` — it does not mean there is no way to actually run any
of this. The R script execution job pattern (`Jube.R`, the sandboxed R server, and the `RScriptTools` submit/poll/
result agent tools; see [R Server](../RServer/index.md)) lets an agent submit a genuine `bnlearn` script as a job and
read back its console output: arbitrary structure (`model2network`, `hc`, `tabu`), hand-set or learned CPTs
(`custom.fit`, `bn.fit`), multi-node querying in either direction (`cpquery`), and intervention (`mutilated()`) are all
real, callable operations this way — everything this file lists as "bnlearn-only, with no product path" is `bnlearn`
-only in the sense of having no native tool, not in the sense of being unreachable.

This is the mechanism behind
[Governance, KPIs and Model Validation](../GovernanceKpisAndModelValidation.md), which names Bayesian networks "the
primary probabilistic framework for both anomaly detection and prescriptive analytics" and the specific fallback
methodology for weak or absent labels — unsupervised, probabilistic detection via conditional probabilities over feature
states, calibrated indirectly (posterior-distribution stability, anomaly-score consistency over time, expert review of
high-risk tails) exactly where label-based calibration cannot be used. That is deliberately a batch, expert-reviewed
use, run as a job and read back, not a per-transaction score — there is no live recall path, which this fallback
methodology does not call for.

## Why the material still earns its place

A TAN classifier is a Bayesian network — a small, fixed-shape one, but a real one, with real conditional independence
assumptions baked into its structure. Reasoning about what it is actually saying requires the same vocabulary this
subsection built on general networks, only applied to the one shape that exists in product:

- **Conditional independence.** TAN's entire reason for existing over plain naive Bayes is a conditional independence
  claim: every predictor is independent of every other predictor given the class, *except* for one chosen parent
  predictor per child. Reading a TAN model's reported per-predictor evidence
  ([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 7) without understanding that
  assumption invites over-reading a predictor's isolated contribution as if it captured every interaction it
  participates in, when by construction it only captures its single strongest pairwise one.

- **Markov blankets.** In any Bayesian network, a node's Markov blanket — its parents, its children and its children's
  other parents — is the exact set of other nodes that renders it conditionally independent of everything outside that
  set. In TAN's tree, the class variable's Markov blanket is every predictor (since every predictor is a child of the
  class); a predictor's Markov blanket is the class, its own tree parent (if it has one) and its tree children. That is
  a precise, checkable statement about which of a TAN model's reported predictor contributions can shift if a
  *different, currently unreported* variable changes, and which cannot — the same reasoning `shd()` and the true `asia`
  DAG in [NetworkValidationAveragingAndCausality.md](NetworkValidationAveragingAndCausality.md) depend on to say a
  learned structure is "close" to a real one at all.

- **Structure learning as a claim, not a
  fact.** [NetworkValidationAveragingAndCausality.md](NetworkValidationAveragingAndCausality.md)'s cross-validation and
  bootstrap material makes the point that a learned structure is one noisy estimate, not ground truth — arc presence and
  direction both carry a confidence, and a single hill-climbing run can get an arc's direction wrong even when the arc
  itself is well supported. TAN's Chow-Liu tree is fitted once, deterministically, from one seeded split, with no
  bootstrap or cross-validated-structure step of its own
  ([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 7's implementation notes; see also
  this file's "what is not" list). That does not make its parent-selection wrong, but it means the same caution applies:
  the reported tree is what the Chow-Liu algorithm judged the maximum-weight spanning structure over one sample, not a
  verified causal fact about which predictor genuinely drives another.

- **Calibrated uncertainty.** TAN's raw posterior, like plain naive Bayes, is typically overconfident, which is exactly
  why its training service fits a Platt calibration curve on a held-out split before reporting a probability
  ([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 7's tool description). Recognising
  *why* an uncalibrated Bayesian posterior overstates its own confidence — because a tree-structured product of
  conditional likelihoods compounds every individual conditional's own error — is general Bayesian-network reasoning,
  not something specific to TAN, and it is the same reasoning that explains why `cpquery`'s conditioning answers
  in [NetworkValidationAveragingAndCausality.md](NetworkValidationAveragingAndCausality.md)
  needed 2,000,000 weighted-logic-sampling draws to stabilise rather than being read off a table directly.

- **Conditioning versus intervention.** This is the sharpest transfer. A TAN model's predictors are genuinely just
  conditioning evidence: scoring a row conditions on every predictor's observed value and reads off the class posterior,
  with no claim anywhere that changing one predictor's value in the real world would move the class probability the way
  the model's own sensitivity to that predictor might suggest.

  [NetworkValidationAveragingAndCausality.md](NetworkValidationAveragingAndCausality.md)'s kidney-stone example is not a
  TAN-specific concern, but it is exactly the trap an analyst falls into when they read a strong predictor-to-class
  relationship out of any classifier's evidence — TAN, logistic regression, a random forest's feature importance, all of
  it — and describe it to a decision-maker as "if we changed this, the risk score would fall," which is an
  interventional claim a conditional model was never built to support. Knowing to ask "is there a plausible common cause
  behind both this predictor and the outcome" before making that kind of statement, and knowing that answering it
  properly would need something like a mutilated graph rather than another conditional query, is the concrete,
  transferable payoff of working
  through [NetworkValidationAveragingAndCausality.md](NetworkValidationAveragingAndCausality.md) on a toy network first.

That vocabulary is not confined to TAN. Conditional independence, calibration and the conditioning-versus- intervention
distinction apply equally to general statistical reasoning elsewhere in `AdvancedAnalyticsTools`
— reading a hypothesis test's p-value, a regression coefficient, or a clustering's silhouette score correctly all
benefit from the same discipline about what was actually conditioned on versus what was actually varied.

## The bounded claim

"Explanatory power" here means specifically: the concepts in this subsection transfer to reasoning carefully about the
one Bayesian-shaped model the product ships, and to general statistical reasoning across
`AdvancedAnalyticsTools` more broadly. It does not mean the product can act on an arbitrary learned Bayesian network,
answer a multi-node query, or compute an interventional probability — those remain `bnlearn`-only, demonstrated here for
the understanding they build, not for a recall path that does not exist. An analyst using this material to describe a
candidate causal story to a human decision-maker should say what the data conditionally supports and be explicit about
what would be needed to say more (an intervention, a designed experiment, or a domain-known absence of confounding)
rather than reporting a conditional relationship as if it already answered the causal question.
