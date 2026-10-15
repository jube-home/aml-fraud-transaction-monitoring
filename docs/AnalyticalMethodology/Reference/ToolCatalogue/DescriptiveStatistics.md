---
layout: default
title: Descriptive Statistics Tool Catalogue
nav_order: 8
parent: Reference
grand_parent: Analytical Methodology
---

# Descriptive statistics tool catalogue

`Jube.Service/Agent/AdvancedAnalyticsTools/DescriptiveStatistics/` is one of the ten semantic tool groups an AskJooby
agent calls directly by name, covering the most basic layer of the statistics toolset: central tendency, spread,
shape, quantiles, frequency tables, running and rolling series statistics, table-wide summaries and simple
transforms. 83 of its 97 tools have no entry anywhere else under `docs/`, which means Jadek, the reply-validating
agent that fact-checks every answer against the RAG-indexed corpus, can never confirm a claim that cites one of them,
even when the answer is correct. This page covers all 97 tools tool by tool, grouped by the source file each tool
lives in. Every tool is bound to the group name `DescriptiveStatistics` and gated behind the `Analytics` permission;
none of them persist anything.

Fourteen tools in this group turn out to already be documented in depth elsewhere, each with a dedicated paragraph
explaining what it computes rather than a bare name-check, because they are used as the "AdvancedAnalytics
equivalent" of a step in the R walkthrough at
[Summary Statistics and Basic Plots in R](../../SummaryStatistics.md), in the charting walkthrough at
[Charting](../../Charting.md), or in the Monte Carlo simulation walkthrough at
[Monte Carlo Simulation](../../PrescriptiveAnalytics/MonteCarloSimulation.md). This page does not duplicate those
write-ups; it cites them and covers the other 83 tools in full below, organised by the same four source files the R
guide's citations already use (`CentralTendencyTools`, `QuantileRankAndFrequencyTools`, `SpreadAndShapeTools`,
`TableAndTransformTools`).

## Already documented elsewhere

- **Mean**, **Median** and **MinimumMaximumRange** (`CentralTendencyTools`) — [Summary Statistics](../../SummaryStatistics.md),
  Procedures 2 and 4.
- **FiveNumberSummary** (`QuantileRankAndFrequencyTools`) — [Summary Statistics](../../SummaryStatistics.md), Procedure 3.
- **FrequencyTableOfCategories** (`QuantileRankAndFrequencyTools`) — [Charting](../../Charting.md), the `table()`
  equivalent.
- **Variance**, **StandardDeviation**, **InterquartileRange**, **Skewness**, **ExcessKurtosis** and **Kurtosis**
  (`SpreadAndShapeTools`) — [Summary Statistics](../../SummaryStatistics.md), Procedures 3, 7 and 10.
- **DescribeColumn**, **SummariseColumns** and **ZScores** (`TableAndTransformTools`) — [Summary Statistics](../../SummaryStatistics.md),
  Procedures 7 and 8, and [Monte Carlo Simulation](../../PrescriptiveAnalytics/MonteCarloSimulation.md).

A convention shared by almost every tool below: a `bool ignoreMissing` parameter (default `false` unless stated
otherwise) controls whether a `NaN` in `values` is dropped and counted in the result's `MissingIgnored`/`MissingCount`
field, or rejected outright as malformed input. Series-shaped tools (the cumulative, rolling and transform tools)
instead let a `NaN` pass through unchanged at the same position in the output rather than dropping it, so the series
stays the same length as the input; this is called out per tool below as "missing entries stay NaN."

## Central tendency, extremes and location (CentralTendencyTools)

**WeightedMean** computes the arithmetic mean with each value counted in proportion to a supplied non-negative
`weights` array of the same length — a value weighted 0 contributes nothing, and the weights need not sum to 1; they
must sum to something above 0.

**TrimmedMean** discards the lowest and highest `trimFraction` share of the sorted values from *each* tail (0 up to
but excluding 0.5, default 0.1) before averaging what remains, a middle ground between the ordinary mean's full use
of the data and the median's full resistance to outliers.

