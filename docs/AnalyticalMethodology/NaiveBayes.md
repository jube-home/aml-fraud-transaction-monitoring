---
layout: default
title: Naive Bayes
nav_order: 16
parent: Analytical Methodology
---

# Naive Bayesian Classifiers and Laplace Estimator

A naive Bayesian classifier applies Bayes' theorem under one simplifying assumption: every predictor is conditionally
independent of every other predictor, given the class. That assumption is rarely exactly true, but it makes the model
cheap to fit, robust on modest data, and able to explain a prediction as an exact sum of one contribution per
predictor — no approximation needed, unlike almost every other model family. It also returns a probability alongside the
classification, which matters for a risk-based approach: a well-calibrated 30% probability of an outcome should actually
be observed on roughly 30% of the cases it is attached to.

Extra dependency beyond the shared package set: `gmodels`, installed into the validation container's library path for
Procedure 5's `CrossTable()` confusion matrix only.

The original guide's dataset is the same German credit dataset used elsewhere in the book (`CreditRisk`). The bundled
CSV is not distributed with this guide, so the procedures below regenerate an equivalent frame synthetically, with a
fixed seed for reproducibility, instead of `read.csv()` against a path that no longer exists. The column names and
shapes follow the original: a mix of continuous predictors (`Requested_Amount`,
`Installment_Percentage_Of_Disposable_Income`, `Present_Residence_Since`, `Age`,
`Number_Of_Existing_Credits_At_This_Bank`, `Dependent_Persons`, `Duration_In_Month`) and a binary target,
`Dependent` (`Good`/`Bad` credit risk).

### Procedure 1: Converting Continuous Data to Categorical Data

`e1071::naiveBayes()` (used from Procedure 2 onward) builds a frequency table per predictor per class, so every
predictor needs to be a factor first — a continuous column such as `Age` has to be cut into bands before it can be used.
R's `cut()` function takes a vector and a vector of interior break points and returns a factor naming the interval each
value falls into. The same `cut()` call is then repeated for every other continuous column, each with its own sensible
break points, and each result is assigned back over the original column in the data frame.

```R
set.seed(11)

n <- 500
CreditRisk <- data.frame(
  Requested_Amount = round(rlnorm(n, meanlog = 8.2, sdlog = 0.6)),
  Installment_Percentage_Of_Disposable_Income = sample(1:10, n, replace = TRUE),
  Present_Residence_Since = sample(1:10, n, replace = TRUE),
  Age = round(rnorm(n, mean = 40, sd = 14)),
  Number_Of_Existing_Credits_At_This_Bank = sample(1:4, n, replace = TRUE, prob = c(0.55, 0.3, 0.1, 0.05)),
  Dependent_Persons = sample(1:3, n, replace = TRUE, prob = c(0.7, 0.25, 0.05)),
  Duration_In_Month = round(rlnorm(n, meanlog = 3.0, sdlog = 0.5)),
  stringsAsFactors = TRUE
)
CreditRisk$Age <- pmax(18, pmin(90, CreditRisk$Age))

str(CreditRisk)

Age <- cut(CreditRisk$Age, c(18, 24, 34, 44, 54, 64, 74, 999))

head(Age)

CreditRisk$Age <- Age

head(CreditRisk$Age)

#Bin

Requested_Amount <- cut(CreditRisk$Requested_Amount, c(0, 5000, 10000, 15000, 20000, 999999))

Installment_Percentage_Of_Disposable_Income <-
  cut(CreditRisk$Installment_Percentage_Of_Disposable_Income, c(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 999))

Present_Residence_Since <- cut(CreditRisk$Present_Residence_Since, c(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 999))

Number_Of_Existing_Credits_At_This_Bank <-
  cut(CreditRisk$Number_Of_Existing_Credits_At_This_Bank, c(0, 1, 2, 3, 4, 5, 999))

Dependent_Persons <- cut(CreditRisk$Dependent_Persons, c(0, 2, 3, 4, 5, 999))

Duration_In_Month <- cut(CreditRisk$Duration_In_Month, c(0, 20, 40, 60, 999))

#Allocate

CreditRisk$Requested_Amount <- Requested_Amount
CreditRisk$Installment_Percentage_Of_Disposable_Income <- Installment_Percentage_Of_Disposable_Income
CreditRisk$Present_Residence_Since <- Present_Residence_Since
CreditRisk$Number_Of_Existing_Credits_At_This_Bank <- Number_Of_Existing_Credits_At_This_Bank
CreditRisk$Dependent_Persons <- Dependent_Persons
CreditRisk$Duration_In_Month <- Duration_In_Month

str(CreditRisk)
```

