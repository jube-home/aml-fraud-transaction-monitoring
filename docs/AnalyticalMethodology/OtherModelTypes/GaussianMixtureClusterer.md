---
layout: default
title: Gaussian Mixture Clusterer
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 16
---

# Gaussian Mixture Clusterer

## Procedure: Gaussian mixture clusterer

A Gaussian mixture models the data as a weighted sum of several Gaussian components and assigns each row a soft
(probabilistic) membership across all of them, rather than k-means's hard, equal-variance, spherical-cluster assumption.

*Package installed beyond the shared set: `mclust`.*

```R
library(mclust)

set.seed(1)
n <- 90
x <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 40, 2), rnorm(n / 3, 70, 2))
y <- c(rnorm(n / 3, 10, 2), rnorm(n / 3, 45, 2), rnorm(n / 3, 15, 2))
merchants <- scale(data.frame(x, y))

model <- Mclust(merchants, G = 3)
print(summary(model))
print(model$parameters$mean)
```

Console output:

```
Package 'mclust' version 6.1.3
Type 'citation("mclust")' for citing this R package in publications.
---------------------------------------------------- 
Gaussian finite mixture model fitted by EM algorithm 
---------------------------------------------------- 

Mclust EEI (diagonal, equal volume and shape) model with 3 components: 

 log-likelihood  n df      BIC      ICL
       71.48328 90 10 97.96846 97.96846

Clustering table:
 1  2  3 
30 30 30 
        [,1]        [,2]       [,3]
x -1.2157694 0.001963785  1.2138056
y -0.8631565 1.381099205 -0.5179427
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsGaussianMixtureClustererTrainingService` / tool
`SubmitGaussianMixtureClustererTraining` — `Kind = Clusterer`, recall class `GaussianMixtureClustererRecall`.
`Components` is `G`; `DiagonalCovariance` picks between a full and a diagonal covariance matrix per component (`mclust`
chose `"EEI"`, diagonal, above on its own model-selection pass, since the DTO's equivalent is a direct flag, not a
search); `Regularization` is the small ridge added to each component's covariance to keep it invertible;
`MaximumIterations`/`Tolerance` bound the EM loop. This same underlying mixture-fitting code is shared with the Gaussian
mixture density family (`AdvancedAnalyticsGaussianMixtureDensityTrainingService`), the difference being whether the
fitted mixture is read as hard-ish cluster membership (`Kind = Clusterer`)
or as a density/likelihood surface for anomaly scoring (`Kind = DensityEstimator`).

## Code audit

