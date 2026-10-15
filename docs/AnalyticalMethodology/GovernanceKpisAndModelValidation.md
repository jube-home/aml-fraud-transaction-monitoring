---
layout: default
title: Governance, KPIs and Model Validation
nav_order: 22
parent: Analytical Methodology
---

# Governance, KPIs and Model Validation

This page covers operational governance, key performance indicators and model validation for Jube's AML and fraud
detection monitoring. Its companion material on label-scarce fallback methodology is covered in
[Prescriptive Analytics](PrescriptiveAnalytics/index.md) and
[Explanatory Role Inside AdvancedAnalytics](BayesianNetworks/ExplanatoryRoleInsideAdvancedAnalytics.md); this page
cross-references that work rather than repeating it.

## Data foundations and the minimum viable dataset

A defensible AML monitoring programme has to be clear, up front, about the minimum data estate its methodology actually
needs. The foundational requirement is the record of transactions itself: properly engineered, it is a sufficient basis
for both rule-based and probabilistic controls, because it carries the behavioural and financial detail, and the
temporal granularity, that every feature in this guide is ultimately built from. In this guide's own terms, that record is what
[Abstraction](AbstractionHarmonised.md) calls **Payload** — the raw per-transaction record from which every Abstraction
Rule and Abstraction Calculation derives a variable (`AbstractionHarmonised.md`'s vocabulary section and its worked
procedures both work directly against Payload fields).

Artefacts beyond that baseline — IP telemetry, device metadata, session attributes, biometric signals — are real
enhancements to feature richness and discriminatory power, but they are opportunistic, not a precondition for
compliance. A monitoring programme should not stall waiting for an idealised data lake or
vendor-grade behavioural telemetry: feature construction, anomaly detection, threshold calibration and the model
validation practice described later on this page are all achievable against the minimum dataset alone, with richer
sources layered in as they become reliably available. That stance matters most exactly where project timelines,
supervisory scrutiny and legacy architecture already constrain what data is accessible — which is the normal operating
condition for a regulated deployment, not an edge case.

## Label availability and the standard/fallback split

The methodology's starting preference is standard supervised practice: where a trustworthy dataset of transactions
labelled fraudulent or non-fraudulent exists, use it to train supervised models, validate feature importance and rule
thresholds against known outcomes, and compute the KPIs below — false positive rate, true positive rate, precision and
ROC curves — directly from labelled data. Labels, where they exist, also support the rule-recall testing, sensitivity
analysis and operational calibration this page describes further down.

In practice, reliable labels are frequently incomplete, inconsistent or simply unavailable, especially early in a
programme's life, and the methodology treats that as the expected case to plan for rather than an exception. The
fallback it prescribes — unsupervised and probabilistic detection over feature states, feature-based risk scoring,
expert-guided review of the transactions that scoring surfaces, and iterative recalibration as reliable labels
eventually accumulate — is documented in [Prescriptive Analytics](PrescriptiveAnalytics/index.md) and
[Explanatory Role Inside AdvancedAnalytics](BayesianNetworks/ExplanatoryRoleInsideAdvancedAnalytics.md): Bayesian
networks as the primary probabilistic framework for anomaly detection under weak or absent labels, run as an R script
job through `Jube.R` and `RScriptTools` rather than as a native, per-transaction recall path, with calibration assessed
indirectly (posterior stability, anomaly-score consistency over time, expert review of high-risk tails) precisely
because label-based calibration cannot be used. This page does not repeat that material; the point that matters here
is governance, not mechanism: whichever regime is in force, KPI tracking continues without interruption, and every
adjustment made under the fallback regime — a recalibrated threshold, a refined rule, a promoted model — is documented
to the same standard as one made under full label availability, so that a later audit sees one continuous, defensible
record rather than a gap where labels happened to be missing.

## Governance and audit considerations

