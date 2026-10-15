---
layout: default
title: Advanced Analytics Engine and Services
nav_order: 4
parent: Testing
---

# Advanced Analytics Engine and Services

Tests of the Advanced Analytics engine, services, repositories, workers, R script execution, serialisation and library
conventions.

## General

### SeedDataCaptureTests

`Jube.Test.AdvancedAnalytics.SeedDataCaptureTests` — 19 tests.

| Test                                       | Kind | Verifies                                       |
|--------------------------------------------|------|------------------------------------------------|
| `CaptureDecisionTreeRegressor`             | Fact | Capture decision tree regressor.               |
| `CaptureKNearestNeighboursRegressor`       | Fact | Capture k nearest neighbours regressor.        |
| `CaptureRandomForestRegressor`             | Fact | Capture random forest regressor.               |
| `CaptureKernelRidgeRegressor`              | Fact | Capture kernel ridge regressor.                |
| `CaptureNeuralNetworkRegressor`            | Fact | Capture neural network regressor.              |
| `CaptureGradientBoostingRegressor`         | Fact | Capture gradient boosting regressor.           |
| `CaptureNeuralNetworkClassifier`           | Fact | Capture neural network classifier.             |
| `CaptureRandomForestClassifier`            | Fact | Capture random forest classifier.              |
| `CaptureDecisionTreeClassifier`            | Fact | Capture decision tree classifier.              |
| `CaptureNaiveBayesClassifier`              | Fact | Capture naive bayes classifier.                |
| `CaptureTreeAugmentedNaiveBayesClassifier` | Fact | Capture tree augmented naive bayes classifier. |
| `CaptureKNearestNeighboursClassifier`      | Fact | Capture k nearest neighbours classifier.       |
| `CaptureBaggingClassifier`                 | Fact | Capture bagging classifier.                    |
| `CaptureAdaBoostClassifier`                | Fact | Capture ada boost classifier.                  |
| `CaptureGradientBoostingClassifier`        | Fact | Capture gradient boosting classifier.          |
| `CaptureLinearSupportVectorClassifier`     | Fact | Capture linear support vector classifier.      |
| `CaptureKernelSupportVectorClassifier`     | Fact | Capture kernel support vector classifier.      |
| `CaptureIsolationForestAnomaly`            | Fact | Capture isolation forest anomaly.              |
| `CaptureOneClassSupportVectorAnomaly`      | Fact | Capture one class support vector anomaly.      |

## Conventions

### DataAccessArchitectureTests

`Jube.Test.AdvancedAnalytics.Conventions.DataAccessArchitectureTests` — 3 tests.

| Test                                                          | Kind | Verifies                                                                  |
|---------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `TheGuardedNamespacesAreNotEmpty`                             | Fact | The guarded namespaces are not empty.                                     |
| `TheScannerCatchesDirectTableAccess`                          | Fact | The scanner catches direct table access.                                  |
| `NoServiceOrEngineClassTouchesLinqToDbOrTheDbContextDirectly` | Fact | No service or engine class touches linq to db or the db context directly. |

### ToolConventionTests

`Jube.Test.AdvancedAnalytics.Conventions.ToolConventionTests` — 12 tests.

| Test                                                               | Kind | Verifies                                                                      |
|--------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `EveryProductionToolHasNoConventionViolations`                     | Fact | Every production tool has no convention violations.                           |
| `TheTrainerToolSetLivesInTheServiceAssemblyWithTheSamePermissions` | Fact | The trainer tool set lives in the service assembly with the same permissions. |
| `ProductionToolNamesAreGloballyUnique`                             | Fact | Production tool names are globally unique.                                    |
| `ProductionToolsDeclareAtLeastOneKnownPermission`                  | Fact | Production tools declare at least one known permission.                       |
| `EveryProductionToolMethodIsCoveredByATest`                        | Fact | Every production tool method is covered by a test.                            |
| `PermissionNamesAreTheFourAgreedNames`                             | Fact | Permission names are the four agreed names.                                   |
| `BadFixtureTripsEveryConvention`                                   | Fact | Bad fixture trips every convention.                                           |
| `GoodFixtureHasNoViolationsAndUnionsGroupPermissions`              | Fact | Good fixture has no violations and unions group permissions.                  |
| `CancellationTokenParametersNeedNoDescription`                     | Fact | Cancellation token parameters need no description.                            |
| `CoverageMetaTestFailsForAnUncoveredFixtureTool`                   | Fact | Coverage meta test fails for an uncovered fixture tool.                       |
| `MeanFixtureToolWorks`                                             | Fact | Mean fixture tool works.                                                      |
| `TrainFixtureToolWorks`                                            | Fact | Train fixture tool works.                                                     |

## Engine

### AdvancedAnalyticsConcurrencyTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsConcurrencyTests` — 2 tests. Category: Unit.

| Test                                                                                      | Kind | Verifies                                                                                             |
|-------------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------|
| `ManyInvocationsWhileTheCollectionIsSwappedSeeEitherTheOldOrTheNewModelAndNeverFailAsync` | Fact | Many invocations while the collection is swapped see either the old or the new model and never fail. |
| `TheSharedImmutableRecallGivesIdenticalResultsFromManyThreadsAsync`                       | Fact | The shared immutable recall gives identical results from many threads.                               |

### AdvancedAnalyticsModelRegistryTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsModelRegistryTests` — 6 tests. Category: Unit.

| Test                                                                            | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `LogisticRegressionPipelineIsRegisteredAndBuildsARecallWithTheVariablesInOrder` | Fact   | Logistic regression pipeline is registered and builds a recall with the variables in order. |
| `AnUnknownKindIsRefusedWithAClearMessageNamingTheRegisteredKinds`               | Fact   | An unknown kind is refused with a clear message naming the registered kinds.                |
| `ACorruptDocumentIsRefused`                                                     | Theory | A corrupt document is refused.                                                              |
| `AKnownKindWithACorruptPayloadIsRefused`                                        | Fact   | A known kind with a corrupt payload is refused.                                             |
| `ANewFamilyRegistersInOnePlaceAndIsThenBuilt`                                   | Fact   | A new family registers in one place and is then built.                                      |
| `ARecallWithoutVariablesIsRefused`                                              | Fact   | A recall without variables is refused.                                                      |

### AdvancedAnalyticsRecallInPipelineTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsRecallInPipelineTests` — 27 tests. Category: Unit.

| Test                                                                               | Kind   | Verifies                                                                                          |
|------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `AbsentValueIsPassedAsNaNSoTheRecallImputesAsTrainedForEveryProcessingType`        | Theory | Absent value is passed as na n so the recall imputes as trained for every processing type.        |
| `APresentZeroIsUsedAsZeroAndNotTreatedAsAbsent`                                    | Theory | A present zero is used as zero and not treated as absent.                                         |
| `APresentValueIsPassedRawFromItsOwnProcessingTypeSource`                           | Theory | A present value is passed raw from its own processing type source.                                |
| `AValueHeldInADifferentSourceThanTheProcessingTypeIsNotSeen`                       | Theory | A value held in a different source than the processing type is not seen.                          |
| `ThePayloadValueIsReadAsADoubleWhenTheStoredValueIsAnInteger`                      | Fact   | The payload value is read as a double when the stored value is an integer.                        |
| `InputVectorFollowsTheStoredVariableOrderAndNotTheNameOrder`                       | Fact   | Input vector follows the stored variable order and not the name order.                            |
| `EachVariableIsResolvedFromItsOwnProcessingTypeInOneVector`                        | Fact   | Each variable is resolved from its own processing type in one vector.                             |
| `OnlySomeVariablesAbsentPassesNaNAtExactlyThosePositions`                          | Fact   | Only some variables absent passes na n at exactly those positions.                                |
| `OnlyTheSecondDotSegmentIsUsedAsTheLookupKey`                                      | Theory | Only the second dot segment is used as the lookup key.                                            |
| `LookupIsCaseSensitive`                                                            | Fact   | Lookup is case sensitive.                                                                         |
| `ARecallThatThrowsOmitsOnlyThatModelAndDoesNotRaise`                               | Fact   | A recall that throws omits only that model and does not raise.                                    |
| `AModelTrainedToRejectMissingValuesOmitsTheEntryWhenAValueIsAbsent`                | Fact   | A model trained to reject missing values omits the entry when a value is absent.                  |
| `ANullRecallOmitsTheEntryButStillRecordsTiming`                                    | Fact   | A null recall omits the entry but still records timing.                                           |
| `ModelsAreKeyedByNameAndEachScoredIndependently`                                   | Fact   | Models are keyed by name and each scored independently.                                           |
| `TimingIsRecordedPerModelWithDurationAndAllocationAndAStageTotal`                  | Fact   | Timing is recorded per model with duration and allocation and a stage total.                      |
| `AnUnsampledInvocationRecordsNoStageTiming`                                        | Fact   | An unsampled invocation records no stage timing.                                                  |
| `NoModelsLeavesTheScoresEmpty`                                                     | Fact   | No models leaves the scores empty.                                                                |
| `ExhaustedBudgetSkipsEveryModelAndLeavesNoScore`                                   | Fact   | Exhausted budget skips every model and leaves no score.                                           |
| `ExhaustedBudgetDoesNotRecordTimingForSkippedModels`                               | Fact   | Exhausted budget does not record timing for skipped models.                                       |
| `GenerousBudgetScoresEveryModel`                                                   | Fact   | Generous budget scores every model.                                                               |
| `TheBudgetIsCheckedBetweenModelsSoALaterModelIsSkippedOnceTheBudgetIsSpent`        | Fact   | The budget is checked between models so a later model is skipped once the budget is spent.        |
| `TheBudgetIsCheckedBetweenInputExtractionsSoAnOverrunModelLeavesNoScoreButIsTimed` | Fact   | The budget is checked between input extractions so an overrun model leaves no score but is timed. |
| `WithoutEnvironmentTheBudgetIsTheDefault`                                          | Fact   | Without environment the budget is the default.                                                    |
| `TheEnvironmentDefaultIsOneSecond`                                                 | Fact   | The environment default is one second.                                                            |
| `TheConfiguredStageTimeoutIsUsedAndZeroDisablesTheBudget`                          | Fact   | The configured stage timeout is used and zero disables the budget.                                |
| `TheStageOutputSitsBesideTheExhaustiveAdaptationAndDoesNotTouchIt`                 | Fact   | The stage output sits beside the exhaustive adaptation and does not touch it.                     |
| `TheStageRunsAfterExhaustiveAndBeforeHttpAdaptationInTheRegisteredStageSequence`   | Fact   | The stage runs after exhaustive and before http adaptation in the registered stage sequence.      |

### AdvancedAnalyticsRecallScoreTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsRecallScoreTests` — 11 tests. Category: Unit.

| Test                                                            | Kind   | Verifies                                                                  |
|-----------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `ScoreAppliesTheRecallToTheRawInputsInVariableOrder`            | Fact   | Score applies the recall to the raw inputs in variable order.             |
| `ScoreMatchesByTheFullPrefixedKeyAndIgnoresExtraKeys`           | Fact   | Score matches by the full prefixed key and ignores extra keys.            |
| `AMissingNullUnconvertibleOrDifferentlyCasedKeyIsPassedAsNaN`   | Theory | A missing null unconvertible or differently cased key is passed as na n.  |
| `APresentZeroAndANumericStringAreValues`                        | Fact   | A present zero and a numeric string are values.                           |
| `AllMissingScoresTheImputedRowAndNothingIsRounded`              | Fact   | All missing scores the imputed row and nothing is rounded.                |
| `ARecallThatThrowsPropagatesFromTheCoreRatherThanReturningZero` | Fact   | A recall that throws propagates from the core rather than returning zero. |
| `RecallByGuidScoresTheKnownModel`                               | Fact   | Recall by guid scores the known model.                                    |
| `RecallByGuidScoresTheRequestedModelAndNotTheOtherModel`        | Fact   | Recall by guid scores the requested model and not the other model.        |
| `RecallByAnUnknownGuidRaisesKeyNotFound`                        | Fact   | Recall by an unknown guid raises key not found.                           |
| `AnExhaustiveGuidIsNotFoundAmongAdvancedAnalyticsModels`        | Fact   | An exhaustive guid is not found among advanced analytics models.          |
| `FindReturnsNullWhenNoModelHasTheGuid`                          | Fact   | Find returns null when no model has the guid.                             |

### AdvancedAnalyticsSyncBuildModelTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsSyncBuildModelTests` — 12 tests. Category: Unit.

| Test                                                                              | Kind   | Verifies                                                                                      |
|-----------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `IdentityGuidKindAndVersionComeFromTheRecords`                                    | Fact   | Identity guid kind and version come from the records.                                         |
| `SpacesInTheNameBecomeUnderscoresAndANullNameDefaults`                            | Fact   | Spaces in the name become underscores and a null name defaults.                               |
| `ResponsePayloadAndReportTableAreTrueOnlyForOne`                                  | Theory | Response payload and report table are true only for one.                                      |
| `VariablesKeepTheStoredOrderWithTheirProcessingTypes`                             | Fact   | Variables keep the stored order with their processing types.                                  |
| `TheBuiltRecallScoresLikeTheSnapshot`                                             | Fact   | The built recall scores like the snapshot.                                                    |
| `ExplicitVariableRowsAreAcceptedByTheSeam`                                        | Fact   | Explicit variable rows are accepted by the seam.                                              |
| `ACorruptOrUnknownSnapshotReportsTheErrorAndYieldsNoModel`                        | Theory | A corrupt or unknown snapshot reports the error and yields no model.                          |
| `ACorruptSnapshotDoesNotStopOtherModelsFromBuilding`                              | Fact   | A corrupt snapshot does not stop other models from building.                                  |
| `MissingMalformedMismatchedOrMisnamedVariablesYieldNoModel`                       | Theory | Missing malformed mismatched or misnamed variables yield no model.                            |
| `VariablePropertyNamesAreCaseInsensitive`                                         | Fact   | Variable property names are case insensitive.                                                 |
| `AVariablesJsonWithNoTransformationIdPropertyDefaultsToNoneAndRecallsIdentically` | Fact   | A variables json with no transformation id property defaults to none and recalls identically. |
| `TransformationIdAndParametersRoundTripThroughVariablesJsonLosslessly`            | Fact   | Transformation id and parameters round trip through variables json losslessly.                |

### AdvancedAnalyticsSyncTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsSyncTests` — 12 tests. Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                               |
|---------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `LoadsTheActiveVersionOfAnActiveTrainedModelOfAnActiveInstanceAsync`            | Fact | Loads the active version of an active trained model of an active instance.             |
| `OnlyTheActiveVersionLoadsNotTheLatestOrAnEarlierOneAsync`                      | Fact | Only the active version loads not the latest or an earlier one.                        |
| `ATrainedModelWithoutAnActiveVersionOrNotActiveIsNotLoadedAsync`                | Fact | A trained model without an active version or not active is not loaded.                 |
| `InactiveDeletedAndUnallocatedInstancesAreSkippedAndOthersStillLoadAsync`       | Fact | Inactive deleted and unallocated instances are skipped and others still load.          |
| `ADeletedTrainedModelIsSkippedAsync`                                            | Fact | A deleted trained model is skipped.                                                    |
| `ACorruptOrUnknownActiveVersionIsLoggedAndSkippedWithoutStoppingTheOthersAsync` | Fact | A corrupt or unknown active version is logged and skipped without stopping the others. |
| `ADuplicateNameIsSkippedAndTheFirstIsKeptAsync`                                 | Fact | A duplicate name is skipped and the first is kept.                                     |
| `ResyncReplacesTheCollectionReferenceAndDropsWhatIsNoLongerActiveAsync`         | Fact | Resync replaces the collection reference and drops what is no longer active.           |
| `APromotedVersionReplacesThePreviousOneAndRollbackRestoresItAsync`              | Fact | A promoted version replaces the previous one and rollback restores it.                 |
| `ACorruptPromotedVersionKeepsNoStaleModelAfterResyncAsync`                      | Fact | A corrupt promoted version keeps no stale model after resync.                          |
| `AnotherTenantsInstanceIsNeverLoadedAsync`                                      | Fact | Another tenants instance is never loaded.                                              |
| `NoInstancesLeavesAnEmptyCollectionAsync`                                       | Fact | No instances leaves an empty collection.                                               |

### AdvancedAnalyticsVariableTransformationRecallEdgeCaseTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsVariableTransformationRecallEdgeCaseTests` — 5 tests. Category:
Unit.

| Test                                                                                            | Kind   | Verifies                                                                                                        |
|-------------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------------|
| `AModelMixingUntransformedAndTransformedVariablesResolvesEverySlotIndependently`                | Fact   | A model mixing untransformed and transformed variables resolves every slot independently.                       |
| `AOneHotExpandedVariableAlongsideOrdinaryVariablesKeepsEverySlotAtTheCorrectIndex`              | Fact   | A one hot expanded variable alongside ordinary variables keeps every slot at the correct index.                 |
| `AMalformedTransformationParametersJsonOnOneVariableDoesNotStopTheOtherVariablesOfTheSameModel` | Fact   | A malformed transformation parameters json on one variable does not stop the other variables of the same model. |
| `ATransformedVariableWithNoPayloadFieldAtAllReachesRecallAsNaNJustLikeUntransformedToday`       | Fact   | A transformed variable with no payload field at all reaches recall as na n just like untransformed today.       |
| `ACategoricalTransformationWithAnUnseenLiveCategoryNeverThrowsAndStillProducesAScore`           | Theory | A categorical transformation with an unseen live category never throws and still produces a score.              |

### AdvancedAnalyticsVariableTransformationRecallTests

`Jube.Test.AdvancedAnalytics.Engine.AdvancedAnalyticsVariableTransformationRecallTests` — 12 tests. Category: Unit.

| Test                                                                           | Kind | Verifies                                                                                   |
|--------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `NoneTransformationLeavesThePayloadValueUnchanged`                             | Fact | None transformation leaves the payload value unchanged.                                    |
| `NoneTransformationLeavesAMissingValueAsNaN`                                   | Fact | None transformation leaves a missing value as na n.                                        |
| `LogTransformationIsAppliedBeforeReachingRecall`                               | Fact | Log transformation is applied before reaching recall.                                      |
| `ZScoreStandardTransformationIsAppliedBeforeReachingRecall`                    | Fact | Z score standard transformation is applied before reaching recall.                         |
| `WinsoriseClipsAboveTheUpperBoundBeforeReachingRecall`                         | Fact | Winsorise clips above the upper bound before reaching recall.                              |
| `DiscretiseCustomEdgesReturnsTheBinIndexBeforeReachingRecall`                  | Fact | Discretise custom edges returns the bin index before reaching recall.                      |
| `CyclicalSinAndCosAreComputedFromTheSamePayloadField`                          | Fact | Cyclical sin and cos are computed from the same payload field.                             |
| `OneHotIndicatorIsOneOnlyWhenTheTextMatchesTheStoredCategory`                  | Fact | One hot indicator is one only when the text matches the stored category.                   |
| `OneHotIndicatorIsZeroWhenTheTextDoesNotMatch`                                 | Fact | One hot indicator is zero when the text does not match.                                    |
| `WeightOfEvidenceLooksUpTheStoredCategoryValue`                                | Fact | Weight of evidence looks up the stored category value.                                     |
| `WeightOfEvidenceFallsBackToTheDefaultValueForAnUnseenCategory`                | Fact | Weight of evidence falls back to the default value for an unseen category.                 |
| `ACategoricalTransformationWithAMissingPayloadFieldFallsBackToTheDefaultValue` | Fact | A categorical transformation with a missing payload field falls back to the default value. |

### VariableTransformationDisplayNameTests

`Jube.Test.AdvancedAnalytics.Engine.VariableTransformationDisplayNameTests` — 5 tests. Category: Unit.

| Test                                                                               | Kind   | Verifies                                                                                        |
|------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `EachTransformationKindAppendsItsOwnFixedAbbreviation`                             | Theory | Each transformation kind appends its own fixed abbreviation.                                    |
| `NoneLeavesTheNameUnchanged`                                                       | Fact   | None leaves the name unchanged.                                                                 |
| `CyclicalSinAndCosProduceASinCosPairFromTheSameBaseName`                           | Fact   | Cyclical sin and cos produce a sin cos pair from the same base name.                            |
| `OneHotIndicatorLeavesTheNameUnchangedBecauseItAlreadyCarriesItsOwnCategorySuffix` | Fact   | One hot indicator leaves the name unchanged because it already carries its own category suffix. |
| `DifferentSourceVariablesWithTheSameTransformationNeverCollide`                    | Fact   | Different source variables with the same transformation never collide.                          |

## Library

### LearningLibraryFixesTests

`Jube.Test.AdvancedAnalytics.Library.LearningLibraryFixesTests` — 7 tests.

| Test                                                               | Kind | Verifies                                                                     |
|--------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `WeightedNaiveBayesMatchesReplicatedRows`                          | Fact | Weighted naive bayes matches replicated rows.                                |
| `EpsilonSupportVectorRegressionRecoversStraightLine`               | Fact | Epsilon support vector regression recovers straight line.                    |
| `EpsilonSupportVectorRegressionEpsilonFlattensTheFittedSlope`      | Fact | Epsilon support vector regression epsilon flattens the fitted slope.         |
| `AdaBoostPerfectWeakLearnerGivesFiniteWeightAndKeepsCallerWeights` | Fact | Ada boost perfect weak learner gives finite weight and keeps caller weights. |
| `DecisionStumpGreaterThanIsStrict`                                 | Fact | Decision stump greater than is strict.                                       |
| `RandomForestWithSameSeedReproducesTheSameForest`                  | Fact | Random forest with same seed reproduces the same forest.                     |
| `LinearDualCoordinateDescentWithSameSeedReproducesTheSameWeights`  | Fact | Linear dual coordinate descent with same seed reproduces the same weights.   |

### SurvivalAndSpectralLibraryFixesTests

`Jube.Test.AdvancedAnalytics.Library.SurvivalAndSpectralLibraryFixesTests` — 10 tests.

| Test                                                                 | Kind   | Verifies                                                                       |
|----------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `KaplanMeierSurvivalDropsToZeroAtAndAfterTheLastFailure`             | Fact   | Kaplan meier survival drops to zero at and after the last failure.             |
| `KaplanMeierSurvivalStaysPositiveAfterACensoredLastObservation`      | Fact   | Kaplan meier survival stays positive after a censored last observation.        |
| `SurvivalSortOrdersLaterTimesFirstWithCensoredBeforeFailuresAtATie`  | Fact   | Survival sort orders later times first with censored before failures at a tie. |
| `SurvivalSortIsStableForIdenticalTimesAndOutcomes`                   | Fact   | Survival sort is stable for identical times and outcomes.                      |
| `LogisticRegressionConfidenceIntervalUsesTheRequestedNormalQuantile` | Fact   | Logistic regression confidence interval uses the requested normal quantile.    |
| `WeibullFitSatisfiesTheMaximumLikelihoodEquations`                   | Fact   | Weibull fit satisfies the maximum likelihood equations.                        |
| `WeibullFitWithIntegerWeightsMatchesReplicatedObservations`          | Fact   | Weibull fit with integer weights matches replicated observations.              |
| `CircularConvolutionMatchesTheDirectDefinition`                      | Theory | Circular convolution matches the direct definition.                            |
| `PowerSpectrumConservesEnergyAndDoesNotDoubleTheNyquistBin`          | Theory | Power spectrum conserves energy and does not double the nyquist bin.           |
| `HilbertTransformOfCosineIsSine`                                     | Fact   | Hilbert transform of cosine is sine.                                           |

