---
layout: default
title: Exact and Approximate Inference
nav_order: 6
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Exact and Approximate Inference

A fitted network (see [ParameterLearningAndCPTs.md](ParameterLearningAndCPTs.md)) is a compact factorisation of a full
joint distribution over every node. Querying it means computing `P(event | evidence)` — the probability of some node's
state given other nodes are fixed — without ever materialising that full joint table, which grows exponentially with the
number of nodes. `bnlearn` splits this into two genuinely different kinds of answer: exact inference, computed once via
junction-tree propagation and correct up to floating-point precision; and approximate inference, which estimates the
same quantity by simulating from the fitted network, carrying a sampling error controlled only by how many particles are
drawn.

`bnlearn` does not implement junction-tree propagation itself — its own inference functions, `cpquery` and
`cpdist`, are both simulation-based and therefore approximate by construction. Exact inference over a
`bnlearn`-fitted network goes through the companion package `gRain` (Højsgaard, part of the `gRbase`
ecosystem), via `bnlearn::as.grain`, which converts a fitted `bn.fit` object into a `gRain` network with no re-fitting
involved. Installing `gRain` requires `BiocManager::install(c("graph", "RBGL"))` for its two Bioconductor-hosted
graph-theory dependencies, then `install.packages(c("gRbase", "gRain"))`.

## Procedure 1: Fit a network to query

The same structure-then-parameters pipeline as the rest of this module — `hc` for structure, `bn.fit` with a Dirichlet
prior for parameters — fitted once here on the bundled `asia` dataset and reused by every procedure below, so that the
exact and approximate answers later in this file are answers to the same query on the same fitted network.

```R
library(bnlearn)
data(asia)

dag <- hc(asia)
fitted <- bn.fit(dag, asia, method = "bayes", iss = 1)

cat("Learned structure arcs:\n")
print(arcs(dag))
cat("\nParents of D (dyspnoea): "); print(parents(dag, "D"))
cat("Parents of T (tuberculosis): "); print(parents(dag, "T"))
cat("\nMarginal P(T = yes) in the data: ", mean(asia$T == "yes"), "\n")
```

Console output:

```
Learned structure arcs:
     from to 
[1,] "B"  "D"
[2,] "L"  "E"
[3,] "E"  "X"
[4,] "S"  "B"
[5,] "T"  "E"
[6,] "E"  "D"
[7,] "S"  "L"

Parents of D (dyspnoea): [1] "B" "E"
Parents of T (tuberculosis): character(0)

Marginal P(T = yes) in the data:  0.0088
```

`T` (tuberculosis) has no parents in this learned structure and is rare in the data (0.88% of rows) — a genuinely
low-probability piece of evidence, exactly the case the rest of this file uses to show why the choice between
approximate methods matters, not just the choice between exact and approximate. The query used throughout is
`P(D = yes | T = yes)` — dyspnoea given tuberculosis, reached in this structure via
`T -> E -> D`.

**Tool equivalent:** none — see the closing note below; the structure- and parameter-learning steps
themselves are covered in [ParameterLearningAndCPTs.md](ParameterLearningAndCPTs.md) and, for the one classifier-shaped
overlap that exists, in [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md).

## Procedure 2: Exact inference via junction-tree propagation

Junction-tree propagation (Lauritzen and Spiegelhalter's algorithm, the method `gRain` implements and the one described
at length in Scutari and Denis, *Bayesian Networks with R*) turns the DAG into an undirected tree of node clusters —
moralise the DAG (marry any two nodes sharing a child, then drop arc direction), triangulate the result so every cycle
of length four or more has a chord, group the triangulated graph's maximal cliques into a tree of clusters connected by
shared-variable separators, then pass messages between neighbouring clusters until every cluster's local table agrees
with every other on their shared variables. Once that message-passing has converged, any single node's (or small node
set's) marginal or conditional distribution can be read directly off whichever cluster contains it, with evidence
entered by multiplying a 0/1 indicator into that node's table before message-passing runs. The propagation is exact: no
sampling is involved anywhere in it, so the only error in the final answer is ordinary floating-point rounding.

`bnlearn::as.grain` converts an already-fitted network straight into this representation with no re-fitting;
`gRain::compile` builds the junction tree and runs the initial round of propagation; `gRain::setEvidence`
absorbs an observed node value into the compiled tree; `gRain::querygrain` reads off the requested node's posterior
distribution.

