---
layout: default
title: Data Preparation and Resampling Tool Catalogue
nav_order: 7
parent: Reference
grand_parent: Analytical Methodology
---

# Data preparation and resampling tool catalogue

`Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling` carries 89 tools that an AskJooby agent calls
by their literal `[AdvancedAnalyticsTool("...")]` name. Jadek, the reply-checking agent, can only confirm a claim
about a tool against this `docs/` tree's RAG index, so a tool with no entry anywhere is one Jadek can never verify.
This page and its companion `ModelEvaluation.md` document every tool in the DataPreparationAndResampling and
ModelEvaluation groups for that reason. Every tool in the group is accounted for below: documented in full, or named
as already covered elsewhere with a citation, never silently omitted.

Every tool in this group shares one convention, enforced by the internal (non-tool) helper
`DataPreparationGuard.cs`: a cell is "missing" when it is `NaN` or infinite, never when it is merely absent from an
array, and a quantile is interpolated with `position = q * (n - 1)` — R's own `stats::quantile()` default
(`type = 7`) — everywhere a tool needs one. Row shuffles use a Fisher-Yates swap seeded from the tool's own `seed`
parameter, which is why every stochastic tool here is reproducible call for call given the same seed.

## Already catalogued elsewhere

Four of the eleven files in this directory are entirely covered, tool for tool, by
[Variable Transformations](../../VariableTransformations.md), which pairs each one with a re-run R procedure, the
exact formula, and in several cases a code audit against the textbook definition:

- **`BinningTools.cs` (6/6 tools).** `"ApplyBinEdges"`, `"DiscretiseCustomEdges"`, `"DiscretiseEqualFrequency"`,
  `"DiscretiseEqualWidth"`, `"DiscretiseKMeans"` and `"DiscretiseSupervised"` are documented under that page's
  "Discretisation and Binning" section (Procedures 10-11).
- **`EncodingTools.cs` (10/10 tools).** `"ApplyCategoryEncoder"`, `"ApplyLabelEncoder"`, `"ApplyOneHotEncoder"`,
  `"ApplyRareCategoryGrouping"`, `"FitFrequencyEncoder"`, `"FitLabelEncoder"`, `"FitOneHotEncoder"`,
  `"FitRareCategoryGrouping"`, `"FitTargetMeanEncoder"` and `"FitWeightOfEvidenceEncoder"` are documented under
  "Categorical Encoding" (Procedures 12-14).
- **`FeatureEngineeringTools.cs` (8/8 tools).** `"EncodeCyclicalFeature"`, `"GenerateColumnPairFeatures"`,
  `"GenerateLagFeatures"` and `"GeneratePolynomialFeatures"` are documented under "Feature Engineering: Cyclical
  Encoding of a Periodic Variable" (Procedure 15); `"ComputeElapsedTime"`, `"ExtractDateTimeParts"` and
  `"ExtractEpochParts"` are documented in
  [Loading, Shaping and Merging Data](../../LoadingShapingMerging.md) (Procedures 4-6); `"SelectTableColumns"` is
  documented there (Procedure 12) and again, with its exact `keep`/reject contract, in
  [Data Frames vs the Packed Array](../../DataFramesVsAccordArrays.md)'s `select()` section.
- **`ScalingTools.cs` (10/11 tools).** `"ApplyColumnFunction"`, `"FitPowerTransformer"`/`"ApplyPowerTransformer"`,
  `"FitTableScaler"`/`"ApplyTableScaler"`/`"InvertTableScaler"`, `"FitQuantileTransformer"`/
  `"ApplyQuantileTransformer"` and `"FitOutlierClipper"`/`"ApplyOutlierClipper"` are documented across that page's
  "Log Transform", "Power Transforms", "Standardisation and Range Normalisation", "Quantile Transformation" and
  "Outlier Winsorisation and Clipping" sections. `"NormaliseRowsToUnitNorm"`, the eleventh tool in this file, is not
  covered there and gets a full entry below.

A further eight tools scattered across other files have a genuine behavioural mention elsewhere — not a full
procedure, but a real statement of what they return, not a bare name match — and are cited rather than duplicated:

- `"FindDuplicateRows"` (`DataQualityTools.cs`) — [Data Frames vs the Packed Array](../../DataFramesVsAccordArrays.md)'s
  `filter()` section states it returns rows that exactly repeat an earlier row as an `int[]` of row numbers, not a
  reduced table.
