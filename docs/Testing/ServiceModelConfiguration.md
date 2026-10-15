---
layout: default
title: Service Model Configuration
nav_order: 15
parent: Testing
---

# Service Model Configuration

Tests of the services that configure and invoke entity analysis models: rules, lists, dictionaries, tags, overrides,
approvals and synchronous invocation.

## Entity analysis inline script

### EntityAnalysisInlineScriptServiceTests

`Jube.Test.Service.EntityAnalysisInlineScript.EntityAnalysisInlineScriptServiceTests` — 18 tests. Category: Service.
Collection: Database.

| Test                                                           | Kind   | Verifies                                                           |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------|
| `GetAllReturnsCatalogueRowAsync`                               | Fact   | Get all returns catalogue row.                                     |
| `CatalogueIsNotTenantScopedAndIsVisibleFromEveryTenantAsync`   | Fact   | Catalogue is not tenant scoped and is visible from every tenant.   |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`            | Fact   | Get async throws forbidden when permission missing.                |
| `ListAsyncThrowsForbiddenWhenPermissionMissingAsync`           | Fact   | List async throws forbidden when permission missing.               |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync` | Theory | Null or blank user name throws not authenticated before any query. |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`         | Fact   | User with no user in tenant row throws not authenticated.          |
| `UnknownUserThrowsNotAuthenticatedAsync`                       | Fact   | Unknown user throws not authenticated.                             |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`            | Fact   | Permission denied logs warn not error and gates hold.              |
| `SuccessfulGetLogsNoInfoAsync`                                 | Fact   | Successful get logs no info.                                       |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`      | Fact   | With gates disabled happy path records no debug info or warn.      |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync` | Fact   | Failure logs error with exception attached and still propagates.   |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                      | Fact   | Each call emits one span with outcome tag.                         |
| `EachCallRecordsOneDurationMeasurementAsync`                   | Fact   | Each call records one duration measurement.                        |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                | Fact   | Audit log gets exactly one line including reads.                   |
| `ReadsAndFailuresPublishNothingAsync`                          | Fact   | Reads and failures publish nothing.                                |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`          | Fact   | Catalogue registers unique pascal case no underscore names.        |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`        | Fact   | List async clamps take and paginates deterministically.            |
| `PreCancelledTokenOnGetThrowsOperationCanceledAsync`           | Fact   | Pre cancelled token on get throws operation canceled.              |

## Entity analysis model

### EntityAnalysisModelServiceTests

`Jube.Test.Service.EntityAnalysisModel.EntityAnalysisModelServiceTests` — 60 tests. Category: Service. Collection:
Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `UpdatePreservesOriginalCreatedAuditButSetsUpdatedUserAndDateAsync`                   | Fact   | Update preserves original created audit but sets updated user and date.                     |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `LandlordUserBypassesPermissionChecksButStaysTenantScopedAsync`                       | Fact   | Landlord user bypasses permission checks but stays tenant scoped.                           |
| `LandlordGetAllNeverContainsOtherTenantsRowsAsync`                                    | Fact   | Landlord get all never contains other tenants rows.                                         |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `UserInBothTenantsGetsEachRowOnceFromGetAllAsync`                                     | Fact   | User in both tenants gets each row once from get all.                                       |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameRequiredMessageResolvesPerCultureAsync`                                          | Fact   | Name required message resolves per culture.                                                 |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameTenantFailsButDifferentTenantSucceedsAsync`                   | Fact   | Duplicate name within same tenant fails but different tenant succeeds.                      |
| `DuplicateNameDifferingOnlyByCaseWithinSameTenantFailsAsync`                          | Fact   | Duplicate name differing only by case within same tenant fails.                             |
| `UpdateWithSameNameOnSameRowPassesAsync`                                              | Fact   | Update with same name on same row passes.                                                   |
| `ReferenceDateXPathNotRequiredWhenPayloadLocationIsNowAsync`                          | Fact   | Reference date x path not required when payload location is now.                            |
| `ReferenceDateXPathRequiredWhenPayloadLocationIsBodyAsync`                            | Fact   | Reference date x path required when payload location is body.                               |
| `InvalidReferenceDatePayloadLocationTypeIdIsRejectedAsync`                            | Fact   | Invalid reference date payload location type id is rejected.                                |
| `InvalidCacheTtlIntervalIsRejectedAsync`                                              | Fact   | Invalid cache ttl interval is rejected.                                                     |
| `ActivationWatcherSampleOutOfRangeOnlyRejectedWhenWatcherEnabledAsync`                | Fact   | Activation watcher sample out of range only rejected when watcher enabled.                  |
| `ImplicitAsyncTimeoutOutOfRangeOnlyRejectedWhenEnabledAsync`                          | Fact   | Implicit async timeout out of range only rejected when enabled.                             |
| `ImplicitAsyncTimeoutMessageResolvesPerCultureAsync`                                  | Fact   | Implicit async timeout message resolves per culture.                                        |
| `ImplicitAsyncSwitchAndTimeoutRoundTripAsync`                                         | Fact   | Implicit async switch and timeout round trip.                                               |
| `EnableTraceRoundTripAsync`                                                           | Fact   | Enable trace round trip.                                                                    |
| `LogsWarnThresholdSwitchAndThresholdRoundTripAsync`                                   | Fact   | Logs warn threshold switch and threshold round trip.                                        |
| `LogsInfoAndWarnThresholdAreIndependentNotMutuallyExclusiveAsync`                     | Fact   | Logs info and warn threshold are independent not mutually exclusive.                        |
| `LogsWarnThresholdOutOfRangeOnlyRejectedWhenEnabledAsync`                             | Fact   | Logs warn threshold out of range only rejected when enabled.                                |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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
| `FieldsWithNoBackingPocoColumnAreAlwaysDefaultOnReadAsync`                            | Fact   | Fields with no backing poco column are always default on read.                              |
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

## Entity analysis model abstraction calculation

### EntityAnalysisModelAbstractionCalculationServiceTests

`Jube.Test.Service.EntityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionCalculationServiceTests` — 45
tests. Category: Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `FunctionScriptRequiredRejectsEmptyAsync`                                             | Fact   | Function script required rejects empty.                                                     |
| `FunctionScriptOverMaximumLengthIsRejectedAsync`                                      | Fact   | Function script over maximum length is rejected.                                            |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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

## Entity analysis model abstraction rule

### EntityAnalysisModelAbstractionRuleServiceTests

`Jube.Test.Service.EntityAnalysisModelAbstractionRule.EntityAnalysisModelAbstractionRuleServiceTests` — 59 tests.
Category: Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `BuilderRuleScriptRequiredRejectsEmptyAsync`                                          | Fact   | Builder rule script required rejects empty.                                                 |
| `CoderRuleScriptRequiredRejectsEmptyWhenCoderModeSelectedAsync`                       | Fact   | Coder rule script required rejects empty when coder mode selected.                          |
| `JsonRequiredRejectsEmptyWhenBuilderModeSelectedAsync`                                | Fact   | Json required rejects empty when builder mode selected.                                     |
| `BuilderModeDoesNotRequireCoderRuleScriptAsync`                                       | Fact   | Builder mode does not require coder rule script.                                            |
| `CoderModeDoesNotRequireBuilderRuleScriptOrJsonAsync`                                 | Fact   | Coder mode does not require builder rule script or json.                                    |
| `InvalidRuleScriptTypeIdIsRejectedAsync`                                              | Fact   | Invalid rule script type id is rejected.                                                    |
| `SearchKeyRequiredWhenSearchEnabledAsync`                                             | Fact   | Search key required when search enabled.                                                    |
| `SearchKeyNotRequiredWhenSearchDisabledAsync`                                         | Fact   | Search key not required when search disabled.                                               |
| `SearchValueNegativeIsRejectedWhenSearchEnabledAsync`                                 | Fact   | Search value negative is rejected when search enabled.                                      |
| `InvalidSearchIntervalIsRejectedWhenSearchEnabledAsync`                               | Theory | Invalid search interval is rejected when search enabled.                                    |
| `InvalidSearchFunctionTypeIdIsRejectedWhenSearchEnabledAsync`                         | Fact   | Invalid search function type id is rejected when search enabled.                            |
| `SearchFunctionKeyRequiredWhenFunctionTypeIsNotCountAsync`                            | Fact   | Search function key required when function type is not count.                               |
| `SearchFunctionKeyNotRequiredWhenFunctionTypeIsCountAsync`                            | Fact   | Search function key not required when function type is count.                               |
| `InvalidOffsetTypeIdIsRejectedWhenOffsetEnabledAsync`                                 | Fact   | Invalid offset type id is rejected when offset enabled.                                     |
| `OffsetValueNegativeIsRejectedWhenOffsetEnabledAsync`                                 | Fact   | Offset value negative is rejected when offset enabled.                                      |
| `OffsetNotValidatedWhenOffsetDisabledAsync`                                           | Fact   | Offset not validated when offset disabled.                                                  |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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

### EntityAnalysisModelAbstractionRuleValidateTests

`Jube.Test.Service.EntityAnalysisModelAbstractionRule.EntityAnalysisModelAbstractionRuleValidateTests` — 12 tests.
Category: Service. Collection: Database.

| Test                                                                       | Kind | Verifies                                                                        |
|----------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `AValidRuleIsReportedValidAndNothingIsStoredAsync`                         | Fact | A valid rule is reported valid and nothing is stored.                           |
| `EveryFailureIsReturnedWithItsPropertyAndCodeAsync`                        | Fact | Every failure is returned with its property and code.                           |
| `AnIdOfZeroValidatesAsACreateSoAnExistingNameIsADuplicateAsync`            | Fact | An id of zero validates as a create so an existing name is a duplicate.         |
| `AUserWithoutPermissionIsRefusedAsync`                                     | Fact | A user without permission is refused.                                           |
| `ANullModelIsRejectedAsync`                                                | Fact | A null model is rejected.                                                       |
| `ParseRuleAcceptsARuleOverTheModelsFieldsAsync`                            | Fact | Parse rule accepts a rule over the models fields.                               |
| `ParseRuleLocatesAnUnknownFieldInTheBuilderTextAsync`                      | Fact | Parse rule locates an unknown field in the builder text.                        |
| `ParseRuleIgnoresFailuresOutsideTheRuleTextAsync`                          | Fact | Parse rule ignores failures outside the rule text.                              |
| `ValidateReportsRuleTextFailuresAlongsideTheOthersAsync`                   | Fact | Validate reports rule text failures alongside the others.                       |
| `OnlyBuilderAbstractionRulesAreRestrictedToCachedFieldsLikeTheEngineAsync` | Fact | Only builder abstraction rules are restricted to cached fields like the engine. |
| `SavingARuleWhoseTextDoesNotParseIsRefusedAndNothingIsStoredAsync`         | Fact | Saving a rule whose text does not parse is refused and nothing is stored.       |
| `UpdatingARuleSoItsTextNoLongerParsesIsRefusedAsync`                       | Fact | Updating a rule so its text no longer parses is refused.                        |

## Entity analysis model activation rule

### EntityAnalysisModelActivationRuleServiceTests

