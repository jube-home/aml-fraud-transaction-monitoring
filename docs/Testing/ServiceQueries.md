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

| Test                                                                        | Kind   | Verifies                                                                           |
|-----------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `AnUnusualTransactionStandsOutAgainstTheKeysOwnNormalAndNamesTheFieldAsync` | Fact   | An unusual transaction stands out against the keys own normal and names the field. |
| `AKeyIsComparedWithItsPeersFieldByFieldAsync`                               | Fact   | A key is compared with its peers field by field.                                   |
| `ThePeersAreTheKeysSegmentWhenItHasOneAndAllKeysWhenItDoesNotAsync`         | Fact   | The peers are the keys segment when it has one and all keys when it does not.      |
| `TheMostUnusualPeerComparisonComesFirstAsync`                               | Fact   | The most unusual peer comparison comes first.                                      |
| `PeersWithNothingToCompareSayWhyAsync`                                      | Fact   | Peers with nothing to compare say why.                                             |
| `ThePeerRepositoryUpdatesInPlaceAndKeepsSegmentsApartAsync`                 | Fact   | The peer repository updates in place and keeps segments apart.                     |
| `OnlyTheRequestedSectionsAreComputedAsync`                                  | Fact   | Only the requested sections are computed.                                          |
| `FeaturesAndModelScoresAreAnalysedAsMeasuresAlongsideThePayloadAsync`       | Fact   | Features and model scores are analysed as measures alongside the payload.          |
| `ACounterpartyRoleBuildsAProfileAndAFirstSeenValueIsNovelAsync`             | Fact   | A counterparty role builds a profile and a first seen value is novel.              |
| `APayloadFieldTheCallersRolesCannotSeeIsNeverAnalysedAsync`                 | Fact   | A payload field the callers roles cannot see is never analysed.                    |
| `TheFieldsListNarrowsTheMeasuresAndTheLimitTruncatesAsync`                  | Fact   | The fields list narrows the measures and the limit truncates.                      |
| `ABadSectionIsRefusedAsync`                                                 | Fact   | A bad section is refused.                                                          |
| `ThePermissionTheWorkflowAndTheKeyAreCheckedAsync`                          | Fact   | The permission the workflow and the key are checked.                               |
| `AKeyWithNoTransactionsGivesAnEmptyButWellFormedReportAsync`                | Fact   | A key with no transactions gives an empty but well formed report.                  |

### CaseKeyJournalChartRendererTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalChartRendererTests` — 3 tests.
Category: Unit.

| Test                                                         | Kind   | Verifies                                                                 |
|--------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EveryChartDrawsAnSvgInBothThemes`                           | Theory | Every chart draws an svg in both themes.                                 |
| `WithNothingToDrawTheChartSaysSoRatherThanDrawingAnEmptyOne` | Theory | With nothing to draw the chart says so rather than drawing an empty one. |
| `EachChartKindMapsToTheSectionItNeeds`                       | Fact   | Each chart kind maps to the section it needs.                            |

### JournalTransactionReaderTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalTransactionReaderTests` — 17 tests.
Category: Unit.

| Test                                                                 | Kind   | Verifies                                                                           |
|----------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `EveryModelSourceIsReadAndTheValueComesFromItsDetailWhenThereIsOne`  | Fact   | Every model source is read and the value comes from its detail when there is one.  |
| `TheModelTheResultAndTheNarrativeAreCarriedAsArchived`               | Fact   | The model the result and the narrative are carried as archived.                    |
| `CalibrationKeepsItsBandsInOrder`                                    | Fact   | Calibration keeps its bands in order.                                              |
| `ContributionsAreSignedByTheirDirectionAndExactnessIsKept`           | Fact   | Contributions are signed by their direction and exactness is kept.                 |
| `APayloadInputTheCallerCannotSeeIsHiddenAndNeverNamedOrCounted`      | Fact   | A payload input the caller cannot see is hidden and never named or counted.        |
| `AFeatureOrAbstractionInputIsNeverHiddenWhateverTheXPathsAre`        | Fact   | A feature or abstraction input is never hidden whatever the x paths are.           |
| `TheDecisionPathIsInOrderAndHidesTheNodesOnInputsTheCallerCannotSee` | Fact   | The decision path is in order and hides the nodes on inputs the caller cannot see. |
| `VisibilityIsNotCaseSensitive`                                       | Fact   | Visibility is not case sensitive.                                                  |
| `TheNamesAXPathMakesVisibleIncludeItsLastPathSegment`                | Fact   | The names AX path makes visible include its last path segment.                     |
| `ValuesCarryTheirGroupAndAnHttpAdaptationIsReadByItsValue`           | Fact   | Values carry their group and an http adaptation is read by its value.              |
| `OnlyVisibleActivationsAreListedWithTheirForcedFlag`                 | Fact   | Only visible activations are listed with their forced flag.                        |
| `KeysAreTheDrillAndIdentifierXPathsWithTheirValues`                  | Fact   | Keys are the drill and identifier x paths with their values.                       |
| `OnlyAnIdentifierReadStraightFromThePayloadCanBePivotedTo`           | Theory | Only an identifier read straight from the payload can be pivoted to.               |
| `TheSignedContributionAppliesTheDirectionToTheMagnitude`             | Theory | The signed contribution applies the direction to the magnitude.                    |
| `NoWeightIsNoSignedContribution`                                     | Fact   | No weight is no signed contribution.                                               |
| `AnAdaptationWithNoDetailStillHasItsValueAndNothingElse`             | Fact   | An adaptation with no detail still has its value and nothing else.                 |
| `GarbageInTheArchiveIsToleratedRatherThanFailing`                    | Fact   | Garbage in the archive is tolerated rather than failing.                           |

### CaseKeyJournalTransactionChartRendererTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTransactionChartRendererTests` — 7 tests.
Category: Unit.

| Test                                                          | Kind   | Verifies                                                                |
|---------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `ContributionsDrawAsAWaterfallWhenExactAndAsBarsWhenRelative` | Theory | Contributions draw as a waterfall when exact and as bars when relative. |
| `HiddenInputsAreNeverDrawnAndTheChartSaysHowManyWereHidden`   | Fact   | Hidden inputs are never drawn and the chart says how many were hidden.  |
| `OnlyTheLargestFifteenInputsAreDrawn`                         | Fact   | Only the largest fifteen inputs are drawn.                              |
| `CalibrationDrawsTheReliabilityCurveInBothThemes`             | Fact   | Calibration draws the reliability curve in both themes.                 |
| `WithNothingToDrawTheChartSaysSo`                             | Theory | With nothing to draw the chart says so.                                 |
| `AHiddenInputIsNeverDrawnEvenIfSomethingLeftAFigureOnIt`      | Fact   | A hidden input is never drawn even if something left a figure on it.    |
| `AContributionOfOnlyHiddenInputsHasNothingToDraw`             | Fact   | A contribution of only hidden inputs has nothing to draw.               |

### CaseKeyJournalBindingTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalBindingTests` — 4 tests.
Category: Unit.

| Test                                                           | Kind   | Verifies                                                             |
|----------------------------------------------------------------|--------|----------------------------------------------------------------------|
| `TheRowsRequestBindsWithOnlyTheWorkflowTheKeyAndTheValueAsync` | Theory | The rows request binds with only the workflow the key and the value. |
| `TheTransactionRequestNeedsOnlyTheWorkflowAndTheIdAsync`       | Fact   | The transaction request needs only the workflow and the id.          |
| `ARequestWithoutTheWorkflowOrTheIdIsRefusedAtTheBoundaryAsync` | Fact   | A request without the workflow or the id is refused at the boundary. |
| `EveryOptionOfTheRowsRequestIsOptionalOnTheWire`               | Fact   | Every option of the rows request is optional on the wire.            |

### CaseKeyJournalIndexJobTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalIndexJobTests` — 11 tests.
Category: Service. Collection: Database.

| Test                                                                              | Kind   | Verifies                                                                                    |
|-----------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `BuildsTheIndexForACaseKeyAndRecordsItAsReadyAsync`                               | Fact   | Builds the index for a case key and records it as ready.                                    |
| `DrillFieldsOfAWorkflowAreIndexedToo`                                             | Fact   | Drill fields of a workflow are indexed too.                                                 |
| `ARunAgainBuildsNothingNewAndKeepsTheIndexAsync`                                  | Fact   | A run again builds nothing new and keeps the index.                                         |
| `NothingIsBuiltBeforeTheArchiveIsLargeEnoughAsync`                                | Fact   | Nothing is built before the archive is large enough.                                        |
| `KeysThatAreNotSafeInAnIndexDefinitionAreNeverIndexedAsync`                       | Fact   | Keys that are not safe in an index definition are never indexed.                            |
| `NoMoreThanTheMaximumNumberOfIndexesIsHeldAsync`                                  | Fact   | No more than the maximum number of indexes is held.                                         |
| `AnIndexThatHasBeenDroppedIsReportedLostAndRebuiltOnceTheRetryTimeHasPassedAsync` | Fact   | An index that has been dropped is reported lost and rebuilt once the retry time has passed. |
| `ABuildLeftBuildingIsRetriedOnlyOnceItLooksAbandonedAsync`                        | Fact   | A build left building is retried only once it looks abandoned.                              |
| `ABuildThatCannotRunIsRecordedAsFailedWithWhyAsync`                               | Fact   | A build that cannot run is recorded as failed with why.                                     |
| `TheLookupFindsTheSameTransactionsWithAndWithoutTheIndexAsync`                    | Theory | The lookup finds the same transactions with and without the index.                          |
| `TextEqualityAndContainmentFindTheSameStringAndWholeNumberRowsAsync`              | Fact   | Text equality and containment find the same string and whole number rows.                   |

### CaseKeyJournalPeerJobTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalPeerJobTests` — 9 tests.
Category: Service. Collection: Database.

