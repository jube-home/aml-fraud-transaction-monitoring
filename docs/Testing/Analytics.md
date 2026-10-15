---
layout: default
title: Analytics Foundations
nav_order: 6
parent: Testing
---

# Analytics Foundations

Tests of the shared analytics methodology used by both Exhaustive and Advanced Analytics, including anomaly handling and
categorical encoding.

## General

### BetaCalibrationTests

`Jube.Test.Analytics.BetaCalibrationTests` — 9 tests.

| Test                                            | Kind | Verifies                                              |
|-------------------------------------------------|------|-------------------------------------------------------|
| `Apply_ImprovesLogLossAndBrierScore`            | Fact | Apply improves log loss and brier score.              |
| `Apply_AlwaysReturnsAProbability`               | Fact | Apply always returns a probability.                   |
| `Fit_ThrowsWhenScoresAreOutsideTheUnitInterval` | Fact | Fit throws when scores are outside the unit interval. |
| `Fit_ThrowsWithOnlyOneClass`                    | Fact | Fit throws with only one class.                       |
| `Fit_SilentlyDropsNaNScoresLikePlatt`           | Fact | Fit silently drops na n scores like platt.            |
| `Fit_ThrowsOnInfiniteScores`                    | Fact | Fit throws on infinite scores.                        |
| `ToPrimitives_FromPrimitives_RoundTrips`        | Fact | To primitives from primitives round trips.            |
| `FromPrimitives_RejectsAnOutOfRangeEpsilon`     | Fact | From primitives rejects an out of range epsilon.      |
| `FromPrimitives_RejectsTheWrongLength`          | Fact | From primitives rejects the wrong length.             |

### BinaryMetricsTests

`Jube.Test.Analytics.BinaryMetricsTests` — 23 tests.

| Test                                                             | Kind | Verifies                                                                 |
|------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `Confusion_KnownAnswers`                                         | Fact | Confusion known answers.                                                 |
| `Confusion_ThresholdIsInclusive`                                 | Fact | Confusion threshold is inclusive.                                        |
| `ConfusionMatrix_DegenerateCountsAreDefined`                     | Fact | Confusion matrix degenerate counts are defined.                          |
| `FBeta_RejectsNonPositiveBeta`                                   | Fact | F beta rejects non positive beta.                                        |
| `RocAuc_WithTies_MatchesIndependentPairCount`                    | Fact | Roc auc with ties matches independent pair count.                        |
| `RocAuc_Extremes`                                                | Fact | Roc auc extremes.                                                        |
| `RocAuc_SingleClassIsNaN`                                        | Fact | Roc auc single class is na n.                                            |
| `RocAuc_IsInvariantToMonotoneTransformAndDropsNaN`               | Fact | Roc auc is invariant to monotone transform and drops na n.               |
| `DeLong_KnownAnswer`                                             | Fact | De long known answer.                                                    |
| `DeLong_TooFewRowsInAClassGivesNaNStandardError`                 | Fact | De long too few rows in a class gives na n standard error.               |
| `RocCurve_GroupsTiesAndEndsAtOneOne`                             | Fact | Roc curve groups ties and ends at one one.                               |
| `RocCurve_SingleClassThrows`                                     | Fact | Roc curve single class throws.                                           |
| `KolmogorovSmirnov_KnownAnswer`                                  | Fact | Kolmogorov smirnov known answer.                                         |
| `AveragePrecision_KnownAnswer_AndPerfectRanking`                 | Fact | Average precision known answer and perfect ranking.                      |
| `LogLossAndBrier_KnownAnswers`                                   | Fact | Log loss and brier known answers.                                        |
| `LogLossAndBrier_WeightsMatchIndependentValuesAndDuplicatedRows` | Fact | Log loss and brier weights match independent values and duplicated rows. |
| `LogLoss_ExtremeProbabilitiesAreClippedNotInfinite`              | Fact | Log loss extreme probabilities are clipped not infinite.                 |
| `Reliability_BinsAndExpectedCalibrationError_KnownAnswers`       | Fact | Reliability bins and expected calibration error known answers.           |
| `Reliability_EmptyBinsAreNaN_AndProbabilityOneLandsInLastBin`    | Fact | Reliability empty bins are na n and probability one lands in last bin.   |
| `PerfectlyCalibratedSet_HasZeroCalibrationError`                 | Fact | Perfectly calibrated set has zero calibration error.                     |
| `InvalidInputs_ThrowStandardExceptions`                          | Fact | Invalid inputs throw standard exceptions.                                |
| `NaNScoresAreDroppedFromEveryMetric`                             | Fact | Na n scores are dropped from every metric.                               |
| `LargeRandomSet_RankAucMatchesPairwiseDefinition`                | Fact | Large random set rank auc matches pairwise definition.                   |

