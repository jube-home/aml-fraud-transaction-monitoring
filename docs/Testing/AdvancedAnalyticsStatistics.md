---
layout: default
title: Advanced Analytics Statistics
nav_order: 2
parent: Testing
---

# Advanced Analytics Statistics

Tests of the Advanced Analytics statistical methods: distributions, hypothesis tests, correlation, regression
diagnostics and the numerical building blocks behind the semantic statistics tools.

## General

### CategoricalAndSurvivalClassifierToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.CategoricalAndSurvivalClassifierToolsTests` — 57 tests.

| Test                                                                                      | Kind | Verifies                                                                                            |
|-------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `CalculateBinaryClassificationMetricsFromCounts_MatchesHandComputation`                   | Fact | Calculate binary classification metrics from counts matches hand computation.                       |
| `CalculateBinaryClassificationMetricsFromCounts_FBetaFollowsBeta`                         | Fact | Calculate binary classification metrics from counts f beta follows beta.                            |
| `CalculateBinaryClassificationMetricsFromCounts_DegenerateAndInvalidInputs`               | Fact | Calculate binary classification metrics from counts degenerate and invalid inputs.                  |
| `CalculateBinaryClassificationMetricsFromLabels_CountsTheConfusionMatrix`                 | Fact | Calculate binary classification metrics from labels counts the confusion matrix.                    |
| `CalculateBinaryClassificationMetricsFromLabels_RejectsBadFlags`                          | Fact | Calculate binary classification metrics from labels rejects bad flags.                              |
| `CalculateBinaryClassificationMetricsAtThreshold_ThresholdIsInclusiveAndNaNIsDropped`     | Fact | Calculate binary classification metrics at threshold threshold is inclusive and na n is dropped.    |
| `CalculateBinaryClassificationMetricsAtThreshold_RejectsNaNThresholdAndAllNaNScores`      | Fact | Calculate binary classification metrics at threshold rejects na n threshold and all na n scores.    |
| `CalculateMulticlassClassificationMetrics_MatchesWikipediaCatsDogsRabbits`                | Fact | Calculate multiclass classification metrics matches wikipedia cats dogs rabbits.                    |
| `CalculateMulticlassClassificationMetrics_WeightedKappaClassOrderAndErrors`               | Fact | Calculate multiclass classification metrics weighted kappa class order and errors.                  |
| `CalculateMulticlassMetricsFromConfusionMatrix_MatchesLabelsVersion`                      | Fact | Calculate multiclass metrics from confusion matrix matches labels version.                          |
| `CalculateMulticlassMetricsFromConfusionMatrix_RejectsNonSquareAndFractionalCounts`       | Fact | Calculate multiclass metrics from confusion matrix rejects non square and fractional counts.        |
| `ComputeRocCurve_AucIsRankProbabilityByHand`                                              | Fact | Compute roc curve auc is rank probability by hand.                                                  |
| `ComputeRocCurve_ThinsPointsDropsNaNAndRejectsSingleClass`                                | Fact | Compute roc curve thins points drops na n and rejects single class.                                 |
| `ComputeRocCurve_TiedScoresGiveHalfCredit`                                                | Fact | Compute roc curve tied scores give half credit.                                                     |
| `ComparePairedRocAucDelong_InvertedScoreHasMirroredAucAndDoubleStandardError`             | Fact | Compare paired roc auc delong inverted score has mirrored auc and double standard error.            |
| `ComparePairedRocAucDelong_IdenticalScoresHaveNoDifference`                               | Fact | Compare paired roc auc delong identical scores have no difference.                                  |
| `ComputePrecisionRecallCurve_AveragePrecisionMatchesHandComputation`                      | Fact | Compute precision recall curve average precision matches hand computation.                          |
| `ComputePrecisionRecallCurve_RejectsNoPositives`                                          | Fact | Compute precision recall curve rejects no positives.                                                |
| `ComputeLiftGainTable_MatchesHandComputedQuintiles`                                       | Fact | Compute lift gain table matches hand computed quintiles.                                            |
| `ComputeLiftGainTable_UnevenGroupSizesAndInvalidGroupCounts`                              | Fact | Compute lift gain table uneven group sizes and invalid group counts.                                |
| `ComputeKolmogorovSmirnovSeparation_MatchesHandComputation`                               | Fact | Compute kolmogorov smirnov separation matches hand computation.                                     |
| `ComputeKolmogorovSmirnovSeparation_PerfectSeparationIsOneAndSingleClassIsRejected`       | Fact | Compute kolmogorov smirnov separation perfect separation is one and single class is rejected.       |
| `ComputeThresholdSweep_ExplicitThresholdsMatchHandCounts`                                 | Fact | Compute threshold sweep explicit thresholds match hand counts.                                      |
| `ComputeThresholdSweep_AutomaticThresholdsAreCappedAndThresholdAboveMaxFlagsNothing`      | Fact | Compute threshold sweep automatic thresholds are capped and threshold above max flags nothing.      |
| `SelectThresholdMaximisingFBeta_PicksHandComputedOptimum`                                 | Fact | Select threshold maximising f beta picks hand computed optimum.                                     |
| `SelectThresholdMaximisingFBeta_RejectsBadBetaAndSingleClass`                             | Fact | Select threshold maximising f beta rejects bad beta and single class.                               |
| `SelectThresholdMaximisingYoudenIndex_TiesChooseTheHighestThreshold`                      | Fact | Select threshold maximising youden index ties choose the highest threshold.                         |
| `SelectThresholdMaximisingYoudenIndex_PerfectSeparationGivesOne`                          | Fact | Select threshold maximising youden index perfect separation gives one.                              |
| `SelectThresholdMaximisingMatthewsCorrelation_PicksHandComputedOptimum`                   | Fact | Select threshold maximising matthews correlation picks hand computed optimum.                       |
| `SelectThresholdMaximisingMatthewsCorrelation_InvertedScoreStillReturnsBestAvailable`     | Fact | Select threshold maximising matthews correlation inverted score still returns best available.       |
| `SelectThresholdMinimisingCost_MatchesHandComputedMinimum`                                | Fact | Select threshold minimising cost matches hand computed minimum.                                     |
| `SelectThresholdMinimisingCost_FlaggingNothingCanBeCheapestAndBadCostsAreRejected`        | Fact | Select threshold minimising cost flagging nothing can be cheapest and bad costs are rejected.       |
| `SelectThresholdForTargetRecall_ReturnsFewestAlertsReachingTarget`                        | Fact | Select threshold for target recall returns fewest alerts reaching target.                           |
| `SelectThresholdForTargetRecall_FullRecallAndInvalidTargets`                              | Fact | Select threshold for target recall full recall and invalid targets.                                 |
| `SelectThresholdForTargetPrecision_ReturnsLowestThresholdKeepingPrecision`                | Fact | Select threshold for target precision returns lowest threshold keeping precision.                   |
| `SelectThresholdForTargetPrecision_UnreachableTargetIsFlagged`                            | Fact | Select threshold for target precision unreachable target is flagged.                                |
| `SelectThresholdForAlertBudget_UsesLargestQueueWithinBudget`                              | Fact | Select threshold for alert budget uses largest queue within budget.                                 |
| `SelectThresholdForAlertBudget_TiesOverBudgetAndInvalidArguments`                         | Fact | Select threshold for alert budget ties over budget and invalid arguments.                           |
| `EvaluateTopKAlertQueue_MatchesHandComputedQueues`                                        | Fact | Evaluate top k alert queue matches hand computed queues.                                            |
| `EvaluateTopKAlertQueue_RejectsOversizedQueuesAndHandlesNoEvents`                         | Fact | Evaluate top k alert queue rejects oversized queues and handles no events.                          |
| `CalculateOneVersusRestMulticlassAuc_PerfectAndReversedModels`                            | Fact | Calculate one versus rest multiclass auc perfect and reversed models.                               |
| `CalculateOneVersusRestMulticlassAuc_AbsentClassIsNaNAndUnknownLabelIsRejected`           | Fact | Calculate one versus rest multiclass auc absent class is na n and unknown label is rejected.        |
| `ComputeCalibrationCurve_EqualCountBinsMatchHandComputation`                              | Fact | Compute calibration curve equal count bins match hand computation.                                  |
| `ComputeCalibrationCurve_UniformBinsAgreeWithSharedExpectedCalibrationError`              | Fact | Compute calibration curve uniform bins agree with shared expected calibration error.                |
| `ComputeCalibrationCurve_RejectsProbabilitiesOutsideUnitInterval`                         | Fact | Compute calibration curve rejects probabilities outside unit interval.                              |
| `CalculateBrierScoreDecomposition_SatisfiesMurphyIdentity`                                | Fact | Calculate brier score decomposition satisfies murphy identity.                                      |
| `CalculateBrierScoreDecomposition_SingleBinReliabilityIsSquaredMeanGap`                   | Fact | Calculate brier score decomposition single bin reliability is squared mean gap.                     |
| `CalculateProbabilityScoringRules_MatchesHandComputedBrierAndLogLoss`                     | Fact | Calculate probability scoring rules matches hand computed brier and log loss.                       |
| `CalculateProbabilityScoringRules_ConfidentWrongPredictionIsClippedAndBadEpsilonRejected` | Fact | Calculate probability scoring rules confident wrong prediction is clipped and bad epsilon rejected. |
| `TestHosmerLemeshowCalibration_MatchesHandComputedStatistic`                              | Fact | Test hosmer lemeshow calibration matches hand computed statistic.                                   |
| `TestHosmerLemeshowCalibration_RejectsTooFewGroupsAndGrosslyMiscalibratedIsSignificant`   | Fact | Test hosmer lemeshow calibration rejects too few groups and grossly miscalibrated is significant.   |
| `FitCalibrationSlopeAndIntercept_PerfectlyCalibratedProbabilitiesGiveSlopeNearOne`        | Fact | Fit calibration slope and intercept perfectly calibrated probabilities give slope near one.         |
| `FitCalibrationSlopeAndIntercept_OverConfidentProbabilitiesHaveSmallSlope`                | Fact | Fit calibration slope and intercept over confident probabilities have small slope.                  |
| `CalculateMulticlassLogLoss_MatchesHandComputation`                                       | Fact | Calculate multiclass log loss matches hand computation.                                             |
| `CalculateMulticlassLogLoss_RejectsUnknownClassNegativeAndAllNaN`                         | Fact | Calculate multiclass log loss rejects unknown class negative and all na n.                          |
| `AdjustProbabilitiesForBaseRate_ShiftsLogOddsByTheBaseRateDifference`                     | Fact | Adjust probabilities for base rate shifts log odds by the base rate difference.                     |
| `AdjustProbabilitiesForBaseRate_RejectsInvalidRatesAndProbabilities`                      | Fact | Adjust probabilities for base rate rejects invalid rates and probabilities.                         |

### CategoricalAndSurvivalSurvivalToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.CategoricalAndSurvivalSurvivalToolsTests` — 38 tests.

| Test                                                                                          | Kind   | Verifies                                                                                                  |
|-----------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------|
| `EstimateKaplanMeierSurvival_MatchesFreireichSixMercaptopurineCurve`                          | Fact   | Estimate kaplan meier survival matches freireich six mercaptopurine curve.                                |
| `EstimateKaplanMeierSurvival_PlateauAtHalfTakesTheMidpointAndQuartilesFollow`                 | Fact   | Estimate kaplan meier survival plateau at half takes the midpoint and quartiles follow.                   |
| `EstimateKaplanMeierSurvival_HandlesCensoringOnlyNaNAndInvalidInput`                          | Fact   | Estimate kaplan meier survival handles censoring only na n and invalid input.                             |
| `EstimateKaplanMeierSurvival_ConfidenceMethodsOrderAsExpected`                                | Fact   | Estimate kaplan meier survival confidence methods order as expected.                                      |
| `EstimateKaplanMeierByGroup_MatchesFreireichMediansAndLogRank`                                | Fact   | Estimate kaplan meier by group matches freireich medians and log rank.                                    |
| `EstimateKaplanMeierByGroup_SingleGroupHasNoLogRankAndBlankGroupsAreDropped`                  | Fact   | Estimate kaplan meier by group single group has no log rank and blank groups are dropped.                 |
| `EstimateNelsonAalenCumulativeHazard_MatchesHarmonicSumsByHand`                               | Fact   | Estimate nelson aalen cumulative hazard matches harmonic sums by hand.                                    |
| `EstimateNelsonAalenCumulativeHazard_TiesAndCensoringAreCountedAtRisk`                        | Fact   | Estimate nelson aalen cumulative hazard ties and censoring are counted at risk.                           |
| `EvaluateSurvivalAtTimes_ReadsTheStepFunctionAtRequestedTimes`                                | Fact   | Evaluate survival at times reads the step function at requested times.                                    |
| `EvaluateSurvivalAtTimes_RequiresAtLeastOneValidTime`                                         | Fact   | Evaluate survival at times requires at least one valid time.                                              |
| `CalculateSurvivalTimeQuantiles_ReturnsMedianAndQuartilesWithBrookmeyerCrowleyLimits`         | Fact   | Calculate survival time quantiles returns median and quartiles with brookmeyer crowley limits.            |
| `CalculateSurvivalTimeQuantiles_UnreachableQuantileIsNaNAndBadProportionsRejected`            | Fact   | Calculate survival time quantiles unreachable quantile is na n and bad proportions rejected.              |
| `CalculateRestrictedMeanSurvivalTime_MatchesAreaUnderTheStepCurveByHand`                      | Fact   | Calculate restricted mean survival time matches area under the step curve by hand.                        |
| `CalculateRestrictedMeanSurvivalTime_HorizonBeyondFollowUpIsFlaggedAndInvalidHorizonRejected` | Fact   | Calculate restricted mean survival time horizon beyond follow up is flagged and invalid horizon rejected. |
| `CompareRestrictedMeanSurvivalTime_MatchesHandComputedDifferenceAndZ`                         | Fact   | Compare restricted mean survival time matches hand computed difference and z.                             |
| `CompareRestrictedMeanSurvivalTime_RequiresExactlyTwoGroups`                                  | Fact   | Compare restricted mean survival time requires exactly two groups.                                        |
| `EstimateMedianFollowUpTime_ReversesTheRolesOfEventsAndCensoring`                             | Fact   | Estimate median follow up time reverses the roles of events and censoring.                                |
| `EstimateMedianFollowUpTime_NoCensoringMeansNoReverseMedian`                                  | Fact   | Estimate median follow up time no censoring means no reverse median.                                      |
| `TestLogRankSurvivalDifference_MatchesFreireichObservedAndExpectedCounts`                     | Fact   | Test log rank survival difference matches freireich observed and expected counts.                         |
| `TestLogRankSurvivalDifference_WeightedStatisticsMatchHandComputedInterleavedSample`          | Theory | Test log rank survival difference weighted statistics match hand computed interleaved sample.             |
| `TestLogRankSurvivalDifference_FlemingHarringtonReducesToLogRankAndTargetsEarlyOrLate`        | Fact   | Test log rank survival difference fleming harrington reduces to log rank and targets early or late.       |
| `TestLogRankSurvivalDifference_ThreeIdenticalGroupsGiveZeroChiSquareOnTwoDegrees`             | Fact   | Test log rank survival difference three identical groups give zero chi square on two degrees.             |
| `FitCoxProportionalHazards_MatchesBreslowSolutionForFreireichLeukaemiaData`                   | Fact   | Fit cox proportional hazards matches breslow solution for freireich leukaemia data.                       |
| `FitCoxProportionalHazards_PredictionsFollowTheProportionalHazardsIdentity`                   | Fact   | Fit cox proportional hazards predictions follow the proportional hazards identity.                        |
| `FitCoxProportionalHazards_RejectsBadInputAndWarnsOnFewEvents`                                | Fact   | Fit cox proportional hazards rejects bad input and warns on few events.                                   |
| `FitCoxProportionalHazards_IsInvariantToTimeUnitsAndDropsIncompleteRows`                      | Fact   | Fit cox proportional hazards is invariant to time units and drops incomplete rows.                        |
| `CalculateHarrellConcordanceIndex_PerfectReversedAndConstantRankings`                         | Fact   | Calculate harrell concordance index perfect reversed and constant rankings.                               |
| `CalculateHarrellConcordanceIndex_CensoredShorterCasesAreNotComparable`                       | Fact   | Calculate harrell concordance index censored shorter cases are not comparable.                            |
| `FitExponentialSurvivalModel_MatchesClosedFormMaximumLikelihood`                              | Fact   | Fit exponential survival model matches closed form maximum likelihood.                                    |
| `FitExponentialSurvivalModel_NoEventsAndZeroTimeAreHandled`                                   | Fact   | Fit exponential survival model no events and zero time are handled.                                       |
| `CalculateIncidenceRatesByGroup_MatchesPersonTimeRatesAndExactRatio`                          | Fact   | Calculate incidence rates by group matches person time rates and exact ratio.                             |
| `CalculateIncidenceRatesByGroup_UnknownReferenceAndZeroPersonTime`                            | Fact   | Calculate incidence rates by group unknown reference and zero person time.                                |
| `TabulateHazardRateByInterval_MatchesHandComputedPersonTime`                                  | Fact   | Tabulate hazard rate by interval matches hand computed person time.                                       |
| `TabulateHazardRateByInterval_RejectsBadBreaks`                                               | Fact   | Tabulate hazard rate by interval rejects bad breaks.                                                      |
| `BuildActuarialLifeTable_MatchesHandComputedIntervals`                                        | Fact   | Build actuarial life table matches hand computed intervals.                                               |
| `BuildActuarialLifeTable_EmptyIntervalsCarrySurvivalForward`                                  | Fact   | Build actuarial life table empty intervals carry survival forward.                                        |
| `CalculateCumulativeIncidenceCompetingRisks_MatchesAalenJohansenByHand`                       | Fact   | Calculate cumulative incidence competing risks matches aalen johansen by hand.                            |
| `CalculateCumulativeIncidenceCompetingRisks_RejectsNegativeCodesAndNoEvents`                  | Fact   | Calculate cumulative incidence competing risks rejects negative codes and no events.                      |

### CategoricalAndSurvivalToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.CategoricalAndSurvivalToolsTests` — 58 tests.

| Test                                                                                                  | Kind   | Verifies                                                                                                           |
|-------------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------|
| `CrossTabulateCategories_CountsCellsAndSkipsBlankLabels`                                              | Fact   | Cross tabulate categories counts cells and skips blank labels.                                                     |
| `CrossTabulateCategories_RejectsMismatchedLengthsAndAllBlank`                                         | Fact   | Cross tabulate categories rejects mismatched lengths and all blank.                                                |
| `AnalyseContingencyTableAssociation_TwoByTwoMatchesHandComputation`                                   | Fact   | Analyse contingency table association two by two matches hand computation.                                         |
| `AnalyseContingencyTableAssociation_MatchesAgrestiPartyIdentificationTable`                           | Fact   | Analyse contingency table association matches agresti party identification table.                                  |
| `AnalyseContingencyTableAssociation_RejectsBadTablesAndWarnsOnSparseCells`                            | Fact   | Analyse contingency table association rejects bad tables and warns on sparse cells.                                |
| `AnalyseContingencyTableAssociation_HugeCountsRemainFinite`                                           | Fact   | Analyse contingency table association huge counts remain finite.                                                   |
| `AnalyseCategoricalAssociationFromLabels_CrossTabulatesThenAnalyses`                                  | Fact   | Analyse categorical association from labels cross tabulates then analyses.                                         |
| `AnalyseCategoricalAssociationFromLabels_ConstantVariableIsRejected`                                  | Fact   | Analyse categorical association from labels constant variable is rejected.                                         |
| `RankCategoricalColumnsByAssociationWithOutcome_RanksInformativeColumnAboveNoise`                     | Fact   | Rank categorical columns by association with outcome ranks informative column above noise.                         |
| `RankCategoricalColumnsByAssociationWithOutcome_RejectsRaggedRowsDuplicateNamesAndSingleClassOutcome` | Fact   | Rank categorical columns by association with outcome rejects ragged rows duplicate names and single class outcome. |
| `CalculateGoodmanKruskalLambda_MatchesHandComputation`                                                | Fact   | Calculate goodman kruskal lambda matches hand computation.                                                         |
| `CalculateGoodmanKruskalLambda_IndependenceIsZeroAndDegenerateIsNaN`                                  | Fact   | Calculate goodman kruskal lambda independence is zero and degenerate is na n.                                      |
| `CalculateOrdinalAssociationMeasures_MatchesAgrestiJobSatisfactionGamma`                              | Fact   | Calculate ordinal association measures matches agresti job satisfaction gamma.                                     |
| `CalculateOrdinalAssociationMeasures_TwoByTwoReducesToYuleQAndPhi`                                    | Fact   | Calculate ordinal association measures two by two reduces to yule q and phi.                                       |
| `CalculateOrdinalAssociationMeasures_ReversedOrderIsNegativeOne`                                      | Fact   | Calculate ordinal association measures reversed order is negative one.                                             |
| `CalculateCategoricalMutualInformation_PerfectDependenceIsOneBit`                                     | Fact   | Calculate categorical mutual information perfect dependence is one bit.                                            |
| `CalculateOddsRatio_MatchesWoolfIntervalByHand`                                                       | Fact   | Calculate odds ratio matches woolf interval by hand.                                                               |
| `CalculateOddsRatio_ZeroCellUsesHaldaneCorrectionOnlyWhenAsked`                                       | Fact   | Calculate odds ratio zero cell uses haldane correction only when asked.                                            |
| `CalculateOddsRatio_RejectsBadInput`                                                                  | Fact   | Calculate odds ratio rejects bad input.                                                                            |
| `CalculateRiskRatio_MatchesPhysiciansHealthStudyAspirinTrial`                                         | Fact   | Calculate risk ratio matches physicians health study aspirin trial.                                                |
| `CalculateRiskRatio_ZeroEventGroupIsCorrectedAndFlagged`                                              | Fact   | Calculate risk ratio zero event group is corrected and flagged.                                                    |
| `CalculateRiskDifference_MatchesNewcombe1998ScoreIntervalExample`                                     | Fact   | Calculate risk difference matches newcombe1998 score interval example.                                             |
| `CalculateRiskDifference_IdenticalRatesGiveZeroDifference`                                            | Fact   | Calculate risk difference identical rates give zero difference.                                                    |
| `CalculateNumberNeededToTreat_InvertsTheRiskDifference`                                               | Fact   | Calculate number needed to treat inverts the risk difference.                                                      |
| `CalculateNumberNeededToTreat_NoDifferenceHasNoFiniteNumber`                                          | Fact   | Calculate number needed to treat no difference has no finite number.                                               |
| `CalculateYuleAssociation_MatchesClosedForms`                                                         | Fact   | Calculate yule association matches closed forms.                                                                   |
| `CalculateYuleAssociation_EmptyOffDiagonalGivesPerfectAssociationAndNaNStandardError`                 | Fact   | Calculate yule association empty off diagonal gives perfect association and na n standard error.                   |
| `CalculateDiagnosticLikelihoodRatios_MatchesBayesByHand`                                              | Fact   | Calculate diagnostic likelihood ratios matches bayes by hand.                                                      |
| `CalculateDiagnosticLikelihoodRatios_DefaultsPretestToPrevalenceAndRejectsBadPretest`                 | Fact   | Calculate diagnostic likelihood ratios defaults pretest to prevalence and rejects bad pretest.                     |
| `AnalyseTwoByTwoTable_CombinesEveryMeasure`                                                           | Fact   | Analyse two by two table combines every measure.                                                                   |
| `AnalyseTwoByTwoTable_RejectsEmptyEventColumn`                                                        | Fact   | Analyse two by two table rejects empty event column.                                                               |
| `AnalyseBinaryExposureAndOutcome_BuildsTheTableFromFlags`                                             | Fact   | Analyse binary exposure and outcome builds the table from flags.                                                   |
| `AnalyseBinaryExposureAndOutcome_RejectsNonBinaryAndMismatched`                                       | Fact   | Analyse binary exposure and outcome rejects non binary and mismatched.                                             |
| `CalculateMantelHaenszelOddsRatio_SingleStratumMatchesHandComputation`                                | Fact   | Calculate mantel haenszel odds ratio single stratum matches hand computation.                                      |
| `CalculateMantelHaenszelOddsRatio_IdenticalStrataAreHomogeneousAndHeterogeneousOnesAreNot`            | Fact   | Calculate mantel haenszel odds ratio identical strata are homogeneous and heterogeneous ones are not.              |
| `CalculateMantelHaenszelOddsRatio_RejectsMalformedStrata`                                             | Fact   | Calculate mantel haenszel odds ratio rejects malformed strata.                                                     |
| `CalculateBinomialProportionConfidenceInterval_MatchesPublishedEightOfTenIntervals`                   | Theory | Calculate binomial proportion confidence interval matches published eight of ten intervals.                        |
| `CalculateBinomialProportionConfidenceInterval_WaldIsClippedAndEdgesAreHandled`                       | Fact   | Calculate binomial proportion confidence interval wald is clipped and edges are handled.                           |
| `CalculateInformationValueByCategory_MatchesClosedFormWeightOfEvidence`                               | Fact   | Calculate information value by category matches closed form weight of evidence.                                    |
| `CalculateInformationValueByCategory_ZeroCellIsSmoothedAndBlankIsSkipped`                             | Fact   | Calculate information value by category zero cell is smoothed and blank is skipped.                                |
| `CalculateInformationValueByBins_SeparatedHalvesMatchSmoothedClosedForm`                              | Fact   | Calculate information value by bins separated halves match smoothed closed form.                                   |
| `CalculateInformationValueByBins_MissingValuesFormTheirOwnBin`                                        | Fact   | Calculate information value by bins missing values form their own bin.                                             |
| `CalculatePopulationStabilityIndexFromCounts_MatchesHandComputation`                                  | Fact   | Calculate population stability index from counts matches hand computation.                                         |
| `CalculatePopulationStabilityIndexFromCounts_RejectsBadInput`                                         | Fact   | Calculate population stability index from counts rejects bad input.                                                |
| `CalculatePopulationStabilityIndexForNumericValues_IdenticalSamplesAreStableAndShiftedOnesAreNot`     | Fact   | Calculate population stability index for numeric values identical samples are stable and shifted ones are not.     |
| `CalculatePopulationStabilityIndexForNumericValues_MissingBinAndEmptyBaseline`                        | Fact   | Calculate population stability index for numeric values missing bin and empty baseline.                            |
| `CalculatePopulationStabilityIndexForCategories_MatchesCountsVersion`                                 | Fact   | Calculate population stability index for categories matches counts version.                                        |
| `CalculatePopulationStabilityIndexForCategories_NewCategoryAppearsAsBin`                              | Fact   | Calculate population stability index for categories new category appears as bin.                                   |
| `TabulateEventRateBySegment_MatchesHandComputedRatesLiftAndHomogeneity`                               | Fact   | Tabulate event rate by segment matches hand computed rates lift and homogeneity.                                   |
| `TabulateEventRateBySegment_FlagsSmallSamplesAndHandlesNoEvents`                                      | Fact   | Tabulate event rate by segment flags small samples and handles no events.                                          |
| `TabulateAlertHitRatesBySegment_ComputesAlertHitAndDetectionRates`                                    | Fact   | Tabulate alert hit rates by segment computes alert hit and detection rates.                                        |
| `TabulateAlertHitRatesBySegment_NoAlertsGiveZeroHitRateAndRejectsBadFlags`                            | Fact   | Tabulate alert hit rates by segment no alerts give zero hit rate and rejects bad flags.                            |
| `CalculatePoissonRateConfidenceInterval_MatchesExactGarwoodLimits`                                    | Fact   | Calculate poisson rate confidence interval matches exact garwood limits.                                           |
| `CalculatePoissonRateConfidenceInterval_RejectsBadInput`                                              | Fact   | Calculate poisson rate confidence interval rejects bad input.                                                      |
| `ComparePoissonRates_MatchesExactBinomialByHand`                                                      | Fact   | Compare poisson rates matches exact binomial by hand.                                                              |
| `ComparePoissonRates_ZeroEventGroupsAreHandled`                                                       | Fact   | Compare poisson rates zero event groups are handled.                                                               |
| `TestPoissonDispersion_MatchesChiSquareClosedFormForTwoDegreesOfFreedom`                              | Fact   | Test poisson dispersion matches chi square closed form for two degrees of freedom.                                 |
| `TestPoissonDispersion_BurstyCountsAreOverdispersedAndZerosAreUntestable`                             | Fact   | Test poisson dispersion bursty counts are overdispersed and zeros are untestable.                                  |

### DataPreparationAndResamplingToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.DataPreparationAndResamplingToolsTests` — 134 tests.

| Test                                                                              | Kind | Verifies                                                                                      |
|-----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `FitLabelEncoderSortedCodesAndMissingIsMinusOne`                                  | Fact | Fit label encoder sorted codes and missing is minus one.                                      |
| `FitLabelEncoderFirstAppearanceAndFrequencyOrders`                                | Fact | Fit label encoder first appearance and frequency orders.                                      |
| `ApplyLabelEncoderUsesGivenRankOrderAndUnknownCode`                               | Fact | Apply label encoder uses given rank order and unknown code.                                   |
| `FitOneHotEncoderProducesOneColumnPerCategory`                                    | Fact | Fit one hot encoder produces one column per category.                                         |
| `FitOneHotEncoderRefusesExplosiveCardinality`                                     | Fact | Fit one hot encoder refuses explosive cardinality.                                            |
| `ApplyOneHotEncoderUnseenCategoryIsAllZeros`                                      | Fact | Apply one hot encoder unseen category is all zeros.                                           |
| `FitTargetMeanEncoderShrinksTowardsTheOverallRate`                                | Fact | Fit target mean encoder shrinks towards the overall rate.                                     |
| `FitTargetMeanEncoderRejectsBadLabelsAndNegativeSmoothing`                        | Fact | Fit target mean encoder rejects bad labels and negative smoothing.                            |
| `FitWeightOfEvidenceIsLogRatioOfEventToNonEventShares`                            | Fact | Fit weight of evidence is log ratio of event to non event shares.                             |
| `FitWeightOfEvidenceSmoothingKeepsEmptyCellsFiniteAndSingleClassIsRejected`       | Fact | Fit weight of evidence smoothing keeps empty cells finite and single class is rejected.       |
| `FitFrequencyEncoderReturnsFractionsOrCounts`                                     | Fact | Fit frequency encoder returns fractions or counts.                                            |
| `ApplyCategoryEncoderMapsKnownAndFallsBackToDefault`                              | Fact | Apply category encoder maps known and falls back to default.                                  |
| `FitRareCategoryGroupingReplacesRareLevelsWithOther`                              | Fact | Fit rare category grouping replaces rare levels with other.                                   |
| `ApplyRareCategoryGroupingMapsUnseenToOther`                                      | Fact | Apply rare category grouping maps unseen to other.                                            |
| `DiscretiseEqualWidthCutsRangeIntoEqualIntervals`                                 | Fact | Discretise equal width cuts range into equal intervals.                                       |
| `DiscretiseEqualWidthWithLabelsAddsEventStatisticsAndHandlesMissingAndConstant`   | Fact | Discretise equal width with labels adds event statistics and handles missing and constant.    |
| `DiscretiseEqualFrequencyCutsAtQuantilesGivingEqualCounts`                        | Fact | Discretise equal frequency cuts at quantiles giving equal counts.                             |
| `DiscretiseEqualFrequencyMergesTiedCutPoints`                                     | Fact | Discretise equal frequency merges tied cut points.                                            |
| `DiscretiseKMeansSeparatesThreeClumps`                                            | Fact | Discretise k means separates three clumps.                                                    |
| `DiscretiseKMeansWithFewDistinctValuesReducesBins`                                | Fact | Discretise k means with few distinct values reduces bins.                                     |
| `DiscretiseSupervisedFindsTheCutWhereTheEventRateJumps`                           | Fact | Discretise supervised finds the cut where the event rate jumps.                               |
| `DiscretiseSupervisedRejectsSingleClassAndTinyBinCount`                           | Fact | Discretise supervised rejects single class and tiny bin count.                                |
| `DiscretiseCustomEdgesPutsAnEdgeValueInTheLowerBin`                               | Fact | Discretise custom edges puts an edge value in the lower bin.                                  |
| `ApplyBinEdgesAssignsBeyondRangeAndMissing`                                       | Fact | Apply bin edges assigns beyond range and missing.                                             |
| `GeneratePolynomialFeaturesDegreeTwoHasPowersAndProduct`                          | Fact | Generate polynomial features degree two has powers and product.                               |
| `GeneratePolynomialFeaturesDegreeThreeCountsAndLimit`                             | Fact | Generate polynomial features degree three counts and limit.                                   |
| `GenerateColumnPairFeaturesRatioDifferenceAndUndefinedCounts`                     | Fact | Generate column pair features ratio difference and undefined counts.                          |
| `GenerateLagFeaturesShiftsAndDifferences`                                         | Fact | Generate lag features shifts and differences.                                                 |
| `GenerateLagFeaturesWithinGroupsAndRollingWindowExcludesCurrentRow`               | Fact | Generate lag features within groups and rolling window excludes current row.                  |
| `ExtractDateTimePartsForAKnownDate`                                               | Fact | Extract date time parts for a known date.                                                     |
| `ExtractDateTimePartsOffsetWeekendMonthEndAndUnparsed`                            | Fact | Extract date time parts offset weekend month end and unparsed.                                |
| `ExtractEpochPartsSecondsMillisecondsAndInvalid`                                  | Fact | Extract epoch parts seconds milliseconds and invalid.                                         |
| `ComputeElapsedTimeInDifferentUnits`                                              | Fact | Compute elapsed time in different units.                                                      |
| `EncodeCyclicalFeatureMakesTheEndsOfTheCycleNeighbours`                           | Fact | Encode cyclical feature makes the ends of the cycle neighbours.                               |
| `SelectTableColumnsKeepsAndReordersByName`                                        | Fact | Select table columns keeps and reorders by name.                                              |
| `SelectByVarianceThresholdDropsConstantAndLowVarianceColumns`                     | Fact | Select by variance threshold drops constant and low variance columns.                         |
| `FilterCorrelatedFeaturesDropsOneOfAPerfectlyCorrelatedPair`                      | Fact | Filter correlated features drops one of a perfectly correlated pair.                          |
| `FilterCorrelatedFeaturesKeepsTheMemberMoreCorrelatedWithTheTarget`               | Fact | Filter correlated features keeps the member more correlated with the target.                  |
| `RankFeaturesByAnovaFMatchesHandComputedTwoClassAnova`                            | Fact | Rank features by anova f matches hand computed two class anova.                               |
| `RankFeaturesByAnovaFRejectsSingleClassAndRanksUnusableFeatureLast`               | Fact | Rank features by anova f rejects single class and ranks unusable feature last.                |
| `RankFeaturesByFRegressionMatchesTextbookSimpleRegression`                        | Fact | Rank features by f regression matches textbook simple regression.                             |
| `RankFeaturesByFRegressionConstantFeatureIsNaN`                                   | Fact | Rank features by f regression constant feature is na n.                                       |
| `RankCountFeaturesByChiSquareMatchesHandComputedStatistic`                        | Fact | Rank count features by chi square matches hand computed statistic.                            |
| `RankCountFeaturesByChiSquareRejectsNegativeValuesAndZeroColumns`                 | Fact | Rank count features by chi square rejects negative values and zero columns.                   |
| `RankCategoricalFeaturesByChiSquareMatchesTwoByTwoHandComputation`                | Fact | Rank categorical features by chi square matches two by two hand computation.                  |
| `RankCategoricalFeaturesByChiSquareRejectsSingleClass`                            | Fact | Rank categorical features by chi square rejects single class.                                 |
| `RankFeaturesByMutualInformationPerfectSeparatorIsLnTwo`                          | Fact | Rank features by mutual information perfect separator is ln two.                              |
| `RankFeaturesByMutualInformationCapturesInformativeMissingness`                   | Fact | Rank features by mutual information captures informative missingness.                         |
| `EliminateFeaturesRecursivelyKeepsTheOnlyRealPredictor`                           | Fact | Eliminate features recursively keeps the only real predictor.                                 |
| `EliminateFeaturesRecursivelyValidatesArguments`                                  | Fact | Eliminate features recursively validates arguments.                                           |
| `ProfileMissingValuesCountsCellsRowsAndPatterns`                                  | Fact | Profile missing values counts cells rows and patterns.                                        |
| `ProfileMissingValuesWarnsAboutEntirelyMissingColumnAndRejectsBadInput`           | Fact | Profile missing values warns about entirely missing column and rejects bad input.             |
| `ImputeMissingValuesMedianReplacesGapsAndReportsThem`                             | Fact | Impute missing values median replaces gaps and reports them.                                  |
| `ImputeMissingValuesMeanModeConstantAndIndicators`                                | Fact | Impute missing values mean mode constant and indicators.                                      |
| `ImputeMissingValuesRejectsAllMissingColumnUnlessConstant`                        | Fact | Impute missing values rejects all missing column unless constant.                             |
| `ApplyImputerUsesFixedFillValuesAndIndicators`                                    | Fact | Apply imputer uses fixed fill values and indicators.                                          |
| `ApplyImputerRejectsWrongNumberOfFillValues`                                      | Fact | Apply imputer rejects wrong number of fill values.                                            |
| `ImputeForwardFillCarriesLastValueAndOptionallyBackfillsLeadingGaps`              | Fact | Impute forward fill carries last value and optionally backfills leading gaps.                 |
| `ImputeLinearInterpolationFillsInteriorGapsOnALine`                               | Fact | Impute linear interpolation fills interior gaps on a line.                                    |
| `ImputeNearestNeighboursCopiesTheNearestRowsValue`                                | Fact | Impute nearest neighbours copies the nearest rows value.                                      |
| `ImputeNearestNeighboursFallsBackToColumnMeanWithoutOverlapAndRejectsEmptyColumn` | Fact | Impute nearest neighbours falls back to column mean without overlap and rejects empty column. |
| `DropSparseRowsAndColumnsRemovesEmptyColumnThenIncompleteRows`                    | Fact | Drop sparse rows and columns removes empty column then incomplete rows.                       |
| `DropSparseRowsAndColumnsRejectsBadFractions`                                     | Fact | Drop sparse rows and columns rejects bad fractions.                                           |
| `FitTableScalerStandardIsZScoreWithSampleStandardDeviation`                       | Fact | Fit table scaler standard is z score with sample standard deviation.                          |
| `FitTableScalerMinMaxRobustAndMaxAbsMatchDefinitions`                             | Fact | Fit table scaler min max robust and max abs match definitions.                                |
| `FitTableScalerHandlesConstantMissingAndEmptyColumns`                             | Fact | Fit table scaler handles constant missing and empty columns.                                  |
| `ApplyTableScalerUsesFixedCentersAndScales`                                       | Fact | Apply table scaler uses fixed centers and scales.                                             |
| `InvertTableScalerRoundTripsAFittedScaler`                                        | Fact | Invert table scaler round trips a fitted scaler.                                              |
| `NormaliseRowsToUnitNormMatchesNormDefinitions`                                   | Fact | Normalise rows to unit norm matches norm definitions.                                         |
| `FitQuantileTransformerUniformMapsRanksToUnitInterval`                            | Fact | Fit quantile transformer uniform maps ranks to unit interval.                                 |
| `FitQuantileTransformerTiesShareTheMidRank`                                       | Fact | Fit quantile transformer ties share the mid rank.                                             |
| `ApplyQuantileTransformerInterpolatesAndClipsToTheEnds`                           | Fact | Apply quantile transformer interpolates and clips to the ends.                                |
| `ApplyPowerTransformerMatchesBoxCoxAndYeoJohnsonFormulas`                         | Fact | Apply power transformer matches box cox and yeo johnson formulas.                             |
| `FitPowerTransformerYeoJohnsonOfSymmetricDataHasLambdaOne`                        | Fact | Fit power transformer yeo johnson of symmetric data has lambda one.                           |
| `FitPowerTransformerBoxCoxOfLogSymmetricDataIsNearLogarithm`                      | Fact | Fit power transformer box cox of log symmetric data is near logarithm.                        |
| `FitPowerTransformerRejectsNonPositiveBoxCoxTooFewValuesAndBadRange`              | Fact | Fit power transformer rejects non positive box cox too few values and bad range.              |
| `ApplyColumnFunctionMatchesClosedFormsAndCountsUndefinedCells`                    | Fact | Apply column function matches closed forms and counts undefined cells.                        |
| `FitOutlierClipperIqrFencesClipTheExtremeValue`                                   | Fact | Fit outlier clipper iqr fences clip the extreme value.                                        |
| `FitOutlierClipperQuantileMadAndStandardDeviationBounds`                          | Fact | Fit outlier clipper quantile mad and standard deviation bounds.                               |
| `FitOutlierClipperRejectsBadQuantilesAndEmptyColumns`                             | Fact | Fit outlier clipper rejects bad quantiles and empty columns.                                  |
| `ApplyOutlierClipperCapsAtFixedBoundsAndKeepsMissing`                             | Fact | Apply outlier clipper caps at fixed bounds and keeps missing.                                 |
| `DescribeDatasetShapeInfersColumnTypes`                                           | Fact | Describe dataset shape infers column types.                                                   |
| `DescribeDatasetShapeRejectsRaggedTables`                                         | Fact | Describe dataset shape rejects ragged tables.                                                 |
| `DescribeCategoricalTableProfilesCardinalityAndEntropy`                           | Fact | Describe categorical table profiles cardinality and entropy.                                  |
| `FindDuplicateRowsGroupsRepeatsAndTreatsNaNAsEqual`                               | Fact | Find duplicate rows groups repeats and treats na n as equal.                                  |
| `FindDuplicateCategoricalRowsTreatsNullAndEmptyAlike`                             | Fact | Find duplicate categorical rows treats null and empty alike.                                  |
| `CheckTargetBalanceReportsRatioAndEntropy`                                        | Fact | Check target balance reports ratio and entropy.                                               |
| `CheckTargetBalanceWarnsOnSingleAndVeryRareClasses`                               | Fact | Check target balance warns on single and very rare classes.                                   |
| `DataQualityReportFlagsConstantLeakageMissingnessAndDuplicates`                   | Fact | Data quality report flags constant leakage missingness and duplicates.                        |
| `DataQualityReportFindsPerfectSeparatorAndIdentifierAndValidatesTargets`          | Fact | Data quality report finds perfect separator and identifier and validates targets.             |
| `DataQualityReportOnCleanDataHasNoErrorsAndUsesNumericTarget`                     | Fact | Data quality report on clean data has no errors and uses numeric target.                      |
| `DataQualityReportCategoricalFlagsFormattingConstantLeakageAndDuplicates`         | Fact | Data quality report categorical flags formatting constant leakage and duplicates.             |
| `DataQualityReportCategoricalFlagsHighCardinalityMissingAndRareCategories`        | Fact | Data quality report categorical flags high cardinality missing and rare categories.           |
| `SampleSimpleRandomWithoutReplacementIsUniqueSortedAndReproducible`               | Fact | Sample simple random without replacement is unique sorted and reproducible.                   |
| `SampleSimpleRandomWithReplacementCanExceedPopulationAndRejectsImpossibleDraws`   | Fact | Sample simple random with replacement can exceed population and rejects impossible draws.     |
| `SampleStratifiedProportionalAndEqualAllocation`                                  | Fact | Sample stratified proportional and equal allocation.                                          |
| `SampleSystematicHasConstantSpacing`                                              | Fact | Sample systematic has constant spacing.                                                       |
| `SampleReservoirKeepsKUniqueRowsAndIsRoughlyUniform`                              | Fact | Sample reservoir keeps k unique rows and is roughly uniform.                                  |
| `SampleWeightedNeverPicksZeroWeightAndFavoursHeavyRows`                           | Fact | Sample weighted never picks zero weight and favours heavy rows.                               |
| `SampleWeightedRejectsBadWeightsAndImpossibleDraws`                               | Fact | Sample weighted rejects bad weights and impossible draws.                                     |
| `SampleClustersReturnsWholeGroups`                                                | Fact | Sample clusters returns whole groups.                                                         |
| `SplitTrainValidationTestPartitionsRowsInTheRequestedShares`                      | Fact | Split train validation test partitions rows in the requested shares.                          |
| `SplitTrainValidationTestStratifiesAndValidatesFractions`                         | Fact | Split train validation test stratifies and validates fractions.                               |
| `SplitByGroupKeepsEveryGroupInOneSet`                                             | Fact | Split by group keeps every group in one set.                                                  |
| `SplitTimeOrderedPutsTheFutureInTest`                                             | Fact | Split time ordered puts the future in test.                                                   |
| `SplitTimeOrderedPurgeGapAndValidation`                                           | Fact | Split time ordered purge gap and validation.                                                  |
| `GenerateKFoldsTestSetsPartitionTheRows`                                          | Fact | Generate k folds test sets partition the rows.                                                |
| `GenerateStratifiedKFoldsKeepsClassProportions`                                   | Fact | Generate stratified k folds keeps class proportions.                                          |
| `GenerateGroupKFoldsNeverSplitsAGroup`                                            | Fact | Generate group k folds never splits a group.                                                  |
| `GenerateTimeSeriesFoldsExpandTheTrainingWindow`                                  | Fact | Generate time series folds expand the training window.                                        |
| `GenerateLeaveOneOutFoldsTestsOneRowEach`                                         | Fact | Generate leave one out folds tests one row each.                                              |
| `GenerateRepeatedKFoldsGivesRepeatsTimesFoldsWithDifferentShuffles`               | Fact | Generate repeated k folds gives repeats times folds with different shuffles.                  |
| `GenerateBootstrapSamplesLeaveAboutAThirdOutOfBag`                                | Fact | Generate bootstrap samples leave about a third out of bag.                                    |
| `GeneratePermutationIsAShuffleOfAllIndices`                                       | Fact | Generate permutation is a shuffle of all indices.                                             |
| `ComputeClassWeightsBalancedIsNOverClassesTimesCount`                             | Fact | Compute class weights balanced is n over classes times count.                                 |
| `ComputeClassWeightsOtherMethodsAreRescaledToMeanOne`                             | Fact | Compute class weights other methods are rescaled to mean one.                                 |
| `RandomUnderSampleKeepsTheMinorityAndCutsTheMajority`                             | Fact | Random under sample keeps the minority and cuts the majority.                                 |
| `RandomOverSampleKeepsEveryOriginalRowAndRepeatsMinorityRows`                     | Fact | Random over sample keeps every original row and repeats minority rows.                        |
| `GenerateSmoteSamplesInterpolatesBetweenMinorityNeighbours`                       | Fact | Generate smote samples interpolates between minority neighbours.                              |
| `GenerateSmoteSamplesRejectsMissingValuesAndTinyMinority`                         | Fact | Generate smote samples rejects missing values and tiny minority.                              |
| `BootstrapStatisticIntervalOfTheMeanMatchesTheClassicalStandardError`             | Fact | Bootstrap statistic interval of the mean matches the classical standard error.                |
| `BootstrapStatisticIntervalAllMethodsBracketTheEstimate`                          | Fact | Bootstrap statistic interval all methods bracket the estimate.                                |
| `BootstrapStatisticIntervalStatisticsConstantsAndValidation`                      | Fact | Bootstrap statistic interval statistics constants and validation.                             |
| `BootstrapTwoSampleDifferenceExcludesZeroForSeparatedSamples`                     | Fact | Bootstrap two sample difference excludes zero for separated samples.                          |
| `BootstrapCorrelationIntervalOfAPerfectLineIsDegenerate`                          | Fact | Bootstrap correlation interval of a perfect line is degenerate.                               |
| `BootstrapCorrelationIntervalMatchesTextbookCorrelationAndBracketsIt`             | Fact | Bootstrap correlation interval matches textbook correlation and brackets it.                  |
| `JackknifeOfTheMeanHasZeroBiasAndTextbookStandardError`                           | Fact | Jackknife of the mean has zero bias and textbook standard error.                              |
| `JackknifeOfTheUnbiasedVarianceHasZeroBiasAndValidatesInput`                      | Fact | Jackknife of the unbiased variance has zero bias and validates input.                         |
| `PermutationTestTwoSampleExactMatchesHandEnumeration`                             | Fact | Permutation test two sample exact matches hand enumeration.                                   |
| `PermutationTestTwoSampleMonteCarloPValueIsBoundedBelowAndOtherStatisticsRun`     | Fact | Permutation test two sample monte carlo p value is bounded below and other statistics run.    |
| `PermutationTestCorrelationExactForPerfectAssociation`                            | Fact | Permutation test correlation exact for perfect association.                                   |
| `PermutationTestCorrelationMonteCarloAndValidation`                               | Fact | Permutation test correlation monte carlo and validation.                                      |
| `PermutationTestPairedExactSignFlipsMatchHandCount`                               | Fact | Permutation test paired exact sign flips match hand count.                                    |
| `PermutationTestPairedMonteCarloAndValidation`                                    | Fact | Permutation test paired monte carlo and validation.                                           |

### DescriptiveStatisticsToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.DescriptiveStatisticsToolsTests` — 100 tests.