Console output:

```
'data.frame':	500 obs. of  7 variables:
 $ Requested_Amount                           : num  2554 3700 1466 1607 7384 ...
 $ Installment_Percentage_Of_Disposable_Income: int  5 3 2 5 5 10 4 3 4 8 ...
 $ Present_Residence_Since                    : int  1 9 1 2 3 10 5 2 9 10 ...
 $ Age                                        : num  24 56 49 18 43 49 48 29 30 42 ...
 $ Number_Of_Existing_Credits_At_This_Bank    : int  1 2 1 2 1 1 2 1 1 1 ...
 $ Dependent_Persons                          : int  1 2 1 1 1 2 1 1 1 1 ...
 $ Duration_In_Month                          : num  37 37 19 21 21 15 23 47 12 15 ...
[1] (18,24] (54,64] (44,54] <NA>    (34,44] (44,54]
Levels: (18,24] (24,34] (34,44] (44,54] (54,64] (64,74] (74,999]
[1] (18,24] (54,64] (44,54] <NA>    (34,44] (44,54]
Levels: (18,24] (24,34] (34,44] (44,54] (54,64] (64,74] (74,999]
'data.frame':	500 obs. of  7 variables:
 $ Requested_Amount                           : Factor w/ 5 levels "(0,5e+03]","(5e+03,1e+04]",..: 1 1 1 1 2 1 2 2 1 1 ...
 $ Installment_Percentage_Of_Disposable_Income: Factor w/ 11 levels "(0,1]","(1,2]",..: 5 3 2 5 5 10 4 3 4 8 ...
 $ Present_Residence_Since                    : Factor w/ 11 levels "(0,1]","(1,2]",..: 1 9 1 2 3 10 5 2 9 10 ...
 $ Age                                        : Factor w/ 7 levels "(18,24]","(24,34]",..: 1 5 4 NA 3 4 4 2 2 3 ...
 $ Number_Of_Existing_Credits_At_This_Bank    : Factor w/ 6 levels "(0,1]","(1,2]",..: 1 2 1 2 1 1 2 1 1 1 ...
 $ Dependent_Persons                          : Factor w/ 5 levels "(0,2]","(2,3]",..: 1 1 1 1 1 1 1 1 1 1 ...
 $ Duration_In_Month                          : Factor w/ 4 levels "(0,20]","(20,40]",..: 2 2 1 2 2 1 2 3 1 1 ...
```

Row 4's `Age` came out `NA`: `cut()`'s default bands are `(18,24]` — open on the left, closed on the right — so a value
of exactly 18 falls outside every band and is silently dropped to `NA`. This is a real trap in hand-rolled binning: a
boundary value chosen to be inclusive (here, the legal minimum lending age)
needs an explicit `include.lowest = TRUE` or a lower first break, or it quietly becomes missing data.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.BinningTools`
/ tool `"DiscretiseCustomEdges"` — takes the same kind of fixed, chosen cut points (`edges`) and returns every row's bin
index plus a bin table (count, share, and — if a 0/1 label is supplied — event rate, weight of evidence and information
value per bin), with an explicit, guarded missing-value marker (`-1`) instead of a silent `NA`. The same tool group also
has `DiscretiseEqualWidth`, `DiscretiseEqualFrequency`,
`DiscretiseKMeans` and `DiscretiseSupervised` for when the cut points should be chosen automatically rather than picked
by hand — `DiscretiseSupervised` in particular replaces the original guide's suggestion of eyeballing a histogram (its
Procedure 55) with cut points chosen by where a variable's event rate actually changes. None of this matters for the
trained classifier in Procedures 2 onward, which works on the continuous predictors directly — see that procedure's
equivalent.

### Procedure 2: Training a Naive Bayesian Classifier

Training is close to a one-liner: pass the data frame, the factor to treat as the dependent variable, and a Laplace
estimator (zero for now). `naiveBayes()` comes from the `e1071` package.

```R
CreditRisk <- readRDS("CreditRiskBinned.rds")   # continuing the session from Procedure 1

