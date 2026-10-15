---
layout: default
title: Bayesian Analysis with bnlearn
nav_order: 18
parent: Analytical Methodology
---

# Bayesian Analysis with bnlearn

This page is a direct translation of the original R training guide's nine Bayesian-network procedures, which built
networks entirely inside Norsys Netica, a commercial modelling GUI with no R involvement at all: stamping a node per
variable, typing in Yes/No states, dragging links from an independent variable to a dependent one to declare "this
causes that," typing subjective belief probabilities into each node's table, compiling the network, then setting a
node's state and watching the rest update by Bayesian inference. Later procedures added Netica's continuous-variable
discretisation shorthand (`[Begin,End] /
Bin`), learning a node's probabilities from a case file instead of typing them in, testing classification accuracy
against that file, inferring nodes automatically from a file's columns, and finally letting Netica learn a
Tree-Augmented Naive Bayes (TAN) structure automatically instead of drawing links by hand.

None of that GUI exists here. This page replaces it entirely with `bnlearn`, an R package for Bayesian network structure
learning, parameter fitting and inference, already installed in the shared library. Every Netica procedure below has a
working R equivalent: `model2network`/`custom.fit` for hand-drawn structure and subjective probabilities, `discretize`
for continuous-variable binning, `hc`/`tabu` for automatic structure learning, `tree.bayes` for TAN structure learning
specifically, `bn.fit` for learning probabilities from data, `cpquery` for inference, and `predict` against held-out
data for classification accuracy.

For a comprehensive treatment of Bayesian network methodology beyond these nine procedures — graphical foundations, the
constraint-based/score-based/hybrid structure-learning families, parameter learning, exact and approximate inference,
structure validation and the conditioning-versus-intervention distinction — see
[Bayesian Networks](BayesianNetworks/index.md).

## Why this module is prescriptive, not just a classifier

Every other module in this guide ends at a confusion matrix: fit a model, score held-out rows, report accuracy. A
Bayesian network can do that too (Procedure 5 below), but the reason Bayesian networks are worth their construction cost
is a different kind of question: not just "is this case fraud," but "if we changed one thing about this case, what would
we expect to see change downstream, and by how much." A learned or hand-specified joint distribution over every node
lets you set evidence anywhere in the network — cause or effect, upstream or downstream — and read off how every other
node's distribution moves in response. Procedures 2 and 8 below are built around that: setting a node's state and
reading the resulting shift in another node's distribution, which is the practical, decision-supporting reason to reach
for a Bayesian network instead of a plain classifier.

### Procedure 1: Build a network by hand and enter subjective probabilities

This collapses the original's Procedures 1 to 4 (new canvas, add dependent/independent variable nodes, set Yes/No
states, link the independent variable to the dependent one as cause to consequence, then type subjective probabilities
into the consequence's table) into their R equivalents. `model2network` declares the causal structure as text instead of
drawing it, and `custom.fit` is bnlearn's direct equivalent of typing numbers into Netica's conditional probability
table dialog — it lets you hand-author every conditional probability table with no data at all, exactly the "belief, not
data" case the original chapter opened with. The scenario re-uses the original's own numbers: `P(Default) = 9%` when a
customer has more than three bank credit products, `3%` otherwise.

```R
library(bnlearn)

dag <- model2network("[CountGT3][Default|CountGT3]")

count.cpt <- matrix(c(0.5, 0.5), ncol = 2, dimnames = list(NULL, c("Yes", "No")))

default.cpt <- matrix(
  c(0.09, 0.91,
    0.03, 0.97),
  ncol = 2,
  dimnames = list(Default = c("Yes", "No"), CountGT3 = c("Yes", "No"))
)

fitted <- custom.fit(dag, dist = list(CountGT3 = count.cpt, Default = default.cpt))

print(fitted)
```

Console output:

```
  Bayesian network parameters

  Parameters of node CountGT3 (multinomial distribution)

Conditional probability table:
 
Yes  No 
0.5 0.5 

  Parameters of node Default (multinomial distribution)

Conditional probability table:
 
       CountGT3
Default  Yes   No
    Yes 0.09 0.03
    No  0.91 0.97
```

