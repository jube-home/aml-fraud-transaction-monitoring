---
layout: default
title: Time Series and Signal Tool Catalogue
nav_order: 14
parent: Reference
grand_parent: Analytical Methodology
---

# Time series and signal tool catalogue

`Jube.Service/Agent/AdvancedAnalyticsTools/TimeSeriesAndSignal` is one of the ten semantic tool groups that the
AdvancedAnalytics agent layer exposes directly to the LLM by name: 111 methods, each decorated
`[AdvancedAnalyticsTool("ToolName")]`, grouped into nine tool classes covering Markov chains and hidden Markov models,
sequence and pattern analysis, series anomaly and change detection, dependence and seasonality tests, forecasting,
series transforms and filters, spectral and signal analysis, and transaction-timing statistics. None of these 111
names appears anywhere else under `docs/`, which means Jadek, the validator agent that fact-checks every
AskJooby reply against a RAG-indexed corpus built from this documentation tree, cannot confirm a claim about
any of them without it. This page covers every tool. Two supporting files in the same directory, `MarkovMath.cs` (matrix and
stationary-distribution helpers) and `TimeSeriesGuard.cs` (input validation and shared statistics), are not tools
themselves and are not catalogued here.

Unless stated otherwise, every tool in this group is stateless: it takes plain arrays (`double[]`, `string[]`,
`int[]`, jagged `double[][]`) and returns a typed result record, with no reference to a model, a payload or engine
state. Numeric inputs are validated by `TimeSeriesGuard` before use, so the error a caller sees for a malformed array
is a descriptive `ArgumentException` naming the parameter, not an unhandled exception.

## HiddenMarkovTools.cs

Eleven tools for discrete- and Gaussian-emission hidden Markov models (HMMs): decoding, likelihood, posterior state
probabilities, next-value prediction, and Baum-Welch training. A discrete HMM models a sequence of categorical
symbols; a Gaussian HMM models a numeric series as a regime-switching normal distribution. All eleven share a model
shape of `transitions` (a square, row-stochastic matrix), `initial` (a probability vector) and, for discrete models,
`emissions` (a row-stochastic matrix of symbol probabilities per state) or, for Gaussian models, `means` and
`standardDeviations` per state. Every probability row must sum to 1 within `1e-6`, every standard deviation must be
above 0, and all eleven are limited to 12 hidden states and 200,000 observations; violating any of these throws an
`ArgumentException` naming the offending row or dimension.

**HiddenMarkovDiscreteDecode** runs the Viterbi algorithm on a discrete HMM to find the single most likely sequence
of hidden states behind an observed symbol sequence (symbol codes from 0 to the emission column count minus 1).
It returns `StatePath` (zero-based decoded states) and `LogLikelihoodOfPath`, the natural-log probability of that
path together with the observations. Use it to label each step of a behaviour sequence with a latent regime, for
example normal versus compromised, under a model supplied directly or produced by `HiddenMarkovDiscreteTrain`.

**HiddenMarkovDiscreteLogLikelihood** computes the log-likelihood of an observed discrete sequence under a given
model by the forward algorithm, summing over all hidden paths rather than picking the best one. It returns
`LogLikelihood`, `AverageLogLikelihoodPerObservation` (for comparing sequences of different lengths), and
`StepLogProbabilities` with `MostSurprisingIndex` locating the single most improbable step; a symbol the model gives
zero probability produces negative infinity at that step.

**HiddenMarkovDiscretePosterior** runs the forward-backward algorithm to give, for every step, the full posterior
probability of each hidden state given the whole sequence (past and future), rather than one decoded path. It
returns `StateProbabilities[t][i]` (each row summing to 1), `MostLikelyStates[t]` (which can differ from the Viterbi
path), and `LogLikelihood`. Use it when the confidence in each step's state assignment matters more than a single
hard labelling.

**HiddenMarkovDiscreteNextSymbolDistribution** predicts the next symbol in a sequence under a discrete HMM: it
filters the current belief about the hidden state from everything observed so far, pushes that belief one step
through the transition matrix, and projects it through the emission matrix. It returns `FilteredStateProbabilities`,
`NextStateProbabilities`, `NextSymbolProbabilities` (summing to 1) and `MostLikelyNextSymbol`, which supports
next-action prediction or checking whether an actual next event was expected.

**HiddenMarkovGaussianDecode** is the Gaussian-emission counterpart of `HiddenMarkovDiscreteDecode`: Viterbi decoding
of the most likely sequence of hidden regimes behind a numeric series (for example daily volume labelled quiet,
normal or busy), under a model supplied directly or trained with `HiddenMarkovGaussianTrain`. It returns `StatePath`
and `LogLikelihoodOfPath` (a log density, so it can be positive since this is a density rather than a probability
mass).

**HiddenMarkovGaussianLogLikelihood** computes the forward-algorithm log-likelihood of a numeric series under a
Gaussian HMM, returning `LogLikelihood`, `AverageLogLikelihoodPerObservation`, `StepLogProbabilities` and
`MostSurprisingIndex`, to judge model fit, compare regime counts, or flag a series a normal-behaviour model finds
unlikely.

**HiddenMarkovGaussianPosterior** is the Gaussian counterpart of `HiddenMarkovDiscretePosterior`: smoothed posterior
regime probabilities at every point of a numeric series from the forward-backward algorithm, returning
`StateProbabilities`, `MostLikelyStates` and `LogLikelihood`, used to see how confident a regime assignment is rather
than only its most likely value.

**HiddenMarkovGaussianNextValueDistribution** gives a one-step-ahead forecast of a numeric series under a Gaussian
HMM: `ExpectedNextValue` (the mixture mean across regimes weighted by their filtered and transitioned
probabilities) and `NextValueStandardDeviation` (the mixture standard deviation, combining within-regime spread and
regime uncertainty), alongside `FilteredStateProbabilities` and `NextStateProbabilities`. A new observation can be
standardised by subtracting `ExpectedNextValue` and dividing by `NextValueStandardDeviation` before comparing it
with a threshold.

**HiddenMarkovDiscreteTrain** fits a discrete HMM to example symbol sequences with Baum-Welch
(expectation-maximisation), discovering latent regimes behind categorical event streams with no labels, for example
channel or merchant-category sequences. Parameters: `states` (1 to 12), `symbols` (0, the default, uses the largest
code seen plus 1), `restarts` (default 3, 1 to 20), `maxIterations` (default 100, 1 to 1000), `tolerance` (default
`1e-6`) and `seed` (default 1, for reproducibility). Training is repeated `restarts` times from different random
starting probabilities (seeded, so reproducible) because Baum-Welch finds only a local optimum, and the run with the
highest log-likelihood is returned, with fitted probabilities floored away from exactly zero. The result's
`Transitions`, `Emissions` and `Initial` carry an arbitrary hidden-state numbering; `LogLikelihood`, `Aic` and `Bic`
measure fit. It requires at least 2 observations per hidden state in total and at least 2 distinct symbol codes.

**HiddenMarkovGaussianTrain** is the Gaussian counterpart: it fits a regime-switching normal model to one or more
numeric series by Baum-Welch, learning a mean, standard deviation, persistence and switching probability for each
regime. Means are seeded by spreading them across the data quantiles with reproducible jitter after the first
restart; `minimumStandardDeviationFraction` (default 0.001, strictly between 0 and 1) floors each regime's fitted
standard deviation as a fraction of the pooled standard deviation, preventing a regime from collapsing onto a single
point. Regimes in the result are ordered by increasing mean. It throws if every training value is identical (no
variation to separate regimes), and otherwise shares the restarts, iteration limits and 12-state, 200,000-observation
caps of the discrete trainer.