`Jube.Test.Service.EntityAnalysisModelActivationRule.EntityAnalysisModelActivationRuleServiceTests` — 60 tests.
Category: Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
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
| `InvalidRuleScriptTypeIdIsRejectedAsync`                                              | Fact   | Invalid rule script type id is rejected.                                                    |
| `BuilderRuleScriptRequiredRejectsEmptyAsync`                                          | Fact   | Builder rule script required rejects empty.                                                 |
| `JsonRequiredRejectsEmptyWhenBuilderModeSelectedAsync`                                | Fact   | Json required rejects empty when builder mode selected.                                     |
| `CoderRuleScriptRequiredRejectsEmptyWhenCoderModeSelectedAsync`                       | Fact   | Coder rule script required rejects empty when coder mode selected.                          |
| `BuilderModeDoesNotRequireCoderRuleScriptAsync`                                       | Fact   | Builder mode does not require coder rule script.                                            |
| `CoderModeDoesNotRequireBuilderRuleScriptOrJsonAsync`                                 | Fact   | Coder mode does not require builder rule script or json.                                    |
| `CaseWorkflowFieldsRequiredWhenEnableCaseWorkflowTrueAsync`                           | Fact   | Case workflow fields required when enable case workflow true.                               |
| `CaseWorkflowFieldsNotRequiredWhenEnableCaseWorkflowFalseAsync`                       | Fact   | Case workflow fields not required when enable case workflow false.                          |
| `BypassFieldsValidatedOnlyWhenCaseWorkflowAndBypassEnabledAsync`                      | Fact   | Bypass fields validated only when case workflow and bypass enabled.                         |
| `ResponseElevationFieldsRequiredWhenEnabledAndSentToActivationWatcherAsync`           | Fact   | Response elevation fields required when enabled and sent to activation watcher.             |
| `ResponseElevationColorsMustBeHexAsync`                                               | Fact   | Response elevation colors must be hex.                                                      |
| `ResponseElevationFieldsNotRequiredWhenNotSentToActivationWatcherAsync`               | Fact   | Response elevation fields not required when not sent to activation watcher.                 |
| `NotificationDestinationRequiredWhenEnableNotificationTrueAsync`                      | Fact   | Notification destination required when enable notification true.                            |
| `TtlCounterFieldsRequiredWhenEnableTtlCounterTrueAsync`                               | Fact   | Ttl counter fields required when enable ttl counter true.                                   |
| `ActivationSampleMustBeBetweenZeroAndOneAsync`                                        | Theory | Activation sample must be between zero and one.                                             |
| `PriorityCannotBeNegativeAsync`                                                       | Fact   | Priority cannot be negative.                                                                |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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
| `ResetCounterZeroesActivationAndEvaluationCountersAsync`                              | Fact   | Reset counter zeroes activation and evaluation counters.                                    |
| `ResetCounterOfMissingRowThrowsNotFoundAsync`                                         | Fact   | Reset counter of missing row throws not found.                                              |
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

### EntityAnalysisModelActivationRuleTtlCounterTests

`Jube.Test.Service.EntityAnalysisModelActivationRule.EntityAnalysisModelActivationRuleTtlCounterTests` — 1 test.
Category: Service. Collection: Database.

| Test                                      | Kind | Verifies                                  |
|-------------------------------------------|------|-------------------------------------------|
| `UpdateKeepsTtlGuidsWhenSwitchedOffAsync` | Fact | Update keeps ttl guids when switched off. |

## Entity analysis model activation rule override

### EntityAnalysisModelActivationRuleOverrideServiceTests

`Jube.Test.Service.EntityAnalysisModelActivationRuleOverride.EntityAnalysisModelActivationRuleOverrideServiceTests` — 41
tests. Category: Service. Collection: Database.

| Test                                                                             | Kind   | Verifies                                                                              |
|----------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `InsertIsRefusedWhereTheActivationRuleIsNotEnabledForOverrideAsync`              | Fact   | Insert is refused where the activation rule is not enabled for override.              |
| `InsertOfForceIsRefusedWithoutTheForceOverridePermissionAsync`                   | Fact   | Insert of force is refused without the force override permission.                     |
| `InsertIsRefusedWhereTheActivationRuleIsBoundToAnotherOverrideKeyAsync`          | Fact   | Insert is refused where the activation rule is bound to another override key.         |
| `InsertPersistsAndReturnsVersionOneAsync`                                        | Fact   | Insert persists and returns version one.                                              |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                | Fact   | Get all returns created row and get by id returns it.                                 |
| `GetByEntityAnalysisModelGuidReturnsOnlyRowsForThatModelAsync`                   | Fact   | Get by entity analysis model guid returns only rows for that model.                   |
| `UpdateTogglesOffExistingRowAndOnANewRowAsync`                                   | Fact   | Update toggles off existing row and on a new row.                                     |
| `UpdateDeleteExpiryDateUpdatesActiveRowAsync`                                    | Fact   | Update delete expiry date updates active row.                                         |
| `UpdateDeleteExpiryDateForMissingRowThrowsNotFoundAsync`                         | Fact   | Update delete expiry date for missing row throws not found.                           |
| `DeleteSoftDeletesAndRowDisappearsFromReadsAsync`                                | Fact   | Delete soft deletes and row disappears from reads.                                    |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                          | Fact   | Delete of missing row throws not found.                                               |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`             | Fact   | Every method throws forbidden when permission missing and writes no row.              |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                   | Theory | Null or blank user name throws not authenticated before any query.                    |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                           | Fact   | User with no user in tenant row throws not authenticated.                             |
| `UnknownUserThrowsNotAuthenticatedAsync`                                         | Fact   | Unknown user throws not authenticated.                                                |
| `UserInTenantBCannotGetOrDeleteTenantARowAsync`                                  | Fact   | User in tenant b cannot get or delete tenant a row.                                   |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                   | Fact   | Get all as tenant b never contains tenant a rows.                                     |
| `OverrideKeyRequiredRejectsNullEmptyOrWhitespaceAsync`                           | Theory | Override key required rejects null empty or whitespace.                               |
| `OverrideKeyOverMaximumLengthIsRejectedWithErrorCodeAsync`                       | Fact   | Override key over maximum length is rejected with error code.                         |
| `OverrideKeyValueRequiredRejectsEmptyAsync`                                      | Fact   | Override key value required rejects empty.                                            |
| `OverrideKeyValueOverMaximumLengthIsRejectedWithErrorCodeAsync`                  | Fact   | Override key value over maximum length is rejected with error code.                   |
| `EmptyEntityAnalysisModelGuidIsRejectedAsync`                                    | Fact   | Empty entity analysis model guid is rejected.                                         |
| `NonExistentEntityAnalysisModelGuidIsRejectedAsync`                              | Fact   | Non existent entity analysis model guid is rejected.                                  |
| `EntityAnalysisModelActivationRuleNameRequiredRejectsNullEmptyOrWhitespaceAsync` | Theory | Entity analysis model activation rule name required rejects null empty or whitespace. |
| `NonExistentEntityAnalysisModelActivationRuleNameIsRejectedAsync`                | Fact   | Non existent entity analysis model activation rule name is rejected.                  |
| `EntityAnalysisModelActivationRuleNameFromAnotherModelIsRejectedAsync`           | Fact   | Entity analysis model activation rule name from another model is rejected.            |
| `DeleteExpiryDateInThePastIsRejectedAsync`                                       | Fact   | Delete expiry date in the past is rejected.                                           |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                        | Fact   | Tampered identity and audit fields on insert have no effect.                          |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                | Fact   | Get by id for missing id returns null not exception.                                  |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                            | Fact   | Pre cancelled token on read throws operation canceled.                                |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                              | Fact   | Permission denied logs warn not error and gates hold.                                 |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                       | Fact   | Successful insert logs exactly one info and reads log no info.                        |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                        | Fact   | With gates disabled happy path records no debug info or warn.                         |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                   | Fact   | Failure logs error with exception attached and still propagates.                      |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                        | Fact   | Each call emits one span with outcome tag.                                            |
| `EachCallRecordsOneDurationMeasurementAsync`                                     | Fact   | Each call records one duration measurement.                                           |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                  | Fact   | Audit log gets exactly one line including reads.                                      |
| `InsertPublishesExactlyOneCreatedEventAsync`                                     | Fact   | Insert publishes exactly one created event.                                           |
| `ReadsAndFailuresPublishNothingAsync`                                            | Fact   | Reads and failures publish nothing.                                                   |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                            | Fact   | Catalogue registers unique pascal case no underscore names.                           |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                          | Fact   | List async clamps take and paginates deterministically.                               |

## Entity analysis model dictionary

### EntityAnalysisModelDictionaryServiceTests

`Jube.Test.Service.EntityAnalysisModelDictionary.EntityAnalysisModelDictionaryServiceTests` — 46 tests. Category:
Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                       |
|---------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                       |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                          |
| `GetByEntityAnalysisModelIdReturnsActiveAndInactiveRowsForThatModelOrderedByIdAsync`  | Fact   | Get by entity analysis model id returns active and inactive rows for that model ordered by id. |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                                |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                      |
| `DeletedRowStillAppearsInUnboundedGetAsyncPreExistingRepositoryQuirkAsync`            | Fact   | Deleted row still appears in unbounded get async pre existing repository quirk.                |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                       |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                             |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                      |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                         |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                     |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                              |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                                |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                          |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                           |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                                 |
| `InvalidEntityAnalysisModelGuidIsRejectedAsync`                                       | Fact   | Invalid entity analysis model guid is rejected.                                                |
| `DataNameRequiredRejectsNullEmptyOrWhitespaceAsync`                                   | Theory | Data name required rejects null empty or whitespace.                                           |
| `DataNameOverMaximumLengthIsRejectedWithErrorCodeAsync`                               | Fact   | Data name over maximum length is rejected with error code.                                     |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                   |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on update have no effect.                                   |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                | Fact   | Update of locked row throws not found.                                                         |
| `UpdateOfSoftDeletedRowRevivesItAsync`                                                | Fact   | Update of soft deleted row revives it.                                                         |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                           | Fact   | Update of never existed id throws not found.                                                   |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                               | Fact   | Delete of missing or already deleted row throws not found.                                     |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                | Fact   | Resubmit of non rejected version throws approval refused and writes no row.                    |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                | Fact   | Resubmit of rejected version bumps version archives old and keeps content.                     |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                     | Fact   | Resubmit of rejected delete keeps deleted true and reopens approval.                           |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                               | Fact   | Retract of non rejected version throws approval refused.                                       |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                             | Fact   | Retract with no approved baseline throws approval refused.                                     |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync` | Fact   | Retract of rejected edit restores last approved content and stamps the rejection retracted.    |
| `RetractOfRejectedDeleteUndeletesAsync`                                               | Fact   | Retract of rejected delete undeletes.                                                          |
| `ActiveLockedAndResponsePayloadRoundTripBoolToByteAndBackAsync`                       | Fact   | Active locked and response payload round trip bool to byte and back.                           |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                     | Fact   | Get by id for missing id returns null not exception.                                           |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                 | Fact   | Pre cancelled token on read throws operation canceled.                                         |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                   | Fact   | Permission denied logs warn not error and gates hold.                                          |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                            | Fact   | Successful insert logs exactly one info and reads log no info.                                 |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                             | Fact   | With gates disabled happy path records no debug info or warn.                                  |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                        | Fact   | Failure logs error with exception attached and still propagates.                               |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                             | Fact   | Each call emits one span with outcome tag.                                                     |
| `EachCallRecordsOneDurationMeasurementAsync`                                          | Fact   | Each call records one duration measurement.                                                    |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                       | Fact   | Audit log gets exactly one line including reads.                                               |
| `InsertPublishesExactlyOneCreatedEventAsync`                                          | Fact   | Insert publishes exactly one created event.                                                    |
| `ReadsAndFailuresPublishNothingAsync`                                                 | Fact   | Reads and failures publish nothing.                                                            |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                 | Fact   | Catalogue registers unique pascal case no underscore names.                                    |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                               | Fact   | List async clamps take and paginates deterministically.                                        |

## Entity analysis model dictionary kvp

### EntityAnalysisModelDictionaryKvpCsvUploadServiceTests

`Jube.Test.Service.EntityAnalysisModelDictionaryKvp.EntityAnalysisModelDictionaryKvpCsvUploadServiceTests` — 15 tests.
Category: Service. Collection: Database.

