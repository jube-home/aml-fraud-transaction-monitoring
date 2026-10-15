---
layout: default
title: Model Evaluation Tool Catalogue
nav_order: 16
parent: Reference
grand_parent: Analytical Methodology
---

# Model evaluation tool catalogue

`Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation` carries 27 tools that an AskJooby agent calls by their
literal `[AdvancedAnalyticsTool("...")]` name. Jadek, the reply-checking agent, can only confirm a claim about a
tool against this `docs/` tree's RAG index, so a tool with no entry anywhere is one Jadek can never verify. This
page and its companion `DataPreparationAndResampling.md` document every tool in the ModelEvaluation and
DataPreparationAndResampling groups for that reason. Only four files in this large directory actually carry
`[AdvancedAnalyticsTool(...)]` methods
(`ModelEvaluationTools.cs`, `ModelSelectionTools.cs`, `MulticlassAndAnomalyTools.cs`,
`MulticlassEvaluationTools.cs` and `RegressionEvaluationTools.cs` — five files once `MulticlassEvaluationTools.cs`
is counted separately from `MulticlassAndAnomalyTools.cs`); every other file in the directory (the model classes,
engines, builders and math helpers) is implementation behind those tool methods and is referenced below only where
it explains a tool's real behaviour. Every tool in the group is accounted for below: documented in full, or named
as already covered elsewhere with a citation, never silently omitted.

