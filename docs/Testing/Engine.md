---
layout: default
title: Real-Time Engine
nav_order: 10
parent: Testing
---

# Real-Time Engine

Tests of the real-time model invocation engine: payload extraction, abstraction rules, TTL counters, activation and the
supporting engine plumbing.

## General

### ContainerResourceLimitReaderTests

`Jube.Test.Engine.ContainerResourceLimitReaderTests` — 1 test. Category: Unit.

| Test                                            | Kind | Verifies                                              |
|-------------------------------------------------|------|-------------------------------------------------------|
| `ReadNeverThrowsAndLeavesUnavailableFieldsNull` | Fact | Read never throws and leaves unavailable fields null. |

## Case risk

### CaseRiskCalculatorTests

`Jube.Test.CaseRisk.CaseRiskCalculatorTests` — 19 tests.
Category: Unit.

| Test                                                                             | Kind   | Verifies                                                                                         |
|----------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------|
| `TheWorkedExampleOfSpecSection4511ReproducesLikelihoodImpactAndPriority`         | Fact   | The worked example of spec section 4511 reproduces likelihood impact and priority.               |
| `ContributionsSumExactlyToTheLogOddsDistanceFromTheBaseRate`                     | Fact   | Contributions sum exactly to the log odds distance from the base rate.                           |
| `NegativeEvidenceLowersTheLikelihoodBelowTheBaseRate`                            | Fact   | Negative evidence lowers the likelihood below the base rate.                                     |
| `TheStrongestEvidenceInAGroupCountsInFullByMagnitudeEvenWhenItIsNegative`        | Fact   | The strongest evidence in a group counts in full by magnitude even when it is negative.          |
| `IndependentEvidenceAddsWhileSharedEvidenceIsDiscounted`                         | Fact   | Independent evidence adds while shared evidence is discounted.                                   |
| `EvidenceBehindFewClosedCasesIsShrunkTowardsTheBaseRate`                         | Fact   | Evidence behind few closed cases is shrunk towards the base rate.                                |
| `EvidenceWithNoClosedCasesBehindItIsIgnored`                                     | Fact   | Evidence with no closed cases behind it is ignored.                                              |
| `OldEvidenceDecaysByItsHalfLife`                                                 | Fact   | Old evidence decays by its half life.                                                            |
| `TheBaseRateIsShrunkTowardsThePriorUntilEnoughCasesHaveClosed`                   | Fact   | The base rate is shrunk towards the prior until enough cases have closed.                        |
| `TheBandsFollowTheDocumentedThresholds`                                          | Theory | The bands follow the documented thresholds.                                                      |
| `TheProbabilityNeverReachesCertaintyEitherWay`                                   | Fact   | The probability never reaches certainty either way.                                              |
| `ImpactIsTheLargerOfTypologyAndMoneyPlusAtMostOneUpliftEachCappedAtSevere`       | Theory | Impact is the larger of typology and money plus at most one uplift each capped at severe.        |
| `ImpactNeverDependsOnTheLikelihood`                                              | Fact   | Impact never depends on the likelihood.                                                          |
| `PriorityScalesExpectedHarmLogarithmicallyToZeroToOneHundred`                    | Theory | Priority scales expected harm logarithmically to zero to one hundred.                            |
| `LaneOneBeatsEverythingAndKeepsTheHoldDeadline`                                  | Fact   | Lane one beats everything and keeps the hold deadline.                                           |
| `AMandatoryReasonPlacesTheCaseInLaneTwo`                                         | Fact   | A mandatory reason places the case in lane two.                                                  |
| `ADiaryMovesTheCaseIntoLaneThreeWithinTheThresholdAndAnOverdueOneNeverDropsBack` | Theory | A diary moves the case into lane three within the threshold and an overdue one never drops back. |
| `NoDeadlineMeansTheStandardLaneWithNoDate`                                       | Fact   | No deadline means the standard lane with no date.                                                |
| `ALaneNeverChangesTheLikelihoodOrTheImpact`                                      | Fact   | A lane never changes the likelihood or the impact.                                               |

### CaseSimilarityCalculatorTests

`Jube.Test.CaseRisk.CaseSimilarityCalculatorTests` — 10 tests.
Category: Unit.

| Test                                                                   | Kind   | Verifies                                                                              |
|------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `TheWorkedExampleScoresEachKindAndTheWhole`                            | Fact   | The worked example scores each kind and the whole.                                    |
| `ARuleOnHalfOfAllCasesAloneNeverMakesTwoCasesSimilar`                  | Fact   | A rule on half of all cases alone never makes two cases similar.                      |
| `AValueOnlyOneCaseCarriesWeighsNothingAndDoesNotDiluteTheScore`        | Fact   | A value only one case carries weighs nothing and does not dilute the score.           |
| `AKindOnlyOneCaseHasIsLeftOutOfTheMean`                                | Fact   | A kind only one case has is left out of the mean.                                     |
| `CasesWithNothingInCommonScoreZero`                                    | Fact   | Cases with nothing in common score zero.                                              |
| `AmountsFallIntoHalfDecadeBands`                                       | Theory | Amounts fall into half decade bands.                                                  |
| `NegativeAmountsBandBySizeWithTheirSign`                               | Fact   | Negative amounts band by size with their sign.                                        |
| `AScoreBetweenZeroAndOneFallsIntoAQuarterAndAnythingElseIntoASizeBand` | Theory | A score between zero and one falls into a quarter and anything else into a size band. |
| `AMeasureBecomesItsSumMeanAndMaximumBands`                             | Fact   | A measure becomes its sum mean and maximum bands.                                     |
| `AFieldWithMoreDistinctValuesThanTheCapContributesNothing`             | Fact   | A field with more distinct values than the cap contributes nothing.                   |

### CaseSimilarityEvaluationTests

`Jube.Test.CaseRisk.CaseSimilarityEvaluationTests` — 3 tests.
Category: Unit.

A leave-one-out harness: each closed case is predicted from the confirmed share of the similar cases closed before it
was created, and the area under the curve is compared with a baseline that only asks whether two cases share a rule.
The world is synthetic and seeded, so the numbers show that the method separates outcomes, not how it performs on a
tenant's own cases.

| Test                                                                                       | Kind | Verifies                                                                                                  |
|--------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------|
| `OnAWorldWithOneConfirmedTypologyTheVectorSeparatesOutcomesBetterThanSharingTheCommonRule` | Fact | On a world with one confirmed typology the vector separates outcomes better than sharing the common rule. |
| `OnlyCasesClosedBeforeTheTargetWasCreatedMayPredictIt`                                     | Fact | Only cases closed before the target was created may predict it.                                           |
| `TheAreaUnderTheCurveIsOneForAPerfectRankingAndAHalfForTies`                               | Fact | The area under the curve is one for a perfect ranking and a half for ties.                                |

### CaseValueReaderTests

`Jube.Test.CaseRisk.CaseValueReaderTests` — 2 tests.
Category: Unit.

| Test                                                         | Kind   | Verifies                                                                 |
|--------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `AValueIsFoundWhateverTheCaseAndWhetherANumberOrNumericText` | Theory | A value is found whatever the case and whether a number or numeric text. |
| `NothingUsableGivesNoValueAndNeverThrows`                    | Theory | Nothing usable gives no value and never throws.                          |

### CaseEscalationSweepTests

`Jube.Test.CaseEscalation.CaseEscalationSweepTests` — 10 tests.
Category: Service. Collection: Database.

| Test                                                                                  | Kind | Verifies                                                                                          |
|---------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `ACaseThatHasStayedInAStatusLongEnoughMovesOnAndTheRecordShowsWhyAsync`               | Fact | A case that has stayed in a status long enough moves on and the record shows why.                 |
| `OnlyOpenCasesInTheRuleStatusAreEverMovedAsync`                                       | Fact | Only open cases in the rule status are ever moved.                                                |
| `ReleasingTheLockIsOptionalAndOtherwiseTheAnalystKeepsTheCaseAsync`                   | Fact | Releasing the lock is optional and otherwise the analyst keeps the case.                          |
| `ACaseOlderThanTheWorkflowResolutionTargetMovesAndNoTargetMeansNoRuleAsync`           | Fact | A case older than the workflow resolution target moves and no target means no rule.               |
| `TheLanePriorityAndDeadlineTriggersReadOnlyTheLatestAssessmentAsync`                  | Fact | The lane priority and deadline triggers read only the latest assessment.                          |
| `ARuleWhoseTargetIsNotAStrongerPriorityOrNotInTheWorkflowIsSkippedAndNeverLoopsAsync` | Fact | A rule whose target is not a stronger priority or not in the workflow is skipped and never loops. |
| `TwoRulesPointingBackAtEachOtherMoveACaseOnceAndThenLeaveItAloneAsync`                | Fact | Two rules pointing back at each other move a case once and then leave it alone.                   |
| `ChainedRulesCarryACaseOnStepByStepAsync`                                             | Fact | Chained rules carry a case on step by step.                                                       |
| `AMoveIsAtomicAgainstTheCaseHavingAlreadyLeftTheStatusAsync`                          | Fact | A move is atomic against the case having already left the status.                                 |
| `TheAfterMoveCallbackRunsOncePerMovedCaseAndItsFailureNeverUndoesTheMoveAsync`        | Fact | The after move callback runs once per moved case and its failure never undoes the move.           |

### CaseStatusNotifierTests

`Jube.Test.CaseEscalation.CaseStatusNotifierTests` — 1 test.
Category: Unit.

| Test                                                                             | Kind | Verifies                                                                                          |
|----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `ThePostBodyCarriesTheCaseItsPayloadAsContextAndTheStatusNameButNeverTheRawJson` | Fact | The post body carries the case its payload as context and the status name but never the raw json. |

### CaseSupervisionServiceTests

`Jube.Test.CaseSupervision.CaseSupervisionServiceTests` — 15 tests.
Category: Service. Collection: Database.

| Test                                                                                  | Kind    | Verifies                                                                                                     |
|---------------------------------------------------------------------------------------|---------|--------------------------------------------------------------------------------------------------------------|
| `TheWorkloadShowsEachAnalystsHoldsTakesPeeksAndClosuresAndTheCasesNobodyHoldsAsync`   | Fact    | The workload shows each analysts holds takes peeks and closures and the cases nobody holds.                  |
| `AnAnalystWhoseRoleLacksTheCasePermissionIsNeverListedOrAssignedAsync`                | Fact    | An analyst whose role lacks the case permission is never listed or assigned.                                 |
| `OnlyAnalystsWhoCanSeeTheWorkflowAndHoldTheCasePermissionAreListedAsync`              | Fact    | Only analysts who can see the workflow and hold the case permission are listed.                              |
| `SupervisionNeedsThePermissionAndAWorkflowTheCallersRoleIsGrantedAsync`               | Fact    | Supervision needs the permission and a workflow the callers role is granted.                                 |
| `StuckLocksAreOpenCasesLockedAndIdleLongEnoughLongestFirstWithTheirRiskAsync`         | Fact    | Stuck locks are open cases locked and idle long enough longest first with their risk.                        |
| `BreakingALockReleasesItAndRecordsWhoAndWhyAndOnlyForAnOpenLockedCaseAsync`           | Fact    | Breaking a lock releases it and records who and why and only for an open locked case.                        |
| `ReassigningLocksTheCaseToAnAnalystWhoCanSeeItAndRecordsTheHandoverAsync`             | Fact    | Reassigning locks the case to an analyst who can see it and records the handover.                            |
| `ACaseCannotBeHandedToSomeoneWhoCouldNotSeeItOrWhenItIsNotOpenAsync`                  | Fact    | A case cannot be handed to someone who could not see it or when it is not open.                              |
| `AWorkloadPeriodThatIsBackwardsOrLongerThanAYearIsRefusedAsync`                       | Fact    | A workload period that is backwards or longer than a year is refused and exactly 366 days is accepted.       |
| `ASupervisorWhoseRoleLacksTheStatusOfACaseCannotBreakOrReassignItsLockAsync`          | Fact    | A supervisor whose role lacks the status of a case cannot break or reassign its lock and nothing is written. |
| `PlacingALegalHoldRecordsTheReasonTheUserAndTheDateAndLiftingItKeepsWhoLiftedItAsync` | Fact    | Placing a legal hold records the reason, user and date, and lifting it keeps who lifted it.                  |
| `ALegalHoldNeedsAReasonAndALimitedOneAsync`                                           | Fact    | A legal hold needs a reason of at most 500 characters and a valid case.                                      |
| `ALegalHoldCanBePlacedOnAClosedCaseAsync`                                             | Fact    | A legal hold can be placed on a closed case.                                                                 |
| `ALegalHoldIsRefusedWithoutTheSupervisionPermissionAndForACaseTheUserCannotSeeAsync`  | Fact    | A legal hold is refused without the supervision permission and for a case the user cannot see.               |
| `PlacingAndLiftingAHoldWritesTimelineEventsAndTheStateCanBeReadAsync`                 | Service | Placing and lifting a legal hold writes the two timeline events and the hold state can be read.              |

### CaseDiaryLaneTests

`Jube.Test.CaseRisk.CaseDiaryLaneTests` — 4 tests.
Category: Unit.

| Test                                                                               | Kind   | Verifies                                                                                          |
|------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `APaymentHoldPutsTheCaseInTheLiveMoneyLaneAndBreachesAtItsDeadline`                | Fact   | A payment hold puts the case in the live money lane and breaches at its deadline.                 |
| `ARegulatoryDeadlinePutsTheCaseInTheMandatoryLaneWhateverTheDistanceToIt`          | Fact   | A regulatory deadline puts the case in the mandatory lane whatever the distance to it.            |
| `ReviewAndCustomerResponseDiariesRankByHowNearTheyAreAndNameThemselvesInTheReason` | Theory | Review and customer response diaries rank by how near they are and name themselves in the reason. |
| `ADiaryThatIsOffOrHasNoDateNeverChangesTheLane`                                    | Theory | A diary that is off or has no date never changes the lane.                                        |

### CaseDiaryServiceTests

`Jube.Test.CaseDiary.CaseDiaryServiceTests` — 8 tests.
Category: Service. Collection: Database.

| Test                                                                                              | Kind | Verifies                                                                                                        |
|---------------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------------|
| `SettingADiaryRecordsItsTypeAndDateAskForAFreshRankingAndLogsWhoDidItAsync`                       | Fact | Setting a diary records its type and date ask for a fresh ranking and logs who did it.                          |
| `ChangingOnlyTheTypeOrOnlyTheDateLogsOnlyWhatChangedAndSettingTheSameAgainLogsNothingAsync`       | Fact | Changing only the type or only the date logs only what changed and setting the same again logs nothing.         |
| `ClearingTurnsTheDiaryOffReturnsTheTypeToReviewAndLogsWhatWasClearedAsync`                        | Fact | Clearing turns the diary off returns the type to review and logs what was cleared.                              |
| `OnlyAnOpenCaseTheCallerCanSeeAndIsNotHeldByAnotherCarriesATypedDiaryAsync`                       | Fact | Only an open case the caller can see and is not held by another carries a typed diary.                          |
| `TheRequestIsValidatedAndTheCallerMustHoldTheCasePermissionAsync`                                 | Fact | The request is validated and the caller must hold the case permission.                                          |
| `ARegulatoryDeadlineAndAPaymentHoldRankTheCaseInTheirLanesAtTheNextSweepWithTheRightTriggerAsync` | Fact | A regulatory deadline and a payment hold rank the case in their lanes at the next sweep with the right trigger. |
| `OnlyCasesWhoseDiaryExpiryChangesTheirStatusAreHandedToTheExpiryAutomationAsync`                  | Fact | Only cases whose diary expiry changes their status are handed to the expiry automation.                         |
| `TheExpiryUpdateOnlyChangesACaseThatIsStillInTheStatusItWasReadInAsync`                           | Fact | The expiry update only changes a case that is still in the status it was read in.                               |

### QaSamplerTests

`Jube.Test.CaseRisk.QaSamplerTests` — 7 tests.
Category: Unit.

| Test                                                                        | Kind   | Verifies                                                                                  |
|-----------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `TheDrawIsStableForACaseAndAlwaysInsideZeroAndOne`                          | Fact   | The draw is stable for a case and always inside zero and one.                             |
| `ThePickedShareTracksTheConfiguredRate`                                     | Theory | The picked share tracks the configured rate.                                              |
| `ARateOfNothingNeverPicksAndAWholeRateAlwaysDoes`                           | Fact   | A rate of nothing never picks and a whole rate always does.                               |
| `OnlyLowPriorityStandardCasesAreEligible`                                   | Theory | Only low priority standard cases are eligible.                                            |
| `TheReportCountsPickedOpenClosedAndDecidedCasesAndTheirConfirmedShare`      | Fact   | The report counts picked open closed and decided cases and their confirmed share.         |
| `TheReportIsEnoughOnceTenSampledCasesHaveAnOutcomeAndSaysNothingBeforeThat` | Fact   | The report is enough once ten sampled cases have an outcome and says nothing before that. |
| `TheSampleRateOverrideMustBeBetweenZeroAndAHalf`                            | Theory | The sample rate override must be between zero and a half.                                 |

### CaseQaSamplingTests

`Jube.Test.CaseQa.CaseQaSamplingTests` — 4 tests.
Category: Service. Collection: Database.

| Test                                                                                       | Kind | Verifies                                                                                               |
|--------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------------|
| `AnEligibleCaseIsDecidedOnceAndAPickedOneIsRecordedWithAnEventAsync`                       | Fact | An eligible case is decided once and a picked one is recorded with an event.                           |
| `OnlyLowPriorityStandardCasesAreEverDecidedAndANilRateLeavesThemUndecidedAsync`            | Fact | Only low priority standard cases are ever decided and a nil rate leaves them undecided.                |
| `APickedCaseIsOrderedAheadOfOrdinaryStandardCasesButBehindUrgentOnesAndHasItsOwnViewAsync` | Fact | A picked case is ordered ahead of ordinary standard cases but behind urgent ones and has its own view. |
| `TheAnalyticsReportCountsPickedCasesAndTheirOutcomesSeparatelyFromEveryoneElseAsync`       | Fact | The analytics report counts picked cases and their outcomes separately from everyone else.             |

### CaseRiskSweepTests

`Jube.Test.CaseRisk.CaseRiskSweepTests` — 15 tests.
Category: Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                          |
|-------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `OutcomeCountsOnlyUseClosedCasesInStatusesMarkedConfirmedOrNotConfirmedAsync`             | Fact | Outcome counts only use closed cases in statuses marked confirmed or not confirmed.               |
| `KeyHistoryCountsEarlierClosedOutcomesOnTheSameKeyAndNeverTheCaseItselfAsync`             | Fact | Key history counts earlier closed outcomes on the same key and never the case itself.             |
| `AnOpenCaseWithNoHistoryGetsAColdStartAssessmentFromThePriorsAsync`                       | Fact | An open case with no history gets a cold start assessment from the priors.                        |
| `ARuleThatHasBeenRightBeforeRaisesTheLikelihoodAndOneThatHasNotLowersItAsync`             | Fact | A rule that has been right before raises the likelihood and one that has not lowers it.           |
| `AKeyWhoseEarlierCasesWereAllFalsePositivesLowersTheLikelihoodAsync`                      | Fact | A key whose earlier cases were all false positives lowers the likelihood.                         |
| `TheMoneyInThePayloadAndTheWorkflowTypologyDecideTheImpactAsync`                          | Fact | The money in the payload and the workflow typology decide the impact.                             |
| `ADiaryWithinTwoDaysPutsTheCaseInTheDeadlineLaneAndAnOverdueOneIsMarkedBreachedAsync`     | Fact | A diary within two days puts the case in the deadline lane and an overdue one is marked breached. |
| `ClosedAndSuspendBypassCasesAreNeverAssessedAndRecentlyCheckedOnesAreSkippedAsync`        | Fact | Closed and suspend bypass cases are never assessed and recently checked ones are skipped.         |
| `AnUnchangedCaseIsCheckedAgainWithoutWritingAnotherAssessmentButAChangeIsRecordedAsync`   | Fact | An unchanged case is checked again without writing another assessment but a change is recorded.   |
| `ACaseThatWasChangedAndSoHasNoCheckDateIsPickedUpFirstAsync`                              | Fact | A case that was changed and so has no check date is picked up first.                              |
| `ABatchLimitLeavesTheRestForTheNextSweepAsync`                                            | Fact | A batch limit leaves the rest for the next sweep.                                                 |
| `TheDeploymentDefaultsAreTheSpecDefaults`                                                 | Fact | The deployment defaults are the spec defaults.                                                    |
| `AWorkflowsOwnRankingSettingsDecideItsCasesAndOnlyItsCasesAsync`                          | Fact | A workflows own ranking settings decide its cases and only its cases.                             |
| `ChangingAWorkflowsRankingSettingsReassessesItsOpenCasesUnderTheModelChangedTriggerAsync` | Fact | Changing a workflows ranking settings reassesses its open cases under the model changed trigger.  |
| `ChangingTheTypologySeverityOrTheValueFieldReassessesTooAsync`                            | Fact | Changing the typology severity or the value field reassesses too.                                 |

### ServiceLevelCalculatorTests

`Jube.Test.CaseRisk.ServiceLevelCalculatorTests` — 7 tests.
Category: Unit.

| Test                                                                            | Kind   | Verifies                                                                                        |
|---------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `NoTargetsMeansNoTargetAndNoMeasures`                                           | Fact   | No targets means no target and no measures.                                                     |
| `AnUnreachedTargetMovesFromOnTrackToAtRiskAtEightyPercentToBreachedAtTheTarget` | Theory | An unreached target moves from on track to at risk at eighty percent to breached at the target. |
| `ATargetReachedInTimeIsMetAndOneReachedLateStaysBreachedForever`                | Fact   | A target reached in time is met and one reached late stays breached forever.                    |
| `TheWorstDimensionDecidesTheOverallStateAndTheMostElapsedBreaksATie`            | Fact   | The worst dimension decides the overall state and the most elapsed breaks a tie.                |
| `ACaseClosedWithoutEverBeingViewedOrOwnedIsNotJudgedOnThoseTargetsAfterClosure` | Fact   | A case closed without ever being viewed or owned is not judged on those targets after closure.  |
| `StatusDwellRunsFromTheMomentOfEntryAndStopsOnceTheCaseIsClosed`                | Fact   | Status dwell runs from the moment of entry and stops once the case is closed.                   |
| `ZeroOrNegativeTargetsAreTreatedAsNoTarget`                                     | Fact   | Zero or negative targets are treated as no target.                                              |

### CaseSearchServiceTests

`Jube.Test.CaseRisk.CaseSearchServiceTests` — 26 tests.
Category: Service. Collection: Database.

| Test                                                                                      | Kind   | Verifies                                                                                                                |
|-------------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------------------|
| `ASignedOutOrTenantlessUserIsRefusedAndOneWithoutThePermissionIsForbiddenAsync`           | Fact   | A signed out or tenantless user is refused and one without the permission is forbidden.                                 |
| `TheTriageOrderPutsLiveMoneyFirstThenReturnedCasesThenLaneAndPriorityAsync`               | Fact   | The triage order puts live money first then returned cases then lane and priority.                                      |
| `EverySortOrdersTheListByItsOwnKeyAsync`                                                  | Theory | Every sort orders the list by its own key.                                                                              |
| `PagingKeepsTheTotalAndNeverRepeatsOrSkipsACaseAsync`                                     | Fact   | Paging keeps the total and never repeats or skips a case.                                                               |
| `ASavedSearchCanFilterOnTheTypeOfTheDiaryAsync`                                           | Fact   | A saved search can filter on the type of the diary.                                                                     |
| `ARowSaysWhatItsDiaryIsForAsync`                                                          | Fact   | A row says what its diary is for.                                                                                       |
| `ARowCarriesTheRiskTheTopDriverTheValueTheStatusAndTheLockAsync`                          | Fact   | A row carries the risk the top driver the value the status and the lock.                                                |
| `AnUnassessedCaseSaysSoAndIsNeverMistakenForALaneOneCaseAsync`                            | Fact   | An unassessed case says so and is never mistaken for a lane one case.                                                   |
| `TheLatestAssessmentIsCopiedOntoTheCaseSoTheQueueOrdersFromAnIndexAsync`                  | Fact   | The latest assessment is copied onto the case (lane, priority, likelihood, sequence) so the queue orders from an index. |
| `OnlyTheLatestAssessmentOfACaseDecidesItsLaneAndItsPlaceInTheUrgentViewAsync`             | Fact   | Only the latest assessment of a case decides its lane and its place in the urgent view.                                 |
| `AttentionNeedsTheCallerToHaveLookedOrHoldTheCaseAndNamesEachReasonAsync`                 | Fact   | Attention needs the caller to have looked or hold the case and names each reason.                                       |
| `TheReturnedViewHoldsExactlyTheCasesTheListFlagsAsNeedingTheCallerAsync`                  | Fact   | The returned view holds exactly the cases the list flags as needing the caller.                                         |
| `TheMineFreeAndAllViewsFollowTheLockAndTheClosedStatusAsync`                              | Fact   | The mine free and all views follow the lock and the closed status.                                                      |
| `TheOverdueViewAndEachRowShowTheServiceLevelsTheWorkflowAndStatusSetAsync`                | Fact   | The overdue view and each row show the service levels the workflow and status set.                                      |
| `CountsListEveryViewTheWorkflowFiltersAndTheSavedSearchesAndFlagAFailingOneAsync`         | Fact   | Counts list every view the workflow filters and the saved searches and flag a failing one.                              |
| `ASavedSearchRunsThroughTheSameCompilerAndNeverShowsMoreThanTheCallerMaySeeAsync`         | Fact   | A saved search runs through the same compiler and never shows more than the caller may see.                             |
| `AWorkflowTheCallersRoleIsNotGrantedAndAnotherTenantsWorkflowAreNotFoundAsync`            | Fact   | A workflow the callers role is not granted and another tenants workflow are not found.                                  |
| `OnlyOneWayOfChoosingTheCasesIsAllowedAndAnUnknownSavedSearchOrSessionIsNotFoundAsync`    | Fact   | Only one way of choosing the cases is allowed and an unknown saved search or session is not found.                      |
| `TheRiskMatrixCountsCasesByLikelihoodAndImpactAndUnassessedCasesUnderZeroAsync`           | Fact   | The risk matrix counts cases by likelihood and impact and unassessed cases under zero.                                  |
| `SkimLocksTheTopCaseRecordsTheLookAndOffersTheNextOnesAsync`                              | Fact   | Skim locks the top case records the look and offers the next ones.                                                      |
| `SkimTakesAReturnedCaseBeforeAHigherPriorityStandardOneAsync`                             | Fact   | Skim takes a returned case before a higher priority standard one.                                                       |
| `TwoPeopleSkimmingAtOnceNeverTakeTheSameCaseAsync`                                        | Fact   | Two people skimming at once never take the same case.                                                                   |
| `AnotherUsersLookAtACaseNeverCreatesAttentionOrDuplicatesTheRowForTheCallerAsync`         | Fact   | Another users look at a case never creates attention or duplicates the row for the caller.                              |
| `TheWorkflowChoicesAreOnlyTheActiveOnesTheCallersRoleIsGrantedInTheirTenantAsync`         | Fact   | The workflow choices are only the active ones the callers role is granted in their tenant.                              |
| `AHostileSkipIsClampedAndNeverOverflowsTheQueryAsync`                                     | Fact   | A hostile skip is clamped and never overflows the query.                                                                |
| `ASavedSearchOnAFieldThatDoesNotExistIsRefusedAsAnInvalidSearchAndNeverAServerErrorAsync` | Fact   | A saved search on a field that does not exist is refused as an invalid search.                                          |

### CaseSummaryServiceTests

`Jube.Test.CaseRisk.CaseSummaryServiceTests` — 6 tests.
Category: Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                           |
|-------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `APeekReturnsTheWholePictureOfACaseAndRecordsTheLookWithoutTakingTheLockAsync`            | Fact | A peek returns the whole picture of a case and records the look without taking the lock.           |
| `ASecondPeekShowsOnlyWhatOthersChangedSinceTheFirstAndHowThePriorityMovedAsync`           | Fact | A second peek shows only what others changed since the first and how the priority moved.           |
| `ACaseTheCallerCannotSeeIsNotFoundAndNothingIsRecordedAsync`                              | Fact | A case the caller cannot see is not found and nothing is recorded.                                 |
| `ASignedOutUserIsRefusedAndOneWithoutThePermissionIsForbiddenAsync`                       | Fact | A signed out user is refused and one without the permission is forbidden.                          |
| `OpeningACaseInFullAlsoCountsAsLookingAtItAsync`                                          | Fact | Opening a case in full also counts as looking at it.                                               |
| `TheWaterfallChartsTheEvidenceWithoutRecordingALookAndRefusesWhatTheCallerCannotSeeAsync` | Fact | The waterfall charts the evidence without recording a look and refuses what the caller cannot see. |

### CaseRiskSettingsResolverTests

`Jube.Test.CaseRisk.CaseRiskSettingsResolverTests` — 14 tests.
Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                       |
|-----------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `NoOverridesLeavesTheEnvironmentDefaultsUntouched`                    | Fact   | No overrides leaves the environment defaults untouched.                        |
| `AnOverrideReplacesOnlyTheSettingItNamesAndTheRestKeepTheDefaults`    | Fact   | An override replaces only the setting it names and the rest keep the defaults. |
| `AListThatCannotBeReadFallsBackToTheDefaultAndNeverThrows`            | Theory | A list that cannot be read falls back to the default and never throws.         |
| `ValidOverridesHaveNoProblems`                                        | Fact   | Valid overrides have no problems.                                              |
| `KappaMustBeAboveZeroAndNoMoreThanOne`                                | Theory | Kappa must be above zero and no more than one.                                 |
| `RhoMustBeBetweenZeroAndOne`                                          | Theory | Rho must be between zero and one.                                              |
| `PriorStrengthMustBeAboveZeroAndNoMoreThanTenThousand`                | Theory | Prior strength must be above zero and no more than ten thousand.               |
| `BaseRatePriorMustStayAwayFromCertainty`                              | Theory | Base rate prior must stay away from certainty.                                 |
| `TheDeadlineWindowMustBeBetweenZeroAndAYear`                          | Theory | The deadline window must be between zero and a year.                           |
| `TheKeyHistoryHalfLifeMustBeAboveZeroAndNoMoreThanTenYears`           | Theory | The key history half life must be above zero and no more than ten years.       |
| `ImpactWeightsMustBeFivePositiveStrictlyIncreasingNumbers`            | Theory | Impact weights must be five positive strictly increasing numbers.              |
| `MoneyBandsMustBeFourPositiveStrictlyIncreasingNumbers`               | Theory | Money bands must be four positive strictly increasing numbers.                 |
| `EveryProblemIsReportedAtOnce`                                        | Fact   | Every problem is reported at once.                                             |
| `TheOverriddenSettingsChangeTheLikelihoodTheWholeCalculationProduces` | Fact   | The overridden settings change the likelihood the whole calculation produces.  |

### CaseAnalyticsDistributionTests

`Jube.Test.CaseRisk.CaseAnalyticsDistributionTests` — 10 tests.
Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                      |
|--------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `PercentilesInterpolateBetweenTheSortedValuesLikeAStatisticsPackage`           | Fact   | Percentiles interpolate between the sorted values like a statistics package.                  |
| `AnEmptyOrSingleValueDistributionNeverThrows`                                  | Fact   | An empty or single value distribution never throws.                                           |
| `TheHistogramAssignsEveryValueToExactlyOneBucketAndLabelsTheEdges`             | Fact   | The histogram assigns every value to exactly one bucket and labels the edges.                 |
| `MinutesAreShownInTheUnitThatReadsBest`                                        | Theory | Minutes are shown in the unit that reads best.                                                |
| `CycleTimesMeasureFromCreationAndCountWhatWasNeverViewedOrOwned`               | Fact   | Cycle times measure from creation and count what was never viewed or owned.                   |
| `ACaseViewedBeforeItWasCreatedNeverProducesANegativeCycleTime`                 | Fact   | A case viewed before it was created never produces a negative cycle time.                     |
| `FlowCountsOpenedClosedAndTheBacklogAtTheEndOfEachDay`                         | Fact   | Flow counts opened closed and the backlog at the end of each day.                             |
| `DwellSharesTheTimeBetweenStatusesAndCountsBreachesAgainstTheTarget`           | Fact   | Dwell shares the time between statuses and counts breaches against the target.                |
| `AgingBucketsOnlyTheOpenCasesByAgeAndLaneAndListsTheOldest`                    | Fact   | Aging buckets only the open cases by age and lane and lists the oldest.                       |
| `ACaseClosedExactlyAtMidnightBelongsToTheNewDayAndLeavesTheBacklogThatEvening` | Fact   | A case closed exactly at midnight belongs to the new day and leaves the backlog that evening. |

