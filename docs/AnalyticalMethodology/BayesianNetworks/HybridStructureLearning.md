---
layout: default
title: Hybrid Structure Learning
nav_order: 4
parent: Bayesian Networks
grand_parent: Analytical Methodology
---

# Hybrid Structure Learning

Pure score-based search (the previous section) considers, at every step of `hc` or `tabu`, every legal single-arc
addition between every pair of nodes not already connected — on a network with many variables that candidate-move list
grows with the square of the node count, and it has to be rescored at every iteration. The hybrid family cuts that down
in two phases: first restrict, using a constraint-based conditional-independence procedure to work out, per node, a much
smaller candidate set of variables that could plausibly be its parents or children; then maximise, running an ordinary
score-based search but only ever allowed to propose arcs between a node and its own candidate set. `bnlearn` implements
the restriction phase as `mmpc` (Max-Min Parents and Children), and two ways of pairing a restriction with a search:
`mmhc`
(always MMPC restricting, hill-climbing maximising) and `rsmax2`, a general framework that takes the restriction and
maximisation algorithms as named arguments so other pairings are possible.

`mmpc` finds each node's candidate parent-and-children set by testing, for every other variable, whether it is
independent of the target given some subset of the variables already accepted as candidates — accepting a variable as a
candidate only if no such conditioning subset makes it independent (the "max-min" in the name refers to how it picks
which conditioning subset to test next: the one minimising the association, maximised over the candidates considered so
far). This produces an undirected skeleton, not a DAG — a candidate set per node, with no arc directions yet. `mmhc` and
`rsmax2` then run hill-climbing exactly as in the previous section, except every proposed single-arc addition is checked
against that skeleton first and rejected outright if it isn't one of the candidate edges MMPC already found.

## Why restricting first is faster

The saving is structural, not incidental: an unrestricted hill-climbing step over `n` variables considers on the order
of `n`-squared candidate arc additions at every iteration, because any node could in principle be any other node's
parent until the search rules it out by trial. Restricting first to MMPC's candidate sets means the maximisation phase
only ever has to consider arcs already known to be plausible, so both the number of candidate moves per iteration and,
in practice, the number of iterations needed to converge, shrink together. That saving compounds with network size: it
is barely noticeable on the six-variable
`learning.test` network used in the previous section, and become worth measuring once there are dozens of variables
competing for parent slots.

### Procedure: MMPC, MMHC and RSMAX2 against hill-climbing on a 37-variable network

`alarm` (20000 rows, 37 discrete variables — a long-standing benchmark network modelling patient monitoring in an
intensive-care unit) is large enough for the restriction phase's saving to actually show up. `hc` is re-run here
specifically for this comparison — it is a fresh run on `alarm`, not a reuse of the previous section's `hc` result,
which was fitted on the unrelated six-variable `learning.test` data.

```R
library(bnlearn)
data(alarm)

set.seed(1)
t.hc <- system.time(hc.fit <- hc(alarm))
cat(sprintf("hc: %.2f sec, %d arcs\n", t.hc["elapsed"], nrow(arcs(hc.fit))))

set.seed(1)
t.mmpc <- system.time(mmpc.fit <- mmpc(alarm))
cat(sprintf("mmpc: %.2f sec, %d candidate-set arcs (%d undirected edges)\n",
            t.mmpc["elapsed"], nrow(arcs(mmpc.fit)), nrow(arcs(mmpc.fit)) / 2))

set.seed(1)
t.mmhc <- system.time(mmhc.fit <- mmhc(alarm))
cat(sprintf("mmhc: %.2f sec, %d arcs\n", t.mmhc["elapsed"], nrow(arcs(mmhc.fit))))

set.seed(1)
t.rsmax2 <- system.time(rsmax2.fit <- rsmax2(alarm, restrict = "mmpc", maximize = "hc"))
cat(sprintf("rsmax2 (restrict = mmpc, maximize = hc): %.2f sec, %d arcs\n",
            t.rsmax2["elapsed"], nrow(arcs(rsmax2.fit))))

cat("\nmmhc arc set identical to rsmax2(mmpc, hc):", setequal(
  apply(arcs(mmhc.fit), 1, paste, collapse = "->"),
  apply(arcs(rsmax2.fit), 1, paste, collapse = "->")), "\n")

skel.edges <- apply(skeleton(mmpc.fit)$arcs, 1, function(r) paste(sort(r), collapse = "-"))
skel.edges <- unique(skel.edges)
mmhc.edges <- apply(mmhc.fit$arcs, 1, function(r) paste(sort(r), collapse = "-"))
cat("Every mmhc arc traces back to an mmpc candidate edge:",
    all(mmhc.edges %in% skel.edges), "\n")
cat("mmpc candidate edges not used by mmhc's maximise phase:",
    sum(!skel.edges %in% mmhc.edges), "of", length(skel.edges), "\n")

cat("\nStructural agreement, mmhc vs hc (compare() on CPDAGs, tp/fp/fn):\n")
print(compare(cpdag(hc.fit), cpdag(mmhc.fit)))
```

