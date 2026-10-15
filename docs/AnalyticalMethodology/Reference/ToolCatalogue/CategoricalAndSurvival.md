---
layout: default
title: Categorical and Survival Tool Catalogue
nav_order: 12
parent: Reference
grand_parent: Analytical Methodology
---

# Categorical and survival tool catalogue

`Jube.Service/Agent/AdvancedAnalyticsTools/CategoricalAndSurvival/` is one of the ten semantic tool groups an AskJooby
agent calls directly by name. None of its 70 tools has any entry elsewhere under `docs/`, which means Jadek, the
reply-validating agent, can never confirm a claim that cites one: a correct answer built on, say, `ComputeRocCurve` or
`FitCoxProportionalHazards` fails validation purely because nothing else in the RAG-indexed corpus says what that
tool computes. This page covers the whole group, tool by tool, grouped by the source file each tool lives in. Every
tool is bound to the group name `CategoricalAndSurvival` and gated behind the `Analytics` permission; none of them
persist anything, and none of them train or save a model.

A handful of conventions recur across almost every tool in this group and are stated once here rather than in each
entry: a binary outcome is always encoded as `1` for the event of interest and `0` otherwise, and a row whose score or
time is `NaN` is silently dropped and counted in the result's `SkippedRows` field rather than rejected outright, unlike
most other malformed input (wrong lengths, negative counts, out-of-range confidence levels), which throws. Tools that
need both outcome classes present (anything built on a ROC curve, a threshold sweep or a confusion matrix) reject data
where every usable row is the same class. Confidence levels are a fraction strictly between 0 and 1, defaulting to
0.95, and intervals built from a normal approximation use the corresponding two-sided z value.

## Confusion-matrix and classification metrics (ConfusionMatrixTools)

These five tools all return the same rich `ConfusionMetricsResult`, or the multi-class analogue, built around the
confusion matrix at one fixed operating point. The binary result carries accuracy, precision (positive predictive
value), recall (sensitivity, detection rate), specificity, negative predictive value, false positive and negative
rates, false discovery and omission rates, F1 and a general F-beta, Matthews correlation, Cohen's kappa, balanced
accuracy, Youden's J, markedness, geometric mean, the two likelihood ratios, the diagnostic odds ratio, the predicted
positive rate (alert rate) and lift, with Wilson intervals on accuracy, precision, recall and specificity. Precision,
recall, specificity, the F-scores, Matthews correlation and kappa report 0 when their denominator is empty rather than
NaN, following the shared platform convention; every other ratio is `NaN` when undefined.

**CalculateBinaryClassificationMetricsFromCounts** computes this full metric set directly from the four confusion
counts (true positives, false positives, true negatives, false negatives) of a rule, alert or model evaluated at one
point. `beta` (default 1) weights the F-beta measure above 1 to favour recall or below 1 to favour precision, and an
optional pair `costFalseNegative`/`costFalsePositive`, supplied together, adds the expected cost per case to the
result. All four counts must be non-negative and not all zero.

**CalculateBinaryClassificationMetricsFromLabels** reaches the same result from two parallel 0/1 arrays, `actual` and
`predicted`, by counting the confusion matrix itself first; use it when per-case flags are available rather than
pre-aggregated counts.

**CalculateBinaryClassificationMetricsAtThreshold** turns a continuous score or probability into flags at one
`threshold` (a case is flagged when its score is greater than or equal to it) and evaluates the same metrics against
the true 0/1 outcome. Rows with a `NaN` score are dropped; see `ComputeThresholdSweep` to inspect many thresholds at
once, or the `SelectThreshold*` tools to choose one automatically.

**CalculateMulticlassClassificationMetrics** evaluates a multi-class classifier from two parallel arrays of class
labels, `actual` and `predicted`. It builds the confusion matrix (rows actual, columns predicted) over the distinct
labels observed (or an explicit `classOrder`, capped at 500 classes), then returns overall accuracy with an interval,
balanced accuracy, Cohen's kappa with its standard error and interval (optionally `Linear` or `Quadratic` weighted via
`kappaWeighting` for ordered classes), a multi-class Matthews correlation, per-class precision/recall/specificity/F1/
F-beta/support, macro, micro and support-weighted averages, and the `topConfusions` most frequent off-diagonal
confusions. Macro treats every class equally, weighted follows class size, and micro pools all cases and equals
accuracy; a class that is never predicted has precision 0, and pairs with a blank label on either side are skipped.

**CalculateMulticlassMetricsFromConfusionMatrix** computes the identical metric set, but from a square matrix of
counts already laid out with rows as actual classes and columns as predicted classes in a shared order, rather than
from raw labels; counts must be whole numbers and the matrix no larger than 500 by 500.

## Contingency tables and categorical association (ContingencyAssociationTools)

**CrossTabulateCategories** builds a contingency table (cross-tabulation) from two parallel arrays of category labels,
one pair per row, returning the sorted row and column labels, the count in every cell, both sets of margins
(`RowTotals`/`ColumnTotals`), the grand total and the number of pairs skipped because either label was blank. It is
the normal way to turn two raw categorical columns into the count table the association tools below expect.