**WinsorisedMean** is the same idea but replaces, rather than deletes, the extreme `winsorFraction` share in each tail
with the nearest retained value (default 0.05 per tail), which limits outlier influence while keeping the original
sample size.

**GeometricMean** computes the n-th root of the product via logarithms, for growth factors, ratios and other
multiplicative quantities; every value must be strictly positive, since a zero or negative value would make the
result zero or undefined.

**HarmonicMean** computes the count divided by the sum of reciprocals — the correct average for rates and ratios over
a fixed quantity, such as speeds over equal distances or price-to-earnings ratios — and is never larger than the
geometric mean. Values must be strictly positive.

**QuadraticMean** (root mean square) is the square root of the mean of the squares, the typical magnitude of values
that can be positive or negative, such as residuals or signal amplitude; negative values are allowed and the result
is always at least the absolute value of the arithmetic mean.

**PowerMean** generalises all of the above to the p-th root of the mean of values raised to power `order`: order 1 is
the arithmetic mean, 2 the quadratic mean, -1 the harmonic mean, and 0 (handled as a special case) the geometric mean;
larger orders weight large values more heavily. Values must be strictly positive unless `order` is a positive even
integer, in which case negative values are safe because they are squared away.

**ContraharmonicMean** (the Lehmer mean) is the sum of `x^order` over the sum of `x^(order-1)` (default `order` 2,
the classic contraharmonic mean, up to 20); because it weights large values by themselves it is always at least the
arithmetic mean, and it answers "what is the mean group size seen by a randomly chosen member" — the sum of the
values must not be zero.

**ExponentialWeightedMean** computes a recency-weighted mean of a time-ordered series in which the most recent
observation (the last element) carries the greatest weight and older observations decay geometrically, controlled by
a smoothing factor `alpha` strictly between 0 (exclusive) and 1 (inclusive, default 0.3): near 1 it tracks the latest
values closely, near 0 it approaches the plain mean.

**GrandMean** combines several *already-computed* group means into one overall mean, weighting each by its own
`sampleSizes` — use it when only per-group summaries, not the raw data, are available; every sample size must be at
least 1 and no group mean may be `NaN`.

**Trimean** is Tukey's robust location estimate, `(Q1 + 2*median + Q3) / 4`, blending the median with the quartiles
(computed by linear interpolation) so it copes better with skewed data than the median alone.

**Midhinge** is the average of the lower and upper quartiles; comparing it with the median shows asymmetry in the
middle of the distribution specifically, since a large gap between them signals the two quartiles are not
symmetric around the median.

**Minimum** and **Maximum** return the smallest and largest value respectively, each with `Count` and
`MissingIgnored`.

**ArgumentOfMinimum** and **ArgumentOfMaximum** locate *where* the extreme value sits rather than just its value: each
returns the zero-based `Index` of the first occurrence (ties broken by position) in the *original* array together
with the `Value` there, so the record behind an extreme low or high can be traced back even when `ignoreMissing`
has dropped other rows — the index returned always refers to the original, unfiltered array.

**Sum** totals a list of numbers for aggregate amounts; an overflow beyond roughly 1.8e308 returns infinity rather
than throwing.

**Product** multiplies a list of numbers, for example compounding growth factors; very long lists can overflow to
infinity or underflow to 0, in which case `GeometricMean` or working on logarithms directly is the safer choice.

**CountValues** profiles data quality before any statistic that would fail on gaps: total, valid (non-missing),
missing, infinite, distinct, zero, positive and negative counts, plus `MissingFraction` (missing over total). It
takes no `ignoreMissing` flag because it is designed to run on data containing `NaN`.

**ModeOfNumbers** returns every value tied for the highest frequency in `Modes` (ascending), the shared `Frequency`,
and `IsUnique` (false when more than one mode exists). For genuinely continuous data where every value differs, every
value is its own mode with frequency 1, so the description recommends binning first in that case.

**ModeOfCategories** is the same idea for text labels, compared exactly and case-sensitively; an empty string is a
valid label in its own right, while `null` is treated as missing when `ignoreMissing` is set.