| Test                                                                            | Kind   | Verifies                                                                                |
|---------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `RebuildsThePeerDistributionOfEachMeasureFromTheArchiveAsync`                   | Fact   | Rebuilds the peer distribution of each measure from the archive.                        |
| `SegmentsGetTheirOwnDistributionAndSmallOnesGetNoneAsync`                       | Fact   | Segments get their own distribution and small ones get none.                            |
| `FewerThanTwentyKeysBuildNothingAsync`                                          | Fact   | Fewer than twenty keys build nothing.                                                   |
| `OnlyTheNewestRowsUpToTheCapAreReadAsync`                                       | Fact   | Only the newest rows up to the cap are read.                                            |
| `PagesThroughTheArchiveWhenTheSampleIsLargerThanOnePageAsync`                   | Fact   | Pages through the archive when the sample is larger than one page.                      |
| `TransactionsOutsideTheWindowAreLeftOutAsync`                                   | Fact   | Transactions outside the window are left out.                                           |
| `ARecentRebuildIsLeftAloneAndAnOldOneIsReplacedInPlaceAsync`                    | Fact   | A recent rebuild is left alone and an old one is replaced in place.                     |
| `ADistributionThatNoLongerQualifiesIsSoftDeletedAndRevivedWhenItDoesAgainAsync` | Fact   | A distribution that no longer qualifies is soft deleted and revived when it does again. |
| `TheRunWalksEveryWorkflowWithACaseKeyAndCountsWhatItDidAsync`                   | Fact   | The run walks every workflow with a case key and counts what it did.                    |

### CaseKeyJournalIndexStatementTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalIndexStatementTests` — 5 tests.
Category: Unit.

| Test                                                                              | Kind   | Verifies                                                                                          |
|-----------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `OnlyOrdinaryFieldNamesAreSafeToPutInAnIndexDefinition`                           | Theory | Only ordinary field names are safe to put in an index definition.                                 |
| `AFieldNameOfMoreThanOneHundredAndTwentyEightCharactersIsNotIndexable`            | Fact   | A field name of more than one hundred and twenty eight characters is not indexable.               |
| `OnlyValuesThatAreTheSameTextInTheDocumentUseTheIndex`                            | Theory | Only values that are the same text in the document use the index.                                 |
| `TheIndexExpressionQuotesTheFieldNameAsALiteral`                                  | Fact   | The index expression quotes the field name as a literal.                                          |
| `TheIndexIsNamedByAHashOfTheFieldAndTheStatementCoversModelExpressionAndIdentity` | Fact   | The index is named by a hash of the field and the statement covers model expression and identity. |

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

| Test                                                                          | Kind   | Verifies                                                                             |
|-------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `PinningAndUnpinningIsSharedAndListedMostRecentFirstAsync`                    | Fact   | Pinning and unpinning is shared and listed most recent first.                        |
| `AWorkflowRowThatWasReplacedWithAnotherModelPinsTheLiveRowsTransactionsAsync` | Fact   | A workflow row that was replaced with another model pins the live rows transactions. |
| `OnlyTransactionsOfTheWorkflowsModelCanBePinnedAsync`                         | Fact   | Only transactions of the workflows model can be pinned.                              |
| `ACaseHoldsNoMoreThanFiftyPinsAsync`                                          | Fact   | A case holds no more than fifty pins.                                                |
| `TheTransactionListIsValidatedAsync`                                          | Fact   | The transaction list is validated.                                                   |
| `ACaseLockedToAnotherUserCannotBeChangedButMyOwnLockCanAsync`                 | Fact   | A case locked to another user cannot be changed but my own lock can.                 |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync`                    | Fact   | The permission the workflow roles and the tenant are checked.                        |

### CaseKeyJournalTagJobTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTagJobTests` — 22 tests.
Category: Service. Collection: Database.

| Test                                                                        | Kind   | Verifies                                                                              |
|-----------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `PreviewCountsTheTransactionsOfTheViewAsync`                                | Fact   | Preview counts the transactions of the view.                                          |
| `ASubmittedJobTagsEveryMatchingTransactionAndReportsItsProgressAsync`       | Fact   | A submitted job tags every matching transaction and reports its progress.             |
| `AFilterOnTheArchiveDecidesWhichTransactionsAreCountedAndTaggedAsync` | Fact | A filter on the archive decides which transactions are counted and tagged. |
| `TheFilterAppliesToEveryLaneAsync` | Fact | The filter applies to every lane. |
| `AFieldWhoseStoredValueCannotBeReadAsItsTypeIsRefusedAsync` | Fact | A field whose stored value cannot be read as its type is refused. |
| `AFilterThatIsNotValidIsRefusedAsync` | Fact | A filter that is not valid is refused. |
| `AJobAddsAndRemovesAndLeavesRowsThatAlreadyHaveTheTagAloneAsync`            | Fact   | A job adds and removes and leaves rows that already have the tag alone.               |
| `UndoPutsEveryChangedTransactionBackAndCanOnlyBeAskedForOnceAsync`          | Fact   | Undo puts every changed transaction back and can only be asked for once.              |
| `ACaseLockedToAnotherUserIsLeftAloneAndCountedAsync`                        | Fact   | A case locked to another user is left alone and counted.                              |
| `AQueuedJobCanBeCancelledAndANeverRunJobIsNotPickedUpAsync`                 | Fact   | A queued job can be cancelled and a never run job is not picked up.                   |
| `ARunningJobStopsAtTheEndOfTheBatchWhenCancelledAndCanStillBeUndoneAsync`   | Fact   | A running job stops at the end of the batch when cancelled and can still be undone.   |
| `AJobWhoseTagIsNoLongerAllowedFailsWithWhyAsync`                            | Fact   | A job whose tag is no longer allowed fails with why.                                  |
| `ARunningJobThatStoppedReportingIsTakenUpAgainAsync`                        | Fact   | A running job that stopped reporting is taken up again.                               |
| `TheViewIsValidatedAndTooManyOrNoMatchesAreRefusedAsync`                    | Fact   | The view is validated and too many or no matches are refused.                         |
| `MergedKeysAndTheWindowAndActivationsDecideWhichTransactionsMatchAsync`     | Fact   | Merged keys and the window and activations decide which transactions match.           |
| `PreviewPagesThroughMoreMatchesThanOnePageHoldsAsync`                       | Fact   | Preview pages through more matches than one page holds.                               |
| `AStoppedOrFailedJobCannotBeUndoneAndACancelledUndoCanBeAskedForAgainAsync` | Fact   | A stopped or failed job cannot be undone and a cancelled undo can be asked for again. |
| `AnUndoCountsTheTransactionsItLeftAloneBecauseTheirCaseIsLockedAsync`       | Fact   | An undo counts the transactions it left alone because their case is locked.           |
| `ARunningJobIsStoppedByTheServiceAtTheNextBatchAsync`                       | Fact   | A running job is stopped by the service at the next batch.                            |
| `TheMatcherStopsReadingOnceMoreThanTheMaximumMatchAsync`                    | Fact   | The matcher stops reading once more than the maximum match.                           |
| `OnlyTheCallersOwnJobsAreVisibleAndTheListIsNewestFirstAsync`               | Fact   | Only the callers own jobs are visible and the list is newest first.                   |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync`                  | Fact   | The permission the workflow roles and the tenant are checked.                         |

### CaseKeyJournalTagServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTagServiceTests` — 12 tests.
Category: Service. Collection: Database.

| Test                                                                            | Kind   | Verifies                                                                              |
|---------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `OptionsListOnlyTheActiveTagsOfTheWorkflowsModelAlphabeticallyAsync`            | Fact   | Options list only the active tags of the workflows model alphabetically.              |
| `AddingAndRemovingChangesEverySelectedTransactionAndReportsBeforeAndAfterAsync` | Fact   | Adding and removing changes every selected transaction and reports before and after.  |
| `TheTagsAreRecordedInTheArchiveTagTableAsync`                                   | Fact   | The tags are recorded in the archive tag table.                                       |
| `ATransactionThatAlreadyHasTheTagIsLeftUntouchedAsync`                          | Fact   | A transaction that already has the tag is left untouched.                             |
| `RestoringThePreviousSetsUndoesATaggingAsync`                                   | Fact   | Restoring the previous sets undoes a tagging.                                         |
| `ATransactionOfAnotherModelIsSkippedAndNeverChangedAsync`                       | Fact   | A transaction of another model is skipped and never changed.                          |
| `ACaseLockedToAnotherUserIsLeftAloneWhileMyOwnLockAndNoLockAreTaggedAsync`      | Fact   | A case locked to another user is left alone while my own lock and no lock are tagged. |
| `AWorkflowRowThatWasReplacedWithAnotherModelTagsTheLiveRowsTransactionsAsync`   | Fact   | A workflow row that was replaced with another model tags the live rows transactions.  |
| `ATagTheModelDoesNotAllowIsRefusedAndNothingChangesAsync`                       | Fact   | A tag the model does not allow is refused and nothing changes.                        |
| `TheRequestIsValidatedAsync`                                                    | Fact   | The request is validated.                                                             |
| `TheRestoreRequestIsValidatedAsync`                                             | Fact   | The restore request is validated.                                                     |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync`                      | Fact   | The permission the workflow roles and the tenant are checked.                         |

### CaseKeyJournalTransactionServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalTransactionServiceTests` — 11 tests.
Category: Service. Collection: Database.