### ChecklistSlaCalculatorTests

`Jube.Test.CaseRisk.ChecklistSlaCalculatorTests` — 9 tests.
Category: Unit.

| Test                                                                                | Kind | Verifies                                                                                            |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `ShownCompletedWithinLateOpenAndBreachedAreCountedPerItem`                          | Fact | Shown completed within late open and breached are counted per item.                                 |
| `MedianAndNinetiethPercentileMinutesRunFromFirstShownToCompletion`                  | Fact | Median and ninetieth percentile minutes run from first shown to completion.                         |
| `AttainmentIsTheShareOfTimedCompletionsWithinTheSla`                                | Fact | Attainment is the share of timed completions within the sla.                                        |
| `TheTargetIsTheSlaOfTheMostRecentlyShownInstance`                                   | Fact | The target is the sla of the most recently shown instance.                                          |
| `AnItemWithNoSlaCountsAsCompletedOnlyAndIsNeverBreached`                            | Fact | An item with no sla counts as completed only and is never breached.                                 |
| `AnOpenItemWithARecordedBreachIsBreachedEvenBeforeItsDueDateIsComparedWithTheClock` | Fact | An open item with arecorded breach is breached even before its due date is compared with the clock. |
| `HiddenItemsAreNeverOpenOrBreached`                                                 | Fact | Hidden items are never open or breached.                                                            |
| `TheWorstItemsComeFirstAndTheTotalsAddUp`                                           | Fact | The worst items come first and the totals add up.                                                   |
| `NoFactsGiveAnEmptyReportWithNoAttainment`                                          | Fact | No facts give an empty report with no attainment.                                                   |

### CaseEventValueGuardTests

`Jube.Test.CaseEvents.CaseEventValueGuardTests` — 3 tests.
Category: Unit.

| Test                                                           | Kind   | Verifies                                                                 |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `CanonicalValuesAreAccepted`                                   | Theory | Canonical values are accepted.                                           |
| `NamesCultureDatesAndValuesOnTheWrongSideAreRefused`           | Theory | Names culture dates and values on the wrong side are refused.            |
| `EnsureCanonicalThrowsAnExplanationThatNamesTheTypeAndTheSide` | Fact   | Ensure canonical throws an explanation that names the type and the side. |

### CaseRiskRecheckTriggerTests

`Jube.Test.CaseRisk.CaseRiskRecheckTriggerTests` — 12 tests.
Category: Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                           |
|-------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `TheQueryMarksOnlyTheNamedOpenCasesAndNeverAClosedOneAsync`                               | Fact | The query marks only the named open cases and never a closed one.                                  |
| `TheQueryWithNoCasesDoesNothingAsync`                                                     | Fact | The query with no cases does nothing.                                                              |
| `AttachingAnActivationToAnExistingCaseMarksItActivationAttachedAndLeavesOthersAsync`      | Fact | Attaching an activation to an existing case marks it activation attached and leaves others.        |
| `ThePaymentHoldDecisionMarksOnlyTheOpenCaseOfTheHoldAsync`                                | Fact | The payment hold decision marks only the open case of the hold.                                    |
| `ClosingACaseMarksTheOtherOpenCasesOnTheSameKeyLinkedKeyChangedButNotItselfAsync`         | Fact | Closing a case marks the other open cases on the same key linked key changed but not itself.       |
| `AnUpdateThatDoesNotCloseTheCaseMarksNothingAsync`                                        | Fact | An update that does not close the case marks nothing.                                              |
| `RecordingAFindingMarksTheNotesOpenCaseFindingRecordedAndNoOtherCaseAsync`                | Fact | Recording a finding marks the notes open case finding recorded and no other case.                  |
| `ChangingTheTypologySeverityMarksTheOpenCasesOfTheWorkflowTypologyChangedAsync`           | Fact | Changing the typology severity marks the open cases of the workflow typology changed.              |
| `AnotherRiskInputChangeMarksTheOpenCasesModelChangedAndAnIrrelevantEditMarksNothingAsync` | Fact | Another risk input change marks the open cases model changed and an irrelevant edit marks nothing. |
| `GraphNodesThatMovedMarkOnlyTheOpenCasesOnThoseNodesNeighbourTaggedAsync`                 | Fact | Graph nodes that moved mark only the open cases on those nodes neighbour tagged.                   |
| `GraphNodesOfAnotherTenantMarkNoCasesAsync`                                               | Fact | Graph nodes of another tenant mark no cases.                                                       |
| `TheEventRepositoryRefusesANonCanonicalStatusNameAndAcceptsAGuidAsync`                    | Fact | The event repository refuses a non canonical status name and accepts a guid.                       |

### CaseChecklistReadModelTests

`Jube.Test.CaseRisk.CaseChecklistReadModelTests` — 8 tests.
Category: Service. Collection: Database.

| Test                                                                                    | Kind | Verifies                                                                                          |
|-----------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `ThePeekReportsRequiredDoneOpenBreachedAndTheNextDueDateAndNeverCountsHiddenItemsAsync` | Fact | The peek reports required done open breached and the next due date and never counts hidden items. |
| `ACaseNeverReconciledReportsZerosAndACompleteChecklistAsync`                            | Fact | Acase never reconciled reports zeros and acomplete checklist.                                     |
| `AClosedCaseReportsZerosEvenWhenItStillHoldsShownItemsAsync`                            | Fact | Aclosed case reports zeros even when it still holds shown items.                                  |
| `EachRowCarriesItsOpenAndBreachedCountsAndOnlyTheStatusItIsInCountsAsync`               | Fact | Each row carries its open and breached counts and only the status it is in counts.                |
| `TheChecklistViewsListOnlyOpenCasesWithOpenOrBreachedItemsInTheCurrentStatusAsync`      | Fact | The checklist views list only open cases with open or breached items in the current status.       |
| `TheChecklistViewsStayInsideTheWorkflowAndTheRolesOfTheCallerAsync`                     | Fact | The checklist views stay inside the workflow and the roles of the caller.                         |
| `PagingTheBreachedViewVisitsEveryCaseOnceAndTheTotalStaysTheSameAsync`                  | Fact | Paging the breached view visits every case once and the total stays the same.                     |
| `TheAnalyticsSectionCountsItemsFirstShownInThePeriodForTheWorkflowOnlyAsync`            | Fact | The analytics section counts items first shown in the period for the workflow only.               |

### CaseAnalyticsQualityTests

`Jube.Test.CaseRisk.CaseAnalyticsQualityTests` — 8 tests.
Category: Unit.

| Test                                                                        | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `RulesAreRankedByTheEffortTheirFalsePositivesCostAndShowTheirPrecision`     | Fact | Rules are ranked by the effort their false positives cost and show their precision.       |
| `ARuleWithNoDecidedCasesHasNoPrecisionRatherThanZero`                       | Fact | A rule with no decided cases has no precision rather than zero.                           |
| `APerfectOrderingFindsEveryConfirmedCaseFirstAndABackwardsOneFindsThemLast` | Fact | A perfect ordering finds every confirmed case first and a backwards one finds them last.  |
| `ThePriorityOrderingIsComparedWithWorkingTheOldestFirstAndTiesCountHalf`    | Fact | The priority ordering is compared with working the oldest first and ties count half.      |
| `TheRankingIsOnlyCalledSufficientWithEnoughOutcomesOfBothKinds`             | Fact | The ranking is only called sufficient with enough outcomes of both kinds.                 |
| `OnlyClosedCasesWithAnOutcomeAreScoredAndOpenOrUndecidedOnesAreLeftOut`     | Fact | Only closed cases with an outcome are scored and open or undecided ones are left out.     |
| `CalibrationComparesPredictedWithObservedAndScoresTheBrierAndExpectedError` | Fact | Calibration compares predicted with observed and scores the brier and expected error.     |
| `ALikelihoodOfExactlyOneFallsInTheTopBinAndNothingIsDroppedFromCalibration` | Fact | A likelihood of exactly one falls in the top bin and nothing is dropped from calibration. |

### MyDayCalculatorTests

`Jube.Test.CaseRisk.MyDayCalculatorTests` — 7 tests.
Category: Unit.

| Test                                                                      | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `OnlyTheCallersOwnClosuresAreCountedAndTheDaysRunOldestToNewest`          | Fact | Only the callers own closures are counted and the days run oldest to newest.            |
| `TheStreakCountsConsecutiveDaysThatMetTheGoalAndStopsAtTheFirstMiss`      | Fact | The streak counts consecutive days that met the goal and stops at the first miss.       |
| `ADayThatHasNotYetMetTheGoalDoesNotBreakTheStreakFromYesterday`           | Fact | A day that has not yet met the goal does not break the streak from yesterday.           |
| `WithNoGoalAStreakIsARunOfDaysWithAtLeastOneClosure`                      | Fact | With no goal a streak is a run of days with at least one closure.                       |
| `ATypicalDayIsTheMedianOfTheThirteenDaysBeforeTodayAndNeverIncludesToday` | Fact | A typical day is the median of the thirteen days before today and never includes today. |
| `AQuietUserHasNoStreakAndZeroEverywhereRatherThanNothing`                 | Fact | A quiet user has no streak and zero everywhere rather than nothing.                     |
| `AGoalOfZeroOrLessIsTreatedAsOneSoANilDayNeverCountsAsMet`                | Fact | A goal of zero or less is treated as one so a nil day never counts as met.              |

### RobustStatisticsTests

`Jube.Test.CaseRisk.Journal.RobustStatisticsTests` — 8 tests.
Category: Unit.

| Test                                                                | Kind | Verifies                                                                      |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `TheFitIsTheMedianAndTheScaledMedianAbsoluteDeviation`              | Fact | The fit is the median and the scaled median absolute deviation.               |
| `AZScoreIsTheDistanceInScalesFromTheMedianAndIsClamped`             | Fact | AZ score is the distance in scales from the median and is clamped.            |
| `FewerThanFiveValuesCannotBeJudgedAndNonFiniteValuesAreIgnored`     | Fact | Fewer than five values cannot be judged and non finite values are ignored.    |
| `AConstantHistoryJudgesAnyDeviationAsTheLargestPossible`            | Fact | A constant history judges any deviation as the largest possible.              |
| `WhenTheMedianDeviationIsZeroTheInterquartileRangeProvidesTheScale` | Fact | When the median deviation is zero the interquartile range provides the scale. |
| `AnEmptyFitHasNoCountAndNoMedian`                                   | Fact | An empty fit has no count and no median.                                      |
| `QuantilesInterpolateAndHandleOneValue`                             | Fact | Quantiles interpolate and handle one value.                                   |
| `ConfidenceStartsAtTwentyValues`                                    | Fact | Confidence starts at twenty values.                                           |

### NotableCalculatorTests

`Jube.Test.CaseRisk.Journal.NotableCalculatorTests` — 6 tests.
Category: Unit.

| Test                                                                           | Kind | Verifies                                                                                     |
|--------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `OnlyTheValueFarFromItsOwnNormalIsNotableAndItNamesTheField`                   | Fact | Only the value far from its own normal is notable and it names the field.                    |
| `ARaisedThresholdLeavesFewerRowsAndALowerOneMore`                              | Fact | A raised threshold leaves fewer rows and a lower one more.                                   |
| `TheWorstRowsComeFirstAndTheLimitCapsThem`                                     | Fact | The worst rows come first and the limit caps them.                                           |
| `FewerThanTwentyValuesAreFlaggedAsLowConfidenceAndFewerThanFiveAreNeverJudged` | Fact | Fewer than twenty values are flagged as low confidence and fewer than five are never judged. |
| `EachFieldKeepsItsOwnNorm`                                                     | Fact | Each field keeps its own norm.                                                               |
| `AFieldWithNoValuesHasAFiniteNormSoTheReportCanBeWrittenAsJson`                | Fact | A field with no values has a finite norm so the report can be written as json.               |

### ProgressionCalculatorTests

`Jube.Test.CaseRisk.Journal.ProgressionCalculatorTests` — 6 tests.
Category: Unit.

| Test                                                          | Kind   | Verifies                                                                  |
|---------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `TheBucketFollowsTheSpanOfTheHistory`                         | Theory | The bucket follows the span of the history.                               |
| `EachBucketHoldsTheMedianAndTheCountOfItsTransactions`        | Fact   | Each bucket holds the median and the count of its transactions.           |
| `TheChangeIsTheLatestAgainstTheMedianOfTheBucketsBefore`      | Fact   | The change is the latest against the median of the buckets before.        |
| `ANarrowDailyHistoryHasNoPreviousAndAZeroPreviousHasNoChange` | Fact   | A narrow daily history has no previous and a zero previous has no change. |
| `TooManyBucketsWidenToWeeksAndAFieldWithNoValuesIsSkipped`    | Fact   | Too many buckets widen to weeks and a field with no values is skipped.    |
| `WeeksStartOnMondayAndNothingGivesAnEmptyReport`              | Fact   | Weeks start on monday and nothing gives an empty report.                  |

### DistributionCalculatorTests

`Jube.Test.CaseRisk.Journal.DistributionCalculatorTests` — 8 tests.
Category: Unit.

| Test                                                                  | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `EveryValueLandsInExactlyOneBinAndTheBinsStayWithinTheirLimits`       | Fact | Every value lands in exactly one bin and the bins stay within their limits.        |
| `FewValuesStillGetTheMinimumNumberOfBins`                             | Fact | Few values still get the minimum number of bins.                                   |
| `ATightClusterWithOneExtremeNeverMakesMoreThanTheMaximumNumberOfBins` | Fact | A tight cluster with one extreme never makes more than the maximum number of bins. |
| `AConstantFieldIsOneBinRatherThanADivisionByZero`                     | Fact | A constant field is one bin rather than a division by zero.                        |
| `TheBoxUsesTukeyFencesAndCountsTheOutliers`                           | Fact | The box uses tukey fences and counts the outliers.                                 |
| `TheLatestValueIsRankedAgainstTheWholeHistory`                        | Fact | The latest value is ranked against the whole history.                              |
| `AFieldWithNoValuesProducesNoReport`                                  | Fact | A field with no values produces no report.                                         |
| `TheMeanAndTheEdgesComeFromTheSortedValues`                           | Fact | The mean and the edges come from the sorted values.                                |

### VelocityCalculatorTests

`Jube.Test.CaseRisk.Journal.VelocityCalculatorTests` — 4 tests.
Category: Unit.

| Test                                                         | Kind | Verifies                                                                   |
|--------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `TheGridCountsTransactionsByWeekdayFromMondayAndHour`        | Fact | The grid counts transactions by weekday from monday and hour.              |
| `TheBusiestHourIsFoundAgainstTheTypicalOne`                  | Fact | The busiest hour is found against the typical one.                         |
| `EachOfTheThreeWindowsIsReportedAndNothingGivesNoBursts`     | Fact | Each of the three windows is reported and nothing gives no bursts.         |
| `TheWindowIsHalfOpenSoAnEventExactlyOneWindowLaterIsNotInIt` | Fact | The window is half open so an event exactly one window later is not in it. |

### GapCalculatorTests

`Jube.Test.CaseRisk.Journal.GapCalculatorTests` — 4 tests.
Category: Unit.

| Test                                                       | Kind | Verifies                                                              |
|------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `EveryGapLandsInOneBinAndTheLabelsReadInTheUnitThatFits`   | Fact | Every gap lands in one bin and the labels read in the unit that fits. |
| `TheLongestDormancyIsFoundWithWhatHappenedInTheDayAfterIt` | Fact | The longest dormancy is found with what happened in the day after it. |
| `AGapExactlyOnAnEdgeBelongsToTheBinThatStartsThere`        | Fact | A gap exactly on an edge belongs to the bin that starts there.        |
| `TheMedianGapIsInHoursAndOneEventHasNoGaps`                | Fact | The median gap is in hours and one event has no gaps.                 |

### NoveltyCalculatorTests

`Jube.Test.CaseRisk.Journal.NoveltyCalculatorTests` — 4 tests.
Category: Unit.

| Test                                                               | Kind | Verifies                                                                          |
|--------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `AFirstSeenValueAfterTheWarmUpIsFlaggedOnItsRowAndNothingBeforeIt` | Fact | A first seen value after the warm up is flagged on its row and nothing before it. |
| `TheFirstFewTransactionsAreNeverNovelBecauseThereIsNoHistoryYet`   | Fact | The first few transactions are never novel because there is no history yet.       |
| `OnlyValuesFirstSeenInTheLastWeekAreListedAsRecentNewestFirst`     | Fact | Only values first seen in the last week are listed as recent newest first.        |
| `BlankAndMissingValuesAreIgnored`                                  | Fact | Blank and missing values are ignored.                                             |

### CounterpartyCalculatorTests

`Jube.Test.CaseRisk.Journal.CounterpartyCalculatorTests` — 4 tests.
Category: Unit.

| Test                                                                      | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `TheTopComeFirstWithTheirShareAndTheConcentrationIsTheSumOfSquaredShares` | Fact | The top come first with their share and the concentration is the sum of squared shares. |
| `AnAmountFieldTotalsTheMoneyPerCounterpartyAndAbsentOneLeavesItOut`       | Fact | An amount field totals the money per counterparty and absent one leaves it out.         |
| `ACounterpartyFirstSeenInTheLastThirtyDaysIsNew`                          | Fact | A counterparty first seen in the last thirty days is new.                               |
| `OnlyTenAreListedAndAFieldWithNothingInItIsOmitted`                       | Fact | Only ten are listed and a field with nothing in it is omitted.                          |

### CorrelationCalculatorTests

`Jube.Test.CaseRisk.Journal.CorrelationCalculatorTests` — 5 tests.
Category: Unit.

| Test                                               | Kind | Verifies                                                     |
|----------------------------------------------------|------|--------------------------------------------------------------|
| `APerfectLineIsOneAndIsListedAsStrong`             | Fact | A perfect line is one and is listed as strong.               |
| `ANegativeRelationshipKeepsItsSign`                | Fact | A negative relationship keeps its sign.                      |
| `AConstantFieldAndFieldsWithTooFewPairsAreLeftOut` | Fact | A constant field and fields with too few pairs are left out. |
| `UnrelatedNoiseIsNotStrong`                        | Fact | Unrelated noise is not strong.                               |
| `PearsonNeedsTenPairsAndVarianceOnBothSides`       | Fact | Pearson needs ten pairs and variance on both sides.          |

### DriftCalculatorTests

`Jube.Test.CaseRisk.Journal.DriftCalculatorTests` — 4 tests.
Category: Unit.

| Test                                                                   | Kind | Verifies                                                                          |
|------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `AShiftedRecentHalfIsFlaggedWithTheDirectionOfTheShift`                | Fact | A shifted recent half is flagged with the direction of the shift.                 |
| `TheSameDistributionInBothHalvesIsNotFlagged`                          | Fact | The same distribution in both halves is not flagged.                              |
| `FewerThanFortyTransactionsOrTwentyPerSideGivesNoJudgement`            | Fact | Fewer than forty transactions or twenty per side gives no judgement.              |
| `TheKolmogorovDistanceIsZeroForIdenticalSamplesAndOneForSeparatedOnes` | Fact | The kolmogorov distance is zero for identical samples and one for separated ones. |

### PeerDistributionCalculatorTests

`Jube.Test.CaseRisk.Journal.PeerDistributionCalculatorTests` — 4 tests.
Category: Unit.

| Test                                                    | Kind | Verifies                                                           |
|---------------------------------------------------------|------|--------------------------------------------------------------------|
| `BuildsTheDistributionOfKeyMediansAndCountsKeysAndRows` | Fact | Builds the distribution of key medians and counts keys and rows.   |
| `NeedsAtLeastTwentyKeysThatHaveValues`                  | Fact | Needs at least twenty keys that have values.                       |
| `IgnoresValuesThatAreNotNumbers`                        | Fact | Ignores values that are not numbers.                               |
| `KeepsASingleBinWhenEveryKeyIsTheSameAndAScaleOfZero`   | Fact | Keeps a single bin when every key is the same and a scale of zero. |

### PeerComparisonCalculatorTests

`Jube.Test.CaseRisk.Journal.PeerComparisonCalculatorTests` — 8 tests.
Category: Unit.

| Test                                                    | Kind | Verifies                                                           |
|---------------------------------------------------------|------|--------------------------------------------------------------------|
| `AKeyAtThePeerMedianIsTypicalAtTheMiddlePercentile`     | Fact | A key at the peer median is typical at the middle percentile.      |
| `AKeyFarAboveOrBelowThePeersIsMarkedAndClampedAtFive`   | Fact | A key far above or below the peers is marked and clamped at five.  |
| `TheThresholdDecidesWhenAKeyStandsOut`                  | Fact | The threshold decides when a key stands out.                       |
| `TheKeyIsComparedByItsMedianAndFewRowsAreLowConfidence` | Fact | The key is compared by its median and few rows are low confidence. |
| `NothingToCompareGivesNoComparison`                     | Fact | Nothing to compare gives no comparison.                            |
| `TooFewPeerKeysAreNotJudged`                            | Fact | Too few peer keys are not judged.                                  |
| `ThePercentileInterpolatesWithinABinAndStopsAtTheEnds`  | Fact | The percentile interpolates within a bin and stops at the ends.    |
| `ABinOfNoWidthCountsOnceTheValueIsPastIt`               | Fact | A bin of no width counts once the value is past it.                |

### JournalSummaryCalculatorTests

`Jube.Test.CaseRisk.Journal.JournalSummaryCalculatorTests` — 3 tests.
Category: Unit.

| Test                                              | Kind | Verifies                                                   |
|---------------------------------------------------|------|------------------------------------------------------------|
| `TheStripCountsTheSpanTheDaysAndTheActivations`   | Fact | The strip counts the span the days and the activations.    |
| `EachMeasureGetsItsTotalsAndTheLatestValueByTime` | Fact | Each measure gets its totals and the latest value by time. |
| `NothingGivesAnEmptySummary`                      | Fact | Nothing gives an empty summary.                            |

### CaseAnalyticsOperationsTests

`Jube.Test.CaseRisk.CaseAnalyticsOperationsTests` — 7 tests.
Category: Unit.

| Test                                                                    | Kind | Verifies                                                                             |
|-------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `ServiceLevelAttainmentCountsMetBreachedAndInFlightByDimension`         | Fact | Service level attainment counts met breached and in flight by dimension.             |
| `TheWatchListHoldsOnlyOpenCasesStillMissingATargetWorstFirst`           | Fact | The watch list holds only open cases still missing a target worst first.             |
| `WeeklyAttainmentGroupsByTheMondayOfTheWeekTheCaseWasCreated`           | Fact | Weekly attainment groups by the monday of the week the case was created.             |
| `TheRhythmCountsArrivalsByWeekdayAndHourAndFindsTheBusiestSlot`         | Fact | The rhythm counts arrivals by weekday and hour and finds the busiest slot.           |
| `WorkloadShowsTheCallersOwnFiguresAndOnlyShowsATeamOfThreeOrMore`       | Fact | Workload shows the callers own figures and only shows a team of three or more.       |
| `HandoffsAndReopensAreBucketedAndPingPongCasesAreListedWorstFirst`      | Fact | Handoffs and reopens are bucketed and ping pong cases are listed worst first.        |
| `OpenCasesAreCountedOncePerStateBreachedBeforeAtRiskAndClosedOnesNever` | Fact | Open cases are counted once per state breached before at risk and closed ones never. |

### CaseAnalyticsChartRendererTests

`Jube.Test.CaseRisk.CaseAnalyticsChartRendererTests` — 5 tests.
Category: Unit.

| Test                                                                      | Kind   | Verifies                                                                                  |
|---------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `EveryChartRendersAnSvgWithItsTitleInBothThemes`                          | Theory | Every chart renders an svg with its title in both themes.                                 |
| `TheChartNamesTheRulesAndStatusesNotTheirGuids`                           | Fact   | The chart names the rules and statuses not their guids.                                   |
| `AChartWithNothingBehindItSaysSoInsteadOfDrawingAnEmptyFrame`             | Theory | A chart with nothing behind it says so instead of drawing an empty frame.                 |
| `TheUnsureGainsAndCalibrationChartsSayTheyRestOnTooFewCases`              | Fact   | The unsure gains and calibration charts say they rest on too few cases.                   |
| `TheRiskMatrixPutsTheMostLikelyAtTheTopAndSaysHowManyCasesAreNotAssessed` | Fact   | The risk matrix puts the most likely at the top and says how many cases are not assessed. |

### CaseAnalyticsServiceTests

`Jube.Test.CaseRisk.CaseAnalyticsServiceTests` — 6 tests.
Category: Service. Collection: Database.

| Test                                                                         | Kind | Verifies                                                                              |
|------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `AStatusThatWasDeletedAndRecreatedWithTheSameGuidDoesNotBreakTheReportAsync` | Fact | A status that was deleted and recreated with the same guid does not break the report. |
| `TheWholeReportIsBuiltFromTheCasesTheirEventsStaysAndAssessmentsAsync`       | Fact | The whole report is built from the cases their events stays and assessments.          |
| `ThePeriodIsValidatedAndTheDefaultIsTheLastThirtyDaysAsync`                  | Fact | The period is validated and the default is the last thirty days.                      |
| `CasesOutsideThePeriodAreLeftOutExceptThoseStillOpenAsync`                   | Fact | Cases outside the period are left out except those still open.                        |
| `OnlyCasesTheCallerMaySeeAreCountedAndAWorkflowTheyLackIsNotFoundAsync`      | Fact | Only cases the caller may see are counted and a workflow they lack is not found.      |
| `ASignedOutUserIsRefusedAndOneWithoutThePermissionIsForbiddenAsync`          | Fact | A signed out user is refused and one without the permission is forbidden.             |

### AbstractionCalculationsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.AbstractionCalculationsExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                         | Kind | Verifies                                                                                    |
|------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `TheRuleResultIsStoredUnderTheCalculationName`                               | Fact | The rule result is stored under the calculation name.                                       |
| `TheRuleDelegateReceivesEveryUpstreamSource`                                 | Fact | The rule delegate receives every upstream source.                                           |
| `ALaterCalculationCanReadAnEarlierOneInTheSameInvocation`                    | Fact | A later calculation can read an earlier one in the same invocation.                         |
| `CalculationsRunInTheOrderTheyAreConfigured`                                 | Fact | Calculations run in the order they are configured.                                          |
| `AFiniteNegativeResultIsStoredUnchanged`                                     | Fact | A finite negative result is stored unchanged.                                               |
| `AnUndefinedResultIsStoredAsReturnedSoARuleThatWantsZeroUsesZeroIfUndefined` | Fact | An undefined result is stored as returned so a rule that wants zero uses zero if undefined. |
| `ReportTableAddsAnArchiveKeyWithProcessingTypeSix`                           | Fact | Report table adds an archive key with processing type six.                                  |
| `WithoutReportTableNoArchiveKeyIsAdded`                                      | Fact | Without report table no archive key is added.                                               |
| `AnExceptionThrownByTheRuleDelegateIsCaughtAndTheCalculationIsSkipped`       | Fact | An exception thrown by the rule delegate is caught and the calculation is skipped.          |
| `AnEntryWithNoCompiledDelegateIsSkippedWithoutThrowing`                      | Fact | An entry with no compiled delegate is skipped without throwing.                             |
| `WhenSampledTheStageTimingRecordsOneItemPerCalculation`                      | Fact | When sampled the stage timing records one item per calculation.                             |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns`                | Fact | When not sampled no stage timing is built but business logic still runs.                    |
| `WithNoCalculationsConfiguredNothingThrowsAndTheDictionaryStaysEmpty`        | Fact | With no calculations configured nothing throws and the dictionary stays empty.              |

### ActivationRuleActivationWatcherExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleActivationWatcherExtensionsTests` — 16 tests. Category: Unit.

| Test                                                                              | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `DoesNothingWhenTheRuleIsNotFlaggedForTheWatcherAsync`                            | Fact | Does nothing when the rule is not flagged for the watcher.                                |
| `DoesNothingWhenSuppressedAsync`                                                  | Fact | Does nothing when suppressed.                                                             |
| `DoesNothingWhenThisIsAReprocessingRunAsync`                                      | Fact | Does nothing when this is a reprocessing run.                                             |
| `DoesNothingWhenTheModelHasActivationWatcherDisabledAsync`                        | Fact | Does nothing when the model has activation watcher disabled.                              |
| `DoesNothingWhenTheWatchCountHasReachedTheThresholdAsync`                         | Fact | Does nothing when the watch count has reached the threshold.                              |
| `DoesNothingWhenTheRandomSampleDrawFailsAsync`                                    | Fact | Does nothing when the random sample draw fails.                                           |
| `PersistsToTheWatcherQueueWithFieldsCopiedFromTheRuleWhenPersistIsAllowedAsync`   | Fact | Persists to the watcher queue with fields copied from the rule when persist is allowed.   |
| `DoesNotPersistToTheWatcherQueueWhenPersistIsDisallowedAsync`                     | Fact | Does not persist to the watcher queue when persist is disallowed.                         |
| `UsesTheResponseElevationKeyValueFromThePayloadWhenPresentAsync`                  | Fact | Uses the response elevation key value from the payload when present.                      |
| `FallsBackToMissingForTheKeyValueWhenThePayloadDoesNotHaveItAsync`                | Fact | Falls back to missing for the key value when the payload does not have it.                |
| `PublishesToRespAndIncrementsTheCounterWhenStreamingIsEnabledAsync`               | Fact | Publishes to redis and increments the counter when streaming is enabled.                  |
| `DoesNotPublishToRespOrIncrementTheCounterWhenStreamingIsDisabledAsync`           | Fact | Does not publish to redis or increment the counter when streaming is disabled.            |
| `ARespFailureIsSwallowedInsideTheCacheRepositorySoTheCounterStillIncrementsAsync` | Fact | A redis failure is swallowed inside the cache repository so the counter still increments. |
| `CarriesTheEntitysPreviousLocationOntoTheNextRecordedActivationAsync`             | Fact | Carries the entitys previous location onto the next recorded activation.                  |
| `RemembersTheLocationEvenWhenTheSampleDeclinesToRecordTheActivationAsync`         | Fact | Remembers the location even when the sample declines to record the activation.            |
| `KeepsEntitiesApartAndIgnoresAnActivationWithNoLocationOrNoKeyValueAsync`         | Fact | Keeps entities apart and ignores an activation with no location or no key value.          |

### ActivationRuleActivationWatcherGeolocationTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleActivationWatcherGeolocationTests` — 5 tests. Category: Unit.

| Test                                                                        | Kind | Verifies                                                                               |
|-----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `GetLatitudeReadsFromTheXPathFlaggedWithDataTypeSix`                        | Fact | Get latitude reads from the x path flagged with data type six.                         |
| `GetLongitudeReadsFromTheXPathFlaggedWithDataTypeSeven`                     | Fact | Get longitude reads from the x path flagged with data type seven.                      |
| `GetLongitudeFallsBackToTheInlineScriptPropertyFlaggedLongitudeNotLatitude` | Fact | Get longitude falls back to the inline script property flagged longitude not latitude. |
| `GetLongitudeDoesNotMatchAPropertyFlaggedLatitudeOnly`                      | Fact | Get longitude does not match a property flagged latitude only.                         |
| `GetLatitudeAndGetLongitudeDefaultToZeroWhenNothingIsConfigured`            | Fact | Get latitude and get longitude default to zero when nothing is configured.             |

### ActivationRuleCreateCaseObjectExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleCreateCaseObjectExtensionsTests` — 15 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                            |
|--------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `ReturnsNullWhenCaseWorkflowIsDisabledAsync`                                   | Fact   | Returns null when case workflow is disabled.                                        |
| `ReturnsNullWhenSuppressedAsync`                                               | Fact   | Returns null when suppressed.                                                       |
| `ReturnsNullWhenThisIsAReprocessingRunAsync`                                   | Fact   | Returns null when this is a reprocessing run.                                       |
| `StillCreatesACaseOnAnOrdinaryRunAfterTheReprocessingGuardAsync`               | Fact   | Still creates a case on an ordinary run after the reprocessing guard.               |
| `PopulatesTenantAndCaseWorkflowIdentifiersFromTheRuleAsync`                    | Fact   | Populates tenant and case workflow identifiers from the rule.                       |
| `SelectsSuspendBypassAndComputesTheDateForEachIntervalAsync`                   | Theory | Selects suspend bypass and computes the date for each interval.                     |
| `DoesNotSetASuspendBypassDateForAnUnrecognisedIntervalAsync`                   | Fact   | Does not set a suspend bypass date for an unrecognised interval.                    |
| `OpensImmediatelyWhenTheBypassSampleDrawFailsAsync`                            | Fact   | Opens immediately when the bypass sample draw fails.                                |
| `UsesTheRulesCaseKeyAndPayloadValueWhenTheCaseKeyIsPresentAsync`               | Fact   | Uses the rules case key and payload value when the case key is present.             |
| `FallsBackToTheEntityInstanceEntryIdWhenTheCaseKeyIsNullAsync`                 | Fact   | Falls back to the entity instance entry id when the case key is null.               |
| `FallsBackToTheEntityInstanceEntryIdWhenTheCaseKeyIsNotInThePayloadAsync`      | Fact   | Falls back to the entity instance entry id when the case key is not in the payload. |
| `SkipsTheIdempotencyCacheEntirelyWhenActivationRuleIdempotencyIsDisabledAsync` | Fact   | Skips the idempotency cache entirely when activation rule idempotency is disabled.  |
| `ReturnsNullWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync`       | Fact   | Returns null when idempotency is enabled and the claim has already been taken.      |
| `CreatesTheCaseWhenIdempotencyIsEnabledAndTheClaimSucceedsAsync`               | Fact   | Creates the case when idempotency is enabled and the claim succeeds.                |
| `DifferentActivationRulesClaimIndependentIdempotencySlotsForTheSameEntryAsync` | Fact   | Different activation rules claim independent idempotency slots for the same entry.  |

### ActivationRuleTtlCounterExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleTtlCounterExtensionsTests` — 17 tests. Category: Unit.

| Test                                                                                     | Kind   | Verifies                                                                                                |
|------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------|
| `DoesNothingWhenTheRuleHasTtlCounterDisabledAsync`                                       | Fact   | Does nothing when the rule has ttl counter disabled.                                                    |
| `DoesNothingWhenThisIsAReprocessingRunAsync`                                             | Fact   | Does nothing when this is a reprocessing run.                                                           |
| `DoesNothingWhenNoModelMatchesTheTargetModelGuidAsync`                                   | Fact   | Does nothing when no model matches the target model guid.                                               |
| `DoesNothingWhenTheModelHasNoCounterMatchingTheCounterGuidAsync`                         | Fact   | Does nothing when the model has no counter matching the counter guid.                                   |
| `DoesNothingWhenThePayloadHasNoValueForTheCountersDataNameAsync`                         | Fact   | Does nothing when the payload has no value for the counters data name.                                  |
| `DoesNothingWhenTheTargetModelHasTtlCounterStorageDisabledAsync`                         | Fact   | Does nothing when the target model has ttl counter storage disabled.                                    |
| `IncrementsByOneAndAddsANewEntryWhenSumIsNotEnabledAndTheCounterHasNoExistingValueAsync` | Fact   | Increments by one and adds a new entry when sum is not enabled and the counter has no existing value.   |
| `AccumulatesTheExistingInMemoryCounterValueWhenIncrementedTwiceAsync`                    | Fact   | Accumulates the existing in memory counter value when incremented twice.                                |
| `SumsThePayloadValueWhenEnableSumIsSetOnTheCounterAsync`                                 | Fact   | Sums the payload value when enable sum is set on the counter.                                           |
| `WritesAnUpsertEntryToTheCacheWhenTheCounterDoesNotLiveForeverAsync`                     | Fact   | Writes an upsert entry to the cache when the counter does not live forever.                             |
| `DoesNotWriteAnUpsertEntryWhenTheCounterLivesForeverAsync`                               | Fact   | Does not write an upsert entry when the counter lives forever.                                          |
| `SkipsTheIdempotencyCacheEntirelyWhenActivationRuleIdempotencyIsDisabledAsync`           | Fact   | Skips the idempotency cache entirely when activation rule idempotency is disabled.                      |
| `DoesNotIncrementWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync`            | Fact   | Does not increment when idempotency is enabled and the claim has already been taken.                    |
| `ClaimsIdempotencyUnderTheCounterKeyWithTheEntryGuidAsTheMemberAsync`                    | Fact   | Idempotency claim is keyed by counter guid with the entry guid as set member, so cleanup can remove it. |
| `DoesNotIncrementOrClaimIdempotencyForANegativeOrNonFiniteSumValueAsync`                 | Theory | A negative or non-finite sum value is neither counted nor claimed.                                      |
| `IncrementsTheCounterUnderTheTargetModelGuidSoTheOwningModelReadsItAsync`                | Fact   | The live counter is keyed by the target model guid.                                                     |
| `StillIncrementsTheInMemoryCounterWhenTheBackgroundCacheWriteFailsAsync`                 | Fact   | Still increments the in memory counter when the background cache write fails.                           |

### ActivationRulesHelperExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRulesHelperExtensionsTests` — 42 tests. Category: Unit.

| Test                                                                                           | Kind   | Verifies                                                                                                      |
|------------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------------|
| `ActivationRuleGetOverridesDoesNotLetAModelLevelForceReachARuleThatIsNotEnabledForForce`       | Fact   | A model level Force does not reach a rule without Enable Force.                                               |
| `ActivationRuleGetOverridesDoesNotLetAModelLevelOverrideReachARuleBoundToAnotherOverrideKey`   | Fact   | A model level override does not reach a rule bound to another Override Key.                                   |
| `ActivationRuleGetOverridesDoesNotLetAModelLevelOverrideReachARuleThatIsNotEnabledForOverride` | Fact   | A model level override does not reach a rule without Enable Override.                                         |
| `ActivationRuleFinishResponseElevationIncrementsCounterWhenElevationIsPositive`                | Fact   | Activation rule finish response elevation increments counter when elevation is positive.                      |
| `ActivationRuleFinishResponseElevationDoesNotIncrementCounterWhenElevationIsZeroOrNegative`    | Theory | Activation rule finish response elevation does not increment counter when elevation is zero or negative.      |
| `ActivationRuleResponseElevationAddToCountersIncrementsAndJournalsWhenElevationIsPositive`     | Fact   | Activation rule response elevation add to counters increments and journals when elevation is positive.        |
| `ActivationRuleResponseElevationAddToCountersDoesNothingWhenElevationIsZero`                   | Fact   | Activation rule response elevation add to counters does nothing when elevation is zero.                       |
| `UpdateContextStateWithActivationRulesOutcomeSetsAllThreeFieldsOnThePayload`                   | Fact   | Update context state with activation rules outcome sets all three fields on the payload.                      |
| `UpdateContextStateWithActivationRulesOutcomeAllowsANullPrevailingId`                          | Fact   | Update context state with activation rules outcome allows a null prevailing id.                               |
| `CheckSuppressedResponseElevationIsFalseWhenQueueCountIsAtOrBelowTheFrequencyLimitCounter`     | Fact   | Check suppressed response elevation is false when queue count is at or below the frequency limit counter.     |
| `CheckSuppressedResponseElevationIsTrueWhenQueueCountExceedsTheFrequencyLimitCounter`          | Fact   | Check suppressed response elevation is true when queue count exceeds the frequency limit counter.             |
| `ProcessResponseElevationDoesNothingWhenSuppressed`                                            | Fact   | Process response elevation does nothing when suppressed.                                                      |
| `ProcessResponseElevationDoesNothingWhenResponseElevationIsDisabledOnTheRule`                  | Fact   | Process response elevation does nothing when response elevation is disabled on the rule.                      |
| `ProcessResponseElevationDoesNothingWhenTheRulesElevationDoesNotExceedTheCurrentHighWaterMark` | Fact   | Process response elevation does nothing when the rules elevation does not exceed the current high water mark. |
| `ProcessResponseElevationRaisesTheHighWaterMarkAndCarriesTheValueForwardWithinAllLimits`       | Fact   | Process response elevation raises the high water mark and carries the value forward within all limits.        |
| `ProcessResponseElevationTruncatesToTheModelsMaxResponseElevationAndIncrementsThatCounter`     | Fact   | Process response elevation truncates to the models max response elevation and increments that counter.        |
| `ProcessResponseElevationTruncatesToTheGatewaysResponseElevationLimitAndIncrementsThatCounter` | Fact   | Process response elevation truncates to the gateways response elevation limit and increments that counter.    |
| `ProcessResponseElevationCopiesContentRedirectAndColoursFromTheRule`                           | Fact   | Process response elevation copies content redirect and colours from the rule.                                 |
| `ProcessResponseElevationEnqueuesAJournalEntryOnlyWhenTheLimitFlagIsEnabled`                   | Fact   | Process response elevation enqueues a journal entry only when the limit flag is enabled.                      |
| `ProcessResponseElevationDoesNotEnqueueAJournalEntryWhenTheLimitFlagIsDisabled`                | Fact   | Process response elevation does not enqueue a journal entry when the limit flag is disabled.                  |
| `ActivationRuleCountsAndArchiveHighWatermarkDoesNothingWhenSuppressed`                         | Fact   | Activation rule counts and archive high watermark does nothing when suppressed.                               |
| `ActivationRuleCountsAndArchiveHighWatermarkIncrementsCountAndSetsPrevailingWhenVisible`       | Fact   | Activation rule counts and archive high watermark increments count and sets prevailing when visible.          |
| `ActivationRuleCountsAndArchiveHighWatermarkDoesNotIncrementCountWhenNotVisible`               | Fact   | Activation rule counts and archive high watermark does not increment count when not visible.                  |
| `ActivationRuleCountsAndArchiveHighWatermarkAccumulatesAcrossMultipleVisibleRules`             | Fact   | Activation rule counts and archive high watermark accumulates across multiple visible rules.                  |
| `ActivationRuleGetOverridesIgnoresARuleThatIsNotEnabledForOverride`                            | Fact   | Activation rule get overrides ignores a rule that is not enabled for override.                                |
| `ActivationRuleGetOverridesIgnoresAForceOnARuleThatIsNotEnabledForForce`                       | Fact   | Activation rule get overrides ignores a force on a rule that is not enabled for force.                        |
| `ActivationRuleGetOverridesStillSuppressesARuleThatIsNotEnabledForForce`                       | Fact   | Activation rule get overrides still suppresses a rule that is not enabled for force.                          |
| `ActivationRuleGetOverridesIgnoresARuleBoundToADifferentOverrideKey`                           | Fact   | Activation rule get overrides ignores a rule bound to a different override key.                               |
| `ActivationRuleGetOverridesHonoursARuleBoundToItsOwnOverrideKey`                               | Fact   | Activation rule get overrides honours a rule bound to its own override key.                                   |
| `ActivationRuleGetOverridesIgnoresARuleTheModelDoesNotHave`                                    | Fact   | Activation rule get overrides ignores a rule the model does not have.                                         |
| `ActivationRuleGetOverridesReturnsNoOverridesWhenNoOverrideXPathsAreConfigured`                | Fact   | Activation rule get overrides returns no overrides when no override x paths are configured.                   |
| `ActivationRuleGetOverridesReturnsNoOverridesWhenTheOverrideKeyIsNotInThePayload`              | Fact   | Activation rule get overrides returns no overrides when the override key is not in the payload.               |
| `ActivationRuleGetOverridesSuppressesAllActivationRulesWhenTheValueIsOverridden`               | Fact   | Activation rule get overrides suppresses all activation rules when the value is overridden.                   |
| `ActivationRuleGetOverridesStaysSuppressedWhenALaterKeyIsNotSuppressed`                        | Fact   | Activation rule get overrides stays suppressed when a later key is not suppressed.                            |
| `ActivationRuleGetOverridesIsSuppressedWhenOnlyALaterKeyIsSuppressed`                          | Fact   | Activation rule get overrides is suppressed when only a later key is suppressed.                              |
| `ActivationRuleGetOverridesIsNotSuppressedWhenNoKeyIsSuppressed`                               | Fact   | Activation rule get overrides is not suppressed when no key is suppressed.                                    |
| `ActivationRuleGetOverridesUnionsActivationRuleOverridesAcrossKeys`                            | Fact   | Activation rule get overrides unions activation rule overrides across keys.                                   |
| `ActivationRuleGetOverridesLetsForceWinOverSuppressAcrossKeys`                                 | Fact   | Activation rule get overrides lets force win over suppress across keys.                                       |
| `ActivationRuleGetOverridesLetsForceWinOverAnAllActivationRulesSuppress`                       | Fact   | Activation rule get overrides lets force win over an all activation rules suppress.                           |
| `ActivationRuleGetOverridesFallsBackToTheAllActivationRulesOverride`                           | Fact   | Activation rule get overrides falls back to the all activation rules override.                                |
| `ActivationRuleGetOverridesIgnoresXPathsThatDoNotHaveOverrideEnabled`                          | Fact   | Activation rule get overrides ignores x paths that do not have override enabled.                              |
| `ActivationRuleGetOverridesDoesNotThrowWhenDependenciesHaveNotSynchronised`                    | Fact   | Activation rule get overrides does not throw when dependencies have not synchronised.                         |

### DictionaryKvpExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.DictionaryKvpExtensionsTests` — 8 tests. Category: Unit.

| Test                                                                         | Kind | Verifies                                                                                 |
|------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `ResolvesTheConfiguredValueFromThePayloadIntoTheDictionaryCache`             | Fact | Resolves the configured value from the payload into the dictionary cache.                |
| `FieldNamesAreMatchedAgainstDataNameNotTheDictionarysOwnName`                | Fact | Field names are matched against data name not the dictionarys own name.                  |
| `AKeyNotPresentInTheLookupTableResolvesToZero`                               | Fact | A key not present in the lookup table resolves to zero.                                  |
| `AFieldMissingFromThePayloadResolvesToZero`                                  | Fact | A field missing from the payload resolves to zero.                                       |
| `DoesNotOverwriteAnAlreadyResolvedValueForTheSameDictionaryName`             | Fact | Does not overwrite an already resolved value for the same dictionary name.               |
| `MultipleDictionariesMappedToTheSameFieldAreAllResolvedIndependently`        | Fact | Multiple dictionaries mapped to the same field are all resolved independently.           |
| `ADictionaryMappedToADifferentFieldIsUnaffectedByAnUnrelatedFieldResolution` | Fact | A dictionary mapped to a different field is unaffected by an unrelated field resolution. |
| `WithNoKvpDictionariesConfiguredNothingThrowsAndTheDictionaryStaysEmpty`     | Fact | With no kvp dictionaries configured nothing throws and the dictionary stays empty.       |

### EntityAnalysisModelInvokeSamplingTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.EntityAnalysisModelInvokeSamplingTests` — 8 tests. Category: Unit.

| Test                                                                                         | Kind | Verifies                                                                                                     |
|----------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------------------|
| `WhenEnableSamplingIsOffEveryInvocationIsSampledRegardlessOfTheRandomDraw`                   | Fact | When enable sampling is off every invocation is sampled regardless of the random draw.                       |
| `WhenEnableSamplingIsOnAndTheDrawIsBelowThePercentageThresholdItIsSampled`                   | Fact | When enable sampling is on and the draw is below the percentage threshold it is sampled.                     |
| `WhenEnableSamplingIsOnAndTheDrawIsAboveThePercentageThresholdItIsNotSampled`                | Fact | When enable sampling is on and the draw is above the percentage threshold it is not sampled.                 |
| `WhenEnableSamplingIsOnAndSamplePercentageIsZeroNoDrawEverPasses`                            | Fact | When enable sampling is on and sample percentage is zero no draw ever passes.                                |
| `WhenEnableSamplingIsOnAndSamplePercentageIsOneHundredEveryDrawPasses`                       | Fact | When enable sampling is on and sample percentage is one hundred every draw passes.                           |
| `IsSampledOnThePayloadAlwaysMirrorsContextLogSampledExactly`                                 | Fact | Is sampled on the payload always mirrors context log sampled exactly.                                        |
| `WhenEnableLogsInfoIsOffEveryInvocationIsSampledEvenWithAStaleLowSamplePercentageConfigured` | Fact | When enable logs info is off every invocation is sampled even with a stale low sample percentage configured. |
| `WhenEnableLogsInfoIsOnSamplingStillThrottlesAsBefore`                                       | Fact | When enable logs info is on sampling still throttles as before.                                              |

### ExecuteAbstractionRulesWithoutSearchKeysExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ExecuteAbstractionRulesWithoutSearchKeysExtensionsTests` — 10 tests.
Category: Unit.

| Test                                                          | Kind | Verifies                                                                 |
|---------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `ARuleThatMatchesAddsOneToTheAbstractionDictionary`           | Fact | A rule that matches adds one to the abstraction dictionary.              |
| `ARuleThatDoesNotMatchAddsZeroToTheAbstractionDictionary`     | Fact | A rule that does not match adds zero to the abstraction dictionary.      |
| `ARuleFlaggedAsSearchIsSkippedEntirelyByThisExtension`        | Fact | A rule flagged as search is skipped entirely by this extension.          |
| `MultipleNonSearchRulesAllRunIndependently`                   | Fact | Multiple non search rules all run independently.                         |
| `AnExceptionThrownByARuleIsCaughtAndSubsequentRulesStillRun`  | Fact | An exception thrown by a rule is caught and subsequent rules still run.  |
| `AReportTableRuleAddsAnArchiveKeyWithTheAbstractionValue`     | Fact | A report table rule adds an archive key with the abstraction value.      |
| `ARuleWithoutReportTableDoesNotAddAnArchiveKey`               | Fact | A rule without report table does not add an archive key.                 |
| `WhenSampledTheStageTimingRecordsOneItemPerNonSearchRule`     | Fact | When sampled the stage timing records one item per non search rule.      |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns` | Fact | When not sampled no stage timing is built but business logic still runs. |
| `WithNoRulesConfiguredNothingThrowsAndAbstractionStaysEmpty`  | Fact | With no rules configured nothing throws and abstraction stays empty.     |

### ExecuteCacheEntriesExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ExecuteCacheEntriesExtensionsTests` — 16 tests. Category: Unit.

| Test                                                                                                    | Kind | Verifies                                                                                                                   |
|---------------------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------------------------|
| `InsertOrReplaceCacheEntriesQueuesNothingWhenCacheIsDisabled`                                           | Fact | Insert or replace cache entries queues nothing when cache is disabled.                                                     |
| `InsertOrReplaceCacheEntriesQueuesAnInsertAndWritesToPayloadWhenNotReprocessingAsync`                   | Fact | Insert or replace cache entries queues an insert and writes to payload when not reprocessing.                              |
| `ExecuteCacheDbStorageWritesNothingWhenReprocessingAsync`                                               | Fact | Execute cache db storage writes nothing when reprocessing.                                                                 |
| `ParseToReducedInternedPayloadAlwaysIncludesTheReferenceDateAtIndexMinusOne`                            | Fact | Parse to reduced interned payload always includes the reference date at index minus one.                                   |
| `ParseToReducedInternedPayloadExcludesAnInlineScriptAttributeWithNoCacheIndexId`                        | Fact | Parse to reduced interned payload excludes an inline script attribute with no cache index id.                              |
| `ParseToReducedInternedPayloadExcludesAnInlineScriptAttributeWhosePayloadKeyIsMissing`                  | Fact | Parse to reduced interned payload excludes an inline script attribute whose payload key is missing.                        |
| `ParseToReducedInternedPayloadIncludesACachedRequestXPathValueWhenCacheIsTrueAndPayloadHasIt`           | Fact | Parse to reduced interned payload includes a cached request x path value when cache is true and payload has it.            |
| `ParseToReducedInternedPayloadExcludesARequestXPathWithCacheFalse`                                      | Fact | Parse to reduced interned payload excludes a request x path with cache false.                                              |
| `ParseToReducedInternedPayloadExcludesAPayloadEntryNotConfiguredForCachingAtAll`                        | Fact | Parse to reduced interned payload excludes a payload entry not configured for caching at all.                              |
| `UpsertCachePayloadLatestWritesTheMatchingPayloadValueForEachSearchKeyAsync`                            | Fact | Upsert cache payload latest writes the matching payload value for each search key.                                         |
| `UpsertCachePayloadLatestStillQueuesAnUpsertWithAnEmptyValueWhenThePayloadHasNoMatchAsync`              | Fact | Upsert cache payload latest still queues an upsert with an empty value when the payload has no match.                      |
| `UpsertCachePayloadLatestHandlesMultipleSearchKeysIndependentlyAsync`                                   | Fact | Upsert cache payload latest handles multiple search keys independently.                                                    |
| `ExecuteCacheDbStorageRecordsStageTimingWhenLogSampledIsTrue`                                           | Fact | Execute cache db storage records stage timing when log sampled is true.                                                    |
| `ExecuteCacheDbStorageDoesNotRecordStageTimingWhenLogSampledIsFalse`                                    | Fact | Execute cache db storage does not record stage timing when log sampled is false.                                           |
| `ExecuteCacheDbStorageQueuesBothPayloadAndPayloadLatestWrites`                                          | Fact | Execute cache db storage queues both payload and payload latest writes.                                                    |
| `ParseToReducedInternedPayloadIncludesAnInlineScriptAttributeValueWhenCacheIndexIdIsSetAndPayloadHasIt` | Fact | Parse to reduced interned payload includes an inline script attribute value when cache index id is set and payload has it. |

### ExhaustiveAdaptationsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ExhaustiveAdaptationsExtensionsTests` — 16 tests. Category: Unit.

| Test                                                                              | Kind   | Verifies                                                                                       |
|-----------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `AddsTheRecalledScoreToTheExhaustiveAdaptationDictionaryUnderTheModelsName`       | Fact   | Adds the recalled score to the exhaustive adaptation dictionary under the models name.         |
| `PayloadValuesAreExtractedAndPassedThroughTheNetworkWithZScoreApplied`            | Fact   | Payload values are extracted and passed through the network with z score applied.              |
| `EachNonPayloadProcessingTypeIsExtractedFromItsOwnDictionaryAndRecalledCorrectly` | Theory | Each non payload processing type is extracted from its own dictionary and recalled correctly.  |
| `AnUnrecognisedProcessingTypeFallsBackToAbstraction`                              | Fact   | An unrecognised processing type falls back to abstraction.                                     |
| `AnAbstractionCalculationTrainedAsProcessingTypeSevenIsReadFromTheCalculationsNotTheMean` | Fact | An abstraction calculation trained as processing type seven is read from the calculations not the mean. |
| `AnAbstractionRuleTrainedAsProcessingTypeSevenStillReadsTheAbstractionDictionary` | Fact | An abstraction rule trained as processing type seven still reads the abstraction dictionary. |
| `AMissingValueFallsBackToTheVariablesConfiguredMean`                              | Fact   | A missing value falls back to the variables configured mean.                                   |
| `ANameContainingADotUsesOnlyTheSegmentAfterTheFirstDotAsTheLookupKey`             | Fact   | A name containing a dot uses only the segment after the first dot as the lookup key.           |
| `NormalisationTypeTwoAppliesTheZScoreFormulaBeforeRecall`                         | Fact   | Normalisation type two applies the z score formula before recall.                              |
| `AnExceptionDuringOneModelsRecallIsCaughtAndSubsequentModelsStillRun`             | Fact   | An exception during one models recall is caught and subsequent models still run.               |
| `WhenSampledTheStageTimingRecordsOneItemPerModel`                                 | Fact   | When sampled the stage timing records one item per model.                                      |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns`                     | Fact   | When not sampled no stage timing is built but business logic still runs.                       |
| `WithNoModelsConfiguredNothingThrowsAndTheDictionaryStaysEmpty`                   | Fact   | With no models configured nothing throws and the dictionary stays empty.                       |
| `TheExplainabilityDetailCarriesTheSameValueAsTheScoreAndReconstructsTheRawOutput` | Fact   | The explainability detail carries the same value as the score and reconstructs the raw output. |
| `AnUncalibratedModelReportsNotCalibratedAndAScoreSpaceContribution`               | Fact   | An uncalibrated model reports not calibrated and a score space contribution.                   |
| `ContributionSourceIsMappedFromTheVariablesProcessingTypeId`                      | Fact   | Contribution source is mapped from the variables processing type id.                           |

### GatewayRulesExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.GatewayRulesExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `ARuleThatMatchesSetsMatchedGatewayRuleAndTheResponseElevationLimit`      | Fact | A rule that matches sets matched gateway rule and the response elevation limit.         |
| `ARuleThatDoesNotMatchLeavesMatchedGatewayRuleFalse`                      | Fact | A rule that does not match leaves matched gateway rule false.                           |
| `ARuleIsSkippedEntirelyWhenTheRandomGatewaySampleIsAtOrAboveItsThreshold` | Fact | A rule is skipped entirely when the random gateway sample is at or above its threshold. |
| `ARuleRunsWhenTheRandomGatewaySampleIsBelowItsThreshold`                  | Fact | A rule runs when the random gateway sample is below its threshold.                      |
| `TheFirstMatchingRuleStopsEvaluationOfSubsequentRules`                    | Fact | The first matching rule stops evaluation of subsequent rules.                           |
| `ARuleThatDoesNotMatchAllowsTheNextRuleToStillBeEvaluated`                | Fact | A rule that does not match allows the next rule to still be evaluated.                  |
| `AnExceptionThrownByARuleIsCaughtAndSubsequentRulesStillRun`              | Fact | An exception thrown by a rule is caught and subsequent rules still run.                 |
| `EvaluationCounterIncrementsForEveryRuleTestedRegardlessOfMatch`          | Fact | Evaluation counter increments for every rule tested regardless of match.                |
| `ActivationCounterOnlyIncrementsWhenTheRuleActuallyMatches`               | Fact | Activation counter only increments when the rule actually matches.                      |
| `ModelInvokeGatewayCounterIncrementsOnTheModelWhenARuleMatches`           | Fact | Model invoke gateway counter increments on the model when a rule matches.               |
| `WhenSampledTheGatewayStageTimingIsPopulatedWithOneItemPerRuleTested`     | Fact | When sampled the gateway stage timing is populated with one item per rule tested.       |
| `WhenNotSampledNoStageTimingIsBuiltAtAll`                                 | Fact | When not sampled no stage timing is built at all.                                       |
| `WithNoRulesConfiguredAtAllNothingMatchesAndNoExceptionIsThrown`          | Fact | With no rules configured at all nothing matches and no exception is thrown.             |

### InlineFunctionsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.InlineFunctionsExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                       | Kind | Verifies                                                                               |
|----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `AStringResultIsAddedToThePayload`                                         | Fact | A string result is added to the payload.                                               |
| `AnIntegerResultIsAddedToThePayload`                                       | Fact | An integer result is added to the payload.                                             |
| `ADoubleResultIsAddedToThePayload`                                         | Fact | A double result is added to the payload.                                               |
| `ABooleanResultIsAddedToThePayload`                                        | Fact | A boolean result is added to the payload.                                              |
| `ANullStringResultIsAddedToThePayloadAsNullRatherThanThrowing`             | Fact | A null string result is added to the payload as null rather than throwing.             |
| `DoesNotOverwriteAnExistingPayloadValueWithTheSameName`                    | Fact | Does not overwrite an existing payload value with the same name.                       |
| `ReportTableAddsAStringArchiveKeyWithProcessingTypeThree`                  | Fact | Report table adds a string archive key with processing type three.                     |
| `ReportTableAddsADoubleArchiveKeyWithTheNumericValue`                      | Fact | Report table adds a double archive key with the numeric value.                         |
| `WithoutReportTableNoArchiveKeyIsAdded`                                    | Fact | Without report table no archive key is added.                                          |
| `AnExceptionThrownByTheRuleDelegateIsCaughtAndSubsequentFunctionsStillRun` | Fact | An exception thrown by the rule delegate is caught and subsequent functions still run. |
| `WhenSampledTheStageTimingRecordsOneItemPerFunction`                       | Fact | When sampled the stage timing records one item per function.                           |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns`              | Fact | When not sampled no stage timing is built but business logic still runs.               |
| `WithNoFunctionsConfiguredNothingThrowsAndThePayloadStaysUntouched`        | Fact | With no functions configured nothing throws and the payload stays untouched.           |

### InlineScriptsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.InlineScriptsExtensionsTests` — 15 tests. Category: Unit.

| Test                                                                                      | Kind | Verifies                                                                                         |
|-------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `AScriptWithNoPayloadEventIsNeverInvokedAsync`                                            | Fact | A script with no payload event is never invoked.                                                 |
| `AStringPropertyIsAddedToThePayloadAsync`                                                 | Fact | A string property is added to the payload.                                                       |
| `AnIntPropertyIsAddedToThePayloadAsync`                                                   | Fact | An int property is added to the payload.                                                         |
| `ADoublePropertyIsAddedToThePayloadAsync`                                                 | Fact | A double property is added to the payload.                                                       |
| `ANullPropertyValueIsSkippedAsync`                                                        | Fact | A null property value is skipped.                                                                |
| `AnUnsupportedPropertyTypeIsSkippedAsync`                                                 | Fact | An unsupported property type is skipped.                                                         |
| `ReportTableAddsAnArchiveKeyWithProcessingTypeOneAsync`                                   | Fact | Report table adds an archive key with processing type one.                                       |
| `WithoutReportTableNoArchiveKeyIsAddedAsync`                                              | Fact | Without report table no archive key is added.                                                    |
| `WhenExecuteAsyncDelegateReturnsFalsePropertiesAreNotProcessedAsync`                      | Fact | When execute async delegate returns false properties are not processed.                          |
| `AnExceptionThrownByTheExecuteDelegateIsCaughtAndSubsequentScriptsStillRunAsync`          | Fact | An exception thrown by the execute delegate is caught and subsequent scripts still run.          |
| `WhenTheExecuteDelegateThrowsPropertiesAreNotPopulatedFromThePossiblyBrokenInstanceAsync` | Fact | When the execute delegate throws properties are not populated from the possibly broken instance. |
| `WhenSampledTheStageTimingRecordsOneItemPerPayloadScriptAsync`                            | Fact | When sampled the stage timing records one item per payload script.                               |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRunsAsync`                        | Fact | When not sampled no stage timing is built but business logic still runs.                         |
| `TheResponsePayloadJObjectIsClearedAfterExecutionAsync`                                   | Fact | The response payload j object is cleared after execution.                                        |
| `WithNoScriptsConfiguredNothingThrowsAsync`                                               | Fact | With no scripts configured nothing throws.                                                       |

### IterateActivationRulesExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.IterateActivationRulesExtensionsTests` — 30 tests. Category: Unit.

| Test                                                                                  | Kind | Verifies                                                                                        |
|---------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ReturnsZeroCountAndNoCaseWhenThereAreNoActivationRulesAsync`                         | Fact | Returns zero count and no case when there are no activation rules.                              |
| `ARuleThatDoesNotMatchIsNotAddedToActivationsAndDoesNotCountAsync`                    | Fact | A rule that does not match is not added to activations and does not count.                      |
| `AMatchedVisibleRuleIsAddedToActivationsAndBecomesPrevailingAsync`                    | Fact | A matched visible rule is added to activations and becomes prevailing.                          |
| `AMatchedButNotVisibleRuleIsAddedToActivationsButDoesNotCountOrBecomePrevailingAsync` | Fact | A matched but not visible rule is added to activations but does not count or become prevailing. |
| `TheLastMatchedVisibleRuleInIterationOrderBecomesPrevailingAsync`                     | Fact | The last matched visible rule in iteration order becomes prevailing.                            |
| `ARuleThatFailsActivationSamplingIsSkippedEntirelyAsync`                              | Fact | A rule that fails activation sampling is skipped entirely.                                      |
| `AForceOverrideActivatesARuleThatWouldNotOtherwiseMatchAsync`                         | Fact | A force override activates a rule that would not otherwise match.                               |
| `AForceOverrideBypassesActivationSamplingAsync`                                       | Fact | A force override bypasses activation sampling.                                                  |
| `AForceOverrideOnlyAppliesToTheRuleItIsBoundToAsync`                                  | Fact | A force override only applies to the rule it is bound to.                                       |
| `AForcedActivationIsMarkedForcedOnThePayloadAndCountedSeparatelyAsync`                | Fact | A forced activation is marked forced on the payload and counted separately.                     |
| `AGenuineMatchIsNotMarkedForcedAndDoesNotRaiseTheForcedCounterAsync`                  | Fact | A genuine match is not marked forced and does not raise the forced counter.                     |
| `ARuleSuppressedAtTheModelLevelIsStillEvaluatedButNotCountedTowardsPrevailingAsync`   | Fact | A rule suppressed at the model level is still evaluated but not counted towards prevailing.     |
| `ARuleSuppressedAtTheRuleLevelViaAnotherModelsOverrideListIsNotCountedAsync`          | Fact | A rule suppressed at the rule level via another models override list is not counted.            |
| `ARuleDisabledForReprocessingIsSuppressedDuringAReprocessingRunAsync`                 | Fact | A rule disabled for reprocessing is suppressed during a reprocessing run.                       |
| `AddsAnArchiveKeyWhenReportTableIsEnabledOnAMatchedRuleAsync`                         | Fact | Adds an archive key when report table is enabled on a matched rule.                             |
| `DoesNotAddAnArchiveKeyWhenReportTableIsDisabledAsync`                                | Fact | Does not add an archive key when report table is disabled.                                      |
| `TwoRulesSharingTheSameNameOnlyRecordTheFirstMatchAsync`                              | Fact | Two rules sharing the same name only record the first match.                                    |
| `AnInlineScriptOverrideCanFlipANonMatchingRuleToMatchedAsync`                         | Fact | An inline script override can flip a non matching rule to matched.                              |
| `AnInlineScriptThatReturnsFalseDoesNotFlipAMatchAsync`                                | Fact | An inline script that returns false does not flip a match.                                      |
| `OnlyTheFirstMatchedRuleWithCaseWorkflowEnabledCreatesACaseAsync`                     | Fact | Only the first matched rule with case workflow enabled creates a case.                          |
| `AReprocessingEligibleRuleDoesNotCreateACaseDuringAReprocessingRunAsync`              | Fact | A reprocessing eligible rule does not create a case during a reprocessing run.                  |
| `ATtlCounterIsStillIncrementedForAMatchedRuleEvenWhenSuppressedAtTheModelLevelAsync`  | Fact | A ttl counter is still incremented for a matched rule even when suppressed at the model level.  |
| `ARuleThatThrowsDuringProcessingIsLoggedAndDoesNotStopSubsequentRulesAsync`           | Fact | A rule that throws during processing is logged and does not stop subsequent rules.              |
| `EveryEvaluatedRuleGetsATimingEntryWithRuleDurationAsync`                             | Fact | Every evaluated rule gets a timing entry with rule duration.                                    |
| `AMatchedShadowRuleIsRecordedAsShadowAndNotAddedToActivationsAsync`                   | Fact | A matched shadow rule is recorded under shadow activation and not added to activations.         |
| `AMatchedShadowRuleDoesNotCountBecomePrevailingOrCreateACaseAsync`                    | Fact | A matched shadow rule does not count, become prevailing or create a case.                       |
| `AMatchedShadowRuleWithAReportTableWritesNoArchiveKeyAsync`                           | Fact | A matched shadow rule with a report table writes no archive key.                                |
| `AShadowRuleThatDoesNotMatchRecordsNothingAsync`                                      | Fact | A shadow rule that does not match records nothing.                                              |
| `ANonShadowRuleIsNeverRecordedAsShadowAsync`                                          | Fact | A non-shadow rule is never recorded as shadow.                                                  |
| `ALiveAndAShadowRuleTogetherKeepTheirOutcomesSeparateAsync`                           | Fact | A live and a shadow rule together keep their outcomes separate.                                 |

### ReferenceDateExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ReferenceDateExtensionsTests` — 6 tests. Category: Unit.

