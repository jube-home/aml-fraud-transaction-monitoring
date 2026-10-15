---
layout: default
title: Distributions and Histograms Tool Catalogue
nav_order: 9
parent: Reference
grand_parent: Analytical Methodology
---

# Distributions and histograms tool catalogue

The product's semantic tool layer for shape, binning and distributional analysis: 62 tools under
`Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms`, callable directly by an LLM agent by their
literal `AdvancedAnalyticsTool` name. None of them has an entry anywhere else under `docs/`, which means Jadek, the
validator agent that fact-checks every AskJooby reply against the RAG-indexed `docs/` corpus, can never confirm a
claim about any of them without this page. Across every tool in this tree, a non-finite (`NaN` or
infinite) result is converted to a `null` in the response rather than being passed through as a number, so a caller
can tell "zero" apart from "undefined" without checking for `NaN` itself.

## Fitting one distribution to data (`DistributionFittingTools`)

**"FitDistribution"** estimates the parameters of one named distribution from a sample by maximum likelihood (or
method of moments where that is the distribution's natural fitting method), given at least two observations with no
missing values. Optional `fixedParameterNames`/`fixedParameterValues` hold some parameters at a supplied value instead
of estimating them — useful for fitting a Binomial with a known `trials`, or a GeneralizedPareto with `location`
pinned at 0. It returns the fitted parameters with their names, the log-likelihood, AIC and BIC (lower is better for
both, but only comparable between distributions of the same kind on the same data), the fitted distribution's mean,
variance and median, and the Kolmogorov-Smirnov distance between data and fit (0 is perfect; above roughly 0.1 with a
reasonable sample size signals a poor fit). Some distributions reject data outside their support (for example
Lognormal needs strictly positive values) and the thrown error names what is wrong. Fewer than 20 values triggers a
note that the estimates and goodness-of-fit measures are weak evidence, and any parameter held fixed is listed as such
in the notes.

**"FitDistributionAndScoreValues"** composes a fit with scoring in one call: it fits a distribution to a reference
sample and then, for a separate array of new values, reports the lower-tail probability, the upper-tail probability,
a two-sided tail probability (twice the smaller tail) and the fitted density for each. This is the tool to reach for
when asking how unusual today's observations are relative to a fitted historical baseline, since it avoids a
round-trip through separately fitting and then separately scoring.

**"RankDistributionFits"** fits many candidate distributions to the same sample at once and ranks them, so the caller
does not have to guess the right family. For every candidate that fits it returns the parameters, log-likelihood, AIC,
BIC, the Kolmogorov-Smirnov statistic and its (optimistic, since the same data both fit and tested the parameters)
p-value, the Anderson-Darling statistic (tail-weighted, lower is better), the chi-square statistic, and the rank of
that candidate under each measure plus an overall average rank. Candidates that fail to fit are listed separately with
the reason. Leaving `candidates` empty tries every distribution whose data requirements the sample satisfies; with
`dataType = Automatic` (the default) the values are treated as counts — and therefore only discrete candidates are
tried — when every value is a whole number and there are at most 50 distinct values, otherwise they are treated as
continuous. Continuous and discrete candidates can never be mixed in one ranking, because their likelihoods are not
comparable; attempting to pass a mixed `candidates` array throws. With fewer than 20 values a note warns that the
chi-square statistic is not computed and every ranking is unstable.

**"QuantileQuantilePlotPoints"** and **"ProbabilityProbabilityPlotPoints"** both generate the point coordinates of a
diagnostic plot against a named distribution, sharing the same underlying machinery and differing only in which axis
is plotted. The sample is sorted and each order statistic is given a plotting position computed by one of four
standard conventions (`Hazen`, the default, is `(i-0.5)/n`; the others are `Blom`, `Weibull` and `Benard`). The Q-Q
tool pairs each sorted observation with the distribution's theoretical quantile at that position and additionally
fits a least-squares line through the points, returning its slope and intercept (which read as the scale and location
when comparing against a standard-shape reference) together with the correlation of the points — a correlation close
to 1 supports the chosen distribution, and curvature at the ends indicates heavier or lighter tails than the model. The
P-P tool instead pairs each observation's empirical probability with its theoretical cumulative probability under the
distribution, so a good fit lies on the diagonal from (0,0) to (1,1); it is more sensitive to departures in the centre
of the distribution, where the Q-Q form is more sensitive in the tails. Leaving `parameters` empty fits the
distribution to the data first rather than requiring the caller to supply it. When the sample has more points than
`maxPoints` (10 to 20000, default 500) the returned point set is evenly thinned, but the correlation, slope and
intercept are always computed from every value.

## Shape, tails and extremes (`DistributionShapeAndTailTools`)

**"NormalityShapeSummary"** screens whether a sample's shape is compatible with a normal distribution, from at least
four observations. It reports the bias-corrected sample skewness (0 for symmetry, positive when the upper tail is
longer) and excess kurtosis (0 for a normal, positive for heavier-than-normal tails), each with a standard error and a
z-score — values beyond roughly 2 in magnitude are evidence against normality — plus the Jarque-Bera statistic and its
p-value once there are at least 8 values, and a plain-language verdict. It is explicitly a quick screen on shape
alone, not a substitute for a formal normality test on small samples, and a sample where every value is identical is
rejected as an error.

**"TailExceedanceByQuantile"** and **"TailExceedanceByThreshold"** both summarise how a sample behaves beyond some cut
point, differing in whether that cut point is given as a quantile level or as a literal value. The quantile form takes
levels strictly between 0 and 1 (for example 0.95, 0.99), looks up the corresponding threshold using a chosen sample
quantile definition (`Type1`–`Type9`, default `Type7`), and for each level reports how many observations exceed the
threshold, the fraction that do, the mean excess beyond it, the mean of the exceeding values themselves (the empirical
expected shortfall, or tail value at risk), and the single largest excess. The threshold form instead takes literal
threshold values directly and additionally reports back the empirical fraction of the sample at or below each
threshold, so the caller can see which quantile a fixed reporting limit corresponds to. Both accept a `direction` of
`Upper` (values above the threshold, the default) or `Lower` (values below it), and a null mean excess signals that
nothing exceeded that particular threshold.

**"FitGeneralizedParetoTail"** fits the upper tail of a sample with a generalized Pareto distribution — the standard
peaks-over-threshold method from extreme value theory — from at least 100 observations. It takes every value above
the threshold implied by a chosen quantile (default 0.9), fits a generalized Pareto to the excesses over that
threshold by maximum likelihood with location pinned at 0, and returns the fitted scale, the shape (the tail index:
near 0 reads as an exponential tail, clearly positive as a heavy power-law tail where huge values remain plausible,
negative as a tail that is bounded above), and the log-likelihood. For any requested overall probabilities above the
threshold quantile and below 1 (for example 0.999), it also returns the corresponding return level — the value
expected to be exceeded only with that residual probability. At least 30 values must exceed the chosen threshold or
the call is rejected as too few for a stable fit, and the result's own notes flag that the tail is only reliable when
the threshold is high enough that the excesses are effectively independent, and the data below it few enough that what
remains is genuinely a tail.

## Histograms and empirical density (`HistogramAndDensityTools`)

Four tools build an ordinary one-dimensional histogram, differing only in how the bin boundaries are chosen; all four
return the same shape of result — edges, centres, widths, raw counts, relative frequencies, densities and the values
on a requested display scale, optionally accumulated — and all accept up to 1,000,000 values. **"HistogramFixedBinCount"**
spans the sample's minimum to maximum with a caller-chosen number of equal-width bins (1 to 100,000); a sample where
every value is identical collapses to a single bin of width 1 around that value, with a note explaining why.
**"HistogramFixedBinWidth"** instead takes a caller-chosen bin width (which must be positive and small enough to
produce at most 100,000 bins) and starts binning at the minimum, so the final bin may run past the sample maximum.
**"HistogramByBinningRule"** picks the bin count automatically from one of six standard rules — Sturges (`1 + log2(n)`,
suited to roughly normal, modestly sized data), Rice (`2·n^(1/3)`), SquareRoot (`sqrt(n)`), Scott and
Freedman-Diaconis (both set the bin width from the standard deviation or interquartile range scaled by `n^(1/3)`,
with Freedman-Diaconis more robust to outliers), and Doane (an adjustment of Sturges for skewed data) — and attaches a
note naming the fallback used if the chosen rule cannot be applied, for example a zero interquartile range. It is also
the tool cross-referenced from `docs/AnalyticalMethodology/Charting.md` as the way to get bin edges and counts as an
inspectable, first-class result rather than a side effect of drawing a chart. **"HistogramCustomEdges"** takes caller-
supplied bin edges directly, which may be unequal width (for example 0, 10, 50, 100, 1000 for transaction-size bands);
edges must be strictly increasing with at least two entries, every bin includes its lower edge and excludes its upper
edge except the last (which includes both), values that fall outside the first or last edge are excluded from every
bin and counted separately in `outsideCount`, and densities divide by each bin's own width so unequal-width bins
remain comparable on that scale.

**"SuggestHistogramBinCounts"** reports, in one call, what bin count and resulting bin width each of the six standard
rules above would independently choose for a sample, so the rules can be compared before committing to one; a large
disagreement between them usually signals outliers or strong skew, in which case Freedman-Diaconis is the safer
choice. Fewer than two values, or a sample where every value is identical, collapses to a single suggested bin.

**"Histogram2D"** builds a two-dimensional grid of counts over paired `x` and `y` observations (which must be the same
length, paired by position), to show where two variables occur together. Bins are equal-width along each axis by
default, or explicit edges can be supplied per axis. It returns the edges on both axes, the count matrix indexed as
`counts[xBin][yBin]`, the matrix of relative frequencies, the marginal counts along each axis, and a single
`outsideCount` for pairs that fell outside the supplied edges on either axis.

**"EmpiricalCumulativeDistribution"** evaluates the empirical CDF of a sample at caller-given points — the fraction of
the sample at or below each point, or, with `upperTail = true`, the fraction strictly above it — which is the
assumption-free analogue of a percentile rank, useful for saying what share of history a new value exceeds. Points
below the sample minimum evaluate to 0 and points at or above the sample maximum evaluate to 1.
**"EmpiricalCumulativeDistributionSteps"** instead returns the whole empirical CDF as its step structure: each
distinct observed value in ascending order, its occurrence count, the running cumulative count, the cumulative
probability and the survival probability, which is the right shape for plotting the ECDF or reading an exact
percentile rank for every value actually observed, with repeated values collapsed to one row.
**"EmpiricalQuantileFunction"** is the inverse operation — for given probabilities it returns the sample value below
which that fraction of the data lies — offered under nine standard definitions (`Type1`–`Type9`) because small samples
disagree meaningfully between them: `Type7` (default) matches R's and Excel's `PERCENTILE.INC`, `Type6` matches
`PERCENTILE.EXC`, `Type1` is the plain inverse of the step function, and `Type8` is the recommended median-unbiased
choice.

**"KernelDensityEstimate"** estimates the probability density of a sample at caller-given points by kernel density
estimation, the smooth alternative to a histogram, in which every observation contributes a small bump (`Gaussian`,
the default and unbounded; `Epanechnikov`, finite-width and the most statistically efficient; or `Uniform`, a plain
box) whose width is the bandwidth. The bandwidth is either picked by a named rule — `NormalReference` (the standard
`1.06·σ·n^(-1/5)` rule), `SilvermanRobust` (uses the smaller of the standard deviation and a scaled interquartile
range, which suits skewed data), or `Oversmoothed` (the widest reasonable choice) — or supplied manually with
`bandwidthRule = Manual`. The returned density integrates to 1 over the whole line but is not itself a probability at
a point. **"KernelDensityCurve"** evaluates the same kernel density but over an evenly spaced grid that extends a
configurable number of bandwidths (`padding`, default 3) beyond the sample's minimum and maximum, and additionally
locates the curve's local maxima, returning their positions, heights, and the resulting mode count — more than one
peak suggests the sample is a mixture of groups, though tiny peaks in noisy data are an artefact of too small a
bandwidth and should be checked against a larger one or the `Oversmoothed` rule before trusting them.
**"KernelBandwidthRules"** reports what bandwidth each of the three automatic rules above would independently choose
for a sample, alongside the standard deviation and interquartile range they are computed from, to help pick a
bandwidth for the two density tools above; a rule that cannot be computed (for example because every value is
identical) is omitted from the result with a note explaining why.

## Mixture models (`MixtureModelTools`)

**"FitGaussianMixture1D"** fits a one-dimensional Gaussian mixture — the sample is described as drawn from several
bell-shaped sub-populations, each with its own mean, standard deviation and weight — by expectation-maximisation from
several random (and one quantile-spread) starting points, keeping the best log-likelihood across `restarts` (1 to 20,
default 5) attempts of up to `maxIterations` (10 to 2000, default 300) each. It is intended for finding hidden
sub-groups, such as customer segments buried in an amount or count distribution. It returns the fitted components
ordered by mean, each with its weight, mean, standard deviation and the count of values hard-assigned to it, the
overall log-likelihood, AIC and BIC, a hard assignment of every input value to its most likely component, and,
only when `returnResponsibilities = true`, the full values-by-components probability matrix. `components` is capped at
10 and at one fifth of the sample size, a component whose fitted standard deviation collapses to near zero relative to
the overall spread is flagged in a note as possibly fitting only a handful of repeated values, and hitting the
iteration cap without convergence is itself reported as a note. **"SelectGaussianMixtureComponents"** automates the
model-selection step this leaves open: it fits mixtures with 1 up to `maximumComponents` (1 to 10, default 5)
components and reports the log-likelihood, AIC and BIC of each, together with which component count each criterion
would recommend — preferring the BIC recommendation when the two disagree, unless there is a great deal of data, since
BIC penalises additional components more heavily. A sharp drop in BIC from k to k+1 followed by little further
improvement is the signal to read as "k+1 groups"; fits that fail for a given k are simply omitted with a note, and
the component sweep stops early, with a note, once there are too few values left to support another component.

**"FitMixtureModel"** generalises the Gaussian case to a mixture of several components drawn from the same chosen
non-Gaussian, continuous, directly-fittable family — Normal, Lognormal, Exponential, Gamma, Weibull, Rayleigh, Cauchy,
Gumbel, InverseGaussian, Pareto, Nakagami and similar, as marked fittable by `ListDistributions` — which matters for
positive or skewed data where a Gaussian mixture would place components at impossible negative values. Passing a
distribution that needs a numerical search rather than a direct estimator, or a discrete one, is rejected outright.
It returns the component weights, each component's parameters in `ListDistributions` order (components ordered by
their first parameter), the log-likelihood, AIC, BIC and a hard assignment of every value to its most likely
component, with the same not-converged note as the Gaussian tool when applicable. **"EvaluateMixtureDistribution"**
is the companion read-only tool: given weights and per-component parameters already in hand (typically from one of
the two fitting tools above), it evaluates the resulting mixture's density and cumulative probability at caller-given
points, and also returns the responsibilities — the probability that each point belongs to each component given its
value, with every row summing to 1 — which is how new values get scored against, or classified into, an
already-fitted mixture without refitting it. Weights must each lie in [0, 1] and sum to 1 (checked to a tolerance of
1e-6) or the call is rejected.

