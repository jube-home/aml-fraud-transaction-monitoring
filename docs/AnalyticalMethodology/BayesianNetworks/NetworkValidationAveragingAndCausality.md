---
layout: default
title: Network Validation, Averaging and Causality
nav_order: 7
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Network Validation, Averaging and Causality

A structure learned by `hc`, `tabu` or `tree.bayes` is a single point estimate from one dataset. Three questions follow
directly from that: how good is this structure compared to the process that actually generated the data, how sensitive
is it to which rows happened to be sampled, and — the sharpest question — does a probability read off the fitted network
answer "what do we expect to see" or "what would happen if we changed something." These are three different reliability
concerns, not one, and `bnlearn` has a distinct tool for each.

## Structure validation: cross-validation and Structural Hamming Distance

`bn.cv` scores a structure-learning algorithm the same way any other model is scored: fit on k-1 folds, measure loss on
the held-out fold, repeat across folds, average. For a discrete network the natural loss is
`"logl"`, the negative mean per-observation log-likelihood of the held-out rows under the network fitted on the training
folds — lower is better, and unlike a single train/test split it is not sensitive to which rows happened to land in the
one held-out slice.

`bnlearn`'s bundled `asia` dataset (5000 synthetic rows over 8 binary variables — smoking, tuberculosis, lung cancer,
bronchitis and related symptoms) ships with a documented "true" generating DAG, which makes it usable here for a second,
different kind of validation: not "how well does this structure predict held-out rows" but
"how close is this learned structure to the process that actually generated the data." `shd()` (Structural Hamming
Distance) counts the number of arc additions, deletions and reversals needed to turn one DAG into another — the standard
metric for this comparison in the structure-learning literature.

### Procedure: cross-validated loss and distance to a known true structure

```R
library(bnlearn)

data(asia)

true.dag <- model2network("[A][S][T|A][L|S][B|S][E|T:L][D|B:E][X|E]")

set.seed(3)
hc.fit <- hc(asia)
cat(sprintf("Structural Hamming Distance (learned vs true) = %d\n", shd(hc.fit, true.dag)))

set.seed(3)
cv.result <- bn.cv(asia, bn = "hc", loss = "logl", k = 10,
                    fit = "bayes", fit.args = list(iss = 1))
cat("\n10-fold cross-validated log-likelihood loss per fold (Bayes-smoothed fit):\n")
print(loss(cv.result))
cat(sprintf("Mean cross-validated loss = %.4f\n", mean(loss(cv.result))))
```

Console output:

```
Structural Hamming Distance (learned vs true) = 1

10-fold cross-validated log-likelihood loss per fold (Bayes-smoothed fit):
[1] 2.21064
Mean cross-validated loss = 2.2106
```

Hill-climbing recovers all but one arc of the literature's true `asia` DAG (an SHD of 1 out of 8 true arcs — a single
addition, deletion or reversal away), and the same structure's held-out log-likelihood loss across 10 folds averages
2.21, a stable single number rather than one train/test split's outcome. `fit = "bayes"`
with `iss = 1` (an equivalent sample size of 1, spread as a Dirichlet prior over each conditional probability table)
matters mechanically here, not just as a nicety: a plain maximum-likelihood fit (`fit = "mle"`, the default) can assign
an unseen training-fold cell a probability of exactly zero, and if a held-out row then falls into that cell its
log-likelihood is `-Inf`, which silently poisons the fold average to `Inf`. The Bayes-smoothed fit keeps every cell
strictly positive, which is why it is the fit method used for cross- validated loss throughout this module.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelSelectionTools`
/ tool `"CompareModels"` runs stratified k-fold cross-validation and reports ROC AUC, average precision, log loss, Brier
score and F1 per model family — the same cross-validated-loss discipline `bn.cv` applies here, but over the fixed TAN
classifier shape ([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 7) rather than an
arbitrary structure, and with no Structural Hamming Distance concept at all: there is no second structure to compare a
fitted TAN tree against, because the product never hand-specifies or independently sources a "true" structure to measure
distance to.

## Bootstrap model averaging

A structure learned from one sample is exactly that: one sample's answer. `boot.strength` resamples the data with
replacement (the standard bootstrap), re-runs the structure-learning algorithm on each resample, and for every possible
arc records two numbers across all the resamples: **strength**, the fraction of resamples in which that arc (in either
direction) appears at all, and **direction**, the fraction of the resamples containing that arc in which it points the
way shown, given that it appears. `averaged.network`
then keeps only the arcs whose strength clears a threshold (0.5 by default, meaning "present in the majority of
resampled structures") and resolves each one's direction by whichever way had the higher conditional confidence,
producing a single consensus DAG that is far less sensitive to which particular rows were sampled than any one `hc` run.

### Procedure: bootstrapped arc strength and an averaged structure

```R
library(bnlearn)

