---
layout: default
title: Score-Based Structure Learning
nav_order: 3
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Score-Based Structure Learning

The constraint-based family builds a skeleton by testing conditional independence between pairs of variables, then
orients whatever edges the tests leave. The score-based family works differently: it searches directly over the space of
candidate DAGs, assigning every candidate a single number — a score that measures how well that structure fits the data,
penalised for complexity — and keeps whichever structure scores best. `bnlearn` implements two searches over that space:
`hc` (hill-climbing) and `tabu` (tabu search).

Hill-climbing starts from a DAG (empty, by default) and repeatedly considers every legal single-arc move available from
the current structure — add an arc, remove one, or reverse one — scores the resulting DAG for each, and takes whichever
single move improves the score the most. It stops when no available move improves on the current structure, which is a
local optimum, not necessarily the best DAG over all of them. Tabu search is the same greedy move-scoring loop with one
addition: a short-term memory (the "tabu list") of recently reversed moves that the search is forbidden from immediately
undoing, which lets it accept a move that temporarily lowers the score in order to walk out of a local optimum it would
otherwise be stuck in.

## Scoring a structure: BIC and BDe

A score is a function of the DAG and the data alone, not of any particular parameter estimate. The Bayesian information
criterion (BIC) is the penalised log-likelihood familiar from ordinary model selection: fit every node's conditional
distribution by maximum likelihood, sum the log-likelihoods, then subtract a penalty proportional to the number of free
parameters and the log of the sample size. The Bayesian Dirichlet equivalent score (BDe) is a different quantity
entirely — not a penalised likelihood but the marginal likelihood of the data under a Dirichlet prior over every
conditional probability table, integrated over the uncertainty in those tables rather than plugging in their
maximum-likelihood estimate. The prior's strength is set by an equivalent sample size (`iss`, imaginary sample size): a
small `iss` is a strong prior belief in simple tables that takes more data to overturn.

### Procedure: scoring a known structure under both criteria

`learning.test` is a bundled synthetic dataset (5000 rows, six discrete variables) whose generating structure is
documented directly in the package (`?learning.test`): `[A][C][F][B|A][D|A:C][E|B:F]` — `A`, `C` and `F`
are roots, `B` depends on `A`, `D` on both `A` and `C`, and `E` on both `B` and `F`. `score()` computes either criterion
for any DAG against any dataset, so the true generating structure can be scored directly and compared against an
uninformative baseline, the empty graph with no arcs at all.

```R
library(bnlearn)
data(learning.test)

true.dag <- model2network("[A][C][F][B|A][D|A:C][E|B:F]")
cat("True DAG model string:", modelstring(true.dag), "\n\n")

bic.true <- score(true.dag, learning.test, type = "bic")
bde.true <- score(true.dag, learning.test, type = "bde", iss = 1)
cat(sprintf("True DAG BIC score = %.4f\n", bic.true))
cat(sprintf("True DAG BDe score (iss = 1) = %.4f\n", bde.true))

empty.dag <- empty.graph(names(learning.test))
bic.empty <- score(empty.dag, learning.test, type = "bic")
bde.empty <- score(empty.dag, learning.test, type = "bde", iss = 1)
cat(sprintf("\nEmpty DAG BIC score = %.4f\n", bic.empty))
cat(sprintf("Empty DAG BDe score (iss = 1) = %.4f\n", bde.empty))
```

Console output:

```
True DAG model string: [A][C][F][B|A][D|A:C][E|B:F] 

True DAG BIC score = -24006.7342
True DAG BDe score (iss = 1) = -24028.0948

Empty DAG BIC score = -28277.5930
Empty DAG BDe score (iss = 1) = -28280.3179
```

Both criteria are large negative numbers by convention (log-scale, and `bnlearn` reports them so that higher is better)
and both agree, decisively, that the true generating structure fits far better than the empty graph — a difference of
over 4000 in either criterion, against six arcs' worth of complexity penalty. That agreement in direction is the point
of calling both of these "goodness-of-fit criteria": they answer the same question, fit against complexity, from two
different derivations (penalised likelihood against marginal likelihood under a prior).