## Multivariate and matrix-variate distributions (`MultivariateDistributionTools`)

Three tools work directly with a given multivariate normal mean and covariance. **"MultivariateNormalDensity"**
evaluates the density, its natural logarithm, and the Mahalanobis distance from the mean (the number of standard
deviations away once correlation and differing scales are accounted for, 0 exactly at the mean) at each given point;
the log form is the one to use in higher dimensions where the raw density would underflow to 0.
**"MultivariateNormalMahalanobisDistance"** computes just the Mahalanobis distance of each point from a given centre,
together with the squared distance and the upper-tail probability of that squared distance under a chi-square
distribution with degrees of freedom equal to the number of variables — a small tail probability, say below 0.001,
flags a multivariate outlier when the data is reasonably normal. **"MultivariateNormalRandomSample"** draws random
row vectors from the given normal and returns them along with their column means as a sanity check that the draw
matches the intended distribution; the same seed always reproduces the same rows. Where the mean and covariance are
not already known, **"MahalanobisOutlierScoresFromSample"** estimates them from a data sample itself (which needs more
rows than columns and no perfectly redundant variables, or the covariance is singular and the error explains why) and
scores either every row of that sample or a separately supplied `pointsToScore` array by the same distance, squared
distance and chi-square tail probability. **"FitMultivariateNormal"** performs the equivalent maximum-likelihood fit
without scoring anything, returning the mean vector, the unbiased covariance matrix, the derived correlation matrix,
the per-variable standard deviations and the total log-likelihood of the sample, under the same more-rows-than-columns
requirement. **"MultivariateNormalOrthantProbability"** evaluates the multivariate normal CDF itself: for each point,
the probability that every coordinate is simultaneously at or below it (`lowerOrthantProbability`) and the
probability every coordinate is simultaneously above it (`upperOrthantProbability`), which in one dimension reduce to
the ordinary CDF and survival function and in general answer joint-exceedance questions; it is capped at 8 dimensions,
computed numerically beyond one dimension, and a value that could not be computed comes back as null rather than an
approximation presented as exact.

