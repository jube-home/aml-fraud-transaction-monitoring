---
layout: default
title: Linear Regression
nav_order: 13
parent: Analytical Methodology
---

# Linear Regression

Linear regression predicts a numeric outcome from one or more numeric predictors when the residuals are reasonably close
to normal. The original course worked against an FDX (FedEx, NYSE) equity file that had already been through Jube's
abstraction engine: a `Dependent` column (a forward return) plus hundreds of abstracted predictor columns named for the
statistic and window they came from (`Median_1`, `Mode_1`, `Skew_3`,
`Range_2_PearsonCorrelation`, `PointStep_5_PearsonCorrelation`, and z-scored or self-correlated variants of the same).
That exact file is not available in this environment, so every snippet below runs against a synthetic stand-in built
with the same column-naming shape and the same kind of relationships (a weak overall signal, a couple of genuinely
predictive columns, and two deliberately collinear groups so the multicollinearity procedure has something real to
find). Coefficients therefore differ from the original screenshots; the mechanics and the shape of the result do not.
`dplyr` and `ggplot2` are both in the shared package set, nothing extra was installed.

### Procedure 1: Scanning Scatter Plots for Relationships

`pairs()` draws every pairwise scatter plot among a data frame's columns in one grid, which is the fastest way to
eyeball which predictors move with the dependent variable before modelling. Run over a whole abstracted file (hundreds
of columns) the grid is unusably large, so the practical habit is to subset to `Dependent` plus one family of related
columns at a time, repeating across families until a feel for the relationships has formed. The original text's
subsetting line had a bracket typo (`pairs[c(...)]` instead of `pairs(FDX[, c(...)])`); corrected below.

```R
FDX <- read.csv("/work/FDX.csv")

pdf("/work/p01_full.pdf")
pairs(FDX)
invisible(dev.off())
cat("Full pairs() matrix rendered over", ncol(FDX), "columns.\n")

pdf("/work/p01_subset.pdf")
pairs(FDX[, c("Dependent", "Median_1", "Median_1_PearsonCorrelation", "Median_1_ZScore",
              "Mode_1", "Mode_1_PearsonCorrelation", "Mode_1_ZScore")])
invisible(dev.off())
cat("Subset pairs() matrix rendered over 7 columns.\n")
```

Console output:

```
Full pairs() matrix rendered over 11 columns.
Subset pairs() matrix rendered over 7 columns.
```

**AdvancedAnalytics equivalent:** `Jube.Charting`, `Chart.ScatterMatrix()` — a scatterplot matrix with histograms on the
diagonal, scatters below and correlation cells above, one row linked across every cell on hover. This replaces `pairs()`
as the exploratory grid; never ggplot2 or base R graphics.

### Procedure 2: Creating a Scatter Plot for Closer Inspection with ggplot2

`plot()` gives an instant scatter of two columns; `qplot()` from ggplot2 draws the same relationship with more visual
polish for anything that will be shown to someone else.

```R
FDX <- read.csv("/work/FDX.csv")
library(ggplot2)

pdf("/work/p02_plot.pdf")
plot(FDX$Median_4, FDX$Dependent)
invisible(dev.off())
cat("Base plot() scatter rendered.\n")

pdf("/work/p02_qplot.pdf")
print(qplot(FDX$Median_4, FDX$Dependent))
invisible(dev.off())
cat("ggplot2 qplot() scatter rendered.\n")
```

Console output:

```
Base plot() scatter rendered.
Warning message:
`qplot()` was deprecated in ggplot2 3.4.0. 
ggplot2 qplot() scatter rendered.
```

(`qplot()` is now a deprecated corner of ggplot2 itself — one more reason it is not a path this product follows.)

**Tool equivalent:** `Jube.Charting`, `Chart.Scatter()` — scatter and bubble charts with trend lines and R
squared, an identity diagonal and log axes built in. Outside the platform this is what you would reach for instead; the
product never renders through ggplot2 or a raw R graphics device.

