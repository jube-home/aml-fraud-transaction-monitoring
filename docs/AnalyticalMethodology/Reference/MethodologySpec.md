---
layout: default
title: Exhaustive Methodology
nav_order: 5
parent: Reference
grand_parent: Analytical Methodology
---

# Exhaustive Search: analytical methodology specification (as-built)

A descriptive, as-built account of how the Exhaustive search actually works, based on the engine's source directly
rather than its documentation. Sections 1 to 9 describe the mechanism; section 10 catalogues its known limitations as
a defect table, each with the evidence it is based on and a proposed direction.

`Training.cs` is `Jube.Engine/Exhaustive/Training.cs`, `Supervised.cs` is
`Jube.Engine/Exhaustive/Algorithms/Supervised.cs`, `Performance.cs` is
`Jube.Engine/Exhaustive/Algorithms/Performance.cs`, `DataUtility.cs` is
`Jube.Engine/Exhaustive/Utilities/DataUtility.cs`. Where a statement is an inference rather than something directly
visible in the code it is marked (inferred).

## 0. Executive summary

The Exhaustive search is a random-subset-of-variables plus evolved-topology feed-forward neural network search,
trained with Levenberg-Marquardt (LM), scored by the mean of accuracy and absolute Spearman correlation of the
thresholded output. The output of the network is not, by itself, a calibrated probability (see
[Exhaustive Calibration](ExhaustiveCalibration.md) for how a calibrated probability is produced from it). The
pipeline has a number of defects that materially affect the meaning of every reported metric; the ones that matter
most are:

1. The "testing" split overlaps the training split (Supervised.cs:1631). All promotion scores, the confusion matrix,
   the ROC points and predicted-versus-actual data are computed on rows that were also used for training.
2. `ReduceDataForOnlyTrialVariables` fills only every second selected column (Supervised.cs:1572-1576), the rest of the
   training matrix is zero, while recall feeds every selected variable. Train and serve inputs differ.
3. Variable selection draws indices from `[0, trialVariableCount-2]` rather than from the variable list
   (Supervised.cs:1644), so it is biased towards the first variables in the list and can never reach the last.
4. Several `Length - 1` off-by-one loops drop the last row or the last variable (Training.cs:757, 789, 819, 849, 891,
   Performance and others), most importantly the last variable never gets statistics or a normalisation type.
5. Normalisation statistics are global (pre-split) and the shuffle, symmetry sampling and (many) RNGs are unseeded.
6. There are two recall implementations (HTTP recall endpoint and in-pipeline adaptation) which impute missing inputs
   differently.

## 1. Pipeline overview and status codes

`Training.StartAsync` (Training.cs:50-144) polls every 60 s (Training.cs:115, 132) for the next instance
(`GetNextExhaustiveSearchInstanceQuery`) and calls `ExecuteAsync` (Training.cs:146). Only one instance is trained at a
time per engine instance. Each step first checks the stop flag (`IsStoppingOrStoppedOrCancelledAsync`,
Training.cs:603-618) and writes a numeric status to the instance row.

| Status | Step | Reference |
|---|---|---|
| 1 | Fetching data | Training.cs:201 |
| 2 | Calculating base statistics | Training.cs:241-246 |
| 3 to 6 | Anomaly branch (One Class SVM) only when `Anomaly` | Training.cs:207-366 |
| 7, 8 | Filter class data extraction and its statistics, only when `Filter` | Training.cs:368-425 |
| 9 | Shuffling | Training.cs:440-445 |
| 10 | Normalising | Training.cs:460-463 |
| 11 | "Over sampling" (actually under-sampling, see 3.4) | Training.cs:478-481 |
| 12 | Storing training data | Training.cs:497-501 |
| 13 | Spearman correlation of every variable to the class | Training.cs:514-518 |
| 14 | Multi-collinearity | Training.cs:533-537 |
| 15 | Supervised training | Training.cs:552-558 |
| 16 | Completed | Training.cs:569 |
| 17 | Not trained because `classCount == 0` | Training.cs:594 |
| 19 | Stopped | Training.cs:581, 615 |

Steps 9 to 15 only run if `classCount > 0` (Training.cs:443).

## 2. Data loading and cleaning

### 2.1 Sources

* Real data: table `Archive` (`Json` column). Mock data: table `MockArchive`, selected per instance by a per-instance
  data source setting. Same JSON shape.
* Mock loading: `Training.LoadMockDataAsync` (Training.cs:1410-1537) parses the embedded CSV `Mock.Get()`
  (`Jube.Engine/Exhaustive/Mock.cs`), skipped entirely if the model already has rows in `MockArchive`
  (Training.cs:1420-1430).

### 2.2 Row sampling

* Negative or population sample: `ArchiveRepository.GetJsonByEntityAnalysisModelIdRandomLimitAsync`
  (`Jube.Data/Repository/ArchiveRepository.cs:102-109`) and the `MockArchive` twin. Despite the name it is
  `ORDER BY EntityAnalysisModelInstanceEntryGuid ... LIMIT 10000`. It is a deterministic ordering by a (random) GUID,
  not `random()` and not stratified. The cap is hard-coded to 10000 (DataUtility.cs:90, 98, 121).
* With a class filter the population query is `Postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync`
  (`Jube.Data/Reporting/Postgres.cs:189-230`): `where ModelId = @n and (NOT (<filter sql>)) order by EntryGuid
  limit 10000`, i.e. the negative class is "everything that does not match the positive filter"
  (DataUtility.cs:118-122; the docs also state "The filter will also be used to negate the sample of negative class").