**AnalyseContingencyTableAssociation** tests and measures association between two categorical variables from an
existing table of counts (at least 2 by 2). It returns Pearson's chi-square with its degrees of freedom and p-value, a
likelihood-ratio G statistic, a Yates continuity-corrected chi-square for 2-by-2 tables, the expected counts under
independence, the Pearson and adjusted standardised residuals (a cell with an absolute adjusted residual above about 2
is driving the association), the cell-level chi-square contributions, and the effect sizes Cramér's V, bias-corrected
Cramér's V, phi, the contingency coefficient and Tschuprow's T, plus a plain-language `EffectSizeLabel`. `Warnings`
reports when expected counts are too small for the chi-square approximation to be trusted (`SmallestExpectedCount` and
`FractionOfCellsExpectedBelowFive` are both returned so the caller can judge for itself); a row or column with zero
observations is rejected outright.

**AnalyseCategoricalAssociationFromLabels** is the same analysis starting from two parallel label arrays rather than a
prepared table: it calls `CrossTabulateCategories` internally and then runs the full association analysis on the
result, dropping any case with a blank label on either side. The adjusted residuals are the place to look for which
specific category pairs occur more or less often than independence predicts.

**CalculateGoodmanKruskalLambda** computes Goodman and Kruskal's lambda from a count table: the proportional reduction
in prediction error for one variable when the other is known. `LambdaColumnGivenRow` is the improvement in guessing
the column category from the row category, `LambdaRowGivenColumn` the reverse, and `LambdaSymmetric` their average; 0
means no help, 1 means perfect prediction from the other variable. Lambda can be 0 even when a real association
exists, because it only credits a change to the most common category, and it is `NaN` when the predicted variable is
already concentrated in a single category.

**CalculateOrdinalAssociationMeasures** measures the strength and direction of association between two variables whose
rows and columns are already in a meaningful increasing order (not just arbitrary categories). It returns concordant,
discordant and tied-pair counts, Goodman-Kruskal gamma with its asymptotic standard error and confidence interval,
Kendall's tau-b, Stuart's tau-c and Somers' D in both directions and symmetric, each running from -1 (perfectly
reversed order) through 0 to +1. Gamma ignores ties and is therefore the largest in magnitude of the group; tau-b and
Somers' D penalise ties. The table is capped at 10,000 cells, and supplying rows or columns out of their natural order
makes the sign and size of every statistic meaningless.

**CalculateCategoricalMutualInformation** quantifies dependence between two categorical variables with information
theory rather than a chi-square assumption of order or shape. It returns the entropy of each variable, their joint
entropy, the mutual information (how much knowing one variable reduces uncertainty about the other, 0 under
independence), a version normalised to between 0 and 1, and Theil's uncertainty coefficients in each direction and
symmetric. `useBits` (default true) reports in bits rather than natural units (nats). A variable with a single
category has zero entropy and gives `NaN` coefficients.

**RankCategoricalColumnsByAssociationWithOutcome** screens many categorical columns at once against a shared binary
outcome and ranks them from strongest to weakest. For every named column it cross-tabulates the categories against
the outcome and returns the chi-square test with its p-value and a Benjamini-Hochberg adjusted p-value (controlling
false discoveries across the many columns tested at once), Cramér's V and its bias-corrected form, the information
value (weight-of-evidence separation, as in `CalculateInformationValueByCategory`) and the mutual information in bits.
Results are ordered by `CramersVBiasCorrected` descending; a column that is constant, or an outcome with a single
class among the usable rows, gets `NaN` statistics and an explanatory `Note` rather than being dropped from the
output. Blank category values are skipped per column, independently of other columns.

## Odds, risk and 2-by-2 table analysis (OddsAndRiskTools)

Nine of this file's ten tools work from a 2-by-2 table laid out as exposed-events, exposed-non-events,
unexposed-events, unexposed-non-events; several share an `applyZeroCellCorrection` flag that adds 0.5 to every cell
when any cell is zero (the Haldane-Anscombe correction) so a ratio stays finite rather than exploding to infinity or
collapsing to zero.

**CalculateOddsRatio** computes the odds ratio comparing an exposed group with an unexposed group: (exposed events
times unexposed non-events) divided by (exposed non-events times unexposed events). Above 1 means the event is more
likely in the exposed group; an interval containing 1 means no clear difference. The interval, z statistic and
two-sided p-value use the Woolf logit method; for rare events the odds ratio approximates the risk ratio below.

**CalculateRiskRatio** computes the risk ratio (relative risk): the exposed event rate divided by the unexposed event
rate, with a log-scale interval, z statistic and p-value. `RelativeRiskReduction` is one minus the ratio. If either
group has no events and the correction is enabled, 0.5 is added to every cell so the ratio and interval stay finite.

