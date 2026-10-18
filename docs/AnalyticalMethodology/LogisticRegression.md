---
layout: default
title: Logistic Regression
nav_order: 14
parent: Analytical Methodology
---

# Logistic Regression

Logistic regression predicts a binary (1/0) outcome — here, whether a debit-card transaction is fraudulent — from one or
more predictors. The dataset is `FraudRisk.csv`, 1,827 transactions split close to evenly between fraud and legitimate.
The copy used for this module ships with the categorical
`Type` field already pivoted into `Is_Chip`, `Is_Swipe` and `Is_Manual` 0/1 columns rather than as a raw character
column; Procedure 1 reconstructs a `Type` character column from those flags first, purely so the pivoting technique
itself can be demonstrated against real data. Two packages beyond the shared set were installed into the validation
container's library path: `ROCR` (Procedures 8 and 9) and `gmodels`
(Procedure 6). `dplyr` and `ggplot2` are both in the shared set already.

### Procedure 1: Pivot a Categorical Variable for Regression Analysis

Character and numeric-label fields need pivoting before they are useful to a model that expects numbers.
`factor()` is the labour-saving route: it converts a character column into a set of levels R itself knows how to use in
a formula, without pivoting every distinct value into its own column by hand.

```R
FraudRisk <- read.csv("/work/FraudRisk.csv", stringsAsFactors = FALSE)

Type <- ifelse(FraudRisk$Is_Chip == 1, "Chip", ifelse(FraudRisk$Is_Swipe == 1, "Swipe", "Manual"))
str(Type)

FraudRisk <- mutate(FraudRisk, Type)

TypeFactor <- factor(FraudRisk$Type)
table(TypeFactor)

FraudRisk <- mutate(FraudRisk, TypeFactor)
```

Console output:

```
 chr [1:1827] "Chip" "Chip" "Chip" "Chip" "Chip" "Chip" "Chip" "Chip" ...

TypeFactor
  Chip Manual  Swipe 
  1087    217    523 
```

A single condition can also be pivoted by hand with `ifelse()`, without going through `factor()` at all — useful when
only one category matters, or as part of horizontal abstraction:

```R
IsHighRisk <- ifelse(FraudRisk$Type == "Manual", 1, 0)
table(IsHighRisk)

FraudRisk <- mutate(FraudRisk, IsHighRisk)
```

Console output:

```
IsHighRisk
   0    1 
1610  217 
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.EncodingTools`
/ tool `"FitOneHotEncoder"` (with `"ApplyOneHotEncoder"` for new rows) — expands a categorical column into one 0/1
column per distinct category, named `<column>=<category>`, in one call over the whole table rather than a `factor()`
plus a manual `ifelse()` per level. `IsHighRisk` above is exactly the
`Type=Manual` column of that output with `dropFirst` left false; there is no separate tool for pivoting a single
condition by hand. A single categorical column reduced to one integer code (rather than one 0/1 column per level) is
`"FitLabelEncoder"`, covered in `DataStructures.md` Procedure 14.

### Procedure 2: Create an Abstraction Deviation Independent Vector

Comparing a value against the typical value for that vector is one of the highest-value transformations in behavioural
analytics. A Z score is the usual choice, but a range (min-max) normalisation suits skewed data better: it places a
value on a 0–1 scale relative to the smallest and largest values observed.

```R
Min_Count_Transactions_1_Day <- min(FraudRisk$Count_Transactions_1_Day)
Max_Count_Transactions_1_Day <- max(FraudRisk$Count_Transactions_1_Day)
c(Min_Count_Transactions_1_Day, Max_Count_Transactions_1_Day)

Range_Deviation_Count_Transactions_1_Day <- (FraudRisk$Count_Transactions_1_Day -
  Min_Count_Transactions_1_Day) / (Max_Count_Transactions_1_Day -
  Min_Count_Transactions_1_Day)
summary(Range_Deviation_Count_Transactions_1_Day)

FraudRisk <- mutate(FraudRisk, Range_Deviation_Count_Transactions_1_Day)
```