**Tool equivalent:** none for scoring a DAG. The same underlying idea — comparing fitted models by a
penalised-likelihood criterion rather than raw fit alone — is shipped for regression model comparison:
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.RegressionModelSelectionTools` / tool
`"CompareModelsByInformationCriteria"` computes AIC, its small-sample correction AICc, and BIC from supplied
log-likelihoods and parameter counts, with Akaike/BIC weights for relative support across candidate models. It compares
arbitrary fitted regression models against each other, not candidate graph structures against a dataset, and has no BDe
or Dirichlet-prior analogue at all.

## Does the search actually find that structure? Hill-climbing and tabu search, BIC against BDe

`hc` and `tabu` search for the best-scoring DAG automatically rather than requiring one to already know the structure to
score it. Run against the same criterion they should reach the same answer if the search converges to the true optimum;
run under different criteria, they can disagree, and how much they disagree depends on how much data is available to
distinguish the criteria's different complexity penalties.

### Procedure: hill-climbing and tabu search at two sample sizes

```R
library(bnlearn)
data(learning.test)

hc.bic <- hc(learning.test, score = "bic")
hc.bde <- hc(learning.test, score = "bde", iss = 1)
tabu.bic <- tabu(learning.test, score = "bic")

cat("n = 5000 (full learning.test)\n")
cat("hc(BIC) arcs:\n"); print(arcs(hc.bic))
cat("hc(BDe) and tabu(BIC) recover the identical arc set:",
    setequal(apply(arcs(hc.bic), 1, paste, collapse = "->"),
             apply(arcs(hc.bde), 1, paste, collapse = "->")) &&
    setequal(apply(arcs(hc.bic), 1, paste, collapse = "->"),
             apply(arcs(tabu.bic), 1, paste, collapse = "->")), "\n")

set.seed(5)
small <- learning.test[sample(nrow(learning.test), 50), ]
hc.bic.small <- hc(small, score = "bic")
hc.bde.small <- hc(small, score = "bde", iss = 1)

cat("\nn = 50 (subsample)\n")
cat("hc(BIC) arcs:\n"); print(arcs(hc.bic.small))
cat("hc(BDe, iss = 1) arcs:\n"); print(arcs(hc.bde.small))
cat("Same arc set as each other:", setequal(
  apply(arcs(hc.bic.small), 1, paste, collapse = "->"),
  apply(arcs(hc.bde.small), 1, paste, collapse = "->")), "\n")
```

Console output:

```
n = 5000 (full learning.test)
hc(BIC) arcs:
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "C"  "D"
[4,] "B"  "E"
[5,] "F"  "E"
hc(BDe) and tabu(BIC) recover the identical arc set: TRUE 

n = 50 (subsample)
hc(BIC) arcs:
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "E"
[4,] "E"  "F"
hc(BDe, iss = 1) arcs:
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
Same arc set as each other: FALSE 
```

At the full 5000-row sample, hill-climbing recovers the true generating structure exactly, and BIC, BDe and tabu search
all agree on identical arcs — the data is decisive enough that the criteria's different complexity penalties don't
change which structure wins. At a 50-row subsample the same two criteria diverge: BIC keeps four arcs, while BDe with
`iss = 1` keeps only two, dropping the `B -> E` and `E -> F`
arcs that BIC still supports. BDe's Dirichlet prior with a small equivalent sample size is, in effect, a stronger prior
belief in simple conditional distributions than BIC's flat per-parameter penalty, and with only 50 rows to overturn it
that belief still shows through. This is the concrete, practical meaning of
"different goodness-of-fit criteria": they encode different assumptions about how much evidence should be required
before adding a parent, and the choice only stops mattering once there is enough data that both converge on the same
answer anyway.

**Tool equivalent:** none. General Bayesian-network structure search over an arbitrary node set is out of
scope (`docs/AnalyticalMethodology/Reference/ModelCatalogue.md`: "KDB, TAN, AODE and HNB give most of the benefit" of
Bayesian-network-style reasoning for the classification use case the product actually needed). Tree-Augmented Naive
Bayes (`Jube.Service.Agent.AdvancedAnalyticsTools.TreeAugmentedNaiveBayesClassifierTrainingRunTools` / tool
`"SubmitTreeAugmentedNaiveBayesClassifierTraining"`, covered in
[BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md)) is the one classifier-shaped structure-learning
algorithm the product ships — it fixes one designated class variable and learns only its Chow-Liu tree of predictor
dependencies, never a free search over arcs between arbitrary nodes the way `hc` and `tabu` run here.

## Score equivalence and the completed partially directed graph

Two different DAGs can encode exactly the same set of conditional independence statements — the same joint distribution
shape — if one can be obtained from the other only by reversing arcs that are not part of a v-structure (a collider,
where two arcs point into the same node from parents that are not themselves directly connected). Such DAGs belong to
the same Markov equivalence class, and any score that is score-equivalent (BIC and BDe both are) must assign them
identical scores, because the score is a function of the independence structure the data supports, not of an arbitrary
choice of edge direction within it.
`cpdag()` collapses a DAG down to its completed partially directed graph — the essential graph for its whole equivalence
class — leaving an edge undirected wherever its direction is not determined by any v-structure, and `vstructs()` lists
the v-structures that do the determining.

### Procedure: v-structures, the CPDAG, and reversing a compelled arc against a reversible one

```R
library(bnlearn)
data(learning.test)

