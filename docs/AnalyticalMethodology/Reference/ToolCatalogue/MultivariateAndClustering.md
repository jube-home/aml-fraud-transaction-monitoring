---
layout: default
title: Multivariate and Clustering Tool Catalogue
nav_order: 13
parent: Reference
grand_parent: Analytical Methodology
---

# Multivariate and clustering tool catalogue

The product's semantic tool layer for grouping rows, comparing cases and reducing dimensionality:
`Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering` carries 52 tools across six classes, covering
clustering algorithms, the measures used to evaluate and use a clustering, discriminant analysis and multivariate
outlier detection, distance and kernel functions (including string and set similarity), nearest neighbour search and
association rule mining, and principal/independent component analysis. AskJooby's validator agent (Jadek) only
confirms a claim against prose indexed from `docs/`, and none of these 52 tool names appears anywhere else in the
documentation tree, so this page is the only place a claim about what one of them does or returns can be confirmed.

## Clustering algorithms

These eleven tools, all on `ClusteringTools`, partition rows into groups by different rules. Every one of them
accepts rows as `double[][]` (or, for k-modes, `int[][]` category codes) and, unless noted, standardises each column
to zero mean and unit variance before clustering when `standardise` is true (the default), so that a column measured
in large units does not dominate the distance calculation; centroids and cluster profiles are always reported back in
the original units regardless. A shared result shape underlies most of them: a cluster label per row (0 to k minus
1), the centroids in original units, a profile per cluster (size, share of rows, per-column mean and standard
deviation, within-cluster sum of squares, and mean/maximum distance of members to the centroid), the total inertia
(summed squared distance to centroids, which always falls as k rises), and the mean silhouette score (between -1 and
1, `NaN` above 5,000 rows or with a single cluster, because the silhouette needs every pairwise distance). A cluster
left empty by a run has size 0 and `NaN` centroid values.

**"RunKMeansClustering"** runs ordinary k-means: starting centres are chosen either by k-means++ (which spreads them
apart) or `Uniform` (random rows), each row is assigned to its nearest centre under the chosen distance (Euclidean by
default; Manhattan, Chebyshev and Cosine are common alternatives), and centres are re-estimated as cluster means until
assignments stop changing or `maximumIterations` (default 100, capped at 1,000) is reached. It is bounded to 100,000
rows and 200 clusters. `Converged` is false when the iteration cap was hit before assignments stabilised. Because the
result depends on the starting points, the `seed` parameter (default 1) should be varied and compared across runs
before a solution is trusted.

**"RunMiniBatchKMeansClustering"** is a faster k-means variant for large row counts: instead of updating centres from
every row each iteration it updates from a random `batchSize` (default 100, capped at the row count) sample per
iteration. Results are close to, but not identical with, ordinary k-means, and depend on both the seed and the batch
size. Bounds match plain k-means (100,000 rows, 200 clusters, 1,000 iterations).

**"RunBinarySplitClustering"** builds k clusters by repeatedly splitting the current set of clusters in two — the
splitting stage of the LBG vector quantisation algorithm. Given the same seed it is deterministic, and because it
grows by splitting it produces a hierarchy-like nested partition as k increases. Bounds and return shape match plain
k-means.

**"RunBalancedKMeansClustering"** constrains cluster assignment so that no cluster absorbs a disproportionate share of
the rows, trading some tightness (inertia is usually higher than plain k-means) for roughly equal cluster sizes. It
is useful for splitting a portfolio into evenly sized review workloads. Because the constrained assignment step is
quadratic in the row count it is bounded to 5,000 rows rather than 100,000, keeping the 200-cluster and 1,000-iteration
caps.

**"RunKMedoidsClustering"** clusters around medoids — actual rows of the data chosen as cluster centres — rather than
computed means, so each cluster has a real, explainable representative case, and the method tolerates outliers and
non-Euclidean distances (the default metric is Manhattan rather than Euclidean) better than k-means. Its result
differs from the shared shape: alongside the label per row it returns the row number and values of each medoid, a
profile per cluster, the total distance of rows to their medoids under the chosen metric (lower is tighter), and the
silhouette under that same metric (`NaN` for a single cluster). `seeding` can be `PamBuild` (the default, a greedy
deterministic build), `KMeansPlusPlus` or `Uniform`; only the latter two consume the random seed. Because the
assignment cost grows with the square of the row count, it is capped at 5,000 rows.

**"RunKModesClustering"** clusters rows of integer category codes — merchant category, channel, country and other
attributes that should be encoded to integer codes before calling it — using k-modes: each cluster is represented by
its mode (the most frequent category per column) and rows are assigned by Hamming distance (the count of mismatched
columns) to the nearest mode. It returns the label per row, each cluster's mode, cluster sizes, the total mismatch
count across all rows (lower is tighter) and whether the run converged. Bounded to 100,000 rows, 200 clusters, 500
columns and 1,000 iterations.