Console output:

```
[1]  1 26
   Min. 1st Qu.  Median    Mean 3rd Qu.    Max. 
 0.0000  0.0400  0.1200  0.1785  0.2800  1.0000 
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tool `"FitTableScaler"` with `method: MinMax` (paired with `"ApplyTableScaler"` for new rows) — for every column in
one call, `(value - minimum) / (maximum - minimum)` using the fitted `Centers` (the minimums) and `Scales` (the ranges),
so training and live rows are normalised identically instead of the minimum and maximum being hand-carried in two loose
vectors. `Standard` (z-score), `RobustMad` and
`RobustIqr` are alternative methods on the same tool for the less-skewed case the module mentions in passing.

### Procedure 3: Fit a one-way Log Curve on a Plot

Before modelling, it helps to see whether a candidate predictor actually discriminates the outcome. Plotting a 0/1
dependent variable against a candidate predictor puts every point on `y = 0` or `y = 1`
with nothing in between; overlaying a `glm(family = "binomial")` smooth turns that into a curve showing the estimated
probability of fraud across the predictor's range.

```R
library(ggplot2)

qplot(FraudRisk$Count_Unsafe_Terminals_1_Day, FraudRisk$Dependent)
ggsave("/work/plot1.png", width = 5, height = 4)

qplot(FraudRisk$Count_Unsafe_Terminals_1_Day, FraudRisk$Dependent) +
  stat_smooth(method = "glm", method.args = list(family = "binomial"))
ggsave("/work/plot2.png", width = 5, height = 4)
```

Console output:

```
Warning message:
`qplot()` was deprecated in ggplot2 3.4.0. 
`geom_smooth()` using formula = 'y ~ x'
`geom_smooth()` using formula = 'y ~ x'
```

**AdvancedAnalytics equivalent:** `Jube.Charting`, `Chart.Roc()` — the question this procedure is really asking ("does
this one predictor separate fraud from legitimate?") is exactly what a ROC curve answers, more precisely than eyeballing
a fitted sigmoid over a scatter of 0/1 points. `Chart.Roc().FromScores("Count_Unsafe_Terminals_1_Day",
actual, rawValues).Render()` treats the raw candidate predictor itself as the score, computes the curve (handling ties,
thinning to at most 2,000 points) and puts the AUC in the legend with the best cut-off (Youden J) marked, so the
discrimination judgement is a number and a curve instead of a shape read off a smoothed line. Never ggplot2 or a raw R
graphics device for this in the product — see `Charting.md`.

### Procedure 4: Forward Stepwise Logistic Regression

`glm()` with `family = "binomial"` fits a logistic regression the same way `lm()` fits a linear one, plus the family
argument. `summary()` gives the model output; `coefficients()` gives the numbers with enough precision to deploy by
hand. Adding the factor from Procedure 1 extends it to a multiple logistic regression, one dummy coefficient per
non-reference level.

```R
LogisticRegressionModel <- glm(Dependent ~
  Count_Unsafe_Terminals_1_Day, data = FraudRisk, family = "binomial")
summary(LogisticRegressionModel)

coefficients(LogisticRegressionModel)

LogisticRegressionModelStepwise <- glm(Dependent ~
  Count_Unsafe_Terminals_1_Day + TypeFactor, data = FraudRisk, family = "binomial")
coefficients(LogisticRegressionModelStepwise)
```

Console output:

```

Call:
glm(formula = Dependent ~ Count_Unsafe_Terminals_1_Day, family = "binomial", 
    data = FraudRisk)

Coefficients:
                             Estimate Std. Error z value Pr(>|z|)    
(Intercept)                  -0.82552    0.06253  -13.20   <2e-16 ***
Count_Unsafe_Terminals_1_Day  0.44032    0.02692   16.36   <2e-16 ***
---
Signif. codes:  0 '***' 0.001 '**' 0.01 '*' 0.05 '.' 0.1 ' ' 1