- `"DropSparseRowsAndColumns"` (`MissingValueTools.cs`) — the same page's `filter()` section, and
  [Loading, Shaping and Merging Data](../../LoadingShapingMerging.md) Procedure 15, both describe it as dropping
  columns then rows by missing-value fraction and handing back the smaller table plus the surviving indices.
- `"RankFeaturesByMutualInformation"` and `"FilterCorrelatedFeatures"` (`FeatureSelectionTools.cs`) —
  [Hybrid Structure Learning](../../BayesianNetworks/HybridStructureLearning.md) describes the first as screening
  candidates by information-theoretic dependence against one target and the second as dropping one of a mutually
  correlated pair of features.
- `"SampleSimpleRandom"`, `"SampleStratified"` and `"SampleWeighted"` (`SamplingTools.cs`) —
  [AbstractionHarmonised](../../AbstractionHarmonised.md) and `DataFramesVsAccordArrays.md`'s `filter()` section
  describe the first as a reproducible simple random draw of row positions and the latter two as index-producing
  sampling designs returning an `int[]` of chosen rows.
- `"SplitTimeOrdered"` (`SamplingTools.cs`) — `DataFramesVsAccordArrays.md`'s `arrange()` section notes it assumes
  rows already arrive in chronological order rather than sorting them.

Everything below is a tool not covered, in full or in part, anywhere else in `docs/`.

## Class imbalance handling

`ClassImbalanceTools.cs` carries four tools for rebalancing or reweighting a skewed binary (or multi-class) outcome
before training.

**`"ComputeClassWeights"`** computes a weight per class, and from it a weight per row, so a model can be told to
pay more attention to a rare class without touching the data itself. `method` selects the formula: `Balanced` (the
default) sets each class's raw weight to `n / (classes * count)`, so every class carries the same total weight and
the mean row weight is exactly 1; `InverseFrequency` is `1/count`; `SquareRootInverse` is `1/sqrt(count)`, a gentler
correction than plain inverse frequency; and `EffectiveNumber` is `(1 - beta) / (1 - beta^count)` for a `beta`
strictly between 0 and 1 (default `0.999`), which lets a few extra rows of an already-large class count for very
little marginal weight. The three non-`Balanced` methods are then rescaled so the row-weight average is still 1,
making every method comparable on the same footing. It returns the distinct classes, their raw counts, the
per-class weight and a per-row weight array aligned to the input `labels`. `beta` outside `(0, 1)` throws.

**`"RandomUnderSample"`** balances classes by discarding rows from the larger classes at random. The smallest class
is always kept whole; every larger class is cut down to `ceil(smallestCount / targetRatio)` rows, so `targetRatio`
of 1 (the default) makes every class equal to the smallest, while a smaller fraction leaves the larger classes
proportionately bigger. It is fast and loses no information about the minority class, but throws away majority-class
rows outright, so it is best used when there is plenty of data to spare. It returns the sorted row indices to keep
plus the class counts before and after; the caller applies the same indices to features and any parallel label
array. The draw is reproducible from `seed` (default 1), and `targetRatio` must be above 0 and at most 1.

**`"RandomOverSample"`** balances the other way: smaller classes are repeated (drawn with replacement) until each has
at least `largestCount * targetRatio` rows. No information is lost, but duplicated rows make a model overconfident
and make cross-validation estimates optimistic — the description explicitly warns to oversample only inside
training folds, never before a train/validation/test split. It returns the row indices to use (every original row
once, then the extra draws, sorted) and the counts before and after. Reproducible from `seed`.

**`"GenerateSmoteSamples"`** implements SMOTE (synthetic minority over-sampling): pick a random row of the
`minorityLabel` class, pick one of its `neighbours` (default 5) nearest same-class neighbours by Euclidean distance,
and place a new synthetic row at a random point on the line between them, rather than an exact duplicate. Every
feature value must already be finite — a row with any `NaN` or infinite cell throws, naming the offending row and
pointing at `ImputeMissingValues` first. The minority class needs at least 2 rows to interpolate between, or the
call throws. `standardiseForDistance` (default true) rescales columns to unit standard deviation before measuring
distance so no single large-magnitude column dominates the neighbour search; a column with zero variance keeps a
scale of 1 rather than dividing by zero. `syntheticCount` of 0 (the default) generates just enough synthetic rows to
match the size of the largest class. The result carries the synthetic rows on their own, the source row and chosen
neighbour behind each one, and — when `includeOriginalRows` is true (the default) — the real rows followed by the
synthetic ones as a single ready-to-fit table and label array. Reproducible from `seed`.