### Procedure 3: Create a Correlation Matrix using Spearman and Pearson

`cor()` produces a full correlation matrix over a data frame in one call. `method` chooses `"pearson"` (linear)
or `"spearman"` (monotone rank); `use = "complete"` avoids the whole matrix collapsing to `NA` the moment any row has a
missing value. The matrix can then be subset to just the `Dependent` column, using `drop = FALSE` (not the string
`"false"` in the original text) to keep the result a matrix rather than a bare vector.

```R
FDX <- read.csv("/work/FDX.csv")

Pearson <- cor(FDX, use = "complete", method = "pearson")
cat("Dimensions of Pearson:", paste(dim(Pearson), collapse = " x "), "\n\n")

PearsonDependent <- Pearson[, "Dependent", drop = FALSE]
cat("PearsonDependent (correlation of every column with Dependent):\n")
print(round(PearsonDependent, 4))
```

Console output:

```
Dimensions of Pearson: 11 x 11 

PearsonDependent (correlation of every column with Dependent):
                               Dependent
Dependent                         1.0000
Median_1                         -0.0280
Median_1_PearsonCorrelation      -0.0213
Median_1_ZScore                  -0.0280
Mode_1                           -0.0178
Mode_1_PearsonCorrelation        -0.0051
Mode_1_ZScore                    -0.0178
Median_4                         -0.0714
Skew_3                            0.4098
Range_2_PearsonCorrelation        0.5018
PointStep_5_PearsonCorrelation    0.4590
```

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.CorrelationTools`
/ tool `"CorrelationMatrix"` — every pairwise Pearson, Spearman or Kendall correlation in one call, with a matching
p-value matrix, pair counts and the strongest pairs pre-ranked with Benjamini-Hochberg adjusted p-values, choosing
pairwise or listwise deletion explicitly instead of `cor()`'s implicit `use=` behaviour. For a single pair, tool
`"PearsonCorrelation"` (or `"SpearmanRankCorrelation"` for the rank version) additionally returns a Fisher z confidence
interval that `cor()` alone does not.

### Procedure 4: Ranking Correlation by Absolute Strength

Both -1 and +1 are equally interesting extremes, so the dependent column is ranked by `abs()` of its correlation rather
than the signed value. The original relied on clicking a column header in the RStudio matrix viewer to sort it, which
cannot be captured in a script; `order()` is the programmatic equivalent.

```R
FDX <- read.csv("/work/FDX.csv")
Pearson <- cor(FDX, use = "complete", method = "pearson")
PearsonDependent <- Pearson[, "Dependent", drop = FALSE]

PearsonDependentAbs <- abs(PearsonDependent)
cat("Absolute correlation with Dependent:\n")
print(round(PearsonDependentAbs, 4))

cat("\nOrdered highest absolute correlation to lowest (programmatic equivalent of the\n")
cat("manual column-header sort in the RStudio matrix viewer):\n")
ordered <- PearsonDependentAbs[order(-PearsonDependentAbs[, 1]), , drop = FALSE]
print(round(ordered, 4))
```

Console output:

```
Absolute correlation with Dependent:
                               Dependent
Dependent                         1.0000
Median_1                          0.0280
Median_1_PearsonCorrelation       0.0213
Median_1_ZScore                   0.0280
Mode_1                            0.0178
Mode_1_PearsonCorrelation         0.0051
Mode_1_ZScore                     0.0178
Median_4                          0.0714
Skew_3                            0.4098
Range_2_PearsonCorrelation        0.5018
PointStep_5_PearsonCorrelation    0.4590

Ordered highest absolute correlation to lowest (programmatic equivalent of the
manual column-header sort in the RStudio matrix viewer):
                               Dependent
