---
layout: default
title: Decision Trees and Boosting
nav_order: 15
parent: Analytical Methodology
---

# Splits, Probability and Decision Trees

The original module introduced splitting data into homogenous groups as the foundation of decision trees, using two
datasets: an abstracted FedEx equity file (`PC_FDX_Close_200x1D_Close_50x1D_10.csv`)
for regression trees, and a German credit-risk file (`CreditRisk.csv`) for classification trees. Neither CSV is part of
this repository — they were practice data shipped with the original training bundle. Every R block below runs against a
locally reconstructed stand-in with the same shape and column intent (a
`FDX` data frame of abstracted numeric predictors and a `Dependent` percentage change; a `CreditRisk`
data frame of 1,000 loan applications with the same categorical columns the original discussed —
`Status_Of_Existing_Checking_Account`, `Credit_History`, `Purpose`, `Housing`, `Savings_Account`, and so on — and a
`Dependent` factor of `Bad`/`Good`), generated with a fixed seed so every result below is reproducible but will not
match the original guide's screenshotted figures exactly. Category labels were also stripped of characters (`<`, `>=`,
`/`) that `C5.0`'s internal file format cannot parse — noted at the point it first matters, in Procedure 4.

### Procedure 1: Create a Decision Tree using rpart

A regression tree splits rows into progressively smaller, more homogenous groups by repeatedly choosing the single
variable and threshold that most reduces the variance of the outcome within each resulting group. Unlike linear
regression, it does not need variable selection up front — it can be pointed at every column and left to find the useful
splits itself. Training uses `rpart()`, naming the dependent variable and `.` for "every other column as a predictor":

```R
library(rpart)

FDX <- read.csv("/work/FDX.csv")

RegressionTree <- rpart(Dependent ~ ., data = FDX)

RegressionTree

summary(RegressionTree)
```

Console output:

```
n= 250 

node), split, n, deviance, yval
      * denotes terminal node

 1) root 250 0.665580300 -0.001981208  
   2) Range_1< 1.111349 136 0.241988700 -0.028919400  
     4) Min_2< 0.118419 93 0.130362000 -0.043637890  
       8) Skew_7>=0.3837525 34 0.038219030 -0.063830350  
        16) PointStep_12_PearsonCorrelation< 0.3894535 21 0.014722860 -0.080899000 *
        17) PointStep_12_PearsonCorrelation>=0.3894535 13 0.007494946 -0.036257920 *
       9) Skew_7< 0.3837525 59 0.070291150 -0.032001560  
        18) PointStep_12_PearsonCorrelation< 0.57451 43 0.038458350 -0.042272560 *
        19) PointStep_12_PearsonCorrelation>=0.57451 16 0.015105500 -0.004398250 *
     5) Min_2>=0.118419 43 0.047906100  0.002913605  
      10) PointStep_12_PearsonCorrelation< 0.014105 23 0.026018840 -0.011924520  
        20) Skew_7>=-0.2085405 12 0.006024114 -0.030126330 *
        21) Skew_7< -0.2085405 11 0.011681960  0.007932000 *
      11) PointStep_12_PearsonCorrelation>=0.014105 20 0.010999860  0.019977450 *
   3) Range_1>=1.111349 114 0.207164700  0.030155590  
     6) Min_2< 0.115759 68 0.090387150  0.011005410  
      12) PointStep_12_PearsonCorrelation< -0.196764 26 0.038539870 -0.012577810 *
      13) PointStep_12_PearsonCorrelation>=-0.196764 42 0.028435250  0.025604550 *
     7) Min_2>=0.115759 46 0.054975720  0.058464540  
      14) PointStep_12_PearsonCorrelation< -0.5141565 15 0.011878270  0.025645600 *
      15) PointStep_12_PearsonCorrelation>=-0.5141565 31 0.019123660  0.074344680 *
Call:
rpart(formula = Dependent ~ ., data = FDX)
  n= 250 

           CP nsplit rel error    xerror       xstd
1  0.32517014      0 1.0000000 1.0057223 0.07955593
2  0.09573690      1 0.6748299 0.7186143 0.06877142
3  0.09285409      2 0.5790930 0.6804645 0.06343808
4  0.03601937      3 0.4862389 0.5315850 0.05169004
5  0.03517537      4 0.4502195 0.5579602 0.05334143
6  0.03283129      5 0.4150441 0.5484864 0.05327956
7  0.02513190      6 0.3822128 0.4962276 0.04808767
8  0.02404100      7 0.3570809 0.4802352 0.04779269
9  0.01635777      8 0.3330399 0.4748094 0.04520039
10 0.01248950      9 0.3166822 0.4991403 0.04755979
11 0.01000000     10 0.3041927 0.4946577 0.04948548

Variable importance
                        Range_1                           Min_2 
                             35                              22 
PointStep_12_PearsonCorrelation                          Skew_7 
                             15                               7 
           PearsonCorrelation_6                       Volume_10 
                              5                               5 
                       StdDev_4                           Max_3 
                              4                               2 
                     Kurtosis_8                       ZScore_11 
                              2                               1 
                       Median_5                      Momentum_9 
                              1                               1 
(summary(RegressionTree) continues with one paragraph of complexity-parameter, primary-split and
surrogate-split detail per node — 21 nodes in this tree; omitted here for length, not fabricated)
```