## Data quality profiling and reporting

`DataQualityTools.cs` carries seven tools; `"FindDuplicateRows"` is covered above, leaving six here.

**`"DescribeDatasetShape"`** is the suggested first look at any numeric table: for every column it infers a type —
`AllMissing`, `Constant` (one distinct finite value), `Binary` (exactly two), `Integer` (more than two, all whole
numbers within `0.0001`) or `Continuous` — and reports the distinct count, missing count, and the minimum, maximum
and mean of the finite values. An all-missing column reports `NaN` for every statistic rather than throwing. It
returns the row and column counts, one `ColumnShape` per column, and an approximate byte size (`rows * columns * 8`,
one IEEE double per cell).

**`"DescribeCategoricalTable"`** is the equivalent profile for a text table: per column, the distinct count, the
missing count (null or empty string), the most frequent value and its count, a cardinality ratio (distinct values
over row count — near 1 reads as identifier-like) and the top `topValues` values by frequency (default 5, ties
broken alphabetically). It also reports each column's Shannon entropy in nats (0 means a single value fills the
column). The description recommends reading this before choosing an encoder: low cardinality suits one-hot, high
cardinality suits frequency or target-mean encoding.

**`"FindDuplicateCategoricalRows"`** is `"FindDuplicateRows"`'s text-table counterpart: rows are joined into a key
with a `\u001f` separator (null and empty treated as the same value) and compared for an exact match to an earlier
row. It returns the duplicate count, the duplicate groups (each group's first occurrence and its repeat row
indices) and the row indices to keep if repeats are dropped.

**`"CheckTargetBalance"`** reports how skewed a classification target's classes are: the count and share of each
class, the imbalance ratio (largest class count over smallest), the relative entropy of the class distribution (1
means perfectly balanced, near 0 means one class dominates) and the smallest class's share of rows. It emits
plain-language warnings when the rarest class is below 1% or 10% of rows (naming the risk that accuracy becomes
misleading and recommending stratified splits, class weights, precision/recall or AUC) and when the rarest class has
fewer than 30 rows (estimates involving it will be very noisy). A single-class target gets its own warning that no
model can be trained on it.

**`"DataQualityReport"`** runs a whole battery of checks over a numeric table in one call and returns a flat list of
`DataQualityIssue` records, each carrying a severity (`Error`, `Warning` or `Info`), the affected column (or `null`
for a table-wide issue), a message and the name of the tool that would fix it. The checks, in the order they run per
column, are: infinite values (`Warning`, fix `ApplyColumnFunction` or `ImputeMissingValues`); entirely missing
(`Error`, fix `DropSparseRowsAndColumns`, and the column is then skipped); more than 50% missing (`Warning`) or more
than 20% missing (`Info`, fix `ImputeMissingValues`); a constant column (`Warning`, fix `SelectByVarianceThreshold`,
then skipped); a near-constant column where one value fills 99% or more of the finite rows (`Warning`); a whole-number
column with 20 or more finite rows where every value is distinct (`Warning`, "looks like a row identifier", fix
`SelectTableColumns`); absolute skewness above 2 on 8 or more finite rows (`Info`, fix `FitPowerTransformer` or
`ApplyColumnFunction`); more than 5% of values outside the 1.5×IQR fences (`Info`, fix `FitOutlierClipper`); and,
when `labels` or `numericTarget` is supplied (not both — supplying both throws), a leakage check per column (`Error`,
fix `SelectTableColumns`) using Pearson correlation against the target, or, for a binary target, the rank-based AUC
of the column against the target, flagged when either reaches `leakageThreshold` (default 0.95, must be between 0.5
and 1) — plus, for a binary target with 5 or more missing values, a check of whether the column's own missingness
indicator correlates at least 0.5 with the target (`Warning`, "InformativeMissingness", since missingness may itself
leak or usefully signal the outcome). Beyond the per-column checks it adds table-wide issues for a 1000-fold or
greater spread between the smallest and largest column standard deviation (`Info`, fix `FitTableScaler`), any exact
duplicate rows found by `"FindDuplicateRows"` (`Warning`), and fewer than 10 rows per column (`Warning`, fix
`SelectByVarianceThreshold` or `FilterCorrelatedFeatures`). The result bundles the per-column shape summary, the
duplicate row count, the target balance (when labels were given) and every issue found.