Four tools cover the Dirichlet distribution, the distribution over sets of proportions that sum to 1 (a multivariate
Beta), useful for uncertainty about category shares such as the mix of transaction types. **"DirichletSummary"**
takes the positive concentration parameters (at least 2; a larger total concentration means less uncertainty around
the mean) and returns the mean share of each component, each component's variance, the full covariance matrix (shares
are negatively correlated with each other because they must sum to 1), and the total concentration.
**"DirichletDensity"** evaluates the density at given proportion vectors, which must lie strictly inside the simplex
(every entry in (0,1), summing to 1) or the call is rejected. **"FitDirichlet"** estimates the concentration
parameters from observed proportion rows (each strictly summing to 1, 2 to 12 components) by numerical maximum
likelihood, returning the concentrations, the implied mean proportions, the total concentration (a precision
measure), and the log-likelihood — counts should be converted to shares and exact zeros avoided before calling it.
**"DirichletRandomSample"** draws reproducible random proportion rows from a given set of concentrations, each row
summing to 1, returning the rows and each component's mean as a sanity check.

Three tools cover the multinomial distribution — counts across several categories after a fixed number of independent
trials. **"MultinomialSummary"** returns, for given trial count and category probabilities, each category's expected
count, its variance, and the full covariance matrix of the counts (categories compete for the fixed trial total, so
the covariances are negative). **"MultinomialProbability"** evaluates the exact probability mass, and its natural log,
of observing specific count patterns; any row that does not sum exactly to the trial count has probability 0 (and a
null log). **"MultinomialRandomSample"** draws reproducible rows of category counts, each summing to the trial count,
returning the mean count per category as a sanity check.

