---
layout: default
title: K-Nearest-Neighbours Regressor
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 6
---

# K-Nearest-Neighbours Regressor

The regression counterpart predicts the plain mean of the k nearest neighbours' targets — no vote fraction to calibrate,
since that mean is already on the target's own scale.

## R equivalent

```R
library(FNN)

set.seed(1)
n <- 200
account_age_days <- runif(n, 1, 2000)
avg_monthly_txn <- runif(n, 1, 60)
settlement_days <- 12 - 0.004 * account_age_days + 0.05 * avg_monthly_txn +
  rnorm(n, 0, 1.5)

train_x <- scale(data.frame(account_age_days, avg_monthly_txn))
new_cases <- data.frame(account_age_days = c(30, 1500), avg_monthly_txn = c(5, 45))
new_x <- scale(new_cases, center = attr(train_x, "scaled:center"),
               scale = attr(train_x, "scaled:scale"))

result <- knn.reg(train = train_x, test = new_x, y = settlement_days, k = 15)
print(result$pred)
```

Console output:

```
[1] 11.455776  8.206563
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsKNearestNeighboursRegressorTrainingService` / tool
`SubmitKNearestNeighboursRegressorTraining` — `Kind = Regressor`, recall class `KNearestNeighboursRegressorRecall`. Same
`NeighbourCount`/`Standardise` shape as the classifier, no calibration fields, same stored-rows recall model.

## Code audit

`AdvancedAnalyticsKNearestNeighboursRegressorTrainingService` delegates to
`KNearestNeighboursRegressorTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Regression/KNearestNeighboursRegressorTrainer.cs:33-55`), the regression sibling of the
classifier trainer, and it shares the same hand-written implementation: no
`Accord.MachineLearning.*` type is instantiated anywhere in this family either.

- **Distance metric and search structure:** identical to the classifier — squared Euclidean distance summed per
  variable, brute-force linear scan of every stored row (`KNearestNeighboursRegressorPipeline.Explain`,
  `Jube.Engine/AdvancedAnalytics/Regression/KNearestNeighboursRegressorPipeline.cs:84-107`; square root taken only for
  the reported distances at line 118, not for ranking).
- **Tie-breaking:** the same stable-sort rule as the classifier —
  `Enumerable.Range(0, _rows.Length).OrderBy(i => distancesSquared[i]).Take(NeighbourCount)` (lines 104-107) — so
  equidistant neighbours are resolved by ascending storage index.
- **k validated against dataset size:** `KNearestNeighboursRegressorTrainer.TrainWithSplit` throws if
  `trainX.Length < options.NeighbourCount` before fitting (lines 82-85), and
  `KNearestNeighboursRegressorPipeline`'s constructor repeats the check (lines 40-43). As with the classifier, only the
  training split size is checked, not validation or test.
- **Prediction:** the plain arithmetic mean of the k neighbours' stored targets (`neighbourTargets.Average()`,
  `KNearestNeighboursRegressorPipeline.Explain` line 135) — an unweighted mean, not a distance-weighted
  (inverse-distance) average; every one of the k neighbours contributes equally regardless of how much closer some are
  than others.
- **No calibration surface:** `KNearestNeighboursRegressorOptions`
  (`Jube.Engine/AdvancedAnalytics/Regression/KNearestNeighboursRegressorOptions.cs:19-38`) carries no
  `Calibrate` or `DecisionThreshold` field at all — confirmed by reading the options class directly, not inferred —
  since a regression target has nothing analogous to calibrate.
- **Snapshot cost:** as with the classifier, every standardised, imputed training row and its target value is stored in
  the model version (`AdvancedAnalyticsKNearestNeighboursRegressorPipelineSnapshot`,
  `KNearestNeighboursRegressorTrainer.cs:187-200`), and recall repeats the same brute-force search, so recall cost
  scales with the training split size; the same `LargeSnapshotWarningThreshold` (200,000 rows, lines 31, 181-185)
  documents this directly in a training warning.
