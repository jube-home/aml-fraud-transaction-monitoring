---
layout: default
title: Random Forest Regressor
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 2
---

# Random Forest Regressor

## Procedure: random forest regressor

The same bagged-tree idea for a continuous target: each tree predicts a number, and the forest predicts their plain
average.

```R
library(randomForest)

set.seed(1)
n <- 200
account_age_days <- runif(n, 1, 2000)
avg_monthly_txn <- runif(n, 1, 60)
settlement_days <- 12 - 0.004 * account_age_days + 0.05 * avg_monthly_txn +
  rnorm(n, 0, 1.5)
train <- data.frame(account_age_days, avg_monthly_txn, settlement_days)

model <- randomForest(settlement_days ~ account_age_days + avg_monthly_txn,
                       data = train, ntree = 100, mtry = 1, nodesize = 5)
print(model)

new_cases <- data.frame(account_age_days = c(30, 1500), avg_monthly_txn = c(5, 45))
predict(model, new_cases)
```

Console output:

```
randomForest 4.7-1.2
Type rfNews() to see new features/changes/bug fixes.

Call:
 randomForest(formula = settlement_days ~ account_age_days + avg_monthly_txn,      data = train, ntree = 100, mtry = 1, nodesize = 5) 
               Type of random forest: regression
                     Number of trees: 100
No. of variables tried at each split: 1

          Mean of squared residuals: 2.77573
                    % Var explained: 63.06
        1         2 
11.091427  6.656765 
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsRandomForestRegressorTrainingService` / tool
`SubmitRandomForestRegressorTraining` — `Kind = Regressor`, recall class `RandomForestRegressorRecall`. Same
`TreeCount`/`MaxDepth`/`MinimumLeafSize`/`FeatureFraction`/`SampleFraction` shape as the classifier, but no
`Calibrate`/`DecisionThreshold` fields: a regressor's forest average is already on the target's own scale, with nothing
to calibrate.

## Code audit

`SubmitTrainingAsync` only validates and queues a run row; the actual training runs when
`AdvancedAnalyticsTrainingWorker` calls
`AdvancedAnalyticsRandomForestRegressorTrainingExecutor.ExecuteAsync`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:1055-1057`), whose `Trainer`
delegate defaults to `RandomForestRegressorTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Regression/RandomForestRegressorTrainer.cs:33-55`).

### No Accord.NET dependency

`RandomForestRegressorTrainer.cs` has no `using Accord.*`. Like the classifier, the forest is a hand-written
implementation local to this file and `Jube.Engine/AdvancedAnalytics/Regression/RandomForestRegressorTree.cs`; nothing
is instantiated from `Accord.MachineLearning.DecisionTrees` or any other Accord regression-tree type.

### Bootstrap and feature subsampling — a different default rule than the classifier

The bootstrap mechanics are identical to the classifier: `sampleSize = round(SampleFraction * trainDesign.Length)`
rows drawn with replacement per tree (`RandomForestRegressorTrainer.cs:154-159`), independently seeded per tree with the
same `DeriveSeed` formula (lines 408-411).

The feature-subsampling default differs from the classifier's `sqrt(p)`. When `options.FeatureFraction <= 0`,
`featuresPerSplit = Math.Min(variableCount, Math.Max(1, variableCount / 3))` (line 140) — **integer division**, i.e.
`floor(p / 3)` clamped to at least 1. This is the textbook regression rule of thumb (`p/3`, as opposed to the
classification rule `sqrt(p)`), and it is a genuinely different formula from the classifier's default despite
`RandomForestRegressorOptions` and `RandomForestClassifierOptions` sharing an identical
`FeatureFraction` field name and shape. The regressor's own validation error text confirms the intent:
"Feature fraction must be between 0 (the one-third default) and 1" (`RandomForestRegressorOptions.cs`).

For `p` not a multiple of 3, `variableCount / 3` truncates rather than rounds — e.g. `p = 5` gives
`5 / 3 = 1` (not 2), so a small variable count subsamples more aggressively than a literal `p/3` fraction would suggest.

### Split criterion: sum of squared error, not Gini

`BuildNode` (`RandomForestRegressorTrainer.cs:316-386`) computes the node mean and its sum of squared errors (`sse`,
line 319) and stops on `depth >= maxDepth`, too few rows, or `sse <= 1e-12` (near-constant target, line 327). Candidate
splits are scored by weighted post-split SSE reduction (`parentImpurity - weightedImpurity`, lines 330, 359-360), the
standard CART regression criterion (minimise residual variance), not the classifier's Gini-proportional impurity. Leaf
value is the node's mean target (line 325), i.e. this is variance-reduction regression-tree growth with the same
recursive structure, minimum-leaf-size and depth-limit mechanics as the classifier.

### Aggregation: plain average of tree predictions

`RandomForestRegressorTree.Predict` returns the node's stored mean; the forest's score (`RawScores`, lines 208-223) is
the plain arithmetic mean of every tree's prediction — there is no boosting or weighting, matching R's `randomForest`
regression behaviour of averaging tree outputs directly (the console output's "Mean of squared residuals" / "% Var
explained" reflect this same averaging idea, computed differently as OOB metrics inside R's own package).

### Out-of-bag error: computed as R-squared, not AUC

The out-of-bag mechanism is structurally identical to the classifier (rows excluded from a tree's bootstrap sample are
scored by that tree only, lines 184-190), but because the target is continuous the OOB summary is an R-squared computed
by the same `Metrics` helper used for the train/validation/test splits (`outOfBagRSquared`, lines 195-200, via
`Metrics("OutOfBag", oobActual, oobPredicted, variableCount)`), stored as
`RandomForestRegressorEvidenceDetails.OutOfBagRSquared`. As with the classifier, this is only computed when more than
`MinimumOutOfBagRows = 10` rows have any out-of-bag coverage (line 195); otherwise an
`OutOfBagUnavailable` warning is recorded (lines 202-206) with no value substituted.

### Metrics: R-squared, adjusted R-squared, RMSE, MAE, mean error

`Metrics` (lines 413-443) computes `RSquared = 1 - SSE/SST` (guarded against `SST == 0` returning `null`, line 435), an
adjusted R-squared using `n - predictorCount - 1` degrees of freedom (lines 436-439, `null`
when that denominator is not positive), RMSE (`sqrt(SSE/n)`), mean absolute error and mean signed error (bias). These
are computed identically for the train, validation and test splits (lines 229-231) and, when covered rows exist, for the
out-of-bag set.

### No calibration

`RandomForestRegressorOptions` (`Jube.Engine/AdvancedAnalytics/Regression/RandomForestRegressorOptions.cs`)
has no `Calibrate` or `DecisionThreshold` field at all — unlike the classifier's options record, there is nothing to
calibrate.
