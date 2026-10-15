---
layout: default
title: Random Forest Classifier
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 1
---

# Random Forest Classifier

## Procedure: random forest classifier

A random forest bags many decision trees, each grown on a bootstrap resample of the rows and restricted at every split
to a random subset of the variables, then votes the share of trees predicting the event of interest. Bagging and the
restricted split candidates decorrelate the individual trees so the forest's average is far steadier than any one tree.

```R
library(randomForest)

set.seed(1)
n <- 200
amount <- c(rnorm(n / 2, mean = 40, sd = 15), rnorm(n / 2, mean = 220, sd = 60))
velocity <- c(rpois(n / 2, 2), rpois(n / 2, 9))
fraud <- factor(c(rep(0, n / 2), rep(1, n / 2)))
train <- data.frame(amount, velocity, fraud)

model <- randomForest(fraud ~ amount + velocity, data = train, ntree = 100,
                       mtry = 1, nodesize = 5, importance = TRUE)
print(model)

new_cases <- data.frame(amount = c(35, 260), velocity = c(1, 10))
predict(model, new_cases, type = "prob")
```

Console output:

```
randomForest 4.7-1.2
Type rfNews() to see new features/changes/bug fixes.

Call:
 randomForest(formula = fraud ~ amount + velocity, data = train,      ntree = 100, mtry = 1, nodesize = 5, importance = TRUE) 
               Type of random forest: classification
                     Number of trees: 100
No. of variables tried at each split: 1

        OOB estimate of  error rate: 0%
Confusion matrix:
    0   1 class.error
0 100   0           0
1   0 100           0
  0 1
1 1 0
2 0 1
attr(,"class")
[1] "matrix" "array"  "votes" 
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsRandomForestClassifierTrainingService` / tool
`SubmitRandomForestClassifierTraining` — `Kind = Classifier`, recall class `RandomForestClassifierRecall`. The DTO's
`TreeCount` (default 100), `MaxDepth` (default 6), `MinLeafSize` (default 5), `FeatureFraction` (0 means
square-root-of-variable-count, the classification rule of thumb) and `SampleFraction` mirror `ntree`, tree depth,
`nodesize` and `mtry`/bootstrap fraction above. The raw vote fraction has no native probability interpretation, so it is
Platt-calibrated on the validation split (`Calibrate`, default true) rather than read off directly, and
`DecisionThreshold` turns that calibrated probability into the confusion-matrix counts recorded as evidence.

## Code audit

`SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsRandomForestClassifierTrainingService.cs:74-123`)
only validates the request and inserts a queued `AdvancedAnalyticsTrainingRun` row; it does no training itself.
`AdvancedAnalyticsTrainingWorker` picks the run up and calls
`AdvancedAnalyticsRandomForestClassifierTrainingExecutor.ExecuteAsync`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:761-763`), whose `Trainer`
delegate defaults to `RandomForestClassifierTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsRandomForestClassifierTrainingExecutor.cs:59-61`).

### No Accord.NET dependency

`RandomForestClassifierTrainer.cs` (`Jube.Engine/AdvancedAnalytics/Classification/RandomForestClassifierTrainer.cs`)
has no `using Accord.*` at all. The forest, its trees, the splitting criterion, the bootstrap sampling and the
out-of-bag estimate are a hand-written implementation local to this file and
`Jube.Engine/AdvancedAnalytics/Classification/RandomForestTree.cs`; nothing is instantiated from
`Accord.MachineLearning.DecisionTrees`. This is unlike several sibling families in the same directory —
`DecisionTreeClassifierTrainer.cs:19-20` does `using Accord.MachineLearning.DecisionTrees` /
`Accord.MachineLearning.DecisionTrees.Learning`, and `KernelSupportVectorClassifierTrainer.cs`,
`NaiveBayesClassifierTrainer.cs` and others in the same folder do call into Accord. Random Forest, Bagging and AdaBoost
specifically do not.

### Bootstrap and feature subsampling

Each of the `TreeCount` trees is grown in a `Parallel.For` body seeded independently per tree
(`DeriveSeed(options.Seed, t)` at line 157, `unchecked(seed * 7919 + index * 104729 + 17)` at lines 473-476).
`sampleSize = round(options.SampleFraction * trainDesign.Length)` (line 148); rows are then drawn **with replacement**
via `random.Next(trainDesign.Length)` in a loop of `sampleSize` draws (lines 161-166) — a genuine bootstrap, not a fixed
subset. With the default `SampleFraction = 1` the bootstrap draws exactly as many rows as the training split holds, so
roughly `1 - 1/e ≈ 36.8%` of training rows are excluded from any one tree's bag and are available for that tree's
out-of-bag evaluation (this fraction is not computed or asserted in code; it is the standard property of sampling `n`
times with replacement from `n` items).

Feature subsampling per split is `featuresPerSplit` (line 145-147): when `FeatureFraction > 0` it is
`ceiling(FeatureFraction * variableCount)`, else `floor(sqrt(variableCount))` clamped to at least 1 — the textbook
classification default (`sqrt(p)`), matching the doc text above. `SampleFeatures`
(lines 459-471) draws that many distinct feature indices by a partial Fisher–Yates shuffle of `0..total-1`, so it is a
true random subset without replacement, freshly drawn **at every node** of every tree (line 396, called once per
`BuildNode` call), not fixed once per tree.

### Split criterion and tree growth

`BuildNode` (lines 382-451) computes `mean = average(y)` over the node's rows and stops when
`depth >= maxDepth`, `rows.Length < 2 * minLeafSize`, or the node is pure (`mean <= 0` or `mean >= 1`, line 392). The
impurity used is `mean * (1 - mean)` (line 395) — proportional to Gini impurity (`Gini = 2p(1-p)` for two classes), so
it ranks candidate splits identically to Gini despite the missing factor of 2. Candidate thresholds are the midpoints
between consecutive distinct observed values of each sampled feature (lines 406-413), each candidate is rejected if
either side would fall below `MinLeafSize`
(line 416), and the split maximising impurity reduction is kept (`bestGain`, lines 427-434, with a `1e-12`
tie-break tolerance). Recursion builds left and right children (lines 445-448). This is a standard CART-style
classification tree; there is no cost-complexity pruning step.

### Aggregation: averaged probability, not majority vote

`RandomForestTree.Predict` (`RandomForestTree.cs:47-50`) returns the leaf's stored `mean` — the fraction of class-1 rows
that landed in that leaf during training — not a hard 0/1 class label. The forest's raw score (`RawScores`,
`RandomForestClassifierTrainer.cs:216-231`) is the **plain average of every tree's leaf mean**
across all `TreeCount` trees. This is soft-probability averaging (as in scikit-learn's
`RandomForestClassifier.predict_proba`), not a majority hard-vote count of per-tree class predictions.

### Out-of-bag error: computed and exposed

For every tree, rows not drawn into its bootstrap sample (`!built[t].InBag[i]`) are scored by that tree only (lines
191-197), accumulated into `oobSums`/`oobCounts` per row. Rows with at least one out-of-bag tree (`covered`, line 199)
get an out-of-bag probability `oobSums[i] / oobCounts[i]`; `BinaryMetrics.RocAuc` is computed on these against the true
training labels (lines 202-207) and stored as
`RandomForestEvidenceDetails.OutOfBagRocAuc`. If 10 or fewer rows have any out-of-bag coverage
(`MinimumOutOfBagRows = 10`, line 35), the OOB AUC is left `null` and a `OutOfBagUnavailable` warning is recorded
instead (lines 209-213) rather than reporting a noisy estimate.

### Calibration and thresholding

The forest's raw vote-fraction score is fed through `PlattCalibration.Fit` (`Jube.Analytics/PlattCalibration.cs`)
when `options.Calibrate` is true (default) and the validation split contains both classes
(`RandomForestClassifierTrainer.cs:240-260`). `PlattCalibration.Fit` is a genuine implementation of Platt's 1999 method
with the Lin/Lin/Weng target smoothing (`high = (N+ + 1)/(N+ + 2)`, `low = 1/(N- + 2)`,
`PlattCalibration.cs:88-96`) fitted by a damped Newton iteration on a logistic negative-log-likelihood (lines 98-162),
not a naive logistic regression on raw 0/1 labels. `DecisionThreshold` (default 0.5) is then applied to the calibrated
probability to produce the confusion-matrix counts (`RandomForestClassifierTrainer.cs:269-273`).

### Variable importance

Each node's split records `importance[bestFeature] += rows.Length * bestGain` (line 441) — a size-weighted
impurity-reduction sum, the same style as scikit-learn's mean-decrease-impurity importance. These are summed across
every tree (lines 179-182) and normalised to sum to 1 (`featureImportances`, lines 184-186), or left as all-zero if no
split anywhere produced positive gain.

### Divergence from the DTO's `FeatureFraction` doc comment

`RandomForestClassifierOptions.Validate` describes `FeatureFraction = 0` as "the square root default"
(`RandomForestClassifierOptions.cs`, error message text), which matches the classifier's own behaviour
(`floor(sqrt(p))`). The sibling regressor family uses a different zero-default (see
`RandomForestRegressor.md`) — the two are not the same rule despite sharing a field name and options shape.