**Tool equivalent:** none — no product tool lets an analyst hand-author a conditional probability table for
an arbitrary node with no training data behind it. The nearest concept is not manual CPT entry at all: it is a
regularisation prior blended in during automatic fitting, for example the
`Smoothing`, `VarianceRegularisation` and `Ridge` constants on
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingDto`, which keep
a data-fitted distribution from collapsing rather than letting an analyst substitute belief for data outright.

### Procedure 2: Manually setting evidence to predict and explain

The original's Procedure 5 hovered over a node's state to "set a finding," producing a forward prediction from the
consequence's table, then reset all findings and set the consequence's state instead, to run the inference backwards and
see which cause was most implicated — Netica's explanatory use of Bayesian inference. `cpquery` does both directions
over the same fitted network: conditioning on the cause estimates the consequence (forward, prediction), and
conditioning on the consequence estimates the cause (backward, explanation), both by weighted logic sampling.

```R
library(bnlearn)

dag <- model2network("[CountGT3][Default|CountGT3]")

count.cpt <- matrix(c(0.5, 0.5), ncol = 2, dimnames = list(NULL, c("Yes", "No")))

default.cpt <- matrix(
  c(0.09, 0.91,
    0.03, 0.97),
  ncol = 2,
  dimnames = list(Default = c("Yes", "No"), CountGT3 = c("Yes", "No"))
)

fitted <- custom.fit(dag, dist = list(CountGT3 = count.cpt, Default = default.cpt))

set.seed(1)
p.default.given.count.yes <- cpquery(fitted,
  event = (Default == "Yes"),
  evidence = (CountGT3 == "Yes"),
  n = 1e6)
cat(sprintf("Forward prediction P(Default = Yes | CountGT3 = Yes) = %.4f\n", p.default.given.count.yes))

set.seed(1)
p.count.given.default.yes <- cpquery(fitted,
  event = (CountGT3 == "Yes"),
  evidence = (Default == "Yes"),
  n = 1e6)
cat(sprintf("Explanatory inference P(CountGT3 = Yes | Default = Yes) = %.4f\n", p.count.given.default.yes))
```

Console output:

```
Forward prediction P(Default = Yes | CountGT3 = Yes) = 0.0889
Explanatory inference P(CountGT3 = Yes | Default = Yes) = 0.7477
```

The forward result recovers the entered 9% directly. The backward result is the genuinely useful one: given that a
customer has defaulted, there is a 75% chance they had more than three bank credit products — the explanatory value
Netica's chapter called out, obtained here by exact Bayes' rule rather than a manual
"reset findings, click the other node" GUI dance.

**Tool equivalent:** none for arbitrary bidirectional evidence on a general network. A trained classifier
such as the TAN model (see Procedure 7) only ever answers the forward question — the probability of its one designated
class variable given a full row of predictor values — and has no mechanism to set evidence on the class and read off a
posterior over an individual predictor.

### Procedure 3: Discretising a continuous variable

The original's Procedure 6 replaced Netica's `Discrete` node type with `Continuous`, then typed shorthand such as
`[0,100] / 5` into a discretisation box to cut a continuous variable into a fixed number of equal- width bins (with
exponential-bin and natural-logarithm-bin variants also on offer). `bnlearn::discretize`
does the same job on a whole data frame at once, with `method = "interval"` for Netica's fixed-width bins and
`method = "quantile"` for equal-frequency bins (the discretisation Netica's shorthand did not offer directly). The
`marks` dataset bundled with bnlearn (88 students' exam marks across five subjects) is used here since it is continuous
throughout.

```R
library(bnlearn)

data(marks)
str(marks)
summary(marks$MECH)

marks.discrete <- discretize(marks, method = "interval", breaks = 3)
str(marks.discrete)
table(marks.discrete$MECH)

marks.quantile <- discretize(marks, method = "quantile", breaks = 3)
table(marks.quantile$MECH)
```

Console output:

```
'data.frame':	88 obs. of  5 variables:
 $ MECH: num  77 63 75 55 63 53 51 59 62 64 ...
 $ VECT: num  82 78 73 72 63 61 67 70 60 72 ...
 $ ALG : num  67 80 71 63 65 72 65 68 58 60 ...
 $ ANL : num  67 70 66 70 70 64 65 62 62 62 ...
 $ STAT: num  81 81 81 68 63 73 68 56 70 45 ...
   Min. 1st Qu.  Median    Mean 3rd Qu.    Max. 
   0.00   30.00   41.50   38.95   49.25   77.00 
