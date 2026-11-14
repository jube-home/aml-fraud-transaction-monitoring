---
layout: default
title: Service Queries
nav_order: 14
parent: Testing
---

# Service Queries

Tests of the query services, one per reporting or page query, covering execution, shaping and tenancy isolation of each
query.

## Case by case key value

### CaseByCaseKeyValueServiceTests

`Jube.Test.Service.Query.CaseByCaseKeyValue.CaseByCaseKeyValueServiceTests` — 26 tests. Category: Service. Collection:
Database.

| Test                                                                            | Kind   | Verifies                                                                                   |
|---------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                      | Theory | Create with null or blank user name throws not authenticated.                              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                       | Fact   | Create with user having no tenant throws not authenticated.                                |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                              | Fact   | Create with unknown user throws not authenticated.                                         |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                             | Fact   | Get async throws forbidden when permission missing.                                        |
| `ForbiddenUserCannotSeeExistingMatchingCaseAsync`                               | Fact   | Forbidden user cannot see existing matching case.                                          |
| `GetAsyncMapsEveryFieldOfTheSeededCaseAsync`                                    | Fact   | Get async maps every field of the seeded case.                                             |
| `GetAsyncMapsNullableColumnsToLegacyDefaultsAsync`                              | Fact   | Get async maps nullable columns to legacy defaults.                                        |
| `GetAsyncReturnsMultipleMatchesNewestIdFirstAsync`                              | Fact   | Get async returns multiple matches newest id first.                                        |
| `GetAsyncReturnsEmptyListWhenNothingMatchesAsync`                               | Fact   | Get async returns empty list when nothing matches.                                         |
| `GetAsyncRequiresBothKeyAndValueToMatchAsync`                                   | Fact   | Get async requires both key and value to match.                                            |
| `TenantBCaseIsInvisibleToTenantAAndViceVersaAsync`                              | Fact   | Tenant b case is invisible to tenant a and vice versa.                                     |
| `TenantBCaseIsInvisibleToTenantAEvenWhenTenantARoleIsGrantedOnTheWorkflowAsync` | Fact   | Tenant b case is invisible to tenant a even when tenant a role is granted on the workflow. |
| `CaseIsHiddenWhenCallersRoleHasNoWorkflowRoleAsync`                             | Fact   | Case is hidden when callers role has no workflow role.                                     |
| `CaseIsHiddenWhenCallersRoleHasNoStatusRoleAsync`                               | Fact   | Case is hidden when callers role has no status role.                                       |
| `CaseIsVisibleToEachRoleGrantedOnBothWorkflowAndStatusAsync`                    | Fact   | Case is visible to each role granted on both workflow and status.                          |
| `CaseIsHiddenWhenWorkflowIsSoftDeletedAsync`                                    | Fact   | Case is hidden when workflow is soft deleted.                                              |
| `CaseIsHiddenWhenStatusIsSoftDeletedAsync`                                      | Fact   | Case is hidden when status is soft deleted.                                                |
| `CaseIsHiddenWhenWorkflowRoleIsSoftDeletedAsync`                                | Fact   | Case is hidden when workflow role is soft deleted.                                         |
| `CaseIsHiddenWhenStatusRoleIsSoftDeletedAsync`                                  | Fact   | Case is hidden when status role is soft deleted.                                           |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                 | Fact   | Pre cancelled token throws operation canceled.                                             |
| `PermissionDeniedLogsWarnNotErrorAsync`                                         | Fact   | Permission denied logs warn not error.                                                     |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                  | Fact   | Failure logs error with exception attached and still propagates.                           |
| `AuditLogGetsExactlyOneLineAsync`                                               | Fact   | Audit log gets exactly one line.                                                           |
| `ReadsAndFailuresPublishNothingAsync`                                           | Fact   | Reads and failures publish nothing.                                                        |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                           | Fact   | Catalogue registers unique pascal case no underscore names.                                |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                          | Fact   | Permission denied message resolves for french culture.                                     |

## Case by id

### CaseByIdMapperTests

`Jube.Test.Service.Query.CaseById.CaseByIdMapperTests` — 2 tests. Category: Unit.

| Test                                                  | Kind | Verifies                                                       |
|-------------------------------------------------------|------|----------------------------------------------------------------|
| `AStoredXssPayloadInFormattedPayloadIsSanitisedOnMap` | Fact | A stored xss payload in formatted payload is sanitised on map. |
| `FormattedPayloadNameIsNotAltered`                    | Fact | Formatted payload name is not altered.                         |

### CaseByIdServiceTests

`Jube.Test.Service.Query.CaseById.CaseByIdServiceTests` — 20 tests. Category: Service. Collection: Database.

| Test                                                                | Kind   | Verifies                                                                   |
|---------------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`          | Theory | Create with null or blank user name throws not authenticated.              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`           | Fact   | Create with user having no tenant throws not authenticated.                |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                  | Fact   | Create with unknown user throws not authenticated.                         |
| `GetThrowsForbiddenWhenPermissionMissingAndWritesNoEventAsync`      | Fact   | Get throws forbidden when permission missing and writes no event.          |
| `GetReturnsFullyMappedCaseAsync`                                    | Fact   | Get returns fully mapped case.                                             |
| `GetRecordsExactlyOneCaseViewedEventAsync`                          | Fact   | Get records exactly one case viewed event.                                 |
| `GetWithNoXPathsReturnsEmptyFormattedPayloadAsync`                  | Fact   | Get with no x paths returns empty formatted payload.                       |
| `GetUnknownCaseThrowsNotFoundWritesNoEventAndLogsWarnNotErrorAsync` | Fact   | Get unknown case throws not found writes no event and logs warn not error. |
| `TenantBCannotSeeTenantACaseAndViceVersaAsync`                      | Fact   | Tenant b cannot see tenant a case and vice versa.                          |
| `CaseWhoseWorkflowIsNotGrantedToCallersRoleIsNotFoundAsync`         | Fact   | Case whose workflow is not granted to callers role is not found.           |
| `CaseWhoseStatusIsNotGrantedToCallersRoleIsNotFoundAsync`           | Fact   | Case whose status is not granted to callers role is not found.             |
| `CaseInDeletedWorkflowIsNotFoundAsync`                              | Fact   | Case in deleted workflow is not found.                                     |
| `XPathNotGrantedToCallersRoleIsOmittedFromFormattedPayloadAsync`    | Fact   | X path not granted to callers role is omitted from formatted payload.      |
| `PreCancelledTokenThrowsOperationCanceledAsync`                     | Fact   | Pre cancelled token throws operation canceled.                             |
| `PermissionDeniedLogsWarnNotErrorAsync`                             | Fact   | Permission denied logs warn not error.                                     |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`      | Fact   | Failure logs error with exception attached and still propagates.           |
| `AuditLogGetsExactlyOneLineAsync`                                   | Fact   | Audit log gets exactly one line.                                           |
| `ReadsAndFailuresPublishNothingAsync`                               | Fact   | Reads and failures publish nothing.                                        |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`               | Fact   | Catalogue registers unique pascal case no underscore names.                |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`              | Fact   | Permission denied message resolves for french culture.                     |

## Case by session case search compile

### CaseChecklistReconcilePlanTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistReconcilePlanTests` — 20 tests. Category: Unit.

| Test                                                                   | Kind   | Verifies                                                                          |
|------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `ANewItemThatIsShownIsInsertedWithItsClockAndDueDate`                  | Fact   | A new item that is shown is inserted with its clock and due date.                 |
| `ANewItemWithoutAnSlaHasNoDueDate`                                     | Fact   | A new item without an sla has no due date.                                        |
| `ANewItemThatIsNotShownCreatesNothing`                                 | Theory | A new item that is not shown creates nothing.                                     |
| `AShownItemWhoseConditionFailsIsHidden`                                | Fact   | A shown item whose condition fails is hidden.                                     |
| `AShownItemWhoseConditionPassesOrFailsToEvaluateIsLeftAlone`           | Theory | A shown item whose condition passes or fails to evaluate is left alone.           |
| `AHiddenItemWhoseConditionPassesIsShownAgainWithoutRestartingTheClock` | Fact   | A hidden item whose condition passes is shown again without restarting the clock. |
| `AHiddenItemStaysHiddenUnlessItsConditionPasses`                       | Theory | A hidden item stays hidden unless its condition passes.                           |
| `ADoneItemIsNeverChanged`                                              | Theory | A done item is never changed.                                                     |
| `ADoneItemWhoseDefinitionHasGoneIsNeverChanged`                        | Fact   | A done item whose definition has gone is never changed.                           |
| `AShownItemWhoseDefinitionIsNoLongerInForceIsHidden`                   | Fact   | A shown item whose definition is no longer in force is hidden.                    |
| `ARowOfAnotherStatusIsIgnored`                                         | Fact   | A row of another status is ignored.                                               |
| `AnAlreadyHiddenOrphanIsLeftAlone`                                     | Fact   | An already hidden orphan is left alone.                                           |
| `ASteadyStateProducesNoOperationsAtAll`                                | Fact   | A steady state produces no operations at all.                                     |
| `ADefinitionListedTwiceIsPlannedOnce`                                  | Fact   | A definition listed twice is planned once.                                        |
| `AnOpenItemWithoutAnSlaHasNoSlaState`                                  | Fact   | An open item without an sla has no sla state.                                     |
| `AnOpenItemBeforeItsDueDateIsOnTrackWithTheMinutesLeft`                | Fact   | An open item before its due date is on track with the minutes left.               |
| `AnOpenItemPastItsDueDateIsBreachedWithNegativeMinutes`                | Fact   | An open item past its due date is breached with negative minutes.                 |
| `ADoneItemIsCompletedOnTimeOrLateAgainstItsDueDate`                    | Fact   | A done item is completed on time or late against its due date.                    |
| `TheRollUpCountsOnlyTheCurrentStatusAndIgnoresHiddenItems`             | Fact   | The roll up counts only the current status and ignores hidden items.              |
| `ACaseWithNothingOpenIsComplete`                                       | Fact   | A case with nothing open is complete.                                             |

### CaseChecklistReconcilerTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistReconcilerTests` — 13 tests. Category: Service. Collection:
Database.

| Test                                                                          | Kind | Verifies                                                                                      |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `OnlyTheItemsWhoseConditionsPassAreShownAndEachIsAuditedOnceOnTheTimeline`    | Fact | Only the items whose conditions pass are shown and each is audited once on the timeline.      |
| `AnotherReconcileWithNothingChangedWritesNothing`                             | Fact | Another reconcile with nothing changed writes nothing.                                        |
| `AHighValueCaseAlsoShowsTheCustomerContactItem`                               | Fact | A high value case also shows the customer contact item.                                       |
| `TheRelatedCasesItemFollowsASecondCaseOnTheKeyAndKeepsItsClockWhenShownAgain` | Fact | The related cases item follows a second case on the key and keeps its clock when shown again. |
| `ATickedItemStaysVisibleAndTickedWhenItsConditionStopsPassing`                | Fact | A ticked item stays visible and ticked when its condition stops passing.                      |
| `AClosedCaseIsLeftAlone`                                                      | Fact | A closed case is left alone.                                                                  |
| `AStatusWithNoChecklistsShowsNothing`                                         | Fact | A status with no checklists shows nothing.                                                    |
| `AFailingShowConditionKeepsTheStateAndCreatesNothing`                         | Fact | A failing show condition keeps the state and creates nothing.                                 |
| `TwoReconcilesAtOnceDoNotDuplicateItems`                                      | Fact | Two reconciles at once do not duplicate items.                                                |
| `TheServiceReturnsTheChecklistWithSlaPositionAsTimePasses`                    | Fact | The service returns the checklist with sla position as time passes.                           |
| `AnotherTenantCannotReadTheChecklistOfACase`                                  | Fact | Another tenant cannot read the checklist of a case.                                           |
| `AnUnknownCaseIsNotFound`                                                     | Fact | An unknown case is not found.                                                                 |
| `AnAnonymousCallerIsNotAuthenticated`                                         | Fact | An anonymous caller is not authenticated.                                                     |

### CaseChecklistCheckTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistCheckTests` — 12 tests. Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                                  |
|-------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `CheckingAnItemWithNoValidationTicksItAndWritesOneAuditAndOneTimelineEvent`   | Fact | Checking an item with no validation ticks it and writes one audit and one timeline event. |
| `AFailingValidationReturnsItsMessageAuditsTheRefusalAndLeavesNoTimelineEvent` | Fact | A failing validation returns its message audits the refusal and leaves no timeline event. |
| `APassingValidationTicksTheItemOnce`                                          | Fact | A passing validation ticks the item once.                                                 |
| `AValidationConditionThatErrorsCountsAsARefusalNotAnError`                    | Fact | A validation condition that errors counts as a refusal not an error.                      |
| `OnlyTheHolderOfTheCaseCanCheckOrUncheck`                                     | Fact | Only the holder of the case can check or uncheck.                                         |
| `UncheckingPutsTheItemBackToShownWithItsClockUnchangedAndAuditsIt`            | Fact | Unchecking puts the item back to shown with its clock unchanged and audits it.            |
| `CheckingATickedItemOrUncheckingAnUntickedOneIsAConflictAndWritesNothing`     | Fact | Checking a ticked item or unchecking an unticked one is a conflict and writes nothing.    |
| `UncheckingAnItemWhoseShowConditionNoLongerPassesHidesIt`                     | Fact | Unchecking an item whose show condition no longer passes hides it.                        |
| `AClosedCaseCannotBeChecked`                                                  | Fact | A closed case cannot be checked.                                                          |
| `AnItemOfAnotherCaseIsNotFound`                                               | Fact | An item of another case is not found.                                                     |
| `AnotherTenantCanNeitherCheckNorUncheckNorOverride`                           | Fact | Another tenant can neither check nor uncheck nor override.                                |
| `AnAnonymousCallerCannotCheck`                                                | Fact | An anonymous caller cannot check.                                                         |

### CaseChecklistEnableFlagTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistEnableFlagTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                             | Kind | Verifies                                                                                        |
|----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ASwitchedOffShowConditionShowsTheItemWithoutRunningItsSql`                      | Fact | A switched off show condition shows the item without running its sql.                           |
| `ASwitchedOnShowConditionRunsItsSqlAndKeepsAFailingItemOff`                      | Fact | A switched on show condition runs its sql and keeps a failing item off.                         |
| `SwitchingTheShowConditionOffReShowsAHiddenItemAndSwitchingItOnEvaluatesItAgain` | Fact | Switching the show condition off re-shows a hidden item and switching it on evaluates it again. |
| `ASwitchedOffValidationLetsTheItemBeTickedWithoutRunningItsSql`                  | Fact | A switched off validation lets the item be ticked without running its sql.                      |
| `ASwitchedOnValidationRunsItsSqlAndRefusesTheTickWithTheMessage`                 | Fact | A switched on validation runs its sql and refuses the tick with the message.                    |

### CaseChecklistHistoryTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistHistoryTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                         |
|----------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `TheHistoryListsTheAuditNewestFirstWithItemNamesAndActors`           | Fact | The history lists the audit newest first with item names and actors.             |
| `TheHistoryIncludesGateEntriesThatAreNotAboutOneItem`                | Fact | The history includes gate entries that are not about one item.                   |
| `TheSnapshotCarriesTheOpenOverrideUntilTheStatusChanges`             | Fact | The snapshot carries the open override until the status changes.                 |
| `TheHistoryIsCappedAtTwoHundredEntries`                              | Fact | The history is capped at two hundred entries.                                    |
| `AnotherTenantAnUnknownCaseAndAnAnonymousCallerCannotReadTheHistory` | Fact | Another tenant, an unknown case and an anonymous caller cannot read the history. |

### CaseChecklistGateTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistGateTests` — 15 tests. Category: Service. Collection: Database.

| Test                                                                                  | Kind | Verifies                                                                                              |
|---------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------|
| `AStatusMoveWithShownItemsOpenIsRefusedAndTheRefusalIsAuditedOutsideTheRefusedUpdate` | Fact | A status move with shown items open is refused and the refusal is audited outside the refused update. |
| `ClosingWithShownItemsOpenIsRefused`                                                  | Fact | Closing with shown items open is refused.                                                             |
| `TheMoveIsAllowedOnceEveryShownItemIsTickedAndHiddenOnesDoNotCount`                   | Fact | The move is allowed once every shown item is ticked and hidden ones do not count.                     |
| `AStatusWithNoChecklistsIsNotGated`                                                   | Fact | A status with no checklists is not gated.                                                             |
| `EditsThatDoNotMoveOrCloseTheCaseAreNotGated`                                         | Fact | Edits that do not move or close the case are not gated.                                               |
| `ReopeningAClosedCaseIsNotGatedEvenWithOpenRows`                                      | Fact | Reopening a closed case is not gated even with open rows.                                             |
| `TheHolderOfTheCaseCannotOverrideItsGateEvenWithThePermission`                        | Fact | The holder of the case cannot override its gate even with the permission.                             |
| `AnOverrideNeedsAReasonOfSensibleLengthAndSomethingOpen`                              | Fact | An override needs a reason of sensible length and something open.                                     |
| `AGrantIsConsumedByTheHoldersMoveAndTheNextMoveIsGatedAgain`                          | Fact | A grant is consumed by the holders move and the next move is gated again.                             |
| `AGrantIsDeadOnceTheCaseHasEnteredAnotherStatusEvenIfItComesBack`                     | Fact | A grant is dead once the case has entered another status even if it comes back.                       |
| `TheUserWhoGrantedAnOverrideCannotBeTheOneWhoUsesIt`                                  | Fact | The user who granted an override cannot be the one who uses it.                                       |
| `AutomationMovesTheCaseWithItemsOpenAndRecordsTheBypass`                              | Fact | Automation moves the case with items open and records the bypass.                                     |
| `AutomationMovingACaseWithNothingOpenRecordsNoBypass`                                 | Fact | Automation moving a case with nothing open records no bypass.                                         |
| `AFailureRecordingTheBypassNeverStopsTheAutomatedMove`                                | Fact | A failure recording the bypass never stops the automated move.                                        |
| `AutomationThatLostTheRaceMovesNothingAndRecordsNoBypass`                             | Fact | Automation that lost the race moves nothing and records no bypass.                                    |

### CaseChecklistOverridePermissionTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistOverridePermissionTests` — 2 tests. Category: Service. Collection:
Database.

| Test                                                                       | Kind | Verifies                                                                              |
|----------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `AUserWithoutPermissionSeventyOneCannotOverrideTheGate`                    | Fact | A user without permission seventy one cannot override the gate.                       |
| `ASupervisorWithPermissionSeventyOneRecordsTheOverrideAgainstTheirOwnName` | Fact | A supervisor with permission seventy one records the override against their own name. |

### CaseChecklistStaleFlagTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistStaleFlagTests` — 8 tests. Category: Service. Collection: Database.

| Test                                                              | Kind | Verifies                                                                   |
|-------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `ADecideEventMarksTheCaseStale`                                   | Fact | A decide event marks the case stale.                                       |
| `EveryChecklistEventTypeLeavesTheCaseNotStale`                    | Fact | Every checklist event type leaves the case not stale.                      |
| `TheNeighbouringEventTypesStillMarkTheCaseStale`                  | Fact | The neighbouring event types still mark the case stale.                    |
| `ABulkInsertAcrossTwoCasesMarksBothAndNoOtherCase`                | Fact | A bulk insert across two cases marks both and no other case.               |
| `ABatchOfOnlyChecklistEventsMarksNothingEvenAcrossCases`          | Fact | A batch of only checklist events marks nothing even across cases.          |
| `AnEventWithNoCaseIsStoredAndMarksNothing`                        | Fact | An event with no case is stored and marks nothing.                         |
| `ReconcilingWritesItsOwnTimelineEventsWithoutLeavingTheCaseStale` | Fact | Reconciling writes its own timeline events without leaving the case stale. |
| `ADecideActionThroughTheCaseServiceMarksTheCaseStale`             | Fact | A decide action through the case service marks the case stale.             |

### CaseChecklistReconcileTriggerTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistReconcileTriggerTests` — 15 tests. Category: Service. Collection:
Database.

| Test                                                               | Kind | Verifies                                                                     |
|--------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `ACaseThatHasNeverBeenReconciledIsReconciledOnTheFirstRead`        | Fact | A case that has never been reconciled is reconciled on the first read.       |
| `AFreshCaseThatIsNotStaleRunsNoShowConditionAndWritesNothing`      | Fact | A fresh case that is not stale runs no show condition and writes nothing.    |
| `AStaleCaseIsReconciledEvenWhenItWasReconciledAMomentAgo`          | Fact | A stale case is reconciled even when it was reconciled a moment ago.         |
| `ACaseNotStaleIsReconciledOnceItIsOlderThanTheMaximumAge`          | Fact | A case not stale is reconciled once it is older than the maximum age.        |
| `AMaximumAgeOfZeroReconcilesOnEveryRead`                           | Fact | A maximum age of zero reconciles on every read.                              |
| `AnEventArrivingWhileTheShowConditionsRunLeavesTheCaseStale`       | Fact | An event arriving while the show conditions run leaves the case stale.       |
| `ReadingAFreshCaseThroughTheServiceLeavesItsReconciledDateAlone`   | Fact | Reading a fresh case through the service leaves its reconciled date alone.   |
| `ReadingAStaleCaseThroughTheServiceReconcilesIt`                   | Fact | Reading a stale case through the service reconciles it.                      |
| `TheMaximumAgeSettingOfZeroMakesTheServiceReconcileOnEveryRead`    | Fact | The maximum age setting of zero makes the service reconcile on every read.   |
| `TheMaximumAgeDefaultsToSixtySecondsAndIgnoresAnUnreadableSetting` | Fact | The maximum age defaults to sixty seconds and ignores an unreadable setting. |
| `ClaimingACaseShowsItsItemsWithoutAnyoneReadingTheChecklist`       | Fact | Claiming a case shows its items without anyone reading the checklist.        |
| `MovingTheStatusReconcilesAgainstTheNewStatus`                     | Fact | Moving the status reconciles against the new status.                         |
| `AFailingReconcileAfterTheSaveDoesNotFailOrUndoTheUpdate`          | Fact | A failing reconcile after the save does not fail or undo the update.         |
| `AnUpdateThatNeitherClaimsNorMovesTheCaseDoesNotReconcile`         | Fact | An update that neither claims nor moves the case does not reconcile.         |
| `ChangeEventsArePublishedForReadsTicksUnticksAndTheClaimHook`      | Fact | Change events are published for reads ticks unticks and the claim hook.      |

### CaseChecklistSlaBreachTests

`Jube.Test.Service.Query.CaseChecklist.CaseChecklistSlaBreachTests` — 14 tests. Category: Service. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                                |
|--------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AnOpenItemPastItsDueTimeIsRecordedOnceWithAnAuditRowAndATimelineEvent`  | Fact | An open item past its due time is recorded once with an audit row and a timeline event. |
| `ReadingAgainAfterTheBreachWritesNothingMore`                            | Fact | Reading again after the breach writes nothing more.                                     |
| `NothingIsRecordedBeforeTheDueTime`                                      | Fact | Nothing is recorded before the due time.                                                |
| `AnItemWithNoSlaIsNeverRecordedHoweverLongItStaysOpen`                   | Fact | An item with no sla is never recorded however long it stays open.                       |
| `ADoneItemIsNeverRecordedAsBreachedLater`                                | Fact | A done item is never recorded as breached later.                                        |
| `ACaseClosedUnderneathNeverHasABreachRecorded`                           | Fact | A case closed underneath never has a breach recorded.                                   |
| `AHiddenItemIsNeverRecordedAsBreached`                                   | Fact | A hidden item is never recorded as breached.                                            |
| `TickingAnItemAfterItFellDueRecordsTheBreachOnceAndTheItemCompletesLate` | Fact | Ticking an item after it fell due records the breach once and the item completes late.  |
| `ACompletedLateItemWithNoBreachYetIsRecordedOnceByTheCompletionPath`     | Fact | A completed late item with no breach yet is recorded once by the completion path.       |
| `ACompletionBeforeTheDueTimeIsNotABreach`                                | Fact | A completion before the due time is not a breach.                                       |
| `ConcurrentCallersRecordTheBreachExactlyOnce`                            | Fact | Concurrent callers record the breach exactly once.                                      |
| `ASweepPassRespectsTheBatchSizeAndEveryBreachIsRecordedOnceAcrossPasses` | Fact | A sweep pass respects the batch size and every breach is recorded once across passes.   |
| `TheSweepIsEnabledByDefaultAndSwitchedOffByItsKey`                       | Fact | The sweep is enabled by default and switched off by its key.                            |
| `AFailingOrMissingSweepNeverThrowsIntoTheEngine`                         | Fact | A failing or missing sweep never throws into the engine.                                |

### CaseBySessionCaseSearchCompileServiceTests

`Jube.Test.Service.Query.CaseBySessionCaseSearchCompile.CaseBySessionCaseSearchCompileServiceTests` — 20 tests.
Category: Service. Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                      |
|-------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                          | Theory | Create with null or blank user name throws not authenticated.                                 |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                           | Fact   | Create with user having no tenant throws not authenticated.                                   |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                  | Fact   | Create with unknown user throws not authenticated.                                            |
| `GetWithoutPermissionThrowsForbiddenAndChangesNothingAsync`                         | Fact   | Get without permission throws forbidden and changes nothing.                                  |
| `GetReturnsTheCaseWithEveryFieldMappedAsync`                                        | Fact   | Get returns the case with every field mapped.                                                 |
| `GetRecordsACaseEventLocksTheRetrievedCaseAndRecordsAnExecutionAsync`               | Fact   | Get records a case event locks the retrieved case and records an execution.                   |
| `GetReturnsACaseAlreadyLockedToTheCallerAsync`                                      | Fact   | Get returns a case already locked to the caller.                                              |
| `GetThrowsNotFoundWhenTheCaseIsLockedToAnotherUserAndStillRecordsTheExecutionAsync` | Fact   | Get throws not found when the case is locked to another user and still records the execution. |
| `GetThrowsNotFoundWhenNoCaseMatchesAsync`                                           | Fact   | Get throws not found when no case matches.                                                    |
| `GetWithUnknownGuidFailsAsTheLegacyNullReferenceOutcomeAndLogsErrorAsync`           | Fact   | Get with unknown guid fails as the legacy null reference outcome and logs error.              |
| `SameTenantUserCannotConsumeAnotherUsersCompiledSearchAsync`                        | Fact   | Same tenant user cannot consume another users compiled search.                                |
| `RepositoryGetByGuidIsScopedToTheOwningUserAsync`                                   | Fact   | Repository get by guid is scoped to the owning user.                                          |
| `TenantBUserCannotRetrieveOrLockATenantACaseThroughATenantACompiledSearchAsync`     | Fact   | Tenant b user cannot retrieve or lock a tenant a case through a tenant a compiled search.     |
| `TenantAUserCannotRetrieveOrLockATenantBCaseThroughATenantBCompiledSearchAsync`     | Fact   | Tenant a user cannot retrieve or lock a tenant b case through a tenant b compiled search.     |
| `EachTenantRetrievesOnlyItsOwnCaseAsync`                                            | Fact   | Each tenant retrieves only its own case.                                                      |
| `UserWithoutTheWorkflowRoleCannotRetrieveTheCaseAsync`                              | Fact   | User without the workflow role cannot retrieve the case.                                      |
| `PreCancelledTokenThrowsOperationCanceledAndChangesNothingAsync`                    | Fact   | Pre cancelled token throws operation canceled and changes nothing.                            |
| `SuccessEmitsOneAuditLineOneSpanAndNoChangeEventAsync`                              | Fact   | Success emits one audit line one span and no change event.                                    |
| `ForbiddenAndNotFoundPublishNothingAndAuditTheirOutcomeAsync`                       | Fact   | Forbidden and not found publish nothing and audit their outcome.                              |
| `ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue`                        | Fact   | Tool is declared on the service method and published by the catalogue.                        |

## Case event by case key value

### CaseEventByCaseKeyValueServiceTests

`Jube.Test.Service.Query.CaseEventByCaseKeyValue.CaseEventByCaseKeyValueServiceTests` — 22 tests. Category: Service.
Collection: Database.

| Test                                                                              | Kind   | Verifies                                                                                     |
|-----------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                        | Theory | Create with null or blank user name throws not authenticated.                                |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                         | Fact   | Create with user having no tenant throws not authenticated.                                  |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                | Fact   | Create with unknown user throws not authenticated.                                           |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                               | Fact   | Get async throws forbidden when permission missing.                                          |
| `GetAsyncReturnsSeededEventsNewestFirstWithExactMappingAsync`                     | Fact   | Get async returns seeded events newest first with exact mapping.                             |
| `EventTypeIdIsMappedToDisplayNameAsync`                                           | Theory | Event type id is mapped to display name.                                                     |
| `GetAsyncReturnsEmptyListForUnknownKeyValueAsync`                                 | Fact   | Get async returns empty list for unknown key value.                                          |
| `GetAsyncDoesNotReturnEventsOfAnotherCaseKeyValueAsync`                           | Fact   | Get async does not return events of another case key value.                                  |
| `TenantBCaseIsInvisibleToTenantAUserEvenWhenTenantARoleIsGrantedItsWorkflowAsync` | Fact   | Tenant b case is invisible to tenant a user even when tenant a role is granted its workflow. |
| `SameKeyValueInTwoTenantsReturnsOnlyEachUsersOwnTenantEventsAsync`                | Fact   | Same key value in two tenants returns only each users own tenant events.                     |
| `CaseIsInvisibleWhenUsersRoleIsNotGrantedTheWorkflowAsync`                        | Fact   | Case is invisible when users role is not granted the workflow.                               |
| `CaseIsInvisibleWhenUsersRoleIsNotGrantedTheStatusAsync`                          | Fact   | Case is invisible when users role is not granted the status.                                 |
| `CaseIsInvisibleWhenWorkflowIsSoftDeletedAsync`                                   | Fact   | Case is invisible when workflow is soft deleted.                                             |
| `CaseIsInvisibleWhenStatusIsSoftDeletedAsync`                                     | Fact   | Case is invisible when status is soft deleted.                                               |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                   | Fact   | Pre cancelled token throws operation canceled.                                               |
| `PermissionDeniedLogsWarnNotErrorAsync`                                           | Fact   | Permission denied logs warn not error.                                                       |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                    | Fact   | Failure logs error with exception attached and still propagates.                             |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                         | Fact   | Each call emits one span with outcome tag.                                                   |
| `AuditLogGetsExactlyOneLineAsync`                                                 | Fact   | Audit log gets exactly one line.                                                             |
| `ReadsAndFailuresPublishNothingAsync`                                             | Fact   | Reads and failures publish nothing.                                                          |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                             | Fact   | Catalogue registers unique pascal case no underscore names.                                  |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                            | Fact   | Permission denied message resolves for french culture.                                       |

## Case journal

### CaseKeyJournalAnalyticsServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalAnalyticsServiceTests` — 14 tests.
Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `AnUnusualTransactionStandsOutAgainstTheKeysOwnNormalAndNamesTheFieldAsync` | Fact | An unusual transaction stands out against the keys own normal and names the field. |
| `AKeyIsComparedWithItsPeersFieldByFieldAsync`                               | Fact | A key is compared with its peers field by field.                                   |
| `ThePeersAreTheKeysSegmentWhenItHasOneAndAllKeysWhenItDoesNotAsync`         | Fact | The peers are the keys segment when it has one and all keys when it does not.      |
| `TheMostUnusualPeerComparisonComesFirstAsync`                               | Fact | The most unusual peer comparison comes first.                                      |
| `PeersWithNothingToCompareSayWhyAsync`                                      | Fact | Peers with nothing to compare say why.                                             |
| `ThePeerRepositoryUpdatesInPlaceAndKeepsSegmentsApartAsync`                 | Fact | The peer repository updates in place and keeps segments apart.                     |
| `OnlyTheRequestedSectionsAreComputedAsync`                                  | Fact | Only the requested sections are computed.                                          |
| `FeaturesAndModelScoresAreAnalysedAsMeasuresAlongsideThePayloadAsync`       | Fact | Features and model scores are analysed as measures alongside the payload.          |
| `ACounterpartyRoleBuildsAProfileAndAFirstSeenValueIsNovelAsync`             | Fact | A counterparty role builds a profile and a first seen value is novel.              |
| `APayloadFieldTheCallersRolesCannotSeeIsNeverAnalysedAsync`                 | Fact | A payload field the callers roles cannot see is never analysed.                    |
| `TheFieldsListNarrowsTheMeasuresAndTheLimitTruncatesAsync`                  | Fact | The fields list narrows the measures and the limit truncates.                      |
| `ABadSectionIsRefusedAsync`                                                 | Fact | A bad section is refused.                                                          |
| `ThePermissionTheWorkflowAndTheKeyAreCheckedAsync`                          | Fact | The permission the workflow and the key are checked.                               |
| `AKeyWithNoTransactionsGivesAnEmptyButWellFormedReportAsync`                | Fact | A key with no transactions gives an empty but well formed report.                  |

### CaseKeyJournalChartRendererTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalChartRendererTests` — 3 tests.
Category: Unit.

| Test                                                         | Kind   | Verifies                                                                 |
|--------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EveryChartDrawsAnSvgInBothThemes`                           | Theory | Every chart draws an svg in both themes.                                 |
| `WithNothingToDrawTheChartSaysSoRatherThanDrawingAnEmptyOne` | Theory | With nothing to draw the chart says so rather than drawing an empty one. |
| `EachChartKindMapsToTheSectionItNeeds`                       | Fact   | Each chart kind maps to the section it needs.                            |

### JournalActivationCardsTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalActivationCardsTests` — 9 tests.
Category: Unit.

| Test                                                                         | Kind   | Verifies                                                                                 |
|------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `ARuleAndAModelAreAnchoredOnTheLatestRowTheyFiredOn`                         | Fact   | A rule and a model are anchored on the latest row they fired on.                         |
| `AModelThatNeverActivatedGetsNoCardAndAHiddenRuleNeverAppears`               | Fact   | A model that never activated gets no card and a hidden rule never appears.               |
| `ACalibratedProbabilityCarriesItsBandAndTheRateObservedInItsReliabilityBand` | Fact   | A calibrated probability carries its band and the rate observed in its reliability band. |
| `AScoreThatIsNotCalibratedIsNeverGivenABandOrCalledAProbability`             | Fact   | A score that is not calibrated is never given a band or called a probability.            |
| `TheBandWordFollowsTheCalibratedProbability`                                 | Theory | The band word follows the calibrated probability.                                        |
| `TheFourLargestDriversAreKeptAndANegativeOneIsNeverDroppedForAPositive`      | Fact   | The four largest drivers are kept and a negative one is never dropped for a positive.    |
| `AnInputTheCallersRolesCannotSeeIsNeverNamedAsADriver`                       | Fact   | An input the callers roles cannot see is never named as a driver.                        |
| `ARuleReadsItsInputsFromTheAnchorAndNeverNamesAHiddenPayloadField`           | Fact   | A rule reads its inputs from the anchor and never names a hidden payload field.          |
| `AForcedActivationIsMarkedForcedOnTheAnchor`                                 | Fact   | A forced activation is marked forced on the anchor.                                      |

### JournalTransactionReaderTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalTransactionReaderTests` — 17 tests.
Category: Unit.

| Test                                                                   | Kind   | Verifies                                                                           |
|------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `EveryModelSourceIsReadAndTheValueComesFromItsDetailWhenThereIsOne`    | Fact   | Every model source is read and the value comes from its detail when there is one.  |
| `TheModelTheResultAndTheNarrativeAreCarriedAsArchived`                 | Fact   | The model the result and the narrative are carried as archived.                    |
| `CalibrationKeepsItsBandsInOrder`                                      | Fact   | Calibration keeps its bands in order.                                              |
| `ContributionsAreSignedByTheirDirectionAndExactnessIsKept`             | Fact   | Contributions are signed by their direction and exactness is kept.                 |
| `APayloadInputTheCallerCannotSeeIsHiddenAndNeverNamedOrCounted`        | Fact   | A payload input the caller cannot see is hidden and never named or counted.        |
| `AFeatureOrAbstractionInputIsNeverHiddenWhateverTheXPathsAre`          | Fact   | A feature or abstraction input is never hidden whatever the x paths are.           |
| `TheDecisionPathIsInOrderAndHidesTheNodesOnInputsTheCallerCannotSee`   | Fact   | The decision path is in order and hides the nodes on inputs the caller cannot see. |
| `VisibilityIsNotCaseSensitive`                                         | Fact   | Visibility is not case sensitive.                                                  |
| `TheNamesAXPathMakesVisibleIncludeItsLastPathSegment`                  | Fact   | The names AX path makes visible include its last path segment.                     |
| `ValuesCarryTheirGroupAndAnHttpAdaptationIsReadByItsValue`             | Fact   | Values carry their group and an http adaptation is read by its value.              |
| `GraphAndSvirfdRecordsAreFlattenedIntoDottedValuesUnderTheirOwnGroups` | Fact   | Graph and svirfd records are flattened into dotted values under their own groups.  |
| `OnlyVisibleActivationsAreListedWithTheirForcedFlag`                   | Fact   | Only visible activations are listed with their forced flag.                        |
| `KeysAreTheDrillAndIdentifierXPathsWithTheirValues`                    | Fact   | Keys are the drill and identifier x paths with their values.                       |
| `TheSignedContributionAppliesTheDirectionToTheMagnitude`               | Theory | The signed contribution applies the direction to the magnitude.                    |
| `NoWeightIsNoSignedContribution`                                       | Fact   | No weight is no signed contribution.                                               |
| `AnAdaptationWithNoDetailStillHasItsValueAndNothingElse`               | Fact   | An adaptation with no detail still has its value and nothing else.                 |
| `GarbageInTheArchiveIsToleratedRatherThanFailing`                      | Fact   | Garbage in the archive is tolerated rather than failing.                           |

### CaseKeyJournalTransactionChartRendererTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTransactionChartRendererTests` — 9 tests.
Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                          |
|-----------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `ContributionsDrawAsAWaterfallWhenExactAndAsBarsWhenRelative`         | Theory | Contributions draw as a waterfall when exact and as bars when relative.           |
| `HiddenInputsAreNeverDrawnAndTheChartSaysHowManyWereHidden`           | Fact   | Hidden inputs are never drawn and the chart says how many were hidden.            |
| `OnlyTheLargestFifteenInputsAreDrawn`                                 | Fact   | Only the largest fifteen inputs are drawn.                                        |
| `CalibrationDrawsTheReliabilityCurveInBothThemes`                     | Fact   | Calibration draws the reliability curve in both themes.                           |
| `WithNothingToDrawTheChartSaysSo`                                     | Theory | With nothing to draw the chart says so.                                           |
| `TheGaugeNamesTheModelAndSaysProbabilityOnlyWhenTheScoreIsCalibrated` | Fact   | The gauge names the model and says probability only when the score is calibrated. |
| `TheDriversChartKeepsTheNegativeInputAndGroupsTheRest`                | Theory | The drivers chart keeps the negative input and groups the rest.                   |
| `AHiddenInputIsNeverDrawnEvenIfSomethingLeftAFigureOnIt`              | Fact   | A hidden input is never drawn even if something left a figure on it.              |
| `AContributionOfOnlyHiddenInputsHasNothingToDraw`                     | Fact   | A contribution of only hidden inputs has nothing to draw.                         |

### CaseKeyJournalBindingTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalBindingTests` — 6 tests.
Category: Unit.

| Test                                                                    | Kind   | Verifies                                                                          |
|-------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `TheRowsRequestBindsFromABodyWithOnlyTheWorkflowTheKeyAndTheValueAsync` | Theory | The rows request binds from a body with only the workflow, the key and the value. |
| `OmittedOptionsOfTheRowsRequestStayNullSoTheServiceDefaultsApplyAsync`  | Fact   | Omitted options of the rows request stay null so the service defaults apply.      |
| `TheTransactionRequestBindsFromTheWorkflowAndTheIdAsync`                | Fact   | The transaction request binds from the workflow and the id.                       |
| `AWronglyTypedTransactionRequestIsRefusedAtTheBoundaryAsync`            | Theory | A wrongly typed transaction request is refused at the boundary.                   |
| `AWronglyTypedRowsOptionIsRefusedAtTheBoundaryAsync`                    | Theory | A wrongly typed rows option is refused at the boundary.                           |
| `EveryOptionOfTheRowsRequestIsOptionalOnTheWire`                        | Fact   | Every option of the rows request is optional on the wire.                         |

### CaseSimilarityVectorReaderTests

`Jube.Test.Service.Query.CaseSimilarity.CaseSimilarityVectorReaderTests` — 10 tests.
Category: Unit.

| Test                                                                        | Kind   | Verifies                                                                                 |
|-----------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `EveryRuleThatFiredOnAnyTransactionIsAComponentAndAHiddenOneIsNot`          | Fact   | Every rule that fired on any transaction is a component and a hidden one is not.         |
| `AnArchiveKeyTextFieldIsAnEntityAndAnyOtherTextFieldIsACategory`            | Fact   | An archive key text field is an entity and any other text field is a category.           |
| `AWholeNumberThatIsAnArchiveKeyIsAnEntityNotAMeasure`                       | Fact   | A whole number that is an archive key is an entity not a measure.                        |
| `ANumberIsAggregatedAcrossEveryTransaction`                                 | Fact   | A number is aggregated across every transaction.                                         |
| `DatesAndCoordinatesAndEmptyValuesContributeNothing`                        | Fact   | Dates and coordinates and empty values contribute nothing.                               |
| `AFieldThatDiffersOnEveryTransactionIsLeftOut`                              | Fact   | A field that differs on every transaction is left out.                                   |
| `AModelScoreIsItsHighestAcrossTransactionsAndFeaturesComeFromTheLatestOnly` | Fact   | A model score is its highest across transactions and features come from the latest only. |
| `TheVectorIsCappedAndOverlongComponentsAreDropped`                          | Fact   | The vector is capped and overlong components are dropped.                                |
| `TheHashIsStableDiffersByFieldAndBySecretAndNeverContainsTheValue`          | Fact   | The hash is stable differs by field and by secret and never contains the value.          |
| `TheHashingKeyIsItsOwnSettingAndAnEmptyOrUnresolvedOneCountsAsNotSet`       | Theory | The hashing key is its own setting and an empty or unresolved one counts as not set.     |
| `AReasonNamesTheRuleOrFieldAndNeverItsValue`                                | Theory | A reason names the rule or field and never its value.                                    |

### CaseSimilarityJobTests

`Jube.Test.Service.Query.CaseSimilarity.CaseSimilarityJobTests` — 6 tests.
Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                    |
|-----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `TheVectorIsBuiltFromEveryTransactionOnTheKeyWithNoConfigurationAsync`            | Fact | The vector is built from every transaction on the key with no configuration.                |
| `ARebuildFollowsNewTransactionsAndTagsAndKeepsTheFrequenciesEqualToARecountAsync` | Fact | A rebuild follows new transactions and tags and keeps the frequencies equal to a recount.   |
| `AnOpenCaseIsDueAgainOnlyOnceItsLastCheckIsOldAsync`                              | Fact | An open case is due again only once its last check is old.                                  |
| `AClosedCaseIsRebuiltOnceAfterItClosesAndThenLeftAsItStoodAsync`                  | Fact | A closed case is rebuilt once after it closes and then left as it stood.                    |
| `ACaseWhoseWorkflowIsGoneIsStampedWithAnEmptyVectorSoItNeverBlocksTheSweepAsync`  | Fact | A case whose workflow is gone is stamped with an empty vector so it never blocks the sweep. |
| `BuildingAVectorWritesNothingElseAboutTheCaseAsync`                               | Fact | Building a vector writes nothing else about the case.                                       |
| `WithoutAHashingKeyNoVectorIsBuiltAndTheCaseStaysDueAsync`                        | Fact | Without a hashing key no vector is built and the case stays due.                            |

### CaseSimilarityServiceTests

`Jube.Test.Service.Query.CaseSimilarity.CaseSimilarityServiceTests` — 6 tests.
Category: Service. Collection: Database.

| Test                                                                               | Kind | Verifies                                                                                    |
|------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `SimilarCasesAreRankedCountedByOutcomeAndExplainedByTheFactsTheyShareAsync`        | Fact | Similar cases are ranked counted by outcome and explained by the facts they share.          |
| `AFactEveryCaseCarriesNeverMakesTwoCasesSimilarAsync`                              | Fact | A fact every case carries never makes two cases similar.                                    |
| `TooFewClosedSimilarCasesAreCountedButNotEnoughToJudgeAsync`                       | Fact | Too few closed similar cases are counted but not enough to judge.                           |
| `ACaseTheSweepHasNotReachedSaysSoAndListsNothingAsync`                             | Fact | A case the sweep has not reached says so and lists nothing.                                 |
| `SimilarCasesInAStatusOrWorkflowTheCallerCannotSeeAreNeitherListedNorCountedAsync` | Fact | Similar cases in a status or workflow the caller cannot see are neither listed nor counted. |
| `ReadingNeedsThePermissionAndTheCaseMustBeVisibleToTheCallerAsync`                 | Fact | Reading needs the permission and the case must be visible to the caller.                    |

### CaseKeyJournalPeerJobTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalPeerJobTests` — 9 tests.
Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `RebuildsThePeerDistributionOfEachMeasureFromTheArchiveAsync`                   | Fact | Rebuilds the peer distribution of each measure from the archive.                        |
| `SegmentsGetTheirOwnDistributionAndSmallOnesGetNoneAsync`                       | Fact | Segments get their own distribution and small ones get none.                            |
| `FewerThanTwentyKeysBuildNothingAsync`                                          | Fact | Fewer than twenty keys build nothing.                                                   |
| `OnlyTheNewestRowsUpToTheCapAreReadAsync`                                       | Fact | Only the newest rows up to the cap are read.                                            |
| `PagesThroughTheArchiveWhenTheSampleIsLargerThanOnePageAsync`                   | Fact | Pages through the archive when the sample is larger than one page.                      |
| `TransactionsOutsideTheWindowAreLeftOutAsync`                                   | Fact | Transactions outside the window are left out.                                           |
| `ARecentRebuildIsLeftAloneAndAnOldOneIsReplacedInPlaceAsync`                    | Fact | A recent rebuild is left alone and an old one is replaced in place.                     |
| `ADistributionThatNoLongerQualifiesIsSoftDeletedAndRevivedWhenItDoesAgainAsync` | Fact | A distribution that no longer qualifies is soft deleted and revived when it does again. |
| `TheRunWalksEveryWorkflowWithACaseKeyAndCountsWhatItDidAsync`                   | Fact | The run walks every workflow with a case key and counts what it did.                    |

### CaseKeyJournalPeerAccumulatorTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalPeerAccumulatorTests` — 6 tests.
Category: Unit.

| Test                                                         | Kind   | Verifies                                                               |
|--------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `BuildsOneDistributionPerMeasureFromTheMedianOfEachKey`      | Fact   | Builds one distribution per measure from the median of each key.       |
| `NothingIsBuiltForFewerThanTwentyKeys`                       | Fact   | Nothing is built for fewer than twenty keys.                           |
| `KeysAreCountedOnceWhateverTheirRows`                        | Fact   | Keys are counted once whatever their rows.                             |
| `EachSegmentGetsItsOwnDistributionTakenFromTheKeysNewestRow` | Fact   | Each segment gets its own distribution taken from the keys newest row. |
| `IdentifiersAndCategoriesAreNotMeasures`                     | Fact   | Identifiers and categories are not measures.                           |
| `RowsWithoutAKeyOrReadableJsonAreIgnored`                    | Theory | Rows without a key or readable json are ignored.                       |

### CaseKeyJournalPinServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalPinServiceTests` — 7 tests.
Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                             |
|-------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `PinningAndUnpinningIsSharedAndListedMostRecentFirstAsync`                    | Fact | Pinning and unpinning is shared and listed most recent first.                        |
| `AWorkflowRowThatWasReplacedWithAnotherModelPinsTheLiveRowsTransactionsAsync` | Fact | A workflow row that was replaced with another model pins the live rows transactions. |
| `OnlyTransactionsOfTheWorkflowsModelCanBePinnedAsync`                         | Fact | Only transactions of the workflows model can be pinned.                              |
| `ACaseHoldsNoMoreThanFiftyPinsAsync`                                          | Fact | A case holds no more than fifty pins.                                                |
| `TheTransactionListIsValidatedAsync`                                          | Fact | The transaction list is validated.                                                   |
| `ACaseLockedToAnotherUserCannotBeChangedButMyOwnLockCanAsync`                 | Fact | A case locked to another user cannot be changed but my own lock can.                 |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync`                    | Fact | The permission the workflow roles and the tenant are checked.                        |

### CaseKeyJournalTagJobTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTagJobTests` — 23 tests.
Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                              |
|-----------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `PreviewCountsTheTransactionsOfTheViewAsync`                                | Fact | Preview counts the transactions of the view.                                          |
| `ASubmittedJobTagsEveryMatchingTransactionAndReportsItsProgressAsync`       | Fact | A submitted job tags every matching transaction and reports its progress.             |
| `AJobAddsAndRemovesAndLeavesRowsThatAlreadyHaveTheTagAloneAsync`            | Fact | A job adds and removes and leaves rows that already have the tag alone.               |
| `UndoPutsEveryChangedTransactionBackAndCanOnlyBeAskedForOnceAsync`          | Fact | Undo puts every changed transaction back and can only be asked for once.              |
| `ACaseLockedToAnotherUserIsLeftAloneAndCountedAsync`                        | Fact | A case locked to another user is left alone and counted.                              |
| `AQueuedJobCanBeCancelledAndANeverRunJobIsNotPickedUpAsync`                 | Fact | A queued job can be cancelled and a never run job is not picked up.                   |
| `ARunningJobStopsAtTheEndOfTheBatchWhenCancelledAndCanStillBeUndoneAsync`   | Fact | A running job stops at the end of the batch when cancelled and can still be undone.   |
| `AJobWhoseTagIsNoLongerAllowedFailsWithWhyAsync`                            | Fact | A job whose tag is no longer allowed fails with why.                                  |
| `ARunningJobThatStoppedReportingIsTakenUpAgainAsync`                        | Fact | A running job that stopped reporting is taken up again.                               |
| `TheViewIsValidatedAndTooManyOrNoMatchesAreRefusedAsync`                    | Fact | The view is validated and too many or no matches are refused.                         |
| `MergedKeysAndTheWindowAndActivationsDecideWhichTransactionsMatchAsync`     | Fact | Merged keys and the window and activations decide which transactions match.           |
| `PreviewPagesThroughMoreMatchesThanOnePageHoldsAsync`                       | Fact | Preview pages through more matches than one page holds.                               |
| `AStoppedOrFailedJobCannotBeUndoneAndACancelledUndoCanBeAskedForAgainAsync` | Fact | A stopped or failed job cannot be undone and a cancelled undo can be asked for again. |
| `AnUndoCountsTheTransactionsItLeftAloneBecauseTheirCaseIsLockedAsync`       | Fact | An undo counts the transactions it left alone because their case is locked.           |
| `ARunningJobIsStoppedByTheServiceAtTheNextBatchAsync`                       | Fact | A running job is stopped by the service at the next batch.                            |
| `TheMatcherStopsReadingOnceMoreThanTheMaximumMatchAsync`                    | Fact | The matcher stops reading once more than the maximum match.                           |
| `AFilterOnTheArchiveDecidesWhichTransactionsAreCountedAndTaggedAsync`       | Fact | A filter on the archive decides which transactions are counted and tagged.            |
| `AFieldWhoseStoredValueCannotBeReadAsItsTypeIsRefusedAsync`                 | Fact | A field whose stored value cannot be read as its type is refused.                     |
| `TheFilterAppliesToEveryLaneAsync`                                          | Fact | The filter applies to every lane.                                                     |
| `AFilterThatIsNotValidIsRefusedAsync`                                       | Fact | A filter that is not valid is refused.                                                |
| `OnlyTheCallersOwnJobsAreVisibleAndTheListIsNewestFirstAsync`               | Fact | Only the callers own jobs are visible and the list is newest first.                   |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync`                  | Fact | The permission the workflow roles and the tenant are checked.                         |
| `OneUserCanHaveOnlyAFewJobsQueuedOnAWorkflowAtOnceAsync`                    | Fact | One user can have only a few jobs queued on a workflow at once.                       |

### CaseKeyJournalTagServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTagServiceTests` — 14 tests.
Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `OptionsListOnlyTheActiveTagsOfTheWorkflowsModelAlphabeticallyAsync`            | Fact | Options list only the active tags of the workflows model alphabetically.              |
| `AddingAndRemovingChangesEverySelectedTransactionAndReportsBeforeAndAfterAsync` | Fact | Adding and removing changes every selected transaction and reports before and after.  |
| `TheTagsAreRecordedInTheArchiveTagTableAsync`                                   | Fact | The tags are recorded in the archive tag table.                                       |
| `ATransactionThatAlreadyHasTheTagIsLeftUntouchedAsync`                          | Fact | A transaction that already has the tag is left untouched.                             |
| `RestoringThePreviousSetsUndoesATaggingAsync`                                   | Fact | Restoring the previous sets undoes a tagging.                                         |
| `ATransactionOfAnotherModelIsSkippedAndNeverChangedAsync`                       | Fact | A transaction of another model is skipped and never changed.                          |
| `ACaseLockedToAnotherUserIsLeftAloneWhileMyOwnLockAndNoLockAreTaggedAsync`      | Fact | A case locked to another user is left alone while my own lock and no lock are tagged. |
| `AWorkflowRowThatWasReplacedWithAnotherModelTagsTheLiveRowsTransactionsAsync`   | Fact | A workflow row that was replaced with another model tags the live rows transactions.  |
| `ATagTheModelDoesNotAllowIsRefusedAndNothingChangesAsync`                       | Fact | A tag the model does not allow is refused and nothing changes.                        |
| `TheRequestIsValidatedAsync`                                                    | Fact | The request is validated.                                                             |
| `TheRestoreRequestIsValidatedAsync`                                             | Fact | The restore request is validated.                                                     |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync`                      | Fact | The permission the workflow roles and the tenant are checked.                         |
| `ANullNameSetOrTransactionListIsRefusedRatherThanFailingAsync`                  | Fact | A null name set or transaction list is refused rather than failing.                   |
| `ARestoreCanOnlyWriteATagNameTheModelHasDefinedAsync`                           | Fact | A restore can only write a tag name the model has defined.                            |

### CaseKeyJournalTransactionServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTransactionServiceTests` — 17 tests.
Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `OpensATransactionWithItsActivationsFeaturesKeysAndModelJourneyAsync`           | Fact | Opens a transaction with its activations features keys and model journey.               |
| `TheJourneyNeverNamesAPayloadInputTheCallersRolesCannotSeeAsync`                | Fact | The journey never names a payload input the callers roles cannot see.                   |
| `ThePayloadIsOnlyTheFieldsTheCallersRolesMaySeeAsync`                           | Fact | The payload is only the fields the callers roles may see.                               |
| `ATransactionOfAnotherModelIsNotFoundEvenWithItsRealIdAsync`                    | Fact | A transaction of another model is not found even with its real id.                      |
| `AnUnknownTransactionAndABadIdAreRefusedAsync`                                  | Fact | An unknown transaction and a bad id are refused.                                        |
| `ThePermissionAndTheWorkflowRolesAreCheckedAsync`                               | Fact | The permission and the workflow roles are checked.                                      |
| `TheQueryNeverReturnsAnotherTenantsTransactionEvenForTheirModelAndIdAsync`      | Fact | The query never returns another tenants transaction even for their model and id.        |
| `AnotherTenantsUserNeverGetsATransactionAsync`                                  | Fact | Another tenants user never gets a transaction.                                          |
| `AroundListsTheSameDaysTransactionsThatShareAKeyAndNothingElseAsync`            | Fact | Around lists the same days transactions that share a key and nothing else.              |
| `AroundCapsEachGroupAndSaysSoAsync`                                             | Fact | Around caps each group and says so.                                                     |
| `AroundIsRefusedWithoutThePermissionOrAnotherTenantsTransactionAsync`           | Fact | Around is refused without the permission or another tenants transaction.                |
| `TheActivationsOfAJournalViewAreOneCardEachAnchoredOnTheLatestTransactionAsync` | Fact | The activations of a journal view are one card each anchored on the latest transaction. |
| `TheActivationsNeverNameAPayloadInputTheCallersRolesCannotSeeAsync`             | Fact | The activations never name a payload input the callers roles cannot see.                |
| `TheActivationsAreRefusedForAWorkflowOrUserWithoutAccessAsync`                  | Fact | The activations are refused for a workflow or user without access.                      |
| `TheActivationsFollowTheJournalWindowAndActivationsOnlyScopeAsync`              | Fact | The activations follow the journal window and activations only scope.                   |
| `WhatFiredOnOneTransactionIsItsRulesAndTheModelsThatCrossedTheirThresholdAsync` | Fact | What fired on one transaction is its rules and the models that crossed their threshold. |
| `WhatFiredOnATransactionIsRefusedForAWorkflowTheCallerCannotSeeAsync`           | Fact | What fired on a transaction is refused for a workflow the caller cannot see.            |

### CaseKeyJournalReplayServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalReplayServiceTests` — 8 tests.
Category: Service. Collection: Database.

| Test                                                                           | Kind | Verifies                                                                                |
|--------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AReplayIsOnlySettingsSharedOnTheCaseAndListedNewestFirstAsync`                | Fact | A replay is only settings shared on the case and listed newest first.                   |
| `ADeletedReplayIsNoLongerOfferedAndTheRowIsKeptAsync`                          | Fact | A deleted replay is no longer offered and the row is kept.                              |
| `TheReplaySettingsAreValidatedAndACaseHoldsNoMoreThanTenAsync`                 | Fact | The replay settings are validated and a case holds no more than ten.                    |
| `ACaseLockedToAnotherUserCannotHaveReplaysSavedOrDeletedButMyOwnLockCanAsync`  | Fact | A case locked to another user cannot have replays saved or deleted but my own lock can. |
| `TheSummaryWorksTheSavedSettingsOutAgainFromTheArchiveAsync`                   | Fact | The summary works the saved settings out again from the archive.                        |
| `TheLensAndShowingEverythingAreSavedWithTheViewAsync`                          | Fact | The lens and showing everything are saved with the view.                                |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync`                     | Fact | The permission the workflow roles and the tenant are checked.                           |
| `TheSpeedAndDistanceLimitsAreSavedWithTheReplayAndUsedWhenItIsSummarisedAsync` | Fact | The speed and distance limits are saved with the replay and used when it is summarised. |

### CaseKeyJournalServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalServiceTests` — 34 tests.
Category: Service. Collection: Database.