**HiddenMarkovGaussianSelectStateCount** answers "how many regimes does this series need?" before calling
`HiddenMarkovGaussianTrain`: it fits models with 1 up to `maxStates` (default 4) regimes and compares them by AIC and
BIC (lower is better; BIC penalises complexity more). `Candidates` lists each tried state count's log-likelihood,
AIC, BIC and parameter count; `BestStatesByBic` and `BestStatesByAic` name the winners (usually BIC favours the
simpler model) and `BestModel` is the fitted model at the BIC winner. A single regime is fitted as the plain normal
distribution rather than a one-state HMM.

## MarkovChainTools.cs

Seven tools for first-order Markov chains over labelled states: estimating a chain from an observed sequence, and
analysing a chain whose transition matrix is already known (stationary behaviour, multi-step projection, mean
first-passage times, and absorption). State labels are arbitrary strings; chains supplied as a matrix are capped at
500 states via `MarkovMath`.

**MarkovChainTransitionMatrix** estimates a first-order Markov chain from an observed sequence of state labels in
time order (for example account status changes, transaction channels or risk bands), and summarises its behaviour in
one call. `States` lists the distinct labels in alphabetical order, indexing every other array; `TransitionCounts`
and `TransitionProbabilities` (counts plus `smoothing`, default 0, divided by the row total) describe the chain;
`StationaryDistribution` is the long-run time share per state (`StationaryIsUnique` is false when several separate
recurrent classes exist); `ExpectedReturnSteps` is 1 divided by that share; `MostLikelyNextState` and
`EntropyRateBits` summarise predictability; `AbsorbingStates` lists states the chain never leaves; and with no
smoothing, a state that only appears at the end of the sequence (no observed exit) is treated as self-absorbing and
listed separately in `StatesWithoutOutgoingTransitions`. Needs at least 2 states in the sequence, and at most 12
distinct states (`MarkovMath.MaximumStates`) — group rare states first if the raw alphabet is larger.

**MarkovChainStationaryDistribution** computes the long-run distribution of a chain whose transition matrix is
already known (each row summing to 1), rather than estimated from data. `IsUnique` is true only when the chain has a
single recurrent class, in which case the distribution does not depend on the starting state; with several recurrent
classes the result is an average from a uniform start, flagged in `Note`. It also reports `RecurrentClasses`,
`TransientStates`, `AbsorbingStates` and `ExpectedReturnSteps` (1 / stationary probability). Limited to 500 states.

**MarkovChainMultiStepProbabilities** projects the state distribution forward a given number of `steps` (0 to
1,000,000) from either `startState` (a zero-based index) or `startDistribution` (a probability vector) — exactly one
of the two, not both. `StepMatrix` is the transition matrix raised to the power `steps` (computed by binary
exponentiation, so large step counts are cheap), and `Distribution` is the projected state probabilities. Use it to
project where an account or case is likely to be in 3 or 12 periods from now.

**MarkovChainSequenceSurprise** scores how unusual a new (`testSequence`) sequence of states is against a Markov
chain learned from a reference (`trainingSequence`), with additive `smoothing` (default 1, must be above 0 so
unseen moves are not impossible) over the union of states seen in either sequence. `SurprisalBits[t]` is minus
log2 of the probability of each observed transition (0 bits certain, 1 bit a coin flip); `AverageBitsPerTransition`
and `Perplexity` (2 to that power) summarise the whole test sequence; `MostSurprisingTransition` and
`UnseenTransitions` (moves never seen in training) round it out. Use it to flag behaviour that departs from a
learned pattern of state-to-state movement.

**MarkovChainMeanFirstPassageTimes** computes, for a chain with a known transition matrix, the expected number of
steps to first reach each target state from each starting state. `MeanFirstPassageSteps[i][j]` is that expectation;
the diagonal `[j][j]` is the mean recurrence time, equal to 1 / stationary probability for an irreducible chain. An
entry is infinity when the target state cannot always be reached from the source. Answers questions like "on average
how many periods until an active account becomes dormant?" Limited to 500 states.

**MarkovChainAbsorptionAnalysis** analyses an absorbing chain — one that ends in one of several final states, each
with probability 1 of staying put, for example a case closed as confirmed, dismissed or escalated. It returns
`ExpectedStepsToAbsorption` per transient state, `AbsorptionProbabilities[i][k]` (the chance of ending in the k-th
absorbing state from the i-th transient one, each row summing to 1) and `ExpectedVisits` (the fundamental matrix).
It throws if no state is absorbing (use `MarkovChainStationaryDistribution` instead for a chain that never ends) or
if a recurrent class is not a single absorbing state. Limited to 500 states.

**MarkovDependenceTest** is a chi-square test of whether the next state in a sequence depends on the current one at
all — whether a Markov model has anything to add over treating states as independent draws. It forms a
current-state-by-next-state contingency table from the sequence and tests it for independence; a small `PValue`
(with `Dependent` true) means real state-to-state structure exists, and `CramersV` (0 to 1) measures its strength.
`Note` warns when many expected cell counts fall below 5, where the chi-square approximation becomes unreliable.
Needs at least 11 state labels with at least 2 distinct current and next states.

## SequencePatternTools.cs

Nine tools for categorical and numeric sequence pattern analysis: n-gram frequency, entropy and complexity measures,
run-length encoding, dimensionality-reducing approximations (PAA and SAX), and two sequence-alignment distances (DTW
and Levenshtein edit distance). Symbol sequences are compared exactly (ordinal, case-sensitive) throughout.

**NGramFrequencyTable** counts the most frequent runs of `n` (default 2) consecutive symbols in an ordered categorical
sequence, such as transaction types, channels or event codes, to find recurring behavioural patterns (for example a
typical three-step path before a chargeback). `NGrams[i]`, `Counts[i]` and `Shares[i]` describe the `topK` (default
20) most frequent patterns, ties broken alphabetically; `DistinctNGrams` and `TotalNGrams` (length minus n plus 1)
describe the whole table. Needs at least `n` symbols.

**SymbolSequenceEntropyProfile** computes the Shannon entropy profile of a symbol sequence: how unpredictable it is
on its own (`BlockEntropiesBits`, entropy of blocks of k symbols) and once earlier symbols are known
(`ConditionalEntropiesBits`, which falls toward 0 as the sequence becomes more predictable from its history).
`NormalisedFirstOrderEntropy` divides entropy by log2 of the alphabet size (NaN for a one-symbol alphabet) and
`Redundancy` is 1 minus it. Use it to separate rigid, scripted or automated behaviour (low entropy) from varied human
behaviour. Estimates from short sequences are biased low, especially at higher orders; `maxOrder` (default 3) is
reduced automatically to fit the sequence length.

**LempelZivSequenceComplexity** computes the Lempel-Ziv (1976) complexity of a symbol sequence — the number of
distinct new phrases encountered when parsing left to right — as a model-free, compression-based measure of
patternedness: repetitive sequences score low, irregular ones high (the classic example `0001101001000101` scores
`PhraseCount` 6). `NormalisedComplexity` is `PhraseCount x log(n) / n` in a logarithm base equal to the alphabet size
(NaN for a single-symbol alphabet), close to 1 for random sequences and near 0 for highly repetitive ones. Needs at
least 2 symbols; reliable normalised values need a few dozen or more.

**RunLengthEncodeSequence** run-length encodes a sequence, collapsing each unbroken stretch of the same symbol into
one run, to measure how long a customer stays in one status or repeats an action. It returns `Symbols`, `Lengths` and
`StartIndices` per run plus `RunCount`, `LongestRun` and `MeanRunLength`. Needs at least 1 symbol.