| Test                                                                       | Kind   | Verifies                                                                         |
|----------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `OpensATransactionWithItsActivationsFeaturesKeysAndModelJourneyAsync`      | Fact   | Opens a transaction with its activations features keys and model journey.        |
| `TheJourneyNeverNamesAPayloadInputTheCallersRolesCannotSeeAsync`           | Fact   | The journey never names a payload input the callers roles cannot see.            |
| `ThePayloadIsOnlyTheFieldsTheCallersRolesMaySeeAsync`                      | Fact   | The payload is only the fields the callers roles may see.                        |
| `ATransactionOfAnotherModelIsNotFoundEvenWithItsRealIdAsync`               | Fact   | A transaction of another model is not found even with its real id.               |
| `AnUnknownTransactionAndABadIdAreRefusedAsync`                             | Fact   | An unknown transaction and a bad id are refused.                                 |
| `ThePermissionAndTheWorkflowRolesAreCheckedAsync`                          | Fact   | The permission and the workflow roles are checked.                               |
| `TheQueryNeverReturnsAnotherTenantsTransactionEvenForTheirModelAndIdAsync` | Fact   | The query never returns another tenants transaction even for their model and id. |
| `AnotherTenantsUserNeverGetsATransactionAsync`                             | Fact   | Another tenants user never gets a transaction.                                   |
| `AroundListsTheSameDaysTransactionsThatShareAKeyAndNothingElseAsync`       | Fact   | Around lists the same days transactions that share a key and nothing else.       |
| `AroundCapsEachGroupAndSaysSoAsync`                                        | Fact   | Around caps each group and says so.                                              |
| `AroundIsRefusedWithoutThePermissionOrAnotherTenantsTransactionAsync`      | Fact   | Around is refused without the permission or another tenants transaction.         |

### CaseKeyJournalReplayServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalReplayServiceTests` — 8 tests.
Category: Service. Collection: Database.

| Test | Kind | Verifies |
|------|------|----------|
| `AReplayIsOnlySettingsSharedOnTheCaseAndListedNewestFirstAsync` | Fact | A replay is only settings shared on the case and listed newest first. |
| `ADeletedReplayIsNoLongerOfferedAndTheRowIsKeptAsync` | Fact | A deleted replay is no longer offered and the row is kept. |
| `TheReplaySettingsAreValidatedAndACaseHoldsNoMoreThanTenAsync` | Fact | The replay settings are validated and a case holds no more than ten. |
| `ACaseLockedToAnotherUserCannotHaveReplaysSavedOrDeletedButMyOwnLockCanAsync` | Fact | A case locked to another user cannot have replays saved or deleted but my own lock can. |
| `TheSummaryWorksTheSavedSettingsOutAgainFromTheArchiveAsync` | Fact | The summary works the saved settings out again from the archive. |
| `TheLensAndShowingEverythingAreSavedWithTheViewAsync` | Fact | The lens and showing everything are saved with the view. |
| `ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync` | Fact | The permission the workflow roles and the tenant are checked. |
| `TheSpeedAndDistanceLimitsAreSavedWithTheReplayAndUsedWhenItIsSummarisedAsync` | Fact | The speed and distance limits are saved with the replay and used when it is summarised. |

### CaseKeyJournalServiceTests

`Jube.Test.Service.Query.CaseKeyJournal.CaseKeyJournalServiceTests` — 32 tests.
Category: Service. Collection: Database.

| Test                                                                            | Kind   | Verifies                                                                              |
|---------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `ReturnsOnlyTheKeysTransactionsOfTheWorkflowsModelNewestFirstAsync`             | Fact   | Returns only the keys transactions of the workflows model newest first.               |
| `AWorkflowRowThatWasReplacedWithAnotherModelReadsTheLiveRowsTransactionsAsync`  | Fact   | A workflow row that was replaced with another model reads the live rows transactions. |
| `MergedKeysShareOneTimelineAndEachRowSaysWhichLanesItBelongsToAsync`            | Fact   | Merged keys share one timeline and each row says which lanes it belongs to.           |
| `MergeNeedsMatchingKeysAndValuesWithAtMostThreeExtraAsync`                      | Fact   | Merge needs matching keys and values with at most three extra.                        |
| `TheJournalSaysWhichColumnsLocateATransactionOnlyWhenTheCallerCanSeeThemAsync` | Fact | The journal says which columns locate a transaction only when the caller can see them. |
| `TheTrackPutsTransactionsInTimeOrderAndMarksTheMovementThatCouldNotHappenAsync` | Fact | The track puts transactions in time order and marks the movement that could not happen. |
| `TheTrackUsesTheSpeedAndDistanceLimitsItIsGivenAndRefusesSillyOnesAsync` | Fact | The track uses the speed and distance limits it is given and refuses silly ones. |
| `TheFilterIsAppliedInTheArchiveBeforeTheLimitAndTheUnfilteredTotalIsReportedAsync` | Fact | The filter is applied in the archive before the limit and the unfiltered total is reported. |
| `TheJournalOffersThePayloadFieldsSetAsDrillFieldsAsKeysAsync` | Fact | The journal offers the payload fields set as drill fields as keys. |
| `APinnedTransactionIsHeldToTheFilterToo` | Fact | A pinned transaction is held to the filter too. |
| `TheTrackCanReturnEveryTransactionWithTheFilteredOnesPickedOutAsync` | Fact | The track can return every transaction with the filtered ones picked out. |
| `TheTrackAppliesTheFilterAndCanReturnOnlyTheSummaryAsync` | Fact | The track applies the filter and can return only the summary. |
| `EachMergedKeyIsItsOwnTrailAndMovementIsOnlyCountedWithinATrailAsync` | Fact | Each merged key is its own trail and movement is only counted within a trail. |
| `TheTrackHasNothingToDrawWhenTheCallerCannotSeeTheLocationColumnsAsync` | Fact | The track has nothing to draw when the caller cannot see the location columns. |
| `TheTrackRefusesAViewThatIsNotValidAndAWorkflowTheCallerCannotSeeAsync` | Fact | The track refuses a view that is not valid and a workflow the caller cannot see. |
| `TheBoldLineMarksRowsSharingTheCasesValueInEveryLensAsync` | Fact | The bold line marks rows sharing the cases value in every lens. |
| `PinnedTransactionsComeFirstEvenOutsideTheWindowAndAreMarkedAsync`              | Fact   | Pinned transactions come first even outside the window and are marked.                |
| `PayloadColumnsAreOnlyTheXPathsTheCallersRolesMaySeeAsync`                      | Fact   | Payload columns are only the x paths the callers roles may see.                       |
| `AConfiguredRoleIsReturnedAsConfiguredAsync`                                    | Fact   | A configured role is returned as configured.                                          |
| `EachLensReturnsItsOwnColumnsAndTheBaseColumnsAsync`                            | Theory | Each lens returns its own columns and the base columns.                               |
| `HiddenActivationsAreNeverNamedAndTheLensCountsSayWhatHasContentAsync`          | Fact   | Hidden activations are never named and the lens counts say what has content.          |
| `ANumericKeyIsFoundWhenTheValueIsAskedForAsText`                                | Fact   | A numeric key is found when the value is asked for as text.                           |
| `TheLimitTruncatesTheNewestFirstAndSaysSoAsync`                                 | Fact   | The limit truncates the newest first and says so.                                     |
| `TheWindowTheElevationAndActivationsOnlyAllNarrowTheRowsAsync`                  | Fact   | The window the elevation and activations only all narrow the rows.                    |
| `TagsComeBackAsAPlainListAsync`                                                 | Fact   | Tags come back as a plain list.                                                       |
| `ABadKeyOrValueIsRefusedAsync`                                                  | Theory | A bad key or value is refused.                                                        |
| `ABadLensOrLimitIsRefusedAsync`                                                 | Theory | A bad lens or limit is refused.                                                       |
| `AReversedOrOversizedWindowIsRefusedAsync`                                      | Fact   | A reversed or oversized window is refused.                                            |
| `ARoleWithoutThePermissionIsRefusedAsync`                                       | Fact   | A role without the permission is refused.                                             |
| `AWorkflowTheCallerHasNoRoleOnIsNotFoundAndAnUnknownOneTooAsync`                | Fact   | A workflow the caller has no role on is not found and an unknown one too.             |
| `TheQueryNeverReturnsAnotherTenantsTransactionsEvenWhenAskedForTheirModelAsync` | Fact   | The query never returns another tenants transactions even when asked for their model. |
| `ANullRequestIsRefusedAndAnUnauthenticatedCallerNeverGetsAServiceAsync`         | Fact   | A null request is refused and an unauthenticated caller never gets a service.         |

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

| Test | Kind | Verifies |
|------|------|----------|
| `AFilterWithNoRulesIsNoFilter` | Fact | A filter with no rules is no filter. |
| `NestedGroupsBecomeParameterisedSqlInOrder` | Fact | Nested groups become parameterised SQL in order. |
| `EveryOperatorMapsToItsSql` | Theory | Every operator maps to its SQL. |
| `ValuesAreConvertedToTheTypeOfTheField` | Fact | Values are converted to the type of the field. |
| `AValueNeverReachesTheSql` | Fact | A value never reaches the SQL. |
| `AFilterThatIsNotValidIsRefused` | Theory | A filter that is not valid is refused. |
| `AShapeThatIsNotAFilterIsRefused` | Theory | A shape that is not a filter is refused. |
| `AFilterThatIsTooBigIsRefused` | Fact | A filter that is too big is refused. |
| `ThePersistedTextKeepsOnlyWhatTheServerUnderstood` | Fact | The persisted text keeps only what the server understood. |
| `OnlyFieldsThatCanBeFilteredAreOffered` | Fact | Only fields that can be filtered are offered. |

### JournalLocationTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalLocationTests` — 3 tests.
Category: Unit.

| Test | Kind | Verifies |
|------|------|----------|
| `OnlyAPlainPayloadFieldHasAName` | Theory | Only a plain payload field has a name. |
| `TheColumnsAreFoundByTheFieldsTheModelUses` | Fact | The columns are found by the fields the model uses. |
| `NoLocationIsReportedUnlessBothColumnsAreVisibleAndNamed` | Fact | No location is reported unless both columns are visible and named. |

### JournalTravelTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalTravelTests` — 10 tests.
Category: Unit.

| Test | Kind | Verifies |
|------|------|----------|
| `TheDistanceBetweenTwoCitiesIsRight` | Fact | The distance between two cities is right. |
| `OnlyARealPositionIsLocated` | Theory | Only a real position is located. |
| `NoLocatedTransactionsMeansNoMovement` | Fact | No located transactions means no movement. |
| `ALegIsImpossibleOnlyWhenItIsFarEnoughAndTooFast` | Fact | A leg is impossible only when it is far enough and too fast. |
| `OnlySensibleLimitsAreAccepted` | Theory | Only sensible limits are accepted. |
| `TheLimitsDecideWhichLegsAreImpossible` | Fact | The limits decide which legs are impossible. |
| `TheSameInstantAtTwoPlacesIsImpossibleWithNoSpeedToName` | Fact | The same instant at two places is impossible with no speed to name. |
| `ALegUnderTheMinimumDistanceIsNeverImpossibleHoweverFast` | Fact | A leg under the minimum distance is never impossible however fast. |
| `EachLaneIsItsOwnTrailAndTheOrderOfArrivalDoesNotMatter` | Fact | Each lane is its own trail and the order of arrival does not matter. |
| `OnlyTheFiveWorstLegsAreKeptFastestFirst` | Fact | Only the five worst legs are kept fastest first. |

### JournalLensReaderTests

`Jube.Test.Service.Query.CaseKeyJournal.JournalLensReaderTests` — 15 tests.
Category: Unit.

| Test                                                               | Kind   | Verifies                                                                         |
|--------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `OnlyVisibleActivationsAreNamedAndAForcedOneSaysSo`                | Fact   | Only visible activations are named and a forced one says so.                     |
| `FeaturesGatherEveryFacetTheRulesSawWithTheirTypes`                | Fact   | Features gather every facet the rules saw with their types.                      |
| `ModelsAndSanctionsKeepTheirOwnFacets`                             | Fact   | Models and sanctions keep their own facets.                                      |
| `PayloadReadsOnlyTheXPathsItIsGivenAndLeavesObjectsOut`            | Fact   | Payload reads only the x paths it is given and leaves objects out.               |
| `PayloadCarriesTheConfiguredRoleAndDrill` | Fact | Payload carries the configured role and drill. |
| `APayloadReadNoLongerCarriesFormatting` | Fact | A payload read no longer carries formatting. |
| `AMatchingPatternFormatsTheCellAndSaysWhichPatternDidIt` | Fact | A matching pattern formats the cell and says which pattern did it. |
| `APatternThatDoesNotMatchOrIsBrokenFormatsNothing`                 | Fact   | A pattern that does not match or is broken formats nothing.                      |
| `TheBoldLineMatchesOnlyWhenTheRowSharesTheCaseValue` | Fact | The bold line matches only when the row shares the case value. |
| `TheBoldLineComparesNumbersAndBooleansAsText` | Fact | The bold line compares numbers and booleans as text. |
| `TheReferenceIsTakenFromTheCaseOnlyForBoldLineXPaths` | Fact | The reference is taken from the case only for bold line x paths. |
| `PresenceSaysWhichLensesHaveAnythingToShow`                        | Fact   | Presence says which lenses have anything to show.                                |
| `OnlyTheFiveLensesAreAccepted`                                     | Theory | Only the five lenses are accepted.                                               |
| `TheKeyIsMatchedAsATextAndAsAnumberSoANumericIdentifierStillFinds` | Fact   | The key is matched as a text and as anumber so a numeric identifier still finds. |
| `AKeyWithQuotesOrBracesCannotEscapeTheContainmentDocument`         | Fact   | A key with quotes or braces cannot escape the containment document.              |

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

`Jube.Test.Service.Query.CaseWorkflowDisplayExecution.CaseWorkflowDisplayExecutionServiceTests` — 40 tests. Category:
Service. Collection: Database.

