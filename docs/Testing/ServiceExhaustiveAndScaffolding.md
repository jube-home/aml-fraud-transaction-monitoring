---
layout: default
title: Service Exhaustive and Scaffolding
nav_order: 16
parent: Testing
---

# Service Exhaustive and Scaffolding

Tests of the services behind Exhaustive training pages: search instances, datasets, charts and model scaffolding.

## Exhaustive charts

### ExhaustiveCalibrationChartServiceTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveCalibrationChartServiceTests` — 10 tests. Category: Service. Collection:
Database.

| Test                                                                                                  | Kind | Verifies                                                                                                        |
|-------------------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------------|
| `ACalibratedModel_DrawsTheRawAndTheCalibratedCurvesWithTheStatisticsInTheCaptionAsync`                | Fact | A calibrated model draws the raw and the calibrated curves with the statistics in the caption.                  |
| `AHistoricalModel_DrawsOnlyTheRawCurveAndSaysItIsNotCalibratedAsync`                                  | Fact | A historical model draws only the raw curve and says it is not calibrated.                                      |
| `ASkippedCalibration_ShowsTheRecordedReasonAsync`                                                     | Fact | A skipped calibration shows the recorded reason.                                                                |
| `AnInvalidStoredCalibrator_IsReportedAndNotDrawnAsCalibratedAsync`                                    | Fact | An invalid stored calibrator is reported and not drawn as calibrated.                                           |
| `PermissionTenantAndUnknownIdBehaviourIsUnchangedAsync`                                               | Fact | Permission tenant and unknown id behaviour is unchanged.                                                        |
| `ThePredictedActualChart_ShowsCalibratedProbabilitiesForACalibratedModelAndLabelsThemAsync`           | Fact | The predicted actual chart shows calibrated probabilities for a calibrated model and labels them.               |
| `TheResultsPageMeasuresPanel_ForACalibratedModel_ShowsTheCalibrationStatisticsAndCaptionAsync`        | Fact | The results page measures panel for a calibrated model shows the calibration statistics and caption.            |
| `TheResultsPageCalibrationChart_ForACalibratedModel_DrawsBothCurvesAndForAHistoricalModelSaysSoAsync` | Fact | The results page calibration chart for a calibrated model draws both curves and for a historical model says so. |
| `TheResultsPageCharts_RenderForBothKindsOfModelAndStayIsolatedByTenantAsync`                          | Fact | The results page charts render for both kinds of model and stay isolated by tenant.                             |
| `TheRenderer_CalibratesRowsAtTheSamplePriorAndLeavesUncalibratedRowsAlone`                            | Fact | The renderer calibrates rows at the sample prior and leaves uncalibrated rows alone.                            |

### ExhaustiveChartRendererTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveChartRendererTests` — 12 tests. Category: Service.

| Test                                                      | Kind | Verifies                                                           |
|-----------------------------------------------------------|------|--------------------------------------------------------------------|
| `EveryChartUsesTheExternalStyleModeByDefault`             | Fact | Every chart uses the external style mode by default.               |
| `InlineModeStillGivesASelfContainedSvg`                   | Fact | Inline mode still gives a self contained svg.                      |
| `TheExternalOutputIsMuchSmallerThanTheInlineOutput`       | Fact | The external output is much smaller than the inline output.        |
| `AChartIdKeepsTheTitleAndDescriptionIdsUniquePerChart`    | Fact | A chart id keeps the title and description ids unique per chart.   |
| `EmptyInputsGiveAnEmptyStateNeverAnException`             | Fact | Empty inputs give an empty state never an exception.               |
| `RocIsSortedAndAnchoredAndCarriesTheAxisTitles`           | Fact | Roc is sorted and anchored and carries the axis titles.            |
| `PredictedActualSplitsByActualAndDropsAnEmptyClass`       | Fact | Predicted actual splits by actual and drops an empty class.        |
| `PredictedActualThinsVeryLargeInputsAndStaysWellFormed`   | Fact | Predicted actual thins very large inputs and stays well formed.    |
| `LearningCurveWithOnePointStillRenders`                   | Fact | Learning curve with one point still renders.                       |
| `ErrorAndVariableHistogramsUseTheirBinsAsCategories`      | Fact | Error and variable histograms use their bins as categories.        |
| `SensitivityKeepsOnlyTheTopBarsAndHostileNamesAreEscaped` | Fact | Sensitivity keeps only the top bars and hostile names are escaped. |
| `OutputIsDeterministic`                                   | Fact | Output is deterministic.                                           |

### ExhaustiveChartServiceTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveChartServiceTests` — 9 tests. Category: Service. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                     |
|-------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `EveryChartServiceRefusesAnUnauthenticatedCallerAsync`                  | Fact | Every chart service refuses an unauthenticated caller.                       |
| `EveryChartServiceDeniesACallerWithoutThePermissionAsync`               | Fact | Every chart service denies a caller without the permission.                  |
| `UnknownIdsReturnAnEmptyStateChartForEveryChartAsync`                   | Fact | Unknown ids return an empty state chart for every chart.                     |
| `RocChartMapsThePointsAndIsIndistinguishableForAnotherTenantAsync`      | Fact | Roc chart maps the points and is indistinguishable for another tenant.       |
| `LearningCurveMapsOnePointPerPromotionAndIsIsolatedByTenantAsync`       | Fact | Learning curve maps one point per promotion and is isolated by tenant.       |
| `PredictedActualHasPositiveAndNegativeSeriesAndIsIsolatedByTenantAsync` | Fact | Predicted actual has positive and negative series and is isolated by tenant. |
| `ErrorHistogramRendersBinsAndIsIsolatedByTenantAsync`                   | Fact | Error histogram renders bins and is isolated by tenant.                      |
| `VariableHistogramRendersTheRequestedVariableOnlyAsync`                 | Fact | Variable histogram renders the requested variable only.                      |
| `ConfusionChartUsesTheStoredCountsAndIsIsolatedByTenantAsync`           | Fact | Confusion chart uses the stored counts and is isolated by tenant.            |

### ExhaustiveNarrativeServiceTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveNarrativeServiceTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                         | Kind | Verifies                                                       |
|--------------------------------------------------------------|------|----------------------------------------------------------------|
| `GetAsync_ProducesEveryGroupForARealisticPromotedTrialAsync` | Fact | Get async produces every group for a realistic promoted trial. |
| `GetAsync_OmitsGroupsThatHaveNoDataInsteadOfGuessingAsync`   | Fact | Get async omits groups that have no data instead of guessing.  |

### ExhaustiveNarrativeTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveNarrativeTests` — 34 tests. Category: Service.