**`"DataQualityReportCategorical"`** is the same battery for a text table: entirely empty columns (`Error`); more
than 50% missing (`Warning`, fix `FitRareCategoryGrouping`); a single-value column (`Warning`); high cardinality —
more than 50 distinct values covering more than half the rows (`Warning`, "identifier-like", fix
`FitFrequencyEncoder` or `FitRareCategoryGrouping`); rare categories — at least 5 distinct values where more than
20% of them occur exactly once (`Info`, fix `FitRareCategoryGrouping`); inconsistent formatting — distinct values
that collapse to the same string once trimmed and lower-cased (`Warning`, same fix); exact duplicate rows via
`"FindDuplicateCategoricalRows"` (`Warning`); and, with `labels` supplied, a Cramér's V leakage check against the
target at or above `leakageThreshold` (default 0.95, between 0.5 and 1, `Error`). It returns the per-column
categorical profile, the duplicate row count and the issue list.

## Feature selection

`FeatureSelectionTools.cs` carries eight tools; `"RankFeaturesByMutualInformation"` and `"FilterCorrelatedFeatures"`
are covered above, leaving six here.

**`"SelectByVarianceThreshold"`** drops numeric columns whose sample variance over their finite values is at or
below `threshold` (default 0, which removes exactly the constant columns that break many models outright). Variance
depends on a column's units, so the description explicitly recommends scaling columns to a common footing first if
raising the threshold above 0 to catch near-constant columns. A column with fewer than 2 finite values gets `NaN`
variance and is always dropped. It returns every column's variance and finite-value count alongside the names kept
and dropped.

**`"RankFeaturesByAnovaF"`** ranks numeric features by how well they separate the classes of a categorical target,
using a one-way ANOVA F test per feature (between-class variance over within-class variance) computed only on rows
where that feature is finite. A feature with fewer than two usable classes, or fewer than one residual degree of
freedom, gets `NaN` and sorts last. It only detects differences in class means and assumes roughly normal,
similarly-spread classes. The result lists features best first with the F statistic, the raw p-value, a
Benjamini-Hochberg-adjusted p-value (correcting for testing many features at once) and the rank.

**`"RankFeaturesByFRegression"`** does the regression analogue against a continuous target: for each feature the
Pearson correlation `r` with the target gives `F = r^2 / (1 - r^2) * (n - 2)` on 1 and `n - 2` degrees of freedom.
It sees only linear, one-feature-at-a-time association. Rows where either the feature or the target is missing are
dropped per feature; fewer than 3 usable rows or no variation in either variable gives `NaN`. Returns the same
best-first shape as the ANOVA ranking, with F, p-value, adjusted p-value and rank.

**`"RankCountFeaturesByChiSquare"`** ranks non-negative numeric features — counts, frequencies, amounts, one-hot
flags — by a chi-square test of whether a feature's total mass is spread across the classes in proportion to class
size; a negative value anywhere in the table throws outright, since the test needs non-negative mass. Missing cells
count as 0 rather than being excluded. A feature that is zero everywhere gets `NaN`. The chi-square statistic's
degrees of freedom are `classes - 1`, and the result is ranked best first with the statistic, p-value, adjusted
p-value and rank.

**`"RankCategoricalFeaturesByChiSquare"`** ranks text features by Pearson's chi-square test of independence against
a categorical target: for each column it builds a category-by-class contingency table (null or empty is its own
category) and computes the chi-square statistic on `(categories - 1) * (classes - 1)` degrees of freedom, the
p-value, a Benjamini-Hochberg-adjusted p-value, and Cramér's V as an effect size between 0 and 1 that is comparable
across features of different cardinality — features are ordered by Cramér's V, best first, not by the raw
statistic. The description warns the test is unreliable when many expected cell counts fall below 5, and recommends
grouping rare categories first; a column with a single category gets `NaN` and sorts last.