Dependent                         1.0000
Range_2_PearsonCorrelation        0.5018
PointStep_5_PearsonCorrelation    0.4590
Skew_3                            0.4098
Median_4                          0.0714
Median_1_ZScore                   0.0280
Median_1                          0.0280
Median_1_PearsonCorrelation       0.0213
Mode_1                            0.0178
Mode_1_ZScore                     0.0178
Mode_1_PearsonCorrelation         0.0051
```

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.CorrelationTools`
/ tool `"RankPredictorsByCorrelation"` — screens every column against one target and returns it already ranked by
absolute strength, with a p-value and a Benjamini-Hochberg adjusted p-value per column, so nothing needs a second manual
sort step.

### Procedure 5: Adding a Trend Line to a Scatter Plot

`stat_smooth(method = "lm")` overlays a line of best fit (with its default shaded confidence ribbon) on a ggplot2
scatter, giving an immediate visual read on direction and strength before any formal model is fitted.

```R
FDX <- read.csv("/work/FDX.csv")
library(ggplot2)

pdf("/work/p05_trend.pdf")
print(qplot(FDX$Median_4, FDX$Dependent) + stat_smooth(method = "lm"))
invisible(dev.off())
cat("Scatter plot with an lm() trend line rendered via stat_smooth().\n")
```

Console output:

```
`geom_smooth()` using formula = 'y ~ x'
Warning message:
`qplot()` was deprecated in ggplot2 3.4.0. 
Scatter plot with an lm() trend line rendered via stat_smooth().
```

As in the original course, this particular relationship (`Dependent` against `Median_4`) is shallow: the fitted slope
has predictive direction but very little strength taken alone, which is why the later procedures build a
multiple-predictor model instead.

**AdvancedAnalytics equivalent:** `Jube.Charting`, `Chart.Regression()` — draws the line, and unlike
`stat_smooth()`'s single ribbon it distinguishes a confidence band for the mean response from a wider prediction band
for a single new observation (Student t quantiles, polynomial fit up to degree 3), matching what
`FitSimpleLinearRegression`'s `predictAt` option computes numerically. Never ggplot2 for this in the product.

### Procedure 6: Creating a One Way Linear Regression Model

`lm(y ~ x, data)` fits ordinary least squares. Printing the model gives just the intercept and slope;
`summary()` adds standard errors, t statistics, p-values, R squared and the F test; `coef()` recovers the coefficients
at full floating point precision, which matters because the truncated display can round away digits that change a
prediction meaningfully (the original text's misspelled `coeefeicents()` is corrected to
`coef()` below).

```R
FDX <- read.csv("/work/FDX.csv")

LinearRegression <- lm(Dependent ~ Median_4, FDX)

cat("print(LinearRegression):\n")
print(LinearRegression)

cat("\nsummary(LinearRegression):\n")
print(summary(LinearRegression))

cat("\ncoef(LinearRegression) at full precision:\n")
print(coef(LinearRegression))
```

Console output:

```
print(LinearRegression):

Call:
lm(formula = Dependent ~ Median_4, data = FDX)

Coefficients:
(Intercept)     Median_4  
    0.01793     -0.09301  


summary(LinearRegression):

Call:
lm(formula = Dependent ~ Median_4, data = FDX)

Residuals:
      Min        1Q    Median        3Q       Max 
-0.092439 -0.017589 -0.000831  0.017018  0.080596 

Coefficients:
             Estimate Std. Error t value Pr(>|t|)    
(Intercept)  0.017931   0.001206  14.873   <2e-16 ***
Median_4    -0.093009   0.058187  -1.598    0.111    
---
Signif. codes:  0 '***' 0.001 '**' 0.01 '*' 0.05 '.' 0.1 ' ' 1

Residual standard error: 0.02695 on 498 degrees of freedom
Multiple R-squared:  0.005104,	Adjusted R-squared:  0.003107 
F-statistic: 2.555 on 1 and 498 DF,  p-value: 0.1106


coef(LinearRegression) at full precision:
(Intercept)    Median_4 
 0.01793091 -0.09300855 
```

