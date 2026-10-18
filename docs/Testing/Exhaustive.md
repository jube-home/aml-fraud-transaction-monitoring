---
layout: default
title: Exhaustive Machine Learning
nav_order: 5
parent: Testing
---

# Exhaustive Machine Learning

Tests of the Exhaustive machine-learning training pipeline: dataset assembly, variable preparation, training trials,
promotion and model recall in the engine.

## General

### Accuracy

`Jube.Test.Exhaustive.Accuracy` — 4 tests.

| Test                                   | Kind   | Verifies                                |
|----------------------------------------|--------|-----------------------------------------|
| `AssertAccuracy_BelowThreshold_Fails`  | Fact   | Assert accuracy below threshold fails.  |
| `AssertAccuracy_NoRows_Fails`          | Fact   | Assert accuracy no rows fails.          |
| `AssertAccuracy_AboveThreshold_Passes` | Fact   | Assert accuracy above threshold passes. |
| `BatchAccuracyAsync`                   | Theory | Batch accuracy.                         |

### AnomalyRunnerTests

`Jube.Test.Exhaustive.AnomalyRunnerTests` — 12 tests.

| Test                                                                 | Kind | Verifies                                                                      |
|----------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `TheOneClassSvmPathProducesTheSameLabelsAsTheOriginalCodePath`       | Fact | The one class svm path produces the same labels as the original code path.    |
| `TheOneClassSvmAdapterScoresTheNegatedProbabilityAndIsThreadSafe`    | Fact | The one class svm adapter scores the negated probability and is thread safe.  |
| `TheAdapterHonoursCancellationAndRefusesUnfittedUse`                 | Fact | The adapter honours cancellation and refuses unfitted use.                    |
| `ANativeDetectorFlagsTheContaminationFractionAndReportsTheReduction` | Fact | A native detector flags the contamination fraction and reports the reduction. |
| `RunsAreDeterministic`                                               | Fact | Runs are deterministic.                                                       |
| `ReductionCanBeSwitchedOffForANativeDetector`                        | Fact | Reduction can be switched off for a native detector.                          |
| `TheContaminationParameterOverridesTheProbabilityAndIsValidated`     | Fact | The contamination parameter overrides the probability and is validated.       |
| `AutomaticRecordsEveryCandidateAndResolvesTheAlgorithm`              | Fact | Automatic records every candidate and resolves the algorithm.                 |
| `AutomaticUsesTrustedPositivesWhenSupplied`                          | Fact | Automatic uses trusted positives when supplied.                               |
| `ACancelledRunThrows`                                                | Fact | A cancelled run throws.                                                       |
| `TheAutomaticCandidateSetIsRestrictedByDimensionalityAndRows`        | Fact | The automatic candidate set is restricted by dimensionality and rows.         |
| `AnExplicitCandidateListOverridesTheDefaultsButNeverAdmitsTheSvm`    | Fact | An explicit candidate list overrides the defaults but never admits the svm.   |

### CategoricalRecallHttpTests

`Jube.Test.Exhaustive.CategoricalRecallHttpTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                          | Kind   | Verifies                                                         |
|---------------------------------------------------------------|--------|------------------------------------------------------------------|
| `ACategoricalStringIsAcceptedAndScoredThroughTheEncoderAsync` | Theory | A categorical string is accepted and scored through the encoder. |
| `WrongTypesAreStillRejectedWith400Async`                      | Theory | Wrong types are still rejected with400.                          |

### CategoricalRecallTests

`Jube.Test.Exhaustive.CategoricalRecallTests` — 14 tests. Category: Unit.

| Test                                                                             | Kind   | Verifies                                                                                    |
|----------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `Sync_BuildsTheEncoderFromTheStoredVocabularyAndSharesItAcrossIndicators`        | Fact   | Sync builds the encoder from the stored vocabulary and shares it across indicators.         |
| `Sync_AnInvalidVocabularyReportsALoadError`                                      | Fact   | Sync an invalid vocabulary reports a load error.                                            |
| `Sync_ModelsWithoutCategoricalMetadataAreUnchanged`                              | Fact   | Sync models without categorical metadata are unchanged.                                     |
| `BuildScoreInputs_ComputesTheIndicatorsFromTheIncomingString`                    | Theory | Build score inputs computes the indicators from the incoming string.                        |
| `Score_AppliesTheNetworkToTheIndicators`                                         | Fact   | Score applies the network to the indicators.                                                |
| `Score_ANormalisedIndicatorIsStandardisedLikeTraining`                           | Fact   | Score a normalised indicator is standardised like training.                                 |
| `EliminatedIndicatorsFoldTheLevelIntoTheReference`                               | Fact   | Eliminated indicators fold the level into the reference.                                    |
| `PipelineAndHttpRecallComputeIdenticalInputsForTheSameString`                    | Theory | Pipeline and http recall compute identical inputs for the same string.                      |
| `Pipeline_AbsentPayloadValueIsMissing`                                           | Fact   | Pipeline absent payload value is missing.                                                   |
| `Pipeline_NumericPayloadValueBecomesItsInvariantString`                          | Fact   | Pipeline numeric payload value becomes its invariant string.                                |
| `TypeGate_AcceptsStringsForACategoricalSourceAndStillRejectsWrongTypesElsewhere` | Fact   | Type gate accepts strings for a categorical source and still rejects wrong types elsewhere. |
| `TypeGate_TheIndicatorNameIsNotARequestKey`                                      | Fact   | Type gate the indicator name is not a request key.                                          |
| `TypeGate_ModelsWithoutCategoricalMetadataRejectStringsAsBefore`                 | Fact   | Type gate models without categorical metadata reject strings as before.                     |
| `HostileStringsNeverThrowOrProduceNonFiniteInputs`                               | Fact   | Hostile strings never throw or produce non finite inputs.                                   |

### CategoricalTrainingTests

`Jube.Test.Exhaustive.CategoricalTrainingTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                                 | Kind | Verifies                                                                                  |
|--------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `Extraction_ExplodesAPayloadStringFieldIntoIndicatorColumnsFittedOnBothClassesAsync` | Fact | Extraction explodes a payload string field into indicator columns fitted on both classes. |
| `Extraction_WithoutTheClassRowsInTheFitAFraudOnlyLevelWouldBeLostAsync`              | Fact | Extraction without the class rows in the fit a fraud only level would be lost.            |
| `Extraction_WithoutTheOptionOnlyTheNumericFieldsAreReadAsBeforeAsync`                | Fact | Extraction without the option only the numeric fields are read as before.                 |
| `Extraction_AnUndeclaredTypeIsInferredFromTheSampleAsync`                            | Fact | Extraction an undeclared type is inferred from the sample.                                |
| `Training_ReachesAPromotedTrialWithIndicatorVariablesAndPersistsTheMetadataAsync`    | Fact | Training reaches a promoted trial with indicator variables and persists the metadata.     |

### ChallengerCategoricalRecallTests

`Jube.Test.Exhaustive.ChallengerCategoricalRecallTests` — 7 tests. Category: Unit.

| Test                                                                         | Kind   | Verifies                                                                               |
|------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `Sync_AChallengerKindBuildsChallengerRecallNotATopologyNetwork`              | Fact   | Sync a challenger kind builds challenger recall not a topology network.                |
| `PipelineAndExternalRecallComputeIdenticalScoresForAChallengerModel`         | Theory | Pipeline and external recall compute identical scores for a challenger model.          |
| `ChallengerCategoricalIndicatorIsNeverZScored`                               | Fact   | Challenger categorical indicator is never z scored.                                    |
| `ChallengerContinuousVariableIsNotStandardisedEither`                        | Fact   | Challenger continuous variable is not standardised either.                             |
| `Pipeline_AbsentPayloadValueFallsBackToTheReferenceIndicatorAndTrainingMean` | Fact   | Pipeline absent payload value falls back to the reference indicator and training mean. |
| `Pipeline_NumericPayloadValueBecomesItsInvariantStringForAChallengerModel`   | Fact   | Pipeline numeric payload value becomes its invariant string for a challenger model.    |
| `HostileStringsNeverThrowOrProduceNonFiniteInputsForAChallengerModel`        | Fact   | Hostile strings never throw or produce non finite inputs for a challenger model.       |

### ChallengerModelTests

`Jube.Test.Exhaustive.ChallengerModelTests` — 7 tests.

| Test                                                                  | Kind   | Verifies                                                                     |
|-----------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `ChallengerProbability_IsBaseRateWhenComplexityIsZero`                | Fact   | Challenger probability is base rate when complexity is zero.                 |
| `ChallengerProbability_RampsTowardsOneAsComplexityApproachesTheLimit` | Fact   | Challenger probability ramps towards one as complexity approaches the limit. |
| `ChallengerProbability_IsUnaffectedByAZeroLimit`                      | Fact   | Challenger probability is unaffected by a zero limit.                        |
| `SelectFamily_NeverChallengesWhenProbabilityIsZero`                   | Fact   | Select family never challenges when probability is zero.                     |
| `SelectFamily_AlwaysReturnsARecognisedKindWhenProbabilityIsOne`       | Fact   | Select family always returns a recognised kind when probability is one.      |
| `Train_ProducesARecognisableModelWithSaneRecall`                      | Theory | Train produces a recognisable model with sane recall.                        |
| `Train_UnrecognisedKindThrows`                                        | Fact   | Train unrecognised kind throws.                                              |

### DemoDataSourceTests

`Jube.Test.Exhaustive.DemoDataSourceTests` — 3 tests. Collection: Database.

| Test                                                               | Kind | Verifies                                                               |
|--------------------------------------------------------------------|------|------------------------------------------------------------------------|
| `DemoDataset_LoadsTheBuiltInRowsOncePerModelIntoMockArchiveAsync`  | Fact | Demo dataset loads the built in rows once per model into mock archive. |
| `ArchiveDataSource_NeverLoadsTheDemoRowsAsync`                     | Fact | Archive data source never loads the demo rows.                         |
| `TheRetiredEnvironmentVariable_HasNoEffectOnEitherDataSourceAsync` | Fact | The retired environment variable has no effect on either data source.  |

### ExhaustiveBaseRateTests

`Jube.Test.Exhaustive.ExhaustiveBaseRateTests` — 9 tests. Category: Service. Collection: Database.

| Test                                                                    | Kind   | Verifies                                                                     |
|-------------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `Archive_TheBaseRateIsTheCountOfFilterMatchesOverThePopulationAsync`    | Fact   | Archive the base rate is the count of filter matches over the population.    |
| `Archive_TheSampleCapsDoNotDistortTheBaseRateAsync`                     | Fact   | Archive the sample caps do not distort the base rate.                        |
| `Archive_OtherModelsAndTheMockTableAreNotCountedAsync`                  | Fact   | Archive other models and the mock table are not counted.                     |
| `Demo_TheMockArchiveIsCountedInsteadOfTheArchiveAsync`                  | Fact   | Demo the mock archive is counted instead of the archive.                     |
| `Archive_AFilterThatMatchesNothingOrEverythingHasNoUsableBaseRateAsync` | Fact   | Archive a filter that matches nothing or everything has no usable base rate. |
| `Archive_AFailureIsLoggedAndFallsBackToTheSamplePriorAsync`             | Fact   | Archive a failure is logged and falls back to the sample prior.              |
| `Dataset_TheClassProportionIsCountedOverTheWholeDatasetAsync`           | Fact   | Dataset the class proportion is counted over the whole dataset.              |
| `Dataset_TheClassProportionIsCountedForBothStoredDatasetKindsAsync`     | Theory | Dataset the class proportion is counted for both stored dataset kinds.       |
| `TheCountSql_IsScopedToTheKeyAndTheFilter`                              | Fact   | The count sql is scoped to the key and the filter.                           |

### ExhaustiveCalibrationConceptTests

`Jube.Test.Exhaustive.ExhaustiveCalibrationConceptTests` — 5 tests. Category: Unit.