| Test                                                                            | Kind   | Verifies                                                                                 |
|---------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `DiscriminationBand_MapsAucToTheHosmerLemeshowConvention`                       | Theory | Discrimination band maps auc to the hosmer lemeshow convention.                          |
| `Performance_MentionsTheAucAndItsBandForARealSample`                            | Fact   | Performance mentions the auc and its band for a real sample.                             |
| `Performance_FlagsLowPrevalenceAsItsOwnParagraph`                               | Fact   | Performance flags low prevalence as its own paragraph.                                   |
| `Performance_IsEmptyWhenThereIsNoPerformanceData`                               | Fact   | Performance is empty when there is no performance data.                                  |
| `Calibration_DescribesScoresAsProbabilitiesOnlyWhenCalibrated`                  | Fact   | Calibration describes scores as probabilities only when calibrated.                      |
| `Sensitivity_NamesTheDominantVariableWhenOneStandsOut`                          | Fact   | Sensitivity names the dominant variable when one stands out.                             |
| `Sensitivity_SaysNoVariableDominatesWhenTwoAreSimilar`                          | Fact   | Sensitivity says no variable dominates when two are similar.                             |
| `Sensitivity_IsEmptyWhenThereAreNoVariables`                                    | Fact   | Sensitivity is empty when there are no variables.                                        |
| `Prescription_CallsOutAStrongDiscriminatorWhenTheClassRangesBarelyOverlap`      | Fact   | Prescription calls out a strong discriminator when the class ranges barely overlap.      |
| `Prescription_OneParagraphPerVariable`                                          | Fact   | Prescription one paragraph per variable.                                                 |
| `Prescription_CallsOutAWeakDiscriminatorWhenTheClassRangesOverlapSubstantially` | Fact   | Prescription calls out a weak discriminator when the class ranges overlap substantially. |
| `Prescription_IsEmptyWhenNeitherClassHasPrescriptionData`                       | Fact   | Prescription is empty when neither class has prescription data.                          |
| `Confusion_ExplainsTheMatrixInPlainTerms`                                       | Fact   | Confusion explains the matrix in plain terms.                                            |
| `Confusion_IsEmptyWhenThereIsNoPerformanceData`                                 | Fact   | Confusion is empty when there is no performance data.                                    |
| `Discrimination_MentionsAucAndAveragePrecision`                                 | Fact   | Discrimination mentions auc and average precision.                                       |
| `Ks_MentionsTheStatisticAndItsScore`                                            | Fact   | Ks mentions the statistic and its score.                                                 |
| `ThresholdSweep_MentionsTheCurrentThresholdTradeOff`                            | Fact   | Threshold sweep mentions the current threshold trade off.                                |
| `ScoreDistribution_DescribesSeparationBetweenClasses`                           | Fact   | Score distribution describes separation between classes.                                 |
| `ScoreDistribution_IsEmptyWithTooFewRowsPerClass`                               | Fact   | Score distribution is empty with too few rows per class.                                 |
| `Gains_DescribesTopDecileCaptureAndLift`                                        | Fact   | Gains describes top decile capture and lift.                                             |
| `Gains_IsEmptyWithFewerThanTenRows`                                             | Fact   | Gains is empty with fewer than ten rows.                                                 |
| `Holistic_JoinsWhicheverPartsAreAvailable`                                      | Fact   | Holistic joins whichever parts are available.                                            |
| `Holistic_FallsBackToASayNothingMessageWhenThereIsNoDataAtAll`                  | Fact   | Holistic falls back to a say nothing message when there is no data at all.               |
| `Build_GroupsPrescriptionRowsByVariableAcrossBothClasses`                       | Fact   | Build groups prescription rows by variable across both classes.                          |
| `Build_OrdersVariablesBySensitivityDescending`                                  | Fact   | Build orders variables by sensitivity descending.                                        |
| `PrecisionRecall_SaysWhichMeasureDominates`                                     | Fact   | Precision recall says which measure dominates.                                           |
| `PrecisionRecall_IsEmptyWhenThereIsNoPerformanceData`                           | Fact   | Precision recall is empty when there is no performance data.                             |
| `Lift_DescribesHowManyDecilesStayAboveRandom`                                   | Fact   | Lift describes how many deciles stay above random.                                       |
| `Lift_IsEmptyWithFewerThanTenRows`                                              | Fact   | Lift is empty with fewer than ten rows.                                                  |
| `Lift_SaysEveryDecileStaysAboveRandomWhenTheModelDiscriminatesThroughout`       | Fact   | Lift says every decile stays above random when the model discriminates throughout.       |
| `LearningCurve_DescribesPromotionsAndTrialsExplored`                            | Fact   | Learning curve describes promotions and trials explored.                                 |
| `LearningCurve_FlagsWhetherTheMostRecentPromotionsAreStillImproving`            | Fact   | Learning curve flags whether the most recent promotions are still improving.             |
| `LearningCurve_IsEmptyWithNoPromotionHistory`                                   | Fact   | Learning curve is empty with no promotion history.                                       |
| `LearningCurve_SaysNothingAboutTrendWithFewerThanThreePromotions`               | Fact   | Learning curve says nothing about trend with fewer than three promotions.                |

### ExhaustivePageChartWiringTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustivePageChartWiringTests` — 10 tests. Category: Service.

| Test                                                                                   | Kind   | Verifies                                                                                             |
|----------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `ExhaustivePageAndScriptsDoNotReferenceKendoChart`                                     | Theory | Exhaustive page and scripts do not reference kendo chart.                                            |
| `ExhaustivePageLoadsTheChartScriptAndScriptUsesEveryChartEndpoint`                     | Fact   | Exhaustive page loads the chart script and script uses every chart endpoint.                         |
| `PageNoLongerDrawsPredictedVersusActualTheErrorHistogramOrTheHtmlConfusionTable`       | Fact   | Page no longer draws predicted versus actual the error histogram or the html confusion table.        |
| `EveryPerformanceContainerInThePageIsFilledByTheScriptOnce`                            | Fact   | Every performance container in the page is filled by the script once.                                |
| `EveryChartHasAMatchingNarrativeDivAndGroup`                                           | Fact   | Every chart has a matching narrative div and group.                                                  |
| `PerformanceEndpointsAreGatedLikeTheOtherChartEndpointsAndAnswerSvgWithTheSameHeaders` | Fact   | Performance endpoints are gated like the other chart endpoints and answer svg with the same headers. |
| `ChartScriptCarriesNoInlineStyleSoAStrictStyleSrcWorks`                                | Fact   | Chart script carries no inline style so a strict style src works.                                    |
| `TheSharedLayoutCarriesTheChartStylesheetSoInjectedMarkupIsStyledByTheSelfPolicy`      | Fact   | The shared layout carries the chart stylesheet so injected markup is styled by the self policy.      |
| `ChartEndpointsNoLongerNeedInlineStylesInTheirContentSecurityPolicy`                   | Fact   | Chart endpoints no longer need inline styles in their content security policy.                       |
| `ChartScriptInjectsMarkupAndCarriesNoScriptExecution`                                  | Fact   | Chart script injects markup and carries no script execution.                                         |

### ExhaustivePerformanceChartServiceTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustivePerformanceChartServiceTests` — 9 tests. Category: Service. Collection:
Database.

| Test                                                                     | Kind | Verifies                                                                      |
|--------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `PerformanceAndHistoryServicesRefuseAnUnauthenticatedCallerAsync`        | Fact | Performance and history services refuse an unauthenticated caller.            |
| `EveryPerformanceChartAndTheHistoryDenyACallerWithoutThePermissionAsync` | Fact | Every performance chart and the history deny a caller without the permission. |
| `UnknownIdsGiveTheEmptyStateForEveryPerformanceChartAndTheHistoryAsync`  | Fact | Unknown ids give the empty state for every performance chart and the history. |
| `ForeignTenantAndUnknownIdsAreIndistinguishableForEveryChartAsync`       | Fact | Foreign tenant and unknown ids are indistinguishable for every chart.         |
| `PromotionHistoryChartMapsEveryPromotionAndIsIsolatedByTenantAsync`      | Fact | Promotion history chart maps every promotion and is isolated by tenant.       |
| `MeasuresPanelMatchesThePythonReferenceThroughTheDatabaseAsync`          | Fact | Measures panel matches the python reference through the database.             |
| `ThresholdAndCostsAreHonouredAndBadValuesFallBackToTheDefaultAsync`      | Fact | Threshold and costs are honoured and bad values fall back to the default.     |
| `EveryChartForARealSeededModelIsSafeSvgWithUniqueDistinctIdsAsync`       | Fact | Every chart for a real seeded model is safe svg with unique distinct ids.     |
| `OnlyTheMostRecentPromotedTrialRowsAreChartedAsync`                      | Fact | Only the most recent promoted trial rows are charted.                         |

### ExhaustivePerformanceRendererTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustivePerformanceRendererTests` — 24 tests. Category: Service.

| Test                                                                        | Kind   | Verifies                                                                              |
|-----------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `MeasuresMatchThePythonReference`                                           | Fact   | Measures match the python reference.                                                  |
| `MeasuresAtADifferentThresholdChangeOnlyTheThresholdMeasures`               | Fact   | Measures at a different threshold change only the threshold measures.                 |
| `MeasuresAreNullForEmptyOrSingleClassData`                                  | Fact   | Measures are null for empty or single class data.                                     |
| `ProbabilityMeasuresAreNotAvailableWhenScoresLeaveTheUnitInterval`          | Fact   | Probability measures are not available when scores leave the unit interval.           |
| `NonFiniteRowsAreDroppedBeforeAnyMeasure`                                   | Fact   | Non finite rows are dropped before any measure.                                       |
| `ActualIsTreatedAsPositiveClassFromOneHalfUp`                               | Fact   | Actual is treated as positive class from one half up.                                 |
| `MeasuresPanelCarriesEveryMeasureWithItsValueAndInterval`                   | Fact   | Measures panel carries every measure with its value and interval.                     |
| `EveryPerformanceChartRendersSafeExternalSvgWithTheExpectedTitles`          | Fact   | Every performance chart renders safe external svg with the expected titles.           |
| `ScoreDistributionShowsPositiveAndNegativeAsSeparateSeriesWithTheThreshold` | Fact   | Score distribution shows positive and negative as separate series with the threshold. |
| `ConfusionChartShowsTheCountsAtTheThreshold`                                | Fact   | Confusion chart shows the counts at the threshold.                                    |
| `ThresholdSweepMarksTheMinimumRiskThresholdOnlyWhenBothCostsAreSupplied`    | Fact   | Threshold sweep marks the minimum risk threshold only when both costs are supplied.   |
| `MinimumRiskThresholdIsTheBayesPointOrNullForUnusableCosts`                 | Fact   | Minimum risk threshold is the bayes point or null for unusable costs.                 |
| `SafeThresholdFallsBackToTheDefault`                                        | Theory | Safe threshold falls back to the default.                                             |
| `EveryChartAnswersTheEmptyStateForNoRowsAndForASingleClass`                 | Fact   | Every chart answers the empty state for no rows and for a single class.               |
| `CalibrationIsEmptyWhenScoresAreNotProbabilities`                           | Fact   | Calibration is empty when scores are not probabilities.                               |
| `PerformanceChartIdsAreUniquePerChart`                                      | Fact   | Performance chart ids are unique per chart.                                           |
| `OutputIsDeterministic`                                                     | Fact   | Output is deterministic.                                                              |
| `InlineModeIsStillAvailable`                                                | Fact   | Inline mode is still available.                                                       |
| `LargeRowSetsStayWithinTheLimits`                                           | Fact   | Large row sets stay within the limits.                                                |
| `HostileTextInRowsCannotExistButHostileHistoryNamesAreNotRenderedAtAll`     | Fact   | Hostile text in rows cannot exist but hostile history names are not rendered at all.  |
| `PromotionHistoryPlotsScoreBestSoFarAndComplexityAgainstTime`               | Fact   | Promotion history plots score best so far and complexity against time.                |
| `PromotionHistoryBestSoFarNeverDecreasesAndComplexityUsesTheSecondaryAxis`  | Fact   | Promotion history best so far never decreases and complexity uses the secondary axis. |
| `PromotionHistoryWithASinglePromotionAndWithNoneStillRender`                | Fact   | Promotion history with a single promotion and with none still render.                 |
| `TheFormatOfTimestampsDoesNotDependOnTheCurrentCulture`                     | Fact   | The format of timestamps does not depend on the current culture.                      |

### ExhaustiveStatisticsChartRendererTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveStatisticsChartRendererTests` — 24 tests. Category: Service.

