---
layout: default
title: Hypothesis Tests Tool Catalogue
nav_order: 10
parent: Reference
grand_parent: Analytical Methodology
---

# Hypothesis tests tool catalogue

Companion to [the hypothesis tests catalogue](../HypothesisTestsToolLayer.md), which describes this tool family by
textbook test name, its shared result shape and the correctness judgement calls made against the underlying
statistics library. This page is the per-tool index: every one of the 66 literal `[AdvancedAnalyticsTool("...")]`
identifiers under `Jube.Service/Agent/AdvancedAnalyticsTools/HypothesisTests`, with the inputs, outputs and
edge-case behaviour specific to that tool, so that a claim naming a tool by its exact callable name can be checked
against a real entry rather than only against the family's prose description.

## The shared result shape

Every tool below except the ANOVA, effect-size, power, interval, p-value-adjustment, p-value-lookup,
critical-value and recommendation tools returns the common `HypothesisTestResult` shape described in
[the tool layer page](../HypothesisTestsToolLayer.md): test name, statistic and its name, degrees of freedom where
applicable, p-value, tail, alpha, whether the null is rejected, the point estimate and null value where applicable,
an effect size with its name where one is defined, a two-sided confidence interval at 1-alpha where one is defined,
sample sizes, warnings and a one-sentence interpretation built from the numbers. The sections below describe only
what is specific to each tool: which statistic, which null hypothesis, which parameters and which edge cases. All
tools live in `Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests` and require the `Analytics` permission.

## Means and variances (`MeanAndVarianceTestTools`)

**`OneSampleTTest`** tests whether the mean of one numeric sample (at least 2 values, some spread) differs from,
exceeds or falls below a stated `hypothesizedMean` (default 0) using Student's t with n-1 degrees of freedom.
`EffectSize` is Cohen's d (mean minus the hypothesised value, divided by the sample standard deviation); the
confidence bounds are the two-sided interval for the mean at `1-alpha`. A sample under 10 values adds a warning
that the normality assumption cannot be checked reliably; identical values are rejected outright because the
variance, and so the statistic, is undefined.

**`OneSampleTTestFromSummary`** runs the same one-sample t test from a mean, a sample (n-1) standard deviation and
a count when the raw values are not at hand. Fields read exactly as `OneSampleTTest`; the standard deviation must
be strictly positive and the count at least 2.

**`TwoSampleTTest`** compares the means of two independent groups. `assumeEqualVariances` defaults to false, which
runs Welch's unequal-variance test (the safe default); true switches to Student's pooled test. The statistic is t
on `mean1 - mean2 - hypothesizedDifference`, `EffectSize` is Cohen's d from the pooled standard deviation, and
`Greater` means group 1 has the larger mean. Either group under 10 values adds a small-sample warning; if the
pooled test is requested and the variances differ by more than a factor of 4, a warning recommends Welch's test
instead; both groups being constant is rejected outright.

**`TwoSampleTTestFromSummary`** runs the same comparison from each group's mean, sample standard deviation and
count, with the same Welch/pooled choice and fields as `TwoSampleTTest`, for when only aggregate reports are
available.

**`PairedTTest`** tests whether the mean of paired differences (`sample1` minus `sample2`, matched element by
element, equal length required) differs from zero, for example before versus after on the same cases. `EffectSize`
is Cohen's d on the differences; `Greater` means the first array tends to be larger. Fewer than 10 pairs adds a
small-sample warning; identical differences across every pair (zero variance) are rejected.

**`OneSampleZTest`** tests a sample mean against `hypothesizedMean` using the normal distribution, intended for
samples of about 30 or more, or when `knownStandardDeviation` is genuinely known rather than estimated. Leaving
`knownStandardDeviation` empty uses the sample standard deviation and, below 30 observations, adds a warning
recommending `OneSampleTTest` instead.