**"RunMeanShiftClustering"** finds clusters as the peaks (modes) of the data's density rather than choosing a cluster
count up front: every row climbs uphill through a smoothed density estimate until it settles near a peak, and rows
that reach the same peak form a cluster, so the number of clusters is discovered, not chosen. The only setting that
really matters is `bandwidth` (the smoothing radius, default 1): small values yield many small clusters, large values
merge everything; with `standardise` true the bandwidth is in standard deviations, making 0.5–2 a sensible search
range. The smoothing `kernel` can be `Gaussian` (default), `Uniform` (a flat window) or `Epanechnikov` (a parabolic
window). It returns the label per row, the mode found for each cluster, cluster sizes and a profile per cluster.
Because every row is compared with every other row, it is bounded to 5,000 rows and 1,000 iterations.

**"RunGaussianMixtureClustering"** fits a Gaussian mixture by expectation maximisation, modelling the data as a
weighted blend of `components` bell-shaped groups, each with its own mean and covariance. Unlike k-means this gives
soft clusters (a probability of membership per component rather than a hard boundary) and can represent elongated or
differently sized groups. It returns, per row, the most likely component and (optionally, via `returnResponsibilities`,
default true) the full probability vector across components, which sums to 1; per component the weight (share of the
data), mean and covariance matrix; and model-selection statistics — the log-likelihood (higher fits better), the
parameter count, AIC and BIC (lower is better for both; BIC's stronger complexity penalty makes it the usual choice
for picking a component count). `covariance` can be `Full` (a complete covariance matrix per component) or
`Diagonal` (variances only, far cheaper with many columns). `regularization` (default 1e-6) is added to the
covariance diagonal to stop a component from collapsing onto a tiny or exactly repeated cluster. Bounded to 50,000
rows, 100 columns, 50 components and 1,000 iterations, and needs at least twice as many rows as components.

**"SelectGaussianMixtureComponentsByBic"** answers the model-selection question that `RunGaussianMixtureClustering`
leaves open: it fits mixtures for every component count from `minComponents` to `maxComponents` (at most 15 values
tried) and tabulates the log-likelihood, parameter count, AIC and BIC, and convergence flag for each, then reports
`BestComponentsByBic` and `BestComponentsByAic`. BIC's stronger penalty on extra components usually gives the more
conservative answer. It is a natural tool to run before `RunGaussianMixtureClustering` to choose `components`, or to
estimate how many distinct populations (customer segments, transaction types) likely exist in a data set.

**"DetectAnomaliesWithGaussianMixture"** uses a Gaussian mixture as an anomaly detector: it fits the mixture to
`referenceData` only, then scores `newData` (or, if omitted, the reference rows themselves) by log probability
density under that mixture — lower density means more unusual. The flagging `threshold` is the log density below
which the lowest `contamination` share (default 0.01, i.e. 1 percent) of the *reference* rows themselves fall, so the
contamination parameter calibrates the cutoff against the reference population's own tail rather than an arbitrary
constant. Because a mixture can represent several legitimate sub-populations at once, it can flag a row that fits
none of several known-good groups — something a single-centre Mahalanobis distance cannot express. Bounded to 50,000
reference rows, 100 columns and 50 components.

**"SelectKMeansPlusPlusSeeds"** exposes the k-means++ seeding rule as a standalone tool: it draws `seeds` rows, the
first uniformly at random and each subsequent one with probability proportional to its squared distance from the
nearest already-chosen row, so the chosen rows are spread apart rather than clustered together. It returns the row
numbers and values of the chosen rows. Beyond initialising other clustering methods, this is useful on its own for
picking a diverse set of representative cases for manual review, since the same seed always reproduces the same
selection. Bounded to 100,000 rows and 200 seeds.

## Evaluating and using a clustering

These nine tools, all on `ClusterEvaluationTools`, take an existing labelling — from any of the clustering tools
above, or from elsewhere — as `double[][] data` plus `int[] labels` (any integers; rows sharing a label form a
cluster) and answer questions about its quality, its meaning, or how to apply it to new data. `labels` is validated
and remapped to a contiguous 0-based range internally, so arbitrary label values are accepted.

**"ComputeSilhouetteScores"** computes, for every row, the standard silhouette: `a` is its mean distance to the other
rows of its own cluster, `b` is its mean distance to the rows of the nearest other cluster, and the score is
`(b − a) / max(a, b)`, bounded between -1 and 1. A score near 1 means the row sits firmly inside its cluster, near 0
means it sits on a border between two clusters, and negative means it likely belongs to a different cluster than the
one it was assigned to; a row that is alone in its own cluster scores 0 by convention. It returns every row's score,
each cluster's mean score, and the overall mean — the single number usually quoted as cluster quality, where above
roughly 0.5 indicates clear structure and below roughly 0.25 indicates weak structure. Because every pair of rows is
compared, it needs at least 2 clusters and is capped at 5,000 rows.

**"ComputeDaviesBouldinIndex"** computes the Davies-Bouldin index: for each cluster it finds the most similar other
cluster (similarity being the sum of the two clusters' mean distances to their own centres, divided by the distance
between the two centres) and averages those worst-case ratios across clusters. Lower is better, 0 is the unreachable
ideal of infinitely tight and far-apart clusters, and values above roughly 1 indicate real overlap. It always uses
Euclidean distance to the cluster means regardless of what distance produced the labelling, needs at least 2
clusters, and is capped at 100,000 rows. It is only meaningful for comparing different clusterings of the *same*
data, never across data sets.