| Test                                                                                                       | Kind | Verifies                                                                                                                |
|------------------------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------------------|
| `AtATwoPercentBaseRate_TheCalibratedMeanMatchesAndTheRawMeanDoesNotAsync`                                  | Fact | At a two percent base rate the calibrated mean matches and the raw mean does not.                                       |
| `AtATwoPercentBaseRate_EceBrierAndLogLossImproveOnTheDeploymentPopulation_AndAucIsUnchangedAsync`          | Fact | At a two percent base rate ece brier and log loss improve on the deployment population and auc is unchanged.            |
| `TheStoredQuality_ShowsTheImprovementOnTheBalancedTestSplitAndAtTheBaseRateAsync`                          | Fact | The stored quality shows the improvement on the balanced test split and at the base rate.                               |
| `TheOptimismOfCalibratingOnTheSelectionSplitIsSmallComparedWithACalibratorFittedOnTheTestSplitItselfAsync` | Fact | The optimism of calibrating on the selection split is small compared with a calibrator fitted on the test split itself. |
| `TheCalibratorIsFittedOnTheValidationSplit_NotOnTheTestLabelsAsync`                                        | Fact | The calibrator is fitted on the validation split not on the test labels.                                                |

### ExhaustiveCalibrationFitterTests

`Jube.Test.Exhaustive.ExhaustiveCalibrationFitterTests` — 26 tests. Category: Unit.

| Test                                                                                      | Kind   | Verifies                                                                                             |
|-------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `Fit_SigmoidOutput_UsesTheLogitInputAndMatchesTheIndependentReference`                    | Fact   | Fit sigmoid output uses the logit input and matches the independent reference.                       |
| `Fit_LinearOutput_UsesTheRawInput`                                                        | Fact   | Fit linear output uses the raw input.                                                                |
| `Fit_BoundedAndUnboundedNonSigmoidFamilies_UseTheRawInput`                                | Theory | Fit bounded and unbounded non sigmoid families use the raw input.                                    |
| `ApplyingTheStoredColumns_ReproducesTheReferenceProbabilitiesAtTheBaseRate`               | Fact   | Applying the stored columns reproduces the reference probabilities at the base rate.                 |
| `Fit_BeforeAndAfterQuality_MatchesTheIndependentReference`                                | Fact   | Fit before and after quality matches the independent reference.                                      |
| `Fit_QualityRoundTripsThroughItsJson`                                                     | Fact   | Fit quality round trips through its json.                                                            |
| `Fit_AnUnusableBaseRate_LeavesTheProbabilityAtTheSamplePrior`                             | Fact   | Fit an unusable base rate leaves the probability at the sample prior.                                |
| `Fit_HardOutputFamilies_AreSkippedWithARecordedReason`                                    | Theory | Fit hard output families are skipped with a recorded reason.                                         |
| `Fit_SingleClassValidation_IsSkipped`                                                     | Fact   | Fit single class validation is skipped.                                                              |
| `Fit_ConstantScores_AreSkipped`                                                           | Fact   | Fit constant scores are skipped.                                                                     |
| `Fit_ScoresWithTwoDistinctValues_AreSkippedAsAHardOutput`                                 | Fact   | Fit scores with two distinct values are skipped as a hard output.                                    |
| `Fit_IsotonicRequested_FitsAMonotoneStepFunctionAndImprovesLogLoss`                       | Fact   | Fit isotonic requested fits a monotone step function and improves log loss.                          |
| `Fit_BetaRequested_OnASigmoidFamily_FitsAndImprovesLogLoss`                               | Fact   | Fit beta requested on a sigmoid family fits and improves log loss.                                   |
| `Fit_BetaRequested_OnANonSigmoidFamily_IsSkippedWithAReason`                              | Fact   | Fit beta requested on a non sigmoid family is skipped with a reason.                                 |
| `Fit_Automatic_PicksACandidateAtLeastAsGoodAsPlattOnValidationBrier`                      | Fact   | Fit automatic picks a candidate at least as good as platt on validation brier.                       |
| `Fit_Automatic_OnANonSigmoidFamily_NeverPicksBeta`                                        | Fact   | Fit automatic on a non sigmoid family never picks beta.                                              |
| `Fit_AnUnknownRequestedMethod_FallsBackToAutomatic`                                       | Fact   | Fit an unknown requested method falls back to automatic.                                             |
| `ApplyingStoredIsotonicColumns_RoundTripsThroughFromColumns`                              | Fact   | Applying stored isotonic columns round trips through from columns.                                   |
| `ApplyingStoredBetaColumns_RoundTripsThroughFromColumns`                                  | Fact   | Applying stored beta columns round trips through from columns.                                       |
| `FromColumns_AnUnknownMethod_IsInvalidAndThrowsOnApply`                                   | Fact   | From columns an unknown method is invalid and throws on apply.                                       |
| `Fit_NonFiniteScores_AreSkipped`                                                          | Fact   | Fit non finite scores are skipped.                                                                   |
| `Fit_TooFewPositives_IsSkippedAndTheMinimumIsTwenty`                                      | Fact   | Fit too few positives is skipped and the minimum is twenty.                                          |
| `Fit_BetweenTwentyAndFortyNinePositives_FitsWithALowSampleWarning`                        | Fact   | Fit between twenty and forty nine positives fits with a low sample warning.                          |
| `Fit_ScoresThatRankBackwards_AreSkippedRatherThanInverted`                                | Fact   | Fit scores that rank backwards are skipped rather than inverted.                                     |
| `Fit_FlippingTheTestLabels_DoesNotChangeTheCalibrator_ButChangingTheValidationLabelsDoes` | Fact   | Fit flipping the test labels does not change the calibrator but changing the validation labels does. |
| `BaseRateResolution_FromCountsOverrideAndAnomalyLabels`                                   | Fact   | Base rate resolution from counts override and anomaly labels.                                        |

### ExhaustiveCalibrationRecallPathsTests

`Jube.Test.Exhaustive.ExhaustiveCalibrationRecallPathsTests` — 7 tests. Category: Unit.

| Test                                                                   | Kind   | Verifies                                                                         |
|------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `ScoreRecallAndTheInPipelineStage_ReturnTheSameCalibratedValue`        | Theory | Score recall and the in pipeline stage return the same calibrated value.         |
| `AModelWithoutACalibrator_BehavesByteForByteAsBefore`                  | Theory | A model without a calibrator behaves byte for byte as before.                    |
| `AnInvalidCalibrator_DoesNotServeRawScoresOnAnyPath`                   | Fact   | An invalid calibrator does not serve raw scores on any path.                     |
| `Sync_BuildsTheCalibrationFromThePromotedTrialRow`                     | Fact   | Sync builds the calibration from the promoted trial row.                         |
| `Sync_ForAModelPromotedBeforeCalibration_LoadsTheIdentity`             | Fact   | Sync for a model promoted before calibration loads the identity.                 |
| `Sync_ForASkippedCalibration_LoadsTheIdentityAndKeepsScoringRaw`       | Fact   | Sync for a skipped calibration loads the identity and keeps scoring raw.         |
| `Sync_ForCorruptColumns_RegistersTheModelLogsAnErrorAndMarksItInvalid` | Fact   | Sync for corrupt columns registers the model logs an error and marks it invalid. |

### ExhaustiveCalibrationValueTests

`Jube.Test.Exhaustive.ExhaustiveCalibrationValueTests` — 8 tests. Category: Unit.

| Test                                                           | Kind   | Verifies                                                                  |
|----------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `NoStoredMethod_IsTheIdentityAndReturnsTheRawScoreByteForByte` | Theory | No stored method is the identity and returns the raw score byte for byte. |
| `Apply_IsPlattThenThePriorShiftToTheBaseRate`                  | Fact   | Apply is platt then the prior shift to the base rate.                     |
| `Apply_WithoutABaseRate_StaysAtTheSamplePrior`                 | Fact   | Apply without a base rate stays at the sample prior.                      |
| `Apply_IsStrictlyMonotoneInTheRawScore`                        | Fact   | Apply is strictly monotone in the raw score.                              |
| `Apply_RawInputMode_UsesTheScoreDirectly`                      | Fact   | Apply raw input mode uses the score directly.                             |
| `Apply_NonFiniteRawValuesPassThroughUnchanged`                 | Fact   | Apply non finite raw values pass through unchanged.                       |
| `AnUnknownMethod_IsInvalid`                                    | Theory | An unknown method is invalid.                                             |
| `CorruptColumns_AreInvalidAndNeverSilentlyRaw`                 | Fact   | Corrupt columns are invalid and never silently raw.                       |

### ExhaustiveRecallTests

`Jube.Test.Exhaustive.ExhaustiveRecallTests` — 5 tests.

| Test                                                                  | Kind | Verifies                                                                      |
|-----------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `BuildScoreInputs_AllPresent_StandardisesType2Only`                   | Fact | Build score inputs all present standardises type2 only.                       |
| `BuildScoreInputs_MissingKeys_AreMeanImputed_NotZero`                 | Fact | Build score inputs missing keys are mean imputed not zero.                    |
| `BuildScoreInputs_MissingMatchesInPipelineFallback`                   | Fact | Build score inputs missing matches in pipeline fallback.                      |
| `BuildScoreInputs_JsonNullAndUnconvertibleValues_AreTreatedAsMissing` | Fact | Build score inputs json null and unconvertible values are treated as missing. |
| `BuildScoreInputs_MatchIsCaseSensitiveAndExact`                       | Fact | Build score inputs match is case sensitive and exact.                         |

### InactiveModelRecallHttpTests

