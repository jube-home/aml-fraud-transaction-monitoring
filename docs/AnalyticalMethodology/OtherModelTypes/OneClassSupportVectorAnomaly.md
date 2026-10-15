---
layout: default
title: One-Class Support Vector Anomaly
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 19
---

# One-Class Support Vector Anomaly Detection

A one-class support vector machine fits a boundary in a reshaped, kernel-induced space that wraps as tightly as its `Nu`
parameter allows around the bulk of the rows, having only ever seen "normal" data — unlike a two-class SVM, it is never
shown an anomalous example to separate from.

## R equivalent

```R
library(e1071)

set.seed(1)
n <- 200
amount <- rnorm(n, mean = 50, sd = 15)
velocity <- rpois(n, 2)
normal <- data.frame(amount, velocity)

model <- svm(normal, y = NULL, type = "one-classification", kernel = "radial",
             nu = 0.1, gamma = 0.5, scale = TRUE)
print(model)

new_cases <- data.frame(amount = c(48, 900), velocity = c(2, 40))
predict(model, new_cases, decision.values = TRUE)
```

Console output:

```

Call:
svm.default(x = normal, y = NULL, scale = TRUE, type = "one-classification", 
    kernel = "radial", gamma = 0.5, nu = 0.1)


Parameters:
   SVM-Type:  one-classification 
 SVM-Kernel:  radial 
      gamma:  0.5 
         nu:  0.1 

Number of Support Vectors:  30

    1     2 
FALSE FALSE 
attr(,"decision.values")
              /
1 -0.0003650763
2 -3.2553941825
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService` / tool
`SubmitOneClassSupportVectorAnomalyTraining` — `Kind = AnomalyDetector`, recall class
`OneClassSupportVectorAnomalyRecall`.
`Nu` and `Gamma` are `nu` and `gamma` above (the kernel is fixed to Gaussian/radial for this family, unlike the
two-class kernel SVM's selectable `Kernel` enum). Labels are optional and never used to draw the boundary, exactly as
`y = NULL` above; when given and both classes appear in the validation split they only Platt-calibrate the raw score
(`Calibrate`) and report held-out quality, never reshape the boundary itself. Note both toy cases above returned `FALSE`
(inside the boundary) despite the second row's amount being wildly larger than anything trained on — a reminder that a
tightly-fit one-class boundary in a high-`gamma` reshaped space can behave counter-intuitively on a single unscaled
outlier, which is exactly the kind of evidence this family's held-out metrics exist to surface before a model is
promoted.

## Code audit

`Jube.Engine/AdvancedAnalytics/Anomaly/OneClassSupportVectorAnomalyTrainer.cs` builds the design matrix (median
imputation plus train-split z-scoring, `OneClassSupportVectorAnomalyTrainer.cs:91-131`) and then calls a real Accord
class:
`Accord.MachineLearning.VectorMachines.Learning.OneclassSupportVectorLearning<IKernel<double[]>>`
(`OneClassSupportVectorAnomalyTrainer.cs:139-146`), with the kernel fixed to
`Gaussian.FromGamma(gamma)` (line 141) — this family never exposes a kernel choice, unlike the two-class kernel SVM
family.

### Formulation

Accord's `BaseOneclassSupportVectorLearning<TModel, TKernel, TInput>.Learn`
(`Accord.MachineLearning/VectorMachines/Learning/OneclassSupportVectorLearning.cs:315-447`) is a direct implementation
of Schölkopf's ν-SVM one-class boundary, the same one LIBSVM ships (the class's own doc comment cites "Chang and Lin,
LIBSVM: A Library for Support Vector Machines, 2013" at lines 286-289). The initial Lagrange multipliers are built from
`n = (int)(nu * l)` (`OneclassSupportVectorLearning.cs:363`): the first `n` rows get `alpha = 1`, one row gets the
fractional remainder `nu * l - n`, and the rest start at 0 (lines 365-378); the quadratic program is then solved by a
Fan-Chen-Lin sequential-minimal-optimisation-style solver under the box constraint `0 <= alpha <= 1` with the equality
constraint enforced through the `ones`
upper-bound vector (`OneclassSupportVectorLearning.cs:358-397`), and the bias term is set from the solver's dual
variable, `Model.Threshold = -s.Rho` (line 422). This is Schölkopf's boundary formulation, not the older Tax & Duin
support vector data description (SVDD): the class's own remark that `Nu` is "an upper bound on the fraction of training
errors and a lower bound of the fraction of support vectors"
(`OneclassSupportVectorLearning.cs:280-284`) is Schölkopf's own characterisation of ν, stated verbatim.

### Nu, Gamma and the outlier-fraction mapping

Accord's own class defaults `nu` to 0.5 internally (`OneclassSupportVectorLearning.cs:162`), but
`OneClassSupportVectorAnomalyTrainer.cs:142` always sets `Nu = options.Nu` explicitly, and
`OneClassSupportVectorAnomalyOptions.cs:21` defaults that to 0.1. There is no separate "expected outlier fraction" field
on the DTO: `Nu` **is** that fraction, in the Schölkopf sense above — at the default 0.1, the fit is asymptotically
guaranteed to treat at most about 10% of the training rows as margin errors and at least about 10% as support vectors.
`Gamma` defaults to 0 on the options record (`OneClassSupportVectorAnomalyOptions.cs:22`), and the trainer maps that to
`1 / variableCount`
(`OneClassSupportVectorAnomalyTrainer.cs:137`) — the classic "one over the number of features" rule of thumb, not a
variance-based autoscale rule; a caller who wants the latter has to compute and pass it themselves.

### Score sign and calibration

Accord's raw decision value is `bias + Σ alpha_i * K(x, sv_i)`
(`OneClassSupportVectorAnomalyPipeline.cs:82-90`), positive for rows the boundary treats as normal. Jube's own pipeline
negates it, `Score(row) = -RawDecision(prepared)` (`OneClassSupportVectorAnomalyPipeline.cs:92-96`), so that —
consistently with the other anomaly families in this section — a **higher** score means more anomalous, the opposite
convention to Accord's own `SupportVectorMachine.Decide`. When labels are supplied and both classes appear in the
validation split, that negated score is Platt-calibrated into a probability
(`OneClassSupportVectorAnomalyTrainer.cs:192-207`); otherwise the raw negated decision value is reported as is
(`OneClassSupportVectorAnomalyTrainer.cs:175-183`).

### Other findings

* The fitted machine's support vectors, their weights and the kernel's `Gamma` are the entire stored model
  (`OneClassSupportVectorAnomalyTrainer.cs:261-280`); there is no compact parametric form, so a warning is raised if the
  support vector set is dense (at least 90% of training rows,
  `OneClassSupportVectorAnomalyTrainer.cs:236-238`) or the snapshot is large (over 200,000 support vectors, lines
  240-245).
* `trainX.Length <= variableCount` is rejected outright (`OneClassSupportVectorAnomalyTrainer.cs:83-84,
  377-379`) — there must be more rows than variables before training is attempted at all.