| Test                                                                           | Kind | Verifies                                                                                |
|--------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `FirstEverCallForAModelUpsertsSinceNoReferenceDateIsStoredYetAsync`            | Fact | First ever call for a model upserts since no reference date is stored yet.              |
| `ANewerReferenceDateOverwritesTheStoredOneAsync`                               | Fact | A newer reference date overwrites the stored one.                                       |
| `AnOlderOutOfOrderReferenceDateIsRejectedAndDoesNotOverwriteTheStoredOneAsync` | Fact | An older out of order reference date is rejected and does not overwrite the stored one. |
| `DifferentTenantsWithTheSameModelGuidDoNotCollideAsync`                        | Fact | Different tenants with the same model guid do not collide.                              |
| `AFutureReferenceDateThrowsAndDoesNotUpsertAsync`                              | Fact | A future reference date throws and does not upsert.                                     |
| `AReferenceDateEqualToTheStoredOneIsAcceptedAndUpsertedAsync`                  | Fact | A reference date equal to the stored one is accepted and upserted.                      |

### ResponseTimePipelineExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ResponseTimePipelineExtensionsTests` — 7 tests. Category: Unit.

| Test                                                                        | Kind | Verifies                                                                               |
|-----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `RecordResponseTimeWhenNotSampledDoesNotCreatePipelineOrEntries`            | Fact | Record response time when not sampled does not create pipeline or entries.             |
| `RecordResponseTimeWhenSampledAddsOneEntryWithTheGivenStageName`            | Fact | Record response time when sampled adds one entry with the given stage name.            |
| `RecordResponseTimeAccumulatesOneEntryPerCallAcrossStages`                  | Fact | Record response time accumulates one entry per call across stages.                     |
| `RecordResponseTimeUpdatesLastResponseTimeElapsedMicrosecondsToTheNewEntry` | Fact | Record response time updates last response time elapsed microseconds to the new entry. |
| `RecordResponseTimeClampsAllocatedBytesToZeroWhenTheDeltaWouldBeNegative`   | Fact | Record response time clamps allocated bytes to zero when the delta would be negative.  |
| `RecordResponseTimeUsesStartBytesUsedWhenNoPriorCheckpointExists`           | Fact | Record response time uses start bytes used when no prior checkpoint exists.            |
| `RecordResponseTimeNeverProducesANegativeDurationOnTheVeryFirstCall`        | Fact | Record response time never produces a negative duration on the very first call.        |

### SanctionsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.SanctionsExtensionsTests` — 17 tests. Category: Unit.

| Test                                                                            | Kind | Verifies                                                                                            |
|---------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `ExecuteSanctionsAsyncWithAnExactMatchPopulatesTheSanctionResponseAsync`        | Fact | Execute sanctions async with an exact match populates the sanction response.                        |
| `ExecuteSanctionsAsyncWithNoMatchDoesNotPopulateTheSanctionResponseAsync`       | Fact | Execute sanctions async with no match does not populate the sanction response.                      |
| `ExecuteSanctionsAsyncWhenPayloadIsMissingTheConfiguredFieldSkipsTheCheckAsync` | Fact | Execute sanctions async when payload is missing the configured field skips the check.               |
| `ExecuteSanctionsAsyncDispatchesToTheConfiguredAggregationTypeAsync`            | Fact | Execute sanctions async dispatches to the configured aggregation type.                              |
| `ExecuteSanctionsAsyncEvaluatesEveryConfiguredCheckIndependentlyAsync`          | Fact | Execute sanctions async evaluates every configured check independently.                             |
| `ExecuteSanctionsAsyncRecordsPerCheckTaskPerformanceWhenLogSampledAsync`        | Fact | Execute sanctions async records per check task performance when log sampled.                        |
| `ExecuteSanctionsAsyncOnCacheMissWritesTheComputedValueIntoCacheAsync`          | Fact | Execute sanctions async on cache miss writes the computed value into cache.                         |
| `ExecuteSanctionsAsyncOnANoMatchDoesNotWriteAnythingToCacheAsync`               | Fact | Execute sanctions async on a no match does not write anything to cache.                             |
| `ExecuteSanctionsAsyncOnAFreshNonExpiredCacheHitDoesNotRecomputeAsync`          | Fact | Execute sanctions async on a fresh non expired cache hit does not recompute.                        |
| `ExecuteSanctionsAsyncTwoChecksWithTheSameDistanceDoNotShareACachedResultAsync` | Fact | Two checks with the same distance but a different name or aggregation do not share a cached result. |
| `ExecuteSanctionsAsyncOnAnExpiredCacheHitRecomputesFromLiveEntriesAsync`        | Fact | Execute sanctions async on an expired cache hit recomputes from live entries.                       |
| `ExecuteSanctionsAsyncOnANonExpiredCachedNullValueStillRecomputesAsync`         | Fact | Execute sanctions async on a non expired cached null value still recomputes.                        |
| `ExecuteSanctionsAsyncCacheKeyIsScopedByDistanceThresholdAsync`                 | Fact | Execute sanctions async cache key is scoped by distance threshold.                                  |
| `ExecuteSanctionsAsyncRecordsTheMatchedEntriesClosestFirstAndCapsThemAsync`     | Fact | The matched entries are recorded closest first and capped at the configured maximum.                |
| `ExecuteSanctionsAsyncRecordsNoMatchDetailWhenThereIsNoMatchAsync`              | Fact | No match detail is recorded when there is no match.                                                 |
| `ExecuteSanctionsAsyncRecordsNoMatchDetailWhenTheSettingIsZeroAsync`            | Fact | No match detail is recorded when the setting is zero, though the aggregate still is.                |
| `ExecuteSanctionsAsyncRestoresTheMatchDetailFromACachedResultAsync`             | Fact | The match detail is restored from a cached result.                                                  |

### SnapshotPinningTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.SnapshotPinningTests` — 2 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `AnInvocationContextKeepsThePinnedGenerationWhenTheModelPublishesANewOne` | Fact | An invocation context keeps the pinned generation when the model publishes a new one. |
| `AContextWithoutAPinnedSnapshotFollowsTheModelsCurrentOne`                | Fact | A context without a pinned snapshot follows the models current one.                   |

### TtlCounterExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.TtlCounterExtensionsTests` — 14 tests. Category: Unit.

| Test                                                                            | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `ApplyTtlCounterIntervalSubtractsTheGivenUnitFromReferenceDate`                 | Theory | Apply ttl counter interval subtracts the given unit from reference date.                    |
| `ApplyTtlCounterIntervalSubtractsMonthsForM`                                    | Fact   | Apply ttl counter interval subtracts months for m.                                          |
| `ApplyTtlCounterIntervalSubtractsYearsForY`                                     | Fact   | Apply ttl counter interval subtracts years for y.                                           |
| `ApplyTtlCounterIntervalWithAVerySmallValueSupportsVeryShortLivedCounters`      | Fact   | Apply ttl counter interval with a very small value supports very short lived counters.      |
| `ApplyTtlCounterIntervalWithZeroValueReturnsReferenceDateUnchanged`             | Fact   | Apply ttl counter interval with zero value returns reference date unchanged.                |
| `ApplyTtlCounterIntervalWithAnUnrecognisedIntervalLeavesReferenceDateUnchanged` | Fact   | Apply ttl counter interval with an unrecognised interval leaves reference date unchanged.   |
| `ApplyTtlCounterIntervalWithANegativeValuePushesTheWindowIntoTheFutureInstead`  | Fact   | Apply ttl counter interval with a negative value pushes the window into the future instead. |
| `FiveSecondWindowIncludesEntriesAtBothInclusiveBoundariesAsync`                 | Fact   | Five second window includes entries at both inclusive boundaries.                           |
| `FiveSecondWindowExcludesEntriesJustOutsideEitherBoundaryAsync`                 | Fact   | Five second window excludes entries just outside either boundary.                           |
| `OneMinuteWindowIncludesEntriesInsideAndExcludesJustOutsideAsync`               | Fact   | One minute window includes entries inside and excludes just outside.                        |
| `ZeroLengthWindowOnlyIncludesAnEntryExactlyAtReferenceDateAsync`                | Fact   | Zero length window only includes an entry exactly at reference date.                        |
| `DifferentDataValuesForTheSameCounterDoNotCollideAsync`                         | Fact   | Different data values for the same counter do not collide.                                  |
| `DoesNothingWhenModelLevelEnableTtlCounterIsFalseAsync`                         | Fact   | Does nothing when model level enable ttl counter is false.                                  |
| `DoesNothingWhenThePayloadHasNoValueForTheCountersDataNameAsync`                | Fact   | Does nothing when the payload has no value for the counters data name.                      |

### WaitTasksExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.WaitTasksExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                                                | Kind   | Verifies                                                                                                     |
|-----------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------|
| `WaitReadTasksAsyncWithNoPendingTasksLeavesAnEmptyReadStatsBlockAsync`                              | Fact   | Wait read tasks async with no pending tasks leaves an empty read stats block.                                |
| `WaitReadTasksAsyncRecordsSanctionsAsyncComputeTimeAndMemoryAsync`                                  | Fact   | Wait read tasks async records sanctions async compute time and memory.                                       |
| `WaitReadTasksAsyncSkipsAFaultedTaskWithoutThrowingAsync`                                           | Fact   | Wait read tasks async skips a faulted task without throwing.                                                 |
| `WaitReadTasksAsyncWhenSampledRecordsTheJoinReadTasksStageDurationAsync`                            | Fact   | Wait read tasks async when sampled records the join read tasks stage duration.                               |
| `WaitReadTasksAsyncWhenNotSampledDoesNotRecordAnyStageAsync`                                        | Fact   | Wait read tasks async when not sampled does not record any stage.                                            |
| `WaitWriteTasksAsyncWithNoPendingTasksLeavesAnEmptyWriteStatsBlockAsync`                            | Fact   | Wait write tasks async with no pending tasks leaves an empty write stats block.                              |
| `WaitWriteTasksAsyncRecordsEachRecognisedWriteTaskTypeAsync`                                        | Theory | Wait write tasks async records each recognised write task type.                                              |
| `WaitWriteTasksAsyncSkipsAFaultedTaskWithoutThrowingAsync`                                          | Fact   | Wait write tasks async skips a faulted task without throwing.                                                |
| `WaitWriteTasksAsyncIgnoresATaskTypeItDoesNotRecogniseAsync`                                        | Fact   | Wait write tasks async ignores a task type it does not recognise.                                            |
| `WaitWriteTasksAsyncWhenSampledRecordsTheJoinWriteTasksStageDurationAsync`                          | Fact   | Wait write tasks async when sampled records the join write tasks stage duration.                             |
| `WaitWriteTasksAsyncWhenNotSampledDoesNotRecordAnyStageAsync`                                       | Fact   | Wait write tasks async when not sampled does not record any stage.                                           |
| `WaitWriteTasksAsyncRecordsMultipleDistinctWriteTaskTypesIndependentlyAsync`                        | Fact   | Wait write tasks async records multiple distinct write task types independently.                             |
| `WaitReadTasksAsyncRecordsTtlCountersAsyncAndAbstractionRulesWithSearchKeysAsyncIndependentlyAsync` | Fact   | Wait read tasks async records ttl counters async and abstraction rules with search keys async independently. |

## Entity analysis model invoke context extensions abstraction rules with search keys

### EntityAnalysisModelAbstractionRuleExecutionUtilityTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ContextExtensions.AbstractionRulesWithSearchKeys.EntityAnalysisModelAbstractionRuleExecutionUtilityTests` — 8 tests. Category: Unit.

| Test                                                                                 | Kind | Verifies                                                                                     |
|--------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `MatchesAreTheDocumentsTheRuleAcceptsInFetchOrderWithTheCurrentTransactionLastAsync` | Fact | Matches are the documents the rule accepts in fetch order with the current transaction last. |
| `MatchesOutsideTheRuleWindowAreDroppedAfterMatchingAsync`                            | Fact | Matches outside the rule window are dropped after matching.                                  |
| `TheSearchKeyTtlShortensTheWindowWhenItIsTighterThanTheRuleIntervalAsync`            | Fact | The search key ttl shortens the window when it is tighter than the rule interval.            |
| `RulesSharingALogicHashEvaluateEachDocumentOnceAndStillApplyTheirOwnWindowsAsync`    | Fact | Rules sharing a logic hash evaluate each document once and still apply their own windows.    |
| `RulesForAnotherSearchKeyOrWithoutSearchAreNotEvaluatedAsync`                        | Fact | Rules for another search key or without search are not evaluated.                            |
| `ARuleThatThrowsLeavesNoEntryAndDoesNotStopTheOtherRulesAsync`                       | Fact | A rule that throws leaves no entry and does not stop the other rules.                        |
| `SortedSetKeysWithNoPayloadAreSkippedAsync`                                          | Fact | Sorted set keys with no payload are skipped.                                                 |
| `ARuleWithNoMatchesStillRecordsAnEmptyEntryAsync`                                    | Fact | A rule with no matches still records an empty entry.                                         |

### AbstractionRuleVariableScanTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ContextExtensions.AbstractionRulesWithSearchKeys.AbstractionRuleVariableScanTests` — 6 tests. Category: Unit.

| Test                                                                   | Kind | Verifies                                                                            |
|------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `AScanVariableStartsAfreshForEveryScanOfTheSameCompiledRule`           | Fact | A scan variable starts afresh for every scan of the same compiled rule.             |
| `ScansRunningAtTheSameTimeDoNotShareTheirVariables`                    | Fact | Scans running at the same time do not share their variables.                        |
| `ARuleTestedOneTransactionAtATimeStartsItsScanVariablesAfreshEachTime` | Fact | A rule tested one transaction at a time starts its scan variables afresh each time. |
| `ARuleCalledWithoutACursorStillRunsAndKeepsNothingBetweenCalls`        | Fact | A rule called without a cursor still runs and keeps nothing between calls.          |
| `TheScanStateIsHeldByTheCursorAndReleasedWhenTheNextScanBegins`        | Fact | The scan state is held by the cursor and released when the next scan begins.        |
| `ARuleWithOnlyRecordVariablesKeepsNoScanState`                         | Fact | A rule with only record variables keeps no scan state.                              |

### EntityAnalysisModelAbstractionRuleAggregatorUtilityTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ContextExtensions.AbstractionRulesWithSearchKeys.EntityAnalysisModelAbstractionRuleAggregatorUtilityTests` — 58 tests. Category: Unit.

| Test                                                                                                      | Kind   | Verifies                                                                                                                   |
|-----------------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------|
| `AggregateReturnsZeroWhenNoMatchesWereRecordedForTheAbstractionRule`                                      | Fact   | Aggregate returns zero when no matches were recorded for the abstraction rule.                                             |
| `AggregateReturnsTheCursorResultCarriedByTheScanForTheCursorResultFunction`                               | Fact   | Aggregate returns the cursor result carried by the scan for the cursor result function.                                    |
| `AggregateReturnsZeroForTheCursorResultFunctionWhenTheMatchesDidNotComeFromACursorScan`                   | Fact   | Aggregate returns zero for the cursor result function when the matches did not come from a cursor scan.                    |
| `AggregateReturnsZeroWhenTheMatchedListForTheRuleIsPresentButEmpty`                                       | Fact   | Aggregate returns zero when the matched list for the rule is present but empty.                                            |
| `AggregateThrowsInsteadOfDegradingToZeroWhenTheAbstractionRuleItselfIsNull`                               | Fact   | Aggregate throws instead of degrading to zero when the abstraction rule itself is null.                                    |
| `ResolveSkipAndFetchIgnoresTheOffsetConfigurationEntirelyWhenOffsetIsDisabled`                            | Theory | Resolve skip and fetch ignores the offset configuration entirely when offset is disabled.                                  |
| `ResolveSkipAndFetchAppliesTheConfiguredOffsetTypeWhenOffsetIsEnabled`                                    | Theory | Resolve skip and fetch applies the configured offset type when offset is enabled.                                          |
| `OffsetFirstSelectsTheRecordAtThePositionGivenByTheOffsetValue`                                           | Fact   | Offset first selects the record at the position given by the offset value.                                                 |
| `OffsetLastCountsBackwardsFromTheMostRecentRecord`                                                        | Fact   | Offset last counts backwards from the most recent record.                                                                  |
| `OffsetSkipFirstSumsEverythingExceptTheOldestRecords`                                                     | Fact   | Offset skip first sums everything except the oldest records.                                                               |
| `OffsetTakeLastSumsOnlyTheMostRecentRecords`                                                              | Fact   | Offset take last sums only the most recent records.                                                                        |
| `OffsetSkipFirstBeyondTheAvailableRecordsYieldsAnEmptyRangeRatherThanThrowing`                            | Fact   | Offset skip first beyond the available records yields an empty range rather than throwing.                                 |
| `OffsetTakeLastBeyondTheAvailableRecordsFallsBackToEverything`                                            | Fact   | Offset take last beyond the available records falls back to everything.                                                    |
| `AggregateCountReturnsTheNumberOfDocumentsAfterOffsetReduction`                                           | Fact   | Aggregate count returns the number of documents after offset reduction.                                                    |
| `AggregateCountReturnsZeroForAnEmptyDocumentList`                                                         | Fact   | Aggregate count returns zero for an empty document list.                                                                   |
| `AggregateDistinctCountCountsUniqueValuesForTheSearchFunctionKey`                                         | Fact   | Aggregate distinct count counts unique values for the search function key.                                                 |
| `AggregateDistinctCountMatchesTheSearchFunctionKeyCaseInsensitively`                                      | Fact   | Aggregate distinct count matches the search function key case insensitively.                                               |
| `AggregateDistinctCountTreatsValuesDifferingOnlyByCaseAsTheSameDistinctEntry`                             | Fact   | Aggregate distinct count treats values differing only by case as the same distinct entry.                                  |
| `AggregateDistinctCountSkipsDocumentsThatDoNotHaveTheSearchFunctionKeyAtAll`                              | Fact   | Aggregate distinct count skips documents that do not have the search function key at all.                                  |
| `AggregateSameCountCountsDocumentsMatchingTheCurrentPayloadValue`                                         | Fact   | Aggregate same count counts documents matching the current payload value.                                                  |
| `AggregateSameCountComparesCaseInsensitively`                                                             | Fact   | Aggregate same count compares case insensitively.                                                                          |
| `AggregateSameCountDoesNotCountDocumentsThatLackTheSearchFunctionKey`                                     | Fact   | Aggregate same count does not count documents that lack the search function key.                                           |
| `AggregateRawReturnsTheDoubleValueOfTheSearchFunctionKeyOnTheLastDocument`                                | Fact   | Aggregate raw returns the double value of the search function key on the last document.                                    |
| `AggregateRawReturnsZeroWhenTheLastDocumentDoesNotHaveTheSearchFunctionKey`                               | Fact   | Aggregate raw returns zero when the last document does not have the search function key.                                   |
| `AggregateRawReturnsZeroRatherThanThrowingWhenTheDocumentListIsEmpty`                                     | Fact   | Aggregate raw returns zero rather than throwing when the document list is empty.                                           |
| `AggregateRawReturnsZeroWhenTheSearchFunctionKeyWasStoredAsAStringRatherThanADouble`                      | Fact   | Aggregate raw returns zero when the search function key was stored as a string rather than a double.                       |
| `RawGracefullyReturnsZeroWhenAnOffsetLeavesAnEmptyWindow`                                                 | Fact   | Raw gracefully returns zero when an offset leaves an empty window.                                                         |
| `BuildValuesArrayReadsTheSearchFunctionKeyFromEachDocumentInOrder`                                        | Fact   | Build values array reads the search function key from each document in order.                                              |
| `BuildValuesArraySubstitutesZeroRatherThanSkippingWhenADocumentLacksTheSearchFunctionKey`                 | Fact   | Build values array substitutes zero rather than skipping when a document lacks the search function key.                    |
| `BuildValuesArrayTreatsAStringTypedFieldAsZero`                                                           | Fact   | Build values array treats a string typed field as zero.                                                                    |
| `BuildValuesArrayIsEmptyWhenTheDocumentListIsEmpty`                                                       | Fact   | Build values array is empty when the document list is empty.                                                               |
| `ComputeExtremeStatisticSumsTheValues`                                                                    | Fact   | Compute extreme statistic sums the values.                                                                                 |
| `ComputeExtremeStatisticAveragesTheValues`                                                                | Fact   | Compute extreme statistic averages the values.                                                                             |
| `ComputeExtremeStatisticFindsTheMedian`                                                                   | Fact   | Compute extreme statistic finds the median.                                                                                |
| `ComputeExtremeStatisticComputesKurtosis`                                                                 | Fact   | Compute extreme statistic computes kurtosis.                                                                               |
| `ComputeExtremeStatisticComputesSkewness`                                                                 | Fact   | Compute extreme statistic computes skewness.                                                                               |
| `ComputeExtremeStatisticComputesStandardDeviation`                                                        | Fact   | Compute extreme statistic computes standard deviation.                                                                     |
| `ComputeExtremeStatisticAddsStandardDeviationToTheMean`                                                   | Fact   | Compute extreme statistic adds standard deviation to the mean.                                                             |
| `ComputeExtremeStatisticDoublesStandardDeviationBeforeAddingTheMeanForFunctionType10`                     | Fact   | Compute extreme statistic doubles standard deviation before adding the mean for function type10.                           |
| `ComputeExtremeStatisticTreatsFunctionType12IdenticallyToFunctionType10WhenCalledDirectly`                | Fact   | Compute extreme statistic treats function type12 identically to function type10 when called directly.                      |
| `FunctionType12IsInterceptedAsSameCountBeforeReachingTheExtremeStatisticSwitch`                           | Fact   | Function type12 is intercepted as same count before reaching the extreme statistic switch.                                 |
| `ComputeExtremeStatisticFindsTheMode`                                                                     | Fact   | Compute extreme statistic finds the mode.                                                                                  |
| `ComputeExtremeStatisticFindsTheMaximum`                                                                  | Fact   | Compute extreme statistic finds the maximum.                                                                               |
| `ComputeExtremeStatisticFindsTheMinimum`                                                                  | Fact   | Compute extreme statistic finds the minimum.                                                                               |
| `ComputeExtremeStatisticReturnsZeroForAnUnrecognisedFunctionType`                                         | Theory | Compute extreme statistic returns zero for an unrecognised function type.                                                  |
| `AggregateReturnsZeroWhenTheMeanOfAnEmptyRangeIsNaN`                                                      | Fact   | Aggregate returns zero when the mean of an empty range is na n.                                                            |
| `AggregateReturnsZeroWhenStandardDeviationOfASingleValueIsNaN`                                            | Fact   | Aggregate returns zero when standard deviation of a single value is na n.                                                  |
| `AggregateReturnsZeroWhenModeThrowsForAnEmptyRangeRatherThanPropagating`                                  | Fact   | Aggregate returns zero when mode throws for an empty range rather than propagating.                                        |
| `AggregateReturnsZeroWhenMaxThrowsForAnEmptyRangeRatherThanPropagating`                                   | Fact   | Aggregate returns zero when max throws for an empty range rather than propagating.                                         |
| `SinceIsZeroWhenNoOffsetIsConfiguredBecauseTheCurrentAndTestDateComeFromTheSameLastRecord`                | Fact   | Since is zero when no offset is configured because the current and test date come from the same last record.               |
| `SinceConvertsTheGapBetweenTheCurrentAndTestRecordUsingTheConfiguredIntervalType`                         | Theory | Since converts the gap between the current and test record using the configured interval type.                             |
| `SinceParsesTheSearchFunctionKeyOnBothRecordsWhenItIsPresentAndParsable`                                  | Fact   | Since parses the search function key on both records when it is present and parsable.                                      |
| `SinceDefaultsToDateTimeMinValueWhenNeitherTheSearchFunctionKeyNorCreatedDateArePresent`                  | Fact   | Since defaults to date time min value when neither the search function key nor created date are present.                   |
| `SinceThrowsWhenTheFullMatchesListIsEmptyRatherThanDegradingGracefully`                                   | Fact   | Since throws when the full matches list is empty rather than degrading gracefully.                                         |
| `SinceHasNoLocalGuardSoAnEmptyOffsetWindowIsCaughtOnlyByTheOuterErrorHandler`                             | Fact   | Since has no local guard so an empty offset window is caught only by the outer error handler.                              |
| `SanitizeNumericResultZeroesOutNonFiniteValues`                                                           | Theory | Sanitize numeric result zeroes out non finite values.                                                                      |
| `SanitizeNumericResultLeavesFiniteValuesUnchanged`                                                        | Fact   | Sanitize numeric result leaves finite values unchanged.                                                                    |
| `AggregateSameCountTreatsAMissingPayloadFieldAsTheLiteralStringNoneWhichCanAccidentallyMatchHistoricData` | Fact   | Aggregate same count treats a missing payload field as the literal string none which can accidentally match historic data. |

## Entity analysis model invoke extraction

### ArchiveKeyExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.ArchiveKeyExtensionsTests` — 11 tests. Category: Unit.

| Test                                                                      | Kind   | Verifies                                                                             |
|---------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `WhenReportTableIsFalseNoArchiveKeyIsAdded`                               | Fact   | When report table is false no archive key is added.                                  |
| `EveryArchiveKeyStartsAtVersionOne`                                       | Fact   | Every archive key starts at version one.                                             |
| `ATypedPayloadValueIsFiledUnderTheColumnOfItsType`                        | Fact   | A typed payload value is filed under the column of its type.                         |
| `AStringValueProducesAnArchiveKeyWithTheKeyValueStringPopulated`          | Fact   | A string value produces an archive key with the key value string populated.          |
| `AnIntValueProducesAnArchiveKeyWithTheKeyValueIntegerPopulated`           | Fact   | An int value produces an archive key with the key value integer populated.           |
| `AFloatValueProducesAnArchiveKeyWithTheKeyValueFloatPopulated`            | Fact   | A float value produces an archive key with the key value float populated.            |
| `ADateValueProducesAnArchiveKeyWithTheKeyValueDatePopulated`              | Fact   | A date value produces an archive key with the key value date populated.              |
| `ABoolValueProducesAnArchiveKeyWithTheKeyValueBooleanPopulatedAsAByte`    | Theory | A bool value produces an archive key with the key value boolean populated as a byte. |
| `PrecedenceFavoursStringOverAnyOtherValueWhenMultipleAreSuppliedTogether` | Fact   | Precedence favours string over any other value when multiple are supplied together.  |
| `NoValueSuppliedProducesAnArchiveKeyWithNoValueFieldsPopulated`           | Fact   | No value supplied produces an archive key with no value fields populated.            |
| `MultipleCallsAppendIndependentArchiveKeysToTheSameList`                  | Fact   | Multiple calls append independent archive keys to the same list.                     |

### EntityAnalysisModelDictionaryNoBoxingExtractorTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.EntityAnalysisModelDictionaryNoBoxingExtractorTests` — 13 tests.
Category: Unit.

| Test                                                                                            | Kind   | Verifies                                                                                                       |
|-------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------|
| `TheEntryAndReferenceDateAreCopiedFromTheirConfiguredFieldNames`                                | Fact   | The entry and reference date are copied from their configured field names.                                     |
| `TheReprocessingRuleInstanceIdIsCarriedOntoThePayload`                                          | Fact   | The reprocessing rule instance id is carried onto the payload.                                                 |
| `TheOriginalEntryBecomesThePayloadInstanceDirectly`                                             | Fact   | The original entry becomes the payload instance directly.                                                      |
| `AFieldAlreadyPresentInTheArchivedEntryIsLeftUntouchedAndNotOverwrittenByAnyDefault`            | Fact   | A field already present in the archived entry is left untouched and not overwritten by any default.            |
| `AStringFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault`                   | Fact   | A string field missing from the archived entry is backfilled from the configured default.                      |
| `AnIntegerFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault`                 | Fact   | An integer field missing from the archived entry is backfilled from the configured default.                    |
| `AnIntegerFieldWithANonNumericDefaultIsNotAddedRatherThanCorruptingThePayloadWithAZero`         | Fact   | An integer field with a non numeric default is not added rather than corrupting the payload with a zero.       |
| `AFloatLatitudeOrLongitudeFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault` | Theory | A float latitude or longitude field missing from the archived entry is backfilled from the configured default. |
| `ABooleanFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault`                  | Theory | A boolean field missing from the archived entry is backfilled from the configured default.                     |
| `ADateFieldMissingFromTheArchivedEntryUsesTheConfiguredDefaultAsADaysOffsetFromNow`             | Fact   | A date field missing from the archived entry uses the configured default as a days offset from now.            |
| `AFieldMissingFromTheArchivedEntryWithNoConfiguredDefaultIsNotAddedToThePayload`                | Fact   | A field missing from the archived entry with no configured default is not added to the payload.                |
| `AnUnrecognisedDataTypeIdFallsBackToStringHandlingForTheDefault`                                | Fact   | An unrecognised data type id falls back to string handling for the default.                                    |
| `TheReturnedContextCarriesTheModelAndAvailableModels`                                           | Fact   | The returned context carries the model and available models.                                                   |

### EntityAnalysisModelInstanceEntryPayloadHelpersTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.EntityAnalysisModelInstanceEntryPayloadHelpersTests` — 6 tests.
Category: Unit.

| Test                                                        | Kind | Verifies                                                            |
|-------------------------------------------------------------|------|---------------------------------------------------------------------|
| `IdentifyingModelMetadataIsCopiedOntoThePayload`            | Fact | Identifying model metadata is copied onto the payload.              |
| `WithNoGuidSuppliedANewGuidIsGenerated`                     | Fact | With no guid supplied a new guid is generated.                      |
| `ASuppliedGuidIsUsedInsteadOfGeneratingANewOne`             | Fact | A supplied guid is used instead of generating a new one.            |
| `EveryPooledCollectionIsInitialisedAndEmpty`                | Fact | Every pooled collection is initialised and empty.                   |
| `InvokeTaskPerformanceAndCreatedDateAreAlwaysPopulated`     | Fact | Invoke task performance and created date are always populated.      |
| `TwoCallsForTheSameModelProduceIndependentPayloadInstances` | Fact | Two calls for the same model produce independent payload instances. |

### EntityAnalysisModelJsonExtractorTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.EntityAnalysisModelJsonExtractorTests` — 22 tests. Category:
Unit.

