---
layout: default
title: Mahalanobis Anomaly
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 20
---

# Mahalanobis Distance Anomaly Detection

Mahalanobis distance measures how far a row sits from the training mean in units that already account for the
predictors' own covariance — a row that is unremarkable on every predictor alone can still have a large Mahalanobis
distance if its combination of values is unusual given how those predictors normally move together.

## R equivalent

```R
set.seed(1)
n <- 200
amount <- rnorm(n, mean = 50, sd = 15)
velocity <- rpois(n, 2)
normal <- data.frame(amount, velocity)

centre <- colMeans(normal)
covariance <- cov(normal)

new_cases <- data.frame(amount = c(48, 900), velocity = c(2, 40))
distances <- mahalanobis(new_cases, center = centre, cov = covariance)
print(distances)

p_values <- pchisq(distances, df = ncol(normal), lower.tail = FALSE)
print(p_values)
```

Console output:

```
[1] 3.417185e-02 4.165551e+03
[1] 0.9830592 0.0000000
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsMahalanobisAnomalyTrainingService` / tool
`SubmitMahalanobisAnomalyTraining` — `Kind = AnomalyDetector`, recall class `MahalanobisAnomalyRecall`. `Standardise`
optionally z-scores first; `RidgeEpsilon` (default 1e-6) is a small diagonal ridge added to the covariance matrix before
inverting it, and `HighConditionNumberThreshold` (default 1e6) flags a near-singular covariance as a training warning
rather than silently returning a numerically unstable distance — both concerns base R's plain `cov()`/`mahalanobis()`
pairing above leaves entirely to the caller. `Calibrate` turns the raw distance into a probability by Platt-scaling it
empirically against validation-split labels, not by referencing the chi-squared distribution the `pchisq()` tail above
uses by hand — see the code audit's "Chi-squared threshold: not used" note below for why the two are not equivalent.

## Code audit

`Jube.Engine/AdvancedAnalytics/Anomaly/MahalanobisAnomalyTrainer.cs` does not call
`Accord.Math.Distances.Mahalanobis`/`SquaredMahalanobis` or any other Accord class at all — a repository-wide search for
`Accord.Math.Distances` and `Distances.Mahalanobis` under `Jube.Engine/AdvancedAnalytics` finds no match. The mean
vector, covariance matrix, its inverse and the resulting squared distance are all computed by hand in the training
service (`MahalanobisAnomalyTrainer.cs:142-247`), using an internal Cholesky-based solver,
`Jube.Engine/AdvancedAnalytics/Regression/SmallLinearAlgebra.cs`, that this family shares with Gaussian Mixture
Density's per-component covariance handling.

### Covariance inversion and regularisation

Inversion is attempted first on the raw sample covariance via `SmallLinearAlgebra.Inverse`
(`MahalanobisAnomalyTrainer.cs:188`), which runs a Cholesky decomposition and treats the matrix as singular when a
diagonal pivot fails to exceed `max(scale, 1) * 1e-13` (`SmallLinearAlgebra.cs:29-34,44-47`, where
`scale` is the largest diagonal entry of the matrix being decomposed) — a numerical, not statistical, singularity test.
Only when that first attempt returns `null` does the trainer add a ridge of
`options.RidgeEpsilon * averageVariance` to the diagonal and retry (`MahalanobisAnomalyTrainer.cs:191-206`);
`RidgeEpsilon` defaults to 1e-6 (`MahalanobisAnomalyOptions.cs:26`) and `averageVariance` is the mean of the diagonal,
floored at `1e-12`
(`MahalanobisAnomalyTrainer.cs:179-184`). So the covariance is **conditionally** regularised — only on detected
near-singularity, not unconditionally shrunk (there is no Ledoit-Wolf-style shrinkage estimator here) — and a warning
(`MahalanobisWarningCodes.CovarianceRegularised`) is recorded whenever that fallback fires. If even the regularised
matrix cannot be inverted, training fails outright (`MahalanobisAnomalyTrainer.cs:200-202`).

Separately, the trainer computes a "condition number" as the ratio of the largest to smallest diagonal entry of the
covariance matrix (`MahalanobisAnomalyTrainer.cs:208-217`) and warns above
`HighConditionNumberThreshold` (default 1e6, `MahalanobisAnomalyOptions.cs:27`). The warning text produced at this point
states plainly that this ratio is "a cheap proxy for a true eigenvalue condition number"
(`MahalanobisAnomalyTrainer.cs:220-224`) — the code itself documents that this is not the real
largest-to-smallest-eigenvalue ratio, only a diagonal-only stand-in, and this diagonal check is independent of whether
the ridge fallback above actually fired.

### Chi-squared threshold: not used

There is no chi-squared-based outlier threshold anywhere in this family. `MahalanobisAnomalyPipeline.Explain`
returns the raw Euclidean-in-whitened-space distance (`Math.Sqrt(Math.Max(squared, 0d))`,
`MahalanobisAnomalyPipeline.cs:68-94`), and `MahalanobisAnomalyRecall.Predict`
(`MahalanobisAnomalyRecall.cs:88-95`) returns either that raw distance or, if a calibrator was fitted, a
Platt-calibrated probability — never a `pchisq`-style tail probability against the chi-squared distribution with `d`
degrees of freedom the way the R snippet above computes by hand. Calibration itself is fitted empirically against
validation-split labels when both classes are present (`MahalanobisAnomalyTrainer.cs:278-301`); when no labels are
supplied, the raw distance is reported completely uncalibrated, with no reference distribution assumed at all. A caller
relying on this family for a formal chi-squared cutoff would need to compute that themselves from the reported distance
and the active variable count.

### Standardisation and defaults

`Standardise` defaults to true (`MahalanobisAnomalyOptions.cs:24`) and governs whether the design matrix is z-scored
(train-split mean/sd) before the centre and covariance are computed (`MahalanobisAnomalyTrainer.cs:126-140`); constant
columns are excluded from the active variable set entirely (`MahalanobisAnomalyTrainer.cs:108-124`), and near-constant
columns (one value in at least 99.5% of training rows, `NearConstantShare`) are flagged but kept. A
minimum-rows-per-variable ratio of 5 (`MinimumRowsPerVariableRatio`, `MahalanobisAnomalyTrainer.cs:31,226-231`) produces
a warning, not a hard failure, when the training split is thin relative to the number of active variables.