### BivariateCopulaTests

`Jube.Test.Analytics.BivariateCopulaTests` — 7 tests.

| Test                                                         | Kind   | Verifies                                                            |
|--------------------------------------------------------------|--------|---------------------------------------------------------------------|
| `GaussianCopula_RecoversItsOwnParameterFromSimulatedData`    | Theory | Gaussian copula recovers its own parameter from simulated data.     |
| `ClaytonCopula_RecoversItsOwnParameterFromSimulatedData`     | Theory | Clayton copula recovers its own parameter from simulated data.      |
| `GumbelCopula_RecoversItsOwnParameterFromSimulatedData`      | Theory | Gumbel copula recovers its own parameter from simulated data.       |
| `FrankCopula_RecoversItsOwnParameterFromSimulatedData`       | Theory | Frank copula recovers its own parameter from simulated data.        |
| `BivariateCopulaFitter_PicksGaussianForGaussianData`         | Fact   | Bivariate copula fitter picks gaussian for gaussian data.           |
| `BivariateCopulaFitter_PicksClaytonForClaytonData`           | Fact   | Bivariate copula fitter picks clayton for clayton data.             |
| `InverseConditionalCdf_IsAConsistentInverseOfConditionalCdf` | Fact   | Inverse conditional cdf is a consistent inverse of conditional cdf. |

### DVineCopulaTests

`Jube.Test.Analytics.DVineCopulaTests` — 4 tests.

| Test                                                           | Kind | Verifies                                                             |
|----------------------------------------------------------------|------|----------------------------------------------------------------------|
| `Sample_RecoversPairwiseCorrelationsOfAKnownGaussianChain`     | Fact | Sample recovers pairwise correlations of a known gaussian chain.     |
| `Sample_ReproducesEachColumnsMarginalMeanAndStandardDeviation` | Fact | Sample reproduces each columns marginal mean and standard deviation. |
| `Fit_TruncatedAtTreeOne_StillProducesAValidSample`             | Fact | Fit truncated at tree one still produces a valid sample.             |
| `Sample_TwoVariables_RecoversTheSingleCorrelation`             | Fact | Sample two variables recovers the single correlation.                |

### DataSplitTests

`Jube.Test.Analytics.DataSplitTests` — 9 tests.

| Test                                                  | Kind   | Verifies                                                   |
|-------------------------------------------------------|--------|------------------------------------------------------------|
| `Split_SetsAreDisjointAndInRange`                     | Theory | Split sets are disjoint and in range.                      |
| `Split_KnownAnswerSizes_OnExactFractions`             | Fact   | Split known answer sizes on exact fractions.               |
| `Split_SameSeedIsReproducible_DifferentSeedDiffers`   | Fact   | Split same seed is reproducible different seed differs.    |
| `Split_Stratified_PreservesClassProportionInEverySet` | Fact   | Split stratified preserves class proportion in every set.  |
| `Split_Empty_ReturnsEmptySets`                        | Fact   | Split empty returns empty sets.                            |
| `Split_SingleRow_NeverDuplicatesTheRow`               | Fact   | Split single row never duplicates the row.                 |
| `Split_InvalidFractions_Throw`                        | Theory | Split invalid fractions throw.                             |
| `Split_NullLabels_Throws`                             | Fact   | Split null labels throws.                                  |
| `Take_GathersRowsInIndexOrder_AndRejectsOutOfRange`   | Fact   | Take gathers rows in index order and rejects out of range. |

### IsotonicCalibrationTests

`Jube.Test.Analytics.IsotonicCalibrationTests` — 9 tests.