| Test                                                                                  | Kind | Verifies                                                                                       |
|---------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `ValidCsvInsertsRowsAndRecordsUploadAsync`                                            | Fact | Valid csv inserts rows and records upload.                                                     |
| `ExistingKeyIsUpdatedAndExpiryKeptWhenNotSpecifiedAsync`                              | Fact | Existing key is updated and expiry kept when not specified.                                    |
| `ReuploadingAnIdenticalKeyValueAndExpiryDateIsIgnoredAndOpensNoNewApprovalCycleAsync` | Fact | Reuploading an identical key value and expiry date is ignored and opens no new approval cycle. |
| `UploadCsvDoesNotInsertARowWhoseKeyIsAwaitingDeletionApprovalAsync`                   | Fact | Upload csv does not insert a row whose key is awaiting deletion approval.                      |
| `MalformedRowsAreLoggedSkippedAndNotCountedAsync`                                     | Fact | Malformed rows are logged skipped and not counted.                                             |
| `EmptyFileWritesUploadRecordWithZeroRecordsAsync`                                     | Fact | Empty file writes upload record with zero records.                                             |
| `NoFilesIsANoOpAsync`                                                                 | Fact | No files is a no op.                                                                           |
| `MultipleFilesEachGetAnUploadRecordAndOneEventAsync`                                  | Fact | Multiple files each get an upload record and one event.                                        |
| `ParentInOtherTenantBehavesAsNotFoundAndWritesNothingAsync`                           | Fact | Parent in other tenant behaves as not found and writes nothing.                                |
| `UnknownDictionaryThrowsNotFoundAsync`                                                | Fact | Unknown dictionary throws not found.                                                           |
| `WithoutPermissionThrowsForbiddenAndWritesNothingAsync`                               | Fact | Without permission throws forbidden and writes nothing.                                        |
| `NullFilesThrowsArgumentNullAsync`                                                    | Fact | Null files throws argument null.                                                               |
| `CancelledTokenThrowsAndPublishesNothingAsync`                                        | Fact | Cancelled token throws and publishes nothing.                                                  |
| `WritesExactlyOneAuditRecordAsync`                                                    | Fact | Writes exactly one audit record.                                                               |
| `CatalogueListsUploadToolOnce`                                                        | Fact | Catalogue lists upload tool once.                                                              |

### EntityAnalysisModelDictionaryKvpServiceTests

`Jube.Test.Service.EntityAnalysisModelDictionaryKvp.EntityAnalysisModelDictionaryKvpServiceTests` — 54 tests. Category:
Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                     |
|---------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                     |
| `GetAllReturnsCreatedRowAsync`                                                        | Fact   | Get all returns created row.                                                                 |
| `GetByEntityAnalysisModelDictionaryIdReturnsRowsForThatDictionaryOrderedByIdAsync`    | Fact   | Get by entity analysis model dictionary id returns rows for that dictionary ordered by id.   |
| `GetByEntityAnalysisModelDictionaryIdWithZeroOrNonExistentIdReturnsEmptyListAsync`    | Theory | Get by entity analysis model dictionary id with zero or non existent id returns empty list.  |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                              |
| `DeleteSoftDeletesAndRowDisappearsFromByDictionaryIdAsync`                            | Fact   | Delete soft deletes and row disappears from by dictionary id.                                |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                     |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                           |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                    |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                       |
| `UserInTenantBCannotUpdateOrDeleteTenantARowAsync`                                    | Fact   | User in tenant b cannot update or delete tenant a row.                                       |
| `GetByEntityAnalysisModelDictionaryIdAsTenantBNeverContainsTenantARowsAsync`          | Fact   | Get by entity analysis model dictionary id as tenant b never contains tenant a rows.         |
| `KvpKeyRequiredRejectsNullEmptyOrWhitespaceAsync`                                     | Theory | Kvp key required rejects null empty or whitespace.                                           |
| `KvpKeyOverMaximumLengthIsRejectedWithErrorCodeAsync`                                 | Fact   | Kvp key over maximum length is rejected with error code.                                     |
| `KvpValueNullIsRejectedAsync`                                                         | Fact   | Kvp value null is rejected.                                                                  |
| `KvpValueOfZeroIsAcceptedAsync`                                                       | Fact   | Kvp value of zero is accepted.                                                               |
| `InvalidEntityAnalysisModelDictionaryIdIsRejectedAsync`                               | Fact   | Invalid entity analysis model dictionary id is rejected.                                     |
| `NonExistentEntityAnalysisModelDictionaryIdIsRejectedAsync`                           | Fact   | Non existent entity analysis model dictionary id is rejected.                                |
| `DeleteExpiryDateInThePastIsRejectedAsync`                                            | Fact   | Delete expiry date in the past is rejected.                                                  |
| `DeleteExpiryDateInTheFutureIsAcceptedAndRoundTripsAsync`                             | Fact   | Delete expiry date in the future is accepted and round trips.                                |
| `CreateDuplicateKvpKeyInSameDictionaryIsRejectedWithErrorCodeAsync`                   | Fact   | Create duplicate kvp key in same dictionary is rejected with error code.                     |
| `CreateSameKeyInDifferentDictionariesIsAllowedAsync`                                  | Fact   | Create same key in different dictionaries is allowed.                                        |
| `UpdateToAnotherRowsExistingKeyIsRejectedAsync`                                       | Fact   | Update to another rows existing key is rejected.                                             |
| `UpdateKeepingItsOwnUnchangedKeyIsAllowedAsync`                                       | Fact   | Update keeping its own unchanged key is allowed.                                             |
| `ReAddingTheSameKeyAfterItsDeletionIsApprovedIsAllowedAsync`                          | Fact   | Re adding the same key after its deletion is approved is allowed.                            |
| `CreateSameKeyWhileItsDeletionIsAwaitingApprovalIsRejectedAsync`                      | Fact   | Create same key while its deletion is awaiting approval is rejected.                         |
| `RepositoryInsertRacingTheLiveUniqueIndexTranslatesToAnOrdinaryExceptionAsync`        | Fact   | Repository insert racing the live unique index translates to an ordinary exception.          |
| `ExpiredValueDisappearsFromByDictionaryIdAsync`                                       | Fact   | Expired value disappears from by dictionary id.                                              |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                 |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on update have no effect.                                 |
| `UpdateOfSoftDeletedRowRevivesItAsync`                                                | Fact   | Update of soft deleted row revives it.                                                       |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                           | Fact   | Update of never existed id throws not found.                                                 |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                               | Fact   | Delete of missing or already deleted row throws not found.                                   |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                | Fact   | Resubmit of non rejected version throws approval refused and writes no row.                  |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                | Fact   | Resubmit of rejected version bumps version archives old and keeps content.                   |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                     | Fact   | Resubmit of rejected delete keeps deleted true and reopens approval.                         |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                               | Fact   | Retract of non rejected version throws approval refused.                                     |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                             | Fact   | Retract with no approved baseline throws approval refused.                                   |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync` | Fact   | Retract of rejected edit restores last approved content and stamps the rejection retracted.  |
| `RetractOfRejectedDeleteUndeletesAsync`                                               | Fact   | Retract of rejected delete undeletes.                                                        |
| `AKeyValuePairDeletedThroughTheServiceIsStillReturnedByIdWithItsDeletedStateAsync`    | Fact   | A key value pair deleted through the service is still returned by id with its deleted state. |
| `GetByIdReturnsTheKeyValuePairWithThatIdAsync`                                        | Fact   | Get by id returns the key value pair with that id.                                           |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                 | Fact   | Pre cancelled token on read throws operation canceled.                                       |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                   | Fact   | Permission denied logs warn not error and gates hold.                                        |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                            | Fact   | Successful insert logs exactly one info and reads log no info.                               |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                             | Fact   | With gates disabled happy path records no debug info or warn.                                |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                        | Fact   | Failure logs error with exception attached and still propagates.                             |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                             | Fact   | Each call emits one span with outcome tag.                                                   |
| `EachCallRecordsOneDurationMeasurementAsync`                                          | Fact   | Each call records one duration measurement.                                                  |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                       | Fact   | Audit log gets exactly one line including reads.                                             |
| `InsertPublishesExactlyOneCreatedEventAsync`                                          | Fact   | Insert publishes exactly one created event.                                                  |
| `ReadsAndFailuresPublishNothingAsync`                                                 | Fact   | Reads and failures publish nothing.                                                          |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                 | Fact   | Catalogue registers unique pascal case no underscore names.                                  |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                               | Fact   | List async clamps take and paginates deterministically.                                      |

## Entity analysis model gateway rule

### EntityAnalysisModelGatewayRuleServiceTests

`Jube.Test.Service.EntityAnalysisModelGatewayRule.EntityAnalysisModelGatewayRuleServiceTests` — 59 tests. Category:
Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `BuilderRuleScriptRequiredRejectsEmptyAsync`                                          | Fact   | Builder rule script required rejects empty.                                                 |
| `CoderRuleScriptRequiredRejectsEmptyWhenCoderModeSelectedAsync`                       | Fact   | Coder rule script required rejects empty when coder mode selected.                          |
| `JsonRequiredRejectsEmptyWhenBuilderModeSelectedAsync`                                | Fact   | Json required rejects empty when builder mode selected.                                     |
| `BuilderModeDoesNotRequireCoderRuleScriptAsync`                                       | Fact   | Builder mode does not require coder rule script.                                            |
| `CoderModeDoesNotRequireBuilderRuleScriptOrJsonAsync`                                 | Fact   | Coder mode does not require builder rule script or json.                                    |
| `InvalidRuleScriptTypeIdIsRejectedAsync`                                              | Fact   | Invalid rule script type id is rejected.                                                    |
| `GatewaySampleOutOfRangeIsRejectedAsync`                                              | Theory | Gateway sample out of range is rejected.                                                    |
| `MaxResponseElevationNegativeIsRejectedAsync`                                         | Fact   | Max response elevation negative is rejected.                                                |
| `PriorityNegativeIsRejectedAsync`                                                     | Fact   | Priority negative is rejected.                                                              |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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
| `RetractOfRejectedReviveAfterApprovedDeleteRestoresTheDeletedBaselineAsync`           | Fact   | Retract of rejected revive after approved delete restores the deleted baseline.             |
| `ResubmitWithANoteStampsResubmittedNoteWithoutClobberingTheRejectionNoteAsync`        | Fact   | Resubmit with a note stamps resubmitted note without clobbering the rejection note.         |
| `RetractWithANoteStampsRetractedNoteWithoutClobberingTheRejectionNoteAsync`           | Fact   | Retract with a note stamps retracted note without clobbering the rejection note.            |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                                      | Fact   | Active and locked round trip bool to byte and back.                                         |
| `ResponsePayloadIsNeverPersistedAsync`                                                | Fact   | Response payload is never persisted.                                                        |
| `UpdateOverwritesCounterFieldsWithSuppliedValuesAsync`                                | Fact   | Update overwrites counter fields with supplied values.                                      |
| `ResetCounterZeroesActivationAndEvaluationCountersAsync`                              | Fact   | Reset counter zeroes activation and evaluation counters.                                    |
| `ResetCounterOfMissingRowThrowsNotFoundAsync`                                         | Fact   | Reset counter of missing row throws not found.                                              |
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

## Entity analysis model http adaptation

### EntityAnalysisModelHttpAdaptationServiceTests

`Jube.Test.Service.EntityAnalysisModelHttpAdaptation.EntityAnalysisModelHttpAdaptationServiceTests` — 47 tests.
Category: Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `HttpEndpointRequiredRejectsNullEmptyOrWhitespaceAsync`                               | Theory | Http endpoint required rejects null empty or whitespace.                                    |
| `HttpEndpointOverMaximumLengthIsRejectedWithErrorCodeAsync`                           | Fact   | Http endpoint over maximum length is rejected with error code.                              |
| `HttpEndpointWithoutLeadingSlashIsRejectedWithErrorCodeAsync`                         | Fact   | Http endpoint without leading slash is rejected with error code.                            |
| `NegativePriorityIsRejectedWithErrorCodeAsync`                                        | Fact   | Negative priority is rejected with error code.                                              |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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

## Entity analysis model inline function

### EntityAnalysisModelInlineFunctionServiceTests

`Jube.Test.Service.EntityAnalysisModelInlineFunction.EntityAnalysisModelInlineFunctionServiceTests` — 47 tests.
Category: Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `InvalidReturnDataTypeIdIsRejectedAsync`                                              | Fact   | Invalid return data type id is rejected.                                                    |
| `InvalidEncryptionIdIsRejectedAsync`                                                  | Fact   | Invalid encryption id is rejected.                                                          |
| `FunctionScriptRequiredRejectsNullEmptyOrWhitespaceAsync`                             | Theory | Function script required rejects null empty or whitespace.                                  |
| `FunctionScriptOverMaximumLengthIsRejectedWithErrorCodeAsync`                         | Fact   | Function script over maximum length is rejected with error code.                            |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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

## Entity analysis model inline script

### EntityAnalysisModelInlineScriptServiceTests

`Jube.Test.Service.EntityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptServiceTests` — 45 tests. Category:
Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `ZeroEntityAnalysisInlineScriptIdIsRejectedAsync`                                     | Fact   | Zero entity analysis inline script id is rejected.                                          |
| `NonExistentEntityAnalysisInlineScriptIdIsRejectedAsync`                              | Fact   | Non existent entity analysis inline script id is rejected.                                  |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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

## Entity analysis model list

### EntityAnalysisModelListServiceTests

`Jube.Test.Service.EntityAnalysisModelList.EntityAnalysisModelListServiceTests` — 45 tests. Category: Service.
Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                       |
|---------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                       |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                          |
| `GetByEntityAnalysisModelIdReturnsActiveAndInactiveRowsForThatModelOrderedByIdAsync`  | Fact   | Get by entity analysis model id returns active and inactive rows for that model ordered by id. |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                                |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                      |
| `DeletedRowStillAppearsInUnboundedGetAsyncPreExistingRepositoryQuirkAsync`            | Fact   | Deleted row still appears in unbounded get async pre existing repository quirk.                |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                       |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                             |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                      |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                         |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                     |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                              |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                                |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                          |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                           |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                                 |
| `InvalidEntityAnalysisModelGuidIsRejectedAsync`                                       | Fact   | Invalid entity analysis model guid is rejected.                                                |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                   |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on update have no effect.                                   |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                | Fact   | Update of locked row throws not found.                                                         |
| `UpdateOfSoftDeletedRowRevivesItAsync`                                                | Fact   | Update of soft deleted row revives it.                                                         |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                           | Fact   | Update of never existed id throws not found.                                                   |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                               | Fact   | Delete of missing or already deleted row throws not found.                                     |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                | Fact   | Resubmit of non rejected version throws approval refused and writes no row.                    |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                | Fact   | Resubmit of rejected version bumps version archives old and keeps content.                     |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                     | Fact   | Resubmit of rejected delete keeps deleted true and reopens approval.                           |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                               | Fact   | Retract of non rejected version throws approval refused.                                       |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                             | Fact   | Retract with no approved baseline throws approval refused.                                     |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync` | Fact   | Retract of rejected edit restores last approved content and stamps the rejection retracted.    |
| `RetractOfRejectedDeleteUndeletesAsync`                                               | Fact   | Retract of rejected delete undeletes.                                                          |
| `RetractOfRejectedReviveAfterApprovedDeleteRestoresTheDeletedBaselineAsync`           | Fact   | Retract of rejected revive after approved delete restores the deleted baseline.                |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                                      | Fact   | Active and locked round trip bool to byte and back.                                            |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                     | Fact   | Get by id for missing id returns null not exception.                                           |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                 | Fact   | Pre cancelled token on read throws operation canceled.                                         |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                   | Fact   | Permission denied logs warn not error and gates hold.                                          |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                            | Fact   | Successful insert logs exactly one info and reads log no info.                                 |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                             | Fact   | With gates disabled happy path records no debug info or warn.                                  |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                        | Fact   | Failure logs error with exception attached and still propagates.                               |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                             | Fact   | Each call emits one span with outcome tag.                                                     |
| `EachCallRecordsOneDurationMeasurementAsync`                                          | Fact   | Each call records one duration measurement.                                                    |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                       | Fact   | Audit log gets exactly one line including reads.                                               |
| `InsertPublishesExactlyOneCreatedEventAsync`                                          | Fact   | Insert publishes exactly one created event.                                                    |
| `ReadsAndFailuresPublishNothingAsync`                                                 | Fact   | Reads and failures publish nothing.                                                            |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                 | Fact   | Catalogue registers unique pascal case no underscore names.                                    |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                               | Fact   | List async clamps take and paginates deterministically.                                        |

