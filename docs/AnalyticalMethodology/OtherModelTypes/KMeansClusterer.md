---
layout: default
title: K-Means Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 10
---

# K-Means Clusterer

## Procedure: K-means

K-means alternates assigning each row to its nearest centroid and recomputing centroids as the mean of their assigned
rows, repeated to convergence from a `k-means++`-seeded start.

```R
set.seed(1)
n <- 90
x <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 40, 2), rnorm(n / 3, 70, 2))
y <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 45, 2), rnorm(n / 3, 15, 2))
merchants <- scale(data.frame(x, y))

model <- kmeans(merchants, centers = 3, nstart = 5, iter.max = 100,
                algorithm = "Hartigan-Wong")
print(model$centers)
print(model$size)
print(table(model$cluster))
```

Console output:

```
             x          y
1  1.213805572 -0.5179427
2 -1.215769357 -0.8631565
3  0.001963785  1.3810992
[1] 30 30 30

 1  2  3 
30 30 30 
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsKMeansClustererTrainingService` / tool
`SubmitKMeansClustererTraining` — `Kind = Clusterer`, recall class `KMeansClustererRecall`. `Clusters`,
`MaximumIterations` and `Tolerance` map to `centers`, `iter.max` and `kmeans`'s internal convergence tolerance;
`Seeding` (`KMeansSeeding`, default `KMeansPlusPlus`) is the initialisation strategy behind
`nstart`/the seeding half of `"Hartigan-Wong"` above.

## Code audit

`AdvancedAnalyticsKMeansClustererTrainingService.SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsKMeansClustererTrainingService.cs:83-132`)
only validates the request and queues an `AdvancedAnalyticsTrainingRun` row; the actual fit is
`KMeansClustererTrainer.Train`/`TrainWithSplit`
(`Jube.Engine/AdvancedAnalytics/Clustering/KMeansClustererTrainer.cs`), whose `Trainer` delegate the training service
wires up as its default implementation (`AdvancedAnalyticsKMeansClustererTrainingService.cs:52-54`).

### What is actually instantiated

`KMeansClustererTrainer.TrainWithSplit` (lines 148-155) builds

```
var model = new KMeans(options.Clusters)
{
    MaxIterations = options.MaximumIterations,
    Tolerance = options.Tolerance,
    UseSeeding = options.Seeding == KMeansSeeding.Uniform ? Seeding.Uniform : Seeding.KMeansPlusPlus,
    ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
};
```

— `Accord.MachineLearning.KMeans` (`Accord.MachineLearning/Clustering/KMeans/KMeans.cs`), fit against the z-scored
(`Standardise` default true), median-imputed design matrix built earlier in the same method (lines 85-133).
`MaxDegreeOfParallelism` is pinned to 1 even though Accord's `compute()` loop is written with `Parallel.For` throughout
(lines 548-601 of `KMeans.cs`) — floating-point summation is not strictly associative across threads, so this pin trades
away Accord's built-in parallelism for exact run-to-run reproducibility under a fixed seed
(`Accord.Math.Random.Generator.ThreadSeed = options.Seed`, trainer line 159).

### Seeding

Accord's `KMeans(int k)` constructor defaults to `UseSeeding = Seeding.KMeansPlusPlus`
(`KMeans.cs:315`), which is what Jube's own DTO default (`KMeansSeeding.KMeansPlusPlus`) maps to (trainer line 153). The
k-means++ path is `DoKMeansPlusPlusSeeding`
(`` Accord.MachineLearning/Clustering/CentroidCluster`3.cs:138-231 ``): pick one centre uniformly at random, then
repeatedly pick the next centre with probability proportional to its squared distance to the nearest centre already
chosen — the standard Arthur & Vassilvitskii (2007) greedy-weighted seeding, not a placeholder. `KMeansSeeding.Uniform`
maps to Accord's `Seeding.Uniform`
(`DoUniformSeeding`, `` CentroidCluster`3.cs:126-136 ``), a plain random sample of `Clusters` rows with no distance
weighting.

### Distance metric

The non-generic `KMeans(int k)` constructor fixes the metric to `SquareEuclidean`
(`KMeans.cs:287-290`: `public KMeans(int k) : this(k, new SquareEuclidean())`). Jube's trainer never overrides
`Distance`, so K-Means clustering here is always squared-Euclidean on the standardised design matrix; there is no
distance-family option on the DTO, which is consistent with squared Euclidean being the metric the centroid-as-mean
update is only exactly correct for.

### Convergence criterion

Accord's `compute()` (`KMeans.cs:496-612`) is genuine Lloyd's algorithm: assign every row to its nearest centroid in
parallel, accumulate weighted sums per cluster, divide into new centroids, then call
`converged(centroids, newCentroids)` (lines 625-654). That method increments `Iterations` and returns
`true` (stop) if either `Iterations >= MaxIterations`, or — for every coordinate of every centroid —
`Math.Abs((centroid[j] - newCentroid[j]) / centroid[j]) < Tolerance` (line 646, inverted: it returns
`false`, i.e. keep iterating, the moment any single coordinate's relative change is `>= Tolerance`). So the real
stopping rule is a genuine combination of an iteration cap and a per-coordinate relative-tolerance check on centroid
movement, not merely a hard iteration cap.

**Finding:** the tolerance check divides by the *previous* centroid coordinate with no epsilon guard or scale
normalisation (`KMeans.cs:646`). Because `Standardise` z-scores every predictor to zero mean by default, it is routine
for some centroid coordinate to sit very close to zero. When the previous value is near zero, a negligible absolute move
produces an enormous or "infinite" relative change (e.g. previous
`1e-10`, new `0` gives a ratio of `1`, already at the default `Tolerance = 1e-5`), so that coordinate keeps reporting
non-convergence for reasons unrelated to how far the centroid actually moved. In the exact degenerate case where both
the previous and new coordinate are precisely `0.0`, the ratio evaluates to
`NaN`, and `NaN >= Tolerance` is `false` in .NET, so that coordinate is (correctly, in that specific case)
treated as unchanged. The practical consequence is that fits on standardised data can run to
`MaximumIterations` and report the `NonConvergence` warning (`KMeansClustererTrainer.cs:174-176`) purely because of
near-zero centroid coordinates, even once the centroids have for all practical purposes stopped moving.