**`TwoSampleZTest`** compares two independent large-sample means with the normal distribution, each group keeping
its own variance (no pooling), intended for both groups having roughly 30 or more observations; a warning
recommends `TwoSampleTTest` when either group is smaller. Both groups being constant is rejected.

**`TwoSampleZTestFromSummary`** runs the same comparison from each group's mean, standard deviation and count.

**`WaldZTestForEstimate`** tests any approximately normal estimate (a fitted coefficient, a difference, a log odds
ratio) against `hypothesizedValue` given its `standardError`, always two-sided: statistic is the Wald
z = (estimate - hypothesizedValue) / standardError. Use it when another tool has returned an estimate and a
standard error but no p-value of its own.

**`OneProportionZTest`** tests an observed proportion (`successes` out of `trials`) against `hypothesizedProportion`
(default 0.5) with the normal approximation under the null standard error. The confidence bounds are the Wilson
score interval at `1-alpha` rather than the plain Wald interval. A warning recommends `BinomialExactTest` when the
expected successes or failures under the null fall below 5.

**`TwoProportionZTest`** compares two independent proportions with the pooled two-proportion z test; `EstimatedValue`
is `proportion1 - proportion2` and `Greater` means group 1 has the higher rate. Every trial in both groups sharing
the same outcome (pooled variance zero) is rejected; a warning recommends `FisherExactTest` when an expected count
under the pooled proportion falls below 5 in either group.

**`BinomialExactTest`** tests `successes` out of `trials` against `hypothesizedProbability` (default 0.5) with the
exact binomial distribution, no normal approximation, so it stays valid for small counts or extreme proportions.
The confidence bounds are the Clopper-Pearson exact interval; the two-sided p-value sums every outcome no more
likely than the one observed.

**`BinomialExactTestFromOutcomes`** runs the same exact binomial test directly from an array of true/false
outcomes (true counts as a success), useful when the underlying data is a flagged/not-flagged list rather than a
pre-aggregated count.

**`FTestTwoVariances`** compares two independent sample variances with statistic = variance1/variance2 and degrees
of freedom n1-1 and n2-1; the confidence bounds are the interval for the true variance ratio at `1-alpha`. It is
sensitive to non-normal data, so a significant result on skewed or heavy-tailed data should be treated cautiously
in favour of `LeveneTest`.

**`FTestTwoVariancesFromSummary`** runs the same F test from each group's sample variance (n-1 denominator) and
count.

**`LeveneTest`** tests whether two or more groups (at least 2 groups, at least 2 values each) have equal variances,
robust to non-normal data. `centre` chooses the deviation reference: `Mean` is the original Levene test, `Median`
(the default) is the Brown-Forsythe variant recommended for skewed data, and `TrimmedMean` drops `trimFraction`
(default 0.1, must stay below 0.5) from each tail before centring. Statistic is F with degrees of freedom
groups-1 and N-groups; the tail is always `Greater` because only large F values indicate unequal variance.

**`BartlettTest`** tests the same equal-variance hypothesis assuming every group is normally distributed; it is
more powerful than `LeveneTest` on normal data but unreliable on skewed data. Statistic is chi-square with
groups-1 degrees of freedom; every group needs some variation or the test is rejected outright.

**`OneWayAnova`** tests whether three or more independent groups (at least 2 groups of at least 2 values; the
tool itself does not enforce a minimum of 3 but is described for that case) share the same mean. It returns a
dedicated `HypothesisAnovaResult`, not the common test shape: `FStatistic` with `DegreesOfFreedomBetween`
(groups-1) and `DegreesOfFreedomWithin` (N-groups), `EtaSquared` (share of total variance explained by group
membership; about 0.01 small, 0.06 medium, 0.14 large), the full sum-of-squares `Table` for between, within and
total, each group's mean and size, and a warning when group sizes are unbalanced by more than a factor of 2 (the
equal-variance assumption then matters more).

