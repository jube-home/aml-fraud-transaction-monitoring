---
layout: default
title: Graphical Models Foundations
nav_order: 1
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Graphical Models Foundations

## DAGs as a factorisation of a joint distribution

A Bayesian network's graph is a set of nodes (one per variable) and directed arcs between them, with no directed cycle —
a DAG. The graph is not decoration on top of the probability model; it is a direct statement of how the joint
distribution over all the variables factorises. For a set of variables
`X1, ..., Xn`, the general chain rule of probability always lets you write the joint distribution as a product of
conditionals, each variable given all the ones before it in some ordering:

```
P(X1, ..., Xn) = P(X1) * P(X2 | X1) * P(X3 | X1, X2) * ... * P(Xn | X1, ..., Xn-1)
```

That factorisation is true of any joint distribution and carries no information by itself — every variable still
conditions on everything before it. A Bayesian network's DAG asserts something stronger: each variable's conditional
term only needs to mention its parents in the graph, not every earlier variable.

```
P(X1, ..., Xn) = product over i of P(Xi | parents(Xi))
```

This is the local Markov property, and it is the entire content of a Bayesian network's structure: a claim about which
variables are conditionally independent of which others, encoded as missing arcs.

### Procedure 1: Declaring a DAG and reading off its factorisation

`bnlearn::model2network` parses a bracketed string — one bracket per node, a `|` before its parent list — directly into
a `bn` structure object, with no data involved at all. `modelstring` serialises it back the same way, and walking each
node's `parents()` reads off exactly the factorisation the graph asserts.

```R
library(bnlearn)

dag <- model2network("[A][B][C|A:B][D|C][E|D]")

cat("Model string (the DAG's own compact serialisation):\n")
print(modelstring(dag))

cat("\nNodes:\n")
print(nodes(dag))

cat("\nArcs:\n")
print(arcs(dag))

cat("\nJoint distribution factorised node by node, one local term per node given its parents:\n")
for (n in nodes(dag)) {
  p <- parents(dag, n)
  if (length(p) == 0) {
    cat(sprintf("  P(%s)\n", n))
  } else {
    cat(sprintf("  P(%s | %s)\n", n, paste(p, collapse = ", ")))
  }
}
```

Console output:

```
Model string (the DAG's own compact serialisation):
[1] "[A][B][C|A:B][D|C][E|D]"

Nodes:
[1] "A" "B" "C" "D" "E"

Arcs:
     from to 
[1,] "A"  "C"
[2,] "B"  "C"
[3,] "C"  "D"
[4,] "D"  "E"

Joint distribution factorised node by node, one local term per node given its parents:
  P(A)
  P(B)
  P(C | A, B)
  P(D | C)
  P(E | D)
```

`C` has two parents (`A` and `B`), and its collider structure — two arrowheads meeting at one node — is the subject of
the next procedure. `D` and `E` form a chain running away from `C`.

**Tool equivalent:** none. Declaring an arbitrary DAG and reading a factorisation off it has no product
surface — the one structure-learning model the product ships, Tree-Augmented Naive Bayes (see
[BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md)), fixes its own shape (one class node, every other
variable beneath it) rather than accepting an analyst-declared graph.

## Conditional independence and d-separation

Two variables that are not directly connected by an arc can still be dependent through the paths that connect them, and
whether they are depends on which other variables you condition on. D-separation is the graphical rule that answers this
without touching any probability arithmetic: it decides, purely from the graph's shape, whether every path between two
nodes is "blocked" given a conditioning set — and if every path is blocked, the two nodes are guaranteed conditionally
independent given that set, for any distribution that factorises over the graph.

Three path shapes decide whether a path is blocked, given a conditioning set `Z`:

- **Chain** (`X -> M -> Y`): blocked if the mediator `M` is in `Z`. Conditioning on the middle of a chain cuts it.
- **Fork** (`X <- M -> Y`): blocked if the common cause `M` is in `Z`. Conditioning on a shared cause cuts the path
  between its effects.
- **Collider** (`X -> M <- Y`): the opposite rule. The path is blocked *unless* `M` (or any descendant of
  `M`) is in `Z`. Two otherwise-unrelated causes of a common effect are marginally independent, but conditioning on that
  effect (or on something downstream of it) makes them dependent — the "explaining away" effect: learning the effect
  happened, and that one cause is ruled out, makes the other cause more likely.

Two nodes are d-separated given `Z` exactly when every path between them is blocked given `Z`; otherwise they are
d-connected.

### Procedure 2: Checking d-separation claims against a small hand-built network

The network from Procedure 1 (`A`, `B` both parents of the collider `C`; `C -> D -> E` a chain running on from it) has
both a collider and a chain in one small structure, so it can carry all three path shapes' d-separation claims.
`bnlearn::dsep(dag, x, y, z)` tests whether `x` and `y` are d-separated given `z`
(default `z = character(0)`, the empty conditioning set), and `bnlearn::mb` reads off a node's Markov blanket directly
from the graph.

