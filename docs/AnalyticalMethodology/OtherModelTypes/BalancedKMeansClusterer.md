---
layout: default
title: Balanced K-Means Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 14
---

# Balanced K-Means Clusterer

## Procedure: balanced k-means

Balanced k-means adds a size constraint to the plain algorithm — clusters are kept to roughly equal (or capped)
membership rather than whatever size the natural geometry produces, which matters when a downstream process (for
example, a review queue assigned by cluster) cannot tolerate one cluster swamping the others.

*No common CRAN package implements size-constrained k-means either. Plain k-means below is the nearest honest R
analogue, but — unlike the product's actual behaviour — it enforces no size constraint at all, so its cluster sizes here
are only balanced by coincidence of the synthetic data's symmetric design.*

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
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsBalancedKMeansClustererTrainingService` / tool
`SubmitBalancedKMeansClustererTraining` — `Kind = Clusterer`, recall class `BalancedKMeansClustererRecall`. The DTO's
`Clusters`, `Seeding`, `MaximumIterations`, `Tolerance` and `Standardise` are the same shape as plain k-means, but the
background worker's fitting loop actually enforces the balance constraint the R snippet above cannot demonstrate — this
is a genuine "R/script-runner has no equivalent behaviour" case, not merely a missing package.

## Code audit

`SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsBalancedKMeansClustererTrainingService.cs:76-143`)
only validates the request and inserts a queued `AdvancedAnalyticsTrainingRun` row; it does no training itself.
`AdvancedAnalyticsTrainingWorker` constructs a `BalancedKMeansClustererTrainingExecutor`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:542-543`) and calls its
`ExecuteAsync` (line 552), whose `Trainer` delegate defaults to `BalancedKMeansClustererTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Training/BalancedKMeansClustererTrainingExecutor.cs:59-61`). That trainer
(`Jube.Engine/AdvancedAnalytics/Clustering/BalancedKMeansClustererTrainer.cs:156-172`) instantiates
`Accord.MachineLearning.BalancedKMeans` (`Accord.MachineLearning/Clustering/KMeans/BalancedKMeans.cs`) and calls
`Learn`.

### Balance is a hard constraint in the assignment step, not a post-hoc rebalancing pass

`BalancedKMeans.Learn` (`Accord.MachineLearning/Clustering/KMeans/BalancedKMeans.cs:126-287`) builds, on every
iteration, an `x.Length` × `x.Length` cost matrix (`munkres = new Munkres(x.Length, x.Length)` at line 195) and solves
it with the Munkres/Hungarian algorithm (`munkres.Minimize()`, line 211) — an exact bipartite perfect-matching solver,
not a heuristic. `GetDistances` (lines 305-317) fills row `i`, column `j`
of that matrix with the distance from data point `j` to centroid `GetIndex(k, i)` where
`GetIndex(clusters, index) => (index + 1) % clusters` (lines 319-325, citing Malinen's thesis equation 6.6 in the
comment directly above). Because there are `x.Length` matrix rows but only `k` distinct centroid identities, the
`(i + 1) % k` mapping cycles every row through a fixed, pre-determined cluster label before the optimiser ever runs, so
the `x.Length` row "slots" are split into `k` groups of size
`floor(x.Length / k)` or `ceil(x.Length / k)` **before matching starts**. Solving the assignment problem only decides
which *data point* goes into which already-size-fixed slot, minimising total distance subject to that fixed slot
distribution (`GetLabels`, lines 289-303, reads the cluster identity for point `j` off the matched row `i`'s
`GetIndex(clusters, i)`). This is a hard, structural balance guarantee — cluster sizes can differ by at most one —
enforced inside every assignment step, not a heuristic that runs plain k-means and then reshuffles points afterwards to
even out sizes.

### Initialisation and iteration

If `Clusters.Centroids[0]` is still unset, `BalancedKMeans.Learn` first runs plain `KMeans.Learn` on the same data to
seed the centroids (lines 178-181); Jube's `BalancedKMeansClustererTrainer` sets `UseSeeding`
from the DTO's `Seeding` field before this call (`BalancedKMeansClustererTrainer.cs:160`), so that initial plain-k-means
pass uses either uniform or k-means++ seeding as requested. After that, the algorithm alternates the Munkres assignment
above with ordinary centroid recomputation (mean of each cluster's assigned points, lines 242-256) until
`converged(centroids, newCentroids)` reports the centroids have stopped moving by more than `Tolerance` (line 260).
`MaxIterations` is validated only as "must be greater than zero" (`BalancedKMeans.cs:129-133`) — if it is zero the
constructor throws rather than iterating unboundedly.

### Warning the product itself records about this family

`BalancedKMeansClustererTrainer.cs:179-182` adds a fixed `IterationCountUnavailable` warning to every run:
"The vendored balanced k-means implementation does not report how many assign-then-recentre iterations it took, and does
not enforce the maximum iterations as a hard stop; it only stops once the centroids settle within the tolerance." This
is accurate to the `Learn` loop above — `Iterations` is set to `0` and never incremented anywhere in
`BalancedKMeans.Learn`, and the `while (!shouldStop)` loop (line 200) has no iteration-count exit condition at all, only
the `converged` check.

### Row-count ceiling

Both `BalancedKMeansClustererTrainer.ValidateData` and `TrainWithSplit` reject a training split larger than
`MaximumTrainingRows = 2000` (`BalancedKMeansClustererTrainer.cs:34, 86-90, 471-475`), with the stated reason that
"Balanced k-means solves an assignment problem whose cost grows with the cube of the row count" — the Munkres/Hungarian
algorithm is `O(n^3)` in the matrix dimension, and here the matrix dimension is the row count itself
(`munkres = new Munkres(x.Length, x.Length)`), so this cap is a direct, correctly-reasoned consequence of the
assignment-problem formulation, not an arbitrary limit.