(Dispersion parameter for binomial family taken to be 1)

    Null deviance: 2532.4  on 1826  degrees of freedom
Residual deviance: 1976.7  on 1825  degrees of freedom
AIC: 1980.7

Number of Fisher Scoring iterations: 5

                 (Intercept) Count_Unsafe_Terminals_1_Day 
                  -0.8255244                    0.4403157 
                 (Intercept) Count_Unsafe_Terminals_1_Day 
                  -1.1538985                    0.2956514 
            TypeFactorManual              TypeFactorSwipe 
                   1.0340266                    1.8307673 
```

**Tool equivalent:** two paths, and neither does automated forward/backward variable selection for a
binomial model — `"StepwiseLinearRegression"`
(`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.RegressionModelSelectionTools`) is OLS-only, and
`EliminateFeaturesRecursively`
(`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureSelectionTools`) ranks by a linear-model
coefficient, not a logistic one. What the product does have:

- `GeneralisedLinearModelTools.FitGeneralisedLinearModel` (family `Binomial`, link `Logit`, its default)
  fits exactly this model — deviance, null deviance, pseudo R², dispersion, log-likelihood, AIC, BIC, a likelihood-ratio
  test against intercept-only, and a coefficient table with odds ratios — in one call over whichever predictors are
  supplied; there is no growing formula, because the whole predictor set is given up front. (`FitProbitRegression` and
  `FitComplementaryLogLogRegression` on the same class are binomial siblings with a different link, not a logit-specific
  shortcut.)
- `LogisticRegressionTools.AssessPredictorSignificance` is the tool-layer's stand-in for "assume the next strongest
  correlating factor": it screens every candidate predictor on its own against the 0/1 outcome and returns each one's
  odds ratio, Wald p-value, Benjamini-Hochberg adjusted p-value and raw-variable ROC AUC, so the choice of what to add
  next is read off a ranked table instead of guessed at.
- For the classifier-grade version of this same fit (train/validation/test split, calibration, held-out metrics, a
  stored snapshot) see Procedure 5's mapping, `TrainLogisticRegression`.

### Procedure 5: Recalling a Logistic Regression Model

`predict.glm()` scores every row of a data frame against a fitted model, the same signature as
`predict.lm()`.

```R
AutomaticLogisticRegression <- predict.glm(LogisticRegressionModel, FraudRisk)
head(AutomaticLogisticRegression)

FraudRisk <- mutate(FraudRisk, AutomaticLogisticRegression)
```

Console output:

```
          1           2           3           4           5           6 
 0.05510711 -0.82552437 -0.82552437 -0.82552437 -0.82552437  0.05510711 
```

**Tool equivalent:** this single `glm()`-and-`predict.glm()` pairing is where the product stops being a
one-call statistics tool and becomes a governed, versioned model, and it comes in two forms:

- `Jube.Service.Agent.AdvancedAnalyticsTools.LogisticRegressionTools` / tool `"TrainLogisticRegression"`
  fits with a train/validation/test split, optional ridge penalty and class weighting, Platt calibration on the
  validation rows, and returns a JSON model snapshot together with held-out metrics, a coefficient table with odds
  ratios, and diagnostic warnings — the interactive path, capped at 2,000 rows, with **nothing persisted**: the caller
  holds the snapshot and passes it back into
  `"RecallLogisticRegression"` to score new rows, exactly filling `predict.glm()`'s role but against a model that was
  itself evaluated on data it never trained on.
- `Jube.Service.Agent.AdvancedAnalyticsTools.LogisticRegressionTrainingRunTools` / tool
  `"SubmitLogisticRegressionTraining"` queues the same fit as a background run against
  `Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsTrainingService` (reached through
  `AdvancedAnalyticsTrainingRunsGateway`, which implements `IAdvancedAnalyticsTrainingRuns` solely for logistic
  regression — it is not shared with the thirty-one trainable model families in
  `Introduction.md`, each of which has its own dedicated training service). This path **does persist**
  a trained model and version row (`TrainedModelId`, `TrainedModelVersionId`), reachable via
  `"GetTrainingRunResult"`, though training never makes that version live — promotion is a separate, deliberate step.
  Use this path once a dataset is bigger than the 2,000-row synchronous cap, or already sits in the Advanced Analytics
  cache.

Logistic regression is therefore neither purely a stateless GLM tool call nor a training service identical in shape to
the thirty-one model families: it is its own dedicated persisted-model pathway, plus a separate nothing-persisted
quick-fit path for exploration, both distinct from
`FitGeneralisedLinearModel`'s plain one-shot regression fit in Procedure 4.

### Procedure 6: Activating Logistic Regression and Creating a Confusion Matrix

A logistic regression's raw output is a log-odds score, roughly -5 to +5, where 0 is the 50/50 point. Activating at 0
turns the score into a 1/0 prediction; `table()` (or, with friendlier output, `gmodels`'s
`CrossTable()`) compares that prediction against the actual outcome.

```R
ActivateAutomaticLogisticRegression <- ifelse(AutomaticLogisticRegression > 0, 1, 0)