| Test                                                       | Kind | Verifies                                                            |
|------------------------------------------------------------|------|---------------------------------------------------------------------|
| `MeanOfTextbookExampleIsFive`                              | Fact | Mean of textbook example is five.                                   |
| `MeanRejectsNaNUnlessIgnoredAndHandlesEmptyAndHugeValues`  | Fact | Mean rejects na n unless ignored and handles empty and huge values. |
| `WeightedMeanMatchesHandComputation`                       | Fact | Weighted mean matches hand computation.                             |
| `TrimmedMeanDropsOneValuePerTail`                          | Fact | Trimmed mean drops one value per tail.                              |
| `WinsorisedMeanReplacesTailsWithNeighbours`                | Fact | Winsorised mean replaces tails with neighbours.                     |
| `GeometricMeanIsRootOfProduct`                             | Fact | Geometric mean is root of product.                                  |
| `HarmonicMeanIsCountOverSumOfReciprocals`                  | Fact | Harmonic mean is count over sum of reciprocals.                     |
| `QuadraticMeanIsRootMeanSquare`                            | Fact | Quadratic mean is root mean square.                                 |
| `PowerMeanRecoversSpecialCases`                            | Fact | Power mean recovers special cases.                                  |
| `ContraharmonicMeanIsSumOfSquaresOverSum`                  | Fact | Contraharmonic mean is sum of squares over sum.                     |
| `ExponentialWeightedMeanWithFullAlphaIsLastValue`          | Fact | Exponential weighted mean with full alpha is last value.            |
| `GrandMeanWeightsByGroupSize`                              | Fact | Grand mean weights by group size.                                   |
| `MedianAveragesTheMiddlePairForEvenCounts`                 | Fact | Median averages the middle pair for even counts.                    |
| `TrimeanCombinesQuartilesAndMedian`                        | Fact | Trimean combines quartiles and median.                              |
| `MidhingeAveragesQuartiles`                                | Fact | Midhinge averages quartiles.                                        |
| `MinimumMaximumRangeReportsExtent`                         | Fact | Minimum maximum range reports extent.                               |
| `MinimumFindsSmallestAndSkipsMissingWhenAsked`             | Fact | Minimum finds smallest and skips missing when asked.                |
| `MaximumFindsLargest`                                      | Fact | Maximum finds largest.                                              |
| `ArgumentOfMinimumReturnsFirstIndexInOriginalArray`        | Fact | Argument of minimum returns first index in original array.          |
| `ArgumentOfMaximumReturnsFirstIndexInOriginalArray`        | Fact | Argument of maximum returns first index in original array.          |
| `SumAddsValues`                                            | Fact | Sum adds values.                                                    |
| `ProductMultipliesValues`                                  | Fact | Product multiplies values.                                          |
| `CountValuesProfilesDataQuality`                           | Fact | Count values profiles data quality.                                 |
| `ModeOfNumbersReturnsEveryTiedMode`                        | Fact | Mode of numbers returns every tied mode.                            |
| `ModeOfCategoriesReturnsTiedLabels`                        | Fact | Mode of categories returns tied labels.                             |
| `WeightedModeUsesTotalWeight`                              | Fact | Weighted mode uses total weight.                                    |
| `CircularMeanOfHoursAroundMidnightIsMidnight`              | Fact | Circular mean of hours around midnight is midnight.                 |
| `CircularSummaryResultantIsCosineOfHalfSpread`             | Fact | Circular summary resultant is cosine of half spread.                |
| `QuantileFollowsHyndmanFanDefinitions`                     | Fact | Quantile follows hyndman fan definitions.                           |
| `QuantilesReturnOnePerProbabilityInOrder`                  | Fact | Quantiles return one per probability in order.                      |
| `PercentilesUsePercentLevels`                              | Fact | Percentiles use percent levels.                                     |
| `CutPointsDivideIntoEqualGroups`                           | Fact | Cut points divide into equal groups.                                |
| `InterpercentileRangeIsDifferenceOfPercentiles`            | Fact | Interpercentile range is difference of percentiles.                 |
| `FiveNumberSummaryProvidesBoxPlotFences`                   | Fact | Five number summary provides box plot fences.                       |
| `PercentileRankOfValueHandlesTies`                         | Fact | Percentile rank of value handles ties.                              |
| `PercentileRanksOfValuesKeepPositionsAndMissing`           | Fact | Percentile ranks of values keep positions and missing.              |
| `RanksHandleEveryTieMethod`                                | Fact | Ranks handle every tie method.                                      |
| `ThresholdShareCountsBothSides`                            | Fact | Threshold share counts both sides.                                  |
| `CumulativeSumSkipsMissingButKeepsPositions`               | Fact | Cumulative sum skips missing but keeps positions.                   |
| `CumulativeProductCompoundsFactors`                        | Fact | Cumulative product compounds factors.                               |
| `CumulativeMaximumIsHighWaterMark`                         | Fact | Cumulative maximum is high water mark.                              |
| `CumulativeMinimumIsLowWaterMark`                          | Fact | Cumulative minimum is low water mark.                               |
| `RollingStatisticComputesTrailingWindows`                  | Fact | Rolling statistic computes trailing windows.                        |
| `ExpandingStatisticUsesEverythingSoFar`                    | Fact | Expanding statistic uses everything so far.                         |
| `ExponentialMovingAverageFollowsRecursion`                 | Fact | Exponential moving average follows recursion.                       |
| `FrequencyTableOfCategoriesCountsAndAccumulates`           | Fact | Frequency table of categories counts and accumulates.               |
| `FrequencyTableOfIntegersOrdersNumerically`                | Fact | Frequency table of integers orders numerically.                     |
| `FrequencyTableOfNumbersFindsRepeatedAmounts`              | Fact | Frequency table of numbers finds repeated amounts.                  |
| `RareCategoriesListsSingletons`                            | Fact | Rare categories lists singletons.                                   |
| `VarianceMatchesPopulationAndSampleForms`                  | Fact | Variance matches population and sample forms.                       |
| `StandardDeviationMatchesWikipediaExample`                 | Fact | Standard deviation matches wikipedia example.                       |
| `StandardErrorIsSdOverRootN`                               | Fact | Standard error is sd over root n.                                   |
| `CoefficientOfVariationIsSdOverMean`                       | Fact | Coefficient of variation is sd over mean.                           |
| `VarianceToMeanRatioFlagsOverdispersion`                   | Fact | Variance to mean ratio flags overdispersion.                        |
| `MeanAbsoluteDeviationAroundMeanAndMedian`                 | Fact | Mean absolute deviation around mean and median.                     |
| `MedianAbsoluteDeviationIsRobustToOutlier`                 | Fact | Median absolute deviation is robust to outlier.                     |
| `InterquartileRangeUsesChosenInterpolation`                | Fact | Interquartile range uses chosen interpolation.                      |
| `QuartileCoefficientOfDispersionIsQuartileRatio`           | Fact | Quartile coefficient of dispersion is quartile ratio.               |
| `SumOfSquaredDeviationsIsThirtyTwo`                        | Fact | Sum of squared deviations is thirty two.                            |
| `WeightedVarianceFrequencyAndReliabilityCorrections`       | Fact | Weighted variance frequency and reliability corrections.            |
| `WeightedStandardDeviationIsRootOfWeightedVariance`        | Fact | Weighted standard deviation is root of weighted variance.           |
| `ExponentialWeightedVarianceOfConstantSeriesIsZero`        | Fact | Exponential weighted variance of constant series is zero.           |
| `PooledVarianceWeightsByDegreesOfFreedom`                  | Fact | Pooled variance weights by degrees of freedom.                      |
| `PooledStandardDeviationIsRootOfPooledVariance`            | Fact | Pooled standard deviation is root of pooled variance.               |
| `PooledVarianceFromSummariesMatchesRawData`                | Fact | Pooled variance from summaries matches raw data.                    |
| `SkewnessMatchesMomentCoefficient`                         | Fact | Skewness matches moment coefficient.                                |
| `ExcessKurtosisMatchesMomentCoefficient`                   | Fact | Excess kurtosis matches moment coefficient.                         |
| `KurtosisIsExcessPlusThree`                                | Fact | Kurtosis is excess plus three.                                      |
| `ShapeStatisticsCombinesAllThree`                          | Fact | Shape statistics combines all three.                                |
| `RawMomentsMatchHandComputation`                           | Fact | Raw moments match hand computation.                                 |
| `CentralMomentsMatchHandComputation`                       | Fact | Central moments match hand computation.                             |
| `StandardisedMomentsRecoverSkewnessAndKurtosis`            | Fact | Standardised moments recover skewness and kurtosis.                 |
| `EntropyOfCountsMatchesInformationTheoryExamples`          | Fact | Entropy of counts matches information theory examples.              |
| `EntropyOfCategoriesUsesObservedFrequencies`               | Fact | Entropy of categories uses observed frequencies.                    |
| `InequalityMeasuresForOneToFour`                           | Fact | Inequality measures for one to four.                                |
| `GiniCoefficientOfConsecutiveIntegers`                     | Fact | Gini coefficient of consecutive integers.                           |
| `GeometricStandardDeviationIsExpOfLogSd`                   | Fact | Geometric standard deviation is exp of log sd.                      |
| `SummariseColumnsGivesOneRowPerColumn`                     | Fact | Summarise columns gives one row per column.                         |
| `SummariseColumnsHandlesMissingSingleAndAllMissingColumns` | Fact | Summarise columns handles missing single and all missing columns.   |
| `DescribeColumnProfilesTextbookExample`                    | Fact | Describe column profiles textbook example.                          |
| `DescribeColumnHandlesConstantMissingAndNegativeData`      | Fact | Describe column handles constant missing and negative data.         |
| `DescribeTableFindsCorrelationsConstantsSkewAndGaps`       | Fact | Describe table finds correlations constants skew and gaps.          |
| `MissingnessProfileCountsCellsAndRows`                     | Fact | Missingness profile counts cells and rows.                          |
| `CovarianceMatrixMatchesHandComputation`                   | Fact | Covariance matrix matches hand computation.                         |
| `CorrelationMatrixIsUnitDiagonalAndSymmetric`              | Fact | Correlation matrix is unit diagonal and symmetric.                  |
| `StrongestCorrelationsAreRankedByAbsoluteValue`            | Fact | Strongest correlations are ranked by absolute value.                |
| `SummariseRowsAggregatesAcrossColumns`                     | Fact | Summarise rows aggregates across columns.                           |
| `SummariseByGroupSplitsStatistics`                         | Fact | Summarise by group splits statistics.                               |
| `SummariseCategoriesProfilesLabels`                        | Fact | Summarise categories profiles labels.                               |
| `TopShareConcentrationMatchesHandComputation`              | Fact | Top share concentration matches hand computation.                   |
| `ZScoresStandardiseTextbookExample`                        | Fact | Z scores standardise textbook example.                              |
| `RobustZScoresUseMedianAndMad`                             | Fact | Robust z scores use median and mad.                                 |
| `CentreValuesSubtractsMeanOrMedian`                        | Fact | Centre values subtracts mean or median.                             |
| `TransformValuesAppliesAndValidatesDomain`                 | Fact | Transform values applies and validates domain.                      |
| `ProportionsOfTotalSumToOne`                               | Fact | Proportions of total sum to one.                                    |
| `WinsoriseValuesCapsAtPercentiles`                         | Fact | Winsorise values caps at percentiles.                               |
| `ZScoreOutliersFlagTheExtremeValue`                        | Fact | Z score outliers flag the extreme value.                            |
| `IqrFenceFlagsValuesBeyondTukeyFences`                     | Fact | Iqr fence flags values beyond tukey fences.                         |
| `MadOutliersUseModifiedZScore`                             | Fact | Mad outliers use modified z score.                                  |
| `QuantilesByColumnReturnsMatrixOfQuantiles`                | Fact | Quantiles by column returns matrix of quantiles.                    |

### DistributionsAndHistogramsFittingToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.DistributionsAndHistogramsFittingToolsTests` — 22 tests. Category: Unit.

| Test                                                                                  | Kind   | Verifies                                                                                          |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `FitExponentialToOneTwoThreeGivesRateOneHalfAndTheClosedFormLikelihood`               | Fact   | Fit exponential to one two three gives rate one half and the closed form likelihood.              |
| `FitNormalRecoversTheSampleMean`                                                      | Fact   | Fit normal recovers the sample mean.                                                              |
| `FitDiscreteDistributionsMatchTheirClosedFormEstimators`                              | Fact   | Fit discrete distributions match their closed form estimators.                                    |
| `FitContinuousClosedFormEstimatorsMatchHandComputation`                               | Fact   | Fit continuous closed form estimators match hand computation.                                     |
| `FitRecoversParametersFromAQuantileGridOfTheSameDistribution`                         | Theory | Fit recovers parameters from a quantile grid of the same distribution.                            |
| `NumericalFitRecoversParametersFromAQuantileGrid`                                     | Theory | Numerical fit recovers parameters from a quantile grid.                                           |
| `FitLaplaceAgreesWithMedianAndMeanAbsoluteDeviationClosedForm`                        | Fact   | Fit laplace agrees with median and mean absolute deviation closed form.                           |
| `FitBinomialHoldsTheNumberOfTrialsFixedAndEstimatesTheProbability`                    | Fact   | Fit binomial holds the number of trials fixed and estimates the probability.                      |
| `FitGeneralizedParetoHoldsTheLocationAtTheSmallestObservation`                        | Fact   | Fit generalized pareto holds the location at the smallest observation.                            |
| `FitDistributionExplainsBadInputs`                                                    | Fact   | Fit distribution explains bad inputs.                                                             |
| `FitAndScoreGivesTailProbabilitiesUnderTheFittedDistribution`                         | Fact   | Fit and score gives tail probabilities under the fitted distribution.                             |
| `FitAndScoreRejectsEmptyScoringValues`                                                | Fact   | Fit and score rejects empty scoring values.                                                       |
| `RankPutsTheGeneratingExponentialFirstByAic`                                          | Fact   | Rank puts the generating exponential first by aic.                                                |
| `RankOfCountDataUsesDiscreteCandidatesAndFindsThePoisson`                             | Fact   | Rank of count data uses discrete candidates and finds the poisson.                                |
| `RankHonoursExplicitCandidatesCriterionAndReportsFailures`                            | Fact   | Rank honours explicit candidates criterion and reports failures.                                  |
| `RankByAverageAndByEachStatisticOrdersCandidates`                                     | Fact   | Rank by average and by each statistic orders candidates.                                          |
| `RankRejectsMixedKindsAndTinySamples`                                                 | Fact   | Rank rejects mixed kinds and tiny samples.                                                        |
| `QuantileQuantilePlotOfNormalDataAgainstNormalIsAStraightLine`                        | Fact   | Quantile quantile plot of normal data against normal is a straight line.                          |
| `QuantileQuantilePlotUsesTheRequestedPlottingPositionAndFitsWhenParametersAreOmitted` | Fact   | Quantile quantile plot uses the requested plotting position and fits when parameters are omitted. |
| `QuantileQuantilePlotThinsLongSamplesEvenlyAndValidatesArguments`                     | Fact   | Quantile quantile plot thins long samples evenly and validates arguments.                         |
| `ProbabilityProbabilityPlotOfMatchingDistributionLiesOnTheDiagonal`                   | Fact   | Probability probability plot of matching distribution lies on the diagonal.                       |
| `ProbabilityProbabilityPlotOfAMismatchedDistributionDeviatesFromTheDiagonal`          | Fact   | Probability probability plot of a mismatched distribution deviates from the diagonal.             |

### DistributionsAndHistogramsHistogramToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.DistributionsAndHistogramsHistogramToolsTests` — 26 tests. Category: Unit.

| Test                                                                    | Kind   | Verifies                                                                             |
|-------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `FixedBinCountOfOneToTenInFiveBinsHasTwoPerBin`                         | Fact   | Fixed bin count of one to ten in five bins has two per bin.                          |
| `FixedBinCountLastBinIncludesTheMaximumAndScalesAndAccumulates`         | Fact   | Fixed bin count last bin includes the maximum and scales and accumulates.            |
| `FixedBinCountHandlesConstantSingleAndInvalidInput`                     | Fact   | Fixed bin count handles constant single and invalid input.                           |
| `FixedBinCountCopesWithHugeMagnitudes`                                  | Fact   | Fixed bin count copes with huge magnitudes.                                          |
| `FixedBinWidthStartsAtTheMinimumAndCoversTheMaximum`                    | Fact   | Fixed bin width starts at the minimum and covers the maximum.                        |
| `FixedBinWidthRejectsNonPositiveWidthsAndAbsurdBinCounts`               | Fact   | Fixed bin width rejects non positive widths and absurd bin counts.                   |
| `BinningRulesGiveTheTextbookBinCountForOneToOneHundred`                 | Theory | Binning rules give the textbook bin count for one to one hundred.                    |
| `BinningRuleFallsBackToSturgesWhenTheSpreadMeasureIsZero`               | Fact   | Binning rule falls back to sturges when the spread measure is zero.                  |
| `CustomEdgesCountIntoUnequalBinsAndReportOutsideValues`                 | Fact   | Custom edges count into unequal bins and report outside values.                      |
| `CustomEdgesMustBeStrictlyIncreasingAndAtLeastTwo`                      | Fact   | Custom edges must be strictly increasing and at least two.                           |
| `SuggestionsListEveryRuleWithItsBinWidth`                               | Fact   | Suggestions list every rule with its bin width.                                      |
| `Histogram2DCountsPairsIntoAGridWithMarginals`                          | Fact   | Histogram2 d counts pairs into a grid with marginals.                                |
| `Histogram2DAcceptsExplicitEdgesAndRejectsMismatchedLengths`            | Fact   | Histogram2 d accepts explicit edges and rejects mismatched lengths.                  |
| `EmpiricalCdfCountsTheShareAtOrBelowEachPoint`                          | Fact   | Empirical cdf counts the share at or below each point.                               |
| `EmpiricalCdfValidatesInput`                                            | Fact   | Empirical cdf validates input.                                                       |
| `EmpiricalCdfStepsGroupRepeatedValues`                                  | Fact   | Empirical cdf steps group repeated values.                                           |
| `EmpiricalQuantilesFollowTheStandardDefinitions`                        | Fact   | Empirical quantiles follow the standard definitions.                                 |
| `EmpiricalQuantilesValidateProbabilities`                               | Fact   | Empirical quantiles validate probabilities.                                          |
| `GaussianKernelDensityMatchesTheHandComputedSumOfBumps`                 | Fact   | Gaussian kernel density matches the hand computed sum of bumps.                      |
| `EpanechnikovAndUniformKernelsHaveFiniteSupport`                        | Fact   | Epanechnikov and uniform kernels have finite support.                                |
| `KernelDensityUsesTheNamedBandwidthRuleAndValidatesInput`               | Fact   | Kernel density uses the named bandwidth rule and validates input.                    |
| `KernelDensityCurveIntegratesToOneAndFindsASinglePeakForBellShapedData` | Fact   | Kernel density curve integrates to one and finds a single peak for bell shaped data. |
| `KernelDensityCurveFindsTwoPeaksForBimodalData`                         | Fact   | Kernel density curve finds two peaks for bimodal data.                               |
| `KernelDensityCurveValidatesItsArguments`                               | Fact   | Kernel density curve validates its arguments.                                        |
| `BandwidthRulesReportEveryRuleWithItsInputs`                            | Fact   | Bandwidth rules report every rule with its inputs.                                   |
| `BandwidthRulesOmitRulesThatCannotBeComputed`                           | Fact   | Bandwidth rules omit rules that cannot be computed.                                  |

### DistributionsAndHistogramsMultivariateToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.DistributionsAndHistogramsMultivariateToolsTests` — 33 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                            |
|---------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `StandardBivariateNormalDensityAtTheOriginIsOneOverTwoPi`                 | Fact | Standard bivariate normal density at the origin is one over two pi.                 |
| `CorrelatedNormalDensityUsesTheInverseCovariance`                         | Fact | Correlated normal density uses the inverse covariance.                              |
| `NormalDensityValidatesShapeSymmetryAndDefiniteness`                      | Fact | Normal density validates shape symmetry and definiteness.                           |
| `MahalanobisDistanceAndChiSquareTailFollowTheClosedForms`                 | Fact | Mahalanobis distance and chi square tail follow the closed forms.                   |
| `OutlierScoresRankAnObviousOutlierFirst`                                  | Fact | Outlier scores rank an obvious outlier first.                                       |
| `OutlierScoresSumOfSquaredDistancesEqualsNMinusOneTimesPForTheSample`     | Fact | Outlier scores sum of squared distances equals n minus one times p for the sample.  |
| `OutlierScoresRejectSingularOrTooSmallSamples`                            | Fact | Outlier scores reject singular or too small samples.                                |
| `FitMatchesHandComputedMeanCovarianceCorrelationAndLikelihood`            | Fact | Fit matches hand computed mean covariance correlation and likelihood.               |
| `FitRejectsTooFewRowsAndMissingValues`                                    | Fact | Fit rejects too few rows and missing values.                                        |
| `NormalSampleIsReproducibleWithTheRightMeanAndCovariance`                 | Fact | Normal sample is reproducible with the right mean and covariance.                   |
| `OrthantProbabilitiesMatchClosedFormsInOneAndTwoDimensions`               | Fact | Orthant probabilities match closed forms in one and two dimensions.                 |
| `OrthantProbabilityLimitsDimensionality`                                  | Fact | Orthant probability limits dimensionality.                                          |
| `DirichletSummaryMatchesTheClosedFormMomentsAndCovariance`                | Fact | Dirichlet summary matches the closed form moments and covariance.                   |
| `DirichletDensityAtTheMeanMatchesTheHandComputedValue`                    | Fact | Dirichlet density at the mean matches the hand computed value.                      |
| `DirichletDensityRejectsPointsOffTheSimplex`                              | Fact | Dirichlet density rejects points off the simplex.                                   |
| `DirichletSampleRowsSumToOneAndAverageToTheMean`                          | Fact | Dirichlet sample rows sum to one and average to the mean.                           |
| `DirichletFitRecoversTheConcentrationsOfALargeSample`                     | Fact | Dirichlet fit recovers the concentrations of a large sample.                        |
| `DirichletFitRejectsRowsThatAreNotProportions`                            | Fact | Dirichlet fit rejects rows that are not proportions.                                |
| `MultinomialSummaryMatchesTheClosedForms`                                 | Fact | Multinomial summary matches the closed forms.                                       |
| `MultinomialProbabilityMatchesTheFactorialFormula`                        | Fact | Multinomial probability matches the factorial formula.                              |
| `MultinomialSampleRowsSumToTheTrialsAndAverageToTheMean`                  | Fact | Multinomial sample rows sum to the trials and average to the mean.                  |
| `WishartSummaryMatchesTheClosedFormMeanAndVariance`                       | Fact | Wishart summary matches the closed form mean and variance.                          |
| `WishartRejectsTooFewDegreesAndBadScale`                                  | Fact | Wishart rejects too few degrees and bad scale.                                      |
| `OneByOneWishartIsAChiSquare`                                             | Fact | One by one wishart is a chi square.                                                 |
| `WishartDensityIsZeroForAMatrixThatIsNotPositiveDefinite`                 | Fact | Wishart density is zero for a matrix that is not positive definite.                 |
| `WishartSamplesAreSymmetricPositiveDefiniteAndAverageToDegreesTimesScale` | Fact | Wishart samples are symmetric positive definite and average to degrees times scale. |
| `InverseWishartSummaryMatchesTheOneByOneClosedForms`                      | Fact | Inverse wishart summary matches the one by one closed forms.                        |
| `InverseWishartDensityMatchesTheOneByOneClosedForm`                       | Fact | Inverse wishart density matches the one by one closed form.                         |
| `CircularVonMisesFisherDensityMatchesTheBesselClosedForm`                 | Fact | Circular von mises fisher density matches the bessel closed form.                   |
| `VonMisesFisherDensityRejectsOddDimensionsAndZeroDirections`              | Fact | Von mises fisher density rejects odd dimensions and zero directions.                |
| `SphericalVonMisesFisherSamplesAreUnitVectorsWithTheMeanResultantLength`  | Fact | Spherical von mises fisher samples are unit vectors with the mean resultant length. |
| `SphereSamplesLieOnTheSurfaceAndAverageToTheCentre`                       | Fact | Sphere samples lie on the surface and average to the centre.                        |
| `BallSamplesLieInsideAndHaveTwoThirdsMeanRadiusInTwoDimensions`           | Fact | Ball samples lie inside and have two thirds mean radius in two dimensions.          |

### DistributionsAndHistogramsShapeTailAndMixtureToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.DistributionsAndHistogramsShapeTailAndMixtureToolsTests` — 19 tests. Category:
Unit.

| Test                                                                            | Kind | Verifies                                                                                 |
|---------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `ShapeSummaryMatchesTheHandComputedSkewnessKurtosisAndJarqueBera`               | Fact | Shape summary matches the hand computed skewness kurtosis and jarque bera.               |
| `ShapeSummaryFlagsSkewAndTailWeight`                                            | Fact | Shape summary flags skew and tail weight.                                                |
| `ShapeSummaryRejectsConstantTinyAndMissingData`                                 | Fact | Shape summary rejects constant tiny and missing data.                                    |
| `QuantileExceedanceMatchesHandComputationForOneToOneHundred`                    | Fact | Quantile exceedance matches hand computation for one to one hundred.                     |
| `QuantileExceedanceLowerTailAndValidation`                                      | Fact | Quantile exceedance lower tail and validation.                                           |
| `ThresholdExceedanceReportsCountsAndMeanExcess`                                 | Fact | Threshold exceedance reports counts and mean excess.                                     |
| `ThresholdExceedanceValidatesInput`                                             | Fact | Threshold exceedance validates input.                                                    |
| `ExponentialTailGivesShapeNearZeroAndTheClosedFormReturnLevel`                  | Fact | Exponential tail gives shape near zero and the closed form return level.                 |
| `HeavyTailGivesPositiveShape`                                                   | Fact | Heavy tail gives positive shape.                                                         |
| `GeneralizedParetoTailValidatesItsArguments`                                    | Fact | Generalized pareto tail validates its arguments.                                         |
| `TwoSeparatedClustersAreRecoveredAsTwoComponents`                               | Fact | Two separated clusters are recovered as two components.                                  |
| `SingleComponentEqualsTheNormalMaximumLikelihoodAndResponsibilitiesAreOptional` | Fact | Single component equals the normal maximum likelihood and responsibilities are optional. |
| `MixtureFitIsReproducibleForASeedAndValidatesInput`                             | Fact | Mixture fit is reproducible for a seed and validates input.                              |
| `ComponentSelectionPicksTwoForTwoSeparatedClusters`                             | Fact | Component selection picks two for two separated clusters.                                |
| `ComponentSelectionStopsWhenValuesRunOut`                                       | Fact | Component selection stops when values run out.                                           |
| `LognormalMixtureRecoversTwoLogScaleGroups`                                     | Fact | Lognormal mixture recovers two log scale groups.                                         |
| `MixtureModelRejectsUnsupportedFamiliesAndBadData`                              | Fact | Mixture model rejects unsupported families and bad data.                                 |
| `MixtureEvaluationMatchesTheHandComputedDensityCumulativeAndResponsibilities`   | Fact | Mixture evaluation matches the hand computed density cumulative and responsibilities.    |
| `MixtureEvaluationValidatesWeightsAndParameters`                                | Fact | Mixture evaluation validates weights and parameters.                                     |

### DistributionsAndHistogramsUnivariateToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.DistributionsAndHistogramsUnivariateToolsTests` — 33 tests. Category: Unit.

