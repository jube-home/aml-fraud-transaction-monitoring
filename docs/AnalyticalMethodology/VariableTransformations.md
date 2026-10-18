---
layout: default
title: Variable Transformations
nav_order: 10
parent: Analytical Methodology
---

# Variable Transformation Directory

This page is a standalone catalogue of variable and feature transformation techniques, kept separate from the
model-training pages that use them. [Summary Statistics](SummaryStatistics.md) already carries the original guide's own
two log-transform procedures (Procedures 12–13, condensed from a single paragraph) as part of its wider
summary-statistics sweep; this page is the fuller reference for that technique and every other rescaling, binning,
encoding and feature-construction technique an analyst reaches for in practice. Every transformation here also produces
the same packed `double[]` table shape that
[Data Frames vs the Packed Array](DataFramesVsAccordArrays.md) describes as the product's one data layer — a transform's
job is to rewrite the numbers in that table, never to change its shape contract.

Each transformation below follows the same shape as the rest of this guide: a real, re-run R snippet first (base R plus
already-installed packages; no fabricated console output), then the exact
`AdvancedAnalytics` tool that does the equivalent job in the product. The tools live in
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling`, split across `ScalingTools.cs`,
`BinningTools.cs`, `EncodingTools.cs` and `FeatureEngineeringTools.cs`. A "Fit" tool learns parameters from training
rows and returns a transformed table plus those parameters; the matching "Apply" tool reuses the parameters on new rows
so training and live scoring see identical arithmetic, which plain R scripting has to reproduce by hand every time.

## Log Transform and Its Reverse

Money and count variables are usually right-skewed: a handful of very large values stretch the mean and standard
deviation away from where most of the data sits. The natural-log transform compresses that long right tail, and its
reverse (`exp`) recovers the original units for reporting.

### Procedure 1: Create a Log Transformation

```R
set.seed(42)
Amount <- round(rlnorm(10, meanlog = 4, sdlog = 1), 2)
Amount

LogAmount <- log(Amount)
round(LogAmount, 4)
```

Console output:

```
 [1] 215.07  31.04  78.50 102.81  81.80  49.10 247.53  49.67 410.93  51.28
 [1] 5.3710 3.4353 4.3631 4.6329 4.4043 3.8939 5.5115 3.9054 6.0184 3.9373
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tool `"ApplyColumnFunction"` with `function: NaturalLog` — applies `ln(x + offset)` to every cell of a column,
returning the transformed table and how many cells were undefined (non-positive after the offset) rather than silently
producing `NaN` in the console the way plain R does.

### Procedure 2: Reverse a Log Transformation

```R
RestoredAmount <- exp(LogAmount)
round(RestoredAmount, 2)
```

Console output:

```
 [1] 215.07  31.04  78.50 102.81  81.80  49.10 247.53  49.67 410.93  51.28
```

**Tool equivalent:** none as a dedicated inverse — `ApplyColumnFunction` has no inverse counterpart of its
own, because a plain log/exp pair carries no fitted parameters to invert. Where the platform does track parameters (a
power transform's lambda, a scaler's center and scale) it gives an explicit inverse tool instead, covered in the next
two sections.

The log transform itself has no single-paper origin: it is the best-known member of Tukey's "ladder of powers" (Tukey,
*Exploratory Data Analysis*, Addison-Wesley, 1977), the ordered family of power transforms (..., 1/x, log x, sqrt x, x,
x², ...) an analyst climbs by eye. Box-Cox and Yeo-Johnson, below, select a member of that same family automatically by
likelihood rather than by eye.

## Power Transforms: Square Root, Box-Cox and Yeo-Johnson

A single fixed function such as `log` or `sqrt` is a guess at how much a column needs to be pulled in. Box-Cox and
Yeo-Johnson instead search for the power (lambda) that makes a column look as close to normal as possible, by maximising
a normal log-likelihood over lambda — the same criterion the product's
`FitPowerTransformer` uses internally. Box-Cox needs strictly positive data; Yeo-Johnson extends the same idea to zero
and negative values by splitting the formula at zero.

Box-Cox is Box, G. E. P. and Cox, D. R. (1964), "An Analysis of Transformations," *Journal of the Royal Statistical
Society, Series B*, 26 (2): 211–252 — y (λ) = (x^λ − 1)/λ for λ≠0, ln x for λ=0, restricted to x>0, with λ chosen to
maximise a normal log-likelihood. Yeo-Johnson is Yeo, I.-K. and Johnson, R. A. (2000), "A New Family of Power
Transformations to Improve Normality or Symmetry," *Biometrika*, 87 (4):
954–959 — the signed extension that mirrors the same idea onto x+1 for x≥0 and onto −x+1 (with exponent 2−λ) for x<0, so
it accepts the zero and negative values Box-Cox rejects.