Every tool fits, evaluates and (with the sole exception of `"CrossValidateLogisticRegression"`, which reuses
`TrainLogisticRegression`'s own fitting code) throws away the model it builds — nothing here persists a trained
model the way the `AdvancedAnalyticsTrainingRunTools` family does. Rows are always split, or cross-validated, by a
caller-supplied `seed`, so repeating a call with the same seed and data reproduces the same split and the same
numbers.

## Already catalogued elsewhere

Six tools in `ModelEvaluationTools.cs` and `ModelSelectionTools.cs` are already documented in real depth by other
pages in this tree, each pairing the tool with a worked R procedure from the original training guide and tracing
the exact fields its result carries:

- **`"CrossValidateLogisticRegression"`** — [Logistic Regression](../../LogisticRegression.md) names it alongside
  `"TrainLogisticRegression"` as the source of the `ClassificationMetrics` confusion-matrix counts, precision,
  recall, F1, Matthews correlation and the `RocAuc`/`RocAucLower95`/`RocAucUpper95` fields, and explains when to
  reach for it (fold stability) instead of `"TrainLogisticRegression"` (the coefficient table and model snapshot).
- **`"EvaluateBoostedTrees"`** and **`"EvaluateDecisionTree"`** —
  [Decision Trees and Boosting](../../DecisionTreesAndBoosting.md) documents both in detail: `"EvaluateDecisionTree"`
  as the source of a confusion table, ROC AUC, average precision, log loss, Brier score, calibration, and the
  `Rules`/`TreeText` fields of its `ModelDescription`; `"EvaluateBoostedTrees"` as the direct match for C5-style
  reweighting boosting (`algorithm = AdaBoost`) and gradient boosting (`algorithm = GradientBoosting`, the default).
- **`"EvaluateNaiveBayes"`** — [Naive Bayes](../../NaiveBayes.md) documents `kind: Categorical` as the quantile-bin,
  Laplace-smoothed equivalent of the guide's own R procedure, with `regularisation` named as exactly the Laplace
  add-one count.
- **`"ComputeLearningCurve"`** and **`"ComputeValidationCurve"`** — `DecisionTreesAndBoosting.md` names both as the
  product's answer to "how many boosting rounds are enough" (the `gbm.perf` question), run against `TreeCount`
  before committing to a persisted training run.

Everything below is a tool not covered, in full or in part, anywhere else in `docs/`.

## Single-family binary classifier evaluation (`ModelEvaluationTools.cs`)

The four tools below share one scheme, repeated nearly verbatim in each tool's own description: rows are split by
`seed` into stratified train, validation and test sets, or into `crossValidationFolds` (2 to 10) stratified
cross-validation folds whose held-out predictions are pooled. Missing-value filling and scaling are learned from
training rows only; any `classImbalance` handling (`None`, `BalancedWeights`, `Oversample` or `Undersample`) touches
training rows only; Platt probability calibration, when `calibrate` is true (the default), is fitted on the
validation rows only; and every reported metric — ROC AUC (with a 95% interval), average precision, log loss, Brier
score, a confusion table at `decisionThreshold` (or, left empty, the threshold that maximises validation F1), and
calibration slope/intercept — comes from the untouched held-out rows. Permutation importance, when
`permutationRepeats` is above 0 (default 3, up to 10), is the drop in held-out ROC AUC when one variable is
shuffled. The shared bound across the family is at most 20,000 rows, 200 predictor columns, 500 trees and 1000
training epochs (kernel support vector machines are limited to 4000 rows), and nothing is saved. The return shape
for all four is `ClassifierEvaluationResult`: family, scheme, seed, the settings used, row/feature/positive counts,
`Train`/`Validation`/`Test` `ClassificationMetrics`, per-fold results and summary, a `CalibrationSummary`, a
hyper-parameter `Sweep` table, `Importance` rows, a `ModelDescription` and `Warnings`.

**`"EvaluateKNearestNeighbours"`** fits a k-nearest-neighbours classifier over a sweep of `neighbourCounts` (up to
16 values between 1 and 500; empty tries `1, 3, 5, 9, 15, 25, 51`) and evaluates the k chosen on the validation rows
only, so the test metrics stay honest even though a training ROC AUC of exactly 1 at `k=1` is both expected and
meaningless. Each row's score is the share of positives among its k nearest training rows on standardised
predictors, so the method needs no training step but is sensitive to irrelevant or badly scaled variables and slow
on large tables. The `Sweep` field in the result reports the training and validation ROC AUC for every k tried.

**`"EvaluateSupportVectorMachine"`** fits a support vector machine over a sweep of the complexity `C` (up to 16
values between 0.0001 and 10000; empty tries `0.1, 1, 10`), with `kernel` set to `Linear` (the default, scales to
20,000 rows) or one of `Gaussian`, `Polynomial`, `Sigmoid` or `Laplacian` (curved boundaries, but limited to 4000
rows because kernel training is quadratic in row count). `kernelWidth` (gamma) defaults, when left empty, to `1`
divided by the number of variables. Predictors are standardised on the training rows; a large `C` fits the training
rows tightly and risks overfitting, a small `C` smooths. The raw margin is converted to a probability by Platt
scaling on the validation rows, and `C` is chosen there too. The `Sweep` field reports training and validation ROC
AUC for every `C` tried, and the result additionally reports the support vector count.

**`"EvaluateRandomForest"`** fits either `mode: RandomForest` (the default — every split considers only a random
subset of the variables, sized by `featureFraction`, 0 meaning the square root of the variable count) or
`mode: Bagging` (every split sees every variable) over `treeCount` trees (1 to 500, default 100), each trained on a
bootstrap sample sized by `sampleFraction` (default 1, between 0.05 and 1). The forest's score is the average leaf
probability across trees, calibrated afterwards on the validation rows; the description calls forests a robust,
low-tuning default for tabular fraud and AML data with interactions, and notes that a large training-to-test ROC AUC
gap is normal for forests and should not alone be read as overfitting — the test metrics are what to judge on. The
result additionally reports the out-of-bag ROC AUC alongside the usual held-out metrics.

**`"EvaluateNeuralNetworkClassifier"`** fits a small multilayer perceptron — sigmoid units trained by resilient
backpropagation on standardised predictors from a seeded random start, so results are reproducible call for call —
with `hiddenLayers` giving the unit count of each of up to 3 hidden layers of up to 64 units each (empty uses one
layer of 8 units) and `epochs` capped at 1000 (default 100). `earlyStoppingPatience` (default 10, 0 to 100) halts
training once the validation error stops improving for that many epochs and keeps the best weights seen; a patience
of 0 trains for every epoch regardless. The description names neural networks as needing more rows than trees and
being the hardest family in this catalogue to explain, recommending them specifically when smooth non-linear
structure is expected and suggesting a direct comparison against `"EvaluateRandomForest"` and
`"EvaluateBoostedTrees"`. The result's `ModelDescription` carries the training error trace alongside the usual
metrics and permutation importance.

## Model and hyper-parameter selection (`ModelSelectionTools.cs`)

`ModelSelectionTools` is constructed with an optional `IAdvancedAnalyticsTrainingDataPointerReader`, and every tool
in this file accepts either inline `inputs`/`labels` or a `cachePointer` — a `Guid` pointing at a stored table in
the Advanced Analytics training-data cache whose last column (or the column named by `labelColumn`) is the label —
but never both at once. This is the same pointer mechanic documented for AskJooby's chat tools at
[The data_pointer mechanic](../../../AskJooby/DataPointerCache.md): a bare `Guid` (no `tbl_` prefix) routes through
`IAdvancedAnalyticsCachedTableReader` rather than the chat-tool cache, but the two pointer families share one read
path, so a `Guid` minted by `AdvancedAnalyticsTrainingDataPut` can be handed straight to `"CompareModels"`,
`"GridSearchHyperParameters"` or any other tool in this file exactly as it would be handed to `InspectDataSchema` or
`PreviewDataRows`.

All nine tools recognise the same twelve `EvaluationFamily` values (`LogisticRegression`, `DecisionTree`,
`RandomForest`, `Bagging`, `AdaBoost`, `GradientBoosting`, `NaiveBayes`, `TreeAugmentedNaiveBayes`,
`KNearestNeighbours`, `LinearSupportVector`, `KernelSupportVector`, `NeuralNetwork`) and the same thirteen
`HyperParameter` values (`MaxDepth`, `MinLeafSize`, `TreeCount`, `FeatureFraction`, `SampleFraction`, `LearningRate`,
`NeighbourCount`, `Complexity`, `KernelWidth`, `HiddenUnits`, `Epochs`, `Regularisation`, `BinCount`), each
applying only to the families it makes sense for (for example `LearningRate` to boosting, `KernelWidth` to
`KernelSupportVector`); passing a hyper-parameter that does not apply to the chosen family throws, naming the ones
that do.

**`"GridSearchHyperParameters"`** searches every combination of one or two named hyper-parameters — `parameterA`
with `valuesA` (1 to 16 values), optionally crossed with `parameterB`/`valuesB` — for one `family`, scored by
stratified cross validation on `folds` (2 to 10, default 5) with `fixedParameters`/`fixedValues` held constant
across every trial. The grid is capped at 64 settings (`valuesA.Length * valuesB.Length`); exceeding it throws,
suggesting `"RandomSearchHyperParameters"` instead. `metric` (default `RocAuc`) ranks trials, and higher-is-better
or lower-is-better is tracked per metric (`RocAuc`, `AveragePrecision`, `F1`, `MatthewsCorrelation` are
higher-is-better; `LogLoss` and `BrierScore` are lower-is-better). With `nested` set to true (default false) it
additionally runs a nested cross validation — re-choosing the best setting inside every one of `outerFolds` (2 to
5, default 3) outer training parts and scoring on that outer fold's own held-out part — which gives an honest
estimate of what the tuning step actually buys, reported as `NestedScore` alongside `SelectionOptimism` (the gap
between the plain best score and the nested one); without nesting, the best score is explicitly flagged as
optimistically biased because it is the maximum over many trials. A `BestAtEdgeOfGrid` warning means the chosen
setting sits on the border of the searched grid and the grid should be widened. The `HyperParameterSearchResult`
carries every trial's mean training/held-out score, its spread across folds and its rank, the best setting, and a
full `ClassifierEvaluationResult` for that best configuration.

**`"RandomSearchHyperParameters"`** samples `trials` (2 to 64, default 20) random combinations of 1 to 4 distinct
hyper-parameters from caller-given `lowerBounds`/`upperBounds`, each optionally drawn on a log scale via
`logScale` (useful for scale parameters such as `Complexity`, `KernelWidth`, `LearningRate` and `Regularisation`,
and requiring a strictly positive lower bound when enabled). Whole-number parameters are rounded after sampling.
The description recommends it as the better first step whenever more than two hyper-parameters are in play, since
it covers a wide space in far fewer fits than a full grid, with `"GridSearchHyperParameters"` reserved for refining
around the winner afterwards. It shares `metric`, `folds`, `nested` and `outerFolds` with the grid-search tool and
returns the same `HyperParameterSearchResult` shape. Trials are capped at 64 and drawn from `seed`, so the same
seed reproduces the same trial set.

**`"ComputePermutationImportance"`** fits one model of `family` (default `RandomForest`) and measures how much each
variable matters by shuffling it in the held-out rows and recording how far the chosen `metric` falls — `RocAuc`
(default), `AveragePrecision` or `LogLoss` only; any other metric throws. The drop is averaged over `repeats`
(default 5, 1 to 10) and, when `crossValidationFolds` is 2 or more, over every fold too, with the standard deviation
across shuffles also reported. Because it is measured on unseen data rather than read off coefficients or split
counts, it reflects what actually generalises; a variable whose importance sits within its own noise of zero is
listed among `NegligibleVariables` and is a candidate to drop, and correlated variables are noted as capable of
sharing importance so each looks individually weak. Importance can even come out negative, when shuffling an
already-useless variable happens by chance to help. The fitted model is trained on training rows only (with
calibration on validation rows) and discarded; rows are capped at 20,000 (4000 for `KernelSupportVector`) and 200
columns.

**`"ComputePartialDependence"`** fits a model of `family` (default `GradientBoosting`) and shows how its predicted
probability changes as one named `variable` moves across its own range from the 5th to the 95th percentile, over
`gridPoints` (2 to 50, default 20) grid points, while every other variable keeps its real per-row values —
classical partial dependence. At each grid point it reports the 10th, 50th and 90th percentile of the individual
rows' predicted probabilities (the spread of individual conditional expectations, not just the mean), computed over
up to 500 held-out rows. A wide band around an otherwise flat mean line signals that the variable's effect depends
on interactions with other variables. The description recommends running `"ComputePermutationImportance"` first and
reading a partial dependence curve only for variables it flags as actually mattering, since the shape of an
unimportant variable means little. `variable` must be one of `variableNames`, or the call throws.

**`"CompareCalibrationMethods"`** fits one model of `family` (default `RandomForest`) and compares four ways of
turning its raw score into a probability on held-out rows: the uncalibrated output; Platt scaling (a logistic
curve, robust on small calibration sets); isotonic regression (a free-form monotone curve, more flexible but prone
to overfitting a small calibration set); and a constant base-rate prediction as the no-skill reference. For each it
reports log loss, Brier score, expected calibration error, ROC AUC, and the calibration slope and intercept (ideal
1 and 0 respectively — a slope below 1 reads as overconfident, above 1 as underconfident), and names the method with
the lowest log loss as `BestMethod`. Calibration is fitted on `validationFraction` rows (must be above 0, since
there is nothing to calibrate with otherwise) and measured on a disjoint `testFraction`. The description notes ROC
AUC only moves for isotonic calibration, since it alone can introduce ties into the ranking that the other three
methods preserve exactly.

**`"CompareScoreCalibrations"`** runs the identical four-way calibration comparison but on existing `scores` rather
than a model this tool fits itself — the right tool for checking a rules-engine score, a vendor risk score, or any
other externally produced score before treating it as a probability. `scoresAreProbabilities` (default true) tells
it whether the scores already sit in `0..1` or are unbounded log-odds-style scores to be passed through a sigmoid
for the "uncalibrated" row. Rows are split by `calibrationFraction` (default 0.5, 0.1 to 0.9); between 30 and 20,000
scores are required, and each class needs at least 8 rows.

**`"CompareModels"`** runs up to twelve classification `families` (default: all of them) on identical stratified
cross-validation folds (2 to 10, default 5, shared across every family and seed) and ranks them by `metric`
(default `RocAuc`). For each family it reports the mean held-out score with its cross-fold spread, the
training-to-held-out gap, the paired fold-by-fold difference to the best family, and whether that difference is
"statistically indistinguishable" from the best — defined as within one standard error of the paired difference.
When two families are indistinguishable the plain-language `Recommendation` deliberately prefers the simplest of
them, in the fixed order logistic regression, naive Bayes, a single decision tree, then forests, boosting and
neural networks, on the stated grounds that a simpler model is easier to explain, cheaper to run and drifts less in
production. Logistic regression is included by calling the very same implementation `"TrainLogisticRegression"`
uses. Every family runs at its own sensible defaults on the same folds, with calibration fitted on a validation
slice inside each fold; the description's suggested workflow is to tune the winner afterwards with
`"GridSearchHyperParameters"`. Bounds are tighter than the single-family tools: at most 10,000 rows (kernel support
vector machines are silently skipped above 4000), 200 columns, and 12 families. The `ModelComparisonResult` carries
the metric and its direction, the per-family `Ranking`, the recommended family name and the recommendation text.

## Multiclass and anomaly evaluation (`MulticlassAndAnomalyTools.cs`, `MulticlassEvaluationTools.cs`)

**`"EvaluateMulticlassClassifier"`** fits one of six algorithms — `MultinomialLogistic` (softmax regression, the
default and the interpretable baseline), `DecisionTree`, `RandomForest`, `NaiveBayes`, `KNearestNeighbours` or
`NeuralNetwork` — for an outcome with 3 to 20 classes (any whole numbers, each needing at least 5 rows) and
evaluates it on held-out rows split by `seed`, stratified, into train/validation/test or `crossValidationFolds`
(2 to 10). It returns accuracy alongside a `MajorityClassAccuracy` baseline (the accuracy of always predicting the
largest class), balanced accuracy, macro/micro/weighted F1, Cohen's kappa, multiclass Matthews correlation, log
loss, Brier score, macro one-versus-rest ROC AUC, per-class precision/recall/F1/support, and the full confusion
matrix (rows actual, columns predicted). The description's explicit warning is to read the confusion matrix and
per-class recall before the headline accuracy, since a dominant class can make accuracy look good while rare
classes are missed entirely — compare against `MajorityClassAccuracy` and prefer balanced accuracy or macro F1 in
that situation. Class probabilities are the model's own raw output and are never recalibrated, unlike the binary
tools' Platt step. Algorithm-specific knobs (`maxDepth`/`minLeafSize` for trees and forests, `treeCount` for
forests, `neighbourCount` for k-nearest-neighbours, `hiddenLayers`/`epochs` for the neural network) are accepted
regardless of algorithm but only apply to the ones that use them. `classImbalance: BalancedWeights` weights rows by
inverse class size where the algorithm supports row weights, and otherwise falls back to oversampling. Bounds: at
most 20,000 rows, 200 predictor columns, 20 classes, 500 trees, 1000 epochs; nothing is saved.

