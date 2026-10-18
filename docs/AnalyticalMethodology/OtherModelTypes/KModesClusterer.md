---
layout: default
title: K-Modes Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 12
---

# K-Modes Clusterer

## Procedure: K-modes

K-modes is k-means for purely categorical predictors: distance is Hamming distance (count of mismatching attributes) and
a "centroid" is the modal (most frequent) category of each attribute among a cluster's members.

*No installable common CRAN package supplies k-modes cleanly in this environment: `klaR::kmodes`, the usual choice,
pulls in `questionr`, which pulls in `shiny`/`httpuv`, which failed to build here for lack of system TLS/curl headers (
an environment limitation, not a licensing or algorithm issue). Rather than force that dependency chain, the snippet
below is a direct, minimal Hamming-distance k-modes loop implementing the same algorithm (Huang, 1998), run for real
like every other snippet in this guide.*

```R
set.seed(1)
n <- 90
channel <- c(rep("Card", n / 3), rep("Wire", n / 3), rep("Card", n / 3))
country_risk <- c(rep("Low", n / 3), rep("High", n / 3), rep("High", n / 3))
device_type <- c(rep("Mobile", n / 3), rep("Desktop", n / 3), rep("Mobile", n / 3))
cases <- data.frame(channel, country_risk, device_type,
                     stringsAsFactors = TRUE)

kmodes_fit <- function(data, k, iter.max = 20) {
  n <- nrow(data)
  modes <- data[sample(n, k), , drop = FALSE]
  cluster <- integer(n)
  for (iter in seq_len(iter.max)) {
    dist_to_mode <- sapply(seq_len(k), function(j) {
      rowSums(sapply(seq_len(ncol(data)), function(col) {
        data[[col]] != modes[[col]][j]
      }))
    })
    new_cluster <- apply(dist_to_mode, 1, which.min)
    if (all(new_cluster == cluster)) break
    cluster <- new_cluster
    for (j in seq_len(k)) {
      members <- data[cluster == j, , drop = FALSE]
      if (nrow(members) == 0) next
      for (col in seq_len(ncol(data))) {
        tab <- table(members[[col]])
        modes[[col]][j] <- names(tab)[which.max(tab)]
      }
    }
  }
  list(modes = modes, cluster = cluster, iterations = iter)
}

result <- kmodes_fit(cases, k = 3)
print(result$modes)
print(table(result$cluster))
```

Console output:

```
   channel country_risk device_type
68    Card         High      Mobile
39    Wire         High     Desktop
1     Card          Low      Mobile

 1  2  3 
30 30 30 
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsKModesClustererTrainingService` / tool
`SubmitKModesClustererTraining` — `Kind = Clusterer`, recall class `KModesClustererRecall`. `Clusters`,
`MaximumIterations`, `Tolerance` and `Seeding` (`KModesSeeding`, default `KModesPlusPlus`) mirror the loop above; there
is deliberately no `Standardise` field on this DTO, since Hamming distance over categories has no numeric scale to
standardise.

## Code audit

`AdvancedAnalyticsKModesClustererTrainingService.SubmitTrainingAsync` only validates the request and queues a run, in
the same shape as K-Means and K-Medoids; the actual fit is
`KModesClustererTrainer.Train`/`TrainWithSplit`
(`Jube.Engine/AdvancedAnalytics/Clustering/KModesClustererTrainer.cs`).

### What is actually instantiated — and a trap the trainer avoids

`KModesClustererTrainer.TrainWithSplit` (lines 188-200) builds

```
var model = new KModes<int>(options.Clusters, new SimpleMatchingDistance())
{
    MaxIterations = options.MaximumIterations,
    Tolerance = options.Tolerance,
    ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
};
...
model.Clusters.Randomize(trainInts, seedingStrategy);
learned = model.Learn(trainInts);
```