| Test                                                                 | Kind   | Verifies                                                                      |
|----------------------------------------------------------------------|--------|-------------------------------------------------------------------------------|
| `ListDistributionsDescribesEveryKindWithParameterNamesAndFitSupport` | Fact   | List distributions describes every kind with parameter names and fit support. |
| `EveryListedDistributionAcceptsItsOwnExampleParametersInDescribe`    | Fact   | Every listed distribution accepts its own example parameters in describe.     |
| `DescribeNormalMatchesClosedFormMoments`                             | Fact   | Describe normal matches closed form moments.                                  |
| `DescribeMatchesTextbookMeanAndVariance`                             | Theory | Describe matches textbook mean and variance.                                  |
| `DescribeUniformDiscreteEntropyIsLogOfTheNumberOfOutcomes`           | Fact   | Describe uniform discrete entropy is log of the number of outcomes.           |
| `DescribeReportsNullForMomentsThatDoNotExist`                        | Fact   | Describe reports null for moments that do not exist.                          |
| `DescribeRejectsWrongParameterCountAndDomains`                       | Fact   | Describe rejects wrong parameter count and domains.                           |
| `DensityOfStandardNormalAtZeroIsOneOverRootTwoPi`                    | Fact   | Density of standard normal at zero is one over root two pi.                   |
| `DensityMatchesClosedForms`                                          | Theory | Density matches closed forms.                                                 |
| `DiscreteDensityIsExactProbabilityMassAndZeroBetweenIntegers`        | Fact   | Discrete density is exact probability mass and zero between integers.         |
| `DensityValidatesInputs`                                             | Fact   | Density validates inputs.                                                     |
| `LogDensityIsNaturalLogAndSurvivesTheFarTail`                        | Fact   | Log density is natural log and survives the far tail.                         |
| `CumulativeProbabilityMatchesTablesAndLimits`                        | Fact   | Cumulative probability matches tables and limits.                             |
| `DiscreteCumulativeProbabilityRoundsDownToWholeNumbers`              | Fact   | Discrete cumulative probability rounds down to whole numbers.                 |
| `UpperTailProbabilityIsAccurateInTheFarTail`                         | Fact   | Upper tail probability is accurate in the far tail.                           |
| `QuantileMatchesPublishedCriticalValues`                             | Fact   | Quantile matches published critical values.                                   |
| `QuantileOfDiscreteIsSmallestWholeNumberReachingTheProbability`      | Fact   | Quantile of discrete is smallest whole number reaching the probability.       |
| `QuantileRejectsProbabilitiesOutsideZeroOne`                         | Fact   | Quantile rejects probabilities outside zero one.                              |
| `HazardRateIsConstantForExponentialAndRisingForWeibullAboveOne`      | Fact   | Hazard rate is constant for exponential and rising for weibull above one.     |
| `CumulativeHazardIsMinusLogOfTheUpperTail`                           | Fact   | Cumulative hazard is minus log of the upper tail.                             |
| `IntervalProbabilityIsDifferenceOfCumulativeProbabilities`           | Fact   | Interval probability is difference of cumulative probabilities.               |
| `IntervalProbabilityRejectsMismatchedOrReversedEnds`                 | Fact   | Interval probability rejects mismatched or reversed ends.                     |
| `CurveSpansTheRequestedQuantilesAndCumulativeIsMonotone`             | Fact   | Curve spans the requested quantiles and cumulative is monotone.               |
| `CurveValidatesItsArguments`                                         | Fact   | Curve validates its arguments.                                                |
| `ProbabilityTableListsExactMassesAndTotals`                          | Fact   | Probability table lists exact masses and totals.                              |
| `ProbabilityTableRejectsContinuousDistributionsAndBadRanges`         | Fact   | Probability table rejects continuous distributions and bad ranges.            |
| `RandomSampleIsReproducibleAndHasTheRightMoments`                    | Fact   | Random sample is reproducible and has the right moments.                      |
| `RandomSampleOfDiscreteDistributionsGivesWholeNumbersInSupport`      | Fact   | Random sample of discrete distributions gives whole numbers in support.       |
| `RandomSampleValidatesCountAndSupport`                               | Fact   | Random sample validates count and support.                                    |
| `LogLikelihoodSumsLogDensitiesAndComputesAicAndBic`                  | Fact   | Log likelihood sums log densities and computes aic and bic.                   |
| `LogLikelihoodIsNullWhenAnObservationIsImpossible`                   | Fact   | Log likelihood is null when an observation is impossible.                     |
| `TailScoresGiveLowerUpperTwoSidedAndSurprise`                        | Fact   | Tail scores give lower upper two sided and surprise.                          |
| `TailScoresOfDiscreteValuesIncludeTheValueInBothTails`               | Fact   | Tail scores of discrete values include the value in both tails.               |

### HypothesisTestsToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.HypothesisTestsToolsTests` — 143 tests.

| Test                                                                                   | Kind | Verifies                                                                                           |
|----------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `ChiSquareGoodnessOfFitTestMatchesMendelsPeaData`                                      | Fact | Chi square goodness of fit test matches mendels pea data.                                          |
| `ChiSquareGoodnessOfFitTestDefaultsToAnEvenSplitAndReducesDegreesOfFreedom`            | Fact | Chi square goodness of fit test defaults to an even split and reduces degrees of freedom.          |
| `ChiSquareGoodnessOfFitTestWarnsForSparseCategoriesAndRejectsBadInput`                 | Fact | Chi square goodness of fit test warns for sparse categories and rejects bad input.                 |
| `ChiSquareIndependenceTestTwoByTwoMatchesClosedForm`                                   | Fact | Chi square independence test two by two matches closed form.                                       |
| `ChiSquareIndependenceTestTwoByThreeUsesRowsMinusOneTimesColumnsMinusOneDegrees`       | Fact | Chi square independence test two by three uses rows minus one times columns minus one degrees.     |
| `ChiSquareIndependenceTestRejectsBadTablesAndWarnsForSmallExpectedCounts`              | Fact | Chi square independence test rejects bad tables and warns for small expected counts.               |
| `FisherExactTestMatchesFishersLadyTastingTea`                                          | Fact | Fisher exact test matches fishers lady tasting tea.                                                |
| `FisherExactTestHandlesZeroCellsAndRejectsNonTwoByTwo`                                 | Fact | Fisher exact test handles zero cells and rejects non two by two.                                   |
| `McNemarTestMatchesTheWikipediaWorkedExample`                                          | Fact | Mc nemar test matches the wikipedia worked example.                                                |
| `McNemarTestRejectsNoDiscordantPairsAndWarnsForFewOfThem`                              | Fact | Mc nemar test rejects no discordant pairs and warns for few of them.                               |
| `BowkerSymmetryTestMatchesHandComputation`                                             | Fact | Bowker symmetry test matches hand computation.                                                     |
| `BowkerSymmetryTestRejectsAnEmptySymmetricPairAndNonSquareTables`                      | Fact | Bowker symmetry test rejects an empty symmetric pair and non square tables.                        |
| `StuartMaxwellHomogeneityTestMatchesHandComputation`                                   | Fact | Stuart maxwell homogeneity test matches hand computation.                                          |
| `BhapkarHomogeneityTestMatchesHandComputation`                                         | Fact | Bhapkar homogeneity test matches hand computation.                                                 |
| `CohensKappaTestMatchesTheGrantProposalExample`                                        | Fact | Cohens kappa test matches the grant proposal example.                                              |
| `CohensKappaTestAgainstANonZeroHypothesisUsesTheGeneralStandardError`                  | Fact | Cohens kappa test against a non zero hypothesis uses the general standard error.                   |
| `CohensKappaTestRejectsBadInput`                                                       | Fact | Cohens kappa test rejects bad input.                                                               |
| `WeightedKappaTestMatchesHandComputationForBothWeightings`                             | Fact | Weighted kappa test matches hand computation for both weightings.                                  |
| `WeightedKappaTestRejectsBadInput`                                                     | Fact | Weighted kappa test rejects bad input.                                                             |
| `KappaComparisonTestMatchesHandComputation`                                            | Fact | Kappa comparison test matches hand computation.                                                    |
| `KappaHomogeneityTestMatchesInverseVarianceComputation`                                | Fact | Kappa homogeneity test matches inverse variance computation.                                       |
| `KappaGroupDifferenceTestComparesTheMeanKappasOfTwoGroups`                             | Fact | Kappa group difference test compares the mean kappas of two groups.                                |
| `AucSignificanceTestMatchesDeLongHandComputation`                                      | Fact | Auc significance test matches de long hand computation.                                            |
| `AucSignificanceTestReportsAnInvertedScoreBelowHalf`                                   | Fact | Auc significance test reports an inverted score below half.                                        |
| `AucSignificanceTestRejectsBadInput`                                                   | Fact | Auc significance test rejects bad input.                                                           |
| `AucComparisonTestMatchesDeLongCovarianceHandComputation`                              | Fact | Auc comparison test matches de long covariance hand computation.                                   |
| `AucComparisonTestRejectsIdenticalModelsAndBadInput`                                   | Fact | Auc comparison test rejects identical models and bad input.                                        |
| `OneSampleTTestMatchesPublishedSleepGroupTwoResult`                                    | Fact | One sample t test matches published sleep group two result.                                        |
| `OneSampleTTestOneSidedTailsSplitTheTwoSidedPValue`                                    | Fact | One sample t test one sided tails split the two sided p value.                                     |
| `OneSampleTTestOneSidedIntervalIsStillTheTwoSidedIntervalAtOneMinusAlpha`              | Fact | One sample t test one sided interval is still the two sided interval at one minus alpha.           |
| `OneSampleTTestNonRejectionUsesAlpha`                                                  | Fact | One sample t test non rejection uses alpha.                                                        |
| `OneSampleTTestWhenMeanEqualsHypothesisGivesZeroStatisticAndPValueOne`                 | Fact | One sample t test when mean equals hypothesis gives zero statistic and p value one.                |
| `OneSampleTTestRejectsBadInputWithTheParameterName`                                    | Fact | One sample t test rejects bad input with the parameter name.                                       |
| `OneSampleTTestHandlesHugeMagnitudesWithoutLosingTheStatistic`                         | Fact | One sample t test handles huge magnitudes without losing the statistic.                            |
| `OneSampleTTestFromSummaryEqualsTheRawResult`                                          | Fact | One sample t test from summary equals the raw result.                                              |
| `OneSampleTTestFromSummaryRejectsNonPositiveSpreadAndTinySamples`                      | Fact | One sample t test from summary rejects non positive spread and tiny samples.                       |
| `TwoSampleTTestWelchMatchesPublishedSleepResult`                                       | Fact | Two sample t test welch matches published sleep result.                                            |
| `TwoSampleTTestPooledMatchesPublishedSleepResult`                                      | Fact | Two sample t test pooled matches published sleep result.                                           |
| `TwoSampleTTestOneSidedDirectionIsGroupOneMinusGroupTwo`                               | Fact | Two sample t test one sided direction is group one minus group two.                                |
| `TwoSampleTTestWarnsWhenPooledButVariancesAreVeryDifferent`                            | Fact | Two sample t test warns when pooled but variances are very different.                              |
| `TwoSampleTTestRejectsBadInput`                                                        | Fact | Two sample t test rejects bad input.                                                               |
| `TwoSampleTTestFromSummaryEqualsTheRawWelchResult`                                     | Fact | Two sample t test from summary equals the raw welch result.                                        |
| `TwoSampleTTestFromSummaryRejectsBadInput`                                             | Fact | Two sample t test from summary rejects bad input.                                                  |
| `PairedTTestMatchesPublishedSleepResult`                                               | Fact | Paired t test matches published sleep result.                                                      |
| `PairedTTestRejectsMismatchedLengthsAndIdenticalPairs`                                 | Fact | Paired t test rejects mismatched lengths and identical pairs.                                      |
| `OneSampleZTestWithKnownSigmaMatchesClosedForm`                                        | Fact | One sample z test with known sigma matches closed form.                                            |
| `OneSampleZTestWithSampleSpreadWarnsForSmallSamples`                                   | Fact | One sample z test with sample spread warns for small samples.                                      |
| `OneSampleZTestRejectsBadSigmaAndConstantSampleWithoutSigma`                           | Fact | One sample z test rejects bad sigma and constant sample without sigma.                             |
| `TwoSampleZTestMatchesClosedForm`                                                      | Fact | Two sample z test matches closed form.                                                             |
| `TwoSampleZTestRejectsBadInputAndWarnsForSmallGroups`                                  | Fact | Two sample z test rejects bad input and warns for small groups.                                    |
| `TwoSampleZTestFromSummaryEqualsRawResult`                                             | Fact | Two sample z test from summary equals raw result.                                                  |
| `WaldZTestMatchesClosedForm`                                                           | Fact | Wald z test matches closed form.                                                                   |
| `OneProportionZTestSixtyHeadsInAHundredTossesGivesZTwo`                                | Fact | One proportion z test sixty heads in a hundred tosses gives z two.                                 |
| `OneProportionZTestWarnsForSparseExpectedCountsAndRejectsBadInput`                     | Fact | One proportion z test warns for sparse expected counts and rejects bad input.                      |
| `TwoProportionZTestMatchesPooledClosedForm`                                            | Fact | Two proportion z test matches pooled closed form.                                                  |
| `TwoProportionZTestRejectsDegenerateAndBadInput`                                       | Fact | Two proportion z test rejects degenerate and bad input.                                            |
| `BinomialExactTestMatchesPublishedSixtyOfHundredResult`                                | Fact | Binomial exact test matches published sixty of hundred result.                                     |
| `BinomialExactTestHandlesBoundariesAndBadInput`                                        | Fact | Binomial exact test handles boundaries and bad input.                                              |
| `BinomialExactTestFromOutcomesCountsTheTrueValues`                                     | Fact | Binomial exact test from outcomes counts the true values.                                          |
| `FTestTwoVariancesMatchesClosedFormOnPlantGrowth`                                      | Fact | F test two variances matches closed form on plant growth.                                          |
| `FTestTwoVariancesRejectsBadInput`                                                     | Fact | F test two variances rejects bad input.                                                            |
| `FTestTwoVariancesFromSummaryEqualsRawResult`                                          | Fact | F test two variances from summary equals raw result.                                               |
| `LeveneTestBrownForsytheMatchesPublishedPlantGrowthResult`                             | Fact | Levene test brown forsythe matches published plant growth result.                                  |
| `LeveneTestMeanCentreMatchesClosedForm`                                                | Fact | Levene test mean centre matches closed form.                                                       |
| `LeveneTestTrimmedMeanRunsAndRejectsBadInput`                                          | Fact | Levene test trimmed mean runs and rejects bad input.                                               |
| `BartlettTestMatchesPublishedPlantGrowthResult`                                        | Fact | Bartlett test matches published plant growth result.                                               |
| `BartlettTestRejectsConstantGroupAndSingleGroup`                                       | Fact | Bartlett test rejects constant group and single group.                                             |
| `OneWayAnovaMatchesPublishedPlantGrowthTable`                                          | Fact | One way anova matches published plant growth table.                                                |
| `OneWayAnovaWithPerfectlySeparatedConstantGroupsHasInfiniteF`                          | Fact | One way anova with perfectly separated constant groups has infinite f.                             |
| `OneWayAnovaFromLabelsEqualsTheGroupedResult`                                          | Fact | One way anova from labels equals the grouped result.                                               |
| `TwoWayAnovaFixedEffectsTestsEverythingAgainstErrorMeanSquare`                         | Fact | Two way anova fixed effects tests everything against error mean square.                            |
| `TwoWayAnovaRandomEffectsTestsFactorsAgainstTheInteraction`                            | Fact | Two way anova random effects tests factors against the interaction.                                |
| `TwoWayAnovaMixedModelTestsFactorAAgainstInteractionAndFactorBAgainstError`            | Fact | Two way anova mixed model tests factor a against interaction and factor b against error.           |
| `TwoWayAnovaRejectsUnbalancedAndMalformedDesigns`                                      | Fact | Two way anova rejects unbalanced and malformed designs.                                            |
| `GrubbsOutlierTestFlagsTheGrossOutlier`                                                | Fact | Grubbs outlier test flags the gross outlier.                                                       |
| `GrubbsOutlierTestRejectsBadInput`                                                     | Fact | Grubbs outlier test rejects bad input.                                                             |
| `EffectSizeTwoSamplesMatchesClosedFormOnSleepData`                                     | Fact | Effect size two samples matches closed form on sleep data.                                         |
| `EffectSizeTwoSamplesRejectsBadInput`                                                  | Fact | Effect size two samples rejects bad input.                                                         |
| `EffectSizeFromSummaryMatchesRawResult`                                                | Fact | Effect size from summary matches raw result.                                                       |
| `OneSampleTTestPowerAnalysisSolvesPowerForHalfSdEffectAndThirtyFourObservations`       | Fact | One sample t test power analysis solves power for half sd effect and thirty four observations.     |
| `OneSampleTTestPowerAnalysisNeedsThirtyFourObservationsForPowerEightyAtHalfSd`         | Fact | One sample t test power analysis needs thirty four observations for power eighty at half sd.       |
| `OneSampleTTestPowerAnalysisSolvesTheDetectableEffect`                                 | Fact | One sample t test power analysis solves the detectable effect.                                     |
| `OneSampleTTestPowerAnalysisRejectsMissingOrInvalidInputs`                             | Fact | One sample t test power analysis rejects missing or invalid inputs.                                |
| `OneSampleZTestPowerAnalysisMatchesTheClosedForm`                                      | Fact | One sample z test power analysis matches the closed form.                                          |
| `OneSampleZTestPowerAnalysisLessTailMirrorsTheGreaterTail`                             | Fact | One sample z test power analysis less tail mirrors the greater tail.                               |
| `OneSampleZTestPowerAnalysisRoundTripsTheEffectSize`                                   | Fact | One sample z test power analysis round trips the effect size.                                      |
| `TwoSampleTTestPowerAnalysisNeedsSixtyFourPerGroupForHalfSdAtPowerEighty`              | Fact | Two sample t test power analysis needs sixty four per group for half sd at power eighty.           |
| `TwoSampleTTestPowerAnalysisSupportsUnequalAllocationAndRejectsBadInput`               | Fact | Two sample t test power analysis supports unequal allocation and rejects bad input.                |
| `TwoSampleZTestPowerAnalysisNeedsSixtyThreePerGroupForHalfSdAtPowerEighty`             | Fact | Two sample z test power analysis needs sixty three per group for half sd at power eighty.          |
| `TwoSampleZTestPowerAnalysisSolvesEffectAndRejectsBadInput`                            | Fact | Two sample z test power analysis solves effect and rejects bad input.                              |
| `MeanConfidenceIntervalMatchesPublishedSleepGroupIntervals`                            | Fact | Mean confidence interval matches published sleep group intervals.                                  |
| `MeanConfidenceIntervalWidensWithConfidenceAndRejectsBadInput`                         | Fact | Mean confidence interval widens with confidence and rejects bad input.                             |
| `ProportionConfidenceIntervalMatchesClosedFormsForSixtyOfHundred`                      | Fact | Proportion confidence interval matches closed forms for sixty of hundred.                          |
| `ProportionConfidenceIntervalIsClippedAtTheBoundsAndWarnsForWaldWithFewSuccesses`      | Fact | Proportion confidence interval is clipped at the bounds and warns for wald with few successes.     |
| `VarianceConfidenceIntervalMatchesChiSquareHandComputation`                            | Fact | Variance confidence interval matches chi square hand computation.                                  |
| `VarianceConfidenceIntervalRejectsBadInput`                                            | Fact | Variance confidence interval rejects bad input.                                                    |
| `MedianConfidenceIntervalForElevenValuesIsTheSecondToTenthOrderStatistic`              | Fact | Median confidence interval for eleven values is the second to tenth order statistic.               |
| `MedianConfidenceIntervalReturnsTheRangeWithAWarningWhenTheSampleIsTooSmall`           | Fact | Median confidence interval returns the range with a warning when the sample is too small.          |
| `AdjustPValuesBonferroniMultipliesByTheNumberOfTests`                                  | Fact | Adjust p values bonferroni multiplies by the number of tests.                                      |
| `AdjustPValuesHolmIsStepDownAndMonotone`                                               | Fact | Adjust p values holm is step down and monotone.                                                    |
| `AdjustPValuesBenjaminiHochbergControlsTheFalseDiscoveryRate`                          | Fact | Adjust p values benjamini hochberg controls the false discovery rate.                              |
| `AdjustPValuesByMethodCoversSidakHochbergAndBenjaminiYekutieli`                        | Fact | Adjust p values by method covers sidak hochberg and benjamini yekutieli.                           |
| `AdjustPValuesRejectsEmptyOutOfRangeAndNaNAndKeepsASingleValueUnchanged`               | Fact | Adjust p values rejects empty out of range and na n and keeps a single value unchanged.            |
| `PValueFromTestStatisticMatchesClosedFormsForEachDistribution`                         | Fact | P value from test statistic matches closed forms for each distribution.                            |
| `PValueFromTestStatisticSupportsLowerTailAndRejectsMissingDegrees`                     | Fact | P value from test statistic supports lower tail and rejects missing degrees.                       |
| `CriticalValueForSignificanceLevelMatchesTextbookValues`                               | Fact | Critical value for significance level matches textbook values.                                     |
| `CriticalValueForSignificanceLevelOneSidedLowerTailAndBadInput`                        | Fact | Critical value for significance level one sided lower tail and bad input.                          |
| `RecommendHypothesisTestChoosesTheClassicTestForEachCommonSituation`                   | Fact | Recommend hypothesis test chooses the classic test for each common situation.                      |
| `RecommendHypothesisTestOnlyNamesToolsThatExist`                                       | Fact | Recommend hypothesis test only names tools that exist.                                             |
| `RecommendHypothesisTestRejectsBadCounts`                                              | Fact | Recommend hypothesis test rejects bad counts.                                                      |
| `EveryHypothesisToolIsSelfDescribedWithoutTheLibraryName`                              | Fact | Every hypothesis tool is self described without the library name.                                  |
| `MannWhitneyUTestCompleteSeparationHasExactPValueOneTenth`                             | Fact | Mann whitney u test complete separation has exact p value one tenth.                               |
| `MannWhitneyUTestMatchesPublishedHollanderWolfeExample`                                | Fact | Mann whitney u test matches published hollander wolfe example.                                     |
| `MannWhitneyUTestWithTiesUsesApproximationAndWarns`                                    | Fact | Mann whitney u test with ties uses approximation and warns.                                        |
| `MannWhitneyUTestRejectsBadInput`                                                      | Fact | Mann whitney u test rejects bad input.                                                             |
| `WilcoxonSignedRankTestOfDifferencesMatchesPublishedDepressionExample`                 | Fact | Wilcoxon signed rank test of differences matches published depression example.                     |
| `WilcoxonSignedRankTestDropsZerosAndRejectsAllZeroInput`                               | Fact | Wilcoxon signed rank test drops zeros and rejects all zero input.                                  |
| `WilcoxonSignedRankPairedTestMatchesPublishedDepressionExample`                        | Fact | Wilcoxon signed rank paired test matches published depression example.                             |
| `WilcoxonSignedRankPairedTestRejectsMismatchAndIdenticalPairs`                         | Fact | Wilcoxon signed rank paired test rejects mismatch and identical pairs.                             |
| `SignTestOneSampleCountsFivePositivesOfNineteenAndSaysMedianIsBelow`                   | Fact | Sign test one sample counts five positives of nineteen and says median is below.                   |
| `SignTestOneSampleRejectsAllTiedAndEmptyInput`                                         | Fact | Sign test one sample rejects all tied and empty input.                                             |
| `SignTestPairedAgreesWithTheOneSampleTestOnTheSameSigns`                               | Fact | Sign test paired agrees with the one sample test on the same signs.                                |
| `SignTestPairedRejectsMismatchAndAllTied`                                              | Fact | Sign test paired rejects mismatch and all tied.                                                    |
| `SignTestFromCountsUsesTheExactBinomial`                                               | Fact | Sign test from counts uses the exact binomial.                                                     |
| `KolmogorovSmirnovOneSampleTestUniformExampleHasStatisticPointOneFive`                 | Fact | Kolmogorov smirnov one sample test uniform example has statistic point one five.                   |
| `KolmogorovSmirnovOneSampleTestRejectsAWronglyCentredNormal`                           | Fact | Kolmogorov smirnov one sample test rejects a wrongly centred normal.                               |
| `KolmogorovSmirnovOneSampleTestSupportsExponentialAndLogNormalAndRejectsBadParameters` | Fact | Kolmogorov smirnov one sample test supports exponential and log normal and rejects bad parameters. |
| `KolmogorovSmirnovTwoSampleTestHandComputedStatisticIsHalf`                            | Fact | Kolmogorov smirnov two sample test hand computed statistic is half.                                |
| `KolmogorovSmirnovTwoSampleTestDetectsDisjointSamplesAndRejectsEmpty`                  | Fact | Kolmogorov smirnov two sample test detects disjoint samples and rejects empty.                     |
| `ShapiroWilkNormalityTestMatchesThePaperExampleWithinApproximationError`               | Fact | Shapiro wilk normality test matches the paper example within approximation error.                  |
| `ShapiroWilkNormalityTestAcceptsNormalQuantilesAndRejectsBadInput`                     | Fact | Shapiro wilk normality test accepts normal quantiles and rejects bad input.                        |
| `AndersonDarlingNormalityTestMatchesHandComputedStatistic`                             | Fact | Anderson darling normality test matches hand computed statistic.                                   |
| `AndersonDarlingNormalityTestAcceptsNormalQuantilesAndRejectsBadInput`                 | Fact | Anderson darling normality test accepts normal quantiles and rejects bad input.                    |
| `LillieforsNormalityTestRejectsSkewedDataAndAcceptsNormalQuantiles`                    | Fact | Lilliefors normality test rejects skewed data and accepts normal quantiles.                        |
| `LillieforsNormalityTestRejectsBadInput`                                               | Fact | Lilliefors normality test rejects bad input.                                                       |
| `JarqueBeraNormalityTestMatchesClosedForm`                                             | Fact | Jarque bera normality test matches closed form.                                                    |
| `JarqueBeraNormalityTestRejectsBadInput`                                               | Fact | Jarque bera normality test rejects bad input.                                                      |
| `ChiSquareDistributionFitTestAcceptsEvenlySpreadUniformData`                           | Fact | Chi square distribution fit test accepts evenly spread uniform data.                               |
| `ChiSquareDistributionFitTestRejectsWrongReferenceAndBadInput`                         | Fact | Chi square distribution fit test rejects wrong reference and bad input.                            |
| `KruskalWallisTestMatchesPublishedHollanderWolfeExample`                               | Fact | Kruskal wallis test matches published hollander wolfe example.                                     |
| `KruskalWallisTestAppliesTheTieCorrection`                                             | Fact | Kruskal wallis test applies the tie correction.                                                    |
| `KruskalWallisTestRejectsBadInput`                                                     | Fact | Kruskal wallis test rejects bad input.                                                             |
| `EveryHypothesisToolFailsOnlyWithArgumentExceptionsWhateverTheInput`                   | Fact | Every hypothesis tool fails only with argument exceptions whatever the input.                      |

