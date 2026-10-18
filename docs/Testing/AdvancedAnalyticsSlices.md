---
layout: default
title: Advanced Analytics Slices
nav_order: 3
parent: Testing
---

# Advanced Analytics Slices

Tests of the Advanced Analytics slice pipeline, which prepares, validates and transforms datasets ahead of statistical
and machine-learning work.

## General

### AdaBoostClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.AdaBoostClassifierExplainToAdaptationTests` — 6 tests.

| Test                                                                                    | Kind | Verifies                                                                                            |
|-----------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                              | Fact | To adaptation is assignable to the shared explanation interface.                                    |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                     | Fact | To adaptation carries the value and model name and family.                                          |
| `ToAdaptation_ContributionIsInexactAndInDecisionFunctionSpaceWithTheWeightedVoteMethod` | Fact | To adaptation contribution is inexact and in decision function space with the weighted vote method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpace`            | Fact | To adaptation describes calibration only when calibrated and uses probability space.                |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                                 | Fact | To adaptation maps source from processing type ids when given.                                      |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                         | Fact | To adaptation leaves source null when processing type ids are not given.                            |

### AdaBoostClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.AdaBoostClassifierRecallTests` — 12 tests.

| Test                                                                     | Kind | Verifies                                                                          |
|--------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `SeparableDataProducesHighProbabilityForPositivesAndLowForNegatives`     | Fact | Separable data produces high probability for positives and low for negatives.     |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`               | Fact | Same seed produces the same snapshot and the same recall bit for bit.             |
| `ADifferentSeedCanProduceADifferentSnapshot`                             | Fact | A different seed can produce a different snapshot.                                |
| `PredictedProbabilitiesAreAlwaysBetweenZeroAndOne`                       | Fact | Predicted probabilities are always between zero and one.                          |
| `RecallRejectsARowOfTheWrongLength`                                      | Fact | Recall rejects a row of the wrong length.                                         |
| `AMissingInputIsImputedWithTheTrainingMedian`                            | Fact | A missing input is imputed with the training median.                              |
| `RequiringBothClassesThrowsAClearExceptionWhenOnlyOneIsPresent`          | Fact | Requiring both classes throws a clear exception when only one is present.         |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                       | Fact | The snapshot round trips through the envelope unchanged.                          |
| `ATamperedOrForeignSnapshotIsRefused`                                    | Fact | A tampered or foreign snapshot is refused.                                        |
| `TheEvidenceReportsPlausibleEnsembleSummariesAndFiniteNonNegativeAlphas` | Fact | The evidence reports plausible ensemble summaries and finite non negative alphas. |
| `ASingleRoundFitsExactlyOneTreeWithAFiniteNonNegativeAlpha`              | Fact | A single round fits exactly one tree with a finite non negative alpha.            |
| `ExplainRawScoreMatchesThePipelineRawScore`                              | Fact | Explain raw score matches the pipeline raw score.                                 |

### AdvancedAnalyticsRecallExtensionsTests

`Jube.Test.AdvancedAnalytics.Slice.AdvancedAnalyticsRecallExtensionsTests` — 5 tests. Category: Unit.

| Test                                                                | Kind   | Verifies                                                                    |
|---------------------------------------------------------------------|--------|-----------------------------------------------------------------------------|
| `BuildScoreInputs_ReadsEachVariableByNameInOrder`                   | Fact   | Build score inputs reads each variable by name in order.                    |
| `BuildScoreInputs_UsesNanForAMissingVariable`                       | Fact   | Build score inputs uses nan for a missing variable.                         |
| `BuildScoreInputs_UsesNanForAnUnparseableOrNullValue`               | Fact   | Build score inputs uses nan for an unparseable or null value.               |
| `BuildScoreInputs_ParsesBooleansAndIntegerTokensLikeExhaustiveDoes` | Fact   | Build score inputs parses booleans and integer tokens like exhaustive does. |
| `RecallBodyHasValidTypesForAdvancedAnalytics_Boundaries`            | Theory | Recall body has valid types for advanced analytics boundaries.              |

### BaggingClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.BaggingClassifierExplainToAdaptationTests` — 7 tests.

| Test                                                                                        | Kind | Verifies                                                                                                 |
|---------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                                  | Fact | To adaptation is assignable to the shared explanation interface.                                         |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                         | Fact | To adaptation carries the value and model name and family.                                               |
| `ToAdaptation_ContributionIsInexactAndInVoteFractionSpaceWithTheTreePathContributionMethod` | Fact | To adaptation contribution is inexact and in vote fraction space with the tree path contribution method. |
| `ToAdaptation_TheDiscountedContributionsDoNotSumBackToRawScoreMinusBaseValue`               | Fact | To adaptation the discounted contributions do not sum back to raw score minus base value.                |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpace`                | Fact | To adaptation describes calibration only when calibrated and uses probability space.                     |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                                     | Fact | To adaptation maps source from processing type ids when given.                                           |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                             | Fact | To adaptation leaves source null when processing type ids are not given.                                 |

### BaggingClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.BaggingClassifierRecallTests` — 12 tests.

| Test                                                                   | Kind | Verifies                                                                        |
|------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `SeparableDataProducesHighProbabilityForPositivesAndLowForNegatives`   | Fact | Separable data produces high probability for positives and low for negatives.   |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`             | Fact | Same seed produces the same snapshot and the same recall bit for bit.           |
| `ADifferentSeedCanProduceADifferentSnapshot`                           | Fact | A different seed can produce a different snapshot.                              |
| `PredictedProbabilitiesAreAlwaysBetweenZeroAndOne`                     | Fact | Predicted probabilities are always between zero and one.                        |
| `RecallRejectsARowOfTheWrongLength`                                    | Fact | Recall rejects a row of the wrong length.                                       |
| `AMissingInputIsImputedWithTheTrainingMedian`                          | Fact | A missing input is imputed with the training median.                            |
| `RequiringBothClassesThrowsAClearExceptionWhenOnlyOneIsPresent`        | Fact | Requiring both classes throws a clear exception when only one is present.       |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                     | Fact | The snapshot round trips through the envelope unchanged.                        |
| `ATamperedOrForeignSnapshotIsRefused`                                  | Fact | A tampered or foreign snapshot is refused.                                      |
| `TheEvidenceReportsPlausibleEnsembleSummaries`                         | Fact | The evidence reports plausible ensemble summaries.                              |
| `CalibrateFalseLeavesTheModelUncalibratedAndCalibrateTrueCalibratesIt` | Fact | Calibrate false leaves the model uncalibrated and calibrate true calibrates it. |
| `ExplainReportsAContributionPerVariableAndAPlausibleBaseValue`         | Fact | Explain reports a contribution per variable and a plausible base value.         |

### BalancedKMeansClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.BalancedKMeansClustererRecallTests` — 12 tests.

| Test                                                                             | Kind | Verifies                                                                                      |
|----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `NearestCentroidDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront`               | Fact | Nearest centroid distance is genuinely the minimum and is moved to the front.                 |
| `PredictReturnsOneDistancePerCentroidInClusterIndexOrderExceptTheNearestIsFirst` | Fact | Predict returns one distance per centroid in cluster index order except the nearest is first. |
| `SameSeedProducesTheSameCentroidsAndTheSameRecallBitForBit`                      | Fact | Same seed produces the same centroids and the same recall bit for bit.                        |
| `RequestingMoreClustersThanDistinctRowsThrowsAClearException`                    | Fact | Requesting more clusters than distinct rows throws a clear exception.                         |
| `RequestingMoreClustersThanRowsThrowsAClearException`                            | Fact | Requesting more clusters than rows throws a clear exception.                                  |
| `ExceedingTheMaximumTrainingRowsThrowsAClearException`                           | Fact | Exceeding the maximum training rows throws a clear exception.                                 |
| `RecallRejectsARowOfTheWrongLength`                                              | Fact | Recall rejects a row of the wrong length.                                                     |
| `AMissingInputIsImputedWithTheTrainingMedian`                                    | Fact | A missing input is imputed with the training median.                                          |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                               | Fact | The snapshot round trips through the envelope unchanged.                                      |
| `ATamperedOrForeignSnapshotIsRefused`                                            | Fact | A tampered or foreign snapshot is refused.                                                    |
| `TheEvidenceReportsPlausibleClusterSummaries`                                    | Fact | The evidence reports plausible cluster summaries.                                             |
| `ClusterSizesAreAsEqualAsPossibleEvenWhenTheNaturalGroupsAreNot`                 | Fact | Cluster sizes are as equal as possible even when the natural groups are not.                  |

### BinarySplitClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.BinarySplitClustererRecallTests` — 12 tests.

| Test                                                                             | Kind | Verifies                                                                                      |
|----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `NearestCentroidDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront`               | Fact | Nearest centroid distance is genuinely the minimum and is moved to the front.                 |
| `PredictReturnsOneDistancePerCentroidInClusterIndexOrderExceptTheNearestIsFirst` | Fact | Predict returns one distance per centroid in cluster index order except the nearest is first. |
| `SameSeedProducesTheSameClustersAndNearlyTheSameRecall`                          | Fact | Same seed produces the same clusters and nearly the same recall.                              |
| `ADifferentSeedCanProduceDifferentCentroids`                                     | Fact | A different seed can produce different centroids.                                             |
| `RequestingASingleClusterIsRejectedBecauseItIsNeverBisected`                     | Fact | Requesting a single cluster is rejected because it is never bisected.                         |
| `RequestingMoreClustersThanDistinctRowsThrowsAClearException`                    | Fact | Requesting more clusters than distinct rows throws a clear exception.                         |
| `RequestingMoreClustersThanRowsThrowsAClearException`                            | Fact | Requesting more clusters than rows throws a clear exception.                                  |
| `RecallRejectsARowOfTheWrongLength`                                              | Fact | Recall rejects a row of the wrong length.                                                     |
| `AMissingInputIsImputedWithTheTrainingMedian`                                    | Fact | A missing input is imputed with the training median.                                          |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                               | Fact | The snapshot round trips through the envelope unchanged.                                      |
| `ATamperedOrForeignSnapshotIsRefused`                                            | Fact | A tampered or foreign snapshot is refused.                                                    |
| `TheEvidenceReportsPlausibleClusterSummaries`                                    | Fact | The evidence reports plausible cluster summaries.                                             |

### DecisionTreeClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.DecisionTreeClassifierExplainToAdaptationTests` — 6 tests.

