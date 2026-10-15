---
layout: default
title: Numerics and Optimisation Tool Catalogue
nav_order: 15
parent: Reference
grand_parent: Analytical Methodology
---

# Numerics and optimisation tool catalogue

`Jube.Service/Agent/AdvancedAnalyticsTools/NumericsAndOptimisation` is the largest of the ten AdvancedAnalyticsTools
groups: 132 methods, each exposed to the LLM agent under the literal name in its `[AdvancedAnalyticsTool("...")]`
attribute, covering calculus and root finding, combinatorics and array utilities, linear algebra, matrix
decompositions, optimisation, and the special functions that the statistical tool layer is built on. None of these
132 names appears in `docs/AgentHierarchy/tool-inventory.md` or anywhere else in `docs/`, so Jadek, the
validator agent that fact-checks every AskJooby reply against the RAG-indexed corpus built from `docs/`, has no page
to confirm a claim about any of them against except this one. This page covers every tool, grouped by the six source
files that define them (`NumericsGuard.cs`, `NumericsShared.cs` and `NumericsText.cs` hold shared validation and
function-family plumbing, not tools in their own right). Two tools, `SortValuesWithOriginalIndices` and
`SortValuesByKeys`, already had genuine behavioural coverage elsewhere, in
[Data Frames vs. Accord Arrays](../../DataFramesVsAccordArrays.html) and
[Loading, Shaping and Merging](../../LoadingShapingMerging.html); they are documented here in full as well, for a
single point of reference, with that prior coverage cited in place.

Every tool in this group validates its inputs through `NumericsGuard` before doing any numerical work: arrays must
be non-null and within a maximum length, matrices must be rectangular (and square, symmetric or finite as the tool
requires), and most numeric inputs are rejected outright if NaN or infinite rather than silently propagated. Several
tools share built-in function families defined once in `NumericsShared.cs`: `NumericsUnivariateFunction` (polynomial,
sine, cosine, exponential, natural log, square root, power, reciprocal, Gaussian, logistic, hyperbolic tangent,
absolute value) for one-variable work, and `NumericsObjectiveFunction` (Sphere, Rosenbrock, Rastrigin, Ackley,
Himmelblau, Booth, Beale, Matyas, Styblinski-Tang, Griewank, and a general Quadratic form) for multi-variable
optimisation and calculus tools. Where a tool takes one of these, its exact parameter defaults and domain are given
in the shared `NumericsText.Function` and `NumericsText.Objective` description strings rather than repeated per
tool.

## CalculusAndRootFindingTools

**EvaluateNamedFunction** evaluates one of the twelve built-in one-variable functions at each of a supplied array of
x values and returns the values as a `NumericsVectorResult`, with a separate `NonFiniteCount` so a caller can see how
many points fell outside the function's domain (for example a negative x fed to `NaturalLog`, which produces NaN
there) without having to scan the array. It is the inspection step before integrating, differentiating or
root-finding the same function. Up to 200,000 x values are accepted.

**IntegrateTrapezoidalRule** approximates the definite integral of a built-in function from `lower` to `upper` with
the composite trapezoidal rule over a chosen number of equal steps (default 1,000, capped at 1,000,000). It is only
second-order accurate, so halving the step only quarters the error; it exists as a simple, robust baseline to compare
against `IntegrateRomberg` or `IntegrateGaussKronrod`. The result never reports an error estimate (`ErrorEstimate` is
always NaN) and `Converged` is simply whether the computed area came out finite.

**IntegrateRomberg** approximates the same kind of integral by Romberg's method: repeated step-halving combined with
Richardson extrapolation, which reaches very high accuracy on smooth functions with far fewer evaluations than the
trapezoidal rule, at the cost of being unsuitable for functions with kinks or singularities inside the interval. The
`steps` parameter (default 10, 2 to 30) is the number of refinement levels, and `FunctionEvaluations` in the result
is derived as `2^(steps-1) + 1` rather than counted directly.

**IntegrateGaussKronrod** integrates a built-in function over a finite interval with a non-adaptive Gauss-Kronrod
rule, driven to a target relative tolerance (default 1e-8, allowed 1e-14 to 0.1) via Accord's
`NonAdaptiveGaussKronrod`. It is the recommended default accurate integrator for smooth functions: `Area` is the
estimate, `ErrorEstimate` the library's own absolute error bound, and `Status` is `Success` when the tolerance was
met or `MaximumSteps` when the best available estimate was returned without reaching it.

**IntegrateAdaptiveGaussKronrodInfinite** integrates a built-in function over a range where either or both ends may
be infinite (`lowerIsInfinite` / `upperIsInfinite`, each default false — when set, the matching finite `lower` or
`upper` argument is simply ignored), using adaptive Gauss-Kronrod subdivision capped at `subintervals` (default 200,
up to 2,000). This is the tool for tail probabilities and expectations over the whole real line, for example
integrating a Gaussian end to end. `Converged` requires both that the solver reported success and that the resulting
`Area` is finite, since a genuinely divergent integral can otherwise report numeric success with an infinite value.

**IntegrateMonteCarloOnInterval** estimates a one-dimensional integral by averaging the function at uniformly random
points drawn with a `seed` (default 1) for reproducibility, over `samples` (default 100,000, up to 5,000,000) draws.
It is unbiased but converges only as the square root of the sample count, so it is a fallback rather than the
preferred one-dimensional method; `ErrorEstimate` is the Monte Carlo one-standard-error uncertainty from Accord's
`MonteCarloIntegration`.

**IntegrateMonteCarloObjectiveOverBox** is the multi-variable counterpart: it integrates a built-in
`NumericsObjectiveFunction` (including the `Quadratic` family, via `quadraticMatrix`, `linearTerms` and
`constantTerm`) over an axis-aligned box defined by one `lowerBounds`/`upperBounds` pair per variable, by uniform
random sampling. The number of variables is implied by the length of the bounds arrays (1 to 50), and this is the
practical route once dimensionality is too high for grid-based quadrature.

**NumericalDerivativeOfNamedFunction** estimates the derivative of a built-in function at a point `x` by central
finite differences, for `order` 1 (slope) through 4, using an odd number of stencil `points` (3 to 9, default 5,
must exceed the order) and a relative `stepSize` (default 0.01, 1e-8 to 1). The accuracy trade-off is explicit in the
description: too small a step amplifies rounding error, too large a step adds truncation error, and more stencil
points buys accuracy at the cost of more function evaluations.

**NumericalGradientOfObjective** estimates the gradient vector of a built-in multi-variable objective at a `point`
(up to 50 variables) by the same finite-difference machinery, returning a `NumericsVectorResult` with one entry per
variable. Because the gradient is zero at a stationary point, this is the tool for checking whether a candidate
optimum returned by one of the `Minimise*` tools is genuinely stationary; expect rounding error around 1e-6 relative
with the default step size and stencil.