### ModelEvaluationMulticlassAndAnomalyTests

`Jube.Test.AdvancedAnalytics.Statistics.ModelEvaluationMulticlassAndAnomalyTests` — 3 tests.

| Test                                          | Kind | Verifies                                           |
|-----------------------------------------------|------|----------------------------------------------------|
| `MulticlassAlgorithmsRecoverBandedClasses`    | Fact | Multiclass algorithms recover banded classes.      |
| `EveryDetectorScoresKnownOutliersHigher`      | Fact | Every detector scores known outliers higher.       |
| `ContaminationIsEstimatedFromASeparatedGroup` | Fact | Contamination is estimated from a separated group. |

### ModelEvaluationRegressionTests

`Jube.Test.AdvancedAnalytics.Statistics.ModelEvaluationRegressionTests` — 8 tests.

| Test                                                           | Kind | Verifies                                                              |
|----------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `LinearRegressionRecoversTwoXPlusOneExactly`                   | Fact | Linear regression recovers two x plus one exactly.                    |
| `RidgeHandlesCollinearPredictorsAndConstantTargetsAreRejected` | Fact | Ridge handles collinear predictors and constant targets are rejected. |
| `TreeRegressionLearnsAStepFunction`                            | Fact | Tree regression learns a step function.                               |
| `NeighbourAndKernelRegressionFitASmoothCurve`                  | Fact | Neighbour and kernel regression fit a smooth curve.                   |
| `NeuralRegressionIsSeededAndFitsALine`                         | Fact | Neural regression is seeded and fits a line.                          |
| `RegressorComparisonRanksLinearFirstOnLinearData`              | Fact | Regressor comparison ranks linear first on linear data.               |
| `RegressionImportanceFindsTheSignalVariable`                   | Fact | Regression importance finds the signal variable.                      |
| `RegressionPartialDependenceIsIncreasingForALine`              | Fact | Regression partial dependence is increasing for a line.               |

### ModelEvaluationSelectionTests

`Jube.Test.AdvancedAnalytics.Statistics.ModelEvaluationSelectionTests` — 9 tests.

| Test                                                                   | Kind | Verifies                                                                   |
|------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `GridSearchPrefersDepthTwoForAnInteractionAndReportsNestedScoreAsync`  | Fact | Grid search prefers depth two for an interaction and reports nested score. |
| `RandomSearchIsSeededAndBoundedAsync`                                  | Fact | Random search is seeded and bounded.                                       |
| `LearningCurveGrowsTheTrainingRowsAsync`                               | Fact | Learning curve grows the training rows.                                    |
| `ValidationCurveShowsDepthOneUnderfitsAnInteractionAsync`              | Fact | Validation curve shows depth one underfits an interaction.                 |
| `PermutationImportanceFlagsTheNoiseVariableAsync`                      | Fact | Permutation importance flags the noise variable.                           |
| `PartialDependenceIsIncreasingForTheSignalAndFlatForNoiseAsync`        | Fact | Partial dependence is increasing for the signal and flat for noise.        |
| `CalibrationComparisonListsFourMethodsAndPlattBeatsRawNaiveBayes`      | Fact | Calibration comparison lists four methods and platt beats raw naive bayes. |
| `ScoreCalibrationCorrectsOverconfidentScores`                          | Fact | Score calibration corrects overconfident scores.                           |
| `ModelComparisonUsesIdenticalFoldsAndRecommendsAnEquivalentModelAsync` | Fact | Model comparison uses identical folds and recommends an equivalent model.  |

### ModelEvaluationToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.ModelEvaluationToolsTests` — 13 tests.

| Test                                                              | Kind | Verifies                                                                    |
|-------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `DecisionTreeRecoversTheKnownInteractionForEveryAlgorithm`        | Fact | Decision tree recovers the known interaction for every algorithm.           |
| `DecisionTreeIsDeterministicAndPruningNeverGrowsIt`               | Fact | Decision tree is deterministic and pruning never grows it.                  |
| `ClassificationToolsRejectBadInput`                               | Fact | Classification tools reject bad input.                                      |
| `SmallSamplesAndLeakedPredictorsAreWarnedAbout`                   | Fact | Small samples and leaked predictors are warned about.                       |
| `SevereImbalanceWarnsUnlessHandled`                               | Fact | Severe imbalance warns unless handled.                                      |
| `RandomForestAndBaggingAreAccurateAndDeterministic`               | Fact | Random forest and bagging are accurate and deterministic.                   |
| `BothBoostingAlgorithmsSeparateTheClasses`                        | Fact | Both boosting algorithms separate the classes.                              |
| `NaiveBayesKindsRankTheClasses`                                   | Fact | Naive bayes kinds rank the classes.                                         |
| `NearestNeighboursReportTheKSweepWithOneSelection`                | Fact | Nearest neighbours report the k sweep with one selection.                   |
| `SupportVectorMachinesSweepComplexity`                            | Fact | Support vector machines sweep complexity.                                   |
| `NeuralNetworkClassifierIsSeededAndAccurate`                      | Fact | Neural network classifier is seeded and accurate.                           |
| `LogisticCrossValidationPoolsEveryRowOnce`                        | Fact | Logistic cross validation pools every row once.                             |
| `TestRowsNeverInfluenceTheFitAndValidationRowsOnlyTheCalibration` | Fact | Test rows never influence the fit and validation rows only the calibration. |

### ModelSelectionToolsCachePointerTests

`Jube.Test.AdvancedAnalytics.Statistics.ModelSelectionToolsCachePointerTests` — 6 tests.

| Test                                                                      | Kind | Verifies                                                                        |
|---------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `PutModelComparisonDataWithNoBoundWriterIsRefusedAsync`                   | Fact | Put model comparison data with no bound writer is refused.                      |
| `PutModelComparisonDataDelegatesToTheBoundWriterAndReturnsItsResultAsync` | Fact | Put model comparison data delegates to the bound writer and returns its result. |
| `GivingBothInlineRowsAndACachePointerIsRefusedAsync`                      | Fact | Giving both inline rows and a cache pointer is refused.                         |
| `GivingNeitherInlineRowsNorACachePointerIsRefusedAsync`                   | Fact | Giving neither inline rows nor a cache pointer is refused.                      |
| `ACachePointerWithNoBoundReaderIsRefusedAsync`                            | Fact | A cache pointer with no bound reader is refused.                                |
| `ACachePointerResolvesToTheSameResultAsTheIdenticalInlineRowsAsync`       | Fact | A cache pointer resolves to the same result as the identical inline rows.       |

### MultivariateAndClusteringToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.MultivariateAndClusteringToolsTests` — 119 tests.

| Test                                                                                     | Kind   | Verifies                                                                                             |
|------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `VectorDistanceMatchesClosedFormsForContinuousMetrics`                                   | Fact   | Vector distance matches closed forms for continuous metrics.                                         |
| `VectorDistanceAngularIsTheArcCosineOfTheCosineSimilarity`                               | Fact   | Vector distance angular is the arc cosine of the cosine similarity.                                  |
| `VectorDistanceHellingerFollowsTheStandardDefinition`                                    | Fact   | Vector distance hellinger follows the standard definition.                                           |
| `VectorDistanceBinaryMetricsMatchHandCounts`                                             | Fact   | Vector distance binary metrics match hand counts.                                                    |
| `VectorDistanceWeightedEuclideanUsesTheWeights`                                          | Fact   | Vector distance weighted euclidean uses the weights.                                                 |
| `VectorDistanceRejectsBadInputWithAnExplanation`                                         | Fact   | Vector distance rejects bad input with an explanation.                                               |
| `DistanceMatrixWithinSetIsSymmetricWithZeroDiagonal`                                     | Fact   | Distance matrix within set is symmetric with zero diagonal.                                          |
| `DistanceMatrixWithinSetHandlesSingleRowAndRejectsRaggedAndHugeInput`                    | Fact   | Distance matrix within set handles single row and rejects ragged and huge input.                     |
| `DistanceMatrixBetweenSetsIsRectangular`                                                 | Fact   | Distance matrix between sets is rectangular.                                                         |
| `DistanceMatrixBetweenSetsRejectsColumnMismatchAndEmpty`                                 | Fact   | Distance matrix between sets rejects column mismatch and empty.                                      |
| `KernelValueMatchesClosedForms`                                                          | Fact   | Kernel value matches closed forms.                                                                   |
| `KernelValueSelfSimilarityOfRadialKernelsIsOneAndBadParametersAreRejected`               | Fact   | Kernel value self similarity of radial kernels is one and bad parameters are rejected.               |
| `KernelValueEveryKernelKindEvaluatesFinitelyOnPositiveData`                              | Fact   | Kernel value every kernel kind evaluates finitely on positive data.                                  |
| `KernelValueDirichletHandlesEqualCoordinatesByTheirLimit`                                | Fact   | Kernel value dirichlet handles equal coordinates by their limit.                                     |
| `KernelGramMatrixIsSymmetricAndNormalisationGivesUnitDiagonal`                           | Fact   | Kernel gram matrix is symmetric and normalisation gives unit diagonal.                               |
| `KernelGramMatrixGaussianDiagonalIsOneAndInputIsValidated`                               | Fact   | Kernel gram matrix gaussian diagonal is one and input is validated.                                  |
| `KernelCrossMatrixIsRectangularAndMatchesTheGramWhenSetsAreEqual`                        | Fact   | Kernel cross matrix is rectangular and matches the gram when sets are equal.                         |
| `HistogramOverlapMatchesClosedForms`                                                     | Fact   | Histogram overlap matches closed forms.                                                              |
| `HistogramOverlapRejectsNegativeEmptyAndMismatchedInput`                                 | Fact   | Histogram overlap rejects negative empty and mismatched input.                                       |
| `GaussianBhattacharyyaEqualsOneEighthOfSquaredMeanShiftForEqualCovariance`               | Fact   | Gaussian bhattacharyya equals one eighth of squared mean shift for equal covariance.                 |
| `GaussianBhattacharyyaRejectsSmallOrMismatchedSamples`                                   | Fact   | Gaussian bhattacharyya rejects small or mismatched samples.                                          |
| `StringDistanceMatchesWikipediaExamples`                                                 | Fact   | String distance matches wikipedia examples.                                                          |
| `StringDistanceHandlesEmptyStringsAndRejectsBadInput`                                    | Fact   | String distance handles empty strings and rejects bad input.                                         |
| `ClosestStringMatchesAreRankedNearestFirstWithStableTies`                                | Fact   | Closest string matches are ranked nearest first with stable ties.                                    |
| `ClosestStringMatchesRejectsEmptyCandidatesAndBadTop`                                    | Fact   | Closest string matches rejects empty candidates and bad top.                                         |
| `StringDistanceMatrixIsSymmetric`                                                        | Fact   | String distance matrix is symmetric.                                                                 |
| `StringDistanceMatrixRejectsEmptyAndNullEntries`                                         | Fact   | String distance matrix rejects empty and null entries.                                               |
| `TokenSequenceEditDistanceCountsInsertionsDeletionsAndSubstitutions`                     | Fact   | Token sequence edit distance counts insertions deletions and substitutions.                          |
| `TokenSequenceEditDistanceHandlesEmptySequencesAndRejectsNull`                           | Fact   | Token sequence edit distance handles empty sequences and rejects null.                               |
| `TokenSetSimilarityMatchesHandCounts`                                                    | Fact   | Token set similarity matches hand counts.                                                            |
| `TokenSetSimilarityHandlesEmptySetsAndRejectsNull`                                       | Fact   | Token set similarity handles empty sets and rejects null.                                            |
| `SeededRandomScopeRestoresThePreviousThreadSeedAfterAClusteringCall`                     | Fact   | Seeded random scope restores the previous thread seed after a clustering call.                       |
| `KMeansSplitsTwoSquaresAndReportsHandComputedInertiaProfilesAndSilhouette`               | Fact   | K means splits two squares and reports hand computed inertia profiles and silhouette.                |
| `KMeansIsRepeatableForASeedAndReportsCentroidsInOriginalUnitsWhenStandardised`           | Fact   | K means is repeatable for a seed and reports centroids in original units when standardised.          |
| `KMeansHandlesSingleClusterAndRejectsBadArguments`                                       | Fact   | K means handles single cluster and rejects bad arguments.                                            |
| `MiniBatchKMeansSeparatesWellSeparatedBlobs`                                             | Fact   | Mini batch k means separates well separated blobs.                                                   |
| `MiniBatchKMeansRejectsBadBatchSize`                                                     | Fact   | Mini batch k means rejects bad batch size.                                                           |
| `BinarySplitSeparatesTwoSquares`                                                         | Fact   | Binary split separates two squares.                                                                  |
| `BinarySplitRejectsBadArguments`                                                         | Fact   | Binary split rejects bad arguments.                                                                  |
| `BalancedKMeansGivesEqualSizedClusters`                                                  | Fact   | Balanced k means gives equal sized clusters.                                                         |
| `BalancedKMeansRejectsTooManyRowsAndClusters`                                            | Fact   | Balanced k means rejects too many rows and clusters.                                                 |
| `KMedoidsUsesActualRowsAsCentres`                                                        | Fact   | K medoids uses actual rows as centres.                                                               |
| `KMedoidsSupportsOtherSeedingAndRejectsBadInput`                                         | Fact   | K medoids supports other seeding and rejects bad input.                                              |
| `KModesFindsCategoricalModesAndCountsMismatches`                                         | Fact   | K modes finds categorical modes and counts mismatches.                                               |
| `KModesRejectsRaggedEmptyAndTooManyClusters`                                             | Fact   | K modes rejects ragged empty and too many clusters.                                                  |
| `MeanShiftDiscoversTheNumberOfClusters`                                                  | Fact   | Mean shift discovers the number of clusters.                                                         |
| `MeanShiftSupportsGaussianKernelAndRejectsBadBandwidth`                                  | Fact   | Mean shift supports gaussian kernel and rejects bad bandwidth.                                       |
| `GaussianMixtureRecoversTwoBlobsAndReportsConsistentInformationCriteria`                 | Fact   | Gaussian mixture recovers two blobs and reports consistent information criteria.                     |
| `GaussianMixtureDiagonalCountsFewerParametersAndValidatesInput`                          | Fact   | Gaussian mixture diagonal counts fewer parameters and validates input.                               |
| `MixtureSelectionPrefersTwoComponentsForTwoBlobs`                                        | Fact   | Mixture selection prefers two components for two blobs.                                              |
| `MixtureSelectionRejectsBadRange`                                                        | Fact   | Mixture selection rejects bad range.                                                                 |
| `MixtureAnomalyDetectionFlagsAPointFarFromEveryComponent`                                | Fact   | Mixture anomaly detection flags a point far from every component.                                    |
| `MixtureAnomalyDetectionScoresReferenceRowsByDefaultAndValidatesContamination`           | Fact   | Mixture anomaly detection scores reference rows by default and validates contamination.              |
| `KMeansPlusPlusSeedsAreDistinctRepeatableRealRows`                                       | Fact   | K means plus plus seeds are distinct repeatable real rows.                                           |
| `KMeansPlusPlusSeedsRejectBadCount`                                                      | Fact   | K means plus plus seeds reject bad count.                                                            |
| `SilhouetteMatchesHandComputationForTwoSquares`                                          | Fact   | Silhouette matches hand computation for two squares.                                                 |
| `SilhouetteScoresSingletonsZeroAndFlagsMisassignedRowsNegative`                          | Fact   | Silhouette scores singletons zero and flags misassigned rows negative.                               |
| `DaviesBouldinIsOneTenthForTwoSquares`                                                   | Fact   | Davies bouldin is one tenth for two squares.                                                         |
| `DaviesBouldinIsWorseForAScrambledLabelling`                                             | Fact   | Davies bouldin is worse for a scrambled labelling.                                                   |
| `CalinskiHarabaszIsSixHundredForTwoSquares`                                              | Fact   | Calinski harabasz is six hundred for two squares.                                                    |
| `CalinskiHarabaszIsInfiniteWhenClustersAreDegenerateAndNanWhenEveryRowIsAlone`           | Fact   | Calinski harabasz is infinite when clusters are degenerate and nan when every row is alone.          |
| `QualitySummaryCombinesTheHandComputedMeasures`                                          | Fact   | Quality summary combines the hand computed measures.                                                 |
| `QualitySummaryRejectsSingleClusterAndMismatchedLabels`                                  | Fact   | Quality summary rejects single cluster and mismatched labels.                                        |
| `ElbowTableIsMonotoneInInertiaAndAllMeasuresPreferTwoForTwoSquares`                      | Fact   | Elbow table is monotone in inertia and all measures prefer two for two squares.                      |
| `ElbowTableRejectsBadRanges`                                                             | Fact   | Elbow table rejects bad ranges.                                                                      |
| `GapStatisticFindsTwoClustersInTwoBlobsAndIsRepeatable`                                  | Fact   | Gap statistic finds two clusters in two blobs and is repeatable.                                     |
| `GapStatisticRejectsBadArguments`                                                        | Fact   | Gap statistic rejects bad arguments.                                                                 |
| `ProfilingReportsCentroidsSpreadAndStandardisedDifferences`                              | Fact   | Profiling reports centroids spread and standardised differences.                                     |
| `ProfilingHandlesConstantColumnsSingletonsAndBadNames`                                   | Fact   | Profiling handles constant columns singletons and bad names.                                         |
| `AssignmentPicksTheNearestCentroidAndReportsAllDistances`                                | Fact   | Assignment picks the nearest centroid and reports all distances.                                     |
| `AssignmentRejectsMismatchedColumnsAndEmptyCentroids`                                    | Fact   | Assignment rejects mismatched columns and empty centroids.                                           |
| `ClusterAgreementMatchesHandCountedPairs`                                                | Fact   | Cluster agreement matches hand counted pairs.                                                        |
| `ClusterAgreementHandlesTrivialPartitionsAndRejectsBadInput`                             | Fact   | Cluster agreement handles trivial partitions and rejects bad input.                                  |
| `KNearestNeighboursAgreeAcrossEveryIndexForAQuery`                                       | Theory | K nearest neighbours agree across every index for a query.                                           |
| `KNearestNeighboursExcludeTheRowItselfWhenQueriesAreTheReference`                        | Theory | K nearest neighbours exclude the row itself when queries are the reference.                          |
| `KNearestNeighboursHonourTheMetricAndMatchBruteForceOnRandomData`                        | Fact   | K nearest neighbours honour the metric and match brute force on random data.                         |
| `KNearestNeighboursRejectBadArguments`                                                   | Fact   | K nearest neighbours reject bad arguments.                                                           |
| `RadiusSearchReturnsEveryRowWithinTheRadiusNearestFirst`                                 | Fact   | Radius search returns every row within the radius nearest first.                                     |
| `RadiusSearchRejectsBadRadiusAndCap`                                                     | Fact   | Radius search rejects bad radius and cap.                                                            |
| `NeighbourClassificationVotesAmongTheNearestLabelledRows`                                | Fact   | Neighbour classification votes among the nearest labelled rows.                                      |
| `NeighbourClassificationRejectsBadArguments`                                             | Fact   | Neighbour classification rejects bad arguments.                                                      |
| `NearestNeighbourOutliersFlagOnlyTheIsolatedRow`                                         | Fact   | Nearest neighbour outliers flag only the isolated row.                                               |
| `NearestNeighbourOutliersRejectBadArguments`                                             | Fact   | Nearest neighbour outliers reject bad arguments.                                                     |
| `AssociationRulesReproduceTheTextbookMarketBasketExample`                                | Fact   | Association rules reproduce the textbook market basket example.                                      |
| `AssociationRulesRespectConfidenceAndCapsAndIgnoreRepeatedItems`                         | Fact   | Association rules respect confidence and caps and ignore repeated items.                             |
| `AssociationRulesRejectBadInputAndUnboundedSizes`                                        | Fact   | Association rules reject bad input and unbounded sizes.                                              |
| `PrincipalComponentAnalysisReproducesTheSmithTutorialEigenAnalysis`                      | Fact   | Principal component analysis reproduces the smith tutorial eigen analysis.                           |
| `PrincipalComponentAnalysisScoresAreProjectionsWithNormalisedSign`                       | Fact   | Principal component analysis scores are projections with normalised sign.                            |
| `PrincipalComponentAnalysisOfPerfectlyCorrelatedDataHasOneComponent`                     | Fact   | Principal component analysis of perfectly correlated data has one component.                         |
| `PrincipalComponentAnalysisOnTheCorrelationBasisIgnoresColumnScale`                      | Fact   | Principal component analysis on the correlation basis ignores column scale.                          |
| `PrincipalComponentAnalysisRejectsBadInputWithExplanations`                              | Fact   | Principal component analysis rejects bad input with explanations.                                    |
| `InterpretPrincipalComponentsListsLargestLoadingsAndCommunalities`                       | Fact   | Interpret principal components lists largest loadings and communalities.                             |
| `InterpretPrincipalComponentsDefaultsNamesAndRejectsBadNames`                            | Fact   | Interpret principal components defaults names and rejects bad names.                                 |
| `ProjectingNewRowsUsesTheReferenceMeansAndComponents`                                    | Fact   | Projecting new rows uses the reference means and components.                                         |
| `ProjectingNewRowsRejectsColumnMismatchAndNan`                                           | Fact   | Projecting new rows rejects column mismatch and nan.                                                 |
| `WhiteningGivesUncorrelatedUnitVarianceColumns`                                          | Fact   | Whitening gives uncorrelated unit variance columns.                                                  |
| `WhiteningDropsNegligibleComponentsOfCollinearData`                                      | Fact   | Whitening drops negligible components of collinear data.                                             |
| `DiagnosticsMatchHandComputedHotellingAndPredictionError`                                | Fact   | Diagnostics match hand computed hotelling and prediction error.                                      |
| `DiagnosticsFlagAFarAwayRowAndValidateInput`                                             | Fact   | Diagnostics flag a far away row and validate input.                                                  |
| `ReconstructionFromOneComponentMatchesHandProjectionAndIsExactForCollinearData`          | Fact   | Reconstruction from one component matches hand projection and is exact for collinear data.           |
| `ReconstructionScoresNewRowsAndRejectsBadInput`                                          | Fact   | Reconstruction scores new rows and rejects bad input.                                                |
| `KernelPrincipalComponentAnalysisWithLinearKernelMatchesOrdinaryScores`                  | Fact   | Kernel principal component analysis with linear kernel matches ordinary scores.                      |
| `KernelPrincipalComponentAnalysisWithGaussianKernelSeparatesTwoBlobsOnTheFirstComponent` | Fact   | Kernel principal component analysis with gaussian kernel separates two blobs on the first component. |
| `KernelPrincipalComponentAnalysisRejectsBadKernelParametersAndSize`                      | Fact   | Kernel principal component analysis rejects bad kernel parameters and size.                          |
| `IndependentComponentAnalysisRecoversMixedUniformSources`                                | Fact   | Independent component analysis recovers mixed uniform sources.                                       |
| `IndependentComponentAnalysisIsRepeatableForAFixedSeedAndValidatesInput`                 | Fact   | Independent component analysis is repeatable for a fixed seed and validates input.                   |
| `LinearDiscriminantAnalysisSeparatesTwoSymmetricClasses`                                 | Fact   | Linear discriminant analysis separates two symmetric classes.                                        |
| `LinearDiscriminantAnalysisWithThreeClassesReturnsTwoDirections`                         | Fact   | Linear discriminant analysis with three classes returns two directions.                              |
| `LinearDiscriminantAnalysisRejectsBadLabels`                                             | Fact   | Linear discriminant analysis rejects bad labels.                                                     |
| `KernelDiscriminantAnalysisSeparatesTwoBlobs`                                            | Fact   | Kernel discriminant analysis separates two blobs.                                                    |
| `KernelDiscriminantAnalysisValidatesRegularisationAndLabels`                             | Fact   | Kernel discriminant analysis validates regularisation and labels.                                    |
| `LinearDiscriminantClassificationAssignsToTheNearestProjectedClassMean`                  | Fact   | Linear discriminant classification assigns to the nearest projected class mean.                      |
| `LinearDiscriminantClassificationRejectsMismatchedColumns`                               | Fact   | Linear discriminant classification rejects mismatched columns.                                       |
| `MahalanobisDistancesMatchHandComputationOnTheUnitCross`                                 | Fact   | Mahalanobis distances match hand computation on the unit cross.                                      |
| `MahalanobisOutlierDetectionFlagsAnInjectedOutlierAndUsesPseudoInverseForCollinearData`  | Fact   | Mahalanobis outlier detection flags an injected outlier and uses pseudo inverse for collinear data.  |
| `ScoringAgainstReferenceUsesOnlyTheReferenceCentreAndCovariance`                         | Fact   | Scoring against reference uses only the reference centre and covariance.                             |
| `ScoringAgainstReferenceRejectsMismatchAndTinyReference`                                 | Fact   | Scoring against reference rejects mismatch and tiny reference.                                       |
| `TsneKeepsTwoSeparatedClustersApartAndIsRepeatableForASeed`                              | Fact   | Tsne keeps two separated clusters apart and is repeatable for a seed.                                |
| `TsneSupportsThreeDimensionsAndRejectsBadInput`                                          | Fact   | Tsne supports three dimensions and rejects bad input.                                                |