* Positive class (Filter branch): `Extraction.GetClassDataAsync` (DataUtility.cs:33-79), same table, filter SQL, same
  10000 limit, label 1 for every row returned (DataUtility.cs:75).
* Anomaly branch: the whole sample is scored by a One Class SVM (see 2.5) and rows scoring below the threshold
  probability become class 1 (Training.cs:305, 805).

### 2.3 Variable list and JSON extraction

* Real data: fields from `GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(model, 5, true)` filtered
  to `ProcessingTypeId` 3, 5 or 7 (DataUtility.cs:134-150). Order is the order returned by that query; this order
  becomes the variable index and (see 5.1) matters.
* Mock data: a hard-coded list of 27 `Abstraction.*` variables (DataUtility.cs:155-342). The list contains
  `Abstraction.DifferentCities1Week` twice (DataUtility.cs:302-314) and the final key is added with
  `headerSequence + 1` (DataUtility.cs:337).
* Value extraction: `jObject.SelectToken(ValueJsonPath)`, `Value<double>()`; a missing token, a non-numeric token or any
  exception yields 0 (DataUtility.cs:351-368). There is no missing-value indicator, no NaN handling, no row filtering.
  Missing is silently the value 0 in training.
* Dependent variable is not part of `variables`; it is the parallel `outputs` array (see 2.4).

### 2.4 Class labelling

* Filter mode: population rows labelled 0, filter rows labelled 1, appended (`Append`, Training.cs:1556-1574;
  Training.cs:422). Population and class rows are disjoint by construction of the `NOT (...)` filter.
* Anomaly mode: labels from the One Class SVM (`GetClassVariableByAnomaly`, Training.cs:783-815) with
  `p < AnomalyProbability => 1` (Training.cs:805).
* Combined mode: anomaly positives remain, filter positives are appended, `classCount` is summed (Training.cs:424).
* Note the loop `for (i < data.Length - 1)` leaves the last element of `outputs` at 0 (Training.cs:789).

### 2.5 Anomaly model (One Class SVM)

`Unsupervised.Learn` (`Jube.Engine/Exhaustive/Algorithms/Unsupervised.cs:22-50`): `Gaussian.Estimate(data, data.Length)`
for the kernel (this uses all N rows, an O(N^2)-style estimate) then `OneclassSupportVectorLearning<Gaussian>` with
default nu. Trained on a normalised copy of the population data (Training.cs:269). It is used only to derive labels; it
is never recalled at scoring time.

### 2.6 Cleaning

There is no outlier treatment, no winsorising, no de-duplication, no imputation and no leakage check. The only
sanitising is `SwapNanInfinityToZero` applied to the *reported* statistics (Training.cs:863-876).

## 3. Split, sampling, ordering

### 3.1 Sequence of operations on the full dataset (Training.cs)

1. Fetch population (`data`) and compute statistics on it only (Training.cs:244, before class rows are appended).
2. (Filter mode) fetch class rows, compute statistics of the class rows into separate tables (Training.cs:418), append
   them (Training.cs:422).
3. Shuffle (`Shuffle`, Training.cs:1576-1601), then normalise (Training.cs:463), then class-symmetry sample
   (Training.cs:481, DatasetSymmetry 690-781).
4. Persist the post-symmetry matrix, correlations, multicollinearity, then `Supervised.TrainAsync`.

There is no split at this stage. Splitting happens inside every trial (3.3).

### 3.2 Shuffle

`Training.Shuffle`: builds an index array, orders by `new Random().Next()` (a fresh unseeded `Random`, Training.cs:1587),
gathers rows and outputs. Not stratified and not reproducible. It is performed once per training instance, so every trial
in the instance sees the same row order and therefore the same split rows (only the columns change).

### 3.3 Train, cross-validation and test split

`Supervised.SplitData` (Supervised.cs:1589-1633), called with the environment percentages
`ExhaustiveTrainingDataSamplePercentage=0.6`, `ExhaustiveCrossValidationDataSamplePercentage=0.2`,
`ExhaustiveTestingDataSamplePercentage=0.2` (DynamicEnvironment.cs:208-214; read at Supervised.cs:57-62). There is no
validation that they sum to 1. There is no stratification. The split is positional over the shuffled, symmetrised data.

```
trainingLength        = (int)(N * train)         // 0.6 N
crossValidationLength = (int)(N * cv)            // 0.2 N
testingLength         = (int)(N * test)          // 0.2 N
SplitArray(x, start, count):  rows [start, start+count-1]   (Supervised.cs:1728-1739, third parameter is a count)

dataTraining    = SplitArray(data, 0,                     trainingLength)            rows [0, 0.6N)
dataCrossVal    = SplitArray(data, trainingLength,        crossValidationLength)     rows [0.6N, 0.8N)
dataTesting     = SplitArray(data, crossValidationLength, testingLength)             rows [0.2N, 0.4N)   <-- overlaps training
outputsCrossVal = SplitArray(out,  trainingLength,        testingLength + cvLength)  rows [0.6N, 1.0N)   (longer than inputs)
outputsTesting  = SplitArray(out,  crossValidationLength, testingLength + cvLength)  rows [0.2N, 0.6N)   (longer than inputs)
```
(Supervised.cs:1625-1632.)