**CalculateRiskDifference** computes the absolute risk difference (exposed rate minus unexposed rate) with a Wald
interval and a Newcombe-Wilson interval that behaves better for rare events, a pooled z test, and the two attributable
fractions: `AttributableFractionAmongExposed` (the share of the exposed group's own events attributable to exposure)
and `PopulationAttributableFraction` (the share of all events attributable to exposure), both assuming the exposure is
causal and the sample mix represents the population.

**CalculateNumberNeededToTreat** converts a risk difference into the number of exposed cases that must be handled to
see one extra event (if the difference is positive, "number needed to harm") or to prevent one event (if negative,
"number needed to treat"), using the reciprocal of the absolute risk difference and the Newcombe interval on the
difference for its own bounds. When that interval spans zero the number needed is unbounded above:
`IntervalSpansInfinity` is true and `Lower`/`Upper` are `NaN`; an exact-zero difference also gives `NaN`, since no
finite number of cases produces an extra event.

**CalculateYuleAssociation** computes Yule's Q and Yule's Y for a 2-by-2 table, plus phi and the odds ratio for
context. Q equals `(ad - bc) / (ad + bc)` and runs from -1 to +1, reaching ±1 whenever one off-diagonal cell is empty;
Y (the coefficient of colligation) is a gentler version of the same idea. Cells are read as exposed-events (a),
exposed-non-events (b), unexposed-events (c), unexposed-non-events (d); a coefficient is `NaN` when undefined for the
zeros supplied.

**CalculateDiagnosticLikelihoodRatios** computes sensitivity, specificity, the predictive values and both likelihood
ratios of a binary rule or alert from its confusion counts, each with a log-scale interval, plus the diagnostic odds
ratio. The positive likelihood ratio (sensitivity over one minus specificity) says how much a flag multiplies the odds
of the event; the negative likelihood ratio says how much a *non*-flag multiplies them, with values well below 1
being reassuring. `PostTestProbabilityAfterPositive`/`AfterNegative` apply Bayes' rule at an optional `pretestProbability`
you supply, or at the sample prevalence when left empty; zero cells can make an interval infinite or `NaN`.

**AnalyseTwoByTwoTable** runs every standard analysis of a 2-by-2 table in one call: the odds ratio, risk ratio, risk
difference (with both Wald and Newcombe intervals), Yule's Q and Y, phi, Pearson chi-square and a Yates-corrected
chi-square with p-values, and the smallest expected count. It is meant as the first look at whether a segment, rule or
channel is associated with an outcome; the description itself recommends falling back to an exact test from the
hypothesis-testing tool layer when the smallest expected count is below 5.

**AnalyseBinaryExposureAndOutcome** builds the same 2-by-2 table from two parallel 0/1 columns (an exposure flag and
an outcome flag, one entry per case) and runs `AnalyseTwoByTwoTable` on the result. Exposure 1 means exposed, outcome
1 means the event; any entry that is not 0 or 1 is rejected.

**CalculateMantelHaenszelOddsRatio** pools several 2-by-2 tables, one per stratum (for example one per country,
channel or month), into a single Mantel-Haenszel common odds ratio with a Robins-Breslow-Greenland interval, tested
with the Cochran-Mantel-Haenszel chi-square (plain and continuity-corrected). It also runs the Breslow-Day test, with
Tarone's correction, of whether the odds ratio is homogeneous across strata: a small p-value there means the pooled
figure should not be quoted alone, because the association genuinely differs by stratum (a Simpson's-paradox check).
Strata with fewer than two usable cases are dropped and counted in `StrataDropped`; a pooled odds ratio that comes out
non-finite (no stratum has both an exposed event and an unexposed non-event, or the reverse) is rejected rather than
returned.

**CalculateBinomialProportionConfidenceInterval** is the one tool in this file that is not a 2-by-2 comparison: it
returns a confidence interval for a single proportion (an alert rate, hit rate or fraud rate) from an event count out
of a number of trials, with a choice of `method` — Wilson (the default, reliable near 0 or 1 and at small samples),
Clopper-Pearson (exact and conservative), Agresti-Coull, Jeffreys (Bayesian equal-tailed) or Wald (simple but poor for
rare events). Zero trials returns `NaN` for all three fields.

## Probability calibration (ProbabilityCalibrationTools)

**ComputeCalibrationCurve** builds a reliability (calibration) curve for predicted probabilities against a 0/1
outcome: cases are grouped into `bins` (default 10, up to 1000) either by `UniformWidth` (splitting 0 to 1 evenly) or
`EqualCount` (similar case counts per bin, which suits skewed rare-event probabilities), and each bin's mean predicted
probability is compared with the event rate actually observed there, with a Wilson interval on the observed rate.
`Gap` is observed minus predicted, so positive means the model under-predicts in that bin; empty bins are omitted
entirely. Beyond the per-bin table the result gives the expected calibration error (the count-weighted mean absolute
gap), the maximum calibration error, the Brier score, log loss, and a calibration slope and intercept fitted by
logistic recalibration on the logit of the probability — slope 1 and intercept 0 are ideal, and a slope below 1 means
the model is over-confident. Probabilities must lie between 0 and 1; rows with `NaN` are dropped.

**CalculateBrierScoreDecomposition** splits the Brier score into Murphy's components using the same equal-width
binning: `Reliability` (calibration error, lower is better), `Resolution` (how much the bins separate events from
non-events, higher is better), `Uncertainty` (the base-rate variance, fixed by the data) and `WithinBinTerm`, a
usually small correction term (within-bin forecast variance minus twice the within-bin covariance of forecast and
outcome, zero when every probability inside a bin is identical) such that Brier equals
`Reliability - Resolution + Uncertainty + WithinBinTerm`. `BrierSkillScore` is one minus the Brier score over the
Brier score of always predicting the base rate: 0 means no better than the base rate, 1 is perfect, negative is worse.
Use it to tell whether a model's error comes from poor calibration (large `Reliability`) or poor discrimination
(small `Resolution`).

**CalculateProbabilityScoringRules** scores predicted probabilities with the two standard proper scoring rules — Brier
score and log loss — each compared with the trivial model that always predicts the base rate via `BrierSkillScore` and
`LogLossSkillScore` (one minus the ratio to that baseline; above 0 means the model adds information).
`CalibrationInTheLarge` is the observed event rate minus the mean predicted probability, so positive means systematic
under-prediction. Probabilities are clipped at `epsilon` (default 1e-15, must stay below 0.5) before the logarithm so
a probability of exactly 0 or 1 does not make log loss infinite.

**TestHosmerLemeshowCalibration** runs the Hosmer-Lemeshow goodness-of-fit test: cases are sorted by predicted
probability and cut into `groups` equal-sized bins (3 to 1000, default 10), and the observed event count in each
group is compared with the count expected from the average predicted probability there. The chi-square statistic has
`groups - 2` degrees of freedom; a p-value below 0.05 means the predictions are significantly miscalibrated, while a
large p-value is weak evidence of good calibration on a small sample and over-sensitive on a very large one. Each
group's own chi-square contribution shows where the miscalibration is concentrated.

**FitCalibrationSlopeAndIntercept** estimates calibration slope and intercept directly by a logistic regression of the
outcome on the logit of the predicted probability. A slope near 1 with an intercept near 0 means well-calibrated
probabilities; a slope below 1 means predictions are too extreme (over-confident), above 1 too timid; a non-zero
intercept alongside a slope near 1 usually signals a base-rate shift rather than a ranking problem. The result's
`Interpretation` field states one of five readings in plain language, from "well calibrated" through
"predictions are far too extreme (over-confident)" to "the recalibration could not be fitted" when the fit is
impossible (for example only one outcome class, or identical probabilities throughout).

**CalculateMulticlassLogLoss** scores multi-class probability predictions: each row holds a probability per class
(columns ordered as `classNames`), rows are rescaled to sum to 1 so unnormalised scores are accepted, and
probabilities are clipped at `epsilon`. It returns the mean log loss and the multi-class Brier score (mean squared
distance to the one-hot outcome, 0 to 2), an implied accuracy (share of rows where the highest probability matches the
actual class), and `LogLossPerActualClass`/`SupportPerClass` so a per-class weak spot is visible. Rows containing a
`NaN` probability are dropped.

**AdjustProbabilitiesForBaseRate** re-bases predicted probabilities from the event rate a model was trained at to a
different real-world event rate, by the prior-shift correction: shifting every probability's log odds by the
difference between `trainingBaseRate` and `targetBaseRate` (both strictly between 0 and 1). Use it after training on a
rebalanced or sampled dataset, or when the production event rate differs from training, so the output can be read as
a true likelihood and compared against a cost-based threshold. Ranking and AUC are unchanged by this transform; only
calibration moves. Input probabilities must lie between 0 and 1; `NaN` entries stay `NaN` in the output array.

## Rates, information value and population stability (RateAndInformationValueTools)

**CalculateInformationValueByCategory** scores how well a categorical variable separates events from non-events with
weight of evidence (WoE) and information value (IV), the standard credit-risk and fraud-scorecard screen. For each
category it returns the counts, event rate, share of all events and of all non-events, and the WoE — the natural log
of the event share over the non-event share, positive meaning riskier than average. `TotalInformationValue` sums
`(event share - non-event share) * WoE` across categories, with the conventional rule of thumb: below 0.02 not
predictive, 0.02 to 0.1 weak, 0.1 to 0.3 medium, 0.3 to 0.5 strong, above 0.5 suspiciously strong (often a leak).
`smoothing` (default 0.5) is added to every category's event and non-event counts whenever any category has a zero
cell, so the logarithm stays finite; set it to 0 to disable and let a zero cell's WoE come back `NaN` instead. Rows
with a blank category or non-binary outcome are skipped and counted.

**CalculateInformationValueByBins** is the numeric-variable counterpart: it bins a continuous variable
(`EqualFrequency`, the default quantile binning, or `EqualWidth`, both from 2 to 100 bins) and scores the bins with
the same WoE/IV logic. `NaN` values form their own "Missing" bin rather than being dropped, so missingness itself can
carry information value. `WeightOfEvidenceIsMonotonic` reports whether risk rises or falls steadily across the bins in
order, which scorecard development usually requires.

**CalculatePopulationStabilityIndexFromCounts**, **CalculatePopulationStabilityIndexForNumericValues** and
**CalculatePopulationStabilityIndexForCategories** are three entry points to the same Population Stability Index
(PSI): the sum over bins of `(current share - baseline share) * log(current share / baseline share)`, with the shared
interpretation below 0.1 stable, 0.1 to 0.25 a moderate shift worth investigating, above 0.25 a significant shift.
`FromCounts` takes pre-binned baseline and current counts directly (at least two bins, each count non-negative).
`ForNumericValues` instead learns bin edges from the baseline sample only (`EqualFrequency` by default, 2 to 100 bins),
puts current values below or above the baseline's range into the first or last bin, and gives `NaN` values their own
Missing bin. `ForCategories` compares baseline and current label frequencies directly, with categories that appear in
either sample forming the bins, so a category that newly appears or disappears shows up as a large contribution to
the index. In every variant, a bin with an empty share is floored at `shareFloor` (default 0.0001) before the
logarithm, and each bin's own contribution is returned so the source of drift is visible, not just the headline
number.

**TabulateEventRateBySegment** tabulates a binary event rate (alert rate, fraud rate, default rate) across every value
of a categorical segment, ranked from highest to lowest rate. Each row carries its count, events, event rate with a
confidence interval (`method` selects Wilson by default, or Clopper-Pearson/Agresti-Coull/Jeffreys/Wald), share of all
rows and of all events, lift against the overall rate (2 means twice the average), and a z test of that segment
against every other row pooled together. `SmallSample` flags segments below `minimumCount` (default 30) as unreliable,
and a homogeneity chi-square across all segments (returned alongside the rows) tests whether the rate genuinely
differs by segment at all. Blank segment labels are skipped and counted.

**TabulateAlertHitRatesBySegment** summarises how an alerting rule or model performs per segment from one alert flag
and one confirmed-event flag per case: for every segment and overall it returns the alert rate (alerts over cases),
the hit rate (confirmed events among alerts, also called precision), the detection rate (confirmed events that were
alerted, also called recall), false positives per true hit, the count of missed events, and Wilson intervals for both
the alert and hit rates. Use it to find segments where alerts are mostly noise (low hit rate) versus segments where
real events slip through (low detection rate).

**CalculatePoissonRateConfidenceInterval** computes an exact Garwood confidence interval for a Poisson rate — alerts
per thousand accounts, cases per day — from an event count and the exposure it was observed over, in whatever unit the
exposure is supplied in. Zero observed events still give a finite, non-zero upper limit, which is the rate that cannot
yet be ruled out; this behaves far better than a normal approximation for rare-event rates.

**ComparePoissonRates** compares two Poisson rates (for example the alert rate before and after a rule change) from
each period's event count and exposure. It returns both rates, the rate ratio with an exact interval (above 1 means
group 1 is higher; when group 2 has no events the ratio and its upper limit are `NaN` because the ratio is unbounded),
the rate difference with a normal-approximation interval, and a two-sided p-value from the exact conditional binomial
test — falling back to a normal approximation, with `PValueMethod` saying so, only once the combined count exceeds
400,000.

**TestPoissonDispersion** tests whether a series of counts (alerts per day, say) behaves like a Poisson process using
the index of dispersion: the sum of squared deviations over the mean, chi-square distributed with `n - 1` degrees of
freedom under the null. `VarianceToMeanRatio` near 1 is consistent with Poisson; well above 1 with a small
`PValueOverdispersion` means bursty, clustered counts that a plain Poisson model or rate interval would understate;
below 1 with a small `PValueUnderdispersion` means unusually regular counts. A series with a zero mean or fewer than
two observations cannot be tested and returns `NaN` throughout with an explanatory `Interpretation`.

## Score ranking and threshold selection (ScoreRankingTools)

This is the largest single file in the group (15 tools) and underpins most model-evaluation and operating-point work.

**ComputeRocCurve** computes the ROC curve of a score against a 0/1 outcome, together with the area under it (AUC)
and its DeLong standard error and confidence interval. Each point gives a threshold (a case is flagged when its score
is at or above it), the false positive rate and the true positive rate there; the first point has a `NaN` threshold
and sits at the origin where nothing is flagged. AUC is the chance that a random event scores above a random
non-event — 0.5 is no skill, 1 is perfect, below 0.5 means the score is inverted — and `Gini` is `2 * AUC - 1`. The
result also returns the Kolmogorov-Smirnov maximum gap between the two rate curves and the Youden-optimal point
(sensitivity, specificity and threshold where `TPR - FPR` is largest). `maxPoints` thins a long curve for display
while AUC is always computed from every point; both classes must be present.

**ComparePairedRocAucDelong** compares the AUC of two different scores evaluated on the *same* cases, using the paired
DeLong test, which accounts for the correlation between the two scores (an unpaired comparison would overstate the
uncertainty). It returns both AUCs, their difference with a standard error, confidence interval, z statistic,
two-sided p-value and the correlation between the two scores; a small p-value means one score genuinely ranks better
than the other on this validation set. Rows where either score is `NaN` are dropped. It should not be used on scores
measured on different cases — that needs an unpaired comparison instead.

**ComputePrecisionRecallCurve** computes the precision-recall curve together with the average precision (the
step-wise area under the curve). Each point gives a threshold, the precision (share of flagged cases that are real
events) and the recall (share of events flagged); `AveragePrecision` should be read against `Prevalence` (the
precision of flagging everything), and this pairing is far more informative than ROC AUC when events are rare, as in
fraud and AML. `BestF1` and its threshold are returned alongside. At least one event is required.

**ComputeLiftGainTable** builds the classic decile (or n-group) lift and gain table: cases are ranked by score from
highest to lowest and cut into `groups` equal-sized bands (2 to 1000, default 10, group 1 holding the highest scores).
Each row gives the score range, count, events, event rate, lift (group rate over overall rate), the cumulative event
rate and lift, cumulative gain (share of all events captured so far) and the Kolmogorov-Smirnov gap between the
cumulative event and non-event shares at that point — read it as "reviewing the top 20 percent of cases finds X
percent of events." The maximum KS gap and the group it occurs at are returned at the top level too. Ties at a group
boundary are split by input order.

**ComputeKolmogorovSmirnovSeparation** measures separation with the Kolmogorov-Smirnov statistic directly — the
largest gap between the cumulative score distributions of the two classes — rather than deriving it from a lift
table. It returns the KS value (0 no separation, 1 perfect), the threshold where the gap is largest with the
sensitivity and false-positive rate there, AUC and Gini for context, and the mean and median score of each class. The
description's own rule of thumb: KS around 0.2 to 0.4 is typical of a usable fraud or credit score, and above about
0.7 is worth checking for leakage. Use the two-sample Kolmogorov-Smirnov hypothesis test (in the separate hypothesis
tests tool layer) for a p-value on the same statistic.

**ComputeThresholdSweep** evaluates a score at many cut-offs in one call and returns a row per threshold with the
confusion counts, alert rate, precision, recall, specificity, F1, F-beta, Youden's J, Matthews correlation and,
when `costFalseNegative`/`costFalsePositive` are both given, the expected cost per case. Give explicit `thresholds`
to inspect specific cut-offs, or leave them empty to get up to `maxThresholds` (default 20, max 1000) spread evenly
across the distinct scores observed. This is the tool to see the whole trade-off curve before choosing an operating
point; the `SelectThreshold*` tools below automate that choice.

**SelectThresholdMaximisingFBeta**, **SelectThresholdMaximisingYoudenIndex**, **SelectThresholdMaximisingMatthewsCorrelation**
and **SelectThresholdMinimisingCost** each search every distinct score as a candidate threshold and return the one
that optimises a different objective, all sharing the result shape `ThresholdSelectionResult` (`Objective`,
`Threshold`, `ObjectiveValue`, `Achievable`, `Note`, the full `Metrics` at that point, and, for the cost variant,
`BayesOptimalThreshold`/`ExpectedCostFlagNone`/`ExpectedCostFlagAll`). F-beta maximises the weighted harmonic mean of
precision and recall (`beta` above 1 favours recall, below 1 precision) but ignores true negatives entirely, which
suits rare-event problems but not cost-sensitive ones. Youden's J maximises `sensitivity + specificity - 1`, the point
on the ROC curve furthest from the chance diagonal, treating a missed event and a false alert as equally costly per
class regardless of how rare events are — which can flag many cases when events are rare. Matthews correlation
maximises a single balanced summary of the whole confusion matrix that stays honest under class imbalance.
Minimising cost takes explicit `costFalseNegative`/`costFalsePositive` (and optional `costTruePositive`/
`costTrueNegative`) and additionally reports the theoretical Bayes-optimal probability threshold
(`costFalsePositive / (costFalsePositive + costFalseNegative)`, meaningful only when the scores are calibrated
probabilities) plus the cost of flagging nothing and flagging everything for comparison; if flagging nothing is
cheapest, the returned threshold sits just above the highest observed score and `Note` says so. All four choose the
highest tied threshold, which flags the fewest cases, and all require both outcome classes present.

**SelectThresholdForTargetRecall** finds the *highest* threshold — and so the fewest alerts — that still reaches at
least `targetRecall`. **SelectThresholdForTargetPrecision** finds the *lowest* threshold that keeps precision at or
above `targetPrecision`; if no threshold reaches that target, `Achievable` is false and the highest-precision point
available is returned instead, with `Note` explaining why. **SelectThresholdForAlertBudget** finds the lowest
threshold whose alert count stays within a capacity budget, given as either `alertFraction` (a share of all cases) or
`alertCount` (an absolute number) — exactly one of the two must be supplied. Because tied scores are flagged together,
the realised alert count can land below the budget; if the single highest score is tied across more cases than the
budget allows, `Achievable` is false and the smallest possible alert set is returned.

**EvaluateTopKAlertQueue** answers, for a ranked queue, "if analysts review the k highest-scoring cases, how many real
events do they find?" for each of several `queueSizes` at once. Each size returns the true positives found, precision
and recall at that cut, lift (precision over the overall event rate) and the lowest score still included in the
queue. Every size must be between 1 and the number of usable cases; ties at the cut are broken by input order.

**CalculateOneVersusRestMulticlassAuc** evaluates a multi-class probability model's ranking quality one class at a
time: for each class, how well its probability column separates that class from every other class (one-versus-rest
AUC). It returns the AUC and support per class plus a macro average (every class weighted equally) and a
support-weighted average. A class that never occurs, or occurs in every row, gets `NaN` AUC and is excluded from both
averages; probabilities need not sum to 1 for this comparison, only be internally higher where that class is more
likely.

## Survival estimation (SurvivalEstimationTools)

Every tool in this file takes a `times`/`events` pair (time to event or censoring, and `1` if the event was actually
observed or `0` if the case was censored while still event-free) and handles right-censoring correctly throughout; a
row with a `NaN` time is dropped and counted, and a negative or infinite time is rejected outright.

**EstimateKaplanMeierSurvival** estimates the survival curve — the probability of still being event-free — with the
Kaplan-Meier product-limit method. Each row gives the time, the number at risk just before it, the events and
censorings at it, the survival probability, its Greenwood standard error, and a pointwise confidence band (`LogLog`
by default, which stays inside 0 and 1, or `Log`/`Plain`). The result also returns the median survival time (the
first time the curve drops to 0.5 or below, `NaN` when it never gets that low) with confidence limits, and the lower
and upper quartile times. `includeCensoringTimes` controls whether censoring-only times get their own row, and
`maxRows` thins a long curve while keeping the first and last rows.

**EstimateKaplanMeierByGroup** runs that same analysis separately for each distinct value of a `groups` array (for
example segment, channel or treatment arm) and compares the curves with a log-rank test. Each group gets its own
subject/event/censored counts, median survival with limits, quartile times, longest observation and, if
`evaluationTimes` are given, survival with a band read at those specific times. The overall log-rank chi-square and
p-value test whether the curves differ at all across the groups (`NaN` with only one group); a `NaN` median for a
group means fewer than half of it had the event within follow-up.

**EstimateNelsonAalenCumulativeHazard** estimates the cumulative hazard function H(t) — the accumulated risk of the
event up to time t — with the Nelson-Aalen estimator, allowing for censoring exactly as Kaplan-Meier does. Each row
gives the cumulative hazard, its standard error, a log-scale confidence band, and `exp(-H(t))` as an approximate
survival curve. The slope of H(t) is the hazard rate itself: a straight line means a constant hazard, a steepening
curve a rising one, which is the main reason to prefer this estimator to Kaplan-Meier when the shape of risk over
time matters.

**EvaluateSurvivalAtTimes** reads the Kaplan-Meier curve at specific times of interest — for example the share of
accounts still chargeback-free after 30, 90 and 180 days — returning, per requested time, the number still at risk,
survival with its standard error and band, the Nelson-Aalen cumulative hazard, and the cumulative probability of the
event (one minus survival). Before the first event, survival is exactly 1. A time beyond the longest observation
returns the last estimate as an extrapolation, which should not be trusted if the final cases were censored rather
than observed to fail — compare against the returned `MaximumObservedTime`.

**CalculateSurvivalTimeQuantiles** finds the times by which chosen proportions of the cohort have had the event, read
off the Kaplan-Meier curve with Brookmeyer-Crowley confidence limits. The proportion 0.5 gives the median survival
time; 0.9 gives the time by which 90 percent have had the event. A `NaN` time means the curve never drops far enough
within the observed period to reach that quantile; a `NaN` limit means the confidence band never crosses the level.

**CalculateRestrictedMeanSurvivalTime** computes the restricted mean survival time (RMST): the average event-free
time over the first `horizon` time units, equal to the area under the Kaplan-Meier curve from 0 to the horizon. Unlike
the median it is always defined and easy to state plainly — "on average an account stays clean for 160 of its first
180 days" — with a standard error, confidence interval and `LifetimeLost` (`horizon - RMST`, the average time lost to
the event within the horizon). `HorizonBeyondLastObservation` warns when the chosen horizon exceeds the longest
observation, in which case the curve is extrapolated as flat beyond it.