data(asia)

set.seed(5)
strength <- boot.strength(asia, R = 200, algorithm = "hc")
cat("Arc strength and direction confidence (top 8 by strength):\n")
print(head(strength[order(-strength$strength), ], 8))

averaged <- averaged.network(strength)
cat("\nAveraged network arcs (default 0.5 strength threshold):\n")
print(arcs(averaged))
```

Console output:

```
Arc strength and direction confidence (top 8 by strength):
   from to strength direction
11    S  B        1    0.3700
19    T  E        1    0.7875
26    L  E        1    0.7625
30    B  S        1    0.6300
35    B  D        1    0.7600
38    E  T        1    0.2125
39    E  L        1    0.2375
41    E  X        1    0.7875

Averaged network arcs (default 0.5 strength threshold):
     from to 
[1,] "T"  "E"
[2,] "L"  "S"
[3,] "L"  "E"
[4,] "B"  "S"
[5,] "B"  "D"
[6,] "E"  "X"
[7,] "E"  "D"
```

`boot.strength`'s data frame lists every arc in both directions, which is why `S B` and `B S` both appear at strength 1
with complementary-looking direction numbers (0.37 and 0.63): the arc between S and B is present in every one of the 200
resampled structures, and 63% of the time hill-climbing orients it `B -> S` rather than `S -> B`. `averaged.network`
used that majority direction, which is why the final structure disagrees with the single-run `hc` fit in Procedure 6
of [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md) on that one arc's orientation — exactly the kind
of instability bootstrapping is meant to expose and average over.

**Tool equivalent:** none. There is no bootstrap-resampled structure ensemble anywhere in
`AdvancedAnalyticsTools`, because there is no repeated structure-learning step to resample in the first place — the TAN
training service ([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 7) fits one Chow-Liu
tree once, on one seeded split, and reports it directly with no arc-stability measure attached.

## Conditioning is not intervention

Every `cpquery` call used so far in this module answers a conditional probability: given that a node is observed to be
in some state, what is the distribution of another node. That is an entirely different question from an interventional
one — given that a node is *forced* into some state, overriding whatever would otherwise have determined it, what is the
distribution of another node — and the two can disagree whenever the node being set has a confounder in common with the
node being read off. This is Simpson's paradox stated as a graph problem: a variable `Z` that influences both the node
being set (`X`) and the node being read (`Y`) creates a non-causal path between them (`X <- Z -> Y`) that plain
conditioning cannot tell apart from the causal path (`X -> Y`) it is actually interested in.

`bnlearn`'s `mutilated()` function makes the distinction mechanical rather than philosophical: it takes a fitted network
and a named list of nodes to fix, deletes every arc *into* each fixed node (severing it from whatever used to determine
it, confounder included) and replaces its distribution with a point mass on the given value. Querying the resulting
graph is Pearl's `do(X = x)` operator; querying the original graph with
`X = x` supplied as ordinary evidence is plain conditioning. The two coincide only when the intervened node has no
parents to begin with (see [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 8, where `S`
in `asia` is a root cause and the two notions collapse into each other) — in every other case they are different graphs
and, as below, different numbers.

### Procedure: conditioning and intervention on a confounded network

The scenario reconstructs a well-known real confounding pattern (kidney stone treatment outcomes, Charig et al. 1986,
popularised as a Simpson's-paradox teaching example): `Z` is stone size (`Small`/`Large`), `X` is which of two
treatments was given (`A`/`B`), and `Y` is treatment success. Doctors preferentially gave treatment `A` to the harder,
large-stone cases (`Z -> X`), and stone size independently affects the chance of success regardless of treatment
(`Z -> Y`), alongside treatment's own effect (`X -> Y`) — exactly the
`X <- Z -> Y` confounding shape described above. The conditional probability tables below are the real published success
rates for each stone-size/treatment combination.

```R
library(bnlearn)

