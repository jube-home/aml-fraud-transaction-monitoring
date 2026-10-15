---
layout: default
title: Isolation Forest Anomaly
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 18
---

# Isolation Forest Anomaly Detection

An isolation forest grows random trees that repeatedly split on a random variable at a random threshold; anomalous rows
are isolated (end up alone in a leaf) in far fewer splits than typical rows, so a row's average path length across the
forest becomes its anomaly score.

## R equivalent

*Package installed beyond the shared set: `isotree`.*

```R
library(isotree)

set.seed(1)
n <- 200
amount <- rnorm(n, mean = 50, sd = 15)
velocity <- rpois(n, 2)
normal <- data.frame(amount, velocity)

model <- isolation.forest(normal, ntrees = 100, sample_size = 256, seed = 1)

new_cases <- data.frame(amount = c(48, 900), velocity = c(2, 40))
predict(model, new_cases)
```

Console output:

```
Warning message:
In isolation.forest(normal, ntrees = 100, sample_size = 256, seed = 1) :
  'sample_size' is larger than the number of rows in 'data', will be decreased.
        1         2 
0.4032024 0.7666546 
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsIsolationForestAnomalyTrainingService` / tool
`SubmitIsolationForestAnomalyTraining` — `Kind = AnomalyDetector`, recall class `IsolationForestAnomalyRecall`.
`TreeCount` is `ntrees`; `SubsampleSize` (default 256, the value from the original Liu, Ting & Zhou 2008 paper) is
`sample_size`. Labels are optional here — the isolation splits never use them to grow the forest, exactly as above, but
when both a normal and an anomalous row fall in the validation split they are used to Platt-calibrate the raw
path-length score into a probability (`Calibrate`); left out, the raw score is reported as is, same as `predict()`
returning an uncalibrated score above.

## Code audit

This is the one family in this section with no `Accord.MachineLearning`/`Accord.Statistics` class behind it at all:
Accord.NET ships no isolation forest, so the training service's own tree-building and scoring is a from-scratch
implementation, `Jube.Engine/AdvancedAnalytics/Anomaly/IsolationForestAnomalyTrainer.cs`, with a supporting scorer in
`Jube.Engine/AdvancedAnalytics/Anomaly/IsolationForestAnomalyPipeline.cs`.

### Tree construction

`BuildNode` (`IsolationForestAnomalyTrainer.cs:318-370`) picks a uniformly random feature index and a uniformly random
split threshold between the observed minimum and maximum of that feature at the current node
(`IsolationForestAnomalyTrainer.cs:334-348`), recursing until a per-tree depth limit or a one-row node is reached. The
depth limit is `depthLimit = ceil(log2(max(subsampleSize, 2)))`
(`IsolationForestAnomalyTrainer.cs:132`) — this is exactly the "average tree height limit" Liu, Ting & Zhou (2008)
recommend (`heightLimit = ceil(log2(ψ))` for subsample size ψ), not an arbitrary cap. Each tree is built on an
independent random subsample of `min(SubsampleSize, trainingRows)` rows drawn with a per-tree derived seed,
`DeriveSeed(seed, t) = seed * 7919 + t * 104729 + 17` (`IsolationForestAnomalyTrainer.cs:139,
372-375`) — deterministic given `options.Seed` (default 1), so two training runs with the same seed and data produce the
same forest.

### Path-length-to-score formula

`PathOf` (`IsolationForestAnomalyTrainer.cs:160-173`) walks a row down a tree counting real splits (`depth`), then at
the leaf it reaches adds `IsolationForestAnomalyPipeline.AveragePath(tree.Size[node])` — the expected additional path
length for the rows still bundled in that leaf. `AveragePath(n)`
(`IsolationForestAnomalyPipeline.cs:43-52`) is:

```
n <= 1  -> 0
n == 2  -> 1
n  > 2  -> 2 * (ln(n - 1) + 0.5772156649015329) - 2 * (n - 1) / n
```

This is an exact implementation of Liu, Ting & Zhou's `c(n) = 2H(n-1) - 2(n-1)/n`, where `H(i)` is the harmonic number
approximated by `ln(i) + γ` with Euler-Mascheroni constant `γ ≈ 0.5772156649015329` — the literal constant appears in
the source. It is not an approximation of the paper's normalisation; it is the paper's formula, including its `n <= 2`
special cases. The final score,
`Math.Pow(2d, -(total / builtTrees.Length) / normaliser)` (`IsolationForestAnomalyTrainer.cs:182`, mirrored in
`IsolationForestAnomalyPipeline.cs:79-80` for recall), is the textbook `s(x, n) = 2^(-E(h(x)) / c(n))`, with
`normaliser = max(AveragePath(subsampleSize), 1e-9)` computed once from the *subsample* size actually used, not the full
training row count (`IsolationForestAnomalyTrainer.cs:158`).

### Subsampling size and tree count defaults

`TreeCount` defaults to 100 and `SubsampleSize` defaults to 256 (`IsolationForestAnomalyOptions.cs:21-22`) — both match
Liu, Ting & Zhou's own recommended defaults (`t = 100`, `ψ = 256`) rather than an arbitrarily chosen pair.
`SubsampleSize` is clamped to the actual training row count if fewer rows are available
(`IsolationForestAnomalyTrainer.cs:131`).

### Other findings

* Missing values are median-imputed per variable, fit on the training split only
  (`IsolationForestAnomalyTrainer.cs:102`, `Imputer.Fit(column, ImputationStrategy.Median)`); a variable that is
  constant in the training split is flagged with an explicit warning (`IsolationForestAnomalyTrainer.cs:109-115`), and
  the split-search loop already skips a feature whose sampled rows share the same value (`high <= low` guard,
  `IsolationForestAnomalyTrainer.cs:345-346`), so a constant column can never isolate a row even before the warning is
  raised.
* Calibration is Platt scaling of the raw path-length score against validation-split labels, only attempted when labels
  exist and both classes appear in that split (`IsolationForestAnomalyTrainer.cs:201-235`); otherwise the raw
  `2^(-h/c(n))` score is reported directly, exactly like the uncalibrated `predict()` output in the R snippet above.
* Train, validation and test rows are drawn from one disjoint split (`DataSplit.Split`,
  `IsolationForestAnomalyTrainer.cs:41-56`) with any leftover rows folded into the training split rather than discarded
  (`IsolationForestAnomalyTrainer.cs:46-56`) — there is no row overlap between the three sets for this family.