**`"EliminateFeaturesRecursively"`** is recursive feature elimination built on a linear model: standardise every
column, fit ordinary least squares against the numeric (or 0/1) target, drop the `step` features (default 1) with
the smallest absolute standardised coefficients, refit on what remains, and repeat until exactly `featuresToKeep`
survive. Because the fit is repeated after every removal it accounts for overlap between correlated features that a
one-shot ranking misses. Rows with any missing value (feature or target) are dropped before fitting, and the call
throws if fewer than `columns + 2` complete rows remain — the description's suggested fix is to impute or drop
features first. `featuresToKeep` must be between 1 and `columns - 1`. It returns the selected feature names and, for
every original feature, a rank (1 for the features kept, a higher number for each eliminated one, with the very
last feature removed ranked 2) and that feature's final standardised coefficient. A perfectly collinear fit throws
with a message recommending `FilterCorrelatedFeatures` or `SelectByVarianceThreshold` first.

## Missing values

`MissingValueTools.cs` carries seven tools; `"DropSparseRowsAndColumns"` is covered above, leaving six here.

**`"ProfileMissingValues"`** is the recommended first step before any imputation decision: per column, the missing
count and fraction; the number of rows that are fully complete; and up to `maximumPatterns` (default 10) of the most
common missing patterns — which exact set of columns is missing together, most frequent first — so a random scatter
of gaps can be told apart from a structural one (for example, a field that is only ever missing alongside another).
It emits plain-language warnings for any column that is entirely missing or more than half missing. An empty table
is rejected outright.

**`"ImputeMissingValues"`** fills missing cells column by column using a strategy learned from the supplied rows:
`Mean`, `Median` (the default, chosen specifically because it resists outliers), `Mode` (most frequent value, ties
broken toward the smallest) or `Constant` (using the `constant` parameter, default 0, for every column). A column
with no finite value at all cannot support `Mean`, `Median` or `Mode` and throws, naming the column and suggesting
`Constant` or dropping it. It returns the filled table, the fill value actually used per column (keep these and
pass them to `"ApplyImputer"` so new data is filled identically), the count of cells replaced per column, and the
exact cells replaced. `addMissingIndicators` (default false) appends a `<column>_missing` 0/1 column for every
column that had at least one gap, preserving the fact that a value was once absent as its own feature.

**`"ApplyImputer"`** replays fill values learned earlier so training and scoring data are treated identically: each
missing cell takes its column's `fillValues` entry, and a fill value of `NaN` deliberately leaves that column
untouched. It returns the same result shape as `"ImputeMissingValues"`, and `indicatorSourceColumns` reproduces the
same `<column>_missing` indicator columns the original fit used.

**`"ImputeForwardFill"`** fills gaps in rows that are already in time or sequence order by carrying the last
observed value of the same column forward — the natural choice for state-like series such as balances or statuses,
where the previous value is the best available guess. A leading gap, before any value has been observed, has no
predecessor and stays `NaN` unless `backfillLeading` is true, in which case it takes the first observed value
instead. It returns the filled table and the exact cells replaced; rows must already be ordered by the caller.

**`"ImputeLinearInterpolation"`** fills gaps in ordered rows by straight-line interpolation between the nearest
observed values before and after, treating row position as the x-axis — suited to smooth measurements such as rates
or exposures rather than state-like values. A gap at the very start or end of a column has no value on one side and
stays `NaN` unless `extrapolateEdges` is true, in which case it copies the nearest observed value rather than
interpolating. Returns the filled table and the cells replaced.

**`"ImputeNearestNeighbours"`** fills each missing cell with the average of that column over the `neighbours`
(default 5) most similar rows, where similarity is Euclidean distance measured only over the columns both rows have
observed and rescaled by how many columns overlapped — so a row missing many columns is not unfairly penalised for
a thin comparison. `standardiseForDistance` (default true) scales columns to unit standard deviation first so no
single large column dominates the distance. A cell with no usable neighbour at all falls back to the column mean. A
column with no observed value anywhere throws, suggesting dropping it or using `"ImputeMissingValues"` with the
`Constant` strategy. The description notes cost grows with the square of the row count, since every missing cell
searches every other row.

## Resampling-based inference

`ResamplingInferenceTools.cs` carries seven tools, none of them covered elsewhere, built around bootstrap, jackknife
and permutation methods for sampling uncertainty and hypothesis testing without distributional assumptions. All
seven are reproducible from a `seed` parameter and treat `NaN`/infinite values as missing, dropping and counting
them.