**`OneWayAnovaFromLabels`** runs the same one-way ANOVA from one long `values` array plus a parallel `labels` array
of whole-number group codes, grouping values that share a label; it needs at least two distinct labels and reuses
the `OneWayAnova` fields and logic.

**`TwoWayAnova`** tests the effect of two factors and their interaction on a numeric outcome from a balanced
design: one `values` array plus zero-based `factorALabels` and `factorBLabels`, every combination of levels
needing the same replicate count (at least 2). `model` chooses `FixedEffects` (default, tests every effect against
the error mean square), `RandomEffects` (both factors tested against the interaction) or
`FactorAFixedFactorBRandom` (the mixed model); the tool deliberately remaps the underlying library's own, swapped,
enum values onto these textbook-correct labels (documented as a correctness fix on the family's conceptual page).
It returns a dedicated `HypothesisTwoWayAnovaResult` with an F statistic, p-value and partial eta squared for
factor A, factor B and the interaction separately, plus level counts and the replicate count; an unbalanced design
(any cell with a different replicate count from cell [0,0]) is rejected with the offending combination named.

**`GrubbsOutlierTest`** tests for a single outlier in a sample (at least 3 values, some spread) assumed normal.
`side` chooses `Either` (default, tests whichever extreme is more extreme), `Maximum` or `Minimum`. Statistic G is
the largest standardised deviation from the mean; run it repeatedly, removing one outlier at a time, to find
several. The tail is always `Greater`.

**`EffectSizeTwoSamples`** computes three standardised effect sizes for the difference between two independent
groups from their raw values, returning a dedicated `HypothesisEffectSizeResult` rather than a test result: Cohen's
d (pooled standard deviation), Hedges' g (d with a small-sample bias correction) and Glass's delta (mean
difference over group 2's own standard deviation, for when group 2 is a control). `Magnitude` labels the absolute
size negligible/small/medium/large on the usual 0.2/0.5/0.8 thresholds; positive values mean group 1 has the
higher mean. Both groups being constant is rejected.

**`EffectSizeFromSummary`** computes the same three effect sizes from each group's mean, sample standard deviation
and count, for when only summary statistics are available.

## Ranks and distributions (`RankAndDistributionTestTools`)

**`MannWhitneyUTest`** (the Wilcoxon rank-sum test) compares two independent groups without assuming normality.
Statistic U1 counts pairs where group 1's value exceeds group 2's, ties counting as half; `EstimatedValue` and
`EffectSize` are both U1/(n1*n2), the probability of superiority (0.5 is no difference). `exact` lets the method
choose (default), forces the exact distribution (only valid without ties) or forces the normal approximation;
`adjustForTies` (default true) applies the tie correction. A warning notes when ties forced the normal
approximation, and another when either group has fewer than 8 values.

**`WilcoxonSignedRankTest`** tests whether a sample's median differs from `hypothesizedMedian` (default 0) without
assuming normality but assuming a symmetric distribution; passing the differences of a paired design directly
tests that the median difference is zero. Values equal to the hypothesised median are dropped before ranking,
and a sample that is entirely equal to the hypothesised median is rejected. Statistic is W+, the sum of ranks of
the values above the hypothesised median; a warning flags the normal approximation when ties, zeros or a large
sample force it, and another flags that zero differences were dropped.

**`WilcoxonSignedRankPairedTest`** runs the paired, non-parametric counterpart to `PairedTTest`: differences are
`sample1` minus `sample2` element by element, zero-difference pairs dropped, and every pair being identical is
rejected. Statistic is W+, the sum of ranks of the positive differences; `Greater` means the first array tends
larger.

**`SignTestOneSample`** tests whether a sample's median differs from `hypothesizedMedian` by counting only how many
values lie above versus below it and applying the exact binomial distribution; values equal to the median are
dropped and a sample entirely equal to the median is rejected. It makes the fewest assumptions of any location
test but has the least power. Per the family's conceptual page, this tool corrects a convention bug in the
underlying library by using the tail directly rather than its inverted-tail convention.

**`SignTestPaired`** runs the same sign test on paired data, counting in how many pairs the first value exceeds the
second (tied pairs dropped, every pair tied is rejected) and testing that share against one half.

**`SignTestFromCounts`** runs the sign test directly from `positiveCount` out of `totalCount` non-tied comparisons,
for when the comparisons are already summarised rather than available as raw pairs.

**`KolmogorovSmirnovOneSampleTest`** tests whether a sample follows a fully specified reference distribution:
`distribution` chooses `Normal` (mean, standard deviation), `Uniform` (minimum, maximum), `Exponential` (rate) or
`LogNormal` (location, shape of the log) via `parameter1`/`parameter2`. Statistic D is the largest vertical gap
between the empirical and reference distribution functions. The tool's own description warns against fitting the
reference parameters from the same sample and then calling it, which inflates the p-value; use
`LillieforsNormalityTest` or `ShapiroWilkNormalityTest` for normality with estimated parameters instead.

**`KolmogorovSmirnovTwoSampleTest`** tests whether two independent samples come from the same continuous
distribution, sensitive to any difference in location, spread or shape, with statistic D the largest gap between
the two empirical distribution functions (0 identical, 1 fully separated); ties reduce accuracy since the test
assumes continuous data.

**`ShapiroWilkNormalityTest`** is the most powerful general test that a sample (at least 4 values, some spread)
came from some normal distribution; statistic W lies in (0, 1) and is close to 1 for normal data. Works well up to
a few thousand observations; with very large samples trivial departures become significant, so the family's
conceptual page recommends also checking skewness and a plot. The tail is `Less` because a small W, not a small
p-value from a large statistic, signals non-normality.

**`AndersonDarlingNormalityTest`** tests normality with extra weight on the tails (at least 8 values, some spread),
making it a good check for heavy tails relevant to outlier and extreme-value analysis; mean and standard deviation
are estimated from the sample itself. Statistic A2 grows as the tails depart more from normal; tail is `Greater`.

**`LillieforsNormalityTest`** is a Kolmogorov-Smirnov test whose p-value is corrected, by Monte Carlo simulation,
for the mean and standard deviation being estimated from the same sample (at least 5 values, some spread).
`iterations` (default 10000, must be between 1000 and 200000) controls the simulation; because the p-value is
simulated it can vary slightly in the third decimal between calls, which is reported as a standing warning.

**`JarqueBeraNormalityTest`** builds its statistic from skewness and excess kurtosis: JB = n/6 * (skewness^2 +
kurtosis^2/4), compared with chi-square at 2 degrees of freedom; `EstimatedValue` is the sample skewness. The
library has no native Jarque-Bera test, so this is an independently composed statistic (noted as a correctness
item on the conceptual page); it needs at least 4 values and some spread, is reliable only for large samples, and
below 30 values a warning recommends `ShapiroWilkNormalityTest` instead. The detail text names whether skewness or
kurtosis drives the result.

**`ChiSquareDistributionFitTest`** tests a continuous sample (at least 20 values) against a fully specified
reference distribution (same four choices as `KolmogorovSmirnovOneSampleTest`) using equal-probability bins sized
from the sample count, with sparse bins merged; degrees of freedom are bins minus 1. It is less powerful than the
Kolmogorov-Smirnov and Anderson-Darling tests for continuous data but is the classic choice for binned checks, and
again the reference parameters must be fixed in advance, not fitted from the same sample.

**`KruskalWallisTest`** is the non-parametric counterpart to `OneWayAnova`: all values across the supplied groups
(at least 2 groups, at least 1 value each) are ranked together and statistic H, with a tie correction, is compared
with chi-square at groups-1 degrees of freedom; the library has no native Kruskal-Wallis test, so H is computed
directly from ranks and tie-corrected by the tool (another documented correctness item). `EffectSize` is epsilon
squared, H/(N-1); a group with fewer than 5 values triggers a warning that the chi-square approximation is rough,
and every value in every group being identical is rejected outright.

## Categorical and agreement (`CategoricalAndAgreementTestTools`)

**`ChiSquareGoodnessOfFitTest`** tests whether observed category counts match expected proportions (even split by
default, or `expectedProportions` scaled to the observed total); `estimatedParameters` reduces the degrees of
freedom (categories - 1 - estimatedParameters) when some of those proportions were themselves fitted from the
data. `EffectSize` is Cohen's w = sqrt(chi-square/n); a warning lists when any expected count falls below 5, where
the chi-square approximation becomes unreliable and sparse categories should be merged.

**`ChiSquareIndependenceTest`** tests independence of the row and column categories in a contingency `table` of
counts (at least 2 rows and 2 columns); `EffectSize` is Cramer's V (equal to phi for a 2x2 table).
`yatesCorrection` applies the continuity correction but only on a genuine 2x2 table. A warning names the smallest
expected count when it falls below 5 and recommends `FisherExactTest` for 2x2 tables or merging categories
otherwise; a row or column summing to zero is rejected.

**`FisherExactTest`** is the exact test for a 2x2 `table` [[a, b], [c, d]], valid at any sample size including
tiny or lopsided tables where the chi-square approximation fails. Statistic is the top-left count a;
`EstimatedValue` is the sample odds ratio (a*d)/(b*c), reported as empty when a zero cell makes it infinite (with
a warning). The two-sided p-value sums every table arrangement no more likely than the one observed.

**`McNemarTest`** tests paired binary outcomes from a 2x2 `table` [[both yes, first yes only], [second yes only,
both no]], using only the discordant cells b and c: statistic (b-c)^2/(b+c), or the Yates-corrected
(|b-c|-0.5)^2/(b+c) when `yatesCorrection` is true, at 1 degree of freedom. No discordant pairs at all (b+c=0) is
rejected as undefined; fewer than 25 discordant pairs triggers a warning recommending the exact
`BinomialExactTest` on b out of b+c against probability 0.5 instead.

**`BowkerSymmetryTest`** generalises McNemar's test to a square `table` (at least 2x2) of paired categorical
ratings, testing whether the probability of moving from category i to j equals the probability of moving from j
to i; statistic is chi-square with k(k-1)/2 degrees of freedom. A pair of categories that never exchange members
in either direction (both off-diagonal cells zero) makes that pair's contribution undefined and is rejected with
the offending category pair named.

**`StuartMaxwellHomogeneityTest`** tests marginal homogeneity (do the row and column ratings share the same
overall category distribution) for a square `table` of at least 3x3, the multi-category generalisation of
McNemar's test focused on the margins rather than cell-by-cell symmetry; statistic is chi-square with k-1 degrees
of freedom. A table with a zero row matched by a zero column in the same position makes the covariance matrix
singular and is rejected.

**`BhapkarHomogeneityTest`** tests the same marginal-homogeneity hypothesis as `StuartMaxwellHomogeneityTest`, with
the same table shape and degrees of freedom, using a statistic that is asymptotically more powerful.

**`CohensKappaTest`** measures and tests agreement between two raters classifying the same cases into the same
categories (a square `table` of at least 2x2), correcting for chance agreement. `EstimatedValue` is Cohen's kappa
(1 perfect, 0 chance-level, negative worse than chance); with `hypothesizedKappa` left at its default 0 the
p-value uses the standard error under no agreement, otherwise the general standard error is used, and the default
tail is `Greater` (testing for agreement above the hypothesised value) rather than two-sided. The detail text
names the Landis-Koch band (slight, fair, moderate, substantial, almost perfect); a table where either rater used
only one category makes kappa undefined and is rejected.

**`WeightedKappaTest`** extends `CohensKappaTest` to ordered categories, where near misses count as partial
agreement: `weighting` is `Linear` (credit falling in proportion to category distance) or `Quadratic` (default,
penalising large disagreements more heavily and matching the intraclass correlation). Categories must already be
in their natural order in the table. Same undefined-kappa rejection as `CohensKappaTest` applies.

**`KappaComparisonTest`** tests whether the Cohen's kappa achieved in two independent tables differs, for example
two pairs of analysts or two periods; statistic is a z score for the absolute kappa difference and the test is
always two-sided. `EstimatedValue` is kappa1 - kappa2; either table having an undefined kappa (a rater using only
one category) is rejected.

**`KappaHomogeneityTest`** tests whether several independent square tables (at least 2, each at least 2x2, sizes
may differ) share the same kappa, weighting each table's kappa by the inverse of its variance. Statistic is
chi-square with tables-1 degrees of freedom; `EstimatedValue` is the variance-weighted overall kappa. A small
p-value means agreement differs between tables and they should not be pooled.

**`KappaGroupDifferenceTest`** compares the average kappa of two groups of tables (each group at least 2 square
tables) with a two-sample t test on the per-table kappas; `assumeEqualVariances` (default true) chooses pooled or
Welch variance, `EstimatedValue` is the mean kappa of group 1 minus group 2, and `Greater` means group 1 agrees
more.

**`AucSignificanceTest`** tests whether a scoring model's ROC AUC differs from `hypothesizedAuc` (default 0.5, no
better than chance) using DeLong's variance; needs at least 2 positives and 2 negatives among the supplied
`labels`. `EstimatedValue` is the exact placement-value AUC (probability a random positive scores above a random
negative, ties counting half) rather than the library's own discretised and tail-flipped ROC statistic — a
documented correctness fix on the conceptual page. A DeLong standard error of zero (perfect separation or
identical scores) is rejected as undefined; an AUC below 0.5 is reported as-is, meaning the score is inversely
related to the label.

**`AucComparisonTest`** compares the AUCs of two scoring models evaluated on the same cases (same labels, same case
order) using DeLong's method for correlated curves; `EstimatedValue` is AUC1 - AUC2 and `Greater` means model 1
ranks better. It must be run on identical cases, not independently; a zero or negative combined variance (both
models ranking the cases identically or perfectly) is rejected as undefined.

## Power, intervals and corrections (`PowerAndIntervalTools`)

**`OneSampleTTestPowerAnalysis`** and **`OneSampleZTestPowerAnalysis`** plan a one-sample (or paired) t test or
z test: `solveFor` chooses `Power` (needs `effectSize` and `sampleSize`), `SampleSize` (needs `effectSize` and
`power`, rounded up to a whole number with the achieved power then reported) or `EffectSize` (needs `sampleSize`
and `power`, the smallest standardised effect the test can detect). `effectSize` is Cohen's d — the true
difference divided by the standard deviation (0.2/0.5/0.8 small/medium/large). `tail` of `Less` is analysed as its
mirror-image `Greater` with the effect size taken as a magnitude.

**`TwoSampleTTestPowerAnalysis`** and **`TwoSampleZTestPowerAnalysis`** run the same three-way solve for a
two-independent-mean comparison: `sampleSize1` plus either `sampleSize2` or `allocationRatio` (group 2 size
divided by group 1 size, default 1 for equal groups, capped at 1000) determine group 2's size when not solving for
`SampleSize`; when solving for `SampleSize`, both group sizes are rounded up and returned.

**`MeanConfidenceInterval`** returns Student's t interval for a population mean (mean plus or minus t times the
standard error) at `confidenceLevel` (default 0.95); needs at least 2 values, and constant data give a zero-width
interval rather than an error.