**"ComputeCalinskiHarabaszIndex"** computes the Calinski-Harabasz index (the variance ratio criterion): the
between-cluster sum of squares divided by (k − 1), over the within-cluster sum of squares divided by (n − k). Higher
is better, growing as clusters become more compact and more separated. It is infinite in the degenerate case where
every row sits exactly on its centroid, and `NaN` when there is only one cluster or as many clusters as rows. Like
Davies-Bouldin, it is intended for comparing different values of k on the same data set, not across data sets, and
needs at least 2 clusters and at most 100,000 rows.

**"EvaluateClusteringQuality"** bundles the standard internal quality measures into one call for a clustering
produced by any method, including one made outside these tools: inertia (within-cluster sum of squared distances to
the means, lower is tighter), the between-cluster sum of squares, the mean silhouette (`NaN` above 5,000 rows),
Davies-Bouldin (lower is better) and Calinski-Harabasz (higher is better). It is the tool to reach for to compare
candidate clusterings of the same data without calling each measure separately. Needs at least 2 clusters and at
most 100,000 rows; the `distance` parameter only affects the silhouette, since the other three measures are always
Euclidean.

**"ComputeKMeansElbowTable"** helps choose a cluster count by running k-means for every k from `minClusters` to
`maxClusters` (at most 30 values, using the same seed throughout for comparability) and tabulating, per k, the
inertia (which always falls as k rises), mean silhouette, Davies-Bouldin and Calinski-Harabasz. `ElbowClusters` is
the k at the sharpest bend in the inertia curve (the point furthest below the straight line joining the first and
last points of the curve); `BestClustersBySilhouette`, `...ByDaviesBouldin` and `...ByCalinskiHarabasz` name the k
each individual measure prefers. These measures routinely disagree with each other, so the guidance is to look for a
k that several of them support rather than trusting any single one. Measures are computed on standardised data when
`standardise` is true (the default). Bounded to 20,000 rows and 30 values of k; the silhouette column is `NaN` above
5,000 rows regardless of the overall row cap.

**"ComputeKMeansGapStatistic"** chooses a cluster count with Tibshirani's gap statistic: for each k it compares the
log of the real data's k-means within-cluster dispersion against the average of that same quantity over
`referenceSets` (default 10, capped at 20) synthetic data sets drawn uniformly at random inside the bounding box of
the real data — so a large gap means the real data clusters far more tightly than random noise of the same shape
would. The output table carries, per k, the observed log dispersion, the expected log dispersion under the reference,
the gap, and its standard error; `BestClusters` applies Tibshirani's one-standard-error rule (the smallest k whose
gap is at least the next k's gap minus that next k's standard error), falling back to the largest k tried if no k
qualifies. The reference sets are drawn using `seed`, so results repeat for a fixed seed. Because it fits many k-means
models (one real plus `referenceSets` reference fits, per candidate k), it is tightly bounded: at most 5,000 rows, 15
values of k, and 20 reference sets.

**"ProfileClusters"** describes each cluster of an existing labelling in plain numbers so it can be interpreted
without staring at raw centroids: size and share of rows, the centroid (mean per variable), the standard deviation
per variable, the within-cluster sum of squares, the mean and maximum Euclidean distance of members to the centroid,
and a ranked list of the variables that most distinguish the cluster. `DistinguishingVariables` lists, for the
`topVariables` (default 3) variables with the largest effect, the standardised difference — (cluster mean − overall
mean) / overall standard deviation — so +1.5 means the cluster averages 1.5 standard deviations above the rest of the
population on that variable; a variable that is constant across all rows has effect exactly 0 rather than dividing by
zero. It accepts labels from any clustering method, not just this tool group's own output. A cluster containing only
one row has `NaN` standard deviations, since a spread cannot be computed from a single point. Capped at 100,000 rows.

**"AssignRowsToNearestCentroid"** assigns new rows to the nearest of a supplied set of centroids — typically centroids
returned by one of the clustering tools above — under a chosen distance metric (Euclidean by default; `minkowskiOrder`,
default 3, configures the Minkowski metric). For each new row it returns the index of the nearest centroid (its
position in the supplied centroid array, 0-based), the distance to that centroid, and the distances to *every*
centroid, so that borderline rows can be spotted — a small gap between the two smallest distances flags an ambiguous
assignment. It is the caller's responsibility to keep the new rows on the same scale as the clustering: if the
original clustering standardised its input, the new rows must be standardised the same way before being passed in.
Bounded to 20,000 new rows and 200 centroids.

**"CompareClusterAssignments"** measures how much two independent clusterings of the same rows agree, independent of
how each one happens to number its clusters, using pair counting over every pair of rows. `RandIndex` is the share of
all row pairs on which the two clusterings agree (both together or both apart), between 0 and 1. `AdjustedRandIndex`
corrects the Rand index for the agreement expected by chance alone, so 1 means the two partitions are identical,
roughly 0 means no better than chance, and a negative value means worse than chance — this is the number to quote
when stability or reproducibility is the question, for example comparing a clustering across random seeds, methods,
or samples. `PairJaccard` is the share of pairs that are together in *either* clustering that are together in *both*.
Two clusterings that are each a single cluster, or each all singletons, trivially score 1 on all three measures.
Bounded between 2 and 1,000,000 rows, and `labelsA`/`labelsB` must be the same length.