| Test                                                                               | Kind | Verifies                                                                                      |
|------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                         | Fact | To adaptation is assignable to the shared explanation interface.                              |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                | Fact | To adaptation carries the value and model name and family.                                    |
| `ToAdaptation_ContributionIsInexactAndInProbabilitySpaceWithTheDecisionPathMethod` | Fact | To adaptation contribution is inexact and in probability space with the decision path method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndAlwaysUsesProbabilitySpace` | Fact | To adaptation describes calibration only when calibrated and always uses probability space.   |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                            | Fact | To adaptation maps source from processing type ids when given.                                |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                    | Fact | To adaptation leaves source null when processing type ids are not given.                      |

### DecisionTreeClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.DecisionTreeClassifierRecallTests` — 13 tests.

| Test                                                                               | Kind | Verifies                                                                                          |
|------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `PredictReturnsAHighProbabilityForAClearlyPositiveRowAndLowForAClearlyNegativeRow` | Fact | Predict returns a high probability for a clearly positive row and low for a clearly negative row. |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                         | Fact | Same seed produces the same snapshot and the same recall bit for bit.                             |
| `ADifferentSeedCanProduceADifferentSnapshot`                                       | Fact | A different seed can produce a different snapshot.                                                |
| `RecallRejectsARowOfTheWrongLength`                                                | Fact | Recall rejects a row of the wrong length.                                                         |
| `AMissingInputIsImputedWithTheTrainingMedianAndStillProducesAFiniteProbability`    | Fact | A missing input is imputed with the training median and still produces a finite probability.      |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                 | Fact | The snapshot round trips through the envelope unchanged.                                          |
| `ATamperedOrForeignSnapshotIsRefused`                                              | Fact | A tampered or foreign snapshot is refused.                                                        |
| `TheEvidenceReportsPlausibleMetricsAndImportancesThatSumToOne`                     | Fact | The evidence reports plausible metrics and importances that sum to one.                           |
| `CalibrateFalseLeavesTheModelUncalibratedAndCalibrateTrueCalibratesIt`             | Fact | Calibrate false leaves the model uncalibrated and calibrate true calibrates it.                   |
| `MaxDepthOneProducesAtMostOneSplit`                                                | Fact | Max depth one produces at most one split.                                                         |
| `AConstantColumnIsExcludedFromTheFitAndWarnedAbout`                                | Fact | A constant column is excluded from the fit and warned about.                                      |
| `BothClassesMustBePresentInTheTrainingSplit`                                       | Fact | Both classes must be present in the training split.                                               |
| `ExplainsRootProbabilityMatchesTheEvidenceAndContributionsMoveTowardTheLeaf`       | Fact | Explains root probability matches the evidence and contributions move toward the leaf.            |

### DecisionTreeRegressorExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.DecisionTreeRegressorExplainToAdaptationTests` — 6 tests.

| Test                                                                         | Kind | Verifies                                                                                |
|------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                   | Fact | To adaptation is assignable to the shared explanation interface.                        |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                          | Fact | To adaptation carries the value and model name and family.                              |
| `ToAdaptation_ContributionIsInexactAndInScoreSpaceWithTheDecisionPathMethod` | Fact | To adaptation contribution is inexact and in score space with the decision path method. |
| `ToAdaptation_IsNeverCalibrated`                                             | Fact | To adaptation is never calibrated.                                                      |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                      | Fact | To adaptation maps source from processing type ids when given.                          |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`              | Fact | To adaptation leaves source null when processing type ids are not given.                |

### DecisionTreeRegressorRecallTests

`Jube.Test.AdvancedAnalytics.Slice.DecisionTreeRegressorRecallTests` — 11 tests.

| Test                                                                         | Kind | Verifies                                                                                    |
|------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `PredictReturnsAHighValueForAClearlyPositiveRowAndLowForAClearlyNegativeRow` | Fact | Predict returns a high value for a clearly positive row and low for a clearly negative row. |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                   | Fact | Same seed produces the same snapshot and the same recall bit for bit.                       |
| `ADifferentSeedCanProduceADifferentSnapshot`                                 | Fact | A different seed can produce a different snapshot.                                          |
| `RecallRejectsARowOfTheWrongLength`                                          | Fact | Recall rejects a row of the wrong length.                                                   |
| `AMissingInputIsImputedWithTheTrainingMedianAndStillProducesAFiniteValue`    | Fact | A missing input is imputed with the training median and still produces a finite value.      |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                           | Fact | The snapshot round trips through the envelope unchanged.                                    |
| `ATamperedOrForeignSnapshotIsRefused`                                        | Fact | A tampered or foreign snapshot is refused.                                                  |
| `TheEvidenceReportsPlausibleMetricsAndImportancesThatSumToOne`               | Fact | The evidence reports plausible metrics and importances that sum to one.                     |
| `MaxDepthOneProducesAtMostOneSplit`                                          | Fact | Max depth one produces at most one split.                                                   |
| `AConstantColumnIsExcludedFromTheFitAndWarnedAbout`                          | Fact | A constant column is excluded from the fit and warned about.                                |
| `ExplainsRootValueMatchesTheEvidenceAndContributionsMoveTowardTheLeaf`       | Fact | Explains root value matches the evidence and contributions move toward the leaf.            |

### GaussianMixtureClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.GaussianMixtureClustererRecallTests` — 11 tests.

| Test                                                                                 | Kind | Verifies                                                                                          |
|--------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `HighestPosteriorIsGenuinelyTheMaximumAndIsMovedToTheFront`                          | Fact | Highest posterior is genuinely the maximum and is moved to the front.                             |
| `PredictReturnsOnePosteriorPerComponentInComponentIndexOrderExceptTheHighestIsFirst` | Fact | Predict returns one posterior per component in component index order except the highest is first. |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                           | Fact | Same seed produces the same snapshot and the same recall bit for bit.                             |
| `ADifferentSeedCanProduceADifferentSnapshot`                                         | Fact | A different seed can produce a different snapshot.                                                |
| `RequestingMoreComponentsThanDistinctRowsThrowsAClearException`                      | Fact | Requesting more components than distinct rows throws a clear exception.                           |
| `RequestingMoreComponentsThanRowsThrowsAClearException`                              | Fact | Requesting more components than rows throws a clear exception.                                    |
| `RecallRejectsARowOfTheWrongLength`                                                  | Fact | Recall rejects a row of the wrong length.                                                         |
| `AMissingInputIsImputedWithTheTrainingMedian`                                        | Fact | A missing input is imputed with the training median.                                              |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                   | Fact | The snapshot round trips through the envelope unchanged.                                          |
| `ATamperedOrForeignSnapshotIsRefused`                                                | Fact | A tampered or foreign snapshot is refused.                                                        |
| `TheEvidenceReportsPlausibleClusterSummaries`                                        | Fact | The evidence reports plausible cluster summaries.                                                 |

### GaussianMixtureDensityExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.GaussianMixtureDensityExplainToAdaptationTests` — 6 tests.

| Test                                                                                       | Kind | Verifies                                                                                                |
|--------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                                 | Fact | To adaptation is assignable to the shared explanation interface.                                        |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                        | Fact | To adaptation carries the value and model name and family.                                              |
| `ToAdaptation_ContributionIsInexactAndInTheLogLikelihoodSpaceWithTheMahalanobisTermMethod` | Fact | To adaptation contribution is inexact and in the log likelihood space with the mahalanobis term method. |
| `ToAdaptation_IsNeverCalibratedAndUsesLogLikelihoodSpace`                                  | Fact | To adaptation is never calibrated and uses log likelihood space.                                        |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                                    | Fact | To adaptation maps source from processing type ids when given.                                          |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                            | Fact | To adaptation leaves source null when processing type ids are not given.                                |

### GaussianMixtureDensityRecallTests

`Jube.Test.AdvancedAnalytics.Slice.GaussianMixtureDensityRecallTests` — 9 tests.

| Test                                                                           | Kind | Verifies                                                                                       |
|--------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `ARowNearAFittedComponentScoresAHigherLogDensityThanARowFarFromEveryComponent` | Fact | A row near a fitted component scores a higher log density than a row far from every component. |
| `ExplainDecomposesPerDimensionAndDoesNotSumExactlyToTheLogDensity`             | Fact | Explain decomposes per dimension and does not sum exactly to the log density.                  |
| `AConstantColumnIsExcludedFromTheMixtureAndCarriesZeroWeight`                  | Fact | A constant column is excluded from the mixture and carries zero weight.                        |
| `EveryVariableConstantThrowsALoudException`                                    | Fact | Every variable constant throws a loud exception.                                               |
| `TooFewRowsForTheRequestedComponentCountThrowsAClearException`                 | Fact | Too few rows for the requested component count throws a clear exception.                       |
| `RecallRejectsARowOfTheWrongLength`                                            | Fact | Recall rejects a row of the wrong length.                                                      |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                             | Fact | The snapshot round trips through the envelope unchanged.                                       |
| `ATamperedOrForeignSnapshotIsRefused`                                          | Fact | A tampered or foreign snapshot is refused.                                                     |
| `RecallIsThreadSafeAndDeterministicUnderContention`                            | Fact | Recall is thread safe and deterministic under contention.                                      |

### GradientBoostingClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.GradientBoostingClassifierExplainToAdaptationTests` — 7 tests.

| Test                                                                                   | Kind | Verifies                                                                                           |
|----------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                             | Fact | To adaptation is assignable to the shared explanation interface.                                   |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                    | Fact | To adaptation carries the value and model name and family.                                         |
| `ToAdaptation_ContributionIsExactAndInLogOddsSpaceWithTheTreePathContributionMethod`   | Fact | To adaptation contribution is exact and in log odds space with the tree path contribution method.  |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpaceEitherWay`  | Fact | To adaptation describes calibration only when calibrated and uses probability space either way.    |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                                | Fact | To adaptation maps source from processing type ids when given.                                     |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                        | Fact | To adaptation leaves source null when processing type ids are not given.                           |
| `ToAdaptation_ProvesExactnessAcrossManyRealFitsThroughTheRecallAndAdaptationRoundTrip` | Fact | To adaptation proves exactness across many real fits through the recall and adaptation round trip. |

### GradientBoostingClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.GradientBoostingClassifierRecallTests` — 14 tests.