## Entity analysis model list value

### EntityAnalysisModelListValueServiceTests

`Jube.Test.Service.EntityAnalysisModelListValue.EntityAnalysisModelListValueServiceTests` — 70 tests. Category: Service.
Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAsync`                                                        | Fact   | Get all returns created row.                                                                |
| `GetByEntityAnalysisModelListIdReturnsRowsForThatListOrderedByIdAsync`                | Fact   | Get by entity analysis model list id returns rows for that list ordered by id.              |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteSoftDeletesAndRowDisappearsFromByListIdAsync`                                  | Fact   | Delete soft deletes and row disappears from by list id.                                     |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotUpdateOrDeleteTenantARowAsync`                                    | Fact   | User in tenant b cannot update or delete tenant a row.                                      |
| `GetByEntityAnalysisModelListIdAsTenantBNeverContainsTenantARowsAsync`                | Fact   | Get by entity analysis model list id as tenant b never contains tenant a rows.              |
| `ListValueRequiredRejectsNullEmptyOrWhitespaceAsync`                                  | Theory | List value required rejects null empty or whitespace.                                       |
| `ListValueOverMaximumLengthIsRejectedWithErrorCodeAsync`                              | Fact   | List value over maximum length is rejected with error code.                                 |
| `InvalidEntityAnalysisModelListIdIsRejectedAsync`                                     | Fact   | Invalid entity analysis model list id is rejected.                                          |
| `NonExistentEntityAnalysisModelListIdIsRejectedAsync`                                 | Fact   | Non existent entity analysis model list id is rejected.                                     |
| `DeleteExpiryDateInThePastIsRejectedAsync`                                            | Fact   | Delete expiry date in the past is rejected.                                                 |
| `DeleteExpiryDateInTheFutureIsAcceptedAndRoundTripsAsync`                             | Fact   | Delete expiry date in the future is accepted and round trips.                               |
| `CreateDuplicateListValueInSameListIsRejectedWithErrorCodeAsync`                      | Fact   | Create duplicate list value in same list is rejected with error code.                       |
| `CreateSameValueTextInDifferentListsIsAllowedAsync`                                   | Fact   | Create same value text in different lists is allowed.                                       |
| `CreateSameValueDifferentCaseIsAllowedAsync`                                          | Fact   | Create same value different case is allowed.                                                |
| `UpdateToAnotherRowsExistingValueIsRejectedAsync`                                     | Fact   | Update to another rows existing value is rejected.                                          |
| `UpdateKeepingItsOwnUnchangedValueIsAllowedAsync`                                     | Fact   | Update keeping its own unchanged value is allowed.                                          |
| `ReAddingTheSameValueAfterItsDeletionIsApprovedIsAllowedAsync`                        | Fact   | Re adding the same value after its deletion is approved is allowed.                         |
| `CreateSameValueWhileItsDeletionIsAwaitingApprovalIsRejectedAsync`                    | Fact   | Create same value while its deletion is awaiting approval is rejected.                      |
| `CreateSameValueAfterItsDeletionWasRejectedAndNotRetractedIsRejectedAsync`            | Fact   | Create same value after its deletion was rejected and not retracted is rejected.            |
| `RepositoryInsertRacingTheLiveUniqueIndexTranslatesToAnOrdinaryExceptionAsync`        | Fact   | Repository insert racing the live unique index translates to an ordinary exception.         |
| `ExpiredValueDisappearsFromByListIdAsync`                                             | Fact   | Expired value disappears from by list id.                                                   |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on update have no effect.                                |
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
| `RetractOfRejectedReviveAfterApprovedDeleteRestoresTheDeletedBaselineAsync`           | Fact   | Retract of rejected revive after approved delete restores the deleted baseline.             |
| `GetByIdMatchesOnItsOwnIdNotItsParentListIdAsync`                                     | Fact   | Get by id matches on its own id not its parent list id.                                     |
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
| `UploadCsvLoadsValidRowsAndRecordsUploadAsync`                                        | Fact   | Upload csv loads valid rows and records upload.                                             |
| `UploadCsvParsesRoundTripExpiryDateAsUtcAsync`                                        | Fact   | Upload csv parses round trip expiry date as utc.                                            |
| `UploadCsvIgnoresRowsThatFullyMatchAnExistingLiveValueAsync`                          | Fact   | Upload csv ignores rows that fully match an existing live value.                            |
| `UploadCsvRejectsRowWhoseValueIsAwaitingDeletionApprovalAsAnErrorAsync`               | Fact   | Upload csv rejects row whose value is awaiting deletion approval as an error.               |
| `UploadCsvRejectsSameValueWithADifferentExpiryDateAsAnErrorAsync`                     | Fact   | Upload csv rejects same value with a different expiry date as an error.                     |
| `UploadCsvIgnoresRowThatMatchesExistingValueAndExpiryDateAsync`                       | Fact   | Upload csv ignores row that matches existing value and expiry date.                         |
| `UploadCsvCountsBlankOverlongAndDatabaseRejectedRowsAsErrorsAndKeepsGoodRowsAsync`    | Fact   | Upload csv counts blank overlong and database rejected rows as errors and keeps good rows.  |
| `UploadCsvEmptyFileRecordsZeroRowsAndStillWritesUploadAsync`                          | Fact   | Upload csv empty file records zero rows and still writes upload.                            |
| `UploadCsvNoFilesWritesNothingAndPublishesNothingAsync`                               | Fact   | Upload csv no files writes nothing and publishes nothing.                                   |
| `UploadCsvMultipleFilesLoadsAllPublishesOnceAndAuditsOnceAsync`                       | Fact   | Upload csv multiple files loads all publishes once and audits once.                         |
| `UploadCsvForeignTenantParentBehavesLikeNotFoundAndWritesNothingAsync`                | Fact   | Upload csv foreign tenant parent behaves like not found and writes nothing.                 |
| `UploadCsvMissingListIsNotFoundAsync`                                                 | Fact   | Upload csv missing list is not found.                                                       |
| `UploadCsvWithoutPermissionIsForbiddenWritesNothingAndWarnsNotErrorsAsync`            | Fact   | Upload csv without permission is forbidden writes nothing and warns not errors.             |
| `UploadCsvBlankUserIsNotAuthenticatedAsync`                                           | Theory | Upload csv blank user is not authenticated.                                                 |
| `UploadCsvCancelledMidUploadRollsEverythingBackAsync`                                 | Fact   | Upload csv cancelled mid upload rolls everything back.                                      |
| `UploadCsvPreCancelledTokenThrowsAndWritesNothingAsync`                               | Fact   | Upload csv pre cancelled token throws and writes nothing.                                   |
| `UploadCsvUnexpectedFailureRollsBackLogsErrorAndPublishesNothingAsync`                | Fact   | Upload csv unexpected failure rolls back logs error and publishes nothing.                  |

## Entity analysis model override

### EntityAnalysisModelOverrideServiceTests

`Jube.Test.Service.EntityAnalysisModelOverride.EntityAnalysisModelOverrideServiceTests` — 35 tests. Category: Service.
Collection: Database.

| Test                                                                 | Kind   | Verifies                                                                 |
|----------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                            | Fact   | Insert persists and returns version one.                                 |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                    | Fact   | Get all returns created row and get by id returns it.                    |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`         | Fact   | Get by entity analysis model id returns only rows for that model.        |
| `UpdateTogglesOffExistingRowAndOnANewRowAsync`                       | Fact   | Update toggles off existing row and on a new row.                        |
| `UpdateDeleteExpiryDateUpdatesActiveRowAsync`                        | Fact   | Update delete expiry date updates active row.                            |
| `UpdateDeleteExpiryDateForMissingRowThrowsNotFoundAsync`             | Fact   | Update delete expiry date for missing row throws not found.              |
| `DeleteSoftDeletesAndRowDisappearsFromReadsAsync`                    | Fact   | Delete soft deletes and row disappears from reads.                       |
| `DeleteOfMissingRowThrowsNotFoundAsync`                              | Fact   | Delete of missing row throws not found.                                  |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync` | Fact   | Every method throws forbidden when permission missing and writes no row. |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`       | Theory | Null or blank user name throws not authenticated before any query.       |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`               | Fact   | User with no user in tenant row throws not authenticated.                |
| `UnknownUserThrowsNotAuthenticatedAsync`                             | Fact   | Unknown user throws not authenticated.                                   |
| `UserInTenantBCannotGetOrDeleteTenantARowAsync`                      | Fact   | User in tenant b cannot get or delete tenant a row.                      |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                       | Fact   | Get all as tenant b never contains tenant a rows.                        |
| `OverrideKeyRequiredRejectsNullEmptyOrWhitespaceAsync`               | Theory | Override key required rejects null empty or whitespace.                  |
| `OverrideKeyOverMaximumLengthIsRejectedWithErrorCodeAsync`           | Fact   | Override key over maximum length is rejected with error code.            |
| `OverrideKeyValueRequiredRejectsEmptyAsync`                          | Fact   | Override key value required rejects empty.                               |
| `OverrideKeyValueOverMaximumLengthIsRejectedWithErrorCodeAsync`      | Fact   | Override key value over maximum length is rejected with error code.      |
| `EmptyEntityAnalysisModelGuidIsRejectedAsync`                        | Fact   | Empty entity analysis model guid is rejected.                            |
| `NonExistentEntityAnalysisModelGuidIsRejectedAsync`                  | Fact   | Non existent entity analysis model guid is rejected.                     |
| `DeleteExpiryDateInThePastIsRejectedAsync`                           | Fact   | Delete expiry date in the past is rejected.                              |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`            | Fact   | Tampered identity and audit fields on insert have no effect.             |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                    | Fact   | Get by id for missing id returns null not exception.                     |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                | Fact   | Pre cancelled token on read throws operation canceled.                   |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                  | Fact   | Permission denied logs warn not error and gates hold.                    |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`           | Fact   | Successful insert logs exactly one info and reads log no info.           |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`            | Fact   | With gates disabled happy path records no debug info or warn.            |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`       | Fact   | Failure logs error with exception attached and still propagates.         |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                            | Fact   | Each call emits one span with outcome tag.                               |
| `EachCallRecordsOneDurationMeasurementAsync`                         | Fact   | Each call records one duration measurement.                              |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                      | Fact   | Audit log gets exactly one line including reads.                         |
| `InsertPublishesExactlyOneCreatedEventAsync`                         | Fact   | Insert publishes exactly one created event.                              |
| `ReadsAndFailuresPublishNothingAsync`                                | Fact   | Reads and failures publish nothing.                                      |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                | Fact   | Catalogue registers unique pascal case no underscore names.              |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`              | Fact   | List async clamps take and paginates deterministically.                  |

