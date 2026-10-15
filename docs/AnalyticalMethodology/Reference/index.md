---
layout: default
title: Reference
parent: Analytical Methodology
nav_order: 21
has_children: true
---

# Reference

As-built methodology notes that other pages in this section cite as evidence: the product's own numeric data
representation, the Exhaustive search engine's mechanism and known limitations, how its scores are calibrated into
probabilities, the catalogue decisions behind the Bayesian classifier family, and the statistical primitives and
hypothesis tests shared across the product's training and analysis tools. These are code audits and design records,
not R-procedure comparisons, so they sit apart from the rest of this section.

| Page | Covers |
|---|---|
| [Numeric Data Representation](FoundationData.md) | The packed `double[]` array every analytical method reads and writes, in place of a data frame or a database table |
| [Exhaustive Methodology](MethodologySpec.md) | As-built mechanism of the Exhaustive search: pipeline, split, normalisation, variable selection, training, metrics, recall, and a catalogue of known limitations |
| [Exhaustive Calibration](ExhaustiveCalibration.md) | How Exhaustive's raw network output is turned into a calibrated probability: Platt, isotonic and beta scaling, prior shift to the deployment base rate, and measured results |
| [Model Catalogue Decisions](ModelCatalogue.md) | Why the Bayesian classifier family is built the way it is: Tree-Augmented Naive Bayes shipped, a general structure search deliberately dropped |
| [Hypothesis Tests Catalogue](HypothesisTestsToolLayer.md) | The classical and non-parametric inference tools available, and correctness judgement calls made against the underlying statistics library |
| [Shared Statistical Primitives](FoundationAnalytics.md) | Calibration, metrics, preprocessing and data-split primitives shared across training and recall |

## Tool Catalogue

The pages above describe this product's statistical and model-training methodology conceptually. The semantic tool
layer in `Jube.Service/Agent/AdvancedAnalyticsTools` -- 817 individual `[AdvancedAnalyticsTool("...")]`-attributed
methods an LLM agent calls directly by that literal name -- is a separate, much larger surface that none of the
pages above name tool-by-tool, and which `docs/AgentHierarchy/tool-inventory.md` does not cover at all (that
inventory documents a different, CRUD-style tool surface). The pages below name every one of those 817 tools
individually: what each computes, its parameters, its return shape, and real edge-case behaviour, cross-referencing
a conceptual page above instead of repeating it wherever one already covers the same tool.

| Page | Covers |
|---|---|
| [Descriptive Statistics](ToolCatalogue/DescriptiveStatistics.md) | Central tendency, spread and shape, quantiles/ranks/frequency, table and transform tools |
| [Categorical and Survival](ToolCatalogue/CategoricalAndSurvival.md) | Confusion matrices, contingency/association, odds and risk, probability calibration, score ranking, survival estimation and modelling |
| [Distributions and Histograms](ToolCatalogue/DistributionsAndHistograms.md) | Distribution fitting, shape and tail diagnostics, histograms and density, mixture models, multivariate and univariate distributions |
| [Hypothesis Tests](ToolCatalogue/HypothesisTests.md) | Every individual test tool behind the [Hypothesis Tests Catalogue](HypothesisTestsToolLayer.md)'s conceptual description, by its exact callable name |
| [Relationships and Regression](ToolCatalogue/RelationshipsAndRegression.md) | Correlation, generalised linear models, linear regression, nonlinear curve fitting, regression diagnostics, model selection, regularised/robust regression |
| [Data Preparation and Resampling](ToolCatalogue/DataPreparationAndResampling.md) | Binning, class imbalance, data quality, encoding, feature engineering/selection, missing values, resampling, scaling, table shaping |
| [Model Evaluation](ToolCatalogue/ModelEvaluation.md) | Ad hoc model evaluation, cross-validation and model selection (`CompareModels`, `GridSearchHyperParameters`, `ComputePermutationImportance`, `ComputePartialDependence`, and their siblings), multiclass and anomaly, regression evaluation |
| [Multivariate and Clustering](ToolCatalogue/MultivariateAndClustering.md) | Cluster evaluation, clustering, discriminant/embedding, distance and kernel, nearest-neighbour and rule, projection tools |
| [Numerics and Optimisation](ToolCatalogue/NumericsAndOptimisation.md) | Calculus/root-finding, combinatorics and utility, linear algebra, matrix decomposition, optimisation, special functions -- the largest single group |
| [Time Series and Signal](ToolCatalogue/TimeSeriesAndSignal.md) | Hidden Markov/Markov chain tools, sequence pattern analysis, series anomaly/dependence/forecasting/transform, spectral signal analysis, transaction timing |
| [Training Run Lifecycle Variants](ToolCatalogue/TrainingRunLifecycleVariants.md) | The family-scoped `Get<Family>TrainingRunStatus`/`Cancel<Family>TrainingRun`/`Get<Family>TrainingRunResult` tools that duplicate the generic training-run lifecycle for six model families, and why each needed its own |
