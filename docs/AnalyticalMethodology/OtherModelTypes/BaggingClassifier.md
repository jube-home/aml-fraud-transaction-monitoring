---
layout: default
title: Bagging Classifier
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 3
---

# Bagging Classifier

## Procedure: bagging classifier

Plain bagging is the random forest's ingredient without the restricted split candidates: every tree sees every variable
at every split, and only the bootstrap resampling decorrelates the ensemble.

```R
library(ipred)

set.seed(1)
n <- 200
amount <- c(rnorm(n / 2, mean = 40, sd = 15), rnorm(n / 2, mean = 220, sd = 60))
velocity <- c(rpois(n / 2, 2), rpois(n / 2, 9))
fraud <- factor(c(rep(0, n / 2), rep(1, n / 2)))
train <- data.frame(amount, velocity, fraud)

model <- bagging(fraud ~ amount + velocity, data = train, nbagg = 100,
                  coob = TRUE)
print(model)

new_cases <- data.frame(amount = c(35, 260), velocity = c(1, 10))
predict(model, new_cases, type = "prob")
```

Console output:

```

Bagging classification trees with 100 bootstrap replications 

Call: bagging.data.frame(formula = fraud ~ amount + velocity, data = train, 
    nbagg = 100, coob = TRUE)

Out-of-bag estimate of misclassification error:  0 

     0 1
[1,] 1 0
[2,] 0 1
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsBaggingClassifierTrainingService` / tool
`SubmitBaggingClassifierTraining` — `Kind = Classifier`, recall class `BaggingClassifierRecall`. The DTO exposes
`TreeCount`, `MaxDepth`, `MinLeafSize` and `SampleFraction` but no feature-fraction field at all (`ipred`'s
`nbagg`/tree depth match one-for-one; there is no `mtry`-equivalent to set, since bagging by definition offers every
variable at every split). `Calibrate` and `DecisionThreshold` work exactly as in the forest.

## Code audit

`SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsBaggingClassifierTrainingService.cs`)
only validates and queues a run; the actual training runs when `AdvancedAnalyticsTrainingWorker` calls
`AdvancedAnalyticsBaggingClassifierTrainingExecutor.ExecuteAsync`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:857-859`), whose `Trainer`
delegate defaults to `BaggingClassifierTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Classification/BaggingClassifierTrainer.cs:37-59`).

### No Accord.NET dependency

`BaggingClassifierTrainer.cs` has no `using Accord.*`. The trees, the bootstrap loop and the out-of-bag estimate are a
hand-written implementation local to this file and
`Jube.Engine/AdvancedAnalytics/Classification/BaggingTree.cs`; nothing is instantiated from
`Accord.MachineLearning`.

### Near-duplicate of the random forest trainer, minus feature subsampling

The two trainer files are otherwise almost line-for-line identical (same imputation, same standardisation bookkeeping
for reporting only, same `Parallel.For` bootstrap loop, same `DeriveSeed` formula
`unchecked(seed * 7919 + index * 104729 + 17)`, same Platt calibration call, same warning codes, same
`Metrics` helper shape). The one substantive algorithmic difference is in `BuildTree`
(`BaggingClassifierTrainer.cs:370-457`): the candidate-feature loop is
`for (var feature = 0; feature < featureCount; feature++)` (line 403) — **every** variable is a split candidate at every
node, with no call to any `SampleFeatures`-equivalent method and no `featuresPerSplit`
parameter anywhere in the file. This is the textbook distinction between bagging and a random forest (bagging =
bootstrap aggregation only; random forest = bagging + restricted feature subsampling per split), and it is genuinely
implemented that way here, not merely described that way in the DTO.

### Bootstrap sampling

Identical mechanics to the random forest classifier: `sampleSize = round(options.SampleFraction * trainDesign.Length)`
rows drawn with replacement per tree (`BaggingClassifierTrainer.cs:158-163`), independently seeded per tree. With the
default `SampleFraction = 1`, each tree's bootstrap sample is the same size as the training split, leaving the standard
`~1/e` fraction of rows out-of-bag per tree.

### Split criterion and aggregation

`BuildNode` (lines 381-451) uses the same `mean * (1 - mean)` Gini-proportional impurity and the same
midpoint-candidate-threshold search as the random forest classifier, with `MinLeafSize` enforced on both sides of a
candidate split (line 415) and a `1e-12` tie-break tolerance on gain (line 426). `BaggingTree.Predict`
(`BaggingTree.cs`) returns the leaf's stored class-1 proportion (`NodeProbability`), and the ensemble's raw score
(`RawScores`, `BaggingClassifierTrainer.cs:213-228`) is the plain average of every tree's leaf probability —
averaged-probability aggregation, the same as the random forest, not a majority hard-vote.

### Out-of-bag error: computed and exposed

Structurally identical to the random forest classifier: rows left out of a tree's bootstrap sample are scored by that
tree only (lines 188-194), an out-of-bag ROC AUC is computed via `BinaryMetrics.RocAuc` over rows with any out-of-bag
coverage (lines 199-205) when more than `MinimumOutOfBagRows = 10` such rows exist, else an `OutOfBagUnavailable`
warning is recorded (lines 207-211) with no value substituted. This mirrors
`ipred::bagging`'s `coob = TRUE` out-of-bag misclassification estimate shown in the R console output above, though the
product reports AUC rather than a raw misclassification rate.

### Calibration

Same Platt-scaling call as the random forest classifier (`PlattCalibration.Fit`,
`BaggingClassifierTrainer.cs:234-258`), gated on `options.Calibrate` (default true) and on the validation split
containing both classes, with the same calibration-slope sanity checks (`CalibrationSlopeLow`/`CalibrationSlopeHigh`,
0.7/1.3, lines 284-292) and `DecisionThreshold` (default 0.5)
applied to the calibrated probability for the reported confusion counts.

### Variable importance despite no feature restriction

`importance[bestFeature] += rows.Length * bestGain` is still accumulated per split (line 440) and normalised across all
trees into `FeatureImportances` (lines 180-183), exactly as in the random forest. Because every variable is always a
candidate here, this importance reflects each variable's typical contribution when competing against every other
variable at every split, rather than a random-forest-style importance that also reflects how often a variable was even
offered as a candidate.