## Entity analysis model reprocessing rule

### EntityAnalysisModelReprocessingRuleServiceTests

`Jube.Test.Service.EntityAnalysisModelReprocessingRule.EntityAnalysisModelReprocessingRuleServiceTests` — 47 tests.
Category: Service. Collection: Database.

| Test                                                                 | Kind   | Verifies                                                                 |
|----------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                            | Fact   | Insert persists and returns version one.                                 |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                    | Fact   | Get all returns created row and get by id returns it.                    |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`         | Fact   | Get by entity analysis model id returns only rows for that model.        |
| `UpdateSupersedesRowWithNewIdAndIncrementsVersionAsync`              | Fact   | Update supersedes row with new id and increments version.                |
| `DeleteSoftDeletesAndRowDisappearsFromReadsAsync`                    | Fact   | Delete soft deletes and row disappears from reads.                       |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync` | Fact   | Every method throws forbidden when permission missing and writes no row. |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`       | Theory | Null or blank user name throws not authenticated before any query.       |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`               | Fact   | User with no user in tenant row throws not authenticated.                |
| `UnknownUserThrowsNotAuthenticatedAsync`                             | Fact   | Unknown user throws not authenticated.                                   |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                | Fact   | User in tenant b cannot get update or delete tenant a row.               |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                       | Fact   | Get all as tenant b never contains tenant a rows.                        |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                      | Theory | Name required rejects null empty or whitespace.                          |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                  | Fact   | Name over maximum length is rejected with error code.                    |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`    | Fact   | Duplicate name within same model fails but different model succeeds.     |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`          | Fact   | Duplicate name differing only by case within same model fails.           |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                        | Fact   | Invalid entity analysis model id is rejected.                            |
| `BuilderRuleScriptRequiredRejectsEmptyAsync`                         | Fact   | Builder rule script required rejects empty.                              |
| `CoderRuleScriptRequiredRejectsEmptyWhenCoderModeSelectedAsync`      | Fact   | Coder rule script required rejects empty when coder mode selected.       |
| `JsonRequiredRejectsEmptyWhenBuilderModeSelectedAsync`               | Fact   | Json required rejects empty when builder mode selected.                  |
| `BuilderModeDoesNotRequireCoderRuleScriptAsync`                      | Fact   | Builder mode does not require coder rule script.                         |
| `CoderModeDoesNotRequireBuilderRuleScriptOrJsonAsync`                | Fact   | Coder mode does not require builder rule script or json.                 |
| `InvalidRuleScriptTypeIdIsRejectedAsync`                             | Fact   | Invalid rule script type id is rejected.                                 |
| `NegativePriorityIsRejectedAsync`                                    | Fact   | Negative priority is rejected.                                           |
| `ReprocessingSampleOutOfRangeIsRejectedAsync`                        | Theory | Reprocessing sample out of range is rejected.                            |
| `NegativeReprocessingValueIsRejectedAsync`                           | Fact   | Negative reprocessing value is rejected.                                 |
| `InvalidReprocessingIntervalIsRejectedAsync`                         | Theory | Invalid reprocessing interval is rejected.                               |
| `EachAllowedReprocessingIntervalIsAcceptedAsync`                     | Theory | Each allowed reprocessing interval is accepted.                          |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`            | Fact   | Tampered identity and audit fields on insert have no effect.             |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`            | Fact   | Tampered identity and audit fields on update have no effect.             |
| `UpdateOfLockedRowThrowsNotFoundAsync`                               | Fact   | Update of locked row throws not found.                                   |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                          | Fact   | Update of soft deleted row throws not found.                             |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                          | Fact   | Update of never existed id throws not found.                             |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`              | Fact   | Delete of missing or already deleted row throws not found.               |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                     | Fact   | Active and locked round trip bool to byte and back.                      |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                    | Fact   | Get by id for missing id returns null not exception.                     |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                | Fact   | Pre cancelled token on read throws operation canceled.                   |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                  | Fact   | Permission denied logs warn not error and gates hold.                    |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`           | Fact   | Successful insert logs exactly one info and reads log no info.           |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`            | Fact   | With gates disabled happy path records no debug info or warn.            |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`       | Fact   | Failure logs error with exception attached and still propagates.         |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                            | Fact   | Each call emits one span with outcome tag.                               |
| `EachCallRecordsOneDurationMeasurementAsync`                         | Fact   | Each call records one duration measurement.                              |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                      | Fact   | Audit log gets exactly one line including reads.                         |
| `InsertPublishesExactlyOneCreatedEventAsync`                         | Fact   | Insert publishes exactly one created event.                              |
| `ReadsAndFailuresPublishNothingAsync`                                | Fact   | Reads and failures publish nothing.                                      |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                | Fact   | Catalogue registers unique pascal case no underscore names.              |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`              | Fact   | List async clamps take and paginates deterministically.                  |

## Entity analysis model reprocessing rule instance

### EntityAnalysisModelReprocessingRuleInstanceServiceTests

`Jube.Test.Service.EntityAnalysisModelReprocessingRuleInstance.EntityAnalysisModelReprocessingRuleInstanceServiceTests` — 36 tests. Category: Service. Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                |
|-------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                           | Fact   | Insert persists and returns version one.                                                |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                   | Fact   | Get all returns created row and get by id returns it.                                   |
| `GetByEntityAnalysisModelReprocessingIdReturnsOnlyRowsForThatRuleAsync`             | Fact   | Get by entity analysis model reprocessing id returns only rows for that rule.           |
| `UpdateSupersedesRowWithNewIdAndIncrementsVersionAsync`                             | Fact   | Update supersedes row with new id and increments version.                               |
| `DeleteSoftDeletesAndRowDisappearsFromReadsAsync`                                   | Fact   | Delete soft deletes and row disappears from reads.                                      |
| `InsertByExistingUpdateUncompletedCreatesInstanceAtStatusZeroAsync`                 | Fact   | Insert by existing update uncompleted creates instance at status zero.                  |
| `InsertByExistingUpdateUncompletedThrowsConflictWhenUncompletedInstanceExistsAsync` | Fact   | Insert by existing update uncompleted throws conflict when uncompleted instance exists. |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                | Fact   | Every method throws forbidden when permission missing and writes no row.                |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                      | Theory | Null or blank user name throws not authenticated before any query.                      |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                              | Fact   | User with no user in tenant row throws not authenticated.                               |
| `UnknownUserThrowsNotAuthenticatedAsync`                                            | Fact   | Unknown user throws not authenticated.                                                  |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                               | Fact   | User in tenant b cannot get update or delete tenant a row.                              |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                      | Fact   | Get all as tenant b never contains tenant a rows.                                       |
| `CrossTenantEntityAnalysisModelReprocessingRuleIdIsRejectedOnInsertAsync`           | Fact   | Cross tenant entity analysis model reprocessing rule id is rejected on insert.          |
| `CrossTenantEntityAnalysisModelReprocessingRuleIdIsRejectedOnUpdateAsync`           | Fact   | Cross tenant entity analysis model reprocessing rule id is rejected on update.          |
| `InvalidEntityAnalysisModelReprocessingRuleIdIsRejectedAsync`                       | Fact   | Invalid entity analysis model reprocessing rule id is rejected.                         |
| `InvalidStatusIdIsRejectedAsync`                                                    | Theory | Invalid status id is rejected.                                                          |
| `NegativeAvailableCountIsRejectedAsync`                                             | Fact   | Negative available count is rejected.                                                   |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                           | Fact   | Tampered identity and audit fields on insert have no effect.                            |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                           | Fact   | Tampered identity and audit fields on update have no effect.                            |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                                         | Fact   | Update of soft deleted row throws not found.                                            |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                         | Fact   | Update of never existed id throws not found.                                            |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                             | Fact   | Delete of missing or already deleted row throws not found.                              |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                   | Fact   | Get by id for missing id returns null not exception.                                    |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                               | Fact   | Pre cancelled token on read throws operation canceled.                                  |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                 | Fact   | Permission denied logs warn not error and gates hold.                                   |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                          | Fact   | Successful insert logs exactly one info and reads log no info.                          |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                           | Fact   | With gates disabled happy path records no debug info or warn.                           |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                      | Fact   | Failure logs error with exception attached and still propagates.                        |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                           | Fact   | Each call emits one span with outcome tag.                                              |
| `EachCallRecordsOneDurationMeasurementAsync`                                        | Fact   | Each call records one duration measurement.                                             |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                     | Fact   | Audit log gets exactly one line including reads.                                        |
| `InsertPublishesExactlyOneCreatedEventAsync`                                        | Fact   | Insert publishes exactly one created event.                                             |
| `ReadsAndFailuresPublishNothingAsync`                                               | Fact   | Reads and failures publish nothing.                                                     |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                               | Fact   | Catalogue registers unique pascal case no underscore names.                             |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                             | Fact   | List async clamps take and paginates deterministically.                                 |

## Entity analysis model request x path

### EntityAnalysisModelRequestXPathServiceTests

`Jube.Test.Service.EntityAnalysisModelRequestXPath.EntityAnalysisModelRequestXPathServiceTests` — 53 tests. Category:
Service. Collection: Database.

| Test                                                                                                | Kind   | Verifies                                                                                                         |
|-----------------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                                           | Fact   | Insert persists and returns version one.                                                                         |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                                   | Fact   | Get all returns created row and get by id returns it.                                                            |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                                        | Fact   | Get by entity analysis model id returns only rows for that model.                                                |
| `GetByOverrideKeyOnlyReturnsOverrideEnabledRowsAsync`                                               | Fact   | Get by override key only returns override enabled rows.                                                          |
| `GetByEntityAnalysisModelIdByDataTypeIgnoresDataTypeIdParameterAndFiltersToStringIntegerFloatAsync` | Fact   | Get by entity analysis model id by data type ignores data type id parameter and filters to string integer float. |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                                     | Fact   | Update increments version and writes audit row.                                                                  |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                               | Fact   | Delete keeps row reachable by id and hides it from lists.                                                        |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                                | Fact   | Every method throws forbidden when permission missing and writes no row.                                         |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                                      | Theory | Null or blank user name throws not authenticated before any query.                                               |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                              | Fact   | User with no user in tenant row throws not authenticated.                                                        |
| `UnknownUserThrowsNotAuthenticatedAsync`                                                            | Fact   | Unknown user throws not authenticated.                                                                           |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                               | Fact   | User in tenant b cannot get update or delete tenant a row.                                                       |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                                      | Fact   | Get all as tenant b never contains tenant a rows.                                                                |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                                     | Theory | Name required rejects null empty or whitespace.                                                                  |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                                 | Fact   | Name over maximum length is rejected with error code.                                                            |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                                   | Fact   | Duplicate name within same model fails but different model succeeds.                                             |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                                         | Fact   | Duplicate name differing only by case within same model fails.                                                   |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                                       | Fact   | Invalid entity analysis model id is rejected.                                                                    |
| `InvalidDataTypeIdIsRejectedAsync`                                                                  | Fact   | Invalid data type id is rejected.                                                                                |
| `InvalidEncryptionIdIsRejectedAsync`                                                                | Fact   | Invalid encryption id is rejected.                                                                               |
| `DateDefaultValueMustBeIntegerOffsetOnlyWhenDataTypeIsDateAsync`                                    | Fact   | Date default value must be integer offset only when data type is date.                                           |
| `NullOrEmptyDefaultValueIsAcceptedAsync`                                                            | Fact   | Null or empty default value is accepted.                                                                         |
| `DefaultValueExceedingMaxLengthIsRejectedAsync`                                                     | Fact   | Default value exceeding max length is rejected.                                                                  |
| `SearchKeySubFieldsOnlyValidatedWhenSearchKeyEnabledAsync`                                          | Fact   | Search key sub fields only validated when search key enabled.                                                    |
| `SearchKeyCacheSubFieldsOnlyValidatedWhenBothSearchKeyAndSearchKeyCacheEnabledAsync`                | Fact   | Search key cache sub fields only validated when both search key and search key cache enabled.                    |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                                           | Fact   | Tampered identity and audit fields on insert have no effect.                                                     |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                                           | Fact   | Tampered identity and audit fields on update have no effect.                                                     |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                              | Fact   | Update of locked row throws not found.                                                                           |
| `UpdateOfSoftDeletedRowRevivesItAsync`                                                              | Fact   | Update of soft deleted row revives it.                                                                           |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                                         | Fact   | Update of never existed id throws not found.                                                                     |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                                             | Fact   | Delete of missing or already deleted row throws not found.                                                       |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                              | Fact   | Resubmit of non rejected version throws approval refused and writes no row.                                      |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                              | Fact   | Resubmit of rejected version bumps version archives old and keeps content.                                       |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                                   | Fact   | Resubmit of rejected delete keeps deleted true and reopens approval.                                             |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                                             | Fact   | Retract of non rejected version throws approval refused.                                                         |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                                           | Fact   | Retract with no approved baseline throws approval refused.                                                       |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync`               | Fact   | Retract of rejected edit restores last approved content and stamps the rejection retracted.                      |
| `RetractOfRejectedDeleteUndeletesAsync`                                                             | Fact   | Retract of rejected delete undeletes.                                                                            |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                                                    | Fact   | Active and locked round trip bool to byte and back.                                                              |
| `FieldsWithNoBackingPocoColumnAreAlwaysDefaultOnReadAsync`                                          | Fact   | Fields with no backing poco column are always default on read.                                                   |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                                   | Fact   | Get by id for missing id returns null not exception.                                                             |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                               | Fact   | Pre cancelled token on read throws operation canceled.                                                           |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                                 | Fact   | Permission denied logs warn not error and gates hold.                                                            |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                                          | Fact   | Successful insert logs exactly one info and reads log no info.                                                   |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                                           | Fact   | With gates disabled happy path records no debug info or warn.                                                    |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                                      | Fact   | Failure logs error with exception attached and still propagates.                                                 |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                                           | Fact   | Each call emits one span with outcome tag.                                                                       |
| `EachCallRecordsOneDurationMeasurementAsync`                                                        | Fact   | Each call records one duration measurement.                                                                      |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                                     | Fact   | Audit log gets exactly one line including reads.                                                                 |
| `InsertPublishesExactlyOneCreatedEventAsync`                                                        | Fact   | Insert publishes exactly one created event.                                                                      |
| `ReadsAndFailuresPublishNothingAsync`                                                               | Fact   | Reads and failures publish nothing.                                                                              |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                               | Fact   | Catalogue registers unique pascal case no underscore names.                                                      |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                                             | Fact   | List async clamps take and paginates deterministically.                                                          |