`Jube.Test.Exhaustive.InactiveModelRecallHttpTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                                               | Kind | Verifies                                                                                 |
|------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `AnInactiveInstanceStillRecallsThroughTheDirectInvokePathAsync`                    | Fact | An inactive instance still recalls through the direct invoke path.                       |
| `AnInactiveInstanceStillScoresThroughTheBatchInvokePathAsync`                      | Fact | An inactive instance still scores through the batch invoke path.                         |
| `ATenantForeignUserIsForbiddenOnTheDirectInvokePathForAnInactiveInstanceAsync`     | Fact | A tenant foreign user is forbidden on the direct invoke path for an inactive instance.   |
| `ATenantUserWithoutTheModelRoleOrPermissionIsForbiddenForAnInactiveInstanceAsync`  | Fact | A tenant user without the model role or permission is forbidden for an inactive instance. |

### LoadDatabaseTests

`Jube.Test.Exhaustive.LoadDatabaseTests` — 23 tests. Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                               |
|-----------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `RealFields_OnlyTtlCounterAbstractionAndAdaptationFieldsSurviveInQueryOrderAsync` | Fact | Real fields only ttl counter abstraction and adaptation fields survive in query order. |
| `RealFields_InactiveDeletedAndOtherTenantFieldsAreExcludedAsync`                  | Fact | Real fields inactive deleted and other tenant fields are excluded.                     |
| `RealFields_ValuesAreExtractedByTheJsonPathAndMissingIsZeroAsync`                 | Fact | Real fields values are extracted by the json path and missing is zero.                 |
| `RealSample_IsOrderedByEntryGuidAndCappedAtTenThousandAsync`                      | Fact | Real sample is ordered by entry guid and capped at ten thousand.                       |
| `RealSample_ExactlyAtAndJustBelowTheCapIsNotTruncatedAsync`                       | Fact | Real sample exactly at and just below the cap is not truncated.                        |
| `RealSample_OtherModelsAndTheMockTableAreNotMixedInAsync`                         | Fact | Real sample other models and the mock table are not mixed in.                          |
| `RealSample_ModelWithNoArchiveRowsGivesVariablesAndNoRowsAsync`                   | Fact | Real sample model with no archive rows gives variables and no rows.                    |
| `MockSample_ReadsMockArchiveWithTheFixedVariableListIgnoringModelFieldsAsync`     | Fact | Mock sample reads mock archive with the fixed variable list ignoring model fields.     |
| `MockSample_IsOrderedByEntryGuidAndCappedAtTenThousandAsync`                      | Fact | Mock sample is ordered by entry guid and capped at ten thousand.                       |
| `ClassData_ReturnsOnlyRowsMatchingTheFilterLabelledOneAsync`                      | Fact | Class data returns only rows matching the filter labelled one.                         |
| `ClassData_WorksWithTheSelectOnlyGuardEnabledAsync`                               | Fact | Class data works with the select only guard enabled.                                   |
| `ClassData_ClassCapIsTenThousandAsync`                                            | Fact | Class data class cap is ten thousand.                                                  |
| `ClassData_MockTableIsUsedWhenMockDataIsSetAsync`                                 | Fact | Class data mock table is used when mock data is set.                                   |
| `NegativeSample_IsTheComplementOfTheFilterAndDisjointFromTheClassAsync`           | Fact | Negative sample is the complement of the filter and disjoint from the class.           |
| `NegativeSample_OfAStrictGreaterThanFilter_KeepsTheBoundaryValueAsync`            | Fact | Negative sample of a strict greater than filter keeps the boundary value.              |
| `NegativeSample_RowsMissingTheFilteredKeyAreInNeitherClassAsync`                  | Fact | Negative sample rows missing the filtered key are in neither class.                    |
| `NegativeSample_CapIsTenThousandAndAppliesIndependentlyOfTheClassCapAsync`        | Fact | Negative sample cap is ten thousand and applies independently of the class cap.        |
| `NegativeSample_MockDataFilterUsesTheMockTableAndFixedVariablesAsync`             | Fact | Negative sample mock data filter uses the mock table and fixed variables.              |
| `LoadMock_PopulatesMockArchiveOncePerModelFromTheEmbeddedCsvAsync`                | Fact | Load mock populates mock archive once per model from the embedded csv.                 |
| `LoadMock_RoundTripsEveryCsvRowThroughJsonBackToTheSameFeatureMatrixAsync`        | Fact | Load mock round trips every csv row through json back to the same feature matrix.      |
| `LoadMock_TagsFraudExactlyWhenTheDependentColumnIsPositiveAsync`                  | Fact | Load mock tags fraud exactly when the dependent column is positive.                    |
| `LoadMock_DoesNotTouchAnotherModelAndSkipsOnlyTheModelAlreadyLoadedAsync`         | Fact | Load mock does not touch another model and skips only the model already loaded.        |
| `LoadMock_SkipsWhenTheModelAlreadyHasAnyMockRowAsync`                             | Fact | Load mock skips when the model already has any mock row.                               |

### LoadJsonExtractionTests

`Jube.Test.Exhaustive.LoadJsonExtractionTests` — 13 tests.

| Test                                                                   | Kind   | Verifies                                                                         |
|------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `ExtractValue_ReadsTheNumberAtThePath`                                 | Theory | Extract value reads the number at the path.                                      |
| `ExtractValue_MissingNonNumericOrMalformedPathIsZero`                  | Theory | Extract value missing non numeric or malformed path is zero.                     |
| `ExtractValue_ConsecutiveDotsInThePathAreTolerated`                    | Fact   | Extract value consecutive dots in the path are tolerated.                        |
| `ExtractValue_MissingAndGenuineZeroAreIndistinguishable`               | Fact   | Extract value missing and genuine zero are indistinguishable.                    |
| `ExtractValue_BooleanAndDateTokensConvertTheWayNewtonsoftConvertsThem` | Fact   | Extract value boolean and date tokens convert the way newtonsoft converts them.  |
| `BuildRows_OneRowPerJsonWithOneColumnPerVariableInVariableOrder`       | Fact   | Build rows one row per json with one column per variable in variable order.      |
| `BuildRows_MissingValuesBecomeZeroWithoutDroppingTheRowOrShorteningIt` | Fact   | Build rows missing values become zero without dropping the row or shortening it. |
| `BuildRows_ExtraJsonFieldsAreIgnored`                                  | Fact   | Build rows extra json fields are ignored.                                        |
| `BuildRows_EmptyJsonListGivesNoRows`                                   | Fact   | Build rows empty json list gives no rows.                                        |
| `BuildRows_NoVariablesGivesEmptyRowsOnePerJson`                        | Fact   | Build rows no variables gives empty rows one per json.                           |
| `BuildRows_UnparseableJsonThrowsRatherThanBeingSkipped`                | Fact   | Build rows unparseable json throws rather than being skipped.                    |
| `BuildRows_VariableKeysAreReadAsAZeroBasedSequence`                    | Fact   | Build rows variable keys are read as a zero based sequence.                      |
| `BuildRows_ManyRowsKeepInputOrder`                                     | Fact   | Build rows many rows keep input order.                                           |

### LoadSampleSqlTests

`Jube.Test.Exhaustive.LoadSampleSqlTests` — 6 tests.

| Test                                                                                              | Kind   | Verifies                                                                                                           |
|---------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------|
| `SampleLimitIsTenThousand`                                                                        | Fact   | Sample limit is ten thousand.                                                                                      |
| `RealArchiveSample_SelectsFromArchiveOrderedByEntryGuidWithModelIdAndLimitAsTheLastTwoParameters` | Fact   | Real archive sample selects from archive ordered by entry guid with model id and limit as the last two parameters. |
| `MockArchiveSample_SelectsFromMockArchive`                                                        | Fact   | Mock archive sample selects from mock archive.                                                                     |
| `ParameterNumbersFollowTheTokenCount`                                                             | Theory | Parameter numbers follow the token count.                                                                          |
| `NegatedFilterIsWrappedSoTheNegationCoversTheWholeClause`                                         | Fact   | Negated filter is wrapped so the negation covers the whole clause.                                                 |
| `OrderIsAlwaysByEntryGuidAndNeverRandom`                                                          | Fact   | Order is always by entry guid and never random.                                                                    |

### LoadVariableSelectionTests

`Jube.Test.Exhaustive.LoadVariableSelectionTests` — 8 tests.

| Test                                                                             | Kind   | Verifies                                                                                    |
|----------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `BuildVariablesFromFields_KeepsOnlyProcessingTypesThreeFiveAndSeven`             | Theory | Build variables from fields keeps only processing types three five and seven.               |
| `BuildVariablesFromFields_PreservesQueryOrderAndNumbersKeysFromZeroWithoutGaps`  | Fact   | Build variables from fields preserves query order and numbers keys from zero without gaps.  |
| `BuildVariablesFromFields_CarriesNameProcessingTypeAndJsonPath`                  | Fact   | Build variables from fields carries name processing type and json path.                     |
| `BuildVariablesFromFields_NoFieldsOrNoEligibleFieldsGivesNoVariables`            | Fact   | Build variables from fields no fields or no eligible fields gives no variables.             |
| `BuildMockVariables_IsTwentySixAbstractionsInCsvHeaderOrderEachOnce`             | Fact   | Build mock variables is twenty six abstractions in csv header order each once.              |
| `BuildMockVariables_EveryVariableIsProcessingTypeFiveWithADollarAbstractionPath` | Fact   | Build mock variables every variable is processing type five with a dollar abstraction path. |
| `BuildMockVariables_ReturnsAFreshDictionaryEachTime`                             | Fact   | Build mock variables returns a fresh dictionary each time.                                  |
| `BuildMockVariables_MockJsonWithEveryFeatureProducesAFullyPopulatedRow`          | Fact   | Build mock variables mock json with every feature produces a fully populated row.           |

### MockCategoricalDatasetTests

`Jube.Test.Exhaustive.MockCategoricalDatasetTests` — 3 tests.

| Test                                                                   | Kind | Verifies                                                                             |
|------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `GenerateResourcesWhenRequested`                                       | Fact | Generate resources when requested.                                                   |
| `TheCommittedFileIsRowAlignedWithMockCsvAndEveryCategoryIsAKnownLabel` | Fact | The committed file is row aligned with mock csv and every category is a known label. |
| `EveryCategoricalColumnUsesEveryOneOfItsLabelsAtLeastOnce`             | Fact | Every categorical column uses every one of its labels at least once.                 |

### MockVariableListTests

`Jube.Test.Exhaustive.MockVariableListTests` — 1 test.

| Test                                                    | Kind | Verifies                                                |
|---------------------------------------------------------|------|---------------------------------------------------------|
| `MockVariables_MatchTheCsvFeatureColumnsOnceEach_Async` | Fact | Mock variables match the csv feature columns once each. |

### NetworkRoundTripTests

`Jube.Test.Exhaustive.NetworkRoundTripTests` — 1 test. Category: Unit.

| Test                                                                 | Kind   | Verifies                                                                       |
|----------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `ATrainedNetworkOfEveryActivationFunctionSurvivesTheEngineRoundTrip` | Theory | A trained network of every activation function survives the engine round trip. |

### ParserJsonToSqlEntityAnalysisModelTests

`Jube.Test.Exhaustive.ParserJsonToSqlEntityAnalysisModelTests` — 12 tests. Category: Unit.

| Test                                                            | Kind   | Verifies                                                                  |
|-----------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `SingleEqualRule_ProducesOneParameterisedComparison`            | Fact   | Single equal rule produces one parameterised comparison.                  |
| `EveryComparisonOperator_MapsToItsSqlToken`                     | Theory | Every comparison operator maps to its sql token.                          |
| `NotEqual_IsNegated`                                            | Fact   | Not equal is negated.                                                     |
| `NestedGroup_WrapsInParenthesesAndJoinsWithCondition`           | Fact   | Nested group wraps in parentheses and joins with condition.               |
| `InOperator_WithAnArrayValue_ProducesOnePlaceholderPerValue`    | Fact   | In operator with an array value produces one placeholder per value.       |
| `NotInOperator_IsNegated`                                       | Fact   | Not in operator is negated.                                               |
| `InOperator_WithASingleScalarValue_StillWorks`                  | Fact   | In operator with a single scalar value still works.                       |
| `InOperator_WithNoValues_Throws`                                | Fact   | In operator with no values throws.                                        |
| `MixedGroup_NumbersTokensAcrossAnInRuleAndAScalarRuleCorrectly` | Fact   | Mixed group numbers tokens across an in rule and a scalar rule correctly. |
| `UnknownField_Throws`                                           | Fact   | Unknown field throws.                                                     |
| `UnknownOperator_Throws`                                        | Fact   | Unknown operator throws.                                                  |
| `ANullRule_ProducesNoSql`                                       | Fact   | A null rule produces no sql.                                              |

### RecallBatchHttpTests

`Jube.Test.Exhaustive.RecallBatchHttpTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                            | Kind | Verifies                                                             |
|-----------------------------------------------------------------|------|----------------------------------------------------------------------|
| `EveryRowIsScoredThroughTheSameRecallPathAsASingleRowAsync`     | Fact | Every row is scored through the same recall path as a single row.    |
| `AnUnparseableValueIsReportedAsARowErrorNotABatchFailureAsync`  | Fact | An unparseable value is reported as a row error not a batch failure. |
| `AColumnWithNoMatchingTrainedVariableIsReportedAsUnmappedAsync` | Fact | A column with no matching trained variable is reported as unmapped.  |

### RecallChainTests

`Jube.Test.Exhaustive.RecallChainTests` — 1 test. Category: Service. Collection: Database.

| Test                                                               | Kind | Verifies                                                                |
|--------------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `ATrainedAndPromotedModelIsSyncedAndRecalledThroughBothPathsAsync` | Fact | A trained and promoted model is synced and recalled through both paths. |

### RecallExplainabilityTests

`Jube.Test.Exhaustive.RecallExplainabilityTests` — 11 tests.