**NumericalHessianOfObjective** estimates the full matrix of second partial derivatives of a built-in objective at a
`point` (up to 20 variables, a tighter cap than the gradient because the Hessian needs nested differencing) using a
`stepSize` between 1e-6 and 1 (default 0.01). The result is symmetric and describes local curvature: positive
definite at a minimum, negative definite at a maximum, indefinite at a saddle; pairing it with
`SymmetricEigenDecomposition` reveals the actual curvature directions.

**NumericalJacobianOfVectorFunction** estimates the Jacobian of one of five built-in vector-valued functions
(`PolarToCartesian`, `SphericalToCartesian`, `Softmax`, `RosenbrockResiduals`, `HimmelblauResiduals`) at a `point`,
with each function requiring a specific point length (2 for polar/Rosenbrock/Himmelblau, 3 for spherical, any length
for Softmax) that is checked and rejected with a specific error if wrong. Row i, column j of the returned matrix is
the derivative of output i with respect to input j, computed with a 5-point stencil regardless of the step-size
parameter passed.

**FindRootBrent** finds a root of a built-in function on `[lower, upper]` using Brent's method (a combination of
bisection, secant and inverse quadratic interpolation that is guaranteed to converge once a sign change is
bracketed). If `f(lower) - target` and `f(upper) - target` do not have opposite signs, `Status` comes back
`RootNotBracketed` rather than throwing; `FindRootBracketsByScanning` is the companion tool for locating a bracketing
interval first. Iterations are capped at 500 and tolerance at 1e-14 to 1 (default 1e-10).

**FindRootBracketsByScanning** scans `[lower, upper]` on an even grid of `gridPoints` (default 1,000, up to 200,000)
and reports every sub-interval where `f(x) - target` changes sign, each of which is a valid input to `FindRootBrent`,
plus a separate `ExactZeros` list for grid points landing exactly on a root. Because it only detects a sign change
between adjacent grid points, two roots closer together than the grid spacing inside one cell cancel and are missed
entirely — a real limitation worth flagging to a caller scanning an oscillating function with too coarse a grid.

**FindExtremumBrent** finds one local minimum or maximum (`maximise`, default false) of a built-in function on
`[lower, upper]` with Brent's derivative-free golden-section-with-parabolic-steps method. It finds a single local
extremum assuming the function is unimodal on the interval, not necessarily the global one; `Converged` false means
the 500-iteration cap (default tolerance 1e-8) was exhausted before the acceptance criterion was met.

**EvaluatePolynomial** evaluates `c0 + c1 x + c2 x^2 + ...` at each of a supplied x array using Horner's scheme for
numerical stability, with coefficients ordered from the constant term upward (up to 40 coefficients, 200,000 points).

**PolynomialDerivativeOrAntiderivative** returns the coefficient array of either the derivative or the
antiderivative (`NumericsPolynomialCalculus.Derivative` or `.Antiderivative`, default `Derivative`) of a polynomial.
The derivative of a single-coefficient (constant) polynomial returns the single coefficient `0`; the antiderivative
takes an explicit `constant` of integration (default 0) as its new leading (constant) term and is capped at 39 input
coefficients so the result still fits the 40-coefficient limit.

**PolynomialMultiply** multiplies two polynomials given as coefficient arrays (constant term first, each capped at
20 coefficients) by direct convolution, returning a coefficient array whose length is `a.Length + b.Length - 1`, i.e.
the product's degree is the sum of the two input degrees.

**PolynomialRoots** finds every real and complex root of a polynomial by building its companion matrix and taking
its eigenvalues (via `JaggedEigenvalueDecomposition`). The leading (highest-degree) coefficient must be non-zero —
trailing zero coefficients should be dropped to lower the degree rather than left in — and results are returned as
parallel `RealParts`/`ImaginaryParts` arrays sorted by real then imaginary part, where a non-zero imaginary part
signals a complex-conjugate pair. The description is explicit that accuracy degrades for high degrees or repeated
roots; degree is capped at 30.

**FitPolynomialByLeastSquares** fits a polynomial of a chosen `degree` (0 to 15) to `(x, y)` points by ordinary least
squares via a Vandermonde design matrix solved with `Solve(y, true)`, returning the fitted `Coefficients` and the
`ResidualSumOfSquares`. There must be strictly more points than the degree (an explicit check throws naming the
exact shortfall), and the description warns that high degrees over a wide x range are ill-conditioned, recommending
the caller centre and scale x first; `EvaluatePolynomial` is the natural pairing for prediction afterward.

