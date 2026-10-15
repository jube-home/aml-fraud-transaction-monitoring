---
layout: default
title: Analytical Methodology
nav_order: 6
has_children: true
---

# Analytical Methodology

This section covers the statistical and machine-learning methodology behind `AdvancedAnalytics`: for every technique, a
condensed R procedure immediately followed by the real `AdvancedAnalytics` tool, service or model-training family that
does the equivalent job in the product today, or a plain statement that no equivalent exists and the procedure stays
R-only.

R has not been erased from Jube's analytics story. It is a reference point for understanding the product, and a genuine
fallback for the residual cases the product does not cover, rather than the only way to do the work.

## Pages

| Page                                                             | Covers                                                                                                                                                                                                                    |
|------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Methodology and Taxonomy](MethodologyAndTaxonomy.md)            | The descriptive/predictive/prescriptive taxonomy, the data-abstraction-model-activation-feedback loop, problem framing and elicitation, and where each already shows up elsewhere in this guide                           |
| [Introduction](Introduction.md)                                  | Where the `AdvancedAnalytics` tool layer and its administration page live, where a decoupled R script runner fits for residual cases, and how to read the R-procedure/product-equivalent pattern used throughout          |
| [Getting Started with R](GettingStartedWithR.md)                 | Launching R and RStudio, the console, working directories, scripts, packages, help, and basic variable assignment                                                                                                         |
| [Data Structures](DataStructures.md)                             | Vectors, matrices, factors, lists and data frames: R's core data structures                                                                                                                                               |
| [Loading, Shaping and Merging Data](LoadingShapingMerging.md)    | Horizontal abstraction with numeric and string functions, regular expressions, dates, importing/exporting CSV and database tables, sorting, merging and reshaping data frames                                             |
| [Summary Statistics](SummaryStatistics.md)                       | Histograms, range, quartiles, mean/median, box plots, variance, standard deviation, z-scores, normalisation, skewness/kurtosis, normal-distribution probabilities, log transforms                                         |
| [Abstraction](AbstractionHarmonised.md)                          | Building dependent and independent variables by hand in R (aggregation, ratios, binary flags, transformations, anchoring) next to Jube's own Abstraction Rule and Abstraction Calculation engine                          |
| [Charting](Charting.md)                                          | ggplot2 rapid exploration (`qplot`) next to `Jube.Charting`, the product's own SVG charting library                                                                                                                       |
| [Linear Regression](LinearRegression.md)                         | Correlation, scatter plots, trend lines, one-way and stepwise linear regression, confidence intervals, heat-map correlation matrices                                                                                      |
| [Logistic Regression](LogisticRegression.md)                     | Categorical pivoting, log-curve fitting, forward stepwise logistic regression, recall, confusion matrices, ROC/AUC                                                                                                        |
| [Decision Trees and Boosting](DecisionTreesAndBoosting.md)       | `rpart` and C5.0 decision trees, business-rule extraction, boosting, and gradient boosting machines                                                                                                                       |
| [Naive Bayes](NaiveBayes.md)                                     | Discretising continuous data and training/recalling naive Bayesian classifiers with a Laplace estimator                                                                                                                   |
| [Bayesian Analysis with bnlearn](BayesianAnalysisWithBnlearn.md) | Bayesian network structure, states, conditional probabilities and TAN structure learning, using `bnlearn` in place of the original Norsys Netica procedures                                                               |
| [Neural Networks](NeuralNetworks.md)                             | Training, plotting and recalling feed-forward neural networks for regression and classification                                                                                                                           |
| [Data Frames vs the Packed Array](DataFramesVsAccordArrays.md)   | R's data frame model against the product's packed `double[]` array convention (`AdvancedAnalyticsCache`, `Rows`/`Columns`, row-major IEEE 754 binary64)                                                                   |
| [Variable Transformations](VariableTransformations.md)           | Log/power transforms, standardisation, quantile transformation, winsorisation, discretisation, categorical encoding and cyclical features — academic origin and a code audit of each against its `AdvancedAnalytics` tool |
| [dplyr Gap Analysis](DplyrGapAnalysis.md)                        | Verb-by-verb: what dplyr operations are covered, what's missing, and which gaps are practical to shim on the array versus better left to SQL                                                                              |
| [Other Model Types](OtherModelTypes/index.md)                    | The 22 trainable model families with no R-procedure equivalent, each with its own page including a code audit of the real implementation behind its training service                                       |
| [Prescriptive Analytics](PrescriptiveAnalytics/index.md)         | Monte Carlo simulation, and a pointer into the Bayesian networks treatment — explanatory grounding, not a real-time recall path                                                                                           |
| [Bayesian Networks](BayesianNetworks/index.md)                   | A comprehensive `bnlearn`/Scutari-style treatment of Bayesian networks: graphical foundations, structure learning, parameter learning, inference, validation and where this fits inside `AdvancedAnalytics`               |
| [Governance, KPIs and Model Validation](GovernanceKpisAndModelValidation.md) | Minimum-dataset data foundations, label availability and fallback governance, the KPI set (FPR/TPR/PPV/ROC and more) with the real tool behind each, and the model validation framework (distributional analysis, calibration, discriminatory power, drift, sensitivity/stress testing, back-testing, sign-off) |
| [Reference](Reference/index.md) | As-built methodology notes cited elsewhere in this section as evidence: numeric data representation, the Exhaustive search engine and its calibration, model catalogue decisions, and the shared statistical primitives and hypothesis tests |

## Not covered here

- **Exhaustive Search.** This is Jube's own long-standing model family, not an R technique, and it already has thorough,
  current documentation: the as-built methodology specification at
  [Exhaustive Methodology](Reference/MethodologySpec.md). Nothing here duplicates it.
- **Deep Learning with H2O.** H2O and deep learning are out of scope for `AdvancedAnalytics` entirely. There is no page
  for it in this section.
