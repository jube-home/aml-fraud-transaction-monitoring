---
layout: default
title: AdaBoost Classifier
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 4
---

# AdaBoost Classifier

## Procedure: AdaBoost classifier

AdaBoost fits a sequence of shallow trees, each one re-weighting the training rows to focus on the cases the previous
trees got wrong, and combines them into a single weighted vote.

```R
library(ada)

set.seed(1)
n <- 200
amount <- c(rnorm(n / 2, mean = 40, sd = 15), rnorm(n / 2, mean = 220, sd = 60))
velocity <- c(rpois(n / 2, 2), rpois(n / 2, 9))
fraud <- c(rep(0, n / 2), rep(1, n / 2))
train <- data.frame(amount, velocity, fraud)

model <- ada(fraud ~ amount + velocity, data = train, iter = 100,
             control = rpart::rpart.control(maxdepth = 2, minsplit = 10))
print(model)

new_cases <- data.frame(amount = c(35, 260), velocity = c(1, 10))
predict(model, new_cases, type = "probs")
```

Console output:

```
Loading required package: rpart
Call:
ada(fraud ~ amount + velocity, data = train, iter = 100, control = rpart::rpart.control(maxdepth = 2, 
    minsplit = 10))

Loss: exponential Method: discrete   Iteration: 100 

Final Confusion Matrix for Data:
          Final Prediction
True value   0   1
         0 100   0
         1   0 100

Train Error: 0 

Out-Of-Bag Error:  0  iteration= 6 

Additional Estimates of number of iterations:

train.err1 train.kap1 
         1          1 

             [,1]         [,2]
[1,] 1.000000e+00 1.001001e-40
[2,] 1.001001e-40 1.000000e+00
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsAdaBoostClassifierTrainingService` / tool
`SubmitAdaBoostClassifierTraining` — `Kind = Classifier`, recall class `AdaBoostClassifierRecall`. `Rounds`
(default 100) is `ada`'s `iter`; `MaxDepth` (default 2, intentionally shallow — AdaBoost's weak learners are meant to be
stumps or near-stumps) and `MinimumLeafSize` mirror the `rpart.control` above. Note the near-zero off-diagonal
probability the R run above actually produced on perfectly separated toy data: discrete AdaBoost with a training error
of zero drives its exponential weights towards infinity, a known instability the service's own calibration and
`DecisionThreshold` handling exists to keep in check rather than expose raw.

## Code audit

`SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsAdaBoostClassifierTrainingService.cs`)
only validates and queues a run; the actual training runs when `AdvancedAnalyticsTrainingWorker` calls
`AdvancedAnalyticsAdaBoostClassifierTrainingExecutor.ExecuteAsync`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:880-882`), whose `Trainer`
delegate defaults to `AdaBoostClassifierTrainer.Train`
(`Jube.Engine/AdvancedAnalytics/Classification/AdaBoostClassifierTrainer.cs:36-58`).

### No Accord.NET dependency

`AdaBoostClassifierTrainer.cs` has no `using Accord.*`. There is no reference anywhere in this file, or in
`Jube.Engine/AdvancedAnalytics/Classification/AdaBoostTree.cs`, to Accord's boosting types. `Accord.MachineLearning`
does ship a `Boosting<TModel>`/`AdaBoost`-style API in this codebase's vendored copy, but it is not the code path this
training service uses; the implementation here is a self-contained discrete AdaBoost written against the same
shallow-tree building block used by the Random Forest and Bagging trainers.

### Which variant: discrete AdaBoost (SAMME reduces to the same formula at two classes)

Per round `m`, a weighted weak learner is grown (`BuildWeightedTree`, lines 396-511) and its **hard** ±1 decision is
read via `AdaBoostTree.Sign` (`AdaBoostTree.cs`: `_nodeVote[leaf] >= 0.5 ? 1 : -1`) — the tree does not expose a soft
probability to the boosting loop, only a sign. The weighted error is
`err = Σ weights[i] where sign(x_i) != y_i` (lines 164-174), clamped to `[1e-10, 1-1e-10]`
(`ErrorEpsilon`, line 176) to avoid a zero or one error producing an infinite or undefined `alpha`. The step size is
`alpha = 0.5 * log((1 - err) / err)` (line 185).

This is precisely Freund and Schapire's original discrete AdaBoost (AdaBoost.M1) update. It is also identical to what
Zhu et al.'s SAMME algorithm reduces to when the number of classes `K = 2`, because SAMME's extra `+ log(K - 1)` term is
`log(1) = 0` at `K = 2` — the two formulations are indistinguishable for a binary classifier, and this code only ever
trains binary labels (`ValidateData`, lines 542-578, requires `labels[i] ∈ {0, 1}`). There is no real-valued ("Real
AdaBoost"/SAMME.R, class-probability-weighted)
variant here: the per-round contribution to the score is the hard sign, never the leaf's continuous
`weightedMean` (line 418), which is used only to decide the sign and, separately, as the split-search impurity input.

### Row reweighting, not resampling

Unlike the Random Forest and Bagging trainers, `AdaBoostClassifierTrainer` does not bootstrap rows at all:
`BuildWeightedTree` is called on `Enumerable.Range(0, design.Length).ToArray()` — every training row, every round (line

507) — with per-row `weights` (initialised uniform at line 145-148, then updated multiplicatively after each round,
     lines 194-213) folded directly into the impurity calculation (`weightedPositive`, `totalWeight`, lines 409-430,
     455-471)
     instead of changing which rows are present. This means there is no bootstrap-excluded set of rows for any round, so
     an out-of-bag error in the bagging sense does not naturally exist for this algorithm — and indeed
     `AdaBoostClassifierTrainer.cs` computes no out-of-bag metric anywhere (no `OutOfBag*` field appears in
     `AdaBoostEvidenceDetails`, unlike
     `RandomForestEvidenceDetails.OutOfBagRocAuc` and `BaggingEvidenceDetails.OutOfBagRocAuc`).

What the class does track per round is the weighted training error itself (`RoundErrors`, line 152, 189, exposed as
`AdaBoostEvidenceDetails.RoundErrors`), which is the closest analogue AdaBoost has to a per-iteration diagnostic, but it
is a training-set statistic, not a held-out one.

### Weight update and reweighting formula

After each round, `factor = exp(alpha * (correct ? -1 : 1))` (line 200) — correctly-classified rows are down-weighted by
`exp(-alpha)`, misclassified rows up-weighted by `exp(alpha)`, then renormalised to sum to 1 (lines 205-213). If the
renormalising sum is non-finite or non-positive, boosting stops early with a recorded warning (`EarlyStopping`, lines
205-210, 220-221) rather than continuing with corrupted weights — this guards exactly the instability the existing doc
text calls out (exponential weights on perfectly separable data driving towards infinity). Boosting also stops early,
before reaching `Rounds`, the first time a round's weak learner is no better than chance (`err >= 0.5`, lines 178-183) —
a normal discrete-AdaBoost termination condition, since `alpha` would otherwise be zero or negative.

### Split criterion

`BuildWeightedTree` (lines 396-511) uses weighted Gini-proportional impurity (`weightedMean * (1 - weightedMean)`
against **weighted** class proportions, lines 418, 430, 473-477) — the weight-aware analogue of the Random
Forest/Bagging split criterion, needed because AdaBoost's rows carry per-round importance weights rather than being
present or absent. Minimum leaf size and max depth are enforced identically to the other two tree-based families (lines
427, 450).

### Aggregation: alpha-weighted margin, normalised, then calibrated

The ensemble's raw score (`RawScores`, lines 230-252) is
`Σ_t alpha_t * sign_t(x) / Σ_t alpha_t` — an alpha-weighted vote of every round's hard sign, normalised into
`[-1, 1]` by the total alpha mass, rather than the boosted strong classifier's usual final `sign(Σ alpha_t h_t(x))`
decision. Keeping the continuous normalised margin (instead of collapsing straight to a hard class label)
is what allows the subsequent Platt-calibration step (`PlattCalibration.Fit`, lines 258-282, same mechanism described in
`RandomForestClassifier.md`) to fit a probability against it; `DecisionThreshold` (default 0.5)
is then applied to that calibrated probability, not to the raw margin, to produce the reported confusion counts (lines
290-294).

### Feature importance

`featureImportance[j] += alpha * importanceThisRound[j]` (line 192) — each round's split-gain importance is scaled by
that round's own `alpha`, so rounds that turned out to be more trustworthy (lower weighted error, larger `alpha`)
contribute more to the final normalised importance (lines 223-226) than rounds that were barely better than chance.