true.dag <- model2network("[A][C][F][B|A][D|A:C][E|B:F]")

cat("v-structures (colliders) in the true DAG:\n")
print(vstructs(true.dag))

cpdag.true <- cpdag(true.dag)
cat("\nCPDAG arcs -- directed rows are compelled, undirected pairs (both directions listed) are reversible:\n")
print(arcs(cpdag.true))

cat(sprintf("\nTrue DAG BIC score = %.4f\n", score(true.dag, learning.test, type = "bic")))

alt.dag <- reverse.arc(true.dag, "A", "B")
cat("\nReversible edge A->B reversed to B->A, model string:", modelstring(alt.dag), "\n")
cat(sprintf("Alternative DAG BIC score = %.4f\n", score(alt.dag, learning.test, type = "bic")))
cat("Same equivalence class as the true DAG (identical CPDAG):",
    isTRUE(all.equal(cpdag(true.dag), cpdag(alt.dag))), "\n")

bad.dag <- reverse.arc(true.dag, "B", "E")
cat("\nCompelled (v-structure) edge B->E reversed to E->B, model string:", modelstring(bad.dag), "\n")
cat(sprintf("That DAG's BIC score = %.4f\n", score(bad.dag, learning.test, type = "bic")))
cat("Same equivalence class as the true DAG:",
    isTRUE(all.equal(cpdag(true.dag), cpdag(bad.dag))), "\n")
```

Console output:

```
v-structures (colliders) in the true DAG:
     X   Z   Y  
[1,] "A" "D" "C"
[2,] "B" "E" "F"

CPDAG arcs -- directed rows are compelled, undirected pairs (both directions listed) are reversible:
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "A"
[4,] "B"  "E"
[5,] "C"  "D"
[6,] "F"  "E"

True DAG BIC score = -24006.7342

Reversible edge A->B reversed to B->A, model string: [B][C][F][A|B][E|B:F][D|A:C] 
Alternative DAG BIC score = -24006.7342
Same equivalence class as the true DAG (identical CPDAG): TRUE 

Compelled (v-structure) edge B->E reversed to E->B, model string: [A][C][F][D|A:C][E|F][B|A:E] 
That DAG's BIC score = -24374.9823
Same equivalence class as the true DAG: FALSE 
```

The true DAG has two v-structures — `A -> D <- C` and `B -> E <- F` — and the CPDAG shows their consequence directly:
every arc into `D` and every arc into `E` is compelled (only one direction appears), while `A -> B`
appears both ways round, marking it as the one reversible arc in this structure. Reversing it to `B -> A`
produces a different DAG with an identical BIC score to five decimal places and an identical CPDAG: it is a different
member of the same equivalence class, indistinguishable from the true structure by any score-equivalent criterion
applied to this data. Reversing `B -> E` instead — an arc that is part of a v-structure — destroys that collider (`E`'s
parents become `F` alone, with `B` now a child of `E`), produces a strictly worse BIC score, and a different CPDAG
entirely: a different equivalence class, not just a different edge direction within the same one.

This is the concrete meaning of "not identifiable from observational data alone": both directions of a reversible arc
explain the data equally well, under any criterion that is score-equivalent, so no amount of scoring more data through
the same observational distribution can ever prefer one over the other. Only a compelled arc — one fixed by a
v-structure — is determined by the data's independence structure at all; telling `A -> B` from `B -> A` here would need
either an intervention or external causal knowledge, not more observational rows.

**Tool equivalent:** none. No shipped model exposes a CPDAG, a v-structure list, or an equivalence-class
comparison; a trained classifier (TAN included) has one designated class variable and a fixed set of directed predictor
relationships around it, not a general DAG whose edge directions are themselves an open question the way they are here.