## Discriminant analysis, embeddings and Mahalanobis distance

These six tools live on `DiscriminantAndEmbeddingTools` and cover supervised class separation, nonlinear embedding
for visualisation, and multivariate outlier detection by Mahalanobis distance.

**"RunLinearDiscriminantAnalysis"** finds the directions along which known classes are best separated — large spread
between class means relative to spread within each class — and returns them together with every row's coordinates in
that space. There are at most (number of classes − 1) useful directions, and `components` (0 by default, meaning
"use the maximum") selects how many to keep. It reports class sizes, prevalence, and means; each direction's
eigenvalue (bigger means more separation, with between-class scatter weighted equally per class); each direction's
share of total separation and the running cumulative total; the direction vectors themselves; the class means
projected into the new space; the projected coordinates of every row (when `returnScores` is true, the default); and
a training accuracy figure from assigning every row to its nearest projected class mean. That accuracy is measured on
the same rows used to fit the model, so it is optimistic and should be read as a measure of separability rather than
of predictive performance. It needs at least 2 classes with at least 2 rows each, more rows in total than classes,
and columns that are not perfectly collinear within any one class — a singular or perfectly separable within-class
scatter is rejected with an explanation pointing at collinear or constant columns. Capped at 20,000 rows.

**"RunKernelDiscriminantAnalysis"** is the same idea carried out in the feature space of a kernel, so that classes
separated by a curved rather than a straight boundary can still be pulled apart. It shares the kernel parameter set
used throughout this tool group (`kernel`, `kernelSigma`, `kernelGamma`, `kernelConstant`, `kernelDegree`,
`kernelAlpha`, `kernelBeta` — see the kernel reference under Distances and kernels, below) and returns eigenvalues,
separation shares, the projected coordinates of every row, the classes' projected means, and training accuracy as
above; because class means and direction vectors live in the kernel's implicit feature space rather than the
original variables, those two fields come back empty. `regularization` (default 1e-4) is added to the within-class
matrix to keep it invertible; a `SingularMatrixException` from the underlying library is caught and re-raised as an
`ArgumentException` suggesting the caller raise `regularization` (for example to 0.01) or change the kernel
parameters. Because the kernel matrix has (rows)² cells, it is capped at 1,000 rows rather than 20,000.

**"ClassifyWithLinearDiscriminant"** learns a linear discriminant model from labelled training rows and immediately
assigns each row of `newData` to whichever class's projected mean is nearest, without saving the fitted model
anywhere. It returns the predicted label per new row and, per row, one score per class in the order of `ClassLabels`
— the negative squared distance to that class's mean in the discriminant space, so a larger (less negative) score
means a better fit. It is offered as a quick, interpretable baseline classifier, or as a way to place new cases
relative to known groups without the overhead of a full model lifecycle. Training needs at least 2 classes with at
least 2 rows each, exactly as for `RunLinearDiscriminantAnalysis`.

**"DetectMultivariateOutliersMahalanobis"** flags rows that are unusual as a *combination* of variables, using the
Mahalanobis distance: the number of standard deviations a row lies from the centre once the correlations between
variables are accounted for, so a row can be flagged because two normally-correlated variables disagree with each
other even if neither variable alone looks extreme. For each row it returns the distance, the squared distance, and a
chi-square p-value with one degree of freedom per independent dimension found in the data (`Rank`); the cutoff is the
distance whose chi-square tail probability equals `significance` (default 0.001, i.e. the 99.9th percentile), and
rows beyond it are flagged. The centre and covariance are estimated from the *same* rows being tested — the classical
rather than a robust estimator — so several genuinely extreme rows can inflate the covariance estimate and mask each
other, and small samples make the chi-square approximation loose; these are real limitations to keep in mind when
reading the result. When columns are perfectly collinear the implementation falls back to a pseudo-inverse of the
covariance matrix, and `Rank` comes back lower than the column count to reflect that; a reference set with literally
no variation in any column (rank 0) is rejected outright, since distances would be undefined. Needs at least 3 rows,
capped at 20,000.

**"ScoreMahalanobisDistanceAgainstReference"** is the same Mahalanobis calculation but deliberately avoids the masking
problem above: the centre and covariance are learned from `referenceData` only, and `newData` is scored against that
fixed reference, so extreme rows in the test set can never contaminate the population they are being measured
against. This is the version to reach for when testing new transactions or customers against clean history, rather
than detecting outliers within a single pooled set. Both data sets must share the same column count; the reference
set needs at least 3 rows, and both are capped at 20,000 rows.

**"EmbedWithTsne"** embeds high-dimensional rows into 2 or 3 dimensions with t-SNE so that cluster structure can be
seen on a scatter plot: points that are near each other in the original space stay near each other in the embedding,
but distances between far-apart groups and the apparent size of a cluster in the embedding carry no meaning and
should never be read as distances or densities. `perplexity` (default 30) is roughly the number of neighbours each
point considers and must stay below one third of (rows − 1); the tool automatically reduces it to fit small samples,
and a request with fewer than 10 rows is rejected outright as too few for a meaningful embedding. `theta` (default
0.5) trades speed for accuracy in the Barnes-Hut approximation, where 0 is exact but slow and 0.5 is the usual
practical choice. The optimisation always runs 1,000 iterations from a random start controlled by `seed`, so the same
seed reproduces the same coordinates, but different seeds give different, *equally valid* layouts of the same
structure — comparisons across runs should be about the shapes found, never the raw coordinates. Bounded to 2,000
rows and 500 columns.

