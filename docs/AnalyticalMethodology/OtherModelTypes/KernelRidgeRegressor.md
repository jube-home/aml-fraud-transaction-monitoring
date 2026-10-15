---
layout: default
title: Kernel Ridge Regressor
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 9
---

# Kernel Ridge Regressor

Kernel ridge regression solves a single ridge-regularised linear system directly against the kernel similarity of every
pair of training rows, rather than fitting a compact coefficient vector by an iterative solver — a closed-form fit whose
"coefficients" are one weight per training row.

*Package installed beyond the shared set: `KRLS` (Kernel Regularized Least Squares, Hainmueller & Hazlett

2014) — `kernlab` has no exported kernel ridge regression function (only `kqr`, kernel quantile regression, and
      `gausspr`, a Gaussian process), so `KRLS` is the closest common CRAN package that actually names and fits kernel
      ridge regression.*

## R equivalent

```R
library(KRLS)

set.seed(1)
n <- 200
account_age_days <- runif(n, 1, 2000)
avg_monthly_txn <- runif(n, 1, 60)
settlement_days <- 12 - 0.004 * account_age_days + 0.05 * avg_monthly_txn +
  rnorm(n, 0, 1.5)

train_x <- scale(cbind(account_age_days, avg_monthly_txn))

model <- krls(X = train_x, y = settlement_days, print.level = 0)
summary(model)

new_cases <- rbind(c(30, 5), c(1500, 45))
new_x <- scale(new_cases, center = attr(train_x, "scaled:center"),
               scale = attr(train_x, "scaled:scale"))
predict(model, newdata = new_x)$fit
```

Console output:

```
## KRLS Package for Kernel-based Regularized Least Squares.

## See Hainmueller and Hazlett (2014) for details.

* *********************** *
Model Summary:

R2: 0.712118 

Average Marginal Effects:
                        Est Std. Error    t value     Pr(>|t|)
account_age_days -1.9494784  0.1236480 -15.766353 8.102014e-37
avg_monthly_txn   0.8320388  0.1426811   5.831458 2.206168e-08

Quartiles of Marginal Effects:
                        25%       50%       75%
account_age_days -2.2977211 -2.082849 -1.765872
avg_monthly_txn   0.2598935  0.825066  1.469536
          [,1]
[1,] 11.997814
[2,]  8.447582
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsKernelRidgeRegressorTrainingService` / tool
`SubmitKernelRidgeRegressorTraining` — `Kind = Regressor`, recall class `KernelRidgeRegressorRecall`. `Complexity`
is the ridge penalty, `Kernel` (`KernelRidgeRegressorKernel`, default `Gaussian`) with `Gamma`/`Degree` match the SVM
DTOs' shape. Every predictor is z-scored before the kernel ever sees it, same as the R snippet's
`scale()` call. Because the closed-form solution keeps a weight for every training row and — unlike a support vector
machine — has no sparsity mechanism to discard any of them during fitting, the stored model grows with the training
split's row count rather than a small fixed parameter count; the background worker stores every standardised training
row with its fitted weight and the kernel parameters. The regression target is never calibrated: what is recalled is the
fitted value on its original scale, exactly as `krls`'s
`predict()$fit` returns above.

## Code audit

`AdvancedAnalyticsKernelRidgeRegressorTrainingService` delegates to `KernelRidgeRegressorTrainer.TrainWithSplit`
(`Jube.Engine/AdvancedAnalytics/Regression/KernelRidgeRegressorTrainer.cs:59-269`). No
`Accord.MachineLearning.*` "kernel ridge regression" class exists in the vendored library or is called here; the fit is
a hand-written closed-form solve built only on `Accord.Math` primitives.

- **Construction:** the trainer builds the full n×n training Gram matrix from the chosen kernel
  (`kernel.Function(trainDesign[i], trainDesign[j])`, symmetric fill, lines 148-158) and adds a ridge penalty
  `lambda = 1 / Complexity` to every diagonal entry (lines 141, 157). This means the DTO's `Complexity` is the
  **inverse** of the textbook ridge parameter λ — larger `Complexity` means a smaller penalty and a system closer to
  unregularised kernel interpolation, the same direction "more complexity, less regularisation"
  reads in the SVM families, but arrived at by literal reciprocal division here rather than by an upper-bound argument
  on Lagrange multipliers as in the SVM solvers.
- **Target centring:** the training targets are centred on their own mean before the solve (`mean = trainY.Average()`,
  `centred = trainY - mean`, lines 162-163), and the mean is added back at predict time
  (`KernelRidgeRegressorPipeline.Predict`: `sum = _mean` then the kernel-weighted alpha terms are added,
  `Jube.Engine/AdvancedAnalytics/Regression/KernelRidgeRegressorPipeline.cs:92-98`). This is equivalent to fitting an
  intercept as the training mean and a mean-zero kernel expansion on top of it, rather than folding an explicit bias
  term into the n×n linear system itself.
- **Solve:** `Accord.Math.Decompositions.JaggedCholeskyDecomposition` is constructed over `(Gram + λI)` with its default
  `robust: false` mode (`KernelRidgeRegressorTrainer.cs:164`), i.e. the plain `LLt` factorisation
  (`Accord.Math/Decompositions/JaggedCholeskyDecomposition.cs:320-350`), whose positive-definiteness is judged by a
  per-pivot tolerance test, `s > 1e-14 * |L[j][j]|` (line 346). When `IsPositiveDefinite` comes back false the trainer
  does not trust that factorisation's own `Solve`; it falls back to
  `matrix.Solve(centred)` (`KernelRidgeRegressorTrainer.cs:172-174`), Accord.Math's generic square-matrix solve
  (`Accord.Math/Matrix/Matrix.Linear.Generated.cs:397-399`), which for a square input dispatches to
  `JaggedLuDecomposition` (`Matrix.Linear.Generated.cs:496-499`) — an LU solve with no positive-definiteness
  requirement. The trainer records a `NearSingularSystem` warning whenever this fallback path is taken (lines 174-178)
  rather than silently returning a potentially unstable answer.
- **Kernel functions:** `BuildKernel` (lines 271-281) exposes five kernels — Gaussian (default), Polynomial, Sigmoid,
  Laplacian and **Linear** (`KernelRidgeRegressorKernel.Linear = 4`,
  `Jube.Engine/AdvancedAnalytics/Regression/KernelRidgeRegressorOptions.cs:19-26`) — one more option than either
  kernel-based classifier family, since a Linear kernel here still yields a genuinely different (dual,
  one-weight-per-training-row) fit from the primal weight vector the Linear Support Vector Classifier produces. Every
  enum value is matched explicitly in the switch, with Gaussian as the `_ =>` default; there is no silent fallback for
  an advertised-but-unbuilt kernel.
- **No sparsity mechanism:** unlike the kernel support vector classifier, `alpha` (the dual weight vector)
  has exactly one entry per training row regardless of `Complexity` or kernel choice — the trainer's own warning above
  200,000 rows states this directly: "kernel ridge regression has no sparsity mechanism"
  (`LargeSnapshotWarningThreshold`, lines 33, 218-223).
- **No calibration:** `KernelRidgeRegressorOptions` carries no `Calibrate` field, and
  `KernelRidgeRegressorPipeline.Predict` returns the reconstructed value directly (lines 92-98) — consistent with every
  regressor family in this section, since there is nothing to calibrate for a continuous target.