```R
library(bnlearn)
library(gRain)

data(asia)
dag <- hc(asia)
fitted <- bn.fit(dag, asia, method = "bayes", iss = 1)

junction <- as.grain(fitted)
cat("class(junction): ", paste(class(junction), collapse = ", "), "\n")

junction.compiled <- compile(junction)
junction.evidence <- setEvidence(junction.compiled, evidence = list(T = "yes"))

result <- querygrain(junction.evidence, nodes = "D")
cat("\nExact P(D | T = yes) via junction-tree propagation (querygrain):\n")
print(result)

exact.p <- result$D["yes"]
cat(sprintf("\nExact P(D = yes | T = yes) = %.6f\n", exact.p))
```

Console output:

```
Loading required package: gRbase

Attaching package: ‘gRbase’

The following objects are masked from ‘package:bnlearn’:

    ancestors, children, nodes, parents

class(junction):  cpt_grain, grain 

Exact P(D | T = yes) via junction-tree propagation (querygrain):
$D
D
       no       yes 
0.2122375 0.7877625 


Exact P(D = yes | T = yes) = 0.787762
```

`0.787762` is now the ground truth the rest of this file's simulation-based estimates are checked against — computed
once, deterministically, with no particle count to tune and no random seed involved anywhere in the computation.

**Tool equivalent:** none. No shipped tool builds a junction tree or runs exact probabilistic propagation
over an arbitrary graph of variables — see the closing note below.

## Procedure 3: Approximate inference — logic sampling versus likelihood weighting

`cpquery` estimates the same kind of conditional probability by simulation, using one of two methods.
`method = "ls"` (logic sampling, the default) forward-samples complete rows from the network's generative process, keeps
only the rows that happen to match the evidence exactly, and reports the fraction of kept rows for which the event
holds — plain rejection sampling. `method = "lw"` (likelihood weighting) instead forces every simulated row's evidence
nodes to the observed value directly, and compensates by weighting each row by how probable that forced value actually
was given its own simulated parents, so no row is ever discarded.

The difference between the two is invisible when evidence is common, and stark when it is not: with `T`
observed in only 0.88% of real rows, logic sampling has to reject on the order of 99% of every simulated batch before it
has any evidence-matching rows left to average, while likelihood weighting keeps and correctly re-weights all of them.
Five independent replications (different seeds) at each of four particle counts show exactly this gap, checked here
against Procedure 2's exact value of 0.787762.

```R
library(bnlearn)
data(asia)

dag <- hc(asia)
fitted <- bn.fit(dag, asia, method = "bayes", iss = 1)

run.reps <- function(method, n, reps = 5) {
  vals <- numeric(reps)
  for (i in seq_len(reps)) {
    set.seed(100 + i)
    if (method == "lw") {
      vals[i] <- cpquery(fitted, event = (D == "yes"), evidence = list(T = "yes"), method = "lw", n = n)
    } else {
      vals[i] <- cpquery(fitted, event = (D == "yes"), evidence = (T == "yes"), method = "ls", n = n)
    }
  }
  vals
}

for (n in c(200, 1000, 10000, 1e5)) {
  ls.vals <- run.reps("ls", n)
  lw.vals <- run.reps("lw", n)
  cat(sprintf("n = %d\n", n))
  cat("  logic sampling (ls) estimates:          ", paste(sprintf("%.4f", ls.vals), collapse = ", "), "\n")
  cat(sprintf("  logic sampling (ls) mean, sd:            %.4f, %.4f\n", mean(ls.vals, na.rm = TRUE), sd(ls.vals[!is.na(ls.vals)])))
  cat("  likelihood weighting (lw) estimates:    ", paste(sprintf("%.4f", lw.vals), collapse = ", "), "\n")
  cat(sprintf("  likelihood weighting (lw) mean, sd:      %.4f, %.4f\n\n", mean(lw.vals, na.rm = TRUE), sd(lw.vals[!is.na(lw.vals)])))
}
```

Console output:

```
n = 200
  logic sampling (ls) estimates:           0.0000, 1.0000, 0.0000, 0.0000, 1.0000 
  logic sampling (ls) mean, sd:            0.4000, 0.5477
  likelihood weighting (lw) estimates:     0.8050, 0.8100, 0.7400, 0.7850, 0.7450 
  likelihood weighting (lw) mean, sd:      0.7770, 0.0329

n = 1000
  logic sampling (ls) estimates:           0.8333, 0.9231, 0.7000, 0.9000, 1.0000 
  logic sampling (ls) mean, sd:            0.8713, 0.1128
  likelihood weighting (lw) estimates:     0.7860, 0.7850, 0.7890, 0.7920, 0.8050 
  likelihood weighting (lw) mean, sd:      0.7914, 0.0081

n = 10000
  logic sampling (ls) estimates:           0.7624, 0.7500, 0.8228, 0.8515, 0.7363 
  logic sampling (ls) mean, sd:            0.7846, 0.0499
  likelihood weighting (lw) estimates:     0.7904, 0.7873, 0.7901, 0.7821, 0.7792 
  likelihood weighting (lw) mean, sd:      0.7858, 0.0050

n = 100000
  logic sampling (ls) estimates:           0.7770, 0.7722, 0.7825, 0.8267, 0.7800 
  logic sampling (ls) mean, sd:            0.7877, 0.0222
  likelihood weighting (lw) estimates:     0.7882, 0.7883, 0.7896, 0.7865, 0.7882 
  likelihood weighting (lw) mean, sd:      0.7882, 0.0011
```

At `n = 200`, logic sampling is not merely noisy, it is unusable: only one or two simulated rows out of 200 survive
rejection, so each replication's estimate is either exactly 0 or exactly 1 depending on the single surviving row's
outcome (mean 0.40, standard deviation 0.55 across five replications — a coin flip, not an estimate). Likelihood
weighting at the same `n = 200` already has a standard deviation of 0.033 around a mean within 0.011 of the exact value.
Both methods converge towards 0.787762 as `n` grows, but likelihood weighting's standard deviation is smaller than logic
sampling's at every particle count tested here — by roughly seventeen-fold at `n = 200`, falling to twofold by `n = 1e5`
as logic sampling finally accumulates enough accepted rows (on the order of 880 out of 100,000) to stop being dominated
by rejection noise. This is the general reason likelihood weighting is preferred whenever evidence sits on a
low-probability state: it never discards a simulated row, so its accuracy is governed by `n` alone, while logic
sampling's accuracy is governed by `n × P(evidence)` — the number of rows it actually keeps — which collapses for rare
evidence unless `n` is raised to compensate.

**Tool equivalent:** none. No shipped tool draws forward samples from a fitted joint distribution over a
graph of variables, with or without importance weighting — see the closing note below.

## Procedure 4: Sampling a node's distribution as particles — `cpdist`

`cpquery` answers one probability at a time. `cpdist` instead returns the simulated particles themselves — a data frame
of draws from one or more nodes' conditional distribution given evidence — for further summarising, plotting, or feeding
into some other calculation, rather than collapsing straight to a single number. The distinction between the two
sampling methods shows up directly in the shape of what `cpdist`
hands back: logic sampling returns however many rows survived rejection (which can be far fewer than requested), with no
weights attached, since every surviving row is by construction an equally-valid sample from the evidence-conditioned
distribution; likelihood weighting always returns exactly the requested number of rows, but they are only valid once
each row's `"weights"` attribute is taken into account.

The query here moves to `E` (has parents `T` and `L` in this structure, so its likelihood weights genuinely vary from
row to row rather than degenerating to a constant) as evidence, with `X` as the sampled node — a 20,000-request budget
is not used; 5,000 is enough to show both effects clearly.

```R
library(bnlearn)
data(asia)

dag <- hc(asia)
fitted <- bn.fit(dag, asia, method = "bayes", iss = 1)

cat("Parents of E in the learned structure: ")
print(parents(dag, "E"))

set.seed(5)
particles.ls <- cpdist(fitted, nodes = "X", evidence = (E == "yes"), method = "ls", n = 5000)
cat("\ncpdist, method = 'ls' (logic sampling / rejection):\n")
cat("rows returned (after rejection, requested n = 5000): ", nrow(particles.ls), "\n")
cat("has 'weights' attribute: ", !is.null(attr(particles.ls, "weights")), "\n")
print(prop.table(table(particles.ls$X)))

set.seed(5)
particles.lw <- cpdist(fitted, nodes = "X", evidence = list(E = "yes"), method = "lw", n = 5000)
cat("\ncpdist, method = 'lw' (likelihood weighting):\n")
cat("rows returned (n = 5000 requested): ", nrow(particles.lw), "\n")
w <- attr(particles.lw, "weights")
cat("has 'weights' attribute: ", !is.null(w), "\n")
cat("distinct weight values: ", length(unique(w)), "\n")
print(summary(w))
cat("weighted P(X = yes | E = yes) from these particles: ",
    sum(w[particles.lw$X == "yes"]) / sum(w), "\n")
cat("unweighted (naive, ignores weights) P(X = yes | E = yes) from same particles: ",
    mean(particles.lw$X == "yes"), "\n")
```