**`"BootstrapStatisticInterval"`** estimates the sampling uncertainty of one of ten statistics — `Mean` (default),
`Median`, `StandardDeviation`, `Variance`, `Minimum`, `Maximum`, `Quantile` (using the `probability` parameter),
`TrimmedMean` (using `trimFraction`, the share cut from each end), `Skewness` or `Kurtosis` — by resampling the data
with replacement `resamples` times (default 2000, between 100 and 200000) and returns a confidence interval at
`confidenceLevel` (default 0.95). Four interval methods are offered: `Percentile` (the default — quantiles of the
resampled statistic), `Basic` (reflecting the percentile bounds around the point estimate), `Normal` (point estimate
minus bias, plus or minus a z-score times the bootstrap standard error) and `BiasCorrectedAccelerated` (BCa, which
additionally corrects for skew using a jackknife-estimated acceleration constant and is the most accurate of the
four for smooth statistics). The result carries the real-data estimate, the bootstrap mean, the bias (bootstrap mean
minus estimate), the standard error, the interval bounds, and — for BCa only — the bias constant `z0` and the
acceleration. At least 2 finite values are required, and a perfectly constant sample yields a zero-width interval
rather than an error.

**`"BootstrapTwoSampleDifference"`** bootstraps the difference between the same statistic computed on two
independent samples (`first` minus `second`), each resampled separately with replacement — for example, the
difference in median transaction amount between two segments. If the resulting interval excludes 0, the difference
is unlikely to be chance at that confidence level. `Percentile`, `Basic` and `Normal` methods are supported;
`BiasCorrectedAccelerated` is rejected outright with a message that BCa applies to a single sample only.

**`"BootstrapCorrelationInterval"`** bootstraps the correlation between two paired variables by resampling whole
pairs together with replacement, which preserves the pairing and gives a more honest interval than a normal-theory
one for skewed data or small samples. `kind` selects `Pearson` (linear) or `Spearman` (rank-based, monotone); pairs
where either value is missing are dropped and counted, and a resample with no variation in one variable yields `NaN`
and is skipped from the bootstrap distribution rather than failing the whole call. At least 3 complete pairs are
required, and the real-data correlation must be defined (a constant variable throws). All four interval methods
(including BCa, via leave-one-out jackknife estimates on the real pairs) are supported here.

**`"JackknifeStatistic"`** estimates the bias and standard error of a statistic by leaving out one observation at a
time — deterministic, needing no random numbers at all. It returns the full-data estimate, the `n` leave-one-out
estimates (a leave-one-out value that deviates sharply from the rest flags an influential observation), the
jackknife bias estimate, the bias-corrected estimate, the standard error, and a Student-t confidence interval built
around the original estimate using `n - 1` degrees of freedom. For the mean the standard error reduces to the
textbook `s / sqrt(n)` with zero bias. The description explicitly warns the jackknife works poorly for non-smooth
statistics such as the median or minimum, recommending `"BootstrapStatisticInterval"` for those instead. At least 3
finite values are required.

**`"PermutationTestTwoSample"`** tests whether two samples differ in `DifferenceInMeans` (default),
`DifferenceInMedians` or `DifferenceInStandardDeviations` without distributional assumptions, by pooling the two
samples and repeatedly reassigning which rows belong to which group. When the number of distinct ways to split the
pooled data into the observed group sizes is at most 20000, the test enumerates every one of them exactly;
otherwise it draws `permutations` random reassignments (default 10000, between 100 and 1,000,000) and the p-value is
computed as `(extreme + 1) / (permutations + 1)`, which is never exactly zero even in the Monte Carlo case.
`alternative` is `TwoSided` (default), `Greater` (first sample larger) or `Less`. The result carries the observed
statistic, p-value, the permutation count actually used, whether the test was exact, and the mean, standard
deviation and 2.5th/97.5th percentiles of the simulated null distribution.

**`"PermutationTestCorrelation"`** tests association between two paired variables by repeatedly shuffling one of
them and comparing the observed `Pearson` or `Spearman` correlation with the shuffled correlations. With 7 or fewer
complete pairs every one of the `n!` orderings is enumerated exactly; above that it falls back to the same Monte
Carlo `(extreme + 1) / (permutations + 1)` p-value as the two-sample test. `alternative` reads `Greater` as positive
association and `Less` as negative. At least 3 complete pairs are required and a constant variable throws, since its
correlation is undefined.