| Test                                                                                          | Kind   | Verifies                                                                                                        |
|-----------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------------|
| `TheEntryIsExtractedViaEntryXPathAndAddedToThePayloadUnderEntryName`                          | Fact   | The entry is extracted via entry x path and added to the payload under entry name.                              |
| `TheReferenceDateIsExtractedViaReferenceDateXPathAndAddedToThePayload`                        | Fact   | The reference date is extracted via reference date x path and added to the payload.                             |
| `WhenReferenceDateLocationTypeIsNowTheCurrentUtcTimeIsUsedInstead`                            | Fact   | When reference date location type is now the current utc time is used instead.                                  |
| `AFieldAlreadyPresentInThePayloadFromEntryOrReferenceDateExtractionIsNotReprocessedAsAnXPath` | Fact   | A field already present in the payload from entry or reference date extraction is not reprocessed as an x path. |
| `AStringFieldIsExtractedAndAddedToThePayload`                                                 | Fact   | A string field is extracted and added to the payload.                                                           |
| `AFloatFieldIsExtractedAsADoubleAndReportedToTheArchiveKeys`                                  | Fact   | A float field is extracted as a double and reported to the archive keys.                                        |
| `LatitudeAndLongitudeDataTypesAreExtractedAsDoublesLikeAFloat`                                | Theory | Latitude and longitude data types are extracted as doubles like a float.                                        |
| `AnIntegerFieldIsExtractedAndReported`                                                        | Fact   | An integer field is extracted and reported.                                                                     |
| `AnIntegerFieldThatFailsToParseIsSkippedWithoutThrowing`                                      | Fact   | An integer field that fails to parse is skipped without throwing.                                               |
| `ADoubleFieldThatFailsToParseIsSkippedWithoutThrowing`                                        | Fact   | A double field that fails to parse is skipped without throwing.                                                 |
| `ABooleanFieldIsParsedCaseInsensitively`                                                      | Theory | A boolean field is parsed case insensitively.                                                                   |
| `ADateFieldIsParsedAndReported`                                                               | Fact   | A date field is parsed and reported.                                                                            |
| `AnUnrecognisedDataTypeIdFallsBackToStringHandling`                                           | Fact   | An unrecognised data type id falls back to string handling.                                                     |
| `AMissingFieldWithADefaultValueFallsBackToTheDefault`                                         | Fact   | A missing field with a default value falls back to the default.                                                 |
| `AMissingFieldWithNoDefaultValueIsNotAddedToThePayload`                                       | Fact   | A missing field with no default value is not added to the payload.                                              |
| `AMissingDateFieldWithADefaultUsesTheDefaultAsADaysOffsetFromNow`                             | Fact   | A missing date field with a default uses the default as a days offset from now.                                 |
| `DeterministicEncryptionProducesTheSameCipherTextForTheSamePlainText`                         | Fact   | Deterministic encryption produces the same cipher text for the same plain text.                                 |
| `RandomEncryptionProducesDifferentCipherTextOnEachExtraction`                                 | Fact   | Random encryption produces different cipher text on each extraction.                                            |
| `AStringFieldResolvesAConfiguredKvpDictionaryValueIntoTheDictionaryCache`                     | Fact   | A string field resolves a configured kvp dictionary value into the dictionary cache.                            |
| `AnEmptyInputStreamProducesANullJObjectAndDoesNotThrow`                                       | Fact   | An empty input stream produces a null j object and does not throw.                                              |
| `MalformedJsonPropagatesAJsonReaderException`                                                 | Fact   | Malformed json propagates a json reader exception.                                                              |
| `TheReturnedContextCarriesTheModelAvailableModelsAndAsyncFlag`                                | Fact   | The returned context carries the model available models and async flag.                                         |

## Entity analysis model invoke models payload entity analysis model instance entry payload logs

### FastPathRequestReaderTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.FastPathRequestReaderTests` — 25 tests. Category:
Unit.

| Test                                                                                               | Kind   | Verifies                                                                                                           |
|----------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------|
| `EveryCuratedPathAgreesWithNewtonsoftUnderEveryCulture`                                            | Theory | Every curated path agrees with newtonsoft under every culture.                                                     |
| `TheReaderAnswersEveryLeafShapeIncludingBigNumbersAndNegativeZeroBecauseItUsesNewtonsoftsOwnLexer` | Fact   | The reader answers every leaf shape including big numbers and negative zero because it uses newtonsofts own lexer. |
| `ContainerValuesAreDeferredToTheDomBecauseTheirTextIsFormattedJson`                                | Fact   | Container values are deferred to the dom because their text is formatted json.                                     |
| `PathsOutsideThePlanAreDeferredToTheDom`                                                           | Fact   | Paths outside the plan are deferred to the dom.                                                                    |
| `APlanIsOnlyBuiltFromSimpleDottedAndIndexedPaths`                                                  | Fact   | A plan is only built from simple dotted and indexed paths.                                                         |
| `APlanIsRefusedWhenAnObjectWouldNeedMoreThan63TrackedChildren`                                     | Fact   | A plan is refused when an object would need more than 63 tracked children.                                         |
| `AModelWithOnlySimplePathsGetsAPlan`                                                               | Fact   | A model with only simple paths gets a plan.                                                                        |
| `AModelWithAnyNonSimplePathGetsNoPlan`                                                             | Theory | A model with any non simple path gets no plan.                                                                     |
| `LeniencyIsIdenticalBecauseItIsTheSameParser`                                                      | Theory | Leniency is identical because it is the same parser.                                                               |
| `DocumentsThatAreNotAWellFormedObjectNeverProduceAReader`                                          | Theory | Documents that are not a well formed object never produce a reader.                                                |
| `ADuplicatedKeyOnATrackedPathNeverProducesAReaderBecauseTheDomIsLastWins`                          | Theory | A duplicated key on a tracked path never produces a reader because the dom is last wins.                           |
| `ADuplicatedKeyThatNoPathTracksIsIrrelevantAndStillAgrees`                                         | Fact   | A duplicated key that no path tracks is irrelevant and still agrees.                                               |
| `NestingDepthIsPolicedByTheSameReaderAndNeverOverrunsTheStack`                                     | Theory | Nesting depth is policed by the same reader and never overruns the stack.                                          |
| `SeededFuzzOfRandomDocumentsAndPathsNeverDisagreesWithNewtonsoft`                                  | Fact   | Seeded fuzz of random documents and paths never disagrees with newtonsoft.                                         |
| `SeededFuzzOfRawTextWithDuplicateKeysCommentsAndStrayWhitespaceNeverDisagreesWithNewtonsoft`       | Fact   | Seeded fuzz of raw text with duplicate keys comments and stray whitespace never disagrees with newtonsoft.         |
| `RandomDoubleBitPatternsRenderIdenticallyToNewtonsoft`                                             | Fact   | Random double bit patterns render identically to newtonsoft.                                                       |
| `RandomIntegersAcrossTheInt64BoundaryRenderIdenticallyToNewtonsoft`                                | Fact   | Random integers across the int 64 boundary render identically to newtonsoft.                                       |
| `TheAccessorSelectsExactlyAsTheOldPathDidIncludingDefaultFallbackAndUnplannedPaths`                | Fact   | The accessor selects exactly as the old path did including default fallback and unplanned paths.                   |
| `TheAccessorReferenceDateMatchesTheOldPathForEveryValueShape`                                      | Fact   | The accessor reference date matches the old path for every value shape.                                            |
| `TheAccessorEntryValueMatchesTheOldPath`                                                           | Fact   | The accessor entry value matches the old path.                                                                     |
| `TheLazyJObjectIsEquivalentToAnEagerParseAndIsOnlyBuiltOnDemand`                                   | Fact   | The lazy j object is equivalent to an eager parse and is only built on demand.                                     |
| `WithoutAPlanTheAccessorParsesEagerlyExactlyAsBefore`                                              | Fact   | Without a plan the accessor parses eagerly exactly as before.                                                      |
| `DocumentsWithADuplicatedTrackedKeyFallBackToTheDomAndStillAnswerLastWins`                         | Fact   | Documents with a duplicated tracked key fall back to the dom and still answer last wins.                           |
| `MalformedDocumentsStillThrowFromCreateAsBefore`                                                   | Fact   | Malformed documents still throw from create as before.                                                             |
| `AnEmptyStreamYieldsANullJObjectAndNullSelections`                                                 | Fact   | An empty stream yields a null j object and null selections.                                                        |

### EntityAnalysisModelJsonExtractorFastPathParityTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.EntityAnalysisModelJsonExtractorFastPathParityTests` — 12 tests.
Category:
Unit.

| Test                                                                                     | Kind   | Verifies                                                                                                 |
|------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------|
| `FastPathWithDirectCastsProducesExactlyTheSamePayloadAndArchiveKeysAsTheEagerPath`       | Theory | Fast path with direct casts produces exactly the same payload and archive keys as the eager path.        |
| `EveryLiteralInEveryFieldTypeMatchesTheEagerPathExhaustively`                            | Fact   | Every literal in every field type matches the eager path exhaustively.                                   |
| `TheSwitchOnMeansEagerParsingAndNoPlanWhateverThePaths`                                  | Fact   | The switch on means eager parsing and no plan whatever the paths.                                        |
| `TheSwitchOffWithOnlySimplePathsGivesAPlanCoveringEveryRequestEntryAndReferenceDatePath` | Fact   | The switch off with only simple paths gives a plan covering every request entry and reference date path. |
| `TheSwitchOffWithAComplexPathStillParsesEagerly`                                         | Fact   | The switch off with a complex path still parses eagerly.                                                 |
| `FlippingTheSwitchAtRuntimeIsHonouredWithoutANewSnapshot`                                | Fact   | Flipping the switch at runtime is honoured without a new snapshot.                                       |
| `TheDirectCastsAreActuallyTakenForTheCommonTypedTokens`                                  | Fact   | The direct casts are actually taken for the common typed tokens.                                         |
| `ShapesThatAreNotAnExactInt32FallBackToTheStringRoute`                                   | Theory | Shapes that are not an exact int 32 fall back to the string route.                                       |
| `ShapesThatAreNotAnExactFiniteDoubleFallBackToTheStringRoute`                            | Theory | Shapes that are not an exact finite double fall back to the string route.                                |
| `OnlyABooleanTokenIsCastDirectlyToABoolean`                                              | Theory | Only a boolean token is cast directly to a boolean.                                                      |
| `DoubleCastsEqualTheStringRouteBitForBitAcrossEveryInstalledCulture`                     | Fact   | Double casts equal the string route bit for bit across every installed culture.                          |
| `Int32CastsEqualTheStringRouteAcrossEveryInstalledCulture`                               | Fact   | Int 32 casts equal the string route across every installed culture.                                      |

### InvocationLogTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Logs.InvocationLogTests` — 4 tests. Category: Unit.

| Test                                                            | Kind   | Verifies                                                                   |
|-----------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `AddEntryAppendsToEntries`                                      | Fact   | Add entry appends to entries.                                              |
| `EveryAddedEntryIsPresentInTheSerialisedOutput`                 | Fact   | Every added entry is present in the serialised output.                     |
| `AnEmptyLogStillSerialisesTheAnchorDateWithAnEmptyEntriesArray` | Fact   | An empty log still serialises the anchor date with an empty entries array. |
| `NoPropertyOnTheInvocationLogModelIsNonPublic`                  | Theory | No property on the invocation log model is non public.                     |

## Entity analysis model invoke simulation

### AbstractionAggregationSimulatorTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.AbstractionAggregationSimulatorTests` — 6 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                      |
|---------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `CountsTheMatchingTransactionsInsideTheWindowIncludingTheCurrentOneAsync` | Fact | Counts the matching transactions inside the window including the current one. |
| `SumsTheFunctionKeyOverTheMatchesInTheWindowAsync`                        | Fact | Sums the function key over the matches in the window.                         |
| `ASearchKeyWithAShorterTtlShortensTheWindowAndSaysSoAsync`                | Fact | A search key with a shorter ttl shortens the window and says so.              |
| `ARuleThatThrowsIsReportedAsync`                                          | Fact | A rule that throws is reported.                                               |
| `EngineDefaultsApplyWhenTheRecordLeavesFieldsUnset`                       | Fact | Engine defaults apply when the record leaves fields unset.                    |
| `CachedPayloadsDecodeByIndexWithTheReferenceDateAtMinusOne`               | Fact | Cached payloads decode by index with the reference date at minus one.         |

### AbstractionCursorSimulationTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.AbstractionCursorSimulationTests` — 27 tests. Category: Unit.

| Test                                                                            | Kind | Verifies                                                                                 |
|---------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `TheThirdAndFifthTransactionAreSelectedByPositionAsync`                         | Fact | The third and fifth transaction are selected by position.                                |
| `DescendingOrderCountsPositionsFromTheMostRecentTransactionAsync`               | Fact | Descending order counts positions from the most recent transaction.                      |
| `TheThirdAndFifthMatchingTransactionAreSelectedWithAScanVariableAsync`          | Fact | The third and fifth matching transaction are selected with a scan variable.              |
| `AScanVariableCountsFromTheMostRecentTransactionUnderDescendingOrderAsync`      | Fact | A scan variable counts from the most recent transaction under descending order.          |
| `ARunningTotalInAScanVariableEndsTheScanAtItsLimitAsync`                        | Fact | A running total in a scan variable ends the scan at its limit.                           |
| `ATextScanVariableRemembersThePreviousTransactionAsync`                         | Fact | A text scan variable remembers the previous transaction.                                 |
| `AScanVariableStartsFromItsLiteralAsync`                                        | Fact | A scan variable starts from its literal.                                                 |
| `ARecordVariableStartsAfreshForEveryTransactionAsync`                           | Fact | A record variable starts afresh for every transaction.                                   |
| `AScanVariableFeedsTheCursorResultAsync`                                        | Fact | A scan variable feeds the cursor result.                                                 |
| `ExitForAfterSettingMatchedAcceptsTheRecordAndEndsTheScanAsync`                 | Fact | Exit for after setting matched accepts the record and ends the scan.                     |
| `ExitForWithoutMatchedEndsTheScanWithoutAcceptingTheRecordAsync`                | Fact | Exit for without matched ends the scan without accepting the record.                     |
| `ContinueForSkipsTheRecordAndCarriesOnAsync`                                    | Fact | Continue for skips the record and carries on.                                            |
| `ReturnStillAnswersForTheRecordWithoutEndingTheScanAsync`                       | Fact | Return still answers for the record without ending the scan.                             |
| `PositionAndSizeDescribeTheRulesWindowNotTheWholeFetchAsync`                    | Fact | Position and size describe the rules window not the whole fetch.                         |
| `FirstLastAndCurrentAreKnownToTheCursorAsync`                                   | Fact | First last and current are known to the cursor.                                          |
| `MatchesCountsTheRecordsAcceptedSoFarAsync`                                     | Fact | Matches counts the records accepted so far.                                              |
| `ARuleWithoutTheCursorKeepsItsMatchesAndOnlyChangesOrderWhenDescendingAsync`    | Fact | A rule without the cursor keeps its matches and only changes order when descending.      |
| `AnExceptionInOneRecordIsLoggedAndTheScanCarriesOnAsync`                        | Fact | An exception in one record is logged and the scan carries on.                            |
| `ACatalogueFunctionAggregatesTheMatchesWithItsDefaultParametersAsync`           | Fact | A catalogue function aggregates the matches with its default parameters.                 |
| `ACatalogueFunctionTakesItsStaticParametersFromTheRuleAsync`                    | Fact | A catalogue function takes its static parameters from the rule.                          |
| `AnOrderSensitiveFunctionFollowsTheIterationOrderAsync`                         | Fact | An order sensitive function follows the iteration order.                                 |
| `TheCursorAndACatalogueFunctionComposeAsync`                                    | Fact | The cursor and a catalogue function compose.                                             |
| `AFunctionThatCannotBeComputedAggregatesToZeroAsync`                            | Fact | A function that cannot be computed aggregates to zero.                                   |
| `ARuleReturnsItsOwnAggregationThroughTheCursorResultAsync`                      | Fact | A rule returns its own aggregation through the cursor result.                            |
| `TheCursorResultStopsGrowingWhenTheScanIsEndedAndFollowsTheIterationOrderAsync` | Fact | The cursor result stops growing when the scan is ended and follows the iteration order.  |
| `TheCursorResultIsZeroWhenTheRuleNeverSetsItAndIgnoresWhichRecordsMatchedAsync` | Fact | The cursor result is zero when the rule never sets it and ignores which records matched. |
| `TheParseResultSaysWhetherTheRuleUsesTheCursor`                                 | Fact | The parse result says whether the rule uses the cursor.                                  |

### InlineScriptRunnerTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.InlineScriptRunnerTests` — 4 tests. Category: Unit.

| Test                                                       | Kind | Verifies                                                      |
|------------------------------------------------------------|------|---------------------------------------------------------------|
| `TheScriptReadsThePayloadAndItsPropertiesAreReturnedAsync` | Fact | The script reads the payload and its properties are returned. |
| `AScriptThatReturnsFalseContributesNothingAsync`           | Fact | A script that returns false contributes nothing.              |
| `AScriptThatThrowsIsReportedAsync`                         | Fact | A script that throws is reported.                             |
| `AScriptThatDoesNotCompileReportsItsErrorsAsync`           | Fact | A script that does not compile reports its errors.            |

### InvocationContextBuilderTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.InvocationContextBuilderTests` — 9 tests. Category: Unit.

| Test                                                           | Kind   | Verifies                                                               |
|----------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `RequestJsonFieldsAreExtractedTypedWithTheEnginesDefaults`     | Fact   | Request json fields are extracted typed with the engines defaults.     |
| `OnlyExtractionIsComputedFromRequestJsonAndStorageIsExcluded`  | Fact   | Only extraction is computed from request json and storage is excluded. |
| `TheReferenceDateComesFromTheClockWhenTheModelSaysSo`          | Fact   | The reference date comes from the clock when the model says so.        |
| `ABlankContextCarriesOnlyTheDefaults`                          | Fact   | A blank context carries only the defaults.                             |
| `AnArchivedTransactionReadsBackThroughTheEnginesOwnSerializer` | Fact   | An archived transaction reads back through the engines own serializer. |
| `TheArchiveStoresDictionaryValuesUnderDictionaryNotKvp`        | Fact   | The archive stores dictionary values under dictionary not kvp.         |
| `AnOverlaySetsTypedValuesAndReportsUnknownNamesAndBadValues`   | Fact   | An overlay sets typed values and reports unknown names and bad values. |
| `TextConvertsByCompletionDataType`                             | Theory | Text converts by completion data type.                                 |
| `ValuesFormatInvariantly`                                      | Fact   | Values format invariantly.                                             |

### RuleBacktestTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.RuleBacktestTests` — 8 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                 |
|--------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EachOutcomeIsTheCellOfTheConfusionMatrix`                         | Theory | Each outcome is the cell of the confusion matrix.                        |
| `AnActivationRuleIsCountedAgainstATagClassFromArchivedValuesAsync` | Fact   | An activation rule is counted against a tag class from archived values.  |
| `TheClassCanCombineTagsAndValuesAsync`                             | Fact   | The class can combine tags and values.                                   |
| `TheFilterRunsAsAReprocessingRuleAndOnlyKeptRowsAreEvaluatedAsync` | Fact   | The filter runs as a reprocessing rule and only kept rows are evaluated. |
| `WithoutAClassOnlyFiringIsCountedAndPagesAccumulateAsync`          | Fact   | Without a class only firing is counted and pages accumulate.             |
| `SamplesAreCappedButCountsAreNotAsync`                             | Fact   | Samples are capped but counts are not.                                   |
| `ARuntimeErrorCountsAsNotFiredAsTheEngineTreatsItAsync`            | Fact   | A runtime error counts as not fired as the engine treats it.             |
| `AnExplanationGivesTheOutcomeAndTheValuesReadAsync`                | Fact   | An explanation gives the outcome and the values read.                    |

### RuleRunnerTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.RuleRunnerTests` — 7 tests. Category: Unit.

| Test                                                                 | Kind   | Verifies                                                               |
|----------------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `AGatewayRuleRunsAgainstTheContextAsync`                             | Theory | A gateway rule runs against the context.                               |
| `AReprocessingRuleUsesTheReprocessingDelegateAsync`                  | Fact   | A reprocessing rule uses the reprocessing delegate.                    |
| `AnAbstractionRuleCompilesAsTheEnginesAbstractionRuleClassAsync`     | Fact   | An abstraction rule compiles as the engines abstraction rule class.    |
| `AnActivationRuleSeesCountersAbstractionsAndEarlierActivationsAsync` | Fact   | An activation rule sees counters abstractions and earlier activations. |
| `AnAbstractionCalculationReturnsItsNumberAsync`                      | Fact   | An abstraction calculation returns its number.                         |
| `AnInlineFunctionReturnsItsValueAsync`                               | Fact   | An inline function returns its value.                                  |
| `ARuntimeErrorIsReportedRatherThanSwallowedAsync`                    | Fact   | A runtime error is reported rather than swallowed.                     |

### TimeWindowTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.TimeWindowTests` — 6 tests. Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                          |
|-----------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `ATtlCounterWindowIsTheEnginesOwnArithmetic`                          | Theory | A ttl counter window is the engines own arithmetic.                               |
| `AnAbstractionRuleWindowIsTheEnginesVisualBasicDateAdd`               | Theory | An abstraction rule window is the engines visual basic date add.                  |
| `TheAbstractionWindowIsShortenedBySearchKeyTtlExactlyAsTheEngineDoes` | Fact   | The abstraction window is shortened by search key ttl exactly as the engine does. |
| `ACalendarWindowAlsoOffersWeeksAndMeansYearsByY`                      | Fact   | A calendar window also offers weeks and means years by y.                         |
| `AnIntervalTheEntityDoesNotAllowIsRefused`                            | Theory | An interval the entity does not allow is refused.                                 |
| `EveryIntervalHasAName`                                               | Fact   | Every interval has a name.                                                        |

## Entity analysis model manager background tasks task starters

### CachePruneTaskStarterIntervalTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.CachePruneTaskStarterIntervalTests` — 6 tests.
Category: Unit.

| Test                                                                            | Kind   | Verifies                                                                                  |
|---------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `EachRecognisedIntervalSubtractsFromReferenceDate`                              | Theory | Each recognised interval subtracts from reference date.                                   |
| `SecondsIntervalSupportsVeryShortRetentionWindows`                              | Fact   | Seconds interval supports very short retention windows.                                   |
| `UnrecognisedIntervalCharFallsBackToDays`                                       | Fact   | Unrecognised interval char falls back to days.                                            |
| `ZeroIntervalValueReturnsReferenceDateUnchanged`                                | Fact   | Zero interval value returns reference date unchanged.                                     |
| `NegativeIntervalValuePushesTheThresholdIntoTheFutureRiskingDeletingEverything` | Fact   | Negative interval value pushes the threshold into the future risking deleting everything. |
| `NullReferenceDateReturnsNullThresholdSoTheCallerSkipsPruning`                  | Fact   | Null reference date returns null threshold so the caller skips pruning.                   |

### CachePruneTaskStarterPostgresBackedTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.CachePruneTaskStarterPostgresBackedTests` — 1 test. Category: Service. Collection: Database.

| Test                                                                                              | Kind | Verifies                                                                                                    |
|---------------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------|
| `PruneModelAsyncDeletesOnlyWhatsExpiredAcrossPayloadAndPayloadLatestAndAuditsBothToPostgresAsync` | Fact | Prune model async deletes only whats expired across payload and payload latest and audits both to postgres. |

### CachePruneTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.CachePruneTaskStarterTests` — 4 tests.
Category: Unit.

| Test                                                                                 | Kind | Verifies                                                                                            |
|--------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `PruneModelDoesNothingWhenNoReferenceDateIsStoredForTheModelAsync`                   | Fact | Prune model does nothing when no reference date is stored for the model.                            |
| `PruneModelLooksUpTheReferenceDateUsingTheModelsOwnTenantAndGuidAsync`               | Fact | Prune model looks up the reference date using the models own tenant and guid.                       |
| `GetDeletionLimitOrDefaultIfNullUsesTheShippedDefaultOfOneThousandWhenNotOverridden` | Fact | Get deletion limit or default if null uses the shipped default of one thousand when not overridden. |
| `GetDeletionLimitOrDefaultIfNullUsesTheConfiguredOverrideWhenSet`                    | Fact | Get deletion limit or default if null uses the configured override when set.                        |

### EntityAnalysisModelSyncWatermarkQueryPostgresBackedTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.EntityAnalysisModelSyncWatermarkQueryPostgresBackedTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                                                         | Kind | Verifies                                                                                                 |
|----------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `TheWatermarkQueryTranslatesAndRunsAgainstPostgresAsync`                                     | Fact | The watermark query translates and runs against postgres.                                                |
| `ATenantWithNoRowsAtAllProducesAZeroWatermarkRatherThanFailingAsync`                         | Fact | A tenant with no rows at all produces a zero watermark rather than failing.                              |
| `TheSameDatabaseStateProducesAnEqualWatermarkSoTheEngineDoesNotRebuildAsync`                 | Fact | The same database state produces an equal watermark so the engine does not rebuild.                      |
| `AnApiKeyIssuedToAnActiveUserWithATenantRoleMovesTheWatermarkSoTheEngineResynchronisesAsync` | Fact | An api key issued to an active user with a tenant role moves the watermark so the engine resynchronises. |

### LruJournalPruneTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.LruJournalPruneTaskStarterTests` — 7 tests.
Category: Unit.

| Test                                                     | Kind   | Verifies                                                           |
|----------------------------------------------------------|--------|--------------------------------------------------------------------|
| `MinutesIntervalReturnsATimeSpanInMinutes`               | Fact   | Minutes interval returns a time span in minutes.                   |
| `HoursIntervalReturnsATimeSpanInHours`                   | Fact   | Hours interval returns a time span in hours.                       |
| `AnyOtherIntervalIncludingDIsTreatedAsDays`              | Theory | Any other interval including d is treated as days.                 |
| `AVerySmallValueSupportsAShortMaxAgeWindow`              | Fact   | A very small value supports a short max age window.                |
| `ZeroValueReturnsAZeroLengthTimeSpan`                    | Fact   | Zero value returns a zero length time span.                        |
| `ANonNumericOrMissingValueDefaultsToOne`                 | Theory | A non numeric or missing value defaults to one.                    |
| `ANegativeValueProducesANegativeTimeSpanWithNoGuardRail` | Fact   | A negative value produces a negative time span with no guard rail. |

### TtlCounterAdministrationCacheServiceTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministrationCacheServiceTests` — 6
tests. Category: Unit.

| Test                                                             | Kind   | Verifies                                                                 |
|------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EachRecognisedIntervalSubtractsFromReferenceDate`               | Theory | Each recognised interval subtracts from reference date.                  |
| `SecondsIntervalSupportsVeryShortLivedCounters`                  | Fact   | Seconds interval supports very short lived counters.                     |
| `UnrecognisedIntervalFallsBackToDays`                            | Fact   | Unrecognised interval falls back to days.                                |
| `ZeroIntervalValueReturnsReferenceDateUnchanged`                 | Fact   | Zero interval value returns reference date unchanged.                    |
| `NegativeIntervalValuePushesTheWindowIntoTheFutureInstead`       | Fact   | Negative interval value pushes the window into the future instead.       |
| `AnOverflowingIntervalIsCaughtAndReturnsReferenceDateUnadjusted` | Fact   | An overflowing interval is caught and returns reference date unadjusted. |

### TtlCounterAdministrationTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministrationTaskStarterTests` — 5
tests. Category: Unit.

| Test                                                                                    | Kind | Verifies                                                                                        |
|-----------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ProcessTtlCounterDeprecationDecrementsTheCounterByTheExpiredEntrysValueAsync`          | Fact | Process ttl counter deprecation decrements the counter by the expired entrys value.             |
| `ProcessTtlCounterDeprecationDeletesTheExpiredCounterEntryAsync`                        | Fact | Process ttl counter deprecation deletes the expired counter entry.                              |
| `ProcessTtlCounterDeprecationRecordsTheRemovalBatchEntryUsingDataValueNotDataNameAsync` | Fact | Process ttl counter deprecation records the removal batch entry using data value not data name. |
| `ADecrementFailureDoesNotPreventTheEntryFromStillBeingDeletedAsync`                     | Fact | A decrement failure does not prevent the entry from still being deleted.                        |
| `MultipleExpiredEntriesProcessedInSequenceDoNotCrossContaminateBatchEntriesAsync`       | Fact | Multiple expired entries processed in sequence do not cross contaminate batch entries.          |

## Entity analysis model manager backtest

### BacktestHeartbeatTests

`Jube.Test.Engine.EntityAnalysisModelManager.Backtest.BacktestHeartbeatTests` — 8 tests. Category: Service. Collection:
Database.

| Test                                                              | Kind   | Verifies                                                                   |
|-------------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `TheIntervalIsAQuarterOfTheStaleWindowBetweenOneAndTenSeconds`    | Theory | The interval is a quarter of the stale window between one and ten seconds. |
| `APageLongerThanTheStaleWindowNoLongerFailsARunningInstanceAsync` | Fact   | A page longer than the stale window no longer fails a running instance.    |
| `AnInstanceWhoseHeartbeatHasStoppedIsStillRecoveredAsync`         | Fact   | An instance whose heartbeat has stopped is still recovered.                |
| `ReportedProgressIsWrittenOnTheNextBeatAsync`                     | Fact   | Reported progress is written on the next beat.                             |
| `AStopOrARecoveryIsSeenWithinABeatEvenMidPageAsync`               | Theory | A stop or a recovery is seen within a beat even mid page.                  |
| `APageRecordsProgressAndCarriesOnUntilAStopIsSeenAsync`           | Fact   | A page records progress and carries on until a stop is seen.               |
| `ADisposedHeartbeatWritesNothingMoreAsync`                        | Fact   | A disposed heartbeat writes nothing more.                                  |
| `AFailedBeatIsRetriedRatherThanEndingTheHeartbeatAsync`           | Fact   | A failed beat is retried rather than ending the heartbeat.                 |

## Entity analysis model manager entity analysis model context extensions

### InlineScriptCompilationExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions.InlineScriptCompilationExtensionsTests` — 17 tests. Category: Unit.

| Test                                                                                             | Kind   | Verifies                                                                                                 |
|--------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------|
| `AValidCSharpScriptCompilesWithoutErrorsAndLocatesTheInterfaceImplementation`                    | Fact   | A valid c sharp script compiles without errors and locates the interface implementation.                 |
| `ASyntaxOrSemanticErrorProducesCompileErrorsAndNoInterfaceImplementation`                        | Fact   | A syntax or semantic error produces compile errors and no interface implementation.                      |
| `TheActivatorDelegateCreatesANewInstanceOfTheCompiledType`                                       | Fact   | The activator delegate creates a new instance of the compiled type.                                      |
| `TheExecuteAsyncDelegateInvokesTheCompiledMethodAndReturnsItsResultAsync`                        | Fact   | The execute async delegate invokes the compiled method and returns its result.                           |
| `ReportTableResponsePayloadAndCacheIndexAttributesAreReflectedOntoThePropertyAttribute`          | Fact   | Report table response payload and cache index attributes are reflected onto the property attribute.      |
| `SearchKeyAttributeSubFieldsAreCapturedOntoTheDistinctSearchKey`                                 | Fact   | Search key attribute sub fields are captured onto the distinct search key.                               |
| `SearchKeyDecoratedPropertyIsAlsoAddedAsAModelGroupingKey`                                       | Fact   | Search key decorated property is also added as a model grouping key.                                     |
| `LatitudeAndLongitudeAttributesAreReflectedIndependentlyOfEachOther`                             | Fact   | Latitude and longitude attributes are reflected independently of each other.                             |
| `APropertyWithNoAttributesHasEveryDecorationFlagFalseAndNoSearchKey`                             | Fact   | A property with no attributes has every decoration flag false and no search key.                         |
| `EventAttributesOnExecuteAsyncArePopulatedAndOrderedByPriority`                                  | Fact   | Event attributes on execute async are populated and ordered by priority.                                 |
| `RunningTheCompiledScriptThroughReflectInlineScriptHelperPopulatesThePayloadAndArchiveKeysAsync` | Fact   | Running the compiled script through reflect inline script helper populates the payload and archive keys. |
| `ABooleanPropertyIsAddedToThePayloadAsABooleanAndArchivedAsABooleanKeyAsync`                     | Fact   | A boolean property is added to the payload as a boolean and archived as a boolean key.                   |
| `DeclaredDependenciesAreSplitOnSemicolonOrCommaAndTrimmed`                                       | Theory | Declared dependencies are split on semicolon or comma and trimmed.                                       |
| `NoDeclaredDependenciesAddsNothingBeyondTheLoadedAssembliesAndCryptography`                      | Theory | No declared dependencies adds nothing beyond the loaded assemblies and cryptography.                     |
| `RunningTheCompiledScriptThroughExecuteInlineScriptsAsyncEndToEndPopulatesThePayloadAsync`       | Fact   | Running the compiled script through execute inline scripts async end to end populates the payload.       |
| `TheSystemWideDemonstrationVisualBasicScriptCompilesAndProducesASixDigitOtpAsync`                | Fact   | The system wide demonstration visual basic script compiles and produces a six digit otp.                 |
| `CompilingTheSameCodeTwiceProducesIndependentDelegatesThatStillBehaveIdentically`                | Fact   | Compiling the same code twice produces independent delegates that still behave identically.              |

### InlineScriptLoadTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions.InlineScriptLoadTests` — 21 tests. Category: Unit.

| Test                                                                               | Kind   | Verifies                                                                                     |
|------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `ANewRecordIsCompiledAddedOnceAndReportedAsCompiled`                               | Fact   | A new record is compiled, added once and reported as compiled.                               |
| `AnUnchangedRecordKeepsTheLoadedInstanceAndIsReportedAsUnchanged`                  | Fact   | An unchanged record keeps the loaded instance and is reported as unchanged.                  |
| `AnUnchangedRecordStillTakesTheLatestNameAndCreatedDate`                           | Fact   | An unchanged record still takes the latest name and created date.                            |
| `ChangedCodeReplacesTheLoadedScriptInPlaceWithTheNewAttributes`                    | Fact   | Changed code replaces the loaded script in place with the new attributes.                    |
| `ChangedCodeIsPickedUpWhenTheCreatedDateHasNotMoved`                               | Fact   | Changed code is picked up when the created date has not moved.                               |
| `ChangedCodeWithASearchKeyDoesNotAccumulateGroupingKeys`                           | Fact   | Changed code with a search key does not accumulate grouping keys.                            |
| `CodeRevertedToAnAlreadyCompiledVersionIsConfiguredInFullFromTheAssemblyCache`     | Fact   | Code reverted to an already compiled version is configured in full from the assembly cache.  |
| `TwoRecordsSharingTheSameCodeAreBothLoadedFromOneCompiledAssembly`                 | Fact   | Two records sharing the same code are both loaded from one compiled assembly.                |
| `ACompileErrorIsReportedAndLeavesThePreviouslyLoadedScriptInPlace`                 | Fact   | A compile error is reported and leaves the previously loaded script in place.                |
| `ACompileErrorOnANewRecordLoadsNothing`                                            | Fact   | A compile error on a new record loads nothing.                                               |
| `CodeWithNoClassImplementingTheInterfaceIsReportedAndLoadsNothing`                 | Fact   | Code with no class implementing the interface is reported and loads nothing.                 |
| `ANullLanguageIsLoadedAsVisualBasic`                                               | Fact   | A null language is loaded as visual basic.                                                   |
| `TheDeclaredDependencyIsCarriedOntoTheLoadedScript`                                | Fact   | The declared dependency is carried onto the loaded script.                                   |
| `HasChangedComparesCodeDependenciesAndLanguage`                                    | Theory | Has changed compares code, dependencies and language.                                        |
| `ConfiguringTheSameInstanceTwiceDoesNotDuplicatePropertiesOrGroupingKeys`          | Fact   | Configuring the same instance twice does not duplicate properties or grouping keys.          |
| `APropertyDecoratedWithResponsePayloadIsWrittenToTheResponsePayloadAsync`          | Fact   | A property decorated with response payload is written to the response payload.               |
| `APropertyWithoutResponsePayloadIsLeftOutOfTheResponsePayloadAsync`                | Fact   | A property without response payload is left out of the response payload.                     |
| `AddingResponsePayloadToALoadedScriptPutsThePropertyInTheResponseAfterReloadAsync` | Fact   | Adding response payload to a loaded script puts the property in the response after reload.   |
| `AddingResponsePayloadToTheVisualBasicDemonstrationScriptReturnsTheOtpAsync`       | Fact   | Adding response payload to the visual basic demonstration script returns the otp.            |
| `AScriptReturningFalseLeavesItsPropertiesOutOfTheResponsePayloadAsync`             | Fact   | A script returning false leaves its properties out of the response payload.                  |
| `EverySupportedPropertyTypeIsWrittenToTheResponsePayloadWithItsOwnJsonTypeAsync`   | Fact   | Every supported property type is written to the response payload with its own json type.     |

## Entity analysis model manager entity analysis model context models

### SnapshotsTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Models.SnapshotsTests` — 7 tests. Category:
Unit.

| Test                                                             | Kind | Verifies                                                                    |
|------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `OneBuilderIsHandedOutPerModelForTheWholeCycle`                  | Fact | One builder is handed out per model for the whole cycle.                    |
| `EachModelGetsItsOwnBuilder`                                     | Fact | Each model gets its own builder.                                            |
| `NothingReachesAModelUntilTheCyclePublishes`                     | Fact | Nothing reaches a model until the cycle publishes.                          |
| `EveryModelTouchedInTheCycleIsPublishedTogether`                 | Fact | Every model touched in the cycle is published together.                     |
| `AModelNotTouchedInTheCycleKeepsItsSnapshot`                     | Fact | A model not touched in the cycle keeps its snapshot.                        |
| `PublishingClearsTheCycleSoTheNextOneStartsFromWhatWasPublished` | Fact | Publishing clears the cycle so the next one starts from what was published. |
| `PublishingTwiceWithoutAnotherCycleDoesNotAdvanceTheGeneration`  | Fact | Publishing twice without another cycle does not advance the generation.     |

## Entity analysis model manager entity analysis model context utilities

### EntityAnalysisModelRecordMapperTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities.EntityAnalysisModelRecordMapperTests` — 32 tests. Category: Unit.

| Test                                                                                           | Kind   | Verifies                                                                                                    |
|------------------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------|
| `NameNullOnTheRecordDefaultsToAnEmptyString`                                                   | Fact   | Name null on the record defaults to an empty string.                                                        |
| `NameWithSpacesHasSpacesReplacedWithUnderscores`                                               | Fact   | Name with spaces has spaces replaced with underscores.                                                      |
| `EntryXPathNullOnTheRecordDefaultsToAnEmptyString`                                             | Fact   | Entry x path null on the record defaults to an empty string.                                                |
| `EntryXPathIsCopiedVerbatimWhenPresent`                                                        | Fact   | Entry x path is copied verbatim when present.                                                               |
| `ReferenceDateXPathNullOnTheRecordDefaultsToAnEmptyString`                                     | Fact   | Reference date x path null on the record defaults to an empty string.                                       |
| `ReferenceDateXPathIsCopiedVerbatimWhenPresent`                                                | Fact   | Reference date x path is copied verbatim when present.                                                      |
| `EntryNameNullOnTheRecordDefaultsToAnEmptyString`                                              | Fact   | Entry name null on the record defaults to an empty string.                                                  |
| `EntryNameIsCopiedVerbatimWhenPresent`                                                         | Fact   | Entry name is copied verbatim when present.                                                                 |
| `ReferenceDateNameNullOnTheRecordDefaultsToAnEmptyString`                                      | Fact   | Reference date name null on the record defaults to an empty string.                                         |
| `ReferenceDateNameIsCopiedVerbatimWhenPresent`                                                 | Fact   | Reference date name is copied verbatim when present.                                                        |
| `BooleanFlagIsTrueOnlyWhenTheRecordValueIsExactlyOne`                                          | Theory | Boolean flag is true only when the record value is exactly one.                                             |
| `CharFieldFallsBackToItsDocumentedDefaultWhenNull`                                             | Theory | Char field falls back to its documented default when null.                                                  |
| `IntFieldFallsBackToItsDocumentedDefaultWhenNull`                                              | Theory | Int field falls back to its documented default when null.                                                   |
| `DoubleFieldFallsBackToItsDocumentedDefaultWhenNull`                                           | Theory | Double field falls back to its documented default when null.                                                |
| `ImplicitAsyncTimeoutMillisecondsPassesThroughIncludingNull`                                   | Fact   | Implicit async timeout milliseconds passes through including null.                                          |
| `LogsWarnThresholdMillisecondsPassesThroughIncludingNull`                                      | Fact   | Logs warn threshold milliseconds passes through including null.                                             |
| `SamplePercentagePassesThroughIncludingNull`                                                   | Fact   | Sample percentage passes through including null.                                                            |
| `GuidIsAssignedWhenTheRecordGuidIsNotEmpty`                                                    | Fact   | Guid is assigned when the record guid is not empty.                                                         |
| `GuidIsLeftUnchangedWhenTheRecordGuidIsEmpty`                                                  | Fact   | Guid is left unchanged when the record guid is empty.                                                       |
| `TenantRegistryIdIsAssignedWhenPresent`                                                        | Fact   | Tenant registry id is assigned when present.                                                                |
| `TenantRegistryIdIsLeftUnchangedWhenTheRecordValueIsNull`                                      | Fact   | Tenant registry id is left unchanged when the record value is null.                                         |
| `TenantRegistryIdBeingNullIsLoggedAsAnErrorNotJustADebugMessage`                               | Fact   | Tenant registry id being null is logged as an error not just a debug message.                               |
| `MaxActivationWatcherValueResetsToZeroWhenTheRecordValueIsNull`                                | Fact   | Max activation watcher value resets to zero when the record value is null.                                  |
| `MaxActivationWatcherValueIsReplacedNotAccumulatedAcrossRepeatedSyncs`                         | Fact   | Max activation watcher value is replaced not accumulated across repeated syncs.                             |
| `MaxActivationWatcherValueTracksTheLatestDatabaseValueAcrossSyncsRatherThanTheFirst`           | Fact   | Max activation watcher value tracks the latest database value across syncs rather than the first.           |
| `FullyPopulatedRecordMapsEveryFieldOntoItsCorrespondingContextProperty`                        | Fact   | Fully populated record maps every field onto its corresponding context property.                            |
| `CreateEntityAnalysisModelWiresIdentityFromRecordAndContext`                                   | Fact   | Create entity analysis model wires identity from record and context.                                        |
| `CreateEntityAnalysisModelWiresSharedServicesByReference`                                      | Fact   | Create entity analysis model wires shared services by reference.                                            |
| `CreateEntityAnalysisModelWiresSharedContextCollectionsByReferenceSoLaterUpdatesArePropagated` | Fact   | Create entity analysis model wires shared context collections by reference so later updates are propagated. |
| `CreateEntityAnalysisModelStartsOnAnEmptySnapshotThatTheModelOwns`                             | Fact   | Create entity analysis model starts on an empty snapshot that the model owns.                               |
| `CreateEntityAnalysisModelPopulatesArchiverBuffersPerArchiverPersistThreadsSetting`            | Fact   | Create entity analysis model populates archiver buffers per archiver persist threads setting.               |
| `CreateEntityAnalysisModelReturnsAFreshInstanceForEachCall`                                    | Fact   | Create entity analysis model returns a fresh instance for each call.                                        |

### SyncWatermarkDecisionTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities.SyncWatermarkDecisionTests` — 12
tests. Category: Unit.

| Test                                                                  | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `TheFirstCycleForATenantAlwaysSynchronises`                           | Fact | The first cycle for a tenant always synchronises.                                  |
| `AnUnreadableWatermarkFallsBackToSynchronising`                       | Fact | An unreadable watermark falls back to synchronising.                               |
| `AnUnchangedWatermarkDoesNotSynchronise`                              | Fact | An unchanged watermark does not synchronise.                                       |
| `AnApprovalSynchronisesBecauseApprovalIsWhatMakesAChangeEffective`    | Fact | An approval synchronises because approval is what makes a change effective.        |
| `ANewOverrideSynchronises`                                            | Fact | A new override synchronises.                                                       |
| `AnOverrideDeletedWithoutChangingTheHighestIdStillSynchronises`       | Fact | An override deleted without changing the highest id still synchronises.            |
| `APromotedExhaustiveModelSynchronises`                                | Fact | A promoted exhaustive model synchronises.                                          |
| `AnExpiryStillInTheFutureDoesNotSynchronise`                          | Fact | An expiry still in the future does not synchronise.                                |
| `AnExpiryThatHasPassedSynchronisesEvenThoughNothingWasWritten`        | Fact | An expiry that has passed synchronises even though nothing was written.            |
| `AnExpiryFallingDueExactlyNowSynchronises`                            | Fact | An expiry falling due exactly now synchronises.                                    |
| `NoExpiryAtAllDoesNotSynchronise`                                     | Fact | No expiry at all does not synchronise.                                             |
| `TheExpiryThatMattersIsTheOneTheLastSynchronisationSawNotTheFreshOne` | Fact | The expiry that matters is the one the last synchronisation saw not the fresh one. |

## Entity analysis model manager entity analysis model models

### ModelSnapshotBuilderTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshotBuilderTests` — 11 tests. Category:
Unit.

| Test                                                                            | Kind | Verifies                                                                                     |
|---------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `ABuilderOverNoPreviousSnapshotProducesTheFirstGeneration`                      | Fact | A builder over no previous snapshot produces the first generation.                           |
| `EachBuildIncrementsTheGenerationFromThePreviousSnapshot`                       | Fact | Each build increments the generation from the previous snapshot.                             |
| `CollectionsThatTheCycleDoesNotReplaceAreCarriedForwardByReference`             | Fact | Collections that the cycle does not replace are carried forward by reference.                |
| `ACollectionReplacedInTheCycleDoesNotAffectThePreviousSnapshot`                 | Fact | A collection replaced in the cycle does not affect the previous snapshot.                    |
| `TheSearchKeysDictionaryIsCopiedSoMidCycleAdditionsAreNotVisibleUntilPublished` | Fact | The search keys dictionary is copied so mid cycle additions are not visible until published. |
| `ActivationRuleCountersAreSharedWithTheReplacementRuleSoLateIncrementsAreKept`  | Fact | Activation rule counters are shared with the replacement rule so late increments are kept.   |
| `GatewayRuleCountersAreSharedWithTheReplacementRuleSoLateIncrementsAreKept`     | Fact | Gateway rule counters are shared with the replacement rule so late increments are kept.      |
| `ARuleThatIsNewInThisGenerationKeepsItsOwnCounters`                             | Fact | A rule that is new in this generation keeps its own counters.                                |
| `CountersAreNotDisturbedWhenTheCycleDidNotReplaceTheRules`                      | Fact | Counters are not disturbed when the cycle did not replace the rules.                         |
| `ARuleThatAppearsTwiceInTheOutgoingGenerationDoesNotFailTheCarryForward`        | Fact | A rule that appears twice in the outgoing generation does not fail the carry forward.        |
| `BuildCarriesEveryCollectionOntoTheSnapshot`                                    | Fact | Build carries every collection onto the snapshot.                                            |

### ModelSnapshotCutoverTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshotCutoverTests` — 4 tests. Category:
Unit.

| Test                                                                | Kind | Verifies                                                                 |
|---------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AModelStartsOnAnEmptyFirstSnapshot`                                | Fact | A model starts on an empty first snapshot.                               |
| `PublishingReplacesTheWholeSnapshotInOneStep`                       | Fact | Publishing replaces the whole snapshot in one step.                      |
| `AReaderHoldingASnapshotNeverObservesAMixtureOfTwoGenerationsAsync` | Fact | A reader holding a snapshot never observes a mixture of two generations. |
| `EveryGenerationAReaderObservesIsOneThatWasActuallyPublishedAsync`  | Fact | Every generation a reader observes is one that was actually published.   |

## Reprocessing replay response elevation

### ResponseElevationRangeExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ResponseElevationRangeExtensionsTests` — 1 test. Category: Unit.

| Test                                                               | Kind | Verifies                                                                |
|--------------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `AReplayNamesTheElevationButStampsNoHoldAndDispatchesNothingAsync` | Fact | A replay names the elevation but stamps no hold and dispatches nothing. |

## Entity analysis model manager reprocessing

### ReprocessingDateRangeTests

`Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing.ReprocessingDateRangeTests` — 6 tests. Category: Unit.

| Test                                                             | Kind   | Verifies                                                                         |
|------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `TheStartIsTheLastReferenceDateLessTheInterval`                  | Theory | The start is the last reference date less the interval.                          |
| `AMonthBackFromTheEndOfAMonthLandsOnTheLastDayOfTheShorterMonth` | Fact   | A month back from the end of a month lands on the last day of the shorter month. |
| `AnUnknownIntervalOrANegativeValueHasNoStart`                    | Theory | An unknown interval or a negative value has no start.                            |
| `AnIntervalReachingBeforeTheCalendarStartsAtTheBeginning`        | Fact   | An interval reaching before the calendar starts at the beginning.                |
| `TheSampleIsAPercentage`                                         | Theory | The sample is a percentage.                                                      |
| `OverManyDrawsTheSampledShareMatchesThePercentage`               | Theory | Over many draws the sampled share matches the percentage.                        |

### ReprocessingPageLimitTests

`Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing.ReprocessingPageLimitTests` — 1 test (11 cases). Category:
Unit.

| Test                                    | Kind   | Verifies                                      |
|-----------------------------------------|--------|-----------------------------------------------|
| `ThePageSizeIsAlwaysPositiveAndBounded` | Theory | The page size is always positive and bounded. |

### ReprocessingTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing.ReprocessingTaskStarterTests` — 15 tests. Category: Service.
Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `WithNothingWaitingNothingIsClaimedAsync`                                         | Fact | With nothing waiting nothing is claimed.                                                  |
| `EveryDocumentInTheWindowIsProcessedExactlyOnceAcrossPagesAndTiesAsync`           | Fact | Every document in the window is processed exactly once across pages and ties.             |
| `TheReprocessedArchiveCarriesTheNewModelOutputAsync`                              | Fact | The reprocessed archive carries the new model output.                                     |
| `OnlyDocumentsWithinTheIntervalOfTheLatestAreProcessedAsync`                      | Fact | Only documents within the interval of the latest are processed.                           |
| `DocumentsArchivedAfterTheRunStartedAreNotProcessedAsync`                         | Fact | Documents archived after the run started are not processed.                               |
| `ASampleOfNothingProcessesButMatchesNothingAsync`                                 | Fact | A sample of nothing processes but matches nothing.                                        |
| `AHalfSampleIsAPercentageNotAFractionAsync`                                       | Fact | A half sample is a percentage not a fraction.                                             |
| `ARuleThatDoesNotParseFailsTheInstanceAndDoesNotBlockTheNextAsync`                | Fact | A rule that does not parse fails the instance and does not block the next.                |
| `AModelWithNoArchiveCompletesAtOnceAsync`                                         | Fact | A model with no archive completes at once.                                                |
| `DeletingTheInstanceDuringTheRunStopsItAndLeavesItDeletedAsync`                   | Fact | Deleting the instance during the run stops it and leaves it deleted.                      |
| `ReprocessingLeavesTheCacheExactlyAsLiveTrafficLeftItAsync`                       | Fact | Reprocessing leaves the cache exactly as live traffic left it.                            |
| `TwoStartersNeverClaimTheSameInstanceAsync`                                       | Fact | Two starters never claim the same instance.                                               |
| `ACancelledRunIsRequeuedAndTheRerunProcessesEveryDocumentExactlyOnceAsync`        | Fact | A cancelled run is requeued and the rerun processes every document exactly once.          |
| `AnInstanceAbandonedByACrashedProcessIsReclaimedOnlyOnceItsHeartbeatIsStaleAsync` | Fact | An instance abandoned by a crashed process is reclaimed only once its heartbeat is stale. |
| `AQueuedInstanceOfADeletedOrInactiveRuleIsNeverClaimedAsync`                      | Fact | A queued instance of a deleted or inactive rule is never claimed.                         |

## Integrity

### EngineInstanceLivenessTests

`Jube.Test.Engine.Integrity.EngineInstanceLivenessTests` — 3 tests. Category: Unit.

| Test                                             | Kind   | Verifies                                                 |
|--------------------------------------------------|--------|----------------------------------------------------------|
| `TheToleranceIsTwoMinutesAndTheRetentionOneHour` | Fact   | The tolerance is two minutes and the retention one hour. |
| `TheCutoffIsTheToleranceBeforeNow`               | Fact   | The cutoff is the tolerance before now.                  |
| `AHeartbeatIsLiveOnlyWithinTheTolerance`         | Theory | A heartbeat is live only within the tolerance.           |

### ModelIntegrityRulesTests

`Jube.Test.Engine.Integrity.ModelIntegrityRulesTests` — 12 tests. Category: Unit.

| Test                                                                  | Kind | Verifies                                                                        |
|-----------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `DependencyFindingsSeparateBrokenActiveEntitiesFromInactiveOnes`      | Fact | Dependency findings separate broken active entities from inactive ones.         |
| `OnlyActiveEntitiesTheEngineFailedToCompileAreErrors`                 | Fact | Only active entities the engine failed to compile are errors.                   |
| `ConfigurationFindingsCoverTheModelTtlCountersAndSearchWindows`       | Fact | Configuration findings cover the model ttl counters and search windows.         |
| `WithoutAnEngineOnlyTheNodesAreChecked`                               | Fact | Without an engine only the nodes are checked.                                   |
| `TheEnginesLoadedEntitiesAreComparedWithTheActiveOnes`                | Fact | The engines loaded entities are compared with the active ones.                  |
| `ALiveInstanceWithNoStateForTheModelHasNotLoadedIt`                   | Fact | A live instance with no state for the model has not loaded it.                  |
| `EveryFindingCarriesAMessageKeyWhoseResourceTakesExactlyItsArguments` | Fact | Every finding carries a message key whose resource takes exactly its arguments. |
| `AnInactiveModelIsNotExpectedToBeLoaded`                              | Fact | An inactive model is not expected to be loaded.                                 |
| `TheSnapshotRecordsWhatTheEngineModelHasLoadedAndWhetherItStarted`    | Fact | The snapshot records what the engine model has loaded and whether it started.   |
| `TheGraphDrawsUsesToTheUsedWithMissingNamesAsTheirOwnNodes`           | Fact | The graph draws uses to the used with missing names as their own nodes.         |
| `AFocusKeepsOnlyTheNeighbourhoodWithinTheRadius`                      | Fact | A focus keeps only the neighbourhood within the radius.                         |
| `ABigGraphKeepsTheMostConnectedNodesAndSaysSo`                        | Fact | A big graph keeps the most connected nodes and says so.                         |

## Metrics

### EtcdApiResponseTests

`Jube.Test.Engine.Metrics.EtcdApiResponseTests` — 7 tests. Category: Unit.

| Test                                                       | Kind   | Verifies                                                            |
|------------------------------------------------------------|--------|---------------------------------------------------------------------|
| `AMissingCredentialIsRejectedRatherThanReadAsAPayload`     | Fact   | A missing credential is rejected rather than read as a payload.     |
| `AnInvalidTokenIsRejectedRatherThanReadAsAPayload`         | Fact   | An invalid token is rejected rather than read as a payload.         |
| `AnAuthenticatedMaintenanceStatusIsRead`                   | Fact   | An authenticated maintenance status is read.                        |
| `AnErrorEnvelopeReturnedWithASuccessStatusIsStillRejected` | Fact   | An error envelope returned with a success status is still rejected. |
| `AnEmptyBodyIsRejected`                                    | Theory | An empty body is rejected.                                          |
| `ABodyThatIsNotAJsonObjectIsRejected`                      | Theory | A body that is not a json object is rejected.                       |
| `AServerErrorWithNoEtcdEnvelopeStillReportsItsStatus`      | Fact   | A server error with no etcd envelope still reports its status.      |

## Override

### OverrideModelSyncTests

`Jube.Test.Engine.Override.OverrideModelSyncTests` — 14 tests. Category: Service. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                                                    |
|----------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------|
| `SyncKeepsEverySuppressedValueHeldAgainstOneKeyAsync`                | Fact | Sync keeps every suppressed value held against one key.                                                     |
| `SyncKeepsSuppressedValuesAcrossSeveralKeysAsync`                    | Fact | Sync keeps suppressed values across several keys.                                                           |
| `SyncKeepsEveryActivationRuleHeldAgainstOneValueAsync`               | Fact | Sync keeps every activation rule held against one value.                                                    |
| `SyncKeepsEveryValueHeldAgainstOneActivationRuleKeyAsync`            | Fact | Sync keeps every value held against one activation rule key.                                                |
| `SyncMergesModelAndActivationRuleOverridesOntoOneValueAsync`         | Fact | Sync merges model and activation rule overrides onto one value.                                             |
| `SyncCarriesTheForceOverrideKindAsync`                               | Fact | Sync carries the force override kind.                                                                       |
| `SyncLetsForceWinWhereBothKindsAreHeldForOneActivationRuleAsync`     | Fact | Sync lets force win where both kinds are held for one activation rule.                                      |
| `SyncSkipsExpiredAndDeletedOverridesAsync`                           | Fact | Sync skips expired and deleted overrides.                                                                   |
| `SyncReplacesThePreviousOverridesRatherThanAccumulatingAsync`        | Fact | Sync replaces the previous overrides rather than accumulating.                                              |
| `SyncIgnoresAForceOverrideThatHasNeverBeenApprovedAsync`             | Fact | Sync ignores a Force override that has never been approved, and keeps a Suppress override.                  |
| `SyncIgnoresARejectedForceOverrideAsync`                             | Fact | Sync ignores a rejected Force override at both the model and the rule level.                                |
| `SyncAppliesAnApprovedForceOverrideAsync`                            | Fact | Sync applies an approved Force override.                                                                    |
| `SyncKeepsTheApprovedVersionLiveWhileAnEditOrARemovalIsPendingAsync` | Fact | Sync keeps the approved version live while a removal is pending, and drops it once the removal is approved. |
| `SyncStillSkipsAnApprovedForceOverrideThatHasExpiredAsync`           | Fact | Sync still skips an approved Force override that has expired.                                               |

## Sanctions

### LevenshteinDistanceTests

`Jube.Test.Engine.Sanctions.LevenshteinDistanceTests` — 39 tests. Category: Unit.

| Test                                                                                        | Kind   | Verifies                                                                                                   |
|---------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------|
| `CheckMultipartStringWithNullOrBlankInputReturnsNoMatches`                                  | Theory | Check multipart string with null or blank input returns no matches.                                        |
| `CheckMultipartStringReturnsMatchesInAscendingEntryIdOrderWhateverTheInsertionOrder`        | Fact   | Matches come back in ascending entry id order, so First and Last aggregation is deterministic.             |
| `CheckMultipartStringWithNoSanctionEntriesReturnsNoMatches`                                 | Fact   | Check multipart string with no sanction entries returns no matches.                                        |
| `CheckMultipartStringWhereEveryInputTokenIsAStopTokenReturnsNoMatches`                      | Fact   | Check multipart string where every input token is a stop token returns no matches.                         |
| `CheckMultipartStringWithEntryThatHasNoUsableTokensIsSkipped`                               | Fact   | Check multipart string with entry that has no usable tokens is skipped.                                    |
| `CheckMultipartStringWithExactMatchReturnsZeroDistance`                                     | Fact   | Check multipart string with exact match returns zero distance.                                             |
| `CheckMultipartStringWithExactMatchIsCaseInsensitive`                                       | Fact   | Check multipart string with exact match is case insensitive.                                               |
| `CheckMultipartStringNormalizesDiacriticsBeforeComparing`                                   | Fact   | Check multipart string normalizes diacritics before comparing.                                             |
| `CheckMultipartStringStripsPunctuationBeforeComparing`                                      | Fact   | Check multipart string strips punctuation before comparing.                                                |
| `CheckMultipartStringWithOneCharacterEditWithinThresholdMatches`                            | Fact   | Check multipart string with one character edit within threshold matches.                                   |
| `CheckMultipartStringWithEditDistanceBeyondMaxDistanceIsRejected`                           | Fact   | Check multipart string with edit distance beyond max distance is rejected.                                 |
| `CheckMultipartStringWithEditDistanceExactlyAtMaxDistanceMatches`                           | Fact   | Check multipart string with edit distance exactly at max distance matches.                                 |
| `CheckMultipartStringWithCompletelyUnrelatedNameIsRejected`                                 | Fact   | Check multipart string with completely unrelated name is rejected.                                         |
| `CheckMultipartStringIgnoresConfiguredHonorificsOnBothSides`                                | Fact   | Check multipart string ignores configured honorifics on both sides.                                        |
| `CheckMultipartStringSingleCharacterTokensAreAlwaysIgnoredRegardlessOfStopTokens`           | Fact   | Check multipart string single character tokens are always ignored regardless of stop tokens.               |
| `CheckMultipartStringWithCoverageRatioBelowHalfIsRejected`                                  | Fact   | Check multipart string with coverage ratio below half is rejected.                                         |
| `CheckMultipartStringWithCoverageRatioExactlyAtHalfIsAccepted`                              | Fact   | Check multipart string with coverage ratio exactly at half is accepted.                                    |
| `CheckMultipartStringWithNoMaxCoverageRatioConfiguredStillRejectsAMuchLongerInputByDefault` | Fact   | Check multipart string with no max coverage ratio configured still rejects a much longer input by default. |
| `CheckMultipartStringWithMaxCoverageRatioExplicitlyRaisedAllowsTheLongerInputThrough`       | Fact   | Check multipart string with max coverage ratio explicitly raised allows the longer input through.          |
| `CheckMultipartStringWithMaxCoverageRatioRejectsMuchLongerInputThanEntry`                   | Fact   | Check multipart string with max coverage ratio rejects much longer input than entry.                       |
| `CheckMultipartStringWithMaxCoverageRatioAllowsInputWithinTheConfiguredRatio`               | Fact   | Check multipart string with max coverage ratio allows input within the configured ratio.                   |
| `CheckMultipartStringWithMaxDistanceRatioRejectsProportionallyLargeEditOnShortToken`        | Fact   | Check multipart string with max distance ratio rejects proportionally large edit on short token.           |
| `CheckMultipartStringWithMaxDistanceRatioStillAllowsProportionallySmallEditOnLongToken`     | Fact   | Check multipart string with max distance ratio still allows proportionally small edit on long token.       |
| `CheckMultipartStringWithMaxDistanceRatioNeverAllowsBelowOneCharacterOfSlack`               | Fact   | Check multipart string with max distance ratio never allows below one character of slack.                  |
| `CheckMultipartStringDoesNotLetTwoInputTokensClaimTheSameEntryToken`                        | Fact   | Check multipart string does not let two input tokens claim the same entry token.                           |
| `CheckMultipartStringReturnsWorstNotBestPairDistanceAcrossMultipleTokens`                   | Fact   | Check multipart string returns worst not best pair distance across multiple tokens.                        |
| `CheckMultipartStringCombinesTokensFromMultipleElementValuesForOneEntry`                    | Fact   | Check multipart string combines tokens from multiple element values for one entry.                         |
| `CheckMultipartStringReturnsOnlyEntriesThatMatchFromALargerSet`                             | Fact   | Check multipart string returns only entries that match from a larger set.                                  |
| `CheckMultipartStringAcrossManyEntriesIsThreadSafeAndDeterministicInResultSet`              | Fact   | Check multipart string across many entries is thread safe and deterministic in result set.                 |
| `CheckMultipartStringEntryWithFewerTokensThanInputIgnoresInputsExtraDistinguishingToken`    | Fact   | Check multipart string entry with fewer tokens than input ignores inputs extra distinguishing token.       |
| `DefaultMaxDistanceRatioConstantMatchesTheDocumentedServerWideDefault`                      | Fact   | Default max distance ratio constant matches the documented server wide default.                            |
| `DefaultMaxCoverageRatioConstantMatchesTheDocumentedServerWideDefault`                      | Fact   | Default max coverage ratio constant matches the documented server wide default.                            |
| `DefaultConstructorAppliesTheDocumentedMaxDistanceRatioAutomatically`                       | Fact   | Default constructor applies the documented max distance ratio automatically.                               |
| `ParseNullableDistanceRatioWithUnparsableInputReturnsNull`                                  | Theory | Parse nullable distance ratio with unparsable input returns null.                                          |
| `ParseNullableDistanceRatioParsesAValidValue`                                               | Fact   | Parse nullable distance ratio parses a valid value.                                                        |
| `ParseNullableDistanceRatioClampsToUnitInterval`                                            | Theory | Parse nullable distance ratio clamps to unit interval.                                                     |
| `ParseNullableCoverageRatioWithUnparsableInputReturnsNull`                                  | Theory | Parse nullable coverage ratio with unparsable input returns null.                                          |
| `ParseNullableCoverageRatioParsesAValidValue`                                               | Fact   | Parse nullable coverage ratio parses a valid value.                                                        |
| `ParseNullableCoverageRatioClampsNegativeToZeroButHasNoUpperBound`                          | Fact   | Parse nullable coverage ratio clamps negative to zero but has no upper bound.                              |

### SanctionAggregationCalculatorTests

`Jube.Test.Engine.Sanctions.SanctionAggregationCalculatorTests` — 37 tests. Category: Unit.