## Page

### AdvancedAnalyticsPageWiringTests

`Jube.Test.AdvancedAnalytics.Page.AdvancedAnalyticsPageWiringTests` — 12 tests. Category: Service.

| Test                                                                                                    | Kind   | Verifies                                                                                                                 |
|---------------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------------|
| `ThePageLoadsItsScriptAndStylesheetAndCarriesTheGridsAndTabs`                                           | Fact   | The page loads its script and stylesheet and carries the grids and tabs.                                                 |
| `ThePageModelIsGatedOnTheFourAdvancedAnalyticsPermissionsAndDeletingAnAllocatedInstanceNeedsFortySeven` | Fact   | The page model is gated on the four advanced analytics permissions and deleting an allocated instance needs forty seven. |
| `TheScriptUsesOnlyTheReadRoutesTheChartRoutesTheDeleteRouteTheActiveSwitchRouteAndTheApprovalRoutes` | Fact | The script uses only the read routes the chart routes the delete route the active switch route and the approval routes. |
| `TheScriptEscapesServerTextBeforeItBuildsMarkup`                                                        | Fact   | The script escapes server text before it builds markup.                                                                  |
| `TheScriptCarriesNoComments`                                                                            | Theory | The script carries no comments.                                                                                          |
| `NothingWrittenForThePageMentionsTheLibraryName`                                                        | Fact   | Nothing written for the page mentions the library name.                                                                  |
| `TheModelsMenuGroupLinksTheAnalyticsPageUnderTheFourPermissions`                                        | Fact   | The models menu group links the analytics page under the four permissions.                                               |
| `TheModelsGroupIsVisibleToAUserWhoHoldsOnlyAnAdvancedAnalyticsPermission`                               | Fact   | The models group is visible to a user who holds only an advanced analytics permission.                                   |
| `TheEndpointsAreRegisteredAndEveryGroupRequiresAuthorisation`                                           | Fact   | The endpoints are registered and every group requires authorisation.                                                     |
| `TheHttpSurfaceIsReadRoutesChartRoutesOneDeleteTheApprovalRoutesAndNothingThatMutatesElse` | Fact | The http surface is read routes chart routes one delete the approval routes and nothing that mutates else. |
| `TheChartEndpointsCarryTheSvgHeadersAndTheRunnerMapsTheServiceCodes`                                    | Fact   | The chart endpoints carry the svg headers and the runner maps the service codes.                                         |
| `ThePermissionDropdownOfTheRoleScreenReadsTheSpecificationTableSoTheNewPermissionsAppear`               | Fact   | The permission dropdown of the role screen reads the specification table so the new permissions appear.                  |

## R script

### AskJoobyScriptExecutionClientTests

`Jube.Test.AdvancedAnalytics.RScript.AskJoobyScriptExecutionClientTests` — 4 tests. Category: AdvancedAnalytics.

| Test                                                 | Kind | Verifies                                              |
|------------------------------------------------------|------|-------------------------------------------------------|
| `CheckHealthMapsEveryFieldIncludingScriptRulesAsync` | Fact | Check health maps every field including script rules. |
| `ListPackagesMapsEveryPackageAsync`                  | Fact | List packages maps every package.                     |
| `ValidateMapsErrorsAsync`                            | Fact | Validate maps errors.                                 |
| `RunScriptMapsTheFakeGatewaysResultAsync`            | Fact | Run script maps the fake gateways result.             |

### RScriptInteroperabilityTests

`Jube.Test.AdvancedAnalytics.RScript.RScriptInteroperabilityTests` — 3 tests. Category: AdvancedAnalytics.

| Test                                                                                     | Kind | Verifies                                                                                                  |
|------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------|
| `TheTrainingDataAgentIsRegisteredAsALeafUnderModelRiskSoNarrationAndPlanGovernanceSeeIt` | Fact | The training data agent is registered as a leaf under model risk so narration and plan governance see it. |
| `ARejectedScriptReachesTheCallerWithItsStatusAndErrorIntactAsync`                        | Fact | A rejected script reaches the caller with its status and error intact.                                    |
| `TheCompose_FileRunsJubeRInternallyAndPublishesNoHostPort`                               | Fact | The compose file runs jube r internally and publishes no host port.                                       |

### RScriptResultParserTests

`Jube.Test.AdvancedAnalytics.RScript.RScriptResultParserTests` — 6 tests.

| Test                                               | Kind | Verifies                                                  |
|----------------------------------------------------|------|-----------------------------------------------------------|
| `ParseReturnsNullWhenNoConsoleOutput`              | Fact | Parse returns null when no console output.                |
| `ParseReturnsNullWhenMarkersAreAbsent`             | Fact | Parse returns null when markers are absent.               |
| `ParseExtractsValidJsonBetweenMarkers`             | Fact | Parse extracts valid json between markers.                |
| `ParseReturnsWarningWhenMarkersWrapInvalidJson`    | Fact | Parse returns warning when markers wrap invalid json.     |
| `ParseReturnsNullWhenEndMarkerPrecedesStartMarker` | Fact | Parse returns null when end marker precedes start marker. |
| `ParseReturnsNullWhenOnlyStartMarkerIsPresent`     | Fact | Parse returns null when only start marker is present.     |

### RServerClientTests

`Jube.Test.AdvancedAnalytics.RScript.RServerClientTests` — 5 tests.

| Test                                                             | Kind | Verifies                                                            |
|------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `HealthAsyncCallsTheHealthEndpointAndParsesTheResponseAsync`     | Fact | Health async calls the health endpoint and parses the response.     |
| `PackagesAsyncCallsThePackagesEndpointAndParsesTheResponseAsync` | Fact | Packages async calls the packages endpoint and parses the response. |
| `ValidateAsyncPostsTheScriptAndParsesTheResponseAsync`           | Fact | Validate async posts the script and parses the response.            |
| `ExecuteAsyncPostsTheScriptAndTimeoutAndParsesTheResponseAsync`  | Fact | Execute async posts the script and timeout and parses the response. |
| `ExecuteAsyncPassesTheCallersCancellationTokenThroughAsync`      | Fact | Execute async passes the callers cancellation token through.        |

### ScriptExecutionServiceTests

`Jube.Test.AdvancedAnalytics.RScript.ScriptExecutionServiceTests` — 10 tests. Category: Service. Collection: Database.

| Test                                                                                     | Kind | Verifies                                                                                       |
|------------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `RunScriptReturnsTheRealConsoleOutputAndResultOnSuccessAsync`                            | Fact | Run script returns the real console output and result on success.                              |
| `RunScriptReportsATimeoutExactlyAsTheRServerDescribedItAsync`                            | Fact | Run script reports a timeout exactly as the r server described it.                             |
| `RunScriptRefusesAnEmptyScriptWithoutCallingTheServerAsync`                              | Fact | Run script refuses an empty script without calling the server.                                 |
| `RunScriptRefusesATimeoutOutsideOneToThreeHundredSecondsAsync`                           | Fact | Run script refuses a timeout outside one to three hundred seconds.                             |
| `RunScriptIsForbiddenWithoutTheSandboxedScriptExecutionPermissionEvenWithAnalyticsAsync` | Fact | Run script is forbidden without the sandboxed script execution permission even with analytics. |
| `RunScriptHonoursAnExternallyCancelledTokenRatherThanReturningAResultAsync`              | Fact | Run script honours an externally cancelled token rather than returning a result.               |
| `RunScriptResolvesAPointerBelongingToTheCallersOwnTenantAndForwardsItToRAsync`           | Fact | Run script resolves a pointer belonging to the callers own tenant and forwards it to r.        |
| `RunScriptPutsTheCallersUserNameOnTheOutboundRequestAsync`                               | Fact | Run script puts the callers user name on the outbound request.                                 |
| `RunScriptFailsWithAGenericErrorForAnUnresolvablePointerWithoutEverCallingRAsync`        | Fact | Run script fails with a generic error for an unresolvable pointer without ever calling r.      |
| `RunScriptFailsAllDataframesWhenOnlyOneOfSeveralCannotBeResolvedAsync`                   | Fact | Run script fails all dataframes when only one of several cannot be resolved.                   |

## Repository

### AdvancedAnalyticsCacheRepositoryTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsCacheRepositoryTests` — 14 tests. Category: Service.
Collection: Database.

| Test                                                                             | Kind | Verifies                                                                               |
|----------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `PutThenGetRoundTripsPackedDoublesBitExactlyAsync`                               | Fact | Put then get round trips packed doubles bit exactly.                                   |
| `ForeignTenantKeyLooksLikeNotFoundAsync`                                         | Fact | Foreign tenant key looks like not found.                                               |
| `ExpiredEntryLooksLikeNotFoundAndIsRemovedByCleanupAsync`                        | Fact | Expired entry looks like not found and is removed by cleanup.                          |
| `TenantCleanupLeavesOtherTenantsExpiredRowsAndAllTenantsCleanupRemovesThemAsync` | Fact | Tenant cleanup leaves other tenants expired rows and all tenants cleanup removes them. |
| `CleanupNeverRemovesLiveEntriesAsync`                                            | Fact | Cleanup never removes live entries.                                                    |
| `TtlComesFromLimitsAndCanBeOverriddenPerEntryAsync`                              | Fact | Ttl comes from limits and can be overridden per entry.                                 |
| `ShortRealTtlExpiresAsync`                                                       | Fact | Short real ttl expires.                                                                |
| `EntryLargerThanTheEntryLimitIsRejectedAndNothingIsStoredAsync`                  | Fact | Entry larger than the entry limit is rejected and nothing is stored.                   |
| `EntryAtTheEntryLimitIsAcceptedAsync`                                            | Fact | Entry at the entry limit is accepted.                                                  |
| `TenantTotalLimitIsEnforcedPerTenantAndIgnoresExpiredEntriesAsync`               | Fact | Tenant total limit is enforced per tenant and ignores expired entries.                 |
| `ShapeMustMatchTheDataLengthAsync`                                               | Fact | Shape must match the data length.                                                      |
| `NonPositiveTtlIsRejectedAsync`                                                  | Fact | Non positive ttl is rejected.                                                          |
| `MetadataOmitsTheDataAndDeleteRemovesTheEntryAsync`                              | Fact | Metadata omits the data and delete removes the entry.                                  |
| `CancelledTokenStopsEveryOperationAsync`                                         | Fact | Cancelled token stops every operation.                                                 |

### AdvancedAnalyticsCacheSettingsTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsCacheSettingsTests` — 4 tests.

| Test                                                                   | Kind | Verifies                                                                        |
|------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `DefaultsApplyWhenNothingIsConfigured`                                 | Fact | Defaults apply when nothing is configured.                                      |
| `ConfiguredValuesAreUsedAndInvalidOnesFallBack`                        | Fact | Configured values are used and invalid ones fall back.                          |
| `StageTimeBudgetDefaultsToOneSecondAndAcceptsZeroToDisable`            | Fact | Stage time budget defaults to one second and accepts zero to disable.           |
| `SettingsKeysAreRegisteredInTheDynamicEnvironmentWithMatchingDefaults` | Fact | Settings keys are registered in the dynamic environment with matching defaults. |

### AdvancedAnalyticsInstanceRepositoryTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsInstanceRepositoryTests` — 11 tests. Category: Service.
Collection: Database.

