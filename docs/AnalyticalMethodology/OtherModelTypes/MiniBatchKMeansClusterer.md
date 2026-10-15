---
layout: default
title: Mini-Batch K-Means Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 13
---

# Mini-Batch K-Means Clusterer

## Procedure: mini-batch K-means

Mini-batch k-means (Sculley, 2010) is a large-data scalability variant of k-means: instead of recomputing every centroid
from the whole matrix each iteration, it updates centroids from small random batches, trading some precision for
throughput on data too large to fit the full-batch update in memory or time budget.

*Mini-batch k-means is not really a base-R concept: no common CRAN package (base or add-on) in this environment
distinguishes a batched update from plain k-means. The honest R analogue — what a script-runner user would actually
reach for — is plain `stats::kmeans` over the full matrix, shown below.*

```R
set.seed(1)
n <- 90
x <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 40, 2), rnorm(n / 3, 70, 2))
y <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 45, 2), rnorm(n / 3, 15, 2))
merchants <- scale(data.frame(x, y))

model <- kmeans(merchants, centers = 3, nstart = 5, iter.max = 100)
print(model$centers)
print(model$size)
```

Console output:

```
             x          y
1  1.213805572 -0.5179427
2 -1.215769357 -0.8631565
3  0.001963785  1.3810992
[1] 30 30 30
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsMiniBatchKMeansClustererTrainingService` / tool
`SubmitMiniBatchKMeansClustererTraining` — `Kind = Clusterer`, recall class `MiniBatchKMeansClustererRecall`. Unlike the
R analogue above,
this family genuinely batches: `BatchSize` (default 100) caps rows processed per update,
`InitializationBatchSize` optionally uses a smaller batch just to seed the centroids, and
`NumberOfInitializations` repeats the whole batched fit from independent seeds and keeps the best. This is one of the
clearest cases in the guide where the product does something with no equivalent to actually run in plain R — the value
of the mini-batch behaviour only shows up at row counts this guide's toy examples never reach.

## Code audit

`AdvancedAnalyticsMiniBatchKMeansClustererTrainingService.SubmitTrainingAsync` only validates the request and queues a
run, in the same shape as the other clusterer services; the actual fit is
`MiniBatchKMeansClustererTrainer.Train`/`TrainWithSplit`
(`Jube.Engine/AdvancedAnalytics/Clustering/MiniBatchKMeansClustererTrainer.cs`).

### What is actually instantiated

`MiniBatchKMeansClustererTrainer.TrainWithSplit` (lines 151-170) computes an effective
`InitializationBatchSize` (defaulting to `3 * Clusters` when not supplied, capped to the training split's row count,
line 151-153) and builds

```
var model = new MiniBatchKMeans(options.Clusters, options.BatchSize)
{
    MaxIterations = options.MaximumIterations,
    Tolerance = options.Tolerance,
    UseSeeding = options.Seeding == KMeansSeeding.Uniform ? Seeding.Uniform : Seeding.KMeansPlusPlus,
    NumberOfInitializations = options.NumberOfInitializations,
    InitializationBatchSize = effectiveInitializationBatchSize,
    ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
};
```

— `Accord.MachineLearning.MiniBatchKMeans`, which subclasses `KMeans`
(`Accord.MachineLearning/Clustering/KMeans/MiniBatchKMeans.cs:83-440`).

### Confirming this is a genuine mini-batch update, not "K-Means on a subsample"

`MiniBatchKMeans` overrides `Learn` (lines 194-347) with a `while` loop, and this is the specific question worth
checking carefully because it is a common corner-cutting point. What the code does, each iteration:

1. **A fresh random batch every iteration**, not one fixed subsample. `MakeBatch(rows, batchSize)`
   (lines 355-378) draws `batchSize` *distinct* row indices by repeatedly calling
   `Generator.Random.Next(totalNumberOfPoints)` into a `HashSet` until it has enough — a new, independent uniform sample
   without replacement on every pass through the loop (line 279), not a single subsample drawn once up front.