## Distances, kernels and string similarity

These thirteen tools live on `DistanceAndKernelTools` and expose the numeric distance/kernel machinery used
throughout this tool group as first-class, directly callable primitives, plus a matching set of tools for comparing
text and token sequences.

**"ComputeVectorDistance"** computes a single distance between two equal-length numeric vectors under one named
metric: `Euclidean`, `SquaredEuclidean`, `Manhattan`, `Chebyshev`, `Minkowski` (order set by `minkowskiOrder`, default
3; order 1 is Manhattan and order 2 is Euclidean), `Cosine` (1 minus cosine similarity), `Angular` (an angle in
radians), `Canberra`, `BrayCurtis`, `Hellinger` (intended for non-negative probability-like vectors),
`PearsonCorrelation` (1 minus correlation), and the binary family `Hamming`, `Jaccard`, `Dice`, `Matching`,
`Kulczynski`, `RogersTanimoto`, `RusselRao`, `SokalMichener`, `SokalSneath` and `Yule`, all of which treat any non-zero
value as "true". Zero always means identical vectors, and larger values always mean more different, except that for
`Cosine`, `Jaccard` and `Dice` the corresponding *similarity* is one minus the returned distance. `weights` supplies
one non-negative weight per dimension and is only consulted for the `WeightedEuclidean` and
`WeightedSquaredEuclidean` metrics. Non-finite inputs or mismatched vector lengths are rejected with an explanatory
message rather than propagating `NaN`.

**"ComputeDistanceMatrixWithinSet"** computes the full symmetric pairwise distance matrix for one data set under any
of the metrics above: cell `[i][j]` is the distance from row i to row j, the diagonal is exactly zero, and the matrix
is symmetric. It is the building block for inspecting similarity structure, feeding a clustering algorithm, or
finding near-duplicate rows. Because the result has (rows)² cells, it is capped at 2,000 rows; the summary carries the
smallest, largest, and mean off-diagonal distance.

**"ComputeDistanceMatrixBetweenSets"** is the rectangular equivalent for two different data sets: cell `[i][j]`
compares row i of `setA` with row j of `setB`, so the matrix need not be square and has no meaningful diagonal. Each
set is independently capped at 2,000 rows, and both sets must share the same column count. Use it to compare a batch
of new cases against a fixed reference population without computing the within-set distances for either side.

**"ComputeKernelValue"** evaluates a single named kernel function between two vectors, returning one similarity
number where larger means more alike (the inverse sense of a distance). The kernel family is shared across every
kernel-consuming tool in this group: `Linear`, `Polynomial` and `Sigmoid` are dot-product kernels; `Gaussian`,
`Laplacian`, `Cauchy`, `Spherical`, `Circular`, `Wave` and `ThinPlateSpline` are distance kernels controlled by
`kernelSigma`; `Hypersecant`, `SquaredSinc` and `SymmetricTriangle` use `kernelGamma`; `Power`, `Log`, `TStudent`,
`BSpline` and `Bessel` use `kernelDegree`; and `ChiSquare`, `Hellinger` and `HistogramIntersection` are intended for
non-negative, histogram-shaped data. `kernelConstant` supplies the additive constant used by the dot-product kernels
(defaulting to 0 for `Linear`, i.e. a plain dot product, and 1 for the others), `kernelAlpha` is the sigmoid slope /
Pearson omega / HistogramIntersection exponent, and `kernelBeta` is the HistogramIntersection beta exponent. This
tool is the place to sanity-check a kernel and its parameters on one pair of cases before committing to it inside
kernel PCA or kernel discriminant analysis.

**"ComputeKernelGramMatrix"** computes the full kernel (Gram) matrix for one data set: cell `[i][j]` is the kernel
similarity between row i and row j, so the matrix is symmetric and the diagonal holds each row's self-similarity.
With `normalise` true (default false) every cell is divided by the square root of the product of its two diagonal
entries — a cosine-style normalisation that forces the diagonal to exactly 1 and makes magnitudes comparable across
different kernel choices. Capped at 1,000 rows because the matrix has (rows)² cells.

**"ComputeKernelCrossMatrix"** is the rectangular cross-kernel equivalent: cell `[i][j]` compares row i of `setA` with
row j of `setB` under the chosen kernel, each set capped at 1,000 rows and both needing the same column count. Since
the matrix is not square, its `Diagonal` field in the result comes back empty rather than meaningless.

**"CompareHistogramsBhattacharyya"** measures how much two discrete distributions overlap, each given as an array of
non-negative counts or proportions over the same categories or bins in the same order; both arrays are first rescaled
to sum to 1 internally. It returns the Bhattacharyya coefficient (1 for identical distributions, 0 for no overlap
whatsoever), the Bhattacharyya distance (minus the natural log of the coefficient — 0 for identical, infinite for no
overlap), and the Hellinger distance (the square root of one minus the coefficient, bounded between 0 and 1). The
intended use is comparing a current distribution against a baseline, for example transaction amount bands this month
against last month. At least one entry in each histogram must be above zero; a mismatched bin count or a negative
entry is rejected.