**`"EvaluateAnomalyDetector"`** fits one unsupervised anomaly algorithm — `IsolationForest` (the default and
described as the robust choice: random splits isolate outliers), `OneClassSupportVector` (a boundary around the
normal rows in a Gaussian kernel space, limited to 4000 rows), `KMeansDistance` (distance to the nearest of
`clusters` cluster centres), `GaussianMixtureDensity` (negative log density under a mixture fit) or
`MahalanobisDistance` (covariance-aware distance from the mean) — on rows split, stratified by label when one is
given, into train/validation/test. A higher score always means more anomalous, regardless of algorithm. The
expected anomaly share (`contamination`) is taken, in priority order, from the value explicitly given, else from
the share of `knownAnomalyLabels` actually equal to 1, else estimated directly from the score distribution; the
threshold that contamination implies is set on the validation rows and every other figure comes from the test rows.
`knownAnomalyLabels` is entirely optional (1 marks a known anomaly, 0 a normal row) — supplying it unlocks ROC AUC,
average precision, precision, recall and F1 at the chosen threshold, and needs at least 3 known anomalies to be
accepted; `trainOnNormalRowsOnly`, when labels are given, fits the detector on the label-0 rows only, the correct
setup when the normal population is already known and clean. A `FlaggedShareShift` warning fires when the share of
test rows actually flagged differs substantially from the expected contamination. The result also returns the test
score quantiles and the top-scoring test rows by original row number, which is the most direct way to see what the
detector is actually flagging. Bounds: at most 20,000 rows and 200 columns (4000 rows for
`OneClassSupportVector`), up to 500 trees, up to 32 clusters; nothing is saved.