**CompareRestrictedMeanSurvivalTime** compares exactly two groups by RMST up to a shared horizon: each group's RMST,
their difference (first minus second, alphabetically) with standard error, confidence interval, z statistic and
p-value, plus the ratio. Because it needs no proportional-hazards assumption, it is a useful complement to the
log-rank test and a Cox model when the two survival curves cross rather than running cleanly parallel.

**EstimateMedianFollowUpTime** estimates how long a cohort has typically been observed, using the reverse
Kaplan-Meier trick: events and censoring swap roles, so the median of the resulting curve is the median follow-up
that would have been seen had nobody ever had the event. This is the accepted way to report follow-up duration when
events truncate observation early, since a plain median of observation times is biased downward. It returns that
reverse-KM median with limits alongside the plain median and the longest observation for comparison; `NaN` means the
reverse curve never falls to 0.5, which happens when too few cases were censored.

**TestLogRankSurvivalDifference** tests whether two or more groups share the same survival curve, comparing at every
event time the observed events per group against what would be expected under one shared hazard. `weighting` selects
the sensitivity profile: `LogRank` (default) is strongest against a constant hazard ratio, `GehanBreslow` and
`TaroneWare` emphasise early differences, `PetoPeto` emphasises early-to-middle differences, and `FlemingHarrington`
(with exponents `flemingHarringtonP`/`Q`) lets early or late differences be targeted explicitly. It returns per-group
observed and expected events, the chi-square with `groups - 1` degrees of freedom and its p-value, and, for exactly
two groups under `LogRank` weighting only, a one-step hazard ratio of the first group versus the second with
confidence limits.