| Test                                                                               | Kind   | Verifies                                                                                                                                                                                                                                |
|------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ReturnsOnlyTheKeysTransactionsOfTheWorkflowsModelNewestFirstAsync`                | Fact   | Returns only the keys transactions of the workflows model newest first.                                                                                                                                                                 |
| `AWorkflowRowThatWasReplacedWithAnotherModelReadsTheLiveRowsTransactionsAsync`     | Fact   | A workflow row that was replaced with another model reads the live rows transactions.                                                                                                                                                   |
| `MergedKeysShareOneTimelineAndEachRowSaysWhichLanesItBelongsToAsync`               | Fact   | Merged keys share one timeline and each row says which lanes it belongs to.                                                                                                                                                             |
| `ANumericKeyCanBeMergedAsARangeAndATextKeyOnlyByEqualityAsync`                     | Fact   | A numeric key can be merged as a range (at least, between) through the typed archive key column, while a text key only matches by equality and an unknown key, an unreadable value or an unknown operator are refused with their codes. |
| `AKeyThatIsNotAnArchiveKeyCannotBeOpenedAsAJournalAsync`                           | Fact   | A key whose model field is not flagged Archive Key cannot be opened as a journal and says so.                                                                                                                                           |
| `MergeNeedsMatchingKeysAndValuesWithAtMostThreeExtraAsync`                         | Fact   | Merge needs matching keys and values with at most three extra.                                                                                                                                                                          |
| `TheJournalSaysWhichColumnsLocateATransactionOnlyWhenTheCallerCanSeeThemAsync`     | Fact   | The journal says which columns locate a transaction only when the caller can see them.                                                                                                                                                  |
| `TheTrackPutsTransactionsInTimeOrderAndMarksTheMovementThatCouldNotHappenAsync`    | Fact   | The track puts transactions in time order and marks the movement that could not happen.                                                                                                                                                 |
| `TheTrackUsesTheSpeedAndDistanceLimitsItIsGivenAndRefusesSillyOnesAsync`           | Fact   | The track uses the speed and distance limits it is given and refuses silly ones.                                                                                                                                                        |
| `TheFilterIsAppliedInTheArchiveBeforeTheLimitAndTheUnfilteredTotalIsReportedAsync` | Fact   | The filter is applied in the archive before the limit and the unfiltered total is reported.                                                                                                                                             |
| `TheJournalOffersThePayloadFieldsSetAsDrillFieldsAsKeysAsync`                      | Fact   | The journal offers the payload fields set as drill fields as keys.                                                                                                                                                                      |
| `APinnedTransactionIsHeldToTheFilterToo`                                           | Fact   | A pinned transaction is held to the filter too.                                                                                                                                                                                         |
| `TheTrackCanReturnEveryTransactionWithTheFilteredOnesPickedOutAsync`               | Fact   | The track can return every transaction with the filtered ones picked out.                                                                                                                                                               |
| `TheTrackAppliesTheFilterAndCanReturnOnlyTheSummaryAsync`                          | Fact   | The track applies the filter and can return only the summary.                                                                                                                                                                           |
| `EachMergedKeyIsItsOwnTrailAndMovementIsOnlyCountedWithinATrailAsync`              | Fact   | Each merged key is its own trail and movement is only counted within a trail.                                                                                                                                                           |
| `TheTrackHasNothingToDrawWhenTheCallerCannotSeeTheLocationColumnsAsync`            | Fact   | The track has nothing to draw when the caller cannot see the location columns.                                                                                                                                                          |
| `TheTrackRefusesAViewThatIsNotValidAndAWorkflowTheCallerCannotSeeAsync`            | Fact   | The track refuses a view that is not valid and a workflow the caller cannot see.                                                                                                                                                        |
| `TheBoldLineMarksRowsSharingTheCasesValueInEveryLensAsync`                         | Fact   | The bold line marks rows sharing the cases value in every lens.                                                                                                                                                                         |
| `PinnedTransactionsComeFirstEvenOutsideTheWindowAndAreMarkedAsync`                 | Fact   | Pinned transactions come first even outside the window and are marked.                                                                                                                                                                  |
| `PayloadColumnsAreOnlyTheXPathsTheCallersRolesMaySeeAsync`                         | Fact   | Payload columns are only the x paths the callers roles may see.                                                                                                                                                                         |
| `AConfiguredRoleIsReturnedAsConfiguredAsync`                                       | Fact   | A configured role is returned as configured.                                                                                                                                                                                            |
| `EachLensReturnsItsOwnColumnsAndTheBaseColumnsAsync`                               | Theory | Each lens returns its own columns and the base columns.                                                                                                                                                                                 |
| `HiddenActivationsAreNeverNamedAndTheLensCountsSayWhatHasContentAsync`             | Fact   | Hidden activations are never named and the lens counts say what has content.                                                                                                                                                            |
| `ANumericKeyIsFoundWhenTheValueIsAskedForAsText`                                   | Fact   | A numeric key is found when the value is asked for as text.                                                                                                                                                                             |
| `TheLimitTruncatesTheNewestFirstAndSaysSoAsync`                                    | Fact   | The limit truncates the newest first and says so.                                                                                                                                                                                       |
| `TheWindowTheElevationAndActivationsOnlyAllNarrowTheRowsAsync`                     | Fact   | The window the elevation and activations only all narrow the rows.                                                                                                                                                                      |
| `TagsComeBackAsAPlainListAsync`                                                    | Fact   | Tags come back as a plain list.                                                                                                                                                                                                         |
| `ABadKeyOrValueIsRefusedAsync`                                                     | Theory | A bad key or value is refused.                                                                                                                                                                                                          |
| `ABadLensOrLimitIsRefusedAsync`                                                    | Theory | A bad lens or limit is refused.                                                                                                                                                                                                         |
| `AReversedOrOversizedWindowIsRefusedAsync`                                         | Fact   | A reversed or oversized window is refused.                                                                                                                                                                                              |
| `ARoleWithoutThePermissionIsRefusedAsync`                                          | Fact   | A role without the permission is refused.                                                                                                                                                                                               |
| `AWorkflowTheCallerHasNoRoleOnIsNotFoundAndAnUnknownOneTooAsync`                   | Fact   | A workflow the caller has no role on is not found and an unknown one too.                                                                                                                                                               |
| `TheQueryNeverReturnsAnotherTenantsTransactionsEvenWhenAskedForTheirModelAsync`    | Fact   | The query never returns another tenants transactions even when asked for their model.                                                                                                                                                   |
| `ANullRequestIsRefusedAndAnUnauthenticatedCallerNeverGetsAServiceAsync`            | Fact   | A null request is refused and an unauthenticated caller never gets a service.                                                                                                                                                           |

### FieldRoleInferenceTests

`Jube.Test.Service.Query.CaseKeyJournal.FieldRoleInferenceTests` — 6 tests.
Category: Unit.

| Test                                                                | Kind   | Verifies                                                                        |
|---------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `AConfiguredRoleAlwaysWinsAndIsNotCalledInferred`                   | Fact   | A configured role always wins and is not called inferred.                       |
| `TheTypeAndTheDrillFlagDecideWhenNothingIsConfigured`               | Theory | The type and the drill flag decide when nothing is configured.                  |
| `AFewRepeatedStringsAreACategoryAndManyDistinctOnesAreAnIdentifier` | Fact   | A few repeated strings are a category and many distinct ones are an identifier. |
| `WithFewRowsALowNumberOfDistinctValuesIsStillACategory`             | Fact   | With few rows a low number of distinct values is still a category.              |
| `TooManyDistinctValuesIsNeverACategoryEvenWithFewRows`              | Fact   | Too many distinct values is never a category even with few rows.                |
| `AColumnWithNoValuesIsLeftAsACategoryRatherThanFailing`             | Fact   | A column with no values is left as a category rather than failing.              |

### ArchiveFilterTests

`Jube.Test.Service.Query.CaseKeyJournal.ArchiveFilterTests` — 10 tests.
Category: Unit.

| Test                                               | Kind   | Verifies                                                  |
|----------------------------------------------------|--------|-----------------------------------------------------------|
| `AFilterWithNoRulesIsNoFilter`                     | Fact   | A filter with no rules is no filter.                      |
| `NestedGroupsBecomeParameterisedSqlInOrder`        | Fact   | Nested groups become parameterised SQL in order.          |
| `EveryOperatorMapsToItsSql`                        | Theory | Every operator maps to its SQL.                           |
| `ValuesAreConvertedToTheTypeOfTheField`            | Fact   | Values are converted to the type of the field.            |
| `AValueNeverReachesTheSql`                         | Fact   | A value never reaches the SQL.                            |
| `AFilterThatIsNotValidIsRefused`                   | Theory | A filter that is not valid is refused.                    |
| `AShapeThatIsNotAFilterIsRefused`                  | Theory | A shape that is not a filter is refused.                  |
| `AFilterThatIsTooBigIsRefused`                     | Fact   | A filter that is too big is refused.                      |
| `ThePersistedTextKeepsOnlyWhatTheServerUnderstood` | Fact   | The persisted text keeps only what the server understood. |
| `OnlyFieldsThatCanBeFilteredAreOffered`            | Fact   | Only fields that can be filtered are offered.             |

### JournalLocationTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalLocationTests` — 2 tests.
Category: Unit.

| Test                                                      | Kind | Verifies                                                           |
|-----------------------------------------------------------|------|--------------------------------------------------------------------|
| `TheColumnsAreFoundByTheFieldsTheModelUses`               | Fact | The columns are found by the fields the model uses.                |
| `NoLocationIsReportedUnlessBothColumnsAreVisibleAndNamed` | Fact | No location is reported unless both columns are visible and named. |

### JournalTravelTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalTravelTests` — 10 tests.
Category: Unit.

| Test                                                      | Kind   | Verifies                                                             |
|-----------------------------------------------------------|--------|----------------------------------------------------------------------|
| `TheDistanceBetweenTwoCitiesIsRight`                      | Fact   | The distance between two cities is right.                            |
| `OnlyARealPositionIsLocated`                              | Theory | Only a real position is located.                                     |
| `NoLocatedTransactionsMeansNoMovement`                    | Fact   | No located transactions means no movement.                           |
| `ALegIsImpossibleOnlyWhenItIsFarEnoughAndTooFast`         | Fact   | A leg is impossible only when it is far enough and too fast.         |
| `OnlySensibleLimitsAreAccepted`                           | Theory | Only sensible limits are accepted.                                   |
| `TheLimitsDecideWhichLegsAreImpossible`                   | Fact   | The limits decide which legs are impossible.                         |
| `TheSameInstantAtTwoPlacesIsImpossibleWithNoSpeedToName`  | Fact   | The same instant at two places is impossible with no speed to name.  |
| `ALegUnderTheMinimumDistanceIsNeverImpossibleHoweverFast` | Fact   | A leg under the minimum distance is never impossible however fast.   |
| `EachLaneIsItsOwnTrailAndTheOrderOfArrivalDoesNotMatter`  | Fact   | Each lane is its own trail and the order of arrival does not matter. |
| `OnlyTheFiveWorstLegsAreKeptFastestFirst`                 | Fact   | Only the five worst legs are kept fastest first.                     |

### JournalLensReaderTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalLensReaderTests` — 17 tests.
Category: Unit.

| Test                                                                        | Kind   | Verifies                                                                                                 |
|-----------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------|
| `OnlyVisibleActivationsAreNamedAndAForcedOneSaysSo`                         | Fact   | Only visible activations are named and a forced one says so.                                             |
| `FeaturesGatherEveryFacetTheRulesSawWithTheirTypes`                         | Fact   | Features gather every facet the rules saw with their types.                                              |
| `TheGraphFacetFlattensEachEntityIntoDottedScalarsAndLeavesNestedObjectsOut` | Fact   | The graph facet flattens each entity into dotted scalar keys and leaves nested edge and tag objects out. |
| `ModelsAndSanctionsKeepTheirOwnFacets`                                      | Fact   | Models and sanctions keep their own facets.                                                              |
| `PayloadReadsOnlyTheXPathsItIsGivenAndLeavesObjectsOut`                     | Fact   | Payload reads only the x paths it is given and leaves objects out.                                       |
| `PayloadCarriesTheConfiguredRoleAndDrill`                                   | Fact   | Payload carries the configured role and drill.                                                           |
| `APayloadReadNoLongerCarriesFormatting`                                     | Fact   | A payload read no longer carries formatting.                                                             |
| `AMatchingPatternFormatsTheCellAndSaysWhichPatternDidIt`                    | Fact   | A matching pattern formats the cell and says which pattern did it.                                       |
| `APatternThatDoesNotMatchOrIsBrokenFormatsNothing`                          | Fact   | A pattern that does not match or is broken formats nothing.                                              |
| `TheBoldLineMatchesOnlyWhenTheRowSharesTheCaseValue`                        | Fact   | The bold line matches only when the row shares the case value.                                           |
| `TheBoldLineComparesNumbersAndBooleansAsText`                               | Fact   | The bold line compares numbers and booleans as text.                                                     |
| `TheReferenceIsTakenFromTheCaseOnlyForBoldLineXPaths`                       | Fact   | The reference is taken from the case only for bold line x paths.                                         |
| `PresenceSaysWhichLensesHaveAnythingToShow`                                 | Fact   | Presence says which lenses have anything to show.                                                        |
| `OnlyTheFiveLensesAreAccepted`                                              | Theory | Only the five lenses are accepted.                                                                       |
| `BaseDoesNotThrowWhenResponseElevationOrHoldIsNotAnObject`                  | Theory | The base read does not throw when response elevation or hold is not an object.                           |
| `BaseReadsResponseElevationNameAndHoldStateFromObjects`                     | Fact   | The base read takes the response elevation name and hold state from their objects.                       |
| `TheSanctionsLensListsTheMatchedEntriesOfEachCheck`                         | Fact   | The sanctions lens lists the matched entries of each check.                                              |

### CaseJournalServiceTests

`Jube.Test.Service.Query.CaseJournal.CaseJournalServiceTests` — 25 tests. Category: Service. Collection: Database.

| Test                                                                                 | Kind   | Verifies                                                                                        |
|--------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                           | Theory | Create with null or blank user name throws not authenticated.                                   |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                            | Fact   | Create with user having no tenant throws not authenticated.                                     |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                   | Fact   | Create with unknown user throws not authenticated.                                              |
| `GetThrowsForbiddenWhenPermissionMissingAsync`                                       | Fact   | Get throws forbidden when permission missing.                                                   |
| `ForbiddenDoesNotTouchArchiveDataAsync`                                              | Fact   | Forbidden does not touch archive data.                                                          |
| `GetReturnsSeededArchiveRowFieldByFieldAsync`                                        | Fact   | Get returns seeded archive row field by field.                                                  |
| `GetReturnsNewestFirstAndHonoursLimitAsync`                                          | Fact   | Get returns newest first and honours limit.                                                     |
| `GetReturnsEmptyRowsButFixedSchemaWhenNothingMatchesAsync`                           | Fact   | Get returns empty rows but fixed schema when nothing matches.                                   |
| `ActivationsOnlyAndResponseElevationFiltersApplyAsync`                               | Fact   | Activations only and response elevation filters apply.                                          |
| `DrillNameAndValueMustBothMatchAsync`                                                | Fact   | Drill name and value must both match.                                                           |
| `TenantBArchiveIsInvisibleToTenantAAndViceVersaAsync`                                | Fact   | Tenant b archive is invisible to tenant a and vice versa.                                       |
| `TenantBWorkflowXPathColumnsAreNotExposedToTenantAAsync`                             | Fact   | Tenant b workflow x path columns are not exposed to tenant a.                                   |
| `PermittedXPathBecomesTypedColumnAsync`                                              | Fact   | Permitted x path becomes typed column.                                                          |
| `XPathWithoutRoleGrantIsExcludedAsync`                                               | Fact   | X path without role grant is excluded.                                                          |
| `XPathOfWorkflowWithoutWorkflowRoleGrantIsExcludedAsync`                             | Fact   | X path of workflow without workflow role grant is excluded.                                     |
| `BoldLineAndCellFormatAreEmittedWhenXPathMatchesAsync`                               | Fact   | Bold line and cell format are emitted when x path matches.                                      |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                      | Fact   | Pre cancelled token throws operation canceled.                                                  |
| `PermissionDeniedLogsWarnNotErrorAsync`                                              | Fact   | Permission denied logs warn not error.                                                          |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                       | Fact   | Failure logs error with exception attached and still propagates.                                |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                            | Fact   | Each call emits one span with outcome tag.                                                      |
| `EachCallRecordsOneDurationMeasurementAsync`                                         | Fact   | Each call records one duration measurement.                                                     |
| `AuditLogGetsExactlyOneLineAsync`                                                    | Fact   | Audit log gets exactly one line.                                                                |
| `ReadsAndFailuresPublishNothingAsync`                                                | Fact   | Reads and failures publish nothing.                                                             |
| `CatalogueDescriptorHasUniquePascalCaseNoUnderscoreNameAndOperationAttributeMatches` | Fact   | Catalogue descriptor has unique pascal case no underscore name and operation attribute matches. |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                               | Fact   | Permission denied message resolves for french culture.                                          |

## Case note by case key value

### CaseNoteByCaseKeyValueServiceTests

`Jube.Test.Service.Query.CaseNoteByCaseKeyValue.CaseNoteByCaseKeyValueServiceTests` — 24 tests. Category: Service.
Collection: Database.

| Test                                                                      | Kind   | Verifies                                                                             |
|---------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                | Theory | Create with null or blank user name throws not authenticated.                        |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                        | Fact   | Create with unknown user throws not authenticated.                                   |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                 | Fact   | Create with user having no tenant throws not authenticated.                          |
| `GetWithoutPermissionThrowsForbiddenWithCodeAndSpecsAsync`                | Fact   | Get without permission throws forbidden with code and specs.                         |
| `ForbiddenLogsWarnNotErrorAsync`                                          | Fact   | Forbidden logs warn not error.                                                       |
| `GetReturnsSeededNotesMappedFieldByFieldNewestFirstAsync`                 | Fact   | Get returns seeded notes mapped field by field newest first.                         |
| `NotesStoredBeforeTheSanitiserFixAreSanitisedOnReadAsync`                 | Theory | Notes stored before the sanitiser fix are sanitised on read.                         |
| `PlainNotesAreReturnedUnchangedAsync`                                     | Fact   | Plain notes are returned unchanged.                                                  |
| `PriorityIdIsConvertedToTextAsync`                                        | Theory | Priority id is converted to text.                                                    |
| `GetReturnsEmptyWhenNoCaseMatchesKeyValueAsync`                           | Fact   | Get returns empty when no case matches key value.                                    |
| `GetOnlyReturnsNotesForTheMatchingCaseAsync`                              | Fact   | Get only returns notes for the matching case.                                        |
| `TenantBNotesAreInvisibleToTenantAAndViceVersaAsync`                      | Fact   | Tenant b notes are invisible to tenant a and vice versa.                             |
| `TenantAUserCannotSeeTenantBNotesEvenWhenRoleGrantIsForATenantAUserAsync` | Fact   | Tenant a user cannot see tenant b notes even when role grant is for a tenant a user. |
| `UserWithoutTheGrantedRoleSeesNothingAsync`                               | Fact   | User without the granted role sees nothing.                                          |
| `MissingRoleGrantOnAnyOfWorkflowStatusOrActionHidesNotesAsync`            | Theory | Missing role grant on any of workflow status or action hides notes.                  |
| `DeletedWorkflowRoleGrantHidesNotesAsync`                                 | Fact   | Deleted workflow role grant hides notes.                                             |
| `DeletedWorkflowHidesNotesAsync`                                          | Fact   | Deleted workflow hides notes.                                                        |
| `DeletedStatusHidesNotesAsync`                                            | Fact   | Deleted status hides notes.                                                          |
| `PreCancelledTokenThrowsOperationCanceledAndAuditsCancelledAsync`         | Fact   | Pre cancelled token throws operation canceled and audits cancelled.                  |
| `FailureLogsErrorWithExceptionAndPropagatesAsync`                         | Fact   | Failure logs error with exception and propagates.                                    |
| `ExactlyOneAuditRecordPerCallAsync`                                       | Fact   | Exactly one audit record per call.                                                   |
| `ForbiddenCallIsAuditedOnceWithForbiddenOutcomeAsync`                     | Fact   | Forbidden call is audited once with forbidden outcome.                               |
| `ReadsPublishNoChangeEventAsync`                                          | Fact   | Reads publish no change event.                                                       |
| `CatalogueRegistersUniqueToolName`                                        | Fact   | Catalogue registers unique tool name.                                                |

## Case workflow display execution

### CaseWorkflowDisplayExecutionServiceTests

`Jube.Test.Service.Query.CaseWorkflowDisplayExecution.CaseWorkflowDisplayExecutionServiceTests` — 42 tests. Category:
Service. Collection: Database.

| Test                                                                                   | Kind   | Verifies                                                                                           |
|----------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                             | Theory | Create with null or blank user name throws not authenticated.                                      |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                              | Fact   | Create with user having no tenant throws not authenticated.                                        |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                     | Fact   | Create with unknown user throws not authenticated.                                                 |
| `ExecuteThrowsForbiddenWhenPermissionMissingAsync`                                     | Fact   | Execute throws forbidden when permission missing.                                                  |
| `ExecuteReplacesPayloadTokensAndRendersUnknownOnesEmptyAsync`                          | Fact   | Execute replaces payload tokens and renders unknown ones empty.                                    |
| `ExecuteReadsTheCaseRootAndRendersAFieldReadOnlyAsync`                                 | Fact   | Execute reads the case root and renders a field read only.                                         |
| `ExecuteWithATemplateThatDoesNotCompileThrowsTemplateInvalidAsync`                     | Fact   | Execute with a template that does not compile throws template invalid.                             |
| `RenderFormFillsTheValuesAndNamesTheFieldsShownAsync`                                  | Fact   | Render form fills the values and names the fields shown.                                           |
| `RenderFormForAFormNotGrantedToCallersRoleThrowsNotFoundAsync`                         | Fact   | Render form for a form not granted to callers role throws not found.                               |
| `RenderFormWithoutPermissionThrowsForbiddenAsync`                                      | Fact   | Render form without permission throws forbidden.                                                   |
| `RenderFormEntriesDrawsEachEntryReadOnlyWithItsValuesAndTheValuesItHasNoFieldForAsync` | Fact   | Render form entries draws each entry read only with its values and the values it has no field for. |
| `RenderFormEntriesListsTheNewestEntryFirstAsync`                                       | Fact   | Render form entries lists the newest entry first.                                                  |
| `RenderFormEntriesReportsATemplateThatNoLongerCompilesAndStillListsTheValuesAsync`     | Fact   | Render form entries reports a template that no longer compiles and still lists the values.         |
| `RenderFormEntriesSkipsAnEntryOfAFormTheCallersRoleDoesNotHoldAsync`                   | Fact   | Render form entries skips an entry of a form the callers role does not hold.                       |
| `RenderFormEntriesWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Render form entries without permission throws forbidden.                                           |
| `ExecuteDrawsADisplayFromTheRowsOfItsSqlAsync`                                         | Fact   | Execute draws a display from the rows of its sql.                                                  |
| `ExecuteRefusesTheEarlierRootsInADisplayThatHasSqlAsync`                               | Fact   | Execute refuses the earlier roots in a display that has sql.                                       |
| `ExecuteReportsADisplaySqlThatFailsAsTemplateInvalidAsync`                             | Fact   | Execute reports a display sql that fails as template invalid.                                      |
| `RenderFormDrawsAFormFromTheRowsOfItsSqlAsync`                                         | Fact   | Render form draws a form from the rows of its sql.                                                 |
| `RenderFormEntriesDrawsAnEntryFromItsSnapshotWhateverTheFormBecomesAsync`              | Fact   | Render form entries draws an entry from its snapshot whatever the form becomes.                    |
| `ListDispatchesReturnsTheCaseKeysCallsNewestFirstAsync`                                | Fact   | List dispatches returns the case keys calls newest first.                                          |
| `TestSqlRunsForTheCaseAndReturnsTheColumnsAndRowsAsync`                                | Fact   | Test sql runs for the case and returns the columns and rows.                                       |
| `TestSqlRefusesBlockedTablesUnknownParametersAndWritesWithoutRaisingAsync`             | Fact   | Test sql refuses blocked tables unknown parameters and writes without raising.                     |
| `TestSqlNeedsTheSqlAuthoringPermissionAsync`                                           | Fact   | Test sql needs the sql authoring permission.                                                       |
| `TestSqlForACaseOutsideTheCallersTenantThrowsNotFoundAsync`                            | Fact   | Test sql for a case outside the callers tenant throws not found.                                   |
| `ExecuteUnknownCaseThrowsNotFoundAndLogsWarnNotErrorAsync`                             | Fact   | Execute unknown case throws not found and logs warn not error.                                     |
| `ExecuteUnknownDisplayThrowsNotFoundAsync`                                             | Fact   | Execute unknown display throws not found.                                                          |
| `ExecuteWithSoftDeletedStatusThrowsNotFoundAsync`                                      | Fact   | Execute with soft deleted status throws not found.                                                 |
| `TenantBCannotRenderTenantACaseOrDisplayAndViceVersaAsync`                             | Fact   | Tenant b cannot render tenant a case or display and vice versa.                                    |
| `CaseWhoseWorkflowIsNotGrantedToCallersRoleIsNotFoundAsync`                            | Fact   | Case whose workflow is not granted to callers role is not found.                                   |
| `CaseWhoseStatusIsNotGrantedToCallersRoleIsNotFoundAsync`                              | Fact   | Case whose status is not granted to callers role is not found.                                     |
| `DisplayNotGrantedToCallersRoleIsNotFoundAsync`                                        | Fact   | Display not granted to callers role is not found.                                                  |
| `InactiveOrDeletedDisplayIsNotFoundAsync`                                              | Theory | Inactive or deleted display is not found.                                                          |
| `CaseInDeletedWorkflowIsNotFoundAsync`                                                 | Fact   | Case in deleted workflow is not found.                                                             |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                        | Fact   | Pre cancelled token throws operation canceled.                                                     |
| `PermissionDeniedLogsWarnNotErrorAsync`                                                | Fact   | Permission denied logs warn not error.                                                             |
| `AuditLogGetsExactlyOneLineAsync`                                                      | Fact   | Audit log gets exactly one line.                                                                   |
| `ReadsAndFailuresPublishNothingAsync`                                                  | Fact   | Reads and failures publish nothing.                                                                |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                  | Fact   | Catalogue registers unique pascal case no underscore names.                                        |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                                 | Fact   | Permission denied message resolves for french culture.                                             |
| `DisplayOfAnotherWorkflowIsNotFoundForTheCaseEvenWhenTheCallerHoldsEveryRoleAsync`     | Fact   | A display of another workflow is not found for the case even when the caller holds every role.     |

## Case workflow form entry by case key value

| `TheSeededExampleDisplayRendersOnceTheDialectMigrationHasReplacedItsInlineStyleAsync` | Fact | The seeded example
display renders once the dialect migration has replaced its inline style. |

### CaseWorkflowFormEntryByCaseKeyValueServiceTests

`Jube.Test.Service.Query.CaseWorkflowFormEntryByCaseKeyValue.CaseWorkflowFormEntryByCaseKeyValueServiceTests` — 23
tests. Category: Service. Collection: Database.

| Test                                                                                     | Kind   | Verifies                                                                                              |
|------------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                               | Theory | Create with null or blank user name throws not authenticated.                                         |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                                | Fact   | Create with user having no tenant throws not authenticated.                                           |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                       | Fact   | Create with unknown user throws not authenticated.                                                    |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                                      | Fact   | Get async throws forbidden when permission missing.                                                   |
| `ForbiddenUserNeverReachesTheQueryEvenWhenMatchingDataExistsAsync`                       | Fact   | Forbidden user never reaches the query even when matching data exists.                                |
| `GetAsyncReturnsExactlyMappedRowForMatchingCaseKeyValueAsync`                            | Fact   | Get async returns exactly mapped row for matching case key value.                                     |
| `GetAsyncReturnsEmptyListWhenNothingMatchesAsync`                                        | Fact   | Get async returns empty list when nothing matches.                                                    |
| `GetAsyncDoesNotReturnRowsForADifferentKeyOrValueAsync`                                  | Fact   | Get async does not return rows for a different key or value.                                          |
| `GetAsyncOrdersByCaseIdDescendingAsync`                                                  | Fact   | Get async orders by case id descending.                                                               |
| `TenantBDataIsInvisibleToTenantAAsync`                                                   | Fact   | Tenant b data is invisible to tenant a.                                                               |
| `TenantBDataIsInvisibleToTenantAEvenWhenTenantAUsersRoleIsGrantedOnTenantBWorkflowAsync` | Fact   | Tenant b data is invisible to tenant a even when tenant a users role is granted on tenant b workflow. |
| `TenantADataIsInvisibleToTenantBAsync`                                                   | Fact   | Tenant a data is invisible to tenant b.                                                               |
| `EachTenantSeesOnlyItsOwnRowsForTheSameKeyAndValueAsync`                                 | Fact   | Each tenant sees only its own rows for the same key and value.                                        |
| `RowIsHiddenWhenCallersRoleHoldsNoCaseWorkflowRoleAsync`                                 | Fact   | Row is hidden when callers role holds no case workflow role.                                          |
| `RowIsHiddenWhenCallersRoleHoldsNoCaseWorkflowStatusRoleAsync`                           | Fact   | Row is hidden when callers role holds no case workflow status role.                                   |
| `RowIsHiddenWhenWorkflowRoleBelongsToAnotherRoleInTheSameTenantAsync`                    | Fact   | Row is hidden when workflow role belongs to another role in the same tenant.                          |
| `RowIsHiddenWhenWorkflowStatusOrModelIsSoftDeletedAsync`                                 | Theory | Row is hidden when workflow status or model is soft deleted.                                          |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                          | Fact   | Pre cancelled token throws operation canceled.                                                        |
| `PermissionDeniedLogsWarnNotErrorAsync`                                                  | Fact   | Permission denied logs warn not error.                                                                |
| `AuditLogGetsExactlyOneLineAsync`                                                        | Fact   | Audit log gets exactly one line.                                                                      |
| `ReadsAndFailuresPublishNothingAsync`                                                    | Fact   | Reads and failures publish nothing.                                                                   |
| `CatalogueRegistersUniqueToolName`                                                       | Fact   | Catalogue registers unique tool name.                                                                 |
| `GetAsyncHidesAnEntryWhoseFormTheCallersRolesHoldNoGrantOnAsync`                         | Fact   | Get hides an entry whose form the callers roles hold no grant on.                                     |

## Case workflow macro execution

### CaseWorkflowMacroExecutionServiceTests

`Jube.Test.Service.Query.CaseWorkflowMacroExecution.CaseWorkflowMacroExecutionServiceTests` — 24 tests. Category:
Service. Collection: Database.

| Test                                                                           | Kind   | Verifies                                                                               |
|--------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                     | Theory | Create with null or blank user name throws not authenticated.                          |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                      | Fact   | Create with user having no tenant throws not authenticated.                            |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                             | Fact   | Create with unknown user throws not authenticated.                                     |
| `ExecuteWithoutPermissionThrowsForbiddenAndCallsNothingAsync`                  | Fact   | Execute without permission throws forbidden and calls nothing.                         |
| `ExecutePostsTheCaseStatusAndPayloadToTheHttpEndpointAndEchoesTheRequestAsync` | Fact   | Execute posts the case status and payload to the http endpoint and echoes the request. |
| `ExecuteWithGetEndpointTypeIssuesAGetWithNoBodyAsync`                          | Fact   | Execute with get endpoint type issues a get with no body.                              |
| `ExecuteWithNothingEnabledSucceedsAndCallsNothingAsync`                        | Fact   | Execute with nothing enabled succeeds and calls nothing.                               |
| `ExecuteDoesNotModifyTheCaseMacroOrStatusAsync`                                | Fact   | Execute does not modify the case macro or status.                                      |
| `ExecuteUnknownCaseOrMacroThrowsNotFoundAsync`                                 | Fact   | Execute unknown case or macro throws not found.                                        |
| `ExecuteInactiveMacroThrowsNotFoundAsync`                                      | Fact   | Execute inactive macro throws not found.                                               |
| `ExecuteAcrossTenantsBehavesAsNotFoundInBothDirectionsAsync`                   | Fact   | Execute across tenants behaves as not found in both directions.                        |
| `ExecuteRequiresTheCallersRoleOnTheWorkflowStatusAndMacroAsync`                | Fact   | Execute requires the callers role on the workflow status and macro.                    |
| `ExecuteEndpointFailureIsSwallowedAsLegacyAsync`                               | Fact   | Execute endpoint failure is swallowed as legacy.                                       |
| `ExecuteInSqlModePostsTheRowsAndAuditsTheCallAsync`                            | Fact   | Execute in sql mode posts the rows and audits the call.                                |
| `ExecuteInSqlModeRefusesAnEndpointWhoseHostComesFromTheRowsAsync`              | Fact   | Execute in sql mode refuses an endpoint whose host comes from the rows.                |
| `ExecuteInSqlModeRefusesEveryChannelWhenTheSqlFailsAsync`                      | Fact   | Execute in sql mode refuses every channel when the sql fails.                          |
| `ExecuteInSqlModeRefusesAnEmailDestinationThatIsNotAnAddressAsync`             | Fact   | Execute in sql mode refuses an email destination that is not an address.               |
| `ExecuteInPayloadModeAuditsTheEarlierCallAsync`                                | Fact   | Execute in payload mode audits the earlier call.                                       |
| `ExecuteAuditsAFailedCallWithoutRaisingAsync`                                  | Fact   | Execute audits a failed call without raising.                                          |
| `PreCancelledTokenThrowsOperationCanceledAndCallsNothingAsync`                 | Fact   | Pre cancelled token throws operation canceled and calls nothing.                       |
| `SuccessEmitsOneAuditLineOneSpanAndNoChangeEventAsync`                         | Fact   | Success emits one audit line one span and no change event.                             |
| `ForbiddenAndNotFoundPublishNothingAndAuditTheirOutcomeAsync`                  | Fact   | Forbidden and not found publish nothing and audit their outcome.                       |
| `ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue`                   | Fact   | Tool is declared on the service method and published by the catalogue.                 |
| `ExecuteMacroOfAnotherWorkflowThrowsNotFoundAndCallsNothingAsync`              | Fact   | A macro of another workflow throws not found and calls nothing.                        |

### CaseHoldServiceTests

`Jube.Test.Service.Query.CaseHold.CaseHoldServiceTests` — 25 tests. Category: Service. Collection: Database.

| Test                                                                                         | Kind   | Verifies                                                                                               |
|----------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                                   | Theory | Create with null or blank user name throws not authenticated.                                          |
| `CreateWithAUserWithoutATenantThrowsNotAuthenticatedAsync`                                   | Fact   | Create with a user without a tenant throws not authenticated.                                          |
| `ListGivesTheHoldsOfTheCaseWithTheRangeTheDeadlineAndTheMessagesAsync`                       | Fact   | List gives the holds of the case with the range the deadline and the messages.                         |
| `ListIncludesOtherTransactionsWithTheSameKeyAndLeavesOutOtherKeysAsync`                      | Fact   | List includes other transactions with the same key and leaves out other keys.                          |
| `ListWithoutThePermissionOrForACaseOfAnotherTenantIsRefusedAsync`                            | Fact   | List without the permission or for a case of another tenant is refused.                                |
| `ReleaseCallsTheHookWithTheCredentialAndOnA200RecordsReleasedWithItsDecisionAndVersionAsync` | Fact   | Release calls the hook with the credential and on A200 records released with its decision and version. |
| `AnAnswerOtherThanExactly200LeavesThePaymentHeldAndRecordsTheFailedAttemptAsync`             | Theory | An answer other than exactly200 leaves the payment held and records the failed attempt.                |
| `ASecondReleaseOfTheSamePaymentIsRefusedAndTheHookIsNotCalledAgainAsync`                     | Fact   | A second release of the same payment is refused and the hook is not called again.                      |
| `TwoReleasesAtOnceCallTheHookExactlyOnceAsync`                                               | Fact   | Two releases at once call the hook exactly once.                                                       |
| `ReleaseWithoutThePermissionIsRefusedAndNothingIsCalledAsync`                                | Fact   | Release without the permission is refused and nothing is called.                                       |
| `ReleaseWithNoHandlingHookIsRefusedAndNothingChangesAsync`                                   | Fact   | Release with no handling hook is refused and nothing changes.                                          |
| `TheHookWhoseSourceFieldMatchesThePayloadHandlesItAndAnInactiveHookDoesNotAsync`             | Fact   | The hook whose source field matches the payload handles it and an inactive hook does not.              |
| `AHookWhoseAddressIsNotLiteralIsRefusedAndRecordedWithoutCallingAnythingAsync`               | Fact   | A hook whose address is not literal is refused and recorded without calling anything.                  |
| `AHookWhoseCredentialVariableIsNotSetIsNotCalledAsync`                                       | Fact   | A hook whose credential variable is not set is not called.                                             |
| `ACredentialVariableWithoutTheHookPrefixIsNeverReadAsync`                                    | Fact   | A credential variable without the hook prefix is never read.                                           |
| `RefuseCallsTheRefuseUrlWhenTheHookHasOneAndRecordsRefusedAsync`                             | Fact   | Refuse calls the refuse url when the hook has one and records refused.                                 |
| `RefuseWithoutARefuseUrlOrAnyHookIsRecordedWithoutCallingAnythingAsync`                      | Fact   | Refuse without a refuse url or any hook is recorded without calling anything.                          |
| `ADeclinedPaymentCanNeverBeReleasedOrRefusedAsync`                                           | Fact   | A declined payment can never be released or refused.                                                   |
| `ADecisionWithoutAReasonIsInvalidAndNothingIsCalledAsync`                                    | Theory | A decision without a reason is invalid and nothing is called.                                          |
| `ACaseLockedByAnotherUserCannotHaveItsPaymentReleasedAsync`                                  | Fact   | A case locked by another user cannot have its payment released.                                        |
| `ATransactionThatIsNotOnTheCaseCannotBeReleasedThroughItAsync`                               | Fact   | A transaction that is not on the case cannot be released through it.                                   |
| `ACustomBodyIsRenderedWithEveryValueEscapedForJsonAsync`                                     | Fact   | A custom body is rendered with every value escaped for json.                                           |
| `AGetHookCallsTheUrlWithoutABodyAsync`                                                       | Fact   | A get hook calls the url without a body.                                                               |
| `TheToolCatalogueDeclaresTheThreeOperationsWithTheirPermissions`                             | Fact   | The tool catalogue declares the three operations with their permissions.                               |
| `ACaseLockedByAUserWhoseNameDiffersOnlyInCaseIsStillLockedAgainstTheCallerAsync`             | Fact   | A case locked by a user whose name differs only in case is still locked against the caller.            |

## Completions

### CompletionSqlPathEscapingTests

`Jube.Test.Service.Query.Completions.CompletionSqlPathEscapingTests` — 6 tests. Category: Unit.

| Test                                                      | Kind   | Verifies                                                            |
|-----------------------------------------------------------|--------|---------------------------------------------------------------------|
| `SqlLiteralDoublesSingleQuotes`                           | Theory | Sql literal doubles single quotes.                                  |
| `SqlLiteralPassesNullThrough`                             | Fact   | Sql literal passes null through.                                    |
| `SqlJsonStringLiteralRoundTripsToTheOriginalName`         | Theory | Sql json string literal round trips to the original name.           |
| `AnUnescapedHostileNameBreaksOutOfTheLiteral`             | Fact   | An unescaped hostile name breaks out of the literal.                |
| `AnEscapedHostileNameStaysOneSelectOverOneLiteral`        | Fact   | An escaped hostile name stays one select over one literal.          |
| `AnEscapedHostileTagNameStaysOneSelectOverOneJsonLiteral` | Fact   | An escaped hostile tag name stays one select over one json literal. |

### CompletionsServiceTests

`Jube.Test.Service.Query.Completions.CompletionsServiceTests` — 23 tests. Category: Service. Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                   |
|-------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                          | Theory | Create with null or blank user name throws not authenticated.                              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                           | Fact   | Create with user having no tenant throws not authenticated.                                |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                  | Fact   | Create with unknown user throws not authenticated.                                         |
| `EveryOperationThrowsForbiddenWithItsOwnSpecsWhenPermissionMissingAsync`            | Fact   | Every operation throws forbidden with its own specs when permission missing.               |
| `GetByExhaustiveInstanceDatasetReturnsCompletionsForEveryColumnAsync`               | Fact   | Get by exhaustive instance dataset returns completions for every column.                   |
| `GetByExhaustiveInstanceDatasetThrowsNotFoundForUnconfirmedOrForeignOrMissingAsync` | Fact   | Get by exhaustive instance dataset throws not found for unconfirmed or foreign or missing. |
| `GetByCaseWorkflowIdReturnsMappedCompletionsAsync`                                  | Fact   | Get by case workflow id returns mapped completions.                                        |
| `GetByCaseWorkflowGuidIncludingDeletedReturnsMappedCompletionsAsync`                | Fact   | Get by case workflow guid including deleted returns mapped completions.                    |
| `GetByCaseWorkflowIdIncludingDeletedReturnsMappedCompletionsAsync`                  | Fact   | Get by case workflow id including deleted returns mapped completions.                      |
| `GetByEntityAnalysisModelIdReturnsMappedCompletionsAsync`                           | Fact   | Get by entity analysis model id returns mapped completions.                                |
| `GetByEntityAnalysisModelIdParseTypeIdReturnsMappedCompletionsAsync`                | Fact   | Get by entity analysis model id parse type id returns mapped completions.                  |
| `ParseTypeIdZeroOrLessReturnsEmptyAsync`                                            | Fact   | Parse type id zero or less returns empty.                                                  |
| `ModelWithNoXPathsReturnsEmptyListAsync`                                            | Fact   | Model with no x paths returns empty list.                                                  |
| `UnknownCaseWorkflowThrowsCaseWorkflowNotFoundOnEveryWorkflowRouteAsync`            | Fact   | Unknown case workflow throws case workflow not found on every workflow route.              |
| `DeletedWorkflowIsHiddenByIdButVisibleIncludingDeletedAsync`                        | Fact   | Deleted workflow is hidden by id but visible including deleted.                            |
| `TenantBCannotSeeTenantACompletionsAndViceVersaAsync`                               | Fact   | Tenant b cannot see tenant a completions and vice versa.                                   |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                     | Fact   | Pre cancelled token throws operation canceled.                                             |
| `PermissionDeniedLogsWarnNotErrorAsync`                                             | Fact   | Permission denied logs warn not error.                                                     |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                      | Fact   | Failure logs error with exception attached and still propagates.                           |
| `AuditLogGetsExactlyOneLinePerCallAsync`                                            | Fact   | Audit log gets exactly one line per call.                                                  |
| `ReadsAndFailuresPublishNothingAsync`                                               | Fact   | Reads and failures publish nothing.                                                        |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                               | Fact   | Catalogue registers unique pascal case no underscore names.                                |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                              | Fact   | Permission denied message resolves for french culture.                                     |

## Entity analysis model activation rule override query

### EntityAnalysisModelActivationRuleOverrideQueryServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelActivationRuleOverrideQuery.EntityAnalysisModelActivationRuleOverrideQueryServiceTests` —
22 tests. Category: Service. Collection: Database.

