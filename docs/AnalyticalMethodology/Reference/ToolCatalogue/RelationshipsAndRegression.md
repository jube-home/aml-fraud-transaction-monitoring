---
layout: default
title: Relationships and Regression Tool Catalogue
nav_order: 11
parent: Reference
grand_parent: Analytical Methodology
---

# Relationships and regression tool catalogue

Per-tool index for every one of the 55 literal `[AdvancedAnalyticsTool("...")]` identifiers under
`Jube.Service/Agent/AdvancedAnalyticsTools/RelationshipsAndRegression`, so that a claim naming one of these tools
by its exact callable name can be checked against a real entry. Unlike the hypothesis tests family, this group had
partial coverage already: [LinearRegression.md](../../LinearRegression.md) walks a worked R-versus-AdvancedAnalytics
course that names several of these tools with real behavioural detail, and
[LogisticRegression.md](../../LogisticRegression.md), the Bayesian networks
[score-based structure learning](../../BayesianNetworks/ScoreBasedStructureLearning.md) and
[parameter learning](../../BayesianNetworks/ParameterLearningAndCPTs.md) pages each name one more in passing. Those
ten tools are listed below with a citation instead of being re-described; every other tool gets its own entry here.
All tools live in `Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression` and require the
`Analytics` permission. Several share a common input convention: `inputs` is one row (array) per observation with
one entry per predictor column, `outputs` is the matching outcome array, `variableNames` optionally labels the
predictor columns (`x1`, `x2`, ... otherwise), `includeIntercept` defaults to true, `confidenceLevel` defaults to
0.95, and `dropIncompleteRows` defaults to false (rejecting any row containing NaN rather than silently dropping
it) — these are described once here rather than repeated in every entry below.

## Already covered elsewhere