## Survival and hazard models (SurvivalModelTools)

**FitCoxProportionalHazards** fits a Cox proportional hazards regression — how numeric covariates multiply the
instantaneous event risk over time, handling censoring correctly — without saving any model. The coefficient table
gives, per covariate, the log hazard ratio, standard error, Wald z and p-value, the hazard ratio (`exp(coefficient)`)
and its confidence interval: a hazard ratio of 1.5 means each extra unit of that covariate multiplies the event rate
by 1.5 at every time point, below 1 lowers it, and an interval containing 1 means no clear effect. It also returns the
partial log-likelihood, a likelihood-ratio test of the whole model against no covariates, Harrell's concordance index,
the Breslow baseline cumulative hazard and survival at the mean covariate profile, and — if `newCovariates` and
`predictionTimes` are supplied — each new row's relative hazard and survival probability at those times. Covariates
must be numeric (encode categories as 0/1 columns first) and none may be constant; the fit is capped at 10,000 rows
and 50 covariates, ties use the Breslow approximation, and `Warnings` flags non-convergence, fewer than 10 events per
covariate, or coefficients extreme enough to suggest complete separation or collinearity.

**CalculateHarrellConcordanceIndex** measures how well *any* risk score ranks cases by time to event, allowing for
censoring, independent of whether that score came from a Cox model. Among every comparable pair (the shorter-time
case had the event; a tie in time counts only when exactly one of the pair had the event), it counts how often the
higher-risk score belongs to the case that actually failed first — 0.5 is chance, 1 is perfect, pairs with equal
scores count half. `SomersD` (`2C - 1`) and the raw pair counts are returned alongside. It is capped at 20,000 rows
because every pair is examined.