| Test                                                        | Kind | Verifies                                                         |
|-------------------------------------------------------------|------|------------------------------------------------------------------|
| `InsertStampsGuidTenantAuditAndReadsBackByIdAndGuidAsync`   | Fact | Insert stamps guid tenant audit and reads back by id and guid.   |
| `ForeignTenantSeesNotFoundOnEveryReadAsync`                 | Fact | Foreign tenant sees not found on every read.                     |
| `ForeignTenantCannotUpdateOrDeleteAsync`                    | Fact | Foreign tenant cannot update or delete.                          |
| `InsertAgainstAForeignTenantsModelIsNotFoundAsync`          | Fact | Insert against a foreign tenants model is not found.             |
| `ListFiltersByModelAndIsTenantScopedAsync`                  | Fact | List filters by model and is tenant scoped.                      |
| `UpdateChangesFieldsAndIncrementsVersionAsync`              | Fact | Update changes fields and increments version.                    |
| `UpdateToAForeignTenantsModelIsNotFoundAsync`               | Fact | Update to a foreign tenants model is not found.                  |
| `SoftDeleteHidesTheRowKeepsItInTheTableAndDeactivatesAsync` | Fact | Soft delete hides the row keeps it in the table and deactivates. |
| `DeleteWritesAPreDeleteAndAPostDeleteVersionSnapshotAsync`  | Fact | Delete writes a pre delete and a post delete version snapshot.   |
| `NamesAreUniquePerTenantIgnoringCaseAmongLiveRowsOnlyAsync` | Fact | Names are unique per tenant ignoring case among live rows only.  |
| `CancelledTokenStopsEveryOperationAsync`                    | Fact | Cancelled token stops every operation.                           |

### AdvancedAnalyticsPackingTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsPackingTests` — 3 tests.

| Test                                         | Kind | Verifies                                          |
|----------------------------------------------|------|---------------------------------------------------|
| `PackIsLittleEndianEightBytesPerValue`       | Fact | Pack is little endian eight bytes per value.      |
| `UnpackRejectsUnknownVersionAndRaggedLength` | Fact | Unpack rejects unknown version and ragged length. |
| `EmptyArrayRoundTrips`                       | Fact | Empty array round trips.                          |

### AdvancedAnalyticsPermissionTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsPermissionTests` — 2 tests. Category: Service. Collection:
Database.

| Test                                                   | Kind | Verifies                                                  |
|--------------------------------------------------------|------|-----------------------------------------------------------|
| `TheFourPermissionsExistWithTheAgreedIdsAndNamesAsync` | Fact | The four permissions exist with the agreed ids and names. |
| `TheAdministratorRoleIsGrantedAllFourAsync`            | Fact | The administrator role is granted all four.               |

### AdvancedAnalyticsTrainedModelRepositoryTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsTrainedModelRepositoryTests` — 16 tests. Category: Service.
Collection: Database.

| Test                                                                  | Kind | Verifies                                                                   |
|-----------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `InsertRoundTripsEveryKindAndSourceAndStartsInactiveAsync`            | Fact | Insert round trips every kind and source and starts inactive.              |
| `KindIsStoredAsANumberNotClearTextAsync`                              | Fact | Kind is stored as a number not clear text.                                 |
| `InsertAgainstAForeignTenantsInstanceIsNotFoundAsync`                 | Fact | Insert against a foreign tenants instance is not found.                    |
| `ForeignTenantReadsAreNotFoundAndListsAreScopedAsync`                 | Fact | Foreign tenant reads are not found and lists are scoped.                   |
| `ForeignTenantCannotMutateAsync`                                      | Fact | Foreign tenant cannot mutate.                                              |
| `ActivationRequiresAVersionAndActiveVersionMustBelongToTheModelAsync` | Fact | Activation requires a version and active version must belong to the model. |
| `RollbackIsFlippingTheActiveVersionAsync`                             | Fact | Rollback is flipping the active version.                                   |
| `UpdateChangesNameAndCorrelationKeyAndListsByCorrelationKeyAsync`     | Fact | Update changes name and correlation key and lists by correlation key.      |
| `SoftDeleteHidesTheModelAndDeactivatesItAsync`                        | Fact | Soft delete hides the model and deactivates it.                            |
| `VersionNumbersIncrementPerModelAsync`                                | Fact | Version numbers increment per model.                                       |
| `ModelPayloadIsStoredVerbatimAsJsonNotJsonbAsync`                     | Fact | Model payload is stored verbatim as json not jsonb.                        |
| `VersionsAreImmutableAndPayloadIsValidatedAsync`                      | Fact | Versions are immutable and payload is validated.                           |
| `VersionListOmitsThePayloadAndForeignTenantSeesNothingAsync`          | Fact | Version list omits the payload and foreign tenant sees nothing.            |
| `VersionCanReferenceItsProducingRunButNotAForeignOneAsync`            | Fact | Version can reference its producing run but not a foreign one.             |
| `ConcurrentVersionInsertsGetDistinctConsecutiveNumbersAsync`          | Fact | Concurrent version inserts get distinct consecutive numbers.               |
| `CancelledTokenStopsEveryOperationAsync`                              | Fact | Cancelled token stops every operation.                                     |

### AdvancedAnalyticsTrainingRunChildRepositoryTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsTrainingRunChildRepositoryTests` — 15 tests. Category: Service.
Collection: Database.

| Test                                                                     | Kind | Verifies                                                                         |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `StepsAreOrderedByStepOrderTakeInstanceFromTheRunAndRoundTripEnumsAsync` | Fact | Steps are ordered by step order take instance from the run and round trip enums. |
| `StepUpdateStampsTimingAndDurationOnTerminalStatusAsync`                 | Fact | Step update stamps timing and duration on terminal status.                       |
| `TrialsRecordHyperparametersScoreAndExactlyOneCanBeSelectedAsync`        | Fact | Trials record hyperparameters score and exactly one can be selected.             |
| `MarkSelectedRejectsATrialOfAnotherRunAsync`                             | Fact | Mark selected rejects a trial of another run.                                    |
| `IterationsInsertSingleAndInBulkAndListInIterationOrderAsync`            | Fact | Iterations insert single and in bulk and list in iteration order.                |
| `BulkInsertIsAtomicWhenOneRowIsRejectedAsync`                            | Fact | Bulk insert is atomic when one row is rejected.                                  |
| `BulkInsertRollsBackWhenTheDatabaseRejectsALaterRowAsync`                | Fact | Bulk insert rolls back when the database rejects a later row.                    |
| `MetricsRoundTripEverySplitAndFilterBySplitAsync`                        | Fact | Metrics round trip every split and filter by split.                              |
| `MetricOrIterationTrialFromAnotherRunIsNotFoundAsync`                    | Fact | Metric or iteration trial from another run is not found.                         |
| `VariablesKeepSequenceOrderAndStatisticsIncludingNonFiniteValuesAsync`   | Fact | Variables keep sequence order and statistics including non finite values.        |
| `CurvePointsRoundTripEveryCurveAndFilterAndOrderAsync`                   | Fact | Curve points round trip every curve and filter and order.                        |
| `ForeignTenantCannotWriteOrReadAnyChildTableAsync`                       | Fact | Foreign tenant cannot write or read any child table.                             |
| `ChildrenOfASoftDeletedRunCannotBeAddedAsync`                            | Fact | Children of a soft deleted run cannot be added.                                  |
| `EveryChildTableHasARealForeignKeyToTheInstanceAndTheRunAsync`           | Fact | Every child table has a real foreign key to the instance and the run.            |
| `CancelledTokenStopsChildOperationsAsync`                                | Fact | Cancelled token stops child operations.                                          |

### AdvancedAnalyticsTrainingRunRepositoryTests

`Jube.Test.AdvancedAnalytics.Repository.AdvancedAnalyticsTrainingRunRepositoryTests` — 11 tests. Category: Service.
Collection: Database.

| Test                                                                 | Kind | Verifies                                                                   |
|----------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `InsertStartsPendingStampsUserAndRoundTripsEveryKindAndJobModeAsync` | Fact | Insert starts pending stamps user and round trips every kind and job mode. |
| `StatusIsStoredAsANumberAndEveryStatusRoundTripsAsync`               | Fact | Status is stored as a number and every status round trips.                 |
| `ArgumentsAndInputPointerRoundTripAsync`                             | Fact | Arguments and input pointer round trip.                                    |
| `StatusTransitionsStampTimingsAndTerminalStatesAreFinalAsync`        | Fact | Status transitions stamp timings and terminal states are final.            |
| `CancellingRunCanStillBecomeCancelledAsync`                          | Fact | Cancelling run can still become cancelled.                                 |
| `ProgressIsClampedAndNanBecomesZeroAsync`                            | Fact | Progress is clamped and nan becomes zero.                                  |
| `ForeignTenantIsNotFoundOnEveryPathAsync`                            | Fact | Foreign tenant is not found on every path.                                 |
| `ListFiltersByInstanceAndStatusNewestFirstAndCapsTakeAsync`          | Fact | List filters by instance and status newest first and caps take.            |
| `SoftDeleteHidesTheRunAndKeepsTheRowAsync`                           | Fact | Soft delete hides the run and keeps the row.                               |
| `InsertAgainstADeletedInstanceIsNotFoundAsync`                       | Fact | Insert against a deleted instance is not found.                            |
| `CancelledTokenStopsEveryOperationAsync`                             | Fact | Cancelled token stops every operation.                                     |

### ModelSelectionToolsCachePointerIntegrationTests

`Jube.Test.AdvancedAnalytics.Repository.ModelSelectionToolsCachePointerIntegrationTests` — 2 tests. Category: Service.
Collection: Database.

| Test                                                                                            | Kind | Verifies                                                                                                    |
|-------------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------|
| `CompareModelsResolvesARealCachedTableAndReturnsARealComparisonAsync`                           | Fact | Compare models resolves a real cached table and returns a real comparison.                                  |
| `PutModelComparisonDataTurnsADataPointerIntoARealCachePointerThatCompareModelsCanThenReadAsync` | Fact | Put model comparison data turns a data pointer into a real cache pointer that compare models can then read. |

## Sequences

### AdvancedAnalyticsSequenceSetTests

`Jube.Test.AdvancedAnalytics.Sequences.AdvancedAnalyticsSequenceSetTests` — 16 tests.

| Test                                                        | Kind   | Verifies                                                             |
|-------------------------------------------------------------|--------|----------------------------------------------------------------------|
| `OffsetsAreDerivedFromLengths`                              | Fact   | Offsets are derived from lengths.                                    |
| `SequencesComeBackAsRows`                                   | Fact   | Sequences come back as rows.                                         |
| `JaggedRoundTripIsLossless`                                 | Fact   | Jagged round trip is lossless.                                       |
| `InputArraysAreCopiedSoTheSetIsImmutable`                   | Fact   | Input arrays are copied so the set is immutable.                     |
| `ReturnedSequencesAreCopies`                                | Fact   | Returned sequences are copies.                                       |
| `LengthsMustSumToTheObservationCount`                       | Fact   | Lengths must sum to the observation count.                           |
| `EmptySequencesAreRejected`                                 | Fact   | Empty sequences are rejected.                                        |
| `NoSequencesIsRejected`                                     | Fact   | No sequences is rejected.                                            |
| `FeatureCountMustBePositive`                                | Theory | Feature count must be positive.                                      |
| `ValueCountMustBeAWholeNumberOfObservations`                | Fact   | Value count must be a whole number of observations.                  |
| `NonFiniteValuesAreRejected`                                | Theory | Non finite values are rejected.                                      |
| `LengthsThatOverflowAnIntAreRejectedNotWrapped`             | Fact   | Lengths that overflow an int are rejected not wrapped.               |
| `RaggedJaggedInputWithInconsistentFeaturesIsRejected`       | Fact   | Ragged jagged input with inconsistent features is rejected.          |
| `SequenceIndexIsRangeChecked`                               | Fact   | Sequence index is range checked.                                     |
| `JsonShapeUsesTheAgreedNamesAndRoundTripsThroughValidation` | Fact   | Json shape uses the agreed names and round trips through validation. |
| `DeserialisingBrokenLengthsFailsThroughTheConstructor`      | Fact   | Deserialising broken lengths fails through the constructor.          |