**WeightedMode** finds the value with the largest *total weight* rather than the largest count, with ties again
returned together in `Modes`; `Frequency` here is the summed weight rounded to the nearest whole number, so `Count`
(not `Frequency`) is the field to read for the actual number of observations. Weights must be finite and
non-negative.

**CircularMean** computes the mean direction of cyclical data — hour of day, day of week, month, compass bearing —
where, for example, 23:00 and 01:00 are close together even though an ordinary mean of the hours 23 and 1 gives 12.
The cycle length is set by `unit` (`Radians`, `Degrees`, `Hours24` default, `Days7` or `Months12`); the result is
`NaN` when the directions cancel out exactly (resultant length near 0), which `CircularSummaryStatistics` below makes
visible.

**CircularSummaryStatistics** gives a fuller cyclical profile: mean direction, median, mean resultant length (0 to 1,
near 1 meaning values cluster at one time of the cycle, near 0 meaning they spread evenly around it), circular
variance (`1 - resultant length`), circular standard deviation, angular deviation, concentration, skewness and
kurtosis. Use it to test whether activity concentrates at particular times rather than spreading evenly through the
cycle; directions come back in the chosen unit while the spread measures are in radians.

## Quantiles, ranks and frequency tables (QuantileRankAndFrequencyTools)

**Quantile** returns the value below which a given cumulative `probability` (0 to 1) of the data falls — for example
0.9 for the 90th percentile — interpolated between neighbouring sorted values by the chosen `interpolation` rule
(`Linear`, the spreadsheet default, or `Weibull`, used by some statistics packages).

**Quantiles** returns several such quantiles from one call, one per entry of a `probabilities` array, in the same
order; **Percentiles** is the same thing expressed as percent levels from 0 to 100 (more convenient for reporting)
rather than 0-to-1 probabilities.

**EqualProbabilityCutPoints** returns the `parts - 1` interior cut points that divide the data into `parts` (2 to
1000) equal-sized groups — 4 for quartiles, 10 for deciles, 100 for percentiles — together with the matching
probabilities, for binning a variable into equal-frequency bands.

**InterpercentileRange** is the distance between two arbitrary percentiles, such as the 10th-to-90th range, which
ignores the most extreme tails while remaining a flexible, outlier-resistant spread measure; `lowerPercent` must be
below `upperPercent`, both between 0 and 100.

**PercentileRankOfValue** finds where a single `score` sits within a reference `values` array, as a percentile rank
from 0 to 100 (the percent of observations below it) — for example, stating that a transaction is larger than 97
percent of a customer's history. `kind` controls how ties at the exact score are handled: `Weak` counts values at or
below the score, `Strict` only values strictly below, `Mean` (the default) averages the two, which is the usual
choice when ties matter. The score need not itself appear in the reference data.

**PercentileRanksOfValues** applies that same ranking to *every* value against the rest of its own array in one call,
in the original order, converting a skewed variable onto a uniform 0-to-100 scale, or scoring each observation
relative to its peers; a `NaN` entry stays `NaN` in the output and is excluded from the reference set used to rank
everyone else.

**RanksOfValues** returns the rank of each value (1 is smallest, unless `descending` is set) with a choice of tie
handling (`ties`): `Average` (the standard for rank-based statistics, giving tied values the mean of their ranks),
`Minimum`, `Maximum`, `Dense` (no gaps between successive ranks) or `Ordinal` (a distinct rank per position even
among ties). `HasTies` and `TiedGroups` report whether and how much tying occurred; `NaN` entries stay `NaN` and are
excluded from ranking.

**ThresholdShare** counts and proportions observations above, below and equal to a `threshold` — for example the
share of transactions over a reporting limit — with proportions computed relative to the non-missing count, useful
for exceedance rates and simple rule-hit-rate estimates.

**CumulativeSum**, **CumulativeProduct**, **CumulativeMaximum** and **CumulativeMinimum** each return a running
statistic over a series in original order: element i is, respectively, the sum, product, maximum or minimum of
elements 0 through i (a high- or low-water mark for the latter two). `CumulativeProduct` is for compounding growth
factors into a cumulative index and can overflow to infinity or underflow to 0 on a long series. In all four, a `NaN`
entry stays `NaN` at its own position and is skipped when computing the values around it.