Two pairs of tools cover the Wishart family, the distribution of sample covariance (scatter) matrices of multivariate
normal data, and its conjugate inverse. **"WishartSummary"** and **"InverseWishartSummary"** return the mean matrix
and the entrywise variance matrix given degrees of freedom and a positive-definite scale matrix; the Wishart form
requires degrees of freedom at least the matrix size, while the inverse form requires degrees of freedom strictly
greater than the matrix size plus 1, since otherwise the mean does not exist. **"WishartDensity"** and
**"InverseWishartDensity"** evaluate the density (and its log, null when the density is exactly 0) at a given
symmetric matrix of matching size; a matrix that is not positive definite has density 0 outright rather than raising
an error. **"WishartRandomSample"** draws reproducible random symmetric positive-definite matrices (up to 10,000 of
them), returning the entrywise mean of the drawn matrices as a check that it approaches degrees-of-freedom times the
scale matrix; there is no corresponding random-sample tool for the inverse Wishart in this tree.

Four tools cover directional and spatial sampling. **"VonMisesFisherDensity"** evaluates the von Mises-Fisher
distribution — the normal-like distribution of unit-length directions on a sphere, concentrated around a mean
direction by a `concentration` parameter that is 0 for uniform directions and larger for tighter clustering — at
given points, which are normalised to unit length automatically; the density form only supports an even number of
dimensions (2, 4, 6, …), so odd-dimensional cases must go through **"VonMisesFisherRandomSample"** instead, which
needs at least 3 dimensions (2-dimensional angular data belongs to the ordinary VonMises distribution) and returns
reproducible draws plus their mean vector as a check that its direction approaches the specified mean direction.
**"UniformSphereRandomSample"** and **"UniformBallRandomSample"** draw reproducible points uniformly on the surface of
a sphere, or uniformly throughout the interior of a solid ball respectively, given a centre and a positive radius —
useful for simulating isotropic directions, or points scattered evenly within a maximum distance of a centre.