### AdvancedAnalyticsSymbolSequenceSetTests

`Jube.Test.AdvancedAnalytics.Sequences.AdvancedAnalyticsSymbolSequenceSetTests` — 6 tests.

| Test                                         | Kind   | Verifies                                           |
|----------------------------------------------|--------|----------------------------------------------------|
| `OffsetsAndSequencesAreDerivedFromLengths`   | Fact   | Offsets and sequences are derived from lengths.    |
| `JaggedRoundTripIsLossless`                  | Fact   | Jagged round trip is lossless.                     |
| `InputIsCopiedAndReturnedSequencesAreCopies` | Fact   | Input is copied and returned sequences are copies. |
| `InvalidLayoutsAreRejected`                  | Theory | Invalid layouts are rejected.                      |
| `NoSequencesIsRejected`                      | Fact   | No sequences is rejected.                          |
| `JsonRoundTripKeepsTheAgreedShape`           | Fact   | Json round trip keeps the agreed shape.            |

## Serialisation

### AdvancedAnalyticsModelJsonTests

`Jube.Test.AdvancedAnalytics.Serialisation.AdvancedAnalyticsModelJsonTests` — 24 tests.

| Test                                                          | Kind   | Verifies                                                              |
|---------------------------------------------------------------|--------|-----------------------------------------------------------------------|
| `RoundTripIsBitExactForOrdinaryValues`                        | Fact   | Round trip is bit exact for ordinary values.                          |
| `RoundTripPreservesNaNInfinityAndNegativeZero`                | Fact   | Round trip preserves na n infinity and negative zero.                 |
| `NegativeZeroInsideArraysSurvives`                            | Fact   | Negative zero inside arrays survives.                                 |
| `RandomBitPatternsRoundTripExactly`                           | Fact   | Random bit patterns round trip exactly.                               |
| `WorkedExampleMapperRoundTripsTheModelAndItsPredictions`      | Fact   | Worked example mapper round trips the model and its predictions.      |
| `SnapshotIsIndependentOfTheModelItWasTakenFrom`               | Fact   | Snapshot is independent of the model it was taken from.               |
| `EnvelopeUsesCamelCaseAndCarriesNoTypeInformation`            | Fact   | Envelope uses camel case and carries no type information.             |
| `TypeNameInjectionIsRejectedNotHonoured`                      | Fact   | Type name injection is rejected not honoured.                         |
| `CalibratorRoundTrips`                                        | Fact   | Calibrator round trips.                                               |
| `InputDimensionMustMatchTheVariableCount`                     | Fact   | Input dimension must match the variable count.                        |
| `DeserialiseRejectsAnInputCountThatDisagreesWithTheVariables` | Fact   | Deserialise rejects an input count that disagrees with the variables. |
| `StandardErrorsMustHaveOneMoreElementThanTheInputs`           | Fact   | Standard errors must have one more element than the inputs.           |
| `MalformedOrIncompleteJsonFailsLoudly`                        | Theory | Malformed or incomplete json fails loudly.                            |
| `WrongKindIsRejected`                                         | Fact   | Wrong kind is rejected.                                               |
| `UnknownPropertiesAreRejectedRatherThanDropped`               | Fact   | Unknown properties are rejected rather than dropped.                  |
| `WrongElementTypesFailInsteadOfProducingHalfBuiltObjects`     | Fact   | Wrong element types fail instead of producing half built objects.     |
| `DuplicatePropertiesAreRejected`                              | Fact   | Duplicate properties are rejected.                                    |
| `FutureSchemaVersionIsRejected`                               | Fact   | Future schema version is rejected.                                    |
| `DuplicateVariableNamesAreRejected`                           | Fact   | Duplicate variable names are rejected.                                |
| `SizeCapAppliesToBothDirections`                              | Fact   | Size cap applies to both directions.                                  |
| `ExcessiveNestingIsRejected`                                  | Fact   | Excessive nesting is rejected.                                        |
| `ReadKindReturnsTheKindWithoutNeedingTheSnapshotType`         | Fact   | Read kind returns the kind without needing the snapshot type.         |
| `FromSnapshotRejectsAMismatchedStandardErrorLength`           | Fact   | From snapshot rejects a mismatched standard error length.             |
| `SerialisationIsCultureInvariant`                             | Fact   | Serialisation is culture invariant.                                   |

## Service

### AdvancedAnalyticsAdaBoostClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsAdaBoostClassifierTrainingServiceTests` — 2 tests. Category:
Service. Collection: Database.

| Test                                                                   | Kind | Verifies                                                                    |
|------------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `SubmitTrainsAndRecallsAnAdaBoostClassifierThroughTheSharedToolsAsync` | Fact | Submit trains and recalls an ada boost classifier through the shared tools. |
| `CancelViaTheSharedToolCancelsAPendingAdaBoostClassifierRunAsync`      | Fact | Cancel via the shared tool cancels a pending ada boost classifier run.      |

### AdvancedAnalyticsBaggingClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsBaggingClassifierTrainingServiceTests` — 2 tests. Category:
Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                |
|-----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `SubmitTrainsThroughTheRealExecutorAndTheRecalledVersionMatchesDirectRecallAsync` | Fact | Submit trains through the real executor and the recalled version matches direct recall. |
| `CancelToolCancelsAPendingBaggingRunAsync`                                        | Fact | Cancel tool cancels a pending bagging run.                                              |

### AdvancedAnalyticsBalancedKMeansClustererTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsBalancedKMeansClustererTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsBinarySplitClustererTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsBinarySplitClustererTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsChartServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsChartServiceTests` — 6 tests. Category: Service. Collection:
Database.

| Test                                                             | Kind | Verifies                                                            |
|------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `ChartsRefuseUsersWithoutTheAnalyticsPermissionAsync`            | Fact | Charts refuse users without the analytics permission.               |
| `ChartsRenderExternalStyleSvgForAnOwnVersionAsync`               | Fact | Charts render external style svg for an own version.                |
| `ChartsForForeignAndMissingVersionsAreIdenticalEmptyStatesAsync` | Fact | Charts for foreign and missing versions are identical empty states. |
| `AnUnrecognisedStoredModelShowsTheMessageInsteadOfFailingAsync`  | Fact | An unrecognised stored model shows the message instead of failing.  |
| `ACalibrationChartWithoutEvidenceIsAnEmptyStateAsync`            | Fact | A calibration chart without evidence is an empty state.             |
| `ARocChartWithoutCurvePointsIsAnEmptyStateAsync`                 | Fact | A roc chart without curve points is an empty state.                 |

### AdvancedAnalyticsDecisionTreeRegressorTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsDecisionTreeRegressorTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                                        | Kind | Verifies                                                       |
|-------------------------------------------------------------|------|----------------------------------------------------------------|
| `SubmitTrainsExecutesPromotesSyncsAndRecallsBitForBitAsync` | Fact | Submit trains executes promotes syncs and recalls bit for bit. |

### AdvancedAnalyticsGaussianMixtureClustererTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsGaussianMixtureClustererTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsGradientBoostingClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsGradientBoostingClassifierTrainingServiceTests` — 2 tests.
Category: Service. Collection: Database.

| Test                                                                                        | Kind | Verifies                                                                                             |
|---------------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------|
| `SubmitRunsExecutesDirectlyAndPersistsAVersionWhoseRecallMatchesADirectInProcessTrainAsync` | Fact | Submit runs executes directly and persists a version whose recall matches a direct in process train. |
| `SubmitRejectsAnInvalidLearningRateBeforeQueuingARunAsync`                                  | Fact | Submit rejects an invalid learning rate before queuing a run.                                        |

### AdvancedAnalyticsGradientBoostingRegressorTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsGradientBoostingRegressorTrainingServiceTests` — 2 tests.
Category: Service. Collection: Database.

| Test                                                                                        | Kind | Verifies                                                                                             |
|---------------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------|
| `SubmitRunsExecutesDirectlyAndPersistsAVersionWhoseRecallMatchesADirectInProcessTrainAsync` | Fact | Submit runs executes directly and persists a version whose recall matches a direct in process train. |
| `SubmitRejectsAnInvalidLearningRateBeforeQueuingARunAsync`                                  | Fact | Submit rejects an invalid learning rate before queuing a run.                                        |

### AdvancedAnalyticsInstanceActiveSwitchTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsInstanceActiveSwitchTests` — 15 tests. Category: Service.
Collection: Database.

| Test                                                                                 | Kind   | Verifies                                                                                     |
|--------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `SwitchingOnAndOffRoundTripsBumpsTheVersionAndChangesNothingElseAsync`               | Fact   | Switching on and off round trips bumps the version and changes nothing else.                 |
| `ListAndGetReturnTheActiveFlagAsync`                                                 | Fact   | List and get return the active flag.                                                         |
| `RepeatingTheSameStateChangesNothingAndPublishesNoEventAsync`                        | Fact   | Repeating the same state changes nothing and publishes no event.                             |
| `ASwitchPublishesOneUpdatedInstanceEventLikeAllocationDoesAsync`                     | Fact   | A switch publishes one updated instance event like allocation does.                          |
| `ASwitchWritesAnAuditLineNamingTheActorAndTheStateAsync`                             | Fact   | A switch writes an audit line naming the actor and the state.                                |
| `SwitchingAnUnallocatedInstanceOnIsRefusedWithAValidationErrorButOffIsHarmlessAsync` | Fact   | Switching an unallocated instance on is refused with a validation error but off is harmless. |
| `PermissionFortyEightAloneCannotSwitchAndFortySevenAloneCanAsync`                    | Fact   | Permission forty eight alone cannot switch and forty seven alone can.                        |
| `TheUpdateOperationTheLlmUsesStillNeedsFortySevenToChangeActiveAsync`                | Fact   | The update operation the llm uses still needs forty seven to change active.                  |
| `AnotherTenantCannotSwitchAnInstanceAndSeesItAsNotFoundAsync`                        | Fact   | Another tenant cannot switch an instance and sees it as not found.                           |
| `AnInvalidIdIsNotFoundAsync`                                                         | Theory | An invalid id is not found.                                                                  |
| `ASwitchedOffInstanceCanStillBeDeletedAndADeletedOneCannotBeSwitchedAsync`           | Fact   | A switched off instance can still be deleted and a deleted one cannot be switched.           |
| `TheOperationIsNotAnLlmToolAndCarriesItsDescriptionAndPermissionFortySeven`          | Fact   | The operation is not an llm tool and carries its description and permission forty seven.     |
| `SwitchingOffUnloadsTheModelOnTheNextSyncAndSwitchingOnLoadsItAgainAsync`            | Fact   | Switching off unloads the model on the next sync and switching on loads it again.            |
| `ThePipelineStageWritesNothingForASwitchedOffInstanceAndScoresOnceItIsOnAsync`       | Fact   | The pipeline stage writes nothing for a switched off instance and scores once it is on.      |
| `ARecallHeldAcrossASwitchOffAndSyncStillCompletesAndNewLookupsFailClearlyAsync`      | Fact   | A recall held across a switch off and sync still completes and new lookups fail clearly.     |

### AdvancedAnalyticsInstanceServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsInstanceServiceTests` — 23 tests. Category: Service. Collection:
Database.