| Test                                                                                           | Kind   | Verifies                                                                                                   |
|------------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------|
| `Explain_OneLayerIdentityNetwork_ConnectionWeightIsWeightTimesInput`                           | Fact   | Explain one layer identity network connection weight is weight times input.                                |
| `Explain_OneLayerIdentityNetwork_LrpRelevanceAndBaseValueReconstructTheOutputExactly`          | Fact   | Explain one layer identity network lrp relevance and base value reconstruct the output exactly.            |
| `Explain_AnIgnoredInputWithZeroWeightContributesZeroToBothMeasures`                            | Fact   | Explain an ignored input with zero weight contributes zero to both measures.                               |
| `Explain_TwoLayerSigmoidNetwork_LrpRelevancePlusBaseValueReconstructsTheOutput`                | Fact   | Explain two layer sigmoid network lrp relevance plus base value reconstructs the output.                   |
| `Explain_TwoLayerSigmoidNetwork_ConnectionWeightIsSignedByThePathWeightProduct`                | Fact   | Explain two layer sigmoid network connection weight is signed by the path weight product.                  |
| `Explain_ReconstructionHoldsRegardlessOfWhichActivationFunctionTheTopologySearchPicked`        | Theory | Explain reconstruction holds regardless of which activation function the topology search picked.           |
| `Explain_ADeadRectifiedLinearHiddenNeuronPropagatesZeroRelevanceThroughItAndStillReconstructs` | Fact   | Explain a dead rectified linear hidden neuron propagates zero relevance through it and still reconstructs. |
| `Explain_NodeRelevanceAtTheInputBoundaryMatchesEachVariablesLrpRelevance`                      | Fact   | Explain node relevance at the input boundary matches each variables lrp relevance.                         |
| `Explain_NodeRelevanceAtTheOutputBoundaryIsTheRawNetworkOutput`                                | Fact   | Explain node relevance at the output boundary is the raw network output.                                   |
| `Explain_EdgeRelevanceHasTheSameShapeAsEachLayersWeightMatrix`                                 | Fact   | Explain edge relevance has the same shape as each layers weight matrix.                                    |
| `Explain_WithZeroThresholdsEachNeuronsIncomingEdgeRelevanceSumsToItsOwnNodeRelevance`          | Fact   | Explain with zero thresholds each neurons incoming edge relevance sums to its own node relevance.          |

### RecallHttpTests

`Jube.Test.Exhaustive.RecallHttpTests` — 13 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind   | Verifies                                                                        |
|-----------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `ResponseIsTheScoreRoundedToFourDecimalPlacesAsync`                         | Theory | Response is the score rounded to four decimal places.                           |
| `ACalibratedModelReturnsTheCalibratedProbabilityRoundedToFourDecimalsAsync` | Theory | A calibrated model returns the calibrated probability rounded to four decimals. |
| `ASmallProbabilityKeepsItsResolutionAtFourDecimalsAsync`                    | Fact   | A small probability keeps its resolution at four decimals.                      |
| `ResponseIsNotAnUnroundedScoreAsync`                                        | Fact   | Response is not an unrounded score.                                             |
| `ANonFiniteScoreIsA500WithNoBodyAsync`                                      | Fact   | A non finite score is a 500 with no body.                                       |
| `ANetworkThatThrowsIsSwallowedToAZeroScoreWith200Async`                     | Fact   | A network that throws is swallowed to a zero score with200.                     |
| `AMissingKeyIsImputedWithTheMeanAsInThePipelineAsync`                       | Fact   | A missing key is imputed with the mean as in the pipeline.                      |
| `AStandardisedVariableUsesTheStoredMeanAndSdAsync`                          | Fact   | A standardised variable uses the stored mean and sd.                            |
| `VariablesAreConsumedInStoredOrderNotBodyOrderAsync`                        | Fact   | Variables are consumed in stored order not body order.                          |
| `ExtraKeysInTheBodyAreIgnoredAsync`                                         | Fact   | Extra keys in the body are ignored.                                             |
| `TheResponseCarriesPerVariableRelevanceThatReconstructsTheRawScoreAsync`    | Fact   | The response carries per variable relevance that reconstructs the raw score.    |
| `TypeGateRejectionsAre400WithAPlainMessageAsync`                            | Theory | Type gate rejections are400 with a plain message.                               |
| `AnUnknownGuidIs404Async`                                                   | Fact   | An unknown guid is404.                                                          |

### RecallInPipelineTests

`Jube.Test.Exhaustive.RecallInPipelineTests` — 22 tests. Category: Unit.

| Test                                                                             | Kind   | Verifies                                                                                         |
|----------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------|
| `AbsentValue_WithoutStandardisation_FallsBackToTheRawMeanForEveryProcessingType` | Theory | Absent value without standardisation falls back to the raw mean for every processing type.       |
| `AbsentValue_WithStandardisation_ImputesTheMeanAndStandardisesToExactlyZero`     | Theory | Absent value with standardisation imputes the mean and standardises to exactly zero.             |
| `AValueHeldInADifferentSourceThanTheProcessingTypeIsNotSeenAndTheMeanIsImputed`  | Theory | A value held in a different source than the processing type is not seen and the mean is imputed. |
| `APresentZeroIsUsedAsZeroAndNotTreatedAsAbsent`                                  | Theory | A present zero is used as zero and not treated as absent.                                        |
| `PresentValue_WithStandardisation_IsStandardisedAgainstTheStoredMeanAndSd`       | Theory | Present value with standardisation is standardised against the stored mean and sd.               |
| `PresentValue_WithConstantColumnSd_StandardisesToZeroRatherThanNonFinite`        | Fact   | Present value with constant column sd standardises to zero rather than non finite.               |
| `InputVectorFollowsTheStoredVariableOrderAndNotTheNameOrder`                     | Fact   | Input vector follows the stored variable order and not the name order.                           |
| `EachVariableIsResolvedFromItsOwnProcessingTypeInOneVector`                      | Fact   | Each variable is resolved from its own processing type in one vector.                            |
| `OnlyTheSecondDotSegmentIsUsedAsTheLookupKey`                                    | Theory | Only the second dot segment is used as the lookup key.                                           |
| `ADottedNameIsNotFoundUnderItsFullPrefixedKey`                                   | Fact   | A dotted name is not found under its full prefixed key.                                          |
| `LookupIsCaseSensitive`                                                          | Fact   | Lookup is case sensitive.                                                                        |
| `ANetworkThatThrowsOmitsTheEntryAndDoesNotRaise`                                 | Fact   | A network that throws omits the entry and does not raise.                                        |
| `AModelWithoutANetworkOmitsTheEntryButStillRecordsTiming`                        | Fact   | A model without a network omits the entry but still records timing.                              |
| `AFailingModelDoesNotStopTheNextOneFromBeingRecalled`                            | Fact   | A failing model does not stop the next one from being recalled.                                  |
| `ANonFiniteScoreIsStoredUnchangedWithoutAnyFinitenessCheck`                      | Fact   | A non finite score is stored unchanged without any finiteness check.                             |
| `TheScoreIsNotRoundedInThePipeline`                                              | Fact   | The score is not rounded in the pipeline.                                                        |
| `ThePayloadValueIsReadAsADoubleWhenTheStoredValueIsAnInteger`                    | Fact   | The payload value is read as a double when the stored value is an integer.                       |
| `ModelsAreKeyedByNameAndEachScoredIndependently`                                 | Fact   | Models are keyed by name and each scored independently.                                          |
| `ExhaustedBudget_SkipsEveryModelAndLeavesNoScore`                                | Fact   | Exhausted budget skips every model and leaves no score.                                          |
| `GenerousBudget_ScoresEveryModel`                                                | Fact   | Generous budget scores every model.                                                              |
| `ExhaustedBudget_DoesNotRecordTimingForSkippedModels`                            | Fact   | Exhausted budget does not record timing for skipped models.                                      |
| `WithoutEnvironment_TheBudgetIsTheDefault`                                       | Fact   | Without environment the budget is the default.                                                   |

### RecallPermissionTests