**FitExponentialSurvivalModel** fits the simplest possible survival model: a constant hazard, by maximum likelihood,
where the rate is simply events divided by total time at risk. It returns the rate with an exact confidence interval,
the mean and median lifetime implied by that rate, survival with a band at any `evaluationTimes` requested, and
`MaximumAbsoluteDifferenceFromKaplanMeier` — the largest gap between this constant-hazard model and the non-parametric
Kaplan-Meier curve. A small gap (under roughly 0.05) supports the constant-hazard assumption; a large one means risk
genuinely changes over time and a model such as `FitCoxProportionalHazards` or a piecewise hazard
(`TabulateHazardRateByInterval`) is needed instead. With zero observed events the rate is 0 and the mean/median
lifetime are `NaN` because they are unbounded.

**CalculateIncidenceRatesByGroup** computes person-time incidence rates by group: for each group, the event count,
total time at risk, the event rate per unit time with an exact Poisson interval, and a rate ratio against a
`referenceGroup` (defaulting to the alphabetically first group) with its own exact interval and p-value. Use it to
compare event intensity across segments when follow-up lengths genuinely differ, for example chargebacks per
account-year rather than per account. A group with zero time at risk cannot be rated.

**TabulateHazardRateByInterval** estimates a piecewise-constant hazard — the event rate per unit time within
consecutive time windows defined by `breaks` (strictly increasing, starting at 0 or more, with a final open-ended
interval after the last value). Each interval reports entering subjects, events, censored cases, the person-time at
risk within it, the hazard rate with an exact Poisson interval, and the conditional event probability implied by
treating that rate as constant through the interval. A falling sequence of rates across intervals means early-life
risk; a rising sequence means wear-out.

