---
layout: default
title: Principal Component
parent: Other Model Types
grand_parent: Analytical Methodology
nav_order: 22
---

# Principal Component Analysis

Principal component analysis re-expresses correlated predictors as a smaller set of uncorrelated components, each one a
linear combination of the originals, ordered so the first component captures as much of the total variance as possible,
the second the most of what remains, and so on.

## R equivalent

```R
set.seed(1)
n <- 200
amount <- rnorm(n, mean = 50, sd = 15)
velocity <- rpois(n, 2)
chargebacks <- pmax(0, round(0.3 * velocity + rnorm(n, 0, 1)))
dispute_amount <- amount * 0.4 + rnorm(n, 0, 5)
cases <- data.frame(amount, velocity, chargebacks, dispute_amount)

model <- prcomp(cases, center = TRUE, scale. = TRUE)
print(summary(model))
print(model$rotation[, 1:2])
```

Console output:

```
Importance of components:
                         PC1    PC2    PC3     PC4
Standard deviation     1.337 1.1373 0.7914 0.54079
Proportion of Variance 0.447 0.3233 0.1566 0.07311
Cumulative Proportion  0.447 0.7703 0.9269 1.00000
                     PC1        PC2
amount         0.6422482  0.3008156
velocity       0.2891763 -0.6442941
chargebacks    0.2829960 -0.6486442
dispute_amount 0.6510051  0.2713958
```

**Tool equivalent:**
`Jube.Service.Repository.AnalyticsTrainingRun.AdvancedAnalyticsPrincipalComponentTrainingService` / tool
`SubmitPrincipalComponentTraining` — `Kind = DimensionReducer`, recall class `PrincipalComponentRecall`.
`Standardise` (default true) is `center = TRUE, scale. = TRUE` above; instead of the caller picking a component count up
front, `VarianceThreshold` (default 0.95) keeps just enough leading components to explain that share of total variance —
reading straight off the `Cumulative Proportion` row printed above, 0.95 would keep the first three components here —
with `MaximumComponents` as an optional hard cap on top of that rule. Recall for this family is a genuine
`double[] -> double[]` transform (a row of raw predictors in, a row of component scores out), unlike every clusterer in
this guide, which only recalls a cluster id or membership vector.

## Code audit

### Covariance or correlation matrix

`Jube.Engine/AdvancedAnalytics/DimensionReduction/PrincipalComponentTrainer.cs:142` constructs
`new Accord.Statistics.Analysis.PrincipalComponentAnalysis()` with no arguments. Accord's own parameterless constructor
defaults `Method` to `PrincipalComponentMethod.Center`
(`Accord.Statistics/Analysis/PrincipalComponentAnalysis.cs:171-177`), which only mean-centres the data before computing
its covariance matrix — it never asks Accord to divide by each column's standard deviation
(`PrincipalComponentMethod.Standardize` is never selected). Taken alone, this would make the analysis operate on the raw
covariance matrix, which is scale-sensitive.

However, the trainer pre-processes the design matrix itself before Accord ever sees it:
`Standardised()` (`PrincipalComponentTrainer.cs:122-133`) z-scores every column, using the train-split mean and standard
deviation, whenever `options.Standardise` is true — which is the default (`PrincipalComponentOptions.cs:21`). Only when
a caller explicitly sets `Standardise = false` does it fall back to `value - means[j]` (line 129), a mean-centre only,
leaving each variable's original scale in place. So the actual matrix Accord's `Center`-mode covariance calculation
operates on is, by default, the sample correlation matrix of the raw variables (every column already has unit variance
going in) — the product is effectively correlation-matrix PCA by default, and only becomes true, scale-sensitive
covariance-matrix PCA if `Standardise` is turned off. This distinction is real and does change results when variables
have different scales, exactly as the task description anticipates; the trainer resolves it upstream of Accord rather
than through Accord's own `Method` switch.

### Components retained by default

`VarianceThreshold` defaults to 0.95 (`PrincipalComponentOptions.cs:23`). The trainer reads
`analysis.ComponentProportions` (Accord's per-component explained-variance share), builds a running cumulative sum
(`PrincipalComponentTrainer.cs:176-183`), and retains the smallest component count whose cumulative proportion first
reaches or exceeds the threshold (lines 185-192) — clamped to at least 1 and at most the number of components Accord
actually produced, and optionally capped further by
`MaximumComponents` (default unset, i.e. no cap, `PrincipalComponentOptions.cs:25`, applied at
`PrincipalComponentTrainer.cs:194-197`). If that cap forces fewer components than the variance threshold would otherwise
need, a warning records the shortfall (`PrincipalComponentTrainer.cs:199-201`).

### Other findings

* Every eigenvector's sign is normalised so its largest-magnitude loading is positive
  (`PrincipalComponentTrainer.cs:159-174`) — a deliberate post-processing step the trainer performs and Accord does not;
  Accord's own documentation for this class even remarks that users often ask "why my matrices have inverted signs"
  (`Accord.Statistics/Analysis/PrincipalComponentAnalysis.cs:69-73`), which is precisely the ambiguity this sign-flip
  resolves for stored models.
* Held-out quality is a hand-computed RMS reconstruction error (`ReconstructionError`,
  `PrincipalComponentTrainer.cs:210-244`): each row is projected onto the retained components and reconstructed, and the
  root-mean-square residual per row is averaged over the split. This diagnostic is not something Accord exposes
  directly; it is computed entirely in the training service.
* Missing values are median-imputed per variable on the training split, and constant or near-constant columns (one value
  in at least 99.5% of training rows) are flagged with a warning but not excluded from the analysis
  (`PrincipalComponentTrainer.cs:100-118`), unlike Mahalanobis and Gaussian mixture density, which both drop
  exactly-constant columns from their active variable set.
* At least two training rows are required (`PrincipalComponentTrainer.cs:78-79,343-344`), and a warning (not a hard
  failure) is raised if the training split has no more rows than variables, since fewer components are then identifiable
  than variables (`PrincipalComponentTrainer.cs:203-205`).
