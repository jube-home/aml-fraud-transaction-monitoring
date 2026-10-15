---
layout: default
title: Other Model Types
parent: Analytical Methodology
nav_order: 17
has_children: true
---

# Other Model Types

`AdvancedAnalytics` trains thirty-one model families (`Jube.Service/Repository/AnalyticsTrainingRun/`, one
`AdvancedAnalytics*TrainingService.cs` per family); six are covered elsewhere in this guide (linear regression,
decision trees, gradient boosting, naive Bayes, tree-augmented naive Bayes, neural networks). Logistic regression is covered elsewhere too,
but reaches the shared, generic `AdvancedAnalyticsTrainingService` through its own gateway rather than a dedicated
per-family training service of its own (see [LogisticRegression.md](../LogisticRegression.md)), so it is not one of
these thirty-one. The remaining twenty-two each have their own
file in this section, grouped by kind below. Every file follows the same shape: the R snippet using the closest common
CRAN package, the real training service that replaces it, and a code audit of the actual implementation behind that
service (not just its DTO) — what algorithm it really runs, its real defaults, and any divergence from the textbook
method, cited to file and line in the reference worktree.

Every family shares the same submission shape: `SubmitTrainingAsync` on its training service queues a background run and
returns a run id at once, the caller polls `GetRunAsync` for status, and every trained version is immutable and never
made live by training itself — promotion is a separate step. That machinery is identical across all thirty-one families
and is not repeated per file.

## Ensembles and other classifiers

| File                                                                 | Covers                                                                             |
|----------------------------------------------------------------------|------------------------------------------------------------------------------------|
| [RandomForestClassifier.md](RandomForestClassifier.md)               | Bagged, feature-subsampled decision trees, voted by averaged leaf probability      |
| [RandomForestRegressor.md](RandomForestRegressor.md)                 | The regression sibling — SSE-split trees, averaged prediction                      |
| [BaggingClassifier.md](BaggingClassifier.md)                         | Bootstrap-aggregated trees with no feature subsampling                             |
| [AdaBoostClassifier.md](AdaBoostClassifier.md)                       | Discrete AdaBoost (AdaBoost.M1): reweighted rounds of a hard ±1 tree stump         |
| [KNearestNeighboursClassifier.md](KNearestNeighboursClassifier.md)   | Brute-force nearest-neighbour vote                                                 |
| [KNearestNeighboursRegressor.md](KNearestNeighboursRegressor.md)     | Unweighted mean of the k nearest neighbours' targets                               |
| [LinearSupportVectorClassifier.md](LinearSupportVectorClassifier.md) | LIBLINEAR-style dual coordinate descent, linear kernel only                        |
| [KernelSupportVectorClassifier.md](KernelSupportVectorClassifier.md) | Sequential minimal optimisation over Gaussian/polynomial/sigmoid/Laplacian kernels |
| [KernelRidgeRegressor.md](KernelRidgeRegressor.md)                   | Closed-form kernel ridge solve via Cholesky, one alpha per training row            |

## Clustering

| File                                                       | Covers                                                              |
|------------------------------------------------------------|---------------------------------------------------------------------|
| [KMeansClusterer.md](KMeansClusterer.md)                   | K-means++ seeding, squared Euclidean distance                       |
| [KMedoidsClusterer.md](KMedoidsClusterer.md)               | Real PAM build + swap, medoids instead of centroids                 |
| [KModesClusterer.md](KModesClusterer.md)                   | Huang's dissimilarity and per-attribute mode, for categorical rows  |
| [MiniBatchKMeansClusterer.md](MiniBatchKMeansClusterer.md) | Sculley's genuine mini-batch update, not k-means on a subsample     |
| [BalancedKMeansClusterer.md](BalancedKMeansClusterer.md)   | Hungarian-algorithm assignment, equal cluster sizes by construction |
| [BinarySplitClusterer.md](BinarySplitClusterer.md)         | Top-down bisecting k-means (Steinberg & Winkler)                    |
| [GaussianMixtureClusterer.md](GaussianMixtureClusterer.md) | EM-fitted Gaussian mixture, full or diagonal covariance             |
| [MeanShiftClusterer.md](MeanShiftClusterer.md)             | Mode-seeking via kernel-weighted mean shift, fixed bandwidth        |

## Anomaly detection

| File                                                               | Covers                                                              |
|--------------------------------------------------------------------|---------------------------------------------------------------------|
| [IsolationForestAnomaly.md](IsolationForestAnomaly.md)             | Path-length anomaly score, exact Liu/Ting/Zhou (2008) normalisation |
| [OneClassSupportVectorAnomaly.md](OneClassSupportVectorAnomaly.md) | Schölkopf's nu-SVM boundary around the training distribution        |
| [MahalanobisAnomaly.md](MahalanobisAnomaly.md)                     | Covariance-weighted distance from the training mean                 |
| [GaussianMixtureDensity.md](GaussianMixtureDensity.md)             | Log-density under a fitted Gaussian mixture, as an anomaly score    |

## Dimensionality reduction

| File                                           | Covers                                                                   |
|------------------------------------------------|--------------------------------------------------------------------------|
| [PrincipalComponent.md](PrincipalComponent.md) | Standardised (correlation-matrix) PCA by default, 95% variance retention |