The printed tree reads exactly as the original guide describes: an if/then/else cascade down to each `*`
terminal node, for example node 16 — `Range_1 >= 1.111349` is false, `Min_2 < 0.118419`, `Skew_7 >=
0.384`, `PointStep_12_PearsonCorrelation < 0.389` then predict −0.0809. `summary()` gives each node's mean, deviance and
the competing/surrogate splits considered at that point, which is where the original guide's per-node error-rate lookup
comes from.

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsDecisionTreeRegressorTrainingService`, reached through
tool `"SubmitDecisionTreeRegressorTraining"`
(`Jube.Service.Agent.AdvancedAnalyticsTools.DecisionTreeRegressorTrainingRunTools`). It queues a background training run
of the same kind of regression tree (a nested sequence of threshold splits fit to minimise variance) and stores the
fitted model as an immutable version — nothing is printed to a console, the fit is inspected afterwards through the
run's stored evidence and the model diagram covered in Procedure 2. There is no need to hand-pick or drop predictor
columns first, matching the R behaviour described above.

### Procedure 2: Visualise an rpart Decision Tree

`rpart.plot()` turns the same tree into a diagram, which for a tree of any size is much easier to read than the nested
`if`/`else` text — at the cost of usually needing to export the image and zoom in.

```R
library(rpart)
library(rpart.plot)

FDX <- read.csv("/work/FDX.csv")
RegressionTree <- rpart(Dependent ~ ., data = FDX)

pdf("/work/RegressionTree.pdf")
rpart.plot(RegressionTree)
dev.off()

cat("rpart.plot rendered", nrow(RegressionTree$frame), "nodes to /work/RegressionTree.pdf\n")
```

Console output:

```
null device 
          1 
rpart.plot rendered 21 nodes to /work/RegressionTree.pdf
```

**AdvancedAnalytics equivalent:** `Jube.Charting.Diagrams.Diagram.Tree()` → `TreeDiagramBuilder`
(`Jube.Charting/Diagrams/TreeDiagramBuilder.cs`), which renders a `TreeDiagramSpec` (a `TreeNode` tree of split
feature/threshold, sample counts, class counts, value, impurity and branch labels) as an SVG using a Reingold-Tilford
layout — the same "balanced, centred-over-children" tree shape `rpart.plot` produces, but laid out deterministically and
covered by golden-snapshot tests rather than rendered once into an image you export and zoom into. `Diagram.Forest()` →
`ForestOfTreesBuilder` does the equivalent for an ensemble (the first N trees of a random forest or boosted model,
alongside their variable importances) — see the
`AdvancedAnalytics equivalent` note on Procedure 9. Both live in namespace `Jube.Charting.Diagrams`, part of the
product's own backend SVG charting library (see [Charting](Charting.md)).

### Procedure 3: Recalling an rpart Decision Tree

As with every predictive model in the original guide, `predict()` takes the fitted object and a data frame of rows and
returns predictions in the same row order, which can then be merged back onto the source data frame with
`dplyr::mutate()`.

```R
library(rpart)
library(dplyr)

FDX <- read.csv("/work/FDX.csv")
RegressionTree <- rpart(Dependent ~ ., data = FDX)

RegressionTreePredictions <- predict(RegressionTree, FDX)

head(RegressionTreePredictions)

FDX <- mutate(FDX, RegressionTreePredictions)