| Test                                                                                 | Kind | Verifies                                                                                          |
|--------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `AlignedClassesAreTwoSeriesNegativeFirstThenPositiveInTheirFixedColourSlots`         | Fact | Aligned classes are two series negative first then positive in their fixed colour slots.          |
| `AlignedClassesAreShownAsShareOfClassByDefaultAndAsCountsOnRequest`                  | Fact | Aligned classes are shown as share of class by default and as counts on request.                  |
| `EachClassIsNormalisedByItsOwnTotalSoTheImbalanceDoesNotHideThePositiveClass`        | Fact | Each class is normalised by its own total so the imbalance does not hide the positive class.      |
| `ClassesAreAnEmptyStateWithoutDataWhateverTheVariableName`                           | Fact | Classes are an empty state without data whatever the variable name.                               |
| `OlderInstancesShowAllRowsAndThePositiveClassAsDensitiesWithAClearCaption`           | Fact | Older instances show all rows and the positive class as densities with a clear caption.           |
| `OlderInstanceDensitiesHaveUnitAreaOnASharedNumericAxis`                             | Fact | Older instance densities have unit area on a shared numeric axis.                                 |
| `OlderInstanceWithAnAnomalyFallsBackToTheAnomalyRowsAsThePositiveClass`              | Fact | Older instance with an anomaly falls back to the anomaly rows as the positive class.              |
| `OlderInstanceWithoutAnyPositiveClassSaysSoAndKeepsTheAllRowsSeries`                 | Fact | Older instance without any positive class says so and keeps the all rows series.                  |
| `LargeViewIsBiggerThanTheGridCell`                                                   | Fact | Large view is bigger than the grid cell.                                                          |
| `MonteCarloOverlaysTheModelProfileOnTheNegativeAndPositiveClassDensities`            | Fact | Monte carlo overlays the model profile on the negative and positive class densities.              |
| `MonteCarloOnAnOlderInstanceUsesAllRowsAndTheSeparatelyBinnedPositiveClass`          | Fact | Monte carlo on an older instance uses all rows and the separately binned positive class.          |
| `MonteCarloWithoutAnyClassDataKeepsTheModelProfileInItsOwnColourSlot`                | Fact | Monte carlo without any class data keeps the model profile in its own colour slot.                |
| `MonteCarloCountsShowsFrequencyInsteadOfDensity`                                     | Fact | Monte carlo counts shows frequency instead of density.                                            |
| `MonteCarloEmptyStateNamesTheCausesAndIsIdenticalForUnknownAndUnprescribedVariables` | Fact | Monte carlo empty state names the causes and is identical for unknown and unprescribed variables. |
| `MonteCarloSummaryCompareseachStatisticInStandardDeviationsOfAllRows`                | Fact | Monte carlo summary compareseach statistic in standard deviations of all rows.                    |
| `NegativeSummaryIsThePopulationWhenTheClassificationSelectedThePositiveRows`         | Fact | Negative summary is the population when the classification selected the positive rows.            |
| `NegativeSummaryMeanAndDeviationAreExactBySubtractionFromThePopulation`              | Fact | Negative summary mean and deviation are exact by subtraction from the population.                 |
| `NegativeSummaryIsUnavailableWithoutAnAnomalyOrClassificationSummary`                | Fact | Negative summary is unavailable without an anomaly or classification summary.                     |
| `QuantileInterpolatesInsideTheBin`                                                   | Fact | Quantile interpolates inside the bin.                                                             |
| `EdgesRejectOverlapsNonFiniteValuesAndWidenAConstantBin`                             | Fact | Edges reject overlaps non finite values and widen a constant bin.                                 |
| `HostileNamesAreEscapedAndOutputIsDeterministicAndExternalByDefault`                 | Fact | Hostile names are escaped and output is deterministic and external by default.                    |
| `NonFiniteAndHugeNumbersNeverBreakTheSvg`                                            | Fact | Non finite and huge numbers never break the svg.                                                  |
| `InlineStyleModeStillGivesASelfContainedSvg`                                         | Fact | Inline style mode still gives a self contained svg.                                               |
| `AChartIdKeepsTheTitleAndDescriptionIdsUnique`                                       | Fact | A chart id keeps the title and description ids unique.                                            |

### ExhaustiveStatisticsChartServiceTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveStatisticsChartServiceTests` — 7 tests. Category: Service. Collection:
Database.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `ClassesChartHasTheTwoClassesForTheRequestedVariableOnlyAsync` | Fact | Classes chart has the two classes for the requested variable only. |
| `MonteCarloAndSummaryChartsCarryTheModelProfileAsync`          | Fact | Monte carlo and summary charts carry the model profile.            |
| `MonteCarloCountsModeShowsCountInsteadOfDensityAsync`          | Fact | Monte carlo counts mode shows count instead of density.            |
| `GridReturnsOneChartPerVariableInOneCallForEveryViewAsync`     | Fact | Grid returns one chart per variable in one call for every view.    |
| `UnknownForeignAndEmptyIdsGiveTheSameEmptyStateAsync`          | Fact | Unknown foreign and empty ids give the same empty state.           |
| `EveryPathRefusesACallerWithoutThePermissionAsync`             | Fact | Every path refuses a caller without the permission.                |
| `CreateWithoutAUserThrowsNotAuthenticatedAsync`                | Fact | Create without a user throws not authenticated.                    |

### ExhaustiveStatisticsPageWiringTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveStatisticsPageWiringTests` — 6 tests. Category: Service.

| Test                                                                  | Kind | Verifies                                                                         |
|-----------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `PageIncludesTheStatisticsScriptAfterTheChartScript`                  | Fact | Page includes the statistics script after the chart script.                      |
| `ScriptUsesTheGridClassesMonteCarloSummaryAndSingleVariableRoutes`    | Fact | Script uses the grid classes monte carlo summary and single variable routes.     |
| `ScriptCarriesNoInlineStyleKendoChartOrComment`                       | Fact | Script carries no inline style kendo chart or comment.                           |
| `TheStatisticsGridDelegatesToTheScriptAndKeepsTheSingleChartFallback` | Fact | The statistics grid delegates to the script and keeps the single chart fallback. |
| `EndpointsAreRegisteredAdditivelyAndKeepTheHardenedSvgHeaders`        | Fact | Endpoints are registered additively and keep the hardened svg headers.           |
| `NoNewSourceWordsRefertoTheExcludedLibraryName`                       | Fact | No new source words referto the excluded library name.                           |

### ExhaustiveTopologyChartServiceTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveTopologyChartServiceTests` — 7 tests. Category: Service. Collection:
Database.

