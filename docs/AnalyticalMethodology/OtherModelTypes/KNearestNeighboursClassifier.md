---
layout: default
title: K-Nearest-Neighbours Classifier
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 5
---

# K-Nearest-Neighbours Classifier

K-nearest-neighbours makes no assumption about the shape of the boundary at all: to score a new row it finds the k
stored training rows nearest to it on standardised predictors and votes the share belonging to the event of interest.

## R equivalent

```R
library(class)

set.seed(1)
n <- 200
amount <- c(rnorm(n / 2, mean = 40, sd = 15), rnorm(n / 2, mean = 220, sd = 60))
velocity <- c(rpois(n / 2, 2), rpois(n / 2, 9))
fraud <- factor(c(rep(0, n / 2), rep(1, n / 2)))

train_x <- scale(data.frame(amount, velocity))
new_cases <- data.frame(amount = c(35, 260), velocity = c(1, 10))
new_x <- scale(new_cases, center = attr(train_x, "scaled:center"),
               scale = attr(train_x, "scaled:scale"))

result <- knn(train_x, new_x, cl = fraud, k = 15, prob = TRUE)
print(result)
print(attr(result, "prob"))
```

Console output:

```
[1] 0 1
attr(,"prob")
[1] 1 1
Levels: 0 1
[1] 1 1
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsKNearestNeighboursClassifierTrainingService` / tool
`SubmitKNearestNeighboursClassifierTraining` — `Kind = Classifier`, recall class `KNearestNeighboursClassifierRecall`.
`NeighbourCount` (default 15) is `k`; `Standardise` (default true) is the `scale()` step above. This family is unlike
every other classifier family in this section: "training" really is just remembering the rows, so the background worker
stores every standardised, imputed training row inside the model version and recall means searching those stored rows,
rather than a small fitted parameter set. The raw vote fraction is still Platt-calibrated (`Calibrate`,
`DecisionThreshold`) exactly as the other classifiers are.

## Code audit

`AdvancedAnalyticsKNearestNeighboursClassifierTrainingService.SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsKNearestNeighboursClassifierTrainingService.cs:48-50,85-135`)
only queues the run; the actual fit happens in `KNearestNeighboursClassifierTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Classification/KNearestNeighboursClassifierTrainer.cs:36-58`). That trainer calls no
`Accord.MachineLearning.*` type at all — there is no `Accord.MachineLearning.KNearestNeighbors` in this codepath.
Training and recall are both a hand-written implementation in
`Jube.Engine.AdvancedAnalytics.Classification`.

- **Distance metric:** squared Euclidean distance, accumulated per variable in a plain double loop
  (`KNearestNeighboursClassifierPipeline.Explain`,
  `Jube.Engine/AdvancedAnalytics/Classification/KNearestNeighboursClassifierPipeline.cs:87-101`). The square root is
  taken only when reporting the neighbour distances for explanation (line 117), never for ranking, so ranking is
  equivalent to plain Euclidean distance.
- **Neighbour search structure:** brute force. Every call to `Explain` scans every stored training row
  (`for (var i = 0; i < _rows.Length; i++)`, line 89) — an O (rows) linear scan with no spatial index; there is no k-d
  tree, no ball tree and no approximate search of any kind.
- **Tie-breaking:** neighbours are selected with
  `Enumerable.Range(0, _rows.Length).OrderBy(i => distancesSquared[i]).Take(NeighbourCount)` (lines 103-106). LINQ's
  `OrderBy` is a stable sort, so rows tied on distance keep their original storage order — the lowest-index
  (earliest-inserted) tied row always wins. This is an implicit rule; nothing in the DTO or its validator documents it.
- **k validated against dataset size:** yes, in two places.
  `KNearestNeighboursClassifierTrainer.TrainWithSplit` throws before any fitting if
  `trainX.Length < options.NeighbourCount` (lines 91-94), and `KNearestNeighboursClassifierPipeline`'s constructor
  repeats the same check defensively (lines 39-42). There is no equivalent check against the validation or test split
  sizes, only against the training split.
- **Vote fraction to probability:** the raw vote fraction (share of the k neighbours labelled 1, computed at
  `KNearestNeighboursClassifierPipeline.Explain` lines 134-135) is not returned as-is. When `Calibrate` is true and the
  validation split contains both classes it is passed through `Jube.Analytics.PlattCalibration.Fit`
  (`Jube.Analytics/PlattCalibration.cs:18-165`) — a from-scratch re-implementation of Platt scaling with the
  Lin/Weng/Keerthi (2007) target smoothing (`high`/`low` at lines 88-89) and a damped-Newton line search (lines
  110-162). The vendored
  `Accord.MachineLearning.VectorMachines.Learning.Probabilistic.ProbabilisticOutputCalibration` class
  (`Accord.MachineLearning/VectorMachines/Learning/Probabilistic/ProbabilisticOutputCalibration.cs`) is never referenced
  by this family.
- **Snapshot cost:** the persisted model (`AdvancedAnalyticsKNearestNeighboursClassifierPipelineSnapshot`, populated at
  `KNearestNeighboursClassifierTrainer.cs:243-262`) stores every standardised, imputed training row together with its
  label. Recall re-runs the same brute-force scan against those stored rows, so recall cost, not just snapshot size,
  scales linearly with the training split's row count — the trainer's own
  `LargeSnapshotWarningThreshold` (200,000 rows, lines 34, 237-241) exists specifically because there is no
  sparsification of the stored rows at any complexity or neighbour count.