| Test                                                  | Kind | Verifies                                                     |
|-------------------------------------------------------|------|--------------------------------------------------------------|
| `Apply_IsNonDecreasingAcrossTheFittedRange`           | Fact | Apply is non decreasing across the fitted range.             |
| `Apply_ImprovesLogLossAndBrierScore`                  | Fact | Apply improves log loss and brier score.                     |
| `Apply_ClampsToTheBoundaryOutsideTheFittedRange`      | Fact | Apply clamps to the boundary outside the fitted range.       |
| `Fit_ThrowsWhenAllScoresAreEqual`                     | Fact | Fit throws when all scores are equal.                        |
| `Fit_WithOnlyOneClassProducesANearConstantCalibrator` | Fact | Fit with only one class produces a near constant calibrator. |
| `Fit_ThrowsOnNonFiniteScores`                         | Fact | Fit throws on non finite scores.                             |
| `ToJson_FromJson_RoundTripsTheFittedFunction`         | Fact | To json from json round trips the fitted function.           |
| `FromState_RejectsNonIncreasingX`                     | Fact | From state rejects non increasing x.                         |
| `FromState_RejectsAnOutOfRangeY`                      | Fact | From state rejects an out of range y.                        |

### PlattCalibrationTests

`Jube.Test.Analytics.PlattCalibrationTests` — 18 tests.

| Test                                                             | Kind | Verifies                                                               |
|------------------------------------------------------------------|------|------------------------------------------------------------------------|
| `Fit_RawScore_MatchesIndependentNewton`                          | Fact | Fit raw score matches independent newton.                              |
| `Fit_LogitMode_MatchesIndependentNewton`                         | Fact | Fit logit mode matches independent newton.                             |
| `Fit_MiscalibratedSet_MatchesIndependentNewtonInBothModes`       | Fact | Fit miscalibrated set matches independent newton in both modes.        |
| `Apply_MiscalibratedSet_ImprovesLogLossBrierAndCalibrationError` | Fact | Apply miscalibrated set improves log loss brier and calibration error. |
| `Apply_IsStrictlyMonotoneAndPreservesAuc`                        | Fact | Apply is strictly monotone and preserves auc.                          |
| `Fit_InvertedScores_GivesNegativeSlopeThatIsStillMonotone`       | Fact | Fit inverted scores gives negative slope that is still monotone.       |
| `Fit_DuplicatedRowEqualsWeightTwo`                               | Fact | Fit duplicated row equals weight two.                                  |
| `Fit_ZeroWeightRowIsIgnored`                                     | Fact | Fit zero weight row is ignored.                                        |
| `Fit_IsDeterministicAndIgnoresNaNScores`                         | Fact | Fit is deterministic and ignores na n scores.                          |
| `Fit_ExtremeScoresStayFinite`                                    | Fact | Fit extreme scores stay finite.                                        |
| `Fit_SeparableDataStillConvergesBecauseOfSmoothedTargets`        | Fact | Fit separable data still converges because of smoothed targets.        |
| `Fit_SingleClass_Throws`                                         | Fact | Fit single class throws.                                               |
| `Fit_ConstantScores_Throws`                                      | Fact | Fit constant scores throws.                                            |
| `Fit_InvalidArguments_ThrowStandardExceptions`                   | Fact | Fit invalid arguments throw standard exceptions.                       |
| `Fit_HonoursCancellation`                                        | Fact | Fit honours cancellation.                                              |
| `Calibrator_SnapshotsToPrimitivesAndBack`                        | Fact | Calibrator snapshots to primitives and back.                           |
| `Calibrator_FromPrimitives_RejectsBadSnapshots`                  | Fact | Calibrator from primitives rejects bad snapshots.                      |
| `Calibrator_NaNInGivesNaNOut_AndLogitModeClampsOutOfRange`       | Fact | Calibrator na n in gives na n out and logit mode clamps out of range.  |

### PreprocessingTests

`Jube.Test.Analytics.PreprocessingTests` — 15 tests.