dag <- model2network("[Z][X|Z][Y|X:Z]")

z.cpt <- matrix(c(0.51, 0.49), ncol = 2, dimnames = list(NULL, c("Small", "Large")))

x.cpt <- matrix(
  c(0.2437, 0.7563,
    0.7668, 0.2332),
  ncol = 2,
  dimnames = list(X = c("A", "B"), Z = c("Small", "Large")))

y.cpt <- array(
  c(0.9310, 0.0690,
    0.8667, 0.1333,
    0.7300, 0.2700,
    0.6875, 0.3125),
  dim = c(2, 2, 2),
  dimnames = list(Y = c("Yes", "No"), X = c("A", "B"), Z = c("Small", "Large")))

fitted <- custom.fit(dag, dist = list(Z = z.cpt, X = x.cpt, Y = y.cpt))

set.seed(9)
p.y.given.a <- cpquery(fitted, event = (Y == "Yes"), evidence = (X == "A"), n = 2e6)
set.seed(9)
p.y.given.b <- cpquery(fitted, event = (Y == "Yes"), evidence = (X == "B"), n = 2e6)

cat("Conditioning (observational), P(Y = Yes | X = x):\n")
cat(sprintf("  P(Y=Yes | X=A) = %.4f\n", p.y.given.a))
cat(sprintf("  P(Y=Yes | X=B) = %.4f\n", p.y.given.b))

mutilated.a <- mutilated(fitted, list(X = "A"))
mutilated.b <- mutilated(fitted, list(X = "B"))

set.seed(9)
p.y.do.a <- cpquery(mutilated.a, event = (Y == "Yes"), evidence = TRUE, n = 2e6)
set.seed(9)
p.y.do.b <- cpquery(mutilated.b, event = (Y == "Yes"), evidence = TRUE, n = 2e6)

cat("\nIntervention (mutilated graph), P(Y = Yes | do(X = x)):\n")
cat(sprintf("  P(Y=Yes | do(X=A)) = %.4f\n", p.y.do.a))
cat(sprintf("  P(Y=Yes | do(X=B)) = %.4f\n", p.y.do.b))
```

Console output:

```
Conditioning (observational), P(Y = Yes | X = x):
  P(Y=Yes | X=A) = 0.7793
  P(Y=Yes | X=B) = 0.8259

Intervention (mutilated graph), P(Y = Yes | do(X = x)):
  P(Y=Yes | do(X=A)) = 0.8325
  P(Y=Yes | do(X=B)) = 0.7789
```

The two questions disagree, and disagree in direction, not just in magnitude. Conditioning says treatment
`B` looks better (0.8259 against 0.7793): it recovers the raw, confounded pooled success rates, because observing
`X = A` is also indirect evidence that the case was more likely a hard, large stone (since `Z`
drives both `X` and `Y`), which drags `A`'s observed success rate down. Intervening says the opposite —
`A` is actually better (0.8325 against 0.7789) — because `mutilated()` deleted the `Z -> X` arc before querying, so the
query no longer lets an intervened `X` leak any information back about `Z`; it correctly averages each treatment's real
effect over the same, unbiased `Z` distribution (51% small, 49% large) instead of over the sicker population that
treatment `A` actually happened to have been given to in the data.

This is exactly why the interventional question needs the mutilated graph and cannot be answered by conditioning on the
original one: conditioning on `X` picks up the backdoor path through `Z` along with the causal path through `X` itself,
and there is no way to tell the two apart from `cpquery` evidence alone — only from deleting the confounding arc first.

**Tool equivalent:** none. There is no `do`-operator, no mutilated-graph construction and no arc-deletion
primitive anywhere in `AdvancedAnalyticsTools`; every trained model, TAN included, is a fixed function from a row of
predictor values to one class probability, with no internal graph a caller could cut an arc out of even in principle.
Recognising when a candidate cause and effect share a confounder — and therefore when a plain conditional relationship
in a payload or an abstraction is not evidence of what would happen if a value were forced to change — is a piece of
reasoning an analyst has to bring to the product's output themselves;
see [ExplanatoryRoleInsideAdvancedAnalytics.md](ExplanatoryRoleInsideAdvancedAnalytics.md).