**`"EstimateAnomalyContamination"`** takes a plain array of anomaly `scores` with no labels at all and estimates
what share is anomalous using four independent, purely descriptive heuristics: the share of scores above a
modified z-score of 3.5 (built from the median and the median absolute deviation, so it is itself robust to the
outliers it is trying to find); the share above the upper Tukey fence (`Q3 + 1.5 * IQR`); the largest break in the
sorted scores within their top 30%; and the weight of the upper component of a two-component Gaussian mixture fit
to the (log) scores. Each estimate is reported with a note explaining it, or explaining why it could not be
computed. The `RecommendedShare` is the median of whichever estimates actually agree, clamped between 0.1% and 30%,
with the threshold that share implies reported alongside it. The description is explicit that every one of the
four is a heuristic: they agree when there is a genuinely separated anomalous cluster, and disagree — or return
nothing at all — when the scores form one smooth continuum, in which case the stated recommendation is to choose
contamination from business alert capacity instead of from the data. `higherIsMoreAnomalous` (default true) flips
the whole calculation for detectors (such as a likelihood) where a *lower* score is the anomalous direction. Bounds:
20 to 20,000 finite scores; fewer than 20 triggers a `SmallSample` warning rather than an outright failure, and
scores from `"EvaluateAnomalyDetector"` or from any external detector are equally valid input.