### Procedure 3: Square Root Transform

```R
set.seed(42)
Amount <- round(rlnorm(200, meanlog = 4, sdlog = 1), 2)

SqrtAmount <- sqrt(Amount)
round(head(SqrtAmount), 4)
```

Console output:

```
[1] 14.6653  5.5714  8.8600 10.1395  9.0443  7.0071
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tool `"ApplyColumnFunction"` with `function: Sqrt` (or `SignedSqrt` when the column can be negative, computing
`sign(x) * sqrt(|x|)`).

### Procedure 4: Box-Cox Power Transform via Maximum Likelihood

This reproduces the product's own fitting rule by hand: search a grid of lambda, transform with
`(x^lambda - 1) / lambda` (or `log(x)` at lambda 0), and keep the lambda with the highest normal log-likelihood.

```R
boxcox_transform <- function(x, lambda) {
  if (abs(lambda) < 1e-12) log(x) else (x^lambda - 1) / lambda
}

boxcox_loglik <- function(x, lambda) {
  y <- boxcox_transform(x, lambda)
  n <- length(y)
  var_y <- sum((y - mean(y))^2) / n
  jacobian <- sum(log(x))
  -n / 2 * log(var_y) + (lambda - 1) * jacobian
}

lambda_grid <- seq(-2, 2, by = 0.01)
loglik <- sapply(lambda_grid, function(l) boxcox_loglik(Amount, l))
best_lambda <- lambda_grid[which.max(loglik)]
round(best_lambda, 2)

BoxCoxAmount <- boxcox_transform(Amount, best_lambda)
round(head(BoxCoxAmount), 4)

round(c(mean = mean(Amount), skew_before = mean((Amount - mean(Amount))^3) / sd(Amount)^3,
        skew_after = mean((BoxCoxAmount - mean(BoxCoxAmount))^3) / sd(BoxCoxAmount)^3), 4)
```

Console output:

```
[1] 0.05
[1] 6.1613 3.7479 4.8756 5.2134 4.9269 4.2988
       mean skew_before  skew_after 
    83.6283      3.5226      0.0110 
```

The fitted lambda of 0.05 is close to 0, so this column is behaving almost exactly like a log transform — skewness drops
from 3.52 to essentially 0. A real `MASS::boxcox` likelihood plot would sharpen the same picture but is not required
here: this grid search already reproduces the maximisation the product performs with
`Accord.Math.Optimization.BrentSearch`.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tools `"FitPowerTransformer"` (learns one lambda per column by maximising the same normal log-likelihood,
`method: BoxCox`) and `"ApplyPowerTransformer"` (applies fixed lambdas to new rows using the identical formula).

### Procedure 5: Yeo-Johnson Power Transform for Signed Data

Box-Cox refuses a column with zeros or negative values (a daily profit-and-loss delta, for example). Yeo-Johnson keeps
the same maximum-likelihood idea but applies one formula to the non-negative side of a column and a mirrored one to the
negative side.

```R
set.seed(42)
Delta <- round(rt(200, df = 3) * 50, 2)
round(head(Delta), 2)

yeojohnson_transform <- function(x, lambda) {
  pos <- x >= 0
  y <- numeric(length(x))
  y[pos] <- if (abs(lambda) < 1e-12) log(x[pos] + 1) else ((x[pos] + 1)^lambda - 1) / lambda
  neg <- !pos
  y[neg] <- if (abs(lambda - 2) < 1e-12) -log(-x[neg] + 1) else -(((-x[neg] + 1)^(2 - lambda) - 1) / (2 - lambda))
  y
}

yeojohnson_loglik <- function(x, lambda) {
  y <- yeojohnson_transform(x, lambda)
  n <- length(y)
  var_y <- sum((y - mean(y))^2) / n
  jacobian <- sum(sign(x) * log(1 + abs(x)))
  -n / 2 * log(var_y) + (lambda - 1) * jacobian
}

lambda_grid <- seq(-2, 4, by = 0.01)
loglik <- sapply(lambda_grid, function(l) yeojohnson_loglik(Delta, l))
best_lambda <- lambda_grid[which.max(loglik)]
round(best_lambda, 2)