The slope's p-value (0.111) is above the usual 0.05 cut-off, confirming visually what procedure 5 already suggested:
`Median_4` alone is not a reliable predictor of `Dependent`.

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsLinearRegressionTrainingService`
(`SubmitTrainingAsync`, model kind `MultipleLinearRegressionRecall.ModelKind`) trains and versions the model as a
governed asset with steps, metrics and evidence recorded against the run. For the interactive, nothing-persisted version
of exactly this call, `Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.LinearRegressionTools`
/ tool `"FitSimpleLinearRegression"` returns the same coefficient table (estimate, standard error, t, p-value,
confidence interval) plus R squared, adjusted R squared, the F test, AIC/AICc/BIC and a Durbin-Watson statistic that
`summary.lm()` does not print at all.

### Procedure 7: Deploying a One Way Linear Regression Manually with Vector Arithmetic

Deployment is just the fitted formula applied to a column: intercept plus the predictor multiplied by its coefficient,
computed once as vector arithmetic over every row at once, then appended to the data frame with
`mutate()`.

```R
FDX <- read.csv("/work/FDX.csv")
library(dplyr)

LinearRegression <- lm(Dependent ~ Median_4, FDX)
intercept <- coef(LinearRegression)[1]
slope <- coef(LinearRegression)[2]
cat("Intercept:", intercept, " Slope:", slope, "\n\n")

ManualLinearRegression <- intercept + (FDX$Median_4 * slope)

cat("ncol(FDX) before mutate:", ncol(FDX), "\n")
FDX <- mutate(FDX, ManualLinearRegression = ManualLinearRegression)
cat("ncol(FDX) after mutate:", ncol(FDX), "\n\n")

cat("head of the new column:\n")
print(head(FDX[, c("Median_4", "Dependent", "ManualLinearRegression")]))
```

Console output:

```
Intercept: 0.01793091  Slope: -0.09300855 

ncol(FDX) before mutate: 11 
ncol(FDX) after mutate: 12 

head of the new column:
      Median_4    Dependent ManualLinearRegression
1  0.008203630 -0.017198509             0.01716790
2  0.016148044  0.056111402             0.01642901
3  0.010337604  0.018602161             0.01696943
4 -0.045419611  0.017505100             0.02215532
5  0.001752893  0.037935467             0.01776788
6  0.025568083  0.001403019             0.01555286
```

**Tool equivalent:** none for hand-rolled vector arithmetic — the trained model returned by
`AdvancedAnalyticsLinearRegressionTrainingService` (or `PredictWithLinearRegression` below) is applied through the
recall path, never by an analyst re-typing coefficients into a formula. Manual deployment stays "what you'd do without
the platform."

### Procedure 8: Using the predict () Function for a One Way Linear Regression

`predict()` (dispatching to `predict.lm`) takes the fitted model and a data frame and returns the same vector procedure
7 built by hand, without re-typing the coefficients.

```R
FDX <- read.csv("/work/FDX.csv")
library(dplyr)

LinearRegression <- lm(Dependent ~ Median_4, FDX)
intercept <- coef(LinearRegression)[1]
slope <- coef(LinearRegression)[2]
FDX <- mutate(FDX, ManualLinearRegression = intercept + (Median_4 * slope))

AutomaticLinearRegression <- predict(LinearRegression, FDX)
FDX <- mutate(FDX, AutomaticLinearRegression = AutomaticLinearRegression)

cat("head of manual vs automatic prediction, side by side:\n")
print(head(FDX[, c("ManualLinearRegression", "AutomaticLinearRegression")]))

cat("\nall.equal(ManualLinearRegression, AutomaticLinearRegression):\n")
print(all.equal(FDX$ManualLinearRegression, FDX$AutomaticLinearRegression))

cat("\nmax(abs(difference)) once row-name attributes are stripped:\n")
print(max(abs(unname(FDX$ManualLinearRegression) - unname(FDX$AutomaticLinearRegression))))
```

Console output:

```
head of manual vs automatic prediction, side by side:
  ManualLinearRegression AutomaticLinearRegression