**InterpolatePolynomialThroughPoints** builds the unique degree-`(n-1)` polynomial passing exactly through `n`
points with distinct x values (duplicate x values are rejected outright), solving the resulting Vandermonde system
exactly. It is intended for small tables only — at most 20 points — because high-degree interpolation through many
points oscillates wildly between the nodes (Runge's phenomenon) and the system becomes severely ill-conditioned.

**InterpolateLinearly** estimates y at each of a `queries` array by piecewise linear interpolation between sorted,
strictly increasing `(x, y)` points (the strict-increase check throws naming the offending pair). Queries below the
first x or above the last return the caller-supplied `belowValue`/`aboveValue` (both default NaN) rather than
extrapolating, so the tool never silently extends the data; passing the edge y values instead holds the curve flat
beyond the data range.

**ExtrapolateLinearly** is the complement: for queries outside the sorted, strictly increasing data range, it
extends the line through the nearest end segment (first two or last two points) rather than returning a sentinel;
queries inside the range interpolate normally on whichever segment they fall in. The description is explicit that
this should only be used for short extrapolation distances, since most real relationships stop being linear further
out.

## CombinatoricsAndUtilityTools

**EnumeratePermutations** lists orderings of up to 10 supplied values, one ordering per output row, capped at
`maxResults` (default 1,000, up to 20,000); `TotalCount` is the true `n!` regardless of truncation, and duplicate
input values still occupy distinct positions, so a repeated value produces repeated rows rather than being
deduplicated.

**EnumerateCombinations** lists every way to choose `k` of up to 25 supplied values without regard to order, one
selection per row, again capped at `maxResults` with `TotalCount` reporting the true `n choose k` via
`Special.Binomial`.

**EnumerateSubsets** lists every non-empty subset of up to 20 supplied values (including the full set itself), with
`TotalCount` equal to `2^n - 1`; rows vary in length since subsets of different sizes are mixed together in output
order.

**EnumerateSequences** lists every sequence of a given `length` (1 to 12) over an alphabet of `symbols` (2 to 20)
numbered `0` to `symbols - 1`, the Cartesian power with `symbols^length` total sequences, useful for exhaustively
gridding small categorical configurations.

**GenerateTruthTable** returns every combination of `0`/`1` values for a given number of boolean `inputs` (1 to 14)
in counting order, `2^inputs` rows in total with no truncation possible (there is no `maxResults` parameter — the
input cap of 14 already bounds the output to 16,384 rows).

**CountCombinatorialObjects** counts arrangements without enumerating them, for any of five `kind` values:
`Permutations` (`n!`), `PartialPermutations` (`n!/(n-k)!`, ordered picks), `Combinations` (`n choose k`),
`CombinationsWithRepetition` (`(n+k-1) choose k`), and `Sequences` (`n^k`). `Count` is computed from a log-space
intermediate so it stays a finite double (or correctly reports `Infinity` on true overflow) even where the exact
integer would not fit; `NaturalLogOfCount` always stays finite, and `IsExact` is true only when the log is below
30 (count under roughly 10 trillion), below which the count is rounded to the exact integer and above which it is
accurate only to about 12 significant digits. `n` and `k` are each capped at 1,000,000, and `k > n` for
`PartialPermutations`/`Combinations` returns an exact zero count rather than throwing.

**GreatestCommonDivisorAndLeastCommonMultiple** computes, for each pair taken from two equal-length integer arrays
(values capped at ±1,000,000,000, up to 100,000 pairs), both the GCD and LCM, returned concatenated in one
`NumericsVectorResult` — all GCDs first, then all LCMs in the same order. `gcd(0, x)` is defined as `|x|`, and the
LCM of any pair involving zero is `0`.

**EuclideanModulo** computes the mathematical modulo `x mod m`, which is always in `[0, m)` for a positive modulus,
unlike the C#/most languages' `%` remainder operator which can return a negative result for negative `x`. It accepts
fractional `x` and requires a strictly positive `modulus`; the intended uses given are wrapping angles, hours or
cyclic indices.

**PowerOfTwoProperties** reports, for each positive integer in `values` (1 to 1,073,741,823, up to 100,000 values),
four facts packed back-to-back into one `Values` array: the next power of two at or above it, the previous power of
two at or below it, `ceil(log2(value))` (bits needed), and a 1/0 flag for whether it is itself a power of two — aimed
at sizing FFT buffers or hash tables.

**FindIntegerByBinarySearch** finds the integer in `[lower, upper)` (upper excluded) at which a built-in function is
closest to a `target`, via Accord's `BinarySearch`, which works for either a monotonically increasing or a
monotonically decreasing function (both directions are exercised in the test suite) but assumes the function is one
or the other over the searched range — it is not told which, and gives an unreliable answer on a non-monotone
function. Both `lower` and `upper` are capped at ±1,000,000,000 and `upper` must strictly exceed `lower`.

**SortValuesWithOriginalIndices** stably sorts a `double[]` ascending or descending (`descending`, default false)
and returns both the sorted `Values` and the `OriginalIndices` recording where each sorted value came from, so the
same permutation can be applied to other parallel arrays by hand. NaN values are treated as the smallest possible
value by the sort, and up to 200,000 values are accepted. This tool, together with `SortValuesByKeys`, is also
discussed as the closest AdvancedAnalytics primitive to a dataframe's `arrange()`/row-sort in
[Data Frames vs. Accord Arrays](../../DataFramesVsAccordArrays.html#arrange---sort-the-whole-table-by-a-column)
and [Loading, Shaping and Merging](../../LoadingShapingMerging.html#procedure-11-sorting-a-data-frame-with-the-arrange--function), which note that
both tools operate on a single vector rather than a whole table: reordering a multi-column table by one column's
sort order needs the caller to apply `OriginalIndices` to every other column itself, since no tool here takes a
table plus a row-index permutation and returns the reordered table.

**SortValuesByKeys** reorders one `values` array to follow the ascending sort order of a parallel `keys` array of
the same length (for example, ordering transaction amounts by their timestamps); values with tied keys keep their
original relative order (a stable sort via `OrderBy(...).ThenBy(i => i)`). Up to 200,000 pairs are accepted. See the
cross-reference under `SortValuesWithOriginalIndices` above for the same caveat about table-wide sorts.

**SelectNthSmallestValue** returns the zero-based n-th smallest value (or n-th largest when `descending`) of an
array by quickselect (`Sort.NthElement`), which finds the one order statistic without sorting the whole array; `n`
must be strictly less than the number of values, and this is the tool for medians or percentiles of large arrays
where a full sort would be wasteful.

**SelectSmallestValuesSorted** returns the `count` smallest (or largest, when `descending`) values already in
sorted order, via `Sort.Partial` when `count` is less than the array length (and a full `Array.Sort` plus reversal
when it equals the whole array), avoiding sorting the remainder. This is the top-N tool, for example the ten largest
transactions in a batch; `count` cannot exceed the number of values.

**GenerateUniformRandomNumbers** draws `count` (up to 1,000,000) reproducible pseudo-random numbers uniformly on
`[minimum, maximum]` using Accord's Ziggurat generator seeded by `seed` (default 1), so identical arguments always
produce identical output; `maximum` must strictly exceed `minimum`.

**GenerateNormalRandomNumbers** draws `count` reproducible normal (Gaussian) pseudo-random numbers with a given
`mean` (default 0) and `standardDeviation` (default 1, must be positive), again via the seeded Ziggurat generator,
by drawing standard normals and affinely transforming them.

**GenerateExponentialRandomNumbers** draws `count` reproducible exponential pseudo-random numbers with a given
`rate` (default 1, must be positive; the mean of the output is `1/rate`), for simulating waiting times or
inter-arrival gaps between events.

**MonteCarloExpectationOfNamedFunction** estimates `E[f(X)]` for a built-in univariate function `f` of a random
variable `X` drawn from `Uniform` (`distributionParameters` = `[minimum, maximum]`), `Normal` (`[mean,
standardDeviation]`) or `Exponential` (`[rate]`), by drawing `samples` (default 100,000, up to 5,000,000) seeded
values and averaging. The result carries `Estimate` (sample mean), `StandardError` (sample standard deviation over
root-n), `SampleStandardDeviation`, and an approximate 95% interval built from the normal critical value
1.959963984540054; the description notes that quadrupling the sample count only halves the standard error, which is
the usual Monte Carlo rate.

**MonteCarloProbabilityOfNamedFunction** estimates `P(lower <= f(X) <= upper)` for the same three distribution
families by counting the sampled share inside the range — either bound can be omitted (`null`) for an open-ended
range. `StandardError` is the binomial `sqrt(p(1-p)/n)` and the confidence bounds are clipped to `[0, 1]`; the
description flags that rare events with true probability below about 1e-4 need very large sample counts to estimate
reliably, since so few draws land inside the range at all.

**GetMathematicalConstants** takes no arguments and returns a fixed table of 17 named constants (`Pi`, `E`,
`EulerGamma`, `Catalan`, `Sqrt2`, `Sqrt2OverTwo`, `Sqrt2Pi`, `SqrtHalfPi`, `LogPi`, `Log2`, `Log3`, `LogSqrt2Pi`,
`Log2Pi`, `DoubleEpsilon`, `SingleEpsilon`, `DoubleSmall`, `LogMax`, `LogMin`), each with its double-precision `Value`
and a one-line `Meaning` — a lookup to avoid an agent typing a mathematical or machine constant from memory.

**NumericsFastFourierTransform** computes the discrete Fourier transform of a real or complex sequence (up to 65,536
values) by FFT, fastest at power-of-two lengths but correct at any length; `direction` (`Forward` or `Inverse`,
default `Forward`) selects which way, and `Inverse` undoes `Forward` exactly including the `1/n` scaling, so feeding
a Forward result's `Real`/`Imaginary` pair back through `Inverse` recovers the original signal. The worked example in
the description — `Forward([1, 2, 3, 4])` gives `10, -2+2i, -2, -2-2i` — pins down the convention and scaling used.

**NumericsDiscreteCosineTransform** computes the orthonormal type-II discrete cosine transform of a real sequence
(up to 20,000 values), which concentrates energy from smooth signals into the first few coefficients (coefficient 0
is the scaled mean level) and underlies image and audio compression; `Inverse` undoes `Forward` exactly.

**NumericsDiscreteSineTransform** computes the discrete sine transform of a real sequence (up to 20,000 values),
suited to signals that vanish at the boundaries, with `Inverse` undoing `Forward`.

**NumericsHartleyTransform** computes the discrete Hartley transform of a real sequence (up to 20,000 values), a
real-valued cousin of the Fourier transform built from cosine plus sine waves and scaled by `1/sqrt(n)` so that
applying the transform to its own output a second time returns the original signal (it is self-inverse, so there is
no `direction` parameter).

**NumericsHilbertTransform** computes the discrete Hilbert transform of a real signal whose length must be an exact
power of two (up to 65,536), shifting every frequency component by 90 degrees — the Hilbert transform of a cosine is
the matching sine — so that, together with the original signal, it forms the analytic signal whose magnitude gives
the envelope.

**NumericsHaarWaveletTransform** applies one full pass of the Haar wavelet transform to a signal of even length (up
to 65,536): the first half of the `Forward` output holds pairwise averages (the approximation) and the second half
the half-differences (the detail) at that scale; `Inverse` undoes `Forward`. Multi-level decomposition is left to
the caller, who repeats the call on the first half of the previous output.

**NumericsCdf97WaveletTransform** applies one pass of the Cohen-Daubechies-Feauveau 9/7 biorthogonal wavelet
transform — the wavelet used in lossy JPEG2000-style image compression — to a signal of even length, at least 4 and
at most 65,536: smooth approximation coefficients first, then detail coefficients; `Inverse` undoes `Forward`.

## LinearAlgebraTools

**MatrixMultiply** multiplies an `m`-by-`k` matrix `a` by a `k`-by-`n` matrix `b`, giving an `m`-by-`n` result;
`a`'s column count must equal `b`'s row count, and both inputs are capped at 300 by 300 with every entry required to
be finite.

**MatrixVectorMultiply** multiplies an `m`-by-`n` matrix `a` by a length-`n` vector `v`, giving a length-`m` result —
the tool for applying a linear map or computing fitted values from a design matrix and a coefficient vector.

**MatrixTranspose** returns the transpose of an `m`-by-`n` matrix as an `n`-by-`m` matrix, for reshaping before a
multiply whose dimensions would not otherwise line up.

**MatrixElementwiseOperation** applies one of `Add`, `Subtract`, `Multiply` (the Hadamard, entrywise product,
*not* matrix multiplication — `MatrixMultiply` is the tool for that), `Divide` or `Power` (each element of `a` raised
to the matching element of `b`) between two identically shaped matrices (default `Add`). Division by zero or a
negative base with a fractional exponent produces NaN or infinity in that cell rather than throwing, which a caller
inspecting the result should be aware of.

**MatrixInverse** computes the inverse of a square, non-singular matrix via LU decomposition so that `A` times the
inverse is the identity; a singular matrix (zero determinant, dependent rows) is rejected with a message pointing the
caller at `MatrixPseudoInverse` instead. The description recommends `SolveLinearSystem` over explicit inversion
whenever only `A^-1 b` is actually needed, since solving is more numerically accurate than inverting and multiplying.

**MatrixPseudoInverse** computes the Moore-Penrose pseudo-inverse of any matrix — square or rectangular, full rank
or not — via the singular value decomposition, giving least-squares solutions for over-determined systems or
minimum-norm solutions for under-determined ones even when the matrix is singular. For a full column-rank matrix it
equals `(A'A)^-1 A'`.

**MatrixDeterminant** returns the determinant of a square matrix via LU decomposition together with the log of its
absolute value (`LogAbsoluteDeterminant`, useful when the determinant itself would overflow or underflow a double)
and its `Sign`; a zero determinant sets `IsSingular` true and `LogAbsoluteDeterminant` to negative infinity.

**MatrixPseudoDeterminant** returns, as a two-element `Values` array, the pseudo-determinant (the product of a
matrix's non-zero singular values) and its log, for matrices — singular or rectangular — where the ordinary
determinant is zero or undefined, such as a degenerate Gaussian density's covariance matrix.

**MatrixTrace** returns the sum of the diagonal elements of a square matrix, which also equals the sum of its
eigenvalues.

**MatrixRankAndCondition** reports the numerical rank, condition number (`largest/smallest` singular value, infinite
if the smallest is exactly zero) and the extreme singular values of any matrix, derived from its singular value
decomposition. `IsRankDeficient` is true whenever the rank is below the smaller of the row/column counts; the
description calls out that a condition number above roughly 1e12 means the matrix is numerically singular and any
solution computed from it will be unreliable.

**MatrixNorms** computes the four standard matrix norms in one call: `Norm1` (largest absolute column sum),
`NormInfinity` (largest absolute row sum, computed as `Norm1` of the transpose), `Frobenius` (root of the sum of
squared entries), and `Spectral` (the largest singular value) — used to bound how much a linear map can stretch a
vector.

**VectorNorms** returns either the unit-length copy of a vector (`normalise` true) or, by default, a two-element
`Values` array of its Euclidean (L2) norm and squared L2 norm. A zero vector has norm 0 and its "normalised" copy
stays all zeros, since it has no defined direction to normalise onto.

**SolveLinearSystem** solves the square system `A x = b` exactly by LU decomposition, returning `Solution`, the
residual norm `||A x - b||` (which is the accuracy check, not merely a diagnostic) and the condition number of `A`.
A singular `A` is rejected outright with a pointer to `SolveLeastSquares`; a large condition number (the description
cites the same roughly 1e12 threshold used elsewhere) warns that `x` is numerically unreliable even though a value
was returned.

**SolveLinearSystemMultipleRightHandSides** solves `A X = B` for a whole matrix of right-hand sides at once —
column `j` of the result solves `A x = ` column `j` of `B` — avoiding repeated calls to `SolveLinearSystem`; `A`
must be square and non-singular.

**SolveLeastSquares** finds the `x` minimising `||A x - b||^2` for any shape of `A` (QR for tall/over-determined
systems, SVD for wide or rank-deficient ones), the tool for fitting a line or plane through more observations than
unknowns. `ResidualNorm` is zero only for an exactly solvable system; a very large `ConditionNumber` warns of
collinear design-matrix columns.

**MatrixNullSpace** returns an orthonormal basis of the null space of a matrix as a set of row vectors, each
satisfying `A x = 0`, computed from the right singular vectors beyond the numerical rank in the singular value
decomposition. An empty (zero-row) result means the columns are independent and only the trivial solution exists;
a non-empty result reveals a hidden exact linear dependency among the input's columns.

**MatrixReducedRowEchelonForm** reduces any matrix to reduced row echelon form by Gauss-Jordan elimination via
`JaggedReducedRowEchelonForm`, exposing the pivot structure for solving small systems by hand-style elimination. The
description notes that entries near but not exactly zero are ordinary floating-point artefacts and should be
treated as zero by the caller.

**MatrixKroneckerProduct** returns the Kronecker (block tensor) product of two matrices: every element of `a`
scales a full copy of `b`, giving an `(m_a*m_b)`-by-`(n_a*n_b)` block matrix, used to build structured covariance or
design matrices. Because the result's dimensions multiply, both inputs must be small enough that the product still
fits the shared 300-by-300 ceiling, or the call is rejected up front naming the resulting size.

**VectorOuterProduct** returns the outer product of two vectors — a matrix whose `(i, j)` entry is `a[i] * b[j]` —
used to build rank-one matrices; each vector is capped at 300 values (the shared matrix dimension ceiling).

**VectorDotProduct** returns the sum of products of matching elements of two equal-length vectors (up to 200,000
values); a result of zero means the vectors are orthogonal.

**VectorCrossProduct** returns the 3-D cross product of two exactly-3-element vectors: a vector perpendicular to
both, whose length equals the area of the parallelogram the two inputs span. Either input not having exactly 3
elements is rejected.

**MatrixStructureProperties** is the one matrix tool in this group that deliberately accepts non-finite entries
(`finite: false` in its guard call) so it can report on them: it returns `Rows`, `Columns`, `IsSquare`,
`IsSymmetric` (checked with a `1e-9` relative tolerance), `IsPositiveDefinite` (only evaluated when the matrix is
already symmetric), `IsDiagonal`, `IsSingular` (only meaningful, and only computed, when the matrix is square and
clean of NaN/infinity), `HasNaN` and `HasInfinity` — a single diagnostic pass over a matrix of unknown provenance
before feeding it to a stricter tool.

**GenerateStructuredMatrix** builds a standard `size`-by-`size` matrix of a chosen `kind`: `Identity`, `Zeros`,
`Ones`, `Magic` (a magic square, size 3 or above only), `Hilbert` (entry `1/(i+j+1)`, a classic ill-conditioned test
matrix), or `Vandermonde` (needs a separate `nodes` array; row `i` is powers of `nodes[i]`, so the result has one row
per node and `size` columns rather than being square). Size is capped at 300.

**MatrixPower** raises a square matrix to a non-negative integer `power` (up to 1,000,000) by repeated squaring
(binary exponentiation) rather than naive repeated multiplication, so very high powers are still cheap; power 0
returns the identity. Large powers of an unstable matrix (spectral radius above 1) overflow entries to infinity, and
the description names Markov transition matrices and graph path counting as the typical uses.

**GenerateMeshGrid** builds a 2-D grid of every `(x, y)` pair from two independently evenly spaced axes, returned as
one `[x, y]` row per grid point (`xPoints` times `yPoints` rows total, each axis needing at least 2 points), for
evaluating a function over a plane; the two axis sizes are checked up front so their product cannot exceed 40,000
grid points.

**GenerateEvenlySpacedValues** returns `count` (2 to 200,000) evenly spaced values from `start` to `stop` inclusive —
a linear-space axis generator, typically feeding `EvaluateNamedFunction` or a plot.

**NumericsConvolveVectors** convolves a `signal` with a `kernel` (no longer than the signal, both up to 200,000
values), the moving-average/polynomial-multiplication style sliding weighted sum; with `trim` true (default false)
the output is the same length as the signal, otherwise it has length `signal.Length + kernel.Length - 1`.

**RescaleValuesToRange** linearly maps values from a source interval `[fromMinimum, fromMaximum]` onto a target
interval `[toMinimum, toMaximum]`, so the source minimum lands on the target minimum and the source maximum on the
target maximum — the normalise-to-`[0,1]`-or-convert-units tool. Values outside the source interval extrapolate
beyond the target interval rather than being clamped, and a zero-width source interval (`fromMinimum ==
fromMaximum`) is rejected since it cannot be mapped.

## MatrixDecompositionTools

**LuDecomposition** factors a square matrix as `L` (unit lower triangular) times `U` (upper triangular) with partial
pivoting, returning `Lower`, `Upper`, the `PivotPermutation` vector (row `i` of `L*U` corresponds to row
`PivotPermutation[i]` of the input) and the `Determinant` read off cheaply from the factorisation; `IsNonsingular`
false means a zero pivot was encountered during elimination, so the matrix has no inverse.

**QrDecomposition** factors a matrix with at least as many rows as columns as `Q` (orthonormal columns) times `R`
(upper triangular) — a row-deficient matrix (fewer rows than columns) is rejected with a suggestion to supply the
transpose instead. This is the numerically stable route to least-squares problems; `IsFullRank` false means the
columns are linearly dependent, detected from the magnitude of `R`'s diagonal relative to its largest entry.

**CholeskyDecomposition** factors a symmetric positive definite matrix as `L * L'` with `L` lower triangular,
returning the `Determinant` and `LogDeterminant` alongside it; unlike most tools in this group, a matrix that fails
the positive-definiteness check does **not** throw — it returns `IsPositiveDefinite = false` with empty factors and
NaN determinants, which is itself the validity test for a candidate covariance matrix. The description also names
drawing correlated random numbers and fast symmetric-system solving as typical uses.

**SolveSymmetricPositiveDefiniteSystem** solves `A x = b` for a symmetric positive definite `A` (such as a
covariance or normal-equations matrix) via Cholesky factorisation, roughly twice as fast as the general LU-based
solver; a matrix that is not positive definite is rejected outright with a pointer to `SolveLinearSystem` for the
general case.

**SingularValueDecomposition** factors any matrix as `U * diag(SingularValues) * V'`, with singular values returned
largest first; `Rank` is the count of singular values above the library's numerical tolerance and `ConditionNumber`
is their largest-to-smallest ratio. `V`'s columns are the right singular vectors — the principal directions of the
data when the input is mean-centred — and `U`'s columns the left singular vectors.

**SymmetricEigenDecomposition** finds the eigenvalues and orthonormal eigenvectors of a symmetric matrix such as a
covariance or correlation matrix; all eigenvalues are guaranteed real for symmetric input (`HasComplexEigenvalues`
is always false in the result), and `sortDescending` (default true) controls whether the largest eigenvalue comes
first. A non-symmetric input is rejected — `GeneralEigenDecomposition` is the tool for that case.

**GeneralEigenDecomposition** finds the eigenvalues and eigenvectors of any square matrix, symmetric or not, such
as a Markov transition matrix or a dynamical system's state matrix. Eigenvalues may be complex, returned as parallel
`RealEigenvalues`/`ImaginaryEigenvalues` arrays with `HasComplexEigenvalues` true whenever any imaginary part exceeds
`1e-12` in magnitude (a rotation-like or oscillating system); for a complex-conjugate pair, the real and imaginary
parts of the eigenvector occupy adjacent columns of `Eigenvectors`.

**GeneralizedEigenDecomposition** solves the generalised eigenproblem `A v = lambda B v` for two same-size square
matrices `A` and `B`, as used in linear discriminant analysis and vibration analysis (with `B` the identity it
reduces to the ordinary eigenproblem). `IsSingular` or `IsDegenerate` true warns that `B` itself is singular, so some
reported eigenvalues are effectively infinite or undefined rather than numerically meaningful.

**GramSchmidtOrthogonalisation** orthonormalises the columns of a square matrix via the Gram-Schmidt process,
returning `Q` (orthonormal columns) and `R` (upper triangular) such that `A = Q R`; `modified` (default true)
selects the numerically more stable modified Gram-Schmidt variant over the classical one, which matters when the
input's columns are nearly dependent. The matrix must have independent columns — dependent columns surface as NaN
in the result rather than a thrown exception.

**NonnegativeMatrixFactorisation** approximates a non-negative `m`-by-`n` matrix `V` as `W` (`m`-by-`r`) times `H`
(`r`-by-`n`), both entrywise non-negative, via the multiplicative-update algorithm — the parts-based decomposition
behind topic models and similar structure discovery. The algorithm starts from a seeded random factorisation (the
same `seed`, default 1, always reproduces the same result) and runs up to `maximumIterations` (default 500, up to
5,000) updates; `ReconstructionError` is the Frobenius norm of `V - W H` (0 for a perfect fit). Every entry of `V`
must be zero or positive, and `rank` is capped at 50 and at the smaller of the matrix's two dimensions.

## OptimisationTools

**EvaluateNamedObjective** evaluates a built-in `NumericsObjectiveFunction` (including `Quadratic` via
`quadraticMatrix`/`linearTerms`/`constantTerm`) at each row of a `points` matrix (one point per row, all rows the
same number of variables, up to 50 variables and 10,000 points), returning one value per point — the tool for
inspecting a landscape or confirming the value at a candidate optimum before or after running a minimiser.

**MinimiseWithLimitedMemoryBfgs** minimises a built-in smooth objective from a `start` point with limited-memory
BFGS, a fast general-purpose quasi-Newton method for smooth functions of many variables that uses an analytic
gradient where one of the eleven built-in objectives has one, falling back to a 5-point finite-difference gradient
otherwise (`GradientFor` in the shared code). `GradientNorm` in the result is the size of the gradient at the
reported solution (near zero at a true minimum); the description is explicit that this, like every minimiser in this
group, performs local search only — on a multimodal objective such as Rastrigin, the reported minimum is whichever
one is nearest the start, not necessarily the global minimum. Up to 50 variables, 100,000 iterations, and a 20
second wall-clock cap enforced via a `CancellationTokenSource`.

**MinimiseWithBoundedBfgs** is the same limited-memory BFGS method with simple per-variable lower/upper box
constraints (`lowerBounds`/`upperBounds`, either omittable for unbounded on that side; a very large negative number
should stand in for negative infinity rather than an actual infinite value), via Accord's bound-constrained
`BoundedBroydenFletcherGoldfarbShanno` (L-BFGS-B). The `start` point must already lie inside the bounds, and the
description notes that `GradientNorm` here is the *un*projected gradient, so it can legitimately stay non-zero when
the true optimum sits on a bound — a zero `GradientNorm` is not the right convergence signal for this variant the
way it is for the unconstrained ones.

**MinimiseWithConjugateGradient** minimises a built-in objective with the nonlinear conjugate gradient method, which
needs the gradient but far less memory than BFGS and so suits larger problems; `variant` selects the direction-update
formula — `FletcherReeves`, `PolakRibiere` or `PositivePolakRibiere` (default, and described as the most robust of
the three).

**MinimiseWithGradientDescent** minimises with plain fixed-step-size gradient descent: repeatedly step against the
gradient, scaled by `learningRate` (default 0.01, must be in `(0, 10]`). It is the simplest and, on curved valleys
such as Rosenbrock, the slowest method in the group; a learning rate that is too large diverges (`Converged` false
with a very large `Value`) while one too small barely moves within the iteration budget.

**MinimiseWithNelderMead** minimises with the Nelder-Mead simplex method, which uses only function values (no
gradient) and so tolerates rough or noisy objective surfaces at the cost of slow convergence in higher dimensions.
Optional `lowerBounds`/`upperBounds` keep the simplex inside a box, and `initialStep` (default 0.5) sets the starting
simplex size — the description recommends roughly the scale of the expected solution. Evaluations are capped at
200,000 and wall-clock time at 20 seconds.

**MinimiseWithSubplex** runs the Subplex method — Nelder-Mead applied repeatedly to rotating subspaces of the
variables — which the description states is markedly better than plain Nelder-Mead once there are many variables
(10 to 50), still without needing a gradient. Parameters and caps mirror `MinimiseWithNelderMead`.

**CompareUnconstrainedOptimisers** runs all five unconstrained methods (limited-memory BFGS, conjugate gradient,
gradient descent, Nelder-Mead, Subplex) on the same built-in objective and `start` point (up to 20 variables, tighter
than the 50-variable cap on the individual minimisers) and returns one `NumericsMethodComparisonEntry` row per
method with its solution, value, convergence flag and status. A method that throws is caught and reported in that
row's `Error` field with `Value` set to NaN rather than aborting the whole call, so agreement or disagreement across
methods — good evidence either way about whether a true minimum was found — can be read off a single response even
if one method fails outright.

**SolveQuadraticProgram** solves the convex quadratic programme `minimise 0.5 x'Qx + c'x` subject to linear
constraints (`constraintMatrix` rows each paired with a `constraintRelations` entry — `GreaterThanOrEqual`,
`LessThanOrEqual` or `Equal`, defaulting to all `GreaterThanOrEqual` — and a `constraintValues` right-hand side),
using the exact Goldfarb-Idnani dual active-set method. `quadraticMatrix` must be symmetric positive definite (the
strictly convex case); a non-positive-definite `Q` surfaces as `Status = NonPositiveDefinite` and infeasible
constraints as `NoPossibleSolution`, both without an exception. `LagrangeMultipliers` has one entry per constraint
row (zero for an inactive constraint), revealing which constraints are actually binding at the solution. Up to 50
variables and 100 constraints; the description names portfolio weights under a budget constraint and bounded least
squares as typical uses.

**MinimiseWithLinearConstraintsCobyla** minimises a built-in (possibly non-quadratic) objective subject to the same
kind of linear constraints as `SolveQuadraticProgram`, using COBYLA, a derivative-free method that builds linear
approximations inside a trust region that shrinks from `initialStep` (default 0.5) down toward `precision` (default
1e-8). `MaximumConstraintViolation` in the result should be close to zero for a genuinely feasible answer; omitting
all constraint arguments reduces it to an unconstrained COBYLA search. Up to 50 variables, 100 constraints, 100,000
evaluations.

**SolveLinearProgram** solves a linear programme — maximise or minimise `c'x` subject to linear constraints and a
uniform per-variable `lowerBound` (default 0, the usual non-negativity assumption) and optional uniform
`upperBound` — by building the bound constraints as extra rows and delegating to the same COBYLA machinery as
`MinimiseWithLinearConstraintsCobyla`. Because COBYLA is a general derivative-free solver rather than a dedicated
simplex or interior-point LP method, the description is explicit that the answer is only accurate to roughly 1e-6,
not exact, and that an unbounded problem should be read off as `Converged = false` rather than assumed caught by any
special-cased detection. Default constraint relation when none is given is `LessThanOrEqual` (the opposite default
from `SolveQuadraticProgram`'s `GreaterThanOrEqual`), which matters if the two tools' conventions are mixed up.

**FitNamedCurveLeastSquares** fits the parameters of a built-in one-variable function family (the same twelve
families as `EvaluateNamedFunction`/`NumericsText.Function`) to `(x, y)` data by non-linear least squares, choosing
between Levenberg-Marquardt (default) and Gauss-Newton via `method`, using *exact* analytic parameter derivatives
from `NumericsFunctions.PartialDerivatives` rather than finite differences. `initialParameters` must have exactly
the number of values the chosen family expects (2 to 4 for the fixed families, any length up to 40 for `Polynomial`,
matching one-for-one with its coefficients) — the call is rejected up front if the count is wrong, or if there are
fewer data points than parameters. Because the underlying families are non-linear, the result genuinely depends on
the starting guess; the description recommends trying several starts. `StandardErrors` gives the approximate
standard error of each fitted parameter, and `RootMeanSquaredError` expresses the fit error in the same units as y.

**SolveAssignmentProblemMunkres** solves the assignment problem — one row (worker) matched to at most one column
(task) minimising (or, with `maximise` true, maximising) total cost — with the Hungarian/Munkres algorithm, after
padding a non-square `costMatrix` to square with zero-cost dummy entries internally. `ColumnForRow[i]` is the task
assigned to worker `i`, or `-1` for a worker left unassigned when there are more rows than columns (confirmed by the
test suite: a 3-worker, 2-task matrix assigns only 2 and reports `-1` for the third); `TotalCost` sums only the real,
assigned entries of the original (unpadded) cost matrix, never the dummy padding. Costs must be finite and the
matrix is capped at 300 by 300.

## SpecialFunctionTools

**GammaFunction** evaluates the gamma function, the continuous extension of the factorial (`gamma(n) = (n-1)!` for
positive integers), at each supplied value. It is undefined at zero and the negative integers, where the tool
catches Accord's `ArithmeticException` and reports NaN there, or positive infinity for a positive argument large
enough to overflow (above roughly 171); `LogGammaFunction` is the tool for arguments that large.

**LogGammaFunction** evaluates the natural log of the gamma function without ever overflowing, so it remains usable
for arguments where the gamma function itself would already be infinite; it underlies log-factorials, log-binomials
and most likelihood calculations. The argument must be positive — non-positive values give NaN or infinity rather
than being rejected outright.

**DigammaFunction** evaluates the digamma function, the derivative of log-gamma (`psi`), which appears in maximum
likelihood estimation for gamma and beta distributions; `digamma(1)` equals minus the Euler-Mascheroni constant,
about `-0.5772`. Non-positive integers give NaN or infinity.

**TrigammaFunction** evaluates the trigamma function, the second derivative of log-gamma (equivalently the
derivative of digamma), giving the curvature of gamma/beta likelihoods; `trigamma(1)` equals `pi^2/6`, about
`1.645`.

**MultivariateGammaFunction** evaluates the multivariate gamma function of a given `dimension` p (1 to 1,000), which
normalises the Wishart and inverse-Wishart distributions (`p = 1` reduces to the ordinary gamma function); `x` must
exceed `(p - 1) / 2` for each evaluated value, and `logScale` (default false) returns the natural log instead, to
avoid overflow for larger dimensions.

**RegularisedLowerIncompleteGamma** evaluates `P(a, x)`, the probability that a gamma random variable with shape `a`
and unit scale is at most `x` — the building block behind chi-square and Poisson tail probabilities. Results run
over `[0, 1]`; shape `a` must be positive, and a negative `x` is not rejected but simply returns the library's
fallback value of 1.

**RegularisedUpperIncompleteGamma** evaluates `Q(a, x) = 1 - P(a, x)`, the gamma survival function; with `a = k/2`
and `x = statistic/2` this is exactly the chi-square p-value, so it is the tool behind chi-square significance tests
built elsewhere in the tool layer.

**InverseRegularisedIncompleteGamma** inverts either the lower (`upper` false, default) or upper (`upper` true)
regularised incomplete gamma function for a given probability, i.e. the gamma/chi-square quantile function —
critical values. Probabilities must lie strictly inside `(0, 1)` and shape `a` must be positive.

**BetaFunction** evaluates `B(a, b) = gamma(a) gamma(b) / gamma(a+b)` for pairs of positive parameters given as two
equal-length arrays — the normalising constant of the beta distribution; `logScale` (default false) returns the
natural log instead, which is numerically safer for large parameters.

**MultinomialBetaFunction** evaluates the multinomial (Dirichlet) beta function of a positive concentration-
parameter vector `alpha` — the product of `gamma(alpha_i)` divided by `gamma` of their sum, the Dirichlet
distribution's normalising constant — which reduces to the ordinary `BetaFunction` for a 2-element `alpha`. Very
long `alpha` vectors underflow to zero (capped at 1,000 entries regardless).

**RegularisedIncompleteBeta** evaluates `I_x(a, b)`, the beta distribution's CDF, which underlies the Student t, F
and binomial tail probabilities computed elsewhere in the tool layer; `x` must lie in `[0, 1]` and both shape
parameters `a`, `b` must be positive.

**InverseRegularisedIncompleteBeta** inverts `I_x(a, b)` for a given probability — the beta distribution's quantile
function, and so also the route to Student t and F critical values; probabilities must lie in `[0, 1]`.

**ErrorFunction** evaluates the error-function family selected by `kind`: `Erf` (default), its complement `Erfc`
(more accurate than computing `1 - Erf` directly in the upper tail), `InverseErf` (domain `(-1, 1)`) and
`InverseErfc` (domain `(0, 2)`), related to the normal CDF by `erf(x) = 2*Phi(x*sqrt(2)) - 1`. Out-of-domain inverse
arguments give NaN or infinity rather than throwing.

**StandardNormalFunction** evaluates one of eight functions of the standard normal distribution selected by `kind`:
`Cdf` (default), `LogCdf`, `Survival` (`1 - Cdf`, accurate in the far tail), `Quantile` (inverse CDF, for
probabilities strictly in `(0, 1)`; 0 or 1 map to negative/positive infinity), `Density`, `LogDensity`, and the
high-accuracy `HighAccuracyCdf`/`HighAccuracySurvival` variants for extreme z-scores. The description notes a
two-sided p-value is twice the `Survival` of the absolute z-score — the composition most hypothesis-test tools
elsewhere in the catalogue build on.

**BivariateNormalCdf** evaluates `P(X <= x, Y <= y)` for two standard normal variables with correlation `rho`, for
pairs supplied as two equal-length arrays; `rho` must be strictly inside `(-1, 1)` (a perfect correlation collapses
to a one-dimensional problem and is rejected), and with `rho = 0` the result equals the product of the two marginal
CDFs as a sanity check.

**OwensTFunction** evaluates Owen's T function `T(h, a)` — the integral of `exp(-h^2(1+x^2)/2)/(1+x^2)` from 0 to
`a`, divided by `2*pi` — needed for the skew-normal distribution and for certain bivariate normal probability
calculations, for pairs supplied as two equal-length arrays; `T(h, 0) = 0` and `T(0, 1) = 1/8` are given as reference
values.

**BesselFunction** evaluates integer-order Bessel functions selected by `kind`: `FirstKindJ` (default, oscillating,
solutions of the wave equation on a cylinder), `SecondKindY` (singular at `x = 0`, so requires `x > 0`), or
`ModifiedFirstKindI` (growing, used in the von Mises and Rice distributions); integer `order` is capped at 0 to 100,
and the description states results are accurate to roughly seven significant digits.

**FactorialFunction** returns `n!` for each non-negative integer `n` (or its natural log when `logScale` is true),
rejecting any value that is negative, non-integral, or exceeds the representable range — 170 for the plain factorial
(which overflows a double above that), or 1,000,000,000 when `logScale` is requested, since the log stays finite far
beyond where the factorial itself would not.

**BinomialCoefficient** returns `n choose k` for each pair of whole numbers from two equal-length arrays (or its
natural log when `logScale` is true, avoiding overflow for large `n`); `k > n` for a given pair returns exactly 0
(or negative infinity in log scale) rather than throwing, and both `n` and `k` are capped at 1,000,000 with up to
200,000 pairs per call.

**LogSumExp** computes `ln(sum(exp(v_i)))` in one numerically stable pass — the standard trick for adding
probabilities held on the log scale, normalising log-likelihoods, and building softmax — remaining accurate even for
inputs around `+1000` or `-1000` where naive exponentiation would overflow or underflow. An empty array is rejected,
a single value returns itself unchanged, and entries of negative infinity contribute nothing to the sum.

**SoftmaxProbabilities** converts a `scores` array into probabilities (positive, summing to 1) via the numerically
stable softmax `exp(v_i) / sum(exp(v_j))`, used to turn model scores or log-odds into a categorical distribution;
adding the same constant to every score leaves the result unchanged, which is the usual softmax shift-invariance
sanity check.

**SigmoidAndLogit** applies either the logistic function (`Sigmoid`, default — maps any real number to a
probability in `(0, 1)`, with `sigmoid(0) = 0.5`) or its inverse (`Logit` — maps a probability back to log-odds),
selected by `direction`, via Accord's `LogitLinkFunction`. `Logit` of exactly 0 or 1 maps to negative/positive
infinity rather than throwing.

**StableLogExpFunctions** evaluates four expressions that lose precision computed naively, selected by `kind`:
`Log1P` (default, `ln(1+x)`, exact for tiny `x`), `Expm1` (`exp(x)-1`, exact for tiny `x`), `Log1M` (`ln(1-x)`), and
`Softplus` (`ln(1+exp(x))` without overflow for large `x`). Arguments outside a given function's domain (for example
`x <= -1` for `Log1P`) return NaN rather than throwing.

**LogAddAndSubtractPairs** adds or subtracts, on the log scale, pairs of values stored as natural logs: `LogSum`
(default) computes `ln(exp(a)+exp(b))` and `LogDifference` computes `ln(exp(a)-exp(b))`, which requires `a > b` for
the subtraction to be the log of a positive number. Used to combine or offset tiny probabilities without underflow;
the two input arrays must have equal length.

**ReciprocalAndInverseHyperbolicFunctions** evaluates fifteen trigonometric and hyperbolic functions missing from a
basic calculator, selected by `kind`: secant, cosecant, cotangent and their inverses; hyperbolic secant, cosecant and
cotangent; and the six inverse hyperbolic functions (sine, cosine, tangent, secant, cosecant, cotangent). Angles are
in radians throughout, and an argument outside a given function's domain (for example `InverseHyperbolicCosine`
below 1) returns NaN or infinity rather than throwing.

**StirlingGammaApproximation** approximates the gamma function using Stirling's asymptotic formula, accurate to a
fraction of a percent around an argument of 10 and better for larger arguments — a cheap sanity check against
`GammaFunction`/`LogGammaFunction` rather than a replacement for them; the description explicitly warns against
using it below roughly 5, where the approximation has not yet converged.

**GaussianBellFunction** evaluates the zero-mean Gaussian bell curve `exp(-x^2/(2*variance)) / sqrt(2*pi*variance)`
at each `x` (the normal density with a chosen `variance`, positive and required), or, when a parallel `y` array is
also supplied, the two-dimensional bell `exp(-(x^2+y^2)/(2*variance)) / (2*pi*variance)` — used to build smoothing
weights or spatial kernels.

**GaussianKernelWeights** returns the weights of a one-dimensional Gaussian smoothing kernel, sampling the bell
curve at integer offsets `-r` to `r` where `r` is half the (required-odd) `size` (3 to 101), so the centre weight is
always the largest; the weights are **not** rescaled to sum to 1, so a caller wanting a weighted average must divide
by their sum before use. Intended to be paired with `NumericsConvolveVectors` for smoothing a series.

**TaylorSeriesApproximation** approximates one of five functions (`Sine`, `Cosine`, `HyperbolicSine`,
`HyperbolicCosine`, `Exponential`) by summing a fixed number of Taylor series `terms` (default 10, 1 to 200) rather
than calling the closed-form implementation, so a caller can observe how quickly the series converges; more terms
give higher accuracy, and convergence is explicitly noted as slower for larger argument magnitudes.

**BSplineBasisFunction** evaluates the centred cardinal B-spline basis function of a given integer `order` (0 to
20) at each `x`: order 0 is a box function, order 1 a triangle, order 3 the familiar cubic B-spline; the function is
exactly zero outside `(-(order+1)/2, (order+1)/2)`. Used to build smooth basis expansions for curve fitting or
density smoothing.

**StableHypotenuse** computes `sqrt(a^2 + b^2)` for pairs of numbers without the overflow or underflow that squaring
very large or very small values directly would cause, even around magnitudes of `1e200` — the stable route to vector
lengths and complex-number magnitudes; the two input arrays must have equal length.

**PlanarAngle** returns the full-circle angle in `[0, 2*pi)` of the point `(x, y)` measured anticlockwise from the
positive x-axis, for each pair of coordinates from two equal-length arrays (a four-quadrant arctangent); the origin
`(0, 0)` returns 0 by convention rather than being undefined.