Console output:

```
hc: 0.63 sec, 53 arcs
mmpc: 0.35 sec, 64 candidate-set arcs (32 undirected edges)
mmhc: 0.35 sec, 32 arcs
rsmax2 (restrict = mmpc, maximize = hc): 0.37 sec, 32 arcs

mmhc arc set identical to rsmax2(mmpc, hc): TRUE 
Every mmhc arc traces back to an mmpc candidate edge: TRUE 
mmpc candidate edges not used by mmhc's maximise phase: 0 of 32 

Structural agreement, mmhc vs hc (compare() on CPDAGs, tp/fp/fn):
$tp
[1] 19

$fp
[1] 13

$fn
[1] 34
```

`mmhc` and `rsmax2` with `restrict = "mmpc"` and `maximize = "hc"` are, as the identical arc sets confirm, the same
algorithm reached two ways — `rsmax2` is the general form and `mmhc` is that specific, most common pairing pre-wired as
its own function. Both ran faster than unrestricted `hc` here (roughly half the elapsed time, though wall-clock timings
vary run to run and matter more as a direction than an exact multiplier at 37 variables), and both returned a sparser,
32-arc structure against `hc`'s 53. Every one of those 32 arcs traces back to one of MMPC's 32 candidate edges, and
MMPC's restriction phase found no candidate edge that hill-climbing's maximisation phase then declined to use — on this
network, the entire hybrid pipeline reduces to MMPC finding the skeleton and hill-climbing doing nothing but orient it,
adding no further arcs of its own.

That sparser result is not simply "the same answer, faster." `compare()`, which counts true-positive, false-positive and
false-negative arcs between two CPDAGs, shows `mmhc` agreeing with unrestricted `hc` on only 19 of the arcs each
contains (13 arcs `hc` has that `mmhc` lacks entries for as false positives from
`mmhc`'s perspective, 34 the other way — `hc`'s 53-arc structure contains many more arcs than `mmhc`'s 32-arc one). No
true ALARM generating network is bundled with the package or reachable without internet access from this container, so
this is a structural-agreement comparison between two algorithms' output on the same data, not a recovery-accuracy
comparison against a known ground truth; it demonstrates that restricting the candidate sets before searching can
converge on a genuinely different structure, not only a faster route to the same one that unrestricted `hc` would find.
The mechanism is exactly the restriction phase doing its job as designed: any arc whose endpoints MMPC's
conditional-independence tests ruled out as candidates can never be proposed during maximisation, no matter how much it
would have improved the score, where unrestricted `hc` is free to consider it regardless.

**Tool equivalent:** none for producing a DAG, restricted or otherwise — this is out of scope for the same
reason as pure score-based search (see [ScoreBasedStructureLearning.md](ScoreBasedStructureLearning.md)). The nearest
conceptual analogue to MMPC's restriction phase specifically is
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureSelectionTools` / tool
`"RankFeaturesByMutualInformation"`, which screens candidate predictors by information-theoretic dependence with one
designated target before a downstream model is fit on the survivors — the same "narrow the candidate set first, model
later" shape MMPC applies simultaneously to every node's candidate parents and children. It is a materially narrower
tool than MMPC in every direction that matters here: one fixed target rather than all-pairs screening, no conditioning
on other candidates (so no genuine conditional-independence testing, only marginal dependence against the target), and a
ranked feature list rather than an oriented graph. `FilterCorrelatedFeatures` and `PartialCorrelation` in the same tool
group cover, respectively, dropping one of a mutually correlated pair of features and testing a pairwise association
after conditioning on a third variable — closer in spirit to a single MMPC conditional-independence test, but again
applied ad hoc between named variables rather than run automatically over every node's candidate set the way `mmpc`
does.