| Test                                                                   | Kind | Verifies                                                                     |
|------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `RefusesAnUnauthenticatedCallerAsync`                                  | Fact | Refuses an unauthenticated caller.                                           |
| `DeniesACallerWithoutThePermissionAsync`                               | Fact | Denies a caller without the permission.                                      |
| `DrawsTheStoredNetworkWithRealVariableNamesAndSensitivityShadingAsync` | Fact | Draws the stored network with real variable names and sensitivity shading.   |
| `StillDrawsTheNetworkWhenThereIsNoSensitivityDataAsync`                | Fact | Still draws the network when there is no sensitivity data.                   |
| `ForeignTenantUnknownAndNotTrainedIdsAreAllTheSameEmptyStateAsync`     | Fact | Foreign tenant unknown and not trained ids are all the same empty state.     |
| `AnUnreadableStoredTopologyIsAMessageNotAnErrorAsync`                  | Fact | An unreadable stored topology is a message not an error.                     |
| `AMismatchBetweenVariableCountAndInputsFallsBackToTheStoredNamesAsync` | Fact | A mismatch between variable count and inputs falls back to the stored names. |

### ExhaustiveTopologyRendererTests

`Jube.Test.Service.ExhaustiveCharts.ExhaustiveTopologyRendererTests` — 16 tests. Category: Service.

| Test                                                                           | Kind   | Verifies                                                                                       |
|--------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `ARealStoredTopologyIsDrawnWithItsVariableNamesAndACaption`                    | Fact   | A real stored topology is drawn with its variable names and a caption.                         |
| `EveryRealTopologyRendersSafeExternalSvgAndStatesItsActivation`                | Theory | Every real topology renders safe external svg and states its activation.                       |
| `InputNodesAreShadedBySensitivityWithAKeyAndHoverDetail`                       | Fact   | Input nodes are shaded by sensitivity with a key and hover detail.                             |
| `LiveRelevanceShadesEveryLayerNotJustInputsAndTakesPrecedenceOverSensitivity`  | Fact   | Live relevance shades every layer not just inputs and takes precedence over sensitivity.       |
| `LiveRelevanceWithAMismatchedLayerCountIsIgnored`                              | Fact   | Live relevance with a mismatched layer count is ignored.                                       |
| `WithoutSensitivityTheDiagramHasNoKeyAndNoShading`                             | Fact   | Without sensitivity the diagram has no key and no shading.                                     |
| `SensitivityOnlyForUnknownNamesOrAllZeroDoesNotShadeAnything`                  | Fact   | Sensitivity only for unknown names or all zero does not shade anything.                        |
| `ANameCountThatDoesNotMatchTheInputsFallsBackToTheStoredNamesInsteadOfFailing` | Fact   | A name count that does not match the inputs falls back to the stored names instead of failing. |
| `HostileVariableNamesAndSensitivityNamesCannotInjectMarkup`                    | Fact   | Hostile variable names and sensitivity names cannot inject markup.                             |
| `NoJsonGivesTheNotTrainedEmptyState`                                           | Fact   | No json gives the not trained empty state.                                                     |
| `UnreadableJsonGivesAClearMessageInsteadOfAnError`                             | Theory | Unreadable json gives a clear message instead of an error.                                     |
| `AVeryWideNetworkIsDrawnAsTheDensityViewNotAnError`                            | Fact   | A very wide network is drawn as the density view not an error.                                 |
| `ANetworkBeyondTheLayerLimitOffersTheLayerSummaryInsteadOfADiagram`            | Fact   | A network beyond the layer limit offers the layer summary instead of a diagram.                |
| `AnyExceptionMessageFromTheStoredJsonNeverAppearsInTheOutput`                  | Fact   | Any exception message from the stored json never appears in the output.                        |
| `InlineModeStillWorks`                                                         | Fact   | Inline mode still works.                                                                       |
| `ATwoTwoOneNetworkIsDrawnInFull`                                               | Fact   | A two two one network is drawn in full.                                                        |

## Exhaustive instance dataset

### ExhaustiveDatasetWiringTests

`Jube.Test.Service.ExhaustiveInstanceDataset.ExhaustiveDatasetWiringTests` — 10 tests. Category: Service.

| Test                                                                        | Kind | Verifies                                                                                |
|-----------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `Page_LoadsTheDatasetScriptBeforeThePageScriptAndHasEveryControl`           | Fact | Page loads the dataset script before the page script and has every control.             |
| `PageScript_ReadsAndWritesTheDataSourceThroughTheDatasetScript`             | Fact | Page script reads and writes the data source through the dataset script.                |
| `DatasetScript_UsesTheEndpointsKendoWidgetsAndNeverInjectsHtml`             | Fact | Dataset script uses the endpoints kendo widgets and never injects html.                 |
| `TheDemoEnvironmentVariableIsGoneFromCodeAndDocs`                           | Fact | The demo environment variable is gone from code and docs.                               |
| `EnvironmentDefaults_AreFiftyMegabytesAMillionRowsAndFiveHundredColumns`    | Fact | Environment defaults are fifty megabytes a million rows and five hundred columns.       |
| `EnvironmentOverrides_AreHonoured_AndGarbageFallsBackToDefaults`            | Fact | Environment overrides are honoured and garbage falls back to defaults.                  |
| `NewSourceFiles_CarryNoCodeComments`                                        | Fact | New source files carry no code comments.                                                |
| `Endpoints_MapTheCombinedAndBothKindRoutesWithTheJsonBodyAndEncodingChecks` | Fact | Endpoints map the combined and both kind routes with the json body and encoding checks. |
| `NoCodeOrDocsCarryTheRetiredDatasetNames`                                   | Fact | No code or docs carry the retired dataset names.                                        |
| `NewFiles_NeverUseTheForbiddenLibraryWord`                                  | Fact | New files never use the forbidden library word.                                         |

### ExhaustiveInstanceDatasetServiceTests

`Jube.Test.Service.ExhaustiveInstanceDataset.ExhaustiveInstanceDatasetServiceTests` — 29 tests. Category: Service.
Collection: Database.