**`ProportionConfidenceInterval`** returns a confidence interval for a proportion from `successes` out of
`trials`. `method` is `Wilson` (default, accurate near 0 or 1), `ClopperPearson` (conservative exact),
`AgrestiCoull` (simple good approximation) or `Wald` (the textbook interval, offered for comparison but flagged
with a warning as poor when either count is below 10). Bounds are clipped to 0 and 1.

**`VarianceConfidenceInterval`** returns a chi-square-based interval for a population variance, or for the
standard deviation when `forStandardDeviation` is true; it assumes normal data, is wide for small samples (a
warning appears below 30 observations), and needs at least 2 values with some spread.

**`MedianConfidenceInterval`** returns a distribution-free interval for the population median built from order
statistics and the exact binomial distribution, so no normality assumption is needed. The bounds are actual
observed values, so `AchievedConfidenceLevel` is at least the requested level and can be higher; when the sample is
too small to reach the requested level at all, the widest possible interval (sample minimum to maximum) is
returned with a warning rather than an error.

**`AdjustPValuesBonferroni`** multiplies each p-value by the test count (capped at 1), simple and conservative,
best for a small family of tests.

**`AdjustPValuesHolm`** applies the Holm step-down correction: p-values are ordered, the smallest multiplied by m,
the next by m-1 and so on, kept non-decreasing; it controls the same family-wise error rate as Bonferroni but is
never less powerful.

