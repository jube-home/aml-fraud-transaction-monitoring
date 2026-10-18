---
layout: default
title: Mean Shift Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 17
---

# Mean Shift Clusterer

## Procedure: mean shift

Mean shift finds clusters by repeatedly shifting each point towards the mean of its neighbours within a bandwidth, until
points converge onto the same mode; the number of clusters is discovered from the data rather than fixed in advance by a
`Clusters`/`Components` argument.

*Package installed beyond the shared set: `meanShiftR`.*

```R
library(meanShiftR)

set.seed(1)
n <- 90
x <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 40, 2), rnorm(n / 3, 70, 2))
y <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 45, 2), rnorm(n / 3, 15, 2))
merchants <- as.matrix(scale(data.frame(x, y)))

model <- meanShift(merchants, bandwidth = c(0.7, 0.7))
print(table(model$assignment))
print(model$value[!duplicated(model$assignment), ])
```

Console output:

```

 1  2  3 
30 30 30 
             [,1]       [,2]
[1,] -1.207934290 -0.8594380
[2,]  0.008710314  1.3635883
[3,]  1.199488686 -0.5065909
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsMeanShiftClustererTrainingService` / tool
`SubmitMeanShiftClustererTraining` — `Kind = Clusterer`, recall class `MeanShiftClustererRecall`. `Bandwidth`
matches `meanShift`'s `bandwidth`; `Kernel` (`MeanShiftKernel`, default `Uniform`) picks the kernel shape used to weight
neighbours, and `MaxNeighbours` caps the neighbour search, an efficiency control the plain
`meanShiftR` call above has no equivalent of. Note this DTO still carries a `Clusters`-shaped absence correctly — there
is no `Clusters`/`Components` field at all, consistent with mean shift discovering the cluster count rather than being
told it.

## Code audit

`SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsMeanShiftClustererTrainingService.cs`)
queues the run; `AdvancedAnalyticsTrainingWorker` constructs a `MeanShiftClustererTrainingExecutor`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:605-606`) and calls its
`ExecuteAsync` (line 615), which runs `MeanShiftClustererTrainer.TrainWithSplit`/`Train`
(`Jube.Engine/AdvancedAnalytics/Clustering/MeanShiftClustererTrainer.cs:139-147`). That trainer instantiates
`Accord.MachineLearning.MeanShift` (`Accord.MachineLearning/Clustering/MeanShift/MeanShift.cs`) with a kernel built by
its own `KernelOf` switch (`MeanShiftClustererTrainer.cs:280-289`) and calls `Learn`.

### Kernel: a genuine choice of flat/uniform, Gaussian or Epanechnikov — default is flat

`MeanShiftClustererOptions.Kernel` defaults to `MeanShiftKernel.Uniform`
(`Jube.Engine/AdvancedAnalytics/Clustering/MeanShiftClustererOptions.cs:19-30`), and `KernelOf`
(`MeanShiftClustererTrainer.cs:280-289`) maps the three enum values to real, distinct Accord kernel classes:
`UniformKernel` (flat/box), `GaussianKernel(dimension)`, and `EpanechnikovKernel(dimension)`. This is not cosmetic —
`MeanShift.Learn` (`Accord.MachineLearning/Clustering/MeanShift/MeanShift.cs:365-467`) branches on the kernel type at
lines 394-403 (`if (kernel is UniformKernel) func = uniform; else func = general;`) and the two paths compute genuinely
different neighbour weightings:

* `uniform` (lines 590-619) is an unweighted arithmetic mean of every neighbour inside the search radius — every
  neighbour counts equally regardless of distance, the textbook flat/box kernel.
* `general` (lines 554-588) computes a real kernel-weighted mean: for each neighbour it derives a weight
  `g = -kernel.Derivative(u / h²) * weight` (`u` the squared distance, `h` the bandwidth) and accumulates
  `g * point[i]` before dividing by the summed weights — the standard Comaniciu & Meer (2002) mean-shift update rule,
  evaluated through each kernel's own derivative function. Selecting `Gaussian` or
  `Epanechnikov` genuinely changes which points influence a mode and by how much; it is not a label with no runtime
  effect.

### Bandwidth: a fixed hyperparameter, no data-driven estimator

`MeanShiftClustererOptions.Bandwidth` is a plain user-supplied `double` with default `2.0`
(`MeanShiftClustererOptions.cs:28`), validated only for being finite and positive (line 48-49). Nothing in
`MeanShiftClustererTrainer.cs` or in `Accord.MachineLearning.MeanShift` derives a bandwidth from the data — there is no
Silverman's rule, no Scott's rule, no plug-in or cross-validated bandwidth selection anywhere in this path. The caller
must choose `Bandwidth` themselves (in standardised z-score units when `Standardise`
is true, the default), and a mode-count sanity check is the only feedback the product gives after the fact:
`MeanShiftWarningCodes.SingleMode` fires if the fit collapses to one mode (`MeanShiftClustererTrainer.cs:165-168`,
"consider a smaller bandwidth"), and `ManyModes` fires if the mode count reaches at least half the training row count
for 10+ rows (lines 169-173, "consider a larger bandwidth") — both are after-the-fact diagnostics, not a selection
mechanism.

### How a mode search actually runs and how modes are deduplicated

`MeanShift.Learn` (`MeanShift.cs:365-467`) seeds one search point per training row (`createSeeds`, called with bin size
`2 * Bandwidth`, lines 385) and, for each seed, `move`
(`MeanShift.cs:469-531`) repeatedly: finds neighbours within `Bandwidth * 3` via a KD-tree, capped at
`Maximum` neighbours (Jube's `MaxNeighbours`, default `0` meaning unlimited —
`MeanShiftClustererOptions.cs:32`, `MeanShift.cs:154-156` documents `0` as "unlimited"); computes the (possibly
kernel-weighted) mean of those neighbours; moves the current point to that mean; and stops either when it comes within
`Bandwidth` of an already-found mode (lines 506-520, at which point it is snapped onto that mode, deduplicating it) or
when the shift vector's Euclidean norm falls below `Tolerance * Bandwidth`
(line 524, citing Comaniciu 2002 p.606 directly in the comment), bounded by `MaxIterations` (default `100`,
`MeanShiftClustererOptions.cs:34`, checked at `MeanShift.cs:483`). Because nearby-mode snapping happens during the
search itself, the final mode count is bandwidth-dependent and discovered, not fixed — consistent with the doc prose
above.
