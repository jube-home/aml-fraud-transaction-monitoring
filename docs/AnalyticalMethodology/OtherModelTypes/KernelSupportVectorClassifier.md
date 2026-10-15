---
layout: default
title: Kernel Support Vector Classifier
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 8
---

# Kernel Support Vector Classifier

Where the linear machine draws a straight boundary, a kernel support vector machine reshapes the notion of similarity
between rows first (here, a Gaussian/RBF kernel) so the effective boundary can bend around clusters a straight line
never could.

## R equivalent

```R
library(e1071)

set.seed(1)
n <- 200
amount <- c(rnorm(n / 2, mean = 40, sd = 15), rnorm(n / 2, mean = 220, sd = 60))
velocity <- c(rpois(n / 2, 2), rpois(n / 2, 9))
fraud <- factor(c(rep(0, n / 2), rep(1, n / 2)))
train <- data.frame(amount, velocity, fraud)

model <- svm(fraud ~ amount + velocity, data = train, kernel = "radial",
             cost = 1, gamma = 0.5, scale = TRUE, probability = TRUE)
print(model)

new_cases <- data.frame(amount = c(35, 260), velocity = c(1, 10))
pred <- predict(model, new_cases, probability = TRUE)
attr(pred, "probabilities")
```

Console output:

```

Call:
svm(formula = fraud ~ amount + velocity, data = train, kernel = "radial", 
    cost = 1, gamma = 0.5, probability = TRUE, scale = TRUE)


Parameters:
   SVM-Type:  C-classification 
 SVM-Kernel:  radial 
       cost:  1 

Number of Support Vectors:  13

           0           1
1 0.99248528 0.007514724
2 0.00597773 0.994022270
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsKernelSupportVectorClassifierTrainingService` / tool
`SubmitKernelSupportVectorClassifierTraining` — `Kind = Classifier`, recall class `KernelSupportVectorClassifierRecall`.
`Complexity` is `cost`; `Kernel` (`KernelSupportVectorKernel`, default `Gaussian`) picks the kernel family (the R run
above uses `"radial"`, `e1071`'s name for the same Gaussian kernel), with `Gamma` and `Degree`
feeding it. Because the fitted boundary keeps only the support vectors and their weights, the background worker stores
those rows, their weights and the kernel's own parameters inside the model version — recall means weighting the kernel
similarity of a new row against every stored support vector, unlike the linear machine's fixed-size weight vector. The
raw decision value is again Platt-calibrated (`Calibrate`,
`DecisionThreshold`).

## Code audit

`AdvancedAnalyticsKernelSupportVectorClassifierTrainingService` delegates to
`KernelSupportVectorClassifierTrainer.TrainWithSplit`
(`Jube.Engine/AdvancedAnalytics/Classification/KernelSupportVectorClassifierTrainer.cs:62-318`), which instantiates
`Accord.MachineLearning.VectorMachines.Learning.SequentialMinimalOptimization<IKernel<double[]>>`
(lines 19, 147-152).

- **Solver:** genuine Sequential Minimal Optimization — the class's own remarks state it "follows the original algorithm
  by Platt with additional modifications by Keerthi et al."
  (`Accord.MachineLearning/VectorMachines/Learning/SequentialMinimalOptimization.cs:70-72`). The trainer sets only
  `Complexity`, `Kernel` and `Token` (`KernelSupportVectorClassifierTrainer.cs:147-152`); every other parameter is left
  at the library default: `Tolerance = 1e-2`, `Epsilon = 1e-6`,
  `Strategy = SelectionStrategy.WorstPair` (Keerthi's algorithm 2: "always select the worst violation pair to be
  optimized first"), `Shrinking = false`, `CacheSize = -1` — which the class's own doc comment defines as
  "the same number of input vectors, meaning the entire kernel matrix will be computed and cached in memory"
  (`SequentialMinimalOptimization.cs:430-432, 451-454, 522-524, 552-559`).
- **Kernel functions exposed versus advertised:** `BuildKernel`
  (`KernelSupportVectorClassifierTrainer.cs:320-329`) switches on the DTO's `KernelSupportVectorKernel` enum and
  instantiates the vendored `Accord.Statistics.Kernels.Gaussian`, `Polynomial`, `Sigmoid` or `Laplacian`
  kernel classes. Every enum value the DTO defines — Gaussian=0, Polynomial=1, Sigmoid=2, Laplacian=3
  (`KernelSupportVectorClassifierOptions.cs:19-25`) — is handled explicitly by name, with Gaussian doubling as the
  `_ =>` default; there is no advertised kernel that silently falls through to a different one. `Gamma` defaults to
  `1 / variableCount` when the DTO leaves it at 0 (line 144).
- **Recall does not call back into the vendored library:** the trainer copies `machine.SupportVectors`,
  `machine.Weights` and `machine.Threshold` out of the fitted machine once (lines 159-162) into
  `KernelSupportVectorClassifierPipeline`, which recomputes the kernel value itself in its own `KernelValue`
  switch (`Jube.Engine/AdvancedAnalytics/Classification/KernelSupportVectorClassifierPipeline.cs:112-152`) — a second,
  independent implementation of the same four kernel formulas (Gaussian `exp(-γ‖x-y‖²)`, Polynomial
  `(x·y+1)^d`, Sigmoid `tanh(γ x·y)`, Laplacian `exp(-‖x-y‖/σ)` with `σ = sqrt(1/(2γ))`), used only at recall, rather
  than reusing the kernel class's own `Function` method at scoring time.
- **Probability/calibration:** the signed decision value (`bias + Σ coefficient · kernel(x, supportVector)`,
  `KernelSupportVectorClassifierPipeline.cs:92-98`) is Platt-calibrated by `Jube.Analytics.PlattCalibration.Fit` on the
  validation split (`KernelSupportVectorClassifierTrainer.cs:196`) — the same custom calibrator as every other
  classifier family in this section. The vendored
  `Accord.MachineLearning.VectorMachines.Learning.Probabilistic.ProbabilisticOutputCalibration`
  (`Accord.MachineLearning/VectorMachines/Learning/Probabilistic/ProbabilisticOutputCalibration.cs`), a genuine
  Platt-scaling implementation, is never called by this family.
- **Dense-support-vector diagnostic:** if 90% or more of the training rows become support vectors, the trainer records a
  warning that the classes barely separate under the chosen kernel and complexity (`DenseSupportVectorShare = 0.9`,
  lines 35, 255-257) — a genuine data-driven check that has no counterpart in the linear family, which never selects a
  subset of rows as support vectors in the first place.
- **Snapshot cost:** every support vector, its coefficient, and the kernel parameters are persisted
  (`AdvancedAnalyticsKernelSupportVectorClassifierPipelineSnapshot`, lines 266-287); recall weights the kernel
  similarity of a new row against every stored support vector, so both snapshot size and recall cost scale with the
  number of support vectors kept, not with the number of raw predictors — the trainer's own warning above 200,000
  support vectors (`LargeSnapshotWarningThreshold`, lines 36, 259-264) states this directly.