'data.frame':	88 obs. of  5 variables:
 $ MECH: Factor w/ 3 levels "[0,25.6667]",..: 3 3 3 3 3 3 2 3 3 3 ...
 $ VECT: Factor w/ 3 levels "[9,33.3333]",..: 3 3 3 3 3 3 3 3 3 3 ...
 $ ALG : Factor w/ 3 levels "[15,36.6667]",..: 3 3 3 3 3 3 3 3 2 3 ...
 $ ANL : Factor w/ 3 levels "[9,29.3333]",..: 3 3 3 3 3 3 3 3 3 3 ...
 $ STAT: Factor w/ 3 levels "[9,33]","(33,57]",..: 3 3 3 3 3 3 3 2 3 2 ...
 - attr(*, "cutpoints")=List of 5
  ..$ MECH: num [1:4] 0 25.7 51.3 77
  ..$ VECT: num [1:4] 9 33.3 57.7 82
  ..$ ALG : num [1:4] 15 36.7 58.3 80
  ..$ ANL : num [1:4] 9 29.3 49.7 70
  ..$ STAT: num [1:4] 9 33 57 81

      [0,25.6667] (25.6667,51.3333]      (51.3333,77] 
               20                48                20 

 [0,34] (34,48] (48,77] 
     31      30      27 
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.BinningTools`
/ tool `"DiscretiseEqualWidth"` for Netica's fixed-bin shorthand, and tool `"DiscretiseEqualFrequency"` for the quantile
variant — both return the interior edges, each row's bin index, and a bin table in one call, without needing a node's
`Continuous`/`Discrete` toggle at all. Note that this step is not a prerequisite for the product's own TAN classifier
(Procedure 7): it fits continuous predictors directly.

### Procedure 4: Learning node probabilities from data

The original's Procedure 7 replaced the subjective probabilities entered in Procedure 4 with frequentist ones learned
from a case file (`CreditRisk.csv`), calling out that "an interesting exercise is to observe the difference between
subjective and frequentist probabilities." The same comparison is made here without an external file: `rbn` draws a
simulated sample from the hand-specified subjective network built in Procedure 1, and `bn.fit` re-learns the conditional
probability table by maximum likelihood from that sample, over the same fixed structure.

```R
library(bnlearn)

dag <- model2network("[CountGT3][Default|CountGT3]")

count.cpt <- matrix(c(0.5, 0.5), ncol = 2, dimnames = list(NULL, c("Yes", "No")))
default.cpt <- matrix(
  c(0.09, 0.91,
    0.03, 0.97),
  ncol = 2,
  dimnames = list(Default = c("Yes", "No"), CountGT3 = c("Yes", "No"))
)
subjective <- custom.fit(dag, dist = list(CountGT3 = count.cpt, Default = default.cpt))

set.seed(42)
sampled <- rbn(subjective, n = 20000)

learned <- bn.fit(dag, sampled, method = "mle")

cat("Subjective (belief-based) Default | CountGT3:\n")
print(subjective$Default)

cat("\nLearned (frequentist, from 20000 sampled cases) Default | CountGT3:\n")
print(learned$Default)
```

Console output:

```
Subjective (belief-based) Default | CountGT3:

  Parameters of node Default (multinomial distribution)

Conditional probability table:
 
       CountGT3
Default  Yes   No
    Yes 0.09 0.03
    No  0.91 0.97

Learned (frequentist, from 20000 sampled cases) Default | CountGT3:

  Parameters of node Default (multinomial distribution)

Conditional probability table:
 
       CountGT3
Default        Yes         No
    Yes 0.09220922 0.03349665
    No  0.90779078 0.96650335