| Test                                                        | Kind   | Verifies                                                         |
|-------------------------------------------------------------|--------|------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`  | Theory | Create with null or blank user name throws not authenticated.    |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`   | Fact   | Create with user having no tenant throws not authenticated.      |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`          | Fact   | Create with unknown user throws not authenticated.               |
| `ExecuteThrowsForbiddenWhenPermissionMissingAsync`          | Fact   | Execute throws forbidden when permission missing.                |
| `ExecuteReplacesPayloadTokensAndRendersUnknownOnesEmptyAsync`   | Fact   | Execute replaces payload tokens and renders unknown ones empty.       |
| `ExecuteReadsTheCaseRootAndRendersAFieldReadOnlyAsync` | Fact | Execute reads the case root and renders a field read only. |
| `ExecuteWithATemplateThatDoesNotCompileThrowsTemplateInvalidAsync` | Fact | Execute with a template that does not compile throws template invalid. |
| `RenderFormFillsTheValuesAndNamesTheFieldsShownAsync` | Fact | Render form fills the values and names the fields shown. |
| `RenderFormForAFormNotGrantedToCallersRoleThrowsNotFoundAsync` | Fact | Render form for a form not granted to callers role throws not found. |
| `RenderFormWithoutPermissionThrowsForbiddenAsync` | Fact | Render form without permission throws forbidden. |
| `RenderFormEntriesDrawsEachEntryReadOnlyWithItsValuesAndTheValuesItHasNoFieldForAsync` | Fact | Render form entries draws each entry read only with its values and the values it has no field for. |
| `RenderFormEntriesListsTheNewestEntryFirstAsync` | Fact | Render form entries lists the newest entry first. |
| `RenderFormEntriesReportsATemplateThatNoLongerCompilesAndStillListsTheValuesAsync` | Fact | Render form entries reports a template that no longer compiles and still lists the values. |
| `RenderFormEntriesSkipsAnEntryOfAFormTheCallersRoleDoesNotHoldAsync` | Fact | Render form entries skips an entry of a form the callers role does not hold. |
| `RenderFormEntriesWithoutPermissionThrowsForbiddenAsync` | Fact | Render form entries without permission throws forbidden. |
| `ExecuteDrawsADisplayFromTheRowsOfItsSqlAsync` | Fact | Execute draws a display from the rows of its sql. |
| `ExecuteRefusesTheEarlierRootsInADisplayThatHasSqlAsync` | Fact | Execute refuses the earlier roots in a display that has sql. |
| `ExecuteReportsADisplaySqlThatFailsAsTemplateInvalidAsync` | Fact | Execute reports a display sql that fails as template invalid. |
| `RenderFormDrawsAFormFromTheRowsOfItsSqlAsync` | Fact | Render form draws a form from the rows of its sql. |
| `RenderFormEntriesDrawsAnEntryFromItsSnapshotWhateverTheFormBecomesAsync` | Fact | Render form entries draws an entry from its snapshot whatever the form becomes. |
| `ListDispatchesReturnsTheCaseKeysCallsNewestFirstAsync` | Fact | List dispatches returns the case keys calls newest first. |
| `TestSqlRunsForTheCaseAndReturnsTheColumnsAndRowsAsync` | Fact | Test sql runs for the case and returns the columns and rows. |
| `TestSqlRefusesBlockedTablesUnknownParametersAndWritesWithoutRaisingAsync` | Fact | Test sql refuses blocked tables unknown parameters and writes without raising. |
| `TestSqlNeedsTheSqlAuthoringPermissionAsync` | Fact | Test sql needs the sql authoring permission. |
| `TestSqlForACaseOutsideTheCallersTenantThrowsNotFoundAsync` | Fact | Test sql for a case outside the callers tenant throws not found. |
| `ExecuteUnknownCaseThrowsNotFoundAndLogsWarnNotErrorAsync`  | Fact   | Execute unknown case throws not found and logs warn not error.   |
| `ExecuteUnknownDisplayThrowsNotFoundAsync`                  | Fact   | Execute unknown display throws not found.                        |
| `ExecuteWithSoftDeletedStatusThrowsNotFoundAsync`           | Fact   | Execute with soft deleted status throws not found.               |
| `TenantBCannotRenderTenantACaseOrDisplayAndViceVersaAsync`  | Fact   | Tenant b cannot render tenant a case or display and vice versa.  |
| `CaseWhoseWorkflowIsNotGrantedToCallersRoleIsNotFoundAsync` | Fact   | Case whose workflow is not granted to callers role is not found. |
| `CaseWhoseStatusIsNotGrantedToCallersRoleIsNotFoundAsync`   | Fact   | Case whose status is not granted to callers role is not found.   |
| `DisplayNotGrantedToCallersRoleIsNotFoundAsync`             | Fact   | Display not granted to callers role is not found.                |
| `InactiveOrDeletedDisplayIsNotFoundAsync`                   | Theory | Inactive or deleted display is not found.                        |
| `CaseInDeletedWorkflowIsNotFoundAsync`                      | Fact   | Case in deleted workflow is not found.                           |
| `PreCancelledTokenThrowsOperationCanceledAsync`             | Fact   | Pre cancelled token throws operation canceled.                   |
| `PermissionDeniedLogsWarnNotErrorAsync`                     | Fact   | Permission denied logs warn not error.                           |
| `AuditLogGetsExactlyOneLineAsync`                           | Fact   | Audit log gets exactly one line.                                 |
| `ReadsAndFailuresPublishNothingAsync`                       | Fact   | Reads and failures publish nothing.                              |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`       | Fact   | Catalogue registers unique pascal case no underscore names.      |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`      | Fact   | Permission denied message resolves for french culture.           |

## Case workflow form entry by case key value

### CaseWorkflowFormEntryByCaseKeyValueServiceTests

`Jube.Test.Service.Query.CaseWorkflowFormEntryByCaseKeyValue.CaseWorkflowFormEntryByCaseKeyValueServiceTests` — 22
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

## Case workflow macro execution

### CaseWorkflowMacroExecutionServiceTests

`Jube.Test.Service.Query.CaseWorkflowMacroExecution.CaseWorkflowMacroExecutionServiceTests` — 23 tests. Category:
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
| `ExecuteInSqlModePostsTheRowsAndAuditsTheCallAsync` | Fact | Execute in sql mode posts the rows and audits the call. |
| `ExecuteInSqlModeRefusesAnEndpointWhoseHostComesFromTheRowsAsync` | Fact | Execute in sql mode refuses an endpoint whose host comes from the rows. |
| `ExecuteInSqlModeRefusesEveryChannelWhenTheSqlFailsAsync` | Fact | Execute in sql mode refuses every channel when the sql fails. |
| `ExecuteInSqlModeRefusesAnEmailDestinationThatIsNotAnAddressAsync` | Fact | Execute in sql mode refuses an email destination that is not an address. |
| `ExecuteInPayloadModeAuditsTheEarlierCallAsync` | Fact | Execute in payload mode audits the earlier call. |
| `ExecuteAuditsAFailedCallWithoutRaisingAsync` | Fact | Execute audits a failed call without raising. |
| `PreCancelledTokenThrowsOperationCanceledAndCallsNothingAsync`                 | Fact   | Pre cancelled token throws operation canceled and calls nothing.                       |
| `SuccessEmitsOneAuditLineOneSpanAndNoChangeEventAsync`                         | Fact   | Success emits one audit line one span and no change event.                             |
| `ForbiddenAndNotFoundPublishNothingAndAuditTheirOutcomeAsync`                  | Fact   | Forbidden and not found publish nothing and audit their outcome.                       |
| `ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue`                   | Fact   | Tool is declared on the service method and published by the catalogue.                 |

### CaseHoldServiceTests

`Jube.Test.Service.Query.CaseHold.CaseHoldServiceTests` — 24 tests. Category: Service. Collection: Database.

| Test                                                                                         | Kind | Verifies                                                                                               |
|----------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                                   | Theory | Create with null or blank user name throws not authenticated.                                          |
| `CreateWithAUserWithoutATenantThrowsNotAuthenticatedAsync`                                   | Fact | Create with a user without a tenant throws not authenticated.                                          |
| `ListGivesTheHoldsOfTheCaseWithTheRangeTheDeadlineAndTheMessagesAsync`                       | Fact | List gives the holds of the case with the range the deadline and the messages.                         |
| `ListIncludesOtherTransactionsWithTheSameKeyAndLeavesOutOtherKeysAsync`                      | Fact | List includes other transactions with the same key and leaves out other keys.                          |
| `ListWithoutThePermissionOrForACaseOfAnotherTenantIsRefusedAsync`                            | Fact | List without the permission or for a case of another tenant is refused.                                |
| `ReleaseCallsTheHookWithTheCredentialAndOnA200RecordsReleasedWithItsDecisionAndVersionAsync` | Fact | Release calls the hook with the credential and on A200 records released with its decision and version. |
| `AnAnswerOtherThanExactly200LeavesThePaymentHeldAndRecordsTheFailedAttemptAsync`             | Theory | An answer other than exactly200 leaves the payment held and records the failed attempt.                |
| `ASecondReleaseOfTheSamePaymentIsRefusedAndTheHookIsNotCalledAgainAsync`                     | Fact | A second release of the same payment is refused and the hook is not called again.                      |
| `TwoReleasesAtOnceCallTheHookExactlyOnceAsync`                                               | Fact | Two releases at once call the hook exactly once.                                                       |
| `ReleaseWithoutThePermissionIsRefusedAndNothingIsCalledAsync`                                | Fact | Release without the permission is refused and nothing is called.                                       |
| `ReleaseWithNoHandlingHookIsRefusedAndNothingChangesAsync`                                   | Fact | Release with no handling hook is refused and nothing changes.                                          |
| `TheHookWhoseSourceFieldMatchesThePayloadHandlesItAndAnInactiveHookDoesNotAsync`             | Fact | The hook whose source field matches the payload handles it and an inactive hook does not.              |
| `AHookWhoseAddressIsNotLiteralIsRefusedAndRecordedWithoutCallingAnythingAsync`               | Fact | A hook whose address is not literal is refused and recorded without calling anything.                  |
| `AHookWhoseCredentialVariableIsNotSetIsNotCalledAsync`                                       | Fact | A hook whose credential variable is not set is not called.                                             |
| `ACredentialVariableWithoutTheHookPrefixIsNeverReadAsync`                                    | Fact | A credential variable without the hook prefix is never read.                                           |
| `RefuseCallsTheRefuseUrlWhenTheHookHasOneAndRecordsRefusedAsync`                             | Fact | Refuse calls the refuse url when the hook has one and records refused.                                 |
| `RefuseWithoutARefuseUrlOrAnyHookIsRecordedWithoutCallingAnythingAsync`                      | Fact | Refuse without a refuse url or any hook is recorded without calling anything.                          |
| `ADeclinedPaymentCanNeverBeReleasedOrRefusedAsync`                                           | Fact | A declined payment can never be released or refused.                                                   |
| `ADecisionWithoutAReasonIsInvalidAndNothingIsCalledAsync`                                    | Theory | A decision without a reason is invalid and nothing is called.                                          |
| `ACaseLockedByAnotherUserCannotHaveItsPaymentReleasedAsync`                                  | Fact | A case locked by another user cannot have its payment released.                                        |
| `ATransactionThatIsNotOnTheCaseCannotBeReleasedThroughItAsync`                               | Fact | A transaction that is not on the case cannot be released through it.                                   |
| `ACustomBodyIsRenderedWithEveryValueEscapedForJsonAsync`                                     | Fact | A custom body is rendered with every value escaped for json.                                           |
| `AGetHookCallsTheUrlWithoutABodyAsync`                                                       | Fact | A get hook calls the url without a body.                                                               |
| `TheToolCatalogueDeclaresTheThreeOperationsWithTheirPermissions`                             | Fact | The tool catalogue declares the three operations with their permissions.                               |

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

`Jube.Test.Service.Query.EntityAnalysisModelActivationRuleOverrideQuery.EntityAnalysisModelActivationRuleOverrideQueryServiceTests` — 22 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.EntityAnalysisModelBacktest.EntityAnalysisModelBacktestServiceTests` — 16 tests. Category:
Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                             |
|-----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `AnActivationRuleIsCountedAgainstATagClassOfArchivedTransactionsAsync`      | Fact | An activation rule is counted against a tag class of archived transactions.          |
| `ADeletedTagNoLongerCountsAsync`                                            | Fact | A deleted tag no longer counts.                                                      |
| `TheFilterBuilderChoosesTheTransactionsAsync`                               | Fact | The filter builder chooses the transactions.                                         |
| `TheRangeAndLimitChooseTheNewestTransactionsInRangeAsync`                   | Fact | The range and limit choose the newest transactions in range.                         |
| `WithoutAClassOnlyFiringIsCountedAsync`                                     | Fact | Without a class only firing is counted.                                              |
| `AReprocessingRuleRunsAsAGatewayRuleInReprocessingModeAsync`                | Fact | A reprocessing rule runs as a gateway rule in reprocessing mode.                     |
| `AnInvalidRequestComesBackAsErrorsWithTheirPartAsync`                       | Fact | An invalid request comes back as errors with their part.                             |
| `TheFieldsIncludeTheModelsTagsForTheClassOnlyAsync`                         | Fact | The fields include the models tags for the class only.                               |
| `ASubmittedBacktestIsListedAndCanBeStoppedBeforeItStartsAsync`              | Fact | A submitted backtest is listed and can be stopped before it starts.                  |
| `ABacktestThreadRunsASubmittedBacktestToTheSameCountsAsync`                 | Fact | A backtest thread runs a submitted backtest to the same counts.                      |
| `WithAReportConnectionTheArchiveIsReadThroughItAndTheCountsAreTheSameAsync` | Fact | With a report connection the archive is read through it and the counts are the same. |
| `TheArchiveIsReadOnlyThroughTheReportConnectionWhenOneIsSetAsync`           | Fact | The archive is read only through the report connection when one is set.              |
| `ABacktestThreadReadsTheArchiveThroughTheReportConnectionAsync`             | Fact | A backtest thread reads the archive through the report connection.                   |
| `AnExplanationSaysWhyATransactionWasCountedAsync`                           | Fact | An explanation says why a transaction was counted.                                   |
| `AnotherTenantsModelIsNotFoundAndNoPermissionIsForbiddenAsync`              | Fact | Another tenants model is not found and no permission is forbidden.                   |
| `EveryOperationIsCatalogued`                                                | Fact | Every operation is catalogued.                                                       |

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

`Jube.Test.Service.Query.EntityAnalysisModelIntegrity.EntityAnalysisModelIntegrityServiceTests` — 8 tests. Category:
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

`Jube.Test.Service.Query.EntityAnalysisModelSample.EntityAnalysisModelSampleServiceTests` — 30 tests. Category: Service.
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

`Jube.Test.Service.Query.EntityAnalysisModelSynchronisationNodeStatusEntries.EntityAnalysisModelSynchronisationNodeStatusEntriesServiceTests` — 20 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.EntityAnalysisPotentialMultiPartStringNames.EntityAnalysisPotentialMultiPartStringNamesServiceTests` — 17 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeServiceTests` — 19 tests. Category: Service. Collection: Database.

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

### CaseWorkflowReleaseHookVersionHistoryServiceTests