`SubmitTrainingAsync`
(`Jube.Service/Repository/AnalyticsTrainingRun/AdvancedAnalyticsGaussianMixtureClustererTrainingService.cs`)
queues the run; `AdvancedAnalyticsTrainingWorker` constructs a `GaussianMixtureClustererTrainingExecutor`
(`Jube.Engine/AdvancedAnalytics/Training/AdvancedAnalyticsTrainingWorker.cs:563-564`) and calls its
`ExecuteAsync` (line 573), which runs `GaussianMixtureClustererTrainer.TrainWithSplit`/`Train`
(`Jube.Engine/AdvancedAnalytics/Clustering/GaussianMixtureClustererTrainer.cs:152-195`). That trainer instantiates
`Accord.MachineLearning.GaussianMixtureModel`
(`Accord.MachineLearning/Clustering/Gaussian/GaussianMixtureModel.cs`), which internally fits a
`MultivariateMixture<MultivariateNormalDistribution>`
(`Accord.Statistics/Distributions/Multivariate/MultivariateMixture`1.cs`).

### Covariance structure: full or diagonal are reachable; tied/shared is not, though Accord supports it

The trainer builds `Options = new NormalOptions { Diagonal = options.DiagonalCovariance, Regularization =
options.Regularization }` (`GaussianMixtureClustererTrainer.cs:157-161`) and never sets `NormalOptions.Shared`.
`NormalOptions` (`Accord.Statistics/Distributions/Fitting/NormalOptions.cs:46-73`) actually exposes three independent
knobs: `Diagonal` (diagonal covariance per component), `Shared` (a single pooled covariance matrix across every
component in the mixture — a genuine "tied" covariance mode), and `Regularization`. Only
`Diagonal` is exposed on `GaussianMixtureClustererOptions`
(`Jube.Engine/AdvancedAnalytics/Clustering/GaussianMixtureClustererOptions.cs:19-25`, default `false`), so this trained
family can fit **full** covariance (default) or **diagonal** covariance (when
`DiagonalCovariance = true`) per component, but the tied/shared-covariance mode that the underlying Accord library
supports (`MultivariateNormalDistribution.Fit`,
`Accord.Statistics/Distributions/Multivariate/Continuous/MultivariateNormalDistribution.cs:591-610`, which pools
covariances via `Measures.PooledCovariance` when `options.Shared` is set) is unreachable through this training service's
DTO. There is no third, "tied," choice in the product today even though the vendored library could support one.

### EM convergence: on relative log-likelihood change, not a fixed iteration count — but bounded by one

`GaussianMixtureModel.Learn` (`Accord.MachineLearning/Clustering/Gaussian/GaussianMixtureModel.cs:258-298`)
builds a `MixtureOptions` with `Threshold = Tolerance` and `MaxIterations = MaxIterations` (lines 266-273)
and calls `model.Fit(x, weights, mixtureOptions)`. Because `UseLogarithm` defaults to `true`
(`GaussianMixtureModel.cs:221`) and Jube's trainer never overrides it, `MultivariateMixture<T>.Fit`
(`` Accord.Statistics/Distributions/Multivariate/MultivariateMixture`1.cs:452-519 ``) takes the `log` branch and
constructs a `LogExpectationMaximization<double[]>` (lines 472-482) with `Convergence.MaxIterations =
options.MaxIterations` and `Convergence.Tolerance = options.Threshold`. The actual E/M loop
(`LogExpectationMaximization<TObservation>.compute`,
`Accord.Statistics/Distributions/Fitting/LogExpectationMaximization.cs:110-262`)
recomputes `Convergence.NewValue` from the mixture's log-likelihood after every E-step/M-step pair (line 239)
and loops `while (!Convergence.HasConverged)` (line 240). `RelativeConvergence.HasConverged`
(`Accord.Math/Convergence/RelativeConvergence.cs:232-258`) stops the loop the moment **either** condition holds:
`CurrentIteration >= MaxIterations` (line 246), or the log-likelihood's relative change is small enough —
`Delta <= tolerance * Math.Abs(OldValue)` (line 254). So convergence is genuinely tracked on relative log-likelihood
change, with `MaximumIterations` (default 100,
`GaussianMixtureClustererOptions.cs:27`) only as an upper bound, not the stopping rule itself.
`GaussianMixtureClustererTrainer.cs:199-200` reads back `model.Iterations` and reports `Converged = iterations
< options.MaximumIterations` as evidence, and adds a `NonConvergence` warning
(`GaussianMixtureClustererTrainer.cs:208-211`) when the iteration cap, not the tolerance, was what stopped the loop.

### Singular/degenerate covariance guard: ridge regularisation with a hard failure if it still isn't enough

`MultivariateNormalDistribution.Fit`/`decompose`
(`Accord.Statistics/Distributions/Multivariate/Continuous/MultivariateNormalDistribution.cs:619-689`) attempts a
`CholeskyDecomposition` of each component's covariance matrix. When `options.Regularization > 0`, and the matrix is not
yet positive definite, the code adds `regularization` to every diagonal entry and retries the Cholesky decomposition in
a `while (!chol.IsPositiveDefinite)` loop (lines 651-679) — the ridge is added repeatedly, once per failed attempt, not
just once. If the matrix is still not positive definite after that loop exits, or if `Regularization` is `0`,
`NonPositiveDefiniteMatrixException` is thrown (lines 632-637, 681-686). Jube's
`GaussianMixtureClustererOptions.Regularization` defaults to `1e-6`
(`GaussianMixtureClustererOptions.cs:24`), so the ridge is applied by default; the trainer additionally wraps the whole
`model.Learn` call in a `catch (Accord.NonPositiveDefiniteMatrixException ex)` and rethrows it as a plain
`ArgumentException` telling the caller to raise the regularisation, reduce the component count, or supply more rows
(`GaussianMixtureClustererTrainer.cs:185-190`) — so a degenerate fit surfaces as a training-run failure with an
actionable message rather than an unhandled internal exception.

### Initialisation bypasses the model's own default k-means-restart logic

`GaussianMixtureModel.Learn` only runs its own `Initialize(data, weights)` — which itself tries
`Initializations` (default 3, `GaussianMixtureModel.cs:219`) independent k-means restarts and keeps the lowest-error one
(`GaussianMixtureModel.cs:305-331`) — when `clusters.Model == null` (line 260-263). Jube's trainer pre-empts this: it
runs its own single `KMeans` pass with `UseSeeding = Seeding.KMeansPlusPlus` and
`ComputeCovariances = true` and calls `model.Initialize(seedingKMeans)` explicitly before `model.Learn`
(`GaussianMixtureClustererTrainer.cs:171-183`). By the time `Learn` runs, `clusters.Model` is already set, so the
multi-restart initialisation path inside `GaussianMixtureModel` never executes in this product — every Gaussian mixture
clusterer trained here starts from exactly one k-means++ seed, not the library's default best-of-three.