| Test                                                       | Kind   | Verifies                                                      |
|------------------------------------------------------------|--------|---------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync` | Theory | Create with null or blank user name throws not authenticated. |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`  | Fact   | Create with user having no tenant throws not authenticated.   |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`         | Fact   | Create with unknown user throws not authenticated.            |
| `GetThrowsForbiddenWhenPermissionMissingAsync`             | Fact   | Get throws forbidden when permission missing.                 |
| `GetReturnsSuppressedRuleWithExactFieldMappingAsync`       | Fact   | Get returns suppressed rule with exact field mapping.         |
| `GetReturnsRuleUnsuppressedWhenNoOverrideRowExistsAsync`   | Fact   | Get returns rule unsuppressed when no override row exists.    |
| `GetOverrideWithNoExpiryIsActiveWithNullExpiryAsync`       | Fact   | Get override with no expiry is active with null expiry.       |
| `GetIgnoresExpiredOverrideAsync`                           | Fact   | Get ignores expired override.                                 |
| `GetIgnoresDeletedOverrideAsync`                           | Fact   | Get ignores deleted override.                                 |
| `GetIgnoresOverrideForADifferentKeyValueAsync`             | Fact   | Get ignores override for a different key value.               |
| `GetReturnsEmptyForUnknownModelGuidAsync`                  | Fact   | Get returns empty for unknown model guid.                     |
| `GetReturnsEmptyForNullKeyAndValueAsync`                   | Fact   | Get returns empty for null key and value.                     |
| `GetReturnsEmptyWhenKeyIsNotAnOverrideEnabledXpathAsync`   | Fact   | Get returns empty when key is not an override enabled xpath.  |
| `GetReturnsEmptyWhenRuleHasOverrideDisabledAsync`          | Fact   | Get returns empty when rule has override disabled.            |
| `TenantBDataIsInvisibleToTenantAAsync`                     | Fact   | Tenant b data is invisible to tenant a.                       |
| `TenantADataIsInvisibleToTenantBAsync`                     | Fact   | Tenant a data is invisible to tenant b.                       |
| `EachTenantSeesOnlyItsOwnModelWhenBothExistAsync`          | Fact   | Each tenant sees only its own model when both exist.          |
| `PreCancelledTokenThrowsOperationCanceledAsync`            | Fact   | Pre cancelled token throws operation canceled.                |
| `PermissionDeniedLogsWarnNotErrorAsync`                    | Fact   | Permission denied logs warn not error.                        |
| `AuditLogGetsExactlyOneLineAsync`                          | Fact   | Audit log gets exactly one line.                              |
| `ReadsAndFailuresPublishNothingAsync`                      | Fact   | Reads and failures publish nothing.                           |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`      | Fact   | Catalogue registers unique pascal case no underscore names.   |

## Entity analysis model backtest

### EntityAnalysisModelBacktestServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelBacktest.EntityAnalysisModelBacktestServiceTests` — 21 tests. Category:
Service. Collection: Database.

| Test                                                                         | Kind    | Verifies                                                                                               |
|------------------------------------------------------------------------------|---------|--------------------------------------------------------------------------------------------------------|
| `AnActivationRuleIsCountedAgainstATagClassOfArchivedTransactionsAsync`       | Fact    | An activation rule is counted against a tag class of archived transactions.                            |
| `ADeletedTagNoLongerCountsAsync`                                             | Fact    | A deleted tag no longer counts.                                                                        |
| `TheFilterBuilderChoosesTheTransactionsAsync`                                | Fact    | The filter builder chooses the transactions.                                                           |
| `TheRangeAndLimitChooseTheNewestTransactionsInRangeAsync`                    | Fact    | The range and limit choose the newest transactions in range.                                           |
| `WithoutAClassOnlyFiringIsCountedAsync`                                      | Fact    | Without a class only firing is counted.                                                                |
| `AReprocessingRuleRunsAsAGatewayRuleInReprocessingModeAsync`                 | Fact    | A reprocessing rule runs as a gateway rule in reprocessing mode.                                       |
| `AnInvalidRequestComesBackAsErrorsWithTheirPartAsync`                        | Fact    | An invalid request comes back as errors with their part.                                               |
| `TheMessagesOfAnInvalidRequestComeFromTheResourcesInTheCallersLanguageAsync` | Fact    | The messages of an invalid request come from the resources in the callers language.                    |
| `AModelWithTheMostUncompletedBacktestsRefusesAnotherUntilOneIsStoppedAsync`  | Fact    | A model with the most uncompleted backtests refuses another until one is stopped.                      |
| `TheRefusalOfAnotherBacktestIsWordedByTheResourcesInTheCallersLanguageAsync` | Fact    | The refusal of another backtest is worded by the resources in the callers language.                    |
| `TheFieldsIncludeTheModelsTagsForTheClassOnlyAsync`                          | Fact    | The fields include the models tags for the class only.                                                 |
| `ASubmittedBacktestIsListedAndCanBeStoppedBeforeItStartsAsync`               | Fact    | A submitted backtest is listed and can be stopped before it starts.                                    |
| `ABacktestThreadRunsASubmittedBacktestToTheSameCountsAsync`                  | Fact    | A backtest thread runs a submitted backtest to the same counts.                                        |
| `WithAReportConnectionTheArchiveIsReadThroughItAndTheCountsAreTheSameAsync`  | Fact    | With a report connection the archive is read through it and the counts are the same.                   |
| `TheArchiveIsReadOnlyThroughTheReportConnectionWhenOneIsSetAsync`            | Fact    | The archive is read only through the report connection when one is set.                                |
| `ABacktestThreadReadsTheArchiveThroughTheReportConnectionAsync`              | Fact    | A backtest thread reads the archive through the report connection.                                     |
| `AnExplanationSaysWhyATransactionWasCountedAsync`                            | Fact    | An explanation says why a transaction was counted.                                                     |
| `AnotherTenantsModelIsNotFoundAndNoPermissionIsForbiddenAsync`               | Fact    | Another tenants model is not found and no permission is forbidden.                                     |
| `EveryOperationIsCatalogued`                                                 | Fact    | Every operation is catalogued.                                                                         |
| `ATuningSweepReturnsOneConfusionMatrixPerCandidateThresholdAsync`            | Service | Each distinct candidate threshold gets its own confusion matrix with precision and recall.             |
| `ATuningSweepRefusesAMissingPlaceholderBadCandidatesAndTooManyAsync`         | Service | A missing placeholder, a non-numeric candidate, no candidates and more than 12 are refused with codes. |

## Entity analysis model dependency

### EntityAnalysisModelDependencyServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelDependency.EntityAnalysisModelDependencyServiceTests` — 6 tests. Category:
Service. Collection: Database.

| Test                                                            | Kind | Verifies                                                           |
|-----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `AFieldsDependentsIncludeWhatUsesItThroughAnotherEntityAsync`   | Fact | A fields dependents include what uses it through another entity.   |
| `AnEntityNothingUsesHasNoDependentsAndCanBeDeletedAsync`        | Fact | An entity nothing uses has no dependents and can be deleted.       |
| `AnActivationRulesDependenciesResolveToTheEntitiesItNamesAsync` | Fact | An activation rules dependencies resolve to the entities it names. |
| `IssuesFindDanglingNamesAndUnusedEntitiesAsync`                 | Fact | Issues find dangling names and unused entities.                    |
| `AnUnknownKindOrIdComesBackAsAnErrorAsync`                      | Fact | An unknown kind or id comes back as an error.                      |
| `AnotherTenantsModelIsNotFoundAndNoPermissionIsForbiddenAsync`  | Fact | Another tenants model is not found and no permission is forbidden. |

## Entity analysis model history simulation

### EntityAnalysisModelHistorySimulationServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelHistorySimulation.EntityAnalysisModelHistorySimulationServiceTests` — 5
tests. Category: Service. Collection: Database.

| Test                                                       | Kind   | Verifies                                                         |
|------------------------------------------------------------|--------|------------------------------------------------------------------|
| `TheAggregationOverCachedHistoryMatchesTheEngineAsync`     | Theory | The aggregation over cached history matches the engine.          |
| `WithoutACacheTheSimulationSaysWhyAsync`                   | Fact   | Without a cache the simulation says why.                         |
| `ARuleThatDoesNotSearchAndAnUnsetSearchKeyAreRefusedAsync` | Fact   | A rule that does not search and an unset search key are refused. |
| `ATtlCounterIsReadForTheContextsDataValueAsync`            | Fact   | A ttl counter is read for the contexts data value.               |
| `TheCatalogueListsTheHistoryOperations`                    | Fact   | The catalogue lists the history operations.                      |

## Entity analysis model integrity

### EntityAnalysisModelIntegrityServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelIntegrity.EntityAnalysisModelIntegrityServiceTests` — 10 tests. Category:
Service. Collection: Database.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `TheCheckFindsTheModelsProblemsErrorsFirstAsync`               | Fact | The check finds the models problems errors first.                  |
| `ARecordedSnapshotIsComparedWithTheActiveEntitiesAsync`        | Fact | A recorded snapshot is compared with the active entities.          |
| `UpsertingASnapshotReplacesTheInstancesPreviousOneAsync`       | Fact | Upserting a snapshot replaces the instances previous one.          |
| `AnInProcessEngineIsPreferredToItsOwnSnapshotAsync`            | Fact | An in process engine is preferred to its own snapshot.             |
| `TheGraphCanBeCentredOnAnEntityAsync`                          | Fact | The graph can be centred on an entity.                             |
| `ModelsListTheTenantsModelsAsync`                              | Fact | Models list the tenants models.                                    |
| `AnotherTenantsModelIsNotFoundAndNoPermissionIsForbiddenAsync` | Fact | Another tenants model is not found and no permission is forbidden. |
| `TitlesAndMessagesAreLocalisedWithTheirArgumentsAsync`         | Fact | Titles and messages are localised with their arguments.            |
| `ASoftDeletedModelIsNotFoundByEveryOperationAsync`             | Fact | A soft deleted model is not found by every operation.              |
| `EveryCodeHasADescription`                                     | Fact | Every code has a description.                                      |

## Entity analysis model invocation context

### EntityAnalysisModelInvocationContextServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelInvocationContext.EntityAnalysisModelInvocationContextServiceTests` — 8
tests. Category: Service. Collection: Database.

| Test                                                            | Kind | Verifies                                                            |
|-----------------------------------------------------------------|------|---------------------------------------------------------------------|
| `ABlankContextListsEveryRuleNameWithOnlyDefaultsAsync`          | Fact | A blank context lists every rule name with only defaults.           |
| `RequestJsonIsExtractedWithTheModelsEntryAndReferenceDateAsync` | Fact | Request json is extracted with the models entry and reference date. |
| `UnreadableRequestJsonIsRefusedAsync`                           | Fact | Unreadable request json is refused.                                 |
| `AnArchivedTransactionIsReadBackWithItsValuesAsync`             | Fact | An archived transaction is read back with its values.               |
| `AnotherTenantsArchiveOrAnotherModelsArchiveIsNotFoundAsync`    | Fact | Another tenants archive or another models archive is not found.     |
| `AnOverlayRoundTripsTheContextAndReportsBadNamesAsync`          | Fact | An overlay round trips the context and reports bad names.           |
| `PermissionsFollowTheirPurposeAsync`                            | Fact | Permissions follow their purpose.                                   |
| `TheCatalogueListsTheFourContextOperations`                     | Fact | The catalogue lists the four context operations.                    |

## Entity analysis model override query

### EntityAnalysisModelOverrideQueryServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelOverrideQuery.EntityAnalysisModelOverrideQueryServiceTests` — 29 tests.
Category: Service. Collection: Database.

| Test                                                                      | Kind   | Verifies                                                                          |
|---------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `ValuesListsAValueHeldOnlyByAnActivationRuleOverrideAsync`                | Fact   | Values lists a value held only by an activation rule override.                    |
| `ValuesListsAValueOncePerModelWhenSeveralActivationRulesOverrideItAsync`  | Fact   | Values lists a value once per model when several activation rules override it.    |
| `ValuesReportsForceWhereAnyOverrideOnTheValueIsForceAsync`                | Fact   | Values reports force where any override on the value is force.                    |
| `ValuesExcludesDeletedAndExpiredActivationRuleOverridesAsync`             | Fact   | Values excludes deleted and expired activation rule overrides.                    |
| `KeysListsAnOverrideEnabledKeyThatHoldsNothingYetAsync`                   | Fact   | Keys lists an override enabled key that holds nothing yet.                        |
| `KeysAggregatesValuesOverridesForcedAndRulesAcrossBothTablesAsync`        | Fact   | Keys aggregates values overrides forced and rules across both tables.             |
| `KeysExcludesDeletedAndExpiredOverridesFromItsCountsAsync`                | Fact   | Keys excludes deleted and expired overrides from its counts.                      |
| `KeysNeverReportsAnotherTenantsOverridesAsync`                            | Fact   | Keys never reports another tenants overrides.                                     |
| `KeysReportsTheEarliestExpiryAndTheLatestCreationAsync`                   | Fact   | Keys reports the earliest expiry and the latest creation.                         |
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                | Theory | Create with null or blank user name throws not authenticated.                     |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                 | Fact   | Create with user having no tenant throws not authenticated.                       |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                        | Fact   | Create with unknown user throws not authenticated.                                |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                       | Fact   | Get async throws forbidden when permission missing.                               |
| `GetAsyncMapsSuppressedModelFieldByFieldAsync`                            | Fact   | Get async maps suppressed model field by field.                                   |
| `GetAsyncReportsOverrideWithNullExpiryAsync`                              | Fact   | Get async reports override with null expiry.                                      |
| `GetAsyncReturnsModelWithoutOverrideWhenNoneMatchesAsync`                 | Fact   | Get async returns model without override when none matches.                       |
| `GetAsyncExpiredOrDeletedOverridesAreNotActiveAsync`                      | Fact   | Get async expired or deleted overrides are not active.                            |
| `GetAsyncExcludesDeletedModelsDeletedXpathsAndOverrideDisabledAsync`      | Fact   | Get async excludes deleted models deleted xpaths and override disabled.           |
| `GetAsyncReturnsEmptyWhenNoModelSupportsTheKeyAsync`                      | Fact   | Get async returns empty when no model supports the key.                           |
| `TenantBDataIsInvisibleToTenantAAndViceVersaAsync`                        | Fact   | Tenant b data is invisible to tenant a and vice versa.                            |
| `OverrideOnOtherTenantsModelDoesNotLeakIntoOwnModelAsync`                 | Fact   | Override on other tenants model does not leak into own model.                     |
| `PreCancelledTokenThrowsOperationCanceledAsync`                           | Fact   | Pre cancelled token throws operation canceled.                                    |
| `PermissionDeniedLogsWarnNotErrorAsync`                                   | Fact   | Permission denied logs warn not error.                                            |
| `AuditLogGetsExactlyOneLineAsync`                                         | Fact   | Audit log gets exactly one line.                                                  |
| `ReadsAndFailuresPublishNothingAsync`                                     | Fact   | Reads and failures publish nothing.                                               |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                     | Fact   | Catalogue registers unique pascal case no underscore names.                       |
| `ValuesListsEachOverrideOnceWhereAModelGuidIsSharedWithADeletedTwinAsync` | Fact   | Values lists each override once where a model guid is shared with a deleted twin. |
| `ValuesExcludesOverridesOfDeletedModelsAsync`                             | Fact   | Values excludes overrides of deleted models.                                      |
| `ValuesKeepsTheSameValueOnceForEachModelThatOverridesItAsync`             | Fact   | Values keeps the same value once for each model that overrides it.                |

## Entity analysis model sample

### EntityAnalysisModelSampleServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelSample.EntityAnalysisModelSampleServiceTests` — 32 tests. Category: Service.
Collection: Database.

| Test                                                                                       | Kind   | Verifies                                                                                                |
|--------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                                 | Theory | Create with null or blank user name throws not authenticated.                                           |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                                  | Fact   | Create with user having no tenant throws not authenticated.                                             |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                         | Fact   | Create with unknown user throws not authenticated.                                                      |
| `ExecuteThrowsForbiddenWhenPermissionMissingAndWritesNoAuditRowAsync`                      | Fact   | Execute throws forbidden when permission missing and writes no audit row.                               |
| `ExecuteThrowsForbiddenForUserWithoutAnyPermissionAsync`                                   | Fact   | Execute throws forbidden for user without any permission.                                               |
| `PermissionDeniedLogsWarnNotErrorAsync`                                                    | Fact   | Permission denied logs warn not error.                                                                  |
| `PermissionIsCheckedBeforeValidationAsync`                                                 | Fact   | Permission is checked before validation.                                                                |
| `ExecuteThrowsDtoValidationExceptionWithLegacyMessagesAndWritesNoAuditRowAsync`            | Theory | Execute throws dto validation exception with legacy messages and writes no audit row.                   |
| `SampleBoundariesZeroAndOneAreValidAsync`                                                  | Fact   | Sample boundaries zero and one are valid.                                                               |
| `ExecuteThrowsNotFoundForUnknownModelGuidAsync`                                            | Fact   | Execute throws not found for unknown model guid.                                                        |
| `ExecuteThrowsNotFoundForSoftDeletedModelAsync`                                            | Fact   | Execute throws not found for soft deleted model.                                                        |
| `TenantBCannotSampleTenantAModelAndViceVersaAndNoAuditRowIsWrittenAsync`                   | Fact   | Tenant b cannot sample tenant a model and vice versa and no audit row is written.                       |
| `CsvContainsHeaderAndTypeCoercedRowForStringIntFloatDateBooleanAsync`                      | Fact   | Csv contains header and type coerced row for string int float date boolean.                             |
| `CsvQuotesAndEscapesEmbeddedQuotesAndCommasInStringsAsync`                                 | Fact   | Csv quotes and escapes embedded quotes and commas in strings.                                           |
| `CsvUsesLegacyDefaultsForMissingPayloadFieldsAsync`                                        | Fact   | Csv uses legacy defaults for missing payload fields.                                                    |
| `CsvFallsBackToZeroOrEmptyStringWhenCoercionFailsAndLogsInfoAsync`                         | Fact   | Csv falls back to zero or empty string when coercion fails and logs info.                               |
| `CsvTreatsBooleanFalseAsOneBecauseLegacyDoesNotInspectTheValueAsync`                       | Fact   | Csv treats boolean false as one because legacy does not inspect the value.                              |
| `CsvHandlesCurrencyAndDoubleAliasDataTypesSixAndSevenAsync`                                | Fact   | Csv handles currency and double alias data types six and seven.                                         |
| `CsvIncludesOneLinePerSampledRowAndHonoursTheDateWindowAsync`                              | Fact   | Csv includes one line per sampled row and honours the date window.                                      |
| `AFieldWhoseJsonPathIsACatastrophicRegexFilterIsCutShortAndTheCellIsTheDefaultAsync`       | Fact   | A field whose json path is a catastrophic regex filter is cut short and the cell is the default.        |
| `ASampleLargerThanTheByteLimitIsRefusedWithNoFileAndAnErrorAuditRowAsync`                  | Fact   | A sample larger than the byte limit is refused with no file and an error audit row.                     |
| `SampleZeroReturnsHeaderOnlyAsync`                                                         | Fact   | Sample zero returns header only.                                                                        |
| `CsvOnlyContainsArchiveRowsOfTheRequestedModelAsync`                                       | Fact   | Csv only contains archive rows of the requested model.                                                  |
| `FileNameFollowsSampleModelIdTimestampPatternAsync`                                        | Fact   | File name follows sample model id timestamp pattern.                                                    |
| `SuccessWritesOneExecutionLogRowWithRowCountAndParametersAsync`                            | Fact   | Success writes one execution log row with row count and parameters.                                     |
| `SuccessWithZeroRowsStillWritesAuditRowWithRowCountZeroAsync`                              | Fact   | Success with zero rows still writes audit row with row count zero.                                      |
| `FailureWritesAuditRowInErrorWithStackAndPropagatesAsync`                                  | Fact   | Failure writes audit row in error with stack and propagates.                                            |
| `ReportConnectionStringTakesPrecedenceOverConnectionStringAsync`                           | Fact   | Report connection string takes precedence over connection string.                                       |
| `PreCancelledTokenThrowsOperationCanceledAndWritesNoAuditRowAsync`                         | Fact   | Pre cancelled token throws operation canceled and writes no audit row.                                  |
| `ExactlyOneAuditLogLineOnSuccessAsync`                                                     | Fact   | Exactly one audit log line on success.                                                                  |
| `ReadPublishesNoChangeEventOnSuccessOrFailureAsync`                                        | Fact   | Read publishes no change event on success or failure.                                                   |
| `ExecuteIsDeclaredAsAnIdempotentReadServiceOperationNamedEntityAnalysisModelSampleExecute` | Fact   | Execute is declared as an idempotent read service operation named entity analysis model sample execute. |