`Jube.Test.Service.Query.CaseWorkflowReleaseHookVersionHistory.CaseWorkflowReleaseHookVersionHistoryServiceTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                    |
|-----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `EveryUpdateLeavesTheEarlierStateAsAVersionOldestFirstAndTheLatestIsTheLastAsync` | Fact | Every update leaves the earlier state as a version oldest first and the latest is the last. |
| `CompareReportsTheFieldsThatChangedBetweenTwoVersionsAsync`                       | Fact | Compare reports the fields that changed between two versions.                               |
| `WithoutPermission69TheHistoryIsRefusedAsync`                                     | Fact | Without permission69 the history is refused.                                                |
| `AnotherTenantSeesNoVersionsAndCannotFetchOneByIdOrCompareAsync`                  | Fact | Another tenant sees no versions and cannot fetch one by id or compare.                      |
| `ABlankUserIsNotAuthenticatedAsync`                                               | Fact | A blank user is not authenticated.                                                          |

## Exhaustive search instance promoted trial instance confusion

### ExhaustiveSearchInstancePromotedTrialInstanceConfusionServiceTests

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion.ExhaustiveSearchInstancePromotedTrialInstanceConfusionServiceTests` — 22 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramServiceTests` — 14 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveServiceTests` — 17 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual.ExhaustiveSearchInstancePromotedTrialInstancePredictedActualServiceTests` — 18 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery.ExhaustiveSearchInstancePromotedTrialInstanceQueryServiceTests` — 17 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc.ExhaustiveSearchInstancePromotedTrialInstanceRocServiceTests` — 17 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionServiceTests` — 22 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstancePromotionHistory.ExhaustiveSearchInstancePromotionHistoryServiceTests` — 18 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable.ExhaustiveSearchInstanceTrialInstanceVariableServiceTests` — 26 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariableVariance.ExhaustiveSearchInstanceTrialInstanceVariableVarianceServiceTests` — 20 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.ExhaustiveSearchInstanceVariableStatistics.ExhaustiveSearchInstanceVariableStatisticsServiceTests` — 15 tests. Category: Service. Collection: Database.

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

| Test | Kind | Verifies |
|------|------|----------|
| `TheModelOffersItsChildrenInTheGroupsTheMenuUses` | Fact | The model offers its children in the groups the menu uses. |
| `ACaseWorkflowOffersTheKindsThatSitUnderIt` | Fact | A case workflow offers the kinds that sit under it. |
| `EveryChildIsAKnownAreaKeyedToItsParentAndListedByARouteThatExists` | Theory | Every child is a known area keyed to its parent and listed by a route that exists. |
| `TheHierarchyHasNoCycleAndNoAreaHasTwoParents` | Fact | The hierarchy has no cycle and no area has two parents. |
| `TheModelIsTheRootOfTheDeepestChainAndNoChainIsDeeperThanThreeLevels` | Fact | The model is the root of the deepest chain and no chain is deeper than three levels. |
| `TheChildLabelsAndHubsAreTranslatedWhenARuleSaysSoAndKeepTheEnglishOtherwise` | Fact | The child labels and hubs are translated when a rule says so and keep the English otherwise. |

### FormModelEditorTests

`Jube.Test.Service.Query.FormModel.FormModelEditorTests` — 15 tests. Category: Unit.

| Test                                                                      | Kind   | Verifies                                                                                 |
|---------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `TheDatasourceEditorStandsInForTheFivePropertiesThatMakeUpADatasourceDisplay` | Fact | The datasource editor stands in for the five properties that make up a datasource display. |
| `TheLegacyKendoDisplayPropertiesAreNoLongerFormFields` | Fact | The legacy kendo display properties are no longer form fields. |
| `EveryRuleParseTypeEqualsTheParserConstantOfTheSameName`                  | Fact   | Every rule parse type equals the parser constant of the same name.                       |
| `TheRuleEditorsReachTheModelWithTheirParseTypeAndRoles`                   | Fact   | The rule editors reach the model with their parse type and roles.                        |
| `ACoderOnlyRuleEditorNamesOnlyTheCoderProperty`                           | Fact   | A coder only rule editor names only the coder property.                                  |
| `TheCaseFilterEditorClaimsItsSelectAndFilterJson`                         | Fact   | The case filter editor claims its select and filter json.                                |
| `TheExhaustiveDatasetEditorClaimsBothDatasetIdsAndOnlyReadsTheSourceAndStatus` | Fact | The Exhaustive dataset editor claims both dataset ids and only reads the source and status. |
| `TheExhaustiveFilterEditorClaimsTheThreeFilterPropertiesAndReadsTheDataSource` | Fact | The Exhaustive filter editor claims the three filter properties and reads the data source. |
| `TheDataSourceAndStatusAnEditorReadsStayOrdinaryFormFields` | Fact | The data source and status an editor reads stay ordinary form fields. |
| `EveryEditorRegistersWithoutAFailure`                                     | Fact   | Every editor registers without a failure.                                                |
| `EveryClaimedFieldIsMarkedWithItsEditorAndNoFieldIsClaimedTwice`          | Theory | Every claimed field is marked with its editor and no field is claimed twice.             |
| `ABuilderCapableRuleTypeCarriesAllFourRolesAndACoderOnlyTypeJustTheCoder` | Theory | A builder capable rule type carries all four roles and a coder only type just the coder. |
| `AnEditorIsGivenItsContextByTheParentKeyOfItsRecord`                      | Theory | An editor is given its context by the parent key of its record.                          |
| `AFieldLevelEditorAttributeNamesOnlyAComponentTheFormCannotDraw`          | Fact   | A field level editor attribute names only a component the form cannot draw.              |
| `EveryFieldEditorIsOneTheRendererKnows`                                   | Theory | Every field editor is one the renderer knows.                                            |

### FormModelLocalisationTests

`Jube.Test.Service.Query.FormModel.FormModelLocalisationTests` — 14 tests. Category: Unit.

| Test                                                                        | Kind   | Verifies |
|-----------------------------------------------------------------------------|--------|----------|
| `ATranslatedLabelReplacesTheEnglishOneWhereverTheModelShowsText`            | Fact   | A translated label replaces the English one wherever the model shows text. |
| `AnOptionHelpAndPlaceholderAreTranslatedByTheirOwnKeys`                     | Fact   | An option, help and placeholder are translated by their own keys. |
| `AnUntranslatedLabelKeepsTheEnglishFromTheDto`                              | Fact   | An untranslated label keeps the English from the DTO. |
| `LocalisingNeverChangesTheSharedEnglishModel`                               | Fact   | Localising never changes the shared English model. |
| `TheLocalisedModelIsBuiltOncePerCultureAndKeptApartFromOtherCultures`       | Fact   | The localised model is built once per culture and kept apart from other cultures. |
| `AnUnknownAreaHasNoLocalisedModel`                                          | Fact   | An unknown area has no localised model. |
| `EveryOptionKeyIsUniqueToItsSetAndMember`                                   | Fact   | Every option key names its enum set and member. |
| `EveryTranslationKeyInTheResourcesNamesALabelTheModelsReallyHave`           | Theory | Every translation key in the resources names a label the models really have. |
| `TheRealTranslationsReachTheTagModelThroughTheLocalizer`                    | Theory | The real translations reach the tag model through the localizer. |
| `AnEnglishRequestGetsTheEnglishLabelsBecauseNoEnglishResourceOverridesThem` | Fact   | An English request gets the English labels because no English resource overrides them. |
| `ATranslatedDescriptionReplacesTheEnglishOneAndAMissingOneKeepsIt` | Fact | A translated description replaces the English one and a missing one keeps it. |
| `EveryTranslatedTextWasMadeFromTheEnglishItNowHas` | Theory | Every translated description or placeholder was made from the English it now has. |
| `TheUntranslatedTextsAreExactlyThoseInTheBaseline` | Theory | The untranslated descriptions and placeholders are exactly those in the baseline. |
| `ATranslatedPlaceholderReplacesTheEnglishOneAndAFieldWithNoneStaysWithNone` | Fact | A translated placeholder replaces the English one and a field with none stays with none. |

### FormModelOptionTests

`Jube.Test.Service.Query.FormModel.FormModelOptionTests` — 11 tests. Category: Unit.

| Test                                                                                  | Kind   | Verifies                                                                                               |
|---------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------|
| `AnEnumGivesNumericChoicesInValueOrderLabelledByDescriptionOrTheHumanisedName`        | Fact   | An enum gives numeric choices in value order labelled by description or the humanised name.            |
| `AnAllowedSubsetKeepsOnlyTheNamedMembers`                                             | Fact   | An allowed subset keeps only the named members.                                                        |
| `ATextPropertyTakesItsValuesFromEnumMemberAndItsLabelsFromDescription`                | Fact   | A text property takes its values from enum member and its labels from description.                     |
| `AMemberWithNoStoredTextOnATextPropertyAndAnUnknownAllowedMemberAreRecordedNotThrown` | Fact   | A member with no stored text on a text property and an unknown allowed member are recorded not thrown. |
| `TheRealOptionsKeepTheGapsAndTheStoredLettersTheOldPagesUsed`                         | Fact   | The real options keep the gaps and the stored letters the old pages used.                              |
| `EveryOptionSetRegistersWithoutAFailure`                                              | Fact   | Every option set registers without a failure.                                                          |
| `EveryChoiceFieldHasOptionsOrALookup`                                                 | Theory | Every choice field has options or a lookup.                                                            |
| `EveryWidgetIsInTheClosedVocabulary`                                                  | Theory | Every widget is in the closed vocabulary.                                                              |
| `OptionValuesAreUniqueAndEveryOptionIsLabelled`                                       | Theory | Option values are unique and every option is labelled.                                                 |
| `EveryNewDefaultOfAChoiceFieldIsOneOfItsOptions`                                      | Theory | Every new default of a choice field is one of its options.                                             |
| `EveryOptionIsInsideTheValueDomainEveryValidatorRuleAllowsWhateverItsCondition`       | Theory | Every option is inside the value domain every validator rule allows whatever its condition.            |

### FormModelRelationTests

`Jube.Test.Service.Query.FormModel.FormModelRelationTests` — 11 tests. Category: Unit.