| Test                                                   | Kind | Verifies                                                       |
|--------------------------------------------------------|------|----------------------------------------------------------------|
| `ZScore_FitIgnoresNaNAndMatchesIndependentValues`      | Fact | Z score fit ignores na n and matches independent values.       |
| `ZScore_ConstantColumnMapsToZero_SingleValueHasZeroSd` | Fact | Z score constant column maps to zero single value has zero sd. |
| `ZScore_IsFitOnTrainingOnlyAndReappliedUnchanged`      | Fact | Z score is fit on training only and reapplied unchanged.       |
| `MinMax_KnownAnswers_ClipAndConstant`                  | Fact | Min max known answers clip and constant.                       |
| `Robust_KnownAnswers`                                  | Fact | Robust known answers.                                          |
| `Robust_OutlierDoesNotMoveTheFit`                      | Fact | Robust outlier does not move the fit.                          |
| `Robust_ZeroMadMapsToZero`                             | Fact | Robust zero mad maps to zero.                                  |
| `Winsoriser_QuantilesAndClamp`                         | Fact | Winsoriser quantiles and clamp.                                |
| `Winsoriser_InvalidArgumentsThrow`                     | Fact | Winsoriser invalid arguments throw.                            |
| `Imputer_MeanMedianModeConstant_KnownAnswers`          | Fact | Imputer mean median mode constant known answers.               |
| `Imputer_ModeTiesResolveToSmallest`                    | Fact | Imputer mode ties resolve to smallest.                         |
| `Imputer_ReplacesNaNAndInfinityAndFlagsThem`           | Fact | Imputer replaces na n and infinity and flags them.             |
| `Imputer_ConstantNeedsNoData_OthersNeedFiniteValues`   | Fact | Imputer constant needs no data others need finite values.      |
| `FitOnEmptyOrAllMissing_ThrowsForEveryScaler`          | Fact | Fit on empty or all missing throws for every scaler.           |
| `Pipeline_ImputeThenScale_MatchesResolveForTheMean`    | Fact | Pipeline impute then scale matches resolve for the mean.       |

### PriorShiftTests

`Jube.Test.Analytics.PriorShiftTests` — 6 tests.

| Test                                                      | Kind   | Verifies                                                        |
|-----------------------------------------------------------|--------|-----------------------------------------------------------------|
| `Apply_KnownAnswers`                                      | Fact   | Apply known answers.                                            |
| `Apply_EqualPriorsIsIdentity_AndEndpointsAreFixed`        | Fact   | Apply equal priors is identity and endpoints are fixed.         |
| `Apply_IsStrictlyMonotone`                                | Fact   | Apply is strictly monotone.                                     |
| `Apply_InvalidPriors_Throw`                               | Theory | Apply invalid priors throw.                                     |
| `Apply_ProbabilityOutOfRange_Throws`                      | Fact   | Apply probability out of range throws.                          |
| `SyntheticPopulation_BalancedFitCorrectedToKnownBaseRate` | Fact   | Synthetic population balanced fit corrected to known base rate. |

### RegressionAndThresholdTests

`Jube.Test.Analytics.RegressionAndThresholdTests` — 9 tests.

| Test                                                                   | Kind | Verifies                                                                     |
|------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `Regression_KnownAnswers`                                              | Fact | Regression known answers.                                                    |
| `Regression_PerfectAndNaNAndZeroCases`                                 | Fact | Regression perfect and na n and zero cases.                                  |
| `Regression_NaNPairsAreDropped`                                        | Fact | Regression na n pairs are dropped.                                           |
| `Regression_InvalidInputsThrow`                                        | Fact | Regression invalid inputs throw.                                             |
| `MinimumRiskThreshold_KnownAnswers`                                    | Fact | Minimum risk threshold known answers.                                        |
| `MinimumRiskThreshold_IsWhereExpectedCostsAreEqual`                    | Fact | Minimum risk threshold is where expected costs are equal.                    |
| `MinimumRiskThreshold_InvalidCostsThrow`                               | Fact | Minimum risk threshold invalid costs throw.                                  |
| `MinimumRiskThreshold_MinimisesEmpiricalCostOnCalibratedProbabilities` | Fact | Minimum risk threshold minimises empirical cost on calibrated probabilities. |
| `ExpectedCost_KnownAnswer`                                             | Fact | Expected cost known answer.                                                  |

### SplitterTests

`Jube.Test.Analytics.SplitterTests` — 15 tests.