FraudRisk <- mutate(FraudRisk, ActivateAutomaticLogisticRegression)

table(FraudRisk$Dependent, FraudRisk$ActivateAutomaticLogisticRegression)

library("gmodels")
CrossTable(FraudRisk$Dependent, FraudRisk$ActivateAutomaticLogisticRegression)
```

Console output:

```
   
      0   1
  0 818 108
  1 311 590

 
   Cell Contents
|-------------------------|
|                       N |
| Chi-square contribution |
|           N / Row Total |
|           N / Col Total |
|         N / Table Total |
|-------------------------|

 
Total Observations in Table:  1827 

 
                    | FraudRisk$ActivateAutomaticLogisticRegression 
FraudRisk$Dependent |         0 |         1 | Row Total | 
--------------------|-----------|-----------|-----------|
                  0 |       818 |       108 |       926 | 
                    |   105.563 |   170.746 |           | 
                    |     0.883 |     0.117 |     0.507 | 
                    |     0.725 |     0.155 |           | 
                    |     0.448 |     0.059 |           | 
--------------------|-----------|-----------|-----------|
                  1 |       311 |       590 |       901 | 
                    |   108.492 |   175.483 |           | 
                    |     0.345 |     0.655 |     0.493 | 
                    |     0.275 |     0.845 |           | 
                    |     0.170 |     0.323 |           | 
--------------------|-----------|-----------|-----------|
       Column Total |      1129 |       698 |      1827 | 
                    |     0.618 |     0.382 |           | 
--------------------|-----------|-----------|-----------|

 
```

**Tool equivalent:** the activation-and-count-table work is already done for you.
`"TrainLogisticRegression"`'s `TestMetrics` (`LogisticSplitMetrics`,
`Jube.Engine.AdvancedAnalytics.Regression`) carries `TruePositives`, `FalsePositives`, `TrueNegatives`,
`FalseNegatives`, `Precision`, `Recall`, `F1` and `MatthewsCorrelation` at a chosen or cost-minimising
`Threshold`, computed once alongside every other held-out metric rather than requiring a separate
`table()` or `CrossTable()` call against a manually activated vector. `"CrossValidateLogisticRegression"`'s per-fold
`ClassificationMetrics` (`Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation`) carries the same four counts plus
`Accuracy`, `Specificity` and `BalancedAccuracy` directly. For the visual rendering of those same four counts,
`Jube.Charting`
`Chart.ConfusionMatrix().Binary(trueNegative, falsePositive, falseNegative, truePositive).Rates().Render()`
(or `.FromPredictions(actual, predicted)` straight from two label vectors) replaces `CrossTable()`'s printed layout with
row/column/overall percentages on hover and accuracy in the description — never ggplot2 or base R graphics.

### Procedure 7: Output Logistic Regression Model as Probability

A logistic regression's raw score converts to a probability with `P = exp(score) / (1 + exp(score))`, applied vectorised
over the whole prediction column.

```R
PAutomaticLogisticRegression <- exp(AutomaticLogisticRegression) / (1 + exp(AutomaticLogisticRegression))
head(PAutomaticLogisticRegression)