**ThresholdRunLengths** finds unbroken streaks of numeric values that satisfy a threshold condition — `Above`,
`AtOrAbove`, `Below` or `AtOrBelow` (default `Above`) — such as consecutive days with volume above a limit, to
measure persistence of an alert condition. `StartIndices` and `Lengths` describe each streak with `RunCount`,
`LongestRun`, `MeanRunLength` and `ShareOfPointsInRuns`; missing (`NaN`) values never satisfy the condition and break
a streak. Returns zeros when no value qualifies.

**PiecewiseAggregateApproximation** (PAA) shrinks a series to a fixed number of `segments` (1 to the series length)
by averaging equal-width pieces, points straddling a segment boundary split proportionally, giving a fixed-length
shape summary regardless of input length. For `[1, 2, 3, 4, 5, 6]` with 3 segments the result is `[1.5, 3.5, 5.5]`.
Use it to compare series of different lengths, build fixed-size features, or as the first step of SAX.

**SymbolicAggregateApproximation** (SAX) turns a numeric series into a short word of letters: the series is
optionally standardised to mean 0 and standard deviation 1 (`standardise`, default true), reduced to `segments`
PAA averages, and each average mapped to a letter using breakpoints that split a standard normal distribution into
`alphabetSize` (2 to 20, default 4) equally likely bins. `Word`, `Symbols`, `Paa`, `Breakpoints`, `Mean` and
`StandardDeviation` are all returned. A constant series cannot be standardised and is mapped to the middle letters.
Use it to compare series shapes, feed `NGramFrequencyTable`, or precede other sequence tools.

**DynamicTimeWarpingDistance** (DTW) finds the smallest total cost of aligning two numeric series when either may be
stretched or compressed in time, so shapes that are shifted or run at different speeds still match. Point cost is
absolute (default) or squared (`squaredCost`) difference; `bandRadius` (default -1, meaning unrestricted) limits how
far an alignment may drift from the diagonal, widened automatically to at least the difference in the two lengths.
Returns `Distance`, `NormalisedDistance` (divided by path length) and `Path` (the aligned index pairs). Identical
series give 0. The work scales with the product of the two lengths, capped at 25 million cells — reduce the series
first (for example with `PiecewiseAggregateApproximation`) if that is exceeded.

**SymbolSequenceEditDistance** computes the Levenshtein edit distance between two ordered symbol sequences: the
fewest single-symbol insertions, deletions and substitutions to turn one into the other, for comparing behavioural
paths or customer-journey steps. `Distance` is the raw count, `NormalisedDistance` divides by the longer sequence's
length (0 identical, 1 completely different) and `Similarity` is 1 minus that. Either sequence may be empty; both
empty give distance 0 and similarity 1.

## SeriesAnomalyTools.cs

Eight tools for detecting change, drift and anomalies in a numeric series: four control-chart style monitors (CUSUM,
Page-Hinkley, EWMA, Shewhart), a general-purpose spike detector, a level-shift scanner, a binary-segmentation
change-point finder, and drawdown analysis. The control-chart tools share a `Baseline` helper: when `targetMean` or
`standardDeviation` is left `NaN` (the default for both), they are estimated from the first `baselineCount` values
(0 default means the first half of the series, up to 30 points), and the call throws if the resulting baseline
standard deviation is 0 or undefined (a constant or too-short baseline).

**CusumChangeDetection** is a two-sided CUSUM control chart that detects a sustained shift in a series' mean, up or
down, earlier than a single-point threshold would. Each value contributes its deviation from the target minus a
slack allowance `kSigma` (default 0.5) standard deviations to a running sum floored at 0; an alarm fires when a sum
exceeds `hSigma` (default 5) standard deviations, after which both sums reset. `UpperSums` and `LowerSums` are
reported per position, and `AlarmIndices` come with `AlarmDirections` (`Up`/`Down`) and `EstimatedChangeStarts`
(where the alarming sum last left 0). Default settings detect a one-sigma shift within roughly 10 points.

**PageHinkleyDriftDetection** is the Page-Hinkley test for drift, suited to online monitoring with no fixed baseline:
it compares each value with the running average of everything since the last alarm. `delta` (default 0) is the
magnitude of change tolerated without alarm; `lambda` (default 5, in series units) is the alarm threshold.
`IncreaseStatistic` and `DecreaseStatistic` accumulate rise and fall evidence; an alarm in either direction resets
the running mean and both accumulators. `AlarmIndices` and `AlarmDirections` report the hits.

**EwmaControlChart** smooths a series with weight `lambda` (default 0.2, above 0 and at most 1) on the newest value
and alarms when the smoothed value leaves control limits of `targetMean` plus or minus `limitWidth` (default 3)
standard errors, catching small persistent shifts that a Shewhart chart misses while still reacting to large ones.
`Ewma`, the time-varying `UpperLimits`/`LowerLimits` (which widen to a steady width over the first few points) and
`AlarmIndices` are all returned. Typical settings are `lambda` 0.05 to 0.25.

**ShewhartControlChartAlarms** applies the four standard Western Electric run rules to a series standardised against
a target and standard deviation (`ZScores`): Rule 1, a point beyond 3 sigma; Rule 2, 2 of 3 consecutive points beyond
2 sigma on the same side; Rule 3, 4 of 5 beyond 1 sigma on the same side; Rule 4, 8 consecutive points on one side.
Each rule's hits are reported in its own array plus a unioned, ordered `AlarmIndices`.

**SeriesSpikeDetection** flags spikes and dips with a choice of `method` (default `RollingRobustZScore`):
`RollingZScore` (mean/standard deviation of the preceding `window`, default 30), `RollingRobustZScore` (median and
1.4826 x median absolute deviation of the preceding window, resistant to outliers distorting their own baseline),
`GlobalRobustZScore` (the whole series' median and deviation) or `InterquartileFence` (distance beyond Q1/Q3 in
interquartile ranges; 1.5 is Tukey's fence, 3 is "far out"). `Scores` has one entry per value (`NaN` where no
baseline exists yet, such as the first window); `SpikeIndices` and `SpikeValues` list positions whose absolute score
exceeds `threshold` (default 3.5). A zero-spread baseline scores any different value as infinite.

**LevelShiftScan** locates level shifts — a step up or down that persists, as distinct from a one-off spike — by
comparing the mean of the `window` (default 10) before each point with the mean of the window starting there, via a
Welch t-type score. `Scores[t]` is defined only where a full window exists on both sides; `ShiftIndices` (the
strongest points exceeding `threshold`, default 5, no two closer than one window apart) come with `ShiftSizes` and
`ShiftScores`. `ShiftIndices[i]` marks the first observation at the new level. Needs at least twice the window.

**MeanShiftChangePoints** splits a series into segments of roughly constant mean by binary segmentation on sum of
squared errors, without choosing a window in advance. The noise level is estimated robustly from the median absolute
first difference; a split survives only if it beats a BIC-style penalty scaled by `penaltyMultiplier` (default 1;
above 1 is more conservative) and both resulting parts have at least `minSegmentLength` (default 5) points. Splits
are added greedily, largest gain first, up to `maxChangePoints` (default 5). `ChangePointIndices` with `Gains` and
`Segments` (start, end, size, mean of each piece) describe the result; an empty change-point list means no
significant shift was found.

**DrawdownAnalysis** measures, for a level series such as an account balance or cumulative total, how far it falls
below its previous high-water mark and for how long. `Drawdowns[t]` is the level minus the running maximum up to t
(0 at a new high); `MaximumDrawdown` (in level units) and `MaximumDrawdownFraction` (of the peak it fell from, `NaN`
if that peak is 0 or negative) locate the worst fall via `PeakIndex` and `TroughIndex`; `RecoveryIndex` is the first
later point back at that peak level (-1 if it never recovered); `LongestUnderwaterSteps` and
`CurrentUnderwaterSteps` (the run in progress at the series end) measure duration.