1             0.01716790                0.01716790
2             0.01642901                0.01642901
3             0.01696943                0.01696943
4             0.02215532                0.02215532
5             0.01776788                0.01776788
6             0.01555286                0.01555286

all.equal(ManualLinearRegression, AutomaticLinearRegression):
[1] "names for current but not for target"

max(abs(difference)) once row-name attributes are stripped:
[1] 0
```

The two vectors are numerically identical; `all.equal()` only flags that `predict()`'s result carries row-name
attributes that the hand-built vector does not, which `unname()` shows is cosmetic, not a real disagreement.

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.LinearRegressionTools`
/ tool `"PredictWithLinearRegression"` — fits on the supplied training rows and predicts new rows in the same call, but
goes well past `predict()`: every new row gets a confidence interval for the mean response, a wider prediction interval
for a single new observation, its leverage, and an `ExtrapolatedRows` count flagging rows that fall outside the region
the training data actually covered.

### Procedure 9: Identifying Confidence Intervals

Confidence intervals for the coefficients are not printed by `summary()` and must be requested separately with
`confint()` (`confint.lm` is the method actually dispatched to).

```R
FDX <- read.csv("/work/FDX.csv")
LinearRegression <- lm(Dependent ~ Median_4, FDX)

cat("confint(LinearRegression, level = 0.95):\n")
print(confint(LinearRegression, level = 0.95))
```

Console output:

```
confint(LinearRegression, level = 0.95):
                  2.5 %     97.5 %
(Intercept)  0.01556218 0.02029964
Median_4    -0.20732996 0.02131286
```

The slope's interval spans zero, consistent with procedure 6's p-value of 0.111: the data cannot rule out no
relationship at all between `Median_4` and `Dependent`.

**Tool equivalent:** no separate call needed. Both
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.LinearRegressionTools` tools
`"FitSimpleLinearRegression"` and `"FitMultipleLinearRegression"` return a confidence interval on every coefficient in
the same coefficient table as the estimate and p-value, at the caller's chosen `confidenceLevel`
(default 0.95); there is no `confint()`-style follow-up call to remember to make.

### Procedure 10: Create a Stepwise Linear Regression Model

Forward stepwise selection adds predictors one at a time in order of their correlation strength (procedure 4's ranking),
checking that R squared keeps improving and the p-values stay acceptable. Following the ranking from procedure 4,
`Range_2_PearsonCorrelation` and `Skew_3` enter first, then `PointStep_5_PearsonCorrelation`.

```R
FDX <- read.csv("/work/FDX.csv")

MultipleLinearRegression <- lm(Dependent ~ Skew_3 + Range_2_PearsonCorrelation, FDX)
cat("Step 1 -- Dependent ~ Skew_3 + Range_2_PearsonCorrelation\n")
print(summary(MultipleLinearRegression))

MultipleLinearRegression <- lm(Dependent ~ Skew_3 + Range_2_PearsonCorrelation +
                                  PointStep_5_PearsonCorrelation, FDX)
cat("\nStep 2 -- adding PointStep_5_PearsonCorrelation\n")
print(summary(MultipleLinearRegression))
```

Console output:

```
Step 1 -- Dependent ~ Skew_3 + Range_2_PearsonCorrelation

Call:
lm(formula = Dependent ~ Skew_3 + Range_2_PearsonCorrelation, 
    data = FDX)

Residuals:
      Min        1Q    Median        3Q       Max 
-0.066403 -0.014741 -0.000397  0.013683  0.060558 

Coefficients:
                            Estimate Std. Error t value Pr(>|t|)    
(Intercept)                0.0180323  0.0009447   19.09   <2e-16 ***
Skew_3                     0.0103199  0.0009670   10.67   <2e-16 ***
Range_2_PearsonCorrelation 0.0121017  0.0008961   13.51   <2e-16 ***
---
Signif. codes:  0 '***' 0.001 '**' 0.01 '*' 0.05 '.' 0.1 ' ' 1