head(FDX[, c("Dependent", "RegressionTreePredictions")])
```

Console output:

```

Attaching package: ‘dplyr’

The following objects are masked from ‘package:stats’:

    filter, lag

The following objects are masked from ‘package:base’:

    intersect, setdiff, setequal, union

          1           2           3           4           5           6 
 0.02560455 -0.04227256  0.02560455 -0.04227256  0.02564560  0.01997745 
  Dependent RegressionTreePredictions
1  0.030973                0.02560455
2 -0.029834               -0.04227256
3  0.056575                0.02560455
4 -0.032522               -0.04227256
5  0.025251                0.02564560
6  0.020712                0.01997745
```

Note the repeated predicted values (rows 1 and 3 both land on 0.02560455) — every row that falls in the same terminal
node is given that node's mean, which is the defining behaviour of a tree's predictions.

**AdvancedAnalytics equivalent:** `Jube.Engine.AdvancedAnalytics.Regression.DecisionTreeRegressorRecall`. Recall against
a trained decision tree regressor is part of the same engine-synced live-model path every other `AdvancedAnalytics`
model uses — there is no separate "recall" step to call by hand; a promoted model's predictions are produced the same
way for a rule, an abstraction calculation, or an agent tool querying it, all reading the one stored model version.

### Procedure 4: Creating a C5 Decision Tree

`CreditRisk` is far more categorical than the numeric `FDX` predictors used so far. `C5.0()` takes its independent and
dependent variables as two separate arguments rather than a formula — here, negative subsetting drops the first
(`Dependent`) column for the predictor frame and the dependent variable is named explicitly as a vector:

```R
library(C50)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)

C50Tree <- C5.0(CreditRisk[-1], CreditRisk$Dependent)

C50Tree

summary(C50Tree)
```

Console output:

```
Call:
C5.0.default(x = CreditRisk[-1], y = CreditRisk$Dependent)

Classification Tree
Number of samples: 1000 
Number of predictors: 10 

Tree size: 10 

Non-standard options: attempt to group attributes


Call:
C5.0.default(x = CreditRisk[-1], y = CreditRisk$Dependent)


C5.0 [Release 2.07 GPL Edition]  	Sun Sep 27 05:42:56 2026
-------------------------------

Class specified by attribute `outcome'

Read 1000 cases (11 attributes) from undefined.data

Decision tree:

Credit_Amount <= 3403: Good (520/85)
Credit_Amount > 3403:
:...Duration_In_Month <= 28: Good (361/83)
    Duration_In_Month > 28:
    :...Savings_Account in {100_To_500_DM,Above_1000_DM}: Bad (11/3)
        Savings_Account in {500_To_1000_DM,Unknown}: Good (35/12)
        Savings_Account = Below_100_DM:
        :...Housing = Free: Bad (6/3)
            Housing = Rent: Good (10/3)
            Housing = Owner:
            :...Purpose in {Business,Education,New_Car,Other,
                :           Used_Car}: Bad (28/8)
                Purpose = Furniture: Good (11/5)
                Purpose = Radio_Or_TV:
                :...Age <= 31: Bad (6)
                    Age > 31: Good (12/2)


Evaluation on training data (1000 cases):

	    Decision Tree   
	  ----------------  
	  Size      Errors  

	    10  204(20.4%)   <<


	   (a)   (b)    <-classified as
	  ----  ----
	    37   190    (a): class Bad
	    14   759    (b): class Good


	Attribute usage:

	100.00%	Credit_Amount
	 48.00%	Duration_In_Month
	 11.90%	Savings_Account
	  7.30%	Housing
	  5.70%	Purpose
	  1.80%	Age


Time: 0.0 secs
```