| Test                                                                             | Kind   | Verifies                                                                                          |
|----------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `EveryRelationResolvesWithoutAFailure`                                           | Fact   | Every relation resolves without a failure.                                                        |
| `AnApprovalTakesItsKindFromTheAreaNameAndItsHistoryFromTheKind`                  | Fact   | An approval takes its kind from the area name and its history from the kind.                      |
| `TheHttpAdaptationAreaIsNamedDifferentlyFromItsApprovalKindAndSaysSo`            | Fact   | The http adaptation area is named differently from its approval kind and says so.                 |
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

| Test                                                                                      | Kind   | Verifies |
|-------------------------------------------------------------------------------------------|--------|----------|
| `TheLanguageTheClientAsksForPicksTheResourceCultureAndAnUnsupportedOneFallsBackToEnglish` | Theory | The language the client asks for picks the resource culture and an unsupported one falls back to English. |
| `TheFormattingCultureStaysEnglishWhateverTheClientAsksFor`                                | Theory | The formatting culture stays English whatever the client asks for. |
| `NoLanguageHeaderGivesEnglish`                                                            | Fact   | No language header gives English. |

### FormModelRuleTests

`Jube.Test.Service.Query.FormModel.FormModelRuleTests` — 19 tests. Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                          |
|-----------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `AnUnconditionalNotEmptyRuleMakesAFieldRequired`                      | Fact   | An unconditional not empty rule makes a field required.                           |
| `ANotEmptyRuleUnderAConditionIsNotRequiredButIsFlaggedConditional`    | Fact   | A not empty rule under a condition is not required but is flagged conditional.    |
| `MaximumAndMinimumLengthAreCarriedAcrossEvenUnderACondition`          | Fact   | Maximum and minimum length are carried across even under a condition.             |
| `AnInclusiveBetweenRuleGivesBothLimits`                               | Fact   | An inclusive between rule gives both limits.                                      |
| `AnExclusiveBetweenRuleMarksBothLimitsExclusive`                      | Fact   | An exclusive between rule marks both limits exclusive.                            |
| `GreaterThanAndLessThanOrEqualGiveAMinimumAndAMaximum`                | Fact   | Greater than and less than or equal give a minimum and a maximum.                 |
| `ARangeUnderAConditionIsStillCarriedBecauseItBoundsTheValueWhenUsed`  | Fact   | A range under a condition is still carried because it bounds the value when used. |
| `TheTightestOfSeveralLimitsWins`                                      | Fact   | The tightest of several limits wins.                                              |
| `MergingTwoValidatorsOfTheSameDtoKeepsTheTighterLimitAndAnyRequired`  | Fact   | Merging two validators of the same dto keeps the tighter limit and any required.  |
| `OnlyAnAnchoredWholeValuePatternWithNoDotNetOnlySyntaxIsTranslated`   | Theory | Only an anchored whole value pattern with no dot net only syntax is translated.   |
| `AMatchesRuleOnAFieldReachesTheModelAsAPattern`                       | Fact   | A matches rule on a field reaches the model as a pattern.                         |
| `AnIntegerFieldGetsItsExclusiveMinimumAdjustedToTheFirstAllowedValue` | Fact   | An integer field gets its exclusive minimum adjusted to the first allowed value.  |
| `ARangeFromTheValidatorsReachesTheSliderAndAnUnboundedFieldIsNotOne`  | Fact   | A range from the validators reaches the slider and an unbounded field is not one. |
| `TheTagNameIsRequiredAndLimitedToTheValidatorsLength`                 | Fact   | The tag name is required and limited to the validators length.                    |
| `EveryValidatorOfAFormDtoBuildsWithoutItsServices`                    | Fact   | Every validator of a form dto builds without its services.                        |
| `EverySliderAndPercentFieldHasARangeFromAValidator`                   | Theory | Every slider and percent field has a range from a validator.                      |
| `EveryRegularExpressionRuleOnAFormFieldIsCarriedToTheModel`           | Theory | Every regular expression rule on a form field is carried to the model.            |
| `EveryConditionalRequiredRuleIsDeclaredAsARequiredWhenOnTheDto`       | Theory | Every conditional required rule is declared as a required when on the dto.        |
| `NoConditionCarriesAnOperatorAmongItsValues`                          | Theory | No condition carries an operator among its values.                                |

### FormModelRegistryTests

`Jube.Test.Service.Query.FormModel.FormModelRegistryTests` — 21 tests. Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                        |
|-----------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `EveryDtoWithAFormEndpointIsRegisteredUnderItsArea`                   | Fact   | Every dto with a form endpoint is registered under its area.                    |
| `AnAreaIsFoundWhateverTheCase`                                        | Fact   | An area is found whatever the case.                                             |
| `AnUnknownAreaIsNotFound`                                             | Fact   | An unknown area is not found.                                                   |
| `TheModelNamesItsEndpointAndKeysInCamelCase`                          | Fact   | The model names its endpoint and keys in camel case.                            |
| `AnAreaDeclaresAllOperationsUnlessItsDtoSaysOtherwise`                | Fact   | An area declares all operations unless its dto says otherwise.                  |
| `GroupsAreOrderedAndCarryTheirCollapsedState`                         | Fact   | Groups are ordered and carry their collapsed state.                             |
| `FieldsAreOrderedByGroupThenPositionAndOnlyDecoratedPropertiesAppear` | Fact   | Fields are ordered by group then position and only decorated properties appear. |
| `AFieldCarriesItsTypeWidgetDefaultAndDescription`                     | Fact   | A field carries its type widget default and description.                        |
| `ATypeAndItsNullabilityAreReadFromTheProperty`                        | Theory | A type and its nullability are read from the property.                          |
| `ALabelIsTheAttributeLabelOrTheHumanisedPropertyName`                 | Fact   | A label is the attribute label or the humanised property name.                  |
| `ListColumnsCarryTheirTitleOrderAndHiddenColumnsComeLast`             | Fact   | List columns carry their title order and hidden columns come last.              |
| `ALookupIsDescribedWithItsSourceAndFields`                            | Fact   | A lookup is described with its source and fields.                               |
| `EveryFieldNamesAGroupThatIsDeclared`                                 | Theory | Every field names a group that is declared.                                     |
| `NoFieldNameOrListColumnIsDuplicated`                                 | Theory | No field name or list column is duplicated.                                     |
| `EveryConditionAndKeyNamesAFieldOfTheSameRecord`                      | Theory | Every condition and key names a field of the same record.                       |
| `AConditionKeepsItsOperatorValuesAndMatchMode`                        | Fact   | A condition keeps its operator values and match mode.                           |
| `NoFieldLabelShowsAnIdentifierWordTheUserHasNoUseFor` | Theory | No field label shows an identifier word the user has no use for. |
| `EveryFieldLabelIsUniqueWithinItsArea` | Theory | Every field label is unique within its area. |
| `NoRadioSetOffersMoreThanThreeChoicesAndEverySpanIsOneOrTwo` | Theory | No radio set offers more than three choices and every span is one or two. |
| `ARadioSetWithMoreThanThreeChoicesIsDrawnAsASelectAndTwoOrThreeStayRadios` | Fact | A radio set with more than three choices is drawn as a select and two or three stay radios. |
| `AFieldAskingForTwoColumnsKeepsThemAndTheDefaultIsOne` | Fact | A field asking for two columns keeps them and the default is one. |

### FormModelRouteTests

`Jube.Test.Service.Query.FormModel.FormModelRouteTests` — 6 tests. Category: Unit.

| Test                                                   | Kind   | Verifies                                                       |
|--------------------------------------------------------|--------|----------------------------------------------------------------|
| `EveryEndpointMapperRegistersWithoutNeedingAnyService` | Fact   | Every endpoint mapper registers without needing any service.   |
| `TheCatalogueSeesTheFormModelRoutesItself`             | Fact   | The catalogue sees the form model routes itself.               |
| `EveryFormAreaHasTheRoutesForTheOperationsItDeclares`  | Theory | Every form area has the routes for the operations it declares. |
| `EveryFormAreaDeclaresAtLeastOneOperation`             | Theory | Every form area declares at least one operation.               |
| `EveryLookupSourceIsARouteThatExists`                  | Theory | Every lookup source is a route that exists.                    |
| `EveryLookupWithAParentTakesItAsTheLastSegmentOfARouteAndNamesAFieldOfTheArea` | Theory | Every lookup with a parent takes it as the last segment of a route and names a field of the area. |

### GridModelRegistryTests

`Jube.Test.Service.Query.GridModel.GridModelRegistryTests` — 7 tests. Category: Unit.

| Test | Kind | Verifies |
|------|------|----------|
| `EveryDecoratedGridRegistersWithoutAFailure` | Fact | Every decorated grid registers without a failure. |
| `TheArchiverStageGridCarriesItsTimeWindowStageFilterRatioAndOrder` | Fact | The archiver stage grid carries its time window, stage filter, ratio and order. |
| `ASearchOnlyGridHasASearchBoxAndAWindow` | Fact | A search only grid has a search box and a window. |
| `ASnapshotGridHasNoWindowAndNamesItsSort` | Fact | A snapshot grid has no window and names its sort. |
| `EveryGridEndpointIsARouteThatTakesTheParametersTheGridWillSend` | Theory | Every grid endpoint is a route that takes the parameters the grid will send. |
| `EveryGridColumnHasATitleAndNoTwoColumnsShareAField` | Theory | Every grid column has a title and no two columns share a field. |
| `TheGridTitlesColumnsAndFiltersAreTranslatedWhenARuleSaysSoAndKeepTheEnglishOtherwise` | Fact | The grid titles, columns and filters are translated when a rule says so and keep the English otherwise. |

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