## Entity analysis model sanction

### EntityAnalysisModelSanctionServiceTests

`Jube.Test.Service.EntityAnalysisModelSanction.EntityAnalysisModelSanctionServiceTests` — 53 tests. Category: Service.
Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync`                          | Fact   | Get by entity analysis model id returns only rows for that model.                           |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `MultipartStringDataNameRequiredRejectsNullEmptyOrWhitespaceAsync`                    | Theory | Multipart string data name required rejects null empty or whitespace.                       |
| `DistanceOutOfRangeIsRejectedAsync`                                                   | Theory | Distance out of range is rejected.                                                          |
| `CacheValueNegativeIsRejectedAsync`                                                   | Fact   | Cache value negative is rejected.                                                           |
| `CacheIntervalOutsideAllowedSetIsRejectedAsync`                                       | Fact   | Cache interval outside allowed set is rejected.                                             |
| `AggregationTypeIdOutsideAllowedSetIsRejectedAsync`                                   | Fact   | Aggregation type id outside allowed set is rejected.                                        |
| `NullAggregationTypeIdIsAcceptedAsync`                                                | Fact   | Null aggregation type id is accepted.                                                       |
| `MaxDistanceRatioOutOfRangeIsRejectedAsync`                                           | Theory | Max distance ratio out of range is rejected.                                                |
| `MaxCoverageRatioNegativeIsRejectedAsync`                                             | Fact   | Max coverage ratio negative is rejected.                                                    |
| `NullMaxDistanceRatioAndMaxCoverageRatioAreAcceptedAsync`                             | Fact   | Null max distance ratio and max coverage ratio are accepted.                                |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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
| `ReportTableAndResponsePayloadRoundTripBoolToByteAndBackAsync`                        | Fact   | Report table and response payload round trip bool to byte and back.                         |
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

## Entity analysis model tag

### EntityAnalysisModelTagServiceTests

`Jube.Test.Service.EntityAnalysisModelTag.EntityAnalysisModelTagServiceTests` — 43 tests. Category: Service. Collection:
Database.

| Test                                                                                  | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                             | Fact   | Insert persists and returns version one.                                                    |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                     | Fact   | Get all returns created row and get by id returns it.                                       |
| `GetByEntityAnalysisModelIdReturnsOnlyActiveRowsForThatModelOrderedByNameAsync`       | Fact   | Get by entity analysis model id returns only active rows for that model ordered by name.    |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                                       | Fact   | Update increments version and writes audit row.                                             |
| `DeleteKeepsRowReachableByIdAndHidesItFromListsAsync`                                 | Fact   | Delete keeps row reachable by id and hides it from lists.                                   |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                  | Fact   | Every method throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                        | Theory | Null or blank user name throws not authenticated before any query.                          |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                | Fact   | User with no user in tenant row throws not authenticated.                                   |
| `UnknownUserThrowsNotAuthenticatedAsync`                                              | Fact   | Unknown user throws not authenticated.                                                      |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                                 | Fact   | User in tenant b cannot get update or delete tenant a row.                                  |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                        | Fact   | Get all as tenant b never contains tenant a rows.                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                       | Theory | Name required rejects null empty or whitespace.                                             |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                   | Fact   | Name over maximum length is rejected with error code.                                       |
| `DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync`                     | Fact   | Duplicate name within same model fails but different model succeeds.                        |
| `DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync`                           | Fact   | Duplicate name differing only by case within same model fails.                              |
| `InvalidEntityAnalysisModelIdIsRejectedAsync`                                         | Fact   | Invalid entity analysis model id is rejected.                                               |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                             | Fact   | Tampered identity and audit fields on insert have no effect.                                |
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

## Entity approval

### AdvancedAnalyticsInstanceMakerCheckerTests

`Jube.Test.Service.EntityApproval.AdvancedAnalyticsInstanceMakerCheckerTests` — 9 tests. Category: Service. Collection:
Database.

| Test                                                                      | Kind | Verifies                                                                     |
|---------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `ADeletionIsHeldBackFromTheSyncQueryUntilACheckerApprovesItAsync`         | Fact | A deletion is held back from the sync query until a checker approves it.     |
| `ARejectedDeletionLeavesTheInstanceInTheSyncQueryAsync`                   | Fact | A rejected deletion leaves the instance in the sync query.                   |
| `AnActiveFlipIsNotAppliedUntilACheckerApprovesItAsync`                    | Fact | An active flip is not applied until a checker approves it.                   |
| `ARejectedActiveFlipKeepsTheInstanceInactiveAsync`                        | Fact | A rejected active flip keeps the instance inactive.                          |
| `RenamingAnAlreadyApprovedInstanceNeverEntersAPendingStateAsync`          | Fact | Renaming an already approved instance never enters a pending state.          |
| `TheModelsValueStatusListsAPendingActivationOfItsInstanceAsync`           | Fact | The models value status lists a pending activation of its instance.          |
| `ApprovingAllPendingOnTheModelApprovesThePendingActivationAsync`          | Fact | Approving all pending on the model approves the pending activation.          |
| `ResubmittingARejectedDeletionReopensApprovalUnchangedAsync`              | Fact | Resubmitting a rejected deletion reopens approval unchanged.                 |
| `RetractingARejectedDeletionRestoresTheLastApprovedStateImmediatelyAsync` | Fact | Retracting a rejected deletion restores the last approved state immediately. |

### ApprovalNoteAndHistoryTests

`Jube.Test.Service.EntityApproval.ApprovalNoteAndHistoryTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                            | Kind | Verifies                                                             |
|-----------------------------------------------------------------|------|----------------------------------------------------------------------|
| `AnApprovalNoteIsRecordedAndShownInTheHistoryAsync`             | Fact | An approval note is recorded and shown in the history.               |
| `AnApprovalWithoutANoteStoresNoNoteAsync`                       | Fact | An approval without a note stores no note.                           |
| `ARejectionNoteIsRecordedAgainstTheVersionItRejectedAsync`      | Fact | A rejection note is recorded against the version it rejected.        |
| `APendingDeletionIsListedAsDeletedAndDropsOutOnceApprovedAsync` | Fact | A pending deletion is listed as deleted and drops out once approved. |

### EntityApprovalPipelineOrderTests

`Jube.Test.Service.EntityApproval.EntityApprovalPipelineOrderTests` — 6 tests. Category: Unit.

| Test                                                                 | Kind | Verifies                                                                        |
|----------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `EveryApprovalKindHasAPipelineRank`                                  | Fact | Every approval kind has a pipeline rank.                                        |
| `ReferenceDataAndPayloadParsingComeBeforeTheInlineStagesThatUseThem` | Fact | Reference data and payload parsing come before the inline stages that use them. |
| `InlineStagesComeBeforeRulesAndCalculationsThatReadThem`             | Fact | Inline stages come before rules and calculations that read them.                |
| `OrderFollowsThePipelineThenNameWithinAKind`                         | Fact | Order follows the pipeline then name within a kind.                             |
| `TagsComeLastBecauseTheyAreNotOnTheInvocationPath`                   | Fact | Tags come last because they are not on the invocation path.                     |
| `EveryApprovalKindHasAChangeSource`                                  | Fact | Every approval kind has a change source.                                        |