using the **generic** `Accord.MachineLearning.KModes<T>`
(`` Accord.MachineLearning/Clustering/KModes/KModes`1.cs:53-370 ``) with a private
`SimpleMatchingDistance : IDistance<int[]>` (`KModesClustererTrainer.cs:36-48`) that sums `1` for every mismatching
attribute between two integer-coded rows — the Hamming/simple-matching dissimilarity Huang (1998) actually defines. This
matters because Accord also ships a non-generic convenience class,
`public class KModes : KModes<int> { public KModes(int k) : base(k, new Manhattan()) {} }`
(`` KModes`1.cs:391-403 ``), whose *default* distance is **Manhattan**, not Hamming/simple-matching. Had Jube's trainer
used that convenience constructor instead of the generic one with its own explicit
`SimpleMatchingDistance`, the resulting fit would not implement Huang's dissimilarity at all. The trainer avoids that
trap: it never references the non-generic `KModes` class, only `KModes<int>` with its own metric supplied explicitly.

### Confirming the real Huang (1998) rule

Cluster assignment (`` KModes`1.cs:261-271 ``, `int c = labels[i] = Clusters.Decide(x[i])`) uses whatever
`Distance` was registered — here the Hamming/simple-matching metric above — to find each row's nearest mode. Centroid
(mode) update (`` KModes`1.cs:276-292 ``) recomputes, per cluster and per attribute dimension,
`newCentroids[i][d] = p[d].Mode(alreadySorted: false, inPlace: true)` — the modal (most frequent) value in that
dimension among the cluster's current members. `Mode<T>`
(`Accord.Math/Accord.Statistics/Measures.Vectors.cs:836-916`) is a genuine most-frequent-value computation
(dictionary/bag counting for non-`IComparable` element types, sort-then-scan counting for `IComparable`
types such as `int`, which is the case here) — not a mean, not a median. Nearest-neighbour assignment by mismatch count
plus per-attribute modal recomputation is exactly Huang's (1998) k-modes algorithm: this is a real implementation of the
published method, not k-means run on label-encoded integers and not an approximation of either.

### Seeding

Jube maps `KModesSeeding.Uniform -> Seeding.Uniform`, else `Seeding.KMeansPlusPlus`
(`KModesClustererTrainer.cs:199`), passed to `model.Clusters.Randomize(trainInts, seedingStrategy)` before
`Learn` is called. There is no distinct "KModes++" seeding algorithm anywhere in Accord: `Seeding.KMeansPlusPlus`
here re-runs the exact same generic greedy farthest-point seeding routine used for K-Means (`DoKMeansPlusPlusSeeding`,
`` Accord.MachineLearning/Clustering/CentroidCluster`3.cs:138-231 ``), just parameterised with the
Hamming/simple-matching distance instead of squared Euclidean. So the DTO's
`KModesSeeding.KModesPlusPlus` label names a real seeding *idea* (weighted-by-distance greedy seeding) but its
implementation is Accord's generic KMeansPlusPlus routine over a different metric, not a Huang-specific seeding method
described anywhere in the 1998 paper.

### Convergence criterion

`KModes<T>.Learn`'s main loop (`` KModes`1.cs:247-308 ``) assigns points, recomputes modes, then calls
`converged(centroids, newCentroids)` (lines 343-369). That method returns `true` (stop) if
`Iterations > MaxIterations` (line 347 — strictly greater-than, one iteration later than K-Means's
`>=` at `KMeans.cs:629`) or `Token.IsCancellationRequested`; otherwise it checks, per cluster,
`Math.Abs(Distance.Distance(centroid, newCentroid)) >= Tolerance` (line 362) using the **same**
`SimpleMatchingDistance` metric between the previous and new mode vectors.

**Finding — `Tolerance` is not meaningfully tunable for this family.** Because the registered distance between two mode
vectors is always a non-negative *integer* count of attribute mismatches, and `Tolerance`
defaults to `1e-5` (`KModesClustererOptions.cs:33`), the check `>= Tolerance` is satisfied by any mode change at all (a
mismatch count of `1` is already five orders of magnitude above the tolerance) and fails only when the mismatch count is
exactly `0`, i.e. the mode vector is bit-for-bit identical to the previous iteration's. In other words, whatever finite
positive value `Tolerance` is set to (`1e-5`, `0.5`, or `10`, so long as it stays below `1`), the observable stopping
behaviour is identical: stop only on an exact repeat of every cluster's mode vector, or on hitting `MaximumIterations`.
The field exists for shape-parity with the other clusterer DTOs, but for K-Modes it functions as a fixed exact-equality
check rather than a continuously tunable convergence tolerance.