| Test                                                      | Kind | Verifies                                                         |
|-----------------------------------------------------------|------|------------------------------------------------------------------|
| `ForwardChaining_KnownAnswer`                             | Fact | Forward chaining known answer.                                   |
| `ForwardChaining_PreservesTimeOrderOnShuffledInput`       | Fact | Forward chaining preserves time order on shuffled input.         |
| `ForwardChaining_GapLeavesRowsBetweenTrainAndTest`        | Fact | Forward chaining gap leaves rows between train and test.         |
| `ForwardChaining_InvalidArgumentsThrow`                   | Fact | Forward chaining invalid arguments throw.                        |
| `GroupKFold_NoEntityInTwoFolds_AndIsAPartition`           | Fact | Group k fold no entity in two folds and is a partition.          |
| `GroupKFold_FoldSizesAreReasonablyBalanced`               | Fact | Group k fold fold sizes are reasonably balanced.                 |
| `GroupKFold_DeterministicBySeed_AndSeedChangesAssignment` | Fact | Group k fold deterministic by seed and seed changes assignment.  |
| `GroupKFold_InvalidArgumentsThrow`                        | Fact | Group k fold invalid arguments throw.                            |
| `GroupSplit_TrainValidationTest_NeverSharesAnEntity`      | Fact | Group split train validation test never shares an entity.        |
| `GroupSplit_DeterministicBySeed_AndValidatesFractions`    | Fact | Group split deterministic by seed and validates fractions.       |
| `StratifiedKFold_PartitionsAndPreservesClassRatioPerFold` | Fact | Stratified k fold partitions and preserves class ratio per fold. |
| `StratifiedKFold_UnevenClassesStayBalancedWithinOne`      | Fact | Stratified k fold uneven classes stay balanced within one.       |
| `KFold_SeededDeterministicAndPartitions`                  | Fact | K fold seeded deterministic and partitions.                      |
| `KFold_InvalidArgumentsThrow`                             | Fact | K fold invalid arguments throw.                                  |
| `LeakageGuard_DeliberatelyLeakedIndexIsDetected`          | Fact | Leakage guard deliberately leaked index is detected.             |

### StandardisationTests

`Jube.Test.Analytics.StandardisationTests` — 6 tests.

| Test                                                    | Kind   | Verifies                                                       |
|---------------------------------------------------------|--------|----------------------------------------------------------------|
| `ZScore_KnownAnswers`                                   | Theory | Z score known answers.                                         |
| `Resolve_PresentValue_IsStandardisedAndNotImputed`      | Fact   | Resolve present value is standardised and not imputed.         |
| `Resolve_PresentValue_IsUntouchedWhenNotStandardised`   | Fact   | Resolve present value is untouched when not standardised.      |
| `Resolve_Missing_UsesTheMeanSoAStandardisedInputIsZero` | Fact   | Resolve missing uses the mean so a standardised input is zero. |
| `Resolve_Missing_UsesTheRawMeanWhenNotStandardised`     | Fact   | Resolve missing uses the raw mean when not standardised.       |
| `Resolve_NanIsTreatedAsMissing`                         | Fact   | Resolve nan is treated as missing.                             |

## Anomaly

### AnomalyAutoSelectorTests

`Jube.Test.Analytics.Anomaly.AnomalyAutoSelectorTests` — 7 tests.

| Test                                                          | Kind | Verifies                                                              |
|---------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `LabelledSelectionPicksTheDetectorThatFindsTheKnownPositives` | Fact | Labelled selection picks the detector that finds the known positives. |
| `SelectionIsDeterministicAndRecordsEveryCandidate`            | Fact | Selection is deterministic and records every candidate.               |
| `TheGuardRejectsDegenerateAndFailingDetectors`                | Fact | The guard rejects degenerate and failing detectors.                   |
| `NothingSelectedWhenEveryCandidateIsRejected`                 | Fact | Nothing selected when every candidate is rejected.                    |
| `TheTimeBudgetSkipsCandidatesNotReached`                      | Fact | The time budget skips candidates not reached.                         |
| `CancellationIsPropagatedNotRecordedAsATimeout`               | Fact | Cancellation is propagated not recorded as a timeout.                 |
| `TheMassCurvesPreferATightLevelSet`                           | Fact | The mass curves prefer a tight level set.                             |

### AnomalyDetectorTests

`Jube.Test.Analytics.Anomaly.AnomalyDetectorTests` — 23 tests.