**`PearsonCorrelation`** and **`SpearmanRankCorrelation`** — described in
[LinearRegression.md, Procedure 3](../../LinearRegression.md#procedure-3-create-a-correlation-matrix-using-spearman-and-pearson):
the Fisher z confidence interval each adds beyond base R's `cor()`.

**`CorrelationMatrix`** — described in the same procedure: every pairwise correlation with a matching p-value
matrix, pair counts, and the strongest pairs pre-ranked with Benjamini-Hochberg adjusted p-values.

**`RankPredictorsByCorrelation`** — described in
[LinearRegression.md, Procedure 4](../../LinearRegression.md#procedure-4-ranking-correlation-by-absolute-strength):
screens every column against one target, ranked by absolute strength with a p-value and adjusted p-value per
column.

**`FitSimpleLinearRegression`** — described in
[LinearRegression.md, Procedure 6](../../LinearRegression.md#procedure-6-creating-a-one-way-linear-regression-model):
the coefficient table plus R squared, adjusted R squared, the F test, AIC/AICc/BIC and Durbin-Watson that
`summary.lm()` does not print.

**`PredictWithLinearRegression`** — described in
[LinearRegression.md, Procedure 8](../../LinearRegression.md#procedure-8-using-the-predict--function-for-a-one-way-linear-regression):
fits and predicts in one call, with a confidence interval for the mean response, a wider prediction interval for a
single new observation, leverage, and an `ExtrapolatedRows` count.

**`FitMultipleLinearRegression`** — described in
[LinearRegression.md, Procedure 9](../../LinearRegression.md#procedure-9-identifying-confidence-intervals)
(confidence interval on every coefficient at the caller's `confidenceLevel`) and in
[ParameterLearningAndCPTs.md](../../BayesianNetworks/ParameterLearningAndCPTs.md) (a flat multiple regression as the
nearest single-node analogue of a continuous Bayesian network node, with no notion of a second node's own
conditional distribution feeding into it).

**`StepwiseLinearRegression`** — described in
[LinearRegression.md, Procedure 10](../../LinearRegression.md#procedure-10-create-a-stepwise-linear-regression-model)
(Forward/Backward/Both selection by AIC, BIC or p-value entry/exit, every step recorded, with a warning that
stepwise p-values are optimistic) and named again in
[LogisticRegression.md](../../LogisticRegression.md) as OLS-only, not applicable to a binomial model.

**`CompareModelsByInformationCriteria`** — described in
[ScoreBasedStructureLearning.md](../../BayesianNetworks/ScoreBasedStructureLearning.md): AIC, its small-sample
correction AICc, and BIC computed from supplied log-likelihoods and parameter counts, with Akaike and BIC weights
for relative support across candidate models.

**`ApplyLinkFunction`** — described in [LogisticRegression.md](../../LogisticRegression.md): applies a link
(Logit with `inverse: true` is the logistic function) generally for any link, reporting out-of-domain values
through `InvalidCount` instead of silently returning `NaN`.

## Correlation and association (`CorrelationTools`)

**`KendallTauCorrelation`** measures ordinal association as the surplus of concordant pairs over discordant pairs,
scaled to -1..+1. All three variants are always returned — `TauA` (ignores ties), `TauB` (the tested default,
corrects for ties) and `TauC` (adjusts for rectangular tables with different numbers of distinct values) — with
`variant` choosing which one populates `Tau` and the confidence interval. The z statistic and p-value use the
tie-corrected null variance of the concordance surplus; the interval is the Fisher-type interval on the chosen
variant. Cost grows with the square of the row count, so at most 30000 pairs are accepted; `ignoreMissing`
controls whether incomplete pairs are dropped (reporting the count) or rejected.

**`PointBiserialCorrelation`** measures how strongly a numeric variable differs between two groups defined by a
0/1 `group` flag (for example amount for fraud=1 versus fraud=0) as the Pearson correlation between the flag and
the numbers; its p-value is identical to the pooled two-sample t-test p-value. It returns the coefficient, t
statistic, Fisher confidence interval, each group's mean and size, and the pooled standard deviation. Both groups
need at least one row and the flag must contain only 0 and 1; a constant numeric variable gives `NaN`.

**`BiserialCorrelation`** estimates the correlation between a numeric variable and a 0/1 flag that is really a
dichotomised normal latent variable (for example pass/fail from an underlying score), using the biserial
coefficient (M1-M0)/s * p*q/y, where y is the standard normal density at the split. It is always larger in
magnitude than the point-biserial coefficient and can exceed 1 in absolute value when the latent-normal assumption
fails; `TStatistic` holds a z statistic against zero, `DegreesOfFreedom` is `NaN`, and no confidence interval is
produced. Use `PointBiserialCorrelation` instead when the flag is a genuine two-valued measurement rather than a
dichotomised continuum.

**`CovarianceMatrix`** computes the sample covariance matrix of several numeric columns, plus column means,
variances, standard deviations, the total variance (trace) and the generalised variance (determinant).
`Covariance[i][j]` is in the product of columns i and j's units, so it is not comparable across pairs — use
`CorrelationMatrix` for a unit-free view. `unbiased` (default true) divides by n-1 rather than n; with
`ignoreMissing` true, rows containing NaN are dropped whole (listwise) to keep the matrix positive semi-definite,
otherwise they are rejected. A determinant near zero signals the columns are nearly linearly dependent.

**`PartialCorrelation`** measures the relationship between `x` and `y` after removing the linear influence of one
or more `controls` from both — the correlation of the two sets of regression residuals — to ask whether x and y
are still related once confounders are held constant. It returns the coefficient, a t statistic with
n-2-k degrees of freedom (k controls), a two-sided p-value and a Fisher interval with standard error
1/sqrt(n-3-k), alongside `ZeroOrderCorrelation`, the raw (uncontrolled) correlation for comparison. `method` is
`Pearson` or `Spearman` (which ranks every variable first, a rank-based approximation); `Kendall` is not
supported here.

**`SemiPartialCorrelation`** measures `x`'s unique contribution to `y`: the correlation between `y` itself and the
part of `x` left over after regressing `x` on the `controls` (only `x`'s side is residualised, unlike
`PartialCorrelation`, which residualises both). Its square is the increase in R squared when `x` is added to a
regression of `y` on the controls alone. The t statistic and p-value are those of `x`'s own coefficient in a
regression of `y` on `x` plus the controls, with n-k-2 degrees of freedom; no confidence interval is produced
(fields are `NaN`). `method` is `Pearson` or `Spearman`; `Kendall` is not supported.

**`PartialCorrelationMatrix`** computes the partial correlation of every pair of columns (at least 3) controlling
for every other column at once, derived from the inverse of the Pearson correlation matrix. A raw correlation that
collapses once the other columns are controlled for was being driven by them. P-values use n-p degrees of freedom
(p = column count). Perfectly collinear columns make the correlation matrix non-invertible and are rejected with
a pointer to `VarianceInflationFactors` to find them first.

**`MutualInformationEstimate`** estimates how much knowing one numeric variable reduces uncertainty about
another, catching any dependence including non-monotone and non-linear shapes that correlation misses. Both
variables are cut into `bins` (equal width by default, or `EqualFrequency` for skewed data; 0 chooses
floor(sqrt(n/5)) clamped to 2..20 automatically) and the plug-in estimate MI = H(X) + H(Y) - H(X,Y) is returned in
nats and bits, plus a normalised score MI/sqrt(H(X)H(Y)) in 0..1. `GStatistic` = 2n*MI (natural log) is referred
to chi-square with (bins-1)(bins-1) degrees of freedom for a p-value against independence; `biasCorrect` (default
false) subtracts the Miller-Madow correction for the estimator's small-sample upward bias (the G test itself
always uses the raw, uncorrected value). Needs at least 4 complete pairs; estimates depend on the chosen binning
and are biased upward when bins are many relative to n.

**`RankPredictorsByMutualInformation`** screens many numeric columns against one `target` by binned mutual
information and ranks them, so non-linear and non-monotone predictors are not missed the way correlation misses
them. Each column returns MI in bits, the normalised MI, the G test's chi-square p-value and a
Benjamini-Hochberg adjusted p-value across the columns, sorted strongest first with `Rank` 1 best; each column
uses only the rows where both it and the target exist, and binning follows `MutualInformationEstimate`.

**`CorrelationRatio`** measures how strongly a categorical `groups` code explains a numeric `values` variable with
the correlation ratio eta (square root of between-group variance over total variance, 0 to 1) and eta squared.
Unlike Pearson correlation it detects any pattern of group means, not just a linear trend across group codes. Also
returned: the bias-lowered `AdjustedEtaSquared` (epsilon squared), the one-way ANOVA F statistic with its degrees
of freedom and p-value, and each group's label, size and mean. Needs at least 2 distinct groups and more rows than
groups; a numeric variable with no variation gives `NaN`.

**`CompareIndependentCorrelations`** tests whether a correlation observed in one sample differs from a correlation
in a separate, independent sample (for example the same two variables in two customer segments), using the Fisher
z transformation: z = (z1-z2)/sqrt(1/(n1-3)+1/(n2-3)), always two-sided, with a confidence interval for the
difference in Fisher z units. Each sample size must be at least 4 and each correlation strictly between -1 and 1;
it must not be used for correlations sharing a variable or measured on the same rows.

**`CorrelationSummaryInterval`** turns a correlation already known, plus its sample size, into a significance test
and confidence interval without needing the raw data: the t statistic and two-sided p-value against a true
correlation of 0 (n-2 degrees of freedom), the Fisher z value and its standard error 1/sqrt(n-3), the confidence
interval for the true correlation, and the smallest absolute correlation that would be significant at the
complementary level — a quick read of what a given n can detect. The correlation must be strictly between -1 and
1 and n at least 4.

## Linear regression (`LinearRegressionTools`)

**`FitWeightedLinearRegression`** fits a weighted least squares regression, minimising the weighted sum of squared
residuals so rows with larger `weights` pull the line harder; use it for rows of unequal reliability (for example
weight = 1/variance) or to down-weight suspect rows. Weights are treated as precision weights — they do not
change the row count used for degrees of freedom — and every weight must be strictly positive. The result has the
same layout as `FitMultipleLinearRegression` (coefficient table, R squared, F test, AIC/BIC, Durbin-Watson), all
computed on the weighted scale; fitted values and residuals are reported on the original scale.

**`FitPolynomialRegression`** fits `y` as a polynomial of `x` of the chosen `degree` (1 to 12) by least squares,
for curved but smooth single-predictor relationships. With `centreX` true (the default) the terms are powers of
(x - mean(x)), which avoids numerical trouble when x is large (for example a year) without affecting predictions;
coefficients are then expressed in the centred variable. The result matches `FitSimpleLinearRegression`
(coefficient table, R squared, F test, AIC/BIC, Durbin-Watson) and accepts `predictAt` for fitted values with mean
and prediction intervals. High degrees overfit and extrapolate wildly — use `SelectPolynomialDegree` to choose the
degree, keeping it well below the number of distinct x values.

**`SelectPolynomialDegree`** helps choose the polynomial degree for `x` and `y` by fitting every degree from 1 up
to `maxDegree` (2 to 12, default 6) and scoring each on R squared, adjusted R squared, residual standard error,
AIC, BIC, the exact leave-one-out cross-validated RMSE (computed from the hat matrix, so no refitting is needed)
and the p-value of the incremental F test against the previous degree. `BestByAic`, `BestByBic`,
`BestByAdjustedRSquared` and `BestByLeaveOneOut` each name the degree that criterion prefers;
`BestBySequentialFTest` is the degree reached by adding terms while each addition stays significant at 5 percent.
When the criteria disagree, the simpler degree favoured by BIC or leave-one-out error is the safer choice. A
degree that cannot be fitted (too few rows, or collinear powers) is marked `Fitted: false` rather than causing the
whole call to fail.

**`CompareNestedLinearModels`** tests whether a named group of predictors adds explanatory power, by comparing the
full regression against a reduced regression with `dropVariables` removed (the partial F test) — the test for
several coefficients jointly, which individual t tests in the coefficient table cannot do (for example a set of
dummy variables for one categorical factor). It returns the F statistic with its degrees of freedom and p-value, the
residual sum of squares, R squared, AIC and BIC of both models, and a plain-language conclusion. `dropVariables`
must name at least one existing predictor and leave at least one predictor (or an intercept-only reduced model
when an intercept is included).

**`FitMultivariateLinearRegression`** fits the same predictors to several numeric `outputs` columns at once
(multivariate multiple regression), returning one complete `FitMultipleLinearRegression`-shaped entry per outcome
in `Models` — coefficient table, R squared, F test, AIC/BIC and Durbin-Watson for that outcome alone. The
estimates are identical to fitting each outcome separately (the least squares solution does not couple the
outcomes), so this is a convenience for seeing how one set of drivers affects several measures together, not a
true multivariate (seemingly-unrelated-regressions) model. Outcomes are named via `outputNames` (`y1`, `y2`, ...
otherwise); rows with NaN in any outcome or predictor are rejected unless `dropIncompleteRows` is true.

## Generalised linear models (`GeneralisedLinearModelTools`)

**`FitGeneralisedLinearModel`** fits a generalised linear model by iteratively reweighted least squares (Fisher
scoring), extending regression to counts, positive skewed amounts, binary outcomes or Gaussian data in one call.
`family` sets the outcome distribution (`Gaussian`, `Poisson` for counts, `Binomial` for 0/1, `Gamma` or
`InverseGaussian` for positive skewed amounts) and `link` connects the mean to the linear predictor (`Identity`,
`Log`, `Logit`, `Probit`, `ComplementaryLogLog`, `Cauchit`, `Inverse`); leaving `link` empty uses the family
default (Gaussian→Identity, Poisson→Log, Binomial→Logit, Gamma and InverseGaussian→Log). The coefficient table has
the estimate on the link scale, standard error, Wald z or t statistic, p-value, confidence interval, and the
exponentiated estimate with its own interval (a rate ratio for Poisson/Log, an odds ratio for Binomial/Logit, a
multiplicative effect for Gamma/Log). Model-level fields: deviance, null deviance, the deviance-based pseudo R
squared (1 - deviance/null deviance), the Pearson dispersion (1 means the fixed-variance families fit well; well
above 1 signals overdispersion), log-likelihood, AIC, BIC, a likelihood-ratio test against intercept-only,
`Converged` and `Warnings`. Poisson and Binomial have dispersion fixed at 1 unless `estimateDispersion` is true
(quasi-likelihood, scaling standard errors by the Pearson dispersion); the other families always estimate
dispersion and use t statistics. An `offset` (for example the natural log of exposure time, for modelling a rate)
is added to the linear predictor with its coefficient fixed at 1; `weights` supplies an optional prior weight per
row. An intercept is always fitted.

**`FitPoissonRegression`** is `FitGeneralisedLinearModel` fixed to the Poisson family with a Log link for a count
outcome (events per customer, alerts per day, claims per policy), reporting each predictor's effect directly as a
rate ratio (`ExponentiatedEstimate`: 1.10 means 10 percent more events per unit increase, others held fixed).
`exposure` (time at risk, population size, opportunity count; every value must exceed 0) adds its natural log as
an offset so the model describes a rate rather than a raw count. The Pearson dispersion (chi-square over residual
degrees of freedom) well above 1 signals overdispersion, meaning the standard errors are too small; refit with
`estimateDispersion: true` (quasi-Poisson) in that case.

**`FitGammaRegression`** is `FitGeneralisedLinearModel` fixed to the Gamma family for a strictly positive,
right-skewed outcome whose spread grows with its mean (claim or transaction amounts, durations, costs) — the
standard deviation is assumed proportional to the mean. With the default `Log` link, `ExponentiatedEstimate` is
the multiplicative change in the expected outcome per unit increase; `Inverse` is the Gamma family's canonical
link (mean = 1/linear predictor); `Identity` models additive effects but can produce a non-positive fitted mean,
which stops the fit. Dispersion (equal to the squared coefficient of variation) is always estimated, using t
statistics. Every outcome value must be strictly positive.

**`FitProbitRegression`** is `FitGeneralisedLinearModel` fixed to Binomial with a `Probit` link: P(y=1) =
Phi(linear predictor), the standard normal distribution function. Coefficients are on the probit (z-score) scale
and are roughly 0.55 times the corresponding logistic coefficients; the result carries the same coefficient table
and model-level fields as `FitGeneralisedLinearModel`, including warnings if the classes are perfectly separated.
Probit and logit rarely differ materially in fit; choose probit when a latent-normal-threshold interpretation is
wanted. No held-out evaluation or calibration is performed — for a scored, evaluated logistic model use
`TrainLogisticRegression` instead.

**`FitComplementaryLogLogRegression`** is `FitGeneralisedLinearModel` fixed to Binomial with a
`ComplementaryLogLog` link: P(y=1) = 1 - exp(-exp(linear predictor)). Unlike logit and probit the curve is
asymmetric, approaching 1 more slowly than it leaves 0, which suits rare events and discrete-time hazard or
"at least one event occurred" data. Coefficients are on the log-log scale; exp(coefficient) is a hazard ratio for
the event over the observation window. Compare its AIC against `FitProbitRegression` and `TrainLogisticRegression`
when the appropriate link is unclear.

**`PredictWithGeneralisedLinearModel`** fits a GLM to the training rows (same `family`/`link`/`weights`/`offset`/
`estimateDispersion` semantics as `FitGeneralisedLinearModel`) and immediately predicts for `newInputs`. Each
returned row carries the linear predictor and its standard error, the predicted mean on the outcome scale
(expected count, amount or probability) and a confidence interval for that mean obtained by transforming the
link-scale interval through the inverse link — which keeps probabilities within 0 and 1 and counts positive. The
interval is for the mean response only, not for an individual future outcome. `newOffset` is required, on the same
scale as `offset`, whenever the model was fitted with one. The model is refitted on every call; nothing is stored.

## Model diagnostics (`RegressionDiagnosticsTools`)

**`RegressionInfluenceDiagnostics`** finds the rows with an outsized effect on an ordinary least squares fit,
using the standard influence measures per row: `Leverage` (hat value, how unusual the predictor values are;
average is k/n), `StandardisedResidual` and `StudentisedResidual` (the latter computed with that row left out, so
an outlier cannot mask itself — beyond about 3 in size is an outlier), `CooksDistance` (how far every fitted value
moves if the row is deleted; above 4/n is influential, above 1 severe), `Dffits` (change in that row's own fitted
value, in standard errors), `CovarianceRatio` and `Dfbetas` (change in each coefficient, in standard errors, if
the row is deleted). `Flags` names which thresholds a row crosses (`HighLeverage` above 2k/n, `Outlier` at
|studentised| above 3, `Influential` at Cook above 4/n, `HighDffits` above 2*sqrt(k/n), `HighDfbetas` above
2/sqrt(n)). By default only flagged rows appear in `Rows` (up to `maximumRows`, worst Cook first); set
`returnAllRows` to get every row. `MostInfluential` always lists the top `topCount` rows by Cook's distance
regardless of flags. Also returned: `PressStatistic` (sum of squared leave-one-out prediction errors) and the
leave-one-out RMSE. `KeptRowIndices` maps returned row positions back to the original input rows when incomplete
rows were dropped.

**`DurbinWatsonRegressionTest`** checks whether a linear regression's residuals correlate with the residual of the
row immediately before (serial correlation), computed on the rows in the order supplied — sort by time first.
`DurbinWatson` runs 0 to 4: about 2 means no first-order autocorrelation, below about 1.5 positive
autocorrelation (residuals tend to repeat sign, understating standard errors), above about 2.5 negative.
`FirstOrderAutocorrelation` is the lag-1 residual correlation (roughly 1 - DW/2); `ApproximateZ` and
`ApproximatePValue` use the large-sample normal approximation sqrt(n) times that autocorrelation rather than exact
Durbin-Watson bound tables, so the p-value is only a guide above about n=30. See `BreuschGodfreySerialCorrelationTest`
for higher-order serial correlation.

**`BreuschGodfreySerialCorrelationTest`** tests for serial correlation of any order with the Breusch-Godfrey
Lagrange multiplier test: residuals are regressed on the original predictors plus their own lags up to `order`
(1 to 20, missing initial lags set to 0), and LM = n * R squared of that auxiliary regression is compared with
chi-square at `order` degrees of freedom. Unlike `DurbinWatsonRegressionTest` it is valid with lagged outcomes as
predictors and tests several lags jointly; rows must be in time order.

**`BreuschPaganHeteroscedasticityTest`** tests whether residual spread changes with the predictors
(heteroscedasticity) by regressing squared residuals on the predictors; LM = n * R squared of that auxiliary
regression against chi-square with one degree of freedom per predictor. `variant` is `Studentised` (the Koenker
variant, default, robust to non-normal errors) or `Classical` (the original Breusch-Pagan, assumes normal errors,
divides the explained sum of squares by 2*sigma^4). A small p-value means ordinary standard errors are unreliable;
consider an outcome transformation, weighted least squares or robust standard errors. See
`WhiteHeteroscedasticityTest` for non-linear forms of variance change.

**`WhiteHeteroscedasticityTest`** tests for heteroscedasticity of unknown form: squared residuals are regressed on
the predictors, their squares and every pairwise product, with LM = n * R squared of that auxiliary regression
against chi-square with one degree of freedom per auxiliary term. It catches variance that changes non-linearly
or through interactions that `BreuschPaganHeteroscedasticityTest` can miss, at the cost of lower power in small
samples from the larger number of auxiliary terms; it also reacts to some kinds of model misspecification more
generally. At most 10 predictors are accepted, and constant, duplicated or collinear auxiliary terms (for example
the square of a 0/1 dummy) are rejected with a message naming the problem.

**`GoldfeldQuandtTest`** tests whether residual variance differs between the low and high end of one chosen
predictor (`sortVariable`): rows are sorted by that predictor, a central `fractionOmitted` (0 to 0.6, default 0.2)
is left out, the regression is refitted separately on the lower and upper groups, and the ratio of their residual
variances (`Estimate2`/`Estimate1`, upper over lower) is compared with an F distribution. The default one-sided
test (`twoSided: false`) only flags variance rising with the predictor; `twoSided: true` also flags variance
falling. Omitting about 20 percent of the central rows increases power, and each group needs more rows than
coefficients; the test assumes normal errors.

**`ResidualNormalityTests`** summarises whether a linear regression's residuals look normal, which matters for the
exactness of t tests, F tests and prediction intervals in small samples. It returns the residual mean, standard
deviation, skewness, excess kurtosis, the Jarque-Bera statistic and its chi-square (2 df) p-value, the
Shapiro-Wilk statistic and p-value (only when 4 to 5000 residuals are available and they are not all identical,
otherwise `NaN`), the extremes, and the fractions of residuals beyond 2 and 3 standard deviations (about 0.046 and
0.003 under normality). With many rows the tests flag trivial departures while coefficient inference stays
approximately valid by the central limit theorem; with few rows heavy tails or skew matter more.

**`RamseyResetTest`** tests for omitted curvature or non-linear terms: powers 2 up to `power` (2 to 5, default 3)
of the fitted values are added as extra predictors, and an F test checks whether they jointly improve the fit. A
small p-value means the linear specification is inadequate — curved relationship, or an omitted variable or
interaction — without saying which; `Estimate1`/`Estimate2` are the R squared of the original and augmented
models. A model that already fits the outcome exactly, or whose augmented powers are collinear with the existing
predictors, is rejected as undefined.

**`ChowStructuralBreakTest`** tests whether a regression relationship changes at a known point in the row order
(for example before/after a policy change): the model is fitted separately before and from `breakIndex` (the
zero-based first row of the second period) and an F test compares the separate fits against the single pooled
fit, F = ((RSS_pooled - RSS1 - RSS2)/k) / ((RSS1+RSS2)/(n-2k)). `Estimate1`/`Estimate2` hold the two periods'
residual sums of squares; each period needs more rows than coefficients, and rows must already be ordered so the
break genuinely splits the two regimes.

**`VarianceInflationFactors`** is the main per-predictor multicollinearity check: for each column it returns
VIF = 1/(1 - R squared of that column regressed on every other column), `Tolerance` (1/VIF), `RSquaredOnOthers`,
`StandardErrorInflation` (sqrt(VIF), how many times larger that coefficient's standard error is than with
uncorrelated predictors) and a `Severity` label (`Low` below 5, `Moderate` 5 to 10, `High` above 10). A column
that is an exact linear combination of the others gets VIF infinity; with a single column VIF is always 1.
Outcome values are not needed — only `inputs`. High VIFs are fixed by dropping or combining predictors, or by
switching to `FitRidgeRegression`.

**`MulticollinearityConditionDiagnostics`** diagnoses collinearity with the Belsley-Kuh-Welsch method: predictor
columns (plus the intercept column, when `includeIntercept` is true, the Belsley-recommended default) are scaled
to unit length and their singular values examined. Each row of `Rows` is one singular value with its
`ConditionIndex` (largest singular value divided by this one — above about 10 moderate, above 30 severe) and
`VarianceProportions`, the share of each coefficient's variance attributable to that component; a collinearity
problem shows up as a high condition index with variance proportions above about 0.5 for two or more variables,
which are then the variables involved in the near-dependency. Also returned: the overall `ConditionNumber` of the
scaled design, the condition number and determinant of the predictor correlation matrix (near-zero determinant
means near-singular), and the numerical rank. Outcome values are not needed. Use `VarianceInflationFactors`
instead for a simpler per-variable summary.

**`AddedVariablePlotData`** produces the data for an added-variable (partial regression) plot, showing one named
`variable`'s effect after every other predictor has been accounted for: the outcome and the chosen predictor are
each regressed on the remaining predictors, and the two residual sets are returned as paired points. By the
Frisch-Waugh-Lovell theorem, the slope of a line through those points equals the predictor's coefficient in the
full multiple regression, and their correlation is the partial correlation — useful for seeing whether a
coefficient is driven by a few points or is genuinely linear. Needs at least two predictors so there is something
to control for; `KeptRowIndices` maps the residual pairs back to the original rows.

**`CheckLinearRegressionAssumptions`** runs the standard OLS assumption checks in one call, each reported as
passed or failed at the 5 percent level with a plain-language comment: Linearity (`RamseyResetTest`, p-value ≥
0.05 passes), Independence (`BreuschGodfreySerialCorrelationTest` order 1, plus Durbin-Watson between 1.5 and
2.5 — only meaningful when rows are in time order), Constant variance (studentised Breusch-Pagan), Normality of
residuals (Jarque-Bera, and Shapiro-Wilk when available), Multicollinearity (largest VIF below 10) and Influence
(no row with Cook's distance above 1, and fewer than 5 percent of rows above 4/n). A failed check's comment points
to the specific tool to run next (`RegressionInfluenceDiagnostics`, `VarianceInflationFactors`,
`ResidualNormalityTests`, or the individual diagnostic tool); a check that cannot be computed at all (too few
rows, collinear terms) is reported as not passed, with a comment explaining why rather than failing the whole
call.

## Model selection (`RegressionModelSelectionTools`)

**`BestSubsetLinearRegression`** searches every combination of predictors (exhaustive, not greedy) and ranks the
resulting regressions. `criterion` is `Aic` or `Bic` (lower better), `AdjustedRSquared` (higher better) or
`MallowsCp` (lower better; a good model has Cp close to its own parameter count, computed against the full
model's residual variance). It returns `TopModels` overall plus `BestPerSize`, the single best model for each
predictor count, showing how much each extra predictor actually buys. Every subset reports R squared, adjusted R
squared, AIC, BIC, Cp and residual standard error. The search cost doubles with each predictor, so at most 15
predictors are accepted (`maximumSubsetSize`, default uncapped, can additionally restrict the largest subset
tried); collinear subsets are skipped and counted rather than causing an error.

## Regularised and robust regression (`RegularisedAndRobustRegressionTools`)

**`FitRidgeRegression`** fits an L2-penalised (ridge) regression, shrinking coefficients toward zero to stabilise
estimates when predictors are collinear or numerous relative to the row count. Predictors are standardised to unit
sample variance and the outcome centred, then the sum of squared residuals plus `lambda` times the sum of squared
standardised coefficients is minimised; `lambda` 0 reproduces ordinary least squares, larger values shrink harder.
It returns coefficients on both the original and standardised scale (the latter comparable across predictors),
the intercept, the effective degrees of freedom excluding the intercept, R squared, the residual standard error,
the generalised cross-validation score n*RSS/(n-1-EffectiveDegreesOfFreedom)^2 (lower is better, used to compare
lambdas) and approximate standard errors that ignore shrinkage bias — no p-values are given, because shrunken
estimates are biased. At most 500 predictor columns are accepted. Use `TraceRidgeRegularisationPath` to choose
lambda rather than guessing it.

**`TraceRidgeRegularisationPath`** fits ridge regression across a whole range of penalty strengths so the
coefficient shrinkage can be seen and lambda chosen with evidence. For every `lambda` it returns the coefficients
(both scales), intercept, effective degrees of freedom, R squared and the generalised cross-validation score;
`BestLambdaByGcv` and `BestIndex` name the lambda with the lowest GCV, a sensible default balancing fit against
complexity. Supplying `lambdas` uses exactly those values (up to 500); leaving it empty generates `pathLength`
(3 to 200, default 25) values spaced geometrically from 1e-4 to 1e2 times n-1, plus lambda 0. Coefficients that
change sign or swing wildly at small lambda mark collinear predictors.

**`FitNonNegativeLeastSquares`** fits a linear model with no intercept in which every coefficient is constrained
to zero or positive (Lawson-Hanson NNLS) — for mixing proportions, additive cost components, or attributing a
total to non-negative drivers where a negative contribution would be meaningless. Coefficients pushed to exactly
0 are the drivers the data does not support; `ActiveVariables` lists those left with a positive coefficient. It
returns the coefficients, residual sum of squares, RMSE and the uncentred R squared (1 - RSS/sum of squares of y,
because there is no intercept to absorb the mean). No standard errors or p-values are produced, because the
non-negativity constraint invalidates ordinary least-squares inference.

**`FitRansacLinearRegression`** fits a linear regression that resists gross outliers using RANSAC: it repeatedly
fits the model to a random minimal subset of rows, counts the rows within `inlierThreshold` of that fit, keeps
the fit with the most inliers, then refits by ordinary least squares on those inliers alone. Use it when a
minority of rows are corrupted (wrongly entered amounts, sensor glitches) and an ordinary fit is dragged off by
them. It returns `InlierModel` (the full coefficient table and statistics fitted only on inliers), `OrdinaryModel`
(the plain fit on every row, for comparison), the inlier and outlier row indices, the inlier fraction, the
threshold actually used, the trials performed and the `seed`. Leaving `inlierThreshold` empty uses 2.5 times the
robust scale (1.4826 x median absolute deviation) of the ordinary fit's residuals; results are reproducible for a
given seed. Because the inlier set itself was chosen by the data, the inlier model's p-values should be treated
as optimistic — the selection step is not accounted for in its inference.

**`FitMultivariateRegressionByPartialLeastSquares`** fits a partial least squares (PLS, SIMPLS algorithm on
standardised predictors) regression of one numeric outcome on many, possibly collinear, predictors by extracting
`components` latent factors that maximise covariance with the outcome — useful when predictors outnumber or
nearly equal the rows, or are highly correlated, where ordinary least squares breaks down. It returns the
coefficients on the original scale and the intercept for the chosen component count, R squared, residual standard
error, the share of predictor and response variance each component explains (with cumulative shares showing how
many components are worth keeping), and the Variable Importance in Projection per predictor (values above about 1
mark influential predictors). With as many components as predictors the result equals ordinary least squares;
fewer components is how this tool regularises.

**`CrossValidateLinearRegression`** estimates how well a linear regression will predict unseen rows, by `folds`
-fold cross-validation with a seeded shuffle (`seed`), alongside the exact leave-one-out figures computed directly
from the hat matrix (no refitting needed for those). It returns per-fold RMSE, MAE and R squared, the pooled
cross-validated RMSE, MAE and R squared (a large gap against the in-sample RMSE/R squared signals overfitting),
the leave-one-out RMSE, the PRESS statistic (sum of squared leave-one-out prediction errors) and the predicted R
squared = 1 - PRESS/total sum of squares. Every fold refits on its training rows only; `folds` must be between 2
and the row count, and every fold's training set must have more rows than coefficients.

## Non-linear curves (`NonlinearCurveTools`)

**`FitNonlinearCurve`** fits a named non-linear curve y = f(x) by non-linear least squares (Levenberg-Marquardt)
and reports parameter estimates with inference. `family` selects the shape: `Exponential` (growth/decay,
scale*exp(rate*x)), `AsymptoticExponential` (approach to a ceiling or floor, asymptote - amplitude*exp(-rate*x)),
`SaturatingExponential` (maximum*(1-exp(-rate*x)), through the origin), `Power` (scale*x^exponent, x > 0),
`Logarithmic` (intercept + slope*ln x), `Logistic3` (S-curve from 0 to a maximum, with steepness and midpoint),
`Logistic4` (S-curve between a lower and upper level), `Gompertz` (asymmetric S-curve), `MichaelisMenten`
(saturating hyperbola maximumRate*x/(halfSaturation+x)), `Hill` (steeper saturating curve with a coefficient),
`GaussianPeak` (bell-shaped peak) or `Reciprocal` (1/(intercept+slope*x)); the result's `Formula` field states the
exact equation used. Each parameter row has the estimate, standard error, t statistic, p-value and confidence
interval from the Gauss-Newton covariance sigma^2 (J'J)^-1 — an approximation that is poor in small or strongly
curved samples. Model-level fields: residual sum of squares, residual standard error, R squared, adjusted R
squared, Gaussian AIC and BIC, `Converged` and `Warnings`. Starting values are found automatically; supply
`startValues` (in the order of the parameter names shown in `Formula`) when the automatic fit fails or lands on a
poor local minimum, since non-linear fits can have several. `predictAt` returns predictions with delta-method
intervals.

**`CompareNonlinearCurveFamilies`** fits several named curve families to the same `x` and `y` (or, with `families`
left empty, all twelve families that the data's domain allows) and ranks them to find which shape fits best. For
each family it returns whether it could be fitted at all (with a reason when not — for example x must be above 0
for `Power` and `Hill`), R squared, residual standard error, AIC, BIC, the Akaike weight (relative support among
the fitted families, summing to 1) and the fitted parameters; `Rank` 1 is the lowest AIC. `BestByAic` and
`BestByBic` name the winners by each criterion; a difference in AIC under about 2 means the families are
practically indistinguishable, favouring the simpler or more domain-plausible one. Automatic starting values are
used for every family here, so a family may fail or land on a local minimum — refit the finalists directly with
`FitNonlinearCurve` and explicit `startValues` before relying on the result.