| Test                                                                                     | Kind   | Verifies                                                                                            |
|------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------|
| `CalculateSumWithEmptyCollectionReturnsNull`                                             | Fact   | Calculate sum with empty collection returns null.                                                   |
| `CalculateSumAddsAllDistances`                                                           | Fact   | Calculate sum adds all distances.                                                                   |
| `CalculateSumWithSingleZeroDistanceReturnsZeroNotNull`                                   | Fact   | Calculate sum with single zero distance returns zero not null.                                      |
| `CalculateCountWithEmptyCollectionReturnsNull`                                           | Fact   | Calculate count with empty collection returns null.                                                 |
| `CalculateCountReturnsNumberOfMatches`                                                   | Fact   | Calculate count returns number of matches.                                                          |
| `CalculateMaxWithEmptyCollectionReturnsNull`                                             | Fact   | Calculate max with empty collection returns null.                                                   |
| `CalculateMaxReturnsWorstMatch`                                                          | Fact   | Calculate max returns worst match.                                                                  |
| `CalculateMinWithEmptyCollectionReturnsNull`                                             | Fact   | Calculate min with empty collection returns null.                                                   |
| `CalculateMinReturnsBestMatch`                                                           | Fact   | Calculate min returns best match.                                                                   |
| `CalculateFirstWithEmptyCollectionReturnsNull`                                           | Fact   | Calculate first with empty collection returns null.                                                 |
| `CalculateFirstReturnsElementAtIndexZeroRegardlessOfValue`                               | Fact   | Calculate first returns element at index zero regardless of value.                                  |
| `CalculateLastWithEmptyCollectionReturnsNull`                                            | Fact   | Calculate last with empty collection returns null.                                                  |
| `CalculateLastReturnsFinalElementRegardlessOfValue`                                      | Fact   | Calculate last returns final element regardless of value.                                           |
| `CalculateFirstAndLastAreSameElementForSingleMatch`                                      | Fact   | Calculate first and last are same element for single match.                                         |
| `CalculateAverageWithEmptyCollectionReturnsNull`                                         | Fact   | Calculate average with empty collection returns null.                                               |
| `CalculateAverageDividesSumByCount`                                                      | Fact   | Calculate average divides sum by count.                                                             |
| `CalculateAverageWithAllZeroDistancesReturnsZeroNotNull`                                 | Fact   | Calculate average with all zero distances returns zero not null.                                    |
| `CalculateAverageWithNonIntegerResultIsNotTruncated`                                     | Fact   | Calculate average with non integer result is not truncated.                                         |
| `CalculateDispatchesToTheCorrectAggregationForEachKnownTypeId`                           | Theory | Calculate dispatches to the correct aggregation for each known type id.                             |
| `CalculateWithUnknownAggregationTypeIdFallsBackToAverage`                                | Fact   | Calculate with unknown aggregation type id falls back to average.                                   |
| `CalculateWithZeroAggregationTypeIdFallsBackToAverage`                                   | Fact   | Calculate with zero aggregation type id falls back to average.                                      |
| `CalculateWithConfidenceTypeIdDispatchesToConfidence`                                    | Fact   | Calculate with confidence type id dispatches to confidence.                                         |
| `CalculateConfidenceWithEmptyCollectionReturnsNull`                                      | Fact   | Calculate confidence with empty collection returns null.                                            |
| `CalculateConfidenceWithSingleMatchIsPureClosenessOfThatDistance`                        | Fact   | Calculate confidence with single match is pure closeness of that distance.                          |
| `CalculateConfidenceWithSingleExactMatchIsOne`                                           | Fact   | Calculate confidence with single exact match is one.                                                |
| `CalculateConfidenceWithTwoIdenticalDistancesShortCircuitsOnZeroStdDevToCloseness`       | Fact   | Calculate confidence with two identical distances short circuits on zero std dev to closeness.      |
| `CalculateConfidenceWithExactlyTwoDistinctDistancesIsFiniteNotNaN`                       | Fact   | Calculate confidence with exactly two distinct distances is finite not na n.                        |
| `CalculateConfidenceWithTwoDistinctDistancesIsSymmetricUnderReordering`                  | Fact   | Calculate confidence with two distinct distances is symmetric under reordering.                     |
| `CalculateConfidenceWithTwoDistinctDistancesWidelySeparatedIsHigherThanCloselySeparated` | Fact   | Calculate confidence with two distinct distances widely separated is higher than closely separated. |
| `CalculateConfidenceWithThreeMatchesUsesSkewAdjustedReliability`                         | Fact   | Calculate confidence with three matches uses skew adjusted reliability.                             |
| `CalculateConfidenceWithFiveEvenlySpacedMatches`                                         | Fact   | Calculate confidence with five evenly spaced matches.                                               |
| `CalculateConfidenceWithTwoNearDuplicatesAndOneOutlierIsLowerThanIsolatedMatch`          | Fact   | Calculate confidence with two near duplicates and one outlier is lower than isolated match.         |
| `CalculateConfidenceWithManyFarMatchesAndOneExactMatchIsHigh`                            | Fact   | Calculate confidence with many far matches and one exact match is high.                             |
| `CalculateConfidenceIsRoundedToFourDecimalPlaces`                                        | Fact   | Calculate confidence is rounded to four decimal places.                                             |
| `CalculateConfidenceNeverExceedsOne`                                                     | Fact   | Calculate confidence never exceeds one.                                                             |
| `CalculateConfidenceIsNeverNegative`                                                     | Fact   | Calculate confidence is never negative.                                                             |
| `CalculateConfidenceIsNeverNaNAcrossASweepOfClusterSizes`                                | Fact   | Calculate confidence is never na n across a sweep of cluster sizes.                                 |

### SanctionEntryFileImporterReconciliationTests

`Jube.Test.Engine.Sanctions.SanctionEntryFileImporterReconciliationTests` — 9 tests. Category: Service. Collection:
Database.

| Test                                                                                 | Kind | Verifies                                                                                       |
|--------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `AReconcileThatWouldDelistMostOfALargeSourceIsRefusedAsync`                          | Fact | Reconciliation that would delist more than half of 100 or more entries is skipped.             |
| `AReconcileWithinTheSafetyLimitStillDelistsTheMissingRowsOfALargeSourceAsync`        | Fact | A smaller removal on a large source still applies.                                             |
| `ReloadingTheIdenticalFileASecondTimeLeavesEveryRowUnchangedAsync`                   | Fact | Reloading the identical file a second time leaves every row unchanged.                         |
| `AReloadWhoseFileNoLongerContainsARowSoftDeletesOnlyThatRowAsync`                    | Fact | A reload whose file no longer contains a row soft deletes only that row.                       |
| `ARowThatReappearsInALaterFileAfterBeingRemovedIsRevivedNotDuplicatedAsync`          | Fact | A row that reappears in a later file after being removed is revived not duplicated.            |
| `ReconcileRemovedAsyncWithEmptyHashesDoesNotWipeOutTheWholeSourceAsync`              | Fact | Reconcile removed async with empty hashes does not wipe out the whole source.                  |
| `ReconcileRemovedAsyncWithEmptyHashesLogsAtWarnNotInfoAsync`                         | Fact | Reconcile removed async with empty hashes logs at warn not info.                               |
| `ReconcileRemovedAsyncNeverTouchesRowsBelongingToAnotherSourceAsync`                 | Fact | Reconcile removed async never touches rows belonging to another source.                        |
| `WhenEveryRowInASourceIsGenuinelyGoneReconcileRemovedAsyncSoftDeletesAllOfThemAsync` | Fact | When every row in a source is genuinely gone reconcile removed async soft deletes all of them. |

### SanctionEntryFileImporterSdnIntegrationTests

`Jube.Test.Engine.Sanctions.SanctionEntryFileImporterSdnIntegrationTests` — 12 tests. Category: Service. Collection:
SanctionsSdnLoad.

| Test                                                                       | Kind   | Verifies                                                                              |
|----------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `TheLoadParsedEveryRowAndRejectedExactlyTheOneKnownMalformedTrailingLine`  | Fact   | The load parsed every row and rejected exactly the one known malformed trailing line. |
| `OnItsFirstEverLoadEveryPersistedRowWasAFreshInsertNotARevivalOrNoOp`      | Fact   | On its first ever load every persisted row was a fresh insert not a revival or no op. |
| `EveryHashParsedFromTheFileIsUnique`                                       | Fact   | Every hash parsed from the file is unique.                                            |
| `ThePersistedRowCountForTheSourceExactlyMatchesTheInsertedCountAsync`      | Fact   | The persisted row count for the source exactly matches the inserted count.            |
| `EveryPersistedRowHasANonEmptyHashElementValueAndReferenceAsync`           | Fact   | Every persisted row has a non empty hash element value and reference.                 |
| `NoTwoPersistedRowsShareTheSameHashAsync`                                  | Fact   | No two persisted rows share the same hash.                                            |
| `KnownRealSdnEntriesArePersistedWithTheirExactNameAsync`                   | Theory | Known real sdn entries are persisted with their exact name.                           |
| `AnEntryWithAnEmbeddedCommaInItsQuotedNameIsNotTruncatedAtTheCommaAsync`   | Fact   | An entry with an embedded comma in its quoted name is not truncated at the comma.     |
| `ThePayloadColumnRetainsTheFullOriginalRawRowAsync`                        | Fact   | The payload column retains the full original raw row.                                 |
| `TheImportAuditRowRecordsCountsThatAgreeWithWhatWasActuallyPersistedAsync` | Fact   | The import audit row records counts that agree with what was actually persisted.      |
| `TheRejectionAuditRowMatchesTheOneKnownMalformedTrailingLineAsync`         | Fact   | The rejection audit row matches the one known malformed trailing line.                |
| `GetAsyncAcrossAllSourcesStillIncludesOurIsolatedSourcesRowsAsync`         | Fact   | Get async across all sources still includes our isolated sources rows.                |

### SanctionStopTokenSynchroniserTests

`Jube.Test.Engine.Sanctions.SanctionStopTokenSynchroniserTests` — 5 tests. Category: Unit.

| Test                                             | Kind | Verifies                                                    |
|--------------------------------------------------|------|-------------------------------------------------------------|
| `SynchroniseAddsNewTokensWithTheirCategory`      | Fact | Synchronise adds new tokens with their category.            |
| `SynchroniseRemovesTokensThatAreNoLongerLive`    | Fact | Synchronise removes tokens that are no longer in the table. |
| `SynchroniseUpdatesTheCategoryOfAnExistingToken` | Fact | Synchronise updates the category of an existing token.      |
| `SynchroniseWithNoLiveTokensEmptiesTheSet`       | Fact | Synchronise with no live tokens empties the set.            |
| `SynchroniseIgnoresBlankTokens`                  | Fact | Synchronise ignores blank tokens.                           |

### SanctionTextFoldingTests

`Jube.Test.Engine.Sanctions.SanctionTextFoldingTests` — 13 tests. Category: Unit.

| Test                                                                                  | Kind   | Verifies                                                                                                            |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------------------|
| `FoldCollapsesCompatibilityFormsFormatCharactersAndWhitespace`                        | Theory | Full-width letters, soft hyphen, zero-width space, bidi controls, tabs and ligatures fold to plain lower-case text. |
| `FoldRemovesArabicShortVowelMarksFromBothSides`                                       | Fact   | Arabic with and without short vowels folds to the same text.                                                        |
| `FoldKeepsGeorgianAndCyrillicLettersIntactAndLowersCyrillic`                          | Fact   | Georgian is untouched and Cyrillic is lower-cased.                                                                  |
| `AFullWidthQueryMatchesAnAsciiEntryAtDistanceZero`                                    | Fact   | A full-width query matches an ASCII entry at distance 0.                                                            |
| `AQueryPaddedWithZeroWidthSpacesStillMatchesAtDistanceZero`                           | Fact   | Zero-width padding does not evade matching.                                                                         |
| `NormalizeElementValueAppliesTheSameFolding`                                          | Fact   | The loader-side normaliser uses the same folding.                                                                   |
| `CachedEntryTokensGiveTheSameMatchesAsFreshEntriesAndAreRefreshedWhenTheValueChanges` | Fact   | Cached entry tokens give the same matches as fresh entries and are refreshed when the value changes.                |
| `FoldingIsIdempotent`                                                                 | Fact   | Folding is idempotent.                                                                                              |
| `ACyrillicLookalikeSpellingInTheListIsFoundByTheLatinQueryBecauseTheLoaderFoldsIt`    | Fact   | A cyrillic lookalike spelling in the list is found by the latin query because the loader folds it.                  |
| `ACyrillicLookalikeSpellingOfAListedLatinNameIsFoundAtDistanceZero`                   | Fact   | A cyrillic lookalike spelling of a listed latin name is found at distance zero.                                     |
| `IsMixedScriptFlagsLatinWordsContainingCyrillicOrGreekLetters`                        | Theory | Is mixed script flags latin words containing cyrillic or greek letters.                                             |
| `FoldLeavesLegitimateNonLatinNamesUncorrupted`                                        | Theory | Fold leaves legitimate non latin names uncorrupted.                                                                 |
| `FoldMapsLookalikeCyrillicAndGreekLettersToLatinWhenTheWholeWordCanBeRead`            | Theory | Fold maps lookalike cyrillic and greek letters to latin when the whole word can be read.                            |

### SanctionEntryFileImporterIntegrityTests

`Jube.Test.Engine.Sanctions.SanctionEntryFileImporterIntegrityTests` — 2 tests. Category: Unit.

| Test                                                     | Kind | Verifies                                                                                   |
|----------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `AFileThatEndsInAReadFaultIsReportedAsNotCompletedAsync` | Fact | A read fault part way through is reported as not completed, so reconciliation can skip it. |
| `AFileReadToTheEndIsReportedAsCompletedAsync`            | Fact | A normal file is reported as completed.                                                    |

### SanctionEntryFileImporterTests

`Jube.Test.Engine.Sanctions.SanctionEntryFileImporterTests` — 32 tests. Category: Unit.

| Test                                                                                 | Kind   | Verifies                                                                                                                                              |
|--------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ImportAsyncParsesASimpleRowIntoARecordAsync`                                        | Fact   | Import async parses a simple row into a record.                                                                                                       |
| `ImportAsyncSkipsTheConfiguredNumberOfHeaderRowsAsync`                               | Fact   | Import async skips the configured number of header rows.                                                                                              |
| `ImportAsyncWithSkipGreaterThanRowCountProducesNoRecordsAndNoRejectionsAsync`        | Fact   | Import async with skip greater than row count produces no records and no rejections.                                                                  |
| `ImportAsyncHandlesFieldsWithEmbeddedCommasViaQuotingAsync`                          | Fact   | Import async handles fields with embedded commas via quoting.                                                                                         |
| `ImportAsyncCombinesMultipleColumnsWhenMultiPartStringIndexListsSeveralIndicesAsync` | Fact   | Import async combines multiple columns when multi part string index lists several indices.                                                            |
| `ImportAsyncFallsBackToColumnZeroWhenAMultiPartIndexTokenIsNotANumberAsync`          | Fact   | Import async falls back to column zero when a multi part index token is not a number.                                                                 |
| `ImportAsyncCapturesTheFullRawRowAsThePayloadAsync`                                  | Fact   | Import async captures the full raw row as the payload.                                                                                                |
| `ImportAsyncRejectsRowsWithOnlyOneFieldAsInsufficientAsync`                          | Fact   | Import async rejects rows with only one field as insufficient.                                                                                        |
| `ImportAsyncRejectsEveryDataRowWhenReferenceIndexIsNotConfiguredAsync`               | Fact   | Import async rejects every data row when reference index is not configured.                                                                           |
| `ImportAsyncRejectsARowThatThrowsDuringBuildAsParseErrorAndContinuesAsync`           | Fact   | Import async rejects a row that throws during build as parse error and continues.                                                                     |
| `ImportAsyncEmptyFileProducesNoRecordsNoRejectionsAndEmptyHashesAsync`               | Fact   | Import async empty file produces no records no rejections and empty hashes.                                                                           |
| `ImportAsyncWithNullOnRejectedCallbackSwallowsRejectionsWithoutThrowingAsync`        | Fact   | Import async with null on rejected callback swallows rejections without throwing.                                                                     |
| `ImportAsyncProducesTheSameHashForTheSameSourceElementAndReferenceAsync`             | Fact   | Import async produces the same hash for the same source element and reference.                                                                        |
| `ImportAsyncProducesADifferentHashForADifferentSourceAsync`                          | Fact   | Import async produces a different hash for a different source.                                                                                        |
| `ImportAsyncProducesADifferentHashForADifferentReferenceAsync`                       | Fact   | Import async produces a different hash for a different reference.                                                                                     |
| `ImportAsyncHashMatchesTheDocumentedMd5OfSourceIdPlusElementPlusReferenceAsync`      | Fact   | Import async hash matches the documented md5 of source id plus element plus reference.                                                                |
| `ImportAsyncResultHashesContainsOnlySuccessfullyParsedRowsAsync`                     | Fact   | Import async result hashes contains only successfully parsed rows.                                                                                    |
| `NormalizeElementValueLowercasesStripsDiacriticsAndSpacesPunctuation`                | Theory | Normalize element value lowercases, strips diacritics and spaces punctuation.                                                                         |
| `NormalizeElementValueSplitsHyphenExactlyAsMatchTimeTokenizationDoes`                | Fact   | Normalize element value splits a hyphen exactly as match-time tokenization does.                                                                      |
| `ListEntryAndScreenedNameMatchAtDistanceZeroWhateverPunctuationEachSideUses`         | Theory | A list entry and a screened name match at distance zero whatever punctuation each side uses (hyphens, dots, commas, apostrophes).                     |
| `ImportAsyncParsesTheUkSanctionsListLayoutUsingTheCorrectedSourceMappingAsync`       | Fact   | Import async parses the UK sanctions list layout (report date line, header, name columns 4-9, unique id reference) with the corrected source mapping. |
| `ImportAsyncParsesTheOfacAlternateNamesLayoutUsingTheCorrectedSourceMappingAsync`    | Fact   | Import async parses the OFAC alternate names layout (comma delimited, name column 3, entity number reference) with the corrected source mapping.      |
| `ImportAsyncRejectsTheOfacAlternateNamesLayoutUnderTheOriginalSemicolonMappingAsync` | Fact   | The original semicolon mapping rejects every alternate names row, which is why the source loaded nothing.                                             |
| `ImportAsyncParsesTheEntireRealSdnFileAndReportsConsistentCountsAsync`               | Fact   | Import async parses the entire real sdn file and reports consistent counts.                                                                           |
| `ImportAsyncParsesSpecificKnownSdnRowsCorrectlyAsync`                                | Theory | Import async parses specific known sdn rows correctly.                                                                                                |
| `ImportAsyncOnTheRealSdnFileProducesNormalizedTokensThatStillMatchAtQueryTimeAsync`  | Fact   | Import async on the real sdn file produces normalized tokens that still match at query time.                                                          |
| `ImportAsyncLoadsEachAliasAsItsOwnEntryWithTheSameReferenceAsync`                    | Fact   | Each alias is loaded as its own entry with the same reference as the entity.                                                                          |
| `ImportAsyncSkipsRowsWhoseFilterColumnIsNotAnAcceptedValueAsync`                     | Fact   | Rows whose filter column is not an accepted value are skipped and not counted as rejected.                                                            |
| `ImportAsyncComparesTheFilterIgnoringCaseAndSpacesAsync`                             | Fact   | The filter is compared ignoring case and spaces.                                                                                                      |
| `ImportAsyncAliasHashesAreStableAndDifferFromThePrimaryAsync`                        | Fact   | Alias hashes are stable between loads and differ from the primary name.                                                                               |
| `ImportAsyncWithAnAliasColumnButNoSeparatorSplitsOnSemicolonsAsync`                  | Fact   | An alias column with no separator splits on semicolons.                                                                                               |
| `ImportAsyncAliasedEntriesStillMatchAtQueryTimeAsync`                                | Fact   | An aliased entry matches an alias typed at query time.                                                                                                |

### SanctionsChangePollConfigurationTests

`Jube.Test.Engine.Sanctions.SanctionsChangePollConfigurationTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                         | Kind   | Verifies                                                                                |
|------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `TheIntervalFallsBackToTheDefault_WhenItIsMissingGarbageZeroOrNegative`      | Theory | The interval falls back to the default when it is missing garbage zero or negative.     |
| `WhenTheFlagIsFalse_NothingIsPolled_AndANewImportIsNotNoticedAsync`          | Fact   | When the flag is false nothing is polled and a new import is not noticed.               |
| `WhenTheFlagIsNotSet_TheDefaultPollsAndPicksUpANewImportAsync`               | Fact   | When the flag is not set the default polls and picks up a new import.                   |
| `AZeroNegativeOrGarbageInterval_FallsBackToTheDefault_AndDoesNotSpinAsync`   | Theory | A zero negative or garbage interval falls back to the default and does not spin.        |
| `AnIntervalAtOrAboveTheWait_IsOneSliceThatStillChecksAtTheEndOfTheWaitAsync` | Fact   | An interval at or above the wait is one slice that still checks at the end of the wait. |

### SanctionsChangePollTests

`Jube.Test.Engine.Sanctions.SanctionsChangePollTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                           |
|-------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ANewImport_BecomesVisibleInTheCache_WithoutWaitingForTheLoaderIntervalAsync` | Fact | A new import becomes visible in the cache without waiting for the loader interval. |
| `EntriesRemovedByALaterImport_LeaveTheCacheAsync`                             | Fact | Entries removed by a later import leave the cache.                                 |
| `WhenNothingHasBeenImported_TheCacheIsNotReloadedAsync`                       | Fact | When nothing has been imported the cache is not reloaded.                          |
| `AFailingFingerprintQuery_DoesNotEndTheLoop_AndTheNextPollRecoversAsync`      | Fact | A failing fingerprint query does not end the loop and the next poll recovers.      |
| `ACancellation_EndsTheLoopPromptlyAsync`                                      | Fact | A cancellation ends the loop promptly.                                             |

## Ttl counter

### TtlCounterDeprecationTimingTests

`Jube.Test.Engine.TtlCounter.TtlCounterDeprecationTimingTests` — 13 tests. Category: Service. Collection: Database.

| Test                                                                                        | Kind   | Verifies                                                                                             |
|---------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `OnlineAggregationCountExcludesAnEntryOnceItAgesPastTheIntervalAsync`                       | Fact   | Online aggregation count excludes an entry once it ages past the interval.                           |
| `OnlineAggregationSumExcludesEntriesOnceTheyAgePastTheIntervalAsync`                        | Fact   | Online aggregation sum excludes entries once they age past the interval.                             |
| `OnlineAggregationIncludesOnlyTheEntryStillWithinTheWindowWhenAnOlderOneHasAgedOutAsync`    | Fact   | Online aggregation includes only the entry still within the window when an older one has aged out.   |
| `OnlineAggregationIncludesAnEntryExactlyOnTheWindowsLowerBoundaryAsync`                     | Fact   | Online aggregation includes an entry exactly on the windows lower boundary.                          |
| `OnlineAggregationDoesNotCrossContaminateBetweenDifferentDataValuesAsync`                   | Fact   | Online aggregation does not cross contaminate between different data values.                         |
| `BatchAdministrationDecrementsAndRemovesAnExpiredCountCounterWithinAShortRealDelayAsync`    | Fact   | Batch administration decrements and removes an expired count counter within a short real delay.      |
| `BatchAdministrationDecrementsAndRemovesAnExpiredSumCounterWithinAShortRealDelayAsync`      | Fact   | Batch administration decrements and removes an expired sum counter within a short real delay.        |
| `BatchAdministrationDoesNotPruneACounterWellBeforeItsLongerIntervalHasElapsedAsync`         | Fact   | Batch administration does not prune a counter well before its longer interval has elapsed.           |
| `BatchAdministrationNeverPrunesALiveForeverCounterAsync`                                    | Fact   | Batch administration never prunes a live forever counter.                                            |
| `BatchAdministrationPrunesAZeroIntervalCounterOnAnEarlyCycleAsync`                          | Fact   | Batch administration prunes a zero interval counter on an early cycle.                               |
| `BatchAdministrationHandlesMultipleCountersOnTheSameModelIndependentlyAsync`                | Fact   | Batch administration handles multiple counters on the same model independently.                      |
| `BatchAdministrationNeverTouchesAModelThatIsNotStartedAsync`                                | Fact   | Batch administration never touches a model that is not started.                                      |
| `BatchAdministrationWindsBackEveryIntervalUnitAndValueOnceTheReferenceDateJumpsPastItAsync` | Theory | Batch administration winds back every interval unit and value once the reference date jumps past it. |

### TtlCounterModelSyncTests

`Jube.Test.Engine.TtlCounter.TtlCounterModelSyncTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `SyncPopulatesTheInMemoryCounterFromARealRowIncludingNullFieldDefaultsAsync`      | Fact | Sync populates the in memory counter from a real row including null field defaults.       |
| `SyncSkipsAnInactiveCounterDefinitionAsync`                                       | Fact | Sync skips an inactive counter definition.                                                |
| `ASyncedCounterIsPickedUpByTheRealAdministrationLoopAndDeprecatesOnScheduleAsync` | Fact | A synced counter is picked up by the real administration loop and deprecates on schedule. |

### ModelsInScopeExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelManager.SynchronisationScope.ModelsInScopeExtensionsTests` — 4 tests. Category:
Unit.

| Test                                                       | Kind | Verifies                                                               |
|------------------------------------------------------------|------|------------------------------------------------------------------------|
| `WithNoScopeEveryActiveModelIsInScope`                     | Fact | With no scope every active model is in scope.                          |
| `AScopeKeepsOnlyTheModelsItNames`                          | Fact | A scope keeps only the models it names.                                |
| `AScopeNamingAModelThatIsNotLoadedYetStillAdmitsItsRecord` | Fact | A scope naming a model that is not loaded yet still admits its record. |
| `AnEmptyScopeSelectsNothing`                               | Fact | An empty scope selects nothing.                                        |

## Graph recall

### GraphEntityTests

`Jube.Test.Engine.GraphRecall.GraphEntityTests` — 4 tests. Category: Unit.

| Test                                                                                   | Kind | Verifies                                                                                          |
|----------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `AFreshEntityCarriesTypedDefaultsWithDistancesBeyondTheCap`                            | Fact | A fresh entity carries typed defaults with every distance at the cap plus one and Known false.    |
| `TagsTrackTheNearestAndPatternsAreMembershipByName`                                    | Fact | Tags track the nearest tag and distance; patterns are membership by kind name.                    |
| `TheMemberCatalogueReadsWritesAndNamesEveryScalar`                                     | Fact | The member catalogue reads, writes, types and names every scalar member including the edge facts. |
| `TheArchiveConverterWritesCamelCaseMembersAndSkipsEntitiesNotFlaggedForTheReportTable` | Fact | The archive converter writes camelCase members and skips entities whose ReportTable is off.       |

### GraphRecallEvaluatorTests

`Jube.Test.Engine.GraphRecall.GraphRecallEvaluatorTests` — 8 tests. Category: Unit.

| Test                                                                 | Kind | Verifies                                                                                            |
|----------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `EveryConfiguredEntityIsWrittenOnceWithItsConfiguredFlags`           | Fact | Every configured entity is written once with its configured ResponsePayload and ReportTable.        |
| `KnownNodesSurfaceTheirFeaturesTagsPatternsAndEdgeFacts`             | Fact | Known nodes surface their features, tags, patterns and the edge facts to the counterpart.           |
| `TheRegistryGuidScopesEdgeFactsAndRelaxation`                        | Fact | The registry guid scopes which edge definitions count for edge facts and relaxation.                |
| `AnUnknownCounterpartInheritsTheKnownDistancePlusOneAndStaysUnknown` | Fact | An unknown counterpart inherits the known distance plus one in the relaxed value and stays unknown. |
| `RelaxationNeverPassesThroughAHubAndNeedsBothValuesPresent`          | Fact | Relaxation never passes through a hub and needs both payload values present.                        |
| `WithoutASnapshotEveryEntityCarriesItsTypedDefaults`                 | Fact | Without a snapshot every entity carries its typed defaults.                                         |
| `HashedKeysResolveTheSameNodesAsStringKeys`                          | Fact | Hashed keys resolve the same nodes as string keys.                                                  |
| `ThePlanKeepsActiveNamedRowsOnceAndResolvesCounterpartsByName`       | Fact | The plan keeps active named rows once and resolves counterparts by name, never to itself.           |

### GraphRecallEvaluatorEdgeCaseTests

`Jube.Test.Engine.GraphRecall.GraphRecallEvaluatorEdgeCaseTests` — 8 tests. Category: Unit.

| Test                                                                 | Kind | Verifies                                                                           |
|----------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ATaggedEntityDoesNotBridgeToItselfButBridgesToTheOtherTaggedEntity` | Fact | BridgesToTagged counts only the other entities of the transaction.                 |
| `ATagMoreThanTwoHopsAwayDoesNotCountAsTaggedNearby`                  | Fact | A tag three hops away does not count as nearby.                                    |
| `AnAbsentValueNeverBridgesEvenWhenTheOtherEntityIsTagged`            | Fact | An absent value never bridges and stays unknown.                                   |
| `EveryPatternKindBitMapsToItsNamedMembershipAndOnlyThat`             | Fact | Each of the ten pattern kind bits maps to its own named pattern.                   |
| `AnOutOfRangeStoredDistanceIsClampedToBeyond`                        | Fact | Stored distances above the cap or negative read as one beyond the cap.             |
| `ShortValueArraysLeaveTheMissingEntitiesAbsent`                      | Fact | A values array shorter than the plan leaves the rest absent with typed defaults.   |
| `TheCacheKeepsOneSnapshotPerTenantAndNeverFallsBackToAnother`        | Fact | The cache holds one snapshot per tenant and never answers with another tenant's.   |
| `ASnapshotNeverResolvesANodeAnotherTenantsBuilderHolds`              | Fact | A builder refuses edges to nodes it does not hold, so snapshots never mix tenants. |

### GraphRecallTaskStarterTests

`Jube.Test.Engine.GraphRecall.GraphRecallTaskStarterTests` — 14 test cases. Category: Unit.

| Test                                                                            | Kind   | Verifies                             |
|---------------------------------------------------------------------------------|--------|--------------------------------------|
| `TheStartupFlagIsSetOnlyWhenNothingFailedOrAtLeastOneTenantLoaded`              | Theory | The first-cycle flag rule.           |
| `AFailedCycleRetriesAfterAtMostThirtySecondsAndNeverLaterThanTheConfiguredWait` | Theory | The retry wait after a failed cycle. |
| `TheLoaderWaitFallsBackToFifteenMinutesForAnythingButAPositiveInteger`          | Theory | The loader wait setting parse.       |

### GraphRecallSnapshotTests

`Jube.Test.Engine.GraphRecall.GraphRecallSnapshotTests` — 7 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                                                          |
|--------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------------|
| `TheIndexFindsAddedValuesAndMissesEverythingElse`                  | Theory | The index finds added values and misses everything else, hashed or not.                                           |
| `TheHashIsDeterministicAndSeparatesNearbyValues`                   | Fact   | The hash is deterministic and separates nearby values.                                                            |
| `TheBuilderHonoursTheNodeCapAndOnlyCarriesEdgesBetweenLoadedNodes` | Fact   | The builder honours the node cap, carries edges between loaded nodes only and merges a pair's edges per registry. |
| `TheSnapshotKnowsWhichNodeKeysAnActiveDefinitionConnects`          | Fact   | The snapshot knows which node keys an active definition connects, per registry or any.                            |
| `TheBuilderCollectsTheDistinctTagsOfItsNodes`                      | Fact   | The builder collects the distinct tags of its nodes.                                                              |
| `ARowMapsToANodeWithUnknownDistancesAndParsedTags`                 | Fact   | A row maps to a node with unknown distances and parsed tags.                                                      |
| `TheEdgeCapIsIndependentOfTheNodeCapAndIsReportedSeparately`       | Fact   | The edge cap is independent of the node cap and is reported separately.                                           |

### GraphRecallRuleTests

`Jube.Test.Engine.GraphRecall.GraphRecallRuleTests` — 9 tests. Category: Unit.

| Test                                                                          | Kind   | Verifies                                                                                             |
|-------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `AnActivationRuleReadsAScalarMemberOfTheEntityAsync`                          | Theory | `Graph.AccountId.DistanceToConfirmed <= 2` rewrites to the typed object and evaluates.               |
| `AnActivationRuleReadsATagProximityThroughTheMethodMemberAsync`               | Theory | `Graph.AccountId.Tag("Mule").Distance <= 1` passes the method member through and evaluates.          |
| `PatternMembershipEdgeFactsAndAnUnknownTagAllEvaluateAsync`                   | Fact   | Pattern membership, nested edge facts and an unknown tag all evaluate.                               |
| `AnEntityTheModelHasButDidNotEvaluateReadsAsNothingAndTheRuleStaysFalseAsync` | Fact   | An entity the model has but did not evaluate reads as Nothing through `?.` and the rule stays false. |
| `TwoMembersOfTheSameEntityOnOneLineAreEachRewrittenWhole`                     | Fact   | Two members of the same entity on one line are each rewritten whole, longest name first.             |
| `AnEntityTheModelDoesNotHaveIsReportedAsAnErrorSpan`                          | Fact   | An entity the model does not have is reported as an error span.                                      |
| `AMemberTheEntityDoesNotHaveIsReportedAsAnErrorSpan`                          | Fact   | A member the entity does not have is reported as an error span.                                      |
| `AGraphReferenceIsNeverDanglingInTheDependencyGraph`                          | Fact   | A Graph reference is never dangling in the dependency graph.                                         |
| `TheVocabularyDocumentsTheGraphObjectAndItsMethods`                           | Fact   | The vocabulary documents the Graph object, its members, tag proximity, edge facts and methods.       |

### SvirfdRuleTests

`Jube.Test.Engine.Svirfd.SvirfdRuleTests` — 6 tests. Category: Unit.

| Test                                              | Kind   | Verifies                                           |
|---------------------------------------------------|--------|----------------------------------------------------|
| `ActivationRulesReadTheRecordAsync`               | Theory | Activation Rules Read The Record.                  |
| `AMissingRecordReadsAsNothingAndNeverThrowsAsync` | Fact   | AMissing Record Reads As Nothing And Never Throws. |
| `GatewayAndAbstractionRulesReadTheRecordAsync`    | Theory | Gateway And Abstraction Rules Read The Record.     |
| `InlineFunctionsReadTheRecordAsync`               | Fact   | Inline Functions Read The Record.                  |
| `UnknownEntitiesAndMembersAreErrorSpans`          | Fact   | Unknown Entities And Members Are Error Spans.      |
| `TheVocabularyAndTokensCarryTheCatalogue`         | Fact   | The Vocabulary And Tokens Carry The Catalogue.     |

### SvirfdArchiveSerialisationTests

`Jube.Test.Engine.Svirfd.SvirfdArchiveSerialisationTests` — 1 test. Category: Unit.

| Test                                                                | Kind | Verifies                                                                  |
|---------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `TheArchiveWritesOnlyTheReportTableRecordsAsCamelCaseWireJsonAsync` | Fact | The Archive Writes Only The Report Table Records As Camel Case Wire Json. |

### SvirfdPlanTests

`Jube.Test.Engine.Svirfd.SvirfdPlanTests` — 2 tests. Category: Unit.

| Test                                                      | Kind | Verifies                                                                   |
|-----------------------------------------------------------|------|----------------------------------------------------------------------------|
| `ThePlanAllowsRemoteLookupsByDefault`                     | Fact | A plan allows remote lookups unless told otherwise.                        |
| `ThePlanCarriesTheTenantsSwitchWithoutChangingTheEntries` | Fact | The tenant switch is carried by the plan and leaves the entries unchanged. |

### SvirfdInvokeTests

`Jube.Test.Engine.Svirfd.SvirfdInvokeTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                                 |
|---------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `TheEntryIsLookedUpOnEveryInvocationAndSurfacedInTheResponseAndTheArchiveAsync` | Fact | The Entry Is Looked Up On Every Invocation And Surfaced In The Response And The Archive. |
| `ATenantWithRemoteLookupsSwitchedOffStillGetsLocalLookupsAsync`                 | Fact | A tenant with the switch off still gets the local lookup during invocation after a sync. |