## SeriesDependenceTools.cs

Fourteen tools for testing and measuring dependence, randomness, trend and seasonality in a series: the correlogram
family (ACF, PACF, cross-correlation, Ljung-Box, Box-Pierce, Durbin-Watson), two non-parametric randomness tests
(turning points, median runs), a trend test (Mann-Kendall), classical seasonal decomposition and its derived
seasonality-strength and period-estimation tools, and two regression-based lag tests (ARCH effects, Granger
causality).

**AutocorrelationFunction** (ACF) computes the sample autocorrelation at lags 0 to `maxLag` (default 20, reduced to
series length minus 1 if longer): `Values[0]` is always 1. `WhiteNoiseBound` is the plain significance band for pure
noise; `BartlettBounds` is a wider, lag-specific band that allows for correlation already seen at shorter lags;
`SignificantLags` lists lags beyond the plain bound. A constant series returns `NaN` above lag 0. Use it to see
whether a value depends on earlier ones, to spot repeating cycles (a peak at lag 7 in daily data is a weekly
pattern), or to check that residuals are white noise.

**PartialAutocorrelationFunction** (PACF) computes, via the Durbin-Levinson recursion from the ACF, the correlation
at lag k after removing the effect of the shorter lags in between — used to choose the order of an autoregressive
model, since an AR(p) process's PACF cuts off to about zero after lag p while its ACF tails off gradually. `Values`
start at lag 1 (unlike `AutocorrelationFunction`, which includes lag 0); needs at least 4 finite values and returns
`NaN` for a constant series.

**CrossCorrelationFunction** computes the correlation between two equal-length series at lags from `-maxLag` to
`+maxLag` (default 10), to find whether one leads the other and by how many steps, for example card-present spend
leading online spend. A positive lag's large correlation means `x` leads `y`; a negative lag's means `y` leads `x`.
`BestLag`/`BestCorrelation` report the strongest absolute match; `Bound` and `SignificantLags` flag significance. A
constant series on either side returns `NaN` everywhere.

**LjungBoxAutocorrelationTest** is the standard portmanteau test that a series (or a model's residuals) has no
autocorrelation up to `lags` (default 10): `Statistic` (Q) follows chi-square with `DegreesOfFreedom = lags -
modelDegreesOfFreedom` (set the latter, default 0, to the number of fitted ARMA parameters when testing residuals).
A small `PValue` means autocorrelation is present. `StatisticByLag`/`PValueByLag` give the running test at every lag.
Requires `lags` smaller than the series length; prefer this over `BoxPierceAutocorrelationTest` unless the exact
older statistic is specifically needed.

**BoxPierceAutocorrelationTest** is the original, less accurate form of the same portmanteau test
(`Q = n x sum of squared autocorrelations`), kept for when that exact statistic, rather than the more accurate
Ljung-Box correction, is required. Same parameters and result shape as `LjungBoxAutocorrelationTest`.

**DurbinWatsonResidualAutocorrelation** computes the classic Durbin-Watson statistic on an ordered series of model
residuals (sum of squared successive differences over sum of squared values, with residuals **not** centred first,
per the textbook definition): near 2 means no first-order autocorrelation, well below 2 means positive
autocorrelation, well above 2 (toward 4) means negative. `ImpliedFirstOrderAutocorrelation` is approximately
`1 - Statistic/2`; `Reading` labels the result using cut-offs of 1.5 and 2.5. Returns `NaN` if every residual is
zero.

**TurningPointRandomnessTest** is a distribution-free check that a series (or residuals) is random: it counts local
peaks and troughs and compares the count with the number expected under independence (`2(n-2)/3`, variance
`(16n-29)/90`). `ZScore` and the normal-approximation `PValue` flag departure; too few turning points means smooth
persistence or trend, too many means alternating (oscillating) behaviour. Ties do not count as turning points; needs
at least 4 values, with the normal approximation most reliable from about 10 upward.

**MedianRunsRandomnessTest** is the Wald-Wolfowitz runs test on the sequence of values above and below the series
median: a large negative `ZScore` means clustering or trend, a large positive one means alternation. Values equal to
the median are ignored, and the call throws if every remaining value falls on only one side (a constant or
near-constant series).

**MannKendallTrendTest** is the non-parametric Mann-Kendall test for a monotonic trend, paired with the robust
Theil-Sen slope. `S` is the sum of signs of all later-minus-earlier pairwise differences; `ZScore` uses a
tie-corrected, continuity-corrected variance; `KendallTau` rescales `S` to -1..1; `SenSlopePerStep` is the median of
all pairwise slopes (robust to outliers) and `SenIntercept` its matching intercept. `Trend` reads `Increasing`,
`Decreasing` or `NoTrend` at the chosen `alpha` (default 0.05). Assumes independent observations — serial
correlation inflates significance. Limited to 3 to 4000 values; for longer series, aggregate first (for example
with `ResampleEventsToBuckets`).

**ClassicalSeasonalDecomposition** splits an evenly spaced series into trend (a centred moving average over one
full `period`), a repeating `SeasonalIndices`/`Seasonal` pattern, and a remainder, under an `Additive` (default,
`value = trend + seasonal + remainder`) or `Multiplicative` (`value = trend x seasonal x remainder`, requiring every
value above 0) model. `SeasonalStrength` and `TrendStrength` (0 to 1) summarise how much each component explains.
Needs at least two full periods of data; the trend is `NaN` for the first and last half-period.

**SeasonalityStrengthScore** is a thin wrapper around `ClassicalSeasonalDecomposition` that returns just
`SeasonalStrength`, `TrendStrength` and a plain-language `Reading` (`Strong`/`Moderate`/`Weak or no seasonality` at
cut-offs of 0.6 and 0.3), to quickly decide whether a seasonal method or adjustment is worthwhile.

**EstimateSeasonalPeriodByAutocorrelation** estimates the length of a repeating cycle from the local peaks of the
series' autocorrelation function, for when the period is not known in advance (for example whether activity repeats
weekly or every 30 days). The series is optionally de-trended first (`detrend`, default true, a fitted straight
line) so a trend does not mask the cycle. `Period` is the lag of the strongest significant peak up to `maxPeriod`
(default 0, meaning half the series length); `Found` is false (`Period` 0) if no peak clears the white-noise bound.
The answer is in observations, not calendar time — resample an irregular event stream to a regular grid first (see
`ResampleEventsToBuckets`).

**EngleArchLmTest** is Engle's Lagrange-multiplier test for ARCH effects (volatility clustering) in residuals or
returns: large movements tend to follow large movements regardless of sign. It regresses squared values on `lags`
(default 5) of themselves; `Statistic` (usable rows times the regression R-squared) follows chi-square under the
null of no ARCH effect, and a small `PValue` means volatility clusters (`Significant` true). Supply mean-removed
residuals; returns `NaN` if the squared values are constant.

**GrangerCausalityTest** tests whether past values of a candidate `cause` series improve prediction of an `effect`
series beyond the effect series' own past, via an F test comparing two regressions (with and without the cause's
lags). `Statistic`, `PValue`, `DegreesOfFreedomNumerator` (= `lags`, default 2) and `DegreesOfFreedomDenominator`
describe the test; `Significant` flags predictive improvement at `alpha` (default 0.05). This is predictive
precedence, not true causation, and both series should be stationary first (difference a trending series before
calling it). Requires equal-length series longer than `3 x lags + 1`.

## SeriesForecastingTools.cs