Consequences (verified by reading the arithmetic at the default 0.6/0.2/0.2):

* The testing inputs are rows 0.2N to 0.4N, entirely inside the training rows 0 to 0.6N. The "test" set is a subset of
  the training set.
* `outputsTesting` and `outputsCrossValidation` are longer than their inputs; only the first `len(inputs)` entries are
  used because `Performance` iterates over `scores.Length` (Performance.cs:55). Aligned only by prefix.
* Rows in `[0.8N, 1.0N)` are never used as inputs by any set.
* The split is recomputed every time it is called: once per iteration of the variable-elimination loop
  (Supervised.cs:985) and once more for the returned set (Supervised.cs:1117). It is deterministic given the data, so
  the sets are stable within a training instance.

### 3.4 Class symmetry ("over sampling", status 11)

`Training.DatasetSymmetry` (Training.cs:690-781). Counts positives with `(int)outputs.Sum()`; if unequal, removes
the surplus by rejection sampling: draw `seeded.Next(0, Length-1)`, accept if the label equals the class being removed
(Training.cs:730-747). It is an under-sampling of the majority class, not over-sampling (status text is misleading).
Details worth knowing:

* `seeded` is `new Random(Environment.TickCount ^ Guid.NewGuid().GetHashCode())` (Training.cs:48). This is an unseeded,
  non-reproducible RNG; the same instance is handed to `Supervised` (Training.cs:557).
* `Next(0, Length - 1)` never picks the last row.
* The accepted row is added to `remove` without checking whether it is already present (Training.cs:739), so a duplicate
  draw consumes `removeCount` but removes nothing; the balanced result can be under-reduced. (inferred from the code path;
  `remove` is a `List<int>`.)
* The rebuild loop is `i < Length - 1` (Training.cs:757), which drops the final row unconditionally.
* Symmetry happens after normalisation and before the split, so class balance in train, cv and test is only expected
  (positional slices of a shuffled balanced set), not enforced.

### 3.5 Mock data expected effect

For the mock fixture (Section 11) there are 926 negatives and 901 positives, so symmetry removes about 25 rows.

## 4. Normalisation and standardisation

### 4.1 Statistics

`CalculateStatisticsAsync` (Training.cs:878-1312) is called on the pre-shuffle, pre-symmetry population matrix
`data` (Training.cs:244). For variable `i` in `0 .. variables.Count-2` (Training.cs:891, note the `- 1`) it computes:
mean, mode, max, min, kurtosis, skewness, IQR (quartiles), standard deviation (the statistics library's default
estimator, believed sample, unverified), distinct count (O(n^2) `List.Contains`), and a histogram
(Training.cs:995-1082). Bin count logic (Training.cs:1003-1068) uses maxBins 20; note that the branch
`numberOfBins > DistinctValues` at Training.cs:1018 compares against the just-initialised `numberOfBins = 0`, so it is
dead code, and `histogram.Compute(variableData)` at 1083 does not receive `numberOfBins` at all: bin count is the
library's own default, not the computed one (the computed value is only stored).

### 4.2 Normalisation type

Assigned per variable at Training.cs:1126-1137 (only in the population pass, `repositoryVariables` of type
`ExhaustiveSearchInstanceVariableRepository`):

| Type | Rule | Transform |
|---|---|---|
| 0 | `Max == 0` (all zero) | none |
| 1 | `Min == 0` and `Max == 1` (within 1e-4) | none (binary passthrough) |
| 2 | everything else | z-score `(x - mean) / sd` |

Variables that are constant and non-zero (`sd == 0`) are still type 2 and would divide by zero (NaN or infinity).
There is no guard in `NormaliseData` (Training.cs:831) nor at recall (ExhaustiveExtensions.cs:76 and
`ExhaustiveSearchInstancePromotedTrialInstanceVariable.ZScore`). `ZScore` at Training.cs:1539-1554 wraps a try/catch
which cannot trigger for doubles.

### 4.3 Application

* `NormaliseData` (Training.cs:817-843): in-place on the whole matrix, `for i < data.Length - 1` so the last row is never
  normalised; `for j < variables.Count` so all variables are covered, including the last variable whose statistics were
  never computed (mean 0, sd 0, type 0 default, so it passes through unchanged).
* Statistics come from the whole population sample only (before class rows are appended and before any split), so the
  test and cross-validation rows contribute to the mean and standard deviation (leakage, see 10), and positive-class rows
  do not contribute at all in filter mode.
* Anomaly branch normalises a `data.Copy()` before training the SVM (Training.cs:269), with the same statistics.
* Persistence for recall: mean, sd, normalisation type per variable are read back from `ExhaustiveSearchInstanceVariable`
  through `GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery`
  (`Jube.Data/Query/GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery.cs:24-78`) into
  `ExhaustiveSearchInstancePromotedTrialInstanceVariable` at sync time
  (`SyncExhaustiveSearchInstancesExtensions.cs:178-189`).

## 5. Variable selection and ordering

### 5.1 Random subset per trial

`Supervised.OptimiseDataAsync` (Supervised.cs:902-1148) runs one loop per trial.

* Subset size: `GetRandomVariableCount` (Supervised.cs:1742-1758): if fewer available than `minVariableCount` return
  all; else `seeded.Next(min, availableVariableCount)` clamped to `maxVariableCount`. Defaults min 5, max 30
  (DynamicEnvironment.cs:202-205). For the 27-entry mock list the value is in 5..26.