### NumericsAndOptimisationCalculusAndRootFindingToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.NumericsAndOptimisationCalculusAndRootFindingToolsTests` — 22 tests.

| Test                                                                | Kind | Verifies                                                                     |
|---------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `EvaluateNamedFunctionEveryFamily`                                  | Fact | Evaluate named function every family.                                        |
| `IntegrateTrapezoidalRuleKnownIntegrals`                            | Fact | Integrate trapezoidal rule known integrals.                                  |
| `IntegrateRombergKnownIntegrals`                                    | Fact | Integrate romberg known integrals.                                           |
| `IntegrateGaussKronrodKnownIntegralsAndErrorEstimate`               | Fact | Integrate gauss kronrod known integrals and error estimate.                  |
| `IntegrateAdaptiveGaussKronrodInfiniteKnownIntegrals`               | Fact | Integrate adaptive gauss kronrod infinite known integrals.                   |
| `IntegrateMonteCarloOnIntervalIsWithinStandardErrorAndReproducible` | Fact | Integrate monte carlo on interval is within standard error and reproducible. |
| `IntegrateMonteCarloObjectiveOverBoxKnownIntegral`                  | Fact | Integrate monte carlo objective over box known integral.                     |
| `NumericalDerivativeOfNamedFunctionKnownDerivatives`                | Fact | Numerical derivative of named function known derivatives.                    |
| `NumericalGradientOfObjectiveKnownGradients`                        | Fact | Numerical gradient of objective known gradients.                             |
| `NumericalHessianOfObjectiveKnownHessians`                          | Fact | Numerical hessian of objective known hessians.                               |
| `NumericalJacobianOfVectorFunctionKnownJacobians`                   | Fact | Numerical jacobian of vector function known jacobians.                       |
| `FindRootBrentKnownRoots`                                           | Fact | Find root brent known roots.                                                 |
| `FindRootBracketsByScanningFindsSineZeros`                          | Fact | Find root brackets by scanning finds sine zeros.                             |
| `FindExtremumBrentKnownExtrema`                                     | Fact | Find extremum brent known extrema.                                           |
| `EvaluatePolynomialHorner`                                          | Fact | Evaluate polynomial horner.                                                  |
| `PolynomialDerivativeOrAntiderivativeKnownAnswers`                  | Fact | Polynomial derivative or antiderivative known answers.                       |
| `PolynomialMultiplyKnownProduct`                                    | Fact | Polynomial multiply known product.                                           |
| `PolynomialRootsKnownRoots`                                         | Fact | Polynomial roots known roots.                                                |
| `FitPolynomialByLeastSquaresKnownFits`                              | Fact | Fit polynomial by least squares known fits.                                  |
| `InterpolatePolynomialThroughPointsRecoversQuadratic`               | Fact | Interpolate polynomial through points recovers quadratic.                    |
| `InterpolateLinearlyKnownValues`                                    | Fact | Interpolate linearly known values.                                           |
| `ExtrapolateLinearlyExtendsEndSegments`                             | Fact | Extrapolate linearly extends end segments.                                   |

### NumericsAndOptimisationCombinatoricsAndUtilityToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.NumericsAndOptimisationCombinatoricsAndUtilityToolsTests` — 27 tests.

| Test                                                      | Kind | Verifies                                                         |
|-----------------------------------------------------------|------|------------------------------------------------------------------|
| `EnumeratePermutationsListsEveryOrdering`                 | Fact | Enumerate permutations lists every ordering.                     |
| `EnumerateCombinationsListsEverySelection`                | Fact | Enumerate combinations lists every selection.                    |
| `EnumerateSubsetsListsPowerSet`                           | Fact | Enumerate subsets lists power set.                               |
| `EnumerateSequencesListsCartesianPower`                   | Fact | Enumerate sequences lists cartesian power.                       |
| `GenerateTruthTableEnumeratesAllBooleanCombinations`      | Fact | Generate truth table enumerates all boolean combinations.        |
| `CountCombinatorialObjectsKnownCounts`                    | Fact | Count combinatorial objects known counts.                        |
| `GreatestCommonDivisorAndLeastCommonMultipleKnownValues`  | Fact | Greatest common divisor and least common multiple known values.  |
| `EuclideanModuloWrapsIntoRange`                           | Fact | Euclidean modulo wraps into range.                               |
| `PowerOfTwoPropertiesKnownValues`                         | Fact | Power of two properties known values.                            |
| `FindIntegerByBinarySearchFindsSquareRoot`                | Fact | Find integer by binary search finds square root.                 |
| `SortValuesWithOriginalIndicesIsStableAndReportsOrigin`   | Fact | Sort values with original indices is stable and reports origin.  |
| `SortValuesByKeysReordersInParallel`                      | Fact | Sort values by keys reorders in parallel.                        |
| `SelectNthSmallestValueKnownRanks`                        | Fact | Select nth smallest value known ranks.                           |
| `SelectSmallestValuesSortedKnownTopN`                     | Fact | Select smallest values sorted known top n.                       |
| `GenerateUniformRandomNumbersIsReproducibleAndInRange`    | Fact | Generate uniform random numbers is reproducible and in range.    |
| `GenerateNormalRandomNumbersHaveRequestedMoments`         | Fact | Generate normal random numbers have requested moments.           |
| `GenerateExponentialRandomNumbersHaveRequestedMean`       | Fact | Generate exponential random numbers have requested mean.         |
| `MonteCarloExpectationOfNamedFunctionMatchesClosedForms`  | Fact | Monte carlo expectation of named function matches closed forms.  |
| `MonteCarloProbabilityOfNamedFunctionMatchesNormalTables` | Fact | Monte carlo probability of named function matches normal tables. |
| `GetMathematicalConstantsHasKnownValues`                  | Fact | Get mathematical constants has known values.                     |
| `NumericsFastFourierTransformKnownTransformAndRoundTrip`  | Fact | Numerics fast fourier transform known transform and round trip.  |
| `NumericsDiscreteCosineTransformKnownValuesAndRoundTrip`  | Fact | Numerics discrete cosine transform known values and round trip.  |
| `NumericsDiscreteSineTransformRoundTrip`                  | Fact | Numerics discrete sine transform round trip.                     |
| `NumericsHartleyTransformKnownValuesAndSelfInverse`       | Fact | Numerics hartley transform known values and self inverse.        |
| `NumericsHilbertTransformOfCosineIsSine`                  | Fact | Numerics hilbert transform of cosine is sine.                    |
| `NumericsHaarWaveletTransformKnownValuesAndRoundTrip`     | Fact | Numerics haar wavelet transform known values and round trip.     |
| `NumericsCdf97WaveletTransformRoundTripAndSmoothness`     | Fact | Numerics cdf97 wavelet transform round trip and smoothness.      |

### NumericsAndOptimisationLinearAlgebraToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.NumericsAndOptimisationLinearAlgebraToolsTests` — 28 tests.

| Test                                                         | Kind | Verifies                                                             |
|--------------------------------------------------------------|------|----------------------------------------------------------------------|
| `MatrixMultiplyKnownProductAndShapeChecks`                   | Fact | Matrix multiply known product and shape checks.                      |
| `MatrixVectorMultiplyKnownProduct`                           | Fact | Matrix vector multiply known product.                                |
| `MatrixTransposeSwapsRowsAndColumns`                         | Fact | Matrix transpose swaps rows and columns.                             |
| `MatrixElementwiseOperationAllOperations`                    | Fact | Matrix elementwise operation all operations.                         |
| `MatrixInverseKnownAnswerAndSingularRejected`                | Fact | Matrix inverse known answer and singular rejected.                   |
| `MatrixPseudoInverseMatchesPublishedMoorePenroseExample`     | Fact | Matrix pseudo inverse matches published moore penrose example.       |
| `MatrixDeterminantKnownAnswers`                              | Fact | Matrix determinant known answers.                                    |
| `MatrixPseudoDeterminantIsProductOfNonZeroSingularValues`    | Fact | Matrix pseudo determinant is product of non zero singular values.    |
| `MatrixTraceSumsDiagonal`                                    | Fact | Matrix trace sums diagonal.                                          |
| `MatrixRankAndConditionKnownAnswers`                         | Fact | Matrix rank and condition known answers.                             |
| `MatrixNormsMatchDefinitions`                                | Fact | Matrix norms match definitions.                                      |
| `VectorNormsThreeFourFive`                                   | Fact | Vector norms three four five.                                        |
| `SolveLinearSystemKnownSolution`                             | Fact | Solve linear system known solution.                                  |
| `SolveLinearSystemMultipleRightHandSidesKnownSolution`       | Fact | Solve linear system multiple right hand sides known solution.        |
| `SolveLeastSquaresFitsLineExactAndApproximate`               | Fact | Solve least squares fits line exact and approximate.                 |
| `MatrixNullSpaceVectorsAreOrthonormalAndAnnihilateTheMatrix` | Fact | Matrix null space vectors are orthonormal and annihilate the matrix. |
| `MatrixReducedRowEchelonFormKnownAnswers`                    | Fact | Matrix reduced row echelon form known answers.                       |
| `MatrixKroneckerProductMatchesReferenceExample`              | Fact | Matrix kronecker product matches reference example.                  |
| `VectorOuterProductKnownAnswer`                              | Fact | Vector outer product known answer.                                   |
| `VectorDotProductKnownAnswerAndLengthMismatch`               | Fact | Vector dot product known answer and length mismatch.                 |
| `VectorCrossProductKnownAnswers`                             | Fact | Vector cross product known answers.                                  |
| `MatrixStructurePropertiesClassifiesMatrices`                | Fact | Matrix structure properties classifies matrices.                     |
| `GenerateStructuredMatrixKinds`                              | Fact | Generate structured matrix kinds.                                    |
| `MatrixPowerGivesFibonacciNumbers`                           | Fact | Matrix power gives fibonacci numbers.                                |
| `GenerateMeshGridEnumeratesPoints`                           | Fact | Generate mesh grid enumerates points.                                |
| `GenerateEvenlySpacedValuesIncludesBothEnds`                 | Fact | Generate evenly spaced values includes both ends.                    |
| `NumericsConvolveVectorsKnownConvolution`                    | Fact | Numerics convolve vectors known convolution.                         |
| `RescaleValuesToRangeMapsLinearly`                           | Fact | Rescale values to range maps linearly.                               |

### NumericsAndOptimisationMatrixDecompositionToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.NumericsAndOptimisationMatrixDecompositionToolsTests` — 10 tests.

| Test                                                                        | Kind | Verifies                                                                             |
|-----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `LuDecompositionReconstructsPermutedInput`                                  | Fact | Lu decomposition reconstructs permuted input.                                        |
| `QrDecompositionReconstructsAndIsOrthonormal`                               | Fact | Qr decomposition reconstructs and is orthonormal.                                    |
| `CholeskyDecompositionMatchesPublishedExample`                              | Fact | Cholesky decomposition matches published example.                                    |
| `SolveSymmetricPositiveDefiniteSystemKnownSolution`                         | Fact | Solve symmetric positive definite system known solution.                             |
| `SingularValueDecompositionMatchesPublishedValuesAndReconstructs`           | Fact | Singular value decomposition matches published values and reconstructs.              |
| `SymmetricEigenDecompositionKnownEigenpairs`                                | Fact | Symmetric eigen decomposition known eigenpairs.                                      |
| `GeneralEigenDecompositionRealAndComplexCases`                              | Fact | General eigen decomposition real and complex cases.                                  |
| `GeneralizedEigenDecompositionScalesWithB`                                  | Fact | Generalized eigen decomposition scales with b.                                       |
| `GramSchmidtOrthogonalisationReconstructs`                                  | Fact | Gram schmidt orthogonalisation reconstructs.                                         |
| `NonnegativeMatrixFactorisationRecoversRankOneMatrixAndIsSeedDeterministic` | Fact | Nonnegative matrix factorisation recovers rank one matrix and is seed deterministic. |

### NumericsAndOptimisationOptimisationToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.NumericsAndOptimisationOptimisationToolsTests` — 13 tests.

| Test                                                                     | Kind | Verifies                                                                           |
|--------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `EvaluateNamedObjectiveKnownValuesAtKnownMinima`                         | Fact | Evaluate named objective known values at known minima.                             |
| `MinimiseWithLimitedMemoryBfgsFindsKnownMinima`                          | Fact | Minimise with limited memory bfgs finds known minima.                              |
| `MinimiseWithBoundedBfgsRespectsBounds`                                  | Fact | Minimise with bounded bfgs respects bounds.                                        |
| `MinimiseWithConjugateGradientFindsKnownMinima`                          | Fact | Minimise with conjugate gradient finds known minima.                               |
| `MinimiseWithGradientDescentConvergesOnConvexProblemsAndRejectsBadRates` | Fact | Minimise with gradient descent converges on convex problems and rejects bad rates. |
| `MinimiseWithNelderMeadFindsKnownMinimaDerivativeFree`                   | Fact | Minimise with nelder mead finds known minima derivative free.                      |
| `MinimiseWithSubplexFindsKnownMinima`                                    | Fact | Minimise with subplex finds known minima.                                          |
| `CompareUnconstrainedOptimisersAgreeOnConvexProblem`                     | Fact | Compare unconstrained optimisers agree on convex problem.                          |
| `SolveQuadraticProgramMatchesTextbookExamples`                           | Fact | Solve quadratic program matches textbook examples.                                 |
| `MinimiseWithLinearConstraintsCobylaMatchesQuadraticProgram`             | Fact | Minimise with linear constraints cobyla matches quadratic program.                 |
| `SolveLinearProgramMatchesTextbookExamples`                              | Fact | Solve linear program matches textbook examples.                                    |
| `FitNamedCurveLeastSquaresRecoversKnownCurves`                           | Fact | Fit named curve least squares recovers known curves.                               |
| `SolveAssignmentProblemMunkresKnownAssignments`                          | Fact | Solve assignment problem munkres known assignments.                                |

### NumericsAndOptimisationSpecialFunctionToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.NumericsAndOptimisationSpecialFunctionToolsTests` — 32 tests.

| Test                                                 | Kind | Verifies                                                   |
|------------------------------------------------------|------|------------------------------------------------------------|
| `GammaFunctionKnownValues`                           | Fact | Gamma function known values.                               |
| `LogGammaFunctionKnownValuesAndLargeArguments`       | Fact | Log gamma function known values and large arguments.       |
| `DigammaFunctionKnownValues`                         | Fact | Digamma function known values.                             |
| `TrigammaFunctionKnownValues`                        | Fact | Trigamma function known values.                            |
| `MultivariateGammaFunctionMatchesClosedForm`         | Fact | Multivariate gamma function matches closed form.           |
| `RegularisedLowerIncompleteGammaClosedForms`         | Fact | Regularised lower incomplete gamma closed forms.           |
| `RegularisedUpperIncompleteGammaClosedForms`         | Fact | Regularised upper incomplete gamma closed forms.           |
| `InverseRegularisedIncompleteGammaInvertsClosedForm` | Fact | Inverse regularised incomplete gamma inverts closed form.  |
| `BetaFunctionKnownValues`                            | Fact | Beta function known values.                                |
| `MultinomialBetaFunctionReducesToBeta`               | Fact | Multinomial beta function reduces to beta.                 |
| `RegularisedIncompleteBetaClosedForms`               | Fact | Regularised incomplete beta closed forms.                  |
| `InverseRegularisedIncompleteBetaInvertsClosedForms` | Fact | Inverse regularised incomplete beta inverts closed forms.  |
| `ErrorFunctionFamilyKnownValues`                     | Fact | Error function family known values.                        |
| `StandardNormalFunctionKnownValues`                  | Fact | Standard normal function known values.                     |
| `BivariateNormalCdfKnownValues`                      | Fact | Bivariate normal cdf known values.                         |
| `OwensTFunctionKnownValues`                          | Fact | Owens t function known values.                             |
| `BesselFunctionKnownValues`                          | Fact | Bessel function known values.                              |
| `FactorialFunctionKnownValuesAndBounds`              | Fact | Factorial function known values and bounds.                |
| `BinomialCoefficientKnownValues`                     | Fact | Binomial coefficient known values.                         |
| `LogSumExpIsStable`                                  | Fact | Log sum exp is stable.                                     |
| `SoftmaxProbabilitiesSumToOneAndMatchKnownValues`    | Fact | Softmax probabilities sum to one and match known values.   |
| `SigmoidAndLogitInvertEachOther`                     | Fact | Sigmoid and logit invert each other.                       |
| `StableLogExpFunctionsKeepPrecisionNearZero`         | Fact | Stable log exp functions keep precision near zero.         |
| `LogAddAndSubtractPairsKnownValues`                  | Fact | Log add and subtract pairs known values.                   |
| `ReciprocalAndInverseHyperbolicFunctionsKnownValues` | Fact | Reciprocal and inverse hyperbolic functions known values.  |
| `StirlingGammaApproximationIsCloseForLargeArguments` | Fact | Stirling gamma approximation is close for large arguments. |
| `GaussianBellFunctionMatchesNormalDensity`           | Fact | Gaussian bell function matches normal density.             |
| `GaussianKernelWeightsAreSymmetricAndPeakAtCentre`   | Fact | Gaussian kernel weights are symmetric and peak at centre.  |
| `TaylorSeriesApproximationConvergesToTrueValues`     | Fact | Taylor series approximation converges to true values.      |
| `BSplineBasisFunctionKnownValues`                    | Fact | B spline basis function known values.                      |
| `StableHypotenuseHandlesHugeMagnitudes`              | Fact | Stable hypotenuse handles huge magnitudes.                 |
| `PlanarAngleKnownQuadrants`                          | Fact | Planar angle known quadrants.                              |

### RelationshipsAndRegressionCorrelationTests

`Jube.Test.AdvancedAnalytics.Statistics.RelationshipsAndRegressionCorrelationTests` — 28 tests.

| Test                                                                         | Kind | Verifies                                                                                 |
|------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `PearsonMatchesAnscombeQuartetOne`                                           | Fact | Pearson matches anscombe quartet one.                                                    |
| `PearsonEdgeCases`                                                           | Fact | Pearson edge cases.                                                                      |
| `SpearmanMatchesTheClosedFormOnAnscombeQuartetOne`                           | Fact | Spearman matches the closed form on anscombe quartet one.                                |
| `SpearmanIsInvariantToMonotoneTransformsAndHandlesTies`                      | Fact | Spearman is invariant to monotone transforms and handles ties.                           |
| `KendallCountsMatchTheHandCountOnAnscombeQuartetOne`                         | Fact | Kendall counts match the hand count on anscombe quartet one.                             |
| `KendallHandlesTiesAndVariants`                                              | Fact | Kendall handles ties and variants.                                                       |
| `PointBiserialMatchesTheHandComputation`                                     | Fact | Point biserial matches the hand computation.                                             |
| `PointBiserialRejectsBadFlagsAndMissing`                                     | Fact | Point biserial rejects bad flags and missing.                                            |
| `BiserialMatchesTheHandComputation`                                          | Fact | Biserial matches the hand computation.                                                   |
| `CorrelationMatrixAgreesWithThePairwiseToolsAndRanksPairs`                   | Fact | Correlation matrix agrees with the pairwise tools and ranks pairs.                       |
| `CorrelationMatrixHandlesMissingAndConstantColumns`                          | Fact | Correlation matrix handles missing and constant columns.                                 |
| `CovarianceMatrixMatchesTheClosedForm`                                       | Fact | Covariance matrix matches the closed form.                                               |
| `CovarianceMatrixEdgeCases`                                                  | Fact | Covariance matrix edge cases.                                                            |
| `PartialCorrelationMatchesTheFirstOrderFormula`                              | Fact | Partial correlation matches the first order formula.                                     |
| `PartialCorrelationEdgeCases`                                                | Fact | Partial correlation edge cases.                                                          |
| `SemiPartialCorrelationMatchesTheFirstOrderFormula`                          | Fact | Semi partial correlation matches the first order formula.                                |
| `PartialCorrelationMatrixMatchesThePartialCorrelationTool`                   | Fact | Partial correlation matrix matches the partial correlation tool.                         |
| `PartialCorrelationMatrixRejectsCollinearAndSmallInputs`                     | Fact | Partial correlation matrix rejects collinear and small inputs.                           |
| `MutualInformationOfAFunctionalNonLinearRelationEqualsTheEntropyOfTheOutput` | Fact | Mutual information of a functional non linear relation equals the entropy of the output. |
| `MutualInformationEdgeCases`                                                 | Fact | Mutual information edge cases.                                                           |
| `MutualInformationRankingPutsTheNonLinearDriverFirst`                        | Fact | Mutual information ranking puts the non linear driver first.                             |
| `CorrelationRankingOrdersByAbsoluteStrengthAndAdjustsPValues`                | Fact | Correlation ranking orders by absolute strength and adjusts p values.                    |
| `CorrelationRatioMatchesTheOneWayAnovaClosedForm`                            | Fact | Correlation ratio matches the one way anova closed form.                                 |
| `CorrelationRatioEdgeCases`                                                  | Fact | Correlation ratio edge cases.                                                            |
| `CompareIndependentCorrelationsMatchesTheFisherClosedForm`                   | Fact | Compare independent correlations matches the fisher closed form.                         |
| `CompareIndependentCorrelationsRejectsBadInput`                              | Fact | Compare independent correlations rejects bad input.                                      |
| `CorrelationSummaryIntervalMatchesTheCriticalValueTable`                     | Fact | Correlation summary interval matches the critical value table.                           |
| `CorrelationSummaryIntervalRejectsBadInput`                                  | Fact | Correlation summary interval rejects bad input.                                          |

### RelationshipsAndRegressionCurveTests

`Jube.Test.AdvancedAnalytics.Statistics.RelationshipsAndRegressionCurveTests` — 7 tests.

| Test                                                          | Kind   | Verifies                                                              |
|---------------------------------------------------------------|--------|-----------------------------------------------------------------------|
| `EveryFamilyRecoversItsGeneratingParametersFromNoiseFreeData` | Theory | Every family recovers its generating parameters from noise free data. |
| `MichaelisMentenMatchesThePublishedPuromycinFit`              | Fact   | Michaelis menten matches the published puromycin fit.                 |
| `PredictionsUseTheDeltaMethod`                                | Fact   | Predictions use the delta method.                                     |
| `FitValidatesItsInputs`                                       | Fact   | Fit validates its inputs.                                             |
| `FitWarnsWhenParametersAreNotIdentifiable`                    | Fact   | Fit warns when parameters are not identifiable.                       |
| `ComparisonRanksTheSaturatingFamiliesAheadOfUnsuitableOnes`   | Fact   | Comparison ranks the saturating families ahead of unsuitable ones.    |
| `ComparisonReportsFamiliesThatCannotBeFittedAndValidates`     | Fact   | Comparison reports families that cannot be fitted and validates.      |

### RelationshipsAndRegressionDiagnosticsTests

`Jube.Test.AdvancedAnalytics.Statistics.RelationshipsAndRegressionDiagnosticsTests` — 28 tests.

| Test                                                                 | Kind | Verifies                                                                     |
|----------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `InfluenceMeasuresMatchTheSimpleRegressionClosedForms`               | Fact | Influence measures match the simple regression closed forms.                 |
| `InfluenceDfbetasAndPressMatchBruteForceLeaveOneOutRefits`           | Fact | Influence dfbetas and press match brute force leave one out refits.          |
| `InfluenceFlagsTheAnscombeOutlierAndValidates`                       | Fact | Influence flags the anscombe outlier and validates.                          |
| `DurbinWatsonMatchesTheHandComputedResiduals`                        | Fact | Durbin watson matches the hand computed residuals.                           |
| `DurbinWatsonSeparatesAlternatingFromSmoothResiduals`                | Fact | Durbin watson separates alternating from smooth residuals.                   |
| `BreuschGodfreyMatchesTheAuxiliaryRegressionRecomputedByAnotherTool` | Fact | Breusch godfrey matches the auxiliary regression recomputed by another tool. |
| `BreuschGodfreyAcceptsIndependentResidualsAndValidates`              | Fact | Breusch godfrey accepts independent residuals and validates.                 |
| `BreuschPaganMatchesTheAuxiliaryRegressionForBothVariants`           | Fact | Breusch pagan matches the auxiliary regression for both variants.            |
| `BreuschPaganAcceptsConstantVarianceAndValidates`                    | Fact | Breusch pagan accepts constant variance and validates.                       |
| `WhiteMatchesTheAuxiliaryRegressionWithSquaresAndCrossProducts`      | Fact | White matches the auxiliary regression with squares and cross products.      |
| `WhiteRejectsUnusableInput`                                          | Fact | White rejects unusable input.                                                |
| `GoldfeldQuandtMatchesTheHandComputedVarianceRatio`                  | Fact | Goldfeld quandt matches the hand computed variance ratio.                    |
| `GoldfeldQuandtSortsByTheChosenVariableAndValidates`                 | Fact | Goldfeld quandt sorts by the chosen variable and validates.                  |
| `ResidualNormalityMatchesHandComputedMoments`                        | Fact | Residual normality matches hand computed moments.                            |
| `ResidualNormalityDistinguishesNormalFromSkewedResiduals`            | Fact | Residual normality distinguishes normal from skewed residuals.               |
| `ResetMatchesTheNestedFTestOnFittedPowers`                           | Fact | Reset matches the nested f test on fitted powers.                            |
| `ResetAcceptsALinearModelAndValidates`                               | Fact | Reset accepts a linear model and validates.                                  |
| `ChowMatchesTheHandComputedSegmentResiduals`                         | Fact | Chow matches the hand computed segment residuals.                            |
| `ChowDetectsABreakAndValidates`                                      | Fact | Chow detects a break and validates.                                          |
| `VarianceInflationMatchesTheTwoPredictorClosedFormAndLongley`        | Fact | Variance inflation matches the two predictor closed form and longley.        |
| `VarianceInflationHandlesSingleExactlyCollinearAndConstantColumns`   | Fact | Variance inflation handles single exactly collinear and constant columns.    |
| `ConditionIndicesMatchTheTwoColumnClosedForm`                        | Fact | Condition indices match the two column closed form.                          |
| `ConditionIndicesForOrthogonalAndLongleyDesigns`                     | Fact | Condition indices for orthogonal and longley designs.                        |
| `ConditionDiagnosticsValidates`                                      | Fact | Condition diagnostics validates.                                             |
| `AddedVariablePlotSlopeEqualsTheMultipleRegressionCoefficient`       | Fact | Added variable plot slope equals the multiple regression coefficient.        |
| `AddedVariablePlotValidates`                                         | Fact | Added variable plot validates.                                               |
| `AssumptionCheckPassesAWellBehavedModelAndFlagsAProblemModel`        | Fact | Assumption check passes a well behaved model and flags a problem model.      |
| `AssumptionCheckReportsUncomputableChecksAndValidates`               | Fact | Assumption check reports uncomputable checks and validates.                  |

