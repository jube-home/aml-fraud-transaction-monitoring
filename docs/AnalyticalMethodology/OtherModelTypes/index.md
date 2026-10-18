---
layout: default
title: Other Model Types
nav_order: 20
parent: Analytical Methodology
has_children: true
---

# Other Model Types

`AdvancedAnalytics` trains thirty-one model families (`Jube.Service/Repository/AnalyticsTrainingRun/`, one
`AdvancedAnalytics*TrainingService.cs` per family); six are covered elsewhere in this guide (linear regression,
decision trees, gradient boosting, naive Bayes, tree-augmented naive Bayes, neural networks). Logistic regression is covered elsewhere too,
but reaches the shared, generic `AdvancedAnalyticsTrainingService` through its own gateway rather than a dedicated
per-family training service of its own (see [LogisticRegression.md](../LogisticRegression.md)), so it is not one of
these thirty-one. The remaining twenty-two each have their own
file in this section, listed in the menu grouped by kind. Every file follows the same shape: the R snippet using the closest common
CRAN package, the real training service that replaces it, and a code audit of the actual implementation behind that
service (not just its DTO) — what algorithm it really runs, its real defaults, and any divergence from the textbook
method, cited to file and line in the reference worktree.

Every family shares the same submission shape: `SubmitTrainingAsync` on its training service queues a background run and
returns a run id at once, the caller polls `GetRunAsync` for status, and every trained version is immutable and never
made live by training itself — promotion is a separate step. That machinery is identical across all thirty-one families
and is not repeated per file.