| Test                                                                 | Kind | Verifies                                                                      |
|----------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `SeparableDataProducesHighProbabilityForPositivesAndLowForNegatives` | Fact | Separable data produces high probability for positives and low for negatives. |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`           | Fact | Same seed produces the same snapshot and the same recall bit for bit.         |
| `ADifferentSeedCanProduceADifferentSnapshot`                         | Fact | A different seed can produce a different snapshot.                            |
| `PredictedProbabilitiesAreAlwaysBetweenZeroAndOne`                   | Fact | Predicted probabilities are always between zero and one.                      |
| `RecallRejectsARowOfTheWrongLength`                                  | Fact | Recall rejects a row of the wrong length.                                     |
| `AMissingInputIsImputedWithTheTrainingMedian`                        | Fact | A missing input is imputed with the training median.                          |
| `RequiringBothClassesThrowsAClearExceptionWhenOnlyOneIsPresent`      | Fact | Requiring both classes throws a clear exception when only one is present.     |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                   | Fact | The snapshot round trips through the envelope unchanged.                      |
| `ATamperedOrForeignSnapshotIsRefused`                                | Fact | A tampered or foreign snapshot is refused.                                    |
| `TheEvidenceReportsPlausibleEnsembleSummaries`                       | Fact | The evidence reports plausible ensemble summaries.                            |
| `ExplainRawScoreMatchesThePipelineRawScore`                          | Fact | Explain raw score matches the pipeline raw score.                             |
| `ContributionsReconstructTheRawLogOddsMarginExactlyAcrossManyProbes` | Fact | Contributions reconstruct the raw log odds margin exactly across many probes. |
| `ALargerLearningRateReachesAGivenTrainingLossInFewerRounds`          | Fact | A larger learning rate reaches a given training loss in fewer rounds.         |
| `ASingleRoundStillProducesAValidCalibratedModel`                     | Fact | A single round still produces a valid calibrated model.                       |

### GradientBoostingRegressorExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.GradientBoostingRegressorExplainToAdaptationTests` — 7 tests.

| Test                                                                                   | Kind | Verifies                                                                                           |
|----------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                             | Fact | To adaptation is assignable to the shared explanation interface.                                   |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                    | Fact | To adaptation carries the value and model name and family.                                         |
| `ToAdaptation_ContributionIsExactAndInScoreSpaceWithTheTreePathContributionMethod`     | Fact | To adaptation contribution is exact and in score space with the tree path contribution method.     |
| `ToAdaptation_IsNeverCalibrated`                                                       | Fact | To adaptation is never calibrated.                                                                 |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                                | Fact | To adaptation maps source from processing type ids when given.                                     |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                        | Fact | To adaptation leaves source null when processing type ids are not given.                           |
| `ToAdaptation_ProvesExactnessAcrossManyRealFitsThroughTheRecallAndAdaptationRoundTrip` | Fact | To adaptation proves exactness across many real fits through the recall and adaptation round trip. |

### GradientBoostingRegressorRecallTests

`Jube.Test.AdvancedAnalytics.Slice.GradientBoostingRegressorRecallTests` — 15 tests.

| Test                                                                                 | Kind | Verifies                                                                                        |
|--------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `PredictionsTrackTheOutcomeReasonablyWellOnASimpleRelationship`                      | Fact | Predictions track the outcome reasonably well on a simple relationship.                         |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                           | Fact | Same seed produces the same snapshot and the same recall bit for bit.                           |
| `ADifferentSeedCanProduceADifferentSnapshot`                                         | Fact | A different seed can produce a different snapshot.                                              |
| `PredictionsAreAlwaysFinite`                                                         | Fact | Predictions are always finite.                                                                  |
| `RecallRejectsARowOfTheWrongLength`                                                  | Fact | Recall rejects a row of the wrong length.                                                       |
| `AMissingInputIsImputedWithTheTrainingMedian`                                        | Fact | A missing input is imputed with the training median.                                            |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                   | Fact | The snapshot round trips through the envelope unchanged.                                        |
| `ATamperedOrForeignSnapshotIsRefused`                                                | Fact | A tampered or foreign snapshot is refused.                                                      |
| `TheEvidenceReportsPlausibleEnsembleSummaries`                                       | Fact | The evidence reports plausible ensemble summaries.                                              |
| `ExplainPredictedValueMatchesThePipelinePrediction`                                  | Fact | Explain predicted value matches the pipeline prediction.                                        |
| `ContributionsReconstructThePredictedValueExactlyAcrossManyProbesAndAllTrainingRows` | Fact | Contributions reconstruct the predicted value exactly across many probes and all training rows. |
| `ALargerLearningRateReachesAGivenTrainingLossInFewerRounds`                          | Fact | A larger learning rate reaches a given training loss in fewer rounds.                           |
| `ASingleRoundStillProducesAValidModel`                                               | Fact | A single round still produces a valid model.                                                    |
| `FewerRowsThanVariablesStillTrainsSinceTreesHaveNoDesignMatrixRankRequirement`       | Fact | Fewer rows than variables still trains since trees have no design matrix rank requirement.      |
| `TestSplitRowsNeverTouchTheFittedTrainingStatistics`                                 | Fact | Test split rows never touch the fitted training statistics.                                     |

### IsolationForestAnomalyExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.IsolationForestAnomalyExplainToAdaptationTests` — 6 tests.

| Test                                                                                    | Kind | Verifies                                                                                             |
|-----------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                              | Fact | To adaptation is assignable to the shared explanation interface.                                     |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                     | Fact | To adaptation carries the value and model name and family.                                           |
| `ToAdaptation_ContributionIsInexactAndInTheScoreSpaceWithTheTreePathContributionMethod` | Fact | To adaptation contribution is inexact and in the score space with the tree path contribution method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesScoreSpaceOtherwise`         | Fact | To adaptation describes calibration only when calibrated and uses score space otherwise.             |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                                 | Fact | To adaptation maps source from processing type ids when given.                                       |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                         | Fact | To adaptation leaves source null when processing type ids are not given.                             |

### IsolationForestAnomalyRecallTests

`Jube.Test.AdvancedAnalytics.Slice.IsolationForestAnomalyRecallTests` — 10 tests.

| Test                                                                               | Kind | Verifies                                                                                         |
|------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `OutlierRowsScoreHigherThanNormalRows`                                             | Fact | Outlier rows score higher than normal rows.                                                      |
| `RecallReproducesTrainingTimeScoresBitForBit`                                      | Fact | Recall reproduces training time scores bit for bit.                                              |
| `TrainingIsDeterministicForTheSameSeed`                                            | Fact | Training is deterministic for the same seed.                                                     |
| `MissingValuesAreImputedWithTheTrainingMedian`                                     | Fact | Missing values are imputed with the training median.                                             |
| `UnlabelledTrainingHasNoCalibratorAndReportsARawScoreInUnitRange`                  | Fact | Unlabelled training has no calibrator and reports a raw score in unit range.                     |
| `LabelledTrainingWithBothClassesInValidationFitsACalibratorAndProducesARealRocAuc` | Fact | Labelled training with both classes in validation fits a calibrator and produces a real roc auc. |
| `RecallRejectsARowOfTheWrongLength`                                                | Fact | Recall rejects a row of the wrong length.                                                        |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                 | Fact | The snapshot round trips through the envelope unchanged.                                         |
| `ATamperedOrForeignSnapshotIsRefused`                                              | Fact | A tampered or foreign snapshot is refused.                                                       |
| `RecallIsThreadSafeAndDeterministicUnderContention`                                | Fact | Recall is thread safe and deterministic under contention.                                        |

### KMeansClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.KMeansClustererRecallTests` — 11 tests.

| Test                                                                             | Kind | Verifies                                                                                      |
|----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `NearestCentroidDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront`               | Fact | Nearest centroid distance is genuinely the minimum and is moved to the front.                 |
| `PredictReturnsOneDistancePerCentroidInClusterIndexOrderExceptTheNearestIsFirst` | Fact | Predict returns one distance per centroid in cluster index order except the nearest is first. |
| `SameSeedProducesTheSameCentroidsAndTheSameRecallBitForBit`                      | Fact | Same seed produces the same centroids and the same recall bit for bit.                        |
| `ADifferentSeedCanProduceDifferentCentroids`                                     | Fact | A different seed can produce different centroids.                                             |
| `RequestingMoreClustersThanDistinctRowsThrowsAClearException`                    | Fact | Requesting more clusters than distinct rows throws a clear exception.                         |
| `RequestingMoreClustersThanRowsThrowsAClearException`                            | Fact | Requesting more clusters than rows throws a clear exception.                                  |
| `RecallRejectsARowOfTheWrongLength`                                              | Fact | Recall rejects a row of the wrong length.                                                     |
| `AMissingInputIsImputedWithTheTrainingMedian`                                    | Fact | A missing input is imputed with the training median.                                          |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                               | Fact | The snapshot round trips through the envelope unchanged.                                      |
| `ATamperedOrForeignSnapshotIsRefused`                                            | Fact | A tampered or foreign snapshot is refused.                                                    |
| `TheEvidenceReportsPlausibleClusterSummaries`                                    | Fact | The evidence reports plausible cluster summaries.                                             |

### KMedoidsClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.KMedoidsClustererRecallTests` — 12 tests.

| Test                                                                           | Kind | Verifies                                                                                    |
|--------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `NearestMedoidDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront`               | Fact | Nearest medoid distance is genuinely the minimum and is moved to the front.                 |
| `PredictReturnsOneDistancePerMedoidInClusterIndexOrderExceptTheNearestIsFirst` | Fact | Predict returns one distance per medoid in cluster index order except the nearest is first. |
| `SameSeedProducesTheSameMedoidsAndTheSameRecallBitForBit`                      | Fact | Same seed produces the same medoids and the same recall bit for bit.                        |
| `ADifferentSeedCanProduceDifferentMedoidsUnderUniformSeeding`                  | Fact | A different seed can produce different medoids under uniform seeding.                       |
| `RequestingMoreClustersThanDistinctRowsThrowsAClearException`                  | Fact | Requesting more clusters than distinct rows throws a clear exception.                       |
| `RequestingMoreClustersThanRowsThrowsAClearException`                          | Fact | Requesting more clusters than rows throws a clear exception.                                |
| `RecallRejectsARowOfTheWrongLength`                                            | Fact | Recall rejects a row of the wrong length.                                                   |
| `AMissingInputIsImputedWithTheTrainingMedian`                                  | Fact | A missing input is imputed with the training median.                                        |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                             | Fact | The snapshot round trips through the envelope unchanged.                                    |
| `EveryMedoidIsALiteralTrainingRowNotAnAverage`                                 | Fact | Every medoid is a literal training row not an average.                                      |
| `ATamperedOrForeignSnapshotIsRefused`                                          | Fact | A tampered or foreign snapshot is refused.                                                  |
| `TheEvidenceReportsPlausibleClusterSummaries`                                  | Fact | The evidence reports plausible cluster summaries.                                           |

### KModesClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.KModesClustererRecallTests` — 13 tests.

| Test                                                                              | Kind | Verifies                                                                                        |
|-----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `NearestModeDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront`                    | Fact | Nearest mode distance is genuinely the minimum and is moved to the front.                       |
| `PredictReturnsOneMismatchCountPerModeInClusterIndexOrderExceptTheNearestIsFirst` | Fact | Predict returns one mismatch count per mode in cluster index order except the nearest is first. |
| `SameSeedProducesTheSameModesAndTheSameRecallBitForBit`                           | Fact | Same seed produces the same modes and the same recall bit for bit.                              |
| `ADifferentSeedCanProduceDifferentModes`                                          | Fact | A different seed can produce different modes.                                                   |
| `RequestingMoreClustersThanDistinctRowsThrowsAClearException`                     | Fact | Requesting more clusters than distinct rows throws a clear exception.                           |
| `RequestingMoreClustersThanRowsThrowsAClearException`                             | Fact | Requesting more clusters than rows throws a clear exception.                                    |
| `RecallRejectsARowOfTheWrongLength`                                               | Fact | Recall rejects a row of the wrong length.                                                       |
| `AMissingInputIsImputedWithTheTrainingMode`                                       | Fact | A missing input is imputed with the training mode.                                              |
| `ANonIntegerInputIsRoundedToTheNearestCategory`                                   | Fact | A non integer input is rounded to the nearest category.                                         |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                | Fact | The snapshot round trips through the envelope unchanged.                                        |
| `ATamperedOrForeignSnapshotIsRefused`                                             | Fact | A tampered or foreign snapshot is refused.                                                      |
| `TheEvidenceReportsPlausibleClusterSummaries`                                     | Fact | The evidence reports plausible cluster summaries.                                               |
| `ANonIntegerTrainingColumnRaisesAWarning`                                         | Fact | A non integer training column raises a warning.                                                 |

### KNearestNeighboursClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.KNearestNeighboursClassifierRecallTests` — 11 tests.

| Test                                                                      | Kind | Verifies                                                                             |
|---------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `APointNearAKnownClassClusterPredictsThatClassWithHighProbability`        | Fact | A point near a known class cluster predicts that class with high probability.        |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                | Fact | Same seed produces the same snapshot and the same recall bit for bit.                |
| `ADifferentSeedCanProduceADifferentSnapshot`                              | Fact | A different seed can produce a different snapshot.                                   |
| `RequestingMoreNeighboursThanAvailableTrainingRowsThrowsAClearException`  | Fact | Requesting more neighbours than available training rows throws a clear exception.    |
| `RequestingMoreNeighboursThanStoredRowsAtRecallTimeThrowsAClearException` | Fact | Requesting more neighbours than stored rows at recall time throws a clear exception. |
| `RecallRejectsARowOfTheWrongLength`                                       | Fact | Recall rejects a row of the wrong length.                                            |
| `AMissingInputIsImputedWithTheTrainingMedian`                             | Fact | A missing input is imputed with the training median.                                 |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                        | Fact | The snapshot round trips through the envelope unchanged.                             |
| `ATamperedOrForeignSnapshotIsRefused`                                     | Fact | A tampered or foreign snapshot is refused.                                           |
| `TheEvidenceReportsCalibratedTestMetricsOnWellSeparatedClasses`           | Fact | The evidence reports calibrated test metrics on well separated classes.              |
| `TheExplainSharesAreNonNegativeAndSumToOne`                               | Fact | The explain shares are non negative and sum to one.                                  |

### KNearestNeighboursRegressorExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.KNearestNeighboursRegressorExplainToAdaptationTests` — 8 tests.

| Test                                                                  | Kind | Verifies                                                                        |
|-----------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`            | Fact | To adaptation is assignable to the shared explanation interface.                |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                   | Fact | To adaptation carries the value and model name and family.                      |
| `ToAdaptation_ContributionIsInexactAndUsesTheTrainingMeanAsBaseValue` | Fact | To adaptation contribution is inexact and uses the training mean as base value. |
| `ToAdaptation_IsNeverCalibrated`                                      | Fact | To adaptation is never calibrated.                                              |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`               | Fact | To adaptation maps source from processing type ids when given.                  |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`       | Fact | To adaptation leaves source null when processing type ids are not given.        |
| `ToAdaptation_NeverCarriesPerContributionSignificance`                | Fact | To adaptation never carries per contribution significance.                      |
| `ToAdaptation_ContributionWeightsSumToOne`                            | Fact | To adaptation contribution weights sum to one.                                  |

### KNearestNeighboursRegressorRecallTests

`Jube.Test.AdvancedAnalytics.Slice.KNearestNeighboursRegressorRecallTests` — 12 tests.

| Test                                                                      | Kind | Verifies                                                                             |
|---------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `APointNearAKnownTargetClusterPredictsCloseToThatCluster`                 | Fact | A point near a known target cluster predicts close to that cluster.                  |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                | Fact | Same seed produces the same snapshot and the same recall bit for bit.                |
| `ADifferentSeedCanProduceADifferentSnapshot`                              | Fact | A different seed can produce a different snapshot.                                   |
| `RequestingMoreNeighboursThanAvailableTrainingRowsThrowsAClearException`  | Fact | Requesting more neighbours than available training rows throws a clear exception.    |
| `RequestingMoreNeighboursThanStoredRowsAtRecallTimeThrowsAClearException` | Fact | Requesting more neighbours than stored rows at recall time throws a clear exception. |
| `RecallRejectsARowOfTheWrongLength`                                       | Fact | Recall rejects a row of the wrong length.                                            |
| `AMissingInputIsImputedWithTheTrainingMedian`                             | Fact | A missing input is imputed with the training median.                                 |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                        | Fact | The snapshot round trips through the envelope unchanged.                             |
| `ATamperedOrForeignSnapshotIsRefused`                                     | Fact | A tampered or foreign snapshot is refused.                                           |
| `TheEvidenceReportsPlausibleMetricsOnWellSeparatedTargets`                | Fact | The evidence reports plausible metrics on well separated targets.                    |
| `TheExplainSharesAreNonNegativeAndSumToOne`                               | Fact | The explain shares are non negative and sum to one.                                  |
| `ThePredictedValueIsThePlainMeanOfTheNeighbourTargets`                    | Fact | The predicted value is the plain mean of the neighbour targets.                      |

### KernelRidgeRegressorRecallTests

`Jube.Test.AdvancedAnalytics.Slice.KernelRidgeRegressorRecallTests` — 11 tests.

| Test                                                                              | Kind | Verifies                                                                                         |
|-----------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `AGaussianKernelFitsASinusoidThatALinearKernelCannot`                             | Fact | A gaussian kernel fits a sinusoid that a linear kernel cannot.                                   |
| `APolynomialKernelFitsAQuadraticSurface`                                          | Fact | A polynomial kernel fits a quadratic surface.                                                    |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                        | Fact | Same seed produces the same snapshot and the same recall bit for bit.                            |
| `ADifferentSeedCanProduceADifferentSnapshot`                                      | Fact | A different seed can produce a different snapshot.                                               |
| `RecallRejectsARowOfTheWrongLength`                                               | Fact | Recall rejects a row of the wrong length.                                                        |
| `AMissingInputIsImputedWithTheTrainingMedian`                                     | Fact | A missing input is imputed with the training median.                                             |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                | Fact | The snapshot round trips through the envelope unchanged.                                         |
| `ATamperedOrForeignSnapshotIsRefused`                                             | Fact | A tampered or foreign snapshot is refused.                                                       |
| `TheEvidenceReportsPlausibleMetricsAndExpansionSize`                              | Fact | The evidence reports plausible metrics and expansion size.                                       |
| `GammaResolvesAutomaticallyFromTheNumberOfPredictorsWhenLeftAtZero`               | Fact | Gamma resolves automatically from the number of predictors when left at zero.                    |
| `ANearSingularSystemIsHandledEitherWithAWarningOrARefusalRatherThanSilentGarbage` | Fact | A near singular system is handled either with a warning or a refusal rather than silent garbage. |

### KernelSupportVectorClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.KernelSupportVectorClassifierRecallTests` — 12 tests.

| Test                                                                | Kind | Verifies                                                                      |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `AGaussianKernelSeparatesAnXorPatternThatIsNotLinearlySeparable`    | Fact | A gaussian kernel separates an xor pattern that is not linearly separable.    |
| `APolynomialKernelSeparatesAnXorPattern`                            | Fact | A polynomial kernel separates an xor pattern.                                 |
| `ALaplacianKernelSeparatesConcentricCircles`                        | Fact | A laplacian kernel separates concentric circles.                              |
| `ASigmoidKernelFitsAndRecallsWithoutError`                          | Fact | A sigmoid kernel fits and recalls without error.                              |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`          | Fact | Same seed produces the same snapshot and the same recall bit for bit.         |
| `ADifferentSeedCanProduceADifferentSnapshot`                        | Fact | A different seed can produce a different snapshot.                            |
| `RecallRejectsARowOfTheWrongLength`                                 | Fact | Recall rejects a row of the wrong length.                                     |
| `AMissingInputIsImputedWithTheTrainingMedian`                       | Fact | A missing input is imputed with the training median.                          |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                  | Fact | The snapshot round trips through the envelope unchanged.                      |
| `ATamperedOrForeignSnapshotIsRefused`                               | Fact | A tampered or foreign snapshot is refused.                                    |
| `TheEvidenceReportsCalibratedTestMetricsOnWellSeparatedClasses`     | Fact | The evidence reports calibrated test metrics on well separated classes.       |
| `GammaResolvesAutomaticallyFromTheNumberOfPredictorsWhenLeftAtZero` | Fact | Gamma resolves automatically from the number of predictors when left at zero. |

### LinearSupportVectorClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.LinearSupportVectorClassifierExplainToAdaptationTests` — 6 tests.