Ten tools for point forecasting with prediction intervals, model fitting, and forecast evaluation: five benchmark and
exponential-smoothing methods, autoregressive (AR) model fitting/order-selection/forecasting, accuracy scoring, and a
hold-out comparison that runs several methods and ranks them. All forecasting tools return a `SeriesForecastResult`
shape with `Forecast`, `Lower`/`Upper` prediction bounds at `confidenceLevel` (default 0.95), `Fitted` in-sample
one-step predictions, `ResidualStandardDeviation`/`sigma`, and named `ParameterNames`/`ParameterValues`; `horizon` is
capped at 1–10000 steps throughout.

**BaselineSeriesForecast** implements five textbook benchmarks that any more elaborate model should beat: `Naive`
(repeat the last value), `SeasonalNaive` (repeat the value one cycle earlier; needs `seasonalPeriod` of 2 or more and
at least one full cycle), `Mean` (the overall average), `Drift` (extend the line joining the first and last values)
and `MovingAverage` (average of the last `window`, default 3, values). Interval widths use each method's standard
closed-form variance formula (for example Naive widens as `sigma x sqrt(h)`, Drift as `sigma x sqrt(h(1+h/n))`).

**SimpleExponentialSmoothingForecast** produces a flat forecast equal to an exponentially weighted average of the
history, for series with no trend or seasonality. `alpha` (default `NaN`, estimated by minimising one-step squared
error) weights the newest value; the level starts at the first value. Interval width grows as
`sigma x sqrt(1 + (h-1) x alpha^2)`.