**`AdjustPValuesBenjaminiHochberg`** controls the false discovery rate (the expected share of false positives among
tests declared significant) rather than the chance of any false positive, intended for screening many tests (for
example thousands of features or rules) where a few false discoveries are tolerable; it is far more powerful than
Bonferroni or Holm in that setting.

**`AdjustPValuesByMethod`** exposes the same correction family with an explicit `method` choice spanning
Bonferroni, Sidak (exact for independent tests), Holm, Hochberg (step-up, valid for independent or positively
dependent tests), BenjaminiHochberg or BenjaminiYekutieli (false discovery rate valid under any dependence, more
conservative than Benjamini-Hochberg); the library has none of these natively, so each is an independently
composed and tested implementation (a correctness item on the conceptual page).

**`PValueFromTestStatistic`** converts an already-known test statistic into a p-value against a standard
reference distribution: `StandardNormal`, `StudentT` and `ChiSquare` (each needing `degreesOfFreedom`) or `F`
(needing both `degreesOfFreedom` and `degreesOfFreedom2`). `TwoSided` doubles the smaller tail; for `ChiSquare` and
`F` the conventional test direction is `Greater`.

**`CriticalValueForSignificanceLevel`** returns the critical value(s) beyond which a test statistic would reject
the null, for the same four reference distributions as `PValueFromTestStatistic`; `TwoSided` returns both bounds,
`Greater` only the upper and `Less` only the lower, with the unused bound left empty.

## Test selection (`HypothesisTestSelectionTools`)

**`RecommendHypothesisTest`** chooses which of the tools above to call from plain facts about the question and the
data, needing no statistical reasoning from the caller. `design` names the shape of the question (one sample
against a value, two independent groups, paired data, three or more groups, two-way factorial, association of two
categorical variables, agreement between two raters, comparing variances, normality, or distribution fit) and
`dataKind` names the measurement type (`Continuous`, `Ordinal`, `Binary` or `Categorical`); optional hints
(`dataLookNormal`, `smallestGroupSize`, `variancesEqual`, `groups`) refine the choice, for example routing a small
or non-normal continuous two-group comparison to `MannWhitneyUTest` rather than `TwoSampleTTest`, or a small
2x2 categorical association to `FisherExactTest` rather than `ChiSquareIndependenceTest`. The result's
`PrimaryTool` is the exact tool name to call next, `AlternativeTools` are sensible substitutes, `Reason` explains
the choice in plain language, `Assumptions` lists what must hold, and `CompanionTools` names checks or effect-size
tools worth running alongside (for example `LeveneTest` and `ShapiroWilkNormalityTest` before `OneWayAnova`).