### RelationshipsAndRegressionGeneralisedLinearTests

`Jube.Test.AdvancedAnalytics.Statistics.RelationshipsAndRegressionGeneralisedLinearTests` — 14 tests.

| Test                                                             | Kind | Verifies                                                                  |
|------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `PoissonMatchesTheDobsonRandomisedTrialFromTheRGlmDocumentation` | Fact | Poisson matches the dobson randomised trial from the r glm documentation. |
| `GammaWithInverseLinkMatchesTheMcCullaghNelderClottingData`      | Fact | Gamma with inverse link matches the mc cullagh nelder clotting data.      |
| `GaussianIdentityEqualsOrdinaryLeastSquares`                     | Fact | Gaussian identity equals ordinary least squares.                          |
| `BinomialLogitMatchesTheBlissBeetleCoefficients`                 | Fact | Binomial logit matches the bliss beetle coefficients.                     |
| `GlmOptionsOffsetsWeightsDispersionAndValidation`                | Fact | Glm options offsets weights dispersion and validation.                    |
| `PerfectSeparationIsReportedNotHidden`                           | Fact | Perfect separation is reported not hidden.                                |
| `PoissonRegressionMatchesTheDobsonFitAndHandlesExposure`         | Fact | Poisson regression matches the dobson fit and handles exposure.           |
| `GammaRegressionMatchesTheClottingDataAndLogLinkIsPositive`      | Fact | Gamma regression matches the clotting data and log link is positive.      |
| `ProbitMatchesTheBlissBeetleCoefficientsAndTheGenericFit`        | Fact | Probit matches the bliss beetle coefficients and the generic fit.         |
| `ComplementaryLogLogMatchesTheBlissBeetleCoefficients`           | Fact | Complementary log log matches the bliss beetle coefficients.              |
| `PredictionUsesTheInverseLinkAndTheLinkScaleInterval`            | Fact | Prediction uses the inverse link and the link scale interval.             |
| `PredictionKeepsProbabilitiesInRangeAndValidatesOffsets`         | Fact | Prediction keeps probabilities in range and validates offsets.            |
| `LinkFunctionsMatchTheirClosedForms`                             | Fact | Link functions match their closed forms.                                  |
| `LinkFunctionsFlagValuesOutsideTheirDomain`                      | Fact | Link functions flag values outside their domain.                          |

### RelationshipsAndRegressionLinearTests

`Jube.Test.AdvancedAnalytics.Statistics.RelationshipsAndRegressionLinearTests` — 30 tests.

| Test                                                                   | Kind | Verifies                                                                        |
|------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `SimpleRegressionMatchesTheClosedFormAndPublishedAnscombeFigures`      | Fact | Simple regression matches the closed form and published anscombe figures.       |
| `SimpleRegressionOptionsAndEdgeCases`                                  | Fact | Simple regression options and edge cases.                                       |
| `MultipleRegressionReproducesTheNistLongleyCertifiedValues`            | Fact | Multiple regression reproduces the nist longley certified values.               |
| `MultipleRegressionReportsVarianceInflationForCollinearPredictors`     | Fact | Multiple regression reports variance inflation for collinear predictors.        |
| `MultipleRegressionValidatesItsInput`                                  | Fact | Multiple regression validates its input.                                        |
| `WeightedRegressionMatchesTheWeightedClosedForm`                       | Fact | Weighted regression matches the weighted closed form.                           |
| `WeightedRegressionRejectsBadWeights`                                  | Fact | Weighted regression rejects bad weights.                                        |
| `PolynomialRegressionRecoversAnExactQuadraticAndIsInvariantToCentring` | Fact | Polynomial regression recovers an exact quadratic and is invariant to centring. |
| `PolynomialDegreeOneEqualsSimpleRegressionAndValidates`                | Fact | Polynomial degree one equals simple regression and validates.                   |
| `DegreeSelectionFindsTheTrueQuadratic`                                 | Fact | Degree selection finds the true quadratic.                                      |
| `DegreeSelectionMarksUnfittableDegreesAndValidates`                    | Fact | Degree selection marks unfittable degrees and validates.                        |
| `PredictionIntervalsMatchTheClosedForm`                                | Fact | Prediction intervals match the closed form.                                     |
| `PredictionValidatesItsInputs`                                         | Fact | Prediction validates its inputs.                                                |
| `NestedComparisonOfOneVariableEqualsTheSquaredTStatistic`              | Fact | Nested comparison of one variable equals the squared t statistic.               |
| `NestedComparisonOfSeveralVariablesMatchesThePartialFFormula`          | Fact | Nested comparison of several variables matches the partial f formula.           |
| `NestedComparisonRejectsBadNames`                                      | Fact | Nested comparison rejects bad names.                                            |
| `MultivariateRegressionMatchesSeparateFits`                            | Fact | Multivariate regression matches separate fits.                                  |
| `MultivariateRegressionValidates`                                      | Fact | Multivariate regression validates.                                              |
| `RidgeMatchesTheSinglePredictorClosedForm`                             | Fact | Ridge matches the single predictor closed form.                                 |
| `RidgeShrinksCollinearCoefficientsAndValidates`                        | Fact | Ridge shrinks collinear coefficients and validates.                             |
| `RidgePathShrinksMonotonicallyAndPicksTheLowestGcv`                    | Fact | Ridge path shrinks monotonically and picks the lowest gcv.                      |
| `RidgePathValidates`                                                   | Fact | Ridge path validates.                                                           |
| `NonNegativeLeastSquaresClampsNegativeCoefficientsToZero`              | Fact | Non negative least squares clamps negative coefficients to zero.                |
| `NonNegativeLeastSquaresValidates`                                     | Fact | Non negative least squares validates.                                           |
| `RansacFindsTheTrueLineAndTheOutliers`                                 | Fact | Ransac finds the true line and the outliers.                                    |
| `RansacIsReproduciblePerSeedAndValidates`                              | Fact | Ransac is reproducible per seed and validates.                                  |
| `PartialLeastSquaresWithAllComponentsEqualsOrdinaryLeastSquares`       | Fact | Partial least squares with all components equals ordinary least squares.        |
| `PartialLeastSquaresValidates`                                         | Fact | Partial least squares validates.                                                |
| `LeaveOneOutCrossValidationEqualsTheHatMatrixShortcut`                 | Fact | Leave one out cross validation equals the hat matrix shortcut.                  |
| `KFoldCrossValidationIsSeededAndValidates`                             | Fact | K fold cross validation is seeded and validates.                                |

### RelationshipsAndRegressionSelectionTests

`Jube.Test.AdvancedAnalytics.Statistics.RelationshipsAndRegressionSelectionTests` — 8 tests.

| Test                                                    | Kind | Verifies                                                          |
|---------------------------------------------------------|------|-------------------------------------------------------------------|
| `EveryDirectionSelectsTheTrueSignalsUnderAic`           | Fact | Every direction selects the true signals under aic.               |
| `TheSelectedModelIsALocalAicOptimumAndStepsAreRecorded` | Fact | The selected model is a local aic optimum and steps are recorded. |
| `PValueAndBicCriteriaWork`                              | Fact | P value and bic criteria work.                                    |
| `StepwiseValidatesItsInputs`                            | Fact | Stepwise validates its inputs.                                    |
| `BestSubsetMatchesABruteForceOverIndependentFits`       | Fact | Best subset matches a brute force over independent fits.          |
| `BestSubsetSupportsEveryCriterionAndValidates`          | Fact | Best subset supports every criterion and validates.               |
| `InformationCriteriaMatchTheClosedForms`                | Fact | Information criteria match the closed forms.                      |
| `InformationCriteriaValidateAndLinkToARegressionFit`    | Fact | Information criteria validate and link to a regression fit.       |

### TableShapingToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.TableShapingToolsTests` — 17 tests.

| Test                                                           | Kind | Verifies                                                                 |
|----------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `FilterTableRowsKeepsGivenIndicesInGivenOrder`                 | Fact | Filter table rows keeps given indices in given order.                    |
| `FilterTableRowsOnSingleRowTableKeepsThatRow`                  | Fact | Filter table rows on single row table keeps that row.                    |
| `FilterTableRowsRejectsEmptyTable`                             | Fact | Filter table rows rejects empty table.                                   |
| `FilterTableRowsRejectsOutOfRangeIndex`                        | Fact | Filter table rows rejects out of range index.                            |
| `FilterTableRowsRejectsNegativeIndex`                          | Fact | Filter table rows rejects negative index.                                |
| `DistinctTableRowsKeepsAllRowsWhenAllDistinct`                 | Fact | Distinct table rows keeps all rows when all distinct.                    |
| `DistinctTableRowsCollapsesAllDuplicatesToOneRow`              | Fact | Distinct table rows collapses all duplicates to one row.                 |
| `DistinctTableRowsRejectsOutOfRangeTable`                      | Fact | Distinct table rows rejects out of range table.                          |
| `ArrangeTableRowsSortsByMultipleKeysWithMixedDirections`       | Fact | Arrange table rows sorts by multiple keys with mixed directions.         |
| `ArrangeTableRowsIsStableWhenEveryKeyTies`                     | Fact | Arrange table rows is stable when every key ties.                        |
| `ArrangeTableRowsRejectsMismatchedDirectionsLength`            | Fact | Arrange table rows rejects mismatched directions length.                 |
| `ArrangeTableRowsRejectsOutOfRangeColumnIndex`                 | Fact | Arrange table rows rejects out of range column index.                    |
| `RelocateTableColumnsMovesToFrontWhenAfterIsNull`              | Fact | Relocate table columns moves to front when after is null.                |
| `RelocateTableColumnsMovesToEndWhenAfterIsLastRemainingColumn` | Fact | Relocate table columns moves to end when after is last remaining column. |
| `RelocateTableColumnsMovesIntoTheMiddle`                       | Fact | Relocate table columns moves into the middle.                            |
| `RelocateTableColumnsRejectsUnknownColumnName`                 | Fact | Relocate table columns rejects unknown column name.                      |
| `RelocateTableColumnsRejectsAfterBeingOneOfTheMovedColumns`    | Fact | Relocate table columns rejects after being one of the moved columns.     |

### TimeSeriesAndSignalToolsTests

`Jube.Test.AdvancedAnalytics.Statistics.TimeSeriesAndSignalToolsTests` — 219 tests.