## Entity analysis model synchronisation node status entries

### EntityAnalysisModelSynchronisationNodeStatusEntriesServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelSynchronisationNodeStatusEntries.EntityAnalysisModelSynchronisationNodeStatusEntriesServiceTests` —
20 tests. Category: Service. Collection: Database.

| Test                                                                         | Kind   | Verifies                                                                           |
|------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                   | Theory | Create with null or blank user name throws not authenticated.                      |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                    | Fact   | Create with user having no tenant throws not authenticated.                        |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                           | Fact   | Create with unknown user throws not authenticated.                                 |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                          | Fact   | Get async throws forbidden when permission missing.                                |
| `GetAsyncMapsEveryFieldAndComputesFlagsAsync`                                | Fact   | Get async maps every field and computes flags.                                     |
| `GetAsyncExcludesEntriesWithHeartbeatOlderThanOneHourAsync`                  | Fact   | Get async excludes entries with heartbeat older than one hour.                     |
| `GetAsyncReturnsAnEmptyListWhenTenantHasNeverScheduledASynchronisationAsync` | Fact   | Get async returns an empty list when tenant has never scheduled a synchronisation. |
| `GetAsyncReturnsEmptyListWhenScheduleExistsButNoRecentHeartbeatsAsync`       | Fact   | Get async returns empty list when schedule exists but no recent heartbeats.        |
| `TenantsOnlySeeTheirOwnNodeStatusEntriesAsync`                               | Fact   | Tenants only see their own node status entries.                                    |
| `ScheduleOfAnotherTenantDoesNotInfluencePendingFlagAsync`                    | Fact   | Schedule of another tenant does not influence pending flag.                        |
| `PreCancelledTokenThrowsOperationCanceledAsync`                              | Fact   | Pre cancelled token throws operation canceled.                                     |
| `PermissionDeniedLogsWarnNotErrorAsync`                                      | Fact   | Permission denied logs warn not error.                                             |
| `WithGatesDisabledHappyPathRecordsNothingAsync`                              | Fact   | With gates disabled happy path records nothing.                                    |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`               | Fact   | Failure logs error with exception attached and still propagates.                   |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                    | Fact   | Each call emits one span with outcome tag.                                         |
| `EachCallRecordsOneDurationMeasurementAsync`                                 | Fact   | Each call records one duration measurement.                                        |
| `AuditLogGetsExactlyOneLineAsync`                                            | Fact   | Audit log gets exactly one line.                                                   |
| `ReadsAndFailuresPublishNothingAsync`                                        | Fact   | Reads and failures publish nothing.                                                |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                        | Fact   | Catalogue registers unique pascal case no underscore names.                        |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                       | Fact   | Permission denied message resolves for french culture.                             |

## Entity analysis model time window

### EntityAnalysisModelTimeWindowServiceTests

`Jube.Test.Service.Query.EntityAnalysisModelTimeWindow.EntityAnalysisModelTimeWindowServiceTests` — 7 tests. Category:
Service. Collection: Database.

| Test                                                          | Kind | Verifies                                                          |
|---------------------------------------------------------------|------|-------------------------------------------------------------------|
| `ACalendarWindowEndsAtTheReferenceDateAsync`                  | Fact | A calendar window ends at the reference date.                     |
| `ADateWithoutAKindIsTakenAsUtcAsync`                          | Fact | A date without a kind is taken as utc.                            |
| `AnAbstractionRuleWindowIsShortenedToTheSearchKeysTtlAsync`   | Fact | An abstraction rule window is shortened to the search keys ttl.   |
| `TtlCounterWindowsFollowTheCountersSettingsAsync`             | Fact | Ttl counter windows follow the counters settings.                 |
| `InvalidInputComesBackAsErrorsAsync`                          | Fact | Invalid input comes back as errors.                               |
| `TheIntervalsListEveryKindAsync`                              | Fact | The intervals list every kind.                                    |
| `AnotherTenantsRuleIsNotFoundAndNoPermissionIsForbiddenAsync` | Fact | Another tenants rule is not found and no permission is forbidden. |

## Entity analysis potential multi part string names

### EntityAnalysisPotentialMultiPartStringNamesServiceTests

`Jube.Test.Service.Query.EntityAnalysisPotentialMultiPartStringNames.EntityAnalysisPotentialMultiPartStringNamesServiceTests` —
17 tests. Category: Service. Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                  |
|-------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                          | Theory | Create with null or blank user name throws not authenticated.                             |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                           | Fact   | Create with user having no tenant throws not authenticated.                               |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                  | Fact   | Create with unknown user throws not authenticated.                                        |
| `BothOverloadsThrowForbiddenWhenPermissionMissingAsync`                             | Fact   | Both overloads throw forbidden when permission missing.                                   |
| `AnyOneOfTheFourPermissionsIsSufficientAsync`                                       | Theory | Any one of the four permissions is sufficient.                                            |
| `UnrelatedPermissionAloneIsForbiddenAsync`                                          | Fact   | Unrelated permission alone is forbidden.                                                  |
| `ReturnsStringXpathsAndStringInlineFunctionsSortedAndDistinctForBothOverloadsAsync` | Fact   | Returns string xpaths and string inline functions sorted and distinct for both overloads. |
| `InactiveXpathsAndFunctionsAreNotListedAndReactivationListsThemAgainAsync`          | Fact   | Inactive xpaths and functions are not listed and reactivation lists them again.           |
| `ExcludesNonStringDeletedAndOtherModelRowsAsync`                                    | Fact   | Excludes non string deleted and other model rows.                                         |
| `EmptyResultForModelWithNoRowsAndForUnknownModelAsync`                              | Fact   | Empty result for model with no rows and for unknown model.                                |
| `TenantBModelIsInvisibleToTenantAAndViceVersaForBothOverloadsAsync`                 | Fact   | Tenant b model is invisible to tenant a and vice versa for both overloads.                |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                     | Fact   | Pre cancelled token throws operation canceled.                                            |
| `PermissionDeniedLogsWarnNotErrorAsync`                                             | Fact   | Permission denied logs warn not error.                                                    |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                      | Fact   | Failure logs error with exception attached and still propagates.                          |
| `EachCallEmitsOneSpanWithOutcomeTagAndExactlyOneAuditLineAsync`                     | Fact   | Each call emits one span with outcome tag and exactly one audit line.                     |
| `ReadsAndFailuresPublishNothingAsync`                                               | Fact   | Reads and failures publish nothing.                                                       |
| `CatalogueRegistersBothUniqueToolNames`                                             | Fact   | Catalogue registers both unique tool names.                                               |

## Entity analysis request x path inline script names by string integer float data type

### EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeServiceTests

`Jube.Test.Service.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeServiceTests` —
19 tests. Category: Service. Collection: Database.

| Test                                                                              | Kind   | Verifies                                                                                |
|-----------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                        | Theory | Create with null or blank user name throws not authenticated.                           |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                         | Fact   | Create with user having no tenant throws not authenticated.                             |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                | Fact   | Create with unknown user throws not authenticated.                                      |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                               | Fact   | Get async throws forbidden when permission missing.                                     |
| `AnyOneOfTheFourPermissionsSufficesAsync`                                         | Theory | Any one of the four permissions suffices.                                               |
| `UnrelatedPermissionAloneIsForbiddenAsync`                                        | Fact   | Unrelated permission alone is forbidden.                                                |
| `GetAsyncReturnsXpathsScriptPropertiesAndFunctionsMappedFieldByFieldAsync`        | Fact   | Get async returns xpaths script properties and functions mapped field by field.         |
| `InactiveXpathsScriptsAndFunctionsAreNotListedAndReactivationListsThemAgainAsync` | Fact   | Inactive xpaths scripts and functions are not listed and reactivation lists them again. |
| `GetAsyncReturnsEmptyForModelWithNoFieldsAsync`                                   | Fact   | Get async returns empty for model with no fields.                                       |
| `GetAsyncReturnsEmptyForUnknownModelAsync`                                        | Fact   | Get async returns empty for unknown model.                                              |
| `TenantBDataIsInvisibleToTenantAAndViceVersaAsync`                                | Fact   | Tenant b data is invisible to tenant a and vice versa.                                  |
| `OtherTenantInlineFunctionsDoNotLeakThroughTheUnscopedDataQueryAsync`             | Fact   | Other tenant inline functions do not leak through the unscoped data query.              |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                   | Fact   | Pre cancelled token throws operation canceled.                                          |
| `PermissionDeniedLogsWarnNotErrorAsync`                                           | Fact   | Permission denied logs warn not error.                                                  |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                    | Fact   | Failure logs error with exception attached and still propagates.                        |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                         | Fact   | Each call emits one span with outcome tag.                                              |
| `AuditLogGetsExactlyOneLineAsync`                                                 | Fact   | Audit log gets exactly one line.                                                        |
| `ReadsAndFailuresPublishNothingAsync`                                             | Fact   | Reads and failures publish nothing.                                                     |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                             | Fact   | Catalogue registers unique pascal case no underscore names.                             |

## Entity version history

### EntityVersionHistoryQueryServiceTests

`Jube.Test.Service.Query.EntityVersionHistory.EntityVersionHistoryQueryServiceTests` — 9 tests. Category: Service.
Collection: Database.

| Test                                                                    | Kind   | Verifies                                                                  |
|-------------------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `TenantRegistry_IdDateRangeLatestAndCompareBehaveCorrectlyAsync`        | Fact   | Tenant registry id date range latest and compare behave correctly.        |
| `TenantRegistry_NullOrBlankUserNameThrowsNotAuthenticatedAsync`         | Theory | Tenant registry null or blank user name throws not authenticated.         |
| `TenantRegistry_UnknownUserThrowsNotAuthenticatedAsync`                 | Fact   | Tenant registry unknown user throws not authenticated.                    |
| `TenantRegistry_NonLandlordUserIsForbiddenRegardlessOfParentIdAsync`    | Fact   | Tenant registry non landlord user is forbidden regardless of parent id.   |
| `CaseWorkflowStatus_ParentScopingNeverLeaksAnotherParentsVersionsAsync` | Fact   | Case workflow status parent scoping never leaks another parents versions. |
| `CaseWorkflowStatus_NullOrBlankUserNameThrowsNotAuthenticatedAsync`     | Theory | Case workflow status null or blank user name throws not authenticated.    |
| `CaseWorkflowStatus_UnknownUserThrowsNotAuthenticatedAsync`             | Fact   | Case workflow status unknown user throws not authenticated.               |
| `CaseWorkflowStatus_UserWithoutPermissionIsForbiddenAsync`              | Fact   | Case workflow status user without permission is forbidden.                |
| `CaseWorkflowStatus_UserInTenantBCannotReadTenantARowsAsync`            | Fact   | Case workflow status user in tenant b cannot read tenant a rows.          |

### ChecklistVersionHistoryTests

`Jube.Test.Service.Query.EntityVersionHistory.ChecklistVersionHistoryTests` — 8 tests. Category: Service. Collection:
Database.

| Test                                                            | Kind   | Verifies                                                                                                                                                                                              |
|-----------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ChecklistHistoryNeverLeaksAnotherChecklistsVersionsAsync`      | Fact   | Each checklist sees only its own earlier states, the latest is the most recent one, and a version can be fetched by id.                                                                               |
| `ChecklistCompareReportsTheChangedFieldsAsync`                  | Fact   | Comparing two checklist versions reports the changed Name and Priority with their from and to values.                                                                                                 |
| `ChecklistHistoryOfAnotherTenantIsEmptyAsync`                   | Fact   | A user in another tenant gets no versions, no latest, no version by id, nothing for a date range and an empty comparison.                                                                             |
| `ChecklistHistoryRequiresPermissionNineteenAsync`               | Fact   | A user without permission 19 is refused the checklist history.                                                                                                                                        |
| `ItemHistoryHidesBothSqlFieldsWithoutPermissionSixtySevenAsync` | Fact   | Without permission 67 every item history read returns null for ShowSql and ValidateSql, keeps the other fields, and leaves the SQL out of a comparison; with 67 the SQL and its changes are returned. |
| `ItemHistoryOfAnotherTenantIsEmptyAsync`                        | Fact   | A user in another tenant gets no item versions, no latest, no version by id and an empty comparison.                                                                                                  |
| `BothHistoryServicesRefuseABlankUserAsync`                      | Theory | A null, empty or whitespace user name is not authenticated for both the checklist and the item history services.                                                                                      |
| `BothHistoryServicesRefuseAnUnknownUserAsync`                   | Fact   | A user that resolves to no tenant is not authenticated for both services.                                                                                                                             |

### CaseWorkflowReleaseHookVersionHistoryServiceTests

`Jube.Test.Service.Query.CaseWorkflowReleaseHookVersionHistory.CaseWorkflowReleaseHookVersionHistoryServiceTests` — 5
tests. Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                    |
|-----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `EveryUpdateLeavesTheEarlierStateAsAVersionOldestFirstAndTheLatestIsTheLastAsync` | Fact | Every update leaves the earlier state as a version oldest first and the latest is the last. |
| `CompareReportsTheFieldsThatChangedBetweenTwoVersionsAsync`                       | Fact | Compare reports the fields that changed between two versions.                               |
| `WithoutPermission69TheHistoryIsRefusedAsync`                                     | Fact | Without permission69 the history is refused.                                                |
| `AnotherTenantSeesNoVersionsAndCannotFetchOneByIdOrCompareAsync`                  | Fact | Another tenant sees no versions and cannot fetch one by id or compare.                      |
| `ABlankUserIsNotAuthenticatedAsync`                                               | Fact | A blank user is not authenticated.                                                          |

## Exhaustive search instance promoted trial instance confusion

### ExhaustiveSearchInstancePromotedTrialInstanceConfusionServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion.ExhaustiveSearchInstancePromotedTrialInstanceConfusionServiceTests` —
22 tests. Category: Service. Collection: Database.

| Test                                                               | Kind   | Verifies                                                               |
|--------------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`         | Theory | Create with null or blank user name throws not authenticated.          |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`          | Fact   | Create with user having no tenant throws not authenticated.            |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                 | Fact   | Create with unknown user throws not authenticated.                     |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                | Fact   | Get async throws forbidden when permission missing.                    |
| `PermissionDeniedLogsWarnNotErrorAsync`                            | Fact   | Permission denied logs warn not error.                                 |
| `GetAsyncReturnsExactFieldByFieldMappingOfTheQueryAsync`           | Fact   | Get async returns exact field by field mapping of the query.           |
| `GetAsyncAllCountsZeroReturnsZeroRatiosNotNaNAsync`                | Fact   | Get async all counts zero returns zero ratios not na n.                |
| `GetAsyncOnlyTrueNegativesHasZeroDenominatorsForPositiveSideAsync` | Fact   | Get async only true negatives has zero denominators for positive side. |
| `GetAsyncOnlyTruePositivesHasZeroDenominatorsForNegativeSideAsync` | Fact   | Get async only true positives has zero denominators for negative side. |
| `GetAsyncFalseNegativeRatiosUseFalseNegativeCountAsync`            | Fact   | Get async false negative ratios use false negative count.              |
| `GetAsyncIgnoresInactivePromotedTrialInstancesAsync`               | Fact   | Get async ignores inactive promoted trial instances.                   |
| `GetAsyncReturnsAllZeroWhenOnlyInactiveExistsAsync`                | Fact   | Get async returns all zero when only inactive exists.                  |
| `GetAsyncReturnsAllZeroForUnknownSearchInstanceAsync`              | Fact   | Get async returns all zero for unknown search instance.                |
| `TenantBCannotSeeTenantASearchInstanceAsync`                       | Fact   | Tenant b cannot see tenant a search instance.                          |
| `TenantACannotSeeTenantBSearchInstanceAsync`                       | Fact   | Tenant a cannot see tenant b search instance.                          |
| `EachTenantSeesOnlyItsOwnDataAsync`                                | Fact   | Each tenant sees only its own data.                                    |
| `PreCancelledTokenThrowsOperationCanceledAsync`                    | Fact   | Pre cancelled token throws operation canceled.                         |
| `EachCallEmitsOneSpanAndOneDurationMeasurementAsync`               | Fact   | Each call emits one span and one duration measurement.                 |
| `AuditLogGetsExactlyOneLineAsync`                                  | Fact   | Audit log gets exactly one line.                                       |
| `ReadsAndFailuresPublishNothingAsync`                              | Fact   | Reads and failures publish nothing.                                    |
| `GetAsyncDeclaresUniqueReadIdempotentServiceOperation`             | Fact   | Get async declares unique read idempotent service operation.           |
| `PermissionDeniedMessageComesFromResourcesAsync`                   | Fact   | Permission denied message comes from resources.                        |

## Exhaustive search instance promoted trial instance error histogram

### ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramServiceTests` —
14 tests. Category: Service. Collection: Database.

| Test                                                            | Kind   | Verifies                                                             |
|-----------------------------------------------------------------|--------|----------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`      | Theory | Create with null or blank user name throws not authenticated.        |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`       | Fact   | Create with user having no tenant throws not authenticated.          |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`              | Fact   | Create with unknown user throws not authenticated.                   |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`             | Fact   | Get async throws forbidden when permission missing.                  |
| `GetAsyncReturnsTenBinsWhoseFrequenciesSumToTheErrorCountAsync` | Fact   | Get async returns ten bins whose frequencies sum to the error count. |
| `GetAsyncMapsEveryFieldExactlyFromTheDataLayerQueryAsync`       | Fact   | Get async maps every field exactly from the data layer query.        |
| `GetAsyncUsesOnlyTheActivePromotedTrialInstanceAsync`           | Fact   | Get async uses only the active promoted trial instance.              |
| `GetAsyncForUnknownInstanceReturnsNoFrequencyAsync`             | Fact   | Get async for unknown instance returns no frequency.                 |
| `TenantBInstanceIsInvisibleToTenantAAndViceVersaAsync`          | Fact   | Tenant b instance is invisible to tenant a and vice versa.           |
| `PreCancelledTokenThrowsOperationCanceledAsync`                 | Fact   | Pre cancelled token throws operation canceled.                       |
| `PermissionDeniedLogsWarnNotErrorAsync`                         | Fact   | Permission denied logs warn not error.                               |
| `AuditLogGetsExactlyOneLineAsync`                               | Fact   | Audit log gets exactly one line.                                     |
| `ReadsAndFailuresPublishNothingAsync`                           | Fact   | Reads and failures publish nothing.                                  |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`           | Fact   | Catalogue registers unique pascal case no underscore names.          |

## Exhaustive search instance promoted trial instance learning curve

### ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveServiceTests` —
17 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind   | Verifies                                                                           |
|-----------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                  | Theory | Create with null or blank user name throws not authenticated.                      |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                   | Fact   | Create with user having no tenant throws not authenticated.                        |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                          | Fact   | Create with unknown user throws not authenticated.                                 |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                         | Fact   | Get async throws forbidden when permission missing.                                |
| `GetAsyncReturnsSeededCurveOrderedByIdWithRoundedScoreAndExactMappingAsync` | Fact   | Get async returns seeded curve ordered by id with rounded score and exact mapping. |
| `GetAsyncOnlyReturnsPointsOfTheRequestedSearchInstanceAsync`                | Fact   | Get async only returns points of the requested search instance.                    |
| `GetAsyncReturnsEmptyForSearchInstanceWithNoPromotedTrialsAsync`            | Fact   | Get async returns empty for search instance with no promoted trials.               |
| `GetAsyncReturnsEmptyForUnknownSearchInstanceIdAsync`                       | Fact   | Get async returns empty for unknown search instance id.                            |
| `TenantBCannotSeeTenantACurveAsync`                                         | Fact   | Tenant b cannot see tenant a curve.                                                |
| `TenantACannotSeeTenantBCurveAsync`                                         | Fact   | Tenant a cannot see tenant b curve.                                                |
| `PreCancelledTokenThrowsOperationCanceledAsync`                             | Fact   | Pre cancelled token throws operation canceled.                                     |
| `PermissionDeniedLogsWarnNotErrorAsync`                                     | Fact   | Permission denied logs warn not error.                                             |
| `FailureLogsErrorWithExceptionAttachedAndPropagatesAsync`                   | Fact   | Failure logs error with exception attached and propagates.                         |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                   | Fact   | Each call emits one span with outcome tag.                                         |
| `AuditLogGetsExactlyOneLineAsync`                                           | Fact   | Audit log gets exactly one line.                                                   |
| `ReadsAndFailuresPublishNothingAsync`                                       | Fact   | Reads and failures publish nothing.                                                |
| `CatalogueRegistersUniqueToolName`                                          | Fact   | Catalogue registers unique tool name.                                              |

## Exhaustive search instance promoted trial instance predicted actual

### ExhaustiveSearchInstancePromotedTrialInstancePredictedActualServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual.ExhaustiveSearchInstancePromotedTrialInstancePredictedActualServiceTests` —
18 tests. Category: Service. Collection: Database.

| Test                                                                   | Kind   | Verifies                                                                      |
|------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`             | Theory | Create with null or blank user name throws not authenticated.                 |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`              | Fact   | Create with user having no tenant throws not authenticated.                   |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                     | Fact   | Create with unknown user throws not authenticated.                            |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                    | Fact   | Get async throws forbidden when permission missing.                           |
| `GetAsyncReturnsSeededRowsMappedFieldByFieldInIdOrderAsync`            | Fact   | Get async returns seeded rows mapped field by field in id order.              |
| `GetAsyncReturnsEmptyForUnknownSearchInstanceAsync`                    | Fact   | Get async returns empty for unknown search instance.                          |
| `GetAsyncReturnsEmptyWhenPromotedTrialIsNotActiveAsync`                | Fact   | Get async returns empty when promoted trial is not active.                    |
| `GetAsyncExcludesSoftDeletedPredictedActualRowsAsync`                  | Fact   | Get async excludes soft deleted predicted actual rows.                        |
| `GetAsyncIgnoresSoftDeletedPromotedTrialAndFallsBackToTheLiveOneAsync` | Fact   | Get async ignores soft deleted promoted trial and falls back to the live one. |
| `GetAsyncOnlyReturnsRowsOfTheMostRecentlyPromotedTrialAsync`           | Fact   | Get async only returns rows of the most recently promoted trial.              |
| `TenantBCannotSeeTenantASearchInstanceAndViceVersaAsync`               | Fact   | Tenant b cannot see tenant a search instance and vice versa.                  |
| `PreCancelledTokenThrowsOperationCanceledAsync`                        | Fact   | Pre cancelled token throws operation canceled.                                |
| `PermissionDeniedLogsWarnNotErrorAsync`                                | Fact   | Permission denied logs warn not error.                                        |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`         | Fact   | Failure logs error with exception attached and still propagates.              |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                              | Fact   | Each call emits one span with outcome tag.                                    |
| `AuditLogGetsExactlyOneLineAsync`                                      | Fact   | Audit log gets exactly one line.                                              |
| `ReadsAndFailuresPublishNothingAsync`                                  | Fact   | Reads and failures publish nothing.                                           |
| `CatalogueRegistersUniqueToolName`                                     | Fact   | Catalogue registers unique tool name.                                         |

## Exhaustive search instance promoted trial instance query

### ExhaustiveSearchInstancePromotedTrialInstanceQueryServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery.ExhaustiveSearchInstancePromotedTrialInstanceQueryServiceTests` —
17 tests. Category: Service. Collection: Database.

| Test                                                                   | Kind   | Verifies                                                                    |
|------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`             | Theory | Create with null or blank user name throws not authenticated.               |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`              | Fact   | Create with user having no tenant throws not authenticated.                 |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                     | Fact   | Create with unknown user throws not authenticated.                          |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                    | Fact   | Get async throws forbidden when permission missing.                         |
| `GetAsyncMapsEveryFieldExactlyAsync`                                   | Fact   | Get async maps every field exactly.                                         |
| `GetAsyncMapsInactiveAsFalseAsync`                                     | Fact   | Get async maps inactive as false.                                           |
| `GetAsyncOrdersByIdDescendingAndFiltersToRequestedSearchInstanceAsync` | Fact   | Get async orders by id descending and filters to requested search instance. |
| `GetAsyncReturnsEmptyForSearchInstanceWithNoPromotedRowsAsync`         | Fact   | Get async returns empty for search instance with no promoted rows.          |
| `GetAsyncReturnsEmptyForUnknownSearchInstanceAsync`                    | Fact   | Get async returns empty for unknown search instance.                        |
| `TenantBDataIsInvisibleToTenantAAsync`                                 | Fact   | Tenant b data is invisible to tenant a.                                     |
| `TenantADataIsInvisibleToTenantBAsync`                                 | Fact   | Tenant a data is invisible to tenant b.                                     |
| `EachTenantSeesOwnDataAsync`                                           | Fact   | Each tenant sees own data.                                                  |
| `PreCancelledTokenThrowsOperationCanceledAsync`                        | Fact   | Pre cancelled token throws operation canceled.                              |
| `PermissionDeniedLogsWarnNotErrorAsync`                                | Fact   | Permission denied logs warn not error.                                      |
| `AuditLogGetsExactlyOneLineAsync`                                      | Fact   | Audit log gets exactly one line.                                            |
| `ReadsAndFailuresPublishNothingAsync`                                  | Fact   | Reads and failures publish nothing.                                         |
| `CatalogueRegistersUniqueToolName`                                     | Fact   | Catalogue registers unique tool name.                                       |

## Exhaustive search instance promoted trial instance roc

### ExhaustiveSearchInstancePromotedTrialInstanceRocServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc.ExhaustiveSearchInstancePromotedTrialInstanceRocServiceTests` —
17 tests. Category: Service. Collection: Database.

| Test                                                            | Kind   | Verifies                                                            |
|-----------------------------------------------------------------|--------|---------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`      | Theory | Create with null or blank user name throws not authenticated.       |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`       | Fact   | Create with user having no tenant throws not authenticated.         |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`              | Fact   | Create with unknown user throws not authenticated.                  |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`             | Fact   | Get async throws forbidden when permission missing.                 |
| `GetAsyncMapsRocPointsFieldByFieldOrderedByIdAsync`             | Fact   | Get async maps roc points field by field ordered by id.             |
| `GetAsyncReturnsEmptyForUnknownSearchInstanceAsync`             | Fact   | Get async returns empty for unknown search instance.                |
| `GetAsyncReturnsEmptyWhenPromotedTrialInstanceIsNotActiveAsync` | Fact   | Get async returns empty when promoted trial instance is not active. |
| `GetAsyncOnlyReturnsPointsOfThePromotedTrialInstanceAsync`      | Fact   | Get async only returns points of the promoted trial instance.       |
| `TenantBCannotSeeTenantARocPointsAsync`                         | Fact   | Tenant b cannot see tenant a roc points.                            |
| `TenantACannotSeeTenantBRocPointsAsync`                         | Fact   | Tenant a cannot see tenant b roc points.                            |
| `PreCancelledTokenThrowsOperationCanceledAsync`                 | Fact   | Pre cancelled token throws operation canceled.                      |
| `PermissionDeniedLogsWarnNotErrorAsync`                         | Fact   | Permission denied logs warn not error.                              |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`  | Fact   | Failure logs error with exception attached and still propagates.    |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                       | Fact   | Each call emits one span with outcome tag.                          |
| `AuditLogGetsExactlyOneLineAsync`                               | Fact   | Audit log gets exactly one line.                                    |
| `ReadsAndFailuresPublishNothingAsync`                           | Fact   | Reads and failures publish nothing.                                 |
| `CatalogueRegistersUniqueToolName`                              | Fact   | Catalogue registers unique tool name.                               |

## Exhaustive search instance promoted trial instance variable prescription

### ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionServiceTests` —
22 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                         |
|----------------------------------------------------------------|--------|------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`     | Theory | Create with null or blank user name throws not authenticated.    |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`      | Fact   | Create with user having no tenant throws not authenticated.      |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`             | Fact   | Create with unknown user throws not authenticated.               |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`            | Fact   | Get async throws forbidden when permission missing.              |
| `GetAsyncMapsEveryFieldFromSeededRowsAsync`                    | Fact   | Get async maps every field from seeded rows.                     |
| `MissingPrescriptionAndSensitivityDefaultToZeroAsync`          | Fact   | Missing prescription and sensitivity default to zero.            |
| `ResultsAreOrderedBySensitivityDescendingAsync`                | Fact   | Results are ordered by sensitivity descending.                   |
| `RemovedVariablesAreExcludedButNullRemovedIsIncludedAsync`     | Fact   | Removed variables are excluded but null removed is included.     |
| `UnknownPromotedTrialInstanceReturnsEmptyAsync`                | Fact   | Unknown promoted trial instance returns empty.                   |
| `PromotedTrialInstanceWithNoVariablesReturnsEmptyAsync`        | Fact   | Promoted trial instance with no variables returns empty.         |
| `TenantBPromotedTrialInstanceIsInvisibleToTenantAAsync`        | Fact   | Tenant b promoted trial instance is invisible to tenant a.       |
| `TenantAPromotedTrialInstanceIsInvisibleToTenantBAsync`        | Fact   | Tenant a promoted trial instance is invisible to tenant b.       |
| `EachTenantSeesOnlyItsOwnPromotedTrialInstanceAsync`           | Fact   | Each tenant sees only its own promoted trial instance.           |
| `DataQueryIsTenantScopedIndependentlyOfTheServiceGuardAsync`   | Fact   | Data query is tenant scoped independently of the service guard.  |
| `ForeignTenantAttemptLogsWarnNotErrorAsync`                    | Fact   | Foreign tenant attempt logs warn not error.                      |
| `PreCancelledTokenThrowsOperationCanceledAsync`                | Fact   | Pre cancelled token throws operation canceled.                   |
| `PermissionDeniedLogsWarnNotErrorAsync`                        | Fact   | Permission denied logs warn not error.                           |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync` | Fact   | Failure logs error with exception attached and still propagates. |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                      | Fact   | Each call emits one span with outcome tag.                       |
| `AuditLogGetsExactlyOneLineAsync`                              | Fact   | Audit log gets exactly one line.                                 |
| `ReadsAndFailuresPublishNothingAsync`                          | Fact   | Reads and failures publish nothing.                              |
| `CatalogueRegistersTheUniqueToolName`                          | Fact   | Catalogue registers the unique tool name.                        |

## Exhaustive search instance promotion history

### ExhaustiveSearchInstancePromotionHistoryServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotionHistory.ExhaustiveSearchInstancePromotionHistoryServiceTests` —
18 tests. Category: Service. Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                       |
|--------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`               | Theory | Create with null or blank user name throws not authenticated.                  |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                | Fact   | Create with user having no tenant throws not authenticated.                    |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                       | Fact   | Create with unknown user throws not authenticated.                             |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                      | Fact   | Get async throws forbidden when permission missing.                            |
| `GetAsyncReturnsThePromotionsInOrderWithExactMappingAndTrialCountsAsync` | Fact   | Get async returns the promotions in order with exact mapping and trial counts. |
| `GetAsyncOmitsDeletedPromotionsAndNeverReturnsTheSerialisedNetworkAsync` | Fact   | Get async omits deleted promotions and never returns the serialised network.   |
| `GetAsyncOnlyReturnsPointsOfTheRequestedSearchInstanceAsync`             | Fact   | Get async only returns points of the requested search instance.                |
| `GetAsyncReturnsEmptyForSearchInstanceWithNoPromotedTrialsAsync`         | Fact   | Get async returns empty for search instance with no promoted trials.           |
| `GetAsyncReturnsEmptyForUnknownSearchInstanceIdAsync`                    | Fact   | Get async returns empty for unknown search instance id.                        |
| `TenantBCannotSeeTenantACurveAsync`                                      | Fact   | Tenant b cannot see tenant a curve.                                            |
| `TenantACannotSeeTenantBCurveAsync`                                      | Fact   | Tenant a cannot see tenant b curve.                                            |
| `PreCancelledTokenThrowsOperationCanceledAsync`                          | Fact   | Pre cancelled token throws operation canceled.                                 |
| `PermissionDeniedLogsWarnNotErrorAsync`                                  | Fact   | Permission denied logs warn not error.                                         |
| `FailureLogsErrorWithExceptionAttachedAndPropagatesAsync`                | Fact   | Failure logs error with exception attached and propagates.                     |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                | Fact   | Each call emits one span with outcome tag.                                     |
| `AuditLogGetsExactlyOneLineAsync`                                        | Fact   | Audit log gets exactly one line.                                               |
| `ReadsAndFailuresPublishNothingAsync`                                    | Fact   | Reads and failures publish nothing.                                            |
| `CatalogueRegistersUniqueToolName`                                       | Fact   | Catalogue registers unique tool name.                                          |

## Exhaustive search instance trial instance variable

### ExhaustiveSearchInstanceTrialInstanceVariableServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable.ExhaustiveSearchInstanceTrialInstanceVariableServiceTests` —
26 tests. Category: Service. Collection: Database.