**"CompareSamplesBhattacharyyaGaussian"** is the continuous, multivariate analogue: it fits a Gaussian (mean and
covariance) to each of two samples and returns the Bhattacharyya distance between the two fitted Gaussians, a single
number that grows with both the separation of the two means and the difference between the two covariances — 0 means
the fitted Gaussians coincide exactly, and for two Gaussians sharing an identity covariance it reduces to one eighth
of the squared distance between the means. It is intended to quantify drift between a baseline population and a
current one across several variables at once. Each sample needs at least 3 rows, and the two must share the same
column count.

**"ComputeStringDistance"** computes the edit distance between two strings under one of three named algorithms:
`Levenshtein` (the minimum count of single-character insertions, deletions and substitutions turning one string into
the other — the default), `NormalisedLevenshtein` (the same distance divided by the longer string's length, so 0 is
identical and 1 is completely different regardless of length), or `Hamming` (the count of differing positions,
requiring both strings be the same length). `ignoreCase` (default true) lowercases both strings before comparing.
Strings over 2,000 characters are rejected. The returned `Similarity` is always 1 minus the normalised distance,
bounded between 0 and 1, which is the natural field to threshold when fuzzy-matching names, references or account
descriptions that may carry typing errors.

**"FindClosestStringMatches"** ranks a list of `candidates` against one `query` string and returns the `top` (default
5) closest, nearest first, under the same three edit-distance algorithms as `ComputeStringDistance`. Similarity is 1
minus the distance divided by the longer of the two lengths being compared, so an exact match scores 1; `Hamming`
candidates whose length differs from the query are silently skipped rather than erroring, since Hamming distance is
undefined between strings of different lengths. Ties in distance preserve the original candidate order. Bounded to
5,000 candidates of at most 2,000 characters each, and `top` is capped at 1,000.

**"ComputeStringDistanceMatrix"** computes the full pairwise edit-distance matrix across one list of strings,
symmetric with a zero diagonal, under any of the three algorithms above; for `Hamming`, every string in the list must
share the same length. It is intended for finding near-duplicate names or references in bulk, or as a preparatory
step before clustering text values. Capped at 500 strings of at most 2,000 characters each.

**"ComputeTokenSequenceEditDistance"** computes the edit distance between two *ordered* sequences of tokens — the
sequence of actions in two user sessions, or the sequence of merchant categories in two customers' histories — as the
minimum number of single-token insertions, deletions and substitutions turning one sequence into the other, with
order mattering throughout (unlike `ComputeTokenSetSimilarity`, below). It returns the raw distance and a similarity
between 0 and 1 (1 minus the distance divided by the longer sequence's length; two empty sequences score a similarity
of 1 by convention). Each sequence is capped at 2,000 tokens, and `ignoreCase` (default true) applies to token text.

**"ComputeTokenSetSimilarity"** compares two lists of tokens — words, merchant categories, device identifiers, or any
other strings — as unordered sets, collapsing duplicates and ignoring order entirely. It returns the size of each
input set after deduplication, the intersection and union sizes, the Jaccard similarity (intersection over union), the
Dice similarity (twice the intersection over the sum of the two set sizes), and the overlap coefficient (intersection
over the smaller set size). All three similarity measures lie between 0 and 1, are 1 for identical sets, and are
defined as 1 for two empty sets by convention (rather than the 0/0 that would otherwise result). Each token list is
capped at 100,000 entries; `ignoreCase` (default true) folds case before comparing.

## Nearest neighbours and association rules

These five tools live on `NeighbourAndRuleTools`. The first four share search machinery across three index choices —
`KdTree` (the default, efficient for low-dimensional data), `VpTree` (works in any dimension, including non-Euclidean
metrics) and `BruteForce` (a plain scan, giving identical results to the indexed searches and useful mainly as a
cross-check) — and a metric choice of `Euclidean` (default), `Manhattan`, `Chebyshev` or `Minkowski` (order set by
`minkowskiOrder`, default 3). Every one of them supports a "self-query" mode, triggered by leaving `queryData` null,
in which every reference row queries against the rest of the reference set and is never returned as its own nearest
neighbour.

**"FindKNearestNeighbours"** finds, for each query row, the `neighbours` (default 5) closest reference rows and their
distances, nearest first. It is intended for finding look-alike customers, similar past cases, or near-duplicates.
Scaling is the caller's responsibility: columns on very different units should be standardised before calling, since
all four metrics treat every dimension equally. Rows tied at exactly the same distance may be returned in either
order, and which tied row is kept right at the cutoff is unspecified behaviour rather than a documented guarantee.
Bounded to 100,000 reference rows, 5,000 query rows (5,000 self-queries when `queryData` is omitted), and 1,000
neighbours per query.

