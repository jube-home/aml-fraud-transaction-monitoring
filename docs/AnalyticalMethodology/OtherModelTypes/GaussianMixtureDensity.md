---
layout: default
title: Gaussian Mixture Density
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 21
---

# Gaussian Mixture Density Anomaly Scoring

A Gaussian mixture models the data as a weighted sum of several Gaussian components; read as a density rather than as
clusters, the fitted mixture gives every row a likelihood under the model, and a row with unusually low likelihood is
flagged as anomalous.

## R equivalent

*Package installed beyond the shared set: `mclust`.*

```R
library(mclust)

set.seed(1)
n <- 200
amount <- rnorm(n, mean = 50, sd = 15)
velocity <- rpois(n, 2)
normal <- scale(data.frame(amount, velocity))

model <- densityMclust(normal, G = 3, plot = FALSE)
print(summary(model))

new_cases <- data.frame(amount = c(48, 900), velocity = c(2, 40))
new_scaled <- scale(new_cases, center = attr(normal, "scaled:center"),
                     scale = attr(normal, "scaled:scale"))
predict(model, newdata = new_scaled, what = "dens")
```

Console output:

```
Package 'mclust' version 6.1.3
Type 'citation("mclust")' for citing this R package in publications.
------------------------------------------------------- 
Density estimation via Gaussian finite mixture modeling 
------------------------------------------------------- 

Mclust EII (spherical, equal volume) model with 3 components: 

 log-likelihood   n df       BIC       ICL
      -561.0504 200  9 -1169.786 -1245.712
[1] 0.1748917 0.0000000
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsGaussianMixtureDensityTrainingService` / tool
`SubmitGaussianMixtureDensityTraining` — `Kind = DensityEstimator`, recall class `GaussianMixtureDensityRecall`.
`Components`, `Standardise`, `MaxIterations` and `Tolerance` match the Gaussian mixture clusterer's DTO; the extra
`RegularizationEpsilon` (default 1e-6) is the same covariance ridge idea as Mahalanobis's `RidgeEpsilon`, needed here
because a density read of a mixture is far more sensitive to a near-singular component covariance blowing up the
likelihood than a clustering read is. This family is always unsupervised — there is no `Labels` field on the DTO at all,
unlike isolation forest and one-class SVM above, which both accept optional labels purely for calibration.

## Code audit

### Same fitting machinery as the clustering family, genuinely repurposed

`Jube.Engine/AdvancedAnalytics/Density/GaussianMixtureDensityTrainer.cs:166-200` and
`Jube.Engine/AdvancedAnalytics/Clustering/GaussianMixtureClustererTrainer.cs:151-183` both construct
`new Accord.MachineLearning.GaussianMixtureModel(options.Components)` with an identical options shape —
`Options = new NormalOptions { Regularization = ... }`, `MaxIterations`, `Tolerance`,
`ComputeLogLikelihood = true` — and fit it with `.Learn(trainDesign)` against a `GaussianClusterCollection`. This is the
same Accord.NET class and the same EM fitting call in both families; the two training services are separate,
independently-maintained C# files (the code is duplicated, not shared through a common helper), but the underlying
algorithm and its parameterisation are identical. What differs is what happens to the fit afterwards: the clustering
family keeps Accord's own `GaussianClusterCollection` and reads cluster assignment and its own
`mixtureModel.LogLikelihood` off it directly, while the density family discards Accord's own scoring path after
extracting each component's weight, mean and covariance (`GaussianMixtureDensityTrainer.cs:208-260`) and rebuilds its
own scorer from scratch: each component's inverse covariance and log-determinant are computed via the same
`SmallLinearAlgebra.Cholesky` module Mahalanobis uses (`GaussianMixtureDensityTrainer.cs:217-260`), and
`Jube.Engine/AdvancedAnalytics/Density/GaussianMixtureDensityPipeline.cs:76-109` re-derives the multivariate normal
log-density itself — `ComponentLogPdf` computes `-0.5 * (d * ln(2π) + logDet + quadratic)` per component, and
`LogDensity` combines the components via a numerically stable log-sum-exp of
`log(weight_k) + ComponentLogPdf_k`. So this is genuinely a density-estimation use of the identical mixture machinery,
not literally the same code path re-labelled — the fitting call is duplicated verbatim, but the scoring code is
independently written and never calls back into Accord.

### What the anomaly score actually is

`GaussianMixtureDensityRecall.Predict` (`GaussianMixtureDensityRecall.cs:69-74`) returns
`_pipeline.LogDensity(input)` directly: the raw natural-log density of the fitted mixture at that row. It is **not
negated** — this is `log p(x)`, not `-log p(x)`, and there is no calibration step anywhere in this family
(`GaussianMixtureDensityOptions.cs` has no `Calibrate` field, and no `Calibrator`/Platt-scaling call appears in
`GaussianMixtureDensityTrainer.cs`). A lower (more negative) value means a less likely, more anomalous row; there is no
fixed cutoff shipped with the model — only the train/validation/test mean log-likelihood plus BIC and AIC
(`GaussianMixtureDensityTrainer.cs:282-297,311-323`) as diagnostics for whether the fit itself is trustworthy, not as a
score threshold.

### Regularisation is layered, not single-mechanism

Two independent regularisation paths exist. First, Accord's own EM fit is handed
`NormalOptions { Regularization = options.RegularizationEpsilon }`
(`GaussianMixtureDensityTrainer.cs:176`) — Accord's own internal safeguard against singular component covariances during
iteration. Second, after fitting, Jube separately attempts to invert each component's covariance itself; only if that
inversion fails does it add
`options.RegularizationEpsilon * averageVariance` to the component's diagonal and retry
(`GaussianMixtureDensityTrainer.cs:218-244`), the same conditional-ridge-on-singularity pattern as Mahalanobis, using
the same `RegularizationEpsilon` default of 1e-6 (`GaussianMixtureDensityOptions.cs:29`). A warning fires if any
component needed this second regularisation (`GaussianMixtureDensityTrainer.cs:263-267`).

### Defaults and guards

`Components` defaults to 3 (`GaussianMixtureDensityOptions.cs:21`), `MaxIterations` to 100 and `Tolerance` to 1e-5
(lines 25, 27). Training requires at least `Components * (d + 1)` rows, where `d` is the active variable count
(`GaussianMixtureDensityTrainer.cs:149-155`), and warns if there are fewer than
`MinimumRowsPerComponentRatio * (d + 1)` rows per component (default ratio 5,
`GaussianMixtureDensityTrainer.cs:34,157-162`). Non-convergence — the EM loop consuming its full iteration budget — is
reported explicitly (`converged = MaxIterations <= 0 || iterations < MaxIterations`,
`GaussianMixtureDensityTrainer.cs:202-206`).