* Variables: `SelectVariablesAsync` (Supervised.cs:1635-1726) loops `j` from 0 to `count-1` and draws
  `seeded.Next(0, randomTrialVariableCount - 1)` (Supervised.cs:1644). The upper bound is the *subset size*, not the
  variable count. So only indices `0 .. count-2` can be drawn and duplicates are skipped (Supervised.cs:1654), meaning
  the effective subset is smaller than the drawn count and is drawn only from the first variables in list order. For the
  minimum count 5 only indices 0..3 are reachable. The last variable index can never be chosen.
* `VariableSequence` stored is the loop counter `j`, not a dense rank (Supervised.cs:1685). The trial variable order is
  the dictionary insertion order.

### 5.2 Column reduction defect

`ReduceDataForOnlyTrialVariables` (Supervised.cs:1565-1580):

```csharp
for (var k = 0; k < trialVariables.Count - 1; k++)
{
    reducedData[j][k] = data[j][trialVariables.ElementAt(k).Key];
    k += 1;              // extra increment inside the loop
}
```
Only indices 0, 2, 4, ... (below `Count-1`) are populated. All other columns (odd indices and the last column) are 0 for
every row. The network is nevertheless built with `trialVariables.Count` inputs
(Supervised.cs:653, Supervised.cs:1295-1296 and siblings) and recall feeds every selected variable
(ExhaustiveExtensions.cs:46-111, ExhaustiveAdaptationsExtensions.cs:57-64). Inputs that saw only zeros in training have
weights that received no gradient and stay at their Nguyen-Widrow initial values, then receive real values at recall.

### 5.3 Backward elimination by sensitivity

Loop `while (true)` (Supervised.cs:934):

1. If `trialVariables.Count < minVariableCount`, (re)select a random subset and reset the snapshot state
   (Supervised.cs:936-973).
2. Reduce, split, search the activation function (10 candidates, 6.2), then compute sensitivity of each variable
   (Supervised.cs:1021).
3. If the best activation score on cross validation beats `bestSensitivityAnalysisScore` (Supervised.cs:1033), snapshot
   the variables and function, and remove `sensitivityAnalysis.ElementAt(removedVariablesCount).Key`
   (Supervised.cs:1048), then increment `removedVariablesCount`.
4. Else return the snapshot (Supervised.cs:1068-1145) and mark the removed variables in the database
   (loop `j < originalVariables.Count - 1`, Supervised.cs:1075, so the last original variable is never marked removed).

Notes: the sensitivity ranking is recomputed each iteration on the remaining variables, but the removal index
`removedVariablesCount` keeps growing, so after the first removal the variable removed is the second least sensitive of
the new ranking, then the third, and so on, until `ElementAt` runs off the end (an exception that is swallowed per trial
at Supervised.cs:323). If `bestActivationFunctionScore` is never greater than 0 in the first iteration the snapshot is
empty and the method returns an empty variable set. If removal takes the count below `minVariableCount` the loop
re-selects a fresh random subset (step 1) and restarts.

### 5.4 Sensitivity analysis

`Performance.Sensitivity` (Performance.cs:161-180). For variable `test`, for every row in the supplied matrix
`x[i][test] = x[i][test] * U(0.8 x[i][test], 1.2 x[i][test])` (Performance.cs:167-169). The value is multiplied by a
random draw from a range that is itself proportional to the value (so the perturbation is `x^2 * u` with
`u ~ U(0.8, 1.2)`, not `x * u`). The result is `|baseline[i] - model(x')[0]|` averaged over rows
(Supervised.cs:1219). Uses the shared unseeded `seeded` RNG. On normalised data (z-scores, values near 0) the perturbation
is near zero for near-zero inputs and asymmetric in sign. Ranking is ascending by `|mean|` (Supervised.cs:1224-1228).

Rank storage for the promoted model: `SensitivityRank = 1` is hard-coded for every variable (Supervised.cs:489). The
promoted model's sensitivity is computed on `dataCrossValidation` against `performance.Scores`
(Supervised.cs:270-273, 480-481) but at that point `performance.Scores` holds the scores for the (test) matrix from
Supervised.cs:849, not the cross-validation matrix, so baseline and perturbed rows correspond to different rows (they
only match in length at the default 0.2/0.2 split).

### 5.5 Variable statistics stored

* Pre-training statistics for all variables (2.x, 4.1) and Spearman correlation of each variable to the class
  (Training.cs:640-688, `Performance.SpearmansCoeff`, Performance.cs:118-159, ties get average rank). Loop is
  `variables.Count - 1`.
* Multi-collinearity: full pairwise Spearman on the post-symmetry matrix, O(V^2 N log N) (Training.cs:1314-1408).
  Computed and stored; it is not used to select or drop variables.

## 6. Training

### 6.1 Algorithm