**RollingStatistic** computes a chosen statistic (`Mean`, `Sum`, `Median`, `Minimum`, `Maximum`, `StandardDeviation`,
`Variance`, `Range`, `Skewness` or `Count`) over a trailing `window` of the last N observations at every position —
a moving average or moving volatility of a time-ordered series. Position i uses observations `i - window + 1`
through `i`; `NaN` values inside a window are skipped, and the output stays `NaN` until at least `minPeriods` valid
values are available (0 or less means the full window) or until the chosen statistic has enough values to be
defined at all (standard deviation and variance need 2, skewness needs 3). The combination of series length and
window is capped to bound the work done.

**ExpandingStatistic** is the same set of statistics computed over an *expanding* window — everything seen so far at
each position, rather than a fixed trailing window — useful for watching an estimate settle as data accumulates;
`NaN` values are skipped and the output stays `NaN` until `minPeriods` (default 1) valid values exist. It is capped
at 20,000 observations; use `CumulativeSum` directly for a longer running total, or `RollingStatistic` with an
explicit window for a longer series.

**ExponentialMovingAverageSeries** returns one exponential-moving-average value per position of a time-ordered
series: each output is `alpha * current + (1 - alpha) * previous output`, starting from the first value, with
`alpha` strictly between 0 (exclusive) and 1 (inclusive, default 0.3) controlling how fast it reacts. A `NaN` entry
is carried through as `NaN` at its own position and does not update the running average for what follows.

**FrequencyTableOfIntegers** gives value counts for integer codes or discrete counts (day of week, item count, status
code): each distinct value with its count, proportion, percent and cumulative share, ordered by `order`
(`ByCountDescending` default, `ByValueAscending` or `ByFirstAppearance`), with `maxRows` limiting the rows returned
and `RowsOmitted` reporting the rest.

**FrequencyTableOfNumbers** is the same table for *exact* numeric values rather than integer codes — the quickest way
to spot repeated-amount structuring, such as many payments of exactly 9.99 or 1000.00 — matching values exactly
rather than binning them, so continuous data needs a histogram instead; `NaN` entries are missing.

**RareCategories** lists the labels that occur at most `maxCount` times (default 1, i.e. singletons): typos, new
counterparties or genuinely unusual behaviour often surface this way. Rows are ordered by count ascending then label;
`Total` is all observations, `Distinct` the number of distinct labels *overall* (not just the rare ones), and
`RowsOmitted` the rare labels beyond `maxRows`.

## Spread, shape and concentration (SpreadAndShapeTools)

**StandardErrorOfMean** is the sample standard deviation divided by the square root of the sample size, measuring how
precisely the sample mean estimates the true population mean; it shrinks as more data accumulates and needs at least
2 values. As a rule of thumb, the true mean lies within about two standard errors of the sample mean with 95 percent
confidence for reasonably large samples.

**CoefficientOfVariation** is the standard deviation divided by the absolute mean, a unit-free relative-spread
measure that lets variability be compared across variables on different scales (0.25 means the spread is 25 percent
of the level); it is `NaN` or infinite when the mean is 0 and is only meaningful for ratio-scale data not centred on
zero.

**GeometricStandardDeviation** is the exponential of the standard deviation of the logarithms — a *multiplicative*
spread factor for positive, right-skewed amounts. A value of 1 means no spread; 2 means typical values lie within a
factor of 2 above or below the geometric mean. All values must be strictly positive and at least 2 are required.

**VarianceToMeanRatio** (the index of dispersion) is the sample variance divided by the mean, applied to non-negative
counts per interval: a value near 1 is consistent with a Poisson process, above 1 means over-dispersed (bursty or
clustered), below 1 under-dispersed (unusually regular). It is undefined when the mean is 0.

**MeanAbsoluteDeviation** is the average absolute distance of values from their mean, or from their median when
`aroundMedian` is set — a spread measure in the data's own units that is less sensitive to outliers than the
standard deviation, though still less robust than the median-based measure below.

