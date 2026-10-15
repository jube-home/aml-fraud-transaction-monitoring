---
layout: default
title: Binary Split Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 15
---

# Binary Split Clusterer

## Procedure: binary split

Binary split clustering builds a cluster tree by repeated bisection: start with everything in one cluster, split it in
two, then repeatedly split whichever existing cluster reduces the remaining variance most, until the target count is
reached. It is a divisive (top-down), not agglomerative (bottom-up), hierarchical method.

```R
library(cluster)

set.seed(1)
n <- 90
x <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 40, 2), rnorm(n / 3, 70, 2))
y <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 45, 2), rnorm(n / 3, 15, 2))
merchants <- scale(data.frame(x, y))

model <- diana(merchants, metric = "euclidean")
print(model)

assignments <- cutree(as.hclust(model), k = 3)
print(table(assignments))
```

Console output:

```
Merge:
      [,1] [,2]
 [1,]  -63  -85
 [2,]   -7   -9
[... 89 merge rows omitted for length; the full divisive tree over all 90 rows was produced ...]
Divisive coefficient:
[1] 0.9838993

Available components:
[1] "order"  "height" "dc"     "merge"  "diss"   "call"   "data"  
assignments
 1  2  3 
30 30 30 
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsBinarySplitClustererTrainingService` / tool
`SubmitBinarySplitClustererTraining` — `Kind = Clusterer`, recall class `BinarySplitClustererRecall`. `cluster::diana`
is the closest common-package analogue in spirit (top-down, divisive) but it is agglomerative-tree software repurposed:
it builds a full dendrogram and only exposes a flat clustering via `cutree`. The DTO's
`Clusters`, `Seeding`, `MaximumIterations` and `Tolerance` show the real family is closer to repeated k-means-style
bisection than to `diana`'s distance-based divisive merge order — the code audit below confirms exactly which.

## Code audit

`SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsBinarySplitClustererTrainingService.cs:76-155`)
only validates the request and queues an `AdvancedAnalyticsTrainingRun` row. `AdvancedAnalyticsTrainingWorker`
constructs a `BinarySplitClustererTrainingExecutor`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:479-480`) and calls its
`ExecuteAsync` (line 489), which runs `BinarySplitClustererTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Clustering/BinarySplitClustererTrainer.cs:148-165`). That trainer instantiates
`Accord.MachineLearning.BinarySplit` (`Accord.MachineLearning/Clustering/KMeans/BinarySplit.cs`)
directly and calls `Learn` — it is a thin pass-through, not a reimplementation.

### Genuine top-down bisecting k-means (Steinberg & Winkler style), confirmed

`BinarySplit.Learn` (`Accord.MachineLearning/Clustering/KMeans/BinarySplit.cs:94-198`) is exactly the classic
bisecting-k-means procedure:

1. All rows start in one cluster: `clusters[0] = x` (line 152).
2. The loop `for (int current = 1; current < k; current++)` (line 155) runs `k - 1` times.
3. Each iteration picks the cluster with the **largest current distortion** — `distortions.Max(current, out
   choosen)` (lines 158-159) — where `distortions[c]` was set from `kmeans.Clusters[c].Distortion(...)`, the cluster's
   within-cluster sum of squared distances (set at lines 172-173 and, for the very first split, implicitly the SSE of
   the single starting cluster).
4. That cluster is bisected by running a **fresh `k = 2` k-means** on just its rows (`split(clusters[choosen],
   kmeans)`, lines 162-165, `kmeans = new KMeans(2)` at line 137, inheriting `UseSeeding`, `Tolerance` and
   `MaxIterations` from the `BinarySplit` instance's own settings). `split` (lines 200-222) calls
   `kmeans.Randomize(cluster)` then `kmeans.Learn(cluster).Decide(cluster)` — a real, independent 2-means fit on the
   subset, not a distance-based tree merge.
5. The chosen cluster's slot is overwritten with one half, the next free slot gets the other half (lines 164-165,
   168-169), and both halves' distortions are recomputed (lines 172-173) so the next iteration's "largest distortion"
   choice sees up-to-date figures.

This is precisely "repeatedly bisect the highest-SSE cluster" — the Steinberg & Winkler divisive rule — not a
distance-based agglomerative method repurposed top-down. `cluster::diana` in the R snippet above merely approximates the
*shape* (divisive, hierarchical) of this family; its actual splitting rule (largest average dissimilarity pair, then
classify remaining points by which side they are closer to) is unrelated to the trained family's own rule.

### What Jube's wrapper changes versus the raw Accord class

`BinarySplitClustererTrainer.cs:148-155` sets `ComputeCovariances = false` and `ComputeError = false`
explicitly on the `BinarySplit` instance and does not set `ComputeProportions` (so it keeps the class default of
`false`, `BinarySplit.cs:56-60`) — meaning the `if (ComputeProportions) { ... } else {
ComputeInformation(x); }` branch at `BinarySplit.cs:184-195` always takes the `else` path in this product:
proportions are computed independently in Jube's own `BuildClusterSummaries`
(`BinarySplitClustererTrainer.cs:177, 287`) from the training-split labels, not from the Accord class's own
`Proportions` array.

### Distortion metric

`Distortion` (used for both the split-selection criterion and, summed, for `TrainInertia`
`BinarySplitClustererTrainer.cs:184`) is the within-cluster sum of squared Euclidean distances to the centroid — the
same objective plain k-means minimises — so "largest distortion" in step 3 above is literally "highest current
within-cluster SSE," matching the textbook bisecting-k-means description in the doc prose above without simplification.

### No pruning or count validation beyond one hard rule

`BinarySplitClustererOptions.Validate` requires `Clusters >= 2` with an explicit reason in the exception message: with
fewer than two target clusters the starting cluster is never split at all
(`Jube.Engine/AdvancedAnalytics/Clustering/BinarySplitClustererOptions.cs:29-32`). There is no other divergence from the
plain algorithm — no cost-complexity pruning, no re-optimisation pass over the final
`k` clusters once bisection finishes.