The fallback methodology exists specifically so that no critical risk goes unmonitored purely because labelled outcomes
are missing. What makes that defensible under audit is that every step — feature derivation, probabilistic inference,
expert review, and rule or threshold adjustment — is documented and reproducible, and that KPI tracking (false positive
rate, true positive rate, ROC curves, and the operational metrics below) stays active under fallback conditions exactly
as it would under full label availability. In practice this means treating a threshold change, a rule addition or a
model promotion as an event with a recorded rationale and a recorded actor, not a silent configuration edit — the same
discipline [Sign-off expectations](#sign-off-expectations) below formalises into named checkpoints.

## Key performance indicators

The KPIs below are the quantitative measures for evaluating rule and model performance. Where a real, callable
`AdvancedAnalyticsTools` tool already computes a KPI, it is named here and verified against the actual source; where
nothing computes it yet, that is stated plainly instead of implying a tool exists.

| KPI | Definition | Purpose | Computed today by |
|---|---|---|---|
| False Positive Rate (FPR) | Share of legitimate transactions flagged as suspicious | Monitors operational efficiency; controls unnecessary customer friction | `ClassifierMath.Metrics`'s `FalsePositiveRate`, returned by `ConfusionMatrixTools`' `CalculateBinaryClassificationMetricsFromCounts` / `FromLabels` / `AtThreshold` tools (`Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival`) |
| Detection Rate / True Positive Rate (TPR) | Share of fraudulent transactions correctly flagged | Measures fraud/AML detection effectiveness; supports compliance objectives | The same `ConfusionMatrixTools` metrics object's `Recall` (with a Wilson interval); also swept across cut-offs by `ScoreRankingTools`' `ComputeThresholdSweep` |
| Precision / Positive Predictive Value (PPV) | Share of flagged transactions that are truly fraudulent | Balances efficiency and accuracy; informs investigation-team resourcing | `ConfusionMatrixTools`' `Precision` (Wilson interval), and `ScoreRankingTools`' `SelectThresholdForTargetPrecision` / `SelectThresholdMaximisingFBeta` for choosing a cut-off against it |
| Rule Coverage / Feature Coverage | Proportion of transaction types and portfolio segments monitored by rules/models | Ensures comprehensive risk coverage and regulatory defensibility | No dedicated coverage-accounting tool exists; `RateAndInformationValueTools`' `TabulateEventRateBySegment` and `TabulateAlertHitRatesBySegment` give the per-segment event and alert-hit rates a coverage review would be built on, but assembling those into a coverage statement is a manual exercise today |
| Model Calibration Metrics | Comparison of predicted vs. actual outcomes (expected vs. observed probability of fraud) | Ensures probabilistic predictions are accurate and interpretable; supports auditability | `ProbabilityCalibrationTools`' `ComputeCalibrationCurve`, `FitCalibrationSlopeAndIntercept`, `TestHosmerLemeshowCalibration` and `CalculateBrierScoreDecomposition`; Exhaustive's own calibrated recall (Platt, isotonic or beta, `docs/AnalyticalMethodology/Reference/ExhaustiveCalibration.md`) is the production path this feeds |
| Case / Operational Metrics | Volume of flagged transactions, time-to-review, analyst workload | Tracks operational impact of rules and models; helps maintain sustainable workflows | `RateAndInformationValueTools`' `TabulateAlertHitRatesBySegment` gives flagged-volume and hit-rate by segment; analyst time-to-review and workload are case-management operational data with no `AdvancedAnalyticsTools` equivalent, since that is workflow telemetry rather than a statistical computation |
| Cost Curves / ROC Analysis | ROC curves, cost-benefit curves for thresholds | Supports optimisation of thresholds and trade-offs between detection and false positives | `ScoreRankingTools`' `ComputeRocCurve`, `ComputePrecisionRecallCurve` and `ComputeThresholdSweep`; `SelectThresholdMinimisingCost` for an explicit cost-weighted cut-off; Exhaustive's own ROC-point threshold sweep is documented at `docs/AnalyticalMethodology/Reference/MethodologySpec.md` §7.3 |
| Continuous Improvement Metrics | Improvements in detection or reduction in false positives over time | Demonstrates ongoing governance, learning and adaptive methodology | No single tool tracks a KPI's trend release-over-release; `RateAndInformationValueTools`' `CalculatePopulationStabilityIndexFromCounts` / `ForNumericValues` / `ForCategories` give the drift half of that comparison (see [Stability and population drift analysis](#stability-and-population-drift-analysis)), but trending a KPI like FPR across model versions over time is not itself computed anywhere yet |

These KPIs should be tracked per channel and transaction type, to account for portfolio heterogeneity, and reviewed
periodically with compliance, audit and risk stakeholders — `TabulateEventRateBySegment`
and `TabulateAlertHitRatesBySegment` (`Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.RateAndInformationValueTools`)
are the real tools that segment KPI computation by an arbitrary label such as channel or country, so that a per-segment
KPI table is a real, callable result rather than a hand-rolled query.

## Statistical and analytical methodology

This methodology groups its quantitative practice into feature engineering, exploratory data analysis, label analysis
and feature selection, a modelling framework, validation and sensitivity analysis, and continuous improvement. Every
one of those already has a home elsewhere in this guide or the product; this section is a map onto that existing
material rather than new prose, except where noted.

- **Feature engineering.** Deriving candidate features from the transaction record — velocity, amount distributions,
  ratios, historical patterns and behavioural signals — is exactly [Abstraction](AbstractionHarmonised.md)'s subject,
  and the list of transform types (logs, ratios, binary flags, standardisation) is catalogued in full,
  including which `AdvancedAnalytics` tool implements each one, in [Variable Transformations](VariableTransformations.md).
- **Exploratory data analysis.** Univariate distribution, outlier and anomaly examination, and bivariate/multivariate
  relationship analysis, is [Summary Statistics](SummaryStatistics.md)'s subject (histograms, quartiles, box plots,
  variance, z-scores, skewness and kurtosis).
- **Label analysis and feature selection.** Where labels exist, they inform feature importance directly (see the KPI
  table above and `ScoreRankingTools`' threshold-selection tools); `RateAndInformationValueTools`' `CalculateInformationValueByCategory`
  and `CalculateInformationValueByBins` (`Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival`) compute
  weight-of-evidence information value per candidate feature, which is the real, callable form of using labelled data
  to inform feature importance. Where labels are weak or absent, unsupervised and Bayesian
  inference stands in, as described under [Label availability and the standard/fallback split](#label-availability-and-the-standardfallback-split)
  above.
- **Modelling framework.** Bayesian networks are the primary probabilistic framework for anomaly detection and
  prescriptive analytics under weak labels, refined by supervised models once reliable labels exist. The
  shipped, callable form of that is Tree-Augmented Naive Bayes, covered in
  [Bayesian Analysis with bnlearn](BayesianAnalysisWithBnlearn.md) and its product boundary set out precisely in
  [Explanatory Role Inside AdvancedAnalytics](BayesianNetworks/ExplanatoryRoleInsideAdvancedAnalytics.md); where labels
  are reliable, the twenty-two families in [Other Model Types](OtherModelTypes/index.md) and Jube's own Exhaustive
  engine (`docs/AnalyticalMethodology/Reference/MethodologySpec.md`) are the supervised alternative.
- **Validation and sensitivity analysis.** This is the largest topic with no existing home elsewhere in this guide, so
  it gets its own section below rather than a cross-reference.
- **Continuous improvement.** Periodic review of models, rules and thresholds against new data and evolving threat
  patterns is the same governance discipline described under
  [Governance and audit considerations](#governance-and-audit-considerations) above, applied on a schedule rather than
  only at deployment.

## Model validation and performance assurance

Model validation within this methodology provides a rigorous, proportionate and auditable assurance framework, aligned
in principle with Credit Risk model governance practice (the same regulatory lineage — validation before deployment, at
material change, and periodically thereafter — carried over into a domain where labelled outcomes are frequently
incomplete or delayed, adversarial, and non-stationary in a way Credit Risk portfolios usually are not). Its objectives
are to demonstrate that models behave consistently with observed data, that outputs are interpretable, calibrated and
defensible, that degradation, instability or unintended bias is caught, and that an audit trail equivalent in spirit to
Credit Risk model validation exists for every probabilistic model, rule and threshold in production.

### Distributional analysis and feature validation

Validation begins at the feature level, consistent with Credit Risk practice: the empirical distribution of every key
feature (amounts, velocities, ratios, behavioural indicators) is examined, candidate parametric distributions are
assessed where appropriate, and goodness-of-fit is evaluated both visually (histograms, plots — see
[Summary Statistics](SummaryStatistics.md)) and statistically. Features exhibiting instability, extreme skew or
structural breaks are transformed, segmented or excluded, using the same transform catalogue as
[Variable Transformations](VariableTransformations.md).

The statistical half of that goodness-of-fit judgement — beyond visual inspection — has real, callable tools in
`Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.RankAndDistributionTestTools`
(`docs/AnalyticalMethodology/Reference/HypothesisTestsToolLayer.md`): `ShapiroWilkNormalityTest`, `AndersonDarlingNormalityTest`,
`LillieforsNormalityTest` and `JarqueBeraNormalityTest` for normality, `ChiSquareDistributionFitTest` for a general
distributional fit, and `KolmogorovSmirnovOneSampleTest` for a fit against an arbitrary reference distribution.

### Model calibration and probability validation

For probabilistic and Bayesian models, predicted risk probabilities are compared to observed outcomes where labels
exist, assessed at portfolio, segment and channel level. `ProbabilityCalibrationTools`
(`Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival`) is the real tool group for this:
`ComputeCalibrationCurve` bins predicted probability against observed event rate with intervals,
`FitCalibrationSlopeAndIntercept` reports calibration-in-the-large and slope/intercept, `TestHosmerLemeshowCalibration`
gives a goodness-of-fit test for calibration specifically, and `CalculateBrierScoreDecomposition` / `CalculateProbabilityScoringRules`
give scoring-rule-based calibration measures. Exhaustive already runs this in production: every Exhaustive model is
calibrated (Platt, isotonic or beta, selectable per instance) rather than shipping a raw uncalibrated score, documented
in full at `docs/AnalyticalMethodology/Reference/ExhaustiveCalibration.md`.

Where labels are weak or absent, the fallback is to assess calibration indirectly — stability of posterior
distributions, consistency of anomaly scores over time, and expert review of high-risk tails — which is exactly the
indirect-calibration approach already documented in
[Explanatory Role Inside AdvancedAnalytics](BayesianNetworks/ExplanatoryRoleInsideAdvancedAnalytics.md) for the
Bayesian-network fallback path.

### Discriminatory power and ranking validation

Where labels exist, standard discriminatory measures apply: ROC curves and AUC, precision/recall, and KS-style class
separation. All three are real, callable tools on `ScoreRankingTools`
(`Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival`): `ComputeRocCurve` and
`ComparePairedRocAucDelong` (a statistical test for whether two models' AUCs genuinely differ), `ComputePrecisionRecallCurve`,
and `ComputeKolmogorovSmirnovSeparation` for the KS statistic between the score distributions of the two outcome
classes. `ModelSelectionTools`' `CompareModels` (with an `EvaluationFamily` selection) cross-validates a candidate
classifier — Tree-Augmented Naive Bayes included — against up to eleven other families on identical folds, which is the
real form of "comparing discriminatory power across candidate models."

Where labels are weak or absent, the fallback is relative ranking stability across time windows, concentration of risk
in high-score bands, and manual/downstream investigative feedback. No dedicated tool computes
ranking-stability-over-time directly; the closest available building block is running the same score-distribution
comparison (see [Stability and population drift analysis](#stability-and-population-drift-analysis) below) on the
score itself rather than on an input feature.

### Stability and population drift analysis

Models and features are tested for temporal stability: feature distribution drift, changes in score distributions, and
volume/composition shifts in high-risk flags. This is a real, callable tool group:
`RateAndInformationValueTools`' `CalculatePopulationStabilityIndexFromCounts`, `CalculatePopulationStabilityIndexForNumericValues`
and `CalculatePopulationStabilityIndexForCategories` (`Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival`)
compute the population stability index (PSI) between a baseline and a current population, for pre-binned counts,
numeric values or categorical labels respectively. Consistent with the caution that PSI/CSI should be used carefully,
with emphasis on interpretation rather than mechanical thresholds, given fraud's genuinely adversarial
and non-stationary nature, these tools report the index and its per-bin contribution rather than an automatic
pass/fail. A significant drift finding is meant to trigger a review of feature definitions, a threshold recalibration,
or a model re-estimation — the same governance discipline as any other material change, per
[Sign-off expectations](#sign-off-expectations) below.

### Sensitivity and stress testing

Sensitivity analysis assesses model robustness: the impact of threshold shifts on FPR, TPR and queue volumes; behaviour
under elevated transaction volumes or changed customer behaviour; and the impact of removing or perturbing key
features. The threshold-shift half is `ScoreRankingTools`' `ComputeThresholdSweep`, which reports the confusion-matrix
metrics — including FPR and TPR — at a sweep of cut-offs in one call. The feature-perturbation half is
`ModelSelectionTools`' `ComputePermutationImportance` and `ComputePartialDependence` (with `ComputeRegressionPermutationImportance`
/ `ComputeRegressionPartialDependence` on `RegressionEvaluationTools` for regression targets): permutation importance
measures the performance drop when a feature's values are shuffled, which is a direct, real measure of "impact of
removing or perturbing key features," and partial dependence shows how a prediction moves as one feature is varied
with the rest held fixed. Exhaustive has its own long-standing form of this — backward elimination by sensitivity,
documented at `docs/AnalyticalMethodology/Reference/MethodologySpec.md` §5.3–§5.4, and Monte Carlo sensitivity simulation, §6.6,
cross-referenced from [Prescriptive Analytics](PrescriptiveAnalytics/index.md). Volume-and-behaviour stress testing
(what changes under an elevated-volume or shifted-population scenario) has no dedicated tool: it is a scenario exercise
built by feeding a simulated or resampled population — for instance, from Monte Carlo simulation
(`PrescriptiveAnalytics/MonteCarloSimulation.md`) — through the existing evaluation tools above, not a single computed
metric of its own.

### Back-testing and outcome review

Where historical outcomes exist, models and rules are back-tested on prior periods, differences between historical and
current performance are analysed, and material deviations are explained and documented, in support of regulatory
defensibility, management confidence and change-approval decisions. This is the one piece of the validation framework
with **no equivalent at all today**, dedicated or otherwise: there is no walk-forward, out-of-time or temporal
holdout evaluation tool anywhere in `AdvancedAnalyticsTools`, and no back-testing job pattern comparable to the R
script execution job pattern that carries the Bayesian-network fallback methodology (`Jube.R` /
`RScriptTools`, see [R Server](RServer/index.md)). The nearest available building blocks — `CompareModels`' identical-fold
cross-validation, and the population-stability tools above, applied to a manually assembled prior-period-versus-current
split — could be composed into a back-test today, but nothing packages that composition as a single callable operation.
This is recorded here as a genuine gap rather than implied to exist.

## Sign-off expectations

All validation activity above is expected to produce a documented artefact, and at defined stages of the methodology,
formal sign-off from designated stakeholders provides governance, accountability and regulatory defensibility before
deployment. These checkpoints describe organisational process rather than a technique this guide could re-run:

| Stage | Sign-off |
|---|---|
| Feature specification and analytical dataset creation | Risk and Compliance confirm the selected features are appropriate, relevant and sufficient for the intended monitoring objective |
| Modelling framework and threshold definition | Data Science Lead, Risk and Compliance review the probabilistic model structure, its assumptions, and the threshold rationale |
| Rule derivation and deployment | Operations, Risk and Compliance confirm rules derived from model outputs are operationally aligned with risk appetite and implementable within vendor systems |
| Validation, sensitivity analysis and KPI assessment | Audit and Compliance confirm model performance, rule calibration and KPI tracking meet regulatory and internal standards |
| Ongoing review and continuous improvement | Periodic governance review approves updates to models, thresholds or rules as new data, labels or operational insight become available |

Every sign-off is expected to be documented, time-stamped and retained as part of the audit trail, so that every
decision remains transparent, defensible and fully traceable — the same standard
[Governance and audit considerations](#governance-and-audit-considerations) above sets for the fallback methodology
specifically, applied here to the full lifecycle of a model or rule.