risk_score <- as.numeric(CreditRisk$Age) - as.numeric(CreditRisk$Duration_In_Month) +
  rnorm(nrow(CreditRisk))
cut_point <- stats::median(risk_score, na.rm = TRUE)
CreditRisk$Dependent <- factor(ifelse(is.na(risk_score), "Bad",
  ifelse(risk_score > cut_point, "Good", "Bad")))

library(e1071)

BayesianModel <- naiveBayes(CreditRisk, CreditRisk$Dependent, laplace = 0)

BayesianModel
```

Console output:

```
Naive Bayes Classifier for Discrete Predictors

Call:
naiveBayes.default(x = CreditRisk, y = CreditRisk$Dependent, 
    laplace = 0)

A-priori probabilities:
CreditRisk$Dependent
  Bad  Good 
0.522 0.478 

Conditional probabilities:
                    Age
CreditRisk$Dependent     (18,24]     (24,34]     (34,44]     (44,54]
                Bad  0.125523013 0.372384937 0.338912134 0.158995816
                Good 0.004184100 0.100418410 0.334728033 0.338912134
                    Age
CreditRisk$Dependent     (54,64]     (64,74]    (74,999]
                Bad  0.000000000 0.004184100 0.000000000
                Good 0.133891213 0.079497908 0.008368201

                    Duration_In_Month
CreditRisk$Dependent     (0,20]    (20,40]    (40,60]   (60,999]
                Bad  0.39463602 0.45593870 0.11877395 0.03065134
                Good 0.74476987 0.23849372 0.01255230 0.00418410

                    Dependent
CreditRisk$Dependent Bad Good
                Bad    1    0
                Good   0    1
```

(condensed: every other predictor's conditional table printed the same way, elided here for length)

The trailing `Dependent` table is worth pausing on: `naiveBayes(CreditRisk, CreditRisk$Dependent, ...)`
passes the *whole* data frame — including the `Dependent` column itself — as `x`. The fitted model has therefore learned
`Dependent` as one of its own predictors, with a perfect one-to-one conditional table. That is a real bug in this
procedure, not a feature of naive Bayes, and it is the reason Procedures 3 and 5 below look startlingly accurate: the
model has partly memorised the label through this leak, not just learned genuine structure in the other six predictors.

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsNaiveBayesClassifierTrainingService`
/ tool `"SubmitNaiveBayesClassifierTraining"` — queues a background training run that fits a Gaussian (Normal)
distribution to each predictor within each class directly on the **continuous** values, so Procedure 1's binning is not
needed at all for this path: only `AdvancedAnalyticsCache`'s packed
`double[]` rows and a separate `int[]` of 0/1 labels are required. Labels are always a distinct argument from the
predictor matrix, so the leak in the R script above cannot happen by construction — there is no data frame column
position a label could be smuggled in through. The trained pipeline
(`Jube.Engine.AdvancedAnalytics.Classification.NaiveBayesClassifierRecall`) also recalibrates the raw log-posterior-odds
into a probability with a Platt fit on a held-out validation split, addressing the guide's own later observation
(Procedure 3) that naive Bayes's raw posterior is usually overconfident. If the categorical, quantile-binned approach
this R procedure actually takes is wanted for a quick side-by-side comparison rather than a deployable model,
`Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelEvaluationTools`
/ tool `"EvaluateNaiveBayes"` with `kind: Categorical` does the same thing the R script does — quantile-bin each
predictor, count with Laplace smoothing (its `regularisation` parameter is exactly the Laplace add-one count) — but only
as one-off, in-memory evaluation output (ROC AUC, log loss, a confusion table); it is not a way to produce a versioned,
promotable model.