YeoJohnsonDelta <- yeojohnson_transform(Delta, best_lambda)
round(head(YeoJohnsonDelta), 4)
```

Console output:

```
[1] 116.98   6.55  -3.70  -2.89  -2.32 458.25
[1] 0.91
[1]  83.2937   5.8177  -4.0389  -3.1155  -2.4758 289.5866
```

**Tool equivalent:** the same
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools` tools,
`"FitPowerTransformer"` and `"ApplyPowerTransformer"`, with `method: YeoJohnson` — the default method for both tools,
precisely because most real columns are not guaranteed positive.

**Code audit:** `FitPowerTransformer` genuinely performs the maximum-likelihood search the textbook describes rather
than applying a fixed lambda. `LogLikelihood`
(`ScalingTools.cs:664-680`) computes the same profile log-likelihood Box and Cox derive — `-n/2 *
log(variance)` plus `(lambda - 1)` times a Jacobian term, `Σ log(x)` for Box-Cox and `Σ sign(x) *
log(1 + |x|)` for Yeo-Johnson, matching each paper's own Jacobian — and the fit loop (`ScalingTools.cs:350-353`) hands
that function to `Accord.Math.Optimization.BrentSearch.Maximize` over a configurable search range (default `[-5, 5]`,
`ScalingTools.cs:317`), so the lambda returned is the one that actually maximises the likelihood on the supplied column,
not a hand-picked constant. `PowerValue`
(`ScalingTools.cs:640-661`) implements both formulas exactly, including the Yeo-Johnson sign-dependent split at x=0, and
Box-Cox rejects any non-positive value up front (`ScalingTools.cs:343-348`) rather than silently producing NaN mid-fit.

## Standardisation and Range Normalisation

Standardisation (z-score) and range normalisation (min-max) both rescale a column to a common footing, but answer
different questions: a z-score says how many standard deviations a value sits from the mean; a min-max value says where
a value sits between the observed minimum and maximum, in the same 0–1 units whatever the column's original scale.

### Procedure 6: Z-Score Standardisation

```R
set.seed(42)
Amount <- round(rlnorm(10, meanlog = 4, sdlog = 1), 2)
Amount

ZAmount <- (Amount - mean(Amount)) / sd(Amount)
round(ZAmount, 4)
```

Console output:

```
 [1] 215.07  31.04  78.50 102.81  81.80  49.10 247.53  49.67 410.93  51.28
 [1]  0.6810 -0.8236 -0.4355 -0.2368 -0.4086 -0.6759  0.9464 -0.6713  2.2823
[10] -0.6581
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tools `"FitTableScaler"` with `method: Standard` (learns the mean and sample standard deviation per column, returning
them as `Centers`/`Scales`), `"ApplyTableScaler"` (applies fixed centers/scales to new rows so validation and live data
are scaled with training statistics only, avoiding leakage) and
`"InvertTableScaler"` (recovers original units from a scaled value, center and scale, for reporting model outputs or
cluster centres). The same tool also offers `RobustMad` (median and scaled median absolute deviation) and `RobustIqr`
(median and interquartile range) for columns where outliers should not be allowed to set the scale.

### Procedure 7: Min-Max Range Normalisation

```R
MinMaxAmount <- (Amount - min(Amount)) / (max(Amount) - min(Amount))
round(MinMaxAmount, 4)
```

Console output:

```
[1] 0.4844 0.0000 0.1249 0.1889 0.1336 0.0475 0.5699 0.0490 1.0000 0.0533
```

**Tool equivalent:** the same `FitTableScaler` / `ApplyTableScaler` / `InvertTableScaler`
family with `method: MinMax`, which learns the minimum as `Centers` and the range (`max - min`) as
`Scales` so `(value - center) / scale` lands every finite training value inside 0..1.

Both formulas are classical and carry no single citation: z-score standardisation is `z = (x - mean) /
sd`, using whichever standard deviation the implementation chooses (population or sample); min-max normalisation is
`x' = (x - min) / (max - min)`, placing every training value in 0..1.

**Code audit:** `FitTableScaler`'s `Standard` branch calls `Standardisation.Fit`
(`Jube.Analytics/Standardisation.cs:29-43`), which computes the mean and the *sample* standard deviation
(`/(finite.Length - 1)`), matching R's `sd()` rather than a population (n-divisor) figure. Division by zero is guarded:
`Scale()` (`ScalingTools.cs:575-583`) checks `scales[j] == 0 || !double.IsFinite(scales[j])`
and returns 0 for that cell instead of NaN or Infinity, and `InvertTableScaler` shares the same guarded centers/scales.
The file's other two scale rules reuse the same audited helpers: `RobustMad`
(`ScalingTools.cs:125-129`) calls `RobustScaler.Fit` (`Jube.Analytics/Scalers.cs:87-93`), which scales the median
absolute deviation by the constant `1.482602218505602` — the standard consistency factor 1/Φ⁻¹ (0.75) that makes MAD a
consistent estimator of the normal standard deviation — matching the
"1.4826" the tool's own description states; `RobustIqr` (`ScalingTools.cs:130-134`) uses the same
`DataPreparationGuard.Quantile` interpolation audited under Quantile Transformation below.