| Test                                                                           | Kind | Verifies                                                                                  |
|--------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `CusumAlarmsThreePointsAfterUpwardStepByHandComputation`                       | Fact | Cusum alarms three points after upward step by hand computation.                          |
| `CusumDetectsDownwardShiftWithEstimatedBaselineAndValidatesConstantBaseline`   | Fact | Cusum detects downward shift with estimated baseline and validates constant baseline.     |
| `PageHinkleyAlarmsOnStepUpAndStepDownByHand`                                   | Fact | Page hinkley alarms on step up and step down by hand.                                     |
| `PageHinkleyConstantSeriesIsQuietAndDeltaAbsorbsSmallChange`                   | Fact | Page hinkley constant series is quiet and delta absorbs small change.                     |
| `EwmaFirstLimitMatchesClosedFormAndTwoSigmaShiftAlarmsAtThirdPoint`            | Fact | Ewma first limit matches closed form and two sigma shift alarms at third point.           |
| `EwmaValidatesParametersAndStaysQuietInControl`                                | Fact | Ewma validates parameters and stays quiet in control.                                     |
| `ShewhartRuleOneFlagsPointBeyondThreeSigma`                                    | Fact | Shewhart rule one flags point beyond three sigma.                                         |
| `ShewhartRunRulesTwoThreeAndFour`                                              | Fact | Shewhart run rules two three and four.                                                    |
| `SpikeDetectionGlobalRobustScoreMatchesHandComputation`                        | Fact | Spike detection global robust score matches hand computation.                             |
| `SpikeDetectionRollingMethodsFlagSpikeAndDipAgainstQuietHistory`               | Fact | Spike detection rolling methods flag spike and dip against quiet history.                 |
| `LevelShiftScanLocatesStepAtIndexTwenty`                                       | Fact | Level shift scan locates step at index twenty.                                            |
| `LevelShiftScanFindsNoShiftInStationaryNoiseAndValidatesWindow`                | Fact | Level shift scan finds no shift in stationary noise and validates window.                 |
| `MeanShiftFindsTwoChangePointsOfThreeLevelSeries`                              | Fact | Mean shift finds two change points of three level series.                                 |
| `MeanShiftFindsNothingInNoiseHonoursMaxAndValidates`                           | Fact | Mean shift finds nothing in noise honours max and validates.                              |
| `DrawdownMatchesHandComputedBalanceHistory`                                    | Fact | Drawdown matches hand computed balance history.                                           |
| `DrawdownWithoutRecoveryOrWithoutAnyFall`                                      | Fact | Drawdown without recovery or without any fall.                                            |
| `AutocorrelationOfOneToFiveMatchesHandComputedBiasedEstimator`                 | Fact | Autocorrelation of one to five matches hand computed biased estimator.                    |
| `AutocorrelationOfPeriodicSeriesPeaksAtThePeriodAndFlagsSignificantLags`       | Fact | Autocorrelation of periodic series peaks at the period and flags significant lags.        |
| `AutocorrelationEdgeCases`                                                     | Fact | Autocorrelation edge cases.                                                               |
| `PartialAutocorrelationOfOneToFiveMatchesDurbinLevinsonByHand`                 | Fact | Partial autocorrelation of one to five matches durbin levinson by hand.                   |
| `PartialAutocorrelationOfAutoregressiveSeriesCutsOffAfterLagOne`               | Fact | Partial autocorrelation of autoregressive series cuts off after lag one.                  |
| `PartialAutocorrelationConstantSeriesIsNaNAndShortSeriesRejected`              | Fact | Partial autocorrelation constant series is na n and short series rejected.                |
| `CrossCorrelationOfSeriesWithItselfEqualsAutocorrelationByHand`                | Fact | Cross correlation of series with itself equals autocorrelation by hand.                   |
| `CrossCorrelationFindsWhichSeriesLeads`                                        | Fact | Cross correlation finds which series leads.                                               |
| `CrossCorrelationRejectsMismatchedLengthsAndHandlesConstantSeries`             | Fact | Cross correlation rejects mismatched lengths and handles constant series.                 |
| `LjungBoxMatchesClosedFormChiSquareTwoDegreesOfFreedom`                        | Fact | Ljung box matches closed form chi square two degrees of freedom.                          |
| `LjungBoxRejectsStrongAutocorrelationAndModelDegreesOfFreedomAreSubtracted`    | Fact | Ljung box rejects strong autocorrelation and model degrees of freedom are subtracted.     |
| `BoxPierceMatchesClosedFormChiSquareTwoDegreesOfFreedom`                       | Fact | Box pierce matches closed form chi square two degrees of freedom.                         |
| `DurbinWatsonOfAlternatingResidualsIsThreeByHand`                              | Fact | Durbin watson of alternating residuals is three by hand.                                  |
| `DurbinWatsonPositiveCorrelationAndAllZeroResiduals`                           | Fact | Durbin watson positive correlation and all zero residuals.                                |
| `TurningPointTestMatchesHandComputedZScore`                                    | Fact | Turning point test matches hand computed z score.                                         |
| `TurningPointTestOfMonotoneSeriesRejectsRandomness`                            | Fact | Turning point test of monotone series rejects randomness.                                 |
| `MedianRunsTestMatchesHandComputedAlternatingCase`                             | Fact | Median runs test matches hand computed alternating case.                                  |
| `MedianRunsTestOfTrendHasTwoRunsAndConstantSeriesIsRejected`                   | Fact | Median runs test of trend has two runs and constant series is rejected.                   |
| `MannKendallOfPerfectLinearSeriesMatchesHandComputedStatistics`                | Fact | Mann kendall of perfect linear series matches hand computed statistics.                   |
| `MannKendallDecreasingTiesAndLimits`                                           | Fact | Mann kendall decreasing ties and limits.                                                  |
| `AdditiveDecompositionRecoversExactSeasonalPatternAndTrend`                    | Fact | Additive decomposition recovers exact seasonal pattern and trend.                         |
| `MultiplicativeDecompositionRecoversIndicesAndValidatesInput`                  | Fact | Multiplicative decomposition recovers indices and validates input.                        |
| `SeasonalityStrengthIsHighForCycleAndLowForNoise`                              | Fact | Seasonality strength is high for cycle and low for noise.                                 |
| `EstimatedPeriodOfSineWithTrendIsTheTruePeriod`                                | Fact | Estimated period of sine with trend is the true period.                                   |
| `EstimatedPeriodNotFoundForConstantOrPureTrendAndRejectsShortInput`            | Fact | Estimated period not found for constant or pure trend and rejects short input.            |
| `ArchLmOfPerfectlyAlternatingSquaresIsRowsTimesOneWithClosedFormPValue`        | Fact | Arch lm of perfectly alternating squares is rows times one with closed form p value.      |
| `ArchLmOfConstantMagnitudeIsNaNAndTooFewRowsRejected`                          | Fact | Arch lm of constant magnitude is na n and too few rows rejected.                          |
| `GrangerDetectsLaggedCauseAndMatchesIndependentRegression`                     | Fact | Granger detects lagged cause and matches independent regression.                          |
| `GrangerPerfectFitGivesInfiniteFAndBadInputIsRejected`                         | Fact | Granger perfect fit gives infinite f and bad input is rejected.                           |
| `NaiveAndDriftForecastsMatchHandComputation`                                   | Fact | Naive and drift forecasts match hand computation.                                         |
| `SeasonalNaiveRepeatsLastCycleAndValidatesArguments`                           | Fact | Seasonal naive repeats last cycle and validates arguments.                                |
| `SimpleSmoothingWithFixedAlphaMatchesHandRecursion`                            | Fact | Simple smoothing with fixed alpha matches hand recursion.                                 |
| `SimpleSmoothingEstimatesAlphaNearOneForRampAndRejectsBadAlpha`                | Fact | Simple smoothing estimates alpha near one for ramp and rejects bad alpha.                 |
| `HoltReproducesLinearSeriesExactly`                                            | Fact | Holt reproduces linear series exactly.                                                    |
| `HoltDampingFlattensTheTrendAndValidates`                                      | Fact | Holt damping flattens the trend and validates.                                            |
| `HoltWintersReproducesExactlyPeriodicSeriesAdditiveAndMultiplicative`          | Fact | Holt winters reproduces exactly periodic series additive and multiplicative.              |
| `HoltWintersCapturesTrendPlusSeasonAndValidates`                               | Fact | Holt winters captures trend plus season and validates.                                    |
| `LinearTrendMatchesHandComputedRegressionAndTInterval`                         | Fact | Linear trend matches hand computed regression and t interval.                             |
| `LinearTrendOfExactLineHasZeroWidthAndNeedsThreePoints`                        | Fact | Linear trend of exact line has zero width and needs three points.                         |
| `YuleWalkerAndLeastSquaresFitsMatchHandComputationForOneToFive`                | Fact | Yule walker and least squares fits match hand computation for one to five.                |
| `AutoregressiveFitRecoversSimulatedCoefficientAndValidatesOrder`               | Fact | Autoregressive fit recovers simulated coefficient and validates order.                    |
| `OrderSelectionFindsOneForArOneAndZeroForNoise`                                | Fact | Order selection finds one for ar one and zero for noise.                                  |
| `YuleWalkerForecastRevertsTowardMeanByHand`                                    | Fact | Yule walker forecast reverts toward mean by hand.                                         |
| `LeastSquaresForecastContinuesExactRampAndValidates`                           | Fact | Least squares forecast continues exact ramp and validates.                                |
| `AccuracyMetricsMatchPublishedRegressionExample`                               | Fact | Accuracy metrics match published regression example.                                      |
| `AccuracyMetricsZeroActualsPerfectForecastAndValidation`                       | Fact | Accuracy metrics zero actuals perfect forecast and validation.                            |
| `HoldOutComparisonRanksTrendMethodsAboveNaiveOnRamp`                           | Fact | Hold out comparison ranks trend methods above naive on ramp.                              |
| `HoldOutComparisonUsesSeasonalMethodsForPeriodicSeriesAndValidates`            | Fact | Hold out comparison uses seasonal methods for periodic series and validates.              |
| `DiscreteViterbiMatchesBruteForceEnumerationOfAllPaths`                        | Fact | Discrete viterbi matches brute force enumeration of all paths.                            |
| `DiscreteDecodeValidatesModelAndObservations`                                  | Fact | Discrete decode validates model and observations.                                         |
| `DiscreteLogLikelihoodMatchesBruteForceAndStepProbabilitiesSumToTotal`         | Fact | Discrete log likelihood matches brute force and step probabilities sum to total.          |
| `DiscreteLogLikelihoodImpossibleSymbolIsMinusInfinity`                         | Fact | Discrete log likelihood impossible symbol is minus infinity.                              |
| `DiscretePosteriorMatchesBruteForceMarginals`                                  | Fact | Discrete posterior matches brute force marginals.                                         |
| `DiscretePosteriorOfSingleObservationIsBayesRule`                              | Fact | Discrete posterior of single observation is bayes rule.                                   |
| `DiscreteNextSymbolFollowsFilteredBeliefPushedThroughModel`                    | Fact | Discrete next symbol follows filtered belief pushed through model.                        |
| `DiscreteNextSymbolRejectsBadInput`                                            | Fact | Discrete next symbol rejects bad input.                                                   |
| `GaussianViterbiMatchesBruteForceEnumeration`                                  | Fact | Gaussian viterbi matches brute force enumeration.                                         |
| `GaussianDecodeValidatesDeviationsAndShapes`                                   | Fact | Gaussian decode validates deviations and shapes.                                          |
| `GaussianLogLikelihoodMatchesBruteForce`                                       | Fact | Gaussian log likelihood matches brute force.                                              |
| `GaussianLogLikelihoodFlagsOutlierAsMostSurprising`                            | Fact | Gaussian log likelihood flags outlier as most surprising.                                 |
| `GaussianPosteriorMatchesBruteForceMarginals`                                  | Fact | Gaussian posterior matches brute force marginals.                                         |
| `GaussianPosteriorOfSingleObservationIsBayesRule`                              | Fact | Gaussian posterior of single observation is bayes rule.                                   |
| `GaussianNextValueMomentsFollowMixtureFormulas`                                | Fact | Gaussian next value moments follow mixture formulas.                                      |
| `GaussianNextValueOfStickySingleRegimeApproachesRegimeMean`                    | Fact | Gaussian next value of sticky single regime approaches regime mean.                       |
| `DiscreteTrainingRecoversTwoWellSeparatedRegimes`                              | Fact | Discrete training recovers two well separated regimes.                                    |
| `DiscreteTrainingIsDeterministicForSeedAndValidatesArguments`                  | Fact | Discrete training is deterministic for seed and validates arguments.                      |
| `GaussianTrainingRecoversRegimeMeansDeviationsAndPersistence`                  | Fact | Gaussian training recovers regime means deviations and persistence.                       |
| `GaussianTrainingSingleRegimeMatchesSampleMomentsAndValidates`                 | Fact | Gaussian training single regime matches sample moments and validates.                     |
| `StateSelectionPicksTwoRegimesByBicForTwoRegimeData`                           | Fact | State selection picks two regimes by bic for two regime data.                             |
| `StateSelectionSingleRegimeLogLikelihoodMatchesClosedFormAndValidates`         | Fact | State selection single regime log likelihood matches closed form and validates.           |
| `TransitionMatrixEstimateMatchesHandCountedTable`                              | Fact | Transition matrix estimate matches hand counted table.                                    |
| `TransitionMatrixAbsorbingStateAndMissingExitsAndSmoothing`                    | Fact | Transition matrix absorbing state and missing exits and smoothing.                        |
| `StationaryDistributionOfTwoStateChainIsFiveSixthsOneSixth`                    | Fact | Stationary distribution of two state chain is five sixths one sixth.                      |
| `StationaryDistributionPeriodicReducibleAndInvalidMatrices`                    | Fact | Stationary distribution periodic reducible and invalid matrices.                          |
| `MultiStepProbabilitiesMatchSquaredMatrixByHand`                               | Fact | Multi step probabilities match squared matrix by hand.                                    |
| `MultiStepProbabilitiesZeroStepsLongRunAndValidation`                          | Fact | Multi step probabilities zero steps long run and validation.                              |
| `SequenceSurpriseMatchesHandComputedSmoothedProbabilities`                     | Fact | Sequence surprise matches hand computed smoothed probabilities.                           |
| `SequenceSurpriseHandlesNewStatesAndValidation`                                | Fact | Sequence surprise handles new states and validation.                                      |
| `MeanFirstPassageTimesOfTwoStateChainMatchGeometricFormulas`                   | Fact | Mean first passage times of two state chain match geometric formulas.                     |
| `MeanFirstPassageTimesUnreachableTargetsAreInfinite`                           | Fact | Mean first passage times unreachable targets are infinite.                                |
| `GamblersRuinAbsorptionMatchesClassicalResults`                                | Fact | Gamblers ruin absorption matches classical results.                                       |
| `AbsorptionAnalysisRejectsChainsWithoutAbsorbingStructure`                     | Fact | Absorption analysis rejects chains without absorbing structure.                           |
| `DependenceTestOfPerfectlyAlternatingSequenceHasChiSquareEqualToTransitions`   | Fact | Dependence test of perfectly alternating sequence has chi square equal to transitions.    |
| `DependenceTestOfNearIndependentAndDegenerateSequences`                        | Fact | Dependence test of near independent and degenerate sequences.                             |
| `NGramCountsMatchHandCount`                                                    | Fact | N gram counts match hand count.                                                           |
| `NGramTopKLimitsAndValidation`                                                 | Fact | N gram top k limits and validation.                                                       |
| `EntropyProfileOfAlternatingSequenceHasOneBitAndZeroConditionalEntropy`        | Fact | Entropy profile of alternating sequence has one bit and zero conditional entropy.         |
| `EntropyProfileOfConstantAndCyclicSequences`                                   | Fact | Entropy profile of constant and cyclic sequences.                                         |
| `LempelZivClassicExampleHasSixPhrases`                                         | Fact | Lempel ziv classic example has six phrases.                                               |
| `LempelZivOfConstantShortAndPeriodicSequences`                                 | Fact | Lempel ziv of constant short and periodic sequences.                                      |
| `RunLengthEncodingMatchesHandComputation`                                      | Fact | Run length encoding matches hand computation.                                             |
| `RunLengthEncodingSingleSymbolAndValidation`                                   | Fact | Run length encoding single symbol and validation.                                         |
| `ThresholdRunsMatchHandComputation`                                            | Fact | Threshold runs match hand computation.                                                    |
| `ThresholdRunsNaNBreaksStreaksAndNoHits`                                       | Fact | Threshold runs na n breaks streaks and no hits.                                           |
| `PiecewiseAggregateApproximationMatchesExactAndFractionalHandComputation`      | Fact | Piecewise aggregate approximation matches exact and fractional hand computation.          |
| `PiecewiseAggregateApproximationBoundaries`                                    | Fact | Piecewise aggregate approximation boundaries.                                             |
| `SaxOfLinearRampGivesAbcdWithPublishedBreakpoints`                             | Fact | Sax of linear ramp gives abcd with published breakpoints.                                 |
| `SaxConstantSeriesRawModeAndValidation`                                        | Fact | Sax constant series raw mode and validation.                                              |
| `DtwMatchesHandFilledCostMatrix`                                               | Fact | Dtw matches hand filled cost matrix.                                                      |
| `DtwIdenticalSquaredCostBandAndValidation`                                     | Fact | Dtw identical squared cost band and validation.                                           |
| `EditDistanceMatchesHandComputation`                                           | Fact | Edit distance matches hand computation.                                                   |
| `EditDistanceEmptyAndIdenticalSequences`                                       | Fact | Edit distance empty and identical sequences.                                              |
| `FourierTransformOfSampledCosineMatchesClosedForm`                             | Fact | Fourier transform of sampled cosine matches closed form.                                  |
| `FourierTransformOfRampOfLengthFiveMatchesAnalyticFormulaAndScalesFrequencies` | Fact | Fourier transform of ramp of length five matches analytic formula and scales frequencies. |
| `InverseFourierTransformRoundTripsForOddAndEvenLengths`                        | Fact | Inverse fourier transform round trips for odd and even lengths.                           |
| `InverseFourierTransformRejectsMismatchedParts`                                | Fact | Inverse fourier transform rejects mismatched parts.                                       |
| `PowerSpectrumOfUnitCosineOnBinTwoIsNOverTwoAtPeriodEight`                     | Fact | Power spectrum of unit cosine on bin two is n over two at period eight.                   |
| `PowerSpectrumWindowDetrendAndConstantSeries`                                  | Fact | Power spectrum window detrend and constant series.                                        |
| `WelchSpectrumAveragesSevenSegmentsAndFindsPeriodEight`                        | Fact | Welch spectrum averages seven segments and finds period eight.                            |
| `WelchSpectrumValidatesSegmentAndOverlap`                                      | Fact | Welch spectrum validates segment and overlap.                                             |
| `DominantPeriodsRankTwoCyclesWithPowerSharesInAmplitudeSquaredRatio`           | Fact | Dominant periods rank two cycles with power shares in amplitude squared ratio.            |
| `DominantPeriodOfTrendedSeriesAndUnitScaling`                                  | Fact | Dominant period of trended series and unit scaling.                                       |
| `SpectralSummaryOfPureCycleIsPeakyAndOfImpulseIsFlat`                          | Fact | Spectral summary of pure cycle is peaky and of impulse is flat.                           |
| `SpectralSummaryOfConstantSeriesIsUndefined`                                   | Fact | Spectral summary of constant series is undefined.                                         |
| `FisherGMatchesClosedFormPValueForNinetyPercentDominance`                      | Fact | Fisher g matches closed form p value for ninety percent dominance.                        |
| `FisherTestDoesNotFlagNoiseAndConstantIsUndefined`                             | Fact | Fisher test does not flag noise and constant is undefined.                                |
| `ConvolutionMatchesHandComputedFullSameAndValid`                               | Fact | Convolution matches hand computed full same and valid.                                    |
| `ConvolutionWithMovingAverageKernelSmoothsAndValidatesKernelLength`            | Fact | Convolution with moving average kernel smooths and validates kernel length.               |
| `TemplateMatchFindsExactAndRescaledCopies`                                     | Fact | Template match finds exact and rescaled copies.                                           |
| `TemplateMatchConstantWindowsAreNaNAndInputsValidated`                         | Fact | Template match constant windows are na n and inputs validated.                            |
| `HilbertOfCosineGivesUnitEnvelopeSineQuadratureAndConstantFrequency`           | Fact | Hilbert of cosine gives unit envelope sine quadrature and constant frequency.             |
| `HilbertFrequencyScalesWithSamplingIntervalAndPhaseIsUnwrapped`                | Fact | Hilbert frequency scales with sampling interval and phase is unwrapped.                   |
| `DctMatchesClosedFormOrthonormalCoefficients`                                  | Fact | Dct matches closed form orthonormal coefficients.                                         |
| `DctInverseRoundTripsAndPreservesEnergy`                                       | Fact | Dct inverse round trips and preserves energy.                                             |
| `HaarDecompositionOfFourValuesMatchesHandComputation`                          | Fact | Haar decomposition of four values matches hand computation.                               |
| `HaarDecompositionRejectsIndivisibleLengthAndConstantHasNoDetail`              | Fact | Haar decomposition rejects indivisible length and constant has no detail.                 |
| `HaarDenoiseWithHugeThresholdKeepsOnlyApproximations`                          | Fact | Haar denoise with huge threshold keeps only approximations.                               |
| `HaarDenoiseZeroThresholdIsLosslessAndUniversalThresholdCleansNoisyStep`       | Fact | Haar denoise zero threshold is lossless and universal threshold cleans noisy step.        |
| `FrequencyFiltersIsolateTheCorrectCosine`                                      | Fact | Frequency filters isolate the correct cosine.                                             |
| `FrequencyFilterHighPassRemovesMeanAndValidatesCutoffs`                        | Fact | Frequency filter high pass removes mean and validates cutoffs.                            |
| `ZeroCrossingsOfAlternatingSeriesAreAtHalfSamples`                             | Fact | Zero crossings of alternating series are at half samples.                                 |
| `ZeroCrossingsAtAnotherLevelAndEdgeCases`                                      | Fact | Zero crossings at another level and edge cases.                                           |
| `PeakDetectionMatchesHandComputedPeaksAndProminences`                          | Fact | Peak detection matches hand computed peaks and prominences.                               |
| `PeakDetectionTroughsPlateausAndNoPeaks`                                       | Fact | Peak detection troughs plateaus and no peaks.                                             |
| `ParseIsoTimestampsMatchesKnownUnixTimes`                                      | Fact | Parse iso timestamps matches known unix times.                                            |
| `ParseIsoTimestampsNamesTheBadPosition`                                        | Fact | Parse iso timestamps names the bad position.                                              |
| `FormatEpochSecondsProducesIsoUtcAndRoundTrips`                                | Fact | Format epoch seconds produces iso utc and round trips.                                    |
| `FormatEpochSecondsRejectsMillisecondsAndNaN`                                  | Fact | Format epoch seconds rejects milliseconds and na n.                                       |
| `CalendarFeaturesOfNewYear2021AreAFridayAndHonourOffsets`                      | Fact | Calendar features of new year2021 are a friday and honour offsets.                        |
| `CalendarFeaturesFlagWeekendAndRejectBadOffset`                                | Fact | Calendar features flag weekend and reject bad offset.                                     |
| `InterArrivalStatisticsMatchHandComputationAndIgnoreInputOrder`                | Fact | Inter arrival statistics match hand computation and ignore input order.                   |
| `InterArrivalStatisticsTwoEventsAndDuplicates`                                 | Fact | Inter arrival statistics two events and duplicates.                                       |
| `DetectTimeGapsFindsTheLongPauseByMedianMultipleAndAbsoluteThreshold`          | Fact | Detect time gaps finds the long pause by median multiple and absolute threshold.          |
| `DetectTimeGapsRejectsUnsortedInput`                                           | Fact | Detect time gaps rejects unsorted input.                                                  |
| `ResampleEventsToBucketsCountsSumsAndKeepsEmptyBuckets`                        | Fact | Resample events to buckets counts sums and keeps empty buckets.                           |
| `ResampleEventsToBucketsHandlesNegativeTimesUnsortedInputAndLimits`            | Fact | Resample events to buckets handles negative times unsorted input and limits.              |
| `TrailingWindowVelocityUsesHalfOpenWindowsByHand`                              | Fact | Trailing window velocity uses half open windows by hand.                                  |
| `TrailingWindowVelocityDuplicateTimestampsAndValidation`                       | Fact | Trailing window velocity duplicate timestamps and validation.                             |
| `MultiWindowVelocityCountsEachWindowAtTheSnapshotTime`                         | Fact | Multi window velocity counts each window at the snapshot time.                            |
| `MultiWindowVelocityBeforeAnyEventAndBadWindows`                               | Fact | Multi window velocity before any event and bad windows.                                   |
| `RateChangeMatchesClosedFormPoissonTail`                                       | Fact | Rate change matches closed form poisson tail.                                             |
| `RateChangeSilentBaselineAndDecrease`                                          | Fact | Rate change silent baseline and decrease.                                                 |
| `BurstDetectionFindsTwoBurstsByHand`                                           | Fact | Burst detection finds two bursts by hand.                                                 |
| `BurstDetectionFindsNothingInRegularStreamAndValidates`                        | Fact | Burst detection finds nothing in regular stream and validates.                            |
| `SessionizeSplitsOnGapsLongerThanTimeout`                                      | Fact | Sessionize splits on gaps longer than timeout.                                            |
| `SessionizeGapEqualToTimeoutStaysInSession`                                    | Fact | Sessionize gap equal to timeout stays in session.                                         |
| `HourProfileOfUniformDayHasMaximumEntropyAndZeroChiSquare`                     | Fact | Hour profile of uniform day has maximum entropy and zero chi square.                      |
| `CalendarProfileConcentratedActivityAndOtherCycles`                            | Fact | Calendar profile concentrated activity and other cycles.                                  |
| `PeriodicityOfClockworkEventsIsPerfect`                                        | Fact | Periodicity of clockwork events is perfect.                                               |
| `PeriodicityOfIrregularEventsIsLowAndInputIsSorted`                            | Fact | Periodicity of irregular events is low and input is sorted.                               |
| `DispersionIndexMatchesClosedFormChiSquareTwoDegreesOfFreedom`                 | Fact | Dispersion index matches closed form chi square two degrees of freedom.                   |
| `DispersionOfBurstyAndConstantCountsAndValidation`                             | Fact | Dispersion of bursty and constant counts and validation.                                  |
| `DifferencingSquaresGivesOddNumbersAndSecondDifferencesAreConstantTwo`         | Fact | Differencing squares gives odd numbers and second differences are constant two.           |
| `DifferencingPropagatesNaNAndRejectsBadArguments`                              | Fact | Differencing propagates na n and rejects bad arguments.                                   |
| `LagAndLeadShiftValuesAndPadWithNaN`                                           | Fact | Lag and lead shift values and pad with na n.                                              |
| `LaggedFeatureMatrixBuildsHandComputedRows`                                    | Fact | Lagged feature matrix builds hand computed rows.                                          |
| `LaggedFeatureMatrixValidatesLags`                                             | Fact | Lagged feature matrix validates lags.                                                     |
| `CumulativeOperationsMatchHandComputation`                                     | Fact | Cumulative operations match hand computation.                                             |
| `CumulativeSumPropagatesNaNWhileMaximumIgnoresIt`                              | Fact | Cumulative sum propagates na n while maximum ignores it.                                  |
| `PercentChangeMatchesHandComputationAndZeroBaselineIsNaN`                      | Fact | Percent change matches hand computation and zero baseline is na n.                        |
| `LogReturnOfExponentialGrowthIsConstantOne`                                    | Fact | Log return of exponential growth is constant one.                                         |
| `SimpleMovingAverageTrailingCenteredAndPartial`                                | Fact | Simple moving average trailing centered and partial.                                      |
| `SimpleMovingAverageSkipsMissingAndRejectsBadWindow`                           | Fact | Simple moving average skips missing and rejects bad window.                               |
| `WeightedMovingAverageUsesOldestToNewestWeights`                               | Fact | Weighted moving average uses oldest to newest weights.                                    |
| `ExponentialMovingAverageRecursiveAndAdjustedFormsMatchHandComputation`        | Fact | Exponential moving average recursive and adjusted forms match hand computation.           |
| `ExponentialMovingAverageRejectsAmbiguousParameters`                           | Fact | Exponential moving average rejects ambiguous parameters.                                  |
| `ExponentialMovingStatisticsMatchHandComputedRecursion`                        | Fact | Exponential moving statistics match hand computed recursion.                              |
| `ExponentialMovingStatisticsOfConstantHasZeroSpread`                           | Fact | Exponential moving statistics of constant has zero spread.                                |
| `RollingStatisticsOverWindowOfThreeMatchHandComputation`                       | Fact | Rolling statistics over window of three match hand computation.                           |
| `RollingStatisticsIgnoreMissingAndCountFiniteValues`                           | Fact | Rolling statistics ignore missing and count finite values.                                |
| `ExpandingStatisticsMatchHandComputation`                                      | Fact | Expanding statistics match hand computation.                                              |
| `ExpandingStatisticsHandleMissingAndMinPeriods`                                | Fact | Expanding statistics handle missing and min periods.                                      |
| `RollingCorrelationOfProportionalAndOppositeSeriesIsPlusAndMinusOne`           | Fact | Rolling correlation of proportional and opposite series is plus and minus one.            |
| `RollingCorrelationConstantWindowIsNaNAndMismatchRejected`                     | Fact | Rolling correlation constant window is na n and mismatch rejected.                        |
| `RollingZScoreOfSpikeUsesPrecedingWindowOnly`                                  | Fact | Rolling z score of spike uses preceding window only.                                      |
| `RollingZScoreConstantBaselineIsNaNAndIncludeCurrentChangesResult`             | Fact | Rolling z score constant baseline is na n and include current changes result.             |
| `DetrendLinearSeriesRecoversInterceptAndSlope`                                 | Fact | Detrend linear series recovers intercept and slope.                                       |
| `DetrendMeanAndPolynomialMatchClosedForms`                                     | Fact | Detrend mean and polynomial match closed forms.                                           |
| `FillLinearInterpolationForwardBackwardNearestAndConstant`                     | Fact | Fill linear interpolation forward backward nearest and constant.                          |
| `FillEdgeGapsAndAllMissing`                                                    | Fact | Fill edge gaps and all missing.                                                           |
| `MedianFilterRemovesSpikesAndReplicatesEdges`                                  | Fact | Median filter removes spikes and replicates edges.                                        |
| `MedianFilterRejectsEvenWindowAndKeepsConstant`                                | Fact | Median filter rejects even window and keeps constant.                                     |
| `HampelFlagsSingleSpikeAndReplacesWithLocalMedian`                             | Fact | Hampel flags single spike and replaces with local median.                                 |
| `HampelLeavesCleanSeriesAlone`                                                 | Fact | Hampel leaves clean series alone.                                                         |
| `SavitzkyGolayWindowFiveOrderTwoUsesTextbookCoefficients`                      | Fact | Savitzky golay window five order two uses textbook coefficients.                          |
| `SavitzkyGolayReproducesPolynomialAndItsDerivativesIncludingEdges`             | Fact | Savitzky golay reproduces polynomial and its derivatives including edges.                 |
| `SavitzkyGolayValidatesParameters`                                             | Fact | Savitzky golay validates parameters.                                                      |
| `BoxCoxWithGivenLambdaMatchesDefinition`                                       | Fact | Box cox with given lambda matches definition.                                             |
| `BoxCoxEstimatesLambdaNearZeroForLogNormalQuantilesAndRejectsNonPositive`      | Fact | Box cox estimates lambda near zero for log normal quantiles and rejects non positive.     |
| `KalmanWithZeroProcessNoiseIsTheRunningMeanWithHandComputedVariances`          | Fact | Kalman with zero process noise is the running mean with hand computed variances.          |
| `KalmanSteadyStateGainForUnitSignalToNoiseIsTheInverseGoldenRatio`             | Fact | Kalman steady state gain for unit signal to noise is the inverse golden ratio.            |
| `SeriesProfileSummarisesMissingRunsAndMonotonicity`                            | Fact | Series profile summarises missing runs and monotonicity.                                  |
| `SeriesProfileHandlesConstantAllMissingAndSeasonalSeries`                      | Fact | Series profile handles constant all missing and seasonal series.                          |

### VariableTransformationApplierEdgeCaseTests

`Jube.Test.AdvancedAnalytics.Statistics.VariableTransformationApplierEdgeCaseTests` — 24 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                   |
|--------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `AZeroScaleNeverDividesByZeroAndReturnsZeroInstead`                            | Theory | A zero scale never divides by zero and returns zero instead.                               |
| `ANonFiniteScaleNeverThrowsAndReturnsZeroInstead`                              | Theory | A non finite scale never throws and returns zero instead.                                  |
| `LogOfZeroIsNaNNotAnException`                                                 | Fact   | Log of zero is na n not an exception.                                                      |
| `LogOfANegativeValueIsNaNNotAnException`                                       | Fact   | Log of a negative value is na n not an exception.                                          |
| `SqrtOfANegativeValueIsNaNNotAnException`                                      | Fact   | Sqrt of a negative value is na n not an exception.                                         |
| `ReciprocalOfZeroIsNaNNotAnException`                                          | Fact   | Reciprocal of zero is na n not an exception.                                               |
| `SquareAndAbsoluteValueAreAlwaysDefinedForNegativeInputs`                      | Fact   | Square and absolute value are always defined for negative inputs.                          |
| `SignedLog1PIsDefinedForEveryRealValueIncludingNegatives`                      | Theory | Signed log1 p is defined for every real value including negatives.                         |
| `SignedSqrtIsDefinedForEveryRealValueIncludingNegatives`                       | Theory | Signed sqrt is defined for every real value including negatives.                           |
| `CubeRootIsDefinedForNegativeValuesUnlikeSquareRoot`                           | Fact   | Cube root is defined for negative values unlike square root.                               |
| `MalformedParametersJsonForAScalingKindNeverThrowsAndFallsBackToTheRawValue`   | Theory | Malformed parameters json for a scaling kind never throws and falls back to the raw value. |
| `UnparsableParametersJsonForADiscretisationKindNeverThrowsAndBehavesAsNoEdges` | Theory | Unparsable parameters json for a discretisation kind never throws and behaves as no edges. |
| `StructurallyValidButWrongTypedEdgesNeverThrowsAndFallsBackToTheRawValue`      | Theory | Structurally valid but wrong typed edges never throws and falls back to the raw value.     |
| `MalformedParametersJsonForACategoricalKindNeverThrowsAndFallsBackToZero`      | Theory | Malformed parameters json for a categorical kind never throws and falls back to zero.      |
| `MalformedParametersJsonForAOneHotSlotNeverThrowsAndFallsBackToZero`           | Fact   | Malformed parameters json for a one hot slot never throws and falls back to zero.          |
| `AnUnseenCategoryNeverThrowsForAnyCategoricalTransformationKind`               | Theory | An unseen category never throws for any categorical transformation kind.                   |
| `LabelOrdinalOfAnUnseenCategoryReturnsTheUnknownCode`                          | Fact   | Label ordinal of an unseen category returns the unknown code.                              |
| `OneHotIndicatorOfAnUnseenCategoryReturnsZero`                                 | Fact   | One hot indicator of an unseen category returns zero.                                      |
| `TargetMeanOfAnUnseenCategoryReturnsTheDefaultValue`                           | Fact   | Target mean of an unseen category returns the default value.                               |
| `WeightOfEvidenceOfAnUnseenCategoryReturnsTheDefaultValue`                     | Fact   | Weight of evidence of an unseen category returns the default value.                        |
| `FrequencyOfAnUnseenCategoryReturnsTheDefaultValue`                            | Fact   | Frequency of an unseen category returns the default value.                                 |
| `CallingApplyNumericWithACategoricalKindThrowsALoudProgrammingError`           | Fact   | Calling apply numeric with a categorical kind throws a loud programming error.             |
| `CallingApplyCategoricalWithANumericKindThrowsALoudProgrammingError`           | Fact   | Calling apply categorical with a numeric kind throws a loud programming error.             |
| `TheApplierIsPureAndSafeUnderConcurrentUseAcrossManyThreads`                   | Fact   | The applier is pure and safe under concurrent use across many threads.                     |

### VariableTransformationApplierTests

`Jube.Test.AdvancedAnalytics.Statistics.VariableTransformationApplierTests` — 19 tests. Category: Unit.

| Test                                                                                | Kind   | Verifies                                                                                          |
|-------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `ColumnFunctionsMatchScalingToolsApplyColumnFunction`                               | Theory | Column functions match scaling tools apply column function.                                       |
| `BoxCoxMatchesScalingToolsApplyPowerTransformer`                                    | Fact   | Box cox matches scaling tools apply power transformer.                                            |
| `YeoJohnsonMatchesScalingToolsApplyPowerTransformer`                                | Fact   | Yeo johnson matches scaling tools apply power transformer.                                        |
| `TableScalingMatchesScalingToolsApplyTableScaler`                                   | Theory | Table scaling matches scaling tools apply table scaler.                                           |
| `QuantileTransformMatchesScalingToolsApplyQuantileTransformer`                      | Theory | Quantile transform matches scaling tools apply quantile transformer.                              |
| `WinsoriseMatchesScalingToolsApplyOutlierClipper`                                   | Fact   | Winsorise matches scaling tools apply outlier clipper.                                            |
| `DiscretisationMatchesBinningToolsApplyBinEdges`                                    | Theory | Discretisation matches binning tools apply bin edges.                                             |
| `DiscretiseSupervisedMatchesBinningToolsApplyBinEdges`                              | Fact   | Discretise supervised matches binning tools apply bin edges.                                      |
| `DiscretisationOfAMissingValueReturnsMinusOne`                                      | Fact   | Discretisation of a missing value returns minus one.                                              |
| `CyclicalSinAndCosMatchFeatureEngineeringToolsEncodeCyclicalFeature`                | Fact   | Cyclical sin and cos match feature engineering tools encode cyclical feature.                     |
| `LabelOrdinalMatchesEncodingToolsApplyLabelEncoder`                                 | Fact   | Label ordinal matches encoding tools apply label encoder.                                         |
| `LabelOrdinalOfAnUnseenCategoryMatchesTheUnknownCode`                               | Fact   | Label ordinal of an unseen category matches the unknown code.                                     |
| `OneHotIndicatorMatchesEncodingToolsApplyOneHotEncoderForOneSelectedCategory`       | Fact   | One hot indicator matches encoding tools apply one hot encoder for one selected category.         |
| `TargetMeanMatchesEncodingToolsApplyCategoryEncoder`                                | Fact   | Target mean matches encoding tools apply category encoder.                                        |
| `WeightOfEvidenceMatchesEncodingToolsApplyCategoryEncoder`                          | Fact   | Weight of evidence matches encoding tools apply category encoder.                                 |
| `FrequencyMatchesEncodingToolsApplyCategoryEncoder`                                 | Fact   | Frequency matches encoding tools apply category encoder.                                          |
| `ACategoricalTransformationOfAnUnseenCategoryFallsBackToTheDefaultValue`            | Fact   | A categorical transformation of an unseen category falls back to the default value.               |
| `NoneReturnsTheRawValueUnchanged`                                                   | Fact   | None returns the raw value unchanged.                                                             |
| `EveryNumericTransformationOfAMissingValueIsNaNExceptDiscretisationWhichIsMinusOne` | Fact   | Every numeric transformation of a missing value is na n except discretisation which is minus one. |