| Test                                                               | Kind   | Verifies                                                                    |
|--------------------------------------------------------------------|--------|-----------------------------------------------------------------------------|
| `PlantedAnomaliesAreRankedAboveNormalRowsAcrossSeeds`              | Theory | Planted anomalies are ranked above normal rows across seeds.                |
| `ScoresAreDeterministicBySeed`                                     | Theory | Scores are deterministic by seed.                                           |
| `DifferentSeedsChangeARandomisedDetector`                          | Fact   | Different seeds change a randomised detector.                               |
| `ScoringIsThreadSafe`                                              | Theory | Scoring is thread safe.                                                     |
| `NonFiniteValuesAreImputedInFitAndScore`                           | Theory | Non finite values are imputed in fit and score.                             |
| `ACancelledTokenStopsTheFit`                                       | Theory | A cancelled token stops the fit.                                            |
| `ACancelledTokenStopsScoring`                                      | Fact   | A cancelled token stops scoring.                                            |
| `DegenerateInputsThrowClearExceptions`                             | Theory | Degenerate inputs throw clear exceptions.                                   |
| `ScoringARowOfTheWrongWidthIsRejected`                             | Fact   | Scoring a row of the wrong width is rejected.                               |
| `UnregularisedMahalanobisRejectsMoreVariablesThanRows`             | Fact   | Unregularised mahalanobis rejects more variables than rows.                 |
| `ShrinkageMakesMahalanobisUsableWhenVariablesExceedRows`           | Fact   | Shrinkage makes mahalanobis usable when variables exceed rows.              |
| `GaussianMixtureRejectsTooFewRowsForItsComponents`                 | Fact   | Gaussian mixture rejects too few rows for its components.                   |
| `MahalanobisBeatsMarginalDetectorsWhenTheCorrelationIsBroken`      | Fact   | Mahalanobis beats marginal detectors when the correlation is broken.        |
| `IsolationForestPathLengthMatchesTheReferenceDefinition`           | Fact   | Isolation forest path length matches the reference definition.              |
| `HbosMatchesTheReferenceDefinition`                                | Fact   | Hbos matches the reference definition.                                      |
| `MahalanobisMatchesTheReferenceDefinition`                         | Theory | Mahalanobis matches the reference definition.                               |
| `LedoitWolfIntensityMatchesTheReferenceDefinition`                 | Fact   | Ledoit wolf intensity matches the reference definition.                     |
| `LocalOutlierFactorMatchesTheReferenceDefinition`                  | Fact   | Local outlier factor matches the reference definition.                      |
| `KnnDistanceMatchesTheReferenceDefinition`                         | Fact   | Knn distance matches the reference definition.                              |
| `KnnAndLofUseARowCapDeterministically`                             | Fact   | Knn and lof use a row cap deterministically.                                |
| `PcaRetainsFewerComponentsThanVariablesAndScoresOffSubspacePoints` | Fact   | Pca retains fewer components than variables and scores off subspace points. |
| `FeatureBaggingDrawsDifferentSubspacesPerMemberAndIsReproducible`  | Fact   | Feature bagging draws different subspaces per member and is reproducible.   |
| `EnsembleCombinationsAllRankPlantedAnomaliesHigh`                  | Theory | Ensemble combinations all rank planted anomalies high.                      |

### AnomalyReportAndParametersTests

`Jube.Test.Analytics.Anomaly.AnomalyReportAndParametersTests` — 8 tests.

| Test                                                       | Kind   | Verifies                                                           |
|------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `ContaminationFlagsTheTopFractionAndRecordsWhatWasApplied` | Fact   | Contamination flags the top fraction and records what was applied. |
| `ContaminationOutsideTheRangeIsRejected`                   | Theory | Contamination outside the range is rejected.                       |
| `TiedScoresFlagFewerRatherThanMoreThanTheTopFraction`      | Fact   | Tied scores flag fewer rather than more than the top fraction.     |
| `TheReportRoundTripsThroughJsonAndSummarises`              | Fact   | The report round trips through json and summarises.                |
| `ParametersParseWithDefaultsAndRejectUnknownProperties`    | Fact   | Parameters parse with defaults and reject unknown properties.      |
| `OutOfBoundsParametersAreReported`                         | Theory | Out of bounds parameters are reported.                             |
| `OversizedAndNumericEnumParametersAreRejected`             | Fact   | Oversized and numeric enum parameters are rejected.                |
| `FactoryBuildsEveryNativeAlgorithmAndRefusesTheOthers`     | Fact   | Factory builds every native algorithm and refuses the others.      |

### AnomalyVariableReducerTests

`Jube.Test.Analytics.Anomaly.AnomalyVariableReducerTests` — 17 tests.