2. **Assign only the batch's points** to their nearest existing centroid (`Clusters.Decide`, line 286).
3. **Per-centroid running-average update, not a full recompute.** For each point in the batch (lines 302-314):
   ```
   count[clusterIndex]++;
   double eta = 1.0 / count[clusterIndex];
   newCentroids[clusterIndex][i] = (1.0 - eta) * newCentroids[clusterIndex][i] + eta * point[i] * weights[pointIndex];
   ```
   `count` is a **per-cluster hit counter declared once outside the main loop** (lines 256, 270-273) that only ever
   increments across the whole run — it is never reset per batch or per iteration. This
   `eta = 1/count` schedule, applied only to the centroids whose cluster a batch point landed in, is precisely Sculley's
   (2010) Algorithm 1 streaming/partial-fit update: each new assigned point moves its centroid by a shrinking learning
   rate as that centroid accumulates more total hits over the life of the run, converging towards the true running mean
   of every point ever assigned to it — not the mean of one batch and not a mean recomputed from scratch each round.
4. Untouched centroids (those with no point in the current batch) are simply copied unchanged from
   `centroids` into `newCentroids` (lines 293-299) before the next convergence check.

This confirms the family is a genuine incremental mini-batch fit in the sense of Sculley (2010), reusing
`KMeans`'s inherited machinery only for the centroid/cluster bookkeeping — it is not plain K-Means re-run on one fixed
random subsample of the data.

### Initialisation: also batched, also validated on a held-out batch

`InitializeCentroids` (lines 385-439) runs `NumberOfInitializations` candidate seedings. Each candidate draws a fresh
`InitializationBatchSize`-sized batch to seed centroids via
`Clusters.Randomize(centroidBatch, UseSeeding)` — genuinely applying the same k-means++/uniform seeding logic as full
K-Means, just over a small random subsample rather than the whole matrix — plus a second, separately drawn batch used
purely to score each candidate's distortion (`Clusters.Distortion`, line 425). The candidate with the lowest distortion
on its validation batch is kept (lines 429-433). This
seed-multiple-times-on-small-batches-and-validate-on-a-different-batch approach matches the initialisation scheme
Sculley's paper itself recommends for mini-batch k-means; it is not something Jube improvised independently, and it is
not a corner cut relative to the published method.

### Convergence criterion

`MiniBatchKMeans` does not override `converged` — it inherits `KMeans.converged`
(`KMeans.cs:625-654`) unchanged, so it shares that method's iteration cap and the same
divide-by-previous-centroid-coordinate relative-tolerance check documented in `KMeansClusterer.md`, including the
near-zero-coordinate caveat on standardised data (`KMeans.cs:646`). One additional wrinkle specific to the mini-batch
case: because only the centroids touched by the current iteration's random batch are updated, any cluster a batch
happens not to sample in a given iteration is copied through unchanged and therefore trivially satisfies the tolerance
check for that round — it "converges" by not having been touched, not because its true centroid has settled. The loop
only actually exits `converged`
once every cluster's coordinates have simultaneously stopped moving beyond `Tolerance` within the same iteration.

### An always-true initialisation guard

`Learn` line 265 reads `if (Clusters.Centroids != null) { InitializeCentroids(x, weights); }`.
`Clusters.Centroids` is the array reference allocated once in the `KMeansClusterCollection` constructor
(`Accord.MachineLearning/Clustering/KMeans/KMeansCluster.cs:68-70`) and is never null after construction — only its
individual elements start out null, which is what the base `KMeans.compute()` actually checks
(`Clusters.Centroids[0] == null`, `KMeans.cs:514`). Because the mini-batch override checks the array reference rather
than an element, the condition is always true, so `InitializeCentroids` unconditionally re-seeds the centroids from
scratch on every call to `Learn`. This is behaviourally harmless for how Jube calls it — the trainer constructs a fresh
`MiniBatchKMeans` and calls `Learn` exactly once per training run (`MiniBatchKMeansClustererTrainer.cs:161-175`) — but
the guard does not perform the conditional initialisation its shape suggests.