### Procedure 3: Recalling a Naive Bayesian Classifier for Probability

One of the benefits of a Bayesian classifier is that it returns a probability, not just a classification. The familiar
`predict()` function, called with `type = "raw"`, returns that probability for every class rather than the single most
likely one.

```R
library(e1071)

PPredictions <- predict(BayesianModel, CreditRisk, type = "raw")

head(PPredictions)
```

Console output:

```
              Bad         Good
[1,] 9.999941e-01 5.904913e-06
[2,] 2.217521e-05 9.999778e-01
[3,] 9.981097e-01 1.890258e-03
[4,] 9.997933e-01 2.067143e-04
[5,] 5.837010e-04 9.994163e-01
[6,] 7.318265e-04 9.992682e-01
```

Each row sums to one and shows one class dominating heavily — more heavily than a genuinely well-fitted model normally
would, for the reason noted at the end of Procedure 2. Anecdotally (and structurally: the independence assumption is
only ever approximately true, and small per-predictor errors compound multiplicatively), a naive Bayesian model's raw
calibration is often disappointing even when its ranking is accurate, which is why the next equivalent recalibrates
before returning anything.

**AdvancedAnalytics equivalent:** `Jube.Engine.AdvancedAnalytics.Classification.NaiveBayesClassifierRecall.Predict` /
`.PredictProbabilities` — there is no separate, per-model-family "recall" agent tool for naive Bayes the way
`LogisticRegressionTools` has `"RecallLogisticRegression"`; once a trained version is promoted, it is scored through the
same shared recall path every other trained `AdvancedAnalytics` model uses. Because the classifier is strictly binary
(both training tools require labels to be exactly 0 or 1), recall returns one number — the calibrated probability of the
positive class — not the two-column, one-per-factor-level matrix
`predict(type = "raw")` returns here for an arbitrary factor. That single number is already the Platt-fitted probability
described in Procedure 2's equivalent, not the raw posterior — the product does not expose an uncalibrated variant at
recall time.

### Procedure 4: Recalling a Naive Bayesian Classifier for Classification

Rather than read every class's probability and take the larger, `type = "class"` returns the single predicted label
directly. `dplyr::mutate()` then attaches it back onto the original data frame as a new column.

```R
library(e1071)
library(dplyr)

ClassPredictions <- predict(BayesianModel, CreditRisk, type = "class")

head(ClassPredictions)

CreditRisk <- mutate(CreditRisk, ClassPredictions)

head(CreditRisk)
```

Console output:

```
[1] Bad  Good Bad  Bad  Good Good
Levels: Bad Good
  Requested_Amount Installment_Percentage_Of_Disposable_Income
1        (0,5e+03]                                       (4,5]
2        (0,5e+03]                                       (2,3]
3        (0,5e+03]                                       (1,2]
4        (0,5e+03]                                       (4,5]
5    (5e+03,1e+04]                                       (4,5]
6        (0,5e+03]                                      (9,10]
  Present_Residence_Since     Age Number_Of_Existing_Credits_At_This_Bank
1                   (0,1] (18,24]                                   (0,1]
2                   (8,9] (54,64]                                   (1,2]
3                   (0,1] (44,54]                                   (0,1]
4                   (1,2]    <NA>                                   (1,2]
5                   (2,3] (34,44]                                   (0,1]
6                  (9,10] (44,54]                                   (0,1]
  Dependent_Persons Duration_In_Month Dependent ClassPredictions
1             (0,2]           (20,40]       Bad              Bad
2             (0,2]           (20,40]      Good             Good
3             (0,2]            (0,20]       Bad              Bad
4             (0,2]           (20,40]       Bad              Bad
5             (0,2]           (20,40]      Good             Good
6             (0,2]            (0,20]      Good             Good
```

**Tool equivalent:** none, deliberately. Both classifier training tools' own descriptions say this
outright: "recall from the trained model always returns the probability itself, not a class decision." The
`decisionThreshold` argument both tools accept only affects the confusion counts recorded in that training run's own
held-out evidence (true/false positives and negatives, precision, recall); it has no effect at recall time and there is
no separate call that returns a hard label. Turning Procedure 3's probability into a classification is left to the
caller applying its own threshold — usually the same
`decisionThreshold` value the run was evaluated with, but that is a convention, not something enforced by the recall
path.