| Test                                                                  | Kind | Verifies                                                                      |
|-----------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `ConstantAndNearZeroVarianceVariablesAreDropped`                      | Fact | Constant and near zero variance variables are dropped.                        |
| `AFrequentlyRepeatedButVariedVariableIsNotNearZeroVariance`           | Fact | A frequently repeated but varied variable is not near zero variance.          |
| `VariablesAboveTheMissingRateAreDropped`                              | Fact | Variables above the missing rate are dropped.                                 |
| `ExactDuplicatesAndCorrelatedGroupsKeepOneRepresentative`             | Fact | Exact duplicates and correlated groups keep one representative.               |
| `TheCorrelationThresholdIsConfigurable`                               | Fact | The correlation threshold is configurable.                                    |
| `CompleteLinkageDoesNotChainWeaklyCorrelatedVariables`                | Fact | Complete linkage does not chain weakly correlated variables.                  |
| `VariableClusteringKeepsARepresentativePerCluster`                    | Fact | Variable clustering keeps a representative per cluster.                       |
| `ZeroMedianAbsoluteDeviationCanBeDroppedOnRequest`                    | Fact | Zero median absolute deviation can be dropped on request.                     |
| `RobustScalingUsesMedianAndMadAndFallsBackForBinaryColumns`           | Fact | Robust scaling uses median and mad and falls back for binary columns.         |
| `ZScoreScalingGivesUnitVariance`                                      | Fact | Z score scaling gives unit variance.                                          |
| `PcaProjectionChoosesComponentsByExplainedVarianceOrParallelAnalysis` | Fact | Pca projection chooses components by explained variance or parallel analysis. |
| `ReductionIsDeterministicBySeed`                                      | Fact | Reduction is deterministic by seed.                                           |
| `RunningWithoutReductionKeepsEveryVariableUntouched`                  | Fact | Running without reduction keeps every variable untouched.                     |
| `MissingValuesAreImputedWithTheFittedMedianOnTransform`               | Fact | Missing values are imputed with the fitted median on transform.               |
| `ReducingEveryVariableAwayIsAClearError`                              | Fact | Reducing every variable away is a clear error.                                |
| `InvalidOptionsAndShapesAreRejected`                                  | Fact | Invalid options and shapes are rejected.                                      |
| `EveryVariableHasADecisionWithAReason`                                | Fact | Every variable has a decision with a reason.                                  |

## Categorical

### CategoricalConceptTests

`Jube.Test.Analytics.Categorical.CategoricalConceptTests` — 3 tests. Category: Unit.

| Test                                                              | Kind | Verifies                                                                       |
|-------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `EncodedCategoryBeatsTheStringAsZeroBaselineOnHeldOutData`        | Fact | Encoded category beats the string as zero baseline on held out data.           |
| `TheSameStringsNoisedAtRecallGiveTheSameScores`                   | Fact | The same strings noised at recall give the same scores.                        |
| `ALevelThatOnlyAppearsAtRecallDoesNotMoveOtherRowsAndStaysFinite` | Fact | A level that only appears at recall does not move other rows and stays finite. |

### CategoricalFieldEncoderTests

`Jube.Test.Analytics.Categorical.CategoricalFieldEncoderTests` — 27 tests.