**`"PermutationTestPaired"`** tests whether the mean paired difference (`first` minus `second` — for example,
before and after a control change) is zero, by randomly flipping the sign of each difference, which is exactly what
a true null hypothesis of no effect implies. Zero differences carry no information under a sign flip and are
dropped before counting; at least 2 non-zero differences are required. With 14 or fewer non-zero differences, all
`2^n` sign patterns are enumerated exactly; otherwise it is the same Monte Carlo scheme as the other two permutation
tests. Returns the observed mean difference, p-value, permutation count, whether exact, and the null distribution's
mean, standard deviation and 2.5th/97.5th percentiles.

## Sampling, splitting and cross-validation folds

`SamplingTools.cs` is the largest file in the directory, with 17 tools. `"SampleSimpleRandom"`,
`"SampleStratified"`, `"SampleWeighted"` and `"SplitTimeOrdered"` are covered above; the remaining thirteen are
documented here.

**`"SampleSystematic"`** draws every k-th row after a random start, where `k = rowCount / sampleSize`, the classic
design for an evenly spread audit sample over rows already in a meaningful order (such as time). The description
warns the rows must carry no hidden periodicity that lines up with `k` — for example, auditing "every 7th row" on
data that already cycles weekly would silently stop being random. It returns the selected indices ascending,
together with the interval `k` and the random start actually used, both reproducible from `seed`.

**`"SampleReservoir"`** draws a uniform sample of `reservoirSize` row positions from a stream of `streamLength` rows
using reservoir sampling (Algorithm R), needing memory for only the reservoir itself rather than the whole stream —
every row ends up equally likely to be selected regardless of stream length. If `reservoirSize` is at least
`streamLength`, every row is returned. Reproducible from `seed`.

**`"SampleClusters"`** is cluster sampling: whole groups (accounts, devices, and so on, named by `groupKeys`) are
chosen at random, and every row belonging to a chosen group is returned — used when rows within a group are not
independent and a group must never be split across the sampled and unsampled sets. It returns the chosen group
names, the row indices of every row in those groups (ascending) and the unselected rows. Reproducible from `seed`.

**`"SplitTrainValidationTest"`** randomly splits row positions into train, validation and test index sets whose
three fractions (default 0.6/0.2/0.2) may sum to less than 1 — the leftover rows are simply excluded — but must not
exceed 1. When `labels` are supplied and `stratify` is true (the default), each class is split in the same
proportions so a rare class still appears in every set. Reproducible from `seed`; a fraction of 0 yields an empty
set rather than an error.

**`"SplitByGroup"`** splits row positions into train, validation and test sets the same way, but keeps every row of
one group (account, customer, device) entirely inside a single set, which prevents information leaking between sets
through correlated rows of the same entity. Groups are assigned greedily, largest group first, to whichever set sits
furthest below its target share, so the realised fractions become only approximate once groups are large relative to
the target split. Fractions may sum to less than 1 (but more than 0) and not more than 1. Reproducible from `seed`.

**`"GenerateKFolds"`** generates k-fold cross-validation splits: rows are shuffled by `seed` and dealt into `folds`
(default 5, at least 2) near-equal-size folds; fold `i` uses its own rows as the test set and every other row as
training. Every row appears in exactly one fold's test set. `rowCount` must be at least as large as `folds`.

**`"GenerateStratifiedKFolds"`** is the class-aware version: rows of each class are shuffled and dealt round-robin
across folds, so every fold keeps each class in roughly its overall proportion — important for rare events such as
fraud, where plain k-fold can leave a fold with no positives at all. A class with fewer rows than there are folds
simply misses some folds rather than erroring.

**`"GenerateGroupKFolds"`** generates k-fold splits where every group (account, customer, device, named by
`groupKeys`) sits wholly inside one fold's test set, so a model is never tested on an entity it trained on. Groups
are shuffled and then assigned largest first to whichever fold is currently smallest, to keep fold sizes reasonably
balanced. There must be at least as many distinct groups as folds, or the call throws.

**`"GenerateTimeSeriesFolds"`** generates forward-chaining (expanding-window) folds for time-ordered data: rows are
ordered by `times`, cut into `folds + 1` blocks, and fold `i` trains on the first block plus every previous test
block while testing on the next one — so a model is never trained on rows that come after its test block. An
optional `gap` drops that many rows immediately before each test block, guarding against information bleeding
across the boundary. `times` may not contain `NaN`, and there must be at least one row per block
(`times.Length / (folds + 1) >= 1`) or the call throws naming the fix (lower `folds`).

