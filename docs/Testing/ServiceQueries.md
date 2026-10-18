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

`Jube.Test.Service.Query.CaseWorkflowDisplayExecution.CaseWorkflowDisplayExecutionServiceTests` — 20 tests. Category:
Service. Collection: Database.

| Test                                                        | Kind   | Verifies                                                         |
|-------------------------------------------------------------|--------|------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`  | Theory | Create with null or blank user name throws not authenticated.    |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`   | Fact   | Create with user having no tenant throws not authenticated.      |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`          | Fact   | Create with unknown user throws not authenticated.               |
| `ExecuteThrowsForbiddenWhenPermissionMissingAsync`          | Fact   | Execute throws forbidden when permission missing.                |
| `ExecuteReplacesPayloadTokensAndLeavesUnknownTokensAsync`   | Fact   | Execute replaces payload tokens and leaves unknown tokens.       |
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

`Jube.Test.Service.Query.CaseWorkflowMacroExecution.CaseWorkflowMacroExecutionServiceTests` — 17 tests. Category:
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
| `PreCancelledTokenThrowsOperationCanceledAndCallsNothingAsync`                 | Fact   | Pre cancelled token throws operation canceled and calls nothing.                       |
| `SuccessEmitsOneAuditLineOneSpanAndNoChangeEventAsync`                         | Fact   | Success emits one audit line one span and no change event.                             |
| `ForbiddenAndNotFoundPublishNothingAndAuditTheirOutcomeAsync`                  | Fact   | Forbidden and not found publish nothing and audit their outcome.                       |
| `ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue`                   | Fact   | Tool is declared on the service method and published by the catalogue.                 |

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