| Test                                                             | Kind   | Verifies                                                                  |
|------------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `Fit_RanksByFrequencyAndUsesTheMostFrequentAsReference`          | Fact   | Fit ranks by frequency and uses the most frequent as reference.           |
| `Fit_CountsAreExact`                                             | Fact   | Fit counts are exact.                                                     |
| `Apply_KnownLevelSetsExactlyOneIndicator`                        | Fact   | Apply known level sets exactly one indicator.                             |
| `Apply_ReferenceLevelIsAllZero`                                  | Fact   | Apply reference level is all zero.                                        |
| `Apply_UnseenAndPooledLevelsMapToOther`                          | Fact   | Apply unseen and pooled levels map to other.                              |
| `Apply_MissingMapsToTheMissingIndicator`                         | Theory | Apply missing maps to the missing indicator.                              |
| `Apply_WithoutOtherOrMissingIndicatorUnseenAndMissingAreAllZero` | Fact   | Apply without other or missing indicator unseen and missing are all zero. |
| `Indicator_MatchesEncodeForEveryIndex`                           | Fact   | Indicator matches encode for every index.                                 |
| `Apply_IsAtMostOneHot`                                           | Fact   | Apply is at most one hot.                                                 |
| `Apply_NormalisationVariantsGiveTheSameIndicators`               | Fact   | Apply normalisation variants give the same indicators.                    |
| `Fit_MaxLevelsPoolsTheTail`                                      | Fact   | Fit max levels pools the tail.                                            |
| `Fit_MinimumFrequencyPools`                                      | Fact   | Fit minimum frequency pools.                                              |
| `Fit_MinimumFractionRaisesTheMinimum`                            | Fact   | Fit minimum fraction raises the minimum.                                  |
| `Fit_CoverageTargetStopsEarly`                                   | Fact   | Fit coverage target stops early.                                          |
| `Fit_TiesAreBrokenByOrdinalKeyOrder`                             | Fact   | Fit ties are broken by ordinal key order.                                 |
| `Fit_ConstantFieldProducesNoEncoder`                             | Fact   | Fit constant field produces no encoder.                                   |
| `Fit_NoValuesProducesNoEncoder`                                  | Fact   | Fit no values produces no encoder.                                        |
| `Fit_SingleLevelWithEnoughMissingKeepsTheMissingIndicator`       | Fact   | Fit single level with enough missing keeps the missing indicator.         |
| `Fit_LabelIsTheMostFrequentSpelling`                             | Fact   | Fit label is the most frequent spelling.                                  |
| `Fit_IsDeterministicAndOrderIndependent`                         | Fact   | Fit is deterministic and order independent.                               |
| `Counter_CapsTheDistinctKeysAndCountsTheOverflow`                | Fact   | Counter caps the distinct keys and counts the overflow.                   |
| `Serialisation_RoundTripsToIdenticalBehaviour`                   | Fact   | Serialisation round trips to identical behaviour.                         |
| `Serialisation_HostileLabelsSurviveARoundTrip`                   | Fact   | Serialisation hostile labels survive a round trip.                        |
| `FromJson_RejectsTamperedOrInvalidState`                         | Theory | From json rejects tampered or invalid state.                              |
| `FromJson_RejectsUnknownVersions`                                | Fact   | From json rejects unknown versions.                                       |
| `Encode_ShortDestinationThrows`                                  | Fact   | Encode short destination throws.                                          |
| `Resolve_HostileInputNeverThrows`                                | Fact   | Resolve hostile input never throws.                                       |

### CategoricalNormaliserTests

`Jube.Test.Analytics.Categorical.CategoricalNormaliserTests` — 8 tests.

| Test                                           | Kind   | Verifies                                             |
|------------------------------------------------|--------|------------------------------------------------------|
| `Key_NormalisesCaseWhitespaceAndUnicode`       | Theory | Key normalises case whitespace and unicode.          |
| `Key_MissingShapesReturnNull`                  | Theory | Key missing shapes return null.                      |
| `Key_IsCultureInvariant`                       | Fact   | Key is culture invariant.                            |
| `Key_HostileStrings_NeverThrowAndAreBounded`   | Fact   | Key hostile strings never throw and are bounded.     |
| `Key_LongStringsSharingAPrefixCollideByDesign` | Fact   | Key long strings sharing a prefix collide by design. |
| `Key_IsIdempotentOnRandomStrings`              | Fact   | Key is idempotent on random strings.                 |
| `Label_KeepsCaseButRemovesControlCharacters`   | Fact   | Label keeps case but removes control characters.     |
| `ToRaw_UsesTheInvariantCulture`                | Fact   | To raw uses the invariant culture.                   |

### CategoricalPlannerTests

`Jube.Test.Analytics.Categorical.CategoricalPlannerTests` — 9 tests.

| Test                                                            | Kind | Verifies                                                                |
|-----------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `DeclaredStringIsCategorical`                                   | Fact | Declared string is categorical.                                         |
| `DeclaredNumericIsNotCategoricalUnlessIncluded`                 | Fact | Declared numeric is not categorical unless included.                    |
| `UndeclaredTypeIsInferredFromTheSample`                         | Fact | Undeclared type is inferred from the sample.                            |
| `IdentifierLikeFieldsAreSkippedAndReported`                     | Fact | Identifier like fields are skipped and reported.                        |
| `ExcludedFieldsAreSkipped`                                      | Fact | Excluded fields are skipped.                                            |
| `ConstantAndEmptyFieldsAreSkipped`                              | Fact | Constant and empty fields are skipped.                                  |
| `TotalIndicatorBudgetRemovesTheLeastFrequentLevelsAcrossFields` | Fact | Total indicator budget removes the least frequent levels across fields. |
| `TotalIndicatorBudgetIsDeterministic`                           | Fact | Total indicator budget is deterministic.                                |
| `OptionsRoundTripAndSanitise`                                   | Fact | Options round trip and sanitise.                                        |