**MedianAbsoluteDeviation** (MAD) is the median of the absolute distances from the median itself, a spread measure
robust enough that up to half the data can be contaminated without destroying it. With `scaleToNormal` true
(default) the raw MAD is multiplied by 1.4826 so it estimates the standard deviation under normality; false returns
the raw value. A result of exactly 0 means more than half the values are identical.

**QuartileCoefficientOfDispersion** is `(Q3 - Q1) / (Q3 + Q1)`, a robust unit-free relative-spread measure analogous
to the coefficient of variation but built from quartiles instead of the mean and standard deviation; it is `NaN`
when the quartiles sum to 0, and is meaningful mainly for positive, skewed data.

**SumOfSquaredDeviations** is the total sum of squared deviations from the mean (the "sum of squares," SS) — the
building block that variance and analysis of variance both divide down from — returned in squared units of the
original data; dividing by `n - 1` recovers the sample variance.

**WeightedVariance** and **WeightedStandardDeviation** compute variance and its square root when every observation
carries a non-negative `weights` value, with `weightKind` choosing between `Frequency` weights (repeat counts, where
the unbiased estimator divides by total weight minus 1) and `Reliability` weights (importance or probability weights,
using the reliability-weights correction instead). The unbiased estimator needs a total weight above 1.

**ExponentialWeightedVariance** is the time-ordered, recency-weighted counterpart of ordinary variance, with the most
recent observation weighted most heavily, controlled by a decay factor `alpha` strictly between 0 and 1 (default
0.3, larger values forgetting the past faster); a series shorter than 2 values is rejected.

**PooledVariance** and **PooledStandardDeviation** combine the variances of several groups (`groups`, an array of
arrays, which can differ in length) into the single common-variance estimate used by equal-variance t-tests and
ANOVA, averaging each group's variance weighted by its degrees of freedom; every group needs at least 2 values.
**PooledVarianceFromSummaries** reaches the same pooled variance from group summaries alone — `sizes` and
`variances` per group — when the raw data itself is not available; every size must be at least 2 and no variance may
be negative.

**ShapeStatistics** returns skewness, excess kurtosis and Pearson kurtosis together in one call, for judging how far
a variable departs from a normal bell curve: skewness and excess kurtosis both near 0 are consistent with normality,
while large positive values flag a right tail and heavy tails respectively. The unbiased forms need 4 values; the
population forms need only 1.

**RawMoment** is the plain (non-central) moment of a given `order` (1 to 12) — the mean of the values raised to that
power, where order 1 is the mean itself and order 2 the mean square; large values raised to high powers can overflow.

**CentralMoment** is the moment of a given `order` (1 to 12) taken about the *mean* rather than about 0 — order 2 is
the population variance, order 3 measures asymmetry, order 4 tail weight, and order 1 is 0 by definition — using the
population divisor `n` throughout.

**StandardisedMoment** divides the central moment of a given `order` (3 to 12 only; orders 1 and 2 are the mean and
variance themselves) by the population standard deviation raised to that same order, giving a scale-free shape
number: order 3 is skewness and order 4 is Pearson kurtosis, derived here from first principles rather than via the
dedicated `Skewness`/`Kurtosis` tools.

**EntropyOfCounts** computes the Shannon entropy of a distribution supplied directly as non-negative counts or
probabilities (which need not sum to 1): 0 means one category holds everything, and the maximum is the log of the
category count when every category is equally likely. It returns `Entropy` in the chosen `unit` (`Bits` default,
`Nats` or `Hartleys`), `MaximumEntropy`, `NormalisedEntropy` (entropy over maximum, 0 to 1, a measure of evenness) and
`Perplexity` (the effective number of equally likely categories that entropy implies). Zero counts are ignored, and
the total must be positive.

**EntropyOfCategories** computes the same Shannon entropy, but starting from a raw array of category labels whose
frequencies are counted internally first — merchant categories or countries, for example — where low entropy means
activity concentrates on a few categories and high entropy means it spreads evenly; `null` labels are treated as
missing.