Feed-forward activation network with Nguyen-Widrow initial weights (`TopologyRandomise`, Supervised.cs:1582-1587;
`NguyenWidrow.Randomize` draws from the network library's own uniform-distribution generator, unseeded here) trained
with Levenberg-Marquardt learning (Supervised.cs:657, 814, 1472), used at defaults: `lambda`/`LearningRate = 0.1`,
`Adjustment = 10`, `Blocks = 1`, no Bayesian regularisation. There is no learning rate schedule, no momentum (LM has
none), no batch size (full batch, `RunEpoch(data, outputs)`), no weight decay. Targets are 0 and 1 in a single output
neuron. The network also carries annotations (per-input name, mean, sd, min, max, mode, normalisation type) that are
serialised with the model (Supervised.cs:1772-1789).

### 6.2 Activation function search

`SearchForBestActivationFunctionAsync` (Supervised.cs:1265-1538). Ten candidates in a `for j = 1..10` switch: Bipolar
sigmoid(1), Bipolar sigmoid(2), Threshold, Sigmoid(1), Sigmoid(2), Linear(1), Linear(2), ReLU, Identity, Bernoulli
(Supervised.cs:1286-1457). (A Gaussian function network is constructed first at 1279-1284 but immediately replaced in
case 1 and never trained.) For each: fresh network with a single neuron output layer (no hidden layer,
`ActivationNetwork(fn, nInputs, annotations, 1)`), Nguyen-Widrow randomised once, then
`for k = 1; k < ExhaustiveActivationFunctionExplorationEpochs (3)` LM epochs (so 2 epochs at default, Supervised.cs:1475)
each followed by scoring on the cross-validation set at threshold 0.5 (Supervised.cs:1497-1506); the best-scoring
(network, function) over all functions and epochs is kept (Supervised.cs:1514-1518). Note Threshold and Bernoulli are not
differentiable or are stochastic, and Linear/Identity/ReLU are unbounded, which matters for 7.

### 6.3 Topology evolution

`EvolveTopologyAsync` (Supervised.cs:590-888), per trial:

* Start with one hidden layer of one neuron plus the single output neuron (`layers = [1]`, `LayersParamsArray`
  appends the output layer, Supervised.cs:615-618, 890-900). Every iteration builds a fresh network with the chosen
  activation function and fresh Nguyen-Widrow weights (Supervised.cs:653-657); it does not warm-start from the previous
  topology.
* Each topology trial: `ExhaustiveTopologyExplorationEpochs = 3` LM epochs on the training set (Supervised.cs:666-689),
  score on the cross-validation set at threshold 0.5 (Supervised.cs:698-701).
* If the score improves: clone (`DeepMemberwiseClone`), reset the no-improvement counter (Supervised.cs:712-718). A score
  of 1 (within 1e-4) stops the loop (Supervised.cs:727).
* Growth: if the last hidden layer's width exceeds `nInputs * ExhaustiveLayerWidthLimitInputLayerFactor (4)` then, if
  `layers.Count < ExhaustiveLayerDepthLimit (4)` the loop **breaks** (Supervised.cs:741-752, the comparison direction
  looks inverted: a new layer is only added when the depth is already at or above the limit, which for the default limit
  of 4 is never reached with a single hidden layer); otherwise it adds one neuron to the last layer
  (Supervised.cs:768). In practice the search widens one hidden layer until either the width limit, the
  no-improvement limit (`ExhaustiveTopologySinceImprovementLimit = 10`, Supervised.cs:620, 802) or the complexity limit
  (`ExhaustiveTopologyComplexityLimit = 10000`, Supervised.cs:789; computed as the sum of neurons, not weights) is hit.
* Finalisation: the best network is trained for `ExhaustiveTopologyFinalisationEpochs = 20` further LM epochs
  (Supervised.cs:814-839) on the training set with no stopping criterion, then scored on the "test" set at threshold 0.5
  (Supervised.cs:849-851).

### 6.4 Trial loop and promotion

`TrainAsync` (Supervised.cs:52-332): `for i = 1; i < ExhaustiveTrialsLimit (1000)` (so 999 trials, Supervised.cs:128).
A trial is promoted when `performance.Score > bestScore && > 0` (Supervised.cs:215) where `performance.Score` is the
finalised score on the "test" matrix (which overlaps training). Promotion writes the model JSON, ROC, predicted versus
actual, sensitivity and Monte Carlo prescription (6.6). Training ends at the trial limit, at a perfect score of 1
(Supervised.cs:297-310), or on a stop request. Exceptions inside a trial are logged and the loop continues
(Supervised.cs:323-328).

`ModelsSinceBest` is updated per trial (Supervised.cs:313, 583-587); the docs describe termination on
"more than a certain number of topologies tried since best", but the code has no such termination at the trial level;
`ExhaustiveTopologySinceImprovementLimit` applies only inside a topology search.

### 6.5 Iteration recording cadence

Recorded in the database rather than in memory:

* one `ExhaustiveSearchInstanceTrialInstance` per trial (Supervised.cs:143-144);
* one `TopologyTrial` row per topology attempt, with layer, neurons and cross-validation score
  (Supervised.cs:628-643), plus one "Finalisation" row (Supervised.cs:863-877);
* one `ActivationFunctionTrial` row per candidate function (Supervised.cs:1461-1464);
* one `Sensitivity` row per variable per activation search (Supervised.cs:1187-1202);
* per promotion: promoted model, 20 ROC rows, all predicted-vs-actual rows, sensitivity rows and Monte Carlo rows.

There is no per-epoch loss or error recorded. The "learning curve" shown in the UI is the score of each *promoted*
model over time (`GetExhaustiveSearchInstancePromotedTrialInstanceLearningCurveQuery`, rounded to 2 dp, ordered by id).

### 6.6 Monte Carlo prescription

`PerformMonteCarloSimulationToUnderstandTopologyModelAsync` (Supervised.cs:369-470): draws `ExhaustiveSimulationsCount`
(100) synthetic inputs per variable from the stored `TriangularDistribution(min, max, mode)` in normalised space
(built at Training.cs:1147-1182), snaps normalisation-type-1 variables to 0 or 1 at 0.5 (Supervised.cs:384-387), keeps
those where `network(x)[0] > 0.5`, reverses the z-score and stores mean, max, min, sd and IQR per variable
(Supervised.cs:424-434). IQR is computed as `q1 - q3` (negative) and then z-score reversed, so the stored IQR is not a
meaningful value. It is descriptive only and not used in training or recall. Distribution construction can fail silently
(`catch { value = 0 }`, Supervised.cs:389).

## 7. Metrics

### 7.1 Score used for selection

`Performance.CalculatePerformance(scores, dependent, threshold)` (Performance.cs:46-105):

* `predicted = score > threshold ? 1 : 0` (`threshold` = `ExhaustiveValidationTestingActivationThreshold`, default 0.5).
* Confusion counts TP, TN, FP, FN (Performance.cs:74-92).
* `PercentageCorrect` = accuracy.
* `Correlation` = `|Spearman(predictedBinary, actual)|`, NaN mapped to 0 (Performance.cs:97-101). Spearman on two
  binary vectors is a rank version of the phi coefficient, and the absolute value means a perfectly inverted classifier
  scores the same as a perfect one.
* `Score = (accuracy + |rho|) / 2` (Performance.cs:102). This single number drives activation function choice,
  topology choice, variable elimination and promotion. The mean squared error array is computed and discarded
  (Performance.cs:51, 57, 95).
* `Performance` is a single mutable instance shared by all stages (`Supervised.cs:50`), with state overwritten by every
  call (`ResetPerformance`).

### 7.2 Sets

| Purpose | Set used | Reference |
|---|---|---|
| Activation function choice, topology growth, variable elimination | Cross validation (rows 0.6N to 0.8N) | Supervised.cs:1497-1506, 698-701, 1166 |
| Promotion, confusion (TP/FP/TN/FN), ROC, predicted vs actual, error histogram | "Test" (rows 0.2N to 0.4N, inside training) | Supervised.cs:849-851, 215, 245-259 |
| Sensitivity on promoted model | Cross validation matrix vs test scores (mismatched) | Supervised.cs:270-273 |

There is no held-out set that the model has never seen, and no set that was not used for selection.

### 7.3 Threshold-sweep table (ROC points)

`StoreRocAsync` (Supervised.cs:499-530): 20 thresholds starting at 0.05 in steps of 0.05 (0.05, 0.10, ..., 1.00),
recomputing TP, FP, TN, FN and Score from `performance.Scores` and the test labels each time and storing them per
promoted trial. Note the sweep overwrites the shared `performance` counters, leaving them at threshold 1.0 after the
call; the promoted model row had already been written (Supervised.cs:233-235) so the persisted confusion is at 0.5.
`GetExhaustiveSearchInstancePromotedTrialInstanceRocQuery` returns `(Fpr = FP/(FP+TN), Tpr = TP/(TP+FN))` per row,
computed as `Rate(numerator, other)` (`Jube.Data/Query/GetExhaustiveSearchInstancePromotedTrialInstanceRocQuery.cs:67-80`).
No AUC is computed or stored anywhere. Twenty threshold points cannot resolve a step-like or bounded-below score
distribution.

### 7.4 Confusion and other outputs

* Confusion matrix: the persisted TP/FP/TN/FN of the promoted model at threshold 0.5 on the "test" set. The query
  (`GetExhaustiveSearchInstancePromotedTrialInstanceConfusion.cs`) derives row, column and table ratios.
* Predicted vs actual: raw scores and labels for all "test" rows (Supervised.cs:532-551).
* Error histogram: `actual - predicted` of the stored predicted-vs-actual rows, finite values only, 10 bins by the
  histogram utility used for reporting (`GetExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramQuery.cs:60-79`).
* Sensitivity: 5.4. Correlation to class and multicollinearity: 5.5. Variable prescription: 6.6.
* Not computed: precision, recall as metrics, F1, AUC, KS, Gini, Brier score, calibration curve, cost curves, confidence
  intervals.

### 7.5 Scores are not probabilities

The raw network output described in this section is what a model promoted without a calibrator still returns; a
model promoted with a calibrator has that score mapped to a calibrated probability (see
[Exhaustive Calibration](ExhaustiveCalibration.md)).

The final layer is one neuron with whichever activation function won the search (bipolar sigmoid range [-1, 1], linear
and identity unbounded, threshold function 0/1, sigmoid family [0, 1]). The network is trained with squared error on 0/1
targets, on a class-balanced sample (3.4). There is no calibration step, no Platt scaling or isotonic mapping, and no
prior correction for the removal of majority-class rows, in the raw score itself. The raw output used directly as the
"score" should not be interpreted as a probability on its own terms, and the sigmoid family value on a balanced
training sample over-states the real-world fraud rate.

## 8. Recall

There are two recall implementations. They should be treated as two behaviours.

### 8.1 Model load (sync)

`SyncExhaustiveSearchInstancesExtensions` (`Jube.Engine/EntityAnalysisModelManager/EntityAnalysisModel/Context/Extensions/SyncExhaustiveSearchInstancesExtensions.cs`):
for every active model and every active instance loads the last active promoted trial
(`GetExhaustiveSearchInstancePromotedTrialInstanceByLastActiveQuery`, ordered by Id descending), deserialises the
network JSON (lines 159-160), loads the ordered variable list
(`ORDER BY VariableSequence`, `GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery.cs:61`, excluding rows
marked removed) and stores `Name, ProcessingTypeId, Mean, Sd, NormalisationTypeId` per variable
(lines 178-189). The input vector order is this list order.

### 8.2 In-pipeline adaptation (payload lookup)

`ExhaustiveAdaptationsExtensions.ExecuteExhaustiveAdaptation`
(`Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/ExhaustiveAdaptationsExtensions.cs`):

1. For each instance, allocate `double[NetworkVariablesInOrder.Count]`.
2. Clean name: if the stored name contains `.`, take the second segment (`Abstraction.IsChip` gives `IsChip`)
   (lines ~99-103).
3. Look the value up by `ProcessingTypeId`: 1 Payload, 2 Dictionary, 3 TtlCounter, 4 Sanction, 5 Abstraction,
   6 AbstractionCalculation, any other value (including 7, which training accepts) falls to Abstraction
   (line 107-128).
4. If found: `x' = ZScore(x)` = `(x - mean) / sd` when normalisation type is 2 else `x`
   (`ExhaustiveSearchInstancePromotedTrialInstanceVariable.cs:ZScore`).
5. If absent: `x' = ZScore(Mean)`, i.e. mean imputation. For type 2 this is exactly 0; for types 0 and 1 the raw mean.
6. `TopologyNetwork.Compute(data)[0]` is added to the payload's `ExhaustiveAdaptation` dictionary under the instance
   name. An exception is logged and the entry is simply not added. No clamping, no rounding, no threshold, no
   finiteness check.

### 8.3 HTTP recall endpoint

`POST /api/Invoke/ExhaustiveSearchInstance/{guid}`: `InvokeEndpoints.ExhaustiveSearchInstanceAsync`
(`Jube.App/Endpoints/InvokeEndpoints.cs:173`) then `InvokeService.ExhaustiveSearchInstanceAsync`
(`Jube.Service/Invoke/InvokeService.cs:541-`):

* Body must be a flat JSON object. Input names are matched by exact, case-sensitive key equality against the stored
  variable name including its prefix (`Abstraction.IsChip`) (`ExhaustiveExtensions.GetValueFromJson`, lines 158-169);
  nested objects are not searched.
* Type gate (`RecallBodyHasValidTypes`, InvokeService.cs:512-538): a present key must be integer, finite float, boolean,
  null or a finite numeric string, else HTTP 400. Missing keys are allowed.
* `RecallExhaustive` (`Jube.Engine/Exhaustive/Extensions/ExhaustiveExtensions.cs:24-156`): for each variable in order
  `Convert.ToDouble(token)`, z-score for type 2, absent key gives the value **0 (not the mean, and not standardised)**
  (line 96). Any conversion exception also gives 0 (line 105). Note `Convert.ToDouble(null-valued JToken)`
  behaviour is not confirmed here.
* Output: `Compute(scoreInputs)[0]`; on any exception the method returns 0 (swallowed, lines 142-146); a non-finite
  result is HTTP 500 (InvokeService.cs:603); otherwise the value is returned as JSON, rounded (InvokeService.cs:611).
  For a model promoted with a calibrator the value is the calibrated probability at the base rate
  (see [Exhaustive Calibration](ExhaustiveCalibration.md)).
* Ignored/extra keys in the body are silently ignored. There is no check that the provided keys are the variables the
  model was trained on.

### 8.4 Mirror requirements (what a mirror implementation must reproduce)

To reproduce Exhaustive behaviour exactly: input order is the stored `VariableSequence` order; z-score only when type is
2 with the stored (population, pre-split) mean and sd; missing handling is *different* per entry point (8.2 mean
imputation versus 8.3 zero). A single, documented missing-value policy applied identically in training, in-pipeline
recall and HTTP recall would close this gap (see 10, H11).

## 9. Configuration reference

Read once per instance at `Supervised.TrainAsync` (Supervised.cs:54-75). Defaults from
`Jube.DynamicEnvironment/DynamicEnvironment.cs:199-241`; documented in `docs/Concepts/EnvironmentVariables/index.md`.

| Key | Default | Notes |
|---|---|---|
| ExhaustiveTrialsLimit | 1000 | loop is `i < limit`, so 999 trials |
| ExhaustiveMinVariableCount / MaxVariableCount | 5 / 30 | see 5.1 |
| ExhaustiveTrainingDataSamplePercentage | 0.6 | see 3.3 |
| ExhaustiveCrossValidationDataSamplePercentage | 0.2 | |
| ExhaustiveTestingDataSamplePercentage | 0.2 | |
| ExhaustiveValidationTestingActivationThreshold | 0.5 | used for selection and for confusion |
| ExhaustiveTopologySinceImprovementLimit | 10 | |
| ExhaustiveLayerDepthLimit | 4 | logic inverted, see 6.3 |
| ExhaustiveLayerWidthLimitInputLayerFactor | 4 | |
| ExhaustiveTopologyComplexityLimit | 10000 | measured as neuron count |
| ExhaustiveActivationFunctionExplorationEpochs | 3 | 2 epochs actually run (loop from 1) |
| ExhaustiveTopologyExplorationEpochs | 3 | |
| ExhaustiveTopologyFinalisationEpochs | 20 | |
| ExhaustiveSimulationsCount | 100 | |

Hard-coded: sample cap 10000; anomaly-histogram maxBins 20; ROC 20 threshold steps from 0.05; 10 activation functions;
multicollinearity uses Spearman; 60 s poll.

## 10. Known limitations

Severity is a judgement from reading the code; none was confirmed by execution.

| # | Finding | Evidence | A direction that would close it |
|---|---|---|---|
| H1 (high) | Test set is a subset of training rows | Supervised.cs:1631 vs 1625 | Disjoint, explicitly indexed splits with assertion that the index sets do not intersect; report metrics only on data unseen by selection |
| H2 (high) | Selection and reporting share the same set family: promotion by score on the "test" set, which is also in training; cross validation drives model selection so it is optimistic too | 7.2 | Three-way split: fit, select, report; or nested/stratified k-fold with the report fold untouched |
| H3 (high) | Only every other selected column is populated in training, recall uses all | Supervised.cs:1572-1576 | Build the design matrix from an explicit column map that is serialised with the model and used identically at recall |
| H4 (high) | Variable draw bound is the subset size; biased to early variables, unreachable last variable | Supervised.cs:1644 | Draw without replacement from the full candidate list; seed it; store the seed |
| H5 (high) | Off-by-one `Length - 1` in many loops drops the last row, or the last variable (statistics, normalisation type, correlation, marking removed) | Training.cs:757, 789, 819, 849, 891; Supervised.cs:1075 | Iterate full ranges; unit tests for boundary rows |
| H6 (medium) | Normalisation statistics fit on the whole population before the split (and before positives are appended in filter mode) | Training.cs:244, 463 | Fit on the training partition only, persist, and apply to cv, test and recall; document per split |
| H7 (medium) | Unseeded RNG everywhere (shuffle, symmetry, variable draw, sensitivity, Nguyen-Widrow via the network library's own generator) | Training.cs:48, 1587; Supervised.cs via `seeded` | Single injected seed per training run recorded in the model metadata; deterministic order (sort by stable key) |
| H8 (medium) | No stratification; class balance in slices is only probabilistic; symmetry may under-reduce because duplicates are not de-duplicated in `remove` | 3.3, 3.4 | Stratified split; class-weighting or explicit prior correction instead of discarding majority rows |
| H9 (high) | Scores are not calibrated probabilities; class-balanced sampling distorts base rate; output activation family varies by model | 7.5 | Fit Platt or isotonic calibration on held-out data, then apply the prior shift; store the calibration with the model; report Brier and reliability bands — see [Exhaustive Calibration](ExhaustiveCalibration.md) |
| H10 (medium) | Threshold fixed at 0.5 for selection and confusion; ROC has only 20 coarse points and no AUC; `Score` uses `abs(rho)` so inverted models score well | 7.1, 7.3 | Report AUC (ROC and PR), KS, Gini, precision at fixed recall or alert rate; choose the operating threshold on the selection set by an explicit cost or rate rule; drop the abs |
| H11 (medium) | Two recall paths with different missing-value policy (mean vs raw zero), errors swallowed to 0 or omitted | 8.2, 8.3 | One recall function shared by both entry points; reject or flag missing critical inputs; return the count of imputed inputs |
| H12 (medium) | Missing values in training are 0 and indistinguishable from real zero; no imputation statistics | DataUtility.cs:351-368 | Explicit missing indicator or imputation fitted on train, recorded |
| H13 (medium) | `sd == 0` non-binary constant columns give NaN or infinity at fit and recall | 4.2 | Treat `sd == 0` as type 0 or 1, guard division |
| H14 (medium) | Elimination removes `ElementAt(removedCount)` against a recomputed ranking, so it drifts and can throw; may return an empty variable set | Supervised.cs:1048 | Recursive feature elimination with a fixed, well-defined drop rule (least important first), with tests |
| H15 (medium) | Sensitivity perturbation is `x * U(0.8x, 1.2x)`; promoted sensitivity compares mismatched rows; rank hard-coded to 1 | Performance.cs:167; Supervised.cs:270-273, 489 | Permutation importance on held-out rows with a documented seed, using the same rows for baseline and permuted scores |
| H16 (medium) | Topology growth logic inverted (depth never increases); `Complexity` is neuron count not weights; every topology is retrained from scratch for 3 epochs so scores are noisy | Supervised.cs:741, 789 | Fixed and documented search space; multiple restarts per candidate; early stopping on the selection set rather than a fixed 20 epoch tail |
| H17 (low) | Bin count for histograms computed but not used; dead branch | Training.cs:1018, 1083 | Pass the computed bins; tests |
| H18 (low) | IQR of prescriptions negative (`q1 - q3`) and reverse-z-scored | Supervised.cs:431-434 | Correct definition |
| H19 (low) | Duplicate mock variable, and mock last variable is not loaded | 11.4 | Fix fixture, add fixture consistency test |
| H20 (low) | Sampling is "ORDER BY GUID limit 10000" not random; hard cap of 10000 per class | 2.2 | Explicit seeded reservoir or hash-based sampling, configurable cap, temporal split for time-dependent fraud data (train on earlier, evaluate on later) |
| H21 (medium) | `BatchAccuracy` test is vacuous | BatchAccuracy.cs:98-101 | Replace with an in-process test asserting AUC and accuracy on a genuinely held-out slice, seeded |
