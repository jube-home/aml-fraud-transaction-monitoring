---
layout: default
title: Parameter Learning and CPTs
nav_order: 5
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Parameter Learning and Conditional Probability Tables

Structure learning (`hc`, `tabu`, `tree.bayes`) and hand-drawn structure (`model2network`) both produce a DAG with no
numbers attached — an arc says two variables are dependent, not by how much. Fitting parameters means estimating a
conditional probability table (CPT) for every discrete node, or a conditional distribution for every continuous one,
from data. `bnlearn::bn.fit` does this over an existing structure by one of several estimators, selected with its
`method` argument. This file works through the two discrete estimators — maximum likelihood (`method = "mle"`) and
Bayesian estimation with a Dirichlet prior (`method = "bayes"`) — then the continuous and hybrid cases, which fit a
different kind of parameter (regression coefficients, not a table) regardless of which of those two estimators is asked
for.

[BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedures 4 and 5, already used both `bn.fit(...,
method = "mle")` on a hand-specified structure and `bn.fit(..., method = "bayes", iss = 1)` on a Tree-Augmented Naive
Bayes structure, without dwelling on what separates the two estimators or what happens when a parent configuration is
rare or absent from the training data. That is this file's subject.

## Maximum likelihood: counting and dividing

### Procedure 1: Maximum likelihood estimation on a discrete network

`method = "mle"` is the plain estimator: for every node, for every configuration of its parents' states, count how many
training rows fall into each of the node's own states, and divide by the row total for that parent configuration. No
prior belief and no adjustment — the fitted probability for a cell is exactly its observed relative frequency. This is
run here on `learning.test`, a bundled discrete dataset (six variables, all factors, 5000 rows) that ships with
`bnlearn` for exactly this kind of exercise, with structure learned by hill-climbing first so the CPTs are fitted over a
real, non-trivial DAG rather than a single hand-drawn arc.

```R
library(bnlearn)

data(learning.test)
str(learning.test)

dag <- hc(learning.test)
cat("\nLearned structure arcs:\n")
print(arcs(dag))

fitted.mle <- bn.fit(dag, learning.test, method = "mle")
cat("\nMLE-fitted CPT for node F:\n")
print(fitted.mle$F)
```

Console output:

```
'data.frame':	5000 obs. of  6 variables:
 $ A: Factor w/ 3 levels "a","b","c": 2 2 1 1 1 3 3 2 2 2 ...
 $ B: Factor w/ 3 levels "a","b","c": 3 1 1 1 1 3 3 2 2 1 ...
 $ C: Factor w/ 3 levels "a","b","c": 2 3 1 1 2 1 2 1 2 2 ...
 $ D: Factor w/ 3 levels "a","b","c": 1 1 1 1 3 3 3 2 1 1 ...
 $ E: Factor w/ 3 levels "a","b","c": 2 2 1 2 1 3 3 2 3 1 ...
 $ F: Factor w/ 2 levels "a","b": 2 2 1 2 1 1 1 2 1 1 ...

Learned structure arcs:
     from to 
[1,] "A"  "B"
[2,] "A"  "D"
[3,] "C"  "D"
[4,] "B"  "E"
[5,] "F"  "E"

MLE-fitted CPT for node F:

  Parameters of node F (multinomial distribution)

Conditional probability table:
      a      b 
0.5018 0.4982 
```

Node `F` has no parents in the learned structure, so its CPT is a single unconditional split — the observed proportions
0.5018/0.4982 out of 5000 rows, nothing more.

**Tool equivalent:** none for fitting a CPT over an arbitrary learned or hand-drawn structure. The nearest
fitting step under the same maximum-likelihood principle, for the one structure the product does fit automatically, is
`AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService`
([BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md), Procedure 7's structure step, with
`bn.fit(..., method = "mle")` matching [BayesianAnalysisWithBnlearn.md](../BayesianAnalysisWithBnlearn.md) Procedure 4's
use of the same estimator).

### Procedure 2: The zero-count problem and the Dirichlet-prior fix

Maximum likelihood has one structural failure mode: a parent configuration that is rare, or entirely absent, in the
training data gets its CPT column estimated from very few rows, or none. A column estimated from very few rows can
assign a state exactly zero probability purely because that state happened not to occur in a handful of observations —
and a zero in a CPT is not a small number, it is a certainty that state can never happen, which then makes every future
case in that cell impossible to score rather than merely unlikely.

`method = "bayes"` fixes this by blending the observed counts with a Dirichlet prior before dividing: instead of
estimating a cell's probability as `n_ijk / n_ij` (count in the cell over the count for that parent configuration), it
estimates `(n_ijk + alpha_ijk) / (n_ij + alpha_ij)`, where `alpha_ijk` is a small
"pseudo-count" added to every cell before any real data is seen. `bnlearn` spreads a single tuning constant,
`iss` (imaginary sample size — the total weight of the prior, expressed as if it were `iss` additional observations),
uniformly over every cell of the node's full CPT: with `r` states for the node itself and `q`
parent configurations, each cell gets `alpha_ijk = iss / (r * q)`, so `alpha_ij = iss / q` per parent configuration.
This is the BDeu (Bayesian Dirichlet equivalent, uniform) prior described in Scutari and Denis, *Bayesian Networks with
R*, and in the `bnlearn` documentation for `bn.fit`.

The effect is concrete and worth seeing on data engineered to have exactly the failure MLE has: a `Parent`
variable with three levels, two of them common (500 rows each, roughly even splits of a binary `Child`) and one rare (5
rows, all of which happen to have landed on `Child = "x"`) — the kind of rare-combination cell a real dataset produces
without anyone engineering it.

```R
library(bnlearn)

set.seed(2024)

n.a <- 500
n.b <- 500
n.c <- 5

parent <- factor(c(rep("a", n.a), rep("b", n.b), rep("c", n.c)),
                  levels = c("a", "b", "c"))

child.a <- factor(sample(c("x", "y"), n.a, replace = TRUE, prob = c(0.5, 0.5)),
                   levels = c("x", "y"))
child.b <- factor(sample(c("x", "y"), n.b, replace = TRUE, prob = c(0.4, 0.6)),
                   levels = c("x", "y"))
child.c <- factor(rep("x", n.c), levels = c("x", "y"))

data <- data.frame(Parent = parent, Child = c(as.character(child.a), as.character(child.b), as.character(child.c)))
data$Child <- factor(data$Child, levels = c("x", "y"))

cat("Observed counts, Child by Parent:\n")
print(table(Child = data$Child, Parent = data$Parent))

dag <- model2network("[Parent][Child|Parent]")

fitted.mle <- bn.fit(dag, data, method = "mle")
cat("\nMLE-fitted CPT for Child | Parent:\n")
print(fitted.mle$Child)

fitted.bayes.iss1 <- bn.fit(dag, data, method = "bayes", iss = 1)
cat("\nBayesian (Dirichlet prior, iss = 1) CPT for Child | Parent:\n")
print(fitted.bayes.iss1$Child)

fitted.bayes.iss10 <- bn.fit(dag, data, method = "bayes", iss = 10)
cat("\nBayesian (Dirichlet prior, iss = 10) CPT for Child | Parent:\n")
print(fitted.bayes.iss10$Child)
```

Console output:

```
Observed counts, Child by Parent:
     Parent
Child   a   b   c
    x 258 196   5
    y 242 304   0

MLE-fitted CPT for Child | Parent:

  Parameters of node Child (multinomial distribution)

Conditional probability table:
 
     Parent
Child     a     b     c
    x 0.516 0.392 1.000
    y 0.484 0.608 0.000

Bayesian (Dirichlet prior, iss = 1) CPT for Child | Parent:

  Parameters of node Child (multinomial distribution)

Conditional probability table:
 
     Parent
Child         a         b         c
    x 0.5159893 0.3920720 0.9687500
    y 0.4840107 0.6079280 0.0312500

Bayesian (Dirichlet prior, iss = 10) CPT for Child | Parent:

  Parameters of node Child (multinomial distribution)

Conditional probability table:
 
     Parent
Child         a         b         c
    x 0.5158940 0.3927152 0.8000000
    y 0.4841060 0.6072848 0.2000000
```

Column `c` is the payoff. MLE assigns `Child = "y"` a flat zero probability given `Parent = "c"`, because none of the
five observed rows in that cell happened to be `"y"` — any future case with `Parent = "c"` and
`Child = "y"` is now impossible under this model, not just rare. Both Bayesian fits pull that cell off zero:
with `r = 2` states of `Child` and `q = 3` parent configurations, `alpha_ijk = iss / 6`. At `iss = 1`,
`alpha_ijk ≈ 0.1667`, giving `P(y | c) = (0 + 0.1667) / (5 + 0.3333) = 0.03125` — matching the printed 0.0312500
exactly. At `iss = 10`, the same arithmetic with `alpha_ijk = 10/6 ≈ 1.6667` gives
`(0 + 1.6667) / (5 + 3.3333) = 0.2` — again matching the printed value exactly. Columns `a` and `b`, backed by hundreds
of observations each, barely move under either prior (0.5160 and 0.3921 against MLE's 0.516 and 0.392): the prior's
pseudo-count is swamped by real data as soon as there is enough of it, which is the whole design goal — `iss` matters
where data is thin and disappears where it is not. Raising `iss` from 1 to 10 visibly trades away some of that rare
cell's fidelity to its five real observations in exchange for a less extreme estimate; there is no universally correct
`iss`, only a stated tolerance for how much a handful of observations should be allowed to imply certainty.

**Tool equivalent:** none for fitting a discrete CPT under a Dirichlet/BDeu prior over an arbitrary
structure. The same problem — a parameter estimate collapsing to a degenerate value because a class/predictor
combination is thin or absent — has a shipped answer for the product's two fitted classifier-shaped families that face
it: `Jube.Service.Agent.AdvancedAnalyticsTools.NaiveBayesClassifierTrainingRunTools`
/ tool `"SubmitNaiveBayesClassifierTraining"` and
`Jube.Service.Agent.AdvancedAnalyticsTools.TreeAugmentedNaiveBayesClassifierTrainingRunTools` / tool
`"SubmitTreeAugmentedNaiveBayesClassifierTraining"` both expose a `VarianceRegularisation` constant on their training
DTOs (default `1e-9`), described in
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsNaiveBayesClassifierTrainingDto` as "the constant used in
place of a class-conditional variance that would otherwise be zero, undefined or infinite, expressed as a share of that
predictor's overall training variance." It plays the same structural role as
`iss` here — a floor that keeps a thin or degenerate class/predictor combination from collapsing a fitted distribution
to zero or infinite precision — but it floors a Gaussian variance for a continuous predictor, not a Dirichlet
pseudo-count over a discrete CPT cell, because neither product classifier fits a discrete multinomial CPT in the first
place (both fit a per-class Gaussian per predictor).

## Continuous and hybrid networks

### Procedure 3: Linear-Gaussian parameter fitting on a continuous network

When every node is continuous, `bn.fit` has nothing to count — there is no discrete state to tally. Each node is instead
fitted as a linear regression of its own value on its parents' values: `bnlearn` calls the result a linear-Gaussian
node, and its stored parameters are exactly what `lm` would report — an intercept, a coefficient per parent, and the
residual standard deviation. `method` still defaults to maximum likelihood in this case (there is no meaningful
Bayesian/Dirichlet variant for a continuous regression fit, since a Dirichlet prior is a distribution over discrete-cell
probabilities, not over regression coefficients — see Procedure 4's explicit check of this). The bundled `gaussian.test`
dataset (seven continuous variables, 5000 rows, generated to have a real linear-Gaussian structure) is used here with a
hill-climbed structure, as in Procedure 1.

```R
library(bnlearn)

data(gaussian.test)
str(gaussian.test)

dag <- hc(gaussian.test)
cat("\nLearned structure arcs:\n")
print(arcs(dag))

fitted <- bn.fit(dag, gaussian.test)
cat("\nLinear-Gaussian fit for node F (class 'bn.fit.gnode'):\n")
print(fitted$F)
cat("\nclass(fitted$F):\n")
print(class(fitted$F))
```

Console output:

```
'data.frame':	5000 obs. of  7 variables:
 $ A: num  1.113 -0.248 1.855 0.834 0.489 ...
 $ B: num  1.93 11.33 3.03 3.86 4.51 ...
 $ C: num  7.07 24.35 11.09 11.22 10 ...
 $ D: num  8.66 23.36 11.06 11.94 12.54 ...
 $ E: num  0.882 7.04 3.835 1.006 4.085 ...
 $ F: num  24.7 36.8 22 23.3 24.5 ...
 $ G: num  9.22 3.68 2.42 6.09 5.12 ...

Learned structure arcs:
     from to 
[1,] "B"  "D"
[2,] "B"  "C"
[3,] "A"  "C"
[4,] "G"  "F"
[5,] "E"  "F"
[6,] "A"  "F"
[7,] "D"  "F"

Linear-Gaussian fit for node F (class 'bn.fit.gnode'):

  Parameters of node F (Gaussian distribution)

Conditional density: F | A + D + E + G
Coefficients:
 (Intercept)             A             D             E             G  
-0.006047321   1.994853041   1.005636909   1.002577002   1.494373265  
Standard deviation of the residuals: 0.9961475 

class(fitted$F):
[1] "bn.fit.gnode"
```

`F` has four parents in the learned structure (`A`, `D`, `E`, `G`), and its fitted node is a plain multiple linear
regression of `F` on those four — same coefficients, same residual standard deviation an `lm(F ~ A +
D + E + G, data = gaussian.test)` call would report, just stored as a network node (`bn.fit.gnode`) rather than a bare
`lm` object.

**Tool equivalent:** none for a continuous node whose fitted distribution is conditioned on a graph of
other nodes. The nearest single-node analogue is a flat multiple linear regression fit over whichever continuous
predictors are supplied — `Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.LinearRegressionTools`
/ tool `"FitMultipleLinearRegression"`, backed by `AdvancedAnalyticsLinearRegressionTrainingService` — but that fits
exactly one regression, with no notion of a second node's own conditional distribution feeding into it as here, and no
discrete node able to switch the fitted mean and variance as in Procedure 4 below.

### Procedure 4: Conditional-Gaussian parameter fitting on a hybrid network

A hybrid network mixes discrete and continuous nodes, with one structural restriction `bnlearn` enforces: a discrete
node may not have a continuous parent (a continuous value cannot be "counted" into a discrete CPT cell), so every
discrete node must sit at or above the continuous ones it influences. `bn.fit`'s fitting method for this case is
`"mle-cg"` (conditional Gaussian) rather than plain `"mle"` — passing `"mle"` on a hybrid network is rejected outright,
since `"mle"` only knows how to estimate discrete CPTs. Fitting a conditional-Gaussian node whose parents include a
discrete variable works by splitting the continuous data into one group per discrete parent configuration and fitting a
separate linear (here, intercept-only)
Gaussian within each group — so a discrete parent doesn't contribute a regression coefficient the way a continuous
parent would, it selects which of several fitted Gaussians applies.

Three nodes are constructed here to show both hybrid cases in one small network: `A` (discrete, root), `B`
(continuous, with discrete parent `A` — the conditional-Gaussian case), and `C` (continuous, with continuous parent
`B` — the plain linear-Gaussian case from Procedure 3, chained beneath a conditional-Gaussian node).

```R
library(bnlearn)

set.seed(99)
n <- 2000

A <- factor(sample(c("lo", "hi"), n, replace = TRUE, prob = c(0.5, 0.5)))
B <- ifelse(A == "lo", rnorm(n, mean = 10, sd = 2), rnorm(n, mean = 20, sd = 2))
C <- 3 + 0.5 * B + rnorm(n, mean = 0, sd = 1)

hybrid <- data.frame(A = A, B = B, C = C)
str(hybrid)

dag <- model2network("[A][B|A][C|B]")

fitted <- bn.fit(dag, hybrid, method = "mle-cg")

cat("\nNode A (root, discrete) parameters:\n")
print(fitted$A)

cat("\nNode B (continuous, discrete parent A) parameters - conditional Gaussian:\n")
print(fitted$B)
cat("class(fitted$B): ")
print(class(fitted$B))

cat("\nNode C (continuous, continuous parent B) parameters - linear Gaussian:\n")
print(fitted$C)
cat("class(fitted$C): ")
print(class(fitted$C))

cat("\nOverall class of fitted network: ")
print(class(fitted))

cat("\nAttempting bayes-method fit on this hybrid network (expect it to be discrete-only):\n")
result <- tryCatch({
  bn.fit(dag, hybrid, method = "bayes", iss = 1)
}, error = function(e) {
  cat("Error: ", conditionMessage(e), "\n")
  NULL
})
```

Console output:

```
'data.frame':	2000 obs. of  3 variables:
 $ A: Factor w/ 2 levels "hi","lo": 2 1 2 2 2 2 2 1 1 1 ...
 $ B: num  10.53 22.07 9.83 9.47 9.48 ...
 $ C: num  8.07 13.4 8.2 6.91 7.32 ...

Node A (root, discrete) parameters:

  Parameters of node A (multinomial distribution)

Conditional probability table:
    hi    lo 
0.512 0.488 

Node B (continuous, discrete parent A) parameters - conditional Gaussian:

  Parameters of node B (conditional Gaussian distribution)

Conditional density: B | A
Coefficients:
                    0         1
(Intercept)  20.08005  10.08127
Standard deviation of the residuals:
       0         1  
2.020688  1.983867  
Discrete parents' configurations:
    A
0  hi
1  lo
class(fitted$B): [1] "bn.fit.cgnode"

Node C (continuous, continuous parent B) parameters - linear Gaussian:

  Parameters of node C (Gaussian distribution)

Conditional density: C | B
Coefficients:
(Intercept)            B  
  3.0044385    0.4997403  
Standard deviation of the residuals: 0.9814828 
class(fitted$C): [1] "bn.fit.gnode"

Overall class of fitted network: [1] "bn.fit"       "bn.fit.cgnet"

Attempting bayes-method fit on this hybrid network (expect it to be discrete-only):
Error:  parameter estimator 'bayes' may only be used with discrete data.
```

Node `B`'s fit (`bn.fit.cgnode`, a genuinely different class from `C`'s plain `bn.fit.gnode`) is not one regression but
two: an intercept-only Gaussian fitted separately within `A = "hi"` (mean 20.08, sd 2.02) and within `A = "lo"` (mean
10.08, sd 1.98) — recovering the generating means (20 and 10) and standard deviations (2 and 2) closely, as expected
from 2000 simulated rows. Node `C`'s fit is the Procedure 3 case again, recovering the generating intercept (3) and
slope (0.5) on `B` almost exactly, unaffected by `A` sitting two arcs upstream of it. The overall fitted object's class,
`bn.fit.cgnet`, names this a conditional-Gaussian network — `bnlearn`'s term for exactly this mixture of tabular and
regression-fitted nodes. The final call confirms directly, from the package's own error message, that
`method = "bayes"` — the Dirichlet-prior estimator from Procedure 2 — has no continuous counterpart in `bnlearn`: a
Dirichlet distribution is a prior over the cell probabilities of a discrete table, and there is no equivalent notion of
a "pseudo-count" prior over a regression coefficient in this package's Bayesian-estimation code path.

**Tool equivalent:** none. No shipped training service fits a continuous node's distribution as several
separate Gaussians switched by a categorical parent (the specific mechanism a conditional-Gaussian node uses) — the
closest available building blocks remain the same two named in Procedures 2 and 3 separately: `VarianceRegularisation`
-floored per-class Gaussian fitting in the Naive Bayes and TAN classifiers (Procedure 2) for the "one Gaussian per
discrete grouping" half, and flat least-squares regression (`LinearRegressionTools`, Procedure 3) for the "Gaussian
regressed on continuous parents" half — never both chained through one node's fitted distribution as
`bn.fit(..., method = "mle-cg")` does here.