| Test                                                                                      | Kind   | Verifies                                                                                         |
|-------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------|
| `Upload_StoresAPendingDatasetWithColumnsRowsAndWarningsAsync`                             | Fact   | Upload stores a pending dataset with columns rows and warnings.                                  |
| `Upload_ManyRows_AreBatchedAndAllStoredAsync`                                             | Fact   | Upload many rows are batched and all stored.                                                     |
| `Upload_RejectedFile_StoresNothingAtAllAsync`                                             | Fact   | Upload rejected file stores nothing at all.                                                      |
| `Upload_LateFailureAfterManyRows_RollsEverythingBackAsync`                                | Fact   | Upload late failure after many rows rolls everything back.                                       |
| `Upload_ClientDisconnectMidStream_RollsBackAndStoresNothingAsync`                         | Fact   | Upload client disconnect mid stream rolls back and stores nothing.                               |
| `Upload_CapsFromLimits_AreEnforcedAsync`                                                  | Fact   | Upload caps from limits are enforced.                                                            |
| `FileName_IsReducedToItsLeaf`                                                             | Theory | File name is reduced to its leaf.                                                                |
| `FileName_UnacceptableValuesAreRejected`                                                  | Theory | File name unacceptable values are rejected.                                                      |
| `Upload_HostileContentTypes_AreRejectedAsync`                                             | Theory | Upload hostile content types are rejected.                                                       |
| `Upload_HostileSuppliedNames_AreRejectedOrDefaultedAsync`                                 | Theory | Upload hostile supplied names are rejected or defaulted.                                         |
| `Upload_DuplicateSuppliedName_IsRejected_AndDerivedNamesAreDeduplicatedAsync`             | Fact   | Upload duplicate supplied name is rejected and derived names are deduplicated.                   |
| `Confirm_ValidMapping_MakesTheDatasetUsableAsync`                                         | Fact   | Confirm valid mapping makes the dataset usable.                                                  |
| `Confirm_InvalidMappings_AreRefusedAndTheDatasetStaysPendingAsync`                        | Fact   | Confirm invalid mappings are refused and the dataset stays pending.                              |
| `Confirm_ServerOwnedFieldsCannotBeInjectedThroughTheMappingAsync`                         | Fact   | Confirm server owned fields cannot be injected through the mapping.                              |
| `Permissions_EveryOperationIsRefusedWithoutThePermissionAsync`                            | Fact   | Permissions every operation is refused without the permission.                                   |
| `Authentication_UnknownAndTenantlessUsersAreRefusedAsync`                                 | Fact   | Authentication unknown and tenantless users are refused.                                         |
| `TenantIsolation_AnotherTenantSeesAndChangesNothingAsync`                                 | Fact   | Tenant isolation another tenant sees and changes nothing.                                        |
| `TenantIsolation_RepositoryNeverReturnsAnotherTenantsDatasetAsync`                        | Fact   | Tenant isolation repository never returns another tenants dataset.                               |
| `Delete_RemovesRowsAndHidesTheDatasetAsync`                                               | Fact   | Delete removes rows and hides the dataset.                                                       |
| `Instance_UploadedDataSource_RequiresAConfirmedDatasetOfTheSameTenantAsync`               | Fact   | Instance uploaded data source requires a confirmed dataset of the same tenant.                   |
| `Instance_DatasetOfAnotherTenant_IsRejected_MassAssignmentProtectedAsync`                 | Fact   | Instance dataset of another tenant is rejected mass assignment protected.                        |
| `Instance_DataSourceValues_AreKnownOnly_AndDatasetOnlyWithUploadedAsync`                  | Fact   | Instance data source values are known only and dataset only with uploaded.                       |
| `Instance_ValidatorWithoutADatasetRepository_FailsClosedAsync`                            | Fact   | Instance validator without a dataset repository fails closed.                                    |
| `Delete_IsRefusedWhileAnyInstanceUsesItRegardlessOfStatusAsync`                           | Fact   | Delete is refused while any instance uses it regardless of status.                               |
| `EndToEnd_UploadConfirmTrain_ReachesAPromotedTrialWithTheMappedVariablesAsync`            | Fact   | End to end upload confirm train reaches a promoted trial with the mapped variables.              |
| `Upload_CreditRiskFixture_FitsPurposeAsCategoricalAndOffersDependentAsAStringColumnAsync` | Fact   | Upload credit risk fixture fits purpose as categorical and offers dependent as a string column.  |
| `EndToEnd_CategoricalColumnAndStringDependent_TrainsSuccessfullyAsync`                    | Fact   | End to end categorical column and string dependent trains successfully.                          |
| `EndToEnd_TheFilteredColumnIsNeverIncludedAsATrainingVariableEvenIfMappedIncludedAsync`   | Fact   | End to end the filtered column is never included as a training variable even if mapped included. |
| `Training_AnUnconfirmedOrForeignDataset_StopsAtStatusSeventeenWithoutReadingRowsAsync`    | Fact   | Training an unconfirmed or foreign dataset stops at status seventeen without reading rows.       |

### ExhaustiveInstanceJsonServiceTests

`Jube.Test.Service.ExhaustiveInstanceDataset.ExhaustiveInstanceJsonServiceTests` — 31 tests. Category: Service.
Collection: Database.