| Test                                                                              | Kind | Verifies                                                                              |
|-----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `InsertPersistsUnallocatedUnlivedVersionOneInTheCallersTenantAsync`               | Fact | Insert persists unallocated unlived version one in the callers tenant.                |
| `InsertIgnoresASuppliedAllocationAsync`                                           | Fact | Insert ignores a supplied allocation.                                                 |
| `EveryOperationRefusesAnUnpermittedUserAsync`                                     | Fact | Every operation refuses an unpermitted user.                                          |
| `PromotePermissionAloneCannotManageInstancesAndAnalyticsAloneCannotAllocateAsync` | Fact | Promote permission alone cannot manage instances and analytics alone cannot allocate. |
| `GoingLiveOrChangingOutputFlagsNeedsThePromotePermissionAsync`                    | Fact | Going live or changing output flags needs the promote permission.                     |
| `AnInstanceCannotBeLiveWhileUnallocatedAsync`                                     | Fact | An instance cannot be live while unallocated.                                         |
| `NamesAreRequiredBoundedAndUniqueIgnoringCaseAsync`                               | Fact | Names are required bounded and unique ignoring case.                                  |
| `ConcurrentCreatesOfOneNameProduceExactlyOneInstanceAsync`                        | Fact | Concurrent creates of one name produce exactly one instance.                          |
| `UpdateRenamesWithoutChangingVersionAndNeverTouchesTheAllocationAsync`            | Fact | Update renames without changing version and never touches the allocation.             |
| `TenantsCannotSeeOrTouchEachOthersInstancesAsync`                                 | Fact | Tenants cannot see or touch each others instances.                                    |
| `ListPagesByIdAndFiltersByModelAsync`                                             | Fact | List pages by id and filters by model.                                                |
| `AllocateValidatesTheModelBelongsToTheTenantAndIsNotDeletedAsync`                 | Fact | Allocate validates the model belongs to the tenant and is not deleted.                |
| `AllocatePublishesOneUpdatedEventAndRepeatingItChangesNothingAsync`               | Fact | Allocate publishes one updated event and repeating it changes nothing.                |
| `AnAllocatedInstanceCannotMoveToAnotherModelUntilDeallocatedAsync`                | Fact | An allocated instance cannot move to another model until deallocated.                 |
| `DeallocateClearsTheModelAndLiveFlagAndIsIdempotentAsync`                         | Fact | Deallocate clears the model and live flag and is idempotent.                          |
| `ConcurrentAllocationsOfOneInstanceToDifferentModelsLeaveExactlyOneAsync`         | Fact | Concurrent allocations of one instance to different models leave exactly one.         |
| `DeleteSoftDeletesHidesTheRowAndPublishesDeletedAsync`                            | Fact | Delete soft deletes hides the row and publishes deleted.                              |
| `DeletingAnAllocatedInstanceNeedsThePromotePermissionAsync`                       | Fact | Deleting an allocated instance needs the promote permission.                          |
| `ServiceRefusesUsersWithoutAnIdentityOrATenantAsync`                              | Fact | Service refuses users without an identity or a tenant.                                |
| `PermissionDeniedIsAWarningNotAnErrorAsync`                                       | Fact | Permission denied is a warning not an error.                                          |
| `EachCallEmitsASpanADurationMeasurementAndOneAuditLineAsync`                      | Fact | Each call emits a span a duration measurement and one audit line.                     |
| `ReadsAndFailuresPublishNoChangeEventAsync`                                       | Fact | Reads and failures publish no change event.                                           |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                   | Fact | Pre cancelled token throws operation canceled.                                        |

### AdvancedAnalyticsIsolationForestAnomalyTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsIsolationForestAnomalyTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsKMedoidsClustererTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsKMedoidsClustererTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsKModesClustererTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsKModesClustererTrainingServiceTests` — 1 test. Category: Service.
Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsKNearestNeighboursRegressorTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsKNearestNeighboursRegressorTrainingServiceTests` — 1 test.
Category: Service. Collection: Database.

| Test                                                  | Kind | Verifies                                                |
|-------------------------------------------------------|------|---------------------------------------------------------|
| `SubmitTrainExecutesDirectlyAndRecallsBitForBitAsync` | Fact | Submit train executes directly and recalls bit for bit. |

### AdvancedAnalyticsKernelRidgeRegressorTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsKernelRidgeRegressorTrainingServiceTests` — 2 tests. Category:
Service. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                     |
|-------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `SubmitTrainsAndRecallsAKernelRidgeRegressorThroughTheSharedToolsAsync` | Fact | Submit trains and recalls a kernel ridge regressor through the shared tools. |
| `CancelViaTheSharedToolCancelsAPendingKernelRidgeRegressorRunAsync`     | Fact | Cancel via the shared tool cancels a pending kernel ridge regressor run.     |

### AdvancedAnalyticsKernelSupportVectorClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsKernelSupportVectorClassifierTrainingServiceTests` — 2 tests.
Category: Service. Collection: Database.

| Test                                                                             | Kind | Verifies                                                                               |
|----------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `SubmitTrainsAndRecallsAKernelSupportVectorClassifierThroughTheSharedToolsAsync` | Fact | Submit trains and recalls a kernel support vector classifier through the shared tools. |
| `CancelViaTheSharedToolCancelsAPendingKernelSupportVectorClassifierRunAsync`     | Fact | Cancel via the shared tool cancels a pending kernel support vector classifier run.     |

### AdvancedAnalyticsLinearRegressionServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsLinearRegressionServiceTests` — 2 tests. Category: Service.
Collection: Database.

| Test                                                                   | Kind | Verifies                                                                    |
|------------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `SubmitTrainRunExecutesPromotesSyncsAndRecallsBitForBitAsync`          | Fact | Submit train run executes promotes syncs and recalls bit for bit.           |
| `GetRunResultReturnsTheEvidenceTypedToTheLinearRegressionDetailsAsync` | Fact | Get run result returns the evidence typed to the linear regression details. |

### AdvancedAnalyticsLinearSupportVectorClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsLinearSupportVectorClassifierTrainingServiceTests` — 2 tests.
Category: Service. Collection: Database.

| Test                                                                             | Kind | Verifies                                                                               |
|----------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `SubmitTrainsAndRecallsALinearSupportVectorClassifierThroughTheSharedToolsAsync` | Fact | Submit trains and recalls a linear support vector classifier through the shared tools. |
| `CancelViaTheSharedToolCancelsAPendingLinearSupportVectorClassifierRunAsync`     | Fact | Cancel via the shared tool cancels a pending linear support vector classifier run.     |

### AdvancedAnalyticsMeanShiftClustererTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsMeanShiftClustererTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsMiniBatchKMeansClustererTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsMiniBatchKMeansClustererTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                           | Kind | Verifies                                        |
|------------------------------------------------|------|-------------------------------------------------|
| `SubmitTrainsExecutesAndRecallsBitForBitAsync` | Fact | Submit trains executes and recalls bit for bit. |

### AdvancedAnalyticsModelMapperTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsModelMapperTests` — 8 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                 |
|--------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `ReadEvidence_ReturnsNullForMissingOrUnusableJson`                             | Theory | Read evidence returns null for missing or unusable json.                                 |
| `ReadEvidence_ReturnsTheLogisticEvidenceForALogisticKind`                      | Fact   | Read evidence returns the logistic evidence for a logistic kind.                         |
| `ReadEvidence_SaysNothingRatherThanOverreachForAnUnknownKind`                  | Fact   | Read evidence says nothing rather than overreach for an unknown kind.                    |
| `ToPromotionEvidence_DispatchesLogisticEvidenceToTheLogisticDto`               | Fact   | To promotion evidence dispatches logistic evidence to the logistic dto.                  |
| `TransformedVariableCountLabel_ReturnsNullWhenThereIsNothingUsableToCount`     | Theory | Transformed variable count label returns null when there is nothing usable to count.     |
| `TransformedVariableCountLabel_CountsOnlyRowsWithATransformationOtherThanNone` | Fact   | Transformed variable count label counts only rows with a transformation other than none. |
| `TransformedVariableCountLabel_TreatsAnAbsentTransformationIdPropertyAsNone`   | Fact   | Transformed variable count label treats an absent transformation id property as none.    |
| `TransformedVariableCountLabel_AllVariablesTransformedIsReportedInFull`        | Fact   | Transformed variable count label all variables transformed is reported in full.          |

### AdvancedAnalyticsModelServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsModelServiceTests` — 21 tests. Category: Service. Collection:
Database.

| Test                                                                                | Kind | Verifies                                                                                    |
|-------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `ReadOperationsRefuseAnUnpermittedUserAsync`                                        | Fact | Read operations refuse an unpermitted user.                                                 |
| `PromotionAndRollbackRefuseAUserWithoutThePromotePermissionAsync`                   | Fact | Promotion and rollback refuse a user without the promote permission.                        |
| `ModelsAndVersionsAreListedWithHeadlineMetricsAndWarningsButNoPayloadAsync`         | Fact | Models and versions are listed with headline metrics and warnings but no payload.           |
| `ModelPagingClampsTakeAndContinuesAfterAnIdAsync`                                   | Fact | Model paging clamps take and continues after an id.                                         |
| `TenantsCannotSeeOrPromoteEachOthersModelsAsync`                                    | Fact | Tenants cannot see or promote each others models.                                           |
| `PromotionEvidenceCarriesExactlyWhatTheDecisionNeedsAsync`                          | Fact | Promotion evidence carries exactly what the decision needs.                                 |
| `PromotionEvidenceExposesAMissingTestSetAndItsWarningAsync`                         | Fact | Promotion evidence exposes a missing test set and its warning.                              |
| `CompareVersionsShowsFairnessAndTheMetricDifferencesAsync`                          | Fact | Compare versions shows fairness and the metric differences.                                 |
| `CompareNeedsTwoDistinctVersionsThatExistAsync`                                     | Fact | Compare needs two distinct versions that exist.                                             |
| `PromotionRequiresTheInstanceToBeAllocatedAndChangesNothingWithoutItAsync`          | Fact | Promotion requires the instance to be allocated and changes nothing without it.             |
| `PromotionRefusesWhenTheAllocatedJubeModelHasSinceBeenDeletedAsync`                 | Fact | Promotion refuses when the allocated jube model has since been deleted.                     |
| `PromotionMakesTheVersionLivePublishesOneEventAndRecordsTheEvidenceInTheAuditAsync` | Fact | Promotion makes the version live publishes one event and records the evidence in the audit. |
| `TheServiceAppliesNoQualityGateItRecordsTheWarningsInsteadAsync`                    | Fact | The service applies no quality gate it records the warnings instead.                        |
| `PromotingTheLiveVersionAgainChangesNothingAndPublishesNothingAsync`                | Fact | Promoting the live version again changes nothing and publishes nothing.                     |
| `ADeallocatedInstanceCanNoLongerPromoteAsync`                                       | Fact | A deallocated instance can no longer promote.                                               |
| `AVersionOfAnotherModelCannotBePromotedThroughThisOneAsync`                         | Fact | A version of another model cannot be promoted through this one.                             |
| `PromotingASecondVersionReplacesTheFirstAsTheOnlyLiveOneAsync`                      | Fact | Promoting a second version replaces the first as the only live one.                         |
| `RollbackReturnsToTheNextOlderVersionOrToANamedOneAsync`                            | Fact | Rollback returns to the next older version or to a named one.                               |
| `RollbackNeedsALiveVersionAnEarlierOneAndAnAllocationAsync`                         | Fact | Rollback needs a live version an earlier one and an allocation.                             |
| `TwoRacingPromotionsLeaveExactlyOneLiveVersionAsync`                                | Fact | Two racing promotions leave exactly one live version.                                       |
| `RacingPromotionAndRollbackNeverLeaveAModelWithoutAConsistentLiveVersionAsync`      | Fact | Racing promotion and rollback never leave a model without a consistent live version.        |

### AdvancedAnalyticsNaiveBayesClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsNaiveBayesClassifierTrainingServiceTests` — 2 tests. Category:
Service. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                     |
|-------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `SubmitTrainsAndRecallsANaiveBayesClassifierThroughTheSharedToolsAsync` | Fact | Submit trains and recalls a naive bayes classifier through the shared tools. |
| `CancelViaTheSharedToolCancelsAPendingNaiveBayesClassifierRunAsync`     | Fact | Cancel via the shared tool cancels a pending naive bayes classifier run.     |

### AdvancedAnalyticsNeuralNetworkClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsNeuralNetworkClassifierTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                  |
|----------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `ASubmittedRunTrainsPersistsAndIsReadableThroughTheSharedToolsAsync` | Fact | A submitted run trains persists and is readable through the shared tools. |

### AdvancedAnalyticsNeuralNetworkRegressorTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsNeuralNetworkRegressorTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                  |
|----------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `ASubmittedRunTrainsPersistsAndIsReadableThroughTheSharedToolsAsync` | Fact | A submitted run trains persists and is readable through the shared tools. |

### AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingServiceTests` — 2 tests.
Category: Service. Collection: Database.

| Test                                                                                 | Kind | Verifies                                                                                     |
|--------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `SubmitTrainsAndRecallsAOneClassSupportVectorAnomalyModelThroughTheSharedToolsAsync` | Fact | Submit trains and recalls a one class support vector anomaly model through the shared tools. |
| `CancelViaTheSharedToolCancelsAPendingOneClassSupportVectorAnomalyRunAsync`          | Fact | Cancel via the shared tool cancels a pending one class support vector anomaly run.           |

### AdvancedAnalyticsPrincipalComponentTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsPrincipalComponentTrainingServiceTests` — 2 tests. Category:
Service. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                     |
|-------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `SubmitTrainQueuedPromoteSyncAndRecallRoundTripThroughTheDatabaseAsync` | Fact | Submit train queued promote sync and recall round trip through the database. |
| `ToolsSubmitPollAndReadAPrincipalComponentTrainingRunAsync`             | Fact | Tools submit poll and read a principal component training run.               |

### AdvancedAnalyticsRandomForestClassifierSharedTrainingRunServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsRandomForestClassifierSharedTrainingRunServiceTests` — 1 test.
Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `TheSharedToolsReadAndCancelARunSubmittedThroughTheRandomForestSubmitToolAsync` | Fact | The shared tools read and cancel a run submitted through the random forest submit tool. |

### AdvancedAnalyticsRandomForestRegressorTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsRandomForestRegressorTrainingServiceTests` — 1 test. Category:
Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                             |
|---------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `SubmitThenExecutingDirectlyProducesAPromotableVersionWithAMatchingRecallAsync` | Fact | Submit then executing directly produces a promotable version with a matching recall. |

### AdvancedAnalyticsServiceConventionTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsServiceConventionTests` — 5 tests. Category: Service.

| Test                                                           | Kind | Verifies                                                                |
|----------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `EveryOperationIsCataloguedWithMatchingMetadataAndAUniqueName` | Fact | Every operation is catalogued with matching metadata and a unique name. |
| `ServiceToolNamesNeverCollideWithTheTrainerToolNames`          | Fact | Service tool names never collide with the trainer tool names.           |
| `OperationsAndTheirParametersAreDescribedInPlainWords`         | Fact | Operations and their parameters are described in plain words.           |
| `TrainingIsNotADuplicateToolAndCarriesItsOwnDescription`       | Fact | Training is not a duplicate tool and carries its own description.       |
| `EveryDtoPropertyCarriesADescription`                          | Fact | Every dto property carries a description.                               |

### AdvancedAnalyticsSharedTrainingRunServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsSharedTrainingRunServiceTests` — 1 test. Category: Service.
Collection: Database.

| Test                                                                            | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `TheSharedToolsReadAndCancelARunSubmittedThroughAFamilySpecificSubmitToolAsync` | Fact | The shared tools read and cancel a run submitted through a family specific submit tool. |

### AdvancedAnalyticsTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsTrainingServiceTests` — 21 tests. Category: Service. Collection:
Database.

| Test                                                                     | Kind   | Verifies                                                                       |
|--------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `TrainingRecordsTheRunItsStepsMetricsVariablesAndCurvesAsync`            | Fact   | Training records the run its steps metrics variables and curves.               |
| `TrainingStoresAnImmutableVersionUnderANewModelAndNeverActivatesItAsync` | Fact   | Training stores an immutable version under a new model and never activates it. |
| `TheStoredModelScoresExactlyAsItDidAtTrainingTimeAsync`                  | Fact   | The stored model scores exactly as it did at training time.                    |
| `RetrainingUnderTheSameNameAddsAVersionToTheSameModelAsync`              | Fact   | Retraining under the same name adds a version to the same model.               |
| `SameInputsAndSeedGiveIdenticalEvidenceInDistinctVersionsAsync`          | Fact   | Same inputs and seed give identical evidence in distinct versions.             |
| `DifferentModelNamesInTheSameInstanceAreDifferentModelsAsync`            | Fact   | Different model names in the same instance are different models.               |
| `TrainingRefusesUsersWithoutTheTrainingPermissionAndRecordsNothingAsync` | Fact   | Training refuses users without the training permission and records nothing.    |
| `AnotherTenantCannotTrainIntoAnInstanceItDoesNotOwnAsync`                | Fact   | Another tenant cannot train into an instance it does not own.                  |
| `ADeletedOrMissingInstanceIsRefusedAsync`                                | Fact   | A deleted or missing instance is refused.                                      |
| `InvalidRequestsAreRejectedBeforeAnyRunIsCreatedAsync`                   | Theory | Invalid requests are rejected before any run is created.                       |
| `ATrainerRefusalIsRecordedAsFailedWithItsReasonAndStoresNoModelAsync`    | Fact   | A trainer refusal is recorded as failed with its reason and stores no model.   |
| `AnInvalidOperationFromTheTrainerIsAlsoAFailedRunAsync`                  | Fact   | An invalid operation from the trainer is also a failed run.                    |
| `AnUnexpectedTrainerCrashIsRecordedGenericallyAndStillPropagatesAsync`   | Fact   | An unexpected trainer crash is recorded generically and still propagates.      |
| `ACancelledRunIsRecordedAsCancelledStoresNoModelAndPropagatesAsync`      | Fact   | A cancelled run is recorded as cancelled stores no model and propagates.       |
| `ATokenCancelledBeforeAnythingStartsCreatesNoRunAsync`                   | Fact   | A token cancelled before anything starts creates no run.                       |
| `AFailedRunDoesNotDisturbAnEarlierSuccessfulVersionAsync`                | Fact   | A failed run does not disturb an earlier successful version.                   |
| `TrainingPublishesCreatedForANewModelAndUpdatedForANewVersionAsync`      | Fact   | Training publishes created for a new model and updated for a new version.      |
| `TrainingWritesOneAuditLineAndNoWarningsAsync`                           | Fact   | Training writes one audit line and no warnings.                                |
| `TheDatabaseTraceRecordsAStepItsMetricsAndItsDurationAsync`              | Fact   | The database trace records a step its metrics and its duration.                |
| `TheDatabaseTraceTurnsANonStepEntryIntoAnIterationAsync`                 | Fact   | The database trace turns a non step entry into an iteration.                   |
| `TheDatabaseTraceRefusesARunItCannotSeeAsync`                            | Fact   | The database trace refuses a run it cannot see.                                |

### AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingServiceTests` — 2 tests.
Category: Service. Collection: Database.

| Test                                                                                 | Kind | Verifies                                                                                    |
|--------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `SubmitTrainsAndRecallsATreeAugmentedNaiveBayesClassifierThroughTheSharedToolsAsync` | Fact | Submit trains and recalls a tree augmented naive bayes classifier through the shared tools. |
| `CancelViaTheSharedToolCancelsAPendingTreeAugmentedNaiveBayesClassifierRunAsync`     | Fact | Cancel via the shared tool cancels a pending tree augmented naive bayes classifier run.     |

### DecisionTreeClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.DecisionTreeClassifierTrainingServiceTests` — 1 test. Category: Service.
Collection: Database.

| Test                                                                            | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `SubmitTrainsThroughTheRealWorkerAndTheRecalledVersionMatchesDirectRecallAsync` | Fact | Submit trains through the real worker and the recalled version matches direct recall. |

### KNearestNeighboursClassifierTrainingServiceTests

`Jube.Test.AdvancedAnalytics.Service.KNearestNeighboursClassifierTrainingServiceTests` — 1 test. Category: Service.
Collection: Database.

| Test                                                        | Kind | Verifies                                                       |
|-------------------------------------------------------------|------|----------------------------------------------------------------|
| `SubmitTrainThenRecallRoundTripsThroughTheSharedToolsAsync` | Fact | Submit train then recall round trips through the shared tools. |

## Training

### TrainingTraceAndJobModeTests

`Jube.Test.AdvancedAnalytics.Training.TrainingTraceAndJobModeTests` — 3 tests.

| Test                                       | Kind | Verifies                                  |
|--------------------------------------------|------|-------------------------------------------|
| `NullTraceAcceptsEntriesAndCompletesAsync` | Fact | Null trace accepts entries and completes. |
| `NullTraceIsASingleton`                    | Fact | Null trace is a singleton.                |
| `JobModeHasOnlineAndBatch`                 | Fact | Job mode has online and batch.            |

## Walkthrough

### AdvancedAnalyticsClustererWalkthroughTests

`Jube.Test.AdvancedAnalytics.Walkthrough.AdvancedAnalyticsClustererWalkthroughTests` — 2 tests. Category: Service.
Collection: Database.

| Test                                                                            | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `SubmitTrainsPromotesSyncsAndRecallsAClustererModelThroughTheServiceLayerAsync` | Fact | Submit trains promotes syncs and recalls a clusterer model through the service layer. |
| `CancelStopsAQueuedClustererRunBeforeAWorkerClaimsItAsync`                      | Fact | Cancel stops a queued clusterer run before a worker claims it.                        |

### AdvancedAnalyticsGaussianMixtureDensityWalkthroughTests

`Jube.Test.AdvancedAnalytics.Walkthrough.AdvancedAnalyticsGaussianMixtureDensityWalkthroughTests` — 2 tests. Category:
Service. Collection: Database.

| Test                                                                                       | Kind | Verifies                                                                                           |
|--------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `SubmitRunsPromotesSyncsAndRecallsAGaussianMixtureDensityModelThroughTheServiceLayerAsync` | Fact | Submit runs promotes syncs and recalls a gaussian mixture density model through the service layer. |
| `CancelToolCancelsAPendingGaussianMixtureDensityRunAsync`                                  | Fact | Cancel tool cancels a pending gaussian mixture density run.                                        |

### AdvancedAnalyticsMahalanobisAnomalyWalkthroughTests

`Jube.Test.AdvancedAnalytics.Walkthrough.AdvancedAnalyticsMahalanobisAnomalyWalkthroughTests` — 2 tests. Category:
Service. Collection: Database.

| Test                                                                                   | Kind | Verifies                                                                                      |
|----------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `SubmitRunsPromotesSyncsAndRecallsAMahalanobisAnomalyModelThroughTheServiceLayerAsync` | Fact | Submit runs promotes syncs and recalls a mahalanobis anomaly model through the service layer. |
| `CancelToolCancelsAPendingMahalanobisAnomalyRunAsync`                                  | Fact | Cancel tool cancels a pending mahalanobis anomaly run.                                        |