## Quantile Transformation

A power transform bends a column with one formula. A quantile transform instead learns the column's own empirical
distribution and maps each value to its rank within it, which is far more robust to heavy tails and to outliers that a
power transform would still leave stretched out — only the ordering of values matters, not their magnitude.

### Procedure 8: Quantile Transformation to Uniform and Normal

```R
set.seed(42)
Amount <- round(rlnorm(200, meanlog = 4, sdlog = 1), 2)

EmpiricalCdf <- ecdf(Amount)
UniformAmount <- EmpiricalCdf(Amount)
round(head(UniformAmount), 4)

NormalAmount <- qnorm(pmin(pmax(UniformAmount, 1e-7), 1 - 1e-7))
round(head(NormalAmount), 4)

round(c(before_skew = mean((Amount - mean(Amount))^3) / sd(Amount)^3,
        after_skew = mean((NormalAmount - mean(NormalAmount))^3) / sd(NormalAmount)^3), 4)
```

Console output:

```
[1] 0.920 0.265 0.655 0.750 0.670 0.460
[1]  1.4051 -0.6280  0.3989  0.6745  0.4399 -0.1004
before_skew  after_skew 
     3.5226      0.5430 
```

`ecdf()` here plays the same role as the product's stored reference quantiles: both convert a value into
"the fraction of training rows at or below this," clamped away from exactly 0 or 1 before it is pushed through `qnorm`
/the standard-normal inverse.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tools `"FitQuantileTransformer"` (stores up to `quantileCount` reference quantiles per column instead of the full
empirical CDF, output `Uniform` or `Normal`) and `"ApplyQuantileTransformer"` (maps new rows onto those stored
quantiles; values beyond the training range clamp to the ends rather than extrapolate).

This is the probability integral transform, a standard result in distribution theory rather than a named invention: if X
has continuous CDF F, then F (X) is distributed Uniform (0,1); pushing that uniform value through the standard normal
quantile function Φ⁻¹ gives the Normal output option used above. It underlies rank-based nonparametric statistics
generally and has no single origin paper.

**Code audit:** `DataPreparationGuard.Quantile` (`DataPreparationGuard.cs:236-242`) — used both to build
`FitQuantileTransformer`'s reference quantiles and, separately, by `DiscretiseEqualFrequency`'s cut points below —
interpolates with `position = q * (n - 1)`, `lower + (position - lower) * (upper - lower)`, which is exactly R's
`stats::quantile()` default (`type = 7`) interpolation rule, not one of R's other eight interpolation types.
`ApplyQuantileTransformer`'s inverse mapping, `EmpiricalPosition`
(`ScalingTools.cs:603-638`), diverges from a plain single-direction lookup (the kind `numpy.interp` does):
it scans the reference array from both ends and averages the two resulting rank estimates before dividing by n-1, which
symmetrises the result when stored reference quantiles contain duplicate values (ties)
rather than always taking the first match. This is a deliberate tie-handling choice, not a bug, but it is worth knowing
it is not identical to scikit-learn's `QuantileTransformer.inverse_transform`.

## Outlier Winsorisation and Clipping

Winsorising caps extreme values at a threshold instead of deleting the rows they belong to, which keeps the row count
and every other column's data intact while stopping one column's extremes from dominating a distance-based or linear
model.

### Procedure 9: Winsorise Outliers at the 1st and 99th Percentiles

```R
set.seed(42)
Amount <- round(rlnorm(200, meanlog = 4, sdlog = 1), 2)
Amount[3] <- 50000
Amount[57] <- 48000

Bounds <- quantile(Amount, probs = c(0.01, 0.99))
Bounds

ClippedAmount <- pmin(pmax(Amount, Bounds[1]), Bounds[2])
round(range(Amount), 2)
round(range(ClippedAmount), 2)
sum(Amount != ClippedAmount)
```

Console output:

```
       1%       99% 
   4.7507 1285.8006 
[1]     2.74 50000.00
[1]    4.75 1285.80
[1] 4
```