`Jube.Test.Exhaustive.RecallPermissionTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                                       | Kind   | Verifies                                                                         |
|----------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `ThePermissionToUseExhaustiveAuthorisesRecallWithoutAModelApiKeyLinkAsync` | Fact   | The permission to use exhaustive authorises recall without a model api key link. |
| `AnyOtherPermissionDoesNotAuthoriseRecallAsync`                            | Theory | Any other permission does not authorise recall.                                  |
| `TheTenantOfTheModelStillBoundsRecallForAHolderOfThePermissionAsync`       | Fact   | The tenant of the model still bounds recall for a holder of the permission.      |
| `ARevokedPermissionStopsAuthorisingRecallAsync`                            | Fact   | A revoked permission stops authorising recall.                                   |

### RecallScoreTests

`Jube.Test.Exhaustive.RecallScoreTests` — 19 tests. Category: Unit.

| Test                                                                        | Kind   | Verifies                                                                             |
|-----------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `Score_AppliesTheNetworkToTheStandardisedInputsInVariableOrder`             | Fact   | Score applies the network to the standardised inputs in variable order.              |
| `Score_ReturnsTheFirstOutputOnly`                                           | Fact   | Score returns the first output only.                                                 |
| `Score_WithAllValuesMissing_UsesTheImputedMeans`                            | Fact   | Score with all values missing uses the imputed means.                                |
| `Score_WhenTheNetworkCannotBeComputed_Throws`                               | Fact   | Score when the network cannot be computed throws.                                    |
| `Score_WithANullNetwork_Throws`                                             | Fact   | Score with a null network throws.                                                    |
| `Score_WithFewerVariablesThanNetworkInputs_Throws`                          | Fact   | Score with fewer variables than network inputs throws.                               |
| `BuildScoreInputs_ConvertsEveryAcceptedTokenShape`                          | Theory | Build score inputs converts every accepted token shape.                              |
| `BuildScoreInputs_UnconvertibleTokenIsMissingAndImputesTheMean`             | Theory | Build score inputs unconvertible token is missing and imputes the mean.              |
| `BuildScoreInputs_NestedObjectsAreNotSearched`                              | Fact   | Build score inputs nested objects are not searched.                                  |
| `BuildScoreInputs_TraceReportsImputationAndConversionFailure`               | Fact   | Build score inputs trace reports imputation and conversion failure.                  |
| `BuildScoreInputs_WithNoVariables_ReturnsAnEmptyVector`                     | Fact   | Build score inputs with no variables returns an empty vector.                        |
| `BuildScoreInputs_StringNumbersAreReadInvariantlyWhateverTheCurrentCulture` | Fact   | Build score inputs string numbers are read invariantly whatever the current culture. |
| `RecallExhaustive_ForAKnownGuid_ReturnsTheScore`                            | Fact   | Recall exhaustive for a known guid returns the score.                                |
| `RecallExhaustive_ForAnUnknownGuid_ReturnsZeroAndLogsTheError`              | Fact   | Recall exhaustive for an unknown guid returns zero and logs the error.               |
| `RecallExhaustive_WhenTheNetworkThrows_SwallowsTheExceptionAndReturnsZero`  | Fact   | Recall exhaustive when the network throws swallows the exception and returns zero.   |
| `RecallExhaustive_WhenTheNetworkIsNull_ReturnsZero`                         | Fact   | Recall exhaustive when the network is null returns zero.                             |
| `RecallExhaustive_ANonFiniteScoreIsReturnedUnchanged`                       | Fact   | Recall exhaustive a non finite score is returned unchanged.                          |
| `RecallExhaustive_FindsTheInstanceAcrossModels`                             | Fact   | Recall exhaustive finds the instance across models.                                  |
| `RecallBodyHasValidTypes_Boundaries`                                        | Theory | Recall body has valid types boundaries.                                              |

### RecallTimelineTests

`Jube.Test.Exhaustive.RecallTimelineTests` — 1 test. Category: Service. Collection: Database.

| Test                                                         | Kind | Verifies                                                       |
|--------------------------------------------------------------|------|----------------------------------------------------------------|
| `PromotedModelBecomesRecallableWhileTheEngineIsRunningAsync` | Fact | Promoted model becomes recallable while the engine is running. |

### SupervisedRepairTests

`Jube.Test.Exhaustive.SupervisedRepairTests` — 9 tests.

| Test                                                                                       | Kind   | Verifies                                                                                                |
|--------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------|
| `SplitData_TrainCrossValidationAndTest_AreDisjointAndAligned`                              | Theory | Split data train cross validation and test are disjoint and aligned.                                    |
| `ReduceDataForOnlyTrialVariables_PopulatesEveryTrialColumnInOrder`                         | Fact   | Reduce data for only trial variables populates every trial column in order.                             |
| `DrawVariableIndices_AreDistinctAndInRange`                                                | Theory | Draw variable indices are distinct and in range.                                                        |
| `DrawVariableIndices_MinimumSubset_CanReachTheLastVariable`                                | Fact   | Draw variable indices minimum subset can reach the last variable.                                       |
| `LeastSensitiveVariableKey_EliminationRemovesLeastSensitiveEveryTimeAndNeverRunsOffTheEnd` | Fact   | Least sensitive variable key elimination removes least sensitive every time and never runs off the end. |
| `Sensitivity_PerturbsByAFactorOfEightyToOneTwentyPercent_NotBySquaringTheValue`            | Fact   | Sensitivity perturbs by a factor of eighty to one twenty percent not by squaring the value.             |
| `InterquartileRange_IsNonNegativeAndInOriginalUnits`                                       | Fact   | Interquartile range is non negative and in original units.                                              |
| `TryGrowLayers_WidensUntilWideThenAddsALayerUntilTheDepthLimit`                            | Fact   | Try grow layers widens until wide then adds a layer until the depth limit.                              |
| `Performance_Score_RewardsGoodModelsAndDoesNotRewardInvertedOnes`                          | Fact   | Performance score rewards good models and does not reward inverted ones.                                |

### SyncBuildModelTests

`Jube.Test.Exhaustive.SyncBuildModelTests` — 12 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                     |
|--------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `IdentityAndGuidComeFromTheRecord`                                             | Fact   | Identity and guid come from the record.                                                      |
| `SpacesInTheNameBecomeUnderscores`                                             | Fact   | Spaces in the name become underscores.                                                       |
| `ANullNameDefaultsToExhaustiveUnderscoreId`                                    | Fact   | A null name defaults to exhaustive underscore id.                                            |
| `ResponsePayloadAndReportTableAreTrueOnlyForOne`                               | Theory | Response payload and report table are true only for one.                                     |
| `TheNetworkJsonIsDeserialisedToAnEquivalentNetwork`                            | Fact   | The network json is deserialised to an equivalent network.                                   |
| `VariablesKeepTheOrderSuppliedAndCarryTheStatisticsPerVariable`                | Fact   | Variables keep the order supplied and carry the statistics per variable.                     |
| `TheLoadedVariablesStandardiseWithTheStoredStatistics`                         | Fact   | The loaded variables standardise with the stored statistics.                                 |
| `NoVariableRowsLeavesTheNetworkLoadedWithAnEmptyVariableList`                  | Fact   | No variable rows leaves the network loaded with an empty variable list.                      |
| `ANullJsonReportsTheLoadErrorAndStillReturnsTheModelWithoutNetworkOrVariables` | Fact   | A null json reports the load error and still returns the model without network or variables. |
| `AnEmptyJsonStringGivesANullNetworkAndNoError`                                 | Fact   | An empty json string gives a null network and no error.                                      |
| `GarbageJsonNeverEscapesAsAnException`                                         | Fact   | Garbage json never escapes as an exception.                                                  |
| `AnOnErrorCallbackIsOptional`                                                  | Fact   | An on error callback is optional.                                                            |

### SyncExhaustiveSearchInstancesTests

`Jube.Test.Exhaustive.SyncExhaustiveSearchInstancesTests` — 11 tests. Category: Service. Collection: Database.

| Test                                                                         | Kind | Verifies                                                                          |
|------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `LoadsTheNetworkVariablesAndNameOfAPromotedActiveInstanceAsync`              | Fact | Loads the network variables and name of a promoted active instance.               |
| `VariablesAreOrderedByVariableSequenceNotInsertionOrderAsync`                | Fact | Variables are ordered by variable sequence not insertion order.                   |
| `RemovedVariablesAreExcludedAndOnlyThePromotedTrialsVariablesAreLoadedAsync` | Fact | Removed variables are excluded and only the promoted trials variables are loaded. |
| `StatisticsAndProcessingTypeAreLoadedPerVariableAsync`                       | Fact | Statistics and processing type are loaded per variable.                           |
| `TheLastActivePromotedTrialWinsAndInactiveOnesAreIgnoredAsync`               | Fact | The last active promoted trial wins and inactive ones are ignored.                |
| `AnInstanceWithoutAnActivePromotedTrialIsNotRegisteredAsync`                 | Fact | An instance without an active promoted trial is not registered.                   |
| `InactiveAndDeletedInstancesAreSkippedAndOthersStillLoadAsync`               | Fact | Inactive and deleted instances are skipped and others still load.                 |
| `ANullNameDefaultsToExhaustiveUnderscoreIdAsync`                             | Fact | A null name defaults to exhaustive underscore id.                                 |
| `InstancesAreRegisteredInIdOrderAsync`                                       | Fact | Instances are registered in id order.                                             |
| `ACorruptPromotedJsonDoesNotStopTheNextInstanceLoadingAsync`                 | Fact | A corrupt promoted json does not stop the next instance loading.                  |
| `ResyncReplacesTheCollectionAndPicksUpANewlyPromotedTrialAsync`              | Fact | Resync replaces the collection and picks up a newly promoted trial.               |

### TrainClassHistogramEndToEndTests

`Jube.Test.Exhaustive.TrainClassHistogramEndToEndTests` — 2 tests. Collection: Database.

| Test                                                                       | Kind | Verifies                                                                        |
|----------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `ClassHistogramTableRepositoryRoundTripsSoftDeletesAndIsTenantScopedAsync` | Fact | Class histogram table repository round trips soft deletes and is tenant scoped. |
| `MigrationCreatesTheClassHistogramTableWithEveryColumnAsync`               | Fact | Migration creates the class histogram table with every column.                  |

### TrainClassHistogramTests

`Jube.Test.Exhaustive.TrainClassHistogramTests` — 10 tests.

| Test                                                                              | Kind | Verifies                                                                                       |
|-----------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `HandCheckedTenValuesSplitOnTheGivenPopulationEdges`                              | Fact | Hand checked ten values split on the given population edges.                                   |
| `BoundaryValuesGoToTheUpperBinAndTheMaximumToTheLastBin`                          | Fact | Boundary values go to the upper bin and the maximum to the last bin.                           |
| `EveryRowLandsInExactlyOneBinSoClassCountsSumToTheRowCount`                       | Fact | Every row lands in exactly one bin so class counts sum to the row count.                       |
| `IsDeterministicForTheSameInput`                                                  | Fact | Is deterministic for the same input.                                                           |
| `FilterModeKeepsThePopulationAsTheNegativeClassAndClampsPositivesOutsideTheEdges` | Fact | Filter mode keeps the population as the negative class and clamps positives outside the edges. |
| `ConstantColumnIsOneBinAndEveryRowLandsInIt`                                      | Fact | Constant column is one bin and every row lands in it.                                          |
| `NoBinsYieldsNothingAndNaNValuesFallInTheFirstBin`                                | Fact | No bins yields nothing and na n values fall in the first bin.                                  |
| `WriterInsertsNegativeThenPositiveBinsWithTheirLabelsAsync`                       | Fact | Writer inserts negative then positive bins with their labels.                                  |
| `WriteAllProcessesEveryVariableWithItsOwnColumnAndItsOwnStoredEdgesAsync`         | Fact | Write all processes every variable with its own column and its own stored edges.               |
| `WriteAllSkipsAVariableWhoseStoredPopulationHistogramIsEmptyAsync`                | Fact | Write all skips a variable whose stored population histogram is empty.                         |

### TrainDbRecorderTests

`Jube.Test.Exhaustive.TrainDbRecorderTests` — 8 tests. Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                               |
|-------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `IsStopRequested_IsTrueOnlyForStoppingAndStoppedStatusesAsync`                      | Theory | Is stop requested is true only for stopping and stopped statuses.                      |
| `InstanceUpdates_PersistScoreModelsAndModelsSinceBestAsync`                         | Fact   | Instance updates persist score models and models since best.                           |
| `Recorder_InsertsEveryTrialArtefactAndReturnsTheGeneratedIdsAsync`                  | Fact   | Recorder inserts every trial artefact and returns the generated ids.                   |
| `TrialVariables_InsertThenMarkRemovedAsync`                                         | Fact   | Trial variables insert then mark removed.                                              |
| `MarkTrialVariableRemoved_ForAnUnknownPair_ThrowsAsync`                             | Fact   | Mark trial variable removed for an unknown pair throws.                                |
| `DeleteTrialVariables_SoftDeletesTheRowsOfThatTrialOnlyAsync`                       | Fact   | Delete trial variables soft deletes the rows of that trial only.                       |
| `PromotedVariableHistogram_IsInsertedAgainstThePromotedVariableAsync`               | Fact   | Promoted variable histogram is inserted against the promoted variable.                 |
| `PromotedVariableHistogram_ForAnUnknownPromotedVariable_ViolatesTheForeignKeyAsync` | Fact   | Promoted variable histogram for an unknown promoted variable violates the foreign key. |

### TrainInstanceRunTests

`Jube.Test.Exhaustive.TrainInstanceRunTests` — 13 tests. Collection: Database.

| Test                                                                                              | Kind | Verifies                                                                                                |
|---------------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------|
| `RunOneInstance_StopRequested_MovesStraightToStatusNineteenAndReturnsFalseAsync`                  | Fact | Run one instance stop requested moves straight to status nineteen and returns false.                    |
| `RunOneInstance_AlreadyStopped_StaysAtStatusNineteenAsync`                                        | Fact | Run one instance already stopped stays at status nineteen.                                              |
| `RunOneInstance_CancelledToken_ThrowsBeforeTouchingTheStatusAsync`                                | Fact | Run one instance cancelled token throws before touching the status.                                     |
| `RunOneInstance_AnomalyMockData_RunsEveryStageToCompletionAtStatusSixteenAsync`                   | Fact | Run one instance anomaly mock data runs every stage to completion at status sixteen.                    |
| `RunOneInstance_AnomalyMockData_StoresClassHistogramsOnThePopulationBinEdgesAsync`                | Fact | Run one instance anomaly mock data stores class histograms on the population bin edges.                 |
| `RunOneInstance_TheDefaultAlgorithmRecordsAnSvmReportWithNoReductionAsync`                        | Fact | Run one instance the default algorithm records an svm report with no reduction.                         |
| `RunOneInstance_ANativeAlgorithmLabelsTheContaminationFractionAndStoresTheReductionAsync`         | Fact | Run one instance a native algorithm labels the contamination fraction and stores the reduction.         |
| `RunOneInstance_AutomaticRecordsEveryCandidateEvaluationAsync`                                    | Fact | Run one instance automatic records every candidate evaluation.                                          |
| `RunOneInstance_AnomalyThatFlagsNothing_CompletesWithStatusSeventeenAndNoTrialsAsync`             | Fact | Run one instance anomaly that flags nothing completes with status seventeen and no trials.              |
| `StartAsync_PollsRunsTheNextInstanceThenKeepsPollingUntilCancelledAsync`                          | Fact | Start async polls runs the next instance then keeps polling until cancelled.                            |
| `StartAsync_AnAnomalyInstanceThatFlagsNothing_CompletesAtStatusSeventeenAndPollingContinuesAsync` | Fact | Start async an anomaly instance that flags nothing completes at status seventeen and polling continues. |
| `StartAsync_ARunThatThrows_IsSurvivedAndPollingContinuesAsync`                                    | Fact | Start async a run that throws is survived and polling continues.                                        |
| `StartAsync_CancelledBeforeStart_ReturnsImmediatelyWithoutPollingAsync`                           | Fact | Start async cancelled before start returns immediately without polling.                                 |

### TrainLoopTests

`Jube.Test.Exhaustive.TrainLoopTests` — 27 tests.

| Test                                                                                            | Kind | Verifies                                                                                                 |
|-------------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `TrainAsync_RunsTrialsLimitMinusOneTrials_AndReturnsTrueAsync`                                  | Fact | Train async runs trials limit minus one trials and returns true.                                         |
| `TrainAsync_TrialsLimitOfOne_RunsNoTrialsAtAllAsync`                                            | Fact | Train async trials limit of one runs no trials at all.                                                   |
| `TrainAsync_RecordsEveryTrialInStageOrderAsync`                                                 | Fact | Train async records every trial in stage order.                                                          |
| `TrainAsync_ActivationSearch_RecordsTenCandidatesPerVariableEliminationIterationAsync`          | Fact | Train async activation search records ten candidates per variable elimination iteration.                 |
| `TrainAsync_TopologyTrials_GrowOneNeuronAtATimeAndEndWithExactlyOneFinalisationRowAsync`        | Fact | Train async topology trials grow one neuron at a time and end with exactly one finalisation row.         |
| `TrainAsync_TopologySinceImprovementLimit_BoundsTheTopologySearchAsync`                         | Fact | Train async topology since improvement limit bounds the topology search.                                 |
| `TrainAsync_Promotion_IsStrictlyIncreasingAndStartsFromZeroAsync`                               | Fact | Train async promotion is strictly increasing and starts from zero.                                       |
| `TrainAsync_NonPromotedTrials_IncrementModelsSinceBest_AndPromotionDoesNotReportItAsync`        | Fact | Train async non promoted trials increment models since best and promotion does not report it.            |
| `TrainAsync_PerPromotion_WritesTwentyRocRowsWithTheFixedThresholdGridAsync`                     | Fact | Train async per promotion writes twenty roc rows with the fixed threshold grid.                          |
| `TrainAsync_PerPromotion_WritesPredictedVersusActualForEveryTestRowAsync`                       | Fact | Train async per promotion writes predicted versus actual for every test row.                             |
| `TrainAsync_PerPromotion_WritesOnePromotedSensitivityPerVariableWithDenseRanksAsync`            | Fact | Train async per promotion writes one promoted sensitivity per variable with dense ranks.                 |
| `TrainAsync_PerPromotion_PersistsPrescriptionHistogramsLinkedToTheInsertedPrescriptionAsync`    | Fact | Train async per promotion persists prescription histograms linked to the inserted prescription.          |
| `TrainAsync_PerPromotion_WritesTheModelJsonThatReproducesTheRecordedScoresAsync`                | Fact | Train async per promotion writes the model json that reproduces the recorded scores.                     |
| `TrainAsync_PromotedConfusion_IsRecordedAtTheHalfThresholdAsync`                                | Fact | Train async promoted confusion is recorded at the half threshold.                                        |
| `TrainAsync_RemovedVariables_AreOnlyVariablesThatWereSelectedInThatTrialAsync`                  | Fact | Train async removed variables are only variables that were selected in that trial.                       |
| `TrainAsync_VariableSelection_ClearsThePreviousSelectionBeforeEveryDrawAsync`                   | Fact | Train async variable selection clears the previous selection before every draw.                          |
| `TrainAsync_SelectedVariableCount_RespectsMinimumAndMaximumAsync`                               | Fact | Train async selected variable count respects minimum and maximum.                                        |
| `TrainAsync_MinimumAboveAvailableVariables_KeepsEveryVariableAndCompletesAsync`                 | Fact | Train async minimum above available variables keeps every variable and completes.                        |
| `TrainAsync_DrawEqualToTheMinimum_RemovesNothingAndCompletesAsync`                              | Fact | Train async draw equal to the minimum removes nothing and completes.                                     |
| `TrainAsync_Elimination_NeverLeavesFewerThanTheMinimumVariablesAsync`                           | Fact | Train async elimination never leaves fewer than the minimum variables.                                   |
| `TrainAsync_StopRequestedBeforeTheFirstTrial_ReturnsFalseAndRecordsNothingAsync`                | Fact | Train async stop requested before the first trial returns false and records nothing.                     |
| `TrainAsync_StopRequestedBetweenTrials_FinishesTheRunningTrialThenStopsAsync`                   | Fact | Train async stop requested between trials finishes the running trial then stops.                         |
| `TrainAsync_CancellationToken_PropagatesAsOperationCanceledNotSwallowedPerTrialAsync`           | Fact | Train async cancellation token propagates as operation canceled not swallowed per trial.                 |
| `TrainAsync_CancellationMidTrial_PropagatesFromTheRecorderAsync`                                | Fact | Train async cancellation mid trial propagates from the recorder.                                         |
| `TrainAsync_AnErrorInsideOneTrial_IsSwallowedAndTheNextTrialStillRunsAsync`                     | Fact | Train async an error inside one trial is swallowed and the next trial still runs.                        |
| `TrainAsync_AnErrorDuringPromotionStorage_SkipsTheRestOfThatPromotionButKeepsTheBestScoreAsync` | Fact | Train async an error during promotion storage skips the rest of that promotion but keeps the best score. |
| `TrainAsync_SeparableData_ReachesAPerfectScoreAndStopsBeforeTheTrialLimitAsync`                 | Fact | Train async separable data reaches a perfect score and stops before the trial limit.                     |

### TrainMetricsTests

`Jube.Test.Exhaustive.TrainMetricsTests` — 34 tests.

| Test                                                                                             | Kind   | Verifies                                                                                                  |
|--------------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------|
| `CalculatePerformance_CountsTheConfusionMatrixAtTheThreshold`                                    | Fact   | Calculate performance counts the confusion matrix at the threshold.                                       |
| `CalculatePerformance_ThresholdIsStrictlyGreaterThan`                                            | Fact   | Calculate performance threshold is strictly greater than.                                                 |
| `CalculatePerformance_ScoreIsTheMeanOfAccuracyAndSpearmanOfPredictedVersusActual`                | Fact   | Calculate performance score is the mean of accuracy and spearman of predicted versus actual.              |
| `CalculatePerformance_InvertedClassifier_ScoresMinusOneHalf_WithTiedBinaryRanks`                 | Fact   | Calculate performance inverted classifier scores minus one half with tied binary ranks.                   |
| `CalculatePerformance_PerfectClassifier_ScoresOne`                                               | Fact   | Calculate performance perfect classifier scores one.                                                      |
| `CalculatePerformance_ASingleRow_HasNaNCorrelationTreatedAsZero`                                 | Fact   | Calculate performance a single row has na n correlation treated as zero.                                  |
| `CalculatePerformance_NoRows_ScoresNaN_Characterisation`                                         | Fact   | Calculate performance no rows scores na n characterisation.                                               |
| `CalculatePerformance_NaNScoreIsPredictedNegative_AndInfinityHonoursTheThreshold`                | Fact   | Calculate performance na n score is predicted negative and infinity honours the threshold.                |
| `CalculatePerformance_ResetsCountersOnEveryCall`                                                 | Fact   | Calculate performance resets counters on every call.                                                      |
| `CalculateScores_ComputesTheFirstOutputNeuronForEveryRow`                                        | Fact   | Calculate scores computes the first output neuron for every row.                                          |
| `RocPoints_IsTwentyThresholdsFromFiveHundredthsToOne`                                            | Fact   | Roc points is twenty thresholds from five hundredths to one.                                              |
| `RocPoints_MatchesAnIndependentSweepAtEveryThreshold`                                            | Fact   | Roc points matches an independent sweep at every threshold.                                               |
| `RocPoints_AtTheHalfThreshold_AgreesWithCalculatePerformance`                                    | Fact   | Roc points at the half threshold agrees with calculate performance.                                       |
| `RocPoints_ScoresAllAboveOne_PredictEveryRowPositiveEvenAtTheLastThreshold`                      | Fact   | Roc points scores all above one predict every row positive even at the last threshold.                    |
| `RocPoints_DoesNotDisturbAnExistingPerformanceInstance`                                          | Fact   | Roc points does not disturb an existing performance instance.                                             |
| `Spearman_PerfectAgreementAndDisagreement`                                                       | Fact   | Spearman perfect agreement and disagreement.                                                              |
| `Spearman_IsRankBased_SoAMonotoneNonLinearRelationIsOne`                                         | Fact   | Spearman is rank based so a monotone non linear relation is one.                                          |
| `Spearman_KnownValueWithoutTies`                                                                 | Fact   | Spearman known value without ties.                                                                        |
| `Spearman_WithTies_IsThePearsonCorrelationOfAverageRanks`                                        | Theory | Spearman with ties is the pearson correlation of average ranks.                                           |
| `Spearman_IsSymmetric`                                                                           | Fact   | Spearman is symmetric.                                                                                    |
| `Spearman_LengthMismatchThrows`                                                                  | Fact   | Spearman length mismatch throws.                                                                          |
| `Spearman_AConstantSeriesHasNoCorrelation_AndSingleElementAndEmptyAreNaN`                        | Fact   | Spearman a constant series has no correlation and single element and empty are na n.                      |
| `Spearman_ConstantAgainstVarying_IsFiniteNotNaN`                                                 | Fact   | Spearman constant against varying is finite not na n.                                                     |
| `Sensitivity_DoesNotMutateTheInputRows_AndIsRepeatableForTheSameSeed`                            | Fact   | Sensitivity does not mutate the input rows and is repeatable for the same seed.                           |
| `Sensitivity_AVariableTheModelIgnoresHasZeroSensitivity_AndAZeroValueCannotBePerturbed`          | Fact   | Sensitivity a variable the model ignores has zero sensitivity and a zero value cannot be perturbed.       |
| `BuildPrescriptions_KeepsOnlyActivationsAboveHalfAndEmitsOneRowPerVariable`                      | Fact   | Build prescriptions keeps only activations above half and emits one row per variable.                     |
| `BuildPrescriptionsWithHistograms_HistogramCoversTheSampleAndConstantVariablesGetNone`           | Fact   | Build prescriptions with histograms histogram covers the sample and constant variables get none.          |
| `BuildPrescriptions_NetworkThatNeverActivatesPositively_ReturnsOnlyTheNegativeClassPrescription` | Fact   | Build prescriptions network that never activates positively returns only the negative class prescription. |
| `BuildPrescriptions_SplitsSimulationsIntoAPositiveAndANegativeClassPrescriptionPerVariable`      | Fact   | Build prescriptions splits simulations into a positive and a negative class prescription per variable.    |
| `BuildPrescriptions_BinaryVariablesAreSnappedToZeroOrOne_AndMissingDistributionsBecomeZero`      | Fact   | Build prescriptions binary variables are snapped to zero or one and missing distributions become zero.    |
| `GetRandomVariableCount_StaysWithinMinimumAndMaximumAndBelowTheAvailableCount`                   | Theory | Get random variable count stays within minimum and maximum and below the available count.                 |
| `GetRandomVariableCount_FewerAvailableThanTheMinimum_ReturnsEveryAvailableVariable`              | Fact   | Get random variable count fewer available than the minimum returns every available variable.              |
| `Unsupervised_OneClassModel_ScoresAnOutlierLowerThanTheClusterAndTheClassifierFlagsIt`           | Fact   | Unsupervised one class model scores an outlier lower than the cluster and the classifier flags it.        |
| `GetClassVariableByAnomaly_IsStrictlyBelowTheProbability`                                        | Fact   | Get class variable by anomaly is strictly below the probability.                                          |

### TrainRecallExhaustiveTests

`Jube.Test.Exhaustive.TrainRecallExhaustiveTests` — 10 tests.

| Test                                                                                          | Kind | Verifies                                                                                                  |
|-----------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------|
| `RecallExhaustive_ScoresTheMatchingInstanceWithZScoredInputsInVariableOrder`                  | Fact | Recall exhaustive scores the matching instance with z scored inputs in variable order.                    |
| `RecallExhaustive_ChoosesTheInstanceByGuidAcrossModels`                                       | Fact | Recall exhaustive chooses the instance by guid across models.                                             |
| `RecallExhaustive_AMissingOrNullOrUnparseableValue_IsImputedWithTheTrainingMean`              | Fact | Recall exhaustive a missing or null or unparseable value is imputed with the training mean.               |
| `RecallExhaustive_UnknownGuid_ReturnsZeroBecauseTheNotFoundErrorIsSwallowed`                  | Fact | Recall exhaustive unknown guid returns zero because the not found error is swallowed.                     |
| `RecallExhaustive_NoActiveModels_ReturnsZero`                                                 | Fact | Recall exhaustive no active models returns zero.                                                          |
| `RecallExhaustive_ANetworkWhoseInputCountDisagreesWithTheVariables_ReturnsZeroNotAnException` | Fact | Recall exhaustive a network whose input count disagrees with the variables returns zero not an exception. |
| `RecallExhaustive_AnInstanceWithNoNetwork_ReturnsZero`                                        | Fact | Recall exhaustive an instance with no network returns zero.                                               |
| `RecallExhaustiveWithExplainability_MatchesTheScoreOfRecallExhaustive`                        | Fact | Recall exhaustive with explainability matches the score of recall exhaustive.                             |
| `RecallExhaustiveWithExplainability_LrpRelevancePlusBaseValueReconstructsTheRawOutput`        | Fact | Recall exhaustive with explainability lrp relevance plus base value reconstructs the raw output.          |
| `RecallExhaustiveWithExplainability_UnknownGuid_ReturnsZeroAndNoRelevance`                    | Fact | Recall exhaustive with explainability unknown guid returns zero and no relevance.                         |

### TrainShuffleSymmetryTests

`Jube.Test.Exhaustive.TrainShuffleSymmetryTests` — 20 tests.

| Test                                                                                                  | Kind   | Verifies                                                                                                          |
|-------------------------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------------|
| `Shuffle_IsAPermutationThatKeepsEveryRowPairedWithItsLabel`                                           | Fact   | Shuffle is a permutation that keeps every row paired with its label.                                              |
| `Shuffle_SameSeedGivesTheSameOrder_DifferentSeedsGiveDifferentOrders`                                 | Fact   | Shuffle same seed gives the same order different seeds give different orders.                                     |
| `Shuffle_DoesNotMutateItsInputs_AndReusesTheRowInstances`                                             | Fact   | Shuffle does not mutate its inputs and reuses the row instances.                                                  |
| `Shuffle_EmptyAndSingleRow_AreReturnedUnchanged`                                                      | Theory | Shuffle empty and single row are returned unchanged.                                                              |
| `Shuffle_IsUniformEnoughThatEveryRowVisitsEveryPositionOverManySeeds`                                 | Fact   | Shuffle is uniform enough that every row visits every position over many seeds.                                   |
| `Shuffle_DoesNotPreserveClassStratification_ClassRatioOfTheFirstHalfVaries`                           | Fact   | Shuffle does not preserve class stratification class ratio of the first half varies.                              |
| `DatasetSymmetry_RemovesOnlyMajorityRows_KeepsOrder_AndIsSeedDeterministic`                           | Fact   | Dataset symmetry removes only majority rows keeps order and is seed deterministic.                                |
| `DatasetSymmetry_MajorityPositive_RemovesPositivesInstead`                                            | Fact   | Dataset symmetry majority positive removes positives instead.                                                     |
| `DatasetSymmetry_SingleClass_RemovesEveryRow_Characterisation`                                        | Theory | Dataset symmetry single class removes every row characterisation.                                                 |
| `DatasetSymmetry_DoesNotMutateItsInputs_AndKeepsTheRowInstances`                                      | Fact   | Dataset symmetry does not mutate its inputs and keeps the row instances.                                          |
| `DatasetSymmetry_DifferentSeedsPickDifferentSurvivors`                                                | Fact   | Dataset symmetry different seeds pick different survivors.                                                        |
| `DatasetSymmetry_ExplicitTargetBelowBothClasses_TrimsBothClassesToTheTarget`                          | Fact   | Dataset symmetry explicit target below both classes trims both classes to the target.                             |
| `DatasetSymmetry_ExplicitTargetAboveMinority_TrimsOnlyTheMajority_LeavingMinorityForBootstrapToTopUp` | Fact   | Dataset symmetry explicit target above minority trims only the majority leaving minority for bootstrap to top up. |
| `DatasetSymmetry_ExplicitTargetAboveBothClasses_LeavesBothClassesUntouched`                           | Fact   | Dataset symmetry explicit target above both classes leaves both classes untouched.                                |
| `NormaliseData_OnlyZScoresTypeTwoColumns_AndMutatesInPlace`                                           | Fact   | Normalise data only z scores type two columns and mutates in place.                                               |
| `NormaliseData_UsesTheFittedStatisticsForEveryRow_SoARowIsNotAffectedByOthers`                        | Fact   | Normalise data uses the fitted statistics for every row so a row is not affected by others.                       |
| `NormaliseData_MatrixWithFewerColumnsThanVariables_Throws`                                            | Fact   | Normalise data matrix with fewer columns than variables throws.                                                   |
| `NormaliseData_FitOnPopulationIsAppliedToAllRows_LeakageOfTestRowsIntoTheStatistics_H6Async`          | Fact   | Normalise data fit on population is applied to all rows leakage of test rows into the statistics h 6.             |
| `OnlyOutputWithClassificationForData_ReturnsThePositiveRowsInOrder_AndThrowsIfTheCountIsTooLow`       | Fact   | Only output with classification for data returns the positive rows in order and throws if the count is too low.   |
| `OnlyOutputWithClassificationForData_CountTooHigh_LeavesNullRows_Characterisation`                    | Fact   | Only output with classification for data count too high leaves null rows characterisation.                        |

### TrainStatisticsTests

`Jube.Test.Exhaustive.TrainStatisticsTests` — 19 tests.

| Test                                                                                                                  | Kind   | Verifies                                                                                                                       |
|-----------------------------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------------------|
| `Population_ComputesMeanMinMaxAndSampleStandardDeviationAsync`                                                        | Fact   | Population computes mean min max and sample standard deviation.                                                                |
| `Population_AssignsNormalisationTypeByShapeOfTheColumnAsync`                                                          | Fact   | Population assigns normalisation type by shape of the column.                                                                  |
| `Population_ConstantNonZeroColumn_IsZScoreTypeWithZeroSd_AndTheStatisticsRowIsStillWrittenAsync`                      | Fact   | Population constant non zero column is z score type with zero sd and the statistics row is still written.                      |
| `Population_TriangularDistribution_IsBuiltInNormalisedSpaceOnlyWhenMinBelowMaxAsync`                                  | Fact   | Population triangular distribution is built in normalised space only when min below max.                                       |
| `Population_ModeZeroAndMeanNonZero_UsesAFixedSmallModeOfPointZeroOneAsync`                                            | Fact   | Population mode zero and mean non zero uses a fixed small mode of point zero one.                                              |
| `Population_ColumnWithOnlyNonPositiveValuesAndAZeroMode_ClampsTheModeIntoTheRangeAsync`                               | Fact   | Population column with only non positive values and a zero mode clamps the mode into the range.                                |
| `Population_ColumnWithMaximumZeroAndNegativeValues_IsTypedAsNoNormalisationLikeAnAllZeroColumn_CharacterisationAsync` | Fact   | Population column with maximum zero and negative values is typed as no normalisation like an all zero column characterisation. |
| `Population_ConstantColumn_StoresZeroKurtosisAndSkewnessInsteadOfNaNAsync`                                            | Fact   | Population constant column stores zero kurtosis and skewness instead of na n.                                                  |
| `Population_ColumnContainingNaN_StoresZeroedStatisticsAndHasNoDistributionAsync`                                      | Fact   | Population column containing na n stores zeroed statistics and has no distribution.                                            |
| `BuildTriangularDistribution_ForADegenerateRange_ReturnsNull`                                                         | Theory | Build triangular distribution for a degenerate range returns null.                                                             |
| `BuildTriangularDistribution_ClampsTheModeIntoTheRange`                                                               | Theory | Build triangular distribution clamps the mode into the range.                                                                  |
| `Population_StoredBinCount_FollowsTheSturgesLikeRuleAndCapsAtTwentyAsync`                                             | Fact   | Population stored bin count follows the sturges like rule and caps at twenty.                                                  |
| `Population_HistogramRows_AreWrittenPerBinInSequenceForTheInsertedVariableIdAsync`                                    | Fact   | Population histogram rows are written per bin in sequence for the inserted variable id.                                        |
| `Population_HistogramBinCount_IgnoresTheComputedBinCount_H17Async`                                                    | Fact   | Population histogram bin count ignores the computed bin count h 17.                                                            |
| `Classification_WritesClassificationRowsLinkedToTheExistingVariableIds_AndLeavesVariablesAloneAsync`                  | Fact   | Classification writes classification rows linked to the existing variable ids and leaves variables alone.                      |
| `Anomaly_WritesAnomalyRowsLinkedToTheExistingVariableIdsAsync`                                                        | Fact   | Anomaly writes anomaly rows linked to the existing variable ids.                                                               |
| `TheOriginalSignature_ResolvesTheTargetFromTheRepositoryTypeAsync`                                                    | Fact   | The original signature resolves the target from the repository type.                                                           |
| `OnlyTheFirstVariablesCountRowsAreCovered_WhenTheMatrixHasMoreColumnsAsync`                                           | Fact   | Only the first variables count rows are covered when the matrix has more columns.                                              |
| `MockPopulation_ClassifiesEveryColumnAndBuildsADistributionForEveryNonConstantColumnAsync`                            | Fact   | Mock population classifies every column and builds a distribution for every non constant column.                               |

### TrainingRepairTests

`Jube.Test.Exhaustive.TrainingRepairTests` — 6 tests.

| Test                                                                          | Kind | Verifies                                                                        |
|-------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `NormaliseData_NormalisesEveryRowIncludingTheLast`                            | Fact | Normalise data normalises every row including the last.                         |
| `NormaliseData_ConstantNonBinaryColumn_ProducesZeroNotNaN`                    | Fact | Normalise data constant non binary column produces zero not na n.               |
| `DatasetSymmetry_BalancesClassesExactlyAndCanKeepTheLastRow`                  | Fact | Dataset symmetry balances classes exactly and can keep the last row.            |
| `DatasetSymmetry_Balanced_ReturnsEveryRow`                                    | Fact | Dataset symmetry balanced returns every row.                                    |
| `OnlyOutputWithClassificationForData_KeepsAPositiveFinalRow`                  | Fact | Only output with classification for data keeps a positive final row.            |
| `CalculateStatistics_ProducesStatisticsForEveryVariableIncludingTheLastAsync` | Fact | Calculate statistics produces statistics for every variable including the last. |

## Datasets

### DatasetCategoricalSupportTests

`Jube.Test.Exhaustive.Datasets.DatasetCategoricalSupportTests` — 4 tests.

| Test                                                                         | Kind | Verifies                                                                                |
|------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `ColumnWithTwoWellPopulatedLevels_IsFittedAsCategoricalButExcludedByDefault` | Fact | Column with two well populated levels is fitted as categorical but excluded by default. |
| `IdentifierLikeColumn_IsExcludedWithAReason`                                 | Fact | Identifier like column is excluded with a reason.                                       |
| `DistinctValues_AreCountedAndSortedByFrequency`                              | Fact | Distinct values are counted and sorted by frequency.                                    |
| `RawDistinctTracking_TruncatesBeyondTheBudget`                               | Fact | Raw distinct tracking truncates beyond the budget.                                      |

### DatasetCsvParserTests

`Jube.Test.Exhaustive.Datasets.DatasetCsvParserTests` — 33 tests.

| Test                                                               | Kind   | Verifies                                                               |
|--------------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `SimpleFile_IsParsedWithProfilesAsync`                             | Fact   | Simple file is parsed with profiles.                                   |
| `Bom_IsSkippedAndHeaderIsCleanAsync`                               | Fact   | Bom is skipped and header is clean.                                    |
| `Delimiter_IsDetectedAsync`                                        | Theory | Delimiter is detected.                                                 |
| `SemicolonHeaderWithQuotedCommas_UsesSemicolonAsync`               | Fact   | Semicolon header with quoted commas uses semicolon.                    |
| `MixedLineEndings_AreAcceptedAsync`                                | Fact   | Mixed line endings are accepted.                                       |
| `Quoting_EscapedQuotesAndEmbeddedDelimitersAndNewlinesAsync`       | Fact   | Quoting escaped quotes and embedded delimiters and newlines.           |
| `MalformedQuoting_IsRejectedAsync`                                 | Theory | Malformed quoting is rejected.                                         |
| `InjectionCells_AreStoredAsDataNeverEvaluatedAsync`                | Fact   | Injection cells are stored as data never evaluated.                    |
| `MissingTokens_AreCountedAsync`                                    | Fact   | Missing tokens are counted.                                            |
| `Numbers_UseInvariantCultureAndRejectThousandsSeparatorsAsync`     | Fact   | Numbers use invariant culture and reject thousands separators.         |
| `NonFiniteNumbers_AreNotAcceptedAsNumbersAsync`                    | Fact   | Non finite numbers are not accepted as numbers.                        |
| `TextColumn_TooFewRowsToFitAsCategorical_IsExcludedByDefaultAsync` | Fact   | Text column too few rows to fit as categorical is excluded by default. |
| `ConstantAndEmptyColumns_AreExcludedByDefaultAsync`                | Fact   | Constant and empty columns are excluded by default.                    |
| `RaggedRow_IsRejectedWithRowNumberAsync`                           | Fact   | Ragged row is rejected with row number.                                |
| `RaggedRow_TooManyValues_IsRejectedAsync`                          | Fact   | Ragged row too many values is rejected.                                |
| `DuplicateHeaders_AreRejectedAsync`                                | Theory | Duplicate headers are rejected.                                        |
| `BadHeaders_AreRejectedAsync`                                      | Theory | Bad headers are rejected.                                              |
| `HugeCell_IsRejectedAsync`                                         | Fact   | Huge cell is rejected.                                                 |
| `HugeHeader_IsRejectedAsync`                                       | Fact   | Huge header is rejected.                                               |
| `NonUtf8Bytes_AreRejectedAsync`                                    | Fact   | Non utf8 bytes are rejected.                                           |
| `Utf16WithBom_IsRejectedAsync`                                     | Fact   | Utf16 with bom is rejected.                                            |
| `CompressedAndBinaryContent_IsRejectedAsync`                       | Theory | Compressed and binary content is rejected.                             |
| `ControlCharacters_AreRejectedAsync`                               | Fact   | Control characters are rejected.                                       |
| `RowCap_IsEnforcedAsync`                                           | Fact   | Row cap is enforced.                                                   |
| `RowCap_AtTheLimitIsAcceptedAsync`                                 | Fact   | Row cap at the limit is accepted.                                      |
| `ColumnCap_IsEnforcedBeforeAnyRowAsync`                            | Fact   | Column cap is enforced before any row.                                 |
| `ByteCap_IsEnforcedWhileStreamingAsync`                            | Fact   | Byte cap is enforced while streaming.                                  |
| `EmptyFile_HeaderOnly_AndBlankLines_AreHandledAsync`               | Fact   | Empty file header only and blank lines are handled.                    |
| `Cancellation_StopsParsingAsync`                                   | Fact   | Cancellation stops parsing.                                            |
| `LargeStreamedFile_DoesNotBufferRowsAsync`                         | Fact   | Large streamed file does not buffer rows.                              |
| `SuggestName_ProducesSafeNames`                                    | Theory | Suggest name produces safe names.                                      |
| `SuggestName_DeduplicatesAndTruncates`                             | Fact   | Suggest name deduplicates and truncates.                               |
| `RowJson_UsesIndexKeysAndNullsAndText`                             | Fact   | Row json uses index keys and nulls and text.                           |

### DatasetJsonParserTests

`Jube.Test.Exhaustive.Datasets.DatasetJsonParserTests` — 57 tests.

| Test                                                                  | Kind   | Verifies                                                                |
|-----------------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `ArrayOfObjects_IsParsedWithProfilesAsync`                            | Fact   | Array of objects is parsed with profiles.                               |
| `ColumnarForm_IsParsedWithProfilesAsync`                              | Fact   | Columnar form is parsed with profiles.                                  |
| `BothShapes_GiveIdenticalReportsAndRowsAsync`                         | Fact   | Both shapes give identical reports and rows.                            |
| `ObjectForm_UnionOfKeysDefinesColumns_AndMissingKeyIsMissingAsync`    | Fact   | Object form union of keys defines columns and missing key is missing.   |
| `Booleans_BecomeOneAndZero_WithAWarningAsync`                         | Fact   | Booleans become one and zero with a warning.                            |
| `NullIsMissing_AndCountedAsync`                                       | Fact   | Null is missing and counted.                                            |
| `Strings_FlagNonNumericColumns_ExcludedByDefault_AndKeepTheTextAsync` | Fact   | Strings flag non numeric columns excluded by default and keep the text. |
| `MixedTypesInAColumn_AreMixedAndExcludedAsync`                        | Fact   | Mixed types in a column are mixed and excluded.                         |
| `NumbersAsStrings_AreTextAndWarnedAsync`                              | Fact   | Numbers as strings are text and warned.                                 |
| `NegativeZero_IsStoredAsZeroAsync`                                    | Fact   | Negative zero is stored as zero.                                        |
| `IntegersBeyondTwoToThe53_AreWarned_AndExactOnesAreNotAsync`          | Fact   | Integers beyond two to the53 are warned and exact ones are not.         |
| `OverflowingNumbers_AreRejectedAsync`                                 | Theory | Overflowing numbers are rejected.                                       |
| `LargeButFiniteNumber_IsAcceptedAsync`                                | Fact   | Large but finite number is accepted.                                    |
| `UnicodeColumnNames_AreAcceptedAndGetSafeSuggestedNamesAsync`         | Fact   | Unicode column names are accepted and get safe suggested names.         |
| `NestedObject_IsRejectedWithAPrecisePathAsync`                        | Fact   | Nested object is rejected with a precise path.                          |
| `NestedArray_InColumnarRows_IsRejectedWithAPrecisePathAsync`          | Fact   | Nested array in columnar rows is rejected with a precise path.          |
| `DuplicateKeys_InOneObject_AreRejectedAsync`                          | Fact   | Duplicate keys in one object are rejected.                              |
| `KeysDifferingOnlyByCase_AreRejectedLikeCsvHeadersAsync`              | Fact   | Keys differing only by case are rejected like csv headers.              |
| `DuplicateColumnNames_InColumnarForm_AreRejectedAsync`                | Fact   | Duplicate column names in columnar form are rejected.                   |
| `UnsafeNames_AreRejectedWithTheCsvHeaderRulesAsync`                   | Theory | Unsafe names are rejected with the csv header rules.                    |
| `NameLongerThanTheHeaderCap_IsRejectedAsync`                          | Fact   | Name longer than the header cap is rejected.                            |
| `TopLevelScalars_AreRejectedAsync`                                    | Theory | Top level scalars are rejected.                                         |
| `EmptyArray_IsRejectedAsNoRowsAsync`                                  | Fact   | Empty array is rejected as no rows.                                     |
| `ColumnarWithNoRows_IsRejectedAsync`                                  | Fact   | Columnar with no rows is rejected.                                      |
| `EmptyObject_AndEmptyInput_AreRejectedAsync`                          | Fact   | Empty object and empty input are rejected.                              |
| `MalformedJson_IsRejectedAndNothingSucceedsAsync`                     | Theory | Malformed json is rejected and nothing succeeds.                        |
| `TrailingContent_IsRejectedAsync`                                     | Theory | Trailing content is rejected.                                           |
| `TrailingWhitespace_IsAcceptedAsync`                                  | Fact   | Trailing whitespace is accepted.                                        |
| `CsvContent_WithAJsonName_IsRejectedAsync`                            | Fact   | Csv content with a json name is rejected.                               |
| `ByteOrderMark_IsRejectedAsync`                                       | Fact   | Byte order mark is rejected.                                            |
| `BinaryAndUtf16_AreRejectedAsync`                                     | Theory | Binary and utf16 are rejected.                                          |
| `InvalidUtf8_InAString_IsRejectedAsync`                               | Fact   | Invalid utf8 in a string is rejected.                                   |
| `LoneSurrogateEscape_IsRejectedAsync`                                 | Fact   | Lone surrogate escape is rejected.                                      |
| `LoneSurrogateInAName_IsRejectedAsync`                                | Fact   | Lone surrogate in a name is rejected.                                   |
| `ValidSurrogatePair_IsAcceptedAsync`                                  | Fact   | Valid surrogate pair is accepted.                                       |
| `NulInAStringValue_IsRejectedAsync`                                   | Fact   | Nul in a string value is rejected.                                      |
| `DeepNesting_IsRejectedAsync`                                         | Fact   | Deep nesting is rejected.                                               |
| `NestingBeyondTheDepthLimit_InAValue_IsRejectedAsync`                 | Fact   | Nesting beyond the depth limit in a value is rejected.                  |
| `HugeString_IsRejected_WithoutBufferingItAsync`                       | Fact   | Huge string is rejected without buffering it.                           |
| `HugeNumber_IsRejectedAsync`                                          | Fact   | Huge number is rejected.                                                |
| `StringLongerThanTheCellCap_IsRejectedAsync`                          | Fact   | String longer than the cell cap is rejected.                            |
| `LongStoredText_IsTruncatedTo64CharactersAsync`                       | Fact   | Long stored text is truncated to64 characters.                          |
| `NonObjectRow_InArrayForm_IsRejectedWithAPathAsync`                   | Fact   | Non object row in array form is rejected with a path.                   |
| `RaggedRow_InColumnarForm_IsRejectedAsync`                            | Fact   | Ragged row in columnar form is rejected.                                |
| `ColumnarShapeErrors_AreRejectedAsync`                                | Fact   | Columnar shape errors are rejected.                                     |
| `ColumnCap_IsEnforced_ForBothShapesAsync`                             | Fact   | Column cap is enforced for both shapes.                                 |
| `RowCap_IsEnforced_ForBothShapesAsync`                                | Fact   | Row cap is enforced for both shapes.                                    |
| `SizeCap_IsEnforcedAsync`                                             | Fact   | Size cap is enforced.                                                   |
| `LargeFile_WithinCaps_IsStreamedAndCountedAsync`                      | Fact   | Large file within caps is streamed and counted.                         |
| `TokensSplitAcrossReads_AreParsedIdenticallyAsync`                    | Fact   | Tokens split across reads are parsed identically.                       |
| `ManyRowsOfEmptyObjects_AreBatchedAndDoNotExhaustMemoryAsync`         | Fact   | Many rows of empty objects are batched and do not exhaust memory.       |
| `SinkFailure_PropagatesAsync`                                         | Fact   | Sink failure propagates.                                                |
| `Cancellation_IsHonouredAsync`                                        | Fact   | Cancellation is honoured.                                               |
| `ClassBalance_AndConstantColumns_MatchTheCsvInspectionAsync`          | Fact   | Class balance and constant columns match the csv inspection.            |
| `SameDataAsCsv_GivesTheSameColumnsRowsAndReportAsync`                 | Fact   | Same data as csv gives the same columns rows and report.                |
| `CsvParser_NamesJsonContentAsync`                                     | Fact   | Csv parser names json content.                                          |
| `LargeIntegerLiteral_Detection`                                       | Fact   | Large integer literal detection.                                        |

### DatasetMappingRulesTests

`Jube.Test.Exhaustive.Datasets.DatasetMappingRulesTests` — 12 tests.

| Test                                                        | Kind   | Verifies                                                          |
|-------------------------------------------------------------|--------|-------------------------------------------------------------------|
| `ValidMapping_HasNoIssues`                                  | Fact   | Valid mapping has no issues.                                      |
| `MissingColumns_AreRejected`                                | Fact   | Missing columns are rejected.                                     |
| `ColumnCountAndIndexProblems_AreRejected`                   | Fact   | Column count and index problems are rejected.                     |
| `CategoricalTextVariable_IsAcceptedAsAnIndependentVariable` | Fact   | Categorical text variable is accepted as an independent variable. |
| `TextAndConstantVariables_AreRejected`                      | Fact   | Text and constant variables are rejected.                         |
| `UnsafeNames_AreRejected`                                   | Theory | Unsafe names are rejected.                                        |
| `OverlongName_IsRejected`                                   | Fact   | Overlong name is rejected.                                        |
| `DuplicateNames_AreRejectedCaseInsensitively`               | Fact   | Duplicate names are rejected case insensitively.                  |
| `UnknownProcessingTypes_AreRejected`                        | Theory | Unknown processing types are rejected.                            |
| `NoIncludedVariable_IsRejected`                             | Fact   | No included variable is rejected.                                 |
| `ExcludedColumnsAreNotValidated`                            | Fact   | Excluded columns are not validated.                               |
| `ProcessingTypeHelpers_Behave`                              | Fact   | Processing type helpers behave.                                   |
