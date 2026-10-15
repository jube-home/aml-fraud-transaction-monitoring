---
layout: default
title: Linear Support Vector Classifier
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 7
---

# Linear Support Vector Classifier

A linear support vector machine fits the maximum-margin straight boundary between the two classes — the widest possible
corridor separating them, rather than logistic regression's best-fit log-odds line.

## R equivalent

```R
library(e1071)

set.seed(1)
n <- 200
amount <- c(rnorm(n / 2, mean = 40, sd = 15), rnorm(n / 2, mean = 220, sd = 60))
velocity <- c(rpois(n / 2, 2), rpois(n / 2, 9))
fraud <- factor(c(rep(0, n / 2), rep(1, n / 2)))
train <- data.frame(amount, velocity, fraud)

model <- svm(fraud ~ amount + velocity, data = train, kernel = "linear",
             cost = 1, scale = TRUE, probability = TRUE)
print(model)

new_cases <- data.frame(amount = c(35, 260), velocity = c(1, 10))
pred <- predict(model, new_cases, probability = TRUE)
attr(pred, "probabilities")
```

Console output:

```

Call:
svm(formula = fraud ~ amount + velocity, data = train, kernel = "linear", 
    cost = 1, probability = TRUE, scale = TRUE)


Parameters:
   SVM-Type:  C-classification 
 SVM-Kernel:  linear 
       cost:  1 

Number of Support Vectors:  8

             0           1
1 0.9945590041 0.005440996
2 0.0001001911 0.999899809
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsLinearSupportVectorClassifierTrainingService` / tool
`SubmitLinearSupportVectorClassifierTraining` — `Kind = Classifier`, recall class `LinearSupportVectorClassifierRecall`.
`Complexity` (default 1.0) is `cost` above; `Standardise` (default true) is `scale = TRUE`. The background worker fits
the boundary with a dual coordinate-descent solver rather than `e1071`'s SMO, then extracts a plain weight vector and
bias — recall is a single dot product, not a stored-support-vector search, so the snapshot is as small as logistic
regression's. The raw signed distance to the boundary is not a probability and is Platt-calibrated (`Calibrate`,
`DecisionThreshold`) exactly as `e1071`'s own `probability = TRUE` does internally above.

## Code audit

`AdvancedAnalyticsLinearSupportVectorClassifierTrainingService` delegates to
`LinearSupportVectorClassifierTrainer.TrainWithSplit`
(`Jube.Engine/AdvancedAnalytics/Classification/LinearSupportVectorClassifierTrainer.cs:62-315`), which instantiates
`Accord.MachineLearning.VectorMachines.Learning.LinearDualCoordinateDescent` (lines 19, 152-156).

- **Solver:** `LinearDualCoordinateDescent`
  (`Accord.MachineLearning/VectorMachines/Learning/LinearDualCoordinateDescent.cs:157-187`) is a coordinate descent
  algorithm ported from LIBLINEAR's `solve_l2r_l1l2_svc` (class remarks, lines 97-99, 103-105, citing Hsieh et al., ICML
  2008, Algorithm 3) — **not** Sequential Minimal Optimization, unlike the kernel SVM family that sits beside it in the
  same training-service group. Its defaults, none overridden by the trainer: `Loss
  = Loss.L2` (squared hinge loss, the default field value, `BaseLinearDualCoordinateDescent` field `loss =
  Loss.L2` at line 365), `Tolerance = 0.1` (line 359), `max_iter = 1000` (line 357). The trainer sets only
  `Complexity` and `Token` (`LinearSupportVectorClassifierTrainer.cs:152-156`).
- **Kernel:** fixed to `Linear` by construction — `LinearDualCoordinateDescent.Create` always builds a
  `SupportVectorMachine(inputs) { Kernel = kernel }` with the library's `Linear` kernel type
  (`LinearDualCoordinateDescent.cs:183-186`); there is no kernel option on this DTO to diverge from, unlike the kernel
  SVM and kernel ridge families.
- **Weight/bias extraction:** `machine.SupportVectors[0]` is cloned as the weight vector and
  `machine.Threshold` as the bias (`LinearSupportVectorClassifierTrainer.cs:178-179`). For this solver the fitted model
  has exactly one "support vector" entry, which is really the dense weight vector itself
  (`Model.SupportVectors = new[] { kernel.CreateVector(w) }`, `BaseLinearDualCoordinateDescent.InnerRun`,
  `LinearDualCoordinateDescent.cs:454-456`) — not a set of retained training rows. This confirms the existing mapping's
  claim that recall is a single dot product, not a stored-vector search.
- **Reported support vector count is separate from the stored weight vector:** `SupportVectorCount`
  (`LinearSupportVectorClassifierTrainer.cs:180`) is computed from the solver's own Lagrange multipliers
  (`learner.Lagrange.Count(a => a > LagrangeMultiplierFloor)`, floor `1e-9`) — a genuine per-row sparsity count from the
  dual solution, decoupled from the single weight vector actually persisted.
- **Divergence guard:** the trainer checks every weight and the bias for finiteness and throws before returning a model
  if any are non-finite (lines 182-185) — a defence against the coordinate descent solver diverging on separable data at
  too high a `Complexity`; `LinearDualCoordinateDescent` itself has no such guard of its own.
- **Probability:** the raw decision value `w·x + bias` is Platt-calibrated by `Jube.Analytics.PlattCalibration.Fit`
  on the validation split (`LinearSupportVectorClassifierTrainer.cs:212`) — the same custom calibrator used by every
  classifier family in this section, not the vendored
  `Accord.MachineLearning.VectorMachines.Learning.Probabilistic.ProbabilisticOutputCalibration`.
- **Seeding:** `Accord.Math.Random.Generator.ThreadSeed` is set to `options.Seed` for the duration of
  `learner.Learn` and restored in a `finally` block (lines 159-174). The solver's internal `random` field
  (`BaseLinearDualCoordinateDescent.InnerRun`, `LinearDualCoordinateDescent.cs:437`, passed into its
  `DualSvmSolver`) reads from `Accord.Math.Random.Generator`'s thread-static instance, so this is a real, load-bearing
  seeding mechanism for reproducing a fit, not a decorative option.
- **Class imbalance is unweighted:** the trainer warns when the training split is imbalanced beyond 100:1
  (`SevereImbalanceRatio`, lines 35, 260-266) because the solver is not given per-class weights (`Cp`/`Cn` in the
  class's own dual formulation remarks) — both classes share the same upper bound derived from
  `Complexity`.