Console output:

```
Parents of E in the learned structure: [1] "T" "L"

cpdist, method = 'ls' (logic sampling / rejection):
rows returned (after rejection, requested n = 5000):  367 
has 'weights' attribute:  FALSE 

         no         yes 
0.005449591 0.994550409 

cpdist, method = 'lw' (likelihood weighting):
rows returned (n = 5000 requested):  5000 
has 'weights' attribute:  TRUE 
distinct weight values:  4 
    Min.  1st Qu.   Median     Mean  3rd Qu.     Max. 
0.000027 0.000027 0.000027 0.073573 0.000027 1.000000 
weighted P(X = yes | E = yes) from these particles:  0.9918495 
unweighted (naive, ignores weights) P(X = yes | E = yes) from same particles:  0.992
```

Logic sampling kept 367 of the 5,000 requested rows (7.3%, matching `E`'s roughly 7.4% marginal prevalence in the
data) — every one of them unweighted, because every one was generated by an unconstrained forward pass that happened to
land on `E = "yes"` by chance. Likelihood weighting kept all 5,000, with only four distinct weight values because `E`'s
two parents (`T`, `L`) are both binary, giving four possible parent configurations and therefore four possible values of
`P(E = "yes" | T, L)` for the importance weight to take — from 0.000027 for the combination that makes `E = "yes"`
almost impossible given its simulated parents, up to 1.0 for the combination that makes it certain. Reading off the
sampled node's distribution correctly requires using those weights (`sum(w[X == "yes"]) / sum(w)`), not a plain row
count; the two estimates agree closely here (0.9918 weighted against 0.9920 naive) only because `X` happens to be driven
almost entirely by
`E` in this network, so ignoring the weights costs little in this particular case — that near-agreement is a property of
this query, not a property of likelihood weighting in general, and the weighted calculation is the one that is correct
regardless.

**Tool equivalent:** none. See the closing note below.

## Can AdvancedAnalytics run either kind of query?

Not as a native, per-transaction tool. Both exact junction-tree propagation and `bnlearn`'s own approximate simulation
depend on the same thing this whole `BayesianNetworks/` set of files has already recorded as absent: a general
multi-node network, constructed by hand or learned, with arbitrary nodes queryable and evidence settable anywhere. That
capability is out of scope for `AdvancedAnalytics` as a native `[AdvancedAnalyticsTool]`
(`docs/AnalyticalMethodology/Reference/ModelCatalogue.md`, recorded in full
in [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md)'s closing section). The one shipped classifier
that shares an algorithmic root with this file's material,
`AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService`, only ever answers a forward question — the
probability of its one designated class variable given a full row of predictor values — by a single closed-form
calculation over its fitted Gaussians-per-class, not by junction-tree propagation or particle simulation over a general
graph, and it has no notion of an internal node two arcs from the class variable to condition on or sample from, because
its only "nodes" are the class and its direct predictors. Neither exact nor approximate inference over an arbitrary
evidence/query node pair, of the kind this file demonstrates throughout, has a *native* product recall path today.

That is not the same as having no way to run it at all: the R script execution job pattern (`Jube.R`, the sandboxed R
server, and the `RScriptTools` submit/poll/result agent tools; see [R Server](../RServer/index.md)) lets an agent
submit exactly the `as.grain`/`compile`/`querygrain` or `cpquery`/`cpdist` code this file demonstrates as a job and read
back the result — a batch, expert-reviewed operation, not a live one, but a genuine, callable one. See
[Explanatory Role Inside AdvancedAnalytics](ExplanatoryRoleInsideAdvancedAnalytics.md) for how this sits alongside TAN.