### Procedure 5: Create a Naive Bayesian Network with a Laplace Estimator

Changing `laplace` from `0` to `1` means that a class/predictor-level combination with no observations in training is
treated as having seen at least one, rather than being assigned a probability of exactly zero (which would otherwise
make that predictor veto every future case with that combination outright, no matter how strong the other evidence). The
model is then recalled for its classification and checked against the true labels with a confusion matrix — on the same
data it was trained on, which the original guide flags itself as an optimistic best case pending real cross-validation.

```R
library(e1071)
library(gmodels)

SafeBayesianModel <- naiveBayes(CreditRisk, CreditRisk$Dependent, laplace = 1)

ClassPredictions <- predict(SafeBayesianModel, CreditRisk, type = "class")

CrossTable(CreditRisk$Dependent, ClassPredictions)
```

Console output:

```
   Cell Contents
|-------------------------|
|                       N |
| Chi-square contribution |
|           N / Row Total |
|           N / Col Total |
|         N / Table Total |
|-------------------------|

 
Total Observations in Table:  500 

 
                     | ClassPredictions 
CreditRisk$Dependent |       Bad |      Good | Row Total | 
---------------------|-----------|-----------|-----------|
                 Bad |       261 |         0 |       261 | 
                     |   114.242 |   124.758 |           | 
                     |     1.000 |     0.000 |     0.522 | 
                     |     1.000 |     0.000 |           | 
                     |     0.522 |     0.000 |           | 
---------------------|-----------|-----------|-----------|
                Good |         0 |       239 |       239 | 
                     |   124.758 |   136.242 |           | 
                     |     0.000 |     1.000 |     0.478 | 
                     |     0.000 |     1.000 |           | 
                     |     0.000 |     0.478 |           | 
---------------------|-----------|-----------|-----------|
        Column Total |       261 |       239 |       500 | 
                     |     0.522 |     0.478 |           | 
---------------------|-----------|-----------|-----------|
```

Perfect separation here is not a real result: it is Procedure 2's target-as-predictor leak taken to its logical end, on
training data. Treat it as a worked illustration of why the guide itself calls for cross-validation (introduced with
Gradient Boosting, its Procedure 113), not as evidence this model generalises.

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService`
/ tool `"SubmitTreeAugmentedNaiveBayesClassifierTraining"` — its `smoothing` parameter (default `1`) is the direct,
literal descendant of this procedure's Laplace estimator: per the vendored learner
(`Accord.MachineLearning.Bayes.TreeAugmentedNaiveBayesLearning`), each class prior is computed as
`(observedCount + Smoothing) / (total + classes * Smoothing)` — an additive count blended into the class priors before
either is normalised, exactly the add-one role `laplace = 1` plays above, just applied to continuous class priors rather
than discrete per-symbol frequency counts. Tree-Augmented Naive Bayes (TAN)
is a strict generalisation of the plain naive Bayes classifier from Procedures 2–4: every predictor may additionally
depend on one other predictor, with that single-parent structure discovered automatically by a Chow-Liu
maximum-spanning-tree search over the predictors' class-conditional mutual information, rather than assumed independent.
It was purpose-built for `AdvancedAnalytics` specifically because neither the vendored nor the pinned-upstream
Accord.NET had any Bayesian-network code at all — see the "Owner additions" and "Bayesian classifier research" sections
of `docs/AnalyticalMethodology/Reference/ModelCatalogue.md`, which also records why a fully general Bayesian network with unrestricted
structure search was deliberately dropped in favour of TAN (plus the researched-but-not-yet-built KDB, AODE and
hidden-naive-Bayes family). This procedure's Laplace-smoothed naive Bayesian network is, in that sense, TAN's direct
conceptual ancestor. [Bayesian Analysis with bnlearn](BayesianAnalysisWithBnlearn.md) goes much further into that
story — general Bayesian network structure, states and conditional probability tables with `bnlearn`, and how TAN's
structure learning compares.