**`"GenerateLeaveOneOutFolds"`** generates the extreme case of k-fold: fold `i` tests on row `i` alone and trains on
every other row, giving `rowCount` folds of `rowCount - 1` rows each. Because of the resulting cost, `rowCount` is
capped to between 2 and 1000. The description notes it has low bias but high variance and is costly to evaluate, and
recommends ordinary k-fold as usually preferable.

**`"GenerateRepeatedKFolds"`** runs k-fold cross-validation `repeats` times (default 3) with a different shuffle each
time (repeat `r` uses `seed + r`), producing `repeats * folds` train/test pairs in total for a steadier performance
estimate than a single k-fold pass. When `labels` are supplied, every repeat is stratified. Each returned entry is
tagged with its repeat number alongside its fold index and train/test indices.

**`"GenerateBootstrapSamples"`** generates `resamples` (default 10) bootstrap resamples, each a set of row indices
drawn with replacement; about 36.8% of rows are expected to be left out of any one resample purely by the mechanics
of sampling with replacement. For each resample it returns the in-bag indices (the training set, containing
repeats) and the out-of-bag indices (rows never drawn, usable as that resample's own held-out test set).
`sampleSize` of 0 (the default) draws the same number of rows as the original table; resample `r` is seeded from a
deterministic combination of `seed` and `r`, so the whole set is reproducible.

**`"GeneratePermutation"`** generates a single random permutation of the row positions `0` to `rowCount - 1`,
reproducibly from `seed`. Besides shuffling rows directly, the description notes it is the primitive behind building
a null distribution for a permutation test by applying the same shuffle to a label array. The returned array lists,
for each new position, which original row index landed there.

## Row normalisation

**`"NormaliseRowsToUnitNorm"`** (`ScalingTools.cs`) is the one tool in that file not already covered by
[Variable Transformations](../../VariableTransformations.md): unlike every other `ScalingTools` tool, it rescales
each *row* rather than each column, so its length is 1 under a chosen norm — `L1` (sum of absolute values), `L2`
(the default, Euclidean length) or `Max` (largest absolute value). It is intended for profile- or spend-mix-style
features, where the shape of a row's values matters and the row's overall size does not. Missing cells are excluded
from the norm calculation and remain `NaN` in the output; a row whose norm comes out to 0 (every finite value is
itself 0) is left as all zeros rather than dividing by zero. It returns the normalised table alongside each row's
original norm.

## Table shaping

`TableShapingTools.cs` carries four tools, none covered elsewhere, for reshaping an already-packed table by its rows
or columns rather than its values.

**`"FilterTableRows"`** returns a table containing only the given `keepRowIndices`, in the exact order supplied,
compacting the table down to those rows; an index may be repeated to duplicate a row, and an out-of-range index
throws. The description calls this the foundation every other row-shaping tool in the file builds on — it is the
step that turns indices identified by duplicate detection, sparse-row detection or sampling into an actual smaller
table.

**`"DistinctTableRows"`** drops rows that exactly repeat an earlier row (two missing cells count as equal, the same
rule `"FindDuplicateRows"` uses), keeping the first occurrence of each. It is implemented as a thin wrapper: it
calls `"FindDuplicateRows"` internally to get the surviving row indices and then calls `"FilterTableRows"` with
them — the tool to reach for after `"FindDuplicateRows"` when the repeats should actually be removed rather than
only reported.

**`"ArrangeTableRows"`** sorts the whole table by one or more columns at once, named by `sortColumnIndices` (most
significant first) with one `descending` flag per sort column, using a stable sort so rows tied on every sort
column keep their original relative order. At least one sort column is required, and `descending` must have exactly
one entry per sort column. It is implemented as a LINQ `OrderBy`/`ThenBy` chain over the requested columns, followed
by a call to `"FilterTableRows"` with the resulting row order.

**`"RelocateTableColumns"`** moves one or more named `columnsToMove` to a new position — immediately after a named
`after` column, or to the front of the table when `after` is omitted — leaving every other column in its existing
relative order. A column cannot appear in both `columnsToMove` and as the `after` target, and a repeated name in
`columnsToMove` throws. The description frames it as the tool to reach for to reorder columns without having to
name every single column the way `"SelectTableColumns"` (`FeatureEngineeringTools.cs`, documented in
[Loading, Shaping and Merging Data](../../LoadingShapingMerging.md)) requires.