**HoltLinearTrendForecast** tracks a level and a slope that both adapt over time (Holt's linear trend method), so
the forecast continues the recent trend as a straight line, or, with `phi` (damping factor, default 1) below 1, a
trend that flattens out. `alpha`/`beta` default to `NaN` (both estimated together); the level starts at the first
value and the slope at the first difference, so a perfectly linear series is reproduced exactly.

**HoltWintersSeasonalForecast** adds a repeating seasonal component to Holt's method, for series with both trend and
a cycle (weekly cycles in daily counts, daily cycles in hourly counts). `model` is `Additive` (default, constant
seasonal swing) or `Multiplicative` (swing grows with the level; requires every value above 0); `alpha`/`beta`/`gamma`
default to `NaN` (all three estimated jointly). State is initialised from the first two cycles, so at least two full
cycles of history are required; a series that is exactly periodic with no trend is reproduced exactly. Multiplicative
intervals are approximate (they reuse the additive-form variance).

**LinearTrendForecast** extrapolates an ordinary-least-squares straight line against the time index, with exact
prediction intervals from the t distribution (`n - 2` degrees of freedom). Suited to a steady, roughly linear drift
with constant-variance noise; ignores seasonality and assumes independent errors.

**AutoregressiveModelFit** fits an AR(`order`) model — each value a weighted sum of the previous `order` values plus
noise — by `YuleWalker` (default; from sample autocovariances, always stationary) or `LeastSquares` (regression on
lagged values with an intercept). `Coefficients[k-1]` multiplies the value k steps earlier; `Mean` is the implied
long-run level (`NaN` if the coefficients sum to 1, a unit root); `Aic`/`Bic` support comparing orders fitted the
same way on the same data. For `[1, 2, 3, 4, 5]` with order 1, the least-squares fit is exact.

**AutoregressiveOrderSelection** fits AR orders 0 to `maxOrder` (default 10) by Yule-Walker and compares them by AIC
and BIC, returning `BestOrderByAic` and `BestOrderByBic` (order 0 means no dependence on the past; BIC favours
smaller models). Requires more than `2 x maxOrder` points.

**AutoregressiveForecast** forecasts with a fitted AR(`order`) model: each future value uses the previous `order`
values, falling back to earlier forecasts once the real data run out, so forecasts for a stationary series revert
toward the long-run mean. Prediction intervals use the model's psi (moving-average) weights, so they widen with
horizon and then level off. For a trending series, difference it first (`DifferenceSeries`) or use a trend method
instead.

**ForecastAccuracyMetrics** scores a set of forecasts against what actually happened: `MeanError` (signed, positive
means under-forecasting), `MeanAbsoluteError`, `MedianAbsoluteError`, `MeanSquaredError`/`RootMeanSquaredError` (series
units), `MeanAbsolutePercentageError` (a fraction; skips zero actuals, counted in `PercentageErrorsSkipped`),
`SymmetricMeanAbsolutePercentageError`, and `MeanAbsoluteScaledError` (mean absolute error scaled by the mean
absolute naive — or seasonal-naive, via `seasonalPeriod` — error on optional `trainingValues`; below 1 beats the
naive method; `NaN` if `trainingValues` is empty or constant).

**HoldOutForecastComparison** picks a forecasting method empirically: it hides the last `holdOut` points (at most a
third of the series), fits `Naive`, `Mean`, `Drift`, `SimpleExponentialSmoothing`, `HoltLinearTrend`, `LinearTrend`,
an AR model (order chosen by BIC), and, when `seasonalPeriod` (default 1, meaning none) is 2 or more and there is
enough history, `SeasonalNaive` and `HoltWintersAdditive`, on the earlier history, and scores each against the hidden
points. `Scores` is sorted by RMSE (best first) with each method's forecast attached; `BestMethodByRmse` names the
winner and `Skipped` explains methods that could not run (too little data, or a non-finite forecast). Requires at
least `3 x holdOut` and 8 points in total.

## SeriesTransformTools.cs

Twenty-two general-purpose series transforms and filters: lag/lead and differencing operators, cumulative and
percentage-change series, moving-average and exponential-smoothing families, rolling and expanding window statistics
(a shared fourteen-statistic vocabulary: Mean, Sum, Minimum, Maximum, Median, StandardDeviation, Variance, Range,
MedianAbsoluteDeviation, Skewness, Kurtosis, Count, Quantile, CoefficientOfVariation), detrending, missing-value
filling, three denoising filters (median, Hampel, Savitzky-Golay), the Box-Cox power transform, a Kalman local-level
filter, and a one-call series profile.

**DifferenceSeries** replaces each value with its change from `lag` (default 1) steps earlier, optionally repeated
`order` (default 1) times, to remove a trend (order 1) or a repeating pattern (seasonal `lag`, for example 7). Output
length matches input; the first `order x lag` positions are `NaN`. Differencing a straight line once gives a
constant; twice gives zeros.

**LagSeries** shifts a series so each position holds the value `lag` (default 1) steps earlier, or, with a negative
lag, from later (a lead), to align a series with its own past or future for feature building. Positions with no
source value are `NaN`.

**LaggedFeatureMatrix** turns a series into a supervised-learning table: `Features[i][j]` is the value at `Lags[j]`
steps before the target of row i, `Targets[i]` the series value at `RowIndices[i]`. Rows start once the largest lag
is available. Use it to feed ordinary regression or classification with autoregressive features, for example lags 1,
2 and 7 of daily volume.

**CumulativeSeries** computes a running `Sum` (default, Kahan-compensated for numerical stability), `Product`,
`Maximum`, `Minimum` or `Mean` at each position. A `NaN` input makes `Sum`/`Product`/`Mean` `NaN` from that point
onward, while `Maximum`/`Minimum` ignore `NaN`. Use the cumulative sum for a running balance, maximum for a
high-water mark.

**PercentChangeSeries** computes relative change over `periods` (default 1) steps — `value / earlier value - 1`, so
0.10 means up 10 percent — for growth rates of volumes or balances; for prices, prefer `LogReturnSeries`. `NaN` at the
first `periods` positions and wherever the earlier value is 0 or `NaN`.

**LogReturnSeries** computes the natural-log return of a strictly positive series over `periods` steps, which adds up
over time and is approximately symmetric — the usual input for volatility and spike work. `NaN` wherever either value
is 0, negative or missing.

**SimpleMovingAverageSeries** averages a sliding `window` of values, with `alignment` (`Trailing` default, `Centered`
or `Leading`) controlling where the window sits and `minPeriods` (0 default, meaning the full window) controlling how
much of a partial window at the edges is tolerated. Missing values inside the window are skipped.

**WeightedMovingAverageSeries** computes a weighted average of the latest values using caller-supplied `weights`
(oldest to newest, normalised by their sum internally), so recent points can count more, for example `[1, 2, 3]` for
a linear-weighted three-point average. Throws if the weights sum to (near) zero.

**ExponentiallyWeightedSmoothingSeries** computes a series where recent observations fade in weight geometrically
rather than dropping out of a hard window. Give exactly one of `alpha` (weight on the newest value) or `span`
(roughly the number of periods averaged; `alpha = 2/(span+1)`). `adjust` false (default) uses the simple recursive
form starting from the first value; `adjust` true divides by the running sum of weights, removing start-up bias.

**ExponentialMovingStatisticsSeries** tracks an exponentially weighted mean and standard deviation recursively, so
recent behaviour dominates — an adaptive baseline for comparing each new value with the running mean in units of the
running standard deviation. Give exactly one of `alpha` or `span`; the first standard deviation is 0.

**RollingStatisticSeries** computes any of the fourteen window statistics (see section intro) over a sliding
`window`, with `alignment`, `minPeriods` and, for `Quantile`, the `quantile` argument (default 0.5). `Count` always
reports the finite count regardless of `minPeriods`; statistics needing more points than are present (for example
standard deviation of one value) are `NaN`.

**ExpandingStatisticSeries** computes the same statistic vocabulary but over everything from the start of the series
up to and including each position, for a running mean, running volatility, or any statistic of history accumulated so
far. `minPeriods` (default 1) delays the first reported value. Order-based statistics (`Median`,
`MedianAbsoluteDeviation`, `Quantile`, `Skewness`, `Kurtosis`) are capped at 20,000 values — use
`RollingStatisticSeries` with a window for longer series.

**RollingCorrelationSeries** computes the Pearson correlation between two aligned series over a trailing `window`
(at least 3), to watch whether two metrics that normally move together have decoupled. Pairs with a missing value on
either side are dropped; a position is `NaN` with fewer than 3 valid pairs, before the window fills, or when either
side is constant within the window.

**RollingZScoreSeries** computes how many standard deviations each value sits from the mean of its preceding
`window` (at least 2), to flag values unusual relative to recent history (for example a payment far above a
customer's last 30). By default (`includeCurrent` false) the baseline excludes the current value, so a spike does
not dilute its own baseline. `NaN` until `minPeriods` values are available, when the window's standard deviation is
0, or when the current value itself is missing.

**DetrendSeries** removes a trend so what remains is the fluctuation around it: `Mean` (subtracts the average),
`Linear` (default; subtracts a fitted straight line) or `Polynomial` (degree 2 to 6, fitted by least squares against
a time index scaled to `[-1, 1]`). Returns `Detrended`, `Trend`, `Intercept`/`SlopePerStep` (both `NaN` for
`Polynomial`) and `RSquared` (the trend's explained variance share). Requires more points than the fit has
parameters.

**FillMissingValuesInSeries** fills `NaN`/infinite gaps with `LinearInterpolation` (default; straight line between
neighbours), `ForwardFill`, `BackwardFill`, `NearestValue`, `SeriesMean`, `SeriesMedian` or a supplied `Constant`.
Leading or trailing gaps a directional rule cannot reach fall back to the nearest known value for interpolation and
stay `NaN` for the fill methods, counted in `RemainingMissing`; `FilledIndices` lists what was actually filled. Use
it to make a series finite before autocorrelation, spectral or forecasting tools, which reject missing values.

**MedianFilterSeries** replaces each value with the median of a small, odd-length `window` (at least 3, default 3)
centred on it, removing isolated spikes and dropouts while keeping genuine level shifts sharp, unlike a moving
average which smears them. Edges repeat the first/last value to fill the window.

**HampelOutlierFilter** flags and replaces spikes by comparing each value with the median of its `halfWidth`
(default 3, points on each side) neighbourhood, scaled in robust standard deviations (1.4826 x median absolute
deviation). A value beyond `nSigmas` (default 3) robust deviations is an outlier; `Cleaned` replaces outliers with
the local median, `OutlierIndices`/`OutlierScores` (infinite if the local deviation is exactly 0) and `LocalMedians`
describe them. Less fooled by the outliers themselves than a mean-and-standard-deviation approach would be.

**SavitzkyGolayFilter** smooths a series, or estimates its first or second `derivative`, by fitting a low-order
polynomial (`polynomialOrder`, default 2, below `windowLength`) to a sliding odd `windowLength` (default 5) by least
squares — unlike a moving average, it preserves peaks and sharp features. `samplingInterval` (default 1) scales
derivative units. Near the series ends, the polynomial fit to the nearest full window is evaluated at the edge
position. A polynomial of degree up to `polynomialOrder` is reproduced exactly.

**BoxCoxTransformSeries** applies the Box-Cox power transform `(x^lambda - 1)/lambda` (natural log at `lambda = 0`)
to a strictly positive series, to stabilise variance and symmetrise skewed data (amounts, counts, durations) before
modelling. `lambda` 1 is a shift, 0.5 a square root, -1 a reciprocal; left `NaN` (default), it is chosen automatically
by maximising the Box-Cox profile log-likelihood between -3 and 3 (`LambdaEstimated` then true). Every value must be
above 0 and finite — add an offset first if any are 0 or negative.

**KalmanLocalLevelFilter** runs a Kalman filter for a local-level model (a slowly wandering hidden level plus
measurement noise), given `processVariance` (0 or more; drift per step) and `measurementVariance` (above 0;
observation noise). `FilteredLevels`/`FilteredVariances` are the estimate and uncertainty after each observation;
`OneStepPredictions`, `Innovations` and `StandardisedInnovations` (values beyond about 3 are unusual) describe
forecast surprises; `LogLikelihood` and `SteadyStateGain` (the limiting weight on a new observation) summarise the
fit. With `processVariance` 0, the level is simply the running mean.

**SeriesProfile** is a quick diagnostic overview to decide what to do with an unfamiliar series next: length,
missing count, range, mean, spread, whether it is constant or monotonic, linear trend slope, lag-1 autocorrelation, a
Mann-Kendall trend p-value, zero and negative counts, the longest run of identical values, and a suggested seasonal
period (0 if none found, via `EstimateSeasonalPeriodByAutocorrelation`). `Notes` lists plain-language observations
such as "contains missing values" or "strong trend is present". Missing values are counted and excluded from the
other figures; call this first, then the specific tool it points to.

## SpectralSignalTools.cs

Sixteen tools for frequency-domain and signal analysis: the forward and inverse discrete Fourier transform, two
power-spectrum estimators (periodogram and Welch), dominant-period extraction, a spectral shape summary, a
periodicity significance test, convolution and template matching, the Hilbert envelope/phase transform, the discrete
cosine transform, Haar wavelet decomposition and denoising, a frequency-domain brick-wall filter, zero-crossing
analysis and peak/trough detection.

**FourierTransformOfSeries** computes the full discrete Fourier transform of an evenly spaced series of any length,
splitting it into sinusoids. `Real`/`Imaginary` are the unnormalised complex transform (n coefficients for n input
values); the one-sided `Frequencies` (bins 0 to n/2, in cycles per `samplingIntervalSeconds`, default 1),
`Magnitudes`, `Amplitudes` (the equivalent cosine wave's amplitude) and `Phases` cover the physically meaningful half.
Bin 0 is the mean level.

**InverseFourierTransform** is the counterpart: it reconstructs a series from complex frequency coefficients supplied
in the same `Real`/`Imaginary` layout `FourierTransformOfSeries` produces (bin 0 first, negative frequencies in the
upper half), typically after editing coefficients (for example zeroing high frequencies) to get a filtered series
back. `Imaginary` in the result is close to 0 when the input coefficients had conjugate symmetry.

**PowerSpectrumOfSeries** computes the one-sided power spectrum (periodogram): the series is de-trended (`detrend`,
default `Mean`, or `Linear`) and optionally windowed (`window`: `None` default, `Hann`, `Hamming`, `Blackman`, to
reduce spectral leakage) before transforming. `Power[k]` is scaled so a cosine of amplitude A exactly on a bin has
power `A^2 n / 2`; `PeakFrequency`/`PeakPeriod`/`PeakPower` identify the strongest non-zero-frequency bin and
`TotalPowerExcludingDc` sums the rest. Cycles longer than the series length cannot be resolved.

**WelchPowerSpectrum** averages periodograms of overlapping `segmentLength` (at least 8) segments — each
de-trended, windowed (`window` default `Hann`) and transformed — for a smoother, more reliable spectrum from long,
noisy series, at the cost of coarser frequency resolution (bin spacing `1/(segmentLength x interval)`).
`overlapFraction` (default 0.5) sets segment overlap and `Segments` reports how many were averaged.

**DominantPeriodsOfSeries** finds the strongest repeating cycles from local peaks of the power spectrum (excluding
the zero-frequency mean), ranked by power, up to `topK` (default 3). `Periods`/`PeriodsInSamples`/`Frequencies` locate
each peak with `Powers` and `PowerShares`; a cycle that does not fit a whole number of times leaks into neighbouring
bins (use a `window` to reduce this), and long periods are coarsely spaced by the frequency resolution. May return
fewer than `topK` entries.

**SpectralSummary** reduces a spectrum's shape to a handful of numbers that separate noise-like, cyclic and smooth
signals: `SpectralEntropyBits`/`NormalisedSpectralEntropy` (near 1 for white noise, near 0 for one pure cycle),
`SpectralFlatness` (geometric over arithmetic mean power; 1 is perfectly flat, near 0 is peaky), `CentroidFrequency`
(power-weighted mean frequency), `RolloffFrequency` (below which 85 percent of power lies), and
`DominantFrequency`/`DominantPeriod`/`DominantPowerShare`. The zero-frequency mean is excluded and the series is
mean-removed first; a series with no variation returns `NaN` ratios.

**FisherPeriodicityTest** is Fisher's exact g test for one hidden periodicity against a white-noise null: `g` is the
largest periodogram ordinate divided by the sum of all ordinates (excluding the mean and any Nyquist bin). `PValue`
is the exact probability of a g at least that large under white noise; `Significant` flags it below `alpha` (default
0.05); `PeakBin`/`PeakPeriodInSamples` locate the candidate cycle. The test targets a single strong cycle — several
equal-strength cycles dilute `g`. Needs at least 8 values.

**LinearConvolution** convolves a series with a `kernel`: `Full` (default; all `n + m - 1` overlapping positions),
`Same` (the central n values aligned with the input) or `Valid` (only positions of full overlap). For example
`[1, 2, 3]` convolved with `[0, 1, 0.5]` gives `[0, 1, 2.5, 4, 1.5]` in `Full` mode. Use it for custom smoothing or
differencing filters, or to combine two delay distributions. Work is capped at 200 million multiplications.

**TemplateMatchNormalisedCrossCorrelation** slides a `template` pattern along a series, scoring each stretch with
normalised (Pearson) correlation so level and scale are ignored (1 perfect match, 0 unrelated, -1 inverted). Use it
to find repeats of a known shape, for example a structuring pattern of deposits. `BestOffsets`/`BestScores` list up
to `topK` (default 3) non-overlapping matches at least half a template length apart scoring at least `minScore`
(default 0.8). The template must be at least 3 values and not constant.

**HilbertEnvelopeAndPhase** computes the analytic signal of an oscillating series via the Hilbert transform:
`Envelope` (instantaneous amplitude), `Phase` (unwrapped radians) and `InstantaneousFrequency` (phase change per
sample, by central differences, scaled by `samplingIntervalSeconds`) at every point. Use it to track how a cycle's
strength or speed changes over time. The method treats the series as periodic, so results near both ends are
distorted unless it covers whole cycles, and it is only meaningful for narrow-band signals — remove mean and trend
first.

**DiscreteCosineTransformSeries** computes the orthonormal type-II DCT (forward, `inverse` false default) or its
inverse (`inverse` true), packing a smooth series' shape into a few low-order coefficients for compression, smoothing
by discarding high-order terms, or a compact feature vector. The transform preserves the sum of squares; a forward
then inverse call round-trips exactly.

**HaarWaveletDecomposition** performs a multi-level Haar wavelet decomposition, splitting a series into a coarse
`Approximation` and per-level `Details` at successively coarser time scales. The series length must be divisible by
`2^levels` (`levels`, default 3, 1 to 30). `DetailEnergyShares` shows which time scale dominates the variation; for
`[4, 2, 5, 5]` with one level the approximation is `[3, 5]` and details are `[1, 0]`.

**HaarWaveletDenoise** decomposes a series with the Haar wavelet, shrinks small detail coefficients assumed to be
noise, and reconstructs, preserving abrupt level changes better than a moving average would. `threshold` (default
`NaN`, meaning the universal threshold `sigma x sqrt(2 ln n)` with `sigma` estimated robustly from the finest
details) is applied with `soft` (default true; shrinks toward zero) or hard thresholding. `NoiseStandardDeviation`
is always reported even when `threshold` is supplied directly, and `ZeroedCoefficients` of `DetailCoefficients`
shows how much was removed.

**FrequencyDomainFilterSeries** removes chosen frequencies with an ideal brick-wall cut-off: `LowPass` (keeps at or
below `lowCutoff`), `HighPass` (at or above `lowCutoff`), `BandPass` or `BandStop` (between `lowCutoff` and
`highCutoff`, which is required for these two). `Filtered` is the kept signal, `Removed` is the input minus
`Filtered`, and `KeptFrequencyBins` of `TotalFrequencyBins` shows the retained share. Sharp cut-offs ring near the
series ends and treat it as periodic — use `SavitzkyGolayFilter` instead where that matters.

**ZeroCrossingAnalysis** counts and locates points where a series crosses a chosen `level` (default 0) by linear
interpolation between neighbouring samples. `Count`/`RatePerSample` measure oscillation frequency and
`MeanCyclePeriodInSamples` is twice the mean crossing spacing (a full cycle has two crossings; `NaN` with fewer than
2 crossings). Remove the mean or trend first to get crossings of the average level rather than of the raw level.

**PeakDetectionInSeries** finds local peaks (a value higher than both neighbours; the middle of a flat top counts
once) or, with `findTroughs` true, troughs, each with a `Prominences` value — how far it rises above the higher of
the two lowest points reached before a taller peak or the series end on either side, which separates real peaks from
small wiggles. Filters are `minHeight` (absolute level, `NaN` default means none), `minProminence` (default 0) and
`minDistance` (default 1; of two peaks closer than this, only the taller survives). For
`[0, 2, 0, 3, 0, 1, 0]` the peaks are at indices 1, 3 and 5 with prominences 2, 3 and 1.

## TransactionTimingTools.cs

Fourteen tools built specifically around epoch-second event timestamps for fraud and AML timing analysis: timestamp
parsing and formatting, calendar feature extraction, inter-arrival and gap statistics, bucket resampling, trailing-
and multi-window velocity features, a Poisson rate-change test, burst detection, sessionisation, calendar-cycle
profiling and periodicity/dispersion scoring. This class is distinct from the Velocity and Volume rule-taxonomy
pattern (a TTL Counter incremented and read by Activation Rules, documented in
[Velocity and Volume](../../../RuleTaxonomy/RuleTaxonomyTypes/VelocityAndVolume.md)): these are stateless array
functions an analytics agent calls directly over a batch of timestamps, with no engine counter, cache or rule
involved, and none of their 14 literal tool names overlaps anything in that rule-taxonomy page. All validate
timestamps against the supported date range (years 1 to 9999) and throw naming the offending position if a value
looks like it is in milliseconds rather than seconds.

**ParseTimestampsToEpochSeconds** converts ISO 8601 timestamp strings (for example `2021-01-01T00:00:00Z`,
`...+01:00`, or a plain `2021-01-01 10:30:00`) into epoch seconds (since 1970-01-01 00:00:00 UTC, fractional seconds
included), the format every other tool in this class expects. Strings with an offset or `Z` convert to UTC; strings
with none are assumed already UTC. The call fails, naming the position, if any string cannot be parsed.

**FormatEpochSecondsAsIso** is the inverse: it formats epoch seconds as ISO 8601 UTC strings (`yyyy-MM-ddTHH:mm:ss.FFFFFFFZ`,
fractional seconds to 100 nanoseconds) so results from the timing tools can be read as dates.

**TimestampCalendarFeatures** breaks epoch-second timestamps into calendar features — hour, ISO day of week (1
Monday to 7 Sunday), day of month, month, year, day of year, a weekend flag and second of day — in a chosen
`utcOffsetMinutes` (default 0, -1080 to 1080; daylight saving is not applied, so pass the offset actually in force).
Use it to build night-time or weekend-activity features for fraud and AML rules.

**InterArrivalTimeStatistics** computes statistics of the gaps between successive events (sorted first, so input
order does not matter): `Gaps` in seconds, `CoefficientOfVariation` (about 1 for random/Poisson arrivals, near 0 for
machine-regular activity, well above 1 for bursty activity) and `Burstiness` (`(sd-mean)/(sd+mean)`, from -1 perfectly
regular through 0 random to +1 extremely bursty), plus `EventsPerHour` over the observed span. Needs at least 2
events (3 for the spread figures); duplicate timestamps give zero-length gaps.

**DetectTimeGaps** finds unusually long pauses between events — dormancy, outages, missing feed data, reactivation
after silence — by an absolute `minGapSeconds` or, left `NaN` (default), `gapMultiple` (default 5) times the median
gap. Each flagged gap reports the surrounding event indices, their epoch seconds and the pause length; events must
already be in ascending time order.

**ResampleEventsToBuckets** aggregates an irregular event stream into fixed-length, evenly spaced `bucketSeconds`
(default 3600) buckets — the step needed before autocorrelation, seasonality, forecasting or spectral tools can run
on event data. Buckets align to `originEpochSeconds` (default 0, so day buckets start at UTC midnight) and run
continuously from the first to the last event; `includeEmptyBuckets` (default true) keeps zero-count gaps so the
result stays evenly spaced. `Counts`, and, with an optional `values` array, `Sums`/`Means`/`Minimums`/`Maximums` per
bucket. Refuses to build more than 200,000 buckets.

**TrailingWindowVelocity** computes, at every event, how many events (and how much value) occurred in the trailing
`windowSeconds` ending at that event — "transactions in the last hour" or "amount in the last 24 hours" per
transaction. The window is `(t - windowSeconds, t]`; `includeCurrent` (default true) controls whether the event
itself counts in its own window. Also returns `RatesPerHour` and `SecondsSincePreviousEvent`. Events must already be
in ascending order.

**MultiWindowVelocityFeatures** takes a snapshot of activity at one moment (`asOfEpochSeconds`, default `NaN` meaning
the last event) across several look-back `windowsSeconds` at once (for example 60, 3600, 86400), as a ready-made
feature vector that can be evaluated at any point in the past without leaking later data (only events at or before
`asOf` are counted). Returns `Counts`/`Sums`/`RatesPerHour` per window plus `SecondsSinceLastEvent` and
`TotalEventsUpToAsOf`.

**WindowRateChangeTest** tests whether the event rate in a recent window is unusually high or low against a
preceding baseline window, under a Poisson assumption. `recentSeconds` and `baselineSeconds` define the two windows
ending at `asOfEpochSeconds` (default last event); `ExpectedRecentCount` is what the baseline rate predicts (the
baseline count is floored at 0.5 so a silent baseline still gives a usable expectation); `PValueIncrease` and
`PValueDecrease` give the one-sided Poisson tail probabilities. Use it to detect a sudden surge or collapse in
activity for a customer or channel.

**EventBurstDetection** finds bursts — short periods with at least `minEvents` (at least 2) events inside a
`windowSeconds` window, such as rapid-fire card attempts or structured deposits. Overlapping qualifying windows are
merged into one burst; each reports its event span, `EventCount`, `MaximumWindowCount` (the busiest single window)
and `PValue` (the chance of that many events in one window at the overall average Poisson rate; `NaN` if the overall
span is 0). Events must be in ascending order.

**SessionizeEvents** splits an event stream into sessions, starting a new one whenever the gap since the previous
event exceeds `timeoutSeconds` (a gap exactly equal to the timeout does not start a new session). `SessionIds[i]`
gives each event's zero-based session; `Sessions` lists each session's event span, count, start/end and duration.
Use it to group transactions into visits or activity episodes.

**CalendarCycleProfile** profiles when events happen within a calendar cycle — `HourOfDay` (default), `DayOfWeek`
(Monday first), `DayOfMonth` or `MonthOfYear` — in a chosen `utcOffsetMinutes`. `Labels`/`Counts`/`Shares` give the
distribution, `PeakLabel` the busiest bin, `EntropyBits`/`NormalisedEntropy` (1 means perfectly even) measure spread,
and a chi-square `ChiSquareStatistic`/`PValue` tests against a uniform distribution (day-of-month uniformity is only
approximate, since months differ in length). Use it to see whether activity concentrates at unusual times, for
example overnight or weekends.

**EventPeriodicityScore** measures how machine-like an event stream's timing is, to catch scheduled, automated or
beaconing behaviour such as identical-interval payments: `MedianIntervalSeconds`, `CoefficientOfVariation` (near 0
for clockwork regularity, about 1 for random arrivals, above 1 for bursty) and `ShareWithinTolerance` (the fraction
of gaps within `toleranceFraction`, default 0.1, of the median; 1 means every gap is essentially identical).
`Reading` summarises as highly regular (share at least 0.9 and variation below 0.2), somewhat regular, or irregular.
Needs at least 3 events, sorted first.

**CountDispersionIndexTest** is an index-of-dispersion test for event counts per equal-length period (for example
transactions per hour, produced by `ResampleEventsToBuckets`): `DispersionIndex` (variance over mean) is near 1 for
Poisson-random arrivals, well above 1 means overdispersion (bursts or clustering; small `PValueOverdispersed`), well
below 1 means underdispersion (unnaturally regular counts; small `PValueUnderdispersed`). `ChiSquareStatistic` is
`(n-1) x DispersionIndex` with `n-1` degrees of freedom. Needs at least 3 non-negative periods with a positive mean.