## Univariate distribution evaluation (`UnivariateDistributionTools`)

**"ListDistributions"** is the catalogue entry point for every other tool in this group and in `DistributionFittingTools`
and `MixtureModelTools`: for every supported univariate distribution, continuous or discrete, it returns the name to
pass as the `distribution` argument, whether it is discrete, a plain-language description, its support, typical uses,
the ordered parameter names and descriptions (a variadic distribution such as a general discrete probability vector
reports a single `probabilities` parameter), an example parameter array, and whether — and by what method — it can be
fitted to data at all. It is the tool to call first whenever the right distribution, or its parameter order, is not
already known.

**"DescribeDistribution"** summarises a distribution at fixed, caller-supplied parameters: mean, variance, standard
deviation, median, mode, entropy in nats, the lower and upper support limits, and the values at nine standard
percentage points (0.1, 1, 5, 25, 50, 75, 95, 99, 99.9). Any field that does not exist or is infinite for that
distribution — the Cauchy has no mean, and unbounded supports have null limits — comes back null rather than a
placeholder number.

Six tools evaluate the standard pointwise functions of a distribution with given parameters at caller-supplied points,
all sharing the same guard (no `NaN` in the input points) and the same null-for-undefined convention in the output.
**"DistributionDensity"** evaluates the probability density for a continuous distribution (which is not itself a
probability and may exceed 1) or the exact probability mass for a discrete one (where a non-integer point has
probability 0). **"DistributionLogDensity"** evaluates the natural log of the same quantity, for use when raw
densities are small enough to underflow to zero, or when log-likelihoods are being accumulated by hand; a value of
negative infinity (impossible under the distribution) comes back as null. **"DistributionCumulativeProbability"**
evaluates the CDF — the probability of a value at or below each point, rising from 0 to 1 — rounding down to the
nearest whole number first for discrete distributions. **"DistributionUpperTailProbability"** evaluates the
survival function directly (rather than as 1 minus the CDF), so it stays numerically accurate far into the tail where
subtracting from 1 would round to zero; this is the one to use for p-values and exceedance probabilities.
**"DistributionHazardRate"** evaluates the hazard function — the instantaneous failure rate at a point given survival
to that point, i.e. density divided by the survival function — which is how to see whether risk under a lifetime
model (Weibull, Exponential, Gamma and similar) rises, falls, or stays flat over time. **"DistributionCumulativeHazard"**
evaluates the cumulative hazard, the negative log of the survival function, which gives the expected event count by a
given point under a repairable process or a common scale for comparing survival curves.