| Test                                                                            | Kind   | Verifies                                                                           |
|---------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `UploadJson_StoresAPendingJsonDatasetWithColumnsRowsHashAndLabelAsync`          | Fact   | Upload json stores a pending json dataset with columns rows hash and label.        |
| `UploadJson_ColumnarShapeAndStringOverload_AreAcceptedAsync`                    | Fact   | Upload json columnar shape and string overload are accepted.                       |
| `UploadJson_HasTheSameRowsAndColumnsAsTheSameDataUploadedAsCsvAsync`            | Fact   | Upload json has the same rows and columns as the same data uploaded as csv.        |
| `UploadJson_RejectedBody_StoresNothingAsync`                                    | Theory | Upload json rejected body stores nothing.                                          |
| `UploadJson_ErrorLateInTheBody_RollsBackTheRowsAlreadyWrittenAsync`             | Fact   | Upload json error late in the body rolls back the rows already written.            |
| `UploadJson_CapsFromTheServiceLimitsAreEnforcedAsync`                           | Fact   | Upload json caps from the service limits are enforced.                             |
| `UploadJson_StringOverload_RejectsEmptyOversizeAndLoneSurrogatesAsync`          | Fact   | Upload json string overload rejects empty oversize and lone surrogates.            |
| `UploadJson_SourceLabelThatIsAPathOrMarkup_IsRejectedAsync`                     | Theory | Upload json source label that is a path or markup is rejected.                     |
| `UploadJson_LongLabelIsRejected_AndMissingLabelGetsADefaultAsync`               | Fact   | Upload json long label is rejected and missing label gets a default.               |
| `UploadJson_ContentTypeAndCharsetAreCheckedAsync`                               | Theory | Upload json content type and charset are checked.                                  |
| `UploadJson_AcceptableContentTypesAreAcceptedAsync`                             | Theory | Upload json acceptable content types are accepted.                                 |
| `JsonFileUpload_RequiresAJsonExtensionAndUsesTheLeafAsTheLabelAsync`            | Fact   | Json file upload requires a json extension and uses the leaf as the label.         |
| `CsvUpload_RejectsAJsonFileNameAndJsonContentAsync`                             | Fact   | Csv upload rejects a json file name and json content.                              |
| `JsonUpload_WithCsvContentUnderAJsonNameIsRejectedAsync`                        | Fact   | Json upload with csv content under a json name is rejected.                        |
| `Names_AreUniqueAcrossBothKindsAndDefaultedFromTheLabelAsync`                   | Fact   | Names are unique across both kinds and defaulted from the label.                   |
| `Confirm_MakesAJsonDatasetUsable_AndItIsNotUsableBeforeAsync`                   | Fact   | Confirm makes a json dataset usable and it is not usable before.                   |
| `Confirm_UsesTheSharedMappingRules_ForJsonDatasetsAsync`                        | Fact   | Confirm uses the shared mapping rules for json datasets.                           |
| `Confirm_WithTheWrongKind_FindsNothingAsync`                                    | Fact   | Confirm with the wrong kind finds nothing.                                         |
| `Permissions_EveryJsonOperationIsRefusedWithoutThePermissionAsync`              | Fact   | Permissions every json operation is refused without the permission.                |
| `TenantIsolation_AnotherTenantSeesAndChangesNoJsonDatasetAsync`                 | Fact   | Tenant isolation another tenant sees and changes no json dataset.                  |
| `CombinedListing_ShowsBothKinds_AndTheKindFilterNarrowsItAsync`                 | Fact   | Combined listing shows both kinds and the kind filter narrows it.                  |
| `Delete_RemovesTheDatasetAndItsRowsAcrossKindsAsync`                            | Fact   | Delete removes the dataset and its rows across kinds.                              |
| `Instance_ReferencesExactlyOneConfirmedDatasetOfTheSameTenantAsync`             | Fact   | Instance references exactly one confirmed dataset of the same tenant.              |
| `Instance_JsonDatasetOfAnotherTenant_IsRejectedAsync`                           | Fact   | Instance json dataset of another tenant is rejected.                               |
| `Delete_IsRefusedWhileAnyInstanceUsesTheJsonDatasetRegardlessOfStatusAsync`     | Fact   | Delete is refused while any instance uses the json dataset regardless of status.   |
| `CheckConstraint_EnforcesExactlyOneDatasetReferenceOnlyForUploadedDatasetAsync` | Fact   | Check constraint enforces exactly one dataset reference only for uploaded dataset. |
| `Migration_CreatesTheStoresRowTablesColumnsAndIndexesOnAFreshDatabaseAsync`     | Fact   | Migration creates the stores row tables columns and indexes on a fresh database.   |
| `SqlGate_DeniesEveryDatasetStoreAndRowTable`                                    | Fact   | Sql gate denies every dataset store and row table.                                 |
| `EndToEnd_UploadJsonConfirmTrain_MatchesTheSameDataUploadedAsCsvAsync`          | Fact   | End to end upload json confirm train matches the same data uploaded as csv.        |
| `Training_APendingOrForeignJsonDataset_StopsAtStatusSeventeenAsync`             | Fact   | Training a pending or foreign json dataset stops at status seventeen.              |
| `Training_ARunReferencingBothOrNeitherDataset_StopsAtStatusSeventeenAsync`      | Fact   | Training a run referencing both or neither dataset stops at status seventeen.      |

## Exhaustive search instance

### ExhaustiveSearchInstanceServiceTests