| Test                                                             | Kind   | Verifies                                                           |
|------------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`       | Theory | Create with null or blank user name throws not authenticated.      |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`        | Fact   | Create with user having no tenant throws not authenticated.        |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`               | Fact   | Create with unknown user throws not authenticated.                 |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`              | Fact   | Get async throws forbidden when permission missing.                |
| `GetAsyncMapsEveryFieldFromSeededRowsAsync`                      | Fact   | Get async maps every field from seeded rows.                       |
| `EmptyRangeIsTrueWhenMaximumPlusMinimumIsZeroAsync`              | Fact   | Empty range is true when maximum plus minimum is zero.             |
| `NullStatisticsAndTypesMapToZeroAsync`                           | Fact   | Null statistics and types map to zero.                             |
| `ResultsAreOrderedByTrialInstanceVariableSequenceAscendingAsync` | Fact   | Results are ordered by trial instance variable sequence ascending. |
| `RemovedVariablesAreExcludedButNullRemovedIsIncludedAsync`       | Fact   | Removed variables are excluded but null removed is included.       |
| `OnlyTheActivePromotedTrialInstanceIsReturnedAsync`              | Fact   | Only the active promoted trial instance is returned.               |
| `MostRecentlyPromotedActiveTrialInstanceWinsAsync`               | Fact   | Most recently promoted active trial instance wins.                 |
| `UnknownSearchInstanceReturnsEmptyAsync`                         | Fact   | Unknown search instance returns empty.                             |
| `SearchInstanceWithNothingPromotedReturnsEmptyAsync`             | Fact   | Search instance with nothing promoted returns empty.               |
| `PromotedTrialInstanceWithNoVariablesReturnsEmptyAsync`          | Fact   | Promoted trial instance with no variables returns empty.           |
| `TenantBSearchInstanceIsInvisibleToTenantAAsync`                 | Fact   | Tenant b search instance is invisible to tenant a.                 |
| `TenantASearchInstanceIsInvisibleToTenantBAsync`                 | Fact   | Tenant a search instance is invisible to tenant b.                 |
| `EachTenantSeesOnlyItsOwnSearchInstanceAsync`                    | Fact   | Each tenant sees only its own search instance.                     |
| `DataQueryForUserWithNoTenantMatchesNothingAsync`                | Fact   | Data query for user with no tenant matches nothing.                |
| `DataQueryByTrialInstanceIdIsTenantScopedAsync`                  | Fact   | Data query by trial instance id is tenant scoped.                  |
| `PreCancelledTokenThrowsOperationCanceledAsync`                  | Fact   | Pre cancelled token throws operation canceled.                     |
| `PermissionDeniedLogsWarnNotErrorAsync`                          | Fact   | Permission denied logs warn not error.                             |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`   | Fact   | Failure logs error with exception attached and still propagates.   |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                        | Fact   | Each call emits one span with outcome tag.                         |
| `AuditLogGetsExactlyOneLineAsync`                                | Fact   | Audit log gets exactly one line.                                   |
| `ReadsAndFailuresPublishNothingAsync`                            | Fact   | Reads and failures publish nothing.                                |
| `CatalogueRegistersTheUniqueToolName`                            | Fact   | Catalogue registers the unique tool name.                          |

## Exhaustive search instance trial instance variable variance

### ExhaustiveSearchInstanceTrialInstanceVariableVarianceServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariableVariance.ExhaustiveSearchInstanceTrialInstanceVariableVarianceServiceTests` —
20 tests. Category: Service. Collection: Database.

| Test                                                           | Kind   | Verifies                                                         |
|----------------------------------------------------------------|--------|------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`     | Theory | Create with null or blank user name throws not authenticated.    |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`      | Fact   | Create with user having no tenant throws not authenticated.      |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`             | Fact   | Create with unknown user throws not authenticated.               |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`            | Fact   | Get async throws forbidden when permission missing.              |
| `GetAsyncMapsEveryFieldFromSeededRowAsync`                     | Fact   | Get async maps every field from seeded row.                      |
| `GetAsyncOrdersByCorrelationAbsRankAscendingAsync`             | Fact   | Get async orders by correlation abs rank ascending.              |
| `GetAsyncExcludesSoftDeletedCorrelationRowsAsync`              | Fact   | Get async excludes soft deleted correlation rows.                |
| `GetAsyncOnlyReturnsRowsForTheRequestedVariableAsync`          | Fact   | Get async only returns rows for the requested variable.          |
| `GetAsyncReturnsEmptyWhenVariableHasNoCorrelationsAsync`       | Fact   | Get async returns empty when variable has no correlations.       |
| `GetAsyncReturnsEmptyForUnknownVariableIdAsync`                | Fact   | Get async returns empty for unknown variable id.                 |
| `TenantBDataIsInvisibleToTenantAAsync`                         | Fact   | Tenant b data is invisible to tenant a.                          |
| `TenantADataIsInvisibleToTenantBAsync`                         | Fact   | Tenant a data is invisible to tenant b.                          |
| `EachTenantSeesItsOwnDataAsync`                                | Fact   | Each tenant sees its own data.                                   |
| `PreCancelledTokenThrowsOperationCanceledAsync`                | Fact   | Pre cancelled token throws operation canceled.                   |
| `PermissionDeniedLogsWarnNotErrorAsync`                        | Fact   | Permission denied logs warn not error.                           |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync` | Fact   | Failure logs error with exception attached and still propagates. |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                      | Fact   | Each call emits one span with outcome tag.                       |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                | Fact   | Audit log gets exactly one line including reads.                 |
| `ReadsAndFailuresPublishNothingAsync`                          | Fact   | Reads and failures publish nothing.                              |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`          | Fact   | Catalogue registers unique pascal case no underscore names.      |

## Exhaustive search instance variable

### ExhaustiveSearchInstanceVariableServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableServiceTests` — 28 tests.
Category: Service. Collection: Database.

| Test                                                                              | Kind   | Verifies                                                                               |
|-----------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                        | Theory | Create with null or blank user name throws not authenticated.                          |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                         | Fact   | Create with user having no tenant throws not authenticated.                            |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                | Fact   | Create with unknown user throws not authenticated.                                     |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                               | Fact   | Get async throws forbidden when permission missing.                                    |
| `GetAsyncMapsEveryFieldFromSeededRowsAsync`                                       | Fact   | Get async maps every field from seeded rows.                                           |
| `NormalisationTypeMapsToDisplayNameAsync`                                         | Theory | Normalisation type maps to display name.                                               |
| `NullStatisticsMapToZeroAndVariableWithoutHistogramHasEmptyBinsAsync`             | Fact   | Null statistics map to zero and variable without histogram has empty bins.             |
| `HistogramsAreAttachedToTheirOwnVariableAsync`                                    | Fact   | Histograms are attached to their own variable.                                         |
| `OtherSearchInstancesVariablesAreNotReturnedAsync`                                | Fact   | Other search instances variables are not returned.                                     |
| `UnknownSearchInstanceReturnsEmptyAsync`                                          | Fact   | Unknown search instance returns empty.                                                 |
| `TenantBSearchInstanceIsInvisibleToTenantAAsync`                                  | Fact   | Tenant b search instance is invisible to tenant a.                                     |
| `TenantASearchInstanceIsInvisibleToTenantBAsync`                                  | Fact   | Tenant a search instance is invisible to tenant b.                                     |
| `EachTenantSeesOnlyItsOwnSearchInstanceAsync`                                     | Fact   | Each tenant sees only its own search instance.                                         |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                   | Fact   | Pre cancelled token throws operation canceled.                                         |
| `PermissionDeniedLogsWarnNotErrorAsync`                                           | Fact   | Permission denied logs warn not error.                                                 |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                    | Fact   | Failure logs error with exception attached and still propagates.                       |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                         | Fact   | Each call emits one span with outcome tag.                                             |
| `AuditLogGetsExactlyOneLineAsync`                                                 | Fact   | Audit log gets exactly one line.                                                       |
| `ReadsAndFailuresPublishNothingAsync`                                             | Fact   | Reads and failures publish nothing.                                                    |
| `CatalogueRegistersTheUniqueToolName`                                             | Fact   | Catalogue registers the unique tool name.                                              |
| `ReallocateAsync_PersistsThePrefixedNameAndProcessingTypeAndReturnsTheModelAsync` | Fact   | Reallocate async persists the prefixed name and processing type and returns the model. |
| `ReallocateAsync_PrefixesEveryKnownProcessingTypeAsync`                           | Theory | Reallocate async prefixes every known processing type.                                 |
| `ReallocateAsync_UnknownIdThrowsNotFoundAsync`                                    | Fact   | Reallocate async unknown id throws not found.                                          |
| `ReallocateAsync_ForeignTenantsVariableThrowsNotFoundAndIsUnchangedAsync`         | Fact   | Reallocate async foreign tenants variable throws not found and is unchanged.           |
| `ReallocateAsync_UnsafeNameThrowsValidationFailedAsync`                           | Theory | Reallocate async unsafe name throws validation failed.                                 |
| `ReallocateAsync_UnknownProcessingTypeThrowsValidationFailedAsync`                | Theory | Reallocate async unknown processing type throws validation failed.                     |
| `ReallocateAsync_ThrowsForbiddenWhenPermissionMissingAsync`                       | Fact   | Reallocate async throws forbidden when permission missing.                             |
| `ReallocateAsync_PublishesExactlyOneUpdatedChangeEventAsync`                      | Fact   | Reallocate async publishes exactly one updated change event.                           |

## Exhaustive search instance variable statistics

### ExhaustiveSearchInstanceVariableStatisticsServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstanceVariableStatistics.ExhaustiveSearchInstanceVariableStatisticsServiceTests` —
15 tests. Category: Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                      |
|---------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                            | Theory | Create with null or blank user name throws not authenticated.                                 |
| `CreateWithTenantlessOrUnknownUserThrowsNotAuthenticatedAsync`                        | Fact   | Create with tenantless or unknown user throws not authenticated.                              |
| `PermissionMissingThrowsForbiddenForBothTheInstanceAndTheSingleVariableCallsAsync`    | Fact   | Permission missing throws forbidden for both the instance and the single variable calls.      |
| `MapsEveryHistogramAndSummaryOfTheVariableAsync`                                      | Fact   | Maps every histogram and summary of the variable.                                             |
| `SingleVariableCallReturnsOnlyThatVariableAndTheInstanceCallReturnsAllAsync`          | Fact   | Single variable call returns only that variable and the instance call returns all.            |
| `VariableFromAnotherInstanceOfTheSameTenantIsNotReturnedAsync`                        | Fact   | Variable from another instance of the same tenant is not returned.                            |
| `TenantsAreIsolatedInBothDirectionsAndForTheSingleVariableCallAsync`                  | Fact   | Tenants are isolated in both directions and for the single variable call.                     |
| `UnknownAndDeletedInstancesAndVariablesAreEmptyAsync`                                 | Fact   | Unknown and deleted instances and variables are empty.                                        |
| `SoftDeletedClassHistogramRowsAreNotReturnedAsync`                                    | Fact   | Soft deleted class histogram rows are not returned.                                           |
| `InstanceWithoutAPromotedTrialHasNoPrescriptionAndOlderInstancesHaveNoClassBinsAsync` | Fact   | Instance without a promoted trial has no prescription and older instances have no class bins. |
| `VariableNotUsedByThePromotedTrialHasNoPrescriptionAsync`                             | Fact   | Variable not used by the promoted trial has no prescription.                                  |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                       | Fact   | Pre cancelled token throws operation canceled.                                                |
| `PermissionDeniedLogsWarnNotErrorAndFailureLogsErrorAsync`                            | Fact   | Permission denied logs warn not error and failure logs error.                                 |
| `EachCallEmitsOneSpanOneAuditLineAndPublishesNothingAsync`                            | Fact   | Each call emits one span one audit line and publishes nothing.                                |
| `CatalogueRegistersTheUniqueToolName`                                                 | Fact   | Catalogue registers the unique tool name.                                                     |

## Icons

### FormModelChildTests

`Jube.Test.Service.Query.FormModel.FormModelChildTests` — 6 tests. Category: Unit.

| Test                                                                          | Kind   | Verifies                                                                                     |
|-------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `TheModelOffersItsChildrenInTheGroupsTheMenuUses`                             | Fact   | The model offers its children in the groups the menu uses.                                   |
| `ACaseWorkflowOffersTheKindsThatSitUnderIt`                                   | Fact   | A case workflow offers the kinds that sit under it.                                          |
| `EveryChildIsAKnownAreaKeyedToItsParentAndListedByARouteThatExists`           | Theory | Every child is a known area keyed to its parent and listed by a route that exists.           |
| `TheHierarchyHasNoCycleAndNoAreaHasTwoParents`                                | Fact   | The hierarchy has no cycle and no area has two parents.                                      |
| `TheModelIsTheRootOfTheDeepestChainAndNoChainIsDeeperThanFiveLevels`          | Fact   | The model is the root of the deepest chain and no chain is deeper than five levels.          |
| `TheChildLabelsAndHubsAreTranslatedWhenARuleSaysSoAndKeepTheEnglishOtherwise` | Fact   | The child labels and hubs are translated when a rule says so and keep the English otherwise. |

### FormModelParityWidgetTests

`Jube.Test.Service.Query.FormModel.FormModelParityWidgetTests` — 3 tests. Category: Unit.

| Test                                                                 | Kind | Verifies                                                                     |
|----------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `ARegistryParameterDefaultValueFollowsItsDataType`                   | Fact | A registry parameter default value follows its data type.                    |
| `ARegistryParameterStartsAsAString`                                  | Fact | A registry parameter starts as a string.                                     |
| `AnExhaustiveAnomalyParametersFieldOffersTheVariableReductionChoice` | Fact | An exhaustive anomaly parameters field offers the variable reduction choice. |

### FormModelChecklistTests

`Jube.Test.Service.Query.FormModel.FormModelChecklistTests` — 5 tests. Category: Unit.

| Test                                                           | Kind | Verifies                                                                     |
|----------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `AStatusOffersItsChecklistsAndAChecklistOffersItsItems`        | Fact | A status offers its checklists and a checklist offers its items.             |
| `TheConditionsOfAnItemAreCodeFieldsRevealedByTheirSwitches`    | Fact | The conditions of an item are code fields revealed by their switches.        |
| `TheShowAndValidationSwitchesAreSwitchesThatOpenTheirOwnCards` | Fact | The show and validation switches are switches that open their own cards.     |
| `ThePriorityOfAChecklistAndOfAnItemIsANumberThatOrdersTheList` | Fact | The priority of a checklist and of an item is a number that orders the list. |
| `EveryGroupAnItemFieldUsesIsDeclared`                          | Fact | Every group an item field uses is declared.                                  |

### FormModelEditorTests

`Jube.Test.Service.Query.FormModel.FormModelEditorTests` — 20 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                                           |
|--------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------|
| `TheDatasourceEditorStandsInForTheFivePropertiesThatMakeUpADatasourceDisplay`  | Fact   | The datasource editor stands in for the five properties that make up a datasource display.                         |
| `TheLegacyKendoDisplayPropertiesAreNoLongerFormFields`                         | Fact   | The legacy kendo display properties are no longer form fields.                                                     |
| `EveryRuleParseTypeEqualsTheParserConstantOfTheSameName`                       | Fact   | Every rule parse type equals the parser constant of the same name.                                                 |
| `TheRuleEditorsReachTheModelWithTheirParseTypeAndRoles`                        | Fact   | The rule editors reach the model with their parse type and roles.                                                  |
| `TheRuleToolsEditorSitsOnTheActivationRuleAndClaimsNoField`                    | Fact   | The rule tools editor sits on the activation rule, names its rule type through the parse type and claims no field. |
| `OnlyTheActivationRuleCarriesTheRuleToolsEditor`                               | Fact   | The activation rule is the only area that carries the rule tools editor.                                           |
| `EveryRuleToolsTypeNamesAModelEntityKindTheDependencyServiceUnderstands`       | Fact   | Every rule tools type names a model entity kind the dependency service understands.                                |
| `ACoderOnlyRuleEditorNamesOnlyTheCoderProperty`                                | Fact   | A coder only rule editor names only the coder property.                                                            |
| `TheCaseFilterEditorClaimsItsSelectAndFilterJson`                              | Fact   | The case filter editor claims its select and filter json.                                                          |
| `TheExhaustiveDatasetEditorClaimsBothDatasetIdsAndOnlyReadsTheSourceAndStatus` | Fact   | The Exhaustive dataset editor claims both dataset ids and only reads the source and status.                        |
| `TheExhaustiveFilterEditorClaimsTheThreeFilterPropertiesAndReadsTheDataSource` | Fact   | The Exhaustive filter editor claims the three filter properties and reads the data source.                         |
| `TheDataSourceAndStatusAnEditorReadsStayOrdinaryFormFields`                    | Fact   | The data source and status an editor reads stay ordinary form fields.                                              |
| `EveryEditorRegistersWithoutAFailure`                                          | Fact   | Every editor registers without a failure.                                                                          |
| `EveryClaimedFieldIsMarkedWithItsEditorAndNoFieldIsClaimedTwice`               | Theory | Every claimed field is marked with its editor and no field is claimed twice.                                       |
| `ABuilderCapableRuleTypeCarriesAllFourRolesAndACoderOnlyTypeJustTheCoder`      | Theory | A builder capable rule type carries all four roles and a coder only type just the coder.                           |
| `AnEditorIsGivenItsContextByTheParentKeyOfItsRecord`                           | Theory | An editor is given its context by the parent key of its record.                                                    |
| `TheAbstractionRuleCarriesTheParametersOfEveryCatalogueFunction`               | Fact   | The abstraction rule carries the parameters of every catalogue function.                                           |
| `OnlyTheAggregationParametersEditorCarriesParameterSets`                       | Fact   | Only the aggregation parameters editor carries parameter sets.                                                     |
| `AFieldLevelEditorAttributeNamesOnlyAComponentTheFormCannotDraw`               | Fact   | A field level editor attribute names only a component the form cannot draw.                                        |
| `EveryFieldEditorIsOneTheRendererKnows`                                        | Theory | Every field editor is one the renderer knows.                                                                      |

### FormModelLocalisationTests

`Jube.Test.Service.Query.FormModel.FormModelLocalisationTests` — 14 tests. Category: Unit.

| Test                                                                        | Kind   | Verifies                                                                                 |
|-----------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `ATranslatedLabelReplacesTheEnglishOneWhereverTheModelShowsText`            | Fact   | A translated label replaces the English one wherever the model shows text.               |
| `AnOptionHelpAndPlaceholderAreTranslatedByTheirOwnKeys`                     | Fact   | An option, help and placeholder are translated by their own keys.                        |
| `AnUntranslatedLabelKeepsTheEnglishFromTheDto`                              | Fact   | An untranslated label keeps the English from the DTO.                                    |
| `LocalisingNeverChangesTheSharedEnglishModel`                               | Fact   | Localising never changes the shared English model.                                       |
| `TheLocalisedModelIsBuiltOncePerCultureAndKeptApartFromOtherCultures`       | Fact   | The localised model is built once per culture and kept apart from other cultures.        |
| `AnUnknownAreaHasNoLocalisedModel`                                          | Fact   | An unknown area has no localised model.                                                  |
| `EveryOptionKeyIsUniqueToItsSetAndMember`                                   | Fact   | Every option key names its enum set and member.                                          |
| `EveryTranslationKeyInTheResourcesNamesALabelTheModelsReallyHave`           | Theory | Every translation key in the resources names a label the models really have.             |
| `TheRealTranslationsReachTheTagModelThroughTheLocalizer`                    | Theory | The real translations reach the tag model through the localizer.                         |
| `AnEnglishRequestGetsTheEnglishLabelsBecauseNoEnglishResourceOverridesThem` | Fact   | An English request gets the English labels because no English resource overrides them.   |
| `ATranslatedDescriptionReplacesTheEnglishOneAndAMissingOneKeepsIt`          | Fact   | A translated description replaces the English one and a missing one keeps it.            |
| `EveryTranslatedTextWasMadeFromTheEnglishItNowHas`                          | Theory | Every translated description or placeholder was made from the English it now has.        |
| `TheUntranslatedTextsAreExactlyThoseInTheBaseline`                          | Theory | The untranslated descriptions and placeholders are exactly those in the baseline.        |
| `ATranslatedPlaceholderReplacesTheEnglishOneAndAFieldWithNoneStaysWithNone` | Fact   | A translated placeholder replaces the English one and a field with none stays with none. |

### FormModelOptionTests

`Jube.Test.Service.Query.FormModel.FormModelOptionTests` — 11 tests. Category: Unit.
| `EveryOptionIsInsideTheValueDomainEveryValidatorRuleAllowsWhateverItsCondition`       | Theory | Every option is
inside the value domain every validator rule allows whatever its condition. |
| `EveryNewDefaultOfAChoiceFieldIsOneOfItsOptions`                                      | Theory | Every new default of
a choice field is one of its options. |
| `OptionValuesAreUniqueAndEveryOptionIsLabelled`                                       | Theory | Option values are
unique and every option is labelled. |
| `EveryWidgetIsInTheClosedVocabulary`                                                  | Theory | Every widget is in
the closed vocabulary. |
| `EveryChoiceFieldHasOptionsOrALookup`                                                 | Theory | Every choice field
has options or a lookup. |
| `EveryOptionSetRegistersWithoutAFailure`                                              | Fact | Every option set
registers without a failure. |
| `TheRealOptionsKeepTheGapsAndTheStoredLettersTheOldPagesUsed`                         | Fact | The real options keep
the gaps and the stored letters the old pages used. |
| `AMemberWithNoStoredTextOnATextPropertyAndAnUnknownAllowedMemberAreRecordedNotThrown` | Fact | A member with no stored
text on a text property and an unknown allowed member are recorded not thrown. |
| `ATextPropertyTakesItsValuesFromEnumMemberAndItsLabelsFromDescription`                | Fact | A text property takes
its values from enum member and its labels from description. |
| `AnAllowedSubsetKeepsOnlyTheNamedMembers`                                             | Fact | An allowed subset keeps
only the named members. |
| `AnEnumGivesNumericChoicesInValueOrderLabelledByDescriptionOrTheHumanisedName`        | Fact | An enum gives numeric
choices in value order labelled by description or the humanised name. |

| Test | Kind | Verifies |
|------|------|----------|

### FormModelRelationTests

`Jube.Test.Service.Query.FormModel.FormModelRelationTests` — 13 tests. Category: Unit.

| Test                                                                             | Kind   | Verifies                                                                                          |
|----------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `EveryRelationResolvesWithoutAFailure`                                           | Fact   | Every relation resolves without a failure.                                                        |
| `AnApprovalTakesItsKindFromTheAreaNameAndItsHistoryFromTheKind`                  | Fact   | An approval takes its kind from the area name and its history from the kind.                      |
| `TheHttpAdaptationAreaIsNamedDifferentlyFromItsApprovalKindAndSaysSo`            | Fact   | The http adaptation area is named differently from its approval kind and says so.                 |
| `TheGraphRecallAreaIsNamedAfterItsApprovalKindAndCarriesItsHistory`              | Fact   | The graph recall area is named after its approval kind and carries its history.                   |
| `TheSvirfdAreaIsNamedAfterItsApprovalKindAndCarriesItsHistory`                   | Fact   | The Svirfd area is named after its approval kind and carries its history.                         |
| `RolesFollowTheAreaNameForTheEndpointAndTheParentKey`                            | Fact   | Roles follow the area name for the endpoint and the parent key.                                   |
| `ValuesNameTheValueAreaTheParentFieldTheEditableValuesAndTheParentApprovalKind`  | Fact   | Values name the value area the parent field the editable values and the parent approval kind.     |
| `NoApprovalKindIsUsedByTwoAreas`                                                 | Fact   | No approval kind is used by two areas.                                                            |
| `EveryApprovedAreaHasItsVersionHistoryAndItsRetractAndResubmitRoutes`            | Theory | Every approved area has its version history and its retract and resubmit routes.                  |
| `EveryApprovalKindWithAVersionHistoryIsMarkedForApprovalOrIsTheRowOfAValuesGrid` | Fact   | Every approval kind with a version history is marked for approval or is the row of a values grid. |
| `EveryRolesAreaHasItsRoleRoutes`                                                 | Theory | Every roles area has its role routes.                                                             |
| `EveryRoleRouteBelongsToAnAreaMarkedForRoles`                                    | Fact   | Every role route belongs to an area marked for roles.                                             |
| `EveryValuesAreaReadsAndWritesItsRowsThroughRoutesThatExist`                     | Theory | Every values area reads and writes its rows through routes that exist.                            |

### RequestLocalisationTests

`Jube.Test.Service.Query.FormModel.RequestLocalisationTests` — 3 tests. Category: Unit.

| Test                                                                                      | Kind   | Verifies                                                                                                  |
|-------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------|
| `TheLanguageTheClientAsksForPicksTheResourceCultureAndAnUnsupportedOneFallsBackToEnglish` | Theory | The language the client asks for picks the resource culture and an unsupported one falls back to English. |
| `TheFormattingCultureStaysEnglishWhateverTheClientAsksFor`                                | Theory | The formatting culture stays English whatever the client asks for.                                        |
| `NoLanguageHeaderGivesEnglish`                                                            | Fact   | No language header gives English.                                                                         |

### FormModelRuleTests

`Jube.Test.Service.Query.FormModel.FormModelRuleTests` — 19 tests. Category: Unit.
| `NoConditionCarriesAnOperatorAmongItsValues`                          | Theory | No condition carries an operator
among its values. |
| `EveryConditionalRequiredRuleIsDeclaredAsARequiredWhenOnTheDto`       | Theory | Every conditional required rule is
declared as a required when on the dto. |
| `EveryRegularExpressionRuleOnAFormFieldIsCarriedToTheModel`           | Theory | Every regular expression rule on a
form field is carried to the model. |
| `EverySliderAndPercentFieldHasARangeFromAValidator`                   | Theory | Every slider and percent field has a
range from a validator. |
| `EveryValidatorOfAFormDtoBuildsWithoutItsServices`                    | Fact | Every validator of a form dto builds
without its services. |
| `TheTagNameIsRequiredAndLimitedToTheValidatorsLength`                 | Fact | The tag name is required and limited to
the validators length. |
| `ARangeFromTheValidatorsReachesTheSliderAndAnUnboundedFieldIsNotOne`  | Fact | A range from the validators reaches the
slider and an unbounded field is not one. |
| `AnIntegerFieldGetsItsExclusiveMinimumAdjustedToTheFirstAllowedValue` | Fact | An integer field gets its exclusive
minimum adjusted to the first allowed value. |
| `AMatchesRuleOnAFieldReachesTheModelAsAPattern`                       | Fact | A matches rule on a field reaches the
model as a pattern. |
| `OnlyAnAnchoredWholeValuePatternWithNoDotNetOnlySyntaxIsTranslated`   | Theory | Only an anchored whole value pattern
with no dot net only syntax is translated. |
| `MergingTwoValidatorsOfTheSameDtoKeepsTheTighterLimitAndAnyRequired`  | Fact | Merging two validators of the same dto
keeps the tighter limit and any required. |
| `TheTightestOfSeveralLimitsWins`                                      | Fact | The tightest of several limits wins. |
| `ARangeUnderAConditionIsStillCarriedBecauseItBoundsTheValueWhenUsed`  | Fact | A range under a condition is still
carried because it bounds the value when used. |
| `GreaterThanAndLessThanOrEqualGiveAMinimumAndAMaximum`                | Fact | Greater than and less than or equal
give a minimum and a maximum. |
| `AnExclusiveBetweenRuleMarksBothLimitsExclusive`                      | Fact | An exclusive between rule marks both
limits exclusive. |
| `AnInclusiveBetweenRuleGivesBothLimits`                               | Fact | An inclusive between rule gives both
limits. |
| `MaximumAndMinimumLengthAreCarriedAcrossEvenUnderACondition`          | Fact | Maximum and minimum length are carried
across even under a condition. |
| `ANotEmptyRuleUnderAConditionIsNotRequiredButIsFlaggedConditional`    | Fact | A not empty rule under a condition is
not required but is flagged conditional. |
| `AnUnconditionalNotEmptyRuleMakesAFieldRequired`                      | Fact | An unconditional not empty rule makes a
field required. |