Residual standard error: 0.0211 on 497 degrees of freedom
Multiple R-squared:  0.3913,	Adjusted R-squared:  0.3889 
F-statistic: 159.8 on 2 and 497 DF,  p-value: < 2.2e-16


Step 2 -- adding PointStep_5_PearsonCorrelation

Call:
lm(formula = Dependent ~ Skew_3 + Range_2_PearsonCorrelation + 
    PointStep_5_PearsonCorrelation, data = FDX)

Residuals:
      Min        1Q    Median        3Q       Max 
-0.069444 -0.014845 -0.000788  0.013559  0.058831 

Coefficients:
                                Estimate Std. Error t value Pr(>|t|)    
(Intercept)                    0.0179086  0.0009444  18.963  < 2e-16 ***
Skew_3                         0.0102603  0.0009649  10.634  < 2e-16 ***
Range_2_PearsonCorrelation     0.0096053  0.0015824   6.070 2.54e-09 ***
PointStep_5_PearsonCorrelation 0.0034428  0.0018010   1.912   0.0565 .  
---
Signif. codes:  0 '***' 0.001 '**' 0.01 '*' 0.05 '.' 0.1 ' ' 1

Residual standard error: 0.02104 on 496 degrees of freedom
Multiple R-squared:  0.3958,	Adjusted R-squared:  0.3921 
F-statistic: 108.3 on 3 and 496 DF,  p-value: < 2.2e-16
```

Adding `PointStep_5_PearsonCorrelation` raises R squared only marginally (0.3913 to 0.3958) while its own p-value
(0.0565) sits just above the conventional cut-off, and adjusted R squared barely moves — this is the plateau the
original text describes as the stopping signal, made sharper here because
`PointStep_5_PearsonCorrelation` is itself collinear with `Range_2_PearsonCorrelation` (see procedure 11), so it adds
little once `Range_2_PearsonCorrelation` is already in the model.

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.RegressionModelSelectionTools` / tool
`"StepwiseLinearRegression"` — runs Forward, Backward or Both-direction selection by AIC, BIC or p-value entry/exit
thresholds, records every addition or removal as a step with the criterion value at that point, and warns that stepwise
p-values are optimistic (recommending `CrossValidateLinearRegression` to check the result), which is a caveat R's manual
process leaves the analyst to know on their own.

### Procedure 11: Heat Map Correlation Matrix

Multicollinearity is an independent variable that correlates well with the dependent variable but also correlates with
another such variable, understating both variables' true contribution. It shows up as off-diagonal cells of the
correlation matrix that exceed 0.7 in absolute value.

```R
FDX <- read.csv("/work/FDX.csv")
Pearson <- cor(FDX, use = "complete", method = "pearson")

PearsonColinearity <- Pearson <= -0.7 | Pearson >= 0.7
cat("PearsonColinearity (TRUE marks |r| >= 0.7, off the trivial diagonal):\n")
print(PearsonColinearity)

pdf("/work/p11_heatmap.pdf")
heatmap(Pearson, symm = TRUE, main = "FDX Pearson correlation heat map")
invisible(dev.off())
cat("\nheatmap() rendered over the", nrow(Pearson), "x", ncol(Pearson), "correlation matrix.\n")
```

Console output:

```
PearsonColinearity (TRUE marks |r| >= 0.7, off the trivial diagonal):
                               Dependent Median_1 Median_1_PearsonCorrelation
Dependent                           TRUE    FALSE                       FALSE
Median_1                           FALSE     TRUE                        TRUE
Median_1_PearsonCorrelation        FALSE     TRUE                        TRUE
Median_1_ZScore                    FALSE     TRUE                        TRUE
Mode_1                             FALSE    FALSE                       FALSE
Mode_1_PearsonCorrelation          FALSE    FALSE                       FALSE
Mode_1_ZScore                      FALSE    FALSE                       FALSE
Median_4                           FALSE    FALSE                       FALSE
Skew_3                             FALSE    FALSE                       FALSE
Range_2_PearsonCorrelation         FALSE    FALSE                       FALSE
PointStep_5_PearsonCorrelation     FALSE    FALSE                       FALSE
                               Median_1_ZScore Mode_1 Mode_1_PearsonCorrelation
Dependent                                FALSE  FALSE                     FALSE
Median_1                                  TRUE  FALSE                     FALSE
Median_1_PearsonCorrelation               TRUE  FALSE                     FALSE
Median_1_ZScore                            TRUE  FALSE                     FALSE
Mode_1                                    FALSE   TRUE                     FALSE
Mode_1_PearsonCorrelation                 FALSE  FALSE                      TRUE
Mode_1_ZScore                             FALSE   TRUE                     FALSE
Median_4                                  FALSE  FALSE                     FALSE
Skew_3                                    FALSE  FALSE                     FALSE
Range_2_PearsonCorrelation                FALSE  FALSE                     FALSE
PointStep_5_PearsonCorrelation             FALSE  FALSE                     FALSE
                               Mode_1_ZScore Median_4 Skew_3
Dependent                              FALSE    FALSE  FALSE
Median_1                               FALSE    FALSE  FALSE
Median_1_PearsonCorrelation            FALSE    FALSE  FALSE
Median_1_ZScore                        FALSE    FALSE  FALSE
Mode_1                                  TRUE    FALSE  FALSE
Mode_1_PearsonCorrelation              FALSE    FALSE  FALSE
Mode_1_ZScore                           TRUE    FALSE  FALSE
Median_4                               FALSE     TRUE  FALSE
Skew_3                                 FALSE    FALSE   TRUE
Range_2_PearsonCorrelation             FALSE    FALSE  FALSE
PointStep_5_PearsonCorrelation         FALSE    FALSE  FALSE
                               Range_2_PearsonCorrelation
Dependent                                           FALSE
Median_1                                            FALSE
Median_1_PearsonCorrelation                         FALSE
Median_1_ZScore                                     FALSE
Mode_1                                              FALSE
Mode_1_PearsonCorrelation                           FALSE
Mode_1_ZScore                                       FALSE
Median_4                                            FALSE
Skew_3                                              FALSE
Range_2_PearsonCorrelation                           TRUE
PointStep_5_PearsonCorrelation                       TRUE
                               PointStep_5_PearsonCorrelation
Dependent                                               FALSE
Median_1                                                FALSE
Median_1_PearsonCorrelation                             FALSE
Median_1_ZScore                                         FALSE
Mode_1                                                  FALSE
Mode_1_PearsonCorrelation                               FALSE
Mode_1_ZScore                                           FALSE
Median_4                                                FALSE
Skew_3                                                  FALSE
Range_2_PearsonCorrelation                               TRUE
PointStep_5_PearsonCorrelation                           TRUE

heatmap() rendered over the 11 x 11 correlation matrix.
```

Three genuine collinear groups surface: the `Median_1` family (itself, its Pearson-correlation transform and its
z-score — all restatements of the same underlying number), the equivalent `Mode_1` family, and
`Range_2_PearsonCorrelation` with `PointStep_5_PearsonCorrelation` — which is exactly the pair procedure 10's stepwise
model found added little once the other was already in.

**AdvancedAnalytics equivalent:** `Jube.Charting`, `Chart.CorrelationMatrix()` (the `Matrices` chart family) — a
heat-map rendering of a correlation matrix with an optional hierarchical (average-linkage) ordering that groups
correlated variables visually, replacing base R's `heatmap()`. For the numeric detection step feeding it,
`Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.LinearRegressionTools` /
`FitMultipleLinearRegression`'s per-coefficient variance inflation factor (values above about 10 signal collinearity),
or dedicated diagnostics in the same tool group (`RegressionDiagnosticsTools` / tools `"VarianceInflationFactors"` and
`"MulticollinearityConditionDiagnostics"`), are the product's actual multicollinearity check rather than eyeballing a
0.7 threshold on a plotted matrix.