Two planted extreme values (50000 and 48000) are pulled in to the 99th-percentile bound, and the four cells that ended
up clipped (both planted outliers plus the two natural values already past the 1st/99th percentile) are reported by
count.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.ScalingTools`
/ tools `"FitOutlierClipper"` (learns lower/upper bounds per column; `method: Quantiles` reproduces this snippet
exactly, with `IqrFences`, `StandardDeviations` and `MedianAbsoluteDeviations` as alternative bound rules) and
`"ApplyOutlierClipper"` (clips new rows to fixed bounds and reports how many cells were clipped low and high per column,
rather than a single combined count).

The name comes from Charles Winsor, via John Tukey, who popularised winsorising and clipping as part of the exploratory
data analysis tradition (Tukey, *Exploratory Data Analysis*, Addison-Wesley, 1977); the 1.5×IQR "fence" `IqrFences`
defaults to below is Tukey's own rule of thumb from that same tradition.

**Code audit:** `FitOutlierClipper` (`ScalingTools.cs:444-521`) implements all four bound rules exactly as described:
`Quantiles` (`ScalingTools.cs:497-499`) matches this procedure's `quantile()` call cell for cell; `IqrFences`
(`ScalingTools.cs:501-506`) computes `Q1 - k*IQR` / `Q3 + k*IQR` with a default
`multiplier` of 1.5, i.e. Tukey's fences; `StandardDeviations` (`ScalingTools.cs:507-511`) and
`MedianAbsoluteDeviations` (`ScalingTools.cs:512-516`) reuse the same `Standardisation.Fit` /
`RobustScaler.Fit` helpers audited under Standardisation above, so their scale estimates are the same sample-SD and
1.4826×MAD figures. `Clip()` (`ScalingTools.cs:688-718`) is a plain bound-and-count clip with no numerical surprises.

## Discretisation and Binning

Discretising turns a continuous column into a small number of ordered categories. This trades away resolution for two
things a raw continuous value cannot give a downstream model or a business rule:
categories a rule can name directly ("bin 3, $5,000–$10,000") and, when a target is supplied, a weight of evidence and
information value per bin for scorecard-style modelling.

### Procedure 10: Equal-Width and Custom-Edge Binning with cut ()

```R
set.seed(42)
Amount <- round(rlnorm(200, meanlog = 4, sdlog = 1), 2)

EqualWidthBins <- cut(Amount, breaks = 5)
table(EqualWidthBins)

RegulatoryBins <- cut(Amount, breaks = c(-Inf, 1000, 5000, 10000, Inf),
                       labels = c("under1000", "1000to5000", "5000to10000", "over10000"))
table(RegulatoryBins)
```

Console output:

```
EqualWidthBins
(1.93,165]  (165,327]  (327,489]  (489,652]  (652,815] 
       173         21          4          1          1 
RegulatoryBins
  under1000  1000to5000 5000to10000   over10000 
        200           0           0           0 
```

The equal-width result illustrates exactly the weakness the product's own tool description warns about:
with this skewed a column, 173 of 200 rows land in the first of five equal-width bins, which is nearly useless for
separating cases. The regulatory-band cut points (1000/5000/10000) show custom edges, though this particular simulated
column never reaches them.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.BinningTools`
/ tools `"DiscretiseEqualWidth"` (equal-width, matching the first `cut()` call and its own documented weakness on skewed
data — `"DiscretiseEqualFrequency"` cuts at evenly spaced quantiles instead, the usual fix), `"DiscretiseCustomEdges"`
(fixed cut points, matching the regulatory-band `cut()` call) and
`"ApplyBinEdges"` (assigns new rows to bins from any of the `Discretise*` tools' edges, so live data is binned
identically to training data). Passing 0/1 labels to any `Discretise*` tool additionally returns each bin's event rate,
weight of evidence and the column's total information value — R's `cut()` and
`table()` have no equivalent of that in one call.

Binning a continuous variable this way is surveyed in Dougherty, J., Kohavi, R. and Sahami, M. (1995),
"Supervised and Unsupervised Discretization of Continuous Features," *Proceedings of the 12th International Conference
on Machine Learning (ICML)* — which frames equal-width and equal-frequency binning as the two basic unsupervised
(target-blind) strategies demonstrated above, against which supervised methods such as the one below are compared.

**Code audit:** `DiscretiseEqualFrequency` (`BinningTools.cs:61-80`) places its cut points with the same
`DataPreparationGuard.Quantile` type-7 interpolation audited under Quantile Transformation above, then deduplicates
repeated cut points caused by ties before building bins.

### Procedure 11: K-Means Binning