```

The learned table recovers the subjective one closely (0.0922 against 0.09, 0.0335 against 0.03), as expected from
20,000 simulated cases — the same reconciliation exercise the original chapter pointed at, done here against a
reproducible simulated sample rather than a training-course data file.

**Tool equivalent:** none for fitting a conditional probability table over an arbitrary, analyst-specified
structure. The nearest shipped analogue is training a classifier whose structure is fixed and automatic rather than
hand-drawn: see Procedure 7's TAN training service, which fits its conditional distributions from data by the same
maximum-likelihood principle, but over a Chow-Liu tree it discovers itself, not a graph an analyst drew node by node.

### Procedure 5: Testing classification accuracy of a Bayesian network

The original's Procedure 8 highlighted every node, ran Netica's "Test with Cases" against the same case file used for
training, and read off a confusion matrix and error rate for the dependent variable. Here, a network is fitted as a
classifier over bnlearn's built-in `asia` dataset (5000 rows, 8 binary variables — a long-standing synthetic
Bayesian-network teaching set about smoking, tuberculosis, lung cancer, bronchitis and their symptoms), using the TAN
structure from Procedure 7 fitted on an 80% training split and tested by
`predict` against the held-out 20%.

```R
library(bnlearn)

data(asia)

set.seed(7)
n <- nrow(asia)
train.idx <- sample(seq_len(n), size = round(0.8 * n))
train <- asia[train.idx, ]
test <- asia[-train.idx, ]

tan.structure <- tree.bayes(train, training = "S")
cat("TAN structure (class = S, smoking) arcs:\n")
print(arcs(tan.structure))

tan.fitted <- bn.fit(tan.structure, train, method = "bayes", iss = 1)

predicted <- predict(tan.fitted, data = test)

confusion <- table(Predicted = predicted, Actual = test$S)
cat("\nConfusion matrix (rows = predicted, cols = actual):\n")
print(confusion)

accuracy <- sum(diag(confusion)) / sum(confusion)
cat(sprintf("\nHeld-out classification accuracy = %.4f\n", accuracy))
```

Console output:

```
TAN structure (class = S, smoking) arcs:
      from to 
 [1,] "S"  "A"
 [2,] "S"  "T"
 [3,] "S"  "L"
 [4,] "S"  "B"
 [5,] "S"  "E"
 [6,] "S"  "X"
 [7,] "S"  "D"
 [8,] "A"  "T"
 [9,] "T"  "E"
[10,] "E"  "L"
[11,] "D"  "B"
[12,] "E"  "X"
[13,] "E"  "D"

Confusion matrix (rows = predicted, cols = actual):
         Actual
Predicted  no yes
      no  344 127
      yes 146 383

Held-out classification accuracy = 0.7270
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelSelectionTools`
/ tool `"CompareModels"` with `EvaluationFamily.TreeAugmentedNaiveBayes` selected — it runs the TAN family (and up to
eleven others) on identical stratified cross-validation folds and reports held-out ROC AUC, average precision, log loss,
Brier score and F1 per family, rather than a single train/test confusion matrix. It is a more rigorous held-out test
than the original's "score once against the training file," at the cost of not printing a literal confusion-matrix
table.

### Procedure 6: Learning network structure automatically from data

The original's Procedure 9 used Netica's "Add Case File Nodes" to infer nodes from a data file's columns — laborious to
do by hand at any scale — and set up Procedure 11's automatic linking as the real point: with nodes in place, the
network's arcs themselves should be learned rather than drawn one by one. `hc`
(hill-climbing, score-based on BIC by default) and `tabu` (tabu search, a hill-climbing variant that tolerates
temporarily worse moves to escape local optima) both learn a DAG's structure directly from a data frame — R's data frame
columns already are the "inferred nodes," with no separate node-creation step needed at all.

```R
library(bnlearn)

data(asia)
str(asia)

hc.fit <- hc(asia)
cat("\nHill-climbing (score-based, BIC) learned arcs:\n")
print(arcs(hc.fit))

tabu.fit <- tabu(asia)
cat("\nTabu search learned arcs:\n")
print(arcs(tabu.fit))
```

Console output:

```
'data.frame':	5000 obs. of  8 variables:
 $ A: Factor w/ 2 levels "no","yes": 1 1 1 1 1 1 1 1 1 1 ...
 $ S: Factor w/ 2 levels "no","yes": 2 2 1 1 1 2 1 2 2 2 ...
 $ T: Factor w/ 2 levels "no","yes": 1 1 2 1 1 1 1 1 1 1 ...
 $ L: Factor w/ 2 levels "no","yes": 1 1 1 1 1 1 1 1 1 1 ...
 $ B: Factor w/ 2 levels "no","yes": 2 1 1 2 1 1 1 2 2 2 ...
 $ E: Factor w/ 2 levels "no","yes": 1 1 2 1 1 1 1 1 1 1 ...
 $ X: Factor w/ 2 levels "no","yes": 1 1 2 1 1 1 1 1 1 1 ...
 $ D: Factor w/ 2 levels "no","yes": 2 1 2 2 2 2 1 2 2 2 ...