### EntityApprovalServiceTests

`Jube.Test.Service.EntityApproval.EntityApprovalServiceTests` — 32 tests. Category: Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                           |
|-------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `ACheckerWithPermissionApprovesTheCurrentVersionOfAnotherMakersChangeAsync`               | Fact | A checker with permission approves the current version of another makers change.                   |
| `AMakerCannotApproveTheirOwnChangeAsync`                                                  | Fact | A maker cannot approve their own change.                                                           |
| `AMakerCannotRejectTheirOwnChangeAsync`                                                   | Fact | A maker cannot reject their own change.                                                            |
| `ALandlordMayRejectTheirOwnChangeAsync`                                                   | Fact | A landlord may reject their own change.                                                            |
| `ANoteLongerThanTheLimitIsRefusedOnApprovalAndRejectionAsync`                             | Fact | A note longer than the limit is refused on approval and rejection.                                 |
| `ALandlordMayApproveTheirOwnChangeAsync`                                                  | Fact | A landlord may approve their own change.                                                           |
| `AUserWithoutAllowApprovalCannotApproveAsync`                                             | Fact | A user without allow approval cannot approve.                                                      |
| `AStaleVersionCannotBeApprovedAsync`                                                      | Fact | A stale version cannot be approved.                                                                |
| `ARejectedVersionCannotThenBeApprovedAsync`                                               | Fact | A rejected version cannot then be approved.                                                        |
| `ApprovingTheSameVersionTwiceByTheSameCheckerIsRefusedAsync`                              | Fact | Approving the same version twice by the same checker is refused.                                   |
| `AnUnapprovedChangeAppearsInThePendingListAndAnApprovedOneDoesNotAsync`                   | Fact | An unapproved change appears in the pending list and an approved one does not.                     |
| `ApprovingAnEntityThatDoesNotExistInThisTenantIsNotFoundAsync`                            | Fact | Approving an entity that does not exist in this tenant is not found.                               |
| `PendingByKindListsUnapprovedEntitiesOfThatKindAcrossModelsAndOmitsApprovedOnesAsync`     | Fact | Pending by kind lists unapproved entities of that kind across models and omits approved ones.      |
| `PendingByKindNeverListsAnotherTenantsEntitiesAsync`                                      | Fact | Pending by kind never lists another tenants entities.                                              |
| `PendingByKindRefusesAUserWithoutTheViewPermissionAsync`                                  | Fact | Pending by kind refuses a user without the view permission.                                        |
| `AStatusOfAnUnapprovedChangeOffersApprovalToACheckerButNotToTheMakerAsync`                | Fact | A status of an unapproved change offers approval to a checker but not to the maker.                |
| `AStatusAfterACheckerApprovesShowsTheVersionInEffectAndWithdrawsTheCheckersApprovalAsync` | Fact | A status after a checker approves shows the version in effect and withdraws the checkers approval. |
| `APendingListValueCarriesTheIdOfTheListItBelongsToAsync`                                  | Fact | A pending list value carries the id of the list it belongs to.                                     |
| `ACheckerApprovesEveryPendingValueOfAListInOneActionAsync`                                | Fact | A checker approves every pending value of a list in one action.                                    |
| `AMakerWhoAlsoChecksIsRefusedOnEveryValueAndNothingIsApprovedAsync`                       | Fact | A maker who also checks is refused on every value and nothing is approved.                         |
| `BulkApprovalRefusesAKindThatHasNoValuesAsync`                                            | Fact | Bulk approval refuses a kind that has no values.                                                   |
| `AValueStatusShowsWhichValuesOfAListAwaitApprovalAndListsThoseAwaitingFirstAsync`         | Fact | A value status shows which values of a list await approval and lists those awaiting first.         |
| `AMakerIsNotOfferedApprovalOfTheirOwnListValuesInTheValueStatusAsync`                     | Fact | A maker is not offered approval of their own list values in the value status.                      |
| `AValueStatusOfADictionaryListsItsPairsAsPendingKeyValuePairsAsync`                       | Fact | A value status of a dictionary lists its pairs as pending key value pairs.                         |
| `AValueStatusRefusesAKindThatHasNoValuesAsync`                                            | Fact | A value status refuses a kind that has no values.                                                  |
| `AValueStatusOfAListThatDoesNotExistInThisTenantIsNotFoundAsync`                          | Fact | A value status of a list that does not exist in this tenant is not found.                          |
| `ABulkApprovalOfOneListLeavesTheValuesOfAnotherListInTheSameModelPendingAsync`            | Fact | A bulk approval of one list leaves the values of another list in the same model pending.           |
| `ACreatorWithoutATenantCannotActAtAllAsync`                                               | Fact | A creator without a tenant cannot act at all.                                                      |
| `ChangesShowOnlyTheFieldThatChangedAgainstTheLatestSupersededVersionAsync`                | Fact | Changes show only the field that changed against the latest superseded version.                    |
| `ChangesDoNotReportLayoutOnlyDifferencesAsync`                                            | Fact | Changes do not report layout only differences.                                                     |
| `AFirstVersionHasNoEarlierVersionToCompareAsync`                                          | Fact | A first version has no earlier version to compare.                                                 |
| `ChangesForAVersionThatIsNoLongerCurrentAreRefusedAsync`                                  | Fact | Changes for a version that is no longer current are refused.                                       |

### MakerCheckerEngineTests

`Jube.Test.Service.EntityApproval.MakerCheckerEngineTests` — 10 tests. Category: Service. Collection: Database.

| Test                                                                             | Kind | Verifies                                                                                    |
|----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `SyncWhileChangesArePendingRecordsNoErrorsAndTheEngineLogsNoneAsync`             | Fact | Sync while changes are pending records no errors and the engine logs none.                  |
| `AnXPathAddedByAMakerRunsOnlyOnceACheckerApprovesItAsync`                        | Fact | An x path added by a maker runs only once a checker approves it.                            |
| `AnEditToARunningXPathIsNotAppliedUntilACheckerApprovesItAsync`                  | Fact | An edit to a running x path is not applied until a checker approves it.                     |
| `ARejectedEditKeepsTheApprovedVersionUntilTheMakerEditsAgainAsync`               | Fact | A rejected edit keeps the approved version until the maker edits again.                     |
| `APendingDeletionKeepsTheXPathRunningUntilACheckerApprovesItAsync`               | Fact | A pending deletion keeps the x path running until a checker approves it.                    |
| `ARejectedDeletionLeavesTheXPathRunningAsync`                                    | Fact | A rejected deletion leaves the x path running.                                              |
| `AnUpdateToAnApprovedDeletionRevivesTheXPathOnlyOnceApprovedAsync`               | Fact | An update to an approved deletion revives the x path only once approved.                    |
| `ApprovingTheDeletionOfAnXPathARuleUsesIsRefusedAndTheEngineKeepsRunningItAsync` | Fact | Approving the deletion of an x path a rule uses is refused and the engine keeps running it. |
| `ListValuesAddedByAMakerRunOnlyAfterTheCheckerApprovesThemAsync`                 | Fact | List values added by a maker run only after the checker approves them.                      |
| `ALandlordsApprovedEditToTheModelsOwnEnableLogsFlagIsAppliedByTheEngineAsync`    | Fact | A landlords approved edit to the models own enable logs flag is applied by the engine.      |

## Filter

### FilterServiceTests