Where equal-width or equal-frequency bins ignore how a column actually clumps, k-means binning clusters the values into
k groups and cuts halfway between neighbouring cluster centres — closer to how the data is actually shaped. `kmeans()`
is a base R function from the `stats` package (always loaded), not from
`cluster`; `cluster` supplies `pam`/`clara` but no `kmeans()` of its own.

```R
set.seed(42)
Amount <- round(rlnorm(200, meanlog = 4, sdlog = 1), 2)

KmeansFit <- kmeans(Amount, centers = 4, iter.max = 200, nstart = 5)
SortedCentres <- sort(KmeansFit$centers[, 1])
round(SortedCentres, 2)

Edges <- (head(SortedCentres, -1) + tail(SortedCentres, -1)) / 2
round(Edges, 2)

KMeansBins <- cut(Amount, breaks = c(-Inf, Edges, Inf), labels = FALSE)
table(KMeansBins)
```

Console output:

```
     2      1      4      3 
 35.97 109.73 246.76 675.64 
     2      1      4 
 72.85 178.25 461.20 
KMeansBins
  1   2   3   4 
127  49  22   2 
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.BinningTools`
/ tool `"DiscretiseKMeans"` — clusters a column with `Accord.MachineLearning.Clustering.KMeans`, seeding cluster centres
from evenly spaced quantiles rather than the random restarts `nstart` uses here, which makes the product's result
deterministic across runs on the same data; the edges are cut halfway between sorted centres exactly as in this snippet.
The same file's `"DiscretiseSupervised"` tool is a fourth option worth naming here even without its own R snippet: it
places cut points using a one-variable decision tree against a 0/1 target, which is the closest thing to "optimal
binning for a scorecard" in the family.

That target-aware alternative belongs to the family Fayyad, U. M. and Irani, K. B. (1993) established in
"Multi-Interval Discretization of Continuous-Valued Attributes for Classification Learning," *Proceedings of IJCAI-93* —
recursively splitting at the value that most reduces class entropy, stopped by a minimum-description-length criterion.

**Code audit:** `DiscretiseSupervised` (`BinningTools.cs:138-213`) does perform genuine entropy-based splitting, but via
`Accord.MachineLearning.DecisionTrees.Learning.C45Learning`
(`BinningTools.cs:177-182`) — Quinlan's C4.5 induction algorithm, which splits on gain ratio (an entropy-derived
criterion) rather than the criterion Fayyad and Irani's own paper defines. More importantly, the stopping rule is not
Fayyad-Irani's minimum-description-length test: it is a fixed
`MaxHeight = ceil(log2(maximumBins))` and a `MinimumLeafSize` derived from `minimumBinFraction`
(`BinningTools.cs:176-182`). Cut points are then read off the tree breadth-first and only the first
`maximumBins - 1` kept (`BinningTools.cs:186-210`), which approximates "the most informative splits first" because C4.5
chooses its highest-gain-ratio split at the root — but this is a heuristic truncation by hyperparameter, not
Fayyad-Irani's own recursive MDL stopping condition. In short: genuine entropy-based partitioning, using a different
tree-induction algorithm and a simpler, hyperparameter-driven stopping rule than the paper the technique is usually
named after.

## Categorical Encoding

Most models cannot consume text categories directly. Label encoding gives every category an arbitrary integer, useful
for tree models but wrong for anything that reads magnitude as meaning. One-hot encoding avoids that by giving every
category its own 0/1 column. Neither uses a target; target-mean encoding, weight-of-evidence encoding and frequency
encoding all fold in extra information (an outcome, or how common a category is) instead of just an arbitrary or
expanding representation.

### Procedure 12: Label Encoding

```R
set.seed(42)
MerchantCategory <- sample(c("grocery", "electronics", "travel", "jewellery"),
                            size = 20, replace = TRUE, prob = c(0.4, 0.3, 0.2, 0.1))
IsFraud <- rbinom(20, 1, prob = ifelse(MerchantCategory == "jewellery", 0.6,
                                 ifelse(MerchantCategory == "travel", 0.3, 0.05)))

MerchantFactor <- factor(MerchantCategory)
LabelEncoded <- as.integer(MerchantFactor) - 1
data.frame(MerchantCategory, LabelEncoded)
levels(MerchantFactor)
```

Console output:

```
   MerchantCategory LabelEncoded
1         jewellery            2
2         jewellery            2
3           grocery            1
4            travel            3
5       electronics            0
6       electronics            0
7            travel            3
8           grocery            1
9       electronics            0
10           travel            3
11      electronics            0
12           travel            3
13        jewellery            2
14          grocery            1
15      electronics            0
16        jewellery            2
17        jewellery            2
18          grocery            1
19      electronics            0
20      electronics            0
[1] "electronics" "grocery"     "jewellery"   "travel"     
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.EncodingTools`
/ tools `"FitLabelEncoder"` (learns a category list per column — `Sorted` matches R's alphabetical
`factor()` ordering used above, with `FirstAppearance` and `MostFrequentFirst` as alternatives — and returns the coded
table) and `"ApplyLabelEncoder"` (codes new rows from a fixed category list, or from a hand-supplied list in rank order
for a genuinely ordinal category such as Low/Medium/High; anything not found gets a configurable `unknownCode`, unlike
R's `factor()`, which would turn an unseen level into
`NA`).

### Procedure 13: One-Hot Encoding

```R
OneHot <- model.matrix(~ MerchantFactor - 1)
colnames(OneHot) <- levels(MerchantFactor)
head(OneHot, 8)
```

Console output:

```
  electronics grocery jewellery travel
1           0       0         1      0
2           0       0         1      0
3           0       1         0      0
4           0       0         0      1
5           1       0         0      0
6           1       0         0      0
7           0       0         0      1
8           0       1         0      0
```

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.EncodingTools`
/ tools `"FitOneHotEncoder"` (expands each column into one 0/1 column per category, named
`<column>=<category>`; `dropFirst` mirrors dropping the reference level to avoid the dummy-variable trap, and
`maximumCategoriesPerColumn` guards against a high-cardinality column silently exploding the table's width the way
`model.matrix` would) and `"ApplyOneHotEncoder"` (encodes new rows from a fixed category list; an unseen category
becomes all zeros rather than erroring).

### Procedure 14: Target-Mean Encoding

```R
TargetMeanTable <- aggregate(IsFraud, by = list(MerchantCategory = MerchantCategory), FUN = mean)
names(TargetMeanTable)[2] <- "RawEventRate"
OverallRate <- mean(IsFraud)
CountTable <- table(MerchantCategory)
TargetMeanTable$Count <- as.integer(CountTable[TargetMeanTable$MerchantCategory])
Smoothing <- 10
TargetMeanTable$SmoothedEncoding <- (TargetMeanTable$Count * TargetMeanTable$RawEventRate +
                                        Smoothing * OverallRate) / (TargetMeanTable$Count + Smoothing)
TargetMeanTable
```

Console output:

```
  MerchantCategory RawEventRate Count SmoothedEncoding
1      electronics         0.00     7        0.2058824
2          grocery         0.25     4        0.3214286
3        jewellery         0.60     5        0.4333333
4           travel         0.75     4        0.4642857
```

Every category's raw event rate is pulled towards the 30% overall rate, and pulled hardest where a category has the
fewest rows (`electronics` here, at 7 rows, moves furthest from its raw 0%); this is the same smoothing formula
`(n_c * rate_c + m * overallRate) / (n_c + m)` the product tool uses, with `m` as the pseudo-observation count
`Smoothing` above.

**Tool equivalent:** `Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.EncodingTools`
/ tool `"FitTargetMeanEncoder"` (must be fit on training rows only, since the target leaks into the encoding) alongside
`"ApplyCategoryEncoder"` for scoring new rows from the fitted mapping. The same file's
`"FitWeightOfEvidenceEncoder"` (the credit-scoring `ln(event share / non-event share)` encoding, which also reports each
column's information value) and `"FitFrequencyEncoder"` (encodes a category by its raw count or share of rows, useful
for high-cardinality identifiers with no target at all) are the file's other two target-aware/frequency-aware options,
both sharing `"ApplyCategoryEncoder"` to score new data.

`"FitRareCategoryGrouping"`/`"ApplyRareCategoryGrouping"` are worth knowing about alongside all of the above: they
collapse rare categories into a single `other` label before any of these encoders run, which caps how wide a one-hot
table gets and stops a scarcely-seen category from dominating a target-mean or weight-of-evidence cell.

Target-mean encoding with additive smoothing follows Micci-Barreca, D. (2001), "A Preprocessing Scheme for
High-Cardinality Categorical Attributes in Classification and Regression Problems," *ACM SIGKDD Explorations*, 3 (1):
27–32 — the same `(n_c * rate_c + m * prior) / (n_c + m)` blend used in the R snippet above. Weight-of-evidence encoding
is standard credit-scoring practice rather than a single paper's invention; Siddiqi, N., *Credit Risk Scorecards:
Developing and Implementing Intelligent Credit Scoring* (Wiley, 2006) is the standard reference for both the
`ln(event share / non-event share)` formula and the paired information-value statistic
`Σ(event share - non-event share) * WoE`.

**Code audit:** `FitTargetMeanEncoder` (`EncodingTools.cs:148-169`) implements Micci-Barreca's formula verbatim —
`(n * (e / (double)n) + smoothing * overall) / (n + smoothing)` at `EncodingTools.cs:169` — with `m` exposed as the
`smoothing` parameter (default 10 pseudo-observations), not an unsmoothed raw mean. `FitWeightOfEvidenceEncoder`
(`EncodingTools.cs:171-204`) adds its own Laplace-style smoothing to both the event and non-event shares before taking
the log (`EncodingTools.cs:198-203`, default pseudo-count 0.5), matching Siddiqi's practical guidance to avoid `ln(0)`
on empty categories.

There is a genuine inconsistency worth flagging: the per-category weight-of-evidence values are smoothed, but the
column's reported information value is computed separately in `BuildEncoding`
(`EncodingTools.cs:450-461`) from the *raw, unsmoothed* event and non-event shares, and silently contributes 0 for any
category with zero events or zero non-events instead of reusing the same smoothed shares as the WoE column sitting next
to it. The two numbers the tool returns side by side — a category's weight of evidence and the column's overall
information value — are therefore not computed on a consistent basis.

## Feature Engineering: Cyclical Encoding of a Periodic Variable

An hour-of-day or day-of-week column is periodic: hour 23 and hour 0 are one hour apart, not 23. Encoding it as a single
number (or as one-hot categories) hides that adjacency. Splitting it into a sine and a cosine component instead places
every hour on a circle, so the numeric distance between encoded hours matches the real distance around the clock.

### Procedure 15: Cyclical (Sine/Cosine) Encoding of Hour-of-Day

```R
HourOfDay <- c(0, 6, 12, 18, 23)