Reading the first branch: `Credit_Amount > 3403 AND Duration_In_Month > 28 AND Savings_Account in
{100_To_500_DM, Above_1000_DM} → Bad`, matching 11 of the 1,000 training cases, 3 of them wrongly. One mechanical note:
`C5.0`'s internal attribute file cannot parse category labels containing `<`, `>=`, `/`
or similar (the original guide's labels like `"< 0 DM"` and `"0 <= ... < 200 DM"` all fail with `` `:'
or `:=' expected after attribute name ``); the reconstructed `CreditRisk` uses safe labels (`Below_0_DM`, `0_To_200_DM`,
and so on) throughout for this reason.

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsDecisionTreeClassifierTrainingService`, reached through
tool `"SubmitDecisionTreeClassifierTraining"`
(`Jube.Service.Agent.AdvancedAnalyticsTools.DecisionTreeClassifierTrainingRunTools`). Categorical predictors are not
passed as R factors — every training and evaluation tool in
`AdvancedAnalytics` takes rows as `double[][]` (see the `EvaluateDecisionTree` signature in Procedure 6, and
`docs/AnalyticalMethodology/Reference/FoundationData.md`'s packed `double[]` cache), so a categorical column like
`Housing` must already be numeric-coded by whatever builds the row before it reaches the tool; the tree itself still
ends up splitting on thresholds over those codes, the same underlying decision C5.0 expresses as a split over a
categorical set.

### Procedure 5: Recalling a C5 Decision Tree

```R
library(C50)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)
C50Tree <- C5.0(CreditRisk[-1], CreditRisk$Dependent)

CreditRiskPrediction <- predict(C50Tree, CreditRisk)

head(CreditRiskPrediction)
```

Console output:

```
[1] Good Good Good Good Good Good
Levels: Bad Good
```

**AdvancedAnalytics equivalent:** `Jube.Engine.AdvancedAnalytics.Classification.DecisionTreeClassifierRecall`
— the same engine-synced live-model recall path as every other classifier, not a separate predict call.

### Procedure 6: Creating a Confusion Matrix for a C5 Decision Tree

```R
library(C50)
library(gmodels)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)
C50Tree <- C5.0(CreditRisk[-1], CreditRisk$Dependent)
CreditRiskPrediction <- predict(C50Tree, CreditRisk)

CrossTable(CreditRisk$Dependent, CreditRiskPrediction)
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

 
Total Observations in Table:  1000 

 
                     | CreditRiskPrediction 
CreditRisk$Dependent |       Bad |      Good | Row Total | 
---------------------|-----------|-----------|-----------|
                 Bad |        34 |       193 |       227 | 
                     |    55.382 |     2.610 |           | 
                     |     0.150 |     0.850 |     0.227 | 
                     |     0.756 |     0.202 |           | 
                     |     0.034 |     0.193 |           | 
---------------------|-----------|-----------|-----------|
                Good |        11 |       762 |       773 | 
                     |    16.264 |     0.766 |           | 
                     |     0.014 |     0.986 |     0.773 | 
                     |     0.244 |     0.798 |           | 
                     |     0.011 |     0.762 |           | 
---------------------|-----------|-----------|-----------|
        Column Total |        45 |       955 |      1000 | 
                     |     0.045 |     0.955 |           | 
---------------------|-----------|-----------|-----------|
```

Of the 45 cases the model classified `Bad`, 34 (75.6% — column-wise) really were bad; the model missed 193 of the 227
truly-bad accounts (row-wise recall of only 15.0% here, weaker than the original guide's dataset, since this
reconstruction's signal is deliberately noisier — see Procedure 9).

**Tool equivalent:** tool `"EvaluateDecisionTree"`
(`Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelEvaluationTools`) returns a confusion table (true/false
positives and negatives) as one field of its `ClassifierEvaluationResult`, alongside ROC AUC, average precision, log
loss, Brier score and calibration — computed on held-out rows the fit never trained on, not the training rows
`CrossTable` is reading here. There is no standalone confusion-matrix tool: it is always one field of a full evaluation
result.

### Procedure 7: Visualising a C5 Decision Tree

```R
library(C50)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)
C50Tree <- C5.0(CreditRisk[-1], CreditRisk$Dependent)

pdf("/work/C50Tree.pdf", width = 14, height = 9)
plot(C50Tree)
dev.off()

cat("plot(C50Tree) rendered to /work/C50Tree.pdf\n")
```

Console output:

```
null device 
          1 
plot(C50Tree) rendered to /work/C50Tree.pdf
```

As the original guide notes, for any tree of real size the rule form (Procedure 8) communicates the model more usefully
than the diagram.

**AdvancedAnalytics equivalent:** `Jube.Charting.Diagrams.Diagram.Tree()` — the same `TreeDiagramBuilder`
named in Procedure 2; it is family-agnostic (rpart, C5.0 or any tree the training service fits render through the same
spec).

### Procedure 8: Expressing Business Rules from C5

Rebuilding with `rules = TRUE` makes `C5.0` express the same knowledge as a flat, ordered list of
`if`/`then` rules — much more directly implementable outside R, "not least SQL," than a traversed tree — each with a
coverage count and a confidence:

```R
library(C50)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)

C50Tree <- C5.0(CreditRisk[-1], CreditRisk$Dependent, rules = TRUE)

summary(C50Tree)
```

Console output:

```
Call:
C5.0.default(x = CreditRisk[-1], y = CreditRisk$Dependent, rules = TRUE)


C5.0 [Release 2.07 GPL Edition]  	Sun Sep 27 05:43:02 2026
-------------------------------

Class specified by attribute `outcome'

Read 1000 cases (11 attributes) from undefined.data

Rules:

Rule 1: (11/3, lift 3.0)
	Duration_In_Month > 28
	Credit_Amount > 3403
	Savings_Account in {100_To_500_DM, Above_1000_DM}
	->  class Bad  [0.692]

Rule 2: (73/34, lift 2.3)
	Duration_In_Month > 28
	Credit_Amount > 3403
	Savings_Account = Below_100_DM
	->  class Bad  [0.533]

Rule 3: (520/85, lift 1.1)
	Credit_Amount <= 3403
	->  class Good  [0.835]

Rule 4: (262/49, lift 1.0)
	Savings_Account in {500_To_1000_DM, Unknown}
	->  class Good  [0.811]

Rule 5: (741/145, lift 1.0)
	Duration_In_Month <= 28
	->  class Good  [0.803]

Default class: Good


Evaluation on training data (1000 cases):

	        Rules     
	  ----------------
	    No      Errors

	     5  217(21.7%)   <<


	   (a)   (b)    <-classified as
	  ----  ----
	    47   180    (a): class Bad
	    37   736    (b): class Good


	Attribute usage:

	 82.50%	Duration_In_Month
	 60.40%	Credit_Amount
	 34.60%	Savings_Account


Time: 0.0 secs
```

**Tool equivalent:** tool `"EvaluateDecisionTree"` again — its `ClassifierEvaluationResult`
carries a `Rules` field (`string[]`) and a `TreeText` field (`string?`) on its `Model` description
(`ModelDescription.Rules` / `ModelDescription.TreeText`,
`Jube.Service/Agent/AdvancedAnalyticsTools/ModelEvaluation/ModelEvaluationTypes.cs`), produced by the same
`CartTree.Rules(...)` machine (`Jube.Service/Agent/AdvancedAnalyticsTools/ModelEvaluation/CartTree.cs`)
that also emits the readable tree text — so the tool's description explicitly advertises "the readable tree text, the
if-then rules and variable importance" in one call. This lives on the discard-after-evaluate evaluation tool, not on the
persisted `AdvancedAnalyticsDecisionTreeClassifierTrainingService`: training a model that is kept and promoted does not
currently re-expose that model's rules text after the fact —
`EvaluateDecisionTree` is the one place the product turns a tree into rule text today. Separately, the vendored
`Accord.MachineLearning.DecisionTrees.Rules` namespace (`DecisionSet`, `DecisionRule`,
`Simplification`) implements PART-style rule extraction and simplification, but nothing outside
`Accord.MachineLearning` itself references it — it is unused library code, not a wired-up product feature.

### Procedure 9: Boosting and Recalling in C5

Boosting fits several trees in sequence and lets each vote a classification, the majority winning. Adding
`trials = 10` asks for ten:

```R
library(C50)
library(gmodels)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)

C50Tree <- C5.0(CreditRisk[-1], CreditRisk$Dependent, trials = 10)

summary(C50Tree)

CreditRiskPrediction <- predict(C50Tree, CreditRisk)

CrossTable(CreditRisk$Dependent, CreditRiskPrediction)
```

Console output (trials 1 through 8 omitted — each is the same shape as trial 0, one grown on the rows the previous
trials misclassified):

```
Call:
C5.0.default(x = CreditRisk[-1], y = CreditRisk$Dependent, trials = 10)


C5.0 [Release 2.07 GPL Edition]  	Sun Sep 27 05:37:55 2026
-------------------------------

Class specified by attribute `outcome'

Read 1000 cases (11 attributes) from undefined.data

-----  Trial 0:  -----

Decision tree:

Credit_Amount <= 3403: Good (520/85)
Credit_Amount > 3403:
:...Duration_In_Month <= 28: Good (361/83)
    Duration_In_Month > 28:
    :...Savings_Account in {100_To_500_DM,Above_1000_DM}: Bad (11/3)
        Savings_Account in {500_To_1000_DM,Unknown}: Good (35/12)
        Savings_Account = Below_100_DM:
        :...Housing = Free: Bad (6/3)
            Housing = Rent: Good (10/3)
            Housing = Owner:
            :...Purpose in {Business,Education,New_Car,Other,
                :           Used_Car}: Bad (28/8)
                Purpose = Furniture: Good (11/5)
                Purpose = Radio_Or_TV:
                :...Age <= 31: Bad (6)
                    Age > 31: Good (12/2)

[ ... trials 1-8 ... ]

-----  Trial 9:  -----

Decision tree:

Credit_Amount <= 2746: Good (196.6)
Credit_Amount > 2746:
:...Credit_History = Delay_In_Past: Bad (82.8/37)
    Credit_History in {All_Paid,Critical_Other_Credits,Existing_Paid_Duly,
    :                  No_Credit_Open_Or_All_Paid}:
    :...Duration_In_Month <= 27: Good (250.4/30.8)
        Duration_In_Month > 27:
        :...Age > 53: Good (23.4)
            Age <= 53:
            :...Savings_Account = Above_1000_DM: Bad (8.9)
                Savings_Account in {100_To_500_DM,500_To_1000_DM,Below_100_DM,
                :                   Unknown}:
                :...Credit_Amount <= 3461: Good (51.8/7.8)
                    Credit_Amount > 3461:
                    :...Credit_Amount <= 3665: Bad (21.7/3.6)
                        Credit_Amount > 3665: Good (207.3/78.3)


Evaluation on training data (1000 cases):

Trial	    Decision Tree   
-----	  ----------------  
	  Size      Errors  

   0	    10  204(20.4%)
   1	     4  250(25.0%)
   2	     7  317(31.7%)
   3	     3  264(26.4%)
   4	     8  284(28.4%)
   5	     3  322(32.2%)
   6	    10  297(29.7%)
   7	    10  233(23.3%)
   8	     5  230(23.0%)
   9	     8  221(22.1%)
boost	        196(19.6%)   <<


	   (a)   (b)    <-classified as
	  ----  ----
	    37   190    (a): class Bad
	     6   767    (b): class Good


	Attribute usage:

	100.00%	Status_Of_Existing_Checking_Account
	100.00%	Credit_History
	100.00%	Credit_Amount
	 83.00%	Savings_Account
	 82.30%	Duration_In_Month
	 81.40%	Housing
	 79.80%	Purpose
	 36.80%	Age
	 24.50%	Job


Time: 0.0 secs

 
   Cell Contents
|-------------------------|
|                       N |
| Chi-square contribution |
|           N / Row Total |
|           N / Col Total |
|         N / Table Total |
|-------------------------|

 
Total Observations in Table:  1000 

 
                     | CreditRiskPrediction 
CreditRisk$Dependent |       Bad |      Good | Row Total | 
---------------------|-----------|-----------|-----------|
                 Bad |        36 |       191 |       227 | 
                     |    76.557 |     3.273 |           | 
                     |     0.159 |     0.841 |     0.227 | 
                     |     0.878 |     0.199 |           | 
                     |     0.036 |     0.191 |           | 
---------------------|-----------|-----------|-----------|
                Good |         5 |       768 |       773 | 
                     |    22.482 |     0.961 |           | 
                     |     0.006 |     0.994 |     0.773 | 
                     |     0.122 |     0.801 |           | 
                     |     0.005 |     0.768 |           | 
---------------------|-----------|-----------|-----------|
        Column Total |        41 |       959 |      1000 | 
                     |     0.041 |     0.959 |           | 
---------------------|-----------|-----------|-----------|
```

Boosting takes the single tree's 20.4% training error to 19.6% here — a genuinely smaller improvement than the original
guide's dataset (which went from 11.9% to 0.3%), because this reconstruction's signal is deliberately noisy enough that
individual trials range from 22% to 32% error on their own; boosting still helps, just modestly, which is itself a
realistic outcome the fixed dataset in the original guide did not show.

**Tool equivalent:** tool `"EvaluateBoostedTrees"`
(`Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelEvaluationTools`) with `algorithm =
AdaBoost` is the direct match for C5's reweight-the-errors boosting; `algorithm = GradientBoosting` (the default)
instead fits each new tree to the gradient of the loss (see Procedure 10). For a model that is trained and kept rather
than evaluated and discarded, there is no boosted decision-tree classifier —
`AdvancedAnalyticsGradientBoostingClassifierTrainingService` (tool `"SubmitGradientBoostingClassifierTraining"`,
`Jube.Service.Agent.AdvancedAnalyticsTools.GradientBoostingClassifierTrainingRunTools`) is the persisted family that
plays this role, and `AdvancedAnalyticsRandomForestClassifierTrainingService` (tool
`"SubmitRandomForestClassifierTraining"`) is the sibling ensemble-of-trees family that instead grows many independent
trees on bootstrap samples rather than reweighting sequential errors.

### Procedure 10: Creating a Gradient Boosting Machine

A Gradient Boosting Machine is also built from boosted trees, but instead of reweighting misclassified rows it fits each
new tree to the residual (roughly, the current error) of the ensemble so far, tempered by a small learning rate
(`shrinkage`) so no one tree dominates. The package is `gbm`, not `GBM` as the original guide's install step named it —
R package names are case-sensitive on every platform this guide was validated on, and `library(GBM)` fails outright.

```R
library(gbm)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)

set.seed(4210)
GBM <- gbm(Dependent ~ ., data = CreditRisk,
      n.trees = 1000,
      shrinkage = 0.01,
      distribution = "gaussian",
      interaction.depth = 7,
      bag.fraction = 0.9,
      cv.folds = 10,
      n.minobsinnode = 50
)

GBM

pdf("/work/GBMPerformance.pdf")
best_iteration <- gbm.perf(GBM)
dev.off()

cat("Best cross-validation iteration:", best_iteration, "\n")

summary(GBM)
```

Console output:

```
Loaded gbm 2.3.1
This version of gbm is no longer under development. Consider transitioning to gbm3, https://github.com/gbm-developers/gbm3
gbm(formula = Dependent ~ ., distribution = "gaussian", data = CreditRisk, 
    n.trees = 1000, interaction.depth = 7, n.minobsinnode = 50, 
    shrinkage = 0.01, bag.fraction = 0.9, cv.folds = 10)
A gradient boosted model with gaussian loss function.
1000 iterations were performed.
The best cross-validation iteration was 138.
There were 10 predictors of which 10 had non-zero influence.
null device 
          1 
Best cross-validation iteration: 138 
                                                                    var
Credit_Amount                                             Credit_Amount
Duration_In_Month                                     Duration_In_Month
Purpose                                                         Purpose
Age                                                                 Age
Credit_History                                           Credit_History
Status_Of_Existing_Checking_Account Status_Of_Existing_Checking_Account
Savings_Account                                         Savings_Account
Employment_Since                                       Employment_Since
Job                                                                 Job
Housing                                                         Housing
                                      rel.inf
Credit_Amount                       23.311787
Duration_In_Month                   17.824521
Purpose                             13.294715
Age                                  8.861798
Credit_History                       8.788802
Status_Of_Existing_Checking_Account  8.598151
Savings_Account                      6.841690
Employment_Since                     5.999503
Job                                  3.574984
Housing                              2.904049
```

The original guide's `cv.fold` argument is also not the real parameter name — `gbm()` takes `cv.folds`
(plural); the singular form is silently dropped as an unmatched `...` argument rather than raising an error, which would
leave a reader believing they had asked for 10-fold cross-validation when they had not. This run used the corrected name
throughout. 1,000 rounds were fit and 10-fold cross-validation put the best number of rounds to actually use at 138 (of
1,000) — beyond that point the model is fitting cross-validation noise, not signal — and `Credit_Amount` and
`Duration_In_Month` are the two most influential predictors, together accounting for just over 41% of the relative
influence.

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsGradientBoostingClassifierTrainingService`
(tool `"SubmitGradientBoostingClassifierTraining"`) for a 0/1 outcome, or
`AdvancedAnalyticsGradientBoostingRegressorTrainingService` (tool `"SubmitGradientBoostingRegressorTraining"`)
for a continuous one — both under `Jube.Service/Agent/AdvancedAnalyticsTools/`. Choosing "how many rounds are enough"
the way `gbm.perf` does here is the job of tool `"ComputeLearningCurve"` or
`"ComputeValidationCurve"` (`Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.ModelSelectionTools`), run
against `treeCount` before committing to a persisted training run.

### Procedure 11: Recalling a Gradient Boosting Machine

`predict()` on a `gbm` object obeys the same signature as everything else, but the current CRAN release of
`gbm` behaves differently from the original guide's description: asking for predictions without stating
`n.trees` now defaults to the cross-validated best iteration (138 here) and prints `Using 138 trees...`
rather than silently using every tree fit. Since `CreditRisk$Dependent` is a factor, `gaussian` regression here has
modelled it as the numbers 1 (`Bad`, the first factor level) and 2 (`Good`) — a prediction closer to 2 means "more
likely Good":

```R
library(gbm)
library(gmodels)

CreditRisk <- read.csv("/work/CreditRisk.csv", stringsAsFactors = TRUE)

set.seed(4210)
GBM <- gbm(Dependent ~ ., data = CreditRisk,
      n.trees = 1000,
      shrinkage = 0.01,
      distribution = "gaussian",
      interaction.depth = 7,
      bag.fraction = 0.9,
      cv.folds = 10,
      n.minobsinnode = 50
)

GBMPredictions <- predict(GBM, CreditRisk, n.trees = 1000, type = "response")

head(GBMPredictions)

levels(CreditRisk$Dependent)

CreditRiskGBMClassifications <- ifelse(GBMPredictions >= 1.5, "Good", "Bad")

CrossTable(CreditRisk$Dependent, CreditRiskGBMClassifications)
```

Console output:

```
Loaded gbm 2.3.1
This version of gbm is no longer under development. Consider transitioning to gbm3, https://github.com/gbm-developers/gbm3
[1] 1.983578 1.898392 1.811997 1.664582 1.576858 1.704988
[1] "Bad"  "Good"

 
   Cell Contents
|-------------------------|
|                       N |
| Chi-square contribution |
|           N / Row Total |
|           N / Col Total |
|         N / Table Total |
|-------------------------|

 
Total Observations in Table:  1000 

 
                     | CreditRiskGBMClassifications 
CreditRisk$Dependent |       Bad |      Good | Row Total | 
---------------------|-----------|-----------|-----------|
                 Bad |        82 |       145 |       227 | 
                     |   196.222 |    18.698 |           | 
                     |     0.361 |     0.639 |     0.227 | 
                     |     0.943 |     0.159 |           | 
                     |     0.082 |     0.145 |           | 
---------------------|-----------|-----------|-----------|
                Good |         5 |       768 |       773 | 
                     |    57.623 |     5.491 |           | 
                     |     0.006 |     0.994 |     0.773 | 
                     |     0.057 |     0.841 |           | 
                     |     0.005 |     0.768 |           | 
---------------------|-----------|-----------|-----------|
        Column Total |        87 |       913 |      1000 | 
                     |     0.087 |     0.913 |           | 
---------------------|-----------|-----------|-----------|
```

Passing `n.trees = 1000` explicitly (all fitted rounds, rather than the cross-validated best of 138)
matters here: predicting with only the best 138 rounds produced predictions that never dropped below 1.54 for any of the
1,000 training rows, so every single one rounded to `Good` at the fixed 1.5 threshold — a real, worth-knowing failure
mode of applying a raw factor-encoding midpoint as a decision threshold without first checking where the model's
predictions actually fall, rather than a calibrated or data-driven cut. With all 1,000 rounds, of the 87 accounts
classified `Bad`, 82 (94.3%, column-wise) truly were, and 82 of the 227 actually-bad accounts (36.1%, row-wise) were
caught.

**Tool equivalent:** tool `"EvaluateBoostedTrees"` again — it returns a calibrated probability and
confusion table computed against held-out rows directly, so there is no separate midpoint threshold to reconstruct and
no risk of silently predicting with the wrong number of rounds: the tool always fits and predicts with the exact
`treeCount` given. For a persisted, promoted model, recall runs through
`Jube.Engine.AdvancedAnalytics.Classification.GradientBoostingClassifierRecall` (or
`Jube.Engine.AdvancedAnalytics.Regression.GradientBoostingRegressorRecall` for a continuous outcome) — the same
engine-synced live-model path named throughout this module.