| Test                                                                                  | Kind | Verifies                                                                                        |
|---------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                            | Fact | To adaptation is assignable to the shared explanation interface.                                |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                   | Fact | To adaptation carries the value and model name and family.                                      |
| `ToAdaptation_ContributionIsExactAndInDecisionFunctionSpaceWithTheCoefficientMethod`  | Fact | To adaptation contribution is exact and in decision function space with the coefficient method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpaceEitherWay` | Fact | To adaptation describes calibration only when calibrated and uses probability space either way. |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                               | Fact | To adaptation maps source from processing type ids when given.                                  |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                       | Fact | To adaptation leaves source null when processing type ids are not given.                        |

### LinearSupportVectorClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.LinearSupportVectorClassifierRecallTests` — 11 tests.

| Test                                                                     | Kind   | Verifies                                                                             |
|--------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `ClearlyPositiveAndNegativeProbesRecallOnTheCorrectSideOfOneHalf`        | Fact   | Clearly positive and negative probes recall on the correct side of one half.         |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`               | Fact   | Same seed produces the same snapshot and the same recall bit for bit.                |
| `ExplainContributionsPlusTheBiasSumExactlyToTheRawDecisionFunction`      | Fact   | Explain contributions plus the bias sum exactly to the raw decision function.        |
| `ExplainContributionsAreGenuinelyExactNotJustFlagged`                    | Fact   | Explain contributions are genuinely exact not just flagged.                          |
| `ConstantFeatureAcrossTheWholeTrainingSplitIsWarnedAboutButDoesNotThrow` | Fact   | Constant feature across the whole training split is warned about but does not throw. |
| `RecallRejectsARowOfTheWrongLength`                                      | Fact   | Recall rejects a row of the wrong length.                                            |
| `AMissingInputIsImputedWithTheTrainingMedian`                            | Fact   | A missing input is imputed with the training median.                                 |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                       | Fact   | The snapshot round trips through the envelope unchanged.                             |
| `ATamperedOrForeignSnapshotIsRefused`                                    | Fact   | A tampered or foreign snapshot is refused.                                           |
| `TheEvidenceReportsPlausibleMetricsAndCalibration`                       | Fact   | The evidence reports plausible metrics and calibration.                              |
| `VaryingComplexityValuesAllProduceAValidFiniteModel`                     | Theory | Varying complexity values all produce a valid finite model.                          |

### LogisticRegressionCalibrationTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionCalibrationTests` — 7 tests.

| Test                                                                        | Kind   | Verifies                                                                                |
|-----------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `CalibrationRepairsADeliberatelyDistortedScoreOnTheTestSplit`               | Fact   | Calibration repairs a deliberately distorted score on the test split.                   |
| `CalibrationIsFittedOnValidationRowsOnly`                                   | Fact   | Calibration is fitted on validation rows only.                                          |
| `PriorShiftBringsBalancedFitProbabilitiesBackToTheTwoPercentBaseRate`       | Theory | Prior shift brings balanced fit probabilities back to the two percent base rate.        |
| `PriorShiftOffsetMatchesTheClosedFormAndHonoursAnOverride`                  | Fact   | Prior shift offset matches the closed form and honours an override.                     |
| `HeldOutMetricsAreComputedOnTheTestPredictionsAndMatchTheSharedDefinitions` | Fact   | Held out metrics are computed on the test predictions and match the shared definitions. |
| `TheBayesMinimumRiskThresholdIsTheCostMinimiserOnCalibratedProbabilities`   | Fact   | The bayes minimum risk threshold is the cost minimiser on calibrated probabilities.     |
| `AnExplicitThresholdWinsOverTheCostThreshold`                               | Fact   | An explicit threshold wins over the cost threshold.                                     |

### LogisticRegressionExplainTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionExplainTests` — 5 tests.

| Test                                                                        | Kind | Verifies                                                                              |
|-----------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `Explain_ContributionsAreWeightTimesInputAndSumWithInterceptToTheRawLogit`  | Fact | Explain contributions are weight times input and sum with intercept to the raw logit. |
| `Explain_RawLogitMatchesTheExistingRawLogitMethodExactly`                   | Fact | Explain raw logit matches the existing raw logit method exactly.                      |
| `Explain_ContributionsSumWithInterceptExactlyEvenWithStandardisation`       | Fact | Explain contributions sum with intercept exactly even with standardisation.           |
| `Explain_AMissingInputContributesThroughItsIndicatorWeightNotTheRealWeight` | Fact | Explain a missing input contributes through its indicator weight not the real weight. |
| `Explain_APresentInputWithAnIndicatorContributesZeroFromTheIndicatorWeight` | Fact | Explain a present input with an indicator contributes zero from the indicator weight. |

### LogisticRegressionExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionExplainToAdaptationTests` — 7 tests.

| Test                                                                                | Kind | Verifies                                                                                      |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                          | Fact | To adaptation is assignable to the shared explanation interface.                              |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                 | Fact | To adaptation carries the value and model name and family.                                    |
| `ToAdaptation_ContributionIsExactAndReconstructsTheRawLogit`                        | Fact | To adaptation contribution is exact and reconstructs the raw logit.                           |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibrated`                               | Fact | To adaptation describes calibration only when calibrated.                                     |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                             | Fact | To adaptation maps source from processing type ids when given.                                |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                     | Fact | To adaptation leaves source null when processing type ids are not given.                      |
| `ToAdaptation_CarriesPerCoefficientSignificanceWhenPresentAndLeavesItNullOtherwise` | Fact | To adaptation carries per coefficient significance when present and leaves it null otherwise. |

### LogisticRegressionLeakageAndDeterminismTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionLeakageAndDeterminismTests` — 6 tests.

| Test                                                             | Kind | Verifies                                                                   |
|------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `NoTestRowInfluencesPreprocessingFitOrCalibration`               | Fact | No test row influences preprocessing fit or calibration.                   |
| `ValidationRowsChangeOnlyTheCalibrationAndTrainRowsChangeTheFit` | Fact | Validation rows change only the calibration and train rows change the fit. |
| `PreprocessingStatisticsComeFromTrainRowsAlone`                  | Fact | Preprocessing statistics come from train rows alone.                       |
| `SplitsAreDisjointCoverEveryRowAndKeepClassProportions`          | Fact | Splits are disjoint cover every row and keep class proportions.            |
| `TheSameSeedGivesByteIdenticalResultsAndADifferentSeedDoesNot`   | Fact | The same seed gives byte identical results and a different seed does not.  |
| `TrainWithSplitRejectsOverlappingOrOutOfRangeSplits`             | Fact | Train with split rejects overlapping or out of range splits.               |

### LogisticRegressionRecallTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionRecallTests` — 12 tests.

| Test                                                                               | Kind | Verifies                                                                                     |
|------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `RecallReproducesTrainingTimePredictionsBitForBitIncludingMissingInputs`           | Fact | Recall reproduces training time predictions bit for bit including missing inputs.            |
| `RecallReproducesTheUncalibratedAndUnstandardisedPipelinesToo`                     | Fact | Recall reproduces the uncalibrated and unstandardised pipelines too.                         |
| `AMissingInputIsImputedWithTheTrainingMedianAndFlagged`                            | Fact | A missing input is imputed with the training median and flagged.                             |
| `RecallRejectsMissingValuesWhenTheModelWasTrainedToReject`                         | Fact | Recall rejects missing values when the model was trained to reject.                          |
| `RecallRejectsARowOfTheWrongLength`                                                | Fact | Recall rejects a row of the wrong length.                                                    |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                 | Fact | The snapshot round trips through the envelope unchanged.                                     |
| `TheSnapshotCarriesSignificancesMatchingTheCoefficientTableAndExplainSurfacesThem` | Fact | The snapshot carries significances matching the coefficient table and explain surfaces them. |
| `RecallFallsBackToNullSignificancesWhenTheSnapshotPredatesTheField`                | Fact | Recall falls back to null significances when the snapshot predates the field.                |
| `ATamperedOrForeignSnapshotIsRefused`                                              | Fact | A tampered or foreign snapshot is refused.                                                   |
| `RecallIsThreadSafeAndDeterministicUnderContention`                                | Fact | Recall is thread safe and deterministic under contention.                                    |
| `TheToolsScoreExactlyLikeTheTrainerAndRecall`                                      | Fact | The tools score exactly like the trainer and recall.                                         |
| `TheResultSerialisesForAnLlmWithoutNaNAndWithoutBulkPredictionArrays`              | Fact | The result serialises for an llm without na n and without bulk prediction arrays.            |

### LogisticRegressionRecoveryTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionRecoveryTests` — 5 tests.

| Test                                                                                   | Kind | Verifies                                                                                         |
|----------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `EstimatesRecoverTheKnownCoefficientsAtLargeSample`                                    | Fact | Estimates recover the known coefficients at large sample.                                        |
| `WaldIntervalsCoverTheTruthAtAboutTheNominalRate`                                      | Fact | Wald intervals cover the truth at about the nominal rate.                                        |
| `TruePredictorsAreSignificantAndNoiseFollowsTheFalsePositiveRate`                      | Fact | True predictors are significant and noise follows the false positive rate.                       |
| `CoefficientTableMatchesAnIndependentPythonImplementation`                             | Fact | Coefficient table matches an independent python implementation.                                  |
| `RidgePenaltyMatchesAnIndependentPythonImplementationAndLeavesTheInterceptUnpenalised` | Fact | Ridge penalty matches an independent python implementation and leaves the intercept unpenalised. |

### LogisticRegressionValidationTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionValidationTests` — 11 tests.

| Test                                                              | Kind | Verifies                                                                   |
|-------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `RaggedRowsAreRejected`                                           | Fact | Ragged rows are rejected.                                                  |
| `LabelsOtherThanZeroAndOneAreRejected`                            | Fact | Labels other than zero and one are rejected.                               |
| `NaNInputsAreRejectedUnderTheRejectPolicyAndImputedOtherwise`     | Fact | Na n inputs are rejected under the reject policy and imputed otherwise.    |
| `ASingleClassIsRejected`                                          | Fact | A single class is rejected.                                                |
| `FewerRowsThanVariablesIsRejected`                                | Fact | Fewer rows than variables is rejected.                                     |
| `MismatchedLengthsBlankAndDuplicateNamesAndEmptyInputAreRejected` | Fact | Mismatched lengths blank and duplicate names and empty input are rejected. |
| `NonsensicalOptionsAreRejected`                                   | Fact | Nonsensical options are rejected.                                          |
| `AVariableWithNoObservedTrainingValueIsRejected`                  | Fact | A variable with no observed training value is rejected.                    |
| `EveryVariableConstantIsRejected`                                 | Fact | Every variable constant is rejected.                                       |
| `ACancelledTokenStopsTrainingBeforeAnyWork`                       | Fact | A cancelled token stops training before any work.                          |
| `CancellationDuringALongFitIsHonoured`                            | Fact | Cancellation during a long fit is honoured.                                |