Hill-climbing (score-based, BIC) learned arcs:
     from to 
[1,] "B"  "D"
[2,] "L"  "E"
[3,] "E"  "X"
[4,] "S"  "B"
[5,] "T"  "E"
[6,] "E"  "D"
[7,] "S"  "L"

Tabu search learned arcs:
     from to 
[1,] "B"  "D"
[2,] "L"  "E"
[3,] "E"  "X"
[4,] "S"  "B"
[5,] "T"  "E"
[6,] "E"  "D"
[7,] "S"  "L"
```

Both algorithms recover the same seven arcs here, which closely track the network's known generative structure (smoking
causing bronchitis and lung cancer, lung cancer and tuberculosis both causing "either,"
and "either" causing an abnormal X-ray and dyspnoea) — general-purpose structure search finding, without any manual
dragging of links, essentially the causal skeleton the original chapter built by hand in its Procedures 3 and 11.

**Tool equivalent:** none. General Bayesian-network structure search over an arbitrary set of nodes was
researched and explicitly dropped (`docs/AnalyticalMethodology/Reference/ModelCatalogue.md`, "General Bayesian network with structure
search... Drop: KDB, TAN, AODE and HNB give most of the benefit"). Structure learning is only shipped in the one
classifier-shaped form that survived that research: TAN, over a fixed class variable, in Procedure 7.

### Procedure 7: Learning TAN structure to link nodes automatically

The original's Procedure 11 (misnumbered in the source but the guide's final procedure) selected a dependent variable
node, then had Netica learn a Tree-Augmented Naive Bayesian network structure from a case file: every node linked to or
from the dependent variable first, then augmented with additional links between independent variables where the data
supported it. `bnlearn::tree.bayes` is that exact algorithm — its `training` argument names the class node, and it
builds a maximum-weight spanning tree over the remaining variables' pairwise conditional mutual information given that
class, exactly Netica's "naive structure, then augment" description.

```R
library(bnlearn)

data(asia)

tan.structure <- tree.bayes(asia, training = "S")
print(class(tan.structure))
print(arcs(tan.structure))
```

Console output (structure, as already shown fitted on the training split in Procedure 5):

```
[1] "bn.tan" "bn"    
      from to 
 [1,] "S"  "A"
 [2,] "S"  "T"
 [3,] "S"  "L"
 [4,] "S"  "B"
 [5,] "S"  "E"
 [6,] "S"  "X"
 [7,] "S"  "D"
 [8,] "A"  "T"
 [9,] "T"  "E"
[10,] "E"  "L"
[11,] "D"  "B"
[12,] "E"  "X"
[13,] "E"  "D"
```

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.TreeAugmentedNaiveBayesClassifierTrainingRunTools`
/ tool `"SubmitTreeAugmentedNaiveBayesClassifierTraining"` — the closest real overlap in this whole module with a
shipped product capability. Its structure step is the same Chow-Liu maximum-spanning-tree algorithm over pairwise
conditional mutual information given the class that `tree.bayes` runs. Two differences are worth stating plainly rather
than glossing over: it fits every predictor as Gaussian given the class (and linearly dependent on its one tree-parent
predictor when it has one), so continuous predictors are used directly with no discretisation step at all (contrast
Procedure 3); and it is a fixed classifier shape — one designated class variable with every other variable as a
leaf-or-chained predictor beneath it — not a general multi-node network where any node can be queried or intervened on,
which is what the rest of this module's original Netica procedures actually did.

### Procedure 8: Prescriptive worked example — intervening on one node and reading a downstream shift