### GraphRecallLoaderTests

`Jube.Test.Engine.GraphRecall.GraphRecallLoaderTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                                            |
|-------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------|
| `TheLoaderBuildsTheSeededTenantSnapshotAndAConfiguredEntityRuleCompilesAndEvaluatesAsync` | Fact | The loader builds the seeded tenant snapshot and a rule over configured entities compiles and evaluates against it. |
| `TheLoaderReportsLoadTimeAndBytesPerNodeForEveryActiveTenantAsync`                        | Fact | The loader reports load time and bytes per node for every active tenant.                                            |
| `EdgesOfADeactivatedRegistryAreNotLoadedAsync`                                            | Fact | Edges of a deactivated registry are not loaded.                                                                     |
| `TheEdgeCapStopsEdgesWithoutTruncatingNodesAsync`                                         | Fact | The edge cap stops edges without truncating nodes.                                                                  |
| `ASnapshotHoldsOnlyItsOwnTenantsNodesAndATenantWithNothingLoadsEmptyAsync`                | Fact | A snapshot holds only its own tenants nodes and a tenant with nothing loads empty.                                  |
| `TheLoaderReportsTruncationWhenTheNodeCapIsReachedAsync`                                  | Fact | The loader reports truncation when the node cap is reached.                                                         |

### GraphRecallInvokeTests

`Jube.Test.Engine.GraphRecall.GraphRecallInvokeTests` — 1 test. Category: Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                                                                                                                                                                    |
|-------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `TheResponseCarriesTheResponsePayloadEntityAndTheArchiveCarriesTheReportTableEntityAsync` | Fact | A real invocation of a scaffolded model with two approved entities returns the Response Payload entity under `Graph` in the response JSON and writes the Report Table entity under `graph` in the archive JSON, each absent from the other. |

## Connection leak guards

Each test gives the code under test a connection string with its own unique Npgsql pool and a small maximum pool size,
runs the code, then rents every slot of that pool at once. A connection that was rented and never returned makes one of
those opens time out, which fails the test naming the scenario.

### EngineHostConnectionLeakTests

`Jube.Test.ConnectionLeak.EngineHostConnectionLeakTests` — 1 test. Category: Service. Collection: Database.

| Test                                                                                            | Kind | Verifies                                                                                                  |
|-------------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------|
| `AnEngineHostThatHasRunItsBackgroundTasksAndIsDisposedLeavesEveryPooledConnectionReturnedAsync` | Fact | A started, running and disposed engine host leaves every pooled connection returned to its isolated pool. |

### PostgresSamplerConnectionLeakTests

`Jube.Test.ConnectionLeak.PostgresSamplerConnectionLeakTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                                  | Kind | Verifies                                                                                      |
|-----------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `SamplingMetricsOnSeveralLiveSamplersNeverPinsAPooledConnectionAsync` | Fact | Several live metric samplers share one idle pooled connection and pin none.                   |
| `SamplingTheLogOnSeveralLiveSamplersNeverPinsAPooledConnectionAsync`  | Fact | Several live log samplers share one idle pooled connection and pin none.                      |
| `TheNumberOfServerConnectionsDoesNotGrowWithTheNumberOfTicksAsync`    | Fact | Server connections under the isolated application name stay flat however many ticks have run. |

### InfrastructureHealthMetricsStarterConnectionLeakTests

`Jube.Test.ConnectionLeak.InfrastructureHealthMetricsStarterConnectionLeakTests` — 1 test. Category: Service.
Collection: Database.

| Test                                                                                        | Kind | Verifies                                                                                                          |
|---------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------------|
| `AStartedAndCancelledStarterDisposesItsSamplersAndLeavesEveryPooledConnectionReturnedAsync` | Fact | A started and cancelled starter exits cleanly, disposes its samplers and leaves every pooled connection returned. |

### CachePayloadRepositoryConnectionLeakTests

`Jube.Test.ConnectionLeak.CachePayloadRepositoryConnectionLeakTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                                | Kind | Verifies                                                                                  |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `CreatingTheLocalCacheInstanceReturnsItsConnectionToThePoolAsync`   | Fact | Creating the local cache instance returns its connection to the pool.                     |
| `FillingPreferringTheReplicaReturnsItsConnectionToThePoolAsync`     | Fact | Filling from the replica returns its connection to the pool.                              |
| `OnceTheTokenIsCancelledTheRepositoryTouchesTheDatabaseNoMoreAsync` | Fact | After the token is cancelled no connection reappears under the isolated application name. |
| `DisposingTheRepositoryTwiceIsSafeAndStopsItsTimerAsync`            | Fact | Disposing the repository twice is safe and leaves every pooled connection returned.       |

### TaggingStarterConnectionLeakTests

`Jube.Test.ConnectionLeak.TaggingStarterConnectionLeakTests` — 1 test. Category: Service. Collection: Database.

| Test                                                                       | Kind | Verifies                                                                                |
|----------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `ProcessingManyTagsOpensAContextPerTagAndNeverHoldsAPooledConnectionAsync` | Fact | Processing many tags opens a context per tag and holds no pooled connection afterwards. |

### ShortLivedContextConnectionLeakTests

`Jube.Test.ConnectionLeak.ShortLivedContextConnectionLeakTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                             | Kind | Verifies                                                        |
|------------------------------------------------------------------|------|-----------------------------------------------------------------|
| `ReadingTheHttpHeadersManyTimesNeverHoldsAPooledConnectionAsync` | Fact | Reading the HTTP headers repeatedly holds no pooled connection. |
| `TheRenameLatestCountMigrationNeverHoldsAPooledConnectionAsync`  | Fact | The rename latest count migration holds no pooled connection.   |

### NoAsParametersConventionTests

`Jube.Test.Convention.NoAsParametersConventionTests` — 1 test. Category: Convention.

| Test                                             | Kind | Verifies                                                                                                                                    |
|--------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------------------------------|
| `NothingInTheAppBindsAnEndpointWithAsParameters` | Fact | No source file under Jube.App uses the AsParameters attribute: an endpoint takes a JSON or MessagePack body, or explicit scalar parameters. |

### NullTenantEscapeConventionTests

`Jube.Test.Convention.NullTenantEscapeConventionTests` — 3 tests. Category: Convention.

| Test                                                                | Kind | Verifies                                                                                                            |
|---------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------|
| `OnlyTheNamedSystemModeRepositoriesLetAMissingTenantSeeEveryTenant` | Fact | Only the repositories named on the system-mode allow list widen a query to every tenant when the tenant is missing. |
| `EveryNamedSystemModeRepositoryStillCarriesTheEscape`               | Fact | Every allow list entry still carries the escape, so the list stays exact.                                           |
| `NoRowLevelEscapeLetsARowWithNoTenantMatchEveryCaller`              | Fact | No query matches a row with no tenant by testing the row's tenant for a missing value.                              |

### SoftDeletedGuidTwinConventionTests

`Jube.Test.Convention.SoftDeletedGuidTwinConventionTests` — 2 tests. Category: Convention.

| Test                                                         | Kind | Verifies                                                                |
|--------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `EveryByDesignExemptionPointsAtARealFileAndStatesItsReason`  | Fact | Every by design exemption points at a real file and states its reason.  |
| `EveryGuidJoinOrLookupOnASoftDeletedTableAlsoFiltersDeleted` | Fact | Every guid join or lookup on a soft deleted table also filters deleted. |

### DataLayerSchemaConventionTests

`Jube.Test.Convention.DataLayerSchemaConventionTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                                                                    |
|-----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------------------------------|
| `EveryMappedPocoTableAndColumnExistsInTheMigratedSchemaAsync`                     | Fact | Every table and column mapped by a Jube.Data poco exists in the migrated schema.                                                            |
| `EveryQuotedIdentifierInAnSqlLiteralOfTheDataLayerExistsInTheMigratedSchemaAsync` | Fact | Every quoted identifier in a raw SQL literal of Jube.Data names a table or column of the migrated schema or an alias the same file defines. |

### NullTenantRepositoryTests

`Jube.Test.Service.Repository.NullTenant.NullTenantRepositoryTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                              | Kind | Verifies                                                                                                      |
|-------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------|
| `ARepositoryWithNoTenantListsNoSynchronisationSchedulesAsync`     | Fact | A synchronisation schedule repository built with no tenant lists no rows, including a row that has no tenant. |
| `AUserWithNoTenantMembershipListsNoSynchronisationSchedulesAsync` | Fact | A user who belongs to no tenant lists no synchronisation schedules, including a row that has no tenant.       |
| `ARepositoryWithNoTenantListsNoCaseWorkflowFormEntriesAsync`      | Fact | A case workflow form entry repository built with no tenant lists no entries.                                  |

### SqlSchemaSnapshotInvalidationTests

`Jube.Test.Reporting.Check.SqlSchemaSnapshotInvalidationTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                             | Kind | Verifies                                                                                                                |
|------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------------------|
| `ASchemaChangeEventDropsTheCachedSnapshotSoANewTableIsSeenAsync` | Fact | A schema change event drops the cached schema snapshot, so a table created after the snapshot is seen by the next read. |
| `AnUnrelatedChangeEventLeavesTheCachedSnapshotAloneAsync`        | Fact | A change event from another area leaves the cached schema snapshot in place.                                            |
| `AnnouncingASchemaChangeInvalidatesThisNodeAndTellsTheBusAsync`  | Fact | Announcing a schema change invalidates this node at once and publishes one schema change event naming the actor.        |

### DtoValidationSchemaConventionTests

`Jube.Test.Convention.DtoValidationSchemaConventionTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                                               | Kind | Verifies                                                                                                                                                                                                           |
|------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `EveryBoundedStringColumnHasAMatchingMaximumLengthRuleOnItsDtoValidatorAsync`      | Fact | Every string property of a validated DTO whose table column has a length limit carries a maximum length rule no looser than the column, so an overlong string is refused by our validator and not by the database. |
| `EveryUniqueNameIndexHasADuplicatePreCheckOnItsDtoValidatorAsync`                  | Fact | Every table with a unique name index has a duplicate pre-check on the validator of its DTO.                                                                                                                        |
| `EveryForeignKeyPropertyOfAValidatedDtoIsCheckedOrNamedAsGuardedByTheServiceAsync` | Fact | Every foreign key property of a validated DTO has an existence rule on the validator or is named on the list of keys the service guards.                                                                           |

### FormCarrierConventionTests

`Jube.Test.Convention.FormCarrierConventionTests` — 4 tests. Category: Convention.

| Test                                                              | Kind | Verifies                                                                                                                                                                  |
|-------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `NothingInTheAppBindsAnEndpointParameterFromAForm`                | Fact | No source file under Jube.App uses the FromForm attribute: data reaches an endpoint as a JSON or MessagePack body, a route segment or, hardly ever, a query string.       |
| `EveryUseOfAFormOrFileCarrierIsOnTheAllowListOfFileOnlyEndpoints` | Fact | Every use of IFormFile, IFormCollection, ReadFormAsync, HasFormContentType or MultipartReader under Jube.App is in a file named on the allow list of file-only endpoints. |
| `EveryAllowListEntryStillUsesAFormOrFileCarrierAndStatesWhy`      | Fact | Every allow list entry still uses a form or file carrier and carries a reason.                                                                                            |
| `NoAllowListedEndpointReadsAScalarFromTheForm`                    | Fact | No allow-listed endpoint reads a scalar field from the form.                                                                                                              |

### DbContextLeakShapeConventionTests

`Jube.Test.Convention.DbContextLeakShapeConventionTests` — 2 tests. Category: Unit.

| Test                                                           | Kind | Verifies                                                                                                    |
|----------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------|
| `NoTestReadsTheConnectionStringOffAContextThatIsNeverDisposed` | Fact | No test source reads the connection string off a context obtained inline and never disposed.                |
| `NoTestChainsAMemberOffAContextItNeverDisposes`                | Fact | No test source chains a member off a context obtained and used in one expression without a using statement. |

### AuthorSqlPreflightConventionTests

`Jube.Test.Convention.AuthorSqlPreflightConventionTests` — 1 test. Category: Convention.

| Test                                                                           | Kind | Verifies                                                                                    |
|--------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `EveryCodePathThatSendsAuthorSqlToTheDatabaseChecksItBeforeOpeningAConnection` | Fact | Every code path that sends author sql to the database checks it before opening aconnection. |

### CaseServiceLevelSweepTests

`Jube.Test.CaseServiceLevel.CaseServiceLevelSweepTests` — 7 tests. Category: Service.

| Test                                                                     | Kind | Verifies                                                                                                   |
|--------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------------|
| `AResolutionBreachIsRecordedOnceAndNotifiesTheHolderAtHighPriorityAsync` | Fact | A resolution breach is recorded once however many sweeps see it, and notifies the holder at high priority. |
| `ACaseInsideItsTargetIsNotSweptAsync`                                    | Fact | A case inside its target is not swept.                                                                     |
| `AClosedCaseIsNotSweptAsync`                                             | Fact | A closed case is not swept.                                                                                |
| `AnUnheldCaseNotifiesTheSupervisorsOfItsWorkflowAsync`                   | Fact | An unheld case notifies the supervisors of its workflow.                                                   |
| `AUserWithoutTheWorkflowRoleIsNotNotifiedAsync`                          | Fact | A user without the workflow role is not notified.                                                          |
| `AFirstViewBreachIsRaisedUntilTheCaseHasBeenLookedAtAsync`               | Fact | A first view breach is raised until the case has been looked at.                                           |
| `AStatusDwellBreachIsRaisedWhenTheCaseHasStayedInAStatusTooLongAsync`    | Fact | A status dwell breach is raised when the case has stayed in a status too long.                             |

### RetentionPurgeTests

`Jube.Test.Retention.RetentionPurgeTests` — 10 tests. Category: Service.

| Test                                                                             | Kind   | Verifies                                                                                                                                           |
|----------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| `OldArchivedTransactionsAreRemovedWithTheirKeysAndTagsAsync`                     | Fact   | Old archived transactions are removed with their keys and tags.                                                                                    |
| `InboundQueueRowsOfADeletedOrMissingModelAreRemovedAndLiveOnesKeptAsync`         | Fact   | The purge removes inbound queue rows whose model is deleted or missing, counts them in a dry run without removing, and keeps rows of a live model. |
| `RecentArchivedTransactionsAreKeptAsync`                                         | Fact   | Recent archived transactions are kept.                                                                                                             |
| `ATransactionAnOpenOrSuspendedCaseWasRaisedFromIsKeptWhateverItsAgeAsync`        | Theory | A transaction an open or suspended case was raised from is kept whatever its age.                                                                  |
| `ATransactionACaseUnderLegalHoldWasRaisedFromIsKeptEvenWhenTheCaseIsClosedAsync` | Fact   | A transaction a case under legal hold was raised from is kept even when the case is closed.                                                        |
| `ATransactionAClosedCaseWithNoHoldWasRaisedFromIsRemovedAndTheCaseSurvivesAsync` | Fact   | A transaction a closed case with no hold was raised from is removed and the case survives.                                                         |
| `ADryRunCountsWhatItWouldRemoveAndRemovesNothingAsync`                           | Fact   | A dry run counts what it would remove and removes nothing.                                                                                         |
| `ZeroDaysKeepsEverythingAsync`                                                   | Fact   | Zero days keeps everything.                                                                                                                        |
| `ABacklogLargerThanTheChunkIsRemovedInSeveralChunksAsync`                        | Fact   | A backlog larger than the chunk is removed in several chunks.                                                                                      |
| `ExpiredAskJoobyMemoryAndCacheAreRemovedAndLiveOnesKeptAsync`                    | Fact   | Expired AskJooby memory and cache are removed and live ones kept.                                                                                  |

### SanctionExemptionSetTests

`Jube.Test.Engine.Sanctions.SanctionExemptionSetTests` — 7 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                   |
|--------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `SpellingVariantsOfTheSameNameShareAKey`                                       | Theory | Spelling variants of the same name share a key.                                            |
| `DifferentNamesHaveDifferentKeys`                                              | Fact   | Different names have different keys.                                                       |
| `AnApprovedExemptionAppliesToThatNameEntryAndTenantOnly`                       | Fact   | An approved exemption applies to that name entry and tenant only.                          |
| `AnExemptionStopsApplyingOnceItHasExpired`                                     | Fact   | An exemption stops applying once it has expired.                                           |
| `ReplacingTheSetForgetsWhatIsNoLongerApproved`                                 | Fact   | Replacing the set forgets what is no longer approved.                                      |
| `TheFingerprintIsZeroWithNoExemptionsAndChangesWhenTheTenantsExemptionsChange` | Fact   | The fingerprint is zero with no exemptions and changes when the tenants exemptions change. |
| `TheFingerprintDoesNotDependOnTheOrderTheExemptionsArriveIn`                   | Fact   | The fingerprint does not depend on the order the exemptions arrive in.                     |

### SanctionExemptionEngineTests

`Jube.Test.Engine.Sanctions.SanctionExemptionEngineTests` — 4 tests. Category: Unit.

| Test                                                                 | Kind | Verifies                                                                  |
|----------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `AnExemptedEntryIsLeftOutOfTheMatchesAndTheAggregateAsync`           | Fact | An exempted entry is left out of the matches and the aggregate.           |
| `WhenEveryMatchIsExemptedThereIsNoSanctionResultAsync`               | Fact | When every match is exempted there is no sanction result.                 |
| `AnExemptionForAnotherTenantChangesNothingAsync`                     | Fact | An exemption for another tenant changes nothing.                          |
| `ApprovingAnExemptionStopsAResultCachedBeforeItFromBeingServedAsync` | Fact | Approving an exemption stops a result cached before it from being served. |

### SanctionRescreenTests

`Jube.Test.Engine.Sanctions.SanctionRescreenTests` — 9 tests. Category: Service.

| Test                                                                           | Kind | Verifies                                                                             |
|--------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `ARecentlyScreenedNameThatMatchesAChangedEntryIsRecordedAsAHitAsync`           | Fact | A recently screened name that matches a changed entry is recorded as a hit.          |
| `AnEntryThatDidNotChangeIsNotRescreenedAsync`                                  | Fact | An entry that did not change is not rescreened.                                      |
| `RunningAgainRecordsNoDuplicateHitAsync`                                       | Fact | Running again records no duplicate hit.                                              |
| `ANameOutsideTheWindowIsNotRescreenedAsync`                                    | Fact | A name outside the window is not rescreened.                                         |
| `AnApprovedExemptionKeepsTheNameFromBeingReportedAsync`                        | Fact | An approved exemption keeps the name from being reported.                            |
| `TheNumberOfNamesRescreenedForACheckIsCappedAsync`                             | Fact | The number of names rescreened for a check is capped.                                |
| `NothingIsScreenedWhenNoEntryChangedAsync`                                     | Fact | Nothing is screened when no entry changed.                                           |
| `AHitNotifiesTheHoldersOfTheSanctionsPermissionOncePerRunAsync`                | Fact | A hit notifies the holders of the sanctions permission once per run.                 |
| `UpsertingWithAnImportStampsInsertedAndRevivedEntriesButNotUnchangedOnesAsync` | Fact | Upserting with an import stamps inserted and revived entries but not unchanged ones. |

### CaseDigestTests

`Jube.Test.CaseDigest.CaseDigestTests` — 6 tests. Category: Service.

| Test                                                                | Kind   | Verifies                                                                 |
|---------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `ASupervisorGetsOneNotificationPerWorkflowWithTheCountsAsync`       | Fact   | A supervisor gets one notification per workflow with the counts.         |
| `RunningTheDigestAgainTheSameDaySendsNothingMoreAsync`              | Fact   | Running the digest again the same day sends nothing more.                |
| `EmailIsOnlySentWhenSwitchedOnAndCarriesCountsButNoCaseKeysAsync`   | Fact   | Email is only sent when switched on and carries counts but no case keys. |
| `AWorkflowWithNothingOpenAndNothingClosedRecentlyGetsNoDigestAsync` | Fact   | A workflow with nothing open and nothing closed recently gets no digest. |
| `TheScheduleIsDueOnlyInTheHourOnTheChosenDays`                      | Theory | The schedule is due only in the hour on the chosen days.                 |
| `TheScheduleDoesNotRunTwiceInOneDay`                                | Fact   | The schedule does not run twice in one day.                              |

### PopulationStabilityTests

`Jube.Test.ModelDrift.PopulationStabilityTests` — 7 tests. Category: Unit.

| Test                                            | Kind | Verifies                                                        |
|-------------------------------------------------|------|-----------------------------------------------------------------|
| `IdenticalDistributionsHaveNoDrift`             | Unit | Identical windows give a PSI of zero.                           |
| `ShiftedDistributionIsSignificant`              | Unit | A large shift crosses the significant threshold and status 3.   |
| `SlightlyShiftedDistributionIsModerateOrStable` | Unit | A tiny shift is not significant.                                |
| `PsiIsNeverNegativeAndIsFinite`                 | Unit | Unequal window sizes still give a finite non-negative PSI.      |
| `ConstantBaselineDoesNotThrow`                  | Unit | A constant baseline collapses to one cut point without failing. |
| `EmptyWindowIsRefused`                          | Unit | An empty window throws.                                         |
| `StatusFollowsThresholds`                       | Unit | Boundaries 0.1 and 0.25 map to moderate and significant.        |

### ModelDriftJobTests

`Jube.Test.ModelDrift.ModelDriftJobTests` — 3 tests. Category: Service.

| Test                                              | Kind    | Verifies                                                                                       |
|---------------------------------------------------|---------|------------------------------------------------------------------------------------------------|
| `StableScoreIsRecordedWithoutNotification`        | Service | A stable score is recorded with counts and no notification.                                    |
| `SignificantDriftIsRecordedAndNotifiedOncePerDay` | Service | A significant score is recorded each run but notified once per recipient per day (dedupe key). |
| `ThinWindowsAreSkipped`                           | Service | A window under the minimum sample is not checked.                                              |

### InboundQueueUnitTests

`Jube.Test.InboundQueue.InboundQueueUnitTests` — 6 tests. Category: Unit.

| Test                                                    | Kind | Verifies                                                                         |
|---------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `TheBackoffDoublesFromFiveSecondsAndStopsAtFiveMinutes` | Unit | The retry delay starts at 5 seconds, doubles per attempt and stops at 5 minutes. |
| `OnlyWellFormedJsonIsAccepted`                          | Unit | Only well-formed JSON bodies are accepted for the queue.                         |
| `DeeplyNestedJsonIsRefused`                             | Unit | JSON nested deeper than 64 levels is refused.                                    |
| `TheBackoffDoublesFromFiveSecondsAndStopsAtFiveMinutes` | Unit | The retry delay starts at 5 seconds, doubles per attempt and stops at 5 minutes. |
| `OnlyWellFormedJsonIsAccepted`                          | Unit | Only well-formed JSON bodies are accepted for the queue.                         |
| `DeeplyNestedJsonIsRefused`                             | Unit | JSON nested deeper than 64 levels is refused.                                    |

### InboundQueueConsumerTests

`Jube.Test.InboundQueue.InboundQueueConsumerTests` — 9 tests. Category: Service.

| Test                                                                          | Kind    | Verifies                                                                         |
|-------------------------------------------------------------------------------|---------|----------------------------------------------------------------------------------|
| `ProcessedRowsAreDeletedAndEachPayloadReachesTheModelAsync`                   | Service | Processed rows are deleted and every payload reaches the model.                  |
| `TwoNodesRunningTogetherProcessEveryRowExactlyOnceAsync`                      | Service | Two nodes claiming concurrently process every row exactly once.                  |
| `AFailureIsRetriedLaterWithAGrowingDelayThenKilledAtTheMaximumAsync`          | Service | A failure is retried later with a delay and marked dead at the maximum attempts. |
| `AnExceptionRecordsOnlyItsTypeNeverItsMessageAsync`                           | Service | A failure records only the exception type, never its message.                    |
| `AnUnknownOrUnstartedModelIsRetriedNotLostAsync`                              | Service | A model that is unknown or not started is retried, not lost.                     |
| `NothingIsClaimedUntilTheModelsHaveLoadedAsync`                               | Service | Nothing is claimed before the models have loaded.                                |
| `ARowClaimedByADeadNodeIsTakenAgainOnlyAfterItsClaimExpiresAsync`             | Service | The row of a crashed node is reclaimed only after its claim expires.             |
| `RowsAreClaimedOldestFirstAndInBatchesAsync`                                  | Service | Rows are claimed oldest first in batches.                                        |
| `CancellationReleasesWhatWasNotStartedSoNothingWaitsForTheClaimToExpireAsync` | Service | Cancellation releases unstarted rows at once.                                    |

### InboundQueueServiceTests

`Jube.Test.InboundQueue.InboundQueueServiceTests` — 5 tests. Category: Service.

| Test                                                                         | Kind    | Verifies                                                                                                                 |
|------------------------------------------------------------------------------|---------|--------------------------------------------------------------------------------------------------------------------------|
| `AnApiUserOfTheModelEnqueuesAndGetsTheRowIdBackAsync`                        | Service | An API user of the model enqueues and gets 202 with the row id.                                                          |
| `RefusalsAreTheSameAsForTheLiveInvokeRouteAsync`                             | Service | Disabled, bad guid, unknown model, empty, oversized, malformed and anonymous requests are refused and nothing is stored. |
| `AUserWithoutAnApiKeyOnTheModelOrFromAnotherTenantCannotEnqueueAsync`        | Service | A user without access to the model is forbidden and another tenant gets not found.                                       |
| `AFullQueueAnswersTooManyRequestsAsync`                                      | Service | A full queue answers 429.                                                                                                |
| `DepthDeadListRequeueAndDeleteNeedTheModelPermissionAndStayInTheTenantAsync` | Service | Depth, dead list, requeue and delete need the model permission and stay inside the tenant.                               |

### AmqpInboundConsumerTests

`Jube.Test.InboundQueue.AmqpInboundConsumerTests` — 8 tests. Category: Service.

| Test                                                            | Kind    | Verifies                                                                                                   |
|-----------------------------------------------------------------|---------|------------------------------------------------------------------------------------------------------------|
| `EveryMessageIsAcknowledgedOnlyAfterItWasProcessedAsync`        | Service | Messages are acknowledged after processing and the queue empties. Needs JubeTestRabbitMq (an amqp:// uri). |
| `AMessageBeingProcessedWhenTheNodeDiesIsDeliveredAgainAsync`    | Service | A message in flight when the connection dies is redelivered, so it was not acknowledged early.             |
| `AFailedMessageIsTriedOnceMoreThenDroppedNotLoopedForeverAsync` | Service | A failing message is retried once and then dropped.                                                        |
| `MessagesArrivingBeforeTheModelsHaveLoadedAreKeptNotLostAsync`  | Service | Messages before the models load are requeued and processed once they have.                                 |
| `AtMostThePrefetchIsHeldUnacknowledgedAtOnceAsync`              | Service | No more than the prefetch is held unacknowledged.                                                          |
| `AMessageWithoutAUsableModelHeaderIsDroppedAsync`               | Service | A message with no usable model header is dropped.                                                          |
| `AnUnknownModelIsRetriedOnceThenDroppedAsync`                   | Service | An unknown model is retried once and then dropped.                                                         |
| `StoppingWaitsForTheMessagesInFlightAndStopsDeliveryAsync`      | Service | Stopping waits for in-flight messages and cancels the consumer.                                            |

### SanctionMatchAlgorithmTests

`Jube.Test.Engine.Sanctions.SanctionMatchAlgorithmTests` — 10 tests. Category: Unit.

| Test                                                            | Kind | Verifies                                                                                      |
|-----------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `WithoutOptionsTheMatcherBehavesExactlyAsItAlwaysDid`           | Unit | No options and default options give the original Levenshtein results.                         |
| `JaroWinklerAcceptsAPartOnlyAtOrAboveTheMinimumSimilarity`      | Unit | A part is accepted by Jaro-Winkler only at or above the minimum similarity.                   |
| `JaroWinklerStillHonoursTheMaximumEditDistance`                 | Unit | The check's maximum edit distance still caps a Jaro-Winkler match.                            |
| `JaroWinklerRewardsAMatchingPrefixWhereEditDistanceDoesNot`     | Unit | A shared prefix keeps a Jaro-Winkler match that edit distance would lose.                     |
| `TokenSetIgnoresTheOrderOfTheParts`                             | Unit | Token set matches the same parts in a different order.                                        |
| `TokenSetLowersTheScoreForEveryPartLeftUnmatched`               | Unit | Extra unmatched parts lower the token set score, so a lower minimum is needed.                |
| `TokenSetRejectsUnrelatedNamesAndEmptyInput`                    | Unit | Unrelated names and empty input never match.                                                  |
| `TransliterationLetsACyrillicNameMatchItsLatinSpellingBothWays` | Unit | Transliteration matches Cyrillic and Latin spellings both ways; without it they do not match. |
| `TransliterationDoesNotChangeLatinMatches`                      | Unit | Transliteration leaves Latin matches unchanged.                                               |
| `ALatinTokenViewIsCachedAndRefreshedWhenTheValueChanges`        | Unit | The Latin token view is cached and refreshed when the entry value changes.                    |

### SanctionMatchBenchmarkTests

`Jube.Test.Engine.Sanctions.SanctionMatchBenchmarkTests` — 1 tests. Category: Unit.

| Test                                                 | Kind | Verifies                                                                                                            |
|------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------|
| `EachAlgorithmIsMeasuredAgainstTheSameLabelledPairs` | Unit | Reports precision and recall of each algorithm on 24 labelled name pairs; a stricter threshold never raises recall. |