### LogisticRegressionWarningTests

`Jube.Test.AdvancedAnalytics.Slice.LogisticRegressionWarningTests` — 14 tests.

| Test                                                                               | Kind | Verifies                                                                                       |
|------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `AWellPosedProblemRaisesNoModelQualityWarnings`                                    | Fact | A well posed problem raises no model quality warnings.                                         |
| `PerfectSeparationIsDetected`                                                      | Fact | Perfect separation is detected.                                                                |
| `QuasiSeparationFromAZeroCellIsFlaggedByHugeCoefficientsOrNonConvergence`          | Fact | Quasi separation from a zero cell is flagged by huge coefficients or non convergence.          |
| `ConstantColumnsAreExcludedAndReported`                                            | Fact | Constant columns are excluded and reported.                                                    |
| `NearConstantColumnsAreReported`                                                   | Fact | Near constant columns are reported.                                                            |
| `LowEventsPerVariableIsReportedAndAdequateEventsAreNot`                            | Fact | Low events per variable is reported and adequate events are not.                               |
| `HighVarianceInflationIsMeasuredAndReported`                                       | Fact | High variance inflation is measured and reported.                                              |
| `ExactlyDuplicatedPredictorsAreReportedAsInfiniteVariance`                         | Fact | Exactly duplicated predictors are reported as infinite variance.                               |
| `TinyAndMissingTestSplitsAreFlagged`                                               | Fact | Tiny and missing test splits are flagged.                                                      |
| `ImbalanceBeyondOneToOneHundredWithoutWeightsIsFlaggedAndWeightsSilenceIt`         | Fact | Imbalance beyond one to one hundred without weights is flagged and weights silence it.         |
| `ACalibrationSlopeFarFromOneIsFlagged`                                             | Fact | A calibration slope far from one is flagged.                                                   |
| `NonConvergenceIsReportedWhenTheIterationBudgetIsTooSmall`                         | Fact | Non convergence is reported when the iteration budget is too small.                            |
| `TheNoSignificantPredictorsWarningAppearsExactlyWhenEveryPValueExceedsFivePercent` | Fact | The no significant predictors warning appears exactly when every p value exceeds five percent. |
| `BalancedWeightsCarryTheApproximateStandardErrorWarning`                           | Fact | Balanced weights carry the approximate standard error warning.                                 |

### MahalanobisAnomalyExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.MahalanobisAnomalyExplainToAdaptationTests` — 6 tests.

| Test                                                                                  | Kind | Verifies                                                                                          |
|---------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                            | Fact | To adaptation is assignable to the shared explanation interface.                                  |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                   | Fact | To adaptation carries the value and model name and family.                                        |
| `ToAdaptation_ContributionIsInexactAndInTheDistanceSpaceWithTheMahalanobisTermMethod` | Fact | To adaptation contribution is inexact and in the distance space with the mahalanobis term method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesDistanceSpaceOtherwise`    | Fact | To adaptation describes calibration only when calibrated and uses distance space otherwise.       |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                               | Fact | To adaptation maps source from processing type ids when given.                                    |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                       | Fact | To adaptation leaves source null when processing type ids are not given.                          |

### MahalanobisAnomalyRecallTests

`Jube.Test.AdvancedAnalytics.Slice.MahalanobisAnomalyRecallTests` — 12 tests.

| Test                                                                                      | Kind | Verifies                                                                                              |
|-------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------|
| `RecallReproducesTrainingTimeDistancesBitForBit`                                          | Fact | Recall reproduces training time distances bit for bit.                                                |
| `TheFittedDistanceMatchesTheTheoreticalMahalanobisDistanceOnAKnownTwoDimensionalGaussian` | Fact | The fitted distance matches the theoretical mahalanobis distance on a known two dimensional gaussian. |
| `ExplainDecomposesPerDimensionAndDoesNotSumExactlyToTheDistance`                          | Fact | Explain decomposes per dimension and does not sum exactly to the distance.                            |
| `AConstantColumnIsExcludedFromTheCovarianceAndCarriesZeroWeight`                          | Fact | A constant column is excluded from the covariance and carries zero weight.                            |
| `ANearSingularCovarianceIsRegularisedRatherThanThrowing`                                  | Fact | A near singular covariance is regularised rather than throwing.                                       |
| `EveryVariableConstantThrowsALoudException`                                               | Fact | Every variable constant throws a loud exception.                                                      |
| `LabelledTrainingCalibratesWhenBothClassesFallInTheValidationSplitAndUnlabelledDoesNot`   | Fact | Labelled training calibrates when both classes fall in the validation split and unlabelled does not.  |
| `ALargerDistanceCalibratesToAHigherOrEqualProbability`                                    | Fact | A larger distance calibrates to a higher or equal probability.                                        |
| `RecallRejectsARowOfTheWrongLength`                                                       | Fact | Recall rejects a row of the wrong length.                                                             |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                        | Fact | The snapshot round trips through the envelope unchanged.                                              |
| `ATamperedOrForeignSnapshotIsRefused`                                                     | Fact | A tampered or foreign snapshot is refused.                                                            |
| `RecallIsThreadSafeAndDeterministicUnderContention`                                       | Fact | Recall is thread safe and deterministic under contention.                                             |

### MeanShiftClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.MeanShiftClustererRecallTests` — 9 tests.

| Test                                                                      | Kind | Verifies                                                                               |
|---------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `NearestModeDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront`            | Fact | Nearest mode distance is genuinely the minimum and is moved to the front.              |
| `PredictReturnsOneDistancePerModeInModeIndexOrderExceptTheNearestIsFirst` | Fact | Predict returns one distance per mode in mode index order except the nearest is first. |
| `SameDataProducesTheSameModesAndTheSameRecallBitForBit`                   | Fact | Same data produces the same modes and the same recall bit for bit.                     |
| `ADifferentBandwidthCanProduceADifferentModeCount`                        | Fact | A different bandwidth can produce a different mode count.                              |
| `RecallRejectsARowOfTheWrongLength`                                       | Fact | Recall rejects a row of the wrong length.                                              |
| `AMissingInputIsImputedWithTheTrainingMedian`                             | Fact | A missing input is imputed with the training median.                                   |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                        | Fact | The snapshot round trips through the envelope unchanged.                               |
| `ATamperedOrForeignSnapshotIsRefused`                                     | Fact | A tampered or foreign snapshot is refused.                                             |
| `TheEvidenceReportsPlausibleModeSummaries`                                | Fact | The evidence reports plausible mode summaries.                                         |

### MiniBatchKMeansClustererRecallTests

`Jube.Test.AdvancedAnalytics.Slice.MiniBatchKMeansClustererRecallTests` — 13 tests.

| Test                                                                             | Kind | Verifies                                                                                      |
|----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `NearestCentroidDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront`               | Fact | Nearest centroid distance is genuinely the minimum and is moved to the front.                 |
| `PredictReturnsOneDistancePerCentroidInClusterIndexOrderExceptTheNearestIsFirst` | Fact | Predict returns one distance per centroid in cluster index order except the nearest is first. |
| `SameSeedProducesTheSameCentroidsAndTheSameRecallBitForBit`                      | Fact | Same seed produces the same centroids and the same recall bit for bit.                        |
| `ADifferentSeedCanProduceDifferentCentroids`                                     | Fact | A different seed can produce different centroids.                                             |
| `RequestingMoreClustersThanDistinctRowsThrowsAClearException`                    | Fact | Requesting more clusters than distinct rows throws a clear exception.                         |
| `RequestingMoreClustersThanRowsThrowsAClearException`                            | Fact | Requesting more clusters than rows throws a clear exception.                                  |
| `RequestingABatchSizeLargerThanTheTrainingSplitThrowsAClearException`            | Fact | Requesting a batch size larger than the training split throws a clear exception.              |
| `RecallRejectsARowOfTheWrongLength`                                              | Fact | Recall rejects a row of the wrong length.                                                     |
| `AMissingInputIsImputedWithTheTrainingMedian`                                    | Fact | A missing input is imputed with the training median.                                          |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                               | Fact | The snapshot round trips through the envelope unchanged.                                      |
| `ATamperedOrForeignSnapshotIsRefused`                                            | Fact | A tampered or foreign snapshot is refused.                                                    |
| `TheEvidenceReportsPlausibleClusterSummaries`                                    | Fact | The evidence reports plausible cluster summaries.                                             |
| `AnUnsetInitializationBatchSizeIsCappedToTheAvailableTrainingRowsWithoutHanging` | Fact | An unset initialization batch size is capped to the available training rows without hanging.  |

### MultipleLinearRegressionExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.MultipleLinearRegressionExplainToAdaptationTests` — 7 tests.

| Test                                                               | Kind | Verifies                                                                  |
|--------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`         | Fact | To adaptation is assignable to the shared explanation interface.          |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                | Fact | To adaptation carries the value and model name and family.                |
| `ToAdaptation_ContributionIsExactAndReconstructsThePredictedValue` | Fact | To adaptation contribution is exact and reconstructs the predicted value. |
| `ToAdaptation_IsNeverCalibrated`                                   | Fact | To adaptation is never calibrated.                                        |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`            | Fact | To adaptation maps source from processing type ids when given.            |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`    | Fact | To adaptation leaves source null when processing type ids are not given.  |
| `ToAdaptation_NeverCarriesPerCoefficientSignificance`              | Fact | To adaptation never carries per coefficient significance.                 |

### MultipleLinearRegressionRecallTests

`Jube.Test.AdvancedAnalytics.Slice.MultipleLinearRegressionRecallTests` — 13 tests.

| Test                                                                              | Kind | Verifies                                                                                       |
|-----------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `RecallReproducesTrainingTimePredictionsBitForBitIncludingMissingInputs`          | Fact | Recall reproduces training time predictions bit for bit including missing inputs.              |
| `RecallReproducesTheUnstandardisedPipelineToo`                                    | Fact | Recall reproduces the unstandardised pipeline too.                                             |
| `APredictionIsExactlyTheDotProductOfWeightsAndStandardisedInputsPlusTheIntercept` | Fact | A prediction is exactly the dot product of weights and standardised inputs plus the intercept. |
| `AMissingInputIsImputedWithTheTrainingMedianAndFlagged`                           | Fact | A missing input is imputed with the training median and flagged.                               |
| `RecallRejectsMissingValuesWhenTheModelWasTrainedToReject`                        | Fact | Recall rejects missing values when the model was trained to reject.                            |
| `RecallRejectsARowOfTheWrongLength`                                               | Fact | Recall rejects a row of the wrong length.                                                      |
| `FewerRowsThanVariablesThrowsAClearException`                                     | Fact | Fewer rows than variables throws a clear exception.                                            |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                                | Fact | The snapshot round trips through the envelope unchanged.                                       |
| `ATamperedOrForeignSnapshotIsRefused`                                             | Fact | A tampered or foreign snapshot is refused.                                                     |
| `RecallIsThreadSafeAndDeterministicUnderContention`                               | Fact | Recall is thread safe and deterministic under contention.                                      |
| `TestSplitRowsNeverTouchTheFittedTrainingStatistics`                              | Fact | Test split rows never touch the fitted training statistics.                                    |
| `SameSeedProducesTheSameSnapshotAndDifferentSeedsDoNot`                           | Fact | Same seed produces the same snapshot and different seeds do not.                               |
| `TheResultSerialisesForAnLlmWithoutBulkPredictionArrays`                          | Fact | The result serialises for an llm without bulk prediction arrays.                               |

### NaiveBayesClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.NaiveBayesClassifierExplainToAdaptationTests` — 6 tests.