## Regression evaluation (`RegressionEvaluationTools.cs`)

The five single-family tools below share the same scheme as the classification tools in `ModelEvaluationTools.cs`:
rows split by `seed` into train/validation/test (or `crossValidationFolds`, 2 to 10), scaling and missing-value
filling learned from training rows only, and every reported metric — R-squared, mean absolute error, root mean
squared error, MAPE (skipping zero targets), symmetric MAPE, median and maximum absolute error, correlation, and
residual mean/spread/skewness — computed on the untouched held-out rows next to a baseline that always predicts the
training mean. Permutation importance, when requested, is the drop in held-out R-squared when a variable is
shuffled. Shared bounds: at most 20,000 rows, 200 predictor columns, 500 trees, 1000 epochs (kernel ridge regression
is limited to 4000 rows); nothing is saved. The shared return shape is `RegressionEvaluationResult`: family, scheme,
seed, settings, row/feature counts, `Train`/`Validation`/`Test`/`BaselineTest` metrics, per-fold results, a
`Coefficients` array (populated for the linear family), a hyper-parameter `Sweep`, `Importance` rows, a
`ModelDescription` and `Warnings`.

**`"EvaluateLinearRegression"`** fits ordinary least squares (`penalty: None`, the default) or a penalised variant —
`Ridge` (shrinks coefficients, suited to collinear predictors), `Lasso` (drives weak coefficients exactly to zero,
suited to variable selection) or `ElasticNet` (mixes both via `l1Ratio`, default 0.5: 1 is pure Lasso, 0 is pure
Ridge) — sweeping `regularisationValues` (up to 16 values between 0 and 1,000,000, standardised; empty tries
`0.01, 0.1, 1`, ignored entirely when `penalty` is `None`) and picking the best on the validation rows. Coefficients
are reported back on the original variable scale (the change in the target per one unit of the variable) alongside
the intercept. The description frames it as the transparent baseline against which every other regression tool
should be compared: if a tree or forest beats it substantially, the real relationship is non-linear or has
interactions, and warns that unpenalised (`None`) fitting becomes unstable with collinear predictors, where `Ridge`
is the fix.