**"DistributionQuantile"** is the inverse of the CDF: for given probabilities it returns the value below which that
probability mass lies, with 0 and 1 returning the support's ends (null when unbounded) and discrete distributions
returning the smallest integer whose cumulative probability reaches the target. **"DistributionIntervalProbability"**
returns the probability of falling in each of several `[lower, upper]` intervals (paired by position, infinities
allowed to leave an end open); for discrete distributions both ends are inclusive, so an interval of 3 to 5 covers the
integers 3, 4 and 5, while continuous distributions are unaffected by whether the endpoints themselves are included. A
`lower` value above its paired `upper` value is rejected.

**"DistributionCurve"** generates an evenly spaced grid (2 to 5000 points, default 101) across a distribution's useful
range — from the `lowerProbability` quantile to the `upperProbability` quantile, default 0.001 to 0.999, which trims
heavy tails — and returns the density, cumulative probability and survival probability at every grid point, ready to
plot directly or to overlay against a histogram; if the requested quantile range is not finite it falls back to the
distribution's actual support, or to an arbitrary unit interval if that too is unbounded. **"DistributionProbabilityTable"**
is the discrete-only counterpart: given a first and last whole number (at most 10,000 apart), it lists the probability
mass of every integer in that range along with the running cumulative probability and the total probability mass
covered, which is how to read off an exact chance such as "exactly 3 events" or to see how much mass lies outside a
chosen range as one minus the reported total; calling it on a continuous distribution is rejected in favour of
`DistributionCurve` or `DistributionDensity`.

