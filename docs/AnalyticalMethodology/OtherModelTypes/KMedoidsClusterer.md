---
layout: default
title: K-Medoids Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 11
---

# K-Medoids Clusterer

## Procedure: K-medoids

K-medoids is k-means with the centroid replaced by an actual training row (the medoid), which makes it usable with any
dissimilarity, not only squared Euclidean distance, and more resistant to outlying rows skewing a cluster's centre.

```R
library(cluster)

set.seed(1)
n <- 90
x <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 40, 2), rnorm(n / 3, 70, 2))
y <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 45, 2), rnorm(n / 3, 15, 2))
merchants <- scale(data.frame(x, y))

model <- pam(merchants, k = 3, metric = "euclidean")
print(model$medoids)
print(model$clusinfo)
```

Console output:

```
               x          y
[1,] -1.19089859 -0.8725270
[2,]  0.02071951  1.3849218
[3,]  1.20170795 -0.5515688
     size  max_diss   av_diss  diameter separation
[1,]   30 0.2297020 0.1248836 0.4028131   2.181622
[2,]   30 0.3197539 0.1162871 0.5301364   1.816645
[3,]   30 0.3091008 0.1364594 0.5007333   1.816645
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsKMedoidsClustererTrainingService` / tool
`SubmitKMedoidsClustererTraining` — `Kind = Clusterer`, recall class `KMedoidsClustererRecall`. `Seeding`
(`KMedoidsSeeding`, default `PamBuild`) names the PAM-style build phase `pam()` runs above by default; no
`Tolerance` field exists, since PAM's swap phase is a discrete search over training rows, not an iterative numeric
convergence.

## Code audit

`AdvancedAnalyticsKMedoidsClustererTrainingService.SubmitTrainingAsync` only validates the request and queues a run,
identically in shape to the K-Means service; the actual fit is
`KMedoidsClustererTrainer.Train`/`TrainWithSplit`
(`Jube.Engine/AdvancedAnalytics/Clustering/KMedoidsClustererTrainer.cs`).

### What is actually instantiated

`KMedoidsClustererTrainer.TrainWithSplit` (lines 148-154) builds

```
var model = new KMedoids(options.Clusters)
{
    MaxIterations = options.MaximumIterations,
    Initialization = options.Seeding == KMedoidsSeeding.Uniform ? Seeding.Uniform : Seeding.PamBuild,
    ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
};
```

— the non-generic `Accord.MachineLearning.KMedoids` convenience class
(`Accord.MachineLearning/Clustering/KMedoids/KMedoids.cs:376-388`), fit against the same z-scored, median-imputed design
matrix construction used by K-Means. There is no `Tolerance` assignment here at all, matching the fact that
`KMedoidsClustererOptions` (`Jube.Engine/AdvancedAnalytics/Clustering/KMedoidsClustererOptions.cs`)
has no `Tolerance` property — and, as below, Accord's own `KMedoids.Tolerance` property is never read by the algorithm
anyway.

### Seeding

The non-generic `KMedoids(int k)` constructor is `base(k, new Euclidean())` and sets
`Initialization = Seeding.PamBuild` by default (`KMedoids.cs:164-183, 384-387`), which is what Jube's own DTO default
(`KMedoidsSeeding.PamBuild`) maps to. `DoPamBuildSeeding`
(`` Accord.MachineLearning/Clustering/CentroidCluster`3.cs:233-309 ``) is the genuine Kaufman & Rousseeuw (1987)
BUILD phase: pick the point with the smallest total distance to every other point as the first medoid, then repeatedly
add whichever remaining point yields the largest total reduction in every other point's
distance-to-nearest-current-medoid, until `Clusters` medoids are chosen — a real greedy construction heuristic, not a
placeholder. `KMedoidsSeeding.Uniform` maps to Accord's `Seeding.Uniform`, a plain random sample of rows as the initial
medoids, with no BUILD-style greedy selection.

### Distance metric

The non-generic `KMedoids(int k) : base(k, new Euclidean())` (`KMedoids.cs:384-387`) fixes the metric to plain
(non-squared) Euclidean distance — distinct from K-Means, which is fixed to *squared* Euclidean. Jube's trainer never
overrides `Distance`, so every assignment, swap-cost and silhouette calculation in this family runs on plain Euclidean
distance over the standardised design matrix.

### Convergence criterion — the real PAM SWAP step, not an approximation

`KMedoids<T>.Compute` (`KMedoids.cs:300-342`) is the genuine Partitioning Around Medoids algorithm, not a cheaper
approximation of it. Each outer iteration: `assignPointsToClusters` assigns every point to its nearest current medoid
while also recording each point's distance to its *second*-nearest medoid (`KMedoids.Swap.cs:31-65`); `findBestSwap`/
`computeSwapCost`
(`KMedoids.Swap.cs:85-178`) then evaluate, for every non-medoid candidate `h` and every current medoid `i`, the total
cost change `T(i,h)` of swapping medoid `i` for candidate `h` — implemented as the classic Kaufman & Rousseeuw `K_jih`
marginal-cost formula summed over every other point `j`
(`computeSwapCost`, lines 148-177):

```
kjih = (dji == dj)
    ? (djh < secondClusterDistance[j] ? djh - dj : secondClusterDistance[j] - dj)
    : (djh < dj ? djh - dj : 0.0);
```

The single best-improving `(i, h)` pair across the whole search is swapped in (`KMedoids.cs:329-332`), one swap per
outer iteration. The loop (`KMedoids.cs:310-333`) stops when either `Iterations >= MaxIterations`, or the best available
swap no longer improves total cost (`minTih >= 0.0`, line 324) — i.e. a local optimum under one-swap-per-round search
has been reached. This is exactly PAM's textbook SWAP phase, not a
`k`-means-style centroid-movement tolerance and not a corner-cut approximation of it.

**Finding — `Tolerance` is dead configuration in Accord's own class.** `KMedoids<T>.Tolerance`
(`KMedoids.cs:131`, default `1e-5` set at line 181) is declared and documented ("Gets or sets the relative convergence
threshold for stopping the algorithm") but is never referenced anywhere in `Compute`
(`KMedoids.cs:300-342`) or in `KMedoids.Swap.cs` — the only stop conditions checked are the iteration cap and
`minTih >= 0.0`. This confirms, from the algorithm's own source rather than from Jube's DTO shape alone, that a numeric
tolerance genuinely has no role in this family's convergence.

### Cost of the SWAP step

`findBestSwap` evaluates every non-medoid candidate `h` (up to `n` of them) against every current medoid
`i` (`Clusters` of them), and each `computeSwapCost` call sums over every other point `j` — an
`O(Clusters · n²)` cost per outer iteration (`KMedoids.Swap.cs:94-146`). This is the genuine, expected complexity of
PAM's SWAP step, not a subsampled or approximated variant; it is unrelated to (and not bounded by) the
`MaximumSilhouetteRows = 2000` cap the trainer separately applies to its own silhouette diagnostic
(`KMedoidsClustererTrainer.cs:33, 220-235`), which only limits a post-fit metric, not the fit itself.

### Special case: a single cluster

When `Clusters == 1`, `KMedoids<T>.Learn` takes a separate branch (`KMedoids.cs:246-267`) that skips the SWAP machinery
entirely and instead picks the single point with the smallest total distance to every other point as the sole medoid — a
plain `O(n²)` exhaustive search, not an iterative fit. This path is reachable whenever a caller sets `Clusters = 1`,
though Jube's default `Clusters` is 3.