HourSin <- sin(2 * pi * HourOfDay / 24)
HourCos <- cos(2 * pi * HourOfDay / 24)
round(data.frame(HourOfDay, HourSin, HourCos), 4)

Distance <- function(h1, h2) {
  sqrt((sin(2*pi*h1/24) - sin(2*pi*h2/24))^2 + (cos(2*pi*h1/24) - cos(2*pi*h2/24))^2)
}
round(c(dist_23_to_0 = Distance(23, 0), dist_0_to_12 = Distance(0, 12)), 4)
```

Console output:

```
  HourOfDay HourSin HourCos
1         0  0.0000  1.0000
2         6  1.0000  0.0000
3        12  0.0000 -1.0000
4        18 -1.0000  0.0000
5        23 -0.2588  0.9659
dist_23_to_0 dist_0_to_12 
      0.2611       2.0000 
```

Hour 23 sits only 0.26 encoded units from hour 0 — genuinely close — while hour 0 sits a full 2.0 units from hour 12,
the maximum possible distance on the circle: the encoding recovers the clock's real geometry, which a plain 0–23 integer
column cannot.

**Tool equivalent:**
`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling.FeatureEngineeringTools`
/ tool `"EncodeCyclicalFeature"` — returns the `sin`/`cos` pair for any period (24 for hour-of-day, 7 for day-of-week,
12 for month), computed with the identical `sin(2*pi*x/period)`/`cos(2*pi*x/period)`
formulas. The same file's `"GenerateLagFeatures"` (lag/diff/rolling-window features for an already-time-ordered series,
optionally grouped by an account or entity key) and
`"GeneratePolynomialFeatures"` (products and powers of numeric columns up to a chosen degree, with an
`interactionOnly` option to skip pure powers) are the file's other two feature-construction tools beyond simple
rescaling.

Alongside those, `"GenerateColumnPairFeatures"` builds named pairwise ratios, differences, sums, minimums and maximums
such as amount/balance, and `"ExtractDateTimeParts"`/`"ExtractEpochParts"` produce the raw hour-of-day, day-of-week and
similar integers that `"EncodeCyclicalFeature"` then turns cyclical.

Encoding a periodic variable as a sine/cosine pair is standard practice in circular statistics rather than a single
named result — see Mardia, K. V. and Jupp, P. E., *Directional Statistics* (Wiley, 1999)
for the general treatment of angular data as points on a unit circle, of which this is the applied feature-engineering
case.

**Code audit:** `EncodeCyclicalFeature` (`FeatureEngineeringTools.cs:428-446`) computes
`Math.Sin(2 * Math.PI * v / period)` and `Math.Cos(2 * Math.PI * v / period)`
(`FeatureEngineeringTools.cs:441-443`) — the exact formula used in the R snippet above, for any caller-supplied period —
and returns `NaN` for both columns on a missing input rather than silently passing garbage through the trigonometric
functions.
