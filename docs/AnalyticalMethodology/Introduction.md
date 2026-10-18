---
layout: default
title: Introduction
nav_order: 2
parent: Analytical Methodology
---

# Introduction

This page orients the rest of the guide: where the product's own `AdvancedAnalytics` tool layer lives, where a decoupled
R script runner fits for the residual cases the tool layer doesn't cover, and how to read the
R-procedure/product-equivalent pattern used throughout every page that follows.

## Where the product's own tool layer lives

Before reaching for R, check whether `AdvancedAnalytics` already does the job. It is the product's Accord.NET-backed
analytics service, exposed two ways:

- **A semantic tool layer** under `Jube.Service/Agent/AdvancedAnalyticsTools/`, one file per concern, grouped into ten
  areas: DescriptiveStatistics, DistributionsAndHistograms, HypothesisTests, RelationshipsAndRegression,
  CategoricalAndSurvival, MultivariateAndClustering, TimeSeriesAndSignal, NumericsAndOptimisation,
  DataPreparationAndResampling, and ModelEvaluation. Each tool method is decorated `[AdvancedAnalyticsTool("Name")]` and
  is written to be selected and read by an LLM agent reasoning about a procedure, rather than called directly by an
  analyst — the same role R's interactive console played, but exact instead of approximate, and without an analyst
  having to know which function or package to reach for.
- **Thirty-one trainable model families**, each with its own training service under
  `Jube.Service/Repository/AnalyticsTrainingRun/` (for example `AdvancedAnalyticsLinearRegressionTrainingService`,
  `AdvancedAnalyticsNeuralNetworkClassifierTrainingService`, `AdvancedAnalyticsDecisionTreeClassifierTrainingService`).
  These cover the regression, classification, clustering, anomaly-detection and neural-network work this guide's R
  procedures demonstrate module by module.
- **A read-only administration page** at `/Model/AdvancedAnalytics` for browsing instances, trained model versions and
  their held-out evidence (metrics, warnings, calibration and ROC charts, model diagrams), and for deleting an instance.
  Promoting, rolling back, allocating and training a model are agent operations, not something done by clicking through
  this page or calling its HTTP routes directly.

Every module from here on names the real class, tool or training service that replaces (or partially replaces) the R
procedure it discusses. Where the reference could not be verified against the current code, the module says so plainly
rather than guessing at a name.

## Where the R script runner fits

`AdvancedAnalytics` is deliberately not a total replacement for R. Some of what this guide covers in R — ad hoc data
wrangling with regular expressions, one-off Bayesian network authoring, exploratory plotting with ggplot2, ODBC access
to an arbitrary database — has no tool-layer equivalent; it stays "R/script-runner only"
throughout this guide. That residual work has a real home: [R Server](RServer/index.md), a standalone, sandboxed
R server (`Jube.R`) that takes real R source over HTTP and returns real console output, timing and error metadata — the
agent hands it a script and reasons over the result, rather than an analyst switching out of the product into a separate
RStudio session. Every "R/script-runner only" note throughout this guide means: this is what that server runs. Job
orchestration and the analytics-agent side of calling it (submit, poll, read back a result — see
[R Server](RServer/index.md)) are a separate layer in front of the server, not part of the server itself.

## How to read the rest of this guide

Every surviving procedure in the pages that follow is presented the same way:

1. Condensed prose describing what the procedure does and why.
2. The R script, exactly as it was actually run to produce this guide, and the real console output it produced.
3. One closing line naming the real namespace/class and tool or training-service name that does the same job in the
   product today, or stating plainly that none does.

That closing line carries one of two labels, and the distinction matters:

- **Tool equivalent:** is the label on almost every procedure. It means an LLM agent reaches the product's answer
  directly, ad hoc, in one or a few tool calls — no training run, no promotion, no persistence, nothing to wire up
  first. This covers the semantic tool layer's computation tools (`DescribeColumn`, `FitLabelEncoder`,
  `EvaluateDecisionTree`, and the rest), the `RenderChart` tool for ordinary business charts, and — just as much a
  tool call as any of those — submitting one of the thirty-one trainable model families for training
  (`Submit<Family>Training`), checking a run's status or result, and promoting, rolling back or allocating a version.
  Memorialising a result into AdvancedAnalytics is not a separate, UI-only ritual sitting outside the tool layer; it
  is itself a short, ordinary sequence of tool calls an agent makes once ad hoc analysis has shown the result is
  worth wiring into repeatable, promoted scoring — not the default first move for a one-off question.
- **AdvancedAnalytics equivalent:** is reserved for the rare remainder: something that genuinely has no ad hoc tool at
  all, because it only exists on the far side of that memorialising step. The clearest case is recall itself — once
  a model version is promoted, every future transaction scores against it through the engine-synced live-model path
  (`Jube.Engine.AdvancedAnalytics.*`), and there is no tool that invokes that exact persisted version ad hoc the way
  `Evaluate*` refits and evaluates a fresh one; the one exception is logistic regression, which does carry a
  dedicated `RecallLogisticRegression` tool. The other case is a handful of richer evidence charts (a ROC curve,
  a calibration curve, a box plot, a correlation-matrix heat map, a tree or network diagram) that render only on the
  read-only `/Model/AdvancedAnalytics` admin page for a trained version, with no `RenderChart` parameter or other tool
  that draws them on demand today.

Treat every R block as "what you would do without the platform" and every equivalent line as the reason you usually
would not need to.