### AdvancedAnalyticsWalkthroughTests

`Jube.Test.AdvancedAnalytics.Walkthrough.AdvancedAnalyticsWalkthroughTests` — 1 test. Category: Service. Collection:
Database.

| Test                                                                              | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `TheLlmTrainsJudgesPromotesComparesAndRollsBackAModelThroughTheServiceLayerAsync` | Fact | The llm trains judges promotes compares and rolls back a model through the service layer. |

## Worker

### AdvancedAnalyticsTrainingSettingsAndInputTests

`Jube.Test.AdvancedAnalytics.Worker.AdvancedAnalyticsTrainingSettingsAndInputTests` — 5 tests. Category: Service.

| Test                                                                                             | Kind | Verifies                                                                                                  |
|--------------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------|
| `SettingsDefaultToTheDocumentedValuesAndFallBackOnBadValues`                                     | Fact | Settings default to the documented values and fall back on bad values.                                    |
| `TheResolverSupportsInlineAndCachePointerButNotDatasetUntilASourceIsRegistered`                  | Fact | The resolver supports inline and cache pointer but not dataset until a source is registered.              |
| `ADatasetSourceRegisteredWithTheResolverResolvesRowsAndAnUnregisteredOneThrowsNotSupportedAsync` | Fact | A dataset source registered with the resolver resolves rows and an unregistered one throws not supported. |
| `InlineInputWithoutRowsFailsClearlyAsync`                                                        | Fact | Inline input without rows fails clearly.                                                                  |
| `ArgumentsRoundTripThroughJsonWithMissingValuesAndStripInputData`                                | Fact | Arguments round trip through json with missing values and strip input data.                               |

### AdvancedAnalyticsTrainingSubmitTests

`Jube.Test.AdvancedAnalytics.Worker.AdvancedAnalyticsTrainingSubmitTests` — 17 tests. Category: Service. Collection:
Database.

| Test                                                                            | Kind | Verifies                                                                            |
|---------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `SubmitReturnsAtOnceWithAPendingBatchRunAndTrainsNothingAsync`                  | Fact | Submit returns at once with a pending batch run and trains nothing.                 |
| `SubmitAppliesTheExistingValidationAndCreatesNothingWhenItFailsAsync`           | Fact | Submit applies the existing validation and creates nothing when it fails.           |
| `SubmitRefusesInlineRowsOverTheConfiguredCapAsync`                              | Fact | Submit refuses inline rows over the configured cap.                                 |
| `SubmitRefusesInlineDataOverTheConfiguredByteCapAsync`                          | Fact | Submit refuses inline data over the configured byte cap.                            |
| `SubmitRefusesACachePointerThatDoesNotExistOrHasTheWrongShapeAsync`             | Fact | Submit refuses a cache pointer that does not exist or has the wrong shape.          |
| `SubmitRefusesACachePointerOfAnotherTenantAsync`                                | Fact | Submit refuses a cache pointer of another tenant.                                   |
| `SubmitReportsNotSupportedForDatasetInputAsync`                                 | Fact | Submit reports not supported for dataset input.                                     |
| `EveryTrainingOperationRefusesACallerWithoutThePermissionAsync`                 | Fact | Every training operation refuses a caller without the permission.                   |
| `SubmittingAndCancellingNeedTrainingAndReadingNeedsTheAnalyticsPermissionAsync` | Fact | Submitting and cancelling need training and reading needs the analytics permission. |
| `ARunOfAnotherTenantCannotBeSeenCancelledOrListedAsync`                         | Fact | A run of another tenant cannot be seen cancelled or listed.                         |
| `SubmittingAgainstAnotherTenantsInstanceIsRefusedAsync`                         | Fact | Submitting against another tenants instance is refused.                             |
| `TheSynchronousPathRefusesMoreRowsThanTheOnlineCapAndCreatesNoRunAsync`         | Fact | The synchronous path refuses more rows than the online cap and creates no run.      |
| `TheSynchronousPathStillTrainsUnderTheCapAndRefusesCacheInputAsync`             | Fact | The synchronous path still trains under the cap and refuses cache input.            |
| `TheResultCarriesTheFullEvidenceOnlyOnceTheRunSucceededAsync`                   | Fact | The result carries the full evidence only once the run succeeded.                   |
| `TheStatusReadShowsFailureAndCancellationReasonsAndListsFilterByStatusAsync`    | Fact | The status read shows failure and cancellation reasons and lists filter by status.  |
| `ACancelledRunIsFinalAndCannotBeRevivedByAnotherStopAsync`                      | Fact | A cancelled run is final and cannot be revived by another stop.                     |
| `ASucceededRunStaysSucceededWhenCancelledAfterwardsAsync`                       | Fact | A succeeded run stays succeeded when cancelled afterwards.                          |

### AdvancedAnalyticsTrainingToolConventionTests

`Jube.Test.AdvancedAnalytics.Worker.AdvancedAnalyticsTrainingToolConventionTests` — 6 tests. Category: Service.

| Test                                                              | Kind   | Verifies                                                                      |
|-------------------------------------------------------------------|--------|-------------------------------------------------------------------------------|
| `EachTrainingToolDeclaresItsPermissionAndIsDescribedInFull`       | Theory | Each training tool declares its permission and is described in full.          |
| `TheSubmitToolExplainsHowToPollAndTheSynchronousToolStatesItsCap` | Fact   | The submit tool explains how to poll and the synchronous tool states its cap. |
| `SubmitToolPassesEveryArgumentToTheRunsGatewayAsync`              | Fact   | Submit tool passes every argument to the runs gateway.                        |
| `StatusToolReadsTheRunStatusAsync`                                | Fact   | Status tool reads the run status.                                             |
| `CancelToolCancelsTheRunAsync`                                    | Fact   | Cancel tool cancels the run.                                                  |
| `ResultToolReadsTheResultAsync`                                   | Fact   | Result tool reads the result.                                                 |

### AdvancedAnalyticsTrainingToolEndToEndTests

`Jube.Test.AdvancedAnalytics.Worker.AdvancedAnalyticsTrainingToolEndToEndTests` — 2 tests. Category: Service.
Collection: Database.

| Test                                                        | Kind | Verifies                                                         |
|-------------------------------------------------------------|------|------------------------------------------------------------------|
| `TheToolsDriveARunThroughTheGatewayFromSubmitToResultAsync` | Fact | The tools drive a run through the gateway from submit to result. |
| `TheCancelToolCancelsAQueuedRunThroughTheGatewayAsync`      | Fact | The cancel tool cancels a queued run through the gateway.        |

### AdvancedAnalyticsTrainingWorkerTests

`Jube.Test.AdvancedAnalytics.Worker.AdvancedAnalyticsTrainingWorkerTests` — 29 tests. Category: Service. Collection:
Database.

| Test                                                                              | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `TheWorkerTrainsAnInlineRunEndToEndAndRecordsWhatTheSynchronousPathRecordsAsync`  | Fact | The worker trains an inline run end to end and records what the synchronous path records. |
| `TheWorkerAndTheSynchronousPathProduceIdenticalModelsForTheSameSeedAsync`         | Fact | The worker and the synchronous path produce identical models for the same seed.           |
| `ACachePointerRunTrainsIdenticallyToTheInlineRunAsync`                            | Fact | A cache pointer run trains identically to the inline run.                                 |
| `AnExpiredCachePointerFailsTheRunClearlyAndStoresNoModelAsync`                    | Fact | An expired cache pointer fails the run clearly and stores no model.                       |
| `ADatasetInputRunReportsNotSupportedAndStoresNoModelAsync`                        | Fact | A dataset input run reports not supported and stores no model.                            |
| `ARunWithNoUsableArgumentsFailsAndTheWorkerCarriesOnAsync`                        | Fact | A run with no usable arguments fails and the worker carries on.                           |
| `AFailedRunRecordsTheErrorAndStoresNoModelAsync`                                  | Fact | A failed run records the error and stores no model.                                       |
| `AnUnexpectedTrainerExceptionFailsTheRunGenericallyAndTheNextRunStillTrainsAsync` | Fact | An unexpected trainer exception fails the run generically and the next run still trains.  |
| `CancellingAPendingRunCancelsItAtOnceAndTheWorkerNeverPicksItUpAsync`             | Fact | Cancelling a pending run cancels it at once and the worker never picks it up.             |
| `StoppingByGuidBehavesLikeCancellingByIdAsync`                                    | Fact | Stopping by guid behaves like cancelling by id.                                           |
| `CancellingARunningRunStopsTheTrainerAndStoresNoModelAsync`                       | Fact | Cancelling a running run stops the trainer and stores no model.                           |
| `ARunOverTheMaximumDurationIsCancelledAndFailedWithAClearReasonAsync`             | Fact | A run over the maximum duration is cancelled and failed with a clear reason.              |
| `ARunningRunKeepsItsHeartbeatFreshAsync`                                          | Fact | A running run keeps its heartbeat fresh.                                                  |
| `ShuttingDownCancelsTheRunningRunAndStoresNoModelAsync`                           | Fact | Shutting down cancels the running run and stores no model.                                |
| `ARunRecoveredAsStaleWhileItsWorkerIsAliveNeverStoresAModelAsync`                 | Fact | A run recovered as stale while its worker is alive never stores a model.                  |
| `StaleRunningRunsAreRecoveredAtStartAndOnlyBatchRunsWithAnOldHeartbeatAsync`      | Fact | Stale running runs are recovered at start and only batch runs with an old heartbeat.      |
| `StaleRecoveryUsesTheInjectedClockAsync`                                          | Fact | Stale recovery uses the injected clock.                                                   |
| `TheLoopSurvivesDatabaseErrorsBacksOffAndThenTrainsTheRunAsync`                   | Fact | The loop survives database errors backs off and then trains the run.                      |
| `TheBackOffGrowsAfterErrorsAndIsCapped`                                           | Fact | The back off grows after errors and is capped.                                            |
| `TheGlobalConcurrencyCapHoldsSecondRunsBackUntilASlotFreesAsync`                  | Fact | The global concurrency cap holds second runs back until a slot frees.                     |
| `OneTenantCannotExceedItsConcurrencyCapWhileAnotherTenantStillGetsServedAsync`    | Fact | One tenant cannot exceed its concurrency cap while another tenant still gets served.      |
| `PickupIsOldestFirstWithinATenantAndRoundRobinAcrossTenantsAsync`                 | Fact | Pickup is oldest first within a tenant and round robin across tenants.                    |
| `ATenantWithAHugeBacklogCannotStarveAnotherTenantAsync`                           | Fact | A tenant with a huge backlog cannot starve another tenant.                                |
| `ManyConcurrentClaimersClaimEveryRunExactlyOnceAsync`                             | Fact | Many concurrent claimers claim every run exactly once.                                    |
| `ConcurrentClaimersNeverExceedTheTenantCapAsync`                                  | Fact | Concurrent claimers never exceed the tenant cap.                                          |
| `TheClaimStampsTheRunAsRunningWithAStartAHeartbeatAndTheClaimingNodeAsync`        | Fact | The claim stamps the run as running with a start a heartbeat and the claiming node.       |
| `OnlineRunsAndDeletedRunsAreNeverClaimedAsync`                                    | Fact | Online runs and deleted runs are never claimed.                                           |
| `TwoWorkersOnTheSameQueueNeverRunTheSameRunTwiceAsync`                            | Fact | Two workers on the same queue never run the same run twice.                               |
| `TwoIndependentNodesRunningTheLoopBothMakeProgressAndNeverShareARunAsync`         | Fact | Two independent nodes running the loop both make progress and never share a run.          |