**"DistributionRandomSample"** draws reproducible random values (1 to 100,000) from a distribution with fixed
parameters, returning the values plus their sample mean and sample standard deviation as an immediate sanity check;
not every distribution supports random generation, and such a call is rejected with a pointer toward sampling a
different distribution or inverting uniform probabilities through `DistributionQuantile` instead. This is also the
tool referenced from `docs/AnalyticalMethodology/PrescriptiveAnalytics/MonteCarloSimulation.md` as the underlying
draw primitive behind a triangular-distribution Monte Carlo worked example, called with `DistributionKind.Triangular`
and `[minimum, maximum, mode]`. **"DistributionLogLikelihood"** scores a set of observations against a distribution
whose parameters are already fixed — it does not estimate them, unlike `FitDistribution` — returning the total
log-likelihood, the mean log-likelihood per observation, AIC, BIC, and a count of observations that were impossible
(zero probability) under the distribution; if even one observation has zero probability the log-likelihood, AIC and
BIC all come back null rather than a misleadingly finite number, while the count of impossible observations is still
reported. **"DistributionTailScores"** scores each value by how extreme it is under a distribution with fixed
parameters, returning for every value the lower-tail probability, the upper-tail probability, a two-sided tail
probability (twice the smaller of the two, capped at 1) and a base-10 "surprise" score equal to minus the log of the
two-sided probability — a surprise of 3 reads as roughly one-in-a-thousand — which is the tool to reach for when
flagging anomalies against an already fitted or assumed reference distribution; `FitDistributionAndScoreValues` calls
this same scoring logic internally after fitting.

## See also

`"DistributionDensity"` and `"DistributionCumulativeProbability"` are also cited, with a worked R-equivalence example
(`dnorm`/`pnorm`), in `docs/AnalyticalMethodology/SummaryStatistics.md`; `"HistogramByBinningRule"` is cited in
`docs/AnalyticalMethodology/Charting.md` alongside the charting API's own automatic bin selection; and
`"DistributionRandomSample"` is cited in `docs/AnalyticalMethodology/PrescriptiveAnalytics/MonteCarloSimulation.md`.
Those three mentions are brief cross-references rather than full treatments, so the entries above remain the primary
documentation for all 62 tools in this tree.