FraudRisk <- mutate(FraudRisk, PAutomaticLogisticRegression)
```

Console output:

```
        1         2         3         4         5         6 
0.5137733 0.3045922 0.3045922 0.3045922 0.3045922 0.5137733 
```

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.GeneralisedLinearModelTools`
/ tool `"ApplyLinkFunction"` (link `Logit`, `inverse: true`) applies the same logistic function generally, for any link,
with out-of-domain values reported through `InvalidCount` instead of silently producing
`NaN`. In practice this manual conversion is rarely needed at all: `"TrainLogisticRegression"` and
`"RecallLogisticRegression"` already return a probability directly — the log-odds score never has to leave the model and
be converted back by hand.

### Procedure 8: Creating a ROC Curve

The `ROCR` package appraises a classifier by comparing a probability prediction against the actual outcome across every
possible threshold at once. `prediction()` builds the comparison object;
`performance()` with `measure = "tpr"` and `x.measure = "fpr"` turns it into true-positive-rate against
false-positive-rate, plotted with base graphics.

```R
library(ROCR)
ROCRPredictions <- prediction(FraudRisk$PAutomaticLogisticRegression, FraudRisk$Dependent)

ROCRPerformance <- performance(ROCRPredictions, measure = "tpr", x.measure = "fpr")

png("/work/roc.png")
plot(ROCRPerformance)
invisible(dev.off())
cat("ROC plot written\n")
```

Console output:

```
ROC plot written
```

**AdvancedAnalytics equivalent:** `Jube.Charting`, `Chart.Roc()` — the same `RocChartBuilder` named in Procedure 3.
`Chart.Roc().FromScores("Logistic regression", actual, FraudRisk.PAutomaticLogisticRegression).Render()`
replaces the `prediction()`/`performance()`/`plot()` pipeline with one call: it computes the curve (`CurveMath.Roc`,
ties handled, thinned to at most 2,000 points), draws the chance diagonal, marks the best cut-off by Youden's J, and
puts the AUC straight in the legend rather than requiring Procedure 9's separate `performance(..., measure = "auc")`
call — never `ROCR`'s base-graphics `plot()` in the product.

### Procedure 9: Grading the ROC Performance with AUC

The Area Under Curve statistic reduces the ROC curve's shape to one number between 0.5 (no better than chance) and 1
(perfect separation), read off the same `performance()` object with `measure = "auc"`.

```R
AUC <- performance(ROCRPredictions, measure = "auc")
AUC

AUC@y.values
```

Console output:

```
A performance instance
  'Area under the ROC curve'
[[1]]
[1] 0.8034911

```

By the module's own grading rubric (A: Outstanding > 0.9, B: Excellent > 0.8 and <= 0.9, C: Acceptable >
0.7 and <= 0.8, D: Poor > 0.6 and <= 0.7, E: Junk > 0.5 and <= 0.6) this model's 0.803 grades B, excellent.

**Tool equivalent:** `RocAuc` (with `RocAucLower95`/`RocAucUpper95`, a 95% interval `performance()`
does not give at all) is a plain field on both the `LogisticSplitMetrics` and `ClassificationMetrics`
results named in Procedure 6's mapping — returned automatically alongside every other held-out metric the moment
`"TrainLogisticRegression"` or `"CrossValidateLogisticRegression"` runs, never a second call. There is no tool-layer
equivalent for the literal A–E letter-grade bucketing: the product reports the continuous statistic and its confidence
interval, plus a plain-language comparison against the training-set AUC in
`ClassifierEvaluationResult.Interpretation`, and leaves the qualitative label to the reader.