`Jube.Test.Service.Query.Parser.ParserServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                            | Kind   | Verifies                                                          |
|-----------------------------------------------------------------|--------|-------------------------------------------------------------------|
| `NullOrBlankUserIsNotAuthenticatedAsync`                        | Theory | Null or blank user is not authenticated.                          |
| `UnmappedAndNoTenantUsersAreNotAuthenticatedAsync`              | Fact   | Unmapped and no tenant users are not authenticated.               |
| `MissingPermissionIsForbiddenWithCodeAndSpecsAsync`             | Fact   | Missing permission is forbidden with code and specs.              |
| `AnySingleSpecificationOfTheSetPermitsAsync`                    | Theory | Any single specification of the set permits.                      |
| `UnrelatedSpecificationDoesNotPermitAsync`                      | Fact   | Unrelated specification does not permit.                          |
| `ValidRuleCompilesAsync`                                        | Fact   | Valid rule compiles.                                              |
| `UnknownFieldReturnsErrorWithLocatedSpansAsync`                 | Fact   | Unknown field returns error with located spans.                   |
| `CompilerErrorsAreReturnedWithMessageAndSpansAsync`             | Fact   | Compiler errors are returned with message and spans.              |
| `EmptyRuleTextIsHandledWithoutThrowingAsync`                    | Fact   | Empty rule text is handled without throwing.                      |
| `NullRuleTextIsSwallowedIntoErrorResultAsync`                   | Fact   | Null rule text is swallowed into error result.                    |
| `ForeignTenantModelFieldsAreNotVisibleToTheParserAsync`         | Fact   | Foreign tenant model fields are not visible to the parser.        |
| `SameRuleCompilesForOwnerTenantButNotForeignTenantAsync`        | Fact   | Same rule compiles for owner tenant but not foreign tenant.       |
| `ForeignTenantInlineFunctionIsNotVisibleAsync`                  | Fact   | Foreign tenant inline function is not visible.                    |
| `InlineFunctionIsResolvedByTheParserAsync`                      | Fact   | Inline function is resolved by the parser.                        |
| `InlineScriptPublicPropertiesAreResolvedByTheParserAsync`       | Fact   | Inline script public properties are resolved by the parser.       |
| `HttpAndExhaustiveAdaptationsAreVisibleForActivationRulesAsync` | Fact   | Http and exhaustive adaptations are visible for activation rules. |
| `PreCancelledTokenThrowsOperationCanceledAsync`                 | Fact   | Pre cancelled token throws operation canceled.                    |
| `AuditLogGetsExactlyOneLineAndNoChangeEventAsync`               | Fact   | Audit log gets exactly one line and no change event.              |
| `ToolCatalogueListsParserParseOnce`                             | Fact   | Tool catalogue lists parser parse once.                           |

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

`Jube.Test.Service.Query.RuleVocabulary.RuleVocabularyServiceTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                        | Kind | Verifies                                                           |
|-------------------------------------------------------------|------|--------------------------------------------------------------------|
| `FunctionsCanBeFoundByNameAndByTheTypeTheyAreCalledOnAsync` | Fact | Functions can be found by name and by the type they are called on. |
| `PagingContinuesAfterTheLastNameAsync`                      | Fact | Paging continues after the last name.                              |
| `DescribeGivesEverySignatureAndIgnoresCaseAsync`            | Fact | Describe gives every signature and ignores case.                   |
| `NoPermissionIsForbiddenAsync`                              | Fact | No permission is forbidden.                                        |

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

`Jube.Test.Service.Query.VisualisationRegistryDatasourceCommandExecution.VisualisationRegistryDatasourceCommandExecutionServiceTests` — 41 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Query.VisualisationRegistryDatasourceCommandExecution.VisualisationRegistryDatasourceCardServiceTests` — 20 tests. Category: Service. Collection: Database.

| Test                                                                           | Kind   | Verifies                                                                                    |
|--------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `ASavedChartComesBackAsAnSvgWithTheCardIdentityAndNoGridWhenTheGridIsOffAsync` | Fact   | A saved chart comes back as an svg with the card identity and no grid when the grid is off. |
| `WithTheGridOnTheRowsComeAlongAndWithNoChartKindThereIsNoDisplayAsync`         | Fact   | With the grid on the rows come along and with no chart kind there is no display.            |
| `StoredColumnRolesChooseTheSeriesAsync`                                        | Fact   | Stored column roles choose the series.                                                      |
| `TheStylesheetIsOnlySentToACallerWhoDoesNotAlreadyHoldItsVersionAsync`         | Fact   | The stylesheet is only sent to a caller who does not already hold its version.              |
| `ARegistryShownInACaseReceivesTheCaseIdAsItsIdAsync` | Fact | A registry shown in a case receives the case id as its id. |
| `ACaseOfAnotherTenantIsNotReachableThroughTheIdAsync` | Fact | A case of another tenant is not reachable through the id. |
| `TheDirectoryHasNoIdWhateverIsSuppliedAsync` | Fact | The directory has no id whatever is supplied. |
| `EveryCardIsWrittenToTheExecutionLogWithItsRowsAndParametersAsync`             | Fact   | Every card is written to the execution log with its rows and parameters.                    |
| `AFailingQueryIsAnErrorCardAndIsLoggedWithItsErrorAsync`                       | Fact   | A failing query is an error card and is logged with its error.                              |
| `AQueryWithNoRowsIsEmptyAsync`                                                 | Fact   | A query with no rows is empty.                                                              |
| `RowsOverTheLimitAreFlaggedAndTheLogRecordsWhatWasReadAsync`                   | Fact   | Rows over the limit are flagged and the log records what was read.                          |
| `TheCardNeverCarriesTheSqlAsync`                                               | Fact   | The card never carries the sql.                                                             |
| `ADatasourceTheCallerCannotSeeIsNotFoundAndNotLoggedAsync`                     | Fact   | A datasource the caller cannot see is not found and not logged.                             |
| `AnInactiveDatasourceIsNotFoundAsync`                                          | Fact   | An inactive datasource is not found.                                                        |
| `AnUnknownDatasourceIsNotFoundAsync`                                           | Fact   | An unknown datasource is not found.                                                         |
| `ACallerWithoutThePermissionIsForbiddenAsync`                                  | Fact   | A caller without the permission is forbidden.                                               |
| `AParameterValueOfTheWrongTypeOrOfAnotherRegistryOrTwiceIsRefusedAsync`        | Fact   | A parameter value of the wrong type or of another registry or twice is refused.             |
| `AnEmptyParameterEntryIsRefusedNotACrashAsync`                                 | Fact   | An empty parameter entry is refused not a crash.                                            |
| `ACardExplainsItselfInThePageLanguageAsync`                                    | Fact   | A card explains itself in the page language.                                                |
| `AChartThatCannotBeDrawnIsAnErrorCardButTheGridStillComesAsync`                | Fact   | A chart that cannot be drawn is an error card but the grid still comes.                     |

## Watcher stream

### WatcherStreamAuthorizationServiceTests

`Jube.Test.Service.Query.WatcherStream.WatcherStreamAuthorizationServiceTests` — 6 tests. Category: Service. Collection:
Database.

| Test                                                       | Kind   | Verifies                                                      |
|------------------------------------------------------------|--------|---------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync` | Theory | Create with null or blank user name throws not authenticated. |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`         | Fact   | Create with unknown user throws not authenticated.            |
| `TenantlessUserThrowsNotAuthenticatedAsync`                | Fact   | Tenantless user throws not authenticated.                     |
| `MissingPermissionThrowsForbiddenAsync`                    | Fact   | Missing permission throws forbidden.                          |
| `PermittedUserResolvesToTheirOwnTenantAsync`               | Fact   | Permitted user resolves to their own tenant.                  |
| `DifferentTenantsResolveToDifferentTenantIdsAsync`         | Fact   | Different tenants resolve to different tenant ids.            |

## Entity analysis models synchronisation schedules by instance name

### GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryTests

`Jube.Test.Service.Query.EntityAnalysisModelsSynchronisationSchedulesByInstanceName.GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                              | Kind | Verifies                                                                 |
|-------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AWholeTenantRowThatIsDueIsPendingForEveryModelAsync`             | Fact | A whole tenant row that is due is pending for every model.               |
| `ModelRowsThatAreDueNameOnlyThoseModelsAndEachOnceAsync`          | Fact | Model rows that are due name only those models and each once.            |
| `AWholeTenantRowAlongsideAModelRowWidensTheSyncToEveryModelAsync` | Fact | A whole tenant row alongside a model row widens the sync to every model. |
| `ARowOlderThanTheNodesLastSynchronisationIsNotPendingAsync`       | Fact | A row older than the nodes last synchronisation is not pending.          |
| `ARowThatIsNotYetDueIsNotPendingAsync`                            | Fact | A row that is not yet due is not pending.                                |
| `ANodeThatHasNeverSynchronisedHonoursEveryRowThatIsDueAsync`      | Fact | A node that has never synchronised honours every row that is due.        |

## Visualisation registry summary

### GetVisualisationRegistrySummaryQueryTests

`Jube.Test.Service.Query.VisualisationRegistrySummary.GetVisualisationRegistrySummaryQueryTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ARegistryWithNothingRecordedStillGetsARowOfZerosAsync`                     | Fact | A registry with nothing recorded still gets a row of zeros.                        |
| `OnlyActiveUndeletedDatasourcesAndParametersAreCountedAsync`                | Fact | Only active undeleted datasources and parameters are counted.                      |
| `ExecutionsFailuresAndAverageCoverTheWindowButTheLastRunCoversAllTimeAsync` | Fact | Executions failures and average cover the window but the last run covers all time. |
| `ARegistryWhoseOnlyRunsAreOlderThanTheWindowStillShowsWhenItLastRanAsync`   | Fact | A registry whose only runs are older than the window still shows when it last ran. |
| `NoRegistriesMeansNoQueryAsync`                                             | Fact | No registries means no query.                                                      |