**`"EvaluateTreeRegression"`** fits `DecisionTree` (one readable regression tree, with rules and tree text in the
result), `RandomForest` (the default — averages many deep trees on bootstrap samples) or `GradientBoosting`
(shallow trees fitted in sequence to the remaining error, tempered by `learningRate`). Trees capture non-linear
effects, thresholds and interactions without any scaling step, but predict step functions and cannot extrapolate
past the target range actually seen in training — the description recommends comparing against
`"EvaluateLinearRegression"` and treating a small gain as evidence the true relationship is close to linear, and
explicitly flags the `TrainTestGap` warning as the signal that a single deep tree has overfit (forests are safer;
boosting needs shallow trees and a small learning rate). `pruning: ReducedError` prunes a `DecisionTree` using the
validation rows; it is ignored by the ensemble algorithms.

**`"EvaluateNeighbourAndKernelRegression"`** fits `KNearestNeighbours` (the default — predicts the mean target of
the `neighbourCounts` closest training rows on standardised predictors, swept up to 16 values between 1 and 500,
empty trying `3, 5, 9, 15, 25`) or `KernelRidge` (a smooth non-linear surface with a ridge penalty of `1 / complexity`,
swept via `complexityValues` up to 16 values between 0.0001 and 10000, empty trying `1, 10, 100`, with `kernel`
defaulting to `Gaussian` and `kernelWidth` defaulting to `1` divided by the variable count). Both families are
flexible, purely local methods that cannot extrapolate beyond the training data's range and slow down as rows grow,
which is why kernel ridge here is limited to roughly half the usual kernel row bound. The chosen setting is picked
on the validation rows only.