This is the practical payoff the rest of the module builds towards, and has no single procedure in the original guide to
map onto — it is assembled here from the same tools already validated above. A network is fitted on the full `asia` data
using the structure learned in Procedure 6, then `cpquery` is used to compare the distribution of `D` (dyspnoea, a node
two arcs downstream of `S` in the learned structure, reached via
`S -> L -> E -> D` and `S -> B -> D`) under two states of `S` (smoking). Because `S` has no parents in the learned
structure — it is a root cause, not a downstream effect of anything else in this network — conditioning on it and
intervening on it (in Pearl's do-calculus sense) coincide here; that equivalence does not hold in general for a node
with its own parents, where conditioning also picks up information flowing backward from its causes.

```R
library(bnlearn)

data(asia)

structure <- hc(asia)
cat("Learned structure, parents of D (dyspnoea):\n")
print(parents(structure, "D"))
cat("S (smoking) has parents: ")
print(parents(structure, "S"))

fitted <- bn.fit(structure, asia, method = "bayes", iss = 1)

set.seed(11)
p.d.given.smoker <- cpquery(fitted,
  event = (D == "yes"),
  evidence = (S == "yes"),
  n = 1e6)

set.seed(11)
p.d.given.nonsmoker <- cpquery(fitted,
  event = (D == "yes"),
  evidence = (S == "no"),
  n = 1e6)

cat(sprintf("\nP(D = yes | S = yes) = %.4f\n", p.d.given.smoker))
cat(sprintf("P(D = yes | S = no)  = %.4f\n", p.d.given.nonsmoker))
cat(sprintf("Shift in downstream node D from intervening on S = %.4f\n",
            p.d.given.smoker - p.d.given.nonsmoker))
```

Console output:

```
Learned structure, parents of D (dyspnoea):
[1] "B" "E"
S (smoking) has parents: character(0)

P(D = yes | S = yes) = 0.6203
P(D = yes | S = no)  = 0.3151
Shift in downstream node D from intervening on S = 0.3053
```

This is the prescriptive answer a plain classifier cannot give: not "does this case have dyspnoea" but
"if this population's smoking status changed, the probability of downstream dyspnoea would move by about 0.31" — a
quantified, network-wide consequence of intervening on one node, read straight off the joint distribution without
retraining anything.

**Tool equivalent:** none. A trained classifier (TAN or otherwise) returns a probability for its one
designated class variable given a row of predictor values; it has no notion of an internal node two arcs downstream of
an intervention, because it has no internal nodes — its input variables are direct predictors, not endpoints of a graph
that can itself be queried. This kind of "what happens downstream if I change X" reasoning belongs entirely to a fitted
joint distribution over a real multi-node network, which is exactly the capability recorded as a research Drop.

## Can AdvancedAnalytics recall a bnlearn-style model?

Plainly: not today, for the general case this module's original Netica procedures actually walked through.

`docs/AnalyticalMethodology/Reference/ModelCatalogue.md` recorded two related but distinct Bayesian-network capabilities
considered for `AdvancedAnalytics`, with two different outcomes:

- **General Bayesian network with structure search** — an arbitrary DAG over any set of nodes, hand-drawn or learned,
  queryable at any node, exactly what Procedures 1, 2, 4, 6 and 8 above do with `model2network`,
  `custom.fit`, `hc`/`tabu` and `cpquery` — was considered and explicitly dropped. The catalogue's own reasoning:
  "KDB, TAN, AODE and HNB give most of the benefit" of Bayesian-network-style reasoning for the classification use case,
  at a fraction of the engineering cost of a general structure-search-plus-inference engine. There is no cheap recall
  path for this shape of model, and none is planned.
- **Tree-Augmented Naive Bayes (TAN)** was built as the classifier-shaped equivalent — a Chow-Liu
  maximum-spanning-tree structure fixed around one designated class variable, with a Gaussian conditional per predictor
  per class, genuinely new code, real and shipped in
  `AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService`. It genuinely overlaps
  `tree.bayes` (Procedure 7) — same structure-learning algorithm — but only for the classification shape:
  one class variable, every other variable a predictor beneath it, no general multi-node network, no arbitrary-node
  querying or intervention.

So: an arbitrary bnlearn DAG with hand-specified subjective priors, of the kind most of this module's original Netica
procedures actually built, has no cheap `AdvancedAnalytics` recall path today. TAN covers the one shape of
Bayesian-network reasoning the product actually needed — a classifier that captures the single strongest pairwise
predictor interaction per feature — but not a general causal network with evidence settable on any node, predicting or
explaining in either direction, of the kind Procedures 1, 2, 4, 6 and 8 above demonstrate in R.
