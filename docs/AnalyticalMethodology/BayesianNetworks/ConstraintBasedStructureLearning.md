---
layout: default
title: Constraint-Based Structure Learning
nav_order: 2
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Constraint-Based Structure Learning

Everything in [GraphicalModelsFoundations.md](GraphicalModelsFoundations.md) assumed the DAG was already known —
hand-declared, or otherwise given. Constraint-based structure learning recovers the DAG itself from data, using nothing
but repeated conditional-independence (CI) tests: no scoring of whole candidate graphs, no search over graph space as
such. Five algorithms in this family — PC-stable, Grow-Shrink, IAMB, Fast-IAMB and Inter-IAMB — are covered here.

## The shared idea

Every constraint-based algorithm follows the same two-stage shape:

1. **Prune edges by testing conditional independence.** For a candidate edge between two variables, run a CI test
   conditioning on subsets of the other variables. If some conditioning set renders the pair independent, there is no
   direct edge between them — the d-separating set found is recorded (it matters for the next step). What differs
   between algorithms here is *how* the neighbourhood of tests is chosen — see below — and *which* CI test is used to
   decide independence at each step.
2. **Orient whatever the skeleton allows.** Once the undirected skeleton (which pairs have an edge at all)
   is fixed, unshielded triples — `X - Z - Y` with no edge between `X` and `Y` — are checked against the d-separating
   sets recorded in step 1: if `Z` was *not* in the set that separated `X` and `Y`, the triple must be a collider
   (`X -> Z <- Y`), a v-structure, because conditioning on `Z` would otherwise have re-opened a path the test found
   closed. Every v-structure found this way is oriented; whatever direction is not forced by a v-structure, or by
   propagating those orientations without creating a new cycle or a new unshielded collider (Meek's rules), is left
   undirected. The result is a completed partially directed acyclic graph (CPDAG) — an equivalence class of DAGs that
   all encode the same conditional-independence statements — not necessarily a single fully-directed DAG.

## What differs between them

- **PC-stable** (`pc.stable`) tests every pair of variables, starting with the empty conditioning set and growing it,
  pruning edges as soon as some conditioning set is found that separates the pair; "stable" means the neighbourhood used
  to build each conditioning set is fixed for the whole pass rather than updated edge-by-edge, so the result does not
  depend on the order variables happen to be given in (the instability the original PC algorithm was named for).
- **Grow-Shrink** (`gs`) and the **IAMB family** (`iamb`, `fast.iamb`, `inter.iamb`) instead recover each node's Markov
  blanket directly (see [GraphicalModelsFoundations.md](GraphicalModelsFoundations.md)
  for what a Markov blanket is), then assemble the skeleton from those blankets before orienting exactly as above. They
  differ in how a node's blanket is built:
    - **Grow-Shrink** grows a candidate blanket forward, adding any variable not yet conditionally independent of the
      target given the blanket so far, then shrinks it backward, removing any variable that turns out to be
      conditionally independent given the rest of the final candidate set — a strictly two-phase, grow-then-prune
      strategy.
    - **IAMB** interleaves growing and shrinking more aggressively: it grows by always adding whichever remaining
      variable has the *strongest* association with the target (not just any dependent one), then shrinks, which tends
      to need more tests on data like this.
    - **Fast-IAMB** speeds up IAMB's growing phase by adding several variables in one step instead of one at a time, at
      the cost of sometimes adding one that a later shrink step has to remove again. It is flagged as deprecated in this
      package version (see Procedure 3 below).
    - **Inter-IAMB** shrinks after every single growth step rather than only once at the end, trading more frequent
      shrinking for a blanket that never grows as large as IAMB's before being cut back.

The CI test itself is a separate, orthogonal choice, set with every one of these functions' `test` argument. For
discrete data such as `learning.test` below, the default (`test = NULL`, which resolves to `"mi"`) is mutual information
under its asymptotic chi-square distribution; `"mi-sh"` is a shrinkage-estimator variant of the same statistic (the
James-Stein estimator applied to the mutual information itself, rather than plugging in raw empirical probabilities)
that behaves better with few observations relative to the number of parameters being tested. Procedure 3 below runs the
same algorithm under both and checks whether the learned structure changes.

### Procedure 1: Running PC-stable, Grow-Shrink and IAMB against a known generating network

`bnlearn`'s bundled `learning.test` dataset (5000 rows, six discrete variables `A`-`F`) has a documented true generating
network, quoted directly from the package's own `?learning.test` help page:
`[A][C][F][B|A][D|A:C][E|B:F]`. Running all three algorithms against the same data and printing each fitted object shows
the algorithm name, the CI test actually used, the significance threshold and the number of CI tests it took to reach a
structure — everything needed to compare them like for like.

```R
library(bnlearn)

data(learning.test)

true.dag <- model2network("[A][C][F][B|A][D|A:C][E|B:F]")
cat("True generating DAG for learning.test (from bnlearn's own documentation), arcs:\n")
print(arcs(true.dag))

pc.fit <- pc.stable(learning.test)
cat("\n--- PC-stable ---\n")
print(pc.fit)
print(arcs(pc.fit))

gs.fit <- gs(learning.test)
cat("\n--- Grow-Shrink ---\n")
print(gs.fit)
print(arcs(gs.fit))

iamb.fit <- iamb(learning.test)
cat("\n--- IAMB ---\n")
print(iamb.fit)
print(arcs(iamb.fit))

cat("\nAll three learned the identical arc set on this data?\n")
print(all.equal(arcs(pc.fit), arcs(gs.fit)))
print(all.equal(arcs(gs.fit), arcs(iamb.fit)))
```

Console output:

```
True generating DAG for learning.test (from bnlearn's own documentation), arcs:
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "C"  "D"
[4,] "B"  "E"
[5,] "F"  "E"

--- PC-stable ---

  Bayesian network learned via Constraint-based methods

  model:
    [partially directed graph]
  nodes:                                 6 
  arcs:                                  5 
    undirected arcs:                     1 
    directed arcs:                       4 
  average markov blanket size:           2.33 
  average neighbourhood size:            1.67 
  average branching factor:              0.67 

  learning algorithm:                    PC (Stable) 
  conditional independence test:         Mutual Information (disc.) 
  alpha threshold:                       0.05 
  tests used in the learning procedure:  47 

     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "A"
[4,] "B"  "E"
[5,] "C"  "D"
[6,] "F"  "E"

--- Grow-Shrink ---

  Bayesian network learned via Constraint-based methods

  model:
    [partially directed graph]
  nodes:                                 6 
  arcs:                                  5 
    undirected arcs:                     1 
    directed arcs:                       4 
  average markov blanket size:           2.33 
  average neighbourhood size:            1.67 
  average branching factor:              0.67 

  learning algorithm:                    Grow-Shrink 
  conditional independence test:         Mutual Information (disc.) 
  alpha threshold:                       0.05 
  tests used in the learning procedure:  111 

     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "A"
[4,] "B"  "E"
[5,] "C"  "D"
[6,] "F"  "E"

--- IAMB ---

  Bayesian network learned via Constraint-based methods

  model:
    [partially directed graph]
  nodes:                                 6 
  arcs:                                  5 
    undirected arcs:                     1 
    directed arcs:                       4 
  average markov blanket size:           2.33 
  average neighbourhood size:            1.67 
  average branching factor:              0.67 

  learning algorithm:                    IAMB 
  conditional independence test:         Mutual Information (disc.) 
  alpha threshold:                       0.05 
  tests used in the learning procedure:  134 

     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "A"
[4,] "B"  "E"
[5,] "C"  "D"
[6,] "F"  "E"

All three learned the identical arc set on this data?
[1] TRUE
[1] TRUE
```

All three land on the same equivalence class on this data — `A-B` recovered as an undirected edge, the other four arcs
correctly directed — but reach it with very different amounts of testing: 47 tests for PC-stable against 111 for
Grow-Shrink and 134 for IAMB, direct empirical evidence of the neighbourhood-growing strategies named above costing
genuinely different amounts of work for the same result.

**Tool equivalent:** none. General Bayesian-network structure search — recovering an arbitrary DAG's
skeleton and orientation from data, exactly what these five algorithms do — is out of scope for AdvancedAnalytics
(`docs/AnalyticalMethodology/Reference/ModelCatalogue.md`, also recorded in
[BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md)). The only structure-learning algorithm the product
ships, `tree.bayes`-equivalent Tree-Augmented Naive Bayes, is score/mutual-information based over a fixed classifier
shape rather than a general constraint-based search — see
[ScoreBasedStructureLearning.md](ScoreBasedStructureLearning.md) for where that family sits instead.

### Procedure 2: Scoring the learned graphs against the true DAG

`compare()` counts, arc by arc, how many of the arcs actually present in the learned network (`current`) also appear in
the true network (`target`) as true positives, how many appear only in the learned network as false positives, and how
many true arcs are missing entirely as false negatives. Because an undirected edge is stored internally as *two*
opposite directed arcs, an edge the algorithm could not orient contributes to both `fp` (the direction not in the true
DAG) and `fn` (the correct direction, which is present but not credited because the same pair also appears the other way
round) — worth checking explicitly rather than taking the raw counts at face value. `shd()` instead compares each
network's CPDAG (the same equivalence class `pc.stable` etc. actually target, per the shared idea above) rather than the
raw arc set, which is the more appropriate comparison for a family of algorithms that only ever claims to recover an
equivalence class.

```R
library(bnlearn)

data(learning.test)

true.dag <- model2network("[A][C][F][B|A][D|A:C][E|B:F]")
pc.fit <- pc.stable(learning.test)
gs.fit <- gs(learning.test)
iamb.fit <- iamb(learning.test)

cat("compare() summary (tp/fp/fn arc counts) against the true DAG:\n")
cat("PC-stable:  "); print(unlist(compare(true.dag, pc.fit)))
cat("Grow-Shrink:"); print(unlist(compare(true.dag, gs.fit)))
cat("IAMB:       "); print(unlist(compare(true.dag, iamb.fit)))

cat("\nWhich exact arcs, for PC-stable (identical for the other two, since the arc sets match):\n")
print(compare(true.dag, pc.fit, arcs = TRUE))

cat("\nStructural Hamming Distance against the true DAG (computed on CPDAGs):\n")
cat("PC-stable:  "); print(shd(pc.fit, true.dag))
cat("Grow-Shrink:"); print(shd(gs.fit, true.dag))
cat("IAMB:       "); print(shd(iamb.fit, true.dag))

cat("\nTrue DAG's own equivalence class (CPDAG) -- is A-B undirected there too?\n")
print(arcs(cpdag(true.dag)))

cat("\nIs PC-stable's CPDAG identical to the true DAG's CPDAG?\n")
print(all.equal(cpdag(true.dag), cpdag(pc.fit)))
```

Console output:

```
compare() summary (tp/fp/fn arc counts) against the true DAG:
PC-stable:  tp fp fn 
 4  1  1 
Grow-Shrink:tp fp fn 
 4  1  1 
IAMB:       tp fp fn 
 4  1  1 

Which exact arcs, for PC-stable (identical for the other two, since the arc sets match):
$tp
     from to 
[1,] "A"  "D"
[2,] "C"  "D"
[3,] "B"  "E"
[4,] "F"  "E"

$fp
     from to 
[1,] "A"  "B"
[2,] "B"  "A"

$fn
     from to 
[1,] "A"  "B"


Structural Hamming Distance against the true DAG (computed on CPDAGs):
PC-stable:  [1] 0
Grow-Shrink:[1] 0
IAMB:       [1] 0

True DAG's own equivalence class (CPDAG) -- is A-B undirected there too?
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "A"
[4,] "B"  "E"
[5,] "C"  "D"
[6,] "F"  "E"

Is PC-stable's CPDAG identical to the true DAG's CPDAG?
[1] TRUE
```

Read naively, `compare()`'s raw arc counts (4 true positives, 1 false positive, 1 false negative for every algorithm)
look like a miss on the `A-B` edge. It is not: `A-B` sits in no v-structure in the *true* DAG either, so its own CPDAG
leaves `A-B` undirected too — confirmed above by comparing `cpdag(true.dag)` to
`cpdag(pc.fit)` directly, `TRUE`, and by `shd()` (which compares CPDAGs) scoring all three algorithms `0`, a perfect
match. The raw `compare()` counts penalise an algorithm for failing to orient an edge that no amount of data could ever
orient from observational conditional-independence information alone — a genuine limitation of the whole
constraint-based family (and of score-based search over CPDAGs, see
[ScoreBasedStructureLearning.md](ScoreBasedStructureLearning.md)), not a flaw in any one algorithm here.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelSelectionTools`
/ tool `"CompareModels"` compares trained model families against each other on held-out predictive metrics (ROC AUC, log
loss, and so on — see [BayesianAnalysisWithBnlearn.md, Procedure 5](../BayesianAnalysisWithBnlearn.md)), which is a
different comparison in kind: it scores predictive performance of fitted classifiers, not structural agreement between a
learned graph and a known ground-truth DAG. There is no product tool that performs a `compare()`/`shd()`-style
structural comparison, because there is no general structure-learning output for it to compare.

### Procedure 3: Changing the CI test, and the deprecated Fast-IAMB

Every constraint-based function takes a `test` argument naming which CI test drives its pruning decisions (`bnlearn`'s
own documentation topic `conditional.independence.tests` lists the full set available per data type). Swapping
PC-stable's default `"mi"` for the shrinkage estimator `"mi-sh"` is a one-argument change; whether it changes the
learned structure is an empirical question, checked directly below. `fast.iamb` and
`inter.iamb` are run alongside the three algorithms from Procedure 1 to complete the family, and to surface one real
caveat this package version raises on its own: `fast.iamb()` warns that it is deprecated and will be removed in a future
release.

```R
library(bnlearn)

data(learning.test)

pc.fit.mi <- pc.stable(learning.test)
pc.fit.sh <- pc.stable(learning.test, test = "mi-sh")

cat("PC-stable, default test (mutual information, asymptotic chi-square):\n")
print(arcs(pc.fit.mi))

cat("\nPC-stable, test = \"mi-sh\" (shrinkage-estimator mutual information):\n")
print(arcs(pc.fit.sh))

cat("\nSame equivalence class either way on this data?\n")
print(all.equal(cpdag(pc.fit.mi), cpdag(pc.fit.sh)))

cat("\n--- Fast-IAMB ---\n")
fast.fit <- fast.iamb(learning.test)
print(fast.fit)

cat("\n--- Inter-IAMB ---\n")
inter.fit <- inter.iamb(learning.test)
print(inter.fit)

cat("\nNumber of conditional-independence tests each algorithm needed on identical data:\n")
cat(sprintf("  PC-stable:   %d\n", pc.fit.mi$learning$ntests))
cat(sprintf("  Grow-Shrink: %d\n", gs(learning.test)$learning$ntests))
cat(sprintf("  IAMB:        %d\n", iamb(learning.test)$learning$ntests))
cat(sprintf("  Fast-IAMB:   %d\n", fast.fit$learning$ntests))
cat(sprintf("  Inter-IAMB:  %d\n", inter.fit$learning$ntests))
```

Console output:

```
PC-stable, default test (mutual information, asymptotic chi-square):
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "A"
[4,] "B"  "E"
[5,] "C"  "D"
[6,] "F"  "E"

PC-stable, test = "mi-sh" (shrinkage-estimator mutual information):
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "B"  "A"
[4,] "B"  "E"
[5,] "C"  "D"
[6,] "F"  "E"

Same equivalence class either way on this data?
[1] TRUE

--- Fast-IAMB ---
Warning message:
In bnlearn(data = x, cluster = cluster, whitelist = whitelist, blacklist = blacklist,  :
  fast.iamb() is deprecated and will be removed in 2027.

  Bayesian network learned via Constraint-based methods

  model:
    [partially directed graph]
  nodes:                                 6 
  arcs:                                  5 
    undirected arcs:                     1 
    directed arcs:                       4 
  average markov blanket size:           2.33 
  average neighbourhood size:            1.67 
  average branching factor:              0.67 

  learning algorithm:                    Fast-IAMB 
  conditional independence test:         Mutual Information (disc.) 
  alpha threshold:                       0.05 
  tests used in the learning procedure:  140 


--- Inter-IAMB ---

  Bayesian network learned via Constraint-based methods

  model:
    [partially directed graph]
  nodes:                                 6 
  arcs:                                  5 
    undirected arcs:                     1 
    directed arcs:                       4 
  average markov blanket size:           2.33 
  average neighbourhood size:            1.67 
  average branching factor:              0.67 

  learning algorithm:                    Inter-IAMB 
  conditional independence test:         Mutual Information (disc.) 
  alpha threshold:                       0.05 
  tests used in the learning procedure:  136 


Number of conditional-independence tests each algorithm needed on identical data:
  PC-stable:   47
  Grow-Shrink: 111
  IAMB:        134
  Fast-IAMB:   140
  Inter-IAMB:  136
```

The test swap changes nothing here — `learning.test` has 5000 rows, plenty for the asymptotic and shrinkage estimators
to agree — which is itself worth stating plainly: the choice of CI test matters most with small samples or many parents,
not on data this size. The test-count ordering across all five algorithms (47, 111, 134, 136, 140) tracks the
neighbourhood-growing strategy differences named above:
PC-stable's pairwise skeleton search needed the fewest tests by a wide margin; Grow-Shrink's strict grow-then-shrink
came next; the three IAMB variants, which recover a full Markov blanket per node before moving on, needed the most, with
Fast-IAMB's coarser multi-variable growing step needing slightly *more*
tests than plain IAMB here, not fewer, because its growing phase over-adds and its shrink phase then has to correct it.

**Tool equivalent:** none for either finding — no product tool exposes a choice of conditional-independence
test (there is no CI-testing structure search to configure), and `fast.iamb`'s upstream deprecation is a `bnlearn`
-package lifecycle note with nothing to map onto a product surface that does not use the algorithm at all.