`Jube.Test.Service.Filter.FilterServiceTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                   |
|----------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `FilterReturnsOnlyTheMatchingModelsOfTheCallersTenantPagedByIdAsync` | Fact | Filter returns only the matching models of the callers tenant paged by id. |
| `CountGroupsTheMatchingModelsByAFieldAsync`                          | Fact | Count groups the matching models by a field.                               |
| `AnotherTenantSeesNoneOfTheModelsAsync`                              | Fact | Another tenant sees none of the models.                                    |
| `FilterFieldsDescribeTheListDtoAsync`                                | Fact | Filter fields describe the list dto.                                       |
| `InvalidJsonComesBackAsErrorsNotAnExceptionAsync`                    | Fact | Invalid json comes back as errors not an exception.                        |
| `EveryFilterOperationNeedsTheListPermissionAsync`                    | Fact | Every filter operation needs the list permission.                          |

## Invoke

### InvokeEndpointTests

`Jube.Test.Service.Invoke.InvokeEndpointTests` — 60 tests. Collection: Database.

| Test                                                                                   | Kind   | Verifies                                                                                     |
|----------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `Invoke_WithTheDocumentationPayload_Returns200JsonFromTheRealEngineAsync`              | Fact   | Invoke with the documentation payload returns200 json from the real engine.                  |
| `Invoke_OverridesInThePayload_AreReadByTheModelAsync`                                  | Fact   | Invoke overrides in the payload are read by the model.                                       |
| `Invoke_DoesNotRequireAJsonContentTypeAsync`                                           | Theory | Invoke does not require a json content type.                                                 |
| `Invoke_AsyncSegment_ReturnsACallbackTokenInsteadOfTheResponseAsync`                   | Theory | Invoke async segment returns a callback token instead of the response.                       |
| `Invoke_AnyOtherSegment_IsSynchronousAsync`                                            | Fact   | Invoke any other segment is synchronous.                                                     |
| `Invoke_AsyncThenCallback_DeliversTheResponseOfTheAsynchronousInvocationAsync`         | Fact   | Invoke async then callback delivers the response of the asynchronous invocation.             |
| `Invoke_UnknownGuid_Returns404Async`                                                   | Fact   | Invoke unknown guid returns404.                                                              |
| `Invoke_GuidThatIsNotAGuid_Returns404Async`                                            | Fact   | Invoke guid that is not a guid returns404.                                                   |
| `Invoke_MalformedOrNonObjectJson_Returns400WithNoParserDetailAsync`                    | Theory | Invoke malformed or non object json returns400 with no parser detail.                        |
| `Invoke_MalformedJsonOnTheAsyncRoute_Returns400AndQueuesNothingAsync`                  | Fact   | Invoke malformed json on the async route returns400 and queues nothing.                      |
| `Invoke_WhenTheEngineHitsAnError_Returns500WithAnEmptyBodyAndLogsItAsync`              | Fact   | Invoke when the engine hits an error returns500 with an empty body and logs it.              |
| `Invoke_EmptyBody_Returns400WithTheMessageAsAJsonStringAsync`                          | Fact   | Invoke empty body returns400 with the message as a json string.                              |
| `Invoke_BodyOverTheConfiguredMaximum_Returns400Async`                                  | Fact   | Invoke body over the configured maximum returns400.                                          |
| `Invoke_WithoutAContentLength_Returns400Async`                                         | Fact   | Invoke without a content length returns400.                                                  |
| `Invoke_ByAUserNotGrantedTheModel_Returns403Async`                                     | Fact   | Invoke by a user not granted the model returns403.                                           |
| `Invoke_Unauthenticated_Returns401Async`                                               | Fact   | Invoke unauthenticated returns401.                                                           |
| `Invoke_WhenThePublicInvokeControllerIsDisabled_Returns404Async`                       | Fact   | Invoke when the public invoke controller is disabled returns404.                             |
| `Invoke_WhenTheEngineIsNotAvailable_Returns503Async`                                   | Fact   | Invoke when the engine is not available returns503.                                          |
| `Invoke_WithTheWrongVerb_Returns405Async`                                              | Fact   | Invoke with the wrong verb returns405.                                                       |
| `Exhaustive_UnknownInstance_Returns404Async`                                           | Theory | Exhaustive unknown instance returns404.                                                      |
| `Exhaustive_TheGuidOfAModelThatIsNotAnExhaustiveInstance_Returns404Async`              | Fact   | Exhaustive the guid of a model that is not an exhaustive instance returns404.                |
| `Exhaustive_Unauthenticated_Returns401Async`                                           | Fact   | Exhaustive unauthenticated returns401.                                                       |
| `Exhaustive_GateOffAndEngineMissing_Return404And503Async`                              | Fact   | Exhaustive gate off and engine missing return404 and503.                                     |
| `Exhaustive_ValidTypesAndMissingVariables_Return200WithANumberAsync`                   | Theory | Exhaustive valid types and missing variables return200 with a number.                        |
| `Exhaustive_WrongTypedValues_Return400WithAPlainMessageAsync`                          | Theory | Exhaustive wrong typed values return400 with a plain message.                                |
| `Exhaustive_NotAJsonObject_Returns400WithAPlainMessageAsync`                           | Theory | Exhaustive not a json object returns400 with a plain message.                                |
| `Exhaustive_EmptyBody_Returns400AsEmptyRatherThanMalformedAsync`                       | Fact   | Exhaustive empty body returns400 as empty rather than malformed.                             |
| `Exhaustive_AnOutOfRangeNumber_Returns400AsAnInvalidShapeAsync`                        | Fact   | Exhaustive an out of range number returns400 as an invalid shape.                            |
| `Exhaustive_DeeplyNestedBody_Returns400NotA500Async`                                   | Fact   | Exhaustive deeply nested body returns400 not a 500.                                          |
| `Callback_ThatNeverArrives_Returns408Async`                                            | Theory | Callback that never arrives returns408.                                                      |
| `Callback_TimeoutIsInSeconds_AndTheWaitIsCutShortByTheCacheAtCallbackTimeoutAsync`     | Fact   | Callback timeout is in seconds and the wait is cut short by the cache at callback timeout.   |
| `Callback_ClientAbandoningTheRequest_CancelsTheServerSideWaitAsync`                    | Fact   | Callback client abandoning the request cancels the server side wait.                         |
| `Callback_OfAnotherTenantOrNoTenant_IsIndistinguishableFromAGuidThatNeverArrivedAsync` | Theory | Callback of another tenant or no tenant is indistinguishable from a guid that never arrived. |
| `Callback_GuidThatIsNotAGuid_Returns404Async`                                          | Fact   | Callback guid that is not a guid returns404.                                                 |
| `Callback_Unauthenticated_Returns401Async`                                             | Fact   | Callback unauthenticated returns401.                                                         |
| `Callback_GateOffAndEngineMissing_Return404And503Async`                                | Fact   | Callback gate off and engine missing return404 and503.                                       |
| `Sanction_AnswersCamelCaseJson_WhateverTheQueryHoldsAsync`                             | Theory | Sanction answers camel case json whatever the query holds.                                   |
| `Sanction_FindsAnEntryImportedAfterTheEngineStartedWithinTheChangePollAsync`           | Fact   | Sanction finds an entry imported after the engine started within the change poll.            |
| `Sanction_InvalidInput_Returns400WithTheFluentValidationStructureAsync`                | Theory | Sanction invalid input returns400 with the fluent validation structure.                      |
| `Sanction_MultiPartStringOverTheLimit_Returns400ButTheLimitItselfIsAcceptedAsync`      | Fact   | Sanction multi part string over the limit returns400 but the limit itself is accepted.       |
| `Sanction_SeveralInvalidValues_ReportEveryErrorInOneResponseAsync`                     | Fact   | Sanction several invalid values report every error in one response.                          |
| `Sanction_HostileStrings_AreSearchedAsDataAnd200Async`                                 | Theory | Sanction hostile strings are searched as data and200.                                        |
| `Sanction_HostileNumbersAndStrings_AreNeverA500AndNeverLeakAsync`                      | Fact   | Sanction hostile numbers and strings are never a 500 and never leak.                         |
| `Sanction_InvalidInputDoesNotTakeTheGatesOrTheEngineChecksAsync`                       | Fact   | Sanction invalid input does not take the gates or the engine checks.                         |
| `Sanction_Unauthenticated_Returns401Async`                                             | Fact   | Sanction unauthenticated returns401.                                                         |
| `Sanction_GateOff_EngineOff_EngineMissing_Return404_404_503Async`                      | Fact   | Sanction gate off engine off engine missing return404 404 503.                               |
| `Sanction_WithThePostVerb_Returns405Async`                                             | Fact   | Sanction with the post verb returns405.                                                      |
| `Tag_WithAJsonMediaType_Returns200Async`                                               | Theory | Tag with a json media type returns200.                                                       |
| `Tag_PropertyNamesAreMatchedIgnoringCaseAsync`                                         | Theory | Tag property names are matched ignoring case.                                                |
| `Tag_EmptyObject_Returns200_ButJsonNullAndMalformedJson_Return400Async`                | Fact   | Tag empty object returns200 but json null and malformed json return400.                      |
| `Tag_BodyOverTheConfiguredMaximum_Returns400WithoutBufferingItAllAsync`                | Fact   | Tag body over the configured maximum returns400 without buffering it all.                    |
| `Tag_WithoutAJsonMediaType_Returns415Async`                                            | Theory | Tag without a json media type returns415.                                                    |
| `Tag_Unauthenticated_Returns401Async`                                                  | Fact   | Tag unauthenticated returns401.                                                              |
| `Tag_GateOffAndEngineMissing_Return404And503_EvenForMalformedJsonAsync`                | Fact   | Tag gate off and engine missing return404 and503 even for malformed json.                    |
| `Invoke_ResponseCarriesTheActivationsAndElevationOfTheModelAsync`                      | Fact   | Invoke response carries the activations and elevation of the model.                          |
| `Invoke_MissingFields_AreDefaultedByTheModel_NotRejectedAsync`                         | Theory | Invoke missing fields are defaulted by the model not rejected.                               |
| `Invoke_MissingTxnId_GivesAnEmptyEntryIdAsync`                                         | Fact   | Invoke missing txn id gives an empty entry id.                                               |
| `Invoke_MissingOrUnreadableReferenceDate_DefaultsToNowAsync`                           | Theory | Invoke missing or unreadable reference date defaults to now.                                 |
| `Invoke_ReferenceDateInTheFuture_Returns400WithTheMessageAsAJsonStringAsync`           | Fact   | Invoke reference date in the future returns400 with the message as a json string.            |
| `Invoke_UnreadableOrUnmappedValues_DoNotFailTheInvocationAsync`                        | Theory | Invoke unreadable or unmapped values do not fail the invocation.                             |

## Rule execution

### RuleExecutionServiceTests

`Jube.Test.Service.RuleExecution.RuleExecutionServiceTests` — 8 tests. Category: Service. Collection: Database.

| Test                                                                    | Kind   | Verifies                                                                       |
|-------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `AGatewayRuleRunsAgainstAContextBuiltByTheContextServiceAsync`          | Theory | A gateway rule runs against a context built by the context service.            |
| `ListsAreReadFromTheModelAndUnsetNamesAreFlaggedAsync`                  | Fact   | Lists are read from the model and unset names are flagged.                     |
| `ARuleThatDoesNotCompileReportsLocatedErrorsAndRunsNothingAsync`        | Fact   | A rule that does not compile reports located errors and runs nothing.          |
| `AContextForAnotherModelIsRefusedAsync`                                 | Fact   | A context for another model is refused.                                        |
| `CalculationsAndInlineFunctionsReturnTheirValuesAsync`                  | Fact   | Calculations and inline functions return their values.                         |
| `EveryVbNetRuleServiceExposesACataloguedExecute`                        | Theory | Every vb net rule service exposes a catalogued execute.                        |
| `InlineScriptExecutionIsRefusedUnlessEnabledAsync`                      | Fact   | Inline script execution is refused unless enabled.                             |
| `BuilderJsonBecomesARuleThatSavesAndItsProblemsCarryTheirJsonPathAsync` | Fact   | Builder json becomes a rule that saves and its problems carry their json path. |

## Version history

### VersionDiffTests

`Jube.Test.Service.VersionHistory.VersionDiffTests` — 6 tests. Category: Unit.

| Test                                               | Kind | Verifies                                                 |
|----------------------------------------------------|------|----------------------------------------------------------|
| `LayoutOnlyDifferencesAreNotReportedAsChanges`     | Fact | Layout only differences are not reported as changes.     |
| `ATokenChangeIsReported`                           | Fact | A token change is reported.                              |
| `IgnoredFieldsAreSkipped`                          | Fact | Ignored fields are skipped.                              |
| `NonSimplePropertiesAreNotCompared`                | Fact | Non simple properties are not compared.                  |
| `AcrossTypesOnlySharedSimplePropertiesAreCompared` | Fact | Across types only shared simple properties are compared. |
| `MissingSidesProduceNoChanges`                     | Fact | Missing sides produce no changes.                        |

### VersionHistoryProjectionTests

`Jube.Test.Service.VersionHistory.VersionHistoryProjectionTests` — 11 tests. Category: Unit.

| Test                                                                     | Kind | Verifies                                                                            |
|--------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `TheProbeShapesCarryTheValuesTheyAreGivenAsTheyAreRead`                  | Fact | The probe shapes carry the values they are given as they are read.                  |
| `AVersionFieldTheDtoDoesNotExposeIsNotExposedAtAll`                      | Fact | A version field the dto does not expose is not exposed at all.                      |
| `TheUserPasswordHashIsNotExposedByTheUserRegistryDto`                    | Fact | The user password hash is not exposed by the user registry dto.                     |
| `TheExhaustiveFilterTokensAreExposedOnlyBecauseItsDtoAlreadyExposesThem` | Fact | The exhaustive filter tokens are exposed only because its dto already exposes them. |
| `TheVersionMetadataAndParentKeyAreExposedWhetherOrNotTheDtoNamesThem`    | Fact | The version metadata and parent key are exposed whether or not the dto names them.  |
| `TheParentKeyIsExposedFromTheVersionTypeName`                            | Fact | The parent key is exposed from the version type name.                               |
| `ComplexValuesAreNeverExposedEvenWhenTheDtoNamesThem`                    | Fact | Complex values are never exposed even when the dto names them.                      |
| `ProjectingARowReturnsOnlyTheExposedFields`                              | Fact | Projecting a row returns only the exposed fields.                                   |
| `ProjectingAnUnspecifiedCreatedDateStampsItAsUtc`                        | Fact | Projecting an unspecified created date stamps it as utc.                            |
| `ChangesOutsideTheExposedFieldsAreDropped`                               | Fact | Changes outside the exposed fields are dropped.                                     |
| `ChangeValuesAreShownUpToTwoHundredCharactersThenEllipsed`               | Fact | Change values are shown up to two hundred characters then ellipsed.                 |

### VersionTextTests

`Jube.Test.Service.VersionHistory.VersionTextTests` — 12 tests. Category: Unit.

| Test                                                     | Kind   | Verifies                                                         |
|----------------------------------------------------------|--------|------------------------------------------------------------------|
| `NormaliseStripsLineEndingsIndentationAndBlankLines`     | Theory | Normalise strips line endings indentation and blank lines.       |
| `NormaliseKeepsSpacingInsideALine`                       | Fact   | Normalise keeps spacing inside a line.                           |
| `TextThatDiffersOnlyInLayoutMatches`                     | Theory | Text that differs only in layout matches.                        |
| `NullAndEmptyTextMatch`                                  | Fact   | Null and empty text match.                                       |
| `ATokenChangeDoesNotMatch`                               | Fact   | A token change does not match.                                   |
| `TextInsideAStringLiteralStillCountsAsAChange`           | Fact   | Text inside a string literal still counts as a change.           |
| `NonTextValuesMatchOnEquality`                           | Fact   | Non text values match on equality.                               |
| `ChangeKindIsAddedRemovedOrChanged`                      | Fact   | Change kind is added removed or changed.                         |
| `PreviewKeepsTheFirstTwoHundredCharactersThenAnEllipsis` | Fact   | Preview keeps the first two hundred characters then an ellipsis. |
| `PreviewLeavesTextOfTwoHundredCharactersOrFewerWhole`    | Fact   | Preview leaves text of two hundred characters or fewer whole.    |
| `PreviewRendersNonTextValues`                            | Fact   | Preview renders non text values.                                 |
| `ShortenLeavesNonTextValuesAlone`                        | Fact   | Shorten leaves non text values alone.                            |