**InequalityMeasures** profiles concentration of non-negative amounts — spend, balances, transaction counts per
account — in one call: the Gini coefficient (0 perfectly equal, near 1 one holder has almost everything), the Theil
index, the Herfindahl-Hirschman index of shares (`1/n` for perfectly equal shares, 1 for total concentration), the
coefficient of variation, and a Lorenz curve of cumulative population-versus-value shares at up to 101 evenly spaced
points. Values must be non-negative and must not all be zero.

**GiniCoefficient** returns just the Gini number from the same calculation as `InequalityMeasures`, for when the
single headline figure is all that is wanted, with the identical input constraints (non-negative, not all zero).

## Table-wide summaries, correlation and transforms (TableAndTransformTools)

**DescribeTable** is the one-call overview of an entire numeric table before deeper analysis: a per-column summary
(the same shape `SummariseColumns` returns), a missing-value profile, the strongest pairwise correlations (computed
on rows complete in each pair, listed only when the absolute correlation is at least 0.5, up to 10 pairs), and lists
of constant columns, skewed columns (absolute skewness above 1), columns with IQR-fence outliers and columns with any
missing values. Use it to orient on a new dataset before choosing a deeper analysis.

**TableMissingnessProfile** reports exactly how much of a table is missing: per column, the present and missing
counts, missing fraction and infinite-value count; and overall, how many rows are completely present, how many rows
have any gap at all, the distribution of missing-cell counts per row (`MissingCountPerRowDistribution[k]` is the
number of rows with exactly k missing cells), and the first few incomplete row indices for spot-checking. Use it
before modelling to see whether gaps concentrate in a few columns or spread thinly across many rows.

**ColumnCovarianceMatrix** returns the sample covariance matrix of a table's columns: entry `[i][j]` is how columns i
and j vary together, in the product of their original units, with the diagonal holding each column's own variance.
Rows containing any missing value are rejected unless `dropIncompleteRows` is set, in which case they are removed and
counted; at least 2 complete rows are needed. **ColumnCorrelationMatrix** is the unit-free counterpart, each entry a
Pearson correlation between -1 and 1 with 1 on the diagonal; a constant column has an undefined correlation with
everything and gives `NaN` throughout its row and column rather than a spurious 0 or 1.

**StrongestColumnCorrelations** ranks *pairs* of columns by the strength of their Pearson correlation rather than
returning the full matrix, pairing each with its covariance, so redundant or related variables surface without
scanning a whole matrix by eye. Each pair uses pairwise deletion (every row where both columns of that pair are
present, independent of other columns); only pairs at or above `minimumAbsoluteCorrelation` (default 0.3) are kept,
strongest first, up to `maxPairs` (default 20).

**QuantilesByColumn** computes the same set of quantiles for every column of a table at once, for example the 5th,
50th and 95th percentile of every variable in one call for a side-by-side distributional comparison. Missing values
are excluded per column independently, with `Counts` reporting how many values each column actually contributed; a
column with no usable values returns `NaN` quantiles rather than failing the whole call.

**SummariseRows** is the row-wise counterpart to column summaries: for every row of a table it returns the count of
present values, sum, mean, minimum, maximum and standard deviation across that row's columns, ignoring any missing
cells in it — useful for row-level aggregates such as total spend across several product columns, or the spread of
several model scores for one case. Standard deviation is `NaN` for a row with fewer than 2 present values.

**SummariseByGroup** splits one numeric variable by a categorical `groups` array and reports, per group, count,
mean, median, standard deviation, minimum, maximum and sum, plus the pooled standard deviation and the grand mean
across all groups combined — the natural way to compare, say, transaction amounts by channel or country. A `null`
group label forms its own group literally named `(missing)` rather than being dropped; `NaN` values are excluded by
default (`ignoreMissing` defaults to `true` here, unlike most other tools in this group) and counted.

**SummariseCategories** profiles a categorical variable as a whole: total, missing, distinct count, the mode(s) with
their frequency, Shannon entropy in bits, normalised entropy (0 concentrated, 1 perfectly even), Gini impurity,
Herfindahl index, the count of labels seen exactly once, and the top 10 labels by count. A high distinct count
alongside many singletons is the signature of a near-unique identifier rather than a genuine category; `null` labels
count as missing and are excluded from the rest of the profile.