| Test | Kind | Verifies |
|------|------|----------|

### FormModelRegistryTests

`Jube.Test.Service.Query.FormModel.FormModelRegistryTests` — 21 tests. Category: Unit.

| Test                                                                       | Kind   | Verifies                                                                                    |
|----------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `EveryDtoWithAFormEndpointIsRegisteredUnderItsArea`                        | Fact   | Every dto with a form endpoint is registered under its area.                                |
| `AnAreaIsFoundWhateverTheCase`                                             | Fact   | An area is found whatever the case.                                                         |
| `AnUnknownAreaIsNotFound`                                                  | Fact   | An unknown area is not found.                                                               |
| `TheModelNamesItsEndpointAndKeysInCamelCase`                               | Fact   | The model names its endpoint and keys in camel case.                                        |
| `AnAreaDeclaresAllOperationsUnlessItsDtoSaysOtherwise`                     | Fact   | An area declares all operations unless its dto says otherwise.                              |
| `GroupsAreOrderedAndCarryTheirCollapsedState`                              | Fact   | Groups are ordered and carry their collapsed state.                                         |
| `FieldsAreOrderedByGroupThenPositionAndOnlyDecoratedPropertiesAppear`      | Fact   | Fields are ordered by group then position and only decorated properties appear.             |
| `AFieldCarriesItsTypeWidgetDefaultAndDescription`                          | Fact   | A field carries its type widget default and description.                                    |
| `ATypeAndItsNullabilityAreReadFromTheProperty`                             | Theory | A type and its nullability are read from the property.                                      |
| `ALabelIsTheAttributeLabelOrTheHumanisedPropertyName`                      | Fact   | A label is the attribute label or the humanised property name.                              |
| `ListColumnsCarryTheirTitleOrderAndHiddenColumnsComeLast`                  | Fact   | List columns carry their title order and hidden columns come last.                          |
| `ALookupIsDescribedWithItsSourceAndFields`                                 | Fact   | A lookup is described with its source and fields.                                           |
| `EveryFieldNamesAGroupThatIsDeclared`                                      | Theory | Every field names a group that is declared.                                                 |
| `NoFieldNameOrListColumnIsDuplicated`                                      | Theory | No field name or list column is duplicated.                                                 |
| `EveryConditionAndKeyNamesAFieldOfTheSameRecord`                           | Theory | Every condition and key names a field of the same record.                                   |
| `AConditionKeepsItsOperatorValuesAndMatchMode`                             | Fact   | A condition keeps its operator values and match mode.                                       |
| `NoFieldLabelShowsAnIdentifierWordTheUserHasNoUseFor`                      | Theory | No field label shows an identifier word the user has no use for.                            |
| `EveryFieldLabelIsUniqueWithinItsArea`                                     | Theory | Every field label is unique within its area.                                                |
| `NoRadioSetOffersMoreThanThreeChoicesAndEverySpanIsOneOrTwo`               | Theory | No radio set offers more than three choices and every span is one or two.                   |
| `ARadioSetWithMoreThanThreeChoicesIsDrawnAsASelectAndTwoOrThreeStayRadios` | Fact   | A radio set with more than three choices is drawn as a select and two or three stay radios. |
| `AFieldAskingForTwoColumnsKeepsThemAndTheDefaultIsOne`                     | Fact   | A field asking for two columns keeps them and the default is one.                           |

### FormModelRouteTests

`Jube.Test.Service.Query.FormModel.FormModelRouteTests` — 6 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                          |
|--------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `EveryEndpointMapperRegistersWithoutNeedingAnyService`                         | Fact   | Every endpoint mapper registers without needing any service.                                      |
| `TheCatalogueSeesTheFormModelRoutesItself`                                     | Fact   | The catalogue sees the form model routes itself.                                                  |
| `EveryFormAreaHasTheRoutesForTheOperationsItDeclares`                          | Theory | Every form area has the routes for the operations it declares.                                    |
| `EveryFormAreaDeclaresAtLeastOneOperation`                                     | Theory | Every form area declares at least one operation.                                                  |
| `EveryLookupSourceIsARouteThatExists`                                          | Theory | Every lookup source is a route that exists.                                                       |
| `EveryLookupWithAParentTakesItAsTheLastSegmentOfARouteAndNamesAFieldOfTheArea` | Theory | Every lookup with a parent takes it as the last segment of a route and names a field of the area. |

### GridModelRegistryTests

`Jube.Test.Service.Query.GridModel.GridModelRegistryTests` — 7 tests. Category: Unit.

| Test                                                                                   | Kind   | Verifies                                                                                                |
|----------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------|
| `EveryDecoratedGridRegistersWithoutAFailure`                                           | Fact   | Every decorated grid registers without a failure.                                                       |
| `TheArchiverStageGridCarriesItsTimeWindowStageFilterRatioAndOrder`                     | Fact   | The archiver stage grid carries its time window, stage filter, ratio and order.                         |
| `ASearchOnlyGridHasASearchBoxAndAWindow`                                               | Fact   | A search only grid has a search box and a window.                                                       |
| `ASnapshotGridHasNoWindowAndNamesItsSort`                                              | Fact   | A snapshot grid has no window and names its sort.                                                       |
| `EveryGridEndpointIsARouteThatTakesTheParametersTheGridWillSend`                       | Theory | Every grid endpoint is a route that takes the parameters the grid will send.                            |
| `EveryGridColumnHasATitleAndNoTwoColumnsShareAField`                                   | Theory | Every grid column has a title and no two columns share a field.                                         |
| `TheGridTitlesColumnsAndFiltersAreTranslatedWhenARuleSaysSoAndKeepTheEnglishOtherwise` | Fact   | The grid titles, columns and filters are translated when a rule says so and keep the English otherwise. |

### IconsServiceTests

`Jube.Test.Service.Query.Icons.IconsServiceTests` — 15 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind   | Verifies                                                                              |
|-----------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                  | Theory | Create with null or blank user name throws not authenticated.                         |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                   | Fact   | Create with user having no tenant throws not authenticated.                           |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                          | Fact   | Create with unknown user throws not authenticated.                                    |
| `MissingPermissionThrowsForbiddenWithCodeAndRequiredSpecificationsAsync`    | Fact   | Missing permission throws forbidden with code and required specifications.            |
| `ForbiddenDoesNotTouchTheFileSystemAsync`                                   | Fact   | Forbidden does not touch the file system.                                             |
| `ReturnsOneDtoPerFileNameInSourceOrderAsync`                                | Fact   | Returns one dto per file name in source order.                                        |
| `EmptySourceReturnsEmptyListAsync`                                          | Fact   | Empty source returns empty list.                                                      |
| `WebRootSourceListsOnlyFilesOfIconsDirectoryAsNamesAsync`                   | Fact   | Web root source lists only files of icons directory as names.                         |
| `WebRootSourceWithMissingIconsDirectoryPropagatesAndLogsErrorAsync`         | Fact   | Web root source with missing icons directory propagates and logs error.               |
| `ResultIsTheSameForTenantAAndTenantBUsersBecauseIconsAreNotTenantDataAsync` | Fact   | Result is the same for tenant a and tenant b users because icons are not tenant data. |
| `PreCancelledTokenThrowsOperationCanceledAsync`                             | Fact   | Pre cancelled token throws operation canceled.                                        |
| `PermissionDeniedLogsWarnNotErrorAsync`                                     | Fact   | Permission denied logs warn not error.                                                |
| `EachCallEmitsOneSpanWithOutcomeTagAndExactlyOneAuditLineAsync`             | Fact   | Each call emits one span with outcome tag and exactly one audit line.                 |
| `ReadsAndFailuresPublishNothingAsync`                                       | Fact   | Reads and failures publish nothing.                                                   |
| `CatalogueRegistersUniqueToolName`                                          | Fact   | Catalogue registers unique tool name.                                                 |

## Model field reflection

### ModelFieldReflectionActiveTests

`Jube.Test.Service.Query.ModelFieldReflection.ModelFieldReflectionActiveTests` — 3 tests. Category: Service. Collection:
Database.

| Test                                                                 | Kind | Verifies                                                                   |
|----------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `EveryKindOfInactiveItemIsNotListedAndReactivationListsItAgainAsync` | Fact | Every kind of inactive item is not listed and reactivation lists it again. |
| `DeactivatingEveryKindLeavesNoConfiguredItemListedAsync`             | Fact | Deactivating every kind leaves no configured item listed.                  |
| `InactiveListsAreNotListedAndReactivationListsThemAgainAsync`        | Fact | Inactive lists are not listed and reactivation lists them again.           |

## Parser

### ParserServiceTests

`Jube.Test.Service.Query.Parser.ParserServiceTests` — 22 tests. Category: Service. Collection: Database.

| Test                                                                           | Kind   | Verifies                                                                                 |
|--------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `NullOrBlankUserIsNotAuthenticatedAsync`                                       | Theory | Null or blank user is not authenticated.                                                 |
| `UnmappedAndNoTenantUsersAreNotAuthenticatedAsync`                             | Fact   | Unmapped and no tenant users are not authenticated.                                      |
| `MissingPermissionIsForbiddenWithCodeAndSpecsAsync`                            | Fact   | Missing permission is forbidden with code and specs.                                     |
| `AnySingleSpecificationOfTheSetPermitsAsync`                                   | Theory | Any single specification of the set permits.                                             |
| `UnrelatedSpecificationDoesNotPermitAsync`                                     | Fact   | Unrelated specification does not permit.                                                 |
| `ValidRuleCompilesAsync`                                                       | Fact   | Valid rule compiles.                                                                     |
| `UnknownFieldReturnsErrorWithLocatedSpansAsync`                                | Fact   | Unknown field returns error with located spans.                                          |
| `CompilerErrorsAreReturnedWithMessageAndSpansAsync`                            | Fact   | Compiler errors are returned with message and spans.                                     |
| `EmptyRuleTextIsHandledWithoutThrowingAsync`                                   | Fact   | Empty rule text is handled without throwing.                                             |
| `NullRuleTextIsSwallowedIntoErrorResultAsync`                                  | Fact   | Null rule text is swallowed into error result.                                           |
| `ForeignTenantModelFieldsAreNotVisibleToTheParserAsync`                        | Fact   | Foreign tenant model fields are not visible to the parser.                               |
| `SameRuleCompilesForOwnerTenantButNotForeignTenantAsync`                       | Fact   | Same rule compiles for owner tenant but not foreign tenant.                              |
| `ForeignTenantInlineFunctionIsNotVisibleAsync`                                 | Fact   | Foreign tenant inline function is not visible.                                           |
| `InlineFunctionIsResolvedByTheParserAsync`                                     | Fact   | Inline function is resolved by the parser.                                               |
| `InlineScriptPublicPropertiesAreResolvedByTheParserAsync`                      | Fact   | Inline script public properties are resolved by the parser.                              |
| `HttpAndExhaustiveAdaptationsAreVisibleForActivationRulesAsync`                | Fact   | Http and exhaustive adaptations are visible for activation rules.                        |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                | Fact   | Pre cancelled token throws operation canceled.                                           |
| `AuditLogGetsExactlyOneLineAndNoChangeEventAsync`                              | Fact   | Audit log gets exactly one line and no change event.                                     |
| `ToolCatalogueListsParserParseOnce`                                            | Fact   | Tool catalogue lists parser parse once.                                                  |
| `AUserWhoParsesMoreOftenThanTheLimitIsRateLimitedWhileAnotherTenantIsNotAsync` | Fact   | A user who parses more often than the limit is rate limited while another tenant is not. |
| `ATenantsUsersShareTheTenantLimitAsync`                                        | Fact   | A tenants users share the tenant limit.                                                  |
| `TheRateLimitRefusalIsWordedByTheResourcesInTheCallersLanguageAsync`           | Fact   | The rate limit refusal is worded by the resources in the callers language.               |

### ParseRateLimiterTests

`Jube.Test.Service.Query.Parser.ParseRateLimiterTests` — 3 tests. Category: Unit.

| Test                                                           | Kind | Verifies                                                                   |
|----------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `TheCallPastTheLimitIsRefusedUntilTheWindowHasPassed`          | Fact | The call past the limit is refused until the window has passed.            |
| `KeysAreCountedSeparately`                                     | Fact | Keys are counted separately.                                               |
| `ManyDistinctKeysNeverGrowTheTableWithoutBoundOrRefuseANewKey` | Fact | Many distinct keys never grow the table without bound or refuse a new key. |

## Ready

### ReadyServiceTests

`Jube.Test.Service.Query.Ready.ReadyServiceTests` — 5 tests. Category: Service.

| Test                                                         | Kind   | Verifies                                                           |
|--------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `IsReady_MatchesLegacyDecision`                              | Theory | Is ready matches legacy decision.                                  |
| `IsReady_EngineDisabled_DoesNotConsultEngine`                | Fact   | Is ready engine disabled does not consult engine.                  |
| `IsReady_WatcherStreamDisabled_DoesNotConsultWatcherStream`  | Fact   | Is ready watcher stream disabled does not consult watcher stream.  |
| `IsReady_Stopping_ShortCircuitsBeforeEngineAndWatcherStream` | Fact   | Is ready stopping short circuits before engine and watcher stream. |
| `IsReady_EngineNotReady_ShortCircuitsBeforeWatcherStream`    | Fact   | Is ready engine not ready short circuits before watcher stream.    |

## Rule vocabulary

### RuleVocabularyServiceTests

`Jube.Test.Service.Query.RuleVocabulary.RuleVocabularyServiceTests` — 7 tests. Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                            |
|-------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `FunctionsCanBeFoundByNameAndByTheTypeTheyAreCalledOnAsync`                   | Fact | Functions can be found by name and by the type they are called on.                  |
| `PagingContinuesAfterTheLastNameAsync`                                        | Fact | Paging continues after the last name.                                               |
| `DescribeGivesEverySignatureAndIgnoresCaseAsync`                              | Fact | Describe gives every signature and ignores case.                                    |
| `TheWholeVocabularyOffersTheCursorWordsOnlyWhenAskedForAbstractionRulesAsync` | Fact | The whole vocabulary offers the cursor words only when asked for abstraction rules. |
| `TheAgentToolsOfferTheCursorWordsWhenToldTheRuleIsAnAbstractionRuleAsync`     | Fact | The agent tools offer the cursor words when told the rule is an abstraction rule.   |
| `DescribeCarriesTheDescriptionOfTheFunctionEachOverloadAndEachParameterAsync` | Fact | Describe carries the description of the function, each overload and each parameter. |
| `NoPermissionIsForbiddenAsync`                                                | Fact | No permission is forbidden.                                                         |

## Service change stream

### ServiceChangeStreamAuthorizationServiceTests

`Jube.Test.Service.Query.ServiceChangeStream.ServiceChangeStreamAuthorizationServiceTests` — 6 tests. Category: Service.
Collection: Database.

| Test                                                       | Kind   | Verifies                                                      |
|------------------------------------------------------------|--------|---------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync` | Theory | Create with null or blank user name throws not authenticated. |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`         | Fact   | Create with unknown user throws not authenticated.            |
| `TenantlessUserThrowsNotAuthenticatedAsync`                | Fact   | Tenantless user throws not authenticated.                     |
| `AUserWithNoFeaturePermissionIsStillPermittedAsync`        | Fact   | A user with no feature permission is still permitted.         |
| `PermittedUserResolvesToTheirOwnTenantAsync`               | Fact   | Permitted user resolves to their own tenant.                  |
| `DifferentTenantsResolveToDifferentTenantIdsAsync`         | Fact   | Different tenants resolve to different tenant ids.            |

## Tree children

### TreeChildrenPendingColourTests

`Jube.Test.Service.Query.TreeChildren.TreeChildrenPendingColourTests` — 5 tests. Category: Service. Collection:
Database.

| Test                                                                     | Kind | Verifies                                                                         |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `APendingRequestXPathIsOrangeForACallerWhoMayViewPendingApprovalsAsync`  | Fact | A pending request x path is orange for a caller who may view pending approvals.  |
| `ANonPendingRequestXPathKeepsItsActiveColourAsync`                       | Fact | A non pending request x path keeps its active colour.                            |
| `APendingDeletionOfARequestXPathIsOrangeAndStaysInTheTreeAsync`          | Fact | A pending deletion of a request x path is orange and stays in the tree.          |
| `AnApprovedDeletionOfARequestXPathLeavesTheTreeAsync`                    | Fact | An approved deletion of a request x path leaves the tree.                        |
| `ARejectedRequestXPathIsPurpleForACallerWhoMayViewPendingApprovalsAsync` | Fact | A rejected request x path is purple for a caller who may view pending approvals. |

### TreeChildrenServiceTests

`Jube.Test.Service.Query.TreeChildren.TreeChildrenServiceTests` — 22 tests. Category: Service. Collection: Database.

| Test                                                                                 | Kind   | Verifies                                                                                       |
|--------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `NodeTableMatchesTheLegacyRoutesKeysAndPermissionSets`                               | Fact   | Node table matches the legacy routes keys and permission sets.                                 |
| `EveryOperationHasAUniqueToolNameRegisteredInTheCatalogue`                           | Fact   | Every operation has a unique tool name registered in the catalogue.                            |
| `EndpointsExposeExactlyTheLegacyRoutesAsAuthorisedGetsWithTheLegacyParameterNames`   | Fact   | Endpoints expose exactly the legacy routes as authorised gets with the legacy parameter names. |
| `DtoJsonShapesAreUnchanged`                                                          | Fact   | Dto json shapes are unchanged.                                                                 |
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                           | Theory | Create with null or blank user name throws not authenticated.                                  |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                            | Fact   | Create with user having no tenant throws not authenticated.                                    |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                   | Fact   | Create with unknown user throws not authenticated.                                             |
| `EveryNodeThrowsForbiddenWithItsOwnRequiredSpecificationsWhenPermissionMissingAsync` | Theory | Every node throws forbidden with its own required specifications when permission missing.      |
| `EveryNodeIsForbiddenForARoleHoldingOnlyAnUnrelatedPermissionAsync`                  | Theory | Every node is forbidden for a role holding only an unrelated permission.                       |
| `EveryNodeIsAllowedWhenTheCallerHoldsAnyOneOfItsPermissionsAsync`                    | Theory | Every node is allowed when the caller holds any one of its permissions.                        |
| `EveryNodeReturnsSeededChildrenMappedFieldByFieldAsync`                              | Theory | Every node returns seeded children mapped field by field.                                      |
| `EveryDtoShapeIsCoveredByAtLeastOneNode`                                             | Fact   | Every dto shape is covered by at least one node.                                               |
| `ModelNodesFillTheModelIdAndListNodesFillTheModelGuidAsync`                          | Fact   | Model nodes fill the model id and list nodes fill the model guid.                              |
| `UserRegistryChildrenAreTheUsersOfTheRoleAsync`                                      | Fact   | User registry children are the users of the role.                                              |
| `UnknownOrMissingParentReturnsAnEmptyListAsync`                                      | Theory | Unknown or missing parent returns an empty list.                                               |
| `EveryNodeIsTenantIsolatedInBothDirectionsAsync`                                     | Theory | Every node is tenant isolated in both directions.                                              |
| `IdOrderedNodesReturnAscendingIdsAsync`                                              | Fact   | Id ordered nodes return ascending ids.                                                         |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                      | Fact   | Pre cancelled token throws operation canceled.                                                 |
| `PermissionDeniedLogsWarnNotErrorAsync`                                              | Fact   | Permission denied logs warn not error.                                                         |
| `AuditLogGetsExactlyOneLinePerCallAsync`                                             | Fact   | Audit log gets exactly one line per call.                                                      |
| `ReadsAndFailuresPublishNothingAsync`                                                | Fact   | Reads and failures publish nothing.                                                            |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                               | Fact   | Permission denied message resolves for french culture.                                         |

## Visualisation registry datasource command execution

### VisualisationRegistryDatasourceCommandExecutionServiceTests

`Jube.Test.Service.Query.VisualisationRegistryDatasourceCommandExecution.VisualisationRegistryDatasourceCommandExecutionServiceTests` —
41 tests. Category: Service. Collection: Database.

| Test                                                                      | Kind   | Verifies                                                                      |
|---------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------|
| `NullOrBlankUserThrowsNotAuthenticatedAsync`                              | Theory | Null or blank user throws not authenticated.                                  |
| `UnmappedAndNoTenantUsersThrowNotAuthenticatedAsync`                      | Fact   | Unmapped and no tenant users throw not authenticated.                         |
| `MissingPermissionThrowsForbiddenWithSpecsAndCodeAndExecutesNothingAsync` | Fact   | Missing permission throws forbidden with specs and code and executes nothing. |
| `StringIsPassedThroughAsTextAsync`                                        | Fact   | String is passed through as text.                                             |
| `IntegerIsParsedToInt32Async`                                             | Fact   | Integer is parsed to int32.                                                   |
| `IntegerJsonNumberIsParsedTooAsync`                                       | Fact   | Integer json number is parsed too.                                            |
| `DoubleIsParsedToFloat8Async`                                             | Fact   | Double is parsed to float8.                                                   |
| `ByteIsParsedAsync`                                                       | Fact   | Byte is parsed.                                                               |
| `DateWithoutLocalAssumptionIsTreatedAsUtcAsync`                           | Fact   | Date without local assumption is treated as utc.                              |
| `DateWithLocalAssumptionUsesLocalOffsetAsync`                             | Fact   | Date with local assumption uses local offset.                                 |
| `DateWithExplicitOffsetIsNormalisedToUtcRegardlessOfAssumptionAsync`      | Fact   | Date with explicit offset is normalised to utc regardless of assumption.      |
| `UnparseableDateFallsBackToNowAsync`                                      | Fact   | Unparseable date falls back to now.                                           |
| `MissingValueFallsBackToDefaultPerTypeAsync`                              | Fact   | Missing value falls back to default per type.                                 |
| `MissingDateValueDefaultsToDaysAgoFromNowAsync`                           | Fact   | Missing date value defaults to days ago from now.                             |
| `UnknownDataTypeIdIsTreatedAsStringAsync`                                 | Fact   | Unknown data type id is treated as string.                                    |
| `ParameterNameSpacesBecomeUnderscoresAsync`                               | Fact   | Parameter name spaces become underscores.                                     |
| `UnparseableNumericValueThrowsFormatOrOverflowAndWritesNoLogAsync`        | Theory | Unparseable numeric value throws format or overflow and writes no log.        |
| `NullParametersThrowsArgumentNullAsync`                                   | Fact   | Null parameters throws argument null.                                         |
| `DuplicateParameterIdsThrowAsync`                                         | Fact   | Duplicate parameter ids throw.                                                |
| `ParameterWithoutIdThrowsAsync`                                           | Fact   | Parameter without id throws.                                                  |
| `ReturnsDynamicColumnsAndRowsWithRawValuesAsync`                          | Fact   | Returns dynamic columns and rows with raw values.                             |
| `EmptyResultReturnsEmptyListAndLogsZeroRecordsAsync`                      | Fact   | Empty result returns empty list and logs zero records.                        |
| `SuccessWritesExecutionLogAndParameterRowsAsync`                          | Fact   | Success writes execution log and parameter rows.                              |
| `FailingCommandIsCapturedAsErrorLoggedAndReturnsNoRowsAsync`              | Fact   | Failing command is captured as error logged and returns no rows.              |
| `NonSelectCommandIsRejectedByParserAndLoggedAsErrorAsync`                 | Fact   | Non select command is rejected by parser and logged as error.                 |
| `ParameterWithoutValueIsLoggedWithNullValueAsync`                         | Fact   | Parameter without value is logged with null value.                            |
| `EachExecutionWritesItsOwnLogRowAsync`                                    | Fact   | Each execution writes its own log row.                                        |
| `TenantBUserCannotExecuteTenantADatasourceEvenWithRoleGrantAsync`         | Fact   | Tenant b user cannot execute tenant a datasource even with role grant.        |
| `TenantAUserCannotExecuteTenantBDatasourceAsync`                          | Fact   | Tenant a user cannot execute tenant b datasource.                             |
| `ForeignTenantParameterIdsAreIgnoredForCoercionAsync`                     | Fact   | Foreign tenant parameter ids are ignored for coercion.                        |
| `DatasourceWithoutRoleGrantForUserReturnsNothingAsync`                    | Fact   | Datasource without role grant for user returns nothing.                       |
| `InactiveDatasourceReturnsNothingAsync`                                   | Fact   | Inactive datasource returns nothing.                                          |
| `PreCancelledTokenThrowsOperationCanceledAsync`                           | Fact   | Pre cancelled token throws operation canceled.                                |
| `PermissionDeniedLogsWarnNotErrorAsync`                                   | Fact   | Permission denied logs warn not error.                                        |
| `BadParameterLogsErrorWithExceptionAndPropagatesAsync`                    | Fact   | Bad parameter logs error with exception and propagates.                       |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                 | Fact   | Each call emits one span with outcome tag.                                    |
| `EachCallRecordsOneDurationMeasurementAsync`                              | Fact   | Each call records one duration measurement.                                   |
| `AuditLogGetsExactlyOneLineAsync`                                         | Fact   | Audit log gets exactly one line.                                              |
| `ExecutionPublishesNoChangeEventAsync`                                    | Fact   | Execution publishes no change event.                                          |
| `CatalogueDescriptorHasUniqueNameAndOperationAttributeMatches`            | Fact   | Catalogue descriptor has unique name and operation attribute matches.         |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                    | Fact   | Permission denied message resolves for french culture.                        |

### VisualisationRegistryDatasourceCardServiceTests

`Jube.Test.Service.Query.VisualisationRegistryDatasourceCommandExecution.VisualisationRegistryDatasourceCardServiceTests` —
22 tests. Category: Service. Collection: Database.

| Test                                                                           | Kind | Verifies                                                                                    |
|--------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `ASavedChartComesBackAsAnSvgWithTheCardIdentityAndNoGridWhenTheGridIsOffAsync` | Fact | A saved chart comes back as an svg with the card identity and no grid when the grid is off. |
| `WithTheGridOnTheRowsComeAlongAndWithNoChartKindThereIsNoDisplayAsync`         | Fact | With the grid on the rows come along and with no chart kind there is no display.            |
| `StoredColumnRolesChooseTheSeriesAsync`                                        | Fact | Stored column roles choose the series.                                                      |
| `TheStylesheetIsOnlySentToACallerWhoDoesNotAlreadyHoldItsVersionAsync`         | Fact | The stylesheet is only sent to a caller who does not already hold its version.              |
| `ARegistryShownInACaseReceivesTheCaseIdAsItsIdAsync`                           | Fact | A registry shown in a case receives the case id as its id.                                  |
| `ACaseOfAnotherTenantIsNotReachableThroughTheIdAsync`                          | Fact | A case of another tenant is not reachable through the id.                                   |
| `TheDirectoryHasNoIdWhateverIsSuppliedAsync`                                   | Fact | The directory has no id whatever is supplied.                                               |
| `EveryCardIsWrittenToTheExecutionLogWithItsRowsAndParametersAsync`             | Fact | Every card is written to the execution log with its rows and parameters.                    |
| `AFailingQueryIsAnErrorCardAndIsLoggedWithItsErrorAsync`                       | Fact | A failing query is an error card and is logged with its error.                              |
| `AQueryWithNoRowsIsEmptyAsync`                                                 | Fact | A query with no rows is empty.                                                              |
| `RowsOverTheLimitAreFlaggedAndTheLogRecordsWhatWasReadAsync`                   | Fact | Rows over the limit are flagged and the log records what was read.                          |
| `AValueOverTheCellByteLimitIsAnErrorCardThatNamesTheColumnAsync`               | Fact | A value over ReportMaximumCellBytes is an error card naming the column.                     |
| `AResultOverTheTotalByteLimitIsAnErrorCardAsync`                               | Fact | A result over ReportMaximumResultBytes is an error card.                                    |
| `TheCardNeverCarriesTheSqlAsync`                                               | Fact | The card never carries the sql.                                                             |
| `ADatasourceTheCallerCannotSeeIsNotFoundAndNotLoggedAsync`                     | Fact | A datasource the caller cannot see is not found and not logged.                             |
| `AnInactiveDatasourceIsNotFoundAsync`                                          | Fact | An inactive datasource is not found.                                                        |
| `AnUnknownDatasourceIsNotFoundAsync`                                           | Fact | An unknown datasource is not found.                                                         |
| `ACallerWithoutThePermissionIsForbiddenAsync`                                  | Fact | A caller without the permission is forbidden.                                               |
| `AParameterValueOfTheWrongTypeOrOfAnotherRegistryOrTwiceIsRefusedAsync`        | Fact | A parameter value of the wrong type or of another registry or twice is refused.             |
| `AnEmptyParameterEntryIsRefusedNotACrashAsync`                                 | Fact | An empty parameter entry is refused not a crash.                                            |
| `ACardExplainsItselfInThePageLanguageAsync`                                    | Fact | A card explains itself in the page language.                                                |
| `AChartThatCannotBeDrawnIsAnErrorCardButTheGridStillComesAsync`                | Fact | A chart that cannot be drawn is an error card but the grid still comes.                     |

## Watcher stream

### WatcherStreamAuthorizationServiceTests

`Jube.Test.Service.Query.WatcherStream.WatcherStreamAuthorizationServiceTests` — 9 tests. Category: Service. Collection:
Database.