**"FindNeighboursWithinRadius"** is the radius-search sibling: for each query row it finds *every* reference row
within `radius` (strictly, in the chosen metric's own units) rather than a fixed count, using a KD-tree internally
regardless of the `index` parameter's value for the paired nearest-neighbour tool. It is suited to counting or
listing cases inside a fixed similarity threshold, for example accounts within a small distance of a known suspicious
profile. `maxPerQuery` (default 100) caps the list returned per query, keeping the nearest if the radius match count
exceeds it. Bounded the same as `FindKNearestNeighbours`.

**"ClassifyByNearestNeighbours"** classifies each row of `newData` by a majority vote among its `neighbours` (default
5) nearest labelled training rows, with nothing about the model persisted anywhere. It returns the predicted label per
new row (the plurality class among the neighbours, with ties broken in favour of the closer neighbours); per row, the
share of the k neighbours voting for each class in the order of `ClassLabels` (a value of 0.6 means 6 of 10
neighbours belong to that class); and per row a distance-weighted share, where each neighbour contributes `1 / (1 +
distance)` so that nearer neighbours count for more than farther ones. It is offered as a transparent baseline
classifier, and the per-class vote shares are also useful on their own as a measure of how mixed or unambiguous a
case's neighbourhood is. Training labels can be any integers; scaling is again the caller's responsibility. Bounded
to 100,000 training rows, 5,000 rows to classify, and 1,000 neighbours.

**"DetectOutliersByNearestNeighbourDistance"** is a density-based outlier check: for every row it computes the
distance to its own k-th nearest other row (`neighbours`, default 5) and the mean distance to its k nearest, then
flags rows whose k-th-neighbour distance exceeds the upper quartile of all such distances by more than
`iqrMultiplier` (default 1.5) interquartile ranges — the familiar box-plot rule, applied to local neighbour distance
rather than to the raw variable. Isolated rows naturally have large neighbour distances and get flagged; a tight
cluster of several similar anomalies can mask itself if `neighbours` is not set larger than the group's own size, so
`neighbours` should be chosen above the size of the smallest anomalous group of interest. Scaling across columns is
the caller's responsibility. Needs at least 3 rows, and is capped at 5,000 rows and 1,000 neighbours.

**"MineAssociationRules"** finds items that frequently occur together across transactions — market-basket analysis —
using the Apriori algorithm, and turns the frequent combinations into if-then rules. Each transaction is an array of
item names (merchant categories, products, alert types, or any other categorical tag seen together for one customer
or basket); repeated items within one transaction are collapsed. It returns the frequent itemsets — item groups
present in at least `minSupport` (default 0.1, i.e. 10 percent) share of all transactions — and rules of the form
"antecedent implies consequent" carrying support (the share of transactions containing both sides), confidence (the
share of antecedent-containing transactions that also contain the consequent), lift (confidence divided by the
consequent's own support — above 1 means the two sides occur together more than chance would predict, exactly 1 means
statistical independence), and leverage (support minus the product of the two sides' individual supports). Rules are
ordered by lift, then confidence. The bounds are deliberately tight to prevent combinatorial blow-up: at most 20,000
transactions, 12 distinct items within any one transaction, 200 distinct items overall, a `minSupport` floor of 0.001,
and at most 5,000 itemsets and 1,000 rules returned (`maxItemsets` and `maxRules`, both independently settable within
those ceilings).

## Principal component and independent component analysis

These eight tools live on `ProjectionTools` and cover linear and kernel dimensionality reduction. `basis` is shared
across the PCA-family tools: `Correlation` (the default) standardises every variable before extracting components,
appropriate when variables are on different scales or units, while `Covariance` only centres the data and works
directly in the original units, appropriate when the variables already share a common scale.

**"RunPrincipalComponentAnalysis"** finds the orthogonal directions that capture the most variance in the data and
returns the eigenvalues (variance captured per component), the explained-variance ratios and their running
cumulative total, the loadings (each component's unit-length direction expressed in terms of the original variables),
and — when `returnScores` is true (the default) — every row's coordinates on the components. Scaled loadings (loading
times the square root of the eigenvalue) are also returned; under the `Correlation` basis these equal each variable's
correlation with each component, and the accompanying communalities report the share of each variable's own variance
that the returned components explain. `ComponentsForNPercent` reports how many leading components are needed to reach
given cumulative-variance thresholds, and `KaiserComponentCount` applies the Kaiser rule (components whose eigenvalue
exceeds the mean eigenvalue — equivalently, exceeds 1 under the `Correlation` basis). Every loading vector's sign is
normalised so its largest-magnitude entry is positive, removing the otherwise arbitrary sign ambiguity of PCA. Needs
at least 2 rows; constant columns are rejected outright under the `Correlation` basis, since a constant column cannot
be standardised. With more columns than rows, only `min(rows, columns)` components exist. Capped at 20,000 rows and
500 columns.

**"InterpretPrincipalComponents"** turns the raw output of PCA into words: for the leading `components` (default 3)
components it lists the `topVariables` (default 5) original variables with the largest absolute loadings on each,
plus every variable's communality. A component with large positive loadings on "amount" and "count", for instance,
reads as a volume component. `Loading` is the raw eigenvector weight (unit length per component), and
`SquaredLoadingShare` is its square — the share of that component's total direction a single variable accounts for.
Variable names default to `V0`, `V1`, and so on if `variableNames` is omitted. This is meant to be run immediately
after `RunPrincipalComponentAnalysis` to narrate what each retained component actually represents.

**"ProjectOntoPrincipalComponents"** learns principal components from `referenceData` and projects a separate
`newData` set onto them, returning one score vector per new row. New rows are centred — and, under the `Correlation`
basis, scaled — using the reference set's own means and standard deviations, so the resulting scores are directly
comparable with the reference rows' own scores rather than being computed on a different footing. This is the tool
for placing new cases into a component space that was learned from historical data. The reference set needs at least
2 rows, and both sets must share the same column count.

**"WhitenDataWithPca"** decorrelates and rescales data so the returned columns are mutually uncorrelated and each has
variance exactly 1 — the classical PCA whitening transform, used as a preprocessing step for methods that assume
roughly spherical input. Rows are projected onto the principal components and each resulting score column is divided
by the square root of its own eigenvalue. Components whose eigenvalue falls below `1e-12` of the largest eigenvalue
are silently dropped, since dividing by a near-zero eigenvalue would amplify noise rather than signal; as a result,
for strongly collinear data the number of returned columns can be smaller than the number of input variables even
when `components` requests more. `ColumnStandardDeviations` is returned specifically to let the caller confirm each
retained column really does come back with variance 1. If every column turns out to have zero variance (nothing left
to whiten), the call is rejected with an explanation rather than returning degenerate output.

**"ComputePrincipalComponentDiagnostics"** scores how unusual each row is relative to a PCA model learned from
`referenceData`, using the two standard multivariate process-control statistics. `HotellingT2` sums each retained
component's score squared and divided by its own eigenvalue — large when a row is extreme along the dominant patterns
the model has retained. `SquaredPredictionError` is the squared distance, in centred (and, under `Correlation`,
scaled) units, between a row and its reconstruction from only the retained components — large when a row breaks the
correlation pattern the model assumes, regardless of where it sits along the retained directions. `HotellingT2Cutoff`
is a chi-square critical value (a large-sample approximation) with one degree of freedom per retained component at
the chosen `significance` (default 0.01); `IsAbnormalT2` flags rows exceeding it. Test rows default to the reference
rows themselves when `testData` is omitted, which is useful for diagnosing the reference population before scoring
anything new against it.

**"ReconstructFromPrincipalComponents"** compresses rows onto the leading `components` (default 1) principal
components learned from `referenceData` and maps them straight back to the original variables, returning both the
reconstructed rows and, per row, a `SquaredError` — the squared distance between the original row and its
reconstruction in the original units. Because rows that do not follow the dominant correlation pattern reconstruct
poorly, `SquaredError` doubles directly as an interpretable anomaly score, distinct from but related to
`SquaredPredictionError` above. `RetainedVarianceRatio` reports the share of the reference set's total variance the
kept components carry, useful for judging whether the chosen `components` count is actually enough. Test rows default
to the reference rows when `testData` is omitted.

**"RunKernelPrincipalComponentAnalysis"** carries out PCA in the implicit feature space of a kernel rather than
directly on the original variables, which lets it unfold curved or clustered structure — the canonical example being
concentric rings, which a Gaussian kernel can separate but ordinary linear PCA cannot. It returns the eigenvalues of
the centred kernel matrix, their explained-variance ratios (relative to however many components the analysis can
extract, which is capped by the number of input variables), and the score of every row on the requested `components`
(default 2). Because the kernel matrix has (rows)² cells, it is capped at 1,000 rows. `kernelSigma` for a Gaussian
kernel should be chosen near the typical distance between neighbouring points in the data; as with ordinary PCA, the
sign of each component's scores is arbitrary (though repeatable for a fixed input) and carries no meaning on its own.

**"RunIndependentComponentAnalysis"** performs blind source separation: it looks for statistically independent,
non-Gaussian source signals whose linear mixture would explain the observed columns — for example, separating
overlapping underlying behaviours out of combined activity measurements. It returns the estimated source values (one
column per extracted component, for every row), the mixing matrix (how the sources combine to produce the observed
variables) and the demixing matrix (its inverse, mapping observations back to sources). `algorithm` can be `Parallel`
(the default, estimating all components together) or `Deflation` (extracting them one at a time). As with any ICA
method, the order, sign and scale of the recovered sources are arbitrary — a known, expected property rather than a
defect — so results should be read for their shapes and relationships, never their raw magnitudes or which output
column number corresponds to which real-world source. The optimisation starts from a random point controlled by
`seed` (default 1), so a fixed seed reproduces a fixed result. It needs at least as many rows as columns and at least
2 columns, and is capped at 20,000 rows.

## Coverage note

All 52 `[AdvancedAnalyticsTool]` names under
`Jube.Service/Agent/AdvancedAnalyticsTools/MultivariateAndClustering` are documented above. None of these tools'
exact, quoted names appears anywhere else under `docs/`, including in `docs/AnalyticalMethodology/OtherModelTypes/`
(which documents `KMeansClusterer`, `GaussianMixtureClusterer`, `PrincipalComponent`, `KNearestNeighboursClassifier`
and similar pages): those pages describe the standalone trained *model* feature of the same name and never cite the
literal tool names captured here, which belong to a separate semantic tool layer that an agent calls directly.