```R
library(bnlearn)

dag <- model2network("[A][B][C|A:B][D|C][E|D]")

cat("Arcs:\n")
print(arcs(dag))

cat("\nd-separated: A _||_ B given {} (collider at C not conditioned on)?\n")
print(dsep(dag, "A", "B"))

cat("\nd-separated: A _||_ B given {C} (conditioning on the collider opens the path)?\n")
print(dsep(dag, "A", "B", "C"))

cat("\nd-separated: A _||_ D given {C} (chain blocked at the conditioned mediator)?\n")
print(dsep(dag, "A", "D", "C"))

cat("\nd-separated: A _||_ E given {D} (chain blocked further downstream)?\n")
print(dsep(dag, "A", "E", "D"))

cat("\nd-separated: A _||_ E given {} (no conditioning, path open through C and D)?\n")
print(dsep(dag, "A", "E"))

cat("\nMarkov blanket of C:\n")
print(mb(dag, "C"))

cat("\nMarkov blanket of A:\n")
print(mb(dag, "A"))
```

Console output:

```
Arcs:
     from to 
[1,] "A"  "C"
[2,] "B"  "C"
[3,] "C"  "D"
[4,] "D"  "E"

d-separated: A _||_ B given {} (collider at C not conditioned on)?
[1] TRUE

d-separated: A _||_ B given {C} (conditioning on the collider opens the path)?
[1] FALSE

d-separated: A _||_ D given {C} (chain blocked at the conditioned mediator)?
[1] TRUE

d-separated: A _||_ E given {D} (chain blocked further downstream)?
[1] TRUE

d-separated: A _||_ E given {} (no conditioning, path open through C and D)?
[1] FALSE

Markov blanket of C:
[1] "A" "B" "D"

Markov blanket of A:
[1] "B" "C"
```

Every claim lines up with the three path-shape rules above: `A` and `B` are independent until their common effect `C` is
conditioned on, at which point the collider opens and they become dependent (explaining away); conditioning on `C` also
blocks the chain running on to `D`, and conditioning on `D` blocks it again one step further at `E`; with no
conditioning at all, the chain to `E` is open, so `A` and `E` are dependent.

### Markov blankets

A node's Markov blanket is the smallest set of other nodes that renders it conditionally independent of every remaining
node in the network — its parents, its children, and its children's other parents (the
"co-parents," needed because conditioning on a child's own value, per the collider rule above, opens a path back through
every other parent of that child). `C`'s Markov blanket above is exactly `{A, B, D}`: its two parents plus its one
child, with no co-parents to add because `D` has no other parent. `A`'s Markov blanket is `{B, C}`: no parents of its
own, one child (`C`), and `C`'s co-parent `B`.

Nothing outside a node's Markov blanket carries any information about it once the blanket itself is known — the
practical reason the concept matters for structure learning, covered in
[ConstraintBasedStructureLearning.md](ConstraintBasedStructureLearning.md): several of that file's algorithms build a
network by first recovering each node's Markov blanket via conditional-independence testing, then assembling the full
graph from those local neighbourhoods.

**Tool equivalent:** none. No product tool computes d-separation or a Markov blanket for an arbitrary node
set — both are queries against a general graph structure that the product does not carry, for the same reason recorded
in [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md):
general Bayesian-network structure search is out of scope.

## Discrete, Gaussian and conditional Gaussian (hybrid) networks

Everything above is agnostic to what kind of variable each node holds. `bnlearn` fits three kinds of local distribution,
and a single network can mix the last two:

- **Discrete network**: every node is a categorical (multinomial) variable. Each node's local distribution is a
  conditional probability table (CPT) — one probability per combination of its own states and its parents' states.
- **Gaussian network**: every node is continuous, and each node's local distribution is linear-Gaussian — a node's mean
  is an intercept plus a linear combination of its continuous parents' values, with a fixed residual standard deviation,
  exactly an ordinary linear regression of the node on its parents.
- **Conditional Gaussian (hybrid) network**: nodes can be a mix of discrete and continuous, subject to one structural
  rule bnlearn enforces — a discrete node may not have a continuous parent (a category can't be regressed on a continuum
  in this model class). A continuous node with discrete parents gets one linear-Gaussian regression per combination of
  its discrete parents' states, so both the intercept/slope coefficients and the residual standard deviation can vary by
  group.

`custom.fit` builds a fully-parameterised network of any of these three kinds directly from hand-supplied parameters,
with no data or structure-learning step at all — useful here precisely because it isolates what each node kind's
parameters look like.

### Procedure 3: A discrete network

A three-level `Season` node (`Dry`, `Wet`, `Monsoon`) feeding a two-level `RainfallHigh` node. The child's CPT is one
column per parent state, one row per child state.

```R
library(bnlearn)

dag <- model2network("[Season][RainfallHigh|Season]")

season.cpt <- matrix(c(0.5, 0.3, 0.2), ncol = 3,
  dimnames = list(NULL, c("Dry", "Wet", "Monsoon")))

rainfall.cpt <- matrix(
  c(0.05, 0.40, 0.85,
    0.95, 0.60, 0.15),
  ncol = 3, byrow = TRUE,
  dimnames = list(RainfallHigh = c("Yes", "No"), Season = c("Dry", "Wet", "Monsoon")))

fitted <- custom.fit(dag, dist = list(Season = season.cpt, RainfallHigh = rainfall.cpt))

cat("Node classes:\n")
print(sapply(fitted, class))

print(fitted)
```