| Test                                                                | Kind   | Verifies                                                                 |
|---------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`          | Theory | Create with null or blank user name throws not authenticated.            |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                  | Fact   | Create with unknown user throws not authenticated.                       |
| `TenantlessUserThrowsNotAuthenticatedAsync`                         | Fact   | Tenantless user throws not authenticated.                                |
| `MissingPermissionThrowsForbiddenAsync`                             | Fact   | Missing permission throws forbidden.                                     |
| `PermittedUserResolvesToTheirOwnTenantAsync`                        | Fact   | Permitted user resolves to their own tenant.                             |
| `DifferentTenantsResolveToDifferentTenantIdsAsync`                  | Fact   | Different tenants resolve to different tenant ids.                       |
| `StillAuthorisedIsTrueForTheTenantTheStreamWasOpenedForAsync`       | Fact   | Still authorised is true for the tenant the stream was opened for.       |
| `StillAuthorisedIsFalseWhenTheUserNowResolvesToAnotherTenantAsync`  | Fact   | Still authorised is false when the user now resolves to another tenant.  |
| `StillAuthorisedIsFalseWithoutThePermissionOrForAnUnknownUserAsync` | Fact   | Still authorised is false without the permission or for an unknown user. |

## Entity analysis models synchronisation schedules by instance name

### GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryTests

`Jube.Test.Service.Query.EntityAnalysisModelsSynchronisationSchedulesByInstanceName.GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryTests` —
8 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                          |
|-----------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `AWholeTenantRowThatIsDueIsPendingForEveryModelAsync`                       | Fact | A whole tenant row that is due is pending for every model.                        |
| `ModelRowsThatAreDueNameOnlyThoseModelsAndEachOnceAsync`                    | Fact | Model rows that are due name only those models and each once.                     |
| `AWholeTenantRowAlongsideAModelRowWidensTheSyncToEveryModelAsync`           | Fact | A whole tenant row alongside a model row widens the sync to every model.          |
| `ARowOlderThanTheNodesLastSynchronisationIsNotPendingAsync`                 | Fact | A row older than the nodes last synchronisation is not pending.                   |
| `ARowThatIsNotYetDueIsNotPendingAsync`                                      | Fact | A row that is not yet due is not pending.                                         |
| `ANodeThatHasNeverSynchronisedHonoursEveryRowThatIsDueAsync`                | Fact | A node that has never synchronised honours every row that is due.                 |
| `ConfirmingWithTheObservedDateLeavesARowScheduledDuringTheSyncPendingAsync` | Fact | Confirming with the observed date leaves a row scheduled during the sync pending. |
| `ConfirmingWithTheObservedDateHonoursEveryRowThatWasPendingAsync`           | Fact | Confirming with the observed date honours every row that was pending.             |

## Visualisation registry summary

### GetVisualisationRegistrySummaryQueryTests

`Jube.Test.Service.Query.VisualisationRegistrySummary.GetVisualisationRegistrySummaryQueryTests` — 5 tests. Category:
Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ARegistryWithNothingRecordedStillGetsARowOfZerosAsync`                     | Fact | A registry with nothing recorded still gets a row of zeros.                        |
| `OnlyActiveUndeletedDatasourcesAndParametersAreCountedAsync`                | Fact | Only active undeleted datasources and parameters are counted.                      |
| `ExecutionsFailuresAndAverageCoverTheWindowButTheLastRunCoversAllTimeAsync` | Fact | Executions failures and average cover the window but the last run covers all time. |
| `ARegistryWhoseOnlyRunsAreOlderThanTheWindowStillShowsWhenItLastRanAsync`   | Fact | A registry whose only runs are older than the window still shows when it last ran. |
| `NoRegistriesMeansNoQueryAsync`                                             | Fact | No registries means no query.                                                      |

## Case narrative

### CaseNarrativeJobTests

`Jube.Test.Service.Query.CaseNarrative.CaseNarrativeJobTests` — 14 tests. Category: Service. Collection: Database.

| Test                                                                   | Kind | Verifies                                                                                                                      |
|------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------------------------|
| `AnOpenCaseInAWorkflowThatOptedInGetsANarrativeWrittenByAskJoobyAsync` | Fact | An open case in a workflow that opted in gets a narrative written by ask jooby.                                               |
| `ANarrativeIsNotRewrittenWhileTheFactsAreUnchangedAsync`               | Fact | A narrative is not rewritten while the facts are unchanged.                                                                   |
| `ANewAnalystNoteWritesTheNextVersionAndLeavesTheNoteAloneAsync`        | Fact | A new analyst note writes the next version and leaves the note alone.                                                         |
| `AChangeOfStatusIsAChangeOfFactsAsync`                                 | Fact | A change of status is a change of facts.                                                                                      |
| `AWorkflowThatHasNotOptedInIsLeftAloneAsync`                           | Fact | A workflow that has not opted in is left alone.                                                                               |
| `AClosedCaseIsLeftAloneAsync`                                          | Fact | A closed case is left alone.                                                                                                  |
| `ACaseOfAnotherTenantIsNotNarratedAsTheUserOfThisOneAsync`             | Fact | A case of another tenant is not narrated as the user of this one.                                                             |
| `NotesOfAnotherTenantWithTheSameKeyValueAreNotCountedInTheFactsAsync`  | Fact | A note in another tenant that shares the key value is not counted in the narrative facts, so it cannot change the facts hash. |
| `NothingIsWrittenWhenNoUserIsConfiguredAsync`                          | Fact | Nothing is written when no user is configured.                                                                                |
| `ASilentAgentWritesNothingAndTheCaseIsTriedAgainNextCycleAsync`        | Fact | A silent agent writes nothing and the case is tried again next cycle.                                                         |
| `ABlankAnswerWritesNothingAsync`                                       | Fact | A blank answer writes nothing.                                                                                                |
| `ACaseKeyThatIsNotPlainTextNeverReachesTheAgentAsync`                  | Fact | A case key that is not plain text never reaches the agent.                                                                    |
| `ACaseAnotherNodeIsWorkingOnIsSkippedAsync`                            | Fact | A case another node is working on is skipped.                                                                                 |
| `OnlyTheConfiguredNumberOfCasesIsNarratedInACycleAsync`                | Fact | Only the configured number of cases is narrated in a cycle.                                                                   |
| `TheNarrativePromptCarriesNoPayloadTextAndAsksForBehaviourOnlyAsync`   | Fact | The narrative prompt carries no payload text and asks for behaviour only.                                                     |

### CaseNarrativeUnitTests

`Jube.Test.Service.Query.CaseNarrative.CaseNarrativeUnitTests` — 3 tests. Category: Unit.

| Test                                                 | Kind   | Verifies                                                      |
|------------------------------------------------------|--------|---------------------------------------------------------------|
| `OnlyPlainTextKeysAreSentToTheAgent`                 | Theory | Only plain text keys are sent to the agent.                   |
| `TheFactsHashChangesWithAnyFactAndIsStableOtherwise` | Fact   | The facts hash changes with any fact and is stable otherwise. |
| `ThePageContextNamesTheCasePairAndNothingElse`       | Fact   | The page context names the case pair and nothing else.        |

## Graph

### GraphBuildJobTests

`Jube.Test.Service.Query.Graph.GraphBuildJobTests` — 3 tests.
Category: Service. Collection: Database.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `BuildsNodesEdgesAndEventsFromArchiveKeysAndIsIdempotentAsync` | Fact | Builds nodes edges and events from archive keys and is idempotent. |
| `RetractsAndRebuildsAReprocessedTransactionAsync`              | Fact | Retracts and rebuilds areprocessed transaction.                    |
| `FlagsAHubAboveTheDefinitionThresholdAndReweightsEdgesAsync`   | Fact | Flags ahub above the definition threshold and reweights edges.     |

### GraphServiceTests

`Jube.Test.Service.Query.Graph.GraphServiceTests` — 15 tests.
Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                                                              |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------------------|
| `ReturnsTheRingAroundTheSeedWithEvidenceAndExplodeAgreesAsync`                | Fact | Returns the ring around the seed with evidence and explode agrees.                                                    |
| `RenderingAKeyValueWithNoNetworkIsNotFoundRatherThanAServerErrorAsync`        | Fact | Rendering a key value with no network is not found rather than a server error.                                        |
| `RendersDeterministicallyWithPositionsAndCapturesAHashedPairOfFilesAsync`     | Fact | Renders deterministically with positions and captures a hashed pair of files.                                         |
| `SavesListsOverwritesAndRemovesTheCallersOwnExplorationsAsync`                | Fact | Saves lists overwrites and removes the callers own explorations.                                                      |
| `CollapsesBeyondTheCapAndHidesRestrictedEdgesWithoutAGrantAsync`              | Fact | Collapses beyond the cap and hides restricted edges without agrant.                                                   |
| `RefusesBadArgumentsUnknownRegistriesAndMissingPermissionAsync`               | Fact | Refuses bad arguments unknown registries and missing permission.                                                      |
| `TheTraversalPlanUsesTheEdgeIndexesAndNeverScansTheEdgeTableAsync`            | Fact | The traversal plan uses the edge indexes and never scans the edge table.                                              |
| `CaseNodesAreHiddenFromAViewerWithoutTheCaseWorkflowRolesAsync`               | Fact | Case nodes, their edges and node facts are absent for a viewer without the case workflow role, and present with it.   |
| `APathWhoseOnlyRouteRunsThroughAHiddenCaseNodeIsNotReturnedAsync`             | Fact | A path whose only route runs through a case node the viewer cannot open is not returned, and is with the role.        |
| `FlowThroughAHiddenCaseNodeIsLeftOutOfTheTranchesAndTheTotalsAsync`           | Fact | A money tranche into a hidden case node is excluded from tranches, graph nodes, pass-through and the sentence totals. |
| `ExplodeLanesOmitAHiddenCaseNodeAsync`                                        | Fact | Explode lanes omit a hidden case node and keep the visible ones.                                                      |
| `CaseCountsOnANodeFollowTheViewersCaseWorkflowRolesAsync`                     | Fact | Case counts on a node only include cases the viewer holds the workflow and status roles for.                          |
| `RefusesMoreExpandedNodesThanTheCapAndIgnoresForeignNodeIdsAsync`             | Fact | Refuses more expanded node ids than the cap and ignores unknown or foreign ids.                                       |
| `NodeFactsAndEdgeEvidenceHideRestrictedEdgesFromAViewerWithoutAGrantAsync`    | Fact | Node facts omit restricted definitions and edge evidence is not found for a viewer without a registry grant.          |
| `RefusesAFlowWhoseFanOutAndDepthWouldExplodeAndAnOversizedPinnedPayloadAsync` | Fact | Refuses a flow whose fan-out raised to the depth exceeds the budget and an oversized pinned payload.                  |

### GraphAnalyticsTests

`Jube.Test.Service.Query.Graph.GraphAnalyticsTests` — 8 tests.
Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                                        |
|-----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------------|
| `TheAnalyticsCyclesWriteCommunitiesDistancesAndPatternsTheServicesReadBackAsync`  | Fact | The analytics cycles write communities distances and patterns the services read back.                           |
| `PathsFlowAndRelevanceRankingFollowTheSeededChainAsync`                           | Fact | Paths flow and relevance ranking follow the seeded chain.                                                       |
| `TheNetworkEvidenceFamilyReportsTheCommunityRateAndStaysSilentWithoutAGraphAsync` | Fact | The network evidence family reports the community rate and stays silent without agraph.                         |
| `CaseNodesCaseTagsAndTheQueueFastPathFeedProximityToTagAsync`                     | Fact | Case nodes case tags and the queue fast path feed proximity to tag.                                             |
| `CommunityMembersOmitAHiddenCaseNodeAsync`                                        | Fact | A community's member list omits a hidden case node and keeps the other members.                                 |
| `RestrictedEdgesNeverFeedTheCachedDegreesCommunitiesOrPageRankAsync`              | Fact | With a restricted device definition, cached degree, community, component and page rank are computed without it. |
| `MarkingADefinitionRestrictedRestrictsItsExistingEdgesAtOnceAsync`                | Fact | Marking a definition restricted flips its existing edges to restricted in the same call.                        |
| `CasingAPatternIntoAnUnknownCaseIsNotFoundAndLeavesThePatternUndecidedAsync`      | Fact | Casing a pattern into a case the caller cannot open is not found and leaves the pattern state unchanged.        |

### GraphJobsTests

`Jube.Test.Service.Query.Graph.GraphJobsTests` — 2 tests.
Category: Service. Collection: Database.

| Test                                                                           | Kind | Verifies                                                                             |
|--------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `TheRetentionJobCreatesTheMonthlyPartitionsAheadAndNeverDropsCurrentOnesAsync` | Fact | The retention job creates the monthly partitions ahead and never drops current ones. |
| `TheArchiveKeyIndexJobLeavesAValidIndexAndIsIdempotentAsync`                   | Fact | The archive key index job leaves a valid index and is idempotent.                    |

### JournalBoundedJsonPathTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalBoundedJsonPathTests` — 4 tests. Category: Unit.

| Test                                                               | Kind | Verifies                                                                    |
|--------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `APlainPathStillSelectsTheValue`                                   | Fact | A plain path still selects the value.                                       |
| `ACatastrophicRegexFilterSelectsNothingInsteadOfHangingOrThrowing` | Fact | A catastrophic regex filter selects nothing instead of hanging or throwing. |
| `ANullDocumentSelectsNothing`                                      | Fact | A null document selects nothing.                                            |
| `TheTransactionKeysReaderSurvivesAStoredCatastrophicPath`          | Fact | The transaction keys reader survives a stored catastrophic path.            |

### GridModelAccessTests

`Jube.Test.Service.Query.GridModel.GridModelAccessTests` — 5 tests. Category: Unit.

| Test                                                                                            | Kind   | Verifies                                                                                                        |
|-------------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------------|
| `EveryGridAreaIsPinnedHereSoANewAreaForcesADecisionAboutWhoMaySeeIt`                            | Fact   | Every grid area is pinned here so a new area forces a decision about who may see it.                            |
| `EveryGridAreaResolvesToExactlyOneRealServiceSoARenamedServiceCannotSilentlyBecomeLandlordOnly` | Theory | Every grid area resolves to exactly one real service so a renamed service cannot silently become landlord only. |
| `AMonitoringAreaIsLandlordOnlyWithNoTenantPermission`                                           | Theory | A monitoring area is landlord only with no tenant permission.                                                   |
| `ALoginAuditAreaNeedsPermission35AndIsNotLandlordOnly`                                          | Theory | A login audit area needs permission35 and is not landlord only.                                                 |
| `AnUnknownAreaIsNotKnownAndFailsClosedToLandlordOnly`                                           | Fact   | An unknown area is not known and fails closed to landlord only.                                                 |

### GridModelAuthorizationServiceTests

`Jube.Test.Service.Query.GridModel.GridModelAuthorizationServiceTests` — 6 tests. Category: Service. Collection:
Database.

| Test                                                                      | Kind   | Verifies                                                                         |
|---------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `MonitoringAreasAreLandlordOnlyAndTheLoginAreasNeedTheUserPermission`     | Fact   | Monitoring areas are landlord only and the login areas need the user permission. |
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                | Theory | Create with null or blank user name throws not authenticated.                    |
| `UnknownAndTenantlessUsersThrowNotAuthenticatedAsync`                     | Fact   | Unknown and tenantless users throw not authenticated.                            |
| `AUserWithoutPermissionIsRefusedEveryAreaWithALocalisedMessageAsync`      | Fact   | A user without permission is refused every area with a localised message.        |
| `AUserHoldingPermission35SeesTheLoginAreasButNotTheLandlordOnlyOnesAsync` | Fact   | A user holding permission35 sees the login areas but not the landlord only ones. |
| `ALandlordSeesEveryAreaAsync`                                             | Fact   | A landlord sees every area.                                                      |

### CaseBulkServiceTests

`Jube.Test.Service.Query.CaseBulk.CaseBulkServiceTests` — 8 tests. Category: Service.

| Test                                                               | Kind | Verifies                                                                                   |
|--------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `CloseClosesEveryOpenCaseWithTheReasonAndNoteAsync`                | Fact | Close closes every open case with the reason and the trimmed note.                         |
| `CloseSkipsAClosedCaseAnUnknownCaseAndIgnoresDuplicatesAsync`      | Fact | Close skips a closed case and an unknown case and ignores duplicate ids.                   |
| `CloseSkipsACaseHeldByAnotherUserAndLeavesItOpenAsync`             | Fact | Close skips a case held by another user and leaves it open.                                |
| `CloseWithoutTheCasePermissionIsForbiddenAsync`                    | Fact | Close without the case permission is forbidden and changes nothing.                        |
| `CloseRefusesAnEmptyListTooManyCasesAndAnUnknownReasonAsync`       | Fact | Close refuses an empty list, more than 500 cases, an unknown reason and an over-long note. |
| `ReassignHandsEveryEligibleCaseToTheAnalystAsync`                  | Fact | Reassign hands every eligible case to the analyst.                                         |
| `ReassignSkipsAClosedCaseAnUnknownCaseAndAnIneligibleAnalystAsync` | Fact | Reassign skips a closed case, an unknown case and an ineligible analyst.                   |
| `ReassignRequiresAnAnalystAsync`                                   | Fact | Reassign requires an analyst.                                                              |

### CaseTriageAnswerTests

`Jube.Test.Service.Query.CaseTriage.CaseTriageAnswerTests` — 5 tests. Category: Unit.

| Test                                     | Kind | Verifies                                                                                                                |
|------------------------------------------|------|-------------------------------------------------------------------------------------------------------------------------|
| `TheThreeOutcomesMapToTheirIds`          | Unit | confirmed, notconfirmed and inconclusive map to 1, 2 and 0.                                                             |
| `JsonWrappedInProseOrFencesIsFound`      | Unit | JSON inside prose or code fences is found and empty citations dropped.                                                  |
| `UnusableAnswersAreRefused`              | Unit | Null, empty, non-JSON, unknown outcome, missing or blank reason, non-string outcome and broken JSON all give no answer. |
| `LongTextIsClippedAndEvidenceIsCapped`   | Unit | The rationale is clipped to 4000 characters and evidence to 10 citations of 300.                                        |
| `TheDefinitionHashIsStableAndFullLength` | Unit | The prompt hash is a stable 64 character SHA-256.                                                                       |

### CaseTriageJobTests

`Jube.Test.Service.Query.CaseTriage.CaseTriageJobTests` — 5 tests. Category: Service.

| Test                                                                               | Kind    | Verifies                                                                                                    |
|------------------------------------------------------------------------------------|---------|-------------------------------------------------------------------------------------------------------------|
| `AnOpenCaseInAListedWorkflowGetsAPendingRecommendationWithGovernanceEvidenceAsync` | Service | A pending run is stored with outcome, rationale, evidence, conversation id, facts hash and definition hash. |
| `AWorkflowThatIsNotListedIsNeverTriagedAsync`                                      | Service | Unlisted or no workflows are never asked about.                                                             |
| `ARecommendationIsNotRemadeWhileTheFactsAreUnchangedAsync`                         | Service | Unchanged facts mean one agent call.                                                                        |
| `ANewNoteChangesTheFactsAndSupersedesThePendingRecommendationAsync`                | Service | New facts make a new run and the old pending one becomes superseded.                                        |
| `AnUnusableOrSilentAgentWritesNothingAsync`                                        | Service | A reply that is not the JSON shape, or no reply, stores nothing.                                            |

### QaDisagreementSamplingTests

`Jube.Test.Service.Query.CaseTriage.QaDisagreementSamplingTests` — 1 tests. Category: Unit.

| Test                                | Kind | Verifies                                                                       |
|-------------------------------------|------|--------------------------------------------------------------------------------|
| `TheRateIsMultipliedAndCappedAtOne` | Unit | The boosted rate is rate times factor, never below the base and never above 1. |

### ReplayMetricsTests

`Jube.Test.Service.Query.CaseTriage.ReplayMetricsTests` — 2 tests. Category: Unit.

| Test                                       | Kind | Verifies                                                                                |
|--------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `EveryKindOfDisagreementIsCountedOnItsOwn` | Unit | Agreed, missed risk, over-escalation, inconclusive and unusable are counted separately. |
| `NothingScoredHasNoRate`                   | Unit | With nothing scored the agreement rate is empty.                                        |

### CaseTriageReplayBundleTests

`Jube.Test.Service.Query.CaseTriage.CaseTriageReplayBundleTests` — 2 tests. Category: Unit.

| Test                                                        | Kind | Verifies                                                                                                      |
|-------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------|
| `TheBundleCarriesOnlyWhatItWasGivenAndNeverTheHumanOutcome` | Unit | The facts bundle holds the transactions, notes and earlier counts it was given and no outcome or closed date. |
| `LongPayloadsAndNotesAreClipped`                            | Unit | Payloads and notes are clipped to their maximum lengths.                                                      |

### CaseTriageReplayJobTests

`Jube.Test.Service.Query.CaseTriage.CaseTriageReplayJobTests` — 2 tests. Category: Service.

| Test                                                                    | Kind    | Verifies                                                                                                                                                    |
|-------------------------------------------------------------------------|---------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `TheModelSeesOnlyTheFactsFromBeforeTheCutOffAndNeverTheOutcomeAsync`    | Service | Archive rows, notes and earlier closed cases after the cut-off, and AI notes, never reach the model; the human outcome is stored beside the recommendation. |
| `AnUnusableAnswerIsRecordedAsUnscoredAndAReplayRunsOnlyOnceAsync`       | Service | An unparseable answer is stored as unscored, a custom prompt is used, and a finished replay is not run again.                                               |
| `AReplayWithNoWorkflowCoversOnlyTheWorkflowsTheRequesterCanAccessAsync` | Service | A replay with no workflow chosen replays only the closed cases of workflows the requester can access, never a workflow they cannot.                         |

### ReportBuilderTests

`Jube.Test.RegulatoryReport.ReportBuilderTests` — 14 tests. Category: Unit.

| Test                                                        | Kind | Verifies                                                                                                    |
|-------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------|
| `AGoodReportIsBuiltValidatedAndHashed`                      | Unit | A report is built from data, validated against the schema, hashed, and carries no template markup.          |
| `AnOptionalValueThatIsMissingIsLeftOut`                     | Unit | A missing optional value omits its element.                                                                 |
| `AMissingRequiredValueNamesTheDataPathAndTheTemplateLine`   | Unit | A missing required value is a mapping issue naming its data path and template line, and no XML is returned. |
| `ARequiredValueInsideAListNamesTheItem`                     | Unit | The data path of an issue inside a list names the item index.                                               |
| `AnUnknownCodeIsReportedNotGuessed`                         | Unit | A value not in the code list is reported, never guessed.                                                    |
| `ABadNumberOrDateIsReported`                                | Unit | A non-number or non-date is reported once, without a second missing-value issue.                            |
| `AnEmptyRequiredListIsReported`                             | Unit | An empty required list is reported.                                                                         |
| `HostileTextIsEscapedAndNeverBecomesMarkup`                 | Unit | Markup in data is escaped in the output.                                                                    |
| `TheSchemaNamesTheFieldThatBreaksIt`                        | Unit | A schema violation names the element path, line and position.                                               |
| `ExternallyProducedXmlCanBeValidatedAgainstTheProfile`      | Unit | Validate accepts good XML, rejects wrong XML and reports malformed XML.                                     |
| `AProfileWithoutASchemaOrWithABrokenOneSaysSo`              | Unit | No schema and an invalid schema are profile issues.                                                         |
| `ABrokenOrDoctypeTemplateIsRefusedBeforeAnythingIsRendered` | Unit | A malformed template or one with a DOCTYPE is refused.                                                      |
| `NoDataIsReportedNotThrown`                                 | Unit | Null data is an issue, not an exception.                                                                    |
| `TheSameDataAlwaysGivesTheSameBytesAndHash`                 | Unit | The output is deterministic.                                                                                |

### TemplateDirectiveTests

`Jube.Test.RegulatoryReport.TemplateDirectiveTests` — 10 tests. Category: Unit.

| Test                                                                   | Kind | Verifies                                                             |
|------------------------------------------------------------------------|------|----------------------------------------------------------------------|
| `IfIncludesTheElementOnlyWhenTheValueIsPresentAndTrue`                 | Unit | jr:if includes an element only for a present, true value.            |
| `DefaultFillsAnEmptyValueAndKeepEmptyKeepsTheElement`                  | Unit | jr:default fills an empty value and jr:keep-empty keeps the element. |
| `PruneDropsAnElementWhoseContentAllVanished`                           | Unit | jr:prune drops an element left empty.                                |
| `EachRepeatsPerItemWithRelativeAndRootPaths`                           | Unit | jr:each repeats per item with relative and root paths.               |
| `IndexedPathsAndFormatsWork`                                           | Unit | Indexed paths and trim, upper, truncate and number formats work.     |
| `FormattingIsCultureInvariant`                                         | Unit | Numbers format the same under a German culture.                      |
| `AListWhereAValueIsExpectedAndAnObjectWhereAListIsExpectedAreReported` | Unit | A list used as a value and an object used as a list are reported.    |
| `UnknownFormatsAndBadAttributeListsAreReported`                        | Unit | Unknown formats and malformed attribute lists are reported.          |
| `ATemplateNestedTooDeepIsRefused`                                      | Unit | A template nested past 64 levels is refused.                         |
| `AnEachOverTooManyItemsIsRefused`                                      | Unit | A list of over 100000 items is refused.                              |

### ReportProfileLoaderTests

`Jube.Test.RegulatoryReport.ReportProfileLoaderTests` — 3 tests. Category: Unit.

| Test                                          | Kind | Verifies                                                                  |
|-----------------------------------------------|------|---------------------------------------------------------------------------|
| `AProfileIsLoadedFromItsFolder`               | Unit | A profile loads its template, schemas and code lists from its folder.     |
| `AFileOutsideTheFolderOrAMissingOneIsRefused` | Unit | A path outside the folder, a missing file and a missing name are refused. |
| `ADirectoryWithoutAProfileIsRefused`          | Unit | A folder without profile.json is refused.                                 |

### FellegiSunterTests

`Jube.Test.Service.Query.GraphEntityResolution.FellegiSunterTests` — 5 tests. Category: Unit.

| Test                                                 | Kind | Verifies                                                                                             |
|------------------------------------------------------|------|------------------------------------------------------------------------------------------------------|
| `WeightsAreTheBaseTwoLogRatiosOfTheProbabilities`    | Unit | Agreement and disagreement weights are base-2 log ratios, with known answers.                        |
| `ARareValueAgreesWithMoreWeightThanACommonOne`       | Unit | Term-frequency adjustment gives a rare value more weight, by exactly the log of the frequency ratio. |
| `ThePosteriorIsThePriorOddsTimesTwoToTheTotalWeight` | Unit | Known-answer posteriors from a prior and weights.                                                    |
| `ExtremesAreClampedAndNeverNaN`                      | Unit | Extreme weights and probabilities clamp and never give NaN.                                          |
| `MoreAgreementNeverLowersTheProbability`             | Unit | Extra agreeing evidence never lowers the posterior.                                                  |

### GraphEntityComparisonTests

`Jube.Test.Service.Query.GraphEntityResolution.GraphEntityComparisonTests` — 4 tests. Category: Unit.

| Test                                                                 | Kind | Verifies                                                                              |
|----------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `FoldingIgnoresCasePunctuationAndAccents`                            | Unit | Folding makes case, punctuation and accents irrelevant.                               |
| `ValuesThatFoldTheSameAreAnExactAgreementWeightedByHowCommonTheyAre` | Unit | An exact agreement weighs more for a rarer value.                                     |
| `NearValuesAreAClosenessAgreementAndFarOnesAreNotACandidate`         | Unit | Near and reordered values are close agreements; far or empty ones are not candidates. |
| `AShareOfEvidenceFromOneNameAloneIsNeverCertain`                     | Unit | One name alone never reaches the certainty of a hard identifier.                      |

### GraphEntityResolutionTests

`Jube.Test.Service.Query.GraphEntityResolution.GraphEntityResolutionTests` — 7 tests. Category: Service.

| Test                                                                     | Kind    | Verifies                                                                                                               |
|--------------------------------------------------------------------------|---------|------------------------------------------------------------------------------------------------------------------------|
| `TwoNodesSharingAHardIdentifierBecomeAPendingCandidateWithEvidenceAsync` | Service | A shared hard identifier makes one pending candidate with evidence, found once.                                        |
| `AHubAnOversizedClusterAndNodesOfDifferentKeysAreNotCandidatesAsync`     | Service | A hub identifier, a cluster over the maximum and nodes of different keys are not candidates.                           |
| `ConfirmingMergesTheNodesIntoOneResolvedEntityAndUndoTakesItAwayAsync`   | Service | Confirm merges the nodes into the lowest id; undo removes it.                                                          |
| `ConfirmedPairsThatChainAreOneEntityAndRejectionKeepsThemApartAsync`     | Service | Chained confirmed pairs form one entity; a rejected node stays out.                                                    |
| `HardCandidatesAreConfirmedAutomaticallyOnlyWhenSwitchedOnAsync`         | Service | Auto-confirm confirms hard candidates as System and merges them.                                                       |
| `FuzzyCandidatesComeOnlyFromTheConfiguredKeysAndNeverAutoConfirmAsync`   | Service | Fuzzy candidates appear only for configured keys and stay pending even with auto-confirm on.                           |
| `PermissionTenantUnknownAndRepeatedDecisionsAreRefusedAsync`             | Service | Forbidden without permission, not found across tenants or unknown ids, bad input refused, repeated decision conflicts. |

### ModelGovernanceServiceTests

`Jube.Test.Service.Query.ModelGovernance.ModelGovernanceServiceTests` — 7 tests. Category: Service.

| Test                                                                  | Kind    | Verifies                                                                                               |
|-----------------------------------------------------------------------|---------|--------------------------------------------------------------------------------------------------------|
| `EachSaveAppendsAVersionAndTheHistoryIsKeptAsync`                     | Service | Each save appends a version and the history is kept, newest first.                                     |
| `BadStatementsAndUnknownModelsAreRefusedAsync`                        | Service | Blank or oversized owner, purpose and oversight, a bad tier or kind, and an unknown model are refused. |
| `EveryVerbNeedsThePermissionAndAnotherTenantSeesNothingAsync`         | Service | Every verb needs the model permission and another tenant sees nothing.                                 |
| `TheInventoryShowsEveryModelWithItsReviewStatusAsync`                 | Service | The inventory lists every model with not recorded, current, due soon or overdue.                       |
| `TheCardIsOneEscapedHtmlDocumentWithTheStatementAndDriftHistoryAsync` | Service | The card is one HTML document with the statement and drift history, with every value escaped.          |
| `ReviewStatusIsWorkedOutFromTheNextReviewDate`                        | Unit    | The review status follows the next review date, with a 30 day due-soon window.                         |
| `ACardWithNoStatementOrDriftSaysSoAndStillRenders`                    | Unit    | A card with no statement or drift says so and still renders.                                           |