| Test                                                                                  | Kind | Verifies                                                                                        |
|---------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                            | Fact | To adaptation is assignable to the shared explanation interface.                                |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                   | Fact | To adaptation carries the value and model name and family.                                      |
| `ToAdaptation_ContributionIsExactAndInLogOddsSpaceWithTheLogLikelihoodRatioMethod`    | Fact | To adaptation contribution is exact and in log odds space with the log likelihood ratio method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpaceEitherWay` | Fact | To adaptation describes calibration only when calibrated and uses probability space either way. |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                               | Fact | To adaptation maps source from processing type ids when given.                                  |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                       | Fact | To adaptation leaves source null when processing type ids are not given.                        |

### NaiveBayesClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.NaiveBayesClassifierRecallTests` — 11 tests.

| Test                                                                     | Kind | Verifies                                                                             |
|--------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `ClearlyPositiveAndNegativeProbesRecallOnTheCorrectSideOfOneHalf`        | Fact | Clearly positive and negative probes recall on the correct side of one half.         |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`               | Fact | Same seed produces the same snapshot and the same recall bit for bit.                |
| `ExplainContributionsPlusThePriorSumExactlyToTheRawLogOdds`              | Fact | Explain contributions plus the prior sum exactly to the raw log odds.                |
| `ExplainContributionsAreGenuinelyExactNotJustFlagged`                    | Fact | Explain contributions are genuinely exact not just flagged.                          |
| `ConstantFeatureAcrossTheWholeTrainingSplitIsWarnedAboutButDoesNotThrow` | Fact | Constant feature across the whole training split is warned about but does not throw. |
| `ASingleTrainingRowInAClassFloorsTheVarianceInsteadOfThrowing`           | Fact | A single training row in a class floors the variance instead of throwing.            |
| `RecallRejectsARowOfTheWrongLength`                                      | Fact | Recall rejects a row of the wrong length.                                            |
| `AMissingInputIsImputedWithTheTrainingMedian`                            | Fact | A missing input is imputed with the training median.                                 |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                       | Fact | The snapshot round trips through the envelope unchanged.                             |
| `ATamperedOrForeignSnapshotIsRefused`                                    | Fact | A tampered or foreign snapshot is refused.                                           |
| `TheEvidenceReportsPlausibleMetricsAndCalibration`                       | Fact | The evidence reports plausible metrics and calibration.                              |

### NeuralNetworkClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.NeuralNetworkClassifierExplainToAdaptationTests` — 6 tests.

| Test                                                                          | Kind | Verifies                                                                                  |
|-------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                    | Fact | To adaptation is assignable to the shared explanation interface.                          |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                           | Fact | To adaptation carries the value and model name and family.                                |
| `ToAdaptation_ContributionIsInexactAndInTheLogOddsSpaceWithTheGradientMethod` | Fact | To adaptation contribution is inexact and in the log odds space with the gradient method. |
| `ToAdaptation_AlwaysDescribesCalibrationInTheProbabilitySpace`                | Fact | To adaptation always describes calibration in the probability space.                      |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                       | Fact | To adaptation maps source from processing type ids when given.                            |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`               | Fact | To adaptation leaves source null when processing type ids are not given.                  |

### NeuralNetworkClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.NeuralNetworkClassifierRecallTests` — 10 tests.

| Test                                                      | Kind | Verifies                                                             |
|-----------------------------------------------------------|------|----------------------------------------------------------------------|
| `RecallLearnsACleanLinearlySeparableBoundaryReliably`     | Fact | Recall learns a clean linearly separable boundary reliably.          |
| `PredictReturnsASingleProbabilityBetweenZeroAndOne`       | Fact | Predict returns a single probability between zero and one.           |
| `SameSeedProducesTheSameWeightsAndTheSameRecallBitForBit` | Fact | Same seed produces the same weights and the same recall bit for bit. |
| `ADifferentSeedCanProduceADifferentSnapshot`              | Fact | A different seed can produce a different snapshot.                   |
| `RecallRejectsARowOfTheWrongLength`                       | Fact | Recall rejects a row of the wrong length.                            |
| `AMissingInputIsImputedWithTheTrainingMedian`             | Fact | A missing input is imputed with the training median.                 |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`        | Fact | The snapshot round trips through the envelope unchanged.             |
| `ATamperedOrForeignSnapshotIsRefused`                     | Fact | A tampered or foreign snapshot is refused.                           |
| `BothClassesMustBePresentInTheTrainingSplit`              | Fact | Both classes must be present in the training split.                  |
| `ExplainGradientsAreFiniteAndOneEntryPerVariable`         | Fact | Explain gradients are finite and one entry per variable.             |

### NeuralNetworkRegressorExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.NeuralNetworkRegressorExplainToAdaptationTests` — 6 tests.

| Test                                                                        | Kind | Verifies                                                                               |
|-----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                  | Fact | To adaptation is assignable to the shared explanation interface.                       |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                         | Fact | To adaptation carries the value and model name and family.                             |
| `ToAdaptation_ContributionIsInexactAndInTheScoreSpaceWithTheGradientMethod` | Fact | To adaptation contribution is inexact and in the score space with the gradient method. |
| `ToAdaptation_NeverCalibratesAndUsesTheScoreSpace`                          | Fact | To adaptation never calibrates and uses the score space.                               |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                     | Fact | To adaptation maps source from processing type ids when given.                         |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`             | Fact | To adaptation leaves source null when processing type ids are not given.               |

### NeuralNetworkRegressorRecallTests

`Jube.Test.AdvancedAnalytics.Slice.NeuralNetworkRegressorRecallTests` — 10 tests.

| Test                                                                         | Kind | Verifies                                                                               |
|------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `RecallLearnsANonlinearRelationshipReliably`                                 | Fact | Recall learns a nonlinear relationship reliably.                                       |
| `PredictReturnsASingleFiniteValue`                                           | Fact | Predict returns a single finite value.                                                 |
| `SameSeedProducesTheSameWeightsAndTheSameRecallBitForBit`                    | Fact | Same seed produces the same weights and the same recall bit for bit.                   |
| `ADifferentSeedCanProduceADifferentSnapshot`                                 | Fact | A different seed can produce a different snapshot.                                     |
| `RecallRejectsARowOfTheWrongLength`                                          | Fact | Recall rejects a row of the wrong length.                                              |
| `AMissingInputIsImputedWithTheTrainingMedian`                                | Fact | A missing input is imputed with the training median.                                   |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                           | Fact | The snapshot round trips through the envelope unchanged.                               |
| `ATamperedOrForeignSnapshotIsRefused`                                        | Fact | A tampered or foreign snapshot is refused.                                             |
| `AConstantTrainingTargetProducesAFiniteConstantPredictionInsteadOfDiverging` | Fact | A constant training target produces a finite constant prediction instead of diverging. |
| `ExplainGradientsAreFiniteAndOneEntryPerVariable`                            | Fact | Explain gradients are finite and one entry per variable.                               |

### OneClassSupportVectorAnomalyRecallTests

`Jube.Test.AdvancedAnalytics.Slice.OneClassSupportVectorAnomalyRecallTests` — 11 tests.

| Test                                                               | Kind | Verifies                                                                   |
|--------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `FarOutliersScoreHigherThanTheNormalClusterTheyWereFittedOn`       | Fact | Far outliers score higher than the normal cluster they were fitted on.     |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`         | Fact | Same seed produces the same snapshot and the same recall bit for bit.      |
| `RecallRejectsARowOfTheWrongLength`                                | Fact | Recall rejects a row of the wrong length.                                  |
| `AMissingInputIsImputedWithTheTrainingMedian`                      | Fact | A missing input is imputed with the training median.                       |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                 | Fact | The snapshot round trips through the envelope unchanged.                   |
| `ATamperedOrForeignSnapshotIsRefused`                              | Fact | A tampered or foreign snapshot is refused.                                 |
| `UnlabelledTrainingFitsWithNoCalibratorAndReportsTheRawScore`      | Fact | Unlabelled training fits with no calibrator and reports the raw score.     |
| `LabelledTrainingFitsACalibratorAndProducesARealRocAuc`            | Fact | Labelled training fits a calibrator and produces a real roc auc.           |
| `NuRoughlyControlsHowManyTrainingRowsBecomeSupportVectors`         | Fact | Nu roughly controls how many training rows become support vectors.         |
| `TheEvidenceReportsPlausibleScorePercentilesAndSupportVectorCount` | Fact | The evidence reports plausible score percentiles and support vector count. |
| `RecallIsThreadSafeAndDeterministicUnderContention`                | Fact | Recall is thread safe and deterministic under contention.                  |

### PredictorSignificanceTests

`Jube.Test.AdvancedAnalytics.Slice.PredictorSignificanceTests` — 6 tests.

| Test                                                               | Kind | Verifies                                                                    |
|--------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `MatchesAnIndependentPythonUnivariateLogisticImplementation`       | Fact | Matches an independent python univariate logistic implementation.           |
| `BenjaminiHochbergMatchesTheOriginalPaperExample`                  | Fact | Benjamini hochberg matches the original paper example.                      |
| `BenjaminiHochbergKeepsInputOrderSkipsUndefinedAndNeverExceedsOne` | Fact | Benjamini hochberg keeps input order skips undefined and never exceeds one. |
| `TruePredictorsAreFoundAndAdjustedNoiseFalsePositivesAreRare`      | Fact | True predictors are found and adjusted noise false positives are rare.      |
| `SeparatedConstantAndSparseVariablesReturnANoteInsteadOfNumbers`   | Fact | Separated constant and sparse variables return a note instead of numbers.   |
| `InvalidArgumentsAreRejectedAndCancellationIsHonoured`             | Fact | Invalid arguments are rejected and cancellation is honoured.                |