**`"EvaluateNeuralNetworkRegression"`** fits a small multilayer perceptron — sigmoid units trained by resilient
backpropagation on standardised predictors and a target rescaled into the sigmoid's own range, from a seeded start
for reproducibility — with `hiddenLayers` (up to 3 layers of up to 64 units, empty uses one layer of 8 units) and
`epochs` capped at 1000 (default 200). The description repeats the same caution as the classifier version: neural
networks need more rows than trees, cannot extrapolate past the training target range, and are hard to explain, and
recommends comparing against `"EvaluateLinearRegression"` and `"EvaluateTreeRegression"` before adopting one. The
`ModelDescription` carries the training error trace.

**`"CompareRegressors"`** runs up to seven regression `families` (default: all of `LinearRegression`,
`DecisionTree`, `RandomForest`, `GradientBoosting`, `KNearestNeighbours`, `KernelRidge` and `NeuralNetwork`) on
identical cross-validation `folds` (2 to 10, default 5) and ranks them by `metric` — `RSquared` (higher is better)
or `MeanAbsoluteError`/`RootMeanSquaredError` (the default)/`MeanAbsolutePercentageError` (lower is better). As with
`"CompareModels"`, two families whose fold-by-fold difference is within one standard error are called
indistinguishable, and the `Recommendation` then prefers the simplest. `KernelRidge` is silently skipped above 2000
rows. The `RegressorComparisonResult` carries the metric and its direction, the training-mean `BaselineScore`, the
per-family `Ranking`, and the recommended family with its recommendation text. Bounds: at most 20,000 rows and 200
predictor columns; nothing is saved.

**`"ComputeRegressionPermutationImportance"`** is the regression counterpart of
`"ComputePermutationImportance"`: it fits one `family` (default `RandomForest`), shuffles each variable in the
held-out rows, and records the resulting drop in R-squared, averaged over `repeats` (1 to 10, default 5) and,
optionally, over `crossValidationFolds`. It is model-agnostic across every regression family in this catalogue and,
being measured on unseen data, reflects what actually generalises rather than what a coefficient or a split count
merely suggests; a variable whose importance sits near zero, or within its own standard deviation of zero, is a
candidate to drop, and correlated variables are noted as capable of sharing importance so each looks weaker
individually. The fitted model is training-rows-only and discarded. Bounds: at most 20,000 rows and 200 columns.

**`"ComputeRegressionPartialDependence"`** mirrors `"ComputePartialDependence"` for a continuous target: it fits one
`family` (default `RandomForest`) and shows how the average prediction for a named `variable` changes across its
5th-to-95th-percentile range over `gridPoints` (2 to 50, default 20) points, reporting the 10th, 50th and 90th
percentile of individual predictions at each point from up to 500 held-out rows. A wide band around an otherwise
flat average again signals an interaction with other variables, and the description recommends reading the curve
together with the variable's permutation importance, since the shape of an unimportant variable means little.
`variable` must be one of `variableNames`, or the call throws.