**BuildActuarialLifeTable** builds a classical actuarial (life table) survival analysis over the same kind of fixed
`breaks`, the natural choice for grouped or very large data where exact event times are not retained. Each interval
reports subjects entering, those withdrawn (censored) during it, events, the effective number at risk (entering minus
half the withdrawals — the actuarial assumption that censoring happens evenly through the interval), the conditional
event probability with its standard error, cumulative survival at the interval's end with a Greenwood standard error,
and the midpoint hazard rate. Unlike Kaplan-Meier, which this file's other tools use, the life table trades the exact
censoring assumption for tolerance of coarse, grouped time data.

**CalculateCumulativeIncidenceCompetingRisks** estimates the cumulative incidence of each event type when several
mutually exclusive event types compete for the same subjects, using the Aalen-Johansen estimator: the probability
that a case has experienced event type k by time t, allowing for the chance it was removed first by a *different*
event type. Event-type codes are 0 for censored and 1, 2, 3 and so on for each competing event; every row gives the
number at risk, events of each type at that time, the overall event-free survival and the cumulative incidence per
type, which together with survival always sum to 1. The description makes the key point explicit: running
Kaplan-Meier on a single event type alone, ignoring the others, would overstate that type's probability, because it
implicitly treats removal by a competing event as if the subject had simply stayed at risk. No variance is returned
for this estimator.