### PrincipalComponentRecallTests

`Jube.Test.AdvancedAnalytics.Slice.PrincipalComponentRecallTests` — 10 tests.

| Test                                                             | Kind | Verifies                                                                  |
|------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `RecallReproducesTrainingTimeProjectionsBitForBit`               | Fact | Recall reproduces training time projections bit for bit.                  |
| `Pc1IsTheDominantDirectionOfPositivelyCorrelatedVariables`       | Fact | Pc1 is the dominant direction of positively correlated variables.         |
| `RecallMatchesAccordsOwnTransformOnTheSameStandardisedRows`      | Fact | Recall matches accords own transform on the same standardised rows.       |
| `TrainingIsDeterministicForTheSameSeed`                          | Fact | Training is deterministic for the same seed.                              |
| `FewerRowsThanVariablesStillTrainsButCapsTheAvailableComponents` | Fact | Fewer rows than variables still trains but caps the available components. |
| `RecallRejectsARowOfTheWrongLength`                              | Fact | Recall rejects a row of the wrong length.                                 |
| `AMissingInputIsImputedWithTheTrainingMedian`                    | Fact | A missing input is imputed with the training median.                      |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`               | Fact | The snapshot round trips through the envelope unchanged.                  |
| `ATamperedOrForeignSnapshotIsRefused`                            | Fact | A tampered or foreign snapshot is refused.                                |
| `ElementZeroIsAlwaysThePc1Score`                                 | Fact | Element zero is always the pc1 score.                                     |

### RandomForestClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.RandomForestClassifierExplainToAdaptationTests` — 6 tests.

| Test                                                                                        | Kind | Verifies                                                                                                 |
|---------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                                  | Fact | To adaptation is assignable to the shared explanation interface.                                         |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                                         | Fact | To adaptation carries the value and model name and family.                                               |
| `ToAdaptation_ContributionIsInexactAndInVoteFractionSpaceWithTheTreePathContributionMethod` | Fact | To adaptation contribution is inexact and in vote fraction space with the tree path contribution method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpace`                | Fact | To adaptation describes calibration only when calibrated and uses probability space.                     |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                                     | Fact | To adaptation maps source from processing type ids when given.                                           |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                             | Fact | To adaptation leaves source null when processing type ids are not given.                                 |

### RandomForestClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.RandomForestClassifierRecallTests` — 11 tests.

| Test                                                                 | Kind | Verifies                                                                      |
|----------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `SeparableDataProducesHighProbabilityForPositivesAndLowForNegatives` | Fact | Separable data produces high probability for positives and low for negatives. |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`           | Fact | Same seed produces the same snapshot and the same recall bit for bit.         |
| `ADifferentSeedCanProduceADifferentSnapshot`                         | Fact | A different seed can produce a different snapshot.                            |
| `PredictedProbabilitiesAreAlwaysBetweenZeroAndOne`                   | Fact | Predicted probabilities are always between zero and one.                      |
| `RecallRejectsARowOfTheWrongLength`                                  | Fact | Recall rejects a row of the wrong length.                                     |
| `AMissingInputIsImputedWithTheTrainingMedian`                        | Fact | A missing input is imputed with the training median.                          |
| `RequiringBothClassesThrowsAClearExceptionWhenOnlyOneIsPresent`      | Fact | Requiring both classes throws a clear exception when only one is present.     |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                   | Fact | The snapshot round trips through the envelope unchanged.                      |
| `ATamperedOrForeignSnapshotIsRefused`                                | Fact | A tampered or foreign snapshot is refused.                                    |
| `TheEvidenceReportsPlausibleForestSummaries`                         | Fact | The evidence reports plausible forest summaries.                              |
| `ExplainRawScoreMatchesThePipelineRawScore`                          | Fact | Explain raw score matches the pipeline raw score.                             |

### RandomForestRegressorExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.RandomForestRegressorExplainToAdaptationTests` — 7 tests.

| Test                                                                              | Kind | Verifies                                                                                     |
|-----------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                        | Fact | To adaptation is assignable to the shared explanation interface.                             |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamily`                               | Fact | To adaptation carries the value and model name and family.                                   |
| `ToAdaptation_ContributionIsInexactInScoreSpaceWithTheTreePathContributionMethod` | Fact | To adaptation contribution is inexact in score space with the tree path contribution method. |
| `ToAdaptation_IsNeverCalibratedRegardlessOfRequestedValue`                        | Fact | To adaptation is never calibrated regardless of requested value.                             |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                           | Fact | To adaptation maps source from processing type ids when given.                               |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                   | Fact | To adaptation leaves source null when processing type ids are not given.                     |
| `ToAdaptation_NeverCarriesPerVariableSignificance`                                | Fact | To adaptation never carries per variable significance.                                       |

### RandomForestRegressorRecallTests

`Jube.Test.AdvancedAnalytics.Slice.RandomForestRegressorRecallTests` — 13 tests.

| Test                                                                     | Kind   | Verifies                                                                          |
|--------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `StrongSignalDataProducesAHighTestRSquared`                              | Fact   | Strong signal data produces a high test r squared.                                |
| `RecallReproducesTrainingTimePredictionsBitForBitIncludingMissingInputs` | Fact   | Recall reproduces training time predictions bit for bit including missing inputs. |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`               | Fact   | Same seed produces the same snapshot and the same recall bit for bit.             |
| `ADifferentSeedCanProduceADifferentSnapshot`                             | Fact   | A different seed can produce a different snapshot.                                |
| `RecallRejectsARowOfTheWrongLength`                                      | Fact   | Recall rejects a row of the wrong length.                                         |
| `AMissingInputIsImputedWithTheTrainingMedian`                            | Fact   | A missing input is imputed with the training median.                              |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                       | Fact   | The snapshot round trips through the envelope unchanged.                          |
| `ATamperedOrForeignSnapshotIsRefused`                                    | Fact   | A tampered or foreign snapshot is refused.                                        |
| `TheEvidenceReportsPlausibleForestSummaries`                             | Fact   | The evidence reports plausible forest summaries.                                  |
| `ExplainPredictedValueEqualsBaseValuePlusContributions`                  | Fact   | Explain predicted value equals base value plus contributions.                     |
| `FeatureFractionResolvesToAPlausibleFeaturesPerSplit`                    | Theory | Feature fraction resolves to a plausible features per split.                      |
| `FewerRowsThanTreesStillTrainsSuccessfully`                              | Fact   | Fewer rows than trees still trains successfully.                                  |
| `RecallIsThreadSafeAndDeterministicUnderContention`                      | Fact   | Recall is thread safe and deterministic under contention.                         |

### TreeAugmentedNaiveBayesClassifierExplainToAdaptationTests

`Jube.Test.AdvancedAnalytics.Slice.TreeAugmentedNaiveBayesClassifierExplainToAdaptationTests` — 6 tests.

| Test                                                                                  | Kind | Verifies                                                                                        |
|---------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ToAdaptation_IsAssignableToTheSharedExplanationInterface`                            | Fact | To adaptation is assignable to the shared explanation interface.                                |
| `ToAdaptation_CarriesTheValueAndModelNameAndFamilyAndChowLiuStructureLearning`        | Fact | To adaptation carries the value and model name and family and chow liu structure learning.      |
| `ToAdaptation_ContributionIsExactAndInLogOddsSpaceWithTheLogLikelihoodRatioMethod`    | Fact | To adaptation contribution is exact and in log odds space with the log likelihood ratio method. |
| `ToAdaptation_DescribesCalibrationOnlyWhenCalibratedAndUsesProbabilitySpaceEitherWay` | Fact | To adaptation describes calibration only when calibrated and uses probability space either way. |
| `ToAdaptation_MapsSourceFromProcessingTypeIdsWhenGiven`                               | Fact | To adaptation maps source from processing type ids when given.                                  |
| `ToAdaptation_LeavesSourceNullWhenProcessingTypeIdsAreNotGiven`                       | Fact | To adaptation leaves source null when processing type ids are not given.                        |

### TreeAugmentedNaiveBayesClassifierRecallTests

`Jube.Test.AdvancedAnalytics.Slice.TreeAugmentedNaiveBayesClassifierRecallTests` — 12 tests.

| Test                                                                          | Kind | Verifies                                                                                   |
|-------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `ClearlyPositiveAndNegativeProbesRecallOnTheCorrectSideOfOneHalf`             | Fact | Clearly positive and negative probes recall on the correct side of one half.               |
| `SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit`                    | Fact | Same seed produces the same snapshot and the same recall bit for bit.                      |
| `ExplainContributionsPlusThePriorSumExactlyToTheRawLogOdds`                   | Fact | Explain contributions plus the prior sum exactly to the raw log odds.                      |
| `ExplainContributionsAreGenuinelyExactNotJustFlagged`                         | Fact | Explain contributions are genuinely exact not just flagged.                                |
| `ConstantFeatureAcrossTheWholeTrainingSplitIsWarnedAboutButDoesNotThrow`      | Fact | Constant feature across the whole training split is warned about but does not throw.       |
| `RecallRejectsARowOfTheWrongLength`                                           | Fact | Recall rejects a row of the wrong length.                                                  |
| `AMissingInputIsImputedWithTheTrainingMedian`                                 | Fact | A missing input is imputed with the training median.                                       |
| `TheSnapshotRoundTripsThroughTheEnvelopeUnchanged`                            | Fact | The snapshot round trips through the envelope unchanged.                                   |
| `ATamperedOrForeignSnapshotIsRefused`                                         | Fact | A tampered or foreign snapshot is refused.                                                 |
| `TheEvidenceReportsPlausibleMetricsAndCalibration`                            | Fact | The evidence reports plausible metrics and calibration.                                    |
| `TheChowLiuTreeDiscoversAGenuineWithinClassDependencyAndFitsAMeaningfulSlope` | Fact | The chow liu tree discovers a genuine within class dependency and fits a meaningful slope. |
| `ASingleVariableModelHasNoParentAndStillPredicts`                             | Fact | A single variable model has no parent and still predicts.                                  |