Console output:

```
Node classes:
  RainfallHigh         Season 
"bn.fit.dnode" "bn.fit.dnode" 

  Bayesian network parameters

  Parameters of node RainfallHigh (multinomial distribution)

Conditional probability table:
 
            Season
RainfallHigh  Dry  Wet Monsoon
         Yes 0.05 0.40    0.85
         No  0.95 0.60    0.15

  Parameters of node Season (multinomial distribution)

Conditional probability table:
 
    Dry     Wet Monsoon 
    0.5     0.3     0.2 
```

**Tool equivalent:** none for hand-authoring a CPT with no data behind it — see
[BayesianAnalysisWithBnlearn.md, Procedure 1](../BayesianAnalysisWithBnlearn.md) for that finding in full; the nearest
concept there is a regularisation prior blended in during automatic fitting, not manual CPT entry.

### Procedure 4: A Gaussian network

Two continuous nodes, `TransactionAmount` feeding `RiskScore` linearly. A `bn.fit.gnode`'s parameters are exactly a
linear regression's: named coefficients (an `(Intercept)` plus one term per continuous parent) and a residual standard
deviation.

```R
library(bnlearn)

dag <- model2network("[TransactionAmount][RiskScore|TransactionAmount]")

amount.dist <- list(coef = c("(Intercept)" = 250), sd = 80)
risk.dist <- list(coef = c("(Intercept)" = 10, TransactionAmount = 0.12), sd = 5)

fitted <- custom.fit(dag, dist = list(TransactionAmount = amount.dist, RiskScore = risk.dist))

cat("Node classes:\n")
print(sapply(fitted, class))

print(fitted)
```

Console output:

```
Node classes:
        RiskScore TransactionAmount 
   "bn.fit.gnode"    "bn.fit.gnode" 

  Bayesian network parameters

  Parameters of node RiskScore (Gaussian distribution)

Conditional density: RiskScore | TransactionAmount
Coefficients:
      (Intercept)  TransactionAmount  
            10.00               0.12  
Standard deviation of the residuals: 5 

  Parameters of node TransactionAmount (Gaussian distribution)

Conditional density: TransactionAmount
Coefficients:
(Intercept)  
        250  
Standard deviation of the residuals: 80 
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService`
is the real, shipped overlap with this exact node shape — per the code audit in
[BayesianAnalysisWithBnlearn.md, Procedure 7](../BayesianAnalysisWithBnlearn.md), every predictor it fits is a Gaussian
node conditioned on the class (and, where it has one, its one tree-parent predictor) — structurally identical to the
`RiskScore | TransactionAmount` node above, just always with a discrete class variable somewhere in the conditioning set
rather than another continuous node in the general case.

### Procedure 5: A conditional Gaussian (hybrid) network

A discrete `Channel` node (`Online`, `Branch`) feeding a continuous `TransactionAmount` node. Because the parent is
discrete, the child's fitted object is a `bn.fit.cgnode` with one intercept and one residual standard deviation per
parent configuration — here, two of each, one per channel.

```R
library(bnlearn)

dag <- model2network("[Channel][TransactionAmount|Channel]")

channel.cpt <- matrix(c(0.7, 0.3), ncol = 2, dimnames = list(NULL, c("Online", "Branch")))

amount.dist <- list(
  coef = matrix(c(180, 420), nrow = 1, dimnames = list("(Intercept)", NULL)),
  sd = c(60, 150))

fitted <- custom.fit(dag, dist = list(Channel = channel.cpt, TransactionAmount = amount.dist))

cat("Node classes:\n")
print(sapply(fitted, class))

print(fitted)
```

Console output:

```
Node classes:
          Channel TransactionAmount 
   "bn.fit.dnode"   "bn.fit.cgnode" 

  Bayesian network parameters

  Parameters of node Channel (multinomial distribution)

Conditional probability table:
 
Online Branch 
   0.7    0.3 

  Parameters of node TransactionAmount (conditional Gaussian distribution)

Conditional density: TransactionAmount | Channel
Coefficients:
               0    1
(Intercept)  180  420
Standard deviation of the residuals:
  0    1  
 60  150  
Discrete parents' configurations:
   Channel
0   Online
1   Branch
```

Branch transactions average 420 with a wider spread (sd 150) against Online's 180 average and tighter spread (sd 60) —
two entirely separate linear-Gaussian regressions living on the same node, selected by which discrete configuration of
`Channel` a case falls into.

**Tool equivalent:** none as a general hybrid network. The structurally closest thing the product ships is
Tree-Augmented Naive Bayes itself: one discrete root (the class) with Gaussian nodes beneath it is a restricted special
case of a conditional Gaussian network — restricted to exactly one discrete node, at the root, with no discrete node
ever appearing beneath a continuous one — not a network where discrete and continuous nodes can appear at arbitrary
positions relative to each other, subject only to the no-continuous-parent-of-a-discrete-node rule bnlearn enforces in
general.