`Jube.Test.Service.ExhaustiveSearchInstance.ExhaustiveSearchInstanceServiceTests` — 61 tests. Category: Service.
Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `InsertedRowSatisfiesTheTrainingEnginesPickupPredicateAsync`                          | Fact   | Inserted row satisfies the training engines pickup predicate.                               |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `UpdateOfRowAlreadyPickedUpForTrainingThrowsNotFoundAsync`                            | Fact   | Update of row already picked up for training throws not found.                              |
| `SetActiveOfRowAlreadyPickedUpForTrainingSucceedsAsync`                               | Fact   | Set active of row already picked up for training succeeds.                                  |
| `SetActiveOfUnknownIdThrowsNotFoundAsync`                                             | Fact   | Set active of unknown id throws not found.                                                  |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `StopUpdatesStatusToStoppingAsync`                                                    | Fact   | Stop updates status to stopping.                                                            |
| `StopOnUnknownGuidIsANoOpAndDoesNotThrowAsync`                                        | Fact   | Stop on unknown guid is a no op and does not throw.                                         |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `AnomalyProbabilityRequiredWhenAnomalyIsTrueAndOutOfRangeIsRejectedAsync`             | Fact   | Anomaly probability required when anomaly is true and out of range is rejected.             |
| `BaseRateOutsideZeroToOneExclusiveIsRejectedAsync`                                    | Theory | Base rate outside zero to one exclusive is rejected.                                        |
| `BaseRateIsOptionalPersistsOnInsertAndUpdateAndIsSettableByTheClientAsync`            | Fact   | Base rate is optional persists on insert and update and is settable by the client.          |
| `TheServerWrittenCalibrationColumnsAreNotOnTheClientWritableInstanceDto`              | Fact   | The server written calibration columns are not on the client writable instance dto.         |
| `AnomalyProbabilityNotValidatedWhenAnomalyIsFalseAsync`                               | Fact   | Anomaly probability not validated when anomaly is false.                                    |
| `FilterFieldsRequiredWhenFilterIsTrueAsync`                                           | Fact   | Filter fields required when filter is true.                                                 |
| `FilterFieldsPersistWhenFilterIsTrueAndSuppliedAsync`                                 | Fact   | Filter fields persist when filter is true and supplied.                                     |
| `TamperedIdentityAuditAndTrainingStatusFieldsOnInsertHaveNoEffectAsync`               | Fact   | Tampered identity audit and training status fields on insert have no effect.                |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on update have no effect.                                |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                | Fact   | Update of locked row throws not found.                                                      |
| `UpdateOfSoftDeletedRowRevivesItAsync`                                                | Fact   | Update of soft deleted row revives it.                                                      |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                           | Fact   | Update of never existed id throws not found.                                                |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                               | Fact   | Delete of missing or already deleted row throws not found.                                  |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                | Fact   | Resubmit of non rejected version throws approval refused and writes no row.                 |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                | Fact   | Resubmit of rejected version bumps version archives old and keeps content.                  |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                     | Fact   | Resubmit of rejected delete keeps deleted true and reopens approval.                        |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                               | Fact   | Retract of non rejected version throws approval refused.                                    |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                             | Fact   | Retract with no approved baseline throws approval refused.                                  |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync` | Fact   | Retract of rejected edit restores last approved content and stamps the rejection retracted. |
| `RetractOfRejectedDeleteUndeletesAsync`                                               | Fact   | Retract of rejected delete undeletes.                                                       |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                                      | Fact   | Active and locked round trip bool to byte and back.                                         |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                     | Fact   | Get by id for missing id returns null not exception.                                        |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                 | Fact   | Pre cancelled token on read throws operation canceled.                                      |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                   | Fact   | Permission denied logs warn not error and gates hold.                                       |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                            | Fact   | Successful insert logs exactly one info and reads log no info.                              |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                             | Fact   | With gates disabled happy path records no debug info or warn.                               |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                        | Fact   | Failure logs error with exception attached and still propagates.                            |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                             | Fact   | Each call emits one span with outcome tag.                                                  |
| `EachCallRecordsOneDurationMeasurementAsync`                                          | Fact   | Each call records one duration measurement.                                                 |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                       | Fact   | Audit log gets exactly one line including reads.                                            |
| `InsertPublishesExactlyOneCreatedEventAsync`                                          | Fact   | Insert publishes exactly one created event.                                                 |
| `ReadsAndFailuresPublishNothingAsync`                                                 | Fact   | Reads and failures publish nothing.                                                         |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                 | Fact   | Catalogue registers unique pascal case no underscore names.                                 |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                               | Fact   | List async clamps take and paginates deterministically.                                     |
| `AnomalyAlgorithmAndParametersPersistAndAreAuditedAsync`                              | Fact   | Anomaly algorithm and parameters persist and are audited.                                   |
| `DefaultAnomalyAlgorithmIsStoredAsNullAndReadBackAsNullAsync`                         | Fact   | Default anomaly algorithm is stored as null and read back as null.                          |
| `TheAnomalyReportIsServerWrittenAndAClientSuppliedValueIsIgnoredAsync`                | Fact   | The anomaly report is server written and a client supplied value is ignored.                |
| `TheAnomalyReportRoundTripsThroughTheMessagePackPreservationFormatAsync`              | Fact   | The anomaly report round trips through the message pack preservation format.                |
| `InvalidAnomalyAlgorithmParametersAndContaminationAreRejectedAsync`                   | Theory | Invalid anomaly algorithm parameters and contamination are rejected.                        |
| `TheContaminationParameterSatisfiesTheContaminationRuleAsync`                         | Fact   | The contamination parameter satisfies the contamination rule.                               |

## Model scaffolding

### ModelScaffoldTests

`Jube.Test.Service.ModelScaffolding.ModelScaffoldTests` — 13 tests. Collection: Database.

| Test                                                                        | Kind   | Verifies                                                                         |
|-----------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `Copy_HasTheSameRowCountsAsTheSourceModelAsync`                             | Fact   | Copy has the same row counts as the source model.                                |
| `Copy_RemapsEveryForeignKeyOntoTheCopyAsync`                                | Fact   | Copy remaps every foreign key onto the copy.                                     |
| `DefaultOptions_UseTheDocumentationGuidAndTheNamePrefixAsync`               | Fact   | Default options use the documentation guid and the name prefix.                  |
| `Options_SetTheActiveAndLockedFlagsAsync`                                   | Theory | Options set the active and locked flags.                                         |
| `DefaultExclusions_LeaveRuntimeAndCaseDataAndArchiveOutAsync`               | Fact   | Default exclusions leave runtime and case data and archive out.                  |
| `ExcludeTables_LeavesATableOutAndEverythingBelowItAsync`                    | Fact   | Exclude tables leaves a table out and everything below it.                       |
| `Dispose_LeavesNoRowsBehindAsync`                                           | Fact   | Dispose leaves no rows behind.                                                   |
| `TwoScaffolds_LiveSideBySideAsync`                                          | Fact   | Two scaffolds live side by side.                                                 |
| `SuppliedTenant_IsKeptWhenTheScaffoldIsRemovedAsync`                        | Fact   | Supplied tenant is kept when the scaffold is removed.                            |
| `NamePrefix_MustStartWithTheFixturePrefixAsync`                             | Fact   | Name prefix must start with the fixture prefix.                                  |
| `MissingSourceModel_FailsNamingTheModel_AndLeavesNothingBehindAsync`        | Fact   | Missing source model fails naming the model and leaves nothing behind.           |
| `IncludeTables_NamingAnUnreachableTable_FailsLoudlyAsync`                   | Fact   | Include tables naming an unreachable table fails loudly.                         |
| `ATableWithoutAnIdentityId_FailsNamingTheTable_AndLeavesNothingBehindAsync` | Fact   | A table without an identity id fails naming the table and leaves nothing behind. |

### ModelSyncTests

`Jube.Test.Service.ModelScaffolding.ModelSyncTests` — 4 tests. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                   |
|-------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `SharedModel_IsSynchronisedByTheRealEngine_AndListsItsCaller`           | Fact | Shared model is synchronised by the real engine and lists its caller.      |
| `IsolatedCopies_SynchroniseAlongsideTheSharedModelAsync`                | Fact | Isolated copies synchronise alongside the shared model.                    |
| `ChangingAnIsolatedCopy_ThenResynchronising_IsPickedUpByTheEngineAsync` | Fact | Changing an isolated copy then resynchronising is picked up by the engine. |
| `SyncAsync_TimesOutSayingWhatItWasWaitingOnAsync`                       | Fact | Sync async times out saying what it was waiting on.                        |

### SyncScheduleEngineTests

`Jube.Test.Service.ModelScaffolding.SyncScheduleEngineTests` — 8 tests. Collection: Database.

| Test                                                             | Kind | Verifies                                                                |
|------------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `ASyncNowIsPickedUpByTheEngineAsync`                             | Fact | A sync now is picked up by the engine.                                  |
| `AFutureScheduleIsNotSynchronisedBeforeItsTimeAsync`             | Fact | A future schedule is not synchronised before its time.                  |
| `ASyncOnTheScheduleKeepsTheModelLoadedAsync`                     | Fact | A sync on the schedule keeps the model loaded.                          |
| `ADeadEngineHasItsSnapshotRemovedByTheNextLiveSyncAsync`         | Fact | A dead engine has its snapshot removed by the next live sync.           |
| `AWarningForADeadEngineIsRaisedWhileItIsWithinTheRetentionAsync` | Fact | A warning for a dead engine is raised while it is within the retention. |
| `AWarningForADeadEngineDropsOutAfterTheRetentionAsync`           | Fact | A warning for a dead engine drops out after the retention.              |
| `AWarningForADeadEngineIsClearedByTheNextLiveSyncAsync`          | Fact | A warning for a dead engine is cleared by the next live sync.           |
| `AnEngineThatStillReportsIsKeptThroughALiveSyncAsync`            | Fact | An engine that still reports is kept through a live sync.               |