**TopShareConcentration** is a Pareto-style concentration analysis of non-negative amounts in both directions at
once: the share of the total held by the biggest `topFractions` of holders (default 1 percent, 10 percent, 20
percent), and the number of holders needed to reach given `shareTargets` of the total (default 50, 80, 90 percent) —
for example, how many customers make up 80 percent of total value. Values must be non-negative with a positive
total.

**RobustZScores** standardises values as `(value - centre) / scale` using robust estimators rather than the mean and
standard deviation `ZScores` uses: `scale` selects `MedianAbsoluteDeviation` (default, `centre` the median, spread
`1.4826 * MAD`), `InterquartileRange` (`centre` the median, spread `IQR / 1.349`), or `StandardDeviation` (`centre`
the mean, which defeats the purpose of robustness but is accepted for comparison). Values beyond about 3 to 3.5 on
this scale are usually unusual; a series whose computed scale is exactly 0 is rejected, since nothing could be
standardised against it. Missing entries stay `NaN`.

**CentreValues** subtracts the mean or median (`centre`, default `Mean`) from every value, without rescaling, to work
in deviations from the typical value rather than ratios or z-scores; missing entries stay `NaN`.

**TransformValues** applies one of eleven common element-wise transforms (`transform`): natural log, log base 10 or
2, `log(1+x)`, square root, cube root, square, reciprocal, absolute value, sign, negation, or a signed
`log(1+|x|)` that preserves sign across zero. Log and square root are the usual first step to tame right-skewed
amounts before further analysis. A value outside the chosen transform's domain (log needs a strictly positive value,
`log1p` needs greater than -1, square root needs non-negative, reciprocal needs non-zero) is rejected with the index
of the first offending value named in the error; missing entries stay `NaN`.

**ProportionsOfTotal** divides every value by the sum of all of them, so the output always sums to 1 — the share of a
total that each account, product or period holds. The total must not be zero; `Scale` in the result records what
that total was.

**WinsoriseValues** caps extreme values at chosen percentiles rather than removing them: anything below `lowerPercent`
(default 1) is pulled up to that percentile's value and anything above `upperPercent` (default 99) is pulled down to
it, keeping every observation in the series while limiting the influence of outliers on a mean or a model fit;
`Centre`/`Scale` in the result hold the lower and upper cap values actually used.

**FlagOutliersByZScore** flags values whose z-score `(value - mean) / standard deviation` exceeds `threshold` (default
3) in either direction — simple and widely understood, but the mean and standard deviation it is built on are
themselves distorted by the very outliers being sought, so `FlagOutliersByMedianAbsoluteDeviation` is preferred on
small or heavy-tailed data. It returns `IsOutlier` per value in original order, the outlier indices and values, each
value's own z-score in `Scores`, and the `LowerBound`/`UpperBound` value limits implied by the threshold; `NaN`
values are never flagged.

**FlagOutliersByIqrFence** flags values outside Tukey's fences — below `Q1 - k*IQR` or above `Q3 + k*IQR`, with the
multiplier `fenceMultiplier` (default 1.5 for ordinary outliers, 3 for extreme ones, the classic box-plot rule).
Being built on quartiles it is robust and assumes nothing about normality, but can flag many points on strongly
skewed data where the whole upper tail sits outside a fence computed from a tight box. `Scores` here is the signed
number of IQRs a value lies beyond its nearest quartile, 0 for anything inside the box.

**FlagOutliersByMedianAbsoluteDeviation** flags values whose modified z-score, `0.6745 * (value - median) / MAD`,
exceeds `threshold` (default 3.5, the Iglewicz-Hoaglin recommendation). Because it is built on the median and the MAD
rather than the mean and standard deviation, it stays reliable even when up to about half the data is contaminated,
making it the preferred rule for messy real-world amounts. If more than half the values are exactly identical, MAD
itself is 0; the tool falls back to the mean absolute deviation in that case and reports the spread actually used in
`Scale`, rather than failing outright.
